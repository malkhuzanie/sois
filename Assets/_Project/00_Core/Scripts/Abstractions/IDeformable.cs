using _Project._00_Core.Scripts.DataStructures;
using UnityEngine;

namespace _Project._00_Core.Scripts.Abstractions
{
    /// <summary>
    /// Handles object deformation (elastic, plastic, brittle)
    /// </summary>
    public interface IDeformable
    {
        void ApplyDeformation(Vector3 force, Vector3 position);
        void ApplyDeformation(DeformationData deformation);
        
        DeformationType DeformationType { get; set; }
        float ElasticLimit { get; set; }
        float PlasticLimit { get; set; }
        float BrittleThreshold { get; set; }
        
        Mesh GetDeformedMesh();
        bool HasDeformation { get; }
        void ResetDeformation();
    }
    
}