// Assets/_Project/01_Physics/Scripts/XPBD/Constraints/ForceResponsiveVolumeConstraint.cs

using System.Collections.Generic;
using System.Linq;
using _Project._01_Physics.Scripts.XPBD.Core;
using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Constraints
{
    /// <summary>
    /// Volume constraint that becomes softer under high impact forces
    /// </summary>
    public class ForceResponsiveVolumeConstraint : XPBDConstraint
    {
        public List<int> ParticleIndices;
        public Vector3 OriginalCenterOfMass;
        public float TargetRadius;
        public float BaseCompliance;
        public float MaxCompliance;

        // Force tracking
        private float _currentForceLevel = 0f;
        private const float ForceDecayRate = 0.9f;
        private const float ForceThreshold = 2f; // Lower threshold for easier triggering

        public ForceResponsiveVolumeConstraint(List<int> particleIndices, Vector3 originalCenter,
            float targetRadius, float baseCompliance, float maxCompliance = -1f)
        {
            ParticleIndices = new List<int>(particleIndices);
            OriginalCenterOfMass = originalCenter;
            TargetRadius = targetRadius;
            BaseCompliance = baseCompliance;
            MaxCompliance = maxCompliance < 0 ? baseCompliance * 5f : maxCompliance; // 5x softer under impact
        }

        public override void SolveConstraint(List<XPBDParticle> particles, float deltaTime)
        {
            if (!IsActive || ParticleIndices.Count < 4) return;

            // Calculate average velocity magnitude to detect impacts
            float totalVelocityMagnitude = 0f;
            int activeParticles = 0;

            foreach (int idx in ParticleIndices)
            {
                if (idx < particles.Count && !particles[idx].IsFixed)
                {
                    Vector3 velocity = (particles[idx].Position - particles[idx].PreviousPosition) / deltaTime;
                    totalVelocityMagnitude += velocity.magnitude;
                    activeParticles++;
                }
            }

            if (activeParticles == 0) return;

            float avgVelocity = totalVelocityMagnitude / activeParticles;

            // Update force level based on velocity
            if (avgVelocity > ForceThreshold)
            {
                float newForceLevel = (avgVelocity - ForceThreshold) / 5f; // Scale factor
                _currentForceLevel = Mathf.Max(_currentForceLevel, newForceLevel);
                Debug.Log($"Impact detected! Avg velocity: {avgVelocity:F2}, Force level: {_currentForceLevel:F2}");
            }

            // Decay force level
            _currentForceLevel *= ForceDecayRate;
            _currentForceLevel = Mathf.Max(0f, _currentForceLevel);

            // Calculate adaptive compliance
            float adaptiveCompliance = Mathf.Lerp(BaseCompliance, MaxCompliance, _currentForceLevel);

            // Apply volume constraint with adaptive stiffness
            Vector3 currentCenter = Vector3.zero;
            float totalMass = 0.0f;

            foreach (int idx in ParticleIndices)
            {
                if (idx < particles.Count && !particles[idx].IsFixed)
                {
                    float mass = 1.0f / (particles[idx].InverseMass + 0.000001f);
                    currentCenter += particles[idx].PredictedPosition * mass;
                    totalMass += mass;
                }
            }

            if (totalMass <= 0.0f) return;
            currentCenter /= totalMass;

            // Apply radial correction with adaptive compliance
            foreach (int idx in ParticleIndices)
            {
                if (idx < particles.Count && !particles[idx].IsFixed)
                {
                    var particle = particles[idx];
                    Vector3 direction = particle.PredictedPosition - currentCenter;
                    float currentDistance = direction.magnitude;

                    if (currentDistance > 0.001f)
                    {
                        Vector3 normalizedDirection = direction / currentDistance;
                        Vector3 targetPosition = currentCenter + normalizedDirection * TargetRadius;
                        Vector3 correction = targetPosition - particle.PredictedPosition;

                        float alpha = adaptiveCompliance / (deltaTime * deltaTime);
                        float constraintMagnitude = correction.magnitude;

                        if (constraintMagnitude > 0.001f)
                        {
                            Vector3 correctionDirection = correction / constraintMagnitude;
                            float denominator = particle.InverseMass + alpha;

                            if (denominator > 0.0f)
                            {
                                Vector3 positionCorrection = correctionDirection *
                                                             (constraintMagnitude / denominator * particle.InverseMass);

                                // Apply correction - softer during impacts
                                float correctionStrength = Mathf.Lerp(0.6f, 0.2f, _currentForceLevel);
                                particle.PredictedPosition += positionCorrection * correctionStrength;
                            }
                        }
                    }
                }
            }
        }

        public override float EvaluateConstraint(List<XPBDParticle> particles)
        {
            if (ParticleIndices.Count == 0) return 0.0f;

            var center = ParticleIndices
                .Where(idx => idx < particles.Count)
                .Aggregate(Vector3.zero, (current, idx) => current + particles[idx].PredictedPosition);

            center /= ParticleIndices.Count;

            float totalDeviation =
            (
                from idx in ParticleIndices
                where idx < particles.Count
                select Vector3.Distance(particles[idx].PredictedPosition, center)
                into distance
                select Mathf.Abs(distance - TargetRadius)
            ).Sum();

            return totalDeviation / ParticleIndices.Count;
        }
    }
}