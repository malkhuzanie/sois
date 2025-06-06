// Assets/_Project/01_Physics/Scripts/XPBD/Utilities/XPBDMeshGenerator.cs

using System.Collections.Generic;
using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Utilities
{
    /// <summary>
    /// Generates meshes optimized for XPBD simulation
    /// </summary>
    public static class XPBDMeshGenerator
    {
        /// <summary>
        /// Generate sphere mesh optimized for XPBD
        /// Creates uniform triangulation suitable for mass-spring systems
        /// </summary>
        public static Mesh GenerateSphereMesh(float radius, int subdivisions)
        {
            subdivisions = Mathf.Clamp(subdivisions, 0, 4); // Prevent excessive subdivision
            
            var mesh = new Mesh();
            mesh.name = "XPBD_Sphere";
            
            // Start with icosahedron
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            CreateIcosahedron(vertices, triangles, radius);
            
            // Subdivide for smoother sphere
            for (int i = 0; i < subdivisions; i++)
            {
                SubdivideIcosahedron(vertices, triangles, radius);
            }
            
            // Generate UVs and normals
            var uvs = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 normalized = vertices[i].normalized;
                uvs[i] = new Vector2(
                    0.5f + Mathf.Atan2(normalized.z, normalized.x) / (2 * Mathf.PI),
                    0.5f - Mathf.Asin(normalized.y) / Mathf.PI
                );
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            Debug.Log($"Generated XPBD sphere: {vertices.Count} vertices, {triangles.Count/3} triangles");
            
            return mesh;
        }
        
        static void CreateIcosahedron(List<Vector3> vertices, List<int> triangles, float radius)
        {
            float phi = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f; // Golden ratio
            float invNorm = 1.0f / Mathf.Sqrt(phi * phi + 1.0f);
            
            // 12 vertices of icosahedron
            vertices.AddRange(new Vector3[]
            {
                new Vector3(-1,  phi, 0).normalized * radius,
                new Vector3( 1,  phi, 0).normalized * radius,
                new Vector3(-1, -phi, 0).normalized * radius,
                new Vector3( 1, -phi, 0).normalized * radius,
                new Vector3(0, -1,  phi).normalized * radius,
                new Vector3(0,  1,  phi).normalized * radius,
                new Vector3(0, -1, -phi).normalized * radius,
                new Vector3(0,  1, -phi).normalized * radius,
                new Vector3( phi, 0, -1).normalized * radius,
                new Vector3( phi, 0,  1).normalized * radius,
                new Vector3(-phi, 0, -1).normalized * radius,
                new Vector3(-phi, 0,  1).normalized * radius
            });
            
            // 20 faces of icosahedron
            int[][] faces = new int[][]
            {
                new int[]{0, 11, 5}, new int[]{0, 5, 1}, new int[]{0, 1, 7}, new int[]{0, 7, 10}, new int[]{0, 10, 11},
                new int[]{1, 5, 9}, new int[]{5, 11, 4}, new int[]{11, 10, 2}, new int[]{10, 7, 6}, new int[]{7, 1, 8},
                new int[]{3, 9, 4}, new int[]{3, 4, 2}, new int[]{3, 2, 6}, new int[]{3, 6, 8}, new int[]{3, 8, 9},
                new int[]{4, 9, 5}, new int[]{2, 4, 11}, new int[]{6, 2, 10}, new int[]{8, 6, 7}, new int[]{9, 8, 1}
            };
            
            foreach (int[] face in faces)
            {
                triangles.AddRange(face);
            }
        }
        
        static void SubdivideIcosahedron(List<Vector3> vertices, List<int> triangles, float radius)
        {
            var newTriangles = new List<int>();
            var midpointCache = new Dictionary<(int, int), int>();
            
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];
                
                int a = GetMidpoint(v0, v1, vertices, midpointCache, radius);
                int b = GetMidpoint(v1, v2, vertices, midpointCache, radius);
                int c = GetMidpoint(v2, v0, vertices, midpointCache, radius);
                
                // Create 4 new triangles
                newTriangles.AddRange(new[] { v0, a, c });
                newTriangles.AddRange(new[] { v1, b, a });
                newTriangles.AddRange(new[] { v2, c, b });
                newTriangles.AddRange(new[] { a, b, c });
            }
            
            triangles.Clear();
            triangles.AddRange(newTriangles);
        }
        
        static int GetMidpoint(int v0, int v1, List<Vector3> vertices, Dictionary<(int, int), int> cache, float radius)
        {
            var key = v0 < v1 ? (v0, v1) : (v1, v0);
            
            if (cache.TryGetValue(key, out int midpointIndex))
            {
                return midpointIndex;
            }
            
            Vector3 midpoint = ((vertices[v0] + vertices[v1]) * 0.5f).normalized * radius;
            vertices.Add(midpoint);
            midpointIndex = vertices.Count - 1;
            cache[key] = midpointIndex;
            
            return midpointIndex;
        }
        
        /// <summary>
        /// Generate connection information for constraints
        /// Returns edges suitable for distance constraints
        /// </summary>
        public static List<(int, int)> GenerateSphereEdges(Mesh mesh)
        {
            var edges = new HashSet<(int, int)>();
            var triangles = mesh.triangles;
            
            // Add structural edges from triangulation
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];
                
                AddEdge(edges, v0, v1);
                AddEdge(edges, v1, v2);
                AddEdge(edges, v2, v0);
            }
            
            return new List<(int, int)>(edges);
        }
        
        static void AddEdge(HashSet<(int, int)> edges, int v0, int v1)
        {
            if (v0 != v1)
            {
                var edge = v0 < v1 ? (v0, v1) : (v1, v0);
                edges.Add(edge);
            }
        }
    }
}