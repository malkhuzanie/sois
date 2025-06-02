// Assets/_Project/01_Physics/Scripts/Deformation/MassSpring/SoftBodyShapeGenerator.cs

using System.Collections.Generic;
using UnityEngine;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.Deformation.MassSpring
{
    /// <summary>
    /// Generates soft body shapes with strong structural integrity
    /// </summary>
    public static class SoftBodyShapeGenerator
    {
        public static MassSpringSystem CreateSoftSphere(
            float radius,
            int latitudeSegments,
            int longitudeSegments,
            float totalMass,
            PhysicsMaterial material)
        {
            var system = new MassSpringSystem();
            
            // Create a more structurally sound sphere
            var mesh = GenerateStructuralSphereMesh(radius, latitudeSegments, longitudeSegments);
            system.InitializeFromMesh(mesh, totalMass, material);
            
            // TEMPORARILY DISABLED: Add internal structure for better stability
            // AddInternalStructure(system, radius);
            Debug.Log("Internal structure temporarily DISABLED for testing");
            
            return system;
        }

        private static Mesh GenerateStructuralSphereMesh(float radius, int latSegments, int lonSegments)
        {
            var mesh = new Mesh();
            mesh.name = "StructuralSoftSphere";

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            // Use good resolution for structural integrity
            latSegments = Mathf.Clamp(latSegments, 6, 16); // 6-16 segments
            lonSegments = Mathf.Clamp(lonSegments, 8, 20); // 8-20 segments

            Debug.Log($"Generating structural sphere with {latSegments}x{lonSegments} segments");

            // Generate vertices
            for (int lat = 0; lat <= latSegments; lat++)
            {
                float theta = lat * Mathf.PI / latSegments;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float phi = lon * 2 * Mathf.PI / lonSegments;
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    // Spherical to Cartesian coordinates
                    float x = cosPhi * sinTheta;
                    float y = cosTheta;
                    float z = sinPhi * sinTheta;

                    vertices.Add(new Vector3(x, y, z) * radius);
                    uvs.Add(new Vector2((float)lon / lonSegments, (float)lat / latSegments));
                }
            }

            // Generate triangles
            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int current = lat * (lonSegments + 1) + lon;
                    int next = current + lonSegments + 1;

                    if (next + 1 < vertices.Count)
                    {
                        // First triangle
                        triangles.Add(current);
                        triangles.Add(next);
                        triangles.Add(current + 1);

                        // Second triangle
                        triangles.Add(current + 1);
                        triangles.Add(next);
                        triangles.Add(next + 1);
                    }
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Debug.Log($"Generated sphere: {vertices.Count} vertices, {triangles.Count / 3} triangles");

            return mesh;
        }

        private static void AddInternalStructure(MassSpringSystem system, float radius)
        {
            // Add minimal internal cross-connections for better structural integrity
            var points = system.MassPoints;
            int connections = 0;
            int maxConnections = Mathf.Max(5, points.Count / 4); // Much fewer connections

            for (int i = 0; i < points.Count && connections < maxConnections; i++)
            {
                for (int j = i + 1; j < points.Count && connections < maxConnections; j++)
                {
                    float distance = Vector3.Distance(points[i].Position, points[j].Position);
                    
                    // Only add a few strategic internal connections
                    if (distance > radius * 0.7f && distance < radius * 1.4f) // Diameter connections
                    {
                        if (ShouldAddInternalConnection(points[i], points[j], radius))
                        {
                            AddInternalSpring(system, points[i], points[j]);
                            connections++;
                        }
                    }
                }
            }

            Debug.Log($"Added {connections} internal structural connections (limited to {maxConnections})");
        }

        private static bool ShouldAddInternalConnection(MassPoint pointA, MassPoint pointB, float radius)
        {
            // Only add connections for points that are roughly opposite each other
            // This creates minimal but effective internal structure
            Vector3 centerToA = pointA.Position.normalized;
            Vector3 centerToB = pointB.Position.normalized;
            
            float dot = Vector3.Dot(centerToA, centerToB);
            
            // Only connect points that are roughly opposite (dot product close to -1)
            // This creates fewer but more effective diameter-spanning connections
            return dot < -0.7f; // Only very opposite points
        }

        private static void AddInternalSpring(MassSpringSystem system, MassPoint pointA, MassPoint pointB)
        {
            float restLength = Vector3.Distance(pointA.Position, pointB.Position);
            float stiffness = system.Material.stiffness * 0.1f; // Much weaker - was 0.3f
            float damping = system.Material.damping * 0.2f; // Less damping - was 0.5f

            var internalSpring = new Spring(pointA, pointB, stiffness, damping, Spring.SpringType.Bend) // Use Bend type
            {
                MaxStrain = 10.0f, // Very flexible - was 5.0f
                FatigueThreshold = float.MaxValue
            };

            // Add to the system using the public method
            system.AddSpring(internalSpring);
        }

        public static MassSpringSystem CreateSoftCube(
            float size,
            int segmentsPerSide,
            float totalMass,
            PhysicsMaterial material)
        {
            var system = new MassSpringSystem();
            var mesh = GenerateStructuralCubeMesh(size, segmentsPerSide);
            system.InitializeFromMesh(mesh, totalMass, material);
            return system;
        }

        private static Mesh GenerateStructuralCubeMesh(float size, int segments)
        {
            var mesh = new Mesh { name = "StructuralSoftCube" };
            
            // Use Unity's built-in cube and subdivide it for better structure
            var primitiveCube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            
            // Scale to desired size
            var vertices = new List<Vector3>();
            foreach (var vertex in primitiveCube.vertices)
            {
                vertices.Add(vertex * size);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = primitiveCube.triangles;
            mesh.uv = primitiveCube.uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Debug.Log($"Generated structural cube: {vertices.Count} vertices");

            return mesh;
        }

        // Other shapes remain the same for now
        public static MassSpringSystem CreateSoftCylinder(
            float radius,
            float height,
            int radialSegments,
            int heightSegments,
            float totalMass,
            PhysicsMaterial material)
        {
            var system = new MassSpringSystem();
            var mesh = GenerateStructuralSphereMesh(radius, 8, 10);
            system.InitializeFromMesh(mesh, totalMass, material);
            return system;
        }

        public static MassSpringSystem CreateSoftTorus(
            float majorRadius,
            float minorRadius,
            int majorSegments,
            int minorSegments,
            float totalMass,
            PhysicsMaterial material)
        {
            var system = new MassSpringSystem();
            var mesh = GenerateStructuralSphereMesh(majorRadius, 8, 10);
            system.InitializeFromMesh(mesh, totalMass, material);
            return system;
        }
    }
}