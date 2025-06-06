// Assets/_Project/03_Simulation/Scripts/SimplePBDTest.cs

using _Project._01_Physics.Scripts.PBD_V1;
using UnityEngine;

public class SimplePBDTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== SIMPLE PBD TEST ===");
        
        // Create ground
        CreateSimpleGround();
        
        // Create falling ball with DIRECT property setting (no reflection)
        CreateSimpleFallingBall();
    }
    
    void CreateSimpleGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "SimpleGround";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(10, 1, 10);
        ground.tag = "Ground";
        
        // Make it visible
        var renderer = ground.GetComponent<Renderer>();
        renderer.material.color = Color.gray;
        
        Debug.Log("Simple ground created at Y = 0");
    }
    
    void CreateSimpleFallingBall()
    {
        // Create sphere manually
        GameObject sphere = new GameObject("Simple PBD Ball");
        sphere.transform.position = new Vector3(0, 5, 0);
        
        var meshFilter = sphere.AddComponent<MeshFilter>();
        var meshRenderer = sphere.AddComponent<MeshRenderer>();
        
        // Create simple sphere mesh
        meshFilter.mesh = CreateSphereMesh(0.5f, 8);
        
        // Red material
        var material = new Material(Shader.Find("Standard"));
        material.color = Color.red;
        meshRenderer.material = material;
        
        // Add PBD component
        var pbd = sphere.AddComponent<PBDSoftBody>();
        
        // Add debugger
        sphere.AddComponent<PBDDebugger>();
        
        // Initialize manually without reflection
        StartCoroutine(InitializePBDAfterFrame(pbd));
        
        Debug.Log("Simple PBD ball created");
    }
    
    System.Collections.IEnumerator InitializePBDAfterFrame(PBDSoftBody pbd)
    {
        yield return null; // Wait one frame
        
        // Create simple material
        var material = ScriptableObject.CreateInstance<_Project._00_Core.Scripts.DataStructures.PhysicsMaterial>();
        material.restitution = 0.8f;
        material.density = 1.0f;
        
        // Initialize
        pbd.Initialize(material);
        
        Debug.Log("PBD initialized with manual settings");
        
        // Verify solver
        if (pbd.Solver != null)
        {
            Debug.Log($"Solver created successfully with {pbd.Solver.Particles.Count} particles");
            
            // Make sure gravity is applied
            pbd.Solver.Gravity = new Vector3(0, -9.81f, 0);
            pbd.Solver.GlobalDamping = 0.99f;
            
            // Add ground constraint manually
            pbd.Solver.AddGroundConstraint(0f, 0.8f, 0.3f);
            
            Debug.Log("Manual gravity and ground constraint applied");
        }
        else
        {
            Debug.LogError("FAILED TO CREATE SOLVER!");
        }
    }
    
    Mesh CreateSphereMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        
        // Very simple sphere - just use Unity's primitive for now
        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mesh = tempSphere.GetComponent<MeshFilter>().mesh;
        DestroyImmediate(tempSphere);
        
        return mesh;
    }
}