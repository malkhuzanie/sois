using System.Collections.Generic;
using UnityEngine;
using _Project._01_Physics.Scripts.XPBD.Core;

namespace _Project._01_Physics.Scripts.XPBD.Constraints
{
    /// <summary>
    /// Base XPBD constraint with compliance-based stiffness
    /// </summary>
    public abstract class XPBDConstraint
    {
        public float Compliance = 0.0f; // 1/stiffness - XPBD parameter
        public bool IsActive = true;
        
        public abstract void SolveConstraint(List<XPBDParticle> particles, float deltaTime);
        public abstract float EvaluateConstraint(List<XPBDParticle> particles);
    }
    
}