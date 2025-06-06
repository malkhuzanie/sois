// Assets/_Project/03_Simulation/Scripts/Diagnostics/FractureDiagnostics.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using _Project._01_Physics.Scripts.PBD_V1;

namespace _Project._03_Simulation.Scripts.Diagnostics
{
    /// <summary>
    /// Comprehensive diagnostics component for fracture mechanics debugging
    /// </summary>
    public class FractureDiagnostics : MonoBehaviour
    {
        [Header("Monitoring Settings")]
        [SerializeField] private bool enableContinuousMonitoring = true;
        [SerializeField] private bool logStressChanges = false;
        [SerializeField] private bool logConstraintBreaking = true;
        [SerializeField] private bool showStressVisualization = true;
        [SerializeField] private float updateInterval = 0.5f;
        
        [Header("Stress Visualization")]
        [SerializeField] private Color lowStressColor = Color.green;
        [SerializeField] private Color highStressColor = Color.red;
        [SerializeField] private float stressVisualizationScale = 0.1f;
        
        [Header("Alert Thresholds")]
        [SerializeField] private float stressAlertThreshold = 8f;
        [SerializeField] private int brokenConstraintAlertThreshold = 3;
        
        private PBDSoftBody softBody;
        private float lastUpdateTime;
        private Dictionary<int, float> lastStressValues;
        private List<string> diagnosticLog;
        private int frameCount;
        
        // Statistics
        private float maxStressRecorded;
        private int totalConstraintsBroken;
        private float timeToFirstFracture = -1f;
        private bool hasFractured = false;
        
        void Start()
        {
            softBody = GetComponent<PBDSoftBody>();
            if (softBody == null)
            {
                Debug.LogError("FractureDiagnostics requires PBDSoftBody component!");
                enabled = false;
                return;
            }
            
            lastStressValues = new Dictionary<int, float>();
            diagnosticLog = new List<string>();
            
            Debug.Log("=== FRACTURE DIAGNOSTICS STARTED ===");
            LogDiagnostic("Fracture diagnostics initialized");
        }
        
        void Update()
        {
            if (!enableContinuousMonitoring || softBody?.Solver == null) return;
            
            frameCount++;
            
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                PerformDiagnostics();
                lastUpdateTime = Time.time;
            }
        }
        
        void PerformDiagnostics()
        {
            var solver = softBody.Solver;
            var stats = solver.GetStatistics();
            
            // Monitor stress levels
            MonitorStress();
            
            // Monitor constraint breaking
            MonitorConstraints(stats);
            
            // Check for fracture state change
            CheckFractureState();
            
            // Performance monitoring
            MonitorPerformance(stats);
            
            // Alert checks
            CheckAlerts();
        }
        
        void MonitorStress()
        {
            if (softBody.Solver.Particles == null) return;
            
            float totalStress = 0f;
            float maxCurrentStress = 0f;
            int stressedParticles = 0;
            
            foreach (var particle in softBody.Solver.Particles)
            {
                if (!particle.IsActive) continue;
                
                float currentStress = particle.StressAccumulation;
                totalStress += currentStress;
                
                if (currentStress > 0.1f)
                    stressedParticles++;
                
                if (currentStress > maxCurrentStress)
                    maxCurrentStress = currentStress;
                
                if (currentStress > maxStressRecorded)
                {
                    maxStressRecorded = currentStress;
                    LogDiagnostic($"New max stress recorded: {maxStressRecorded:F2} on particle {particle.VertexIndex}");
                }
                
                // Log stress changes
                if (logStressChanges && lastStressValues.ContainsKey(particle.VertexIndex))
                {
                    float stressDelta = currentStress - lastStressValues[particle.VertexIndex];
                    if (Mathf.Abs(stressDelta) > 1f)
                    {
                        LogDiagnostic($"Particle {particle.VertexIndex} stress change: {stressDelta:F2} (now {currentStress:F2})");
                    }
                }
                
                lastStressValues[particle.VertexIndex] = currentStress;
            }
            
            float avgStress = totalStress / softBody.Solver.Particles.Count;
            
            // Log stress summary periodically
            if (frameCount % 100 == 0)
            {
                LogDiagnostic($"Stress Summary - Avg: {avgStress:F2}, Max: {maxCurrentStress:F2}, " +
                             $"Stressed Particles: {stressedParticles}/{softBody.Solver.Particles.Count}");
            }
        }
        
        void MonitorConstraints(object stats)
        {
            // Use reflection to get stats since we don't have the exact type
            var statsType = stats.GetType();
            var brokenConstraintsField = statsType.GetField("brokenConstraints");
            
            if (brokenConstraintsField != null)
            {
                int currentBrokenConstraints = (int)brokenConstraintsField.GetValue(stats);
                
                if (currentBrokenConstraints > totalConstraintsBroken)
                {
                    int newBreaks = currentBrokenConstraints - totalConstraintsBroken;
                    totalConstraintsBroken = currentBrokenConstraints;
                    
                    if (logConstraintBreaking)
                    {
                        LogDiagnostic($"Constraints broken this frame: {newBreaks}, Total: {totalConstraintsBroken}");
                    }
                }
            }
        }
        
        void CheckFractureState()
        {
            if (!hasFractured && softBody.IsFractured)
            {
                hasFractured = true;
                timeToFirstFracture = Time.time;
                LogDiagnostic($"FRACTURE DETECTED at time {timeToFirstFracture:F2}s");
            }
        }
        
