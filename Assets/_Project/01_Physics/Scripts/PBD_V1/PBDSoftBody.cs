// Assets/_Project/01_Physics/Scripts/PBD/PBDSoftBody.cs

using System.Collections.Generic;
using _Project._00_Core.Scripts.Abstractions;
using _Project._00_Core.Scripts.DataStructures;
using UnityEngine;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.PBD_V1
{
    /// <summary>
    /// Improved PBD soft body with fracture mechanics and better mesh handling
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PBDSoftBody : MonoBehaviour, IDeformable
    {
        [Header("Material Properties")]
        [SerializeField] private float density = 1.0f;
        [SerializeField] private float restitution = 0.6f;
        [SerializeField] private float friction = 0.4f;
        
        [Header("PBD Solver Settings")]
        [SerializeField] private int constraintIterations = 4;
        [SerializeField] private float globalStiffness = 0.8f;
        [SerializeField] private float globalDamping = 0.99f;
        
        [Header("Fracture Settings")]
        [SerializeField] private bool enableFracture = false;
        [SerializeField] private float fractureThreshold = 10f;
        [SerializeField] private float stressDecayRate = 0.95f;
        
        [Header("Ground Collision")]
        [SerializeField] private bool enableGroundCollision = true;
        [SerializeField] private float groundY = 0f;
        [SerializeField] private bool autoDetectGround = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showParticles = false;
        [SerializeField] private bool showBrokenConstraints = false;
        
        // Components
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private PBDSolver solver;
        private PhysicsMaterial physicsMaterial;
        
        // Mesh data
        private Mesh originalMesh;
        private Mesh deformedMesh;
        private Vector3[] originalVertices;
        private Vector3[] deformedVertices;
        private int[] originalTriangles;
        private List<int> activeTriangles;
        
        // Fracture data
        private List<GameObject> fragments;
        private Material fracturedMaterial;
        
        // State
        private bool isInitialized = false;
        private bool isFractured = false;
        
        public PBDSolver Solver => solver;
        public bool IsFractured => isFractured;
        
        #region Unity Lifecycle
        
        void Start()
        {
            Initialize();
        }
        
        void FixedUpdate()
        {
            if (!isInitialized) return;
            
            // Update PBD solver
            solver.Update(Time.fixedDeltaTime);
            
            // Update mesh from particles
            UpdateMeshFromParticles();
            
            // Check for fracture
            if (enableFracture && !isFractured)
            {
                CheckForFracture();
            }
        }
        
        #endregion
        
        #region Initialization
        
        public void Initialize(PhysicsMaterial material = null)
        {
            // Get components
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            if (meshFilter.sharedMesh == null)
            {
                Debug.LogError($"[{gameObject.name}] No mesh found on MeshFilter!");
                return;
            }
            
            // Set physics material
            physicsMaterial = material ?? CreateDefaultMaterial();
            
            // Setup mesh data
            SetupMeshData();
            
            // Create solver
            CreateSolver();
            
            // Setup constraints
            SetupConstraints();
            
            // Auto-detect ground if needed
            if (autoDetectGround)
            {
                DetectGround();
            }
            
            isInitialized = true;
            
            Debug.Log($"[{gameObject.name}] PBD Soft Body initialized with {solver.Particles.Count} particles " +
                     $"and {solver.Constraints.Count} constraints. Fracture: {enableFracture}");
        }
        
        private void SetupMeshData()
        {
            originalMesh = meshFilter.sharedMesh;
            deformedMesh = Instantiate(originalMesh);
            deformedMesh.name = originalMesh.name + "_PBD_Deformed";
            
            originalVertices = originalMesh.vertices;
            deformedVertices = new Vector3[originalVertices.Length];
            originalVertices.CopyTo(deformedVertices, 0);
            
            originalTriangles = originalMesh.triangles;
            activeTriangles = new List<int>(originalTriangles);
            
            fragments = new List<GameObject>();
            
            meshFilter.mesh = deformedMesh;
        }
        
        private void CreateSolver()
        {
            solver = new PBDSolver();
            solver.ConstraintIterations = constraintIterations;
            solver.GlobalStiffness = globalStiffness;
            solver.GlobalDamping = globalDamping;
            solver.Gravity = new Vector3(0, -9.81f, 0);
            solver.EnableFracture = enableFracture;
            solver.GlobalFractureThreshold = fractureThreshold;
            solver.StressDecayRate = stressDecayRate;
    
            // Calculate total mass
            float volume = CalculateMeshVolume();
            float totalMass = density * Mathf.Max(volume, 1f);
    
            // Initialize solver from mesh
            solver.InitializeFromMesh(originalMesh, totalMass, enableFracture);
    
            // CRITICAL: Transform particles to world space AND ensure correct positioning
            Vector3 meshCenter = originalMesh.bounds.center;
            for (int i = 0; i < solver.Particles.Count && i < originalVertices.Length; i++)
            {
                var particle = solver.Particles[i];
        
                // Transform from local mesh space to world space
                Vector3 worldPos = transform.TransformPoint(originalVertices[i]);
        
                particle.Position = worldPos;
                particle.PredictedPosition = worldPos;
                particle.OriginalPosition = worldPos;
        
                // Ensure particle is active and not fixed
                particle.IsActive = true;
                particle.SetFixed(false);
                particle.SetMass(totalMass / solver.Particles.Count);
            }
    
            Debug.Log($"Solver initialized with {solver.Particles.Count} particles properly positioned");
        }        
        
        private void SetupConstraints()
        {
            // Ground collision constraint
            if (enableGroundCollision)
            {
                solver.AddGroundConstraint(groundY, restitution, friction);
            }
        }
        
        #endregion
        
        #region Mesh Updates
        
        private void UpdateMeshFromParticles()
        {
            if (solver.Particles.Count < deformedVertices.Length) return;
            
            // Update vertices from particle positions
            for (int i = 0; i < deformedVertices.Length; i++)
            {
                if (i < solver.Particles.Count)
                {
                    var particle = solver.Particles[i];
                    if (particle.IsActive)
                    {
                        // Convert from world space back to local space
                        deformedVertices[i] = transform.InverseTransformPoint(particle.Position);
                    }
                    else
                    {
                        // Keep fractured vertices at their last known position instead of hiding them
                        // This prevents the "image in ground" visual artifact
                        deformedVertices[i] = transform.InverseTransformPoint(particle.Position);
                    }
                }
            }
            
            // Update triangles if fracture occurred (this removes broken triangles)
            if (enableFracture && isFractured)
            {
                UpdateTrianglesForFracture();
            }
            
            // Update mesh
            deformedMesh.vertices = deformedVertices;
            deformedMesh.triangles = activeTriangles.ToArray();
            deformedMesh.RecalculateNormals();
            deformedMesh.RecalculateBounds();
        }
        
        private void UpdateTrianglesForFracture()
        {
            var fracturedParticles = solver.GetFracturedParticles();
            var newTriangles = new List<int>();
            
            // Only include triangles that don't have fractured vertices
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                int v0 = originalTriangles[i];
                int v1 = originalTriangles[i + 1];
                int v2 = originalTriangles[i + 2];
                
                // Check if any vertex is fractured
                if (!fracturedParticles.Contains(v0) && 
                    !fracturedParticles.Contains(v1) && 
                    !fracturedParticles.Contains(v2))
                {
                    newTriangles.Add(v0);
                    newTriangles.Add(v1);
                    newTriangles.Add(v2);
                }
            }
            
            activeTriangles = newTriangles;
        }
        
        #endregion
        
        #region Fracture Mechanics
        
        private void CheckForFracture()
        {
            var stats = solver.GetStatistics();
            
            // Check if significant fracture has occurred
            if (stats.brokenConstraints > 5) // Threshold for considering object "fractured"
            {
                isFractured = true;
                OnFracture();
            }
        }
        
        private void OnFracture()
        {
            Debug.Log($"[{gameObject.name}] Object has fractured! Broken constraints: {solver.GetStatistics().brokenConstraints}");
            
            // Create visual effects
            CreateFractureEffects();
            
            // Optionally create fragments
            if (showBrokenConstraints)
            {
                CreateFragments();
            }
            
            // Change material to show fracture
            if (fracturedMaterial != null)
            {
                meshRenderer.material = fracturedMaterial;
            }
        }
        
        private void CreateFractureEffects()
        {
            // Create particle system for fracture effect
            var particles = gameObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 5f;
            main.startSize = 0.1f;
            main.startColor = Color.white;
            main.maxParticles = 50;
            
            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.0f, 50)
            });
            
            // Destroy particle system after a while
            Destroy(particles, 5f);
        }
        
        private void CreateFragments()
        {
            // Simple fragment creation - create small pieces at fractured particle locations
            var fracturedParticles = solver.GetFracturedParticles();
            
            foreach (int particleIndex in fracturedParticles)
            {
                if (particleIndex < solver.Particles.Count)
                {
                    var particle = solver.Particles[particleIndex];
                    CreateFragment(particle.Position);
                }
            }
        }
        
        private void CreateFragment(Vector3 position)
        {
            // Create a small cube fragment
            GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fragment.name = "Fragment";
            fragment.transform.position = position;
            fragment.transform.localScale = Vector3.one * 0.1f;
            
            // Add physics
            Rigidbody rb = fragment.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            
            // Set material
            if (fracturedMaterial != null)
            {
                fragment.GetComponent<Renderer>().material = fracturedMaterial;
            }
            
            // Destroy after a while
            Destroy(fragment, 10f);
            
            fragments.Add(fragment);
        }
        
        #endregion
        
        #region Helper Methods
        
        private PhysicsMaterial CreateDefaultMaterial()
        {
            var mat = ScriptableObject.CreateInstance<PhysicsMaterial>();
            mat.materialName = "PBD_Default";
            mat.density = density;
            mat.restitution = restitution;
            mat.staticFriction = friction;
            mat.dynamicFriction = friction * 0.8f;
            mat.deformationType = DeformationType.Elastic;
            mat.stiffness = globalStiffness * 1000f;
            mat.damping = (1f - globalDamping) * 100f;
            return mat;
        }
        
        private float CalculateMeshVolume()
        {
            var bounds = originalMesh.bounds;
            return bounds.size.x * bounds.size.y * bounds.size.z * 0.52f;
        }
        
        private void DetectGround()
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
                
                Debug.Log($"[{gameObject.name}] Auto-detected ground at Y = {groundY}");
            }
        }
        
        #endregion
        
        #region IDeformable Implementation
        
        public DeformationType DeformationType
        {
            get => physicsMaterial?.deformationType ?? DeformationType.Elastic;
            set { if (physicsMaterial) physicsMaterial.deformationType = value; }
        }
        
        public float ElasticLimit
        {
            get => physicsMaterial?.elasticLimit ?? 1000f;
            set { if (physicsMaterial) physicsMaterial.elasticLimit = value; }
        }
        
        public float PlasticLimit
        {
            get => physicsMaterial?.plasticLimit ?? 2000f;
            set { if (physicsMaterial) physicsMaterial.plasticLimit = value; }
        }
        
        public float BrittleThreshold
        {
            get => physicsMaterial?.brittleThreshold ?? 5000f;
            set { if (physicsMaterial) physicsMaterial.brittleThreshold = value; }
        }
        
        public bool HasDeformation => isInitialized;
        
        public void ApplyDeformation(Vector3 force, Vector3 position)
        {
            if (!isInitialized) return;
            
            Vector3 impulse = force * Time.fixedDeltaTime;
            solver.ApplyImpulse(position, impulse, 0.5f);
        }
        
        public void ApplyDeformation(DeformationData deformation)
        {
            if (!isInitialized) return;
            
            Vector3 impulse = deformation.force * Time.fixedDeltaTime * deformation.intensity;
            solver.ApplyImpulse(deformation.position, impulse, 0.5f);
        }
        
        public Mesh GetDeformedMesh()
        {
            return deformedMesh;
        }
        
        public void ResetDeformation()
        {
            if (isInitialized)
            {
                solver.Reset();
                isFractured = false;
                
                // Reset mesh
                originalVertices.CopyTo(deformedVertices, 0);
                activeTriangles = new List<int>(originalTriangles);
                
                // Destroy fragments
                foreach (var fragment in fragments)
                {
                    if (fragment != null)
                        Destroy(fragment);
                }
                fragments.Clear();
            }
        }
        
        #endregion
        
        #region Debug
        
        void OnDrawGizmos()
        {
            if (!showDebugInfo || !isInitialized || solver == null) return;
            
            if (showParticles)
            {
                // Draw particles
                Gizmos.color = Color.yellow;
                foreach (var particle in solver.Particles)
                {
                    if (particle.IsActive)
                    {
                        Gizmos.DrawWireSphere(particle.Position, 0.02f);
                        
                        if (particle.IsFixed)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawSphere(particle.Position, 0.03f);
                            Gizmos.color = Color.yellow;
                        }
                    }
                    else
                    {
                        Gizmos.color = Color.gray;
                        Gizmos.DrawWireSphere(particle.Position, 0.01f);
                        Gizmos.color = Color.yellow;
                    }
                }
            }
            
            if (showBrokenConstraints && isFractured)
            {
                // Draw broken constraint locations
                Gizmos.color = Color.red;
                var fracturedParticles = solver.GetFracturedParticles();
                foreach (int particleIndex in fracturedParticles)
                {
                    if (particleIndex < solver.Particles.Count)
                    {
                        Gizmos.DrawCube(solver.Particles[particleIndex].Position, Vector3.one * 0.05f);
                    }
                }
            }
        }
        
        void OnGUI()
        {
            if (!showDebugInfo || !isInitialized) return;
            
            var stats = solver.GetStatistics();
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 250));
            GUILayout.Box($"PBD Soft Body Debug\n\n" +
                         $"Particles: {stats.particles} (Active)\n" +
                         $"Constraints: {stats.constraints} ({stats.activeConstraints} active)\n" +
                         $"Broken Constraints: {stats.brokenConstraints}\n" +
                         $"Solve Time: {stats.solveTime * 1000f:F2}ms\n" +
                         $"Iterations Used: {stats.iterations}/{constraintIterations}\n" +
                         $"Global Stiffness: {globalStiffness:F2}\n" +
                         $"Global Damping: {globalDamping:F3}\n" +
                         $"Fracture Enabled: {enableFracture}\n" +
                         $"Is Fractured: {isFractured}\n" +
                         $"Fragments: {fragments.Count}");
            GUILayout.EndArea();
        }
        
        #endregion
        
        #region Public Methods
        
        public void SetFracturedMaterial(Material material)
        {
            fracturedMaterial = material;
        }
        
        public void TriggerFracture()
        {
            if (enableFracture && !isFractured)
            {
                // Apply high stress to all particles to trigger fracture
                foreach (var particle in solver.Particles)
                {
                    particle.AddStress(fractureThreshold * 2f);
                }
                
                isFractured = true;
                OnFracture();
            }
        }
        
        #endregion
    }
}