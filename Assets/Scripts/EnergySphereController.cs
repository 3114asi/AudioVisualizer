using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class EnergySphereController : MonoBehaviour
    {
        private const int HotspotCount = 6;

        [Header("Targets")]
        public Renderer ringRenderer;
        public Renderer innerRenderer;
        public InnerEnergyMesh innerMesh;
        public Transform sphereTransform;

        [Header("Ring")]
        public float ringRadius = 2.0f;
        public float ringPulseAmp = 0.026f;
        public float ringPulseSpeed = 1.15f;
        public float ringNoiseAmp = 0.018f;
        public float ringNoiseSpeed = 0.32f;

        [Header("Pulse")]
        public float globalPulseSpeed = 0.85f;
        public float globalPulseAmp = 0.10f;
        public float corePulseAmp = 0.18f;

        [Header("Color Drift")]
        public float warmAngleDriftSpeed = 0.10f;
        public float warmAngleDriftAmp = 0.18f;
        public float baseWarmAngle = 0.28f;

        [Header("Hotspots")]
        public float hotspotIntensityBase = 7.0f;
        public float hotspotCycleSpeed = 0.72f;
        public float hotspotWidth = 0.085f;

        [Header("Intensity")]
        public float masterIntensity = 1.0f;
        public float intensityBreathSpeed = 0.52f;
        public float intensityBreathAmp = 0.09f;

        private readonly float[] hotspotAngles =
        {
            0.05f,   // right
            0.72f,   // upper-right
            -1.55f,  // bottom
            -2.36f,  // lower-left
            1.42f,   // top
            3.02f    // left
        };

        private readonly float[] hotspotPhase =
        {
            0.10f, 0.36f, 0.58f, 0.74f, 0.91f, 0.23f
        };

        private MaterialPropertyBlock ringMPB;
        private MaterialPropertyBlock innerMPB;

        private void Awake()
        {
            EnsureBlocks();
        }

        private void Update()
        {
            ApplyState(Time.time);
        }

        public void ApplyState(float time)
        {
            EnsureBlocks();

            float breath = 1.0f + intensityBreathAmp * Mathf.Sin(time * intensityBreathSpeed * Mathf.PI * 2.0f);
            float pulse = 1.0f
                + globalPulseAmp * Mathf.Sin(time * globalPulseSpeed * Mathf.PI * 2.0f)
                + globalPulseAmp * 0.45f * Mathf.Sin(time * globalPulseSpeed * 1.73f * Mathf.PI * 2.0f + 0.6f);
            float microPulse = 1.0f + corePulseAmp * Mathf.Sin(time * 3.2f + 1.1f) * 0.5f;

            float warmAngle = baseWarmAngle
                + warmAngleDriftAmp * Mathf.Sin(time * warmAngleDriftSpeed * Mathf.PI * 2.0f)
                + warmAngleDriftAmp * 0.32f * Mathf.Sin(time * warmAngleDriftSpeed * 2.1f * Mathf.PI * 2.0f + 1.0f);

            if (ringRenderer != null)
            {
                ringRenderer.GetPropertyBlock(ringMPB);
                ringMPB.SetFloat("_EffectTime", time);
                ringMPB.SetFloat("_RingRadius", ringRadius);
                ringMPB.SetFloat("_PulseAmp", ringPulseAmp);
                ringMPB.SetFloat("_PulseSpeed", ringPulseSpeed);
                ringMPB.SetFloat("_NoiseAmp", ringNoiseAmp * (0.85f + 0.25f * Mathf.Sin(time * 1.27f)));
                ringMPB.SetFloat("_NoiseSpeed", ringNoiseSpeed);
                ringMPB.SetFloat("_WarmAngle", warmAngle);
                ringMPB.SetFloat("_HotspotWidth", hotspotWidth);
                ringMPB.SetFloat("_HotspotIntensity", hotspotIntensityBase * (0.9f + 0.35f * microPulse));
                ringMPB.SetFloat("_CoreIntensity", 15.5f * pulse * breath * masterIntensity);
                ringMPB.SetFloat("_GlowIntensity", 3.7f * pulse * breath * masterIntensity);
                ringMPB.SetFloat("_HaloIntensity", 0.72f * breath * masterIntensity);
                ringMPB.SetFloat("_AtmosIntensity", 0.10f * breath * masterIntensity);
                ringMPB.SetFloat("_Exposure", masterIntensity * breath);

                for (int i = 0; i < HotspotCount; i++)
                {
                    float shifted = Mathf.Repeat(time * hotspotCycleSpeed + hotspotPhase[i], 1.0f);
                    float flash = Mathf.Pow(Mathf.Sin(shifted * Mathf.PI), 3.2f);
                    float gate = shifted < 0.62f ? 1.0f : 0.0f;
                    float bias = (i == 0 || i == 1 || i == 2 || i == 3) ? 1.15f : 0.85f;
                    ringMPB.SetFloat("_HotspotAngle" + i, hotspotAngles[i]);
                    ringMPB.SetFloat("_HotspotPower" + i, flash * gate * bias);
                }

                ringRenderer.SetPropertyBlock(ringMPB);
            }

            if (innerRenderer != null)
            {
                innerRenderer.GetPropertyBlock(innerMPB);
                innerMPB.SetFloat("_EffectTime", time);
                innerMPB.SetFloat("_PulseSpeed", globalPulseSpeed);
                innerMPB.SetFloat("_PulseAmp", globalPulseAmp);
                innerMPB.SetFloat("_InnerRadius", ringRadius * 0.965f);
                innerMPB.SetFloat("_Exposure", masterIntensity * breath * 0.72f);
                innerRenderer.SetPropertyBlock(innerMPB);
            }

            if (innerMesh != null)
            {
                innerMesh.ApplyState(time, ringRadius, masterIntensity * breath);
            }

            if (sphereTransform != null)
            {
                float s = 1.0f
                    + ringPulseAmp * 0.18f * Mathf.Sin(time * ringPulseSpeed * 0.7f * Mathf.PI * 2.0f)
                    + ringPulseAmp * 0.10f * Mathf.Sin(time * ringPulseSpeed * 1.9f * Mathf.PI * 2.0f + 0.8f);
                sphereTransform.localScale = Vector3.one * s;
            }
        }

        private void EnsureBlocks()
        {
            ringMPB ??= new MaterialPropertyBlock();
            innerMPB ??= new MaterialPropertyBlock();
        }
    }
}
