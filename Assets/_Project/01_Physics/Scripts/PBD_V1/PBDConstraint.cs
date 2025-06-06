// Assets/_Project/01_Physics/Scripts/PBD/PBDConstraint.cs

using System.Collections.Generic;
using UnityEngine;

namespace _Project._01_Physics.Scripts.PBD_V1
{
    /// <summary>
    /// Base class for all PBD constraints with fracture support
    /// </summary>
    public abstract class PBDConstraint
    {
        public float Stiffness = 1.0f;
        public bool IsActive = true;
        public bool CanBreak = false;
        public float BreakThreshold = float.MaxValue;

        protected float _currentStress = 0f;

        public abstract void SolveConstraint(List<PBDParticle> particles, float stiffness);
        public abstract bool IsSatisfied(List<PBDParticle> particles, float tolerance = 0.01f);

        public virtual bool ShouldBreak()
        {
            return CanBreak && _currentStress > BreakThreshold;
        }

        public virtual void Break()
        {
            IsActive = false;
        }

        public float GetCurrentStress() => _currentStress;
    }

    /// <summary>
    /// Improved distance constraint with fracture mechanics
    /// </summary>
    public class DistanceConstraint : PBDConstraint
    {
        public int ParticleA;
        public int ParticleB;
        public float RestLength;

        // Fracture properties
        public float MaxStrain = 0.5f; // Maximum allowable strain before breaking
        public bool IsBroken = false;

        public DistanceConstraint(int particleA, int particleB, float restLength, float stiffness = 1.0f,
            bool canBreak = false)
        {
            ParticleA = particleA;
            ParticleB = particleB;
            RestLength = restLength;
            Stiffness = stiffness;
            CanBreak = canBreak;

            if (canBreak)
            {
                // Set break threshold based on material properties
                BreakThreshold = RestLength * MaxStrain;
            }
        }

        public override void SolveConstraint(List<PBDParticle> particles, float globalStiffness)
        {
            if (!IsActive || IsBroken || ParticleA >= particles.Count || ParticleB >= particles.Count)
                return;

            var pA = particles[ParticleA];
            var pB = particles[ParticleB];

            if (!pA.IsActive || !pB.IsActive)
            {
                IsActive = false;
                return;
            }

            // Calculate current distance
            Vector3 delta = pB.PredictedPosition - pA.PredictedPosition;
            float currentLength = delta.magnitude;

            if (currentLength < 0.0001f) return; // Avoid division by zero

            // Calculate constraint violation
            float constraint = currentLength - RestLength;
            float strain = Mathf.Abs(constraint) / RestLength;

            // Update stress for fracture mechanics ONLY if fracture is enabled
            // We need to check this somehow - let's add a flag or check if CanBreak is true
            if (CanBreak) // Only accumulate stress for breakable constraints
            {
                _currentStress = strain;

                // Check for fracture
                if (strain > MaxStrain)
                {
                    Break();

                    // Add stress to connected particles
                    pA.AddStress(strain * 10f);
                    pB.AddStress(strain * 10f);

                    return;
                }
            }

            if (Mathf.Abs(constraint) < 0.001f) return; // Already satisfied

            // Calculate correction direction
            Vector3 direction = delta / currentLength;

            // Calculate correction amount based on inverse masses
            float totalInverseMass = pA.InverseMass + pB.InverseMass;
            if (totalInverseMass < 0.0001f) return; // Both particles are fixed

            // Apply stiffness and global stiffness
            float effectiveStiffness = Stiffness * globalStiffness;

            // For shape preservation, apply more aggressive correction
            Vector3 correction = direction * (constraint * effectiveStiffness);

            // Apply corrections proportional to inverse masses
            if (pA.InverseMass > 0)
            {
                pA.PredictedPosition += correction * (pA.InverseMass / totalInverseMass);
            }

            if (pB.InverseMass > 0)
            {
                pB.PredictedPosition -= correction * (pB.InverseMass / totalInverseMass);
            }
        }

        public override void Break()
        {
            base.Break();
            IsBroken = true;
            Debug.Log($"Distance constraint broken between particles {ParticleA} and {ParticleB}");
        }

        public override bool IsSatisfied(List<PBDParticle> particles, float tolerance = 0.01f)
        {
            if (!IsActive || IsBroken || ParticleA >= particles.Count || ParticleB >= particles.Count)
                return true;

            var pA = particles[ParticleA];
            var pB = particles[ParticleB];

            float currentLength = Vector3.Distance(pA.PredictedPosition, pB.PredictedPosition);
            return Mathf.Abs(currentLength - RestLength) < tolerance;
        }

        public string GetDebugInfo(List<PBDParticle> particles)
        {
            if (ParticleA >= particles.Count || ParticleB >= particles.Count)
                return "Invalid constraint";

            float currentLength = Vector3.Distance(
                particles[ParticleA].PredictedPosition,
                particles[ParticleB].PredictedPosition);
            float strain = Mathf.Abs(currentLength - RestLength) / RestLength;

            return $"Distance {ParticleA}-{ParticleB}: Length={currentLength:F3}, Rest={RestLength:F3}, " +
                   $"Strain={strain:F2}, Broken={IsBroken}";
        }
    }

    /// <summary>
    /// Improved ground collision constraint
    /// </summary>
    public class GroundConstraint : PBDConstraint
    {
        public float GroundY;
        public float Restitution = 0.3f;
        public float Friction = 0.4f;

        public GroundConstraint(float groundY, float restitution = 0.3f, float friction = 0.4f)
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
                    // Position correction - move to ground surface with small offset
                    particle.PredictedPosition.y = GroundY + 0.001f;

