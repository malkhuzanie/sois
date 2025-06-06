// Assets/_Project/01_Physics/Scripts/XPBD/Components/XPBDRubberBall.cs

using UnityEngine;
using _Project._01_Physics.Scripts.XPBD.Core;
using _Project._01_Physics.Scripts.XPBD.Materials;
using _Project._01_Physics.Scripts.XPBD.Constraints;
using _Project._01_Physics.Scripts.XPBD.Utilities;
using System.Collections.Generic;

namespace _Project._01_Physics.Scripts.XPBD.Components
{
    /// <summary>
    /// XPBD-based rubber ball with proper elastic behavior
    /// Demonstrates time-step independent elastic simulation
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class XPBDRubberBall : MonoBehaviour
    {
        [Header("Ball Properties")] [SerializeField]
        private float radius = 0.5f;

        [SerializeField] private int meshSubdivisions = 2;
        [SerializeField] private ElasticMaterial material;

        [Header("Ground Collision")] [SerializeField]
        private bool enableGroundCollision = true;

        [SerializeField] private float groundY = 0.0f;
        [SerializeField] private bool autoDetectGround = true;

        [Header("Debug")] [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showParticles = true;
        [SerializeField] private bool showConstraints = true;

        // Components
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private XPBDSolver solver;

        // Mesh data
        private Mesh originalMesh;
        private Mesh deformedMesh;
        private Vector3[] originalVertices;
        private Vector3[] deformedVertices;

        // Simulation state
        private bool isInitialized = false;
        private float ballVolume;

        public XPBDSolver Solver => solver;
        public bool IsInitialized => isInitialized;

        void Start()
        {
            Initialize();
        }

        void FixedUpdate()
        {
            if (!isInitialized) return;

            // Update XPBD simulation
            solver.Update(Time.fixedDeltaTime);

            // Update mesh from particles
            UpdateMeshFromParticles();
        }

        /// <summary>
        /// Initialize the rubber ball simulation
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            // Get components
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            // Create material if none assigned
            if (material == null)
            {
                material = ElasticMaterial.CreateRubberMaterial();
                Debug.Log("Created default rubber material");
            }

            // Setup mesh
            SetupMesh();

            // Create solver
            CreateSolver();

            // Setup constraints
            SetupConstraints();

            // Auto-detect ground if enabled
            if (autoDetectGround)
            {
                DetectGround();
            }

            isInitialized = true;

            Debug.Log($"XPBD Rubber Ball initialized: {solver.Particles.Count} particles, " +
                      $"{solver.Constraints.Count} constraints");
        }

        void SetupMesh()
        {
            // Generate XPBD-optimized sphere mesh
            originalMesh = XPBDMeshGenerator.GenerateSphereMesh(radius, meshSubdivisions);
            deformedMesh = Instantiate(originalMesh);
            deformedMesh.name = originalMesh.name + "_Deformed";

            originalVertices = originalMesh.vertices;
            deformedVertices = new Vector3[originalVertices.Length];
            originalVertices.CopyTo(deformedVertices, 0);

            meshFilter.mesh = deformedMesh;

            // Calculate ball volume for constraints
            ballVolume = (4.0f / 3.0f) * Mathf.PI * radius * radius * radius;
        }

        void CreateSolver()
        {
            solver = new XPBDSolver();
            solver.SolverIterations = material.SolverIterations;
            solver.SubSteps = material.SubSteps;
            solver.Gravity = new Vector3(0, -9.81f, 0);
            solver.GlobalDamping = 1.0f - material.Damping;

            // Create particles from mesh vertices
            float particleVolume = ballVolume / originalVertices.Length;
            float particleMass = material.CalculateParticleMass(particleVolume);

            for (int i = 0; i < originalVertices.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(originalVertices[i]);
                solver.AddParticle(worldPos, particleMass, i);
            }
        }

        void SetupConstraints()
        {
            // VERY soft compliance for dramatic deformation
            float distanceCompliance = material.CalculateDistanceCompliance();
            float volumeCompliance = material.CalculateVolumeCompliance();

            Debug.Log($"Using VERY SOFT compliance - Distance: {distanceCompliance:E3}, Volume: {volumeCompliance:E3}");

            // Add soft distance constraints
            var edges = XPBDMeshGenerator.GenerateSphereEdges(originalMesh);
            foreach (var (v0, v1) in edges)
            {
                float restLength = Vector3.Distance(originalVertices[v0], originalVertices[v1]);
                var constraint = new XPBDDistanceConstraint(v0, v1, restLength, distanceCompliance);
                solver.AddConstraint(constraint);
            }

            // Use FORCE-RESPONSIVE volume constraint for dramatic impact deformation
            var allParticleIndices = new List<int>();
            for (int i = 0; i < solver.Particles.Count; i++)
            {
                allParticleIndices.Add(i);
            }

            Vector3 originalCenter = Vector3.zero;
            foreach (var vertex in originalVertices)
            {
                originalCenter += vertex;
            }

            originalCenter /= originalVertices.Length;

            var forceResponsiveVolume = new ForceResponsiveVolumeConstraint(
                allParticleIndices, originalCenter, radius, volumeCompliance, volumeCompliance * 10f);
            solver.AddConstraint(forceResponsiveVolume);

            // Ground constraint
            if (enableGroundCollision)
            {
                var groundConstraint = new StableGroundConstraintV3(
                    groundY, 
                    material.Restitution, 
                    material.Friction,      // Dynamic friction
                    material.Friction * 1.5f // Static friction (50% higher)
                );
                
                // var groundConstraint = new FinalGroundConstraint(
                //     groundY, material.Restitution, material.Friction);
                // var groundConstraint = new EnhancedXPBDGroundConstraint(
                //     groundY, material.Restitution, material.Friction, material.Friction * 0.8f);
                solver.AddConstraint(groundConstraint);
            }

            // Replace basic monitor with enhanced version
            var oldMonitor = GetComponent<DeformationMonitor>();
            if (oldMonitor != null) Destroy(oldMonitor);

            if (GetComponent<EnhancedDeformationMonitor>() == null)
            {
                gameObject.AddComponent<EnhancedDeformationMonitor>();
            }

            Debug.Log($"Added FORCE-RESPONSIVE constraints for dramatic deformation");
        }

        void UpdateMeshFromParticles()
        {
            // Update vertex positions from particle positions
            for (int i = 0; i < deformedVertices.Length && i < solver.Particles.Count; i++)
            {
                // Convert from world space back to local space
                deformedVertices[i] = transform.InverseTransformPoint(solver.Particles[i].Position);
            }

            // Update mesh
            deformedMesh.vertices = deformedVertices;
            deformedMesh.RecalculateNormals();
            deformedMesh.RecalculateBounds();
        }

        void DetectGround()
        {
            GameObject groundObject = GameObject.FindGameObjectWithTag("Ground");
            if (groundObject != null)
            {
                Collider groundCollider = groundObject.GetComponent<Collider>();
                if (groundCollider != null)
                {
                    groundY = groundCollider.bounds.max.y;
                }
                else
                {
                    groundY = groundObject.transform.position.y +
                              (groundObject.transform.localScale.y * 0.5f);
                }

                Debug.Log($"Auto-detected ground at Y = {groundY}");
            }
        }

        /// <summary>
        /// Apply impulse to the ball
        /// </summary>
        public void ApplyImpulse(Vector3 impulse, Vector3 position, float radius = 0.5f)
        {
            if (!isInitialized) return;

            solver.ApplyImpulse(position, impulse, radius, Time.fixedDeltaTime);
        }

        /// <summary>
        /// Reset ball to initial state
        /// </summary>
        public void ResetBall()
        {
            if (!isInitialized) return;

            // Reset particle positions
            for (int i = 0; i < solver.Particles.Count && i < originalVertices.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(originalVertices[i]);
                solver.Particles[i].Position = worldPos;
                solver.Particles[i].PreviousPosition = worldPos;
                solver.Particles[i].PredictedPosition = worldPos;
            }

            solver.Reset();
        }

        void OnDrawGizmos()
        {
            if (!showDebugInfo || !isInitialized) return;

            if (showParticles)
            {
                // Draw particles
                Gizmos.color = Color.yellow;
                foreach (var particle in solver.Particles)
                {
                    Gizmos.DrawWireSphere(particle.Position, 0.02f);

                    if (particle.IsFixed)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(particle.Position, 0.03f);
                        Gizmos.color = Color.yellow;
                    }
                }
            }

            if (showConstraints)
            {
                // Draw constraint connections (distance constraints only for clarity)
                Gizmos.color = Color.cyan;
                foreach (var constraint in solver.Constraints)
                {
                    if (constraint is XPBDDistanceConstraint dc)
                    {
                        if (dc.ParticleA < solver.Particles.Count && dc.ParticleB < solver.Particles.Count)
                        {
                            Gizmos.DrawLine(
                                solver.Particles[dc.ParticleA].Position,
                                solver.Particles[dc.ParticleB].Position
                            );
                        }
                    }
                }
            }
        }

        void OnGUI()
        {
            if (!showDebugInfo || !isInitialized) return;

            var stats = solver.GetStatistics();

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box($"XPBD Rubber Ball Debug\n\n" +
                          $"Material: {material.MaterialName}\n" +
                          $"Particles: {stats.particles}\n" +
                          $"Constraints: {stats.constraints}\n" +
                          $"Solve Time: {stats.solveTime * 1000f:F2}ms\n" +
                          $"Iterations: {stats.iterations}/{material.SolverIterations}\n" +
                          $"Young's Modulus: {material.YoungModulus / 1000000f:F1} MPa\n" +
                          $"Poisson Ratio: {material.PoissonRatio:F2}\n" +
                          $"Density: {material.Density:F0} kg/m³\n" +
                          $"Ball Volume: {ballVolume:F3} m³");
            GUILayout.EndArea();
        }
    }
}