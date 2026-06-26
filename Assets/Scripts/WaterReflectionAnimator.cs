using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class WaterReflectionAnimator : MonoBehaviour
    {
        public Renderer reflectionRenderer;
        public Renderer[] waterGlowRenderers;
        public float rippleSpeed = 0.18f;
        public float intensity = 1.2f;
        public Vector2 lateralDrift = new Vector2(0.025f, 0.009f);

        private Vector3 startPosition;

        private void Awake()
        {
            startPosition = transform.localPosition;
        }

        private void Update()
        {
            float ripple = Mathf.Repeat(Time.time * rippleSpeed, 1f);
            transform.localPosition = startPosition + new Vector3(
                Mathf.Sin(Time.time * 0.73f) * lateralDrift.x,
                Mathf.Sin(Time.time * 0.41f) * lateralDrift.y,
                0f);

            SetWaterMaterial(reflectionRenderer, ripple, intensity);
            if (waterGlowRenderers == null) return;
            foreach (Renderer target in waterGlowRenderers)
            {
                SetWaterMaterial(target, ripple, intensity * 0.55f);
            }
        }

        private static void SetWaterMaterial(Renderer target, float ripple, float materialIntensity)
        {
            if (target == null || target.material == null) return;
            target.material.SetFloat("_Ripple", ripple);
            target.material.SetFloat("_Intensity", materialIntensity);
        }
    }
}
