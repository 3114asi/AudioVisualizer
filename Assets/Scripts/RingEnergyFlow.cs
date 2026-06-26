using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class RingEnergyFlow : MonoBehaviour
    {
        public Renderer[] flowRenderers;
        public Transform[] orbitingEnergyPoints;
        [Min(0.1f)] public float loopDuration = 10f;
        public float shaderFlowSpeed = 1f;
        public Vector3 orbitCenter = new Vector3(0f, 1.15f, -0.12f);
        public float orbitRadius = 3.26f;
        public float orbitSpeedDegrees = 72f;

        private void Update()
        {
            float offset = Mathf.Repeat(Time.time / loopDuration * shaderFlowSpeed, 1f);

            if (flowRenderers != null)
            {
                foreach (Renderer target in flowRenderers)
                {
                    if (target == null) continue;
                    foreach (Material material in target.materials)
                    {
                        if (material != null)
                        {
                            material.SetFloat("_FlowOffset", offset);
                        }
                    }
                }
            }

            if (orbitingEnergyPoints == null) return;

            for (int i = 0; i < orbitingEnergyPoints.Length; i++)
            {
                Transform point = orbitingEnergyPoints[i];
                if (point == null) continue;

                float angle = (Time.time * orbitSpeedDegrees + i * (360f / orbitingEnergyPoints.Length)) * Mathf.Deg2Rad;
                point.localPosition = orbitCenter + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * orbitRadius;
                point.localScale = Vector3.one * (0.08f + 0.035f * Mathf.Sin(Time.time * 5f + i));
            }
        }
    }
}
