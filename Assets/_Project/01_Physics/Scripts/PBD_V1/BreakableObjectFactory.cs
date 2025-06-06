// Assets/_Project/01_Physics/Scripts/PBD/BreakableObjectFactory.cs

using _Project._01_Physics.Scripts.PBD_V1.Materials;
using _Project._03_Simulation.Scripts.Diagnostics;
using UnityEngine;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.PBD_V1
{
    /// <summary>
    /// Factory for creating various types of breakable objects with proper configuration
    /// </summary>
    public static class BreakableObjectFactory
    {
        /// <summary>
        /// Configuration for creating breakable objects
        /// </summary>
        [System.Serializable]
        public class BreakableConfig
        {
            public string name = "Breakable Object";
            public Vector3 position = Vector3.zero;
            public Vector3 rotation = Vector3.zero;
            public float size = 1f;
            public int resolution = 12;
            public bool enableDiagnostics = true;
            public bool enableFragments = true;
            public Color primaryColor = Color.white;
            public Color fracturedColor = Color.red;
            public float fractureThreshold = 5f;
            public bool autoDetectGround = true;
        }

        #region Glass Objects

        /// <summary>
        /// Creates a breakable glass ball
        /// </summary>
        public static GameObject CreateGlassBall(Vector3 position, float size = 1f, int resolution = 16)
        {
            var config = new BreakableConfig
            {
                name = "Glass Ball",
                position = position,
                size = size,
                resolution = resolution,
                primaryColor = new Color(0.8f, 0.9f, 1.0f, 0.6f),
                fracturedColor = new Color(1.0f, 0.8f, 0.8f, 0.8f),
                fractureThreshold = 3f
            };

            return CreateBreakableObject(config, ObjectType.Sphere, GlassMaterialPresets.CreateWindowGlass());
        }

        /// <summary>
        /// Creates a breakable crystal glass object
        /// </summary>
        public static GameObject CreateCrystalGlass(Vector3 position, float size = 1f, int resolution = 20)
        {
            var config = new BreakableConfig
            {
                name = "Crystal Glass",
                position = position,
                size = size,
                resolution = resolution,
                primaryColor = new Color(0.9f, 0.95f, 1.0f, 0.4f),
                fracturedColor = new Color(1.0f, 0.9f, 0.9f, 0.7f),
                fractureThreshold = 4f
            };

            return CreateBreakableObject(config, ObjectType.Sphere, GlassMaterialPresets.CreateCrystalGlass());
        }

        /// <summary>
        /// Creates a tempered glass object (stronger)
        /// </summary>
        public static GameObject CreateTemperedGlass(Vector3 position, float size = 1f, int resolution = 14)
        {
            var config = new BreakableConfig
            {
                name = "Tempered Glass",
                position = position,
                size = size,
                resolution = resolution,
                primaryColor = new Color(0.7f, 0.8f, 0.9f, 0.7f),
                fracturedColor = new Color(0.9f, 0.7f, 0.7f, 0.9f),
                fractureThreshold = 8f
            };

            return CreateBreakableObject(config, ObjectType.Sphere, GlassMaterialPresets.CreateTemperedGlass());
        }

        /// <summary>
        /// Creates a bouncy rubber ball that will NOT break
        /// </summary>
        public static GameObject CreateBouncyRubberBall(Vector3 position, float size = 1f, int resolution = 12)
        {
            var config = new BreakableConfig
            {
                name = "Bouncy Rubber Ball",
                position = position,
                size = size,
                resolution = resolution,
                primaryColor = Color.red,
                fracturedColor = Color.red, // Same color since it won't break
                fractureThreshold = float.MaxValue, // Never break
                enableFragments = false // No fragments for elastic objects
            };

            return CreateElasticObject(config, ObjectType.Sphere, ElasticMaterialPresets.CreateBouncyRubber());
        }

        /// <summary>
        /// Creates a super bouncy rubber ball with maximum bounce
        /// </summary>
        public static GameObject CreateSuperBouncyBall(Vector3 position, float size = 1f, int resolution = 12)
        {
            var config = new BreakableConfig
            {
                name = "Super Bouncy Ball",
                position = position,
                size = size,
                resolution = resolution,
                primaryColor = new Color(1f, 0.2f, 0.2f), // Bright red
                fracturedColor = new Color(1f, 0.2f, 0.2f),
                fractureThreshold = float.MaxValue,
                enableFragments = false
            };

            return CreateElasticObject(config, ObjectType.Sphere, ElasticMaterialPresets.CreateSuperBouncyRubber());
        }

        /// <summary>
        /// Creates a breakable ceramic vase
        /// </summary>
        public static GameObject CreateCeramicVase(Vector3 position, float size = 1f)
        {
            var config = new BreakableConfig
            {
                name = "Ceramic Vase",
                position = position,
                size = size,
                resolution = 12,
                primaryColor = new Color(0.8f, 0.6f, 0.4f),
                fracturedColor = new Color(0.6f, 0.4f, 0.3f),
                fractureThreshold = 2f
            };

            return CreateBreakableObject(config, ObjectType.Cylinder, GlassMaterialPresets.CreateCeramic());
        }

        /// <summary>
        /// Creates a breakable ceramic plate
        /// </summary>
        public static GameObject CreateCeramicPlate(Vector3 position, float size = 1f)
        {
            var config = new BreakableConfig
            {
                name = "Ceramic Plate",
                position = position,
                size = size,
                resolution = 10,
                primaryColor = new Color(0.9f, 0.9f, 0.8f),
                fracturedColor = new Color(0.7f, 0.7f, 0.6f),
                fractureThreshold = 1.5f
            };

            return CreateBreakableObject(config, ObjectType.Cylinder, GlassMaterialPresets.CreateCeramic());
        }

        #endregion

        #region Ice Objects

        /// <summary>
        /// Creates a breakable ice cube
        /// </summary>
        public static GameObject CreateIceCube(Vector3 position, float size = 1f)
        {
            var config = new BreakableConfig
            {
                name = "Ice Cube",
                position = position,
                size = size,
                resolution = 8,
                primaryColor = new Color(0.8f, 0.9f, 1.0f, 0.8f),
                fracturedColor = new Color(0.9f, 0.95f, 1.0f, 0.9f),
                fractureThreshold = 3f
            };

            return CreateBreakableObject(config, ObjectType.Cube, GlassMaterialPresets.CreateIce());
        }

        /// <summary>
        /// Creates a breakable icicle
        /// </summary>
        public static GameObject CreateIcicle(Vector3 position, float size = 1f)
        {
            var config = new BreakableConfig
            {
                name = "Icicle",
                position = position,
                size = size,
                resolution = 10,
                primaryColor = new Color(0.85f, 0.95f, 1.0f, 0.7f),
                fracturedColor = new Color(0.9f, 0.97f, 1.0f, 0.8f),
                fractureThreshold = 2f
            };

            return CreateBreakableObject(config, ObjectType.Cone, GlassMaterialPresets.CreateIce());
        }

        #endregion

        #region Core Factory Methods

        public enum ObjectType
        {
            Sphere,
            Cube,
            Cylinder,
            Cone
        }

        /// <summary>
        /// Core method for creating elastic (non-breaking) objects
        /// </summary>
        public static GameObject CreateElasticObject(BreakableConfig config, ObjectType objectType,
            PhysicsMaterial physicsMaterial)
        {
            // Create the game object
            GameObject obj = new GameObject(config.name);
            obj.transform.position = config.position;
            obj.transform.rotation = Quaternion.Euler(config.rotation);

            // Add required components
            var meshFilter = obj.AddComponent<MeshFilter>();
            var meshRenderer = obj.AddComponent<MeshRenderer>();

            // Create appropriate mesh
            meshFilter.mesh = CreateMeshForType(objectType, config.size, config.resolution);

            // Create and assign materials
            var primaryMaterial = CreateVisualMaterial(config.primaryColor, objectType);
            meshRenderer.material = primaryMaterial;

            // Add PBD soft body
            var softBody = obj.AddComponent<PBDSoftBody>();
            ConfigureElasticSoftBody(softBody, config, physicsMaterial); // Different configuration for elastic

            // Add diagnostics if enabled (but with different settings)
            if (config.enableDiagnostics)
            {
                var diagnostics = obj.AddComponent<FractureDiagnostics>();
                ConfigureElasticDiagnostics(diagnostics, config);
            }

            // Initialize after a frame
            var initializer = obj.AddComponent<DelayedInitializer>();
            initializer.Initialize(softBody, physicsMaterial);

            Debug.Log($"Created elastic {config.name} at {config.position} (will NOT break)");
            return obj;
        }

        /// <summary>
        /// Core method for creating any breakable object
        /// </summary>
        public static GameObject CreateBreakableObject(BreakableConfig config, ObjectType objectType,
            PhysicsMaterial physicsMaterial)
        {
            // Create the game object
            GameObject obj = new GameObject(config.name);
            obj.transform.position = config.position;
            obj.transform.rotation = Quaternion.Euler(config.rotation);

            // Add required components
            var meshFilter = obj.AddComponent<MeshFilter>();
            var meshRenderer = obj.AddComponent<MeshRenderer>();

            // Create appropriate mesh
            meshFilter.mesh = CreateMeshForType(objectType, config.size, config.resolution);

            // Create and assign materials
            var primaryMaterial = CreateVisualMaterial(config.primaryColor, objectType);
            var fracturedMaterial = CreateVisualMaterial(config.fracturedColor, objectType);
            meshRenderer.material = primaryMaterial;

            // Add PBD soft body
            var softBody = obj.AddComponent<PBDSoftBody>();
            ConfigureSoftBody(softBody, config, physicsMaterial);

            // Set fractured material
            softBody.SetFracturedMaterial(fracturedMaterial);

            // Add diagnostics if enabled
            if (config.enableDiagnostics)
            {
                var diagnostics = obj.AddComponent<FractureDiagnostics>();
                ConfigureDiagnostics(diagnostics, config);
            }

            // Initialize after a frame
            var initializer = obj.AddComponent<DelayedInitializer>();
            initializer.Initialize(softBody, physicsMaterial);

            Debug.Log($"Created breakable {config.name} at {config.position}");
            return obj;
        }

        private static Mesh CreateMeshForType(ObjectType type, float size, int resolution)
        {
            switch (type)
            {
                case ObjectType.Sphere:
                    return PBDMeshGenerator.GenerateSphereMesh(size * 0.5f, resolution, resolution);

                case ObjectType.Cube:
                    return CreateCubeMesh(size);

                case ObjectType.Cylinder:
                    return CreateCylinderMesh(size * 0.5f, size, resolution);

                case ObjectType.Cone:
                    return CreateConeMesh(size * 0.3f, size, resolution);

                default:
                    return PBDMeshGenerator.GenerateSphereMesh(size * 0.5f, resolution, resolution);
            }
        }

        private static Mesh CreateCubeMesh(float size)
        {
            // Create a simple cube mesh
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempCube.transform.localScale = Vector3.one * size;
            Mesh mesh = Object.Instantiate(tempCube.GetComponent<MeshFilter>().mesh);
            Object.DestroyImmediate(tempCube);
            return mesh;
        }

        private static Mesh CreateCylinderMesh(float radius, float height, int resolution)
        {
            // Create a simple cylinder mesh
            GameObject tempCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tempCylinder.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            Mesh mesh = Object.Instantiate(tempCylinder.GetComponent<MeshFilter>().mesh);
            Object.DestroyImmediate(tempCylinder);
            return mesh;
        }

        private static Mesh CreateConeMesh(float radius, float height, int resolution)
        {
            // For simplicity, use a scaled cylinder - in a real implementation you'd create a proper cone
            GameObject tempCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tempCylinder.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            Mesh mesh = Object.Instantiate(tempCylinder.GetComponent<MeshFilter>().mesh);
            Object.DestroyImmediate(tempCylinder);
            return mesh;
        }

        private static Material CreateVisualMaterial(Color color, ObjectType objectType)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;

            // Configure material properties based on object type
            switch (objectType)
            {
                case ObjectType.Sphere: // Glass-like
                    material.SetFloat("_Metallic", 0.0f);
                    material.SetFloat("_Smoothness", 0.9f);
                    if (color.a < 1.0f && material.HasProperty("_Surface"))
                    {
                        material.SetFloat("_Surface", 1); // Transparent
                    }

                    break;

                case ObjectType.Cube: // Ice-like
                    material.SetFloat("_Metallic", 0.1f);
                    material.SetFloat("_Smoothness", 0.8f);
                    break;

                case ObjectType.Cylinder: // Ceramic-like
                    material.SetFloat("_Metallic", 0.0f);
                    material.SetFloat("_Smoothness", 0.6f);
                    break;

                case ObjectType.Cone: // Ice/crystal-like
                    material.SetFloat("_Metallic", 0.2f);
                    material.SetFloat("_Smoothness", 0.9f);
                    break;
            }

            return material;
        }

        private static void ConfigureElasticSoftBody(PBDSoftBody softBody, BreakableConfig config, PhysicsMaterial physicsMaterial)
        {
            // CRITICAL: Disable fracture completely
            SetPrivateField(softBody, "enableFracture", false);
            SetPrivateField(softBody, "fractureThreshold", float.MaxValue);
            SetPrivateField(softBody, "stressDecayRate", 1.0f);
    
            // MAXIMUM settings for PERFECT shape preservation
            SetPrivateField(softBody, "constraintIterations", 8); // High iterations
            SetPrivateField(softBody, "globalStiffness", 1.0f); // MAXIMUM stiffness
            SetPrivateField(softBody, "globalDamping", 0.995f); // High but not excessive damping
    
            // Ground collision with good bounce
            SetPrivateField(softBody, "enableGroundCollision", true);
            SetPrivateField(softBody, "autoDetectGround", true);
            SetPrivateField(softBody, "groundY", 0.0f);
    
            // Material properties for bouncy behavior
            SetPrivateField(softBody, "restitution", 0.8f); // High bounce
            SetPrivateField(softBody, "friction", 0.2f); // Low friction
    
            // Debug settings
            SetPrivateField(softBody, "showDebugInfo", true);
            SetPrivateField(softBody, "showParticles", false);
            SetPrivateField(softBody, "showBrokenConstraints", false);
    
            Debug.Log("Configured ULTRA-RIGID elastic soft body with RIGID constraints!");
        }        
        
        private static void ConfigureElasticDiagnostics(FractureDiagnostics diagnostics, BreakableConfig config)
        {
            // Configure diagnostics for elastic objects (focus on deformation, not fracture)
            var diagType = typeof(FractureDiagnostics);

            SetPrivateField(diagnostics, "enableContinuousMonitoring", true);
            SetPrivateField(diagnostics, "logConstraintBreaking", false); // No breaking expected
            SetPrivateField(diagnostics, "showStressVisualization", false); // Less relevant for elastic
            SetPrivateField(diagnostics, "stressAlertThreshold", float.MaxValue); // Never alert
        }

        private static void ConfigureSoftBody(PBDSoftBody softBody, BreakableConfig config,
            PhysicsMaterial physicsMaterial)
        {
            // Use reflection to configure private fields for BRITTLE (breakable) behavior
            var softBodyType = typeof(PBDSoftBody);

            // Fracture settings
            SetPrivateField(softBody, "enableFracture", true);
            SetPrivateField(softBody, "fractureThreshold", config.fractureThreshold);
            SetPrivateField(softBody, "stressDecayRate", 0.9f);

            // Solver settings for brittle materials
            SetPrivateField(softBody, "constraintIterations", 6);
            SetPrivateField(softBody, "globalStiffness", 0.9f);
            SetPrivateField(softBody, "globalDamping", 0.98f);

            // Ground collision
            SetPrivateField(softBody, "enableGroundCollision", true);
            SetPrivateField(softBody, "autoDetectGround", config.autoDetectGround);

            // Debug settings
            SetPrivateField(softBody, "showDebugInfo", config.enableDiagnostics);
            SetPrivateField(softBody, "showBrokenConstraints", config.enableFragments);
        }

        private static void ConfigureDiagnostics(FractureDiagnostics diagnostics, BreakableConfig config)
        {
            // Use reflection to configure diagnostics
            var diagType = typeof(FractureDiagnostics);

            SetPrivateField(diagnostics, "enableContinuousMonitoring", true);
            SetPrivateField(diagnostics, "logConstraintBreaking", true);
            SetPrivateField(diagnostics, "showStressVisualization", true);
            SetPrivateField(diagnostics, "stressAlertThreshold", config.fractureThreshold * 0.8f);
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"Field {fieldName} not found in {obj.GetType().Name}");
            }
        }

        #endregion

        #region Preset Configurations

        /// <summary>
        /// Get configuration for very fragile objects
        /// </summary>
        public static BreakableConfig GetFragileConfig()
        {
            return new BreakableConfig
            {
                fractureThreshold = 1f,
                enableDiagnostics = true,
                enableFragments = true,
                resolution = 8 // Lower resolution for faster fracture
            };
        }

        /// <summary>
        /// Get configuration for tough objects
        /// </summary>
        public static BreakableConfig GetToughConfig()
        {
            return new BreakableConfig
            {
                fractureThreshold = 15f,
                enableDiagnostics = true,
                enableFragments = true,
                resolution = 20 // Higher resolution for more constraints
            };
        }

        /// <summary>
        /// Get configuration for demonstration objects
        /// </summary>
        public static BreakableConfig GetDemoConfig()
        {
            return new BreakableConfig
            {
                fractureThreshold = 5f,
                enableDiagnostics = true,
                enableFragments = true,
                resolution = 12 // Balanced for good visual and performance
            };
        }

        #endregion
    }

    /// <summary>
    /// Helper component for delayed initialization with better debugging
    /// </summary>
    public class DelayedInitializer : MonoBehaviour
    {
        private PBDSoftBody softBody;
        private PhysicsMaterial physicsMaterial;

        public void Initialize(PBDSoftBody sb, PhysicsMaterial pm)
        {
            softBody = sb;
            physicsMaterial = pm;
            StartCoroutine(InitializeAfterFrame());
        }

        private System.Collections.IEnumerator InitializeAfterFrame()
        {
            yield return null;
    
            if (softBody != null)
            {
                Debug.Log($"Initializing {softBody.gameObject.name}...");
        
                softBody.Initialize(physicsMaterial);
        
                yield return null;
        
                if (softBody.Solver != null)
                {
                    // Standard initialization...
                    foreach (var particle in softBody.Solver.Particles)
                    {
                        if (particle.IsFixed)
                        {
                            particle.SetFixed(false);
                        }
                        particle.SetMass(1.0f);
                    }
            
                    softBody.Solver.Gravity = new Vector3(0, -9.81f, 0);
            
                    // DISABLE SHAPE MEMORY FOR NOW - LET'S GET BASIC PHYSICS WORKING FIRST
                    /*
                    if (!softBody.Solver.EnableFracture)
                    {
                        var meshFilter = softBody.GetComponent<MeshFilter>();
                        if (meshFilter && meshFilter.mesh)
                        {
                            float originalRadius = meshFilter.mesh.bounds.size.magnitude * 0.25f;
                            softBody.Solver.AddShapeMemoryConstraints(originalRadius);
                            Debug.Log($"Added shape memory constraints for elastic object with radius {originalRadius:F2}");
                        }
                    }
                    */
            
                    Debug.Log($"BASIC Object initialized: {softBody.Solver.Particles.Count} particles, {softBody.Solver.Constraints.Count} constraints");
                    Debug.Log($"Fracture enabled: {softBody.Solver.EnableFracture}");
                }
            }
    
            Destroy(this);
        }
    }
}