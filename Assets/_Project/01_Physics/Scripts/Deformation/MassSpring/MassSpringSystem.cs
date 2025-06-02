// Assets/_Project/01_Physics/Scripts/Deformation/MassSpring/MassSpringSystem.cs

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project._00_Core.Scripts.DataStructures;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.Deformation.MassSpring
{
    public class MassSpringSystem
    {
        public List<MassPoint> MassPoints { get; }
        private readonly List<Spring> _springs;
        private Mesh _originalMesh;
        private Mesh _deformedMesh;
        private Vector3[] _originalVertices;
        private Vector3[] _deformedVertices;

        // BALANCED physics parameters (not ultra-conservative)
        public float GlobalDamping { get; set; } = 0.98f; // Reasonable damping
        public Vector3 Gravity { get; set; } = new(0, -9.81f, 0); // Normal gravity

        public PhysicsMaterial Material { get; set; }
        public enum IntegrationMethod { Euler, Verlet, RungeKutta4 }
        public IntegrationMethod Method { get; set; } = IntegrationMethod.Verlet;

        private Vector3[] _previousPositions;
        private bool _physicsEnabled = true;
        
        // REASONABLE stability parameters
        private float _maxForcePerMass = 100f; // Higher force limit
        private float _maxAcceleration = 50f; // Higher acceleration limit  
        private float _maxVelocity = 20f; // Higher velocity limit
        private float _minSpringLength = 0.001f; // Smaller minimum
        private int _constraintIterations = 2; // Fewer iterations for performance

        public MassSpringSystem()
        {
            MassPoints = new List<MassPoint>();
            _springs = new List<Spring>();
        }

        public void InitializeFromMesh(Mesh mesh, float totalMass, PhysicsMaterial material)
        {
            _originalMesh = mesh;
            Material = material;

            if (mesh == null)
            {
                Debug.LogError("InitializeFromMesh: Input mesh is null!");
                return;
            }

            _deformedMesh = Object.Instantiate(mesh);
            _deformedMesh.name = mesh.name + "_Deformed";

            _originalVertices = mesh.vertices;
            _deformedVertices = new Vector3[_originalVertices.Length];

            if (_originalVertices.Length == 0)
            {
                Debug.LogError($"InitializeFromMesh: Mesh '{mesh.name}' has no vertices!");
                return;
            }

            _originalVertices.CopyTo(_deformedVertices, 0);

            // Create mass points
            CreateMassPoints(totalMass);

            // Initialize previous positions for Verlet integration
            _previousPositions = new Vector3[MassPoints.Count];
            for (int i = 0; i < MassPoints.Count; i++)
            {
                _previousPositions[i] = MassPoints[i].Position;
            }

            // Create springs with reasonable parameters
            CreateReasonableSprings(mesh);
            
            Debug.Log($"MassSpringSystem initialized: {MassPoints.Count} points, {_springs.Count} springs");
            ValidateInitialization();
        }

        private void CreateMassPoints(float totalMass)
        {
            float massPerPoint = totalMass / _originalVertices.Length;

            for (int i = 0; i < _originalVertices.Length; i++)
            {
                var massPoint = new MassPoint(i, _originalVertices[i], massPerPoint, i);
                MassPoints.Add(massPoint);
            }
        }

        private void CreateReasonableSprings(Mesh mesh)
        {
            var springPairs = new HashSet<(int, int)>();
            int[] triangles = mesh.triangles;
            
            // Create structural springs from triangle edges
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                if (v0 >= MassPoints.Count || v1 >= MassPoints.Count || v2 >= MassPoints.Count)
                {
                    Debug.LogError($"Invalid vertex index in triangle. v0:{v0}, v1:{v1}, v2:{v2}, Points:{MassPoints.Count}");
                    continue;
                }

                // Add triangle edges
                TryAddSpring(v0, v1, Spring.SpringType.Structural, springPairs);
                TryAddSpring(v1, v2, Spring.SpringType.Structural, springPairs);
                TryAddSpring(v2, v0, Spring.SpringType.Structural, springPairs);
            }

            // Add some shear springs for stability (diagonal connections)
            AddShearSprings(mesh, springPairs);

            Debug.Log($"Created {_springs.Count} springs (structural + shear)");
        }

        private void AddShearSprings(Mesh mesh, HashSet<(int, int)> springPairs)
        {
            int[] triangles = mesh.triangles;
            
            // For each triangle, find adjacent triangles and create cross-connections
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                // Find triangles that share edges with this triangle
                for (int j = i + 3; j < triangles.Length; j += 3)
                {
                    int u0 = triangles[j];
                    int u1 = triangles[j + 1];
                    int u2 = triangles[j + 2];

                    // Check if triangles share an edge and add cross-connection
                    if (SharesEdge(v0, v1, v2, u0, u1, u2, out int sharedV1, out int sharedV2, out int opposite1, out int opposite2))
                    {
                        TryAddSpring(opposite1, opposite2, Spring.SpringType.Shear, springPairs);
                    }
                }
            }
        }

        private bool SharesEdge(int v0, int v1, int v2, int u0, int u1, int u2, 
                               out int sharedV1, out int sharedV2, out int opposite1, out int opposite2)
        {
            sharedV1 = sharedV2 = opposite1 = opposite2 = -1;
            
            var triangle1 = new[] { v0, v1, v2 };
            var triangle2 = new[] { u0, u1, u2 };
            
            var shared = triangle1.Intersect(triangle2).ToArray();
            
            if (shared.Length == 2)
            {
                sharedV1 = shared[0];
                sharedV2 = shared[1];
                opposite1 = triangle1.Except(shared).First();
                opposite2 = triangle2.Except(shared).First();
                return true;
            }
            
            return false;
        }

        private void TryAddSpring(int indexA, int indexB, Spring.SpringType type, HashSet<(int, int)> addedPairs)
        {
            if (indexA == indexB) return;

            int minIndex = Mathf.Min(indexA, indexB);
            int maxIndex = Mathf.Max(indexA, indexB);
            var pair = (minIndex, maxIndex);

            if (addedPairs.Add(pair))
            {
                var pointA = MassPoints[indexA];
                var pointB = MassPoints[indexB];
                float restLength = Vector3.Distance(pointA.Position, pointB.Position);

                // Skip springs that are too short
                if (restLength < _minSpringLength)
                {
                    addedPairs.Remove(pair);
                    return;
                }

                // REASONABLE stiffness and damping
                float stiffness = GetReasonableStiffness(type);
                float damping = GetReasonableDamping(type);

                var spring = new Spring(pointA, pointB, stiffness, damping, type)
                {
                    MaxStrain = type switch
                    {
                        Spring.SpringType.Structural => 3.0f,  // Can stretch to 3x original
                        Spring.SpringType.Shear => 4.0f,       // More flexible
                        Spring.SpringType.Bend => 5.0f,        // Most flexible
                        _ => 3.0f
                    },
                    FatigueThreshold = float.MaxValue
                };

                _springs.Add(spring);
            }
        }

        private float GetReasonableStiffness(Spring.SpringType type)
        {
            float baseStiffness = Material?.stiffness ?? 1000f;
            
            return type switch
            {
                Spring.SpringType.Structural => baseStiffness,
                Spring.SpringType.Shear => baseStiffness * 0.5f,
                Spring.SpringType.Bend => baseStiffness * 0.25f,
                _ => baseStiffness
            };
        }

        private float GetReasonableDamping(Spring.SpringType type)
        {
            float baseDamping = Material?.damping ?? 10f;
            
            return type switch
            {
                Spring.SpringType.Structural => baseDamping,
                Spring.SpringType.Shear => baseDamping * 0.8f,
                Spring.SpringType.Bend => baseDamping * 0.6f,
                _ => baseDamping
            };
        }

        public void Update(float deltaTime)
        {
            if (!_physicsEnabled || MassPoints.Count == 0) return;

            // Reasonable timestep limiting
            deltaTime = Mathf.Clamp(deltaTime, 0.001f, 0.02f); // Max 20ms timestep

            // Clear forces
            foreach (var point in MassPoints)
            {
                point.ClearForces();
            }

            // Apply gravity
            foreach (var point in MassPoints.Where(point => !point.IsFixed))
            {
                point.AddForce(Gravity * point.Mass);
            }

            // Apply spring forces
            foreach (var spring in _springs.Where(s => !s.IsBroken))
            {
                spring.ApplyForces();
            }

            // REASONABLE force limiting (not ultra-conservative)
            LimitForcesReasonably();

            // Update accelerations
            foreach (var point in MassPoints)
            {
                point.UpdateAcceleration();
            }

            // REASONABLE acceleration limiting
            LimitAccelerationsReasonably();

            // Integrate motion
            IntegrateVerlet(deltaTime);

            // REASONABLE velocity limiting
            LimitVelocitiesReasonably();

            // Apply reasonable global damping
            foreach (var point in MassPoints.Where(point => !point.IsFixed))
            {
                point.Velocity *= GlobalDamping;
            }

            // Fewer constraint iterations
            for (int iter = 0; iter < _constraintIterations; iter++)
            {
                ApplyPositionConstraints();
            }

            // Update mesh
            UpdateMeshVertices();

            // Health check less frequently
            if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
            {
                CheckPhysicsHealth();
            }
        }

        private void LimitForcesReasonably()
        {
            foreach (var point in MassPoints.Where(point => !point.IsFixed))
            {
                Vector3 totalForce = point.Force;
                float maxForce = _maxForcePerMass * point.Mass;
                
                if (totalForce.magnitude > maxForce)
                {
                    point.Force = totalForce.normalized * maxForce;
                }
            }
        }

        private void LimitAccelerationsReasonably()
        {
            foreach (var point in MassPoints.Where(point => !point.IsFixed))
            {
                if (point.Acceleration.magnitude > _maxAcceleration)
                {
                    point.Acceleration = point.Acceleration.normalized * _maxAcceleration;
                }
            }
        }

        private void LimitVelocitiesReasonably()
        {
            foreach (var point in MassPoints.Where(point => !point.IsFixed))
            {
                if (point.Velocity.magnitude > _maxVelocity)
                {
                    point.Velocity = point.Velocity.normalized * _maxVelocity;
                }
            }
        }

        private void IntegrateVerlet(float dt)
        {
            float dtSq = dt * dt;
            
            for (int i = 0; i < MassPoints.Count; i++)
            {
                var point = MassPoints[i];
                if (!point.IsFixed)
                {
                    Vector3 currentPosition = point.Position;
                    Vector3 acceleration = point.Acceleration;

                    // Standard Verlet integration (not ultra-conservative)
                    Vector3 newPosition = currentPosition + 
                                        (currentPosition - _previousPositions[i]) * GlobalDamping + 
                                        acceleration * dtSq;

                    _previousPositions[i] = currentPosition;
                    point.Position = newPosition;
                    point.Velocity = (newPosition - currentPosition) / dt;
                }
                else
                {
                    _previousPositions[i] = point.Position;
                    point.Velocity = Vector3.zero;
                    point.Acceleration = Vector3.zero;
                }
            }
        }

        private void ApplyPositionConstraints()
        {
            // Reasonable position-based constraint solver
            foreach (var spring in _springs.Where(s => !s.IsBroken && s.Type == Spring.SpringType.Structural))
            {
                var pointA = spring.PointA;
                var pointB = spring.PointB;
                
                if (pointA.IsFixed && pointB.IsFixed) continue;

                Vector3 delta = pointB.Position - pointA.Position;
                float currentLength = delta.magnitude;
                
                if (currentLength < 0.001f) continue;

                float restLength = spring.RestLength;
                float difference = currentLength - restLength;
                
                // Only correct significant differences
                if (Mathf.Abs(difference) > restLength * 0.01f) // 1% threshold
                {
                    Vector3 direction = delta / currentLength;
                    Vector3 correction = direction * difference * 0.5f; // 50% correction

                    if (!pointA.IsFixed)
                    {
                        pointA.Position += correction * (pointB.IsFixed ? 1f : 0.5f);
                    }
                    if (!pointB.IsFixed)
                    {
                        pointB.Position -= correction * (pointA.IsFixed ? 1f : 0.5f);
                    }
                }
            }
        }

        private void CheckPhysicsHealth()
        {
            bool hasNaN = MassPoints.Any(p => 
                float.IsNaN(p.Position.x) || float.IsNaN(p.Position.y) || float.IsNaN(p.Position.z) ||
                float.IsNaN(p.Velocity.x) || float.IsNaN(p.Velocity.y) || float.IsNaN(p.Velocity.z));
                
            if (hasNaN)
            {
                Debug.LogError("Physics Health: NaN detected! Resetting system.");
                Reset();
                return;
            }

            // Check for exploded positions
            bool hasExploded = MassPoints.Any(p => p.Position.magnitude > 1000f);
            if (hasExploded)
            {
                Debug.LogWarning("Physics Health: Extreme positions detected. Resetting.");
                Reset();
            }
        }

        private void UpdateMeshVertices()
        {
            if (MassPoints.Count == 0 || _deformedVertices.Length != MassPoints.Count || !_deformedMesh)
            {
                return;
            }

            for (int i = 0; i < MassPoints.Count; i++)
            {
                _deformedVertices[i] = MassPoints[i].Position;
            }

            _deformedMesh.vertices = _deformedVertices;
            _deformedMesh.RecalculateNormals();
            _deformedMesh.RecalculateBounds();
        }

        private void ValidateInitialization()
        {
            if (MassPoints.Count == 0 && _originalVertices.Length > 0)
            {
                Debug.LogError("Validation Error: No mass points created!");
            }
            if (_springs.Count == 0 && MassPoints.Count > 1)
            {
                Debug.LogWarning("Validation Warning: No springs created.");
            }

            float avgSpringLength = _springs.Count > 0 ? _springs.Average(s => s.RestLength) : 0;
            Debug.Log($"Average spring length: {avgSpringLength:F4}");
            Debug.Log($"Spring types: Structural={_springs.Count(s => s.Type == Spring.SpringType.Structural)}, " +
                     $"Shear={_springs.Count(s => s.Type == Spring.SpringType.Shear)}");
        }

        public void ApplyImpulse(Vector3 localPosition, Vector3 localImpulse, float radius)
        {
            foreach (var point in MassPoints)
            {
                if (point.IsFixed) continue;

                float distanceSq = (point.Position - localPosition).sqrMagnitude;
                if (distanceSq < radius * radius)
                {
                    float falloff = 1.0f - Mathf.Sqrt(distanceSq) / radius;
                    Vector3 impulse = localImpulse * (falloff * point.InverseMass);
                    
                    // Reasonable impulse limiting
                    if (impulse.magnitude > _maxVelocity)
                    {
                        impulse = impulse.normalized * _maxVelocity;
                    }
                    
                    point.Velocity += impulse;
                }
            }
        }

        public Mesh GetDeformedMesh() => _deformedMesh;

        public (int totalSprings, int brokenSprings, float averageStress) GetStatistics()
        {
            if (_springs.Count == 0) return (0, 0, 0);
            
            int broken = _springs.Count(s => s.IsBroken);
            float totalStress = _springs.Sum(s => s.GetStressLevel());
            return (_springs.Count, broken, totalStress / _springs.Count);
        }

        public void Reset()
        {
            foreach (var point in MassPoints)
            {
                point.Reset();
            }

            foreach (var spring in _springs)
            {
                spring.Repair();
            }

            // Reset previous positions
            for (int i = 0; i < MassPoints.Count; i++)
            {
                _previousPositions[i] = MassPoints[i].Position;
            }

            _physicsEnabled = true;
            UpdateMeshVertices();
        }

        public void AddSpring(Spring spring)
        {
            if (spring != null)
            {
                _springs.Add(spring);
            }
        }

        public void FixPoints(Vector3 localPosition, float radius)
        {
            foreach (var point in MassPoints.Where(point => Vector3.Distance(point.Position, localPosition) < radius))
            {
                point.IsFixed = true;
                point.Velocity = Vector3.zero;
            }
        }
    }
}