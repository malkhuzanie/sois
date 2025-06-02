// File: Assets/_Project/03_Simulation/Scripts/Scenes/SimpleFallingTest.cs
using UnityEngine;
using _Project._01_Physics.Scripts.Deformation.MassSpring;
using _Project._00_Core.Scripts.DataStructures;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

public class SimpleFallingTest : MonoBehaviour
{
    private MassSpringSystem system;
    private GameObject ball;
    private MeshFilter meshFilter;
    
    void Start()
    {
        Debug.Log("=== SIMPLE FALLING TEST ===");
        
        // Create material
        var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
        material.stiffness = 50f;
        material.damping = 2f;
        
        // Create ball
        ball = new GameObject("Simple Falling Ball");
        ball.transform.position = Vector3.zero; // Start at origin
        
        meshFilter = ball.AddComponent<MeshFilter>();
        MeshRenderer renderer = ball.AddComponent<MeshRenderer>();
        
        Material renderMat = new Material(Shader.Find("Standard"));
        renderMat.color = Color.green;
        renderer.material = renderMat;
        
        // Create system
        system = SoftBodyShapeGenerator.CreateSoftSphere(0.5f, 6, 6, 1.0f, material);
        system.Gravity = new Vector3(0, -2f, 0); // Slower gravity for clarity
        system.GlobalDamping = 0.99f;
        
        // Set initial mesh
        meshFilter.mesh = system.GetDeformedMesh();
        
        Debug.Log("Simple test initialized - watch the green ball fall");
    }
    
    void FixedUpdate()
    {
        if (system != null && meshFilter != null)
        {
            // Update physics
            system.Update(Time.fixedDeltaTime);
            
            // Get updated mesh
            Mesh updatedMesh = system.GetDeformedMesh();
            meshFilter.mesh = updatedMesh;
            
            // OPTION 1: Keep GameObject stationary (current approach)
            // The mesh vertices move, but GameObject stays at origin
            
            // OPTION 2: Move GameObject to follow center of mass (uncomment to try)
            /*
            Vector3 centerOfMass = CalculateCenterOfMass();
            ball.transform.position = centerOfMass;
            */
            
            // Debug
            Vector3 meshCenter = updatedMesh.bounds.center;
            Debug.Log($"Mesh bounds center: {meshCenter:F2}, GameObject pos: {ball.transform.position:F2}");
        }
    }
    
    Vector3 CalculateCenterOfMass()
    {
        // This would require accessing mass points from the system
        // For now, return the mesh bounds center
        return meshFilter.mesh.bounds.center;
    }
}