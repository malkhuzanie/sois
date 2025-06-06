// Assets/_Project/01_Physics/Scripts/XPBD/Components/EnhancedDeformationMonitor.cs

using UnityEngine;
using _Project._01_Physics.Scripts.XPBD.Components;

namespace _Project._01_Physics.Scripts.XPBD.Components
{
    /// <summary>
    /// Enhanced deformation monitor with visual compression feedback
    /// </summary>
    public class EnhancedDeformationMonitor : MonoBehaviour
    {
        [Header("Monitoring")]
        [SerializeField] private bool showDeformationInfo = true;
        [SerializeField] private bool visualizeDeformation = true;
        [SerializeField] private float compressionScale = 2.0f; // Exaggerate compression visually
        
        private XPBDRubberBall rubberBall;
        private MeshRenderer meshRenderer;
        private Material originalMaterial;
        private Material compressedMaterial;
        
        private float originalRadius;
        private float currentMinRadius = float.MaxValue;
        private float currentMaxRadius = 0f;
        private float maxCompressionSeen = 0f;
        
        void Start()
        {
            rubberBall = GetComponent<XPBDRubberBall>();
            meshRenderer = GetComponent<MeshRenderer>();
            originalMaterial = meshRenderer.material;
            
            // Create compressed material (redder when compressed)
            compressedMaterial = new Material(originalMaterial);
            compressedMaterial.color = Color.Lerp(originalMaterial.color, Color.red, 0.5f);
            
            originalRadius = GetPrivateField<float>(rubberBall, "radius");
        }
        
        void Update()
        {
            if (rubberBall == null || !rubberBall.IsInitialized) return;
            
            MonitorDeformation();
            
            if (visualizeDeformation)
            {
                VisualizeCompression();
            }
        }
        
        void MonitorDeformation()
        {
            if (rubberBall.Solver == null) return;
            
            // Calculate min and max distances from center for better deformation analysis
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
            
            currentMinRadius = float.MaxValue;
            currentMaxRadius = 0f;
            
            foreach (var particle in rubberBall.Solver.Particles)
            {
                if (!particle.IsFixed)
                {
                    float distance = Vector3.Distance(particle.Position, center);
                    currentMinRadius = Mathf.Min(currentMinRadius, distance);
                    currentMaxRadius = Mathf.Max(currentMaxRadius, distance);
                }
            }
            
            float compression = originalRadius - currentMinRadius;
            if (compression > maxCompressionSeen)
            {
                maxCompressionSeen = compression;
                Debug.Log($"NEW MAX COMPRESSION: {compression:F3} units ({(compression/originalRadius*100):F1}%)");
            }
            
            // Reset max compression tracking periodically
            if (Time.frameCount % 600 == 0) // Every 10 seconds
            {
                Debug.Log($"Max compression in last 10 seconds: {maxCompressionSeen:F3} units");
                maxCompressionSeen = 0f;
            }
        }
        
        void VisualizeCompression()
        {
            if (currentMinRadius == float.MaxValue) return;
            
            float compressionRatio = (originalRadius - currentMinRadius) / originalRadius;
            compressionRatio = Mathf.Clamp01(compressionRatio * compressionScale);
            
            // Change material color based on compression
            Color currentColor = Color.Lerp(originalMaterial.color, Color.red, compressionRatio);
            meshRenderer.material.color = currentColor;
            
            // Change emission for dramatic effect
            if (compressionRatio > 0.1f)
            {
                meshRenderer.material.SetColor("_EmissionColor", Color.red * compressionRatio * 0.5f);
                meshRenderer.material.EnableKeyword("_EMISSION");
            }
            else
            {
                meshRenderer.material.SetColor("_EmissionColor", Color.black);
                meshRenderer.material.DisableKeyword("_EMISSION");
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
            
            float compressionAmount = originalRadius - currentMinRadius;
            float compressionPercent = (compressionAmount / originalRadius) * 100f;
            float deformationAmount = currentMaxRadius - currentMinRadius;
            
            GUILayout.BeginArea(new Rect(320, 10, 280, 200));
            GUILayout.Box($"ENHANCED Deformation Monitor\n\n" +
                         $"Original Radius: {originalRadius:F3}\n" +
                         $"Min Radius: {currentMinRadius:F3}\n" +
                         $"Max Radius: {currentMaxRadius:F3}\n" +
                         $"Compression: {compressionAmount:F3} units\n" +
                         $"Compression %: {compressionPercent:F1}%\n" +
                         $"Deformation: {deformationAmount:F3}\n" +
                         $"Max Seen: {maxCompressionSeen:F3}\n" +
                         $"Status: {(compressionPercent > 5 ? "COMPRESSING!" : "Normal")}");
            GUILayout.EndArea();
        }
    }
}