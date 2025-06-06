// Assets/_Project/01_Physics/Scripts/PBD/PBDParticle.cs

using UnityEngine;

namespace _Project._01_Physics.Scripts.PBD_V1
{
    /// <summary>
    /// Improved PBD particle with better stability and debugging
    /// </summary>
    [System.Serializable]
    public class PBDParticle
    {
        // Core properties
        public Vector3 Position;
        public Vector3 PredictedPosition;
        public Vector3 Velocity;
        public float InverseMass; // 0 = infinite mass (fixed)
        
        // Mesh binding
        public int VertexIndex;
        public Vector3 OriginalPosition;
        
        // Constraints
        public bool IsFixed;
        
        // Fracture support
        public bool IsOnSurface; // Whether this particle is on the object surface
        public float StressAccumulation; // Accumulated stress for fracture
        public bool IsActive = true; // Whether particle is still part of the object
        
        public PBDParticle(Vector3 position, float mass = 1f, int vertexIndex = -1)
        {
            Position = position;
            PredictedPosition = position;
            OriginalPosition = position;
            Velocity = Vector3.zero;
            SetMass(mass);
            VertexIndex = vertexIndex;
            IsFixed = false;
            IsOnSurface = true; // Default to surface particle
            StressAccumulation = 0f;
        }
        
        public void SetMass(float mass)
        {
            if (IsFixed)
            {
                InverseMass = 0f;
                return;
            }
            
            // Ensure minimum mass for stability
            mass = Mathf.Max(mass, 0.001f);
            InverseMass = 1f / mass;
        }
        
        public void SetFixed(bool isFixed)
        {
            IsFixed = isFixed;
            if (IsFixed)
            {
                InverseMass = 0f;
                Velocity = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Predict position based on current velocity (first step of PBD)
        /// </summary>
        public void PredictPosition(Vector3 externalAcceleration, float deltaTime)
        {
            if (IsFixed || !IsActive)
            {
                PredictedPosition = Position;
                return;
            }
            
            // Integrate velocity with external forces (gravity)
            Velocity += externalAcceleration * deltaTime;
            
            // Apply velocity damping for stability
            Velocity *= 0.999f;
            
            // Predict new position
            PredictedPosition = Position + Velocity * deltaTime;
        }
        
        /// <summary>
        /// Update actual position and velocity from predicted position (final step of PBD)
        /// </summary>
        public void UpdateFromPredicted(float deltaTime)
        {
            if (IsFixed || !IsActive) return;
            
            // Calculate new velocity based on position change
            Vector3 newVelocity = (PredictedPosition - Position) / deltaTime;
            
            // Smooth velocity changes to prevent instability
            Velocity = Vector3.Lerp(Velocity, newVelocity, 0.9f);
            
            // Update position
            Position = PredictedPosition;
        }
        
        /// <summary>
        /// Apply damping to velocity
        /// </summary>
        public void ApplyDamping(float damping)
        {
            if (!IsFixed && IsActive)
            {
                Velocity *= damping;
            }
        }
        
        /// <summary>
        /// Add stress for fracture mechanics
        /// </summary>
        public void AddStress(float stress)
        {
            if (IsActive)
            {
                StressAccumulation += stress;
            }
        }
        
        /// <summary>
        /// Reset stress accumulation
        /// </summary>
        public void ResetStress()
        {
            StressAccumulation = 0f;
        }
        
        /// <summary>
        /// Deactivate particle (used in fracture)
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            InverseMass = 0f;
            Velocity = Vector3.zero;
        }
        
        public string GetDebugInfo()
        {
            return $"Particle {VertexIndex}: Pos={Position:F2}, Vel={Velocity.magnitude:F2}, " +
                   $"Fixed={IsFixed}, Active={IsActive}, Stress={StressAccumulation:F2}";
        }
    }
}