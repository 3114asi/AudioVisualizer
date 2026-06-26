using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ediskrad.AudioVisualizer
{
    public sealed class PortalPulseController : MonoBehaviour
    {
        [Header("Targets")]
        public Renderer[] emissionRenderers;
        public Light[] accentLights;
        public Volume postProcessVolume;

        [Header("Pulse")]
        [Min(0.1f)] public float loopDuration = 10f;
        public float baseEmission = 2.2f;
        public float pulseEmission = 1.4f;
        public AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Bloom")]
        public float baseBloomIntensity = 1.05f;
        public float pulseBloomIntensity = 0.65f;

        private Bloom bloom;

        private void Awake()
        {
            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out bloom);
            }
        }

        private void Update()
        {
            float normalized = Mathf.Repeat(Time.time / loopDuration, 1f);
            float wave = 0.5f + 0.5f * Mathf.Sin(normalized * Mathf.PI * 2f);
            float pulse = 1f + pulseCurve.Evaluate(wave) * pulseEmission;
            float intensity = baseEmission * pulse;

            if (emissionRenderers != null)
            {
                foreach (Renderer target in emissionRenderers)
                {
                    if (target == null) continue;
                    foreach (Material material in target.materials)
                    {
                        if (material == null) continue;
                        material.SetFloat("_Pulse", pulse);
                        material.SetFloat("_Intensity", intensity);
                    }
                }
            }

            if (accentLights != null)
            {
                foreach (Light accentLight in accentLights)
                {
                    if (accentLight == null) continue;
                    accentLight.intensity = intensity * 0.35f;
                }
            }

            if (bloom != null)
            {
                bloom.intensity.value = baseBloomIntensity + pulseBloomIntensity * wave;
            }
        }
    }
}
