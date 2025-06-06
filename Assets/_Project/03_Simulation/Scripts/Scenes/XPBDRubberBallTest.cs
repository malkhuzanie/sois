// Assets/_Project/03_Simulation/Scripts/Scenes/XPBDRubberBallTest.cs

using UnityEngine;
using _Project._01_Physics.Scripts.XPBD.Components;
using _Project._01_Physics.Scripts.XPBD.Materials;

namespace _Project._03_Simulation.Scripts.Scenes
{
    /// <summary>
    /// Test scene for XPBD rubber ball with proper elastic behavior
    /// Validates time-step independent elastic simulation
    /// </summary>
    public class XPBDRubberBallTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool createTestBalls = true;
        [SerializeField] private bool createGround = true;
        [SerializeField] private Vector3 ballSpawnPosition = new Vector3(0, 5, 0);
        [SerializeField] private float ballRadius = 0.5f;
        [SerializeField] private int meshSubdivisions = 2;
        
        [Header("Multiple Ball Test")]
        [SerializeField] private bool createMultipleBalls = false;
        [SerializeField] private int numberOfBalls = 3;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(2, 2, 2);
        
        private XPBDRubberBall[] testBalls;
        
        void Start()
        {
            Debug.Log("=== XPBD RUBBER BALL TEST ===");
            
            if (createGround)
                CreateGround();
            
            if (createTestBalls)
            {
                if (createMultipleBalls)
                    CreateMultipleBalls();
                else
                    CreateSingleBall();
            }
            
            // Ensure good physics timestep
            Time.fixedDeltaTime = 0.02f; // 50Hz
            
            Debug.Log("XPBD rubber ball test initialized - observe elastic bouncing behavior!");
        }
        
        void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(20, 1, 20);
            ground.tag = "Ground";
            
