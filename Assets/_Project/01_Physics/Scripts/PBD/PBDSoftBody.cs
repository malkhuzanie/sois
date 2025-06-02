// Assets/_Project/01_Physics/Scripts/PBD/PBDSoftBody.cs

using UnityEngine;
using _Project._00_Core.Scripts.Abstractions;
using _Project._00_Core.Scripts.DataStructures;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.PBD
{
    /// <summary>
    /// Enhanced PBD soft body with proper mesh generation
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PBDSoftBody : MonoBehaviour, IDeformable
    {
        [Header("Mesh Generation")]
        [SerializeField] private bool useCustomMesh = true;
        [SerializeField] private int sphereResolution = 12; // Increased default resolution
        [SerializeField] private float sphereRadius = 0.5f;
        
        [Header("PBD Solver Settings")]
        [SerializeField] private int constraintIterations = 8;
        [SerializeField] private float globalStiffness = 0.9f;
        [SerializeField] private float globalDamping = 0.99f;
        
        [Header("Material Properties")]
        [SerializeField] private float density = 1.0f;
        [SerializeField] private float restitution = 0.6f;
        [SerializeField] private float friction = 0.4f;
        
        [Header("Ground Collision")]
        [SerializeField] private bool enableGroundCollision = true;
        [SerializeField] private float groundY = 0f;
        [SerializeField] private bool autoDetectGround = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showConstraints = false;
        
        [Header("Rubber Behavior")]
        [SerializeField] private bool enableDeformationVisualization = true;
        [SerializeField] private float deformationScale = 1.5f;
        
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
        
        // State
        private bool isInitialized = false;
        
        public PBDSolver Solver => solver;
        
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
        }
        
        #endregion
        
        // #region Initialization
        
        public void Initialize(PhysicsMaterial material = null)
        {
            // Get components
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            // Create or use existing mesh
            if (useCustomMesh)
            {
                CreateCustomSphereMesh();
            }
            else if (meshFilter.sharedMesh == null)
            {
                Debug.LogError($"[{gameObject.name}] No mesh found on MeshFilter!");
                return;
            }
            
            // Set physics material
            physicsMaterial = material ?? CreateDefaultMaterial();
            
            // Create solver
            solver = new PBDSolver();
            ConfigureSolver();
            
            // Initialize from mesh
            InitializeFromMesh();
            
            // Setup constraints
            SetupConstraints();
            
            // Auto-detect ground if needed
            if (autoDetectGround)
            {
                DetectGround();
            }
            
            isInitialized = true;
            
            Debug.Log($"[{gameObject.name}] PBD Soft Body initialized with {solver.Particles.Count} particles and {solver.Constraints.Count} constraints");
        }
        
        private void CreateCustomSphereMesh()
        {
            // Use our custom sphere generator
            originalMesh = PBDMeshGenerator.GenerateSphereMesh(sphereRadius, sphereResolution, sphereResolution);
            meshFilter.mesh = originalMesh;
            
            Debug.Log($"Created custom sphere mesh with resolution {sphereResolution}");
        }
        
        private void ConfigureSolver()
        {
            solver.ConstraintIterations = constraintIterations;
            solver.GlobalStiffness = globalStiffness;
            solver.GlobalDamping = globalDamping;
            solver.Gravity = new Vector3(0, -9.81f, 0);
        }
        
        private void InitializeFromMesh()
        {
            if (originalMesh == null)
                originalMesh = meshFilter.sharedMesh;
                
            deformedMesh = Instantiate(originalMesh);
            deformedMesh.name = originalMesh.name + "_PBD_Deformed";
            
            originalVertices = originalMesh.vertices;
            deformedVertices = new Vector3[originalVertices.Length];
            originalVertices.CopyTo(deformedVertices, 0);
            
            // Calculate total mass
            float volume = CalculateMeshVolume();
            float totalMass = density * Mathf.Max(volume, 1f);
            
            // Initialize solver from mesh
            solver.InitializeFromMesh(originalMesh, totalMass);
            
            // Apply transform to particles
            for (int i = 0; i < solver.Particles.Count; i++)
            {
                var particle = solver.Particles[i];
                particle.Position = transform.TransformPoint(particle.Position);
                particle.PredictedPosition = particle.Position;
                particle.OriginalPosition = particle.Position;
            }
            
            meshFilter.mesh = deformedMesh;
        }
        
        private void SetupConstraints()
        {
            // Ground collision constraint
            if (enableGroundCollision)
            {
                solver.AddGroundConstraint(groundY, restitution, friction);
            }
        }
        
        // Rest of the methods remain similar but with better error handling...
        
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
            // Simplified volume calculation
            var bounds = originalMesh.bounds;
            return bounds.size.x * bounds.size.y * bounds.size.z * 0.52f; // Approximate sphere volume factor
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
        
        private void UpdateMeshFromParticles()
        {
            if (solver.Particles.Count != deformedVertices.Length) return;
            
            // Update vertices from particle positions
            for (int i = 0; i < solver.Particles.Count; i++)
            {
                // Convert from world space back to local space
                deformedVertices[i] = transform.InverseTransformPoint(solver.Particles[i].Position);
            }
            
            // Update mesh
            deformedMesh.vertices = deformedVertices;
            deformedMesh.RecalculateNormals();
            deformedMesh.RecalculateBounds();
        }
        
        private void ConfigureForRubberBehavior()
        {
            if (solver != null)
            {
                // Rubber-specific solver settings
                solver.ConstraintIterations = Mathf.Min(constraintIterations, 4); // Fewer iterations for flexibility
                solver.GlobalStiffness = Mathf.Min(globalStiffness, 0.7f); // Limit stiffness for deformation
                solver.GlobalDamping = Mathf.Max(globalDamping, 0.99f); // High damping value for energy retention
        
                // Enhance ground collision with rubber properties
                if (enableGroundCollision)
                {
                    // Remove existing ground constraints
                    solver.Constraints.RemoveAll(c => c is GroundConstraint);
            
                    // Add enhanced ground constraint
                    var enhancedGroundConstraint = new GroundConstraint(groundY, restitution, friction * 0.7f);
                    solver.AddConstraint(enhancedGroundConstraint);
                }
            }
        }
        
        public void SetRubberBehavior(float bounceMultiplier = 1.5f)
        {
            if (physicsMaterial != null)
            {
                physicsMaterial.restitution *= bounceMultiplier;
                physicsMaterial.stiffness *= 0.7f; // Reduce stiffness for more deformation
                physicsMaterial.damping *= 0.5f;   // Reduce damping for more bounce
            }
    
            ConfigureForRubberBehavior();
    
            Debug.Log($"Rubber behavior applied - Restitution: {physicsMaterial?.restitution:F2}");
        }
        
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
            }
        }
        
        #endregion
        
        #region Debug
        
        void OnDrawGizmos()
        {
            if (!showDebugInfo || !isInitialized || solver == null) return;
            
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
        
        void OnGUI()
        {
            if (!showDebugInfo || !isInitialized) return;
            
            var stats = solver.GetStatistics();
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box($"PBD Soft Body Debug\n\n" +
                         $"Particles: {stats.particles}\n" +
                         $"Constraints: {stats.constraints} ({stats.activeConstraints} active)\n" +
                         $"Solve Time: {stats.solveTime * 1000f:F2}ms\n" +
                         $"Iterations Used: {stats.iterations}/{constraintIterations}\n" +
                         $"Global Stiffness: {globalStiffness:F2}\n" +
                         $"Global Damping: {globalDamping:F3}");
            GUILayout.EndArea();
        }
        
        #endregion
    }
}