using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class InnerNCSSpectrumSphere : MonoBehaviour
    {
        [Header("Target")]
        public Renderer targetRenderer;

        [Header("Shape")]
        public float radiusScale = 0.958f;
        public float coreDark = 0.60f;
        public float shellWidth = 0.45f;
        public float edgeFeather = 0.055f;
        public float edgeBias = 2.75f;
        public float coreAbsorb = 0.35f;
        public float absorbRadius = 0.68f;

        [Header("InnerEnergyMembrane")]
        public float membraneFill = 0.060f;
        public float membraneIntensity = 0.48f;
        public float fresnelPower = 2.3f;

        [Header("SpectrumRibbonLayer")]
        public float bandIntensity = 0.40f;
        public float bandSharpness = 155.0f;
        public float waveAmp = 0.040f;
        public float ribbon1R = 0.954f;
        public float ribbon2R = 0.884f;
        public float ribbon3R = 0.812f;
        public float fineThreadIntensity = 0.035f;

        [Header("InnerParticleField")]
        public float dotIntensity = 1.10f;
        public float dotDensity = 165.0f;
        public float dotThreshold = 0.860f;
        public float dotCenterFill = 0.0f;
        public float particleDrift = 0.055f;

        [Header("ProceduralNoiseFlow")]
        public float noiseAmp = 0.085f;
        public float noiseFreq = 4.9f;
        public float noiseSpeed = 0.075f;
        public float pulseSpeed = 0.24f;
        public float pulseAmp = 0.055f;

        [Header("Global")]
        public float exposure = 0.90f;

        private MaterialPropertyBlock mpb;
        private EnergySphereController ownerController;
        private bool ownerLookupDone;

        private void Awake()
        {
            Ensure();
        }

        private void Reset()
        {
            targetRenderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (!ownerLookupDone)
            {
                ownerController = GetComponentInParent<EnergySphereController>();
                ownerLookupDone = true;
            }

            if (ownerController != null)
                return;

            ApplyState(Time.time, 2.0f, 1.0f);
        }

        public void ApplyState(float time, float ringRadius, float masterIntensity)
        {
            Ensure();
            if (targetRenderer == null)
                return;

            float energy = Mathf.Clamp(masterIntensity, 0.0f, 1.15f);

            targetRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_EffectTime", time);
            mpb.SetFloat("_InnerRadius", ringRadius * radiusScale);
            mpb.SetFloat("_CoreDark", coreDark);
            mpb.SetFloat("_ShellWidth", shellWidth);
            mpb.SetFloat("_EdgeFeather", edgeFeather);
            mpb.SetFloat("_EdgeBias", edgeBias);
            mpb.SetFloat("_CoreAbsorb", coreAbsorb);
            mpb.SetFloat("_AbsorbRadius", absorbRadius);
            mpb.SetFloat("_MembraneFill", membraneFill);
            mpb.SetFloat("_MembraneIntensity", membraneIntensity);
            mpb.SetFloat("_FresnelPower", fresnelPower);
            mpb.SetFloat("_BandIntensity", bandIntensity);
            mpb.SetFloat("_BandSharpness", bandSharpness);
            mpb.SetFloat("_WaveAmp", waveAmp);
            mpb.SetFloat("_Ribbon1R", ribbon1R);
            mpb.SetFloat("_Ribbon2R", ribbon2R);
            mpb.SetFloat("_Ribbon3R", ribbon3R);
            mpb.SetFloat("_FineThreadIntensity", fineThreadIntensity);
            mpb.SetFloat("_DotIntensity", dotIntensity);
            mpb.SetFloat("_DotDensity", dotDensity);
            mpb.SetFloat("_DotThreshold", dotThreshold);
            mpb.SetFloat("_DotCenterFill", dotCenterFill);
            mpb.SetFloat("_ParticleDrift", particleDrift);
            mpb.SetFloat("_NoiseAmp", noiseAmp);
            mpb.SetFloat("_NoiseFreq", noiseFreq);
            mpb.SetFloat("_NoiseSpeed", noiseSpeed);
            mpb.SetFloat("_PulseSpeed", pulseSpeed);
            mpb.SetFloat("_PulseAmp", pulseAmp);
            mpb.SetFloat("_Exposure", exposure * energy);
            targetRenderer.SetPropertyBlock(mpb);
        }

        private void Ensure()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            mpb ??= new MaterialPropertyBlock();
        }
    }
}
