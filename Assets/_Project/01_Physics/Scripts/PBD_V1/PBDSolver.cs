// Assets/_Project/01_Physics/Scripts/PBD/PBDSolver.cs

using System.Collections.Generic;
using System.Linq;
using _Project._01_Physics.Scripts.PBD_V1.Constraints;
using UnityEngine;

namespace _Project._01_Physics.Scripts.PBD_V1
{
    /// <summary>
    /// Improved PBD solver with fracture mechanics and better stability
    /// </summary>
    public class PBDSolver
    {
        public List<PBDParticle> Particles { get; private set; }
        public List<PBDConstraint> Constraints { get; private set; }

        // Solver settings
        public int ConstraintIterations = 4;
        public float GlobalStiffness = 0.8f;
        public float GlobalDamping = 0.99f;
        public Vector3 Gravity = new Vector3(0, -9.81f, 0);

        // Fracture settings
        public bool EnableFracture = false;
        public float GlobalFractureThreshold = 10f;
        public float StressDecayRate = 0.95f;

        // Performance monitoring
        public int LastIterationsUsed { get; private set; }
        public float LastSolveTime { get; private set; }

        // Constraint organization
        private List<DistanceConstraint> _distanceConstraints;
        private List<GroundConstraint> _groundConstraints;
        private List<VolumeConstraint> _volumeConstraints;
        private List<ShapeMemoryConstraint> _shapeMemoryConstraints;
        private List<SphereVolumeConstraint> _volumePreservationConstraints;
        private List<RigidDistanceConstraint> _rigidDistanceConstraints;


        // Fracture management
        private List<PBDConstraint> _brokenConstraints;
        private HashSet<int> _fracturedParticles;

        public PBDSolver()
        {
            Particles = new List<PBDParticle>();
            Constraints = new List<PBDConstraint>();
            _distanceConstraints = new List<DistanceConstraint>();
            _groundConstraints = new List<GroundConstraint>();
            _volumeConstraints = new List<VolumeConstraint>();
            _brokenConstraints = new List<PBDConstraint>();
            _fracturedParticles = new HashSet<int>();
            _shapeMemoryConstraints = new List<ShapeMemoryConstraint>();
            _volumePreservationConstraints = new List<SphereVolumeConstraint>();
            _rigidDistanceConstraints = new List<RigidDistanceConstraint>();
        }

        public void InitializeFromMesh(Mesh mesh, float totalMass, bool enableFracture = false)
        {
            if (mesh == null)
            {
                Debug.LogError("PBDSolver: Cannot initialize from null mesh");
                return;
            }

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            if (vertices.Length == 0)
            {
                Debug.LogError("PBDSolver: Mesh has no vertices");
                return;
            }

            EnableFracture = enableFracture;

            // Clear existing data
            ClearAll();

            // Create particles
            float massPerParticle = totalMass / vertices.Length;
            for (int i = 0; i < vertices.Length; i++)
            {
                AddParticle(vertices[i], massPerParticle, i);
            }

            // Create constraints based on fracture setting
            if (enableFracture)
            {
                CreateBreakableConstraints(mesh);
            }
            else
            {
                CreateRigidConstraints(mesh);
            }

            Debug.Log(
                $"PBD Solver initialized: {Particles.Count} particles, {Constraints.Count} constraints, Fracture: {enableFracture}");
        }

        private void CreateRigidConstraints(Mesh mesh)
        {
            var triangles = mesh.triangles;
            var edgeSet = new HashSet<(int, int)>();

            // Create RIGID constraints from triangle edges
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                // Add triangle edges as RIGID constraints
                AddRigidEdgeConstraint(v0, v1, edgeSet, 1.0f);
                AddRigidEdgeConstraint(v1, v2, edgeSet, 1.0f);
                AddRigidEdgeConstraint(v2, v0, edgeSet, 1.0f);
            }

            Debug.Log($"Created {_rigidDistanceConstraints.Count} RIGID constraints for elastic object");
        }

