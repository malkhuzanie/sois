// File: ./03_Simulation/Scripts/Scenes/SuperBouncePBDTest.cs

using UnityEngine;
using _Project._01_Physics.Scripts.PBD;

public class SuperBouncePBDTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== SUPER BOUNCE PBD TEST (MODIFIED FOR DEFORMATION) ===");
        
        CreateGround();
        CreateSuperBouncyBall(); // This will now create a more deformable ball
    }
    
    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "SuperGround";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(20, 1, 20);
        ground.tag = "Ground";
        
        var renderer = ground.GetComponent<Renderer>();
        // Ensure a visible material if running in URP/HDRP
        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        groundMat.color = Color.gray;
        renderer.material = groundMat;
        
        Debug.Log("Super bounce ground created");
    }
    
    void CreateSuperBouncyBall()
    {
        GameObject ball = new GameObject("Super Bouncy (More Deformable) Ball");
        ball.transform.position = new Vector3(0, 8, 0); // Start higher
        
        var meshFilter = ball.AddComponent<MeshFilter>();
        var meshRenderer = ball.AddComponent<MeshRenderer>();
        
        // Create sphere mesh manually
        meshFilter.mesh = CreateDetailedSphereMesh(0.5f, 12); // Resolution 12 for enough particles
        
        // Bright color to see deformation better
        // Ensure a visible material if running in URP/HDRP
        Material ballMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        ballMat.color = Color.cyan;
        meshRenderer.material = ballMat;
        
        var pbd = ball.AddComponent<PBDSoftBody>();
        
        // Initialize after a frame
        StartCoroutine(InitializeSuperBouncyAndDeformable(pbd));
    }
    
    System.Collections.IEnumerator InitializeSuperBouncyAndDeformable(PBDSoftBody pbd)
    {
        yield return null;
    
        // Create ultra bouncy material from preset
        var material = PBDMaterialPresets.CreateUltraBouncyRubber(); 
    
        pbd.Initialize(material); // This sets up internal constraints first
    
        if (pbd.Solver != null)
        {
            // MODIFIED SETTINGS FOR DEFORMATION + BOUNCE
            pbd.Solver.GlobalDamping = 0.998f;    // Still very bouncy, slightly more damping than ultra-high
            
            // --- KEY CHANGES FOR DEFORMATION ---
            pbd.Solver.GlobalStiffness = 0.15f;   // <<<< SIGNIFICANTLY REDUCED (was 0.3f or higher before any explicit set)
                                                  //      Lower stiffness means particles move more freely under constraint forces.
            pbd.Solver.ConstraintIterations = 2;  // <<<< REDUCED (was 3 or higher)
                                                  //      Fewer iterations mean constraints are less strictly enforced, allowing deformation.
            // --- END KEY CHANGES ---

            pbd.Solver.Gravity = new Vector3(0, -9.81f, 0);
        
            // This is CRITICAL: By clearing constraints and only adding the ground,
            // the ball has NO internal structure holding its shape, making it prone to deformation.
            pbd.Solver.Constraints.Clear(); 
            
            // Use the UltraBouncyGroundConstraint for high bounce, but the deformation will come
            // from the lack of internal structure and the soft solver settings above.
            // The ground Y should match your actual ground surface for PBDSoftBody
            var ultraGroundConstraint = new UltraBouncyGroundConstraint(0.0f, 0.99f, 0.1f); 
            pbd.Solver.Constraints.Add(ultraGroundConstraint);
        
            Debug.Log("SUPER BOUNCY & DEFORMABLE PBD initialized!");
            Debug.Log($"Solver Settings: GlobalStiffness={pbd.Solver.GlobalStiffness}, Iterations={pbd.Solver.ConstraintIterations}");
            Debug.Log($"Particles: {pbd.Solver.Particles.Count}, Constraints: {pbd.Solver.Constraints.Count} (should be 1: the ground constraint)");
        }
        else
        {
            Debug.LogError("PBD Solver is null after initialization!");
        }
    }    
    
    Mesh CreateDetailedSphereMesh(float radius, int segments)
    {
        // Use the PBDMeshGenerator for consistent sphere generation
        return PBDMeshGenerator.GenerateSphereMesh(radius, segments, segments);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 150));
        GUILayout.Box("Super Bounce PBD Test (Deformable)\n\n" +
                      "Ball should now deform more visibly\n" +
                      "on impact due to softer solver settings:\n" +
                      $"- Global Stiffness: {GameObject.FindObjectOfType<PBDSoftBody>()?.Solver?.GlobalStiffness ?? 0.15f}\n" +
                      $"- Constraint Iterations: {GameObject.FindObjectOfType<PBDSoftBody>()?.Solver?.ConstraintIterations ?? 2}\n" +
                      "- Internal structural constraints are cleared.");
        GUILayout.EndArea();
    }
}