                    // Velocity correction for bounce
                    if (particle.Velocity.y < 0)
                    {
                        // Calculate impact velocity
                        float impactSpeed = Mathf.Abs(particle.Velocity.y);

                        // Apply restitution - THIS IS THE KEY FOR BOUNCING
                        float bounceSpeed = impactSpeed * Restitution;

                        // Set new upward velocity
                        particle.Velocity.y = bounceSpeed;

                        // Apply friction to horizontal velocity
                        float frictionReduction = 1f - Friction;
                        particle.Velocity.x *= frictionReduction;
                        particle.Velocity.z *= frictionReduction;

                        // ONLY add stress for significant impacts AND fracture-enabled objects
                        // This is determined by checking if the solver has fracture enabled
                        // We'll pass this information through a flag or check the solver
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

    /// <summary>
    /// Volume constraint to prevent unrealistic compression with fracture support
    /// </summary>
    public class VolumeConstraint : PBDConstraint
    {
        public int[] ParticleIndices; // 4 particles forming a tetrahedron
        public float RestVolume;
        private bool _isBroken = false;

        public VolumeConstraint(int[] particleIndices, List<PBDParticle> particles, float stiffness = 0.5f,
            bool canBreak = false)
        {
            if (particleIndices.Length != 4)
            {
                Debug.LogError("Volume constraint requires exactly 4 particles");
                return;
            }

            ParticleIndices = particleIndices;
            Stiffness = stiffness;
            CanBreak = canBreak;

            // Calculate rest volume
            RestVolume = CalculateTetrahedronVolume(particles);

            if (RestVolume < 0.00001f)
            {
                Debug.LogWarning("Volume constraint has very small rest volume");
                RestVolume = 0.00001f;
            }
        }

        private float CalculateTetrahedronVolume(List<PBDParticle> particles)
        {
            if (ParticleIndices.Length != 4) return 0f;

            Vector3 p0 = particles[ParticleIndices[0]].Position;
            Vector3 p1 = particles[ParticleIndices[1]].Position;
            Vector3 p2 = particles[ParticleIndices[2]].Position;
            Vector3 p3 = particles[ParticleIndices[3]].Position;

            // Volume = |det(p1-p0, p2-p0, p3-p0)| / 6
            Vector3 v1 = p1 - p0;
            Vector3 v2 = p2 - p0;
            Vector3 v3 = p3 - p0;

            float volume = Mathf.Abs(Vector3.Dot(v1, Vector3.Cross(v2, v3))) / 6f;
            return volume;
        }

        public override void SolveConstraint(List<PBDParticle> particles, float globalStiffness)
        {
            if (!IsActive || _isBroken || ParticleIndices.Length != 4) return;

            // Check if any particle is inactive
            foreach (int idx in ParticleIndices)
            {
                if (!particles[idx].IsActive)
                {
                    _isBroken = true;
                    IsActive = false;
                    return;
                }
            }

            // Calculate current volume
            float currentVolume = CalculateCurrentVolume(particles);

            if (currentVolume < 0.00001f) return; // Avoid division by zero

            // Volume preservation constraint
            float constraint = currentVolume - RestVolume;
            float volumeStrain = Mathf.Abs(constraint) / RestVolume;

            _currentStress = volumeStrain;

            // Check for fracture due to excessive compression/expansion
            if (CanBreak && volumeStrain > 0.8f) // 80% volume change breaks the constraint
            {
                Break();
                return;
            }

            if (Mathf.Abs(constraint) < 0.00001f) return; // Already satisfied

            // Apply volume correction (simplified approach)
            Vector3 center = Vector3.zero;
            float totalInverseMass = 0f;

            foreach (int idx in ParticleIndices)
            {
                center += particles[idx].PredictedPosition;
                totalInverseMass += particles[idx].InverseMass;
            }

            center /= 4f;

            if (totalInverseMass < 0.0001f) return;

            // Apply correction towards/away from center to preserve volume
            float correctionFactor = constraint * Stiffness * globalStiffness * 0.25f / totalInverseMass;

            foreach (int idx in ParticleIndices)
            {
                var particle = particles[idx];
                if (particle.InverseMass > 0)
                {
                    Vector3 direction = (particle.PredictedPosition - center).normalized;
                    Vector3 correction = direction * (correctionFactor * particle.InverseMass);
                    particle.PredictedPosition -= correction;
                }
            }
        }

        public override void Break()
        {
            base.Break();
            _isBroken = true;
            Debug.Log($"Volume constraint broken for tetrahedron with particles: {string.Join(",", ParticleIndices)}");
        }

        private float CalculateCurrentVolume(List<PBDParticle> particles)
        {
            Vector3 p0 = particles[ParticleIndices[0]].PredictedPosition;
            Vector3 p1 = particles[ParticleIndices[1]].PredictedPosition;
            Vector3 p2 = particles[ParticleIndices[2]].PredictedPosition;
            Vector3 p3 = particles[ParticleIndices[3]].PredictedPosition;

            Vector3 v1 = p1 - p0;
            Vector3 v2 = p2 - p0;
            Vector3 v3 = p3 - p0;

            return Mathf.Abs(Vector3.Dot(v1, Vector3.Cross(v2, v3))) / 6f;
        }

        public override bool IsSatisfied(List<PBDParticle> particles, float tolerance = 0.1f)
        {
            if (!IsActive || _isBroken) return true;

            float currentVolume = CalculateCurrentVolume(particles);
            return Mathf.Abs(currentVolume - RestVolume) < RestVolume * tolerance;
        }
    }
}