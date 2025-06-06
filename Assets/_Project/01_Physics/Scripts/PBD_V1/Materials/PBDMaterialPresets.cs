using _Project._00_Core.Scripts.DataStructures;
using UnityEngine;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.PBD_V1.Materials
{
    public static class PBDMaterialPresets
    {
        public static PhysicsMaterial CreateRubberMaterial()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "High_Bounce_Rubber";
            material.density = 0.9f;
            material.restitution = 0.85f;  // Much higher bounce
            material.staticFriction = 0.7f;
            material.dynamicFriction = 0.6f;
            material.deformationType = DeformationType.Elastic;
            material.stiffness = 800f;     // Lower for more deformation
            material.damping = 8f;         // Lower damping for more bounce
            material.elasticLimit = 3000f;
            return material;
        }
        
        public static PhysicsMaterial CreateSuperBouncyRubber()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Super_Bouncy_Rubber";
            material.density = 0.8f;
            material.restitution = 0.98f;  // Very high bounce
            material.staticFriction = 0.8f;
            material.dynamicFriction = 0.7f;
            material.deformationType = DeformationType.Elastic;
            material.stiffness = 1f;     // Even lower for more deformation
            material.damping = 5f;         // Very low damping
            material.elasticLimit = 4000f;
            return material;
        }
        
        public static PhysicsMaterial CreateUltraBouncyRubber()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Ultra_Bouncy_Rubber";
            material.density = 0.7f;
            material.restitution = 0.99f;  // Nearly perfect bounce
            material.staticFriction = 0.1f;
            material.dynamicFriction = 0.05f;
            material.deformationType = DeformationType.Elastic;
            material.stiffness = 600f;     // Lower for more deformation
            material.damping = 2f;         // Very low damping
            material.elasticLimit = 5000f;
            return material;
        }
    }
}