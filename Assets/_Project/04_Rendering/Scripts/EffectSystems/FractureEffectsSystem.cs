// Assets/_Project/04_Rendering/Scripts/FractureEffectsSystem.cs

using System.Collections;
using System.Collections.Generic;
using _Project._01_Physics.Scripts.PBD_V1;
using UnityEngine;

namespace _Project._04_Rendering.Scripts.EffectSystems
{
    /// <summary>
    /// Advanced visual effects system for fracture mechanics
    /// </summary>
    public class FractureEffectsSystem : MonoBehaviour
    {
        [Header("Particle Effects")]
        [SerializeField] private bool enableParticleEffects = true;
        [SerializeField] private int maxParticles = 100;
        [SerializeField] private float particleLifetime = 3f;
        [SerializeField] private float particleSpeed = 8f;
        [SerializeField] private float particleSize = 0.05f;
        
        [Header("Fragment Effects")]
        [SerializeField] private bool enableFragments = true;
        [SerializeField] private int maxFragments = 20;
        [SerializeField] private float fragmentLifetime = 10f;
        [SerializeField] private float fragmentSpeedMultiplier = 1.5f;
        
        [Header("Flash Effects")]
        [SerializeField] private bool enableFlashEffect = true;
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashIntensity = 2f;
        
        [Header("Screen Effects")]
        [SerializeField] private bool enableScreenShake = true;
        [SerializeField] private float shakeIntensity = 0.3f;
        [SerializeField] private float shakeDuration = 0.5f;
        
        [Header("Audio Effects")]
        [SerializeField] private bool enableAudioEffects = true;
        [SerializeField] private AudioClip[] fractureAudioClips;
        [SerializeField] private float audioVolume = 0.7f;
        
        // Internal components
        private PBDSoftBody softBody;
        private AudioSource audioSource;
        private ParticleSystem particleSystem;
        private Camera mainCamera;
        private List<GameObject> activeFragments;
        
        // Effect state
        private bool hasTriggeredEffects = false;
        private MaterialPropertyBlock flashPropertyBlock;
        private Renderer objectRenderer;
        private Material originalMaterial;
        
        void Start()
        {
            Initialize();
        }
        
        void Initialize()
        {
            softBody = GetComponent<PBDSoftBody>();
            if (softBody == null)
            {
                Debug.LogError("FractureEffectsSystem requires PBDSoftBody component!");
                enabled = false;
                return;
            }
            
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                originalMaterial = objectRenderer.material;
                flashPropertyBlock = new MaterialPropertyBlock();
            }
            
            // Setup audio
            if (enableAudioEffects)
            {
                SetupAudioSystem();
            }
            
            // Setup particle system
            if (enableParticleEffects)
            {
                SetupParticleSystem();
            }
            
            // Find main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            
            activeFragments = new List<GameObject>();
            
            Debug.Log($"Fracture effects system initialized for {gameObject.name}");
        }
        
