// Assets/_Project/03_Simulation/Scripts/Scenes/ComprehensiveFractureDemo.cs

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using _Project._01_Physics.Scripts.PBD_V1;

public class ComprehensiveFractureDemo : MonoBehaviour
{
    [Header("Demo Configuration")]
    [SerializeField] private bool autoSpawnObjects = true;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxObjectsInScene = 8;
    [SerializeField] private bool enableManualSpawning = true;
    
    [Header("Spawn Areas")]
    [SerializeField] private Vector3 spawnAreaCenter = new Vector3(0, 8, 0);
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(6, 2, 6);
    [SerializeField] private float minSpawnHeight = 6f;
    [SerializeField] private float maxSpawnHeight = 12f;
    
    [Header("Object Types")]
    [SerializeField] private bool spawnGlassBalls = true;
    [SerializeField] private bool spawnCrystalGlass = true;
    [SerializeField] private bool spawnCeramicVases = true;
    [SerializeField] private bool spawnIceCubes = true;
    [SerializeField] private bool spawnTemperedGlass = true;
    
    [Header("Visual Effects")]
    [SerializeField] private bool enableParticleEffects = true;
    [SerializeField] private bool enableSoundEffects = false;
    [SerializeField] private bool enableSlowMotion = false;
    [SerializeField] private float slowMotionScale = 0.3f;
    
    [Header("Demo Controls")]
    [SerializeField] private bool enableResetTimer = true;
    [SerializeField] private float resetInterval = 60f;
    
    private List<GameObject> activeObjects;
    private float lastSpawnTime;
    private float lastResetTime;
    private int spawnedObjectCount;
    private bool isSlowMotionActive = false;
    
    // Object type definitions
    private readonly string[] objectTypeNames = { "Glass Ball", "Crystal Glass", "Ceramic Vase", "Ice Cube", "Tempered Glass" };
    private int currentObjectTypeIndex = 0;
    
    void Start()
    {
        Debug.Log("=== COMPREHENSIVE FRACTURE DEMO ===");
        
        activeObjects = new List<GameObject>();
        lastSpawnTime = Time.time;
        lastResetTime = Time.time;
        
        SetupScene();
        
        if (autoSpawnObjects)
        {
            StartCoroutine(AutoSpawnRoutine());
        }
        
        Debug.Log("Comprehensive fracture demo initialized!");
        Debug.Log("Controls:");
        Debug.Log("  SPACE: Spawn random object");
        Debug.Log("  1-5: Spawn specific object type");
        Debug.Log("  S: Toggle slow motion");
        Debug.Log("  C: Clear all objects");
        Debug.Log("  R: Reset demo");
    }
    
    void SetupScene()
    {
        CreateAdvancedGround();
        SetupLighting();
        SetupCamera();
        
        // Spawn initial objects if enabled
        if (autoSpawnObjects)
        {
            SpawnInitialObjects();
        }
    }
    
    void CreateAdvancedGround()
    {
        // Create main ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Advanced Ground";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(25, 1, 25);
        ground.tag = "Ground";
        
        // Create realistic stone material
        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.color = new Color(0.3f, 0.3f, 0.3f);
        groundMat.SetFloat("_Metallic", 0.0f);
        groundMat.SetFloat("_Smoothness", 0.2f);
        ground.GetComponent<Renderer>().material = groundMat;
        
        ground.isStatic = true;
        
        // Add some platforms at different heights for variety
        CreatePlatform(new Vector3(-8, 2, 0), new Vector3(4, 0.5f, 4));
        CreatePlatform(new Vector3(8, 3, 0), new Vector3(3, 0.5f, 3));
        CreatePlatform(new Vector3(0, 1, 8), new Vector3(5, 0.5f, 2));
        
        Debug.Log("Advanced ground with platforms created");
    }
    
    void CreatePlatform(Vector3 position, Vector3 scale)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Platform";
        platform.transform.position = position;
        platform.transform.localScale = scale;
        platform.tag = "Ground";
        
