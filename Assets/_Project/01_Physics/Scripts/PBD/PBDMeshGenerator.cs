// Assets/_Project/01_Physics/Scripts/PBD/PBDMeshGenerator.cs

using UnityEngine;
using System.Collections.Generic;

namespace _Project._01_Physics.Scripts.PBD
{
    /// <summary>
    /// Generates proper meshes for PBD soft body simulation
    /// </summary>
    public static class PBDMeshGenerator
    {
        /// <summary>
        /// Generate a sphere mesh with adequate resolution for soft body simulation
        /// </summary>
        public static Mesh GenerateSphereMesh(float radius, int longitudeSegments, int latitudeSegments)
        {
            // Ensure minimum resolution for soft body
            longitudeSegments = Mathf.Max(longitudeSegments, 8);
            latitudeSegments = Mathf.Max(latitudeSegments, 6);
            
            var mesh = new Mesh();
            mesh.name = "PBD_Sphere";
            
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            
            // Add top pole
            vertices.Add(new Vector3(0, radius, 0));
            uvs.Add(new Vector2(0.5f, 1f));
            
            // Generate latitude rings
            for (int lat = 1; lat < latitudeSegments; lat++)
            {
                float theta = lat * Mathf.PI / latitudeSegments;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);
                float y = radius * cosTheta;
                
                for (int lon = 0; lon < longitudeSegments; lon++)
                {
                    float phi = lon * 2 * Mathf.PI / longitudeSegments;
                    float x = radius * sinTheta * Mathf.Cos(phi);
                    float z = radius * sinTheta * Mathf.Sin(phi);
                    
                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2((float)lon / longitudeSegments, 1f - (float)lat / latitudeSegments));
                }
            }
            
            // Add bottom pole
            vertices.Add(new Vector3(0, -radius, 0));
            uvs.Add(new Vector2(0.5f, 0f));
            
            // Generate triangles
            // Top cap
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int next = (lon + 1) % longitudeSegments;
                triangles.Add(0); // Top pole
                triangles.Add(1 + next);
                triangles.Add(1 + lon);
            }
            
            // Middle rings
            for (int lat = 0; lat < latitudeSegments - 2; lat++)
            {
                for (int lon = 0; lon < longitudeSegments; lon++)
                {
                    int current = 1 + lat * longitudeSegments + lon;
                    int next = 1 + lat * longitudeSegments + (lon + 1) % longitudeSegments;
                    int below = 1 + (lat + 1) * longitudeSegments + lon;
                    int belowNext = 1 + (lat + 1) * longitudeSegments + (lon + 1) % longitudeSegments;
                    
                    // First triangle
                    triangles.Add(current);
                    triangles.Add(belowNext);
                    triangles.Add(next);
                    
                    // Second triangle
                    triangles.Add(current);
                    triangles.Add(below);
                    triangles.Add(belowNext);
                }
            }
            
            // Bottom cap
            int bottomPoleIndex = vertices.Count - 1;
            int lastRingStart = 1 + (latitudeSegments - 2) * longitudeSegments;
            
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int next = (lon + 1) % longitudeSegments;
                triangles.Add(bottomPoleIndex); // Bottom pole
                triangles.Add(lastRingStart + lon);
                triangles.Add(lastRingStart + next);
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            Debug.Log($"Generated sphere mesh: {vertices.Count} vertices, {triangles.Count / 3} triangles");
            
            return mesh;
        }
        
        /// <summary>
        /// Create internal structure for better volume preservation
        /// </summary>
        public static void AddInternalVertices(Mesh mesh, float radius, int internalLayers = 2)
        {
            var vertices = new List<Vector3>(mesh.vertices);
            var triangles = new List<int>(mesh.triangles);
            
            int originalVertexCount = vertices.Count;
            
            // Add internal concentric spheres
            for (int layer = 1; layer <= internalLayers; layer++)
            {
                float layerRadius = radius * (1f - (float)layer / (internalLayers + 1));
                
                // Add center point for this layer
                vertices.Add(Vector3.zero);
                
                // Add vertices at reduced radius
                for (int i = 1; i < originalVertexCount - 1; i++) // Skip poles
                {
                    Vector3 surfaceVertex = mesh.vertices[i];
                    Vector3 internalVertex = surfaceVertex.normalized * layerRadius;
                    vertices.Add(internalVertex);
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            Debug.Log($"Added internal structure: {vertices.Count} total vertices");
        }
    }
}