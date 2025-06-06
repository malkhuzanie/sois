// Assets/_Project/01_Physics/Scripts/XPBD/Materials/ElasticMaterial.cs

using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Materials
{
    /// <summary>
    /// Fixed elastic material with proper stiffness calculations
    /// </summary>
    [CreateAssetMenu(fileName = "ElasticMaterial", menuName = "XPBD/Elastic Material")]
    public class ElasticMaterial : ScriptableObject
    {
        [Header("Physical Properties")] [SerializeField]
        private string materialName = "Rubber";

        [SerializeField] private float youngModulus = 5000000f; // 5 MPa for firm rubber
        [SerializeField] private float poissonRatio = 0.35f; // Less compressible
        [SerializeField] private float density = 1200f; // kg/m³

        [Header("Simulation Parameters")] [SerializeField]
        private float restitution = 0.85f; // Higher bounce

        [SerializeField] private float friction = 0.4f;
        [SerializeField] private float damping = 0.01f; // Much less damping

        [Header("XPBD Settings")] [SerializeField]
        private int solverIterations = 12; // More iterations for stability

        [SerializeField] private int subSteps = 2;

        // Properties
        public string MaterialName => materialName;
        public float YoungModulus => youngModulus;
        public float PoissonRatio => poissonRatio;
        public float Density => density;
        public float Restitution => restitution;
        public float Friction => friction;
        public float Damping => damping;
        public int SolverIterations => solverIterations;
        public int SubSteps => subSteps;

        /// <summary>
        /// Much softer compliance for visible deformation
        /// </summary>
        public float CalculateDistanceCompliance()
        {
            // MUCH softer for visible deformation
            float stiffnessScale = 50000f; // Reduced from 200000f
            return 1.0f / (youngModulus * stiffnessScale);
        }

        /// <summary>
        /// Moderate volume compliance - prevent collapse but allow compression
        /// </summary>
        public float CalculateVolumeCompliance()
        {
            // Softer volume constraint
            float volumeStiffnessScale = 150000f; // Reduced from 800000f
            return 1.0f / (youngModulus * volumeStiffnessScale);
        }

        /// <summary>
        /// Calculate mass for particle based on volume
        /// </summary>
        public float CalculateParticleMass(float particleVolume)
        {
            return density * particleVolume;
        }

        /// <summary>
        /// Create proper rubber material - FIXED VERSION
        /// </summary>
        public static ElasticMaterial CreateRubberMaterial()
        {
            var material = CreateInstance<ElasticMaterial>();
            material.materialName = "Stable Rubber";
            material.youngModulus = 5000f; // Slightly firmer
            material.poissonRatio = 0.45f;
            material.density = 1200f;
            material.restitution = 0.6f; // Less bouncy to settle faster
            material.friction = 0.7f; // Higher base friction
            material.damping = 0.02f;
            material.solverIterations = 8;
            material.subSteps = 2;
            return material;
        }
    }
}