        Material platformMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        platformMat.color = new Color(0.4f, 0.4f, 0.5f);
        platformMat.SetFloat("_Metallic", 0.1f);
        platformMat.SetFloat("_Smoothness", 0.3f);
        platform.GetComponent<Renderer>().material = platformMat;
        
        platform.isStatic = true;
    }
    
    void SetupLighting()
    {
        // Create main directional light
        GameObject lightObj = new GameObject("Main Light");
        Light mainLight = lightObj.AddComponent<Light>();
        mainLight.type = LightType.Directional;
        mainLight.intensity = 1.2f;
        mainLight.color = new Color(1f, 0.95f, 0.8f);
        mainLight.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(45, -30, 0);
        
        // Add fill light
        GameObject fillLightObj = new GameObject("Fill Light");
        Light fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.3f;
        fillLight.color = new Color(0.8f, 0.9f, 1f);
        fillLight.shadows = LightShadows.None;
        fillLightObj.transform.rotation = Quaternion.Euler(-20, 150, 0);
        
        // Setup ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.4f, 0.6f, 0.8f);
        RenderSettings.ambientEquatorColor = new Color(0.3f, 0.4f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
        
        Debug.Log("Advanced lighting setup complete");
    }
    
    void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("Demo Camera");
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
        }
        
        // Position camera for optimal viewing
        mainCamera.transform.position = new Vector3(8, 12, -15);
        mainCamera.transform.LookAt(new Vector3(0, 3, 0));
        mainCamera.backgroundColor = new Color(0.1f, 0.15f, 0.2f);
        mainCamera.fieldOfView = 75f;
        
        // Add camera controller for better viewing
        var cameraController = mainCamera.gameObject.AddComponent<DemoCameraController>();
        
        Debug.Log("Demo camera setup complete");
    }
    
    void SpawnInitialObjects()
    {
        // Spawn a few objects to start the demo
        StartCoroutine(SpawnInitialObjectsCoroutine());
    }
    
    IEnumerator SpawnInitialObjectsCoroutine()
    {
        yield return new WaitForSeconds(1f);
        
        for (int i = 0; i < 3; i++)
        {
            SpawnRandomObject();
            yield return new WaitForSeconds(1f);
        }
    }
    
    IEnumerator AutoSpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (activeObjects.Count < maxObjectsInScene)
            {
                SpawnRandomObject();
            }
            
            // Clean up destroyed objects
            CleanupDestroyedObjects();
        }
    }
    
    void Update()
    {
        HandleInput();
        
        // Auto reset if enabled
        if (enableResetTimer && Time.time - lastResetTime > resetInterval)
        {
            ResetDemo();
        }
        
        // Update object count display
        CleanupDestroyedObjects();
    }
    
    void HandleInput()
    {
        // Manual spawning
        if (enableManualSpawning)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SpawnRandomObject();
            }
            
            // Specific object types
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SpawnSpecificObject(0); // Glass Ball
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SpawnSpecificObject(1); // Crystal Glass
            if (Input.GetKeyDown(KeyCode.Alpha3))
                SpawnSpecificObject(2); // Ceramic Vase
            if (Input.GetKeyDown(KeyCode.Alpha4))
                SpawnSpecificObject(3); // Ice Cube
            if (Input.GetKeyDown(KeyCode.Alpha5))
                SpawnSpecificObject(4); // Tempered Glass
        }
        
        // Slow motion toggle
        if (Input.GetKeyDown(KeyCode.S))
        {
            ToggleSlowMotion();
        }
        
        // Clear all objects
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearAllObjects();
        }
        
        // Reset demo
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetDemo();
        }
        
        // Cycle object types for next spawn
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentObjectTypeIndex = (currentObjectTypeIndex + 1) % objectTypeNames.Length;
            Debug.Log($"Next spawn type: {objectTypeNames[currentObjectTypeIndex]}");
        }
    }
    
    void SpawnRandomObject()
    {
        if (activeObjects.Count >= maxObjectsInScene)
        {
            Debug.Log("Maximum objects in scene reached");
            return;
        }
        
        int objectType = Random.Range(0, 5);
        SpawnSpecificObject(objectType);
    }
    
    void SpawnSpecificObject(int typeIndex)
    {
        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject spawnedObject = null;
        
        switch (typeIndex)
        {
            case 0: // Glass Ball
                if (spawnGlassBalls)
                    spawnedObject = BreakableObjectFactory.CreateGlassBall(spawnPos, Random.Range(0.8f, 1.5f));
                break;
                
            case 1: // Crystal Glass
                if (spawnCrystalGlass)
                    spawnedObject = BreakableObjectFactory.CreateCrystalGlass(spawnPos, Random.Range(0.7f, 1.3f));
                break;
                
            case 2: // Ceramic Vase
                if (spawnCeramicVases)
                    spawnedObject = BreakableObjectFactory.CreateCeramicVase(spawnPos, Random.Range(0.9f, 1.4f));
                break;
                
            case 3: // Ice Cube
                if (spawnIceCubes)
                    spawnedObject = BreakableObjectFactory.CreateIceCube(spawnPos, Random.Range(0.8f, 1.2f));
                break;
                
            case 4: // Tempered Glass
                if (spawnTemperedGlass)
                    spawnedObject = BreakableObjectFactory.CreateTemperedGlass(spawnPos, Random.Range(0.9f, 1.6f));
                break;
        }
        
        if (spawnedObject != null)
        {
            activeObjects.Add(spawnedObject);
            spawnedObjectCount++;
            
            // Add random initial velocity for more interesting dynamics
            var softBody = spawnedObject.GetComponent<PBDSoftBody>();
            if (softBody != null)
            {
                StartCoroutine(AddInitialVelocity(softBody));
            }
            
            // Auto-destroy after some time to prevent buildup
            StartCoroutine(AutoDestroyObject(spawnedObject, 30f));
            
            Debug.Log($"Spawned {objectTypeNames[typeIndex]} #{spawnedObjectCount} at {spawnPos}");
            lastSpawnTime = Time.time;
        }
    }
    
    IEnumerator AddInitialVelocity(PBDSoftBody softBody)
    {
        yield return new WaitForSeconds(0.1f); // Wait for initialization
        
        if (softBody != null && softBody.Solver != null)
        {
            Vector3 randomVelocity = new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-1f, 1f),
                Random.Range(-2f, 2f)
            );
            
            softBody.ApplyDeformation(randomVelocity * 5f, softBody.transform.position);
        }
    }
    
    IEnumerator AutoDestroyObject(GameObject obj, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        
        if (obj != null)
        {
            activeObjects.Remove(obj);
            Destroy(obj);
        }
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
            Random.Range(0, spawnAreaSize.y),
            Random.Range(-spawnAreaSize.z * 0.5f, spawnAreaSize.z * 0.5f)
        );
        
        Vector3 spawnPos = spawnAreaCenter + randomOffset;
        spawnPos.y = Mathf.Max(spawnPos.y, minSpawnHeight);
        spawnPos.y = Mathf.Min(spawnPos.y, maxSpawnHeight);
        
        return spawnPos;
    }
    
    void CleanupDestroyedObjects()
    {
        activeObjects.RemoveAll(obj => obj == null);
    }
    
    void ToggleSlowMotion()
    {
        if (enableSlowMotion)
        {
            isSlowMotionActive = !isSlowMotionActive;
            
            if (isSlowMotionActive)
            {
                Time.timeScale = slowMotionScale;
                Time.fixedDeltaTime = 0.02f * slowMotionScale;
                Debug.Log("Slow motion activated");
            }
            else
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                Debug.Log("Normal speed restored");
            }
        }
    }
    
    void ClearAllObjects()
    {
        foreach (var obj in activeObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        
        activeObjects.Clear();
        Debug.Log("All objects cleared");
    }
    
    void ResetDemo()
    {
        ClearAllObjects();
        spawnedObjectCount = 0;
        lastResetTime = Time.time;
        
        // Reset time scale
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isSlowMotionActive = false;
        
        Debug.Log("Demo reset complete");
        
        // Spawn initial objects again
        if (autoSpawnObjects)
        {
            SpawnInitialObjects();
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw spawn area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
        
        // Draw spawn height limits
        Gizmos.color = Color.green;
        Vector3 minHeightPos = spawnAreaCenter;
        minHeightPos.y = minSpawnHeight;
        Gizmos.DrawWireCube(minHeightPos, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.z));
        
        Gizmos.color = Color.red;
        Vector3 maxHeightPos = spawnAreaCenter;
        maxHeightPos.y = maxSpawnHeight;
        Gizmos.DrawWireCube(maxHeightPos, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.z));
    }
    
    void OnGUI()
    {
        // Demo information panel
        GUILayout.BeginArea(new Rect(10, 10, 500, 400));
        GUILayout.Box("COMPREHENSIVE FRACTURE MECHANICS DEMO\n\n" +
                      "This demo showcases various breakable materials:\n" +
                      "• Glass Balls - Standard window glass behavior\n" +
                      "• Crystal Glass - Pure, more brittle than regular glass\n" +
                      "• Ceramic Vases - Very brittle, shatters easily\n" +
                      "• Ice Cubes - Cold and brittle, unique fracture patterns\n" +
                      "• Tempered Glass - Stronger, requires more force to break\n\n" +
                      "Controls:\n" +
                      "• SPACE: Spawn random object\n" +
                      "• 1-5: Spawn specific object type\n" +
                      "• S: Toggle slow motion (Matrix effect!)\n" +
                      "• C: Clear all objects\n" +
                      "• R: Reset entire demo\n" +
                      "• TAB: Cycle next spawn type\n\n" +
                      $"Active Objects: {activeObjects.Count}/{maxObjectsInScene}\n" +
                      $"Total Spawned: {spawnedObjectCount}\n" +
                      $"Next Type: {objectTypeNames[currentObjectTypeIndex]}\n" +
                      $"Auto Spawn: {(autoSpawnObjects ? "ON" : "OFF")}\n" +
                      $"Slow Motion: {(isSlowMotionActive ? "ACTIVE" : "OFF")}\n" +
                      $"Time Scale: {Time.timeScale:F2}x");
        GUILayout.EndArea();
        
        // Object type legend
        GUILayout.BeginArea(new Rect(Screen.width - 200, 10, 190, 150));
        GUILayout.Box("OBJECT TYPES:\n\n" +
                      "1 - Glass Ball\n" +
                      "2 - Crystal Glass\n" +
                      "3 - Ceramic Vase\n" +
                      "4 - Ice Cube\n" +
                      "5 - Tempered Glass");
        GUILayout.EndArea();
    }
}

