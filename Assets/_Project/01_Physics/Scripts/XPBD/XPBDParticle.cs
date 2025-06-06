// Assets/_Project/01_Physics/Scripts/XPBD/Core/XPBDParticle.cs

using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Core
{
    /// <summary>
    /// XPBD Particle with Verlet integration - Clean implementation based on Jakobsen's approach
    /// </summary>
    [System.Serializable]
    public class XPBDParticle
    {
        [Header("State")]
        public Vector3 Position;
        public Vector3 PreviousPosition;
        public Vector3 PredictedPosition;
        
        [Header("Properties")]
        public float InverseMass = 1.0f;
        public bool IsFixed = false;
        
        [Header("Rendering")]
        public int VertexIndex = -1;
        
        // Constructor
        public XPBDParticle(Vector3 position, float mass = 1.0f, int vertexIndex = -1)
        {
            Position = position;
            PreviousPosition = position;
            PredictedPosition = position;
            VertexIndex = vertexIndex;
            SetMass(mass);
        }
        
        public void SetMass(float mass)
        {
            if (IsFixed)
            {
                InverseMass = 0.0f;
                return;
            }
            
            mass = Mathf.Max(mass, 0.001f); // Prevent zero mass
            InverseMass = 1.0f / mass;
        }
        
        public void SetFixed(bool isFixed)
        {
            IsFixed = isFixed;
            if (IsFixed)
            {
                InverseMass = 0.0f;
            }
        }
        
        /// <summary>
        /// Verlet integration step - predict new position
        /// </summary>
        public void PredictPosition(Vector3 externalAcceleration, float deltaTime)
        {
            if (IsFixed)
            {
                PredictedPosition = Position;
                return;
            }
            
            // Verlet integration: x' = 2x - x* + a*dt²
            Vector3 velocity = Position - PreviousPosition;
            PredictedPosition = Position + velocity + externalAcceleration * deltaTime * deltaTime;
        }
        
        /// <summary>
        /// Update position and previous position after constraint solving
        /// </summary>
        public void UpdatePosition()
        {
            if (IsFixed) return;
            
            PreviousPosition = Position;
            Position = PredictedPosition;
        }
        
        /// <summary>
        /// Get current velocity (derived from position history)
        /// </summary>
        public Vector3 GetVelocity(float deltaTime)
        {
            if (deltaTime <= 0.0f) return Vector3.zero;
            return (Position - PreviousPosition) / deltaTime;
        }
        
        /// <summary>
        /// Apply damping to reduce oscillations
        /// </summary>
        public void ApplyDamping(float damping)
        {
            if (IsFixed) return;
            
            Vector3 velocity = Position - PreviousPosition;
            PreviousPosition = Position - velocity * damping;
        }
        
        /// <summary>
        /// Apply impulse by modifying position history
        /// </summary>
        public void ApplyImpulse(Vector3 impulse, float deltaTime)
        {
            if (IsFixed || InverseMass <= 0.0f) return;
            
            Vector3 velocityChange = impulse * InverseMass;
            PreviousPosition -= velocityChange * deltaTime;
        }
    }
}