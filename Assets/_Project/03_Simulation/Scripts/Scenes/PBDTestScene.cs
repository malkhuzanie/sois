// Assets/_Project/03_Simulation/Scripts/Scenes/PBDTestScene.cs

using UnityEngine;
using UnityEngine.InputSystem;
using _Project._01_Physics.Scripts.PBD;

public class PBDTestScene : MonoBehaviour
{
    [Header("Test Settings")] 
    [SerializeField] private Vector3 ballStartPosition = new Vector3(0, 5, 0);
    [SerializeField] private float ballSize = 1.5f;
    [SerializeField] private int sphereResolution = 12;
    [SerializeField] private bool createGround = true;

    private PBDSoftBody softBody;

    void Start()
    {
        Debug.Log("=== FIXED PBD SOFT BODY TEST ===");

        if (createGround)
            CreateGround();

        CreateFixedPBDSoftBody();

        Time.fixedDeltaTime = 0.02f;

        Debug.Log("Fixed PBD test initialized - should have proper sphere shape!");
    }

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(30, 1, 30);
        ground.tag = "Ground";

        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.color = new Color(0.8f, 0.8f, 0.8f);
        ground.GetComponent<Renderer>().material = groundMat;

        ground.isStatic = true;

        Debug.Log("Ground created for fixed PBD test");
    }

    void CreateFixedPBDSoftBody()
    {
        // Create empty GameObject
        GameObject sphere = new GameObject("Rubber Ball - High Bounce");
        sphere.transform.position = ballStartPosition;
        sphere.transform.localScale = Vector3.one * ballSize;

        // Add required components
        sphere.AddComponent<MeshFilter>();

        var renderer = sphere.AddComponent<MeshRenderer>();
        Material sphereMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        sphereMat.color = Color.red; // Red for rubber
        renderer.material = sphereMat;

        // Add PBD component with SUPER BOUNCY settings
        softBody = sphere.AddComponent<PBDSoftBody>();

        // Configure for MAXIMUM bounce
        SetPBDField("useCustomMesh", true);
        SetPBDField("sphereResolution", sphereResolution);
        SetPBDField("sphereRadius", 0.5f);
        SetPBDField("constraintIterations", 3); // Fewer iterations for more flexibility
        SetPBDField("globalStiffness", 0.4f); // Lower stiffness for more deformation
        SetPBDField("globalDamping", 0.999f); // VERY high damping value (less energy loss)
        SetPBDField("enableGroundCollision", true);
        SetPBDField("groundY", 0.0f);
        SetPBDField("restitution", 0.95f); // VERY high bounce
        SetPBDField("friction", 0.3f); // Lower friction
        SetPBDField("showDebugInfo", true);

        // Use the super bouncy rubber material
        var superBouncyMaterial = PBDMaterialPresets.CreateSuperBouncyRubber();

        // Initialize with super bouncy material
        softBody.Initialize(superBouncyMaterial);
        softBody.SetRubberBehavior(2.0f); // 2x bounce multiplier

        Debug.Log($"SUPER BOUNCY rubber ball created");

        if (softBody.Solver != null)
        {
            // Adjust solver settings for MAXIMUM bounce
            softBody.Solver.GlobalStiffness = 0.4f;
            softBody.Solver.GlobalDamping = 0.999f; // Minimize energy loss
            softBody.Solver.Gravity = new Vector3(0, -9.81f, 0);

            var stats = softBody.Solver.GetStatistics();
            Debug.Log($"Super Bouncy Ball Statistics: {stats.particles} particles, {stats.constraints} constraints");
        }
    }

    void SetPBDField(string fieldName, object value)
    {
        var field = typeof(PBDSoftBody).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(softBody, value);
    }

    void Update()
    {
        // NEW INPUT SYSTEM - Use Keyboard class instead of Input
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Apply upward force
        if (keyboard.fKey.wasPressedThisFrame && softBody != null)
        {
            softBody.ApplyDeformation(Vector3.up * 50f, softBody.transform.position);
            Debug.Log("Applied upward force to PBD soft body");
        }

        // Status check
        if (keyboard.spaceKey.wasPressedThisFrame && softBody?.Solver != null)
        {
            var stats = softBody.Solver.GetStatistics();
            bool isValid = softBody.Solver.ValidateState();

            Debug.Log("=== PBD STATUS CHECK ===");
            Debug.Log($"Solver Valid: {isValid}");
            Debug.Log($"Particles: {stats.particles}");
            Debug.Log($"Constraints: {stats.constraints}");
            Debug.Log($"Solve Time: {stats.solveTime * 1000f:F2}ms");
        }

        // Reset test
        if (keyboard.rKey.wasPressedThisFrame)
        {
            if (softBody != null)
            {
                DestroyImmediate(softBody.gameObject);
            }

            CreateFixedPBDSoftBody();
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 250, 450, 200));
        GUILayout.Box("SUPER BOUNCY PBD Test\n\n" +
                      "ENHANCED SETTINGS:\n" +
                      "✓ Restitution: 0.95 (very high bounce)\n" +
                      "✓ Damping: 0.999 (minimal energy loss)\n" +
                      "✓ Lower stiffness for more deformation\n" +
                      "✓ Super bouncy material preset\n" +
                      "✓ Enhanced ground collision\n\n" +
                      "Controls:\n" +
                      "• SPACE: Check status\n" +
                      "• F: Apply upward force\n" +
                      "• R: Reset test\n\n" +
                      "Ball should bounce MUCH higher now!");
        GUILayout.EndArea();
    }
}