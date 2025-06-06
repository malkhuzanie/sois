using System.Collections.Generic;
using _Project._01_Physics.Scripts.XPBD.Core;
using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Constraints
{
    /// <summary>
    /// XPBD Distance Constraint - maintains distance between two particles
    /// Key for rubber ball structural integrity
    /// </summary>
    public class XPBDDistanceConstraint : XPBDConstraint
    {
        public int ParticleA;
        public int ParticleB;
        public float RestLength;
        
        public XPBDDistanceConstraint(int particleA, int particleB, float restLength, float compliance = 0.0f)
        {
            ParticleA = particleA;
            ParticleB = particleB;
            RestLength = restLength;
            Compliance = compliance;
        }
        
        public override void SolveConstraint(List<XPBDParticle> particles, float deltaTime)
        {
            if (!IsActive || ParticleA >= particles.Count || ParticleB >= particles.Count)
                return;
                
            var pA = particles[ParticleA];
            var pB = particles[ParticleB];
            
            if (!pA.IsFixed && !pB.IsFixed && (pA.InverseMass + pB.InverseMass) <= 0.0f)
                return;
            
            // Calculate constraint violation
            Vector3 delta = pB.PredictedPosition - pA.PredictedPosition;
            float currentLength = delta.magnitude;
            
            if (currentLength < 0.0001f) return;
            
            float constraintValue = currentLength - RestLength;
            
            // XPBD compliance formulation
            float alpha = Compliance / (deltaTime * deltaTime);
            Vector3 direction = delta / currentLength;
            float denominator = pA.InverseMass + pB.InverseMass + alpha;
            
            if (denominator <= 0.0f) return;
            
            // Calculate and apply position corrections
            Vector3 correction = -(constraintValue / denominator) * direction;
            
            if (!pA.IsFixed)
                pA.PredictedPosition -= correction * pA.InverseMass;
            if (!pB.IsFixed)
                pB.PredictedPosition += correction * pB.InverseMass;
        }
        
        public override float EvaluateConstraint(List<XPBDParticle> particles)
        {
            if (ParticleA >= particles.Count || ParticleB >= particles.Count)
                return 0.0f;
                
            float currentLength = Vector3.Distance(
                particles[ParticleA].PredictedPosition,
                particles[ParticleB].PredictedPosition);
            return currentLength - RestLength;
        }
    }
}