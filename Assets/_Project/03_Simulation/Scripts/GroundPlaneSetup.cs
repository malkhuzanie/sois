using UnityEngine;

namespace _Project._03_Simulation.Scripts
{
    /// <summary>
    /// Fixed ground plane setup with proper materials
    /// </summary>
    public class GroundPlaneSetup : MonoBehaviour
    {
        [Header("Ground Settings")]
        [SerializeField] private Vector3 groundSize = new(20f, 1f, 20f);
        [SerializeField] private Vector3 groundPosition = new(0f, -2f, 0f);
        [SerializeField] private Material groundMaterial;
        [SerializeField] private bool createMaterialAutomatically = true;
        
        [Header("Control")]
        [SerializeField] private bool preventDuplicates = true;
        
        void Start()
        {
            if (preventDuplicates && GameObject.Find("Ground") != null)
            {
                Debug.Log("Ground already exists, skipping creation");
                return;
            }
            
            CreateGroundPlane();
        }
        
        void CreateGroundPlane()
        {
            // Create ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = groundPosition;
            ground.transform.localScale = groundSize;
            
            // Apply material
            Renderer renderer = ground.GetComponent<Renderer>();
            if (groundMaterial != null)
            {
                renderer.material = groundMaterial;
            }
            else if (createMaterialAutomatically)
            {
                Material defaultMaterial = CreateDefaultGroundMaterial();
                renderer.material = defaultMaterial;
                
                // Save the material to avoid pink rendering
                #if UNITY_EDITOR
                SaveMaterialAsAsset(defaultMaterial);
                #endif
            }
            
            // Ensure it has a proper collider
            SetupGroundCollider(ground);
            
            Debug.Log("Ground plane created successfully with proper material");
        }
        
        void SetupGroundCollider(GameObject ground)
        {
            MeshCollider collider = ground.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = ground.AddComponent<MeshCollider>();
            }
            
            collider.convex = false;
            collider.isTrigger = false;
        }
        
        Material CreateDefaultGroundMaterial()
        {
            Material material = null;
            
            // Try different shaders in order of preference
            string[] shaderNames = {
                "Universal Render Pipeline/Lit",
                "Standard",
                "Legacy Shaders/Diffuse",
                "Sprites/Default" // Last resort
            };
            
            foreach (string shaderName in shaderNames)
            {
                var shader = Shader.Find(shaderName);
                if (shader != null && shader.name != "Hidden/InternalErrorShader")
                {
                    material = new Material(shader);
                    Debug.Log($"Using shader: {shaderName}");
                    break;
                }
            }
            
            // If all else fails, create with built-in shader
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                Debug.LogWarning("Could not find suitable shader, using Standard as fallback");
            }
            
            // Configure material properties
            material.name = "GroundMaterial_Generated";
            material.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            
            // Set shader-specific properties safely
            SetMaterialProperty(material, "_BaseColor", new Color(0.7f, 0.7f, 0.7f, 1f));
            SetMaterialProperty(material, "_Color", new Color(0.7f, 0.7f, 0.7f, 1f));
            SetMaterialProperty(material, "_Metallic", 0f);
            SetMaterialProperty(material, "_Smoothness", 0.3f);
            SetMaterialProperty(material, "_Glossiness", 0.3f);
            
            return material;
        }
        
        void SetMaterialProperty(Material material, string propertyName, object value)
        {
            if (material.HasProperty(propertyName))
            {
                if (value is Color color)
                    material.SetColor(propertyName, color);
                else if (value is float floatVal)
                    material.SetFloat(propertyName, floatVal);
            }
        }
        
        #if UNITY_EDITOR
        void SaveMaterialAsAsset(Material material)
        {
            string materialPath = "Assets/_Project/Materials/";
            if (!System.IO.Directory.Exists(materialPath))
            {
                System.IO.Directory.CreateDirectory(materialPath);
            }
            
            try
            {
                UnityEditor.AssetDatabase.CreateAsset(material, materialPath + "GroundMaterial.mat");
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"Material saved to: {materialPath}GroundMaterial.mat");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not save material as asset: {e.Message}");
            }
        }
        #endif
        
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green * 0.5f;
            Gizmos.DrawCube(groundPosition, groundSize);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundPosition, groundSize);
        }
        
        [ContextMenu("Recreate Ground")]
        public void RecreateGround()
        {
            GameObject existingGround = GameObject.Find("Ground");
            if (existingGround != null)
            {
                DestroyImmediate(existingGround);
            }
            CreateGroundPlane();
        }
    }
}