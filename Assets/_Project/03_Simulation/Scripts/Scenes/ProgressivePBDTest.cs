// Assets/_Project/03_Simulation/Scripts/ProgressivePBDTest.cs

using UnityEngine;
using _Project._01_Physics.Scripts.PBD;

public class ProgressivePBDTest : MonoBehaviour
{
    [Header("Progressive Test Settings")]
    [SerializeField] private TestLevel currentLevel = TestLevel.Basic;
    [SerializeField] private Vector3 ballStartPosition = new Vector3(0, 5, 0);
    [SerializeField] private float ballSize = 1.5f;
    
    private PBDSoftBody softBody;
    
    public enum TestLevel
    {
        Basic,              // Only distance constraints
        WithGroundCollision, // Add ground collision
        WithVolumeConstraints, // Add volume preservation
        Full                // All features
    }
    
    void Start()
    {
        Debug.Log("=== PROGRESSIVE PBD TEST ===");
        Debug.Log($"Starting with level: {currentLevel}");
        
        CreateGround();
        CreatePBDSoftBodyForLevel();
        
        Time.fixedDeltaTime = 0.02f;
        
        Debug.Log("Progressive test initialized - use number keys to change levels!");
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
    }
    
    void CreatePBDSoftBodyForLevel()
    {
        // Clean up existing
        if (softBody != null)
        {
            DestroyImmediate(softBody.gameObject);
        }
        
        // Create sphere mesh
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = $"PBD Test Level {currentLevel}";
        sphere.transform.position = ballStartPosition;
        sphere.transform.localScale = Vector3.one * ballSize;
        
        DestroyImmediate(sphere.GetComponent<Collider>());
        
        // Color based on level
        Material sphereMat = new Material(Shader.Find("Standard"));
        sphereMat.color = GetColorForLevel();
        sphere.GetComponent<Renderer>().material = sphereMat;
        
        // Add PBD component
        softBody = sphere.AddComponent<PBDSoftBody>();
        
        // Configure based on level
        ConfigureForLevel();
        
        // Create physics material
        var physicsMat = CreatePhysicsMaterial();
        
        // Initialize
        softBody.Initialize(physicsMat);
        
        LogLevelInfo();
    }
    
    Color GetColorForLevel()
    {
        return currentLevel switch
        {
            TestLevel.Basic => Color.white,
            TestLevel.WithGroundCollision => Color.yellow,
            TestLevel.WithVolumeConstraints => Color.cyan,
            TestLevel.Full => Color.green,
            _ => Color.gray
        };
    }
    
    void ConfigureForLevel()
    {
        var softBodyType = typeof(PBDSoftBody);
        
        // Base settings
        SetField("constraintIterations", 5);
        SetField("globalStiffness", 0.8f);
        SetField("globalDamping", 0.99f);
        SetField("showDebugInfo", true);
        
        // Level-specific settings
        switch (currentLevel)
        {
            case TestLevel.Basic:
                SetField("enableGroundCollision", false);
                SetField("maintainVolume", false);
                Debug.Log("BASIC: Only distance constraints (should fall and deform on ground)");
                break;
                
            case TestLevel.WithGroundCollision:
                SetField("enableGroundCollision", true);
                SetField("maintainVolume", false);
                Debug.Log("GROUND COLLISION: Distance + ground constraints (should bounce)");
                break;
                
            case TestLevel.WithVolumeConstraints:
                SetField("enableGroundCollision", true);
                SetField("maintainVolume", true);
                SetField("volumeStiffness", 0.3f); // Lower stiffness to start
                Debug.Log("VOLUME: Distance + ground + volume constraints (should maintain shape)");
                break;
                
            case TestLevel.Full:
                SetField("enableGroundCollision", true);
                SetField("maintainVolume", true);
                SetField("volumeStiffness", 0.5f);
                Debug.Log("FULL: All features enabled (should be realistic soft body)");
                break;
        }
    }
    
