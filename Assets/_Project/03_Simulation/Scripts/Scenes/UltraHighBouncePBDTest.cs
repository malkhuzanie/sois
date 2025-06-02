// Assets/_Project/03_Simulation/Scripts/Scenes/UltraHighBouncePBDTest.cs

using UnityEngine;
using _Project._01_Physics.Scripts.PBD;

public class UltraHighBouncePBDTest : MonoBehaviour
{
    [Header("Extreme Bounce Settings")]
    [SerializeField] private bool createTestBall = true;
    [SerializeField] private Vector3 ballStartPosition = new Vector3(0, 8, 0);
    [SerializeField] private float ballSize = 1.5f;
    [SerializeField] private int ballResolution = 8; // Reduced for fewer constraints
    
    void Start()
    {
        Debug.Log("=== ULTRA HIGH BOUNCE PBD TEST ===");
        
        CreateGround();
        
        if (createTestBall)
            CreateUltraBouncyBall();
        
        Time.fixedDeltaTime = 0.016f; // 60Hz physics
        
        Debug.Log("Ultra high bounce test initialized - ball should bounce EXTREMELY high!");
    }
    
    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "UltraGround";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(30, 1, 30);
        ground.tag = "Ground";
        
        var renderer = ground.GetComponent<Renderer>();
        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.color = new Color(0.8f, 0.8f, 0.8f);
        renderer.material = groundMat;
        
        ground.isStatic = true;
        
        Debug.Log("Ultra ground created");
    }
    
    void CreateUltraBouncyBall()
    {
        GameObject ball = new GameObject("Ultra High Bounce Ball");
        ball.transform.position = ballStartPosition;
        
        var meshFilter = ball.AddComponent<MeshFilter>();
        var meshRenderer = ball.AddComponent<MeshRenderer>();
        
        // Create sphere mesh with LOW resolution to minimize constraints
        meshFilter.mesh = CreateSimpleSphereMesh(0.5f, ballResolution);
        
        // Bright bounce material
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = Color.magenta; // Bright magenta for visibility
        meshRenderer.material = material;
        
        var pbd = ball.AddComponent<PBDSoftBody>();
        
        // Initialize after a frame
        StartCoroutine(InitializeUltraBouncy(pbd));
    }
    
    System.Collections.IEnumerator InitializeUltraBouncy(PBDSoftBody pbd)
    {
        yield return null;
    
        // Create ultra bouncy material with extreme settings
        var material = PBDMaterialPresets.CreateUltraBouncyRubber();
        material.restitution = 0.999f; // Nearly perfect bounce
        material.stiffness = 200f; // Much lower stiffness
        material.damping = 1f; // Minimal damping
    
        pbd.Initialize(material);
    
        if (pbd.Solver != null)
        {
            // ULTRA HIGH BOUNCE SETTINGS
            pbd.Solver.GlobalDamping = 0.9999f; // Almost zero energy loss
            pbd.Solver.GlobalStiffness = 0.1f; // Very low stiffness for maximum flexibility
            pbd.Solver.ConstraintIterations = 1; // MINIMAL constraint solving to preserve energy
            pbd.Solver.Gravity = new Vector3(0, -9.81f, 0);
        
            // Clear all constraints and add only minimal ones
            pbd.Solver.Constraints.Clear();
            
            // Add ULTRA BOUNCY ground constraint
            var ultraGroundConstraint = new UltraBouncyGroundConstraint(0f, 0.999f, 0.05f);
            pbd.Solver.Constraints.Add(ultraGroundConstraint);
        
            Debug.Log("ULTRA HIGH BOUNCE PBD initialized with extreme bounce settings!");
            Debug.Log($"Particles: {pbd.Solver.Particles.Count}, Constraints: {pbd.Solver.Constraints.Count}");
        }
    }    
    
    Mesh CreateSimpleSphereMesh(float radius, int segments)
    {
        // Create a very simple sphere to minimize constraint count
        segments = Mathf.Clamp(segments, 4, 10); // Keep it simple
        
        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = Instantiate(tempSphere.GetComponent<MeshFilter>().mesh);
        DestroyImmediate(tempSphere);
        
        // Simplify the mesh
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        
        // Scale to desired radius
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] *= radius;
        }
        
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        Debug.Log($"Simple sphere: {vertices.Length} vertices, {triangles.Length/3} triangles");
        
        return mesh;
    }
}

/// <summary>
/// Ultra bouncy ground constraint that preserves maximum energy
/// </summary>
public class UltraBouncyGroundConstraint : _Project._01_Physics.Scripts.PBD.PBDConstraint
{
    public float GroundY;
    public float Restitution = 0.999f;
    public float Friction = 0.05f;

    public UltraBouncyGroundConstraint(float groundY, float restitution = 0.999f, float friction = 0.05f)
    {
        GroundY = groundY;
        Restitution = restitution;
        Friction = friction;
        Stiffness = 1.0f;
    }

    public override void SolveConstraint(System.Collections.Generic.List<_Project._01_Physics.Scripts.PBD.PBDParticle> particles, float globalStiffness)
    {
        foreach (var particle in particles)
        {
            if (particle.IsFixed) continue;

            // Check if particle is below ground
            if (particle.PredictedPosition.y < GroundY)
            {
                // Position correction - move to ground surface
                particle.PredictedPosition.y = GroundY;

                // EXTREME velocity correction for MAXIMUM bounce
                if (particle.Velocity.y < 0)
                {
                    // Calculate impact velocity
                    float impactSpeed = Mathf.Abs(particle.Velocity.y);

                    // Apply restitution with MASSIVE energy boost
                    float bounceSpeed = impactSpeed * Restitution;

                    // EXTREME BOOST: Add massive energy compensation
                    float energyBoost = 2.5f; // Even higher boost!
                    bounceSpeed *= energyBoost;
                    
                    // Add extra bounce impulse based on impact
                    float extraBounce = Mathf.Clamp(impactSpeed * 0.5f, 0f, 20f);
                    bounceSpeed += extraBounce;

                    // Set new upward velocity
                    particle.Velocity.y = bounceSpeed;

                    // Minimal friction
                    float frictionReduction = 1f - (Friction * 0.02f);
                    particle.Velocity.x *= frictionReduction;
                    particle.Velocity.z *= frictionReduction;

                    Debug.Log($"ULTRA BOUNCE: Impact={impactSpeed:F2}, Bounce={bounceSpeed:F2}, Boost={energyBoost}, Extra={extraBounce:F2}");
                }
            }
        }
    }
    
    public override bool IsSatisfied(System.Collections.Generic.List<_Project._01_Physics.Scripts.PBD.PBDParticle> particles, float tolerance = 0.01f)
    {
        foreach (var particle in particles)
        {
            if (particle.PredictedPosition.y < GroundY - tolerance)
                return false;
        }
        return true;
    }
}