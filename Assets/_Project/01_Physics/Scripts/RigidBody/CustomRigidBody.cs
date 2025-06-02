using _Project._00_Core.Scripts.Abstractions;
using _Project._01_Physics.Scripts.Helpers;
using UnityEngine;

namespace _Project._01_Physics.Scripts.RigidBody
{
    /// <summary>
    /// Custom rigid body implementation - replaces Unity's Rigidbody
    /// This is the core of our physics system
    /// </summary>
    public class CustomRigidBody : MonoBehaviour, IRigidBody
    {
        [Header("Mass Properties")]
        [SerializeField] private float mass = 1.0f;
        [SerializeField] private bool isKinematic = false;
        [SerializeField] private bool isStatic = false;
        
        [Header("Motion State")]
        [SerializeField] private Vector3 velocity = Vector3.zero;
        [SerializeField] private Vector3 angularVelocity = Vector3.zero;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        private Vector3 _force = Vector3.zero;
        private Vector3 _torque = Vector3.zero;
        private Matrix4x4 _inertiaTensor = Matrix4x4.identity;
        private Matrix4x4 _inverseInertiaTensor = Matrix4x4.identity;
        
        #region IRigidBody Implementation
        
        public Vector3 Position 
        { 
            get => transform.position; 
            set => transform.position = value; 
        }
        
        public Quaternion Rotation 
        { 
            get => transform.rotation; 
            set => transform.rotation = value; 
        }
        
        public Vector3 Scale 
        { 
            get => transform.localScale; 
            set => transform.localScale = value; 
        }
        
        public Vector3 Velocity { get => velocity; set => velocity = value; }
        public Vector3 AngularVelocity { get => angularVelocity; set => angularVelocity = value; }
        
        public float Mass 
        { 
            get => mass; 
            set 
            { 
                mass = Mathf.Max(0.001f, value); // Prevent zero mass
                UpdateInertiaTensor();
            } 
        }
        
        public float InverseMass => isStatic || isKinematic ? 0f : 1f / mass;
        
        public Matrix4x4 InertiaTensor
        {
            get => _inertiaTensor;
            set => _inertiaTensor = value;
        }

        public Matrix4x4 InverseInertiaTensor => _inverseInertiaTensor;
        
        public Vector3 Force { get => _force; set => _force = value; }
        public Vector3 Torque { get => _torque; set => _torque = value; }
        
        public bool IsKinematic { get => isKinematic; set => isKinematic = value; }
        public bool IsStatic { get => isStatic; set => isStatic = value; }
        
        #endregion
        
        #region Unity Lifecycle
        
        void Start()
        {
            UpdateInertiaTensor();
        }
        
        void Update()
        {
            if (showDebugInfo)
            {
                DrawDebugInfo();
            }
        }
        
        #endregion
        
        #region Force Application
        
        public void AddForce(Vector3 newForce)
        {
            if (isStatic || isKinematic)
            {
                return;
            }
            _force += newForce;
        }
        
        public void AddForceAtPosition(Vector3 newForce, Vector3 position)
        {
            if (isStatic || isKinematic)
            {
                return;
            }
            
            AddForce(newForce);
            
            // Add torque from offset
            Vector3 offset = position - Position;
            Vector3 torqueFromForce = Vector3.Cross(offset, newForce);
            AddTorque(torqueFromForce);
        }
        
        public void AddTorque(Vector3 newTorque)
        {
            if (isStatic || isKinematic)
            {
                return;
            }
            _torque += newTorque;
        }
        
        public void ClearForces()
        {
            _force = Vector3.zero;
            _torque = Vector3.zero;
        }
        
        #endregion
        
        #region Physics Integration
        
        /// <summary>
        /// Update physics using Verlet integration
        /// Called by the physics engine each timestep
        /// </summary>
        public void IntegratePhysics(float deltaTime)
        {
            if (isStatic || isKinematic)
            {
                return;
            }
            
            // Linear motion integration
            var acceleration = _force * InverseMass;
            velocity += acceleration * deltaTime;
            Position += velocity * deltaTime;
            
            // Angular motion integration using inertia tensor
            // α = I⁻¹ * τ (angular acceleration = inverse inertia tensor * torque)
            var torque4 = new Vector4(_torque.x, _torque.y, _torque.z, 0);
            var angularAcceleration4 = _inverseInertiaTensor * torque4;
            var angularAcceleration = new Vector3(angularAcceleration4.x, angularAcceleration4.y, angularAcceleration4.z);
            
            angularVelocity += angularAcceleration * deltaTime;
            
            // Apply angular velocity to rotation
            if (angularVelocity.magnitude > 0.001f)
            {
                float angle = angularVelocity.magnitude * deltaTime;
                var axis = angularVelocity.normalized;
                var deltaRotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis);
                Rotation = deltaRotation * Rotation;
            }
            
            // Clear forces after integration
            ClearForces();
        }
        
        #endregion
        
        #region Helper Methods
        
        private void UpdateInertiaTensor()
        {
            // Use our helper class to calculate proper inertia tensor
            // For now, assuming all objects are boxes (cubes)
            // Later, you can extend this based on collider shape
            
            var size = Scale; 
            _inertiaTensor = InertiaTensorHelper.CalculateBoxInertia(mass, size);
            
            if (InverseMass > 0 && !isStatic && !isKinematic)
            {
                _inverseInertiaTensor = InertiaTensorHelper.CalculateInverseInertia(_inertiaTensor);
            }
            else
            {
                // Static or kinematic objects have zero inverse inertia
                _inverseInertiaTensor = Matrix4x4.zero;
            }
        }
        
        private void DrawDebugInfo()
        {
            // Draw velocity vector
            if (velocity.magnitude > 0.1f)
            {
                Debug.DrawRay(Position, velocity, Color.green);
            }
            
            // Draw force vector
            if (_force.magnitude > 0.1f)
            {
                Debug.DrawRay(Position, _force * 0.1f, Color.red);
            }
        }
        
        #endregion
    }
}