        void SetupAudioSystem()
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.volume = audioVolume;
            audioSource.spatialBlend = 0.8f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 20f;
        }
        
        void SetupParticleSystem()
        {
            GameObject particleObj = new GameObject("FractureParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.zero;
            
            particleSystem = particleObj.AddComponent<ParticleSystem>();
            
            // Configure particle system
            var main = particleSystem.main;
            main.startLifetime = particleLifetime;
            main.startSpeed = particleSpeed;
            main.startSize = particleSize;
            main.startColor = Color.white;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // Shape
            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;
            
            // Emission (manual control)
            var emission = particleSystem.emission;
            emission.enabled = false;
            
            // Velocity over lifetime
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2f);
            
            // Size over lifetime
            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // Color over lifetime
            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.gray, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = gradient;
        }
        
        void Update()
        {
            // Check for fracture
            if (!hasTriggeredEffects && softBody != null && softBody.IsFractured)
            {
                TriggerFractureEffects();
            }
            
            // Clean up destroyed fragments
            CleanupFragments();
        }
        
        public void TriggerFractureEffects()
        {
            if (hasTriggeredEffects) return;
            
            hasTriggeredEffects = true;
            
            Debug.Log($"Triggering fracture effects for {gameObject.name}");
            
            // Trigger all effects
            if (enableParticleEffects)
                TriggerParticleEffect();
            
            if (enableFragments)
                CreateFragments();
            
            if (enableFlashEffect)
                StartCoroutine(FlashEffect());
            
            if (enableScreenShake && mainCamera != null)
                StartCoroutine(ScreenShakeEffect());
            
            if (enableAudioEffects)
                TriggerAudioEffect();
        }
        
        void TriggerParticleEffect()
        {
            if (particleSystem == null) return;
            
            // Burst particles
            var emission = particleSystem.emission;
            particleSystem.Emit(maxParticles);
            
            Debug.Log("Particle fracture effect triggered");
        }
        
        void CreateFragments()
        {
            if (softBody?.Solver == null) return;
            
            // Create fragments based on fractured particles
            var fracturedParticles = softBody.Solver.GetFracturedParticles();
            int fragmentsToCreate = Mathf.Min(maxFragments, fracturedParticles.Count);
            
            for (int i = 0; i < fragmentsToCreate; i++)
            {
                if (i < fracturedParticles.Count)
                {
                    int particleIndex = fracturedParticles[i];
                    if (particleIndex < softBody.Solver.Particles.Count)
                    {
                        var particle = softBody.Solver.Particles[particleIndex];
                        CreateSingleFragment(particle.Position);
                    }
                }
            }
            
            Debug.Log($"Created {fragmentsToCreate} fragments");
        }
        
        void CreateSingleFragment(Vector3 position)
        {
            // Create fragment geometry
            GameObject fragment = CreateFragmentGeometry();
            fragment.transform.position = position;
            fragment.transform.rotation = Random.rotation;
            
            // Add physics
            Rigidbody rb = fragment.AddComponent<Rigidbody>();
            rb.mass = Random.Range(0.01f, 0.1f);
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            
            // Add random force
            Vector3 randomForce = Random.insideUnitSphere * fragmentSpeedMultiplier;
            randomForce.y = Mathf.Abs(randomForce.y); // Ensure upward component
            rb.AddForce(randomForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            
            // Set material to match parent
            if (objectRenderer != null)
            {
                Renderer fragmentRenderer = fragment.GetComponent<Renderer>();
                if (fragmentRenderer != null)
                {
                    fragmentRenderer.material = objectRenderer.material;
                }
            }
            
            activeFragments.Add(fragment);
            
            // Auto-destroy after lifetime
            StartCoroutine(DestroyFragmentAfterTime(fragment, fragmentLifetime));
        }
        
        GameObject CreateFragmentGeometry()
        {
            // Create random fragment shape
            int shapeType = Random.Range(0, 3);
            GameObject fragment;
            
            switch (shapeType)
            {
                case 0: // Cube fragment
                    fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    fragment.transform.localScale = Vector3.one * Random.Range(0.05f, 0.15f);
                    break;
                    
                case 1: // Sphere fragment
                    fragment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    fragment.transform.localScale = Vector3.one * Random.Range(0.03f, 0.12f);
                    break;
                    
                default: // Irregular fragment (scaled cube)
                    fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    fragment.transform.localScale = new Vector3(
                        Random.Range(0.02f, 0.1f),
                        Random.Range(0.02f, 0.1f),
                        Random.Range(0.02f, 0.1f)
                    );
                    break;
            }
            
            fragment.name = "FractureFragment";
            return fragment;
        }
        
        IEnumerator DestroyFragmentAfterTime(GameObject fragment, float time)
        {
            yield return new WaitForSeconds(time);
            
            if (fragment != null)
            {
                activeFragments.Remove(fragment);
                
                // Fade out effect
                StartCoroutine(FadeOutFragment(fragment));
            }
        }
        
        IEnumerator FadeOutFragment(GameObject fragment)
        {
            Renderer renderer = fragment.GetComponent<Renderer>();
            if (renderer == null)
            {
                Destroy(fragment);
                yield break;
            }
            
            Material fragmentMaterial = renderer.material;
            Color originalColor = fragmentMaterial.color;
            
            float fadeTime = 1f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(originalColor.a, 0f, elapsed / fadeTime);
                
                Color newColor = originalColor;
                newColor.a = alpha;
                fragmentMaterial.color = newColor;
                
                yield return null;
            }
            
            Destroy(fragment);
        }
        
        IEnumerator FlashEffect()
        {
            if (objectRenderer == null) yield break;
            
            // Flash the object bright
            flashPropertyBlock.SetColor("_EmissionColor", flashColor * flashIntensity);
            objectRenderer.SetPropertyBlock(flashPropertyBlock);
            
            yield return new WaitForSeconds(flashDuration);
            
            // Reset emission
            flashPropertyBlock.SetColor("_EmissionColor", Color.black);
            objectRenderer.SetPropertyBlock(flashPropertyBlock);
            
            Debug.Log("Flash effect completed");
        }
        
        IEnumerator ScreenShakeEffect()
        {
            if (mainCamera == null) yield break;
            
            Vector3 originalPosition = mainCamera.transform.position;
            float elapsed = 0f;
            
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                
                // Calculate shake intensity (decreases over time)
                float currentIntensity = shakeIntensity * (1f - elapsed / shakeDuration);
                
                // Apply random offset
                Vector3 randomOffset = Random.insideUnitSphere * currentIntensity;
                mainCamera.transform.position = originalPosition + randomOffset;
                
                yield return null;
            }
            
            // Reset camera position
            mainCamera.transform.position = originalPosition;
            
            Debug.Log("Screen shake effect completed");
        }
        
        void TriggerAudioEffect()
        {
            if (audioSource == null || fractureAudioClips == null || fractureAudioClips.Length == 0)
                return;
            
            // Play random fracture sound
            AudioClip clipToPlay = fractureAudioClips[Random.Range(0, fractureAudioClips.Length)];
            audioSource.PlayOneShot(clipToPlay, audioVolume);
            
            Debug.Log("Audio fracture effect triggered");
        }
        
        void CleanupFragments()
        {
            activeFragments.RemoveAll(fragment => fragment == null);
        }
        
        public void ResetEffects()
        {
            hasTriggeredEffects = false;
            
            // Destroy all fragments
            foreach (var fragment in activeFragments)
            {
                if (fragment != null)
                    Destroy(fragment);
            }
            activeFragments.Clear();
            
            // Reset material
            if (objectRenderer != null && flashPropertyBlock != null)
            {
                flashPropertyBlock.SetColor("_EmissionColor", Color.black);
                objectRenderer.SetPropertyBlock(flashPropertyBlock);
            }
            
            // Stop particle system
            if (particleSystem != null)
            {
                particleSystem.Stop();
                particleSystem.Clear();
            }
            
            Debug.Log("Fracture effects reset");
        }
        
        void OnDestroy()
        {
            // Clean up fragments when object is destroyed
            foreach (var fragment in activeFragments)
            {
                if (fragment != null)
                    Destroy(fragment);
            }
        }
        
        #region Public Configuration Methods
        
        public void SetParticleEffectEnabled(bool enabled)
        {
            enableParticleEffects = enabled;
        }
        
        public void SetFragmentEffectEnabled(bool enabled)
        {
            enableFragments = enabled;
        }
        
        public void SetScreenShakeEnabled(bool enabled)
        {
            enableScreenShake = enabled;
        }
        
        public void SetAudioEffectEnabled(bool enabled)
        {
            enableAudioEffects = enabled;
        }
        
        public void SetEffectIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            
            shakeIntensity = 0.3f * intensity;
            flashIntensity = 2f * intensity;
            particleSpeed = 8f * intensity;
            fragmentSpeedMultiplier = 1.5f * intensity;
        }
        
        #endregion
    }
}