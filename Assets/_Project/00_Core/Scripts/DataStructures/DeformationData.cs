using UnityEngine;

namespace _Project._00_Core.Scripts.DataStructures
{
    [System.Serializable]
    public struct DeformationData
    {
        public Vector3 force;
        public Vector3 position;
        public float intensity;
        public DeformationType type;
    }
}