        void MonitorPerformance(object stats)
        {
            var statsType = stats.GetType();
            var solveTimeField = statsType.GetField("solveTime");
            
            if (solveTimeField != null)
            {
                float solveTime = (float)solveTimeField.GetValue(stats);
                
                if (solveTime > 0.01f) // 10ms threshold
                {
                    LogDiagnostic($"WARNING: High solve time detected: {solveTime * 1000f:F2}ms");
                }
            }
        }
        
        void CheckAlerts()
        {
            // Check for high stress alert
            foreach (var particle in softBody.Solver.Particles)
            {
                if (particle.IsActive && particle.StressAccumulation > stressAlertThreshold)
                {
                    Debug.LogWarning($"HIGH STRESS ALERT: Particle {particle.VertexIndex} stress = {particle.StressAccumulation:F2}");
                }
            }
            
            // Check for broken constraints alert
            if (totalConstraintsBroken > brokenConstraintAlertThreshold)
            {
                Debug.LogWarning($"FRACTURE ALERT: {totalConstraintsBroken} constraints broken!");
            }
        }
        
        void LogDiagnostic(string message)
        {
            string timestampedMessage = $"[{Time.time:F2}s] {message}";
            diagnosticLog.Add(timestampedMessage);
            
            // Keep log size manageable
            if (diagnosticLog.Count > 1000)
            {
                diagnosticLog.RemoveAt(0);
            }
            
            Debug.Log($"[FractureDiag] {timestampedMessage}");
        }
        
        void OnDrawGizmos()
        {
            if (!showStressVisualization || softBody?.Solver == null) return;
            
            // Visualize stress levels on particles
            foreach (var particle in softBody.Solver.Particles)
            {
                if (!particle.IsActive) continue;
                
                float stressRatio = Mathf.Clamp01(particle.StressAccumulation / softBody.Solver.GlobalFractureThreshold);
                Color stressColor = Color.Lerp(lowStressColor, highStressColor, stressRatio);
                
                Gizmos.color = stressColor;
                float size = 0.02f + (stressRatio * stressVisualizationScale);
                Gizmos.DrawSphere(particle.Position, size);
                
                // Draw stress level as a vertical bar
                if (stressRatio > 0.1f)
                {
                    Vector3 barStart = particle.Position + Vector3.up * 0.1f;
                    Vector3 barEnd = barStart + Vector3.up * (stressRatio * 0.5f);
                    Gizmos.DrawLine(barStart, barEnd);
                }
            }
        }
        
        void OnGUI()
        {
            if (!enableContinuousMonitoring) return;
            
            // Real-time diagnostics display
            GUILayout.BeginArea(new Rect(Screen.width - 350, 10, 340, 400));
            GUILayout.Box("FRACTURE DIAGNOSTICS\n\n" +
                         $"Max Stress Recorded: {maxStressRecorded:F2}\n" +
                         $"Total Constraints Broken: {totalConstraintsBroken}\n" +
                         $"Has Fractured: {hasFractured}\n" +
                         $"Time to Fracture: {(timeToFirstFracture > 0 ? timeToFirstFracture.ToString("F2") + "s" : "N/A")}\n" +
                         $"Update Interval: {updateInterval:F1}s\n" +
                         $"Alert Threshold: {stressAlertThreshold:F1}\n\n" +
                         "Recent Log Entries:");
            
            // Show recent log entries
            GUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(200));
            int startIndex = Mathf.Max(0, diagnosticLog.Count - 10);
            for (int i = startIndex; i < diagnosticLog.Count; i++)
            {
                GUILayout.Label(diagnosticLog[i], GUILayout.Width(320));
            }
            GUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Get comprehensive fracture report
        /// </summary>
        public string GetDiagnosticReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== FRACTURE DIAGNOSTICS REPORT ===");
            report.AppendLine($"Test Duration: {Time.time:F2}s");
            report.AppendLine($"Max Stress Recorded: {maxStressRecorded:F2}");
            report.AppendLine($"Total Constraints Broken: {totalConstraintsBroken}");
            report.AppendLine($"Has Fractured: {hasFractured}");
            report.AppendLine($"Time to First Fracture: {(timeToFirstFracture > 0 ? timeToFirstFracture.ToString("F2") + "s" : "Not fractured")}");
            
            if (softBody?.Solver != null)
            {
                var stats = softBody.Solver.GetStatistics();
                report.AppendLine($"Current Active Particles: {stats.particles}");
                report.AppendLine($"Current Active Constraints: {stats.activeConstraints}");
            }
            
            report.AppendLine("\nRecent Activity:");
            int startIndex = Mathf.Max(0, diagnosticLog.Count - 20);
            for (int i = startIndex; i < diagnosticLog.Count; i++)
            {
                report.AppendLine(diagnosticLog[i]);
            }
            
            return report.ToString();
        }
        
        /// <summary>
        /// Reset diagnostics for new test
        /// </summary>
        public void ResetDiagnostics()
        {
            maxStressRecorded = 0f;
            totalConstraintsBroken = 0;
            timeToFirstFracture = -1f;
            hasFractured = false;
            frameCount = 0;
            lastStressValues.Clear();
            diagnosticLog.Clear();
            
            LogDiagnostic("Diagnostics reset for new test");
        }
        
        [ContextMenu("Print Diagnostic Report")]
        public void PrintDiagnosticReport()
        {
            Debug.Log(GetDiagnosticReport());
        }
        
        [ContextMenu("Reset Diagnostics")]
        public void ResetDiagnosticsMenu()
        {
            ResetDiagnostics();
        }
    }
}