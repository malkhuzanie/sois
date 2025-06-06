using System.Collections.Generic;
using _Project._01_Physics.Scripts.XPBD.Core;
using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Constraints
{
    /// <summary>
    /// XPBD Volume Constraint - maintains spherical volume for rubber balls
    /// Critical for incompressible rubber behavior
    /// </summary>
    public class XPBDVolumeConstraint : XPBDConstraint
    {
        public List<int> ParticleIndices;
        public float RestVolume;
        public Vector3 CenterOfMass;
        
        public XPBDVolumeConstraint(List<int> particleIndices, float restVolume, float compliance = 0.0f)
        {
            ParticleIndices = new List<int>(particleIndices);
            RestVolume = restVolume;
            Compliance = compliance;
        }
        
        public override void SolveConstraint(List<XPBDParticle> particles, float deltaTime)
        {
            if (!IsActive || ParticleIndices.Count < 4) return;
            
            // Calculate center of mass
            CenterOfMass = Vector3.zero;
            float totalInverseMass = 0.0f;
            
            foreach (int idx in ParticleIndices)
            {
                if (idx < particles.Count && !particles[idx].IsFixed)
                {
                    CenterOfMass += particles[idx].PredictedPosition * particles[idx].InverseMass;
                    totalInverseMass += particles[idx].InverseMass;
                }
            }
            
            if (totalInverseMass <= 0.0f) return;
            CenterOfMass /= totalInverseMass;
            
            // Calculate current volume (approximate as sphere)
            float avgDistance = 0.0f;
            int validParticles = 0;
            
            foreach (int idx in ParticleIndices)
            {
                if (idx < particles.Count && !particles[idx].IsFixed)
                {
                    avgDistance += Vector3.Distance(particles[idx].PredictedPosition, CenterOfMass);
                    validParticles++;
                }
            }
            
            if (validParticles == 0) return;
            
            float avgRadius = avgDistance / validParticles;
            float currentVolume = (4.0f / 3.0f) * Mathf.PI * avgRadius * avgRadius * avgRadius;
            
            float constraintValue = currentVolume - RestVolume;
            
            // XPBD volume correction
            float alpha = Compliance / (deltaTime * deltaTime);
            float correction = constraintValue / (totalInverseMass + alpha);
            
            // Apply radial corrections to maintain volume
            foreach (int idx in ParticleIndices)
            {
                if (idx < particles.Count && !particles[idx].IsFixed)
                {
                    var particle = particles[idx];
                    Vector3 directionFromCenter = (particle.PredictedPosition - CenterOfMass).normalized;
                    Vector3 positionCorrection = -directionFromCenter * (correction * particle.InverseMass * 0.1f);
                    particle.PredictedPosition += positionCorrection;
                }
            }
        }
        
        public override float EvaluateConstraint(List<XPBDParticle> particles)
        {
            if (ParticleIndices.Count < 4) return 0.0f;
            
            // Simplified volume calculation
            Vector3 center = Vector3.zero;
            foreach (int idx in ParticleIndices)
            {
                if (idx < particles.Count)
                    center += particles[idx].PredictedPosition;
            }
            center /= ParticleIndices.Count;
            
            float avgDistance = 0.0f;
            foreach (int idx in ParticleIndices)
            {
                if (idx < particles.Count)
                    avgDistance += Vector3.Distance(particles[idx].PredictedPosition, center);
            }
            avgDistance /= ParticleIndices.Count;
            
            float currentVolume = (4.0f / 3.0f) * Mathf.PI * avgDistance * avgDistance * avgDistance;
            return currentVolume - RestVolume;
        }
    }
    
}