            // Create realistic ground material
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.6f, 0.6f, 0.6f);
            groundMat.SetFloat("_Metallic", 0.0f);
            groundMat.SetFloat("_Smoothness", 0.3f);
            ground.GetComponent<Renderer>().material = groundMat;
            
            ground.isStatic = true;
            
            Debug.Log("Ground created for XPBD test");
        }
        
        void CreateSingleBall()
        {
            GameObject ballObj = new GameObject("XPBD Rubber Ball");
            ballObj.transform.position = ballSpawnPosition;
            
            // Add mesh components
            ballObj.AddComponent<MeshFilter>();
            ballObj.AddComponent<MeshRenderer>();
            
            // Create rubber ball material
            Material ballMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ballMaterial.color = Color.red;
            ballMaterial.SetFloat("_Metallic", 0.0f);
            ballMaterial.SetFloat("_Smoothness", 0.8f);
            ballObj.GetComponent<MeshRenderer>().material = ballMaterial;
            
            // Add XPBD rubber ball component
            var rubberBall = ballObj.AddComponent<XPBDRubberBall>();
            
            // Configure via reflection to set private fields
            SetPrivateField(rubberBall, "radius", ballRadius);
            SetPrivateField(rubberBall, "meshSubdivisions", meshSubdivisions);
            SetPrivateField(rubberBall, "material", ElasticMaterial.CreateRubberMaterial());
            SetPrivateField(rubberBall, "showDebugInfo", true);
            
            testBalls = new XPBDRubberBall[] { rubberBall };
            
            Debug.Log("Single XPBD rubber ball created");
        }
        
        void CreateMultipleBalls()
        {
            // testBalls = new XPBDRubberBall[numberOfBalls];
            // 
            // for (int i = 0; i < numberOfBalls; i++)
            // {
            //     Vector3 position = ballSpawnPosition + new Vector3(
            //         Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
            //         Random.Range(0, spawnAreaSize.y),
            //         Random.Range(-spawnAreaSize.z * 0.5f, spawnAreaSize.z * 0.5f)
            //     );
            //     
            //     GameObject ballObj = new GameObject($"XPBD Rubber Ball {i + 1}");
            //     ballObj.transform.position = position;
            //     
            //     // Add mesh components
            //     ballObj.AddComponent<MeshFilter>();
            //     ballObj.AddComponent<MeshRenderer>();
            //     
            //     // Create varied ball materials
            //     Material ballMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            //     ballMaterial.color = new Color(
            //         Random.Range(0.3f, 1.0f),
            //         Random.Range(0.3f, 1.0f),
            //         Random.Range(0.3f, 1.0f)
            //     );
            //     ballMaterial.SetFloat("_Metallic", 0.0f);
            //     ballMaterial.SetFloat("_Smoothness", 0.8f);
            //     ballObj.GetComponent<MeshRenderer>().material = ballMaterial;
            //     
            //     // Add XPBD rubber ball component
            //     var rubberBall = ballObj.AddComponent<XPBDRubberBall>();
            //     
            //     // Configure with varied properties
            //     ElasticMaterial material;
            //     switch (i % 3)
            //     {
            //         case 0: material = ElasticMaterial.CreateRubberMaterial(); break;
            //         // case 1: material = ElasticMaterial.CreateSoftRubberMaterial(); break;
            //         // default: material = ElasticMaterial.CreateFirmRubberMaterial(); break;
            //     }
            //     
            //     SetPrivateField(rubberBall, "radius", ballRadius * Random.Range(0.8f, 1.2f));
            //     SetPrivateField(rubberBall, "meshSubdivisions", meshSubdivisions);
            //     SetPrivateField(rubberBall, "material", material);
            //     SetPrivateField(rubberBall, "showDebugInfo", i == 0); // Only show debug for first ball
            //     
            //     testBalls[i] = rubberBall;
            // }
            
            // Debug.Log($"Created {numberOfBalls} XPBD rubber balls with varied properties");
        }
        
        void Update()
        {
            // Apply test forces
            if (Input.GetKeyDown(KeyCode.Space) && testBalls != null)
            {
                foreach (var ball in testBalls)
                {
                    if (ball != null)
                    {
                        // Apply STRONG downward force to see deformation
                        Vector3 impulse = Vector3.down * 25f; // Strong impact
                        ball.ApplyImpulse(impulse, ball.transform.position + Vector3.up * 0.2f);
                    }
                }
                Debug.Log("Applied STRONG downward impulse to test deformation");
            }
    
            // Apply upward force
            if (Input.GetKeyDown(KeyCode.U) && testBalls != null)
            {
                foreach (var ball in testBalls)
                {
                    if (ball != null)
                    {
                        Vector3 impulse = Vector3.up * 15f;
                        ball.ApplyImpulse(impulse, ball.transform.position);
                    }
                }
                Debug.Log("Applied upward impulse");
            }
    
            // Reset balls
            if (Input.GetKeyDown(KeyCode.R) && testBalls != null)
            {
                foreach (var ball in testBalls)
                {
                    if (ball != null)
                    {
                        ball.ResetBall();
                    }
                }
                Debug.Log("Reset all test balls");
            }
        }        
        
        void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
        
        T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (T)field.GetValue(obj) : default(T);
        }
        
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 200, 400, 200));
            GUILayout.Box("XPBD Rubber Ball Test - DEFORMATION FOCUS\n\n" +
                          "Now with MUCH softer material for visible deformation:\n" +
                          "✓ Good friction (ball stops sliding) ✅\n" +
                          "✓ Softer constraints (should deform on impact)\n" +
                          "✓ Deformation monitoring\n\n" +
                          "Controls:\n" +
                          "• SPACE: Apply STRONG downward force (test deformation)\n" +
                          "• U: Apply upward force\n" +
                          "• R: Reset ball positions\n\n" +
                          $"Active Balls: {(testBalls?.Length ?? 0)}\n" +
                          "Watch the Deformation Monitor panel!");
            GUILayout.EndArea();
        }
    }
}