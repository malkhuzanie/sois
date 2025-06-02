using UnityEngine;

namespace _Project._00_Core.Scripts.Abstractions
{
    /// <summary>
    /// Represents a rigid body with position, rotation, and motion properties
    /// This is the foundation of all physics objects
    /// </summary>
    public interface IRigidBody
    {
        // Transform properties
        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }
        Vector3 Scale { get; set; }
        
        // Motion properties  
        Vector3 Velocity { get; set; }
        Vector3 AngularVelocity { get; set; }
        
        // Mass properties
        float Mass { get; set; }
        float InverseMass { get; }
        Matrix4x4 InertiaTensor { get; set; }
        Matrix4x4 InverseInertiaTensor { get; }
        
        Vector3 Force { get; set; }
        Vector3 Torque { get; set; }
        
        // State
        bool IsKinematic { get; set; }  // Doesn't respond to physics
        bool IsStatic { get; set; }     // Never moves
        
        void AddForce(Vector3 force);
        void AddForceAtPosition(Vector3 force, Vector3 position);
        void AddTorque(Vector3 torque);
        void ClearForces();
    }
}