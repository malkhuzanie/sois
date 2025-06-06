// Assets/_Project/01_Physics/Scripts/XPBD/Constraints/FinalGroundConstraint.cs

using System.Collections.Generic;
using _Project._01_Physics.Scripts.XPBD.Core;
using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Constraints
{
    /// <summary>
    /// Final, clean ground constraint solution - simple and stable
    /// Replaces EnhancedXPBDGroundConstraint with better friction logic
    /// </summary>
    public class FinalGroundConstraint : XPBDConstraint
    {
        public float GroundY = 0.0f;
        public float Restitution = 0.75f;
        public float Friction = 0.8f;
        public float ContactThreshold = 0.1f;
        
        // Simple state tracking - just contact time
        private Dictionary<int, float> contactTime = new Dictionary<int, float>();
        
        public FinalGroundConstraint(float groundY, float restitution = 0.75f, float friction = 0.8f)
        {
            GroundY = groundY;
            Restitution = restitution;
            Friction = friction;
            Compliance = 0.0f;
        }
        
        public override void SolveConstraint(List<XPBDParticle> particles, float deltaTime)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                var particle = particles[i];
                if (particle.IsFixed) continue;
                
                float distanceToGround = particle.PredictedPosition.y - GroundY;
                bool isInContact = distanceToGround <= ContactThreshold;
                
                if (isInContact)
                {
                    // Update contact time
                    contactTime[i] = contactTime.GetValueOrDefault(i, 0f) + deltaTime;
                    
                    // Calculate velocity
                    Vector3 velocity = (particle.Position - particle.PreviousPosition) / deltaTime;
                    
                    // 1. POSITION CORRECTION - Simple penetration fix
                    if (particle.PredictedPosition.y < GroundY)
                    {
                        particle.PredictedPosition.y = GroundY;
                    }
                    
                    // 2. VERTICAL VELOCITY - Simple bounce
                    if (velocity.y < -0.1f) // Significant downward velocity
                    {
                        float bounceVelocity = -velocity.y * Restitution;
                        particle.PreviousPosition.y = particle.Position.y - bounceVelocity * deltaTime;
                    }
                    else if (velocity.y > -0.1f && velocity.y < 0.3f)
                    {
                        // Small vertical velocity - gradually stop
                        particle.PreviousPosition.y = particle.Position.y - velocity.y * 0.8f * deltaTime;
                    }
                    
                    // 3. HORIZONTAL FRICTION - Progressive and simple
                    ApplySimpleFriction(particle, velocity, deltaTime, i);
                }
                else
                {
                    // Reset contact time when not touching ground
                    contactTime[i] = 0f;
                }
            }
        }
        
        private void ApplySimpleFriction(XPBDParticle particle, Vector3 velocity, float deltaTime, int particleIndex)
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;
            
            if (horizontalSpeed < 0.001f) return; // No horizontal movement
            
            float timeInContact = contactTime.GetValueOrDefault(particleIndex, 0f);
            
            // Progressive friction: starts normal, increases with contact time
            float effectiveFriction = Friction;
            
            if (timeInContact > 0.2f) // After 0.2 seconds of contact
            {
                effectiveFriction = Friction * 1.5f; // 50% more friction
            }
            
            if (timeInContact > 0.5f) // After 0.5 seconds of contact
            {
                effectiveFriction = Friction * 2.0f; // Double friction
            }
            
            // Apply friction force
            Vector3 frictionDirection = -horizontalVelocity.normalized;
            float frictionForce = effectiveFriction * 9.81f; // friction * gravity
            float frictionImpulse = frictionForce * deltaTime;
            
            // Don't apply more friction than needed to stop the motion
            float frictionMagnitude = Mathf.Min(frictionImpulse, horizontalSpeed);
            Vector3 frictionVelocityChange = frictionDirection * frictionMagnitude;
            
            // Apply friction by modifying previous position
            particle.PreviousPosition.x -= frictionVelocityChange.x * deltaTime;
            particle.PreviousPosition.z -= frictionVelocityChange.z * deltaTime;
            
            // Final stop for very low speeds
            if (horizontalSpeed < 0.05f && timeInContact > 0.3f)
            {
                // Apply strong damping to completely stop
                particle.PreviousPosition.x = particle.Position.x - velocity.x * 0.1f * deltaTime;
                particle.PreviousPosition.z = particle.Position.z - velocity.z * 0.1f * deltaTime;
            }
            
            // Emergency complete stop for persistent tiny movements
            if (horizontalSpeed < 0.01f && timeInContact > 1.0f)
            {
                particle.PreviousPosition.x = particle.Position.x;
                particle.PreviousPosition.z = particle.Position.z;
            }
        }
        
        public override float EvaluateConstraint(List<XPBDParticle> particles)
        {
            float maxPenetration = 0.0f;
            foreach (var particle in particles)
            {
                if (particle.PredictedPosition.y < GroundY)
                {
                    maxPenetration = Mathf.Max(maxPenetration, GroundY - particle.PredictedPosition.y);
                }
            }
            return maxPenetration;
        }
    }
}