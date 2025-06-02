// Assets/_Project/01_Physics/Scripts/Deformation/MassSpring/Spring.cs

using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project._01_Physics.Scripts.Deformation.MassSpring
{
    /// <summary>
    /// Balanced spring implementation with reasonable force handling
    /// </summary>
    [System.Serializable]
    public class Spring
    {
        // Connected mass points
        public MassPoint PointA { get; private set; }
        public MassPoint PointB { get; private set; }

        // Spring properties
        public float RestLength { get; set; }
        public float Stiffness { get; set; }
        public float Damping { get; set; }
        public SpringType Type { get; set; }

        // Breaking properties
        public float MaxStrain { get; set; }
        public float CurrentStrain { get; private set; }
        public bool IsBroken { get; private set; }

        // Stress tracking
        public float AccumulatedStress { get; private set; }
        public float FatigueThreshold { get; set; }

        // REASONABLE force limiting (not ultra-conservative)
        private float _maxForceMultiplier = 10f; // Much higher force multiplier

        public enum SpringType
        {
            Structural,  // Main structure
            Shear,       // Resist shearing
            Bend         // Resist bending
        }

        public Spring(MassPoint a, MassPoint b, float stiffness, float damping, SpringType type = SpringType.Structural)
        {
            PointA = a;
            PointB = b;

            // Calculate and validate rest length
            RestLength = Vector3.Distance(a.Position, b.Position);
            
            if (RestLength < 0.001f)
            {
                Debug.LogWarning($"Spring created with small RestLength: {RestLength:F6} between {a.Id} and {b.Id}");
                RestLength = 0.001f; // Minimum safe length
            }

            Stiffness = Mathf.Max(1f, stiffness); // Reasonable minimum stiffness
            Damping = Mathf.Max(0.1f, damping);   // Reasonable minimum damping
            Type = type;

            // Reasonable breaking properties
            MaxStrain = type switch
            {
                SpringType.Structural => 3.0f,   // Can stretch to 3x original length
                SpringType.Shear => 4.0f,        // More flexible
                SpringType.Bend => 5.0f,         // Most flexible
                _ => 3.0f
            };

            FatigueThreshold = float.MaxValue; // Disable fatigue by default
            IsBroken = false;
        }

        /// <summary>
        /// Apply spring forces with reasonable force calculations
        /// </summary>
        public void ApplyForces()
        {
            if (IsBroken) return;

            Vector3 springVector = PointB.Position - PointA.Position;
            float currentLength = springVector.magnitude;

            // Handle degenerate case where points are too close
            if (currentLength < 0.0001f)
            {
                ApplyRepulsionForces();
                return;
            }

            Vector3 springDirection = springVector / currentLength;
            float extension = currentLength - RestLength;
            CurrentStrain = currentLength / RestLength;

            // Check for breaking
            if (ShouldBreak())
            {
                Break();
                return;
            }

            // REASONABLE spring force calculation
            float springForceMagnitude = CalculateSpringForce(extension);
            float dampingForceMagnitude = CalculateDampingForce(springDirection);

            // Combine forces
            float totalForceMagnitude = springForceMagnitude + dampingForceMagnitude;

            // REASONABLE force limiting (not ultra-conservative)
            float maxForce = Stiffness * _maxForceMultiplier;
            totalForceMagnitude = Mathf.Clamp(totalForceMagnitude, -maxForce, maxForce);

            Vector3 totalForce = totalForceMagnitude * springDirection;

            // Apply equal and opposite forces
            PointA.AddForce(-totalForce);
            PointB.AddForce(totalForce);

            // Update stress tracking
            AccumulatedStress += Mathf.Abs(springForceMagnitude) * Time.fixedDeltaTime;
        }

        private float CalculateSpringForce(float extension)
        {
            // Standard Hooke's law with reasonable limiting
            float springForce = -Stiffness * extension;
            
            // Apply reasonable limiting for extreme extensions
            float maxExtension = RestLength * 2f; // Allow 2x stretch before heavy limiting
            
            if (Mathf.Abs(extension) > maxExtension)
            {
                float excessExtension = Mathf.Abs(extension) - maxExtension;
                float limitingFactor = 1f / (1f + excessExtension / RestLength);
                springForce *= limitingFactor;
            }
            
            return springForce;
        }

        private float CalculateDampingForce(Vector3 springDirection)
        {
            Vector3 relativeVelocity = PointB.Velocity - PointA.Velocity;
            float velocityAlongSpring = Vector3.Dot(relativeVelocity, springDirection);
            
            // Standard damping force
            return -Damping * velocityAlongSpring;
        }

        private void ApplyRepulsionForces()
        {
            // Apply reasonable repulsion force when points are too close
            Vector3 randomDirection = Random.onUnitSphere * 0.001f;
            float repulsionForce = Stiffness * 0.1f; // Reasonable repulsion

            PointA.AddForce(-randomDirection * repulsionForce);
            PointB.AddForce(randomDirection * repulsionForce);
        }

        private bool ShouldBreak()
        {
            // Reasonable breaking conditions
            if (Mathf.Abs(CurrentStrain - 1.0f) > (MaxStrain - 1.0f))
            {
                return true;
            }

            // Break based on accumulated stress (if fatigue is enabled)
            if (FatigueThreshold < float.MaxValue && AccumulatedStress > FatigueThreshold)
            {
                return true;
            }

            return false;
        }

        public void Break()
        {
            if (!IsBroken)
            {
                IsBroken = true;
                Debug.Log($"Spring {PointA.Id}-{PointB.Id} broke (strain: {CurrentStrain:F2}, type: {Type})");
            }
        }

        public void Repair()
        {
            IsBroken = false;
            AccumulatedStress = 0;
            CurrentStrain = 1.0f;
        }

        public float GetCurrentLength()
        {
            return Vector3.Distance(PointA.Position, PointB.Position);
        }

        public float GetStressLevel()
        {
            if (MaxStrain <= 1.0f) return 0f;
            return Mathf.Clamp01(Mathf.Abs(CurrentStrain - 1.0f) / (MaxStrain - 1.0f));
        }

        public string GetDebugInfo()
        {
            return $"Spring {PointA.Id}-{PointB.Id}: " +
                   $"Length={GetCurrentLength():F4}, " +
                   $"Rest={RestLength:F4}, " +
                   $"Strain={CurrentStrain:F2}, " +
                   $"Stress={GetStressLevel():F2}, " +
                   $"Type={Type}, " +
                   $"Broken={IsBroken}";
        }
    }
}