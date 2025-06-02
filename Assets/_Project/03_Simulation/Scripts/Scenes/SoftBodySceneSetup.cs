using UnityEngine;
using _Project._00_Core.Scripts.DataStructures;
using _Project._01_Physics.Scripts.Deformation.MassSpring;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._03_Simulation.Scripts.Scenes
{
    /// <summary>
    /// Sets up a complete soft body demonstration scene with falling objects.
    /// No input required - just run and watch!
    /// </summary>
    public class SoftBodySceneSetup : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private bool createGround = true;
        [SerializeField] private bool createSoftBodies = true;
        [SerializeField] private bool createLighting = true;
        [SerializeField] private bool createCamera = true;
        
        [Header("Ground Settings")]
        [SerializeField] private Vector3 groundSize = new Vector3(20, 0.5f, 20);
        [SerializeField] private Material groundMaterial;
        
        [Header("Soft Body Objects")]
        [SerializeField] private Vector3 ballSpawnPosition = new Vector3(-2, 5, 0);
        [SerializeField] private Vector3 cubeSpawnPosition = new Vector3(2, 7, 0);
        [SerializeField] private Material softBodyMaterial;
        
        [Header("Physics Settings")]
        [SerializeField] private float globalGravity = -9.81f;
        
        // Physics materials for different objects
        private PhysicsMaterial rubberMaterial;
        private PhysicsMaterial jellyMaterial;
        
        void Start()
        {
            SetupScene();
        }
        
        void SetupScene()
        {
            // Create physics materials
            CreatePhysicsMaterials();
            
            // Setup scene components
            if (createCamera) SetupCamera();
            if (createLighting) SetupLighting();
            if (createGround) CreateGroundPlane();
            if (createSoftBodies) CreateSoftBodyObjects();
            
            // Configure physics
            Time.fixedDeltaTime = 0.02f; // 50 Hz physics update
        }
        
        void CreatePhysicsMaterials()
        {
            // Rubber material for the ball
            rubberMaterial = ScriptableObject.CreateInstance<PhysicsMaterial>();
            rubberMaterial.materialName = "Rubber";
            rubberMaterial.density = 1.2f;
            rubberMaterial.restitution = 0.7f;  // Bouncy
            rubberMaterial.staticFriction = 0.9f;
            rubberMaterial.dynamicFriction = 0.8f;
            rubberMaterial.deformationType = DeformationType.Elastic;
            rubberMaterial.stiffness = 1500f;
            rubberMaterial.damping = 15f;
            rubberMaterial.elasticLimit = 2000f;
            
            // Jelly material for the cube
            jellyMaterial = ScriptableObject.CreateInstance<PhysicsMaterial>();
            jellyMaterial.materialName = "Jelly";
            jellyMaterial.density = 0.9f;
            jellyMaterial.restitution = 0.3f;
            jellyMaterial.staticFriction = 0.5f;
            jellyMaterial.dynamicFriction = 0.4f;
            jellyMaterial.deformationType = DeformationType.Elastic;
            jellyMaterial.stiffness = 500f;   // Soft
            jellyMaterial.damping = 30f;      // High damping for wobble
            jellyMaterial.elasticLimit = 3000f;
        }
        
        void SetupCamera()
        {
            // Check if main camera exists
            if (Camera.main == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();
                cameraObj.tag = "MainCamera";
            }
            
            // Position the camera
            Camera.main.transform.position = new Vector3(0, 8, -15);
            Camera.main.transform.rotation = Quaternion.Euler(20, 0, 0);
            Camera.main.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
            
            // Add a simple camera controller
            var controller = Camera.main.gameObject.AddComponent<SimpleCameraController>();
        }
        
        void SetupLighting()
        {
            // Directional light (sun)
            GameObject sunLight = new GameObject("Sun Light");
            Light sun = sunLight.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.0f;
            sun.color = new Color(1f, 0.95f, 0.8f);
            sunLight.transform.rotation = Quaternion.Euler(45, -30, 0);
            sun.shadows = LightShadows.Soft;
            
            // Ambient light
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.5f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
        }
        
        void CreateGroundPlane()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = groundSize;
            
            // Create or use ground material
            if (groundMaterial == null)
            {
                groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                groundMaterial.color = new Color(0.7f, 0.7f, 0.7f);
            }
            ground.GetComponent<Renderer>().material = groundMaterial;
            
            // Make it static for optimization
            ground.isStatic = true;
            
            // Add collision for soft bodies to interact with
            // Since we're using custom physics, we need to handle ground collision in the soft body system
            ground.layer = LayerMask.NameToLayer("Default");
            ground.tag = "Ground";
        }
        
        void CreateSoftBodyObjects()
        {
            // Create soft ball
            CreateSoftBall();
            
            // Create soft cube
            CreateSoftCube();
            
            // Create additional objects for variety
            CreateAdditionalObjects();
        }
        
        void CreateSoftBall()
        {
            GameObject ball = new GameObject("Soft Rubber Ball");
            ball.transform.position = ballSpawnPosition;
            
            // Add required components
            MeshFilter meshFilter = ball.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = ball.AddComponent<MeshRenderer>();
            
            // Create or use soft body material
            if (softBodyMaterial == null)
            {
                softBodyMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                softBodyMaterial.color = new Color(1f, 0.2f, 0.2f); // Red
            }
            meshRenderer.material = softBodyMaterial;
            
            // Add soft body component
            SoftBodyComponent softBody = ball.AddComponent<SoftBodyComponent>();
            
            // Configure via reflection (since we can't directly set serialized fields)
            System.Type type = softBody.GetType();
            
            // Set shape type to sphere
            var shapeField = type.GetField("shapeType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            shapeField?.SetValue(softBody, SoftBodyComponent.ShapeType.Sphere);
            
            // Set size
            var sizeField = type.GetField("size", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sizeField?.SetValue(softBody, 1.0f);
            
            // Set resolution
            var resField = type.GetField("resolution", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            resField?.SetValue(softBody, 12);
            
            // Set mass
            var massField = type.GetField("totalMass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            massField?.SetValue(softBody, 1.0f);
            
            // Set physics material
            var matField = type.GetField("physicsMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            matField?.SetValue(softBody, rubberMaterial);
            
            // Set gravity
            var gravityField = type.GetField("customGravity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            gravityField?.SetValue(softBody, new Vector3(0, globalGravity, 0));
            
            // Add ground collision handler
            ball.AddComponent<GroundCollisionHandler>();
        }
        
        void CreateSoftCube()
        {
            GameObject cube = new GameObject("Soft Jelly Cube");
            cube.transform.position = cubeSpawnPosition;
            
            // Add required components
            MeshFilter meshFilter = cube.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = cube.AddComponent<MeshRenderer>();
            
            // Create material
            Material cubeMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            cubeMaterial.color = new Color(0.2f, 1f, 0.2f); // Green
            meshRenderer.material = cubeMaterial;
            
            // Add soft body component
            SoftBodyComponent softBody = cube.AddComponent<SoftBodyComponent>();
            
            // Configure via reflection
            System.Type type = softBody.GetType();
            
            // Set shape type to cube
            var shapeField = type.GetField("shapeType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            shapeField?.SetValue(softBody, SoftBodyComponent.ShapeType.Cube);
            
            // Set size
            var sizeField = type.GetField("size", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sizeField?.SetValue(softBody, 1.2f);
            
            // Set resolution
            var resField = type.GetField("resolution", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            resField?.SetValue(softBody, 5);
            
            // Set mass
            var massField = type.GetField("totalMass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            massField?.SetValue(softBody, 0.8f);
            
            // Set physics material
            var matField = type.GetField("physicsMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            matField?.SetValue(softBody, jellyMaterial);
            
            // Set gravity
            var gravityField = type.GetField("customGravity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            gravityField?.SetValue(softBody, new Vector3(0, globalGravity, 0));
            
            // Add ground collision handler
            cube.AddComponent<GroundCollisionHandler>();
        }
        
        void CreateAdditionalObjects()
        {
            // Create a few more objects at different heights
            for (int i = 0; i < 3; i++)
            {
                GameObject obj = new GameObject($"Soft Object {i + 3}");
                obj.transform.position = new Vector3(
                    Random.Range(-3f, 3f),
                    Random.Range(10f, 15f),
                    Random.Range(-2f, 2f)
                );
                
                // Add components
                MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
                
                // Random color
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(Random.value, Random.value, Random.value);
                meshRenderer.material = mat;
                
                // Add soft body
                SoftBodyComponent softBody = obj.AddComponent<SoftBodyComponent>();
                
                // Configure randomly
                System.Type type = softBody.GetType();
                
                // Random shape
                var shapes = new SoftBodyComponent.ShapeType[] { 
                    SoftBodyComponent.ShapeType.Sphere, 
                    SoftBodyComponent.ShapeType.Cube,
                    SoftBodyComponent.ShapeType.Cylinder
                };
                var shapeField = type.GetField("shapeType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                shapeField?.SetValue(softBody, shapes[Random.Range(0, shapes.Length)]);
                
                // Random size
                var sizeField = type.GetField("size", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                sizeField?.SetValue(softBody, Random.Range(0.8f, 1.5f));
                
                // Set other properties
                var resField = type.GetField("resolution", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                resField?.SetValue(softBody, Random.Range(5, 10));
                
                var massField = type.GetField("totalMass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                massField?.SetValue(softBody, Random.Range(0.5f, 1.5f));
                
                var matField = type.GetField("physicsMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                matField?.SetValue(softBody, Random.value > 0.5f ? rubberMaterial : jellyMaterial);
                
                var gravityField = type.GetField("customGravity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                gravityField?.SetValue(softBody, new Vector3(0, globalGravity, 0));
                
                // Add ground collision
                obj.AddComponent<GroundCollisionHandler>();
            }
        }
    }
    
    /// <summary>
    /// Simple camera controller that doesn't use legacy Input
    /// Uses mouse position for orbit control
    /// </summary>
    public class SimpleCameraController : MonoBehaviour
    {
        [SerializeField] private float orbitSpeed = 2f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private Vector3 targetPoint = Vector3.zero;
        [SerializeField] private float distance = 15f;
        
        private float currentYaw = 0f;
        private float currentPitch = 20f;
        
        void Start()
        {
            // Set initial position
            UpdateCameraPosition();
        }
        
        void Update()
        {
            // Simple automatic rotation for demo
            currentYaw += orbitSpeed * Time.deltaTime;
            
            // Gentle vertical oscillation
            currentPitch = 20f + Mathf.Sin(Time.time * 0.2f) * 10f;
            
            UpdateCameraPosition();
        }
        
        void UpdateCameraPosition()
        {
            // Calculate position based on spherical coordinates
            float pitchRad = currentPitch * Mathf.Deg2Rad;
            float yawRad = currentYaw * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            ) * distance;
            
            transform.position = targetPoint + offset;
            transform.LookAt(targetPoint);
        }
    }
    
    /// <summary>
    /// Handles ground collision for soft bodies
    /// Since we're using custom physics, we need custom collision handling
    /// </summary>
    public class GroundCollisionHandler : MonoBehaviour
    {
        private SoftBodyComponent softBody;
        private float groundY = 0.25f; // Top of ground
        private float repulsionForce = 500f;
        private float damping = 0.8f;
        
        void Start()
        {
            softBody = GetComponent<SoftBodyComponent>();
            
            // Find ground height
            GameObject ground = GameObject.FindGameObjectWithTag("Ground");
            if (ground != null)
            {
                groundY = ground.transform.position.y + ground.transform.localScale.y * 0.5f;
            }
        }
        
        void FixedUpdate()
        {
            if (softBody == null) return;
            
            // Simple ground collision
            // Check if any part of the soft body is below ground
            Bounds bounds = softBody.GetComponent<MeshFilter>().mesh.bounds;
            Vector3 worldCenter = transform.TransformPoint(bounds.center);
            float bottomY = worldCenter.y - bounds.extents.y;
            
            if (bottomY < groundY)
            {
                // Apply upward force
                float penetration = groundY - bottomY;
                Vector3 force = Vector3.up * (penetration * repulsionForce);
                
                // Apply to the bottom of the object
                Vector3 contactPoint = new Vector3(worldCenter.x, bottomY, worldCenter.z);
                DeformationData deformation = new DeformationData
                {
                    force = force,
                    position = contactPoint,
                    intensity = Mathf.Clamp01(penetration),
                    type = softBody.DeformationType
                };
                
                softBody.ApplyDeformation(deformation);
            }
        }
    }
}