    void SetField(string fieldName, object value)
    {
        var field = typeof(PBDSoftBody).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(softBody, value);
    }
    
    _Project._00_Core.Scripts.DataStructures.PhysicsMaterial CreatePhysicsMaterial()
    {
        var mat = ScriptableObject.CreateInstance<_Project._00_Core.Scripts.DataStructures.PhysicsMaterial>();
        mat.materialName = $"PBD_Level_{currentLevel}";
        mat.density = 1.0f;
        mat.restitution = 0.6f;
        mat.staticFriction = 0.4f;
        mat.dynamicFriction = 0.3f;
        return mat;
    }
    
    void LogLevelInfo()
    {
        if (softBody?.Solver != null)
        {
            var stats = softBody.Solver.GetStatistics();
            Debug.Log($"Level {currentLevel}: {stats.particles} particles, {stats.constraints} constraints");
        }
    }
    
    void Update()
    {
        // Level switching
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeLevel(TestLevel.Basic);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeLevel(TestLevel.WithGroundCollision);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeLevel(TestLevel.WithVolumeConstraints);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ChangeLevel(TestLevel.Full);
        }
        
        // Force application
        if (Input.GetKeyDown(KeyCode.F) && softBody != null)
        {
            softBody.ApplyDeformation(Vector3.up * 50f, softBody.transform.position);
            Debug.Log($"Applied force at level {currentLevel}");
        }
        
        // Status check
        if (Input.GetKeyDown(KeyCode.Space) && softBody?.Solver != null)
        {
            var stats = softBody.Solver.GetStatistics();
            bool isValid = softBody.Solver.ValidateState();
            
            Debug.Log($"=== LEVEL {currentLevel} STATUS ===");
            Debug.Log($"Valid: {isValid}");
            Debug.Log($"Particles: {stats.particles}");
            Debug.Log($"Constraints: {stats.constraints} ({stats.activeConstraints} active)");
            Debug.Log($"Solve Time: {stats.solveTime * 1000f:F2}ms");
            
            if (!isValid)
            {
                Debug.LogError($"Level {currentLevel} is UNSTABLE!");
            }
            else
            {
                Debug.Log($"Level {currentLevel} is working correctly!");
            }
        }
        
        // Reset current level
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log($"Resetting level {currentLevel}");
            CreatePBDSoftBodyForLevel();
        }
    }
    
    void ChangeLevel(TestLevel newLevel)
    {
        if (newLevel != currentLevel)
        {
            Debug.Log($"Changing from {currentLevel} to {newLevel}");
            currentLevel = newLevel;
            CreatePBDSoftBodyForLevel();
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 500, 400));
        GUILayout.Box($"Progressive PBD Test - Current Level: {currentLevel}\n\n" +
                     GetLevelDescription() + "\n\n" +
                     "Level Controls:\n" +
                     "• 1: Basic (distance constraints only)\n" +
                     "• 2: + Ground Collision\n" +
                     "• 3: + Volume Constraints\n" +
                     "• 4: Full Features\n\n" +
                     "Test Controls:\n" +
                     "• SPACE: Check status\n" +
                     "• F: Apply force\n" +
                     "• R: Reset current level\n\n" +
                     "Strategy:\n" +
                     "Start with level 1, ensure it works,\n" +
                     "then progressively add features.\n" +
                     "If any level explodes, that feature\n" +
                     "needs debugging!");
        GUILayout.EndArea();
    }
    
    string GetLevelDescription()
    {
        return currentLevel switch
        {
            TestLevel.Basic => "WHITE: Only distance constraints\nShould fall and flatten on ground",
            TestLevel.WithGroundCollision => "YELLOW: + Ground collision\nShould bounce off ground",
            TestLevel.WithVolumeConstraints => "CYAN: + Volume preservation\nShould maintain shape better",
            TestLevel.Full => "GREEN: All features\nShould behave like realistic soft body",
            _ => "Unknown level"
        };
    }
}