using UnityEngine;

namespace _Project._01_Physics.Scripts.Helpers
{
    /// <summary>
    /// Utility class for calculating inertia tensors for common shapes
    /// This will be very useful as you add more complex objects
    /// </summary>
    public static class InertiaTensorHelper
    {
        /// <summary>
        /// Calculate inertia tensor for a rectangular box (cube)
        /// </summary>
        public static Matrix4x4 CalculateBoxInertia(float mass, Vector3 size)
        {
            float ixx = (mass / 12f) * (size.y * size.y + size.z * size.z);
            float iyy = (mass / 12f) * (size.x * size.x + size.z * size.z);
            float izz = (mass / 12f) * (size.x * size.x + size.y * size.y);

            return new Matrix4x4(
                new Vector4(ixx, 0, 0, 0),
                new Vector4(0, iyy, 0, 0),
                new Vector4(0, 0, izz, 0),
                new Vector4(0, 0, 0, 1)
            );
        }
        
        /// <summary>
        /// Calculate inertia tensor for a solid sphere
        /// </summary>
        public static Matrix4x4 CalculateSphereInertia(float mass, float radius)
        {
            float i = (2f / 5f) * mass * radius * radius;
            
            return new Matrix4x4(
                new Vector4(i, 0, 0, 0),
                new Vector4(0, i, 0, 0),
                new Vector4(0, 0, i, 0),
                new Vector4(0, 0, 0, 1)
            );
        }
        
        /// <summary>
        /// Calculate inertia tensor for a cylinder (around Y-axis)
        /// </summary>
        public static Matrix4x4 CalculateCylinderInertia(float mass, float radius, float height)
        {
            float ixx = (mass / 12f) * (3 * radius * radius + height * height);
            float iyy = 0.5f * mass * radius * radius;
            float izz = ixx; // Same as ixx for cylinder
            
            return new Matrix4x4(
                new Vector4(ixx, 0, 0, 0),
                new Vector4(0, iyy, 0, 0),
                new Vector4(0, 0, izz, 0),
                new Vector4(0, 0, 0, 1)
            );
        }
        
        /// <summary>
        /// Calculate inverse of inertia tensor safely
        /// </summary>
        public static Matrix4x4 CalculateInverseInertia(Matrix4x4 inertiaTensor)
        {
            // For diagonal matrices, inverse is just 1/diagonal elements
            float ixx = inertiaTensor.m00;
            float iyy = inertiaTensor.m11;
            float izz = inertiaTensor.m22;
            
            float invIxx = ixx > 0.001f ? 1f / ixx : 0f;
            float invIyy = iyy > 0.001f ? 1f / iyy : 0f;
            float invIzz = izz > 0.001f ? 1f / izz : 0f;
            
            return new Matrix4x4(
                new Vector4(invIxx, 0, 0, 0),
                new Vector4(0, invIyy, 0, 0),
                new Vector4(0, 0, invIzz, 0),
                new Vector4(0, 0, 0, 1)
            );
        }
    }
}