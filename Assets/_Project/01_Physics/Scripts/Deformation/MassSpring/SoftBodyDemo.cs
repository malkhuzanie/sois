using UnityEngine;
using UnityEngine.InputSystem; // Added for the new Input System
using _Project._00_Core.Scripts.DataStructures;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.Deformation.MassSpring
{
    /// <summary>
    /// Demo scene controller for testing soft body physics.
    /// Shows how to interact with soft bodies and demonstrates different materials.
    /// </summary>
    public class SoftBodyDemo : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject softBodyPrefab; // Note: This prefab is not used in the SpawnSoftBody method as written.
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spawnForce = 5f;
        
        [Header("Material Presets")]
        [SerializeField] private PhysicsMaterial rubberMaterial;
        [SerializeField] private PhysicsMaterial jellyMaterial;
        [SerializeField] private PhysicsMaterial glassMaterial;
        [SerializeField] private PhysicsMaterial clothMaterial;
        
        [Header("Interaction")]
        [SerializeField] private float interactionForce = 100f;
        [SerializeField] private float interactionRadius = 0.5f;
        [SerializeField] private LayerMask softBodyLayer;
        
        private Camera mainCamera;
        
        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera not found. Make sure you have a camera tagged 'MainCamera' in the scene.");
                enabled = false; // Disable script if no main camera
                return;
            }
            
            // Create default materials if not assigned
            CreateDefaultMaterials();
            
            // Spawn initial objects
            SpawnDemoObjects();
        }
        
        void CreateDefaultMaterials()
        {
            if (rubberMaterial == null)
            {
                rubberMaterial = ScriptableObject.CreateInstance<PhysicsMaterial>();
                rubberMaterial.materialName = "Rubber";
                rubberMaterial.density = 1.2f;
                rubberMaterial.restitution = 0.8f;  // Very bouncy
                rubberMaterial.staticFriction = 0.9f;
                rubberMaterial.dynamicFriction = 0.8f;
                rubberMaterial.deformationType = DeformationType.Elastic;
                rubberMaterial.stiffness = 2000f;  // Quite stiff
                rubberMaterial.damping = 20f;
                rubberMaterial.elasticLimit = 2000f;
            }
            
            if (jellyMaterial == null)
            {
                jellyMaterial = ScriptableObject.CreateInstance<PhysicsMaterial>();
                jellyMaterial.materialName = "Jelly";
                jellyMaterial.density = 0.9f;
                jellyMaterial.restitution = 0.3f;
                jellyMaterial.staticFriction = 0.5f;
                jellyMaterial.dynamicFriction = 0.4f;
                jellyMaterial.deformationType = DeformationType.Elastic;
                jellyMaterial.stiffness = 300f;  // Very soft
                jellyMaterial.damping = 50f;     // High damping for wobble
                jellyMaterial.elasticLimit = 3000f;
            }
            
            if (glassMaterial == null)
            {
                glassMaterial = ScriptableObject.CreateInstance<PhysicsMaterial>();
                glassMaterial.materialName = "Glass";
                glassMaterial.density = 2.5f;
                glassMaterial.restitution = 0.4f;
                glassMaterial.staticFriction = 0.4f;
                glassMaterial.dynamicFriction = 0.3f;
                glassMaterial.deformationType = DeformationType.Brittle;
                glassMaterial.stiffness = 5000f;  // Very stiff
                glassMaterial.damping = 5f;       // Low damping
                glassMaterial.brittleThreshold = 1000f;  // Breaks easily
            }
            
            if (clothMaterial == null)
            {
                clothMaterial = ScriptableObject.CreateInstance<PhysicsMaterial>();
                clothMaterial.materialName = "Cloth";
                clothMaterial.density = 0.3f;
                clothMaterial.restitution = 0.1f;
                clothMaterial.staticFriction = 0.7f;
                clothMaterial.dynamicFriction = 0.6f;
                clothMaterial.deformationType = DeformationType.Elastic;
                clothMaterial.stiffness = 100f;   // Very flexible
                clothMaterial.damping = 10f;
                clothMaterial.elasticLimit = 5000f;  // Can stretch a lot
            }
        }
        
        void SpawnDemoObjects()
        {
            // Spawn a variety of soft body objects
            Vector3 basePosition = spawnPoint ? spawnPoint.position : Vector3.zero;
            
            // Rubber ball
            SpawnSoftBody(SoftBodyComponent.ShapeType.Sphere, rubberMaterial, 
                         basePosition + Vector3.left * 2, "Rubber Ball");
            
            // Jelly cube
            // SpawnSoftBody(SoftBodyComponent.ShapeType.Cube, jellyMaterial, 
            //              basePosition, "Jelly Cube");
            
            // Glass cylinder (brittle)
            // SpawnSoftBody(SoftBodyComponent.ShapeType.Cylinder, glassMaterial, 
            //              basePosition + Vector3.right * 2, "Glass Cylinder");
        }
        
        GameObject SpawnSoftBody(SoftBodyComponent.ShapeType shape, PhysicsMaterial material, 
                                Vector3 position, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;
            
            // Add mesh components
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            
            // Add soft body component
            var softBody = obj.AddComponent<SoftBodyComponent>();
            
            // TODO: Configure the softBody with the provided 'material' and 'shape'.
            // Example: softBody.Initialize(shape, material);
            // Currently, the 'material' and 'shape' parameters are not used to configure the softBody.
            
            return obj;
        }
        
        void Update()
        {
            HandleMouseInteraction();
            HandleKeyboardInput();
        }
        
        void HandleMouseInteraction()
        {
            // Guard clause if no mouse is present
            if (Mouse.current == null) return;

            bool leftMouseButtonHeld = Mouse.current.leftButton.isPressed;
            bool rightMouseButtonHeld = Mouse.current.rightButton.isPressed;
            bool middleMouseButtonHeld = Mouse.current.middleButton.isPressed;
            
            if (leftMouseButtonHeld || rightMouseButtonHeld || middleMouseButtonHeld)
            {
                Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue()); // Replaced Input.mousePosition
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, 100f, softBodyLayer))
                {
                    SoftBodyComponent softBody = hit.collider.GetComponent<SoftBodyComponent>();
                    if (softBody != null)
                    {
                        Vector3 force = Vector3.zero;
                        
                        if (leftMouseButtonHeld) // Push // Replaced Input.GetMouseButton(0)
                        {
                            force = ray.direction * interactionForce;
                        }
                        else if (rightMouseButtonHeld) // Pull // Replaced Input.GetMouseButton(1)
                        {
                            force = -ray.direction * interactionForce;
                        }
                        else if (middleMouseButtonHeld) // Explode // Replaced Input.GetMouseButton(2)
                        {
                            // Ensure DeformationType is properly set on softBody, or use a default/parameter.
                            // Assuming softBody.Explode handles its effect.
                            softBody.Explode(hit.point, interactionForce * 5, interactionRadius * 2);
                            return; // Return after explode, as in original logic
                        }
                        
                        // Apply deformation (only if not exploded)
                        DeformationData deformation = new DeformationData
                        {
                            force = force,
                            position = hit.point,
                            intensity = 1.0f,
                            type = softBody.DeformationType // Ensure softBody.DeformationType is valid
                        };
                        
                        softBody.ApplyDeformation(deformation);
                    }
                }
            }
        }
        
        void HandleKeyboardInput()
        {
            // Guard clause if no keyboard is present
            if (Keyboard.current == null) return;

            // Spawn new objects
            if (Keyboard.current.digit1Key.wasPressedThisFrame) // Replaced Input.GetKeyDown(KeyCode.Alpha1)
            {
                SpawnNewSoftBody(SoftBodyComponent.ShapeType.Sphere, rubberMaterial);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) // Replaced Input.GetKeyDown(KeyCode.Alpha2)
            {
                SpawnNewSoftBody(SoftBodyComponent.ShapeType.Cube, jellyMaterial);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) // Replaced Input.GetKeyDown(KeyCode.Alpha3)
            {
                SpawnNewSoftBody(SoftBodyComponent.ShapeType.Cylinder, glassMaterial);
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame) // Replaced Input.GetKeyDown(KeyCode.Alpha4)
            {
                SpawnNewSoftBody(SoftBodyComponent.ShapeType.Torus, clothMaterial);
            }
            
            // Reset all soft bodies
            if (Keyboard.current.rKey.wasPressedThisFrame) // Replaced Input.GetKeyDown(KeyCode.R)
            {
                SoftBodyComponent[] allSoftBodies = FindObjectsOfType<SoftBodyComponent>();
                foreach (var softBody in allSoftBodies)
                {
                    softBody.ResetDeformation();
                }
            }
            
            // Clear scene
            if (Keyboard.current.cKey.wasPressedThisFrame) // Replaced Input.GetKeyDown(KeyCode.C)
            {
                SoftBodyComponent[] allSoftBodies = FindObjectsOfType<SoftBodyComponent>();
                foreach (var softBody in allSoftBodies)
                {
                    Destroy(softBody.gameObject);
                }
            }
        }
        
        void SpawnNewSoftBody(SoftBodyComponent.ShapeType shape, PhysicsMaterial material)
        {
            if (mainCamera == null) return; // Should not happen if Start() check passed

            Vector3 spawnPos = mainCamera.transform.position + mainCamera.transform.forward * 5f;
            GameObject newObj = SpawnSoftBody(shape, material, spawnPos, $"{shape} ({material.materialName})");
            
            // Give it some initial velocity
            if (newObj.TryGetComponent<SoftBodyComponent>(out var softBody))
            {
                // Assuming this overload exists: ApplyDeformation(Vector3 force, Vector3 position)
                // Or, more consistently:
                // DeformationData initialForce = new DeformationData {
                //     force = mainCamera.transform.forward * spawnForce,
                //     position = spawnPos, // or newObj.transform.position
                //     intensity = 1.0f,
                //     type = material.deformationType // Assuming softBody is configured with this material
                // };
                // softBody.ApplyDeformation(initialForce);
                // For now, keeping the original call signature assuming it's valid.
                softBody.ApplyDeformation(mainCamera.transform.forward * spawnForce, spawnPos);
            }
        }
        
        void OnGUI()
        {
            // Instructions
            GUILayout.BeginArea(new Rect(10, 200, 400, 300));
            GUILayout.Box("Soft Body Demo Controls:\n\n" +
                         "Mouse:\n" +
                         "- Left Click: Push object\n" +
                         "- Right Click: Pull object\n" +
                         "- Middle Click: Explode at point\n\n" +
                         "Keyboard:\n" +
                         "- 1: Spawn rubber sphere\n" +
                         "- 2: Spawn jelly cube\n" +
                         "- 3: Spawn glass cylinder\n" +
                         "- 4: Spawn cloth torus\n" +
                         "- R: Reset all deformations\n" +
                         "- C: Clear all objects");
            GUILayout.EndArea();
        }
    }
}