        private void CreateBreakableConstraints(Mesh mesh)
        {
            // Use the existing CreateStructuralConstraints method for brittle objects
            CreateStructuralConstraints(mesh, true);
        }

        private void AddRigidEdgeConstraint(int a, int b, HashSet<(int, int)> edgeSet, float stiffness)
        {
            if (a == b) return;

            int min = Mathf.Min(a, b);
            int max = Mathf.Max(a, b);

            if (edgeSet.Add((min, max)))
            {
                float restLength = Vector3.Distance(Particles[a].Position, Particles[b].Position);
                if (restLength > 0.001f)
                {
                    var constraint = new RigidDistanceConstraint(a, b, restLength, stiffness);
                    AddConstraint(constraint);
                    _rigidDistanceConstraints.Add(constraint);
                }
            }
        }

// Update ClearAll method:
        private void ClearAll()
        {
            Particles.Clear();
            Constraints.Clear();
            _distanceConstraints.Clear();
            _groundConstraints.Clear();
            _volumeConstraints.Clear();
            _rigidDistanceConstraints.Clear(); // Add this line
            _brokenConstraints.Clear();
            _fracturedParticles.Clear();
        }

        private void CreateStructuralConstraints(Mesh mesh, bool enableFracture)
        {
            var triangles = mesh.triangles;
            var edgeSet = new HashSet<(int, int)>();

            // Create constraints from triangle edges only (simplified approach)
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                // Add triangle edges as constraints
                AddEdgeConstraint(v0, v1, edgeSet, 1.0f, enableFracture);
                AddEdgeConstraint(v1, v2, edgeSet, 1.0f, enableFracture);
                AddEdgeConstraint(v2, v0, edgeSet, 1.0f, enableFracture);
            }
        }

        private void CreateVolumeConstraintsForFracture(Mesh mesh)
        {
            // Create tetrahedra for volume preservation and fracture
            var triangles = mesh.triangles;

            // Simple approach: create tetrahedra from surface triangles and center point
            Vector3 center = Vector3.zero;
            foreach (var particle in Particles)
            {
                center += particle.Position;
            }

            center /= Particles.Count;

            // Add center particle
            int centerIndex = AddParticle(center, 1.0f, -1);

            // Create tetrahedra from each triangle + center
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int[] tetraIndices =
                {
                    triangles[i],
                    triangles[i + 1],
                    triangles[i + 2],
                    centerIndex
                };

                var volumeConstraint = new VolumeConstraint(tetraIndices, Particles, 0.3f, EnableFracture);
                AddConstraint(volumeConstraint);
                _volumeConstraints.Add(volumeConstraint);
            }
        }

        private void AddEdgeConstraint(int a, int b, HashSet<(int, int)> edgeSet, float stiffness, bool canBreak)
        {
            if (a == b) return;

            int min = Mathf.Min(a, b);
            int max = Mathf.Max(a, b);

            if (edgeSet.Add((min, max)))
            {
                float restLength = Vector3.Distance(Particles[a].Position, Particles[b].Position);
                if (restLength > 0.001f)
                {
                    var constraint = new DistanceConstraint(a, b, restLength, stiffness, canBreak);
                    AddConstraint(constraint);
                    _distanceConstraints.Add(constraint);
                }
            }
        }

        /// <summary>
        /// Main update loop with fracture processing
        /// </summary>
        public void Update(float deltaTime)
        {
            if (Particles.Count == 0) return;

            float startTime = Time.realtimeSinceStartup;

            // Clamp timestep for stability
            deltaTime = Mathf.Clamp(deltaTime, 0.001f, 0.02f);

            // Debug: Log gravity application for first few frames
            if (Time.frameCount < 100)
            {
                Debug.Log($"Frame {Time.frameCount}: Applying gravity {Gravity} to {Particles.Count} particles");
            }

            // PBD Algorithm
            ApplyExternalForces(deltaTime);
            PredictPositions(deltaTime);
            SolveConstraints();
            UpdateVelocitiesAndPositions(deltaTime);
            ApplyDamping();

            // Process fracture only if enabled
            if (EnableFracture)
            {
                ProcessFracture();
                DecayStress();
            }

            LastSolveTime = Time.realtimeSinceStartup - startTime;
        }

        private void ResetAllStress()
        {
            foreach (var particle in Particles.Where(particle => particle.IsActive))
            {
                particle.ResetStress();
            }
        }

        private void ApplyExternalForces(float deltaTime)
        {
            foreach (var particle in Particles)
            {
                if (!particle.IsFixed && particle.IsActive)
                {
                    // Apply gravity
                    particle.Velocity += Gravity * deltaTime;

                    // Debug for first few particles in early frames
                    if (Time.frameCount < 50 && particle.VertexIndex < 3)
                    {
                        Debug.Log($"Particle {particle.VertexIndex}: Velocity after gravity = {particle.Velocity}");
                    }
                }
            }
        }

        private void PredictPositions(float deltaTime)
        {
            foreach (var particle in Particles.Where(particle => particle.IsActive))
            {
                particle.PredictPosition(Vector3.zero, deltaTime);
            }
        }

        private void SolveConstraints()
        {
            LastIterationsUsed = 0;
    
            // Determine iterations based on object type
            int actualIterations = EnableFracture ? ConstraintIterations : ConstraintIterations * 2;
    
            for (int iteration = 0; iteration < actualIterations; iteration++)
            {
                LastIterationsUsed++;
        
                // PHASE 1: Ground Collision (highest priority)
                foreach (var constraint in _groundConstraints)
                {
                    if (constraint.IsActive)
                    {
                        SolveGroundConstraintWithContext(constraint as GroundConstraint);
                    }
                }
        
                // PHASE 2: Structural Constraints
                if (EnableFracture)
                {
                    // Use breakable distance constraints for brittle objects
                    foreach (var constraint in _distanceConstraints)
                    {
                        if (constraint.IsActive)
                        {
                            constraint.SolveConstraint(Particles, GlobalStiffness);
                        }
                    }
                }
                else
                {
                    // Use RIGID constraints for elastic objects - solve multiple times per iteration
                    for (int rigidPass = 0; rigidPass < 2; rigidPass++)
                    {
                        foreach (var constraint in _rigidDistanceConstraints)
                        {
                            if (constraint.IsActive)
                            {
                                constraint.SolveConstraint(Particles, GlobalStiffness);
                            }
                        }
                    }
                }
        
                // PHASE 3: Volume constraints (if any)
                foreach (var constraint in _volumeConstraints)
                {
                    if (constraint.IsActive)
                    {
                        constraint.SolveConstraint(Particles, GlobalStiffness * 0.5f);
                    }
                }
            }
        }
        
        private Vector3 CalculateCenterOfMass()
        {
            Vector3 center = Vector3.zero;
            float totalMass = 0f;

            foreach (var particle in Particles)
            {
                if (particle.IsActive)
                {
                    float mass = particle.InverseMass > 0 ? 1f / particle.InverseMass : 0f;
                    center += particle.PredictedPosition * mass;
                    totalMass += mass;
                }
            }

            return totalMass > 0 ? center / totalMass : Vector3.zero;
        }

        private void UpdateShapeConstraintCenters(Vector3 centerOfMass)
        {
            foreach (var constraint in _shapeMemoryConstraints)
            {
                constraint.UpdateCenterOfMass(centerOfMass);
            }
        }

