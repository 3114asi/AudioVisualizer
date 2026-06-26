using System.Collections.Generic;
using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class RadialBurstSpawner : MonoBehaviour
    {
        [System.Serializable]
        public sealed class RayBurst
        {
            public Renderer renderer;
            public Transform transform;
            public float angle;
            public float cooldown;
            public float age = 999f;
        }

        public List<RayBurst> bursts = new List<RayBurst>();
        public Vector2 intervalRange = new Vector2(0.35f, 1.4f);
        public float flashDuration = 0.28f;
        public float maxScale = 1.25f;

        private float nextBurstTime;

        private void Start()
        {
            ScheduleNextBurst();
            foreach (RayBurst burst in bursts)
            {
                SetAlpha(burst, 0f);
            }
        }

        private void Update()
        {
            if (Time.time >= nextBurstTime && bursts.Count > 0)
            {
                Trigger(bursts[Random.Range(0, bursts.Count)]);
                ScheduleNextBurst();
            }

            foreach (RayBurst burst in bursts)
            {
                if (burst == null || burst.renderer == null) continue;
                burst.age += Time.deltaTime;
                float t = Mathf.Clamp01(burst.age / flashDuration);
                float alpha = Mathf.Sin((1f - t) * Mathf.PI) * (1f - t);
                SetAlpha(burst, alpha);
                if (burst.transform != null)
                {
                    burst.transform.localScale = new Vector3(maxScale, Mathf.Lerp(0.15f, 1f, alpha), 1f);
                }
            }
        }

        public void Trigger(RayBurst burst)
        {
            if (burst == null) return;
            burst.age = 0f;
            if (burst.transform != null)
            {
                burst.transform.localRotation = Quaternion.Euler(0f, 0f, burst.angle);
            }
        }

        private void ScheduleNextBurst()
        {
            nextBurstTime = Time.time + Random.Range(intervalRange.x, intervalRange.y);
        }

        private static void SetAlpha(RayBurst burst, float alpha)
        {
            if (burst.renderer == null || burst.renderer.material == null) return;
            Color color = burst.renderer.material.HasProperty("_Color")
                ? burst.renderer.material.GetColor("_Color")
                : Color.white;
            color.a = alpha;
            burst.renderer.material.SetColor("_Color", color);
            burst.renderer.material.SetFloat("_Intensity", Mathf.Lerp(0f, 5f, alpha));
        }
    }
}
