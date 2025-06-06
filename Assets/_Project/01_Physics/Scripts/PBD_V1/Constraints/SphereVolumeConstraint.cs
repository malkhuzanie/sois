using System.Collections.Generic;
using UnityEngine;

namespace _Project._01_Physics.Scripts.PBD_V1.Constraints
{
    /// <summary>
    /// Maintains the spherical volume of the object
    /// Prevents excessive compression/expansion
    /// </summary>
    public class SphereVolumeConstraint : PBDConstraint
    {
        public Vector3 CenterOfMass;
        public float OriginalRadius;
        public float VolumeStiffness = 0.8f;
        public List<int> SurfaceParticleIndices;

        public SphereVolumeConstraint(List<int> surfaceParticles, float originalRadius, float stiffness = 0.8f)
        {
            SurfaceParticleIndices = new List<int>(surfaceParticles);
            OriginalRadius = originalRadius;
            VolumeStiffness = stiffness;
            Stiffness = stiffness;
        }

        public void UpdateCenterOfMass(Vector3 newCenter)
        {
            CenterOfMass = newCenter;
        }

        public override void SolveConstraint(List<PBDParticle> particles, float globalStiffness)
        {
            if (!IsActive || SurfaceParticleIndices.Count == 0) return;

            // Calculate current average radius
            float totalDistance = 0f;
            int validParticles = 0;

            foreach (int index in SurfaceParticleIndices)
            {
                if (index < particles.Count && particles[index].IsActive)
                {
                    float distance = Vector3.Distance(particles[index].PredictedPosition, CenterOfMass);
                    totalDistance += distance;
                    validParticles++;
                }
            }

            if (validParticles == 0) return;

            float currentRadius = totalDistance / validParticles;
            float radiusError = currentRadius - OriginalRadius;

            // If radius is too different from original, correct it
            if (Mathf.Abs(radiusError) > 0.01f)
            {
                float correctionFactor = VolumeStiffness * globalStiffness * 0.1f;
                
                foreach (int index in SurfaceParticleIndices)
                {
                    if (index < particles.Count && particles[index].IsActive && particles[index].InverseMass > 0)
                    {
                        var particle = particles[index];
                        Vector3 directionFromCenter = (particle.PredictedPosition - CenterOfMass).normalized;
                        
                        // Push particle towards correct radius
                        Vector3 targetPosition = CenterOfMass + directionFromCenter * OriginalRadius;
                        Vector3 correction = (targetPosition - particle.PredictedPosition) * correctionFactor;
                        
                        particle.PredictedPosition += correction * particle.InverseMass;
                    }
                }
            }
        }

        public override bool IsSatisfied(List<PBDParticle> particles, float tolerance = 0.1f)
        {
            // Check if average radius is close to original
            float totalDistance = 0f;
            int validParticles = 0;

            foreach (int index in SurfaceParticleIndices)
            {
                if (index < particles.Count && particles[index].IsActive)
                {
                    totalDistance += Vector3.Distance(particles[index].PredictedPosition, CenterOfMass);
                    validParticles++;
                }
            }

            if (validParticles == 0) return true;
            
            float currentRadius = totalDistance / validParticles;
            return Mathf.Abs(currentRadius - OriginalRadius) < tolerance;
        }
    }
}