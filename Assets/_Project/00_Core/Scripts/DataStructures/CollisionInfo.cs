using _Project._00_Core.Scripts.Abstractions;
using UnityEngine;

namespace _Project._00_Core.Scripts.DataStructures
{
    [System.Serializable]
    public struct CollisionInfo
    {
        public ISimulationObject ObjectA;
        public ISimulationObject ObjectB;
        public Vector3 ContactPoint;
        public Vector3 ContactNormal;
        public float PenetrationDepth;
        public float RelativeVelocity;
        public bool IsColliding;
    }
}