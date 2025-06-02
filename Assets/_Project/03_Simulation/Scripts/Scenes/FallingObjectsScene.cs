using UnityEngine;
using _Project._01_Physics.Scripts.Deformation.MassSpring;

namespace _Project._03_Simulation.Scripts.Scenes
{
    /// <summary>
    /// Simple scene demonstrating a soft cube and ball falling onto the ground.
    /// No input required - just add this script to an empty GameObject and press play!
    /// </summary>
    public class FallingObjectsScene : MonoBehaviour
    {
        [Header("Scene Setup")]
        [SerializeField] private bool autoSetup = true;
        
        void Start()
        {
            if (autoSetup)
            {
                SetupCompleteScene();
            }
        }
        
        void SetupCompleteScene()
        {
            // 1. Setup Camera
            SetupCamera();
            
            // 2. Setup Lighting
            SetupLighting();
            
            // 3. Create Ground
            CreateGround();
            
            // 4. Create Falling Objects
            CreateFallingObjects();
            
            // 5. Configure Time
            Time.fixedDeltaTime = 0.02f; // 50 Hz physics
            
            Debug.Log("Scene setup complete! Watch the soft bodies fall and deform!");
        }
        
        void SetupCamera()
        {
            // Check if camera exists
            GameObject cameraObj = Camera.main?.gameObject;
            if (cameraObj == null)
            {
                cameraObj = new GameObject("Main Camera");
                cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();
                cameraObj.tag = "MainCamera";
            }
            
            // Position camera
            cameraObj.transform.position = new Vector3(5, 8, -10);
            cameraObj.transform.LookAt(new Vector3(0, 2, 0));
            
            // Set background
            Camera.main.backgroundColor = new Color(0.3f, 0.4f, 0.5f);
            Camera.main.fieldOfView = 60;
        }
        
        void SetupLighting()
        {
            // Create directional light if it doesn't exist
            Light[] lights = FindObjectsOfType<Light>();
            Light directionalLight = null;
            
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }
            
            if (directionalLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                directionalLight = lightObj.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }
            
            // Configure light
            directionalLight.transform.rotation = Quaternion.Euler(45, -30, 0);
            directionalLight.intensity = 1.0f;
            directionalLight.color = Color.white;
            directionalLight.shadows = LightShadows.Soft;
            
            // Setup ambient
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.5f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.3f, 0.2f);
        }
        
        void CreateGround()
        {
            // Create ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(15, 0.5f, 15);
            ground.tag = "Ground";
            
            // Create checkerboard material
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (groundMat.shader == null) // Fallback for built-in pipeline
            {
                groundMat = new Material(Shader.Find("Standard"));
            }
            groundMat.color = new Color(0.8f, 0.8f, 0.8f);
            ground.GetComponent<Renderer>().material = groundMat;
            
            // Make static
            ground.isStatic = true;
            
            // Remove the collider since we're using custom physics
            Destroy(ground.GetComponent<Collider>());
        }
        
        void CreateFallingObjects()
        {
            // Create a rubber ball
            GameObject ball = SoftBodyFactory.Presets.CreateRubberBall(
                position: new Vector3(0, 5, 0),
                size: 1.5f
            );
            
            // Create a jelly cube
            // GameObject cube = SoftBodyFactory.Presets.CreateJellyCube(
            //     position: new Vector3(2, 8, 0),
            //     size: 1.2f
            // );
            
            // Add some initial random velocity to make it interesting
            var ballWrapper = ball.GetComponent<SoftBodyWrapper>();
            if (ballWrapper != null)
            {
                ballWrapper.ApplyDeformation(
                    Vector3.right * 5f + Vector3.forward * 3f,
                    ball.transform.position
                );
            }
            
            // var cubeWrapper = cube.GetComponent<SoftBodyWrapper>();
            // if (cubeWrapper != null)
            // {
            //     cubeWrapper.ApplyDeformation(
            //         Vector3.left * 30f + Vector3.back * 20f,
            //         cube.transform.position
            //     );
            // }
        }
        
        void OnGUI()
        {
            // Display info
            GUI.Box(new Rect(10, 10, 300, 100), 
                "Soft Body Physics Demo\n\n" +
                "Watch as different materials fall and deform!\n" +
                "Red = Rubber (bouncy)\n" +
                "Green = Jelly (wobbly)\n" +
                "Blue = Glass (brittle)\n" +
                "Yellow = Cloth (flexible)");
        }
    }
}