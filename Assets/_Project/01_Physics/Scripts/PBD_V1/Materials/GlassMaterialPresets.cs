// Assets/_Project/01_Physics/Scripts/PBD/GlassMaterialPresets.cs

using _Project._00_Core.Scripts.DataStructures;
using UnityEngine;
using PhysicsMaterial = _Project._00_Core.Scripts.DataStructures.PhysicsMaterial;

namespace _Project._01_Physics.Scripts.PBD_V1.Materials
{
    /// <summary>
    /// Specialized material presets for glass and other brittle materials
    /// </summary>
    public static class GlassMaterialPresets
    {
        /// <summary>
        /// Creates a standard window glass material - brittle and shatters easily
        /// </summary>
        public static PhysicsMaterial CreateWindowGlass()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Window_Glass";
            material.density = 2.5f; // Typical glass density (g/cm³)
            material.restitution = 0.05f; // Glass doesn't bounce much
            material.staticFriction = 0.7f;
            material.dynamicFriction = 0.5f;
            material.deformationType = DeformationType.Brittle;
            material.stiffness = 3000f; // Very rigid
            material.damping = 80f; // High damping for stability
            material.elasticLimit = 50f; // Very low - glass breaks easily
            material.plasticLimit = 60f; // Minimal plastic deformation
            material.brittleThreshold = 100f; // Shatters at low force
            return material;
        }
        
        /// <summary>
        /// Creates tempered glass material - stronger than window glass but still brittle
        /// </summary>
        public static PhysicsMaterial CreateTemperedGlass()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Tempered_Glass";
            material.density = 2.5f;
            material.restitution = 0.1f; // Slightly more bouncy
            material.staticFriction = 0.8f;
            material.dynamicFriction = 0.6f;
            material.deformationType = DeformationType.Brittle;
            material.stiffness = 4000f; // More rigid than window glass
            material.damping = 100f;
            material.elasticLimit = 150f; // Higher than window glass
            material.plasticLimit = 180f;
            material.brittleThreshold = 300f; // Requires more force to break
            return material;
        }
        
        /// <summary>
        /// Creates crystal glass material - very pure and brittle
        /// </summary>
        public static PhysicsMaterial CreateCrystalGlass()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Crystal_Glass";
            material.density = 2.8f; // Higher density for crystal
            material.restitution = 0.15f; // Crystal can be slightly more resonant
            material.staticFriction = 0.9f; // Very smooth surface
            material.dynamicFriction = 0.7f;
            material.deformationType = DeformationType.Brittle;
            material.stiffness = 5000f; // Very rigid and pure
            material.damping = 60f; // Less damping for crystal clarity
            material.elasticLimit = 80f; // Moderate elastic limit
            material.plasticLimit = 90f;
            material.brittleThreshold = 150f; // Pure crystal can be fragile
            return material;
        }
        
        /// <summary>
        /// Creates safety glass material - designed to break into small, less dangerous pieces
        /// </summary>
        public static PhysicsMaterial CreateSafetyGlass()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Safety_Glass";
            material.density = 2.4f; // Slightly lighter due to treatment
            material.restitution = 0.08f;
            material.staticFriction = 0.6f;
            material.dynamicFriction = 0.4f;
            material.deformationType = DeformationType.Brittle;
            material.stiffness = 2500f; // Less rigid for safety
            material.damping = 120f; // Higher damping for controlled breakage
            material.elasticLimit = 200f; // Higher threshold
            material.plasticLimit = 250f;
            material.brittleThreshold = 400f; // Requires significant force
            return material;
        }
        
        /// <summary>
        /// Creates ceramic material - very brittle and hard
        /// </summary>
        public static PhysicsMaterial CreateCeramic()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Ceramic";
            material.density = 3.5f; // Higher density than glass
            material.restitution = 0.02f; // Very little bounce
            material.staticFriction = 0.8f;
            material.dynamicFriction = 0.6f;
            material.deformationType = DeformationType.Brittle;
            material.stiffness = 6000f; // Very rigid
            material.damping = 150f; // High damping
            material.elasticLimit = 30f; // Very low elastic limit
            material.plasticLimit = 40f;
            material.brittleThreshold = 80f; // Shatters easily
            return material;
        }
        
        /// <summary>
        /// Creates ice material - brittle when cold, can fracture dramatically
        /// </summary>
        public static PhysicsMaterial CreateIce()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Ice";
            material.density = 0.9f; // Ice floats on water
            material.restitution = 0.3f; // Can bounce a bit
            material.staticFriction = 0.1f; // Very slippery
            material.dynamicFriction = 0.05f;
            material.deformationType = DeformationType.Brittle;
            material.stiffness = 1000f; // Less rigid than glass
            material.damping = 40f; // Lower damping
            material.elasticLimit = 100f;
            material.plasticLimit = 120f;
            material.brittleThreshold = 200f; // Fractures under moderate force
            return material;
        }
        
        /// <summary>
        /// Creates thin glass sheet material - very fragile
        /// </summary>
        public static PhysicsMaterial CreateThinGlassSheet()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Thin_Glass_Sheet";
            material.density = 2.5f;
            material.restitution = 0.03f; // Minimal bounce
            material.staticFriction = 0.7f;
            material.dynamicFriction = 0.5f;
            material.deformationType = DeformationType.Brittle;
            material.stiffness = 2000f; // Lower stiffness due to thinness
            material.damping = 200f; // High damping for thin material
            material.elasticLimit = 20f; // Extremely low - very fragile
            material.plasticLimit = 25f;
            material.brittleThreshold = 40f; // Breaks with minimal force
            return material;
        }
        
        /// <summary>
        /// Creates a test glass material optimized for demonstrations
        /// </summary>
        public static PhysicsMaterial CreateTestGlass()
        {
            var material = ScriptableObject.CreateInstance<PhysicsMaterial>();
            material.materialName = "Test_Glass";
            material.density = 2.0f; // Lighter for better visual effect
            material.restitution = 0.2f; // A bit more bouncy for drama
            material.staticFriction = 0.6f;
            material.dynamicFriction = 0.4f;
            material.deformationType = DeformationType.Brittle;
            material.stiffness = 1500f; // Moderate stiffness for predictable behavior
            material.damping = 50f; // Lower damping for more dynamic behavior
            material.elasticLimit = 100f; // Tuned for demo purposes
            material.plasticLimit = 120f;
            material.brittleThreshold = 200f; // Breaks with reasonable impact
            return material;
        }
    }
}