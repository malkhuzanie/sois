// Assets/_Project/01_Physics/Scripts/XPBD/Constraints/ConstraintStabilizer.cs

using System.Collections.Generic;
using _Project._01_Physics.Scripts.XPBD.Core;
using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Constraints
{
    /// <summary>
    /// Stabilization system that dampens internal constraint forces when the object should be at rest
    /// </summary>
    public class ConstraintStabilizer : XPBDConstraint
    {
        public float RestThreshold = 0.1f;          // Speed below which object is considered "at rest"
        public float RestDuration = 0.3f;           // Time object must be slow to be considered "settled"
        public float InternalDamping = 0.8f;        // Damping applied to internal forces when settled
        public float GroundY = 0.0f;                // Ground level for contact detection
        public float ContactThreshold = 0.2f;       // Distance considered "in contact" with ground
        
        // State tracking
        private Dictionary<int, float> restTime = new Dictionary<int, float>();
        private Dictionary<int, Vector3> restPosition = new Dictionary<int, Vector3>();
        private float globalRestTime = 0f;
        private Vector3 lastCenterOfMass = Vector3.zero;
        private bool isGloballyAtRest = false;
        
        public ConstraintStabilizer(float groundY, float restThreshold = 0.1f, float internalDamping = 0.8f)
        {
            GroundY = groundY;
            RestThreshold = restThreshold;
            InternalDamping = internalDamping;
            Compliance = 0.0f;
        }
        
        public override void SolveConstraint(List<XPBDParticle> particles, float deltaTime)
        {
            // Calculate global rest state
            UpdateGlobalRestState(particles, deltaTime);
            
            // Apply stabilization to individual particles
            for (int i = 0; i < particles.Count; i++)
            {
                var particle = particles[i];
                if (particle.IsFixed) continue;
                
                UpdateParticleRestState(i, particle, deltaTime);
                
                // Apply stabilization if particle or object is at rest
                if (ShouldStabilizeParticle(i, particle))
                {
                    ApplyStabilization(particle, deltaTime, i);
                }
            }
        }
        
        private void UpdateGlobalRestState(List<XPBDParticle> particles, float deltaTime)
        {
            // Calculate center of mass and average velocity
            Vector3 centerOfMass = Vector3.zero;
            Vector3 totalVelocity = Vector3.zero;
            int activeParticles = 0;
            bool anyInContact = false;
            
            foreach (var particle in particles)
            {
                if (!particle.IsFixed)
                {
                    centerOfMass += particle.Position;
                    Vector3 velocity = (particle.Position - particle.PreviousPosition) / deltaTime;
                    totalVelocity += velocity;
                    activeParticles++;
                    
                    // Check if any particle is in contact with ground
                    if (particle.Position.y - GroundY <= ContactThreshold)
                    {
                        anyInContact = true;
                    }
                }
            }
            
            if (activeParticles > 0)
            {
                centerOfMass /= activeParticles;
                float avgSpeed = totalVelocity.magnitude / activeParticles;
                
                // Check if object is globally at rest
                bool shouldBeAtRest = avgSpeed < RestThreshold && anyInContact;
                
                if (shouldBeAtRest)
                {
                    globalRestTime += deltaTime;
                    isGloballyAtRest = globalRestTime > RestDuration;
                }
                else
                {
                    globalRestTime = 0f;
                    isGloballyAtRest = false;
                }
                
                lastCenterOfMass = centerOfMass;
            }
        }
        
        private void UpdateParticleRestState(int particleIndex, XPBDParticle particle, float deltaTime)
        {
            Vector3 velocity = (particle.Position - particle.PreviousPosition) / deltaTime;
            float speed = velocity.magnitude;
            bool inContact = particle.Position.y - GroundY <= ContactThreshold;
            
            if (speed < RestThreshold && inContact)
            {
                // Particle is moving slowly and in contact
                restTime[particleIndex] = restTime.GetValueOrDefault(particleIndex, 0f) + deltaTime;
                
                // Update rest position if this is a new rest state
                if (restTime[particleIndex] <= deltaTime * 2f)
                {
                    restPosition[particleIndex] = particle.Position;
                }
            }
            else
            {
                // Particle is moving fast or not in contact
                restTime[particleIndex] = 0f;
                restPosition.Remove(particleIndex);
            }
        }
        
        private bool ShouldStabilizeParticle(int particleIndex, XPBDParticle particle)
        {
            float particleRestTime = restTime.GetValueOrDefault(particleIndex, 0f);
            bool particleAtRest = particleRestTime > RestDuration;
            
            // Stabilize if particle is individually at rest OR object is globally at rest
            return particleAtRest || isGloballyAtRest;
        }
        
        private void ApplyStabilization(XPBDParticle particle, float deltaTime, int particleIndex)
        {
            Vector3 velocity = (particle.Position - particle.PreviousPosition) / deltaTime;
            
            // Apply damping to reduce internal oscillations
            float dampingStrength = InternalDamping;
            
            // Stronger damping if globally at rest
            if (isGloballyAtRest)
            {
                dampingStrength = Mathf.Max(dampingStrength, 0.9f);
            }
            
            // Apply velocity damping
            particle.PreviousPosition = particle.Position - velocity * dampingStrength * deltaTime;
            
            // Position stabilization toward rest position if available
            if (restPosition.ContainsKey(particleIndex))
            {
                Vector3 restPos = restPosition[particleIndex];
                float distanceFromRest = Vector3.Distance(particle.Position, restPos);
                
                if (distanceFromRest > 0.01f) // Only correct if significantly displaced
                {
                    // Gentle correction toward rest position
                    Vector3 correction = (restPos - particle.Position) * 0.05f; // 5% correction
                    particle.PredictedPosition += correction;
                }
            }
            
            // Extra stabilization for very small movements
            if (velocity.magnitude < 0.02f)
            {
                // Nearly stopped - apply very strong damping
                particle.PreviousPosition.x = particle.Position.x - velocity.x * 0.01f * deltaTime;
                particle.PreviousPosition.z = particle.Position.z - velocity.z * 0.01f * deltaTime;
            }
        }
        
        public override float EvaluateConstraint(List<XPBDParticle> particles)
        {
            // This constraint doesn't enforce a specific geometric constraint
            // It only provides stabilization, so constraint error is always 0
            return 0.0f;
        }
        
        // Public interface for debugging
        public bool IsGloballyAtRest => isGloballyAtRest;
        public float GlobalRestTime => globalRestTime;
        public int ParticlesAtRest => restTime.Count;
    }
}