using UnityEngine;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._00_Core.Scripts.Abstractions
{
    /// <summary>
    /// Main simulation object that combines physics, collision, and rendering
    /// </summary>
    public interface ISimulationObject
    {
        // Core systems
        IRigidBody RigidBody { get; }
        ICollider Collider { get; }
        IDeformable Deformable { get; }

        void UpdatePhysics(float deltaTime);
        void UpdateDeformation(float deltaTime);
        void UpdateVisuals();

        string ObjectID { get; }
        bool IsActive { get; set; }
        PhysicsMaterial Material { get; set; }
    }
}