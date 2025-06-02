using UnityEngine;

namespace _Project._00_Core.Scripts.DataStructures
{
    [System.Serializable]
    public class PhysicsMaterial : ScriptableObject
    {
        [Header("Basic Properties")]
        public string materialName = "Default";
        public float density = 1.0f;
        
        [Header("Collision Response")]
        public float restitution = 0.5f;      // Bounciness (0-1)
        public float staticFriction = 0.6f;   // Static friction coefficient
        public float dynamicFriction = 0.4f;  // Dynamic friction coefficient
        
        [Header("Deformation Properties")]
        public DeformationType deformationType = DeformationType.Elastic;
        public float elasticLimit = 1000f;    // Force threshold for elastic deformation
        public float plasticLimit = 2000f;    // Force threshold for plastic deformation  
        public float brittleThreshold = 5000f; // Force threshold for breaking
        public float stiffness = 10000f;      // Material stiffness (for deformation)
        public float damping = 100f;          // Energy dissipation
    }
}