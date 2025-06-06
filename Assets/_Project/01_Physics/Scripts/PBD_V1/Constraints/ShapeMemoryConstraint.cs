using System.Collections.Generic;
using UnityEngine;

namespace _Project._01_Physics.Scripts.PBD_V1.Constraints
{
    /// <summary>
    /// Constraint that tries to restore particles to their original positions
    /// This is what gives rubber its "shape memory" behavior
    /// </summary>
    public class ShapeMemoryConstraint : PBDConstraint
    {
        public int ParticleIndex;
        public Vector3 OriginalLocalPosition; // Position relative to center of mass
        public Vector3 CenterOfMass;
        public float MemoryStrength = 1.0f;

        public ShapeMemoryConstraint(int particleIndex, Vector3 originalLocalPos, float strength = 1.0f)
        {
            ParticleIndex = particleIndex;
            OriginalLocalPosition = originalLocalPos;
            MemoryStrength = strength;
            Stiffness = strength;
        }

        public void UpdateCenterOfMass(Vector3 newCenter)
        {
            CenterOfMass = newCenter;
        }

        public override void SolveConstraint(List<PBDParticle> particles, float globalStiffness)
        {
            if (!IsActive || ParticleIndex >= particles.Count) return;

            var particle = particles[ParticleIndex];
            if (!particle.IsActive || particle.IsFixed) return;

            // Calculate where this particle SHOULD be
            Vector3 targetWorldPosition = CenterOfMass + OriginalLocalPosition;
            
            // Calculate correction needed
            Vector3 correction = targetWorldPosition - particle.PredictedPosition;
            
            // Apply shape memory correction (gentler than distance constraints)
            float effectiveStiffness = MemoryStrength * globalStiffness * 0.3f; // Gentler than distance
            Vector3 correctionForce = correction * effectiveStiffness;
            
            if (particle.InverseMass > 0)
            {
                particle.PredictedPosition += correctionForce * particle.InverseMass;
            }
        }

        public override bool IsSatisfied(List<PBDParticle> particles, float tolerance = 0.1f)
        {
            if (ParticleIndex >= particles.Count) return true;
            
            var particle = particles[ParticleIndex];
            Vector3 targetPos = CenterOfMass + OriginalLocalPosition;
            float distance = Vector3.Distance(particle.PredictedPosition, targetPos);
            
            return distance < tolerance;
        }
    }
}