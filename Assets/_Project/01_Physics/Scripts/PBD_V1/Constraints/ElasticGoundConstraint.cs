using System.Collections.Generic;
using UnityEngine;

namespace _Project._01_Physics.Scripts.PBD_V1.Constraints
{
    /// <summary>
    /// Gentler ground constraint specifically for elastic (shape-preserving) objects
    /// </summary>
    public class ElasticGroundConstraint : PBDConstraint
    {
        public float GroundY;
        public float Restitution = 0.8f;
        public float Friction = 0.3f;
        public float SoftnessFactor = 0.5f; // Makes ground collision softer

        public ElasticGroundConstraint(float groundY, float restitution = 0.8f, float friction = 0.3f)
        {
            GroundY = groundY;
            Restitution = restitution;
            Friction = friction;
            Stiffness = 1.0f;
        }

        public override void SolveConstraint(List<PBDParticle> particles, float globalStiffness)
        {
            foreach (var particle in particles)
            {
                if (particle.IsFixed || !particle.IsActive) continue;

                // Check if particle is below ground
                if (particle.PredictedPosition.y < GroundY)
                {
                    // Gentler position correction with softness factor
                    float penetration = GroundY - particle.PredictedPosition.y;
                    float correctionAmount = penetration * SoftnessFactor;
                    
                    particle.PredictedPosition.y = particle.PredictedPosition.y + correctionAmount;

                    // Gentler velocity correction
                    if (particle.Velocity.y < 0)
                    {
                        float impactSpeed = Mathf.Abs(particle.Velocity.y);
                        
                        // Apply restitution more gradually
                        float bounceSpeed = impactSpeed * Restitution;
                        
                        // Smooth the velocity change to prevent jarring
                        particle.Velocity.y = Mathf.Lerp(particle.Velocity.y, bounceSpeed, 0.5f);

                        // Gentle friction application
                        float frictionReduction = 1f - (Friction * 0.1f);
                        particle.Velocity.x *= frictionReduction;
                        particle.Velocity.z *= frictionReduction;
                        
                        // NO stress accumulation for elastic objects
                        // particle.AddStress(...) - Remove this
                    }
                }
            }
        }
        
        public override bool IsSatisfied(List<PBDParticle> particles, float tolerance = 0.01f)
        {
            foreach (var particle in particles)
            {
                if (particle.IsActive && particle.PredictedPosition.y < GroundY - tolerance)
                    return false;
            }
            return true;
        }
    }
}