// Assets/_Project/01_Physics/Scripts/PBD/PBDParticle.cs

using UnityEngine;

namespace _Project._01_Physics.Scripts.PBD
{
    /// <summary>
    /// PBD particle - much simpler and more stable than mass-spring mass points
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
        
        public PBDParticle(Vector3 position, float mass = 1f, int vertexIndex = -1)
        {
            Position = position;
            PredictedPosition = position;
            OriginalPosition = position;
            Velocity = Vector3.zero;
            SetMass(mass);
            VertexIndex = vertexIndex;
            IsFixed = false;
        }
        
        public void SetMass(float mass)
        {
            InverseMass = IsFixed ? 0f : (mass > 0.001f ? 1f / mass : 0f);
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
            if (IsFixed)
            {
                PredictedPosition = Position;
                return;
            }
            
            // Integrate velocity with external forces (gravity)
            Velocity += externalAcceleration * deltaTime;
            
            // Predict new position
            PredictedPosition = Position + Velocity * deltaTime;
        }
        
        /// <summary>
        /// Update actual position and velocity from predicted position (final step of PBD)
        /// </summary>
        public void UpdateFromPredicted(float deltaTime)
        {
            if (IsFixed) return;
            
            // Update velocity based on position change
            Velocity = (PredictedPosition - Position) / deltaTime;
            
            // Update position
            Position = PredictedPosition;
        }
        
        /// <summary>
        /// Apply damping to velocity
        /// </summary>
        public void ApplyDamping(float damping)
        {
            if (!IsFixed)
            {
                Velocity *= damping;
            }
        }
        
        public string GetDebugInfo()
        {
            return $"Particle {VertexIndex}: Pos={Position:F2}, Vel={Velocity.magnitude:F2}, Fixed={IsFixed}";
        }
    }
}