// Assets/_Project/01_Physics/Scripts/Deformation/MassSpring/MassPoint.cs

using UnityEngine;

namespace _Project._01_Physics.Scripts.Deformation.MassSpring
{
    /// <summary>
    /// Ultra-stable mass point with extremely conservative force handling
    /// </summary>
    [System.Serializable]
    public class MassPoint
    {
        // Unique identifier
        public int Id { get; private set; }
        
        // Physical properties
        public float Mass { get; set; }
        public float InverseMass { get; private set; }
        
        // State variables
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Acceleration { get; set; }
        
        // Force accumulator - now with ultra-conservative limits
        private Vector3 _force;
        public Vector3 Force 
        { 
            get => _force; 
            set => _force = value; 
        }
        
        // Original position for deformation reference
        public Vector3 OriginalPosition { get; set; }
        
        // Constraints
        public bool IsFixed { get; set; }
        
        // Mesh correspondence
        public int VertexIndex { get; set; }

        // Ultra-stable tracking
        private Vector3 _lastValidPosition;
        private float _maxDisplacementPerFrame = 1f; // Even smaller movement limit
        private float _maxForceAccumulation = 10f; // Much lower force accumulation limit

        public MassPoint(int id, Vector3 position, float mass = 1.0f, int vertexIndex = -1)
        {
            Id = id;
            Position = position;
            OriginalPosition = position;
            _lastValidPosition = position;
            
            SetMass(mass);
            
            Velocity = Vector3.zero;
            Acceleration = Vector3.zero;
            _force = Vector3.zero;
            IsFixed = false;
            VertexIndex = vertexIndex;
        }

        public void SetMass(float mass)
        {
            Mass = Mathf.Max(0.1f, mass); // Higher minimum mass for stability
            InverseMass = IsFixed ? 0f : 1f / Mass;
        }

        /// <summary>
        /// Add force with ULTRA conservative validation
        /// </summary>
        public void AddForce(Vector3 force)
        {
            if (IsFixed) return;

            // Validate force for NaN
            if (force.x != force.x || force.y != force.y || force.z != force.z)
            {
                Debug.LogWarning($"NaN force applied to mass point {Id}");
                return;
            }

            // MUCH HIGHER force limiting (was 5f)
            if (force.magnitude > 200f) 
            {
                force = force.normalized * 200f;
            }

            // Allow much higher force accumulation (was 10f)
            Vector3 newTotalForce = _force + force;
            if (newTotalForce.magnitude > 500f) // Much higher limit
            {
                // Less aggressive blending (was 0.01f)
                float blendFactor = 0.2f; // Allow 20% of new force
                _force = Vector3.Lerp(_force, newTotalForce.normalized * 500f, blendFactor);
            }
            else
            {
                _force += force; // Normal addition
            }
        }
        
        /// <summary>
        /// Update acceleration with ULTRA conservative limits
        /// </summary>
        public void UpdateAcceleration()
        {
            if (IsFixed)
            {
                Acceleration = Vector3.zero;
                return;
            }

            Acceleration = _force * InverseMass;

            // MUCH HIGHER acceleration limiting (was 2f)
            if (Acceleration.magnitude > 100f)
            {
                Acceleration = Acceleration.normalized * 100f;
            }
        }
        
        /// <summary>
        /// Clear accumulated forces
        /// </summary>
        public void ClearForces()
        {
            _force = Vector3.zero;
        }

        /// <summary>
        /// Validate and constrain position changes with ultra-conservative limits
        /// </summary>
        public void ValidatePosition()
        {
            if (IsFixed) return;

            // Check for NaN positions
            if (Position.x != Position.x || Position.y != Position.y || Position.z != Position.z)
            {
                Debug.LogError($"NaN position detected on mass point {Id}, resetting to last valid position");
                Position = _lastValidPosition;
                Velocity = Vector3.zero;
                return;
            }

            // REASONABLE displacement limiting
            Vector3 displacement = Position - _lastValidPosition;
            if (displacement.magnitude > _maxDisplacementPerFrame)
            {
                Position = _lastValidPosition + displacement.normalized * _maxDisplacementPerFrame;
            }
            
            // Check for extreme positions (more reasonable)
            if (Position.magnitude > 100f) // Was 20f
            {
                Debug.LogWarning($"Extreme position {Position.magnitude:F2} on mass point {Id}, clamping");
                Position = Position.normalized * 100f;
            }

            _lastValidPosition = Position;
        }
        
        public Vector3 GetDisplacement()
        {
            return Position - OriginalPosition;
        }

        public float GetDisplacementMagnitude()
        {
            return GetDisplacement().magnitude;
        }

        public void Reset()
        {
            Position = OriginalPosition;
            _lastValidPosition = OriginalPosition;
            Velocity = Vector3.zero;
            Acceleration = Vector3.zero;
            ClearForces();
        }

        public void SetFixed(bool isFixed)
        {
            IsFixed = isFixed;
            InverseMass = IsFixed ? 0f : 1f / Mass;
            
            if (IsFixed)
            {
                Velocity = Vector3.zero;
                Acceleration = Vector3.zero;
                ClearForces();
            }
        }

        public string GetDebugInfo()
        {
            return $"MassPoint {Id}: " +
                   $"Pos={Position:F3}, " +
                   $"Vel={Velocity.magnitude:F3}, " +
                   $"Force={_force.magnitude:F3}, " +
                   $"Mass={Mass:F3}, " +
                   $"Fixed={IsFixed}, " +
                   $"Displacement={GetDisplacementMagnitude():F3}";
        }

        public void ApplyDamping(float dampingFactor)
        {
            if (IsFixed) return;
            
            // Ensure damping factor is very conservative
            dampingFactor = Mathf.Clamp(dampingFactor, 0.9f, 0.999f);
            Velocity *= dampingFactor;
        }

        public void LimitVelocity(float maxVelocity)
        {
            if (IsFixed) return;
            
            // Use an even more conservative max velocity
            maxVelocity = Mathf.Min(maxVelocity, 1f); // Very low velocity limit
            
            if (Velocity.magnitude > maxVelocity)
            {
                Velocity = Velocity.normalized * maxVelocity;
            }
        }
    }
}