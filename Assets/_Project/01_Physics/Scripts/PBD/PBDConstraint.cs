// Assets/_Project/01_Physics/Scripts/PBD/PBDConstraint.cs

using UnityEngine;
using System.Collections.Generic;

namespace _Project._01_Physics.Scripts.PBD
{
    /// <summary>
    /// Base class for all PBD constraints
    /// </summary>
    public abstract class PBDConstraint
    {
        public float Stiffness = 1.0f;
        public bool IsActive = true;

        public abstract void SolveConstraint(List<PBDParticle> particles, float stiffness);
        public abstract bool IsSatisfied(List<PBDParticle> particles, float tolerance = 0.01f);
    }

    /// <summary>
    /// Distance constraint - maintains distance between two particles
    /// This replaces springs but is much more stable
    /// </summary>
    public class DistanceConstraint : PBDConstraint
    {
        public int ParticleA;
        public int ParticleB;
        public float RestLength;

        public DistanceConstraint(int particleA, int particleB, float restLength, float stiffness = 1.0f)
        {
            ParticleA = particleA;
            ParticleB = particleB;
            RestLength = restLength;
            Stiffness = stiffness;
        }

        public override void SolveConstraint(List<PBDParticle> particles, float globalStiffness)
        {
            if (!IsActive || ParticleA >= particles.Count || ParticleB >= particles.Count)
                return;

            var pA = particles[ParticleA];
            var pB = particles[ParticleB];

            // Calculate current distance
            Vector3 delta = pB.PredictedPosition - pA.PredictedPosition;
            float currentLength = delta.magnitude;

            if (currentLength < 0.0001f) return; // Avoid division by zero

            // Calculate constraint violation
            float constraint = currentLength - RestLength;

            if (Mathf.Abs(constraint) < 0.001f) return; // Already satisfied

            // Calculate correction direction
            Vector3 direction = delta / currentLength;

            // Calculate correction amount based on inverse masses
            float totalInverseMass = pA.InverseMass + pB.InverseMass;
            if (totalInverseMass < 0.0001f) return; // Both particles are fixed

            // Apply stiffness and global stiffness
            float effectiveStiffness = Stiffness * globalStiffness;
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

        public override bool IsSatisfied(List<PBDParticle> particles, float tolerance = 0.01f)
        {
            if (ParticleA >= particles.Count || ParticleB >= particles.Count)
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
            float strain = currentLength / RestLength;

            return
                $"Distance {ParticleA}-{ParticleB}: Length={currentLength:F3}, Rest={RestLength:F3}, Strain={strain:F2}";
        }
    }

    /// <summary>
    /// Ground collision constraint - prevents particles from going below ground
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
                if (particle.IsFixed) continue;

                // Check if particle is below ground
                if (particle.PredictedPosition.y < GroundY)
                {
                    // Position correction - move to ground surface
                    particle.PredictedPosition.y = GroundY;

                    // SUPER ENHANCED velocity correction - PRESERVE MUCH MORE ENERGY
                    if (particle.Velocity.y < 0)
                    {
                        // Calculate impact velocity
                        float impactSpeed = Mathf.Abs(particle.Velocity.y);

                        // Apply restitution with MUCH better energy conservation
                        float bounceSpeed = impactSpeed * Restitution;

                        // BOOST: Add much more energy to compensate for system losses
                        float energyBoost = 1.8f; // Increased from 1.2f to 1.8f
                        bounceSpeed *= energyBoost;

                        // Set new upward velocity
                        particle.Velocity.y = bounceSpeed;

                        // Reduce friction impact even more
                        float frictionReduction = 1f - (Friction * 0.05f); // Reduced from 0.1f
                        particle.Velocity.x *= frictionReduction;
                        particle.Velocity.z *= frictionReduction;

                        Debug.Log($"SUPER ENHANCED Bounce: Impact={impactSpeed:F2}, Bounce={bounceSpeed:F2}, Boost={energyBoost}");
                    }
                }
            }
        }
        
        public override bool IsSatisfied(List<PBDParticle> particles, float tolerance = 0.01f)
        {
            foreach (var particle in particles)
            {
                if (particle.PredictedPosition.y < GroundY - tolerance)
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Volume constraint - maintains volume of tetrahedra to prevent unrealistic compression
    /// This is what prevents the "pancaking" effect you were seeing
    /// </summary>
    public class VolumeConstraint : PBDConstraint
    {
        public int[] ParticleIndices; // 4 particles forming a tetrahedron
        public float RestVolume;

        public VolumeConstraint(int[] particleIndices, List<PBDParticle> particles, float stiffness = 0.5f)
        {
            if (particleIndices.Length != 4)
            {
                Debug.LogError("Volume constraint requires exactly 4 particles");
                return;
            }

            ParticleIndices = particleIndices;
            Stiffness = stiffness;

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
            if (!IsActive || ParticleIndices.Length != 4) return;

            // Calculate current volume
            float currentVolume = CalculateCurrentVolume(particles);

            if (currentVolume < 0.00001f) return; // Avoid division by zero

            // Volume preservation constraint
            float constraint = currentVolume - RestVolume;

            if (Mathf.Abs(constraint) < 0.00001f) return; // Already satisfied

            // Apply volume correction (simplified approach)
            // In a full implementation, you'd calculate proper gradients
            // For now, we'll use a simplified approach that works well in practice

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
            float currentVolume = CalculateCurrentVolume(particles);
            return Mathf.Abs(currentVolume - RestVolume) < RestVolume * tolerance;
        }
    }
}