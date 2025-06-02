using _Project._00_Core.Scripts.DataStructures;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project._00_Core.Scripts.Abstractions
{
    /// <summary>
    /// Handles collision detection between objects
    /// </summary>
    public interface ICollider
    {
        bool CheckCollision(ICollider other);
        CollisionInfo GetCollisionInfo(ICollider other);
        
        // Geometric properties
        Bounds GetBounds();
        Vector3 GetClosestPoint(Vector3 point);
        
        // For GJK algorithm
        Vector3 GetSupportPoint(Vector3 direction);
        
        Tile.ColliderType Type { get; }
        bool IsTrigger { get; set; }
    }
}