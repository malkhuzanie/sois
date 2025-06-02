// Assets/_Project/03_Simulation/Scripts/PhysicsDiagnostic.cs
using UnityEngine;
using _Project._01_Physics.Scripts.Deformation.MassSpring;

public class PhysicsDiagnostic : MonoBehaviour
{
    [Header("Diagnostic Settings")]
    [SerializeField] private bool enableDiagnostics = true;
    [SerializeField] private float diagnosticInterval = 1f;
    
    private SoftBodyWrapper softBodyWrapper;
    private float lastDiagnosticTime;
    
    void Start()
    {
        softBodyWrapper = GetComponent<SoftBodyWrapper>();
        lastDiagnosticTime = Time.time;
    }
    
    void Update()
    {
        if (!enableDiagnostics || softBodyWrapper?.System == null) return;
        
        if (Time.time - lastDiagnosticTime > diagnosticInterval)
        {
            RunDiagnostics();
            lastDiagnosticTime = Time.time;
        }
    }
    
    void RunDiagnostics()
    {
        var system = softBodyWrapper.System;
        var massPoints = system.MassPoints;
        
        Debug.Log("=== PHYSICS DIAGNOSTIC ===");
        Debug.Log($"Gravity: {system.Gravity}");
        Debug.Log($"Global Damping: {system.GlobalDamping}");
        Debug.Log($"Total Mass Points: {massPoints.Count}");
        
        // Check if any points are fixed
        int fixedPoints = 0;
        int movingPoints = 0;
        Vector3 totalForce = Vector3.zero;
        Vector3 totalVelocity = Vector3.zero;
        Vector3 averagePosition = Vector3.zero;
        
        foreach (var point in massPoints)
        {
            if (point.IsFixed)
                fixedPoints++;
            else
                movingPoints++;
                
            totalForce += point.Force;
            totalVelocity += point.Velocity;
            averagePosition += point.Position;
        }
        
        averagePosition /= massPoints.Count;
        
        Debug.Log($"Fixed points: {fixedPoints}, Moving points: {movingPoints}");
        Debug.Log($"Average position: {averagePosition}");
        Debug.Log($"Total force magnitude: {totalForce.magnitude:F3}");
        Debug.Log($"Average velocity magnitude: {(totalVelocity / massPoints.Count).magnitude:F3}");
        Debug.Log($"World position: {transform.position}");
        
        // Check if gravity is being applied
        Vector3 expectedGravityForce = system.Gravity * (massPoints.Count > 0 ? massPoints[0].Mass : 1f) * massPoints.Count;
        Debug.Log($"Expected total gravity force: {expectedGravityForce}");
        
        // Check if object has moved
        Vector3 worldCenter = transform.TransformPoint(averagePosition);
        Debug.Log($"World center: {worldCenter}");
        
        if (totalVelocity.magnitude < 0.01f && totalForce.magnitude < 0.01f)
        {
            Debug.LogWarning("BALL IS STUCK! No forces or velocities detected.");
            Debug.LogWarning("Possible causes:");
            Debug.LogWarning("- All points are fixed");
            Debug.LogWarning("- Gravity not being applied");
            Debug.LogWarning("- Forces being cancelled out");
            Debug.LogWarning("- Cohesion forces too strong");
        }
    }
}