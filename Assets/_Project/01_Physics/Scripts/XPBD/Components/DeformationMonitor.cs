// Assets/_Project/01_Physics/Scripts/XPBD/Components/DeformationMonitor.cs

using UnityEngine;
using _Project._01_Physics.Scripts.XPBD.Components;

namespace _Project._01_Physics.Scripts.XPBD.Components
{
    /// <summary>
    /// Monitors and visualizes ball deformation
    /// </summary>
    public class DeformationMonitor : MonoBehaviour
    {
        [Header("Monitoring")]
        [SerializeField] private bool showDeformationInfo = true;
        [SerializeField] private float deformationThreshold = 0.05f;
        
        private XPBDRubberBall rubberBall;
        private float originalRadius;
        private float currentRadius;
        private float maxDeformation = 0f;
        private bool isDeformed = false;
        
        void Start()
        {
            rubberBall = GetComponent<XPBDRubberBall>();
            originalRadius = GetPrivateField<float>(rubberBall, "radius");
        }
        
        void Update()
        {
            if (rubberBall == null || !rubberBall.IsInitialized) return;
            
            MonitorDeformation();
            
            if (showDeformationInfo && isDeformed)
            {
                Debug.Log($"Ball deformed! Original radius: {originalRadius:F3}, Current: {currentRadius:F3}, " +
                         $"Compression: {((originalRadius - currentRadius) / originalRadius * 100):F1}%");
            }
        }
        
        void MonitorDeformation()
        {
            if (rubberBall.Solver == null) return;
            
            // Calculate current effective radius by measuring particle distances from center
            Vector3 center = Vector3.zero;
            int activeParticles = 0;
            
            foreach (var particle in rubberBall.Solver.Particles)
            {
                if (!particle.IsFixed)
                {
                    center += particle.Position;
                    activeParticles++;
                }
            }
            
            if (activeParticles == 0) return;
            center /= activeParticles;
            
            // Calculate average distance from center (current radius)
            float totalDistance = 0f;
            foreach (var particle in rubberBall.Solver.Particles)
            {
                if (!particle.IsFixed)
                {
                    totalDistance += Vector3.Distance(particle.Position, center);
                }
            }
            
            currentRadius = totalDistance / activeParticles;
            float deformation = originalRadius - currentRadius;
            
            // Update max deformation
            if (deformation > maxDeformation)
            {
                maxDeformation = deformation;
            }
            
            // Check if significantly deformed
            isDeformed = deformation > deformationThreshold;
            
            // Reset max deformation periodically
            if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
            {
                if (showDeformationInfo && maxDeformation > 0.01f)
                {
                    Debug.Log($"Max deformation in last 5 seconds: {maxDeformation:F3} units " +
                             $"({(maxDeformation / originalRadius * 100):F1}% compression)");
                }
                maxDeformation = 0f;
            }
        }
        
        T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (T)field.GetValue(obj) : default(T);
        }
        
        void OnGUI()
        {
            if (!showDeformationInfo || !rubberBall.IsInitialized) return;
            
            GUILayout.BeginArea(new Rect(320, 10, 250, 150));
            GUILayout.Box($"Deformation Monitor\n\n" +
                         $"Original Radius: {originalRadius:F3}\n" +
                         $"Current Radius: {currentRadius:F3}\n" +
                         $"Compression: {((originalRadius - currentRadius) / originalRadius * 100):F1}%\n" +
                         $"Max Deformation: {maxDeformation:F3}\n" +
                         $"Is Deformed: {isDeformed}\n" +
                         $"Status: {(isDeformed ? "COMPRESSING" : "NORMAL")}");
            GUILayout.EndArea();
        }
    }
}