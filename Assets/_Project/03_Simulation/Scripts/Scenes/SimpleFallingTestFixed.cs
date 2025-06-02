// Assets/_Project/03_Simulation/Scripts/SimpleFallingTestFixed.cs
using UnityEngine;
using _Project._01_Physics.Scripts.Deformation.MassSpring;
using _Project._00_Core.Scripts.DataStructures;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

public class SimpleFallingTestFixed : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool createTestBall = true;
    [SerializeField] private bool createGround = true;
    [SerializeField] private Vector3 ballStartPosition = new Vector3(0, 5, 0);
    [SerializeField] private float ballSize = 1.5f;
    
    void Start()
    {
        Debug.Log("=== ULTRA-STABLE FALLING TEST ===");
        
        // Create ground first
        CreateGround();
        
        if (createTestBall)
        {
            CreateUltraStableBall();
        }
        
        // Make sure physics timestep is conservative
        Time.fixedDeltaTime = 0.02f; // 50Hz physics for better stability
        
        Debug.Log("Ultra-stable test initialized - the ball should fall naturally without premature deformation");
    }
    
    void CreateGround()
    {
        // Create ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(20, 1, 20);
        ground.tag = "Ground";
        
        // Create ground material
        Material groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.7f, 0.7f, 0.7f);
        ground.GetComponent<Renderer>().material = groundMat;
        
        // Make static
        ground.isStatic = true;
        
        Debug.Log("Ground created at Y = 0");
    }
    
    void CreateUltraStableBall()
    {
        // Create an ultra-stable rubber ball using the factory
        GameObject ball = SoftBodyFactory.Presets.CreateRubberBall(ballStartPosition, ballSize);
        
        if (ball != null)
        {
            Debug.Log($"Created ultra-stable ball at {ballStartPosition} with size {ballSize}");
            
            // Get the wrapper component to check its settings
            var wrapper = ball.GetComponent<SoftBodyWrapper>();
            if (wrapper != null && wrapper.System != null)
            {
                var stats = wrapper.System.GetStatistics();
                Debug.Log($"Ball stats: {wrapper.System.MassPoints.Count} points, {stats.totalSprings} springs");
                Debug.Log($"System gravity: {wrapper.System.Gravity}");
                Debug.Log($"System damping: {wrapper.System.GlobalDamping}");
                
                // Log initial mesh bounds to check for collapse
                var mesh = wrapper.GetComponent<MeshFilter>().mesh;
                if (mesh != null)
                {
                    Debug.Log($"Initial mesh bounds: center={mesh.bounds.center}, size={mesh.bounds.size}");
                }
            }
        }
        else
        {
            Debug.LogError("Failed to create ultra-stable ball!");
        }
    }
    
    void Update()
    {
        // Monitor mesh bounds to detect collapse
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var softBodies = FindObjectsOfType<SoftBodyWrapper>();
            Debug.Log($"Found {softBodies.Length} soft bodies in scene");
            
            foreach (var body in softBodies)
            {
                if (body.System != null)
                {
                    var stats = body.System.GetStatistics();
                    Debug.Log($"{body.name}: {stats.totalSprings} springs, {stats.brokenSprings} broken, avg stress: {stats.averageStress:F3}");
                    
                    var mesh = body.GetComponent<MeshFilter>().mesh;
                    if (mesh != null)
                    {
                        Debug.Log($"Mesh bounds: center={mesh.bounds.center}, size={mesh.bounds.size}");
                    }
                }
            }
        }
        
        // Reset test with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Resetting test...");
            
            // Destroy existing soft bodies
            var existingBodies = FindObjectsOfType<SoftBodyWrapper>();
            foreach (var body in existingBodies)
            {
                DestroyImmediate(body.gameObject);
            }
            
            // Create new test ball
            CreateUltraStableBall();
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Box("Ultra-Stable Soft Body Test\n\n" +
                     "The ball should:\n" +
                     "• Fall naturally under reduced gravity\n" +
                     "• Maintain its shape during fall\n" +
                     "• Deform only when hitting the ground\n" +
                     "• No 'High acceleration' warnings\n" +
                     "• Mesh bounds should NOT be (0,0,0)\n\n" +
                     "Controls:\n" +
                     "• SPACE: Show debug info\n" +
                     "• R: Reset test");
        GUILayout.EndArea();
    }
}