// Assets/_Project/01_Physics/Scripts/Deformation/MassSpring/SoftBodyWrapper.cs

using UnityEngine;
using _Project._00_Core.Scripts.Abstractions;
using _Project._00_Core.Scripts.DataStructures;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.Deformation.MassSpring
{
    /// <summary>
    /// Improved wrapper with better ground collision and cohesive body behavior
    /// </summary>
    public class SoftBodyWrapper : MonoBehaviour, IDeformable
    {
        public MassSpringSystem System { get; private set; }

        private PhysicsMaterial _physicsMaterial;
        private MeshFilter _meshFilter;
        private bool _initialized = false;

        [Header("Ground Collision Settings")]
        [SerializeField] private bool enableGroundCollision = true;
        [SerializeField] private float groundYPosition = 0.0f;
        [SerializeField] private float groundRepulsionStrength = 200f;
        [SerializeField] private float groundCollisionDamping = 0.7f;
        [SerializeField] private float penetrationTolerance = 0.01f;

        [Header("Cohesion Settings")]
        [SerializeField] private bool maintainCohesion = false; // Disable initially to test
        [SerializeField] private float cohesionStrength = 10f; // Much lower - was 100f
        [SerializeField] private float centerOfMassInfluence = 0.1f; // Much lower - was 0.3f

        [Header("Physics Settings")]
        [SerializeField] private bool enablePhysicsDebugging = false;
        [SerializeField] private float maxVelocity = 30f;
        [SerializeField] private float emergencyDamping = 0.95f;

        // Cohesion tracking
        private Vector3 _originalCenterOfMass;
        private float _originalRadius;
        
        // Performance optimization
        private int _frameCounter = 0;
        private const int GROUND_CHECK_FREQUENCY = 2; // Check every 2 frames

        public void Initialize(MassSpringSystem massSpringSystem, PhysicsMaterial material)
        {
            System = massSpringSystem;
            _physicsMaterial = material;
            _meshFilter = GetComponent<MeshFilter>();
            
            if (_meshFilter == null)
            {
                Debug.LogError($"[{gameObject.name}] MeshFilter missing!");
                return;
            }

            _initialized = true;
            
            // Calculate initial properties for cohesion
            CalculateInitialProperties();
            
            Debug.Log($"SoftBodyWrapper initialized on {gameObject.name}");
            ValidateSystem();
        }

        private void CalculateInitialProperties()
        {
            if (System?.MassPoints == null || System.MassPoints.Count == 0) return;

            // Calculate original center of mass
            Vector3 centerSum = Vector3.zero;
            foreach (var point in System.MassPoints)
            {
                centerSum += point.Position;
            }
            _originalCenterOfMass = centerSum / System.MassPoints.Count;

            // Calculate original radius (average distance from center)
            float radiusSum = 0f;
            foreach (var point in System.MassPoints)
            {
                radiusSum += Vector3.Distance(point.Position, _originalCenterOfMass);
            }
            _originalRadius = radiusSum / System.MassPoints.Count;

            Debug.Log($"Original center: {_originalCenterOfMass:F2}, radius: {_originalRadius:F2}");
        }

        void Start()
        {
            if (enableGroundCollision)
            {
                FindGroundPosition();
            }

            if (!_initialized)
            {
                Debug.LogError($"[{gameObject.name}] Not initialized! Call Initialize() first.");
                enabled = false;
                return;
            }

            if (_meshFilter.mesh == null && System != null)
            {
                _meshFilter.mesh = System.GetDeformedMesh();
            }
        }

        void FixedUpdate()
        {
            if (!_initialized || System == null || !enabled || !gameObject.activeInHierarchy)
                return;

            _frameCounter++;

            // Update physics
            System.Update(Time.fixedDeltaTime);

            // Maintain cohesion
            if (maintainCohesion)
            {
                ApplyCohesionForces();
            }

            // Validate and correct positions
            ValidateAndCorrectPositions();

            // Update mesh
            UpdateMesh();

            // Ground collision with reduced frequency
            if (enableGroundCollision && _frameCounter % GROUND_CHECK_FREQUENCY == 0)
            {
                HandleImprovedGroundCollision();
            }

            // Emergency corrections less frequently
            if (_frameCounter % 30 == 0)
            {
                ApplyEmergencyCorrections();
            }
        }

        private void ApplyCohesionForces()
        {
            if (System?.MassPoints == null) return;

            // Calculate current center of mass
            Vector3 currentCenter = Vector3.zero;
            foreach (var point in System.MassPoints)
            {
                currentCenter += point.Position;
            }
            currentCenter /= System.MassPoints.Count;

            // Apply gentle forces to maintain shape
            foreach (var point in System.MassPoints)
            {
                if (point.IsFixed) continue;

                // Vector from current center to point
                Vector3 centerToPoint = point.Position - currentCenter;
                
                // Vector from original center to point (in original configuration)
                Vector3 originalCenterToPoint = point.OriginalPosition - _originalCenterOfMass;
                
                // Desired position based on original configuration
                Vector3 desiredPosition = currentCenter + originalCenterToPoint;
                
                // Gentle restoring force
                Vector3 restoringForce = (desiredPosition - point.Position) * cohesionStrength * point.Mass;
                
                // Limit the force to prevent instability
                if (restoringForce.magnitude > cohesionStrength * 2f)
                {
                    restoringForce = restoringForce.normalized * cohesionStrength * 2f;
                }
                
                point.AddForce(restoringForce * centerOfMassInfluence);
            }
        }

        private void HandleImprovedGroundCollision()
        {
            if (System?.MassPoints == null) return;

            var collidingPoints = new System.Collections.Generic.List<MassPoint>();
            var penetrations = new System.Collections.Generic.List<float>();
            
            // First pass: identify all colliding points
            foreach (var point in System.MassPoints)
            {
                if (point.IsFixed) continue;

                Vector3 worldPos = transform.TransformPoint(point.Position);
                float penetration = groundYPosition - worldPos.y;

                if (penetration > -penetrationTolerance) // Slightly above ground
                {
                    collidingPoints.Add(point);
                    penetrations.Add(penetration);
                }
            }

            if (collidingPoints.Count == 0) return;

            // Calculate average collision normal and penetration
            float avgPenetration = 0f;
            foreach (float pen in penetrations)
            {
                avgPenetration += pen;
            }
            avgPenetration /= penetrations.Count;

            // Apply cohesive collision response
            ApplyCohesiveCollisionResponse(collidingPoints, penetrations, avgPenetration);
        }

        private void ApplyCohesiveCollisionResponse(
            System.Collections.Generic.List<MassPoint> collidingPoints, 
            System.Collections.Generic.List<float> penetrations,
            float avgPenetration)
        {
            Vector3 avgVelocity = Vector3.zero;
            float totalMass = 0f;

            // Calculate average velocity and total mass of colliding region
            foreach (var point in collidingPoints)
            {
                Vector3 worldVel = transform.TransformDirection(point.Velocity);
                avgVelocity += worldVel * point.Mass;
                totalMass += point.Mass;
            }
            
            if (totalMass > 0)
            {
                avgVelocity /= totalMass;
            }

            // Apply response to all colliding points together
            for (int i = 0; i < collidingPoints.Count; i++)
            {
                var point = collidingPoints[i];
                float penetration = penetrations[i];
                
                if (penetration > 0) // Actually penetrating
                {
                    // Position correction
                    Vector3 worldPos = transform.TransformPoint(point.Position);
                    worldPos.y = groundYPosition + penetrationTolerance;
                    point.Position = transform.InverseTransformPoint(worldPos);

                    // Velocity response based on average collision velocity
                    Vector3 worldVelocity = transform.TransformDirection(point.Velocity);
                    
                    if (avgVelocity.y < 0) // Moving downward
                    {
                        // Bounce based on material properties
                        float restitution = _physicsMaterial?.restitution ?? 0.3f;
                        worldVelocity.y = -avgVelocity.y * restitution;
                        
                        // Apply friction
                        worldVelocity.x *= (1f - groundCollisionDamping * 0.5f);
                        worldVelocity.z *= (1f - groundCollisionDamping * 0.5f);
                    }
                    
                    point.Velocity = transform.InverseTransformDirection(worldVelocity);
                }
                else if (penetration > -penetrationTolerance * 2) // Very close to ground
                {
                    // Apply gentle upward force
                    Vector3 upwardForce = Vector3.up * groundRepulsionStrength * point.Mass;
                    Vector3 localForce = transform.InverseTransformDirection(upwardForce);
                    point.AddForce(localForce);
                }
            }
        }

        private void FindGroundPosition()
        {
            GameObject groundObject = GameObject.FindGameObjectWithTag("Ground");
            if (groundObject != null)
            {
                Collider groundCollider = groundObject.GetComponent<Collider>();
                if (groundCollider != null)
                {
                    groundYPosition = groundCollider.bounds.max.y;
                }
                else
                {
                    groundYPosition = groundObject.transform.position.y + 
                                    (groundObject.transform.localScale.y * 0.5f);
                }
                Debug.Log($"[{gameObject.name}] Ground Y position set to: {groundYPosition:F3}");
            }
        }

        private void ValidateSystem()
        {
            if (System?.MassPoints == null || System.MassPoints.Count == 0)
            {
                Debug.LogError($"[{gameObject.name}] Invalid mass-spring system!");
                _initialized = false;
                return;
            }

            var stats = System.GetStatistics();
            Debug.Log($"[{gameObject.name}] System validated: {System.MassPoints.Count} points, {stats.totalSprings} springs");
        }

        private void ValidateAndCorrectPositions()
        {
            if (System?.MassPoints == null) return;

            foreach (var point in System.MassPoints)
            {
                point.ValidatePosition();
                point.LimitVelocity(maxVelocity);
            }
        }

        private void UpdateMesh()
        {
            if (_meshFilter && System != null)
            {
                Mesh updatedMesh = System.GetDeformedMesh();
                if (updatedMesh)
                {
                    _meshFilter.mesh = updatedMesh;
                }
            }
        }

        private void ApplyEmergencyCorrections()
        {
            if (System?.MassPoints == null) return;

            bool needsCorrection = false;
            
            foreach (var point in System.MassPoints)
            {
                if (point.Position.magnitude > 1000f || point.Velocity.magnitude > maxVelocity * 3f)
                {
                    needsCorrection = true;
                    break;
                }
            }

            if (needsCorrection)
            {
                Debug.LogWarning($"[{gameObject.name}] Applying emergency corrections");
                
                foreach (var point in System.MassPoints)
                {
                    if (point.Position.magnitude > 1000f)
                    {
                        point.Position = point.Position.normalized * 10f;
                    }
                    
                    point.Velocity *= emergencyDamping;
                }
            }
        }

        #region IDeformable Implementation

        public DeformationType DeformationType
        {
            get => _physicsMaterial?.deformationType ?? DeformationType.Elastic;
            set { if (_physicsMaterial) _physicsMaterial.deformationType = value; }
        }

        public float ElasticLimit
        {
            get => _physicsMaterial?.elasticLimit ?? 1000f;
            set { if (_physicsMaterial) _physicsMaterial.elasticLimit = value; }
        }

        public float PlasticLimit
        {
            get => _physicsMaterial?.plasticLimit ?? 2000f;
            set { if (_physicsMaterial) _physicsMaterial.plasticLimit = value; }
        }

        public float BrittleThreshold
        {
            get => _physicsMaterial?.brittleThreshold ?? 5000f;
            set { if (_physicsMaterial) _physicsMaterial.brittleThreshold = value; }
        }

        public bool HasDeformation => System != null;

        public void ApplyDeformation(Vector3 force, Vector3 position)
        {
            if (System != null && _initialized)
            {
                Vector3 localPos = transform.InverseTransformPoint(position);
                Vector3 localForce = transform.InverseTransformDirection(force);
                float radius = 0.5f;
                System.ApplyImpulse(localPos, localForce, radius);
            }
        }

        public void ApplyDeformation(DeformationData deformation)
        {
            if (System != null && _initialized)
            {
                Vector3 localPos = transform.InverseTransformPoint(deformation.position);
                Vector3 localForce = transform.InverseTransformDirection(deformation.force);
                float radius = 0.5f * deformation.intensity;
                System.ApplyImpulse(localPos, localForce, radius);
            }
        }

        public Mesh GetDeformedMesh()
        {
            return System?.GetDeformedMesh();
        }

        public void ResetDeformation()
        {
            System?.Reset();
            if (maintainCohesion)
            {
                CalculateInitialProperties();
            }
        }

        #endregion

        #region Debug and Testing

        void OnDrawGizmos()
        {
            if (!enablePhysicsDebugging || System?.MassPoints == null) return;

            // Draw mass points
            Gizmos.color = Color.yellow;
            foreach (var point in System.MassPoints)
            {
                Vector3 worldPos = transform.TransformPoint(point.Position);
                Gizmos.DrawWireSphere(worldPos, 0.02f);
                
                if (point.IsFixed)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(worldPos, 0.03f);
                    Gizmos.color = Color.yellow;
                }
            }

            // Draw center of mass
            if (maintainCohesion && System.MassPoints.Count > 0)
            {
                Vector3 currentCenter = Vector3.zero;
                foreach (var point in System.MassPoints)
                {
                    currentCenter += point.Position;
                }
                currentCenter /= System.MassPoints.Count;
                
                Vector3 worldCenter = transform.TransformPoint(currentCenter);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(worldCenter, 0.05f);
            }

            // Draw ground plane
            if (enableGroundCollision)
            {
                Gizmos.color = Color.green;
                Vector3 groundCenter = new Vector3(transform.position.x, groundYPosition, transform.position.z);
                Gizmos.DrawWireCube(groundCenter, new Vector3(10f, 0.1f, 10f));
            }
        }

        [ContextMenu("Debug System Info")]
        public void DebugSystemInfo()
        {
            if (System != null)
            {
                var stats = System.GetStatistics();
                Debug.Log($"System Stats - Points: {System.MassPoints.Count}, " +
                         $"Springs: {stats.totalSprings}, " +
                         $"Broken: {stats.brokenSprings}, " +
                         $"Avg Stress: {stats.averageStress:F3}");
            }
        }

        [ContextMenu("Reset Physics")]
        public void ResetPhysics()
        {
            System?.Reset();
            _frameCounter = 0;
            if (maintainCohesion)
            {
                CalculateInitialProperties();
            }
        }

        #endregion
    }
}