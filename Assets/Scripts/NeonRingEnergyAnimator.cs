using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class NeonRingEnergyAnimator : MonoBehaviour
    {
        private const float Tau = Mathf.PI * 2.0f;

        [Header("Links")]
        public Renderer ringRenderer;
        public EnergySphereController controller;

        [Header("Ring")]
        public float baseRadius = 2.0f;
        public float radiusPulseAmp = 0.010f;
        public float radiusPulseSpeed = 0.55f;

        [Header("Emission")]
        public float exposurePulseAmp = 0.16f;
        public float corePulseAmp = 0.18f;
        public float glowPulseAmp = 0.22f;
        public float instabilityPulseAmp = 0.18f;

        [Header("Colour Drift")]
        public float baseWarmAngle = 0.16f;
        public float warmAngleDriftAmp = 0.11f;
        public float warmAngleDriftSpeed = 0.10f;

        private MaterialPropertyBlock block;
        private Material sourceMaterial;

        private float baseExposure = 0.32f;
        private float baseCoreIntensity = 16.0f;
        private float baseWhiteCoreIntensity = 64.0f;
        private float baseUnderRingGlowIntensity = 22.0f;
        private float basePinkIntensity = 5.8f;
        private float baseMagentaIntensity = 2.15f;
        private float basePurpleIntensity = 1.55f;
        private float baseBlueIntensity = 0.42f;
        private float baseBloomIntensity = 0.11f;
        private float baseAtmosIntensity = 0.012f;
        private float baseInstability = 0.18f;

        private void Awake()
        {
            CaptureBaseValues();
        }

        private void OnEnable()
        {
            CaptureBaseValues();
        }

        private void Update()
        {
            ApplyState(Time.time);
        }

        public void ApplyState(float time)
        {
            EnsureLinks();
            block ??= new MaterialPropertyBlock();
            if (ringRenderer == null)
                return;

            CaptureBaseValues();

            float envelope = controller != null ? controller.CurrentEnvelope : 1.0f;
            if (controller != null)
                envelope = Mathf.Max(envelope, controller.EvaluateEnvelope(time));

            float slowPulse = Smooth01(0.5f + 0.5f * Mathf.Sin(time * radiusPulseSpeed * Tau - Mathf.PI * 0.5f));
            float shimmer = 0.5f + 0.5f * Mathf.Sin(time * 1.37f + 0.8f);
            float micro = 0.5f + 0.5f * Mathf.Sin(time * 4.70f + 1.4f);
            float energy = Mathf.Lerp(0.72f, 1.0f, Mathf.Clamp01(envelope));

            float ringPulse = 1.0f + radiusPulseAmp * Mathf.Lerp(-0.7f, 1.0f, slowPulse);
            float corePulse = 1.0f + corePulseAmp * (shimmer - 0.5f) * 0.55f + micro * 0.045f;
            float glowPulse = 1.0f + glowPulseAmp * (slowPulse - 0.45f);
            float warmAngle = baseWarmAngle
                + warmAngleDriftAmp * 0.55f * Mathf.Sin(time * warmAngleDriftSpeed * Tau)
                + warmAngleDriftAmp * 0.18f * Mathf.Sin(time * warmAngleDriftSpeed * 2.0f * Tau + 1.0f);

            ringRenderer.GetPropertyBlock(block);
            SetFloat("_RingRadius", baseRadius * ringPulse);
            SetFloat("_Exposure", baseExposure * energy * (1.0f + exposurePulseAmp * (slowPulse - 0.35f)));
            SetFloat("_WarmAngle", warmAngle);
            SetFloat("_CoreIntensity", baseCoreIntensity * energy * corePulse);
            SetFloat("_WhiteCoreIntensity", baseWhiteCoreIntensity * energy * corePulse);
            SetFloat("_UnderRingGlowIntensity", baseUnderRingGlowIntensity * energy * glowPulse);
            SetFloat("_PinkIntensity", basePinkIntensity * energy * glowPulse);
            SetFloat("_MagentaIntensity", baseMagentaIntensity * energy * glowPulse);
            SetFloat("_PurpleIntensity", basePurpleIntensity * energy * (0.94f + 0.10f * slowPulse));
            SetFloat("_BlueIntensity", baseBlueIntensity * energy * (0.92f + 0.16f * (1.0f - slowPulse)));
            SetFloat("_BloomIntensity", baseBloomIntensity * energy * glowPulse);
            SetFloat("_AtmosIntensity", baseAtmosIntensity * energy * (0.85f + 0.30f * slowPulse));
            SetFloat("_Instability", baseInstability * (1.0f + instabilityPulseAmp * micro));
            ringRenderer.SetPropertyBlock(block);
        }

        private void EnsureLinks()
        {
            if (controller == null)
                controller = GetComponent<EnergySphereController>();

            if (ringRenderer != null)
                return;

            Transform ring = transform.Find("Multi Layer Ring Quad");
            if (ring != null)
                ringRenderer = ring.GetComponent<Renderer>();
        }

        private void CaptureBaseValues()
        {
            EnsureLinks();
            if (ringRenderer == null || ringRenderer.sharedMaterial == null)
                return;

            Material mat = ringRenderer.sharedMaterial;
            if (sourceMaterial == mat)
                return;

            sourceMaterial = mat;
            baseRadius = GetFloat(mat, "_RingRadius", baseRadius);
            baseExposure = GetFloat(mat, "_Exposure", baseExposure);
            baseWarmAngle = GetFloat(mat, "_WarmAngle", baseWarmAngle);
            baseCoreIntensity = GetFloat(mat, "_CoreIntensity", baseCoreIntensity);
            baseWhiteCoreIntensity = GetFloat(mat, "_WhiteCoreIntensity", baseWhiteCoreIntensity);
            baseUnderRingGlowIntensity = GetFloat(mat, "_UnderRingGlowIntensity", baseUnderRingGlowIntensity);
            basePinkIntensity = GetFloat(mat, "_PinkIntensity", basePinkIntensity);
            baseMagentaIntensity = GetFloat(mat, "_MagentaIntensity", baseMagentaIntensity);
            basePurpleIntensity = GetFloat(mat, "_PurpleIntensity", basePurpleIntensity);
            baseBlueIntensity = GetFloat(mat, "_BlueIntensity", baseBlueIntensity);
            baseBloomIntensity = GetFloat(mat, "_BloomIntensity", baseBloomIntensity);
            baseAtmosIntensity = GetFloat(mat, "_AtmosIntensity", baseAtmosIntensity);
            baseInstability = GetFloat(mat, "_Instability", baseInstability);
        }

        private void SetFloat(string propertyName, float value)
        {
            if (sourceMaterial == null || !sourceMaterial.HasProperty(propertyName))
                return;

            block.SetFloat(propertyName, value);
        }

        private static float GetFloat(Material mat, string propertyName, float fallback)
        {
            return mat != null && mat.HasProperty(propertyName) ? mat.GetFloat(propertyName) : fallback;
        }

        private static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3.0f - 2.0f * x);
        }
    }
}
