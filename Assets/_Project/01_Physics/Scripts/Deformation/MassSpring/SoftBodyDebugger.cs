using UnityEngine;
using _Project._00_Core.Scripts.DataStructures;

namespace _Project._01_Physics.Scripts.Deformation.MassSpring
{
    /// <summary>
    /// Debug component to check soft body status
    /// </summary>
    [RequireComponent(typeof(SoftBodyWrapper))]
    public class SoftBodyDebugger : MonoBehaviour
    {
        private SoftBodyWrapper wrapper;
        private MeshFilter meshFilter;
        private Vector3 lastPosition;
        
        [Header("Debug Info")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private float positionCheckInterval = 0.5f;
        
        private float lastCheckTime;
        
        void Start()
        {
            wrapper = GetComponent<SoftBodyWrapper>();
            meshFilter = GetComponent<MeshFilter>();
            lastPosition = transform.position;
            lastCheckTime = Time.time;
            
            if (showDebugLogs)
            {
                Debug.Log($"[SoftBodyDebugger] Started on {gameObject.name}");
                Debug.Log($"[SoftBodyDebugger] Has SoftBodyWrapper: {wrapper != null}");
                Debug.Log($"[SoftBodyDebugger] Has MeshFilter: {meshFilter != null}");
                Debug.Log($"[SoftBodyDebugger] Has Mesh: {meshFilter?.mesh != null}");
                Debug.Log($"[SoftBodyDebugger] Mesh vertex count: {meshFilter?.mesh?.vertexCount ?? 0}");
            }
        }
        
        void Update()
        {
            if (Time.time - lastCheckTime > positionCheckInterval)
            {
                lastCheckTime = Time.time;
                
                if (showDebugLogs)
                {
                    bool hasMovement = Vector3.Distance(transform.position, lastPosition) > 0.001f;
                    
                    if (meshFilter?.mesh != null)
                    {
                        var bounds = meshFilter.mesh.bounds;
                        Debug.Log($"[SoftBodyDebugger] {gameObject.name} - " +
                                 $"Pos: {transform.position:F2}, " +
                                 $"Moving: {hasMovement}, " +
                                 $"Mesh Bounds Center: {bounds.center:F2}, " +
                                 $"Mesh Bounds Size: {bounds.size:F2}");
                    }
                    
                    // Try applying a small force to see if it responds
                    if (!hasMovement && wrapper != null)
                    {
                        Debug.LogWarning($"[SoftBodyDebugger] {gameObject.name} is not moving! Applying test force...");
                        wrapper.ApplyDeformation(Vector3.down * 10f, transform.position);
                    }
                }
                
                lastPosition = transform.position;
            }
        }
        
        void OnDrawGizmos()
        {
            if (meshFilter?.mesh != null)
            {
                Gizmos.color = Color.yellow;
                var bounds = meshFilter.mesh.bounds;
                var worldCenter = transform.TransformPoint(bounds.center);
                var worldSize = Vector3.Scale(bounds.size, transform.localScale);
                Gizmos.DrawWireCube(worldCenter, worldSize);
                
                // Draw gravity direction
                Gizmos.color = Color.red;
                Gizmos.DrawRay(worldCenter, Vector3.down * 2f);
            }
        }
    }
}