// Assets/_Project/01_Physics/Scripts/PBD/PBDSolver.cs

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace _Project._01_Physics.Scripts.PBD
{
    /// <summary>
    /// Enhanced PBD solver with better constraint generation
    /// </summary>
    public class PBDSolver
    {
        public List<PBDParticle> Particles { get; private set; }
        public List<PBDConstraint> Constraints { get; private set; }
        
        // Solver settings
        public int ConstraintIterations = 8; // Increased for better stability
        public float GlobalStiffness = 0.9f;
        public float GlobalDamping = 0.99f;
        public Vector3 Gravity = new Vector3(0, -9.81f, 0);
        
        // Performance monitoring
        public int LastIterationsUsed { get; private set; }
        public float LastSolveTime { get; private set; }
        
        // Constraint organization
        private List<DistanceConstraint> _distanceConstraints;
        private List<GroundConstraint> _groundConstraints;
        private List<VolumeConstraint> _volumeConstraints;
        
        public PBDSolver()
        {
            Particles = new List<PBDParticle>();
            Constraints = new List<PBDConstraint>();
            _distanceConstraints = new List<DistanceConstraint>();
            _groundConstraints = new List<GroundConstraint>();
            _volumeConstraints = new List<VolumeConstraint>();
        }
        
        /// <summary>
        /// Enhanced mesh initialization with better constraint generation
        /// </summary>
        public void InitializeFromMesh(Mesh mesh, float totalMass)
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
            
            // Clear existing data
            Particles.Clear();
            Constraints.Clear();
            _distanceConstraints.Clear();
            _groundConstraints.Clear();
            _volumeConstraints.Clear();
            
            // Create particles
            float massPerParticle = totalMass / vertices.Length;
            for (int i = 0; i < vertices.Length; i++)
            {
                AddParticle(vertices[i], massPerParticle, i);
            }
            
            // Create comprehensive constraint network
            CreateStructuralConstraints(mesh);
            CreateShearConstraints(mesh);
            CreateBendingConstraints(mesh);
            
            Debug.Log($"PBD Solver initialized: {Particles.Count} particles, {Constraints.Count} constraints");
            LogConstraintBreakdown();
        }
        
        private void CreateStructuralConstraints(Mesh mesh)
        {
            var triangles = mesh.triangles;
            var edgeSet = new HashSet<(int, int)>();
            
            // Create constraints from triangle edges
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];
                
                // Add triangle edges
                AddEdgeConstraint(v0, v1, edgeSet, 1.0f, "Structural");
                AddEdgeConstraint(v1, v2, edgeSet, 1.0f, "Structural");
                AddEdgeConstraint(v2, v0, edgeSet, 1.0f, "Structural");
            }
        }
        
        private void CreateShearConstraints(Mesh mesh)
        {
            var triangles = mesh.triangles;
            var edgeSet = new HashSet<(int, int)>();
            
            // Create diagonal constraints within quads (if any)
            // This helps prevent shearing
            for (int i = 0; i < triangles.Length; i += 6) // Process pairs of triangles
            {
                if (i + 5 >= triangles.Length) break;
                
                // Find shared edge between two triangles
                int[] tri1 = { triangles[i], triangles[i + 1], triangles[i + 2] };
                int[] tri2 = { triangles[i + 3], triangles[i + 4], triangles[i + 5] };
                
                var sharedVertices = tri1.Intersect(tri2).ToArray();
                if (sharedVertices.Length == 2)
                {
                    // Find the non-shared vertices
                    int unique1 = tri1.Except(sharedVertices).FirstOrDefault();
                    int unique2 = tri2.Except(sharedVertices).FirstOrDefault();
                    
                    // Create diagonal constraint
                    AddEdgeConstraint(unique1, unique2, edgeSet, 0.8f, "Shear");
                }
            }
        }
        
        private void CreateBendingConstraints(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var edgeSet = new HashSet<(int, int)>();
            
            // Create longer-range constraints to resist bending
            for (int i = 0; i < vertices.Length; i++)
            {
                for (int j = i + 1; j < vertices.Length; j++)
                {
                    float distance = Vector3.Distance(vertices[i], vertices[j]);
                    
                    // Create bending constraints for vertices that are 2-3 edge lengths apart
                    if (distance > 0.5f && distance < 2.0f)
                    {
                        if (Random.value < 0.3f) // Only add some to avoid too many constraints
                        {
                            AddEdgeConstraint(i, j, edgeSet, 0.6f, "Bending");
                        }
                    }
                }
            }
        }
        
        private void AddEdgeConstraint(int a, int b, HashSet<(int, int)> edgeSet, float stiffness, string type)
        {
            if (a == b) return;
            
            int min = Mathf.Min(a, b);
            int max = Mathf.Max(a, b);
            
            if (edgeSet.Add((min, max)))
            {
                float restLength = Vector3.Distance(Particles[a].Position, Particles[b].Position);
                if (restLength > 0.001f)
                {
                    var constraint = new DistanceConstraint(a, b, restLength, stiffness);
                    AddConstraint(constraint);
                    _distanceConstraints.Add(constraint);
                }
            }
        }
        
        /// <summary>
        /// Main update loop with improved constraint solving
        /// </summary>
        public void Update(float deltaTime)
        {
            if (Particles.Count == 0) return;
            
            float startTime = Time.realtimeSinceStartup;
            
            // Clamp timestep for stability
            deltaTime = Mathf.Clamp(deltaTime, 0.001f, 0.02f);
            
            // PBD Algorithm
            ApplyExternalForces(deltaTime);
            PredictPositions(deltaTime);
            SolveConstraints();
            UpdateVelocitiesAndPositions(deltaTime);
            ApplyDamping();
            
            LastSolveTime = Time.realtimeSinceStartup - startTime;
        }
        
        private void ApplyExternalForces(float deltaTime)
        {
            foreach (var particle in Particles)
            {
                if (!particle.IsFixed)
                {
                    particle.Velocity += Gravity * deltaTime;
                }
            }
        }
        
        private void PredictPositions(float deltaTime)
        {
            foreach (var particle in Particles)
            {
                particle.PredictPosition(Vector3.zero, deltaTime);
            }
        }
        
        private void SolveConstraints()
        {
            LastIterationsUsed = 0;
            
            // Solve constraints in order of importance
            for (int iteration = 0; iteration < ConstraintIterations; iteration++)
            {
                LastIterationsUsed++;
                
                // Solve structural constraints first (most important)
                foreach (var constraint in _distanceConstraints)
                {
                    if (constraint.IsActive)
                    {
                        constraint.SolveConstraint(Particles, GlobalStiffness);
                    }
                }
                
                // Then solve collision constraints
                foreach (var constraint in _groundConstraints)
                {
                    if (constraint.IsActive)
                    {
                        constraint.SolveConstraint(Particles, 1.0f); // Ground constraints should be rigid
                    }
                }
                
                // Finally solve volume constraints (if any)
                foreach (var constraint in _volumeConstraints)
                {
                    if (constraint.IsActive)
                    {
                        constraint.SolveConstraint(Particles, GlobalStiffness * 0.5f);
                    }
                }
            }
        }
        
        private void UpdateVelocitiesAndPositions(float deltaTime)
        {
            foreach (var particle in Particles)
            {
                particle.UpdateFromPredicted(deltaTime);
            }
        }
        
        private void ApplyDamping()
        {
            foreach (var particle in Particles)
            {
                if (particle.IsFixed) continue;

                // MUCH LESS AGGRESSIVE damping to preserve bounce energy
                float damping = GlobalDamping;

                // Special case: preserve upward bounce energy even more
                if (particle.Velocity.y > 0.05f) // Ball is bouncing up
                {
                    damping = 0.9995f; // Almost no damping when bouncing
                }
                else if (particle.Velocity.y < -0.05f) // Ball is falling
                {
                    damping = 0.999f; // Minimal damping when falling
                }
                else // Near zero vertical velocity
                {
                    damping = 0.995f; // Still minimal damping
                }

                particle.Velocity *= damping;
            }
        }
        
        // Existing methods (AddParticle, AddConstraint, etc.) remain the same...
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
        
        private void LogConstraintBreakdown()
        {
            Debug.Log($"Constraint breakdown - Distance: {_distanceConstraints.Count}, " +
                     $"Ground: {_groundConstraints.Count}, Volume: {_volumeConstraints.Count}");
        }
        
        // Add ground constraint
        public void AddGroundConstraint(float groundY, float restitution = 0.3f, float friction = 0.4f)
        {
            var groundConstraint = new GroundConstraint(groundY, restitution, friction);
            AddConstraint(groundConstraint);
            _groundConstraints.Add(groundConstraint);
        }
        
        // Rest of the methods remain the same...
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
                if (particle.IsFixed) continue;
                
                float distance = Vector3.Distance(particle.Position, center);
                if (distance < radius)
                {
                    float falloff = 1f - (distance / radius);
                    particle.Velocity += impulse * falloff * particle.InverseMass;
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
            }
        }
        
        public (int particles, int constraints, int activeConstraints, float solveTime, int iterations) GetStatistics()
        {
            int activeConstraints = Constraints.Count(c => c.IsActive);
            return (Particles.Count, Constraints.Count, activeConstraints, LastSolveTime, LastIterationsUsed);
        }
        
        public bool ValidateState()
        {
            foreach (var particle in Particles)
            {
                if (float.IsNaN(particle.Position.x) || float.IsNaN(particle.Position.y) || float.IsNaN(particle.Position.z))
                {
                    Debug.LogError("PBD Solver: NaN position detected!");
                    return false;
                }
                
                if (float.IsNaN(particle.Velocity.x) || float.IsNaN(particle.Velocity.y) || float.IsNaN(particle.Velocity.z))
                {
                    Debug.LogError("PBD Solver: NaN velocity detected!");
                    return false;
                }
            }
            return true;
        }
    }
}