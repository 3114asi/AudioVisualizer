using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class EnergySphereController : MonoBehaviour
    {
        private const int HotspotCount = 6;
        private const float Tau = Mathf.PI * 2.0f;

        [Header("Targets")]
        public Renderer ringRenderer;
        public Renderer innerRenderer;
        public InnerEnergyMesh innerMesh;
        public InnerNCSSpectrumSphere innerNCSSphere;
        public Transform sphereTransform;

        [Header("Loop Animation")]
        public float animationDuration = 3.333333f;
        public float fadeInDuration = 0.85f;
        public float fadeOutDuration = 1.05f;
        public float pulseSpeed = 0.30f;
        public float pulseAmplitude = 0.12f;

        [Header("Emission Range")]
        public float emissionMin = 0.16f;
        public float emissionMax = 1.0f;

        [Header("Linked Effects")]
        public float rayIntensity = 0.62f;
        public float particleLifetime = 3.2f;
        public float particleSpeed = 0.34f;

        [Header("Color Weights")]
        [Range(0f, 1.5f)] public float blueColorWeight = 1.05f;
        [Range(0f, 1.5f)] public float pinkColorWeight = 0.88f;

        [Header("Ring")]
        public float ringRadius = 2.0f;
        public float ringPulseAmp = 0.018f;
        public float ringPulseSpeed = 0.55f;
        public float ringNoiseAmp = 0.018f;
        public float ringNoiseSpeed = 0.18f;

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

        public float CurrentEnvelope { get; private set; } = 1.0f;
        public float CurrentEmission { get; private set; } = 1.0f;
        public float CurrentCycle01 { get; private set; }
        public float CurrentCycleTime { get; private set; }

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

            CurrentCycleTime = Mathf.Repeat(time, Mathf.Max(0.01f, animationDuration));
            CurrentCycle01 = CurrentCycleTime / Mathf.Max(0.01f, animationDuration);
            CurrentEnvelope = EvaluateEnvelope(time);

            float slowPulse = 0.5f + 0.5f * Mathf.Sin(time * pulseSpeed * Tau - Mathf.PI * 0.5f);
            slowPulse = Smooth01(slowPulse);
            float breath = 1.0f + pulseAmplitude * Mathf.Lerp(-0.45f, 1.0f, slowPulse);
            float emission = Mathf.Lerp(emissionMin, emissionMax, CurrentEnvelope) * breath * masterIntensity;
            CurrentEmission = emission;

            float pulse = 1.0f + pulseAmplitude * 0.55f * Mathf.Sin(time * pulseSpeed * Tau);
            float microPulse = 1.0f + corePulseAmp * 0.25f * Mathf.Sin(time * 1.15f + 1.1f);

            float warmAngle = baseWarmAngle
                + warmAngleDriftAmp * 0.55f * Mathf.Sin(time * warmAngleDriftSpeed * Tau)
                + warmAngleDriftAmp * 0.18f * Mathf.Sin(time * warmAngleDriftSpeed * 2.0f * Tau + 1.0f);

            if (ringRenderer != null)
            {
                ringRenderer.GetPropertyBlock(ringMPB);
                ringMPB.SetFloat("_EffectTime", time);
                ringMPB.SetFloat("_RingRadius", ringRadius);
                ringMPB.SetFloat("_PulseAmp", ringPulseAmp);
                ringMPB.SetFloat("_PulseSpeed", ringPulseSpeed);
                ringMPB.SetFloat("_NoiseAmp", ringNoiseAmp * (0.70f + 0.30f * Smooth01(Mathf.Sin(time * 0.45f) * 0.5f + 0.5f)));
                ringMPB.SetFloat("_NoiseSpeed", ringNoiseSpeed);
                ringMPB.SetFloat("_WarmAngle", warmAngle);
                ringMPB.SetFloat("_AngleStrength", pinkColorWeight);
                ringMPB.SetFloat("_BlueColorWeight", blueColorWeight);
                ringMPB.SetFloat("_PinkColorWeight", pinkColorWeight);
                ringMPB.SetFloat("_HotspotWidth", hotspotWidth);
                ringMPB.SetFloat("_HotspotIntensity", hotspotIntensityBase * rayIntensity * (0.85f + 0.20f * microPulse));
                ringMPB.SetFloat("_CoreIntensity", 15.5f * pulse * emission);
                ringMPB.SetFloat("_GlowIntensity", 3.7f * pulse * emission);
                ringMPB.SetFloat("_HaloIntensity", 0.72f * emission);
                ringMPB.SetFloat("_AtmosIntensity", 0.10f * emission);
                ringMPB.SetFloat("_Exposure", emission);

                for (int i = 0; i < HotspotCount; i++)
                {
                    float shifted = Mathf.Repeat(time * hotspotCycleSpeed * 0.42f + hotspotPhase[i], 1.0f);
                    float flash = SmoothPulse(shifted, 0.12f, 0.58f, 0.96f);
                    float bias = (i == 0 || i == 1 || i == 2 || i == 3) ? 1.15f : 0.85f;
                    ringMPB.SetFloat("_HotspotAngle" + i, hotspotAngles[i]);
                    ringMPB.SetFloat("_HotspotPower" + i, flash * bias * CurrentEnvelope * rayIntensity);
                }

                ringRenderer.SetPropertyBlock(ringMPB);
            }

            if (innerNCSSphere != null)
            {
                innerNCSSphere.ApplyState(time, ringRadius, emission);
            }
            else if (innerRenderer != null)
            {
                innerRenderer.GetPropertyBlock(innerMPB);
                innerMPB.SetFloat("_EffectTime", time);
                innerMPB.SetFloat("_PulseSpeed", pulseSpeed);
                innerMPB.SetFloat("_PulseAmp", pulseAmplitude * 0.65f);
                innerMPB.SetFloat("_InnerRadius", ringRadius * 0.965f);
                innerMPB.SetFloat("_Exposure", emission * 0.72f);
                innerRenderer.SetPropertyBlock(innerMPB);
            }

            if (innerMesh != null)
            {
                innerMesh.ApplyState(time, ringRadius, emission);
            }

            if (sphereTransform != null)
            {
                float s = 1.0f
                    + pulseAmplitude * 0.018f * Mathf.Sin(time * pulseSpeed * Tau)
                    + ringPulseAmp * 0.08f * Mathf.Sin(time * ringPulseSpeed * Tau + 0.8f);
                sphereTransform.localScale = Vector3.one * s;
            }
        }

        public float EvaluateEnvelope(float time)
        {
            float duration = Mathf.Max(0.01f, animationDuration);
            float cycleTime = Mathf.Repeat(time, duration);
            float fadeIn = Smooth01(Mathf.Clamp01(cycleTime / Mathf.Max(0.01f, fadeInDuration)));
            float fadeOut = Smooth01(Mathf.Clamp01((duration - cycleTime) / Mathf.Max(0.01f, fadeOutDuration)));
            return Mathf.Clamp01(Mathf.Min(fadeIn, fadeOut));
        }

        private static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3.0f - 2.0f * x);
        }

        private static float SmoothPulse(float phase, float attackEnd, float holdEnd, float releaseEnd)
        {
            float attack = Smooth01(Mathf.InverseLerp(0.0f, attackEnd, phase));
            float release = 1.0f - Smooth01(Mathf.InverseLerp(holdEnd, releaseEnd, phase));
            return Mathf.Clamp01(Mathf.Min(attack, release));
        }

        private void EnsureBlocks()
        {
            ringMPB ??= new MaterialPropertyBlock();
            innerMPB ??= new MaterialPropertyBlock();
        }
    }
}
