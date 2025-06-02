// Assets/_Project/01_Physics/Scripts/Deformation/MassSpring/SoftBodyFactory.cs

using _Project._00_Core.Scripts.Abstractions;
using UnityEngine;
using _Project._00_Core.Scripts.DataStructures;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.Deformation.MassSpring
{
    /// <summary>
    /// Factory with balanced, reasonable material parameters for stable soft bodies
    /// </summary>
    public static class SoftBodyFactory
    {
        public struct SoftBodyConfig
        {
            public string name;
            public SoftBodyComponent.ShapeType shapeType;
            public Vector3 position;
            public float size;
            public int resolution;
            public float mass;
            public PhysicsMaterial physicsMaterial;
            public Material renderMaterial;
            public Color? color;
            public bool useGravity;
            public Vector3 gravity;

            public static SoftBodyConfig Default => new SoftBodyConfig
            {
                name = "Soft Body",
                shapeType = SoftBodyComponent.ShapeType.Sphere,
                position = Vector3.zero,
                size = 1f,
                resolution = 8, // Reasonable resolution for stability
                mass = 1f,
                physicsMaterial = null,
                renderMaterial = null,
                color = null,
                useGravity = true,
                gravity = new Vector3(0, -9.81f, 0) // Normal gravity
            };
        }

        public static GameObject CreateSoftBody(SoftBodyConfig config)
        {
            // Create game object
            GameObject obj = new GameObject(config.name);
            obj.transform.position = config.position;

            // Add required components
            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();

            // Set up render material
            SetupRenderMaterial(meshRenderer, config);

            // Create physics material with reasonable defaults
            PhysicsMaterial physMat = config.physicsMaterial ?? CreateReasonablePhysicsMaterial();

            // Create the mass-spring system
            MassSpringSystem system = CreateMassSpringSystemForShape(config, physMat);

            if (system != null)
            {
                // Configure system with reasonable parameters
                ConfigureSystemReasonably(system, config);

                // Set mesh
                meshFilter.mesh = system.GetDeformedMesh();

                // Add wrapper component
                var wrapper = obj.AddComponent<SoftBodyWrapper>();
                wrapper.Initialize(system, physMat);

                LogCreationStats(obj.name, system);
            }

            return obj;
        }

        private static void SetupRenderMaterial(MeshRenderer renderer, SoftBodyConfig config)
        {
            if (config.renderMaterial != null)
            {
                renderer.material = config.renderMaterial;
            }
            else
            {
                // Create default material
                Material defaultMat = null;
                
                string[] shaderNames = { "Universal Render Pipeline/Lit", "Standard" };
                
                foreach (string shaderName in shaderNames)
                {
                    Shader shader = Shader.Find(shaderName);
                    if (shader != null)
                    {
                        defaultMat = new Material(shader);
                        break;
                    }
                }

                if (defaultMat == null)
                {
                    defaultMat = new Material(Shader.Find("Standard"));
                }

                defaultMat.color = config.color ?? Color.white;
                renderer.material = defaultMat;
            }
        }

        private static PhysicsMaterial CreateReasonablePhysicsMaterial()
        {
            var mat = ScriptableObject.CreateInstance<PhysicsMaterial>();
            mat.materialName = "ReasonableSoftBody";
            mat.density = 1.0f;
            mat.restitution = 0.3f;
            mat.staticFriction = 0.6f;
            mat.dynamicFriction = 0.4f;
            mat.deformationType = DeformationType.Elastic;
            mat.stiffness = 2000f;     // Higher stiffness for better structure
            mat.damping = 30f;         // Higher damping for stability
            mat.elasticLimit = 5000f;
            return mat;
        }
        
        private static MassSpringSystem CreateMassSpringSystemForShape(SoftBodyConfig config, PhysicsMaterial physMat)
        {
            return config.shapeType switch
            {
                SoftBodyComponent.ShapeType.Sphere => SoftBodyShapeGenerator.CreateSoftSphere(
                    config.size * 0.5f, 
                    config.resolution, // Use full resolution
                    config.resolution, 
                    config.mass, physMat),
                
                SoftBodyComponent.ShapeType.Cube => SoftBodyShapeGenerator.CreateSoftCube(
                    config.size, 
                    config.resolution, 
                    config.mass, physMat),
                
                SoftBodyComponent.ShapeType.Cylinder => SoftBodyShapeGenerator.CreateSoftCylinder(
                    config.size * 0.5f,
                    config.size,
                    config.resolution,
                    config.resolution / 2,
                    config.mass, physMat),
                
                _ => null
            };
        }

        private static void ConfigureSystemReasonably(MassSpringSystem system, SoftBodyConfig config)
        {
            system.Gravity = config.useGravity ? config.gravity : Vector3.zero;
            system.Method = MassSpringSystem.IntegrationMethod.Verlet;
            system.GlobalDamping = 0.98f; // Reasonable damping for stability
        }

        private static void LogCreationStats(string name, MassSpringSystem system)
        {
            var stats = system.GetStatistics();
            Debug.Log($"Created '{name}': {system.MassPoints.Count} points, {stats.totalSprings} springs");
        }

        public static class Presets
        {
            public static GameObject CreateRubberBall(Vector3 position, float size = 1f)
            {
                var material = CreateReasonableRubberMaterial();
                var config = SoftBodyConfig.Default;
                config.name = "Rubber Ball";
                config.shapeType = SoftBodyComponent.ShapeType.Sphere;
                config.position = position;
                config.size = size;
                config.resolution = 8;  // Good resolution for stability and visual quality
                config.mass = 1.0f;     
                config.physicsMaterial = material;
                config.color = new Color(0.8f, 0.2f, 0.2f);

                return CreateSoftBody(config);
            }

            public static GameObject CreateJellyCube(Vector3 position, float size = 1f)
            {
                var material = CreateJellyMaterial();
                var config = SoftBodyConfig.Default;
                config.name = "Jelly Cube";
                config.shapeType = SoftBodyComponent.ShapeType.Cube;
                config.position = position;
                config.size = size;
                config.resolution = 6;
                config.mass = 0.8f;
                config.physicsMaterial = material;
                config.color = new Color(0.2f, 0.8f, 0.2f);

                return CreateSoftBody(config);
            }

            private static PhysicsMaterial CreateReasonableRubberMaterial()
            {
                var mat = ScriptableObject.CreateInstance<PhysicsMaterial>();
                mat.materialName = "ReasonableRubber";
                mat.density = 1.2f;
                mat.restitution = 0.6f; // Bouncy but not too much
                mat.staticFriction = 0.9f;
                mat.dynamicFriction = 0.8f;
                mat.deformationType = DeformationType.Elastic;
                mat.stiffness = 3000f;  // High structural integrity
                mat.damping = 25f;      // Good damping for stability
                mat.elasticLimit = 10000f;
                return mat;
            }

            private static PhysicsMaterial CreateJellyMaterial()
            {
                var mat = ScriptableObject.CreateInstance<PhysicsMaterial>();
                mat.materialName = "Jelly";
                mat.density = 0.9f;
                mat.restitution = 0.3f;
                mat.staticFriction = 0.5f;
                mat.dynamicFriction = 0.4f;
                mat.deformationType = DeformationType.Elastic;
                mat.stiffness = 500f;   // Softer than rubber
                mat.damping = 30f;      // Higher damping for wobble effect
                mat.elasticLimit = 15000f;
                return mat;
            }
        }
    }
}