// Assets/_Project/03_Simulation/Scripts/Scenes/BreakableGlassTest.cs

using UnityEngine;
using _Project._00_Core.Scripts.DataStructures;
using _Project._01_Physics.Scripts.PBD_V1;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

public class BreakableGlassTest : MonoBehaviour
{
    [Header("Glass Ball Settings")]
    [SerializeField] private Vector3 ballStartPosition = new Vector3(0, 8, 0);
    [SerializeField] private float ballSize = 1.0f;
    [SerializeField] private int sphereResolution = 16; // Higher resolution for better fracture
    
    [Header("Material Settings")]
    [SerializeField] private Color glassColor = new Color(0.8f, 0.9f, 1.0f, 0.6f);
    [SerializeField] private Color fracturedGlassColor = new Color(1.0f, 0.8f, 0.8f, 0.8f);
    
    [Header("Fracture Settings")]
    [SerializeField] private float fractureThreshold = 5f;
    [SerializeField] private float impactSensitivity = 2f;
    [SerializeField] private bool enableFragments = true;
    
    [Header("Test Controls")]
    [SerializeField] private bool autoReset = false;
    [SerializeField] private float resetDelay = 10f;
    
    private PBDSoftBody glassBall;
    private Material glassMaterial;
    private Material fracturedMaterial;
    private float resetTimer = 0f;
    
    void Start()
    {
        Debug.Log("=== BREAKABLE GLASS BALL TEST ===");
        
        CreateMaterials();
        CreateGround();
        CreateBreakableGlassBall();
        SetupCamera();
        
        Debug.Log("Breakable glass test initialized! Watch the glass ball fall and shatter on impact.");
    }
    
    void CreateMaterials()
    {
        // Create glass material (transparent/translucent)
        glassMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        glassMaterial.color = glassColor;
        glassMaterial.SetFloat("_Metallic", 0.0f);
        glassMaterial.SetFloat("_Smoothness", 0.9f);
        
        // Make it transparent if supported
        if (glassMaterial.HasProperty("_Surface"))
        {
            glassMaterial.SetFloat("_Surface", 1); // Transparent
            glassMaterial.SetFloat("_Blend", 0); // Alpha blend
        }
        
        // Create fractured glass material
        fracturedMaterial = new Material(glassMaterial);
        fracturedMaterial.color = fracturedGlassColor;
        fracturedMaterial.SetFloat("_Smoothness", 0.3f); // Rougher when fractured
        
        Debug.Log("Glass materials created");
    }
    
    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "HardGround";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(20, 1, 20);
        ground.tag = "Ground";
        
        // Make ground look hard (stone/concrete)
        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.color = new Color(0.4f, 0.4f, 0.4f);
        groundMat.SetFloat("_Metallic", 0.0f);
        groundMat.SetFloat("_Smoothness", 0.1f);
        ground.GetComponent<Renderer>().material = groundMat;
        
        ground.isStatic = true;
        
