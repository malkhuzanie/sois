using UnityEngine;
using _Project._00_Core.Scripts.Abstractions;
using _Project._00_Core.Scripts.DataStructures;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.Deformation.MassSpring
{
    /// <summary>
    /// Unity component that integrates the mass-spring soft body system.
    /// Implements IDeformable interface for compatibility with the physics engine.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SoftBodyComponent : MonoBehaviour, IDeformable
    {
        [Header("Shape Configuration")]
        [SerializeField] private ShapeType shapeType = ShapeType.Sphere;
        [SerializeField] private float size = 1.0f;
        [SerializeField] private int resolution = 10;
        [SerializeField] private float totalMass = 1.0f;
        
        [Header("Physics Material")]
        [SerializeField] private PhysicsMaterial physicsMaterial;
        
        [Header("Simulation Settings")]
        [SerializeField] private MassSpringSystem.IntegrationMethod integrationMethod = MassSpringSystem.IntegrationMethod.Verlet;
        [SerializeField] private float globalDamping = 0.99f;
        [SerializeField] private bool useGravity = true;
        [SerializeField] private Vector3 customGravity = new Vector3(0, -9.81f, 0);
        
        [Header("Interaction")]
        [SerializeField] private float fixedPointRadius = 0.1f;
        [SerializeField] private Vector3[] fixedPoints;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showSprings = false;
        [SerializeField] private bool showStress = false;
        
        // Components
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MassSpringSystem _massSpringSystem;
        
        // Original mesh backup
        private Mesh _originalMesh;
        
        // IDeformable implementation
        public DeformationType DeformationType 
        { 
            get => physicsMaterial?.deformationType ?? DeformationType.Elastic;
            set 
            { 
                if (physicsMaterial != null)
                    physicsMaterial.deformationType = value;
            }
        }
        
        public float ElasticLimit 
        { 
            get => physicsMaterial?.elasticLimit ?? 1000f;
            set 
            { 
                if (physicsMaterial != null)
                    physicsMaterial.elasticLimit = value;
            }
        }
        
        public float PlasticLimit 
        { 
            get => physicsMaterial?.plasticLimit ?? 2000f;
            set 
            { 
                if (physicsMaterial != null)
                    physicsMaterial.plasticLimit = value;
            }
        }
        
        public float BrittleThreshold 
        { 
            get => physicsMaterial?.brittleThreshold ?? 5000f;
            set 
            { 
                if (physicsMaterial != null)
                    physicsMaterial.brittleThreshold = value;
            }
        }
        
        public bool HasDeformation => _massSpringSystem != null;
        
        public enum ShapeType
        {
            Sphere,
            Cube,
            Cylinder,
            Torus,
            CustomMesh
        }
        
        void Start()
        {
            Initialize();
        }
        
        void Initialize()
        {
            // Get components
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            
            // Create default physics material if none assigned
            if (physicsMaterial == null)
            {
                physicsMaterial = new PhysicsMaterial
                {
                    materialName = "Default Soft Body",
                    density = 1.0f,
                    restitution = 0.3f,
                    staticFriction = 0.6f,
                    dynamicFriction = 0.4f,
                    deformationType = DeformationType.Elastic,
                    elasticLimit = 1000f,
                    plasticLimit = 2000f,
                    brittleThreshold = 5000f,
                    stiffness = 1000f,
                    damping = 10f
                };
            }
            
            // Create the soft body based on shape type
            CreateSoftBody();
            
            // Apply fixed points
            if (fixedPoints != null && fixedPoints.Length > 0)
            {
                foreach (var point in fixedPoints)
                {
                    _massSpringSystem.FixPoints(transform.TransformPoint(point), fixedPointRadius);
                }
            }
        }
        
        void CreateSoftBody()
        {
            switch (shapeType)
            {
                case ShapeType.Sphere:
                    _massSpringSystem = SoftBodyShapeGenerator.CreateSoftSphere(
                        size * 0.5f, resolution, resolution, totalMass, physicsMaterial);
                    break;
                    
                case ShapeType.Cube:
                    _massSpringSystem = SoftBodyShapeGenerator.CreateSoftCube(
                        size, resolution, totalMass, physicsMaterial);
                    break;
                    
                case ShapeType.Cylinder:
                    _massSpringSystem = SoftBodyShapeGenerator.CreateSoftCylinder(
                        size * 0.5f, size, resolution, resolution / 2, totalMass, physicsMaterial);
                    break;
                    
                case ShapeType.Torus:
                    _massSpringSystem = SoftBodyShapeGenerator.CreateSoftTorus(
                        size * 0.5f, size * 0.2f, resolution, resolution / 2, totalMass, physicsMaterial);
                    break;
                    
                case ShapeType.CustomMesh:
                    if (_meshFilter.sharedMesh != null)
                    {
                        _originalMesh = _meshFilter.sharedMesh;
                        _massSpringSystem = new MassSpringSystem();
                        _massSpringSystem.InitializeFromMesh(_originalMesh, totalMass, physicsMaterial);
                    }
                    else
                    {
                        Debug.LogError("No mesh assigned for custom soft body!");
                        return;
                    }
                    break;
            }
            
            // Configure the system
            _massSpringSystem.Method = integrationMethod;
            _massSpringSystem.GlobalDamping = globalDamping;
            _massSpringSystem.Gravity = useGravity ? customGravity : Vector3.zero;
            
            // Set the deformed mesh
            _meshFilter.mesh = _massSpringSystem.GetDeformedMesh();
        }
        
        void FixedUpdate()
        {
            if (_massSpringSystem != null)
            {
                // Update the physics simulation
                _massSpringSystem.Update(Time.fixedDeltaTime);
                
                // The mesh is automatically updated by the system
                // but we might need to update bounds for culling
                _meshFilter.mesh.RecalculateBounds();
            }
        }
        
        #region IDeformable Implementation
        
        public void ApplyDeformation(Vector3 force, Vector3 position)
        {
            if (_massSpringSystem != null)
            {
                // Convert world position to local space
                Vector3 localPos = transform.InverseTransformPoint(position);
                
                // Apply as an impulse with falloff
                float impulseMagnitude = force.magnitude;
                float radius = size * 0.3f; // Affect 30% of object size
                
                _massSpringSystem.ApplyImpulse(localPos, force.normalized * impulseMagnitude, radius);
            }
        }
        
        public void ApplyDeformation(DeformationData deformation)
        {
            if (_massSpringSystem != null)
            {
                Vector3 localPos = transform.InverseTransformPoint(deformation.position);
                float radius = size * 0.3f * deformation.intensity;
                
                _massSpringSystem.ApplyImpulse(localPos, deformation.force, radius);
                
                // Handle different deformation types
                if (deformation.type == DeformationType.Brittle)
                {
                    // For brittle deformation, we might want to break springs
                    // This is handled automatically by the spring stress system
                }
            }
        }
        
        public Mesh GetDeformedMesh()
        {
            return _massSpringSystem?.GetDeformedMesh();
        }
        
        public void ResetDeformation()
        {
            _massSpringSystem?.Reset();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Apply an explosion force
        /// </summary>
        public void Explode(Vector3 explosionPosition, float explosionForce, float explosionRadius)
        {
            if (_massSpringSystem != null)
            {
                Vector3 localPos = transform.InverseTransformPoint(explosionPosition);
                Vector3 explosionDir = (transform.position - explosionPosition).normalized;
                
                _massSpringSystem.ApplyImpulse(localPos, explosionDir * explosionForce, explosionRadius);
            }
        }
        
        /// <summary>
        /// Fix/unfix points at runtime
        /// </summary>
        public void ToggleFixedPoint(Vector3 worldPosition, float radius, bool fix)
        {
            if (_massSpringSystem != null)
            {
                Vector3 localPos = transform.InverseTransformPoint(worldPosition);
                
                if (fix)
                {
                    _massSpringSystem.FixPoints(localPos, radius);
                }
                // Note: Unfixing would require adding that functionality to MassSpringSystem
            }
        }
        
        #endregion
        
        #region Debug Visualization
        
        void OnDrawGizmos()
        {
            if (!showDebugInfo || _massSpringSystem == null) return;
            
            // Get statistics
            var stats = _massSpringSystem.GetStatistics();
            
            if (showSprings)
            {
                DrawSprings();
            }
            
            if (showStress)
            {
                DrawStress();
            }
        }
        
        void DrawSprings()
        {
            // This would require exposing the springs from MassSpringSystem
            // For now, just show debug info
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * size);
        }
        
        void DrawStress()
        {
            // Color code the mesh based on stress levels
            // This would require additional implementation
        }
        
        void OnGUI()
        {
            if (showDebugInfo && _massSpringSystem != null)
            {
                var stats = _massSpringSystem.GetStatistics();
                
                GUILayout.BeginArea(new Rect(10, 10, 300, 150));
                GUILayout.Box($"Soft Body Debug Info\n" +
                             $"Total Springs: {stats.totalSprings}\n" +
                             $"Broken Springs: {stats.brokenSprings}\n" +
                             $"Average Stress: {stats.averageStress:F2}\n" +
                             $"Shape: {shapeType}\n" +
                             $"Integration: {integrationMethod}");
                GUILayout.EndArea();
            }
        }
        
        #endregion
    }
}