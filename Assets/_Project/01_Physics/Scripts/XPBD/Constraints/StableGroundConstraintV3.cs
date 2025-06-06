using System.Collections.Generic;
using System.Linq;
using _Project._01_Physics.Scripts.XPBD.Core;
using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Constraints
{
    /// <summary>
    /// V3: Definitive ground constraint with proper rest detection and friction
    /// Changes from V2/Final:
    /// - Implements velocity-based rest detection
    /// - Uses stronger static friction when at rest
    /// - Applies position stabilization to prevent drift
    /// - Includes energy dissipation to prevent perpetual motion
    /// </summary>
    public class StableGroundConstraintV3 : XPBDConstraint
    {
        public readonly float GroundY = 0.0f;
        public readonly float Restitution = 0.7f;
        public readonly float DynamicFriction = 0.6f;
        public readonly float StaticFriction = 0.9f;
        public const float RestVelocityThreshold = 0.05f; // Below this speed, consider at rest
        public readonly float PositionStabilization = 0.95f; // How much to correct position drift
        
        // Per-particle state
        private Dictionary<int, ParticleGroundState> particleStates = new Dictionary<int, ParticleGroundState>();
        
        private class ParticleGroundState
        {
            public float ContactTime = 0f;
            public bool IsResting = false;
            public Vector3 RestPosition = Vector3.zero;
            public int RestFrames = 0;
        }
        
        public StableGroundConstraintV3(float groundY, float restitution = 0.7f, 
            float dynamicFriction = 0.6f, float staticFriction = 0.9f)
        {
            GroundY = groundY;
            Restitution = restitution;
            DynamicFriction = dynamicFriction;
            StaticFriction = staticFriction;
            Compliance = 0.0f;
        }
        
        public override void SolveConstraint(List<XPBDParticle> particles, float deltaTime)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                var particle = particles[i];
                if (particle.IsFixed) continue;
                
                // Get or create state
                if (!particleStates.TryGetValue(i, out var state))
                {
                    state = new ParticleGroundState();
                    particleStates[i] = state;
                }
                
                // Check ground contact
                float penetration = GroundY - particle.PredictedPosition.y;
                bool isInContact = penetration > -0.01f; // Small tolerance
                
                if (isInContact)
                {
                    // Update contact time
                    state.ContactTime += deltaTime;
                    
                    // Position correction - always keep above ground
                    if (particle.PredictedPosition.y < GroundY)
                    {
                        particle.PredictedPosition.y = GroundY;
                    }
                    
                    // Calculate velocities
                    Vector3 velocity = particle.GetVelocity(deltaTime);
                    float verticalSpeed = Mathf.Abs(velocity.y);
                    float horizontalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
                    
                    // Rest detection
                    bool shouldBeResting = state.ContactTime > 0.1f && 
                                         verticalSpeed < RestVelocityThreshold && 
                                         horizontalSpeed < RestVelocityThreshold;
                    
                    if (shouldBeResting)
                    {
                        if (!state.IsResting)
                        {
                            // Just started resting - record position
                            state.IsResting = true;
                            state.RestPosition = particle.Position;
                            state.RestFrames = 0;
                        }
                        else
                        {
                            state.RestFrames++;
                        }
                        
                        // Apply strong stabilization when resting
                        ApplyRestStabilization(particle, state, deltaTime);
                    }
                    else
                    {
                        state.IsResting = false;
                        state.RestFrames = 0;
                        
                        // Normal collision response
                        ApplyCollisionResponse(particle, velocity, state, deltaTime);
                    }
                }
                else
                {
                    // Not in contact - reset state
                    state.ContactTime = 0f;
                    state.IsResting = false;
                    state.RestFrames = 0;
                }
            }
        }
        
        private void ApplyRestStabilization(XPBDParticle particle, ParticleGroundState state, float deltaTime)
        {
            // Strong position stabilization to prevent drift
            Vector3 targetPos = state.RestPosition;
            targetPos.y = GroundY; // Ensure on ground
            
            // Blend towards rest position
            particle.PredictedPosition = Vector3.Lerp(particle.PredictedPosition, targetPos, PositionStabilization);
            
            // Kill velocity completely after a few frames at rest
            if (state.RestFrames > 5)
            {
                // Set previous position to current to zero velocity
                particle.PreviousPosition = particle.Position;
                particle.PredictedPosition.y = GroundY;
                
                // Lock horizontal position
                particle.PredictedPosition.x = targetPos.x;
                particle.PredictedPosition.z = targetPos.z;
            }
        }
        
        private void ApplyCollisionResponse(XPBDParticle particle, Vector3 velocity, 
            ParticleGroundState state, float deltaTime)
        {
            // Vertical response with energy dissipation
            if (velocity.y < -0.01f)
            {
                // Bounce with restitution and energy loss
                float bounceVel = -velocity.y * Restitution;
                
                // Additional energy dissipation for small bounces
                if (bounceVel < 0.5f)
                {
                    bounceVel *= 0.5f; // Extra damping for micro-bounces
                }
                
                particle.PreviousPosition.y = particle.Position.y - bounceVel * deltaTime;
            }
            else if (Mathf.Abs(velocity.y) < 0.1f)
            {
                // Very small vertical motion - just stop it
                particle.PreviousPosition.y = particle.Position.y;
            }
            
            // Horizontal friction
            Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
            float horizontalSpeed = horizontalVel.magnitude;
            
            if (horizontalSpeed > 0.001f)
            {
                // Determine friction coefficient
                float frictionCoeff = velocity.magnitude < RestVelocityThreshold * 2f 
                    ? StaticFriction 
                    : DynamicFriction;
                
                // Friction force
                Vector3 frictionDir = -horizontalVel.normalized;
                float maxFrictionDeltaV = frictionCoeff * 9.81f * deltaTime;
                
                // Apply friction (but don't reverse direction)
                float frictionDeltaV = Mathf.Min(maxFrictionDeltaV, horizontalSpeed);
                Vector3 velocityChange = frictionDir * frictionDeltaV;
                
                particle.PreviousPosition.x -= velocityChange.x * deltaTime;
                particle.PreviousPosition.z -= velocityChange.z * deltaTime;
                
                // Extra damping for slow motion
                if (horizontalSpeed < RestVelocityThreshold)
                {
                    particle.PreviousPosition.x = particle.Position.x - horizontalVel.x * 0.1f * deltaTime;
                    particle.PreviousPosition.z = particle.Position.z - horizontalVel.z * 0.1f * deltaTime;
                }
            }
        }
        
        public override float EvaluateConstraint(List<XPBDParticle> particles)
        {
            return particles
                .Where(particle => particle.PredictedPosition.y < GroundY)
                .Aggregate(0.0f, (current, particle) => Mathf.Max(current, GroundY - particle.PredictedPosition.y));
        }
        
        // Public method to check if all particles are resting
        public bool AreAllParticlesResting()
        {
            if (particleStates.Values.Any(state => !state.IsResting))
            {
                return false;
            }

            return particleStates.Count > 0;
        }
    }
}