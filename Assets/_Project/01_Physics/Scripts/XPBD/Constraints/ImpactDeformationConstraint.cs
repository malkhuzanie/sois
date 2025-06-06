// Assets/_Project/01_Physics/Scripts/XPBD/Constraints/ImpactDeformationConstraint.cs

using System.Collections.Generic;
using _Project._01_Physics.Scripts.XPBD.Core;
using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Constraints
{
    /// <summary>
    /// Special constraint that allows increased deformation during impacts
    /// Temporarily softens constraints when high forces are detected
    /// </summary>
    public class ImpactDeformationConstraint : XPBDConstraint
    {
        private XPBDDistanceConstraint baseConstraint;
        private float baseCompliance;
        private float impactCompliance;
        private float impactDecayRate = 0.95f;
        private float currentImpactFactor = 0.0f;
        private float impactThreshold = 5.0f; // Velocity threshold for impact detection
        
        public ImpactDeformationConstraint(XPBDDistanceConstraint originalConstraint, float impactMultiplier = 3.0f)
        {
            baseConstraint = originalConstraint;
            baseCompliance = originalConstraint.Compliance;
            impactCompliance = baseCompliance * impactMultiplier; // Softer during impact
        }
        
        public override void SolveConstraint(List<XPBDParticle> particles, float deltaTime)
        {
            if (!IsActive || baseConstraint.ParticleA >= particles.Count || baseConstraint.ParticleB >= particles.Count)
                return;
            
            var pA = particles[baseConstraint.ParticleA];
            var pB = particles[baseConstraint.ParticleB];
            
            // Detect impact by checking relative velocity
            Vector3 relativeVelocity = (pA.Position - pA.PreviousPosition) - (pB.Position - pB.PreviousPosition);
            float impactMagnitude = relativeVelocity.magnitude / deltaTime;
            
            // Increase impact factor if high relative velocity detected
            if (impactMagnitude > impactThreshold)
            {
                float newImpactFactor = (impactMagnitude - impactThreshold) / 10.0f;
                currentImpactFactor = Mathf.Max(currentImpactFactor, newImpactFactor);
            }
            
            // Decay impact factor over time
            currentImpactFactor *= impactDecayRate;
            currentImpactFactor = Mathf.Max(0.0f, currentImpactFactor);
            
            // Interpolate compliance based on impact
            float activeCompliance = Mathf.Lerp(baseCompliance, impactCompliance, currentImpactFactor);
            
            // Apply constraint with dynamic compliance
            Vector3 delta = pB.PredictedPosition - pA.PredictedPosition;
            float currentLength = delta.magnitude;
            
            if (currentLength < 0.0001f) return;
            
            float constraintValue = currentLength - baseConstraint.RestLength;
            float alpha = activeCompliance / (deltaTime * deltaTime);
            Vector3 direction = delta / currentLength;
            float denominator = pA.InverseMass + pB.InverseMass + alpha;
            
            if (denominator <= 0.0f) return;
            
            Vector3 correction = -(constraintValue / denominator) * direction;
            
            if (!pA.IsFixed)
                pA.PredictedPosition -= correction * pA.InverseMass;
            if (!pB.IsFixed)
                pB.PredictedPosition += correction * pB.InverseMass;
        }
        
        public override float EvaluateConstraint(List<XPBDParticle> particles)
        {
            return baseConstraint.EvaluateConstraint(particles);
        }
    }
}