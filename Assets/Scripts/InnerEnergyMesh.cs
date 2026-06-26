using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class InnerEnergyMesh : MonoBehaviour
    {
        [Header("Targets")]
        public Renderer targetRenderer;

        [Header("Shape")]
        public float radiusScale = 0.965f;
        public float exposure = 0.72f;
        public float gridIntensity = 0.075f;
        public float dotIntensity = 0.78f;

        private MaterialPropertyBlock mpb;

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
            ApplyState(Time.time, 2.0f, 1.0f);
        }

        public void ApplyState(float time, float ringRadius, float masterIntensity)
        {
            Ensure();
            if (targetRenderer == null)
                return;

            targetRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_EffectTime", time);
            mpb.SetFloat("_InnerRadius", ringRadius * radiusScale);
            mpb.SetFloat("_Exposure", exposure * masterIntensity);
            mpb.SetFloat("_GridIntensity", gridIntensity);
            mpb.SetFloat("_DotIntensity", dotIntensity);
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
