// Assets/_Project/03_Simulation/Scripts/BallBehaviorTest.cs

using _Project._00_Core.Scripts.DataStructures;
using _Project._01_Physics.Scripts.Deformation.MassSpring;
using UnityEngine;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

public class BallBehaviorTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool createTestBall = true;
    [SerializeField] private bool createGround = true;
    [SerializeField] private Vector3 ballStartPosition = new Vector3(0, 5, 0);
    [SerializeField] private float ballSize = 1.5f;
    [SerializeField] private int ballResolution = 8;
    
    private SoftBodyWrapper ballWrapper;
    
    void Start()
    {
        Debug.Log("=== BALL BEHAVIOR TEST ===");
        
        // Create ground first
        if (createGround)
            CreateGround();
        
        if (createTestBall)
            CreateTestBall();
        
        // Reasonable physics timestep
        Time.fixedDeltaTime = 0.02f; // 50Hz physics
        
        Debug.Log("Ball behavior test initialized - the ball should maintain its spherical shape while falling and bouncing");
    }
    
    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(20, 1, 20);
        ground.tag = "Ground";
        
        Material groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.7f, 0.7f, 0.7f);
        ground.GetComponent<Renderer>().material = groundMat;
        
        ground.isStatic = true;
        
        Debug.Log("Ground created at Y = 0");
    }
    
    void CreateTestBall()
    {
        // Create ball using the improved factory with higher resolution and stronger springs
        var config = SoftBodyFactory.SoftBodyConfig.Default;
        config.name = "Test Ball (No Internal Structure)";
        config.position = ballStartPosition;
        config.size = ballSize;
        config.resolution = ballResolution; // Use the ballResolution from inspector
        config.color = Color.red;
        config.physicsMaterial = CreateStrongRubberMaterial(); // Use custom strong material
        
        GameObject ball = SoftBodyFactory.CreateSoftBody(config);
        ballWrapper = ball.GetComponent<SoftBodyWrapper>();
        
        // DISABLE COHESION FOR TESTING
        if (ballWrapper != null)
        {
            var cohesionField = typeof(SoftBodyWrapper).GetField("maintainCohesion", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cohesionField?.SetValue(ballWrapper, false);
            Debug.Log("Cohesion DISABLED for testing");
        }
        
        if (ballWrapper != null && ballWrapper.System != null)
        {
            var stats = ballWrapper.System.GetStatistics();
            Debug.Log($"Test ball created: {ballWrapper.System.MassPoints.Count} points, {stats.totalSprings} springs");
            Debug.Log($"System gravity: {ballWrapper.System.Gravity}");
            Debug.Log($"System damping: {ballWrapper.System.GlobalDamping}");
            
            // Check for fixed points
            int fixedCount = 0;
            foreach (var point in ballWrapper.System.MassPoints)
            {
                if (point.IsFixed) fixedCount++;
            }
            Debug.Log($"Fixed points: {fixedCount} (should be 0)");
            
            // Log mesh bounds
            var mesh = ball.GetComponent<MeshFilter>().mesh;
            if (mesh != null)
            {
                Debug.Log($"Initial mesh bounds: center={mesh.bounds.center}, size={mesh.bounds.size}");
            }
        }
        else
        {
            Debug.LogError("Failed to create test ball or get wrapper component!");
        }
        
        // Add diagnostic component
        ball.AddComponent<PhysicsDiagnostic>();
    }
    
    private PhysicsMaterial CreateStrongRubberMaterial()
    {
        var mat = ScriptableObject.CreateInstance<PhysicsMaterial>();
        mat.materialName = "StrongRubber";
        mat.density = 1.2f;
        mat.restitution = 0.6f;
        mat.staticFriction = 0.9f;
        mat.dynamicFriction = 0.8f;
        mat.deformationType = DeformationType.Elastic;
        mat.stiffness = 4000f;  // Very high stiffness to maintain shape
        mat.damping = 35f;      // Good damping for stability
        mat.elasticLimit = 15000f;
        return mat;
    }
    
    void Update()
    {
        // Check ball integrity
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckBallIntegrity();
        }
        
        // Add some force with F key
        if (Input.GetKeyDown(KeyCode.F) && ballWrapper != null)
        {
            ballWrapper.ApplyDeformation(Vector3.up * 10f, ballWrapper.transform.position);
            Debug.Log("Applied upward force to ball");
        }
        
        // Manual gravity test with G key
        if (Input.GetKeyDown(KeyCode.G) && ballWrapper?.System != null)
        {
            Debug.Log("MANUAL GRAVITY TEST - Applying strong downward force");
            foreach (var point in ballWrapper.System.MassPoints)
            {
                if (!point.IsFixed)
                {
                    point.AddForce(Vector3.down * 98.1f); // 10x normal gravity
                }
            }
        }
        
        // Check if ball is actually moving
        if (Input.GetKeyDown(KeyCode.M) && ballWrapper?.System != null)
        {
            Vector3 totalVel = Vector3.zero;
            Vector3 totalForce = Vector3.zero;
            Vector3 avgPos = Vector3.zero;
            
            foreach (var point in ballWrapper.System.MassPoints)
            {
                totalVel += point.Velocity;
                totalForce += point.Force;
                avgPos += point.Position;
            }
            
            avgPos /= ballWrapper.System.MassPoints.Count;
            
            Debug.Log($"=== MOVEMENT CHECK ===");
            Debug.Log($"Average local position: {avgPos}");
            Debug.Log($"World position: {ballWrapper.transform.position}");
            Debug.Log($"Total velocity magnitude: {totalVel.magnitude}");
            Debug.Log($"Total force magnitude: {totalForce.magnitude}");
            Debug.Log($"Expected gravity force: {ballWrapper.System.Gravity * ballWrapper.System.MassPoints.Count}");
            
            if (totalVel.magnitude < 0.1f && totalForce.magnitude < 1f)
            {
                Debug.LogError("BALL IS STUCK! No movement or forces detected.");
            }
            else
            {
                Debug.Log("Ball has movement/forces - system is working!");
            }
        }
        
        // Reset test with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTest();
        }
    }
    
    void CheckBallIntegrity()
    {
        if (ballWrapper != null && ballWrapper.System != null)
        {
            var stats = ballWrapper.System.GetStatistics();
            Debug.Log($"Ball integrity check:");
            Debug.Log($"- Springs: {stats.totalSprings} total, {stats.brokenSprings} broken");
            Debug.Log($"- Average stress: {stats.averageStress:F3}");
            
            var mesh = ballWrapper.GetComponent<MeshFilter>().mesh;
            if (mesh != null)
            {
                Debug.Log($"- Mesh bounds: center={mesh.bounds.center:F2}, size={mesh.bounds.size:F2}");
                
                // Check if mesh has collapsed (bounds too small)
                if (mesh.bounds.size.magnitude < 0.5f)
                {
                    Debug.LogError("BALL HAS COLLAPSED! Mesh bounds are too small.");
                }
                else
                {
                    Debug.Log("Ball shape integrity looks good.");
                }
            }
        }
    }
    
    void ResetTest()
    {
        Debug.Log("Resetting test...");
        
        // Destroy existing ball
        if (ballWrapper != null)
        {
            DestroyImmediate(ballWrapper.gameObject);
        }
        
        // Create new test ball
        CreateTestBall();
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 280));
        GUILayout.Box("Debug Ball Behavior Test\n\n" +
                     "Testing with simplified system:\n" +
                     "• NO internal structure\n" +
                     "• NO cohesion forces\n" +
                     "• ONLY surface springs\n" +
                     "• Should definitely fall!\n\n" +
                     "If ball still doesn't fall, the issue is in:\n" +
                     "• Basic gravity application\n" +
                     "• Mass point physics\n" +
                     "• Force integration\n\n" +
                     "Controls:\n" +
                     "• SPACE: Check ball integrity\n" +
                     "• F: Apply upward force\n" +
                     "• G: Apply manual gravity (10x)\n" +
                     "• M: Check movement status\n" +
                     "• R: Reset test\n\n" +
                     "Watch console for diagnostic output!");
        GUILayout.EndArea();
    }
}