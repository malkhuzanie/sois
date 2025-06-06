// Assets/_Project/03_Simulation/Scripts/PBDDebugger.cs

using _Project._01_Physics.Scripts.PBD_V1;
using UnityEngine;

public class PBDDebugger : MonoBehaviour
{
    private PBDSoftBody softBody;
    private Vector3 lastPosition;
    private float lastTime;
    
    void Start()
    {
        softBody = GetComponent<PBDSoftBody>();
        lastPosition = transform.position;
        lastTime = Time.time;
    }
    
    void Update()
    {
        if (softBody == null) return;
        
        // Check if position is changing
        Vector3 currentPos = transform.position;
        float deltaTime = Time.time - lastTime;
        
        if (deltaTime > 0.5f) // Every half second
        {
            Vector3 movement = currentPos - lastPosition;
            float speed = movement.magnitude / deltaTime;
            
            Debug.Log($"=== PBD DEBUGGING ===");
            Debug.Log($"Position: {currentPos:F3}");
            Debug.Log($"Movement in last {deltaTime:F1}s: {movement:F3}");
            Debug.Log($"Speed: {speed:F3}");
            Debug.Log($"Is falling: {movement.y < -0.01f}");
            
            if (softBody.Solver != null)
            {
                Debug.Log($"Solver particles: {softBody.Solver.Particles.Count}");
                Debug.Log($"Solver constraints: {softBody.Solver.Constraints.Count}");
                Debug.Log($"Solver gravity: {softBody.Solver.Gravity}");
                
                // Check first few particles
                for (int i = 0; i < Mathf.Min(3, softBody.Solver.Particles.Count); i++)
                {
                    var particle = softBody.Solver.Particles[i];
                    Debug.Log($"Particle {i}: Pos={particle.Position:F2}, Vel={particle.Velocity:F2}, Fixed={particle.IsFixed}");
                }
            }
            else
            {
                Debug.LogError("SOLVER IS NULL!");
            }
            
            lastPosition = currentPos;
            lastTime = Time.time;
        }
    }
}