        Debug.Log("Hard ground created for glass impact test");
    }
    
    void CreateBreakableGlassBall()
    {
        GameObject ball = new GameObject("Breakable Glass Ball");
        ball.transform.position = ballStartPosition;
        
        var meshFilter = ball.AddComponent<MeshFilter>();
        var meshRenderer = ball.AddComponent<MeshRenderer>();
        
        // Create high-resolution sphere for better fracture visualization
        meshFilter.mesh = CreateHighQualitySphereMesh(ballSize * 0.5f, sphereResolution);
        meshRenderer.material = glassMaterial;
        
        // Add PBD soft body with fracture enabled
        glassBall = ball.AddComponent<PBDSoftBody>();
        
        // Initialize after a frame to ensure all components are ready
        StartCoroutine(InitializeGlassBall());
    }
    
    System.Collections.IEnumerator InitializeGlassBall()
    {
        yield return null;
        
        // Create brittle glass material
        var glassMaterialData = CreateGlassMaterial();
        
        // Configure glass ball for fracture
        ConfigureGlassBallFields();
        
        // Initialize the soft body
        glassBall.Initialize(glassMaterialData);
        
        // Set fractured material for visual feedback
        glassBall.SetFracturedMaterial(fracturedMaterial);
        
        if (glassBall.Solver != null)
        {
            Debug.Log($"Glass ball initialized successfully!");
            Debug.Log($"Particles: {glassBall.Solver.Particles.Count}");
            Debug.Log($"Constraints: {glassBall.Solver.Constraints.Count}");
            Debug.Log($"Fracture enabled: {glassBall.Solver.EnableFracture}");
            Debug.Log($"Fracture threshold: {glassBall.Solver.GlobalFractureThreshold}");
        }
        else
        {
            Debug.LogError("Failed to initialize glass ball solver!");
        }
    }
    
    PhysicsMaterial CreateGlassMaterial()
    {
        var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
        material.materialName = "BrittleGlass";
        material.density = 2.5f; // Glass density
        material.restitution = 0.1f; // Low bounce - glass doesn't bounce much
        material.staticFriction = 0.6f;
        material.dynamicFriction = 0.4f;
        material.deformationType = DeformationType.Brittle;
        material.stiffness = 2000f; // High stiffness - glass is rigid
        material.damping = 50f; // High damping for stability
        material.elasticLimit = 100f; // Very low - glass breaks easily
        material.plasticLimit = 150f; // Glass doesn't deform plastically much
        material.brittleThreshold = 200f; // Breaks at relatively low force
        
        return material;
    }
    
    void ConfigureGlassBallFields()
    {
        // Use reflection to set private fields since they're not public properties
        var softBodyType = typeof(PBDSoftBody);
        
        // Material properties
        SetField("density", 2.5f);
        SetField("restitution", 0.1f);
        SetField("friction", 0.4f);
        
        // PBD solver settings - make it more rigid but breakable
        SetField("constraintIterations", 6); // More iterations for stability
        SetField("globalStiffness", 0.9f); // High stiffness
        SetField("globalDamping", 0.98f); // Good damping for stability
        
        // Fracture settings - make it break easily
        SetField("enableFracture", true);
        SetField("fractureThreshold", fractureThreshold);
        SetField("stressDecayRate", 0.8f); // Stress accumulates faster
        
        // Ground collision
        SetField("enableGroundCollision", true);
        SetField("groundY", 0.0f);
        
        // Debug
        SetField("showDebugInfo", true);
        SetField("showParticles", false);
        SetField("showBrokenConstraints", enableFragments);
        
        Debug.Log("Glass ball configured for brittle fracture behavior");
    }
    
    void SetField(string fieldName, object value)
    {
        var field = typeof(PBDSoftBody).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(glassBall, value);
        }
        else
        {
            Debug.LogWarning($"Field {fieldName} not found in PBDSoftBody");
        }
    }
    
    Mesh CreateHighQualitySphereMesh(float radius, int resolution)
    {
        // Use the PBD mesh generator for consistent results
        return PBDMeshGenerator.GenerateSphereMesh(radius, resolution, resolution);
    }
    
    void SetupCamera()
    {
        // Find or create camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
        }
        
        // Position camera to get good view of the falling ball
        mainCamera.transform.position = new Vector3(3, 4, -8);
        mainCamera.transform.LookAt(new Vector3(0, 2, 0));
        mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.2f); // Dark background
        
        Debug.Log("Camera positioned for optimal glass fracture viewing");
    }
    
    void Update()
    {
        // Manual fracture trigger for testing
        if (Input.GetKeyDown(KeyCode.F) && glassBall != null)
        {
            Debug.Log("Manual fracture triggered!");
            glassBall.TriggerFracture();
        }
        
        // Apply impulse to test impact fracture
        if (Input.GetKeyDown(KeyCode.Space) && glassBall != null && !glassBall.IsFractured)
        {
            Debug.Log("Applying impact force to glass ball");
            glassBall.ApplyDeformation(Vector3.down * 20f, glassBall.transform.position);
        }
        
        // Reset test
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTest();
        }
        
        // Auto reset if enabled
        if (autoReset && glassBall != null && glassBall.IsFractured)
        {
            resetTimer += Time.deltaTime;
            if (resetTimer >= resetDelay)
            {
                ResetTest();
            }
        }
        
        // Monitor glass ball state
        MonitorGlassBall();
    }
    
    void MonitorGlassBall()
    {
        if (glassBall == null || glassBall.Solver == null) return;
        
        // Check if glass ball has hit the ground and should fracture
        if (!glassBall.IsFractured)
        {
            // Check if any particle is moving fast and near ground
            foreach (var particle in glassBall.Solver.Particles)
            {
                if (particle.IsActive && particle.Position.y < 1f && particle.Velocity.magnitude > 5f)
                {
                    // High speed impact - add extra stress
                    float impactStress = particle.Velocity.magnitude * impactSensitivity;
                    particle.AddStress(impactStress);
                }
            }
        }
        
        // Debug output every few seconds
        if (Time.time % 3f < Time.deltaTime)
        {
            var stats = glassBall.Solver.GetStatistics();
            Debug.Log($"Glass Ball Status - Fractured: {glassBall.IsFractured}, " +
                     $"Active Particles: {stats.particles}, Broken Constraints: {stats.brokenConstraints}");
        }
    }
    
    void ResetTest()
    {
        Debug.Log("Resetting glass ball test...");
        
        if (glassBall != null)
        {
            glassBall.ResetDeformation();
            glassBall.transform.position = ballStartPosition;
            
            // Reset material
            glassBall.GetComponent<MeshRenderer>().material = glassMaterial;
        }
        
        resetTimer = 0f;
        
        Debug.Log("Glass ball test reset complete");
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 150, 400, 300));
        GUILayout.Box("BREAKABLE GLASS BALL TEST\n\n" +
                      "This test demonstrates fracture mechanics:\n" +
                      "• Glass ball falls under gravity\n" +
                      "• Impacts generate stress in particles\n" +
                      "• High stress breaks constraints\n" +
                      "• Broken constraints create visible fracture\n" +
                      "• Fractured areas turn reddish\n" +
                      "• Fragments may be generated\n\n" +
                      "Controls:\n" +
                      "• SPACE: Apply impact force\n" +
                      "• F: Trigger manual fracture\n" +
                      "• R: Reset test\n\n" +
                      $"Ball Status: {(glassBall?.IsFractured == true ? "FRACTURED" : "INTACT")}\n" +
                      $"Auto Reset: {autoReset} ({resetDelay}s)\n" +
                      $"Fracture Threshold: {fractureThreshold}\n" +
                      $"Impact Sensitivity: {impactSensitivity}");
        GUILayout.EndArea();
    }
}