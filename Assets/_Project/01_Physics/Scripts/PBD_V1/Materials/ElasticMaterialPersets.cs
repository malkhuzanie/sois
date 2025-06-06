// Assets/_Project/01_Physics/Scripts/PBD/ElasticMaterialPresets.cs

using _Project._00_Core.Scripts.DataStructures;
using UnityEngine;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.PBD_V1.Materials
{
    /// <summary>
    /// Material presets for elastic (non-breaking) objects like rubber, foam, etc.
    /// </summary>
    public static class ElasticMaterialPresets
    {
        /// <summary>
        /// Creates a bouncy rubber ball material that does NOT break and MAINTAINS SHAPE
        /// </summary>
        public static PhysicsMaterial CreateBouncyRubber()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Shape_Preserving_Rubber";
            material.density = 1.0f;
            material.restitution = 0.8f; // High bounce
            material.staticFriction = 0.7f;
            material.dynamicFriction = 0.6f;
            material.deformationType = DeformationType.Elastic;
            material.stiffness = 3000f; // INCREASED from 1200f - more rigid to maintain shape
            material.damping = 50f; // INCREASED from 20f - better stability
            material.elasticLimit = 10000f; // VERY high - won't break under normal use
            material.plasticLimit = 15000f; // Even higher
            material.brittleThreshold = 50000f; // Extremely high - rubber doesn't break easily
            return material;
        }
        
        /// <summary>
        /// Creates a super bouncy rubber with maximum bounce and STRONG shape preservation
        /// </summary>
        public static PhysicsMaterial CreateSuperBouncyRubber()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Super_Bouncy_Shape_Preserving_Rubber";
            material.density = 0.8f;
            material.restitution = 0.95f; // Nearly perfect bounce
            material.staticFriction = 0.5f;
            material.dynamicFriction = 0.4f;
            material.deformationType = DeformationType.Elastic;
            material.stiffness = 2500f; // INCREASED from 800f - better shape preservation
            material.damping = 40f; // INCREASED from 10f - more stability
            material.elasticLimit = 20000f; // Extremely high limits
            material.plasticLimit = 30000f;
            material.brittleThreshold = 100000f; // Never breaks under normal use
            return material;
        }
        
        /// <summary>
        /// Creates a flexible foam material that compresses but doesn't break
        /// </summary>
        public static PhysicsMaterial CreateFoam()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Flexible_Foam";
            material.density = 0.3f; // Light
            material.restitution = 0.4f; // Moderate bounce
            material.staticFriction = 0.8f;
            material.dynamicFriction = 0.7f;
            material.deformationType = DeformationType.Elastic;
            material.stiffness = 200f; // Very soft
            material.damping = 50f; // High damping for foam
            material.elasticLimit = 5000f; // High limits - foam compresses but doesn't break
            material.plasticLimit = 8000f;
            material.brittleThreshold = 15000f;
            return material;
        }
        
        /// <summary>
        /// Creates a jelly/gel material that wobbles but doesn't break
        /// </summary>
        public static PhysicsMaterial CreateJelly()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Wobbly_Jelly";
            material.density = 1.1f;
            material.restitution = 0.3f; // Low bounce
            material.staticFriction = 0.2f; // Slippery
            material.dynamicFriction = 0.1f;
            material.deformationType = DeformationType.Elastic;
            material.stiffness = 300f; // Soft and wobbly
            material.damping = 40f; // Moderate damping for wobble effect
            material.elasticLimit = 8000f; // High - jelly stretches but doesn't break easily
            material.plasticLimit = 12000f;
            material.brittleThreshold = 25000f;
            return material;
        }
        
        /// <summary>
        /// Creates a stretchy elastic material like a rubber band
        /// </summary>
        public static PhysicsMaterial CreateStretchyElastic()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Stretchy_Elastic";
            material.density = 0.9f;
            material.restitution = 0.7f;
            material.staticFriction = 0.6f;
            material.dynamicFriction = 0.5f;
            material.deformationType = DeformationType.Elastic;
            material.stiffness = 600f; // Moderate stiffness
            material.damping = 15f; // Low damping for elasticity
            material.elasticLimit = 15000f; // Very high - can stretch a lot
            material.plasticLimit = 20000f;
            material.brittleThreshold = 40000f; // Very tough
            return material;
        }
        
        /// <summary>
        /// Creates a memory foam material that slowly returns to shape
        /// </summary>
        public static PhysicsMaterial CreateMemoryFoam()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Memory_Foam";
            material.density = 0.5f;
            material.restitution = 0.1f; // Very low bounce
            material.staticFriction = 0.9f;
            material.dynamicFriction = 0.8f;
            material.deformationType = DeformationType.Elastic;
            material.stiffness = 100f; // Very soft
            material.damping = 80f; // High damping for slow recovery
            material.elasticLimit = 3000f; // Lower but still safe
            material.plasticLimit = 5000f;
            material.brittleThreshold = 10000f;
            return material;
        }
    }
}