// Add method to create shape memory constraints for elastic objects
        public void AddShapeMemoryConstraints(float originalRadius)
        {
            if (EnableFracture) return; // Only for elastic objects

            Vector3 initialCenter = CalculateCenterOfMass();

            // Create shape memory for all particles
            for (int i = 0; i < Particles.Count; i++)
            {
                var particle = Particles[i];
                Vector3 localPosition = particle.OriginalPosition - initialCenter;

                var memoryConstraint = new ShapeMemoryConstraint(i, localPosition, 0.5f);
                _shapeMemoryConstraints.Add(memoryConstraint);
                Constraints.Add(memoryConstraint);
            }

            // Create volume preservation
            var surfaceIndices = new List<int>();
            for (int i = 0; i < Particles.Count; i++)
            {
                surfaceIndices.Add(i); // For now, treat all particles as surface
            }

            var volumeConstraint = new SphereVolumeConstraint(surfaceIndices, originalRadius, 0.6f);
            _volumePreservationConstraints.Add(volumeConstraint);
            Constraints.Add(volumeConstraint);

            Debug.Log($"Added {_shapeMemoryConstraints.Count} shape memory constraints and 1 volume constraint");
        }

        private void SolveGroundConstraintWithContext(GroundConstraint groundConstraint)
        {
            foreach (var particle in Particles)
            {
                if (particle.IsFixed || !particle.IsActive) continue;

                // Check if particle is below ground
                if (particle.PredictedPosition.y < groundConstraint.GroundY)
                {
                    // Position correction
                    particle.PredictedPosition.y = groundConstraint.GroundY + 0.001f;

                    // Velocity correction for bounce
                    if (particle.Velocity.y < 0)
                    {
                        float impactSpeed = Mathf.Abs(particle.Velocity.y);

                        // Apply restitution
                        float bounceSpeed = impactSpeed * groundConstraint.Restitution;
                        particle.Velocity.y = bounceSpeed;

                        // Apply friction
                        float frictionReduction = 1f - groundConstraint.Friction;
                        particle.Velocity.x *= frictionReduction;
                        particle.Velocity.z *= frictionReduction;

                        // ONLY add stress if fracture is enabled AND impact is significant
                        if (EnableFracture && impactSpeed > 2f)
                        {
                            float impactStress = impactSpeed * 0.05f;
                            particle.AddStress(impactStress);
                        }
                    }
                }
            }
        }

        private void UpdateVelocitiesAndPositions(float deltaTime)
        {
            foreach (var particle in Particles.Where(particle => particle.IsActive))
            {
                particle.UpdateFromPredicted(deltaTime);
            }
        }

        private void ApplyDamping()
        {
            foreach (var particle in Particles.Where(particle => particle.IsActive))
            {
                particle.ApplyDamping(GlobalDamping);
            }
        }

        /// <summary>
        /// Process fracture mechanics
        /// </summary>
        private void ProcessFracture()
        {
            if (!EnableFracture) return;

            // Check for constraint breaking
            var constraintsToRemove = new List<PBDConstraint>();

            foreach (var constraint in Constraints)
            {
                if (constraint.IsActive && constraint.ShouldBreak())
                {
                    constraint.Break();
                    constraintsToRemove.Add(constraint);
                    _brokenConstraints.Add(constraint);
                }
            }

            // Remove broken constraints
            foreach (var constraint in constraintsToRemove)
            {
                Constraints.Remove(constraint);

                if (constraint is DistanceConstraint dc)
                    _distanceConstraints.Remove(dc);
                else if (constraint is VolumeConstraint vc)
                    _volumeConstraints.Remove(vc);
            }

            // Check for particle fracture
            foreach (var particle in Particles)
            {
                if (particle.IsActive && particle.StressAccumulation > GlobalFractureThreshold)
                {
                    FractureParticle(particle);
                }
            }
        }

        private void FractureParticle(PBDParticle particle)
        {
            if (_fracturedParticles.Contains(particle.VertexIndex)) return;

            _fracturedParticles.Add(particle.VertexIndex);
            particle.Deactivate();

            // Remove all constraints connected to this particle
            var constraintsToRemove = new List<PBDConstraint>();

            foreach (var constraint in _distanceConstraints)
            {
                if (constraint.ParticleA == particle.VertexIndex || constraint.ParticleB == particle.VertexIndex)
                {
                    constraint.Break();
                    constraintsToRemove.Add(constraint);
                }
            }

            foreach (var constraint in constraintsToRemove)
            {
                Constraints.Remove(constraint);
                _distanceConstraints.Remove((DistanceConstraint)constraint);
            }

            Debug.Log(
                $"Particle {particle.VertexIndex} fractured due to excessive stress: {particle.StressAccumulation:F2}");
        }

        private void DecayStress()
        {
            foreach (var particle in Particles)
            {
                if (particle.IsActive)
                {
                    particle.StressAccumulation *= StressDecayRate;
                }
            }
        }

        public int AddParticle(Vector3 position, float mass = 1f, int vertexIndex = -1)
        {
            var particle = new PBDParticle(position, mass, vertexIndex);
            Particles.Add(particle);
            return Particles.Count - 1;
        }

        public void AddConstraint(PBDConstraint constraint)
        {
            Constraints.Add(constraint);
        }

        public void AddGroundConstraint(float groundY, float restitution = 0.3f, float friction = 0.4f)
        {
            var groundConstraint = new GroundConstraint(groundY, restitution, friction);
            AddConstraint(groundConstraint);
            _groundConstraints.Add(groundConstraint);
        }

        public void FixParticles(Vector3 center, float radius)
        {
            foreach (var particle in Particles)
            {
                if (Vector3.Distance(particle.Position, center) < radius)
                {
                    particle.SetFixed(true);
                }
            }
        }

        public void ApplyImpulse(Vector3 center, Vector3 impulse, float radius)
        {
            foreach (var particle in Particles)
            {
                if (particle.IsFixed || !particle.IsActive) continue;

                float distance = Vector3.Distance(particle.Position, center);
                if (distance < radius)
                {
                    float falloff = 1f - (distance / radius);
                    particle.Velocity += impulse * falloff * particle.InverseMass;

                    // Add stress from impact
                    float impactStress = impulse.magnitude * falloff * 0.1f;
                    particle.AddStress(impactStress);
                }
            }
        }

        public void Reset()
        {
            foreach (var particle in Particles)
            {
                particle.Position = particle.OriginalPosition;
                particle.PredictedPosition = particle.OriginalPosition;
                particle.Velocity = Vector3.zero;
                particle.ResetStress();
                particle.IsActive = true;
            }

            // Reactivate broken constraints
            foreach (var constraint in _brokenConstraints)
            {
                constraint.IsActive = true;
                Constraints.Add(constraint);

                if (constraint is DistanceConstraint dc)
                {
                    dc.IsBroken = false;
                    _distanceConstraints.Add(dc);
                }
            }

            _brokenConstraints.Clear();
            _fracturedParticles.Clear();
        }

        public (int particles, int constraints, int activeConstraints, float solveTime, int iterations, int
            brokenConstraints) GetStatistics()
        {
            int activeConstraints = Constraints.Count(c => c.IsActive);
            int activeParticles = Particles.Count(p => p.IsActive);
            return (activeParticles, Constraints.Count, activeConstraints, LastSolveTime, LastIterationsUsed,
                _brokenConstraints.Count);
        }

        public bool ValidateState()
        {
            foreach (var particle in Particles)
            {
                if (!particle.IsActive) continue;

                if (float.IsNaN(particle.Position.x) || float.IsNaN(particle.Position.y) ||
                    float.IsNaN(particle.Position.z))
                {
                    Debug.LogError("PBD Solver: NaN position detected!");
                    return false;
                }

                if (float.IsNaN(particle.Velocity.x) || float.IsNaN(particle.Velocity.y) ||
                    float.IsNaN(particle.Velocity.z))
                {
                    Debug.LogError("PBD Solver: NaN velocity detected!");
                    return false;
                }
            }

            return true;
        }

        public List<int> GetFracturedParticles()
        {
            return new List<int>(_fracturedParticles);
        }
    }
}