/// <summary>
/// Simple camera controller for the demo
/// </summary>
public class DemoCameraController : MonoBehaviour
{
    [SerializeField] private float orbitSpeed = 10f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private Vector3 targetPoint = Vector3.zero;
    [SerializeField] private float distance = 20f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 30f;
    
    private float currentYaw = 0f;
    private float currentPitch = 20f;
    private bool isAutoRotating = true;
    
    void Start()
    {
        UpdateCameraPosition();
    }
    
    void Update()
    {
        HandleInput();
        
        if (isAutoRotating)
        {
            // Slow automatic rotation for cinematic effect
            currentYaw += orbitSpeed * Time.deltaTime;
        }
        
        UpdateCameraPosition();
    }
    
    void HandleInput()
    {
        // Toggle auto rotation
        if (Input.GetKeyDown(KeyCode.A))
        {
            isAutoRotating = !isAutoRotating;
        }
        
        // Manual camera control with mouse
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            isAutoRotating = false;
            currentYaw += Input.GetAxis("Mouse X") * 5f;
            currentPitch -= Input.GetAxis("Mouse Y") * 5f;
            currentPitch = Mathf.Clamp(currentPitch, -30f, 80f);
        }
        
        // Zoom with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }
    
    void UpdateCameraPosition()
    {
        // Calculate position based on spherical coordinates
        float pitchRad = currentPitch * Mathf.Deg2Rad;
        float yawRad = currentYaw * Mathf.Deg2Rad;
        
        Vector3 offset = new Vector3(
            Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            Mathf.Sin(pitchRad),
            Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        ) * distance;
        
        transform.position = targetPoint + offset;
        transform.LookAt(targetPoint);
    }
}