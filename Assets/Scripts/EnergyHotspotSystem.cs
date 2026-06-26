using System.Collections.Generic;
using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class EnergyHotspotSystem : MonoBehaviour
    {
        [Header("Links")]
        public EnergyRayBurstSystem linkedRays;

        [Header("Hotspots")]
        public int hotspotCount = 12;
        public float sphereRadius = 2.0f;
        public float hotspotSizeMin = 0.12f;
        public float hotspotSizeMax = 0.34f;
        public float hotspotIntensity = 9.2f;

        [Header("Timing")]
        public float flashDuration = 0.42f;
        public float intervalMin = 0.10f;
        public float intervalMax = 0.48f;
        public int flashesPerEventMin = 1;
        public int flashesPerEventMax = 3;

        [Header("Preferred Regions (degrees)")]
        public float[] preferredAngles = { 0f, 18f, 45f, 86f, 185f, 222f, 270f, 318f };

        private sealed class Hotspot
        {
            public GameObject go;
            public Renderer renderer;
            public float age;
            public float lifetime;
            public float baseSize;
            public float baseIntensity;
            public bool active;
            public MaterialPropertyBlock mpb;
        }

        private readonly List<Hotspot> hotspots = new List<Hotspot>();
        private float nextFlashTime;
        private Mesh quadMesh;

        private void OnEnable()
        {
            EnsureInitialized();
            ScheduleNext(0.08f);
        }

        private void Update()
        {
            EnsureInitialized();

            if (Time.time >= nextFlashTime)
            {
                int count = Random.Range(flashesPerEventMin, flashesPerEventMax + 1);
                float baseAngle = PickAngle();
                for (int i = 0; i < count; i++)
                    FlashAtAngle(baseAngle + Random.Range(-14f, 14f), 1.0f + i * 0.15f);

                ScheduleNext();
            }

            foreach (Hotspot h in hotspots)
            {
                if (!h.active)
                    continue;

                h.age += Time.deltaTime;
                float age01 = Mathf.Clamp01(h.age / h.lifetime);
                UpdateHotspotVisual(h, age01);

                if (age01 >= 1f)
                {
                    h.active = false;
                    SetVisible(h, false);
                }
            }
        }

        public void EnsureInitialized()
        {
            if (quadMesh == null)
                CreateQuadMesh();

            if (hotspots.Count > 0)
                return;

            RebindSerializedHotspots();
            if (hotspots.Count > 0)
                return;

            CreateHotspots();
        }

        public void PreviewAtTime(float time)
        {
            EnsureInitialized();

            foreach (Hotspot h in hotspots)
            {
                h.active = false;
                SetVisible(h, false);
            }

            float[] previewAngles = { 0f, 34f, 82f, 188f, 226f, 270f, 314f };
            int visible = Mathf.Min(previewAngles.Length, hotspots.Count);
            for (int i = 0; i < visible; i++)
            {
                float phase = Mathf.Repeat(time * 0.45f + i * 0.17f, 1.0f);
                if (phase > 0.70f && i > 2)
                    continue;

                Hotspot h = hotspots[i];
                ConfigureHotspot(h, previewAngles[i] + Mathf.Sin(time * 1.2f + i) * 6f,
                    Mathf.Lerp(0.75f, 1.35f, Mathf.Sin(time + i * 0.6f) * 0.5f + 0.5f));
                h.active = true;
                h.age = phase * h.lifetime;
                SetVisible(h, true);
                UpdateHotspotVisual(h, phase);
            }
        }

        private void CreateQuadMesh()
        {
            quadMesh = new Mesh();
            quadMesh.name = "HotspotQuad";
            quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
            };
            quadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            quadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quadMesh.RecalculateBounds();
        }

        private void RebindSerializedHotspots()
        {
            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Hotspot_"))
                    continue;

                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer == null)
                    continue;

                hotspots.Add(new Hotspot
                {
                    go = child.gameObject,
                    renderer = renderer,
                    age = 999f,
                    lifetime = flashDuration,
                    baseSize = hotspotSizeMin,
                    baseIntensity = hotspotIntensity,
                    active = false,
                    mpb = new MaterialPropertyBlock()
                });
            }
        }

        private void CreateHotspots()
        {
            Shader shader = Shader.Find("AudioVisualizer/HotspotGlow");
            if (shader == null)
            {
                Debug.LogWarning("[EnergyHotspot] HotspotGlow shader not found");
                return;
            }

            for (int i = 0; i < hotspotCount; i++)
            {
                GameObject go = new GameObject($"Hotspot_{i:00}");
                go.transform.SetParent(transform, false);

                MeshFilter mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = quadMesh;

                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                Material mat = new Material(shader);
                mat.renderQueue = 3080;
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                Hotspot h = new Hotspot
                {
                    go = go,
                    renderer = mr,
                    age = 999f,
                    lifetime = flashDuration,
                    baseSize = hotspotSizeMin,
                    baseIntensity = hotspotIntensity,
                    active = false,
                    mpb = new MaterialPropertyBlock()
                };
                SetVisible(h, false);
                hotspots.Add(h);
            }
        }

        private void FlashAtAngle(float angleDeg, float intensityScale)
        {
            Hotspot available = hotspots.Find(h => !h.active);
            if (available == null)
                return;

            ConfigureHotspot(available, angleDeg, intensityScale);
            available.age = 0f;
            available.active = true;
            SetVisible(available, true);

            if (linkedRays != null)
                linkedRays.FireBurstAroundAngle(angleDeg, Random.Range(1, 4), intensityScale + 0.20f);
        }

        private void ConfigureHotspot(Hotspot h, float angleDeg, float intensityScale)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * sphereRadius;
            h.go.transform.localPosition = pos;
            h.go.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

            h.baseSize = Random.Range(hotspotSizeMin, hotspotSizeMax) * Mathf.Lerp(0.85f, 1.25f, intensityScale - 1f);
            h.baseIntensity = hotspotIntensity * intensityScale * Random.Range(0.75f, 1.35f);
            h.lifetime = flashDuration * Random.Range(0.55f, 1.35f);

            Color c = ColorForAngle(angleDeg);
            h.renderer.GetPropertyBlock(h.mpb);
            h.mpb.SetColor("_Color", c);
            h.mpb.SetFloat("_Intensity", h.baseIntensity);
            h.mpb.SetFloat("_Falloff", Random.Range(2.2f, 4.5f));
            h.renderer.SetPropertyBlock(h.mpb);
        }

        private void UpdateHotspotVisual(Hotspot h, float age01)
        {
            float envelope = Mathf.Sin(age01 * Mathf.PI);
            envelope *= Mathf.Lerp(1.0f, 0.45f, age01);

            h.renderer.GetPropertyBlock(h.mpb);
            h.mpb.SetFloat("_Intensity", h.baseIntensity * envelope);
            h.renderer.SetPropertyBlock(h.mpb);

            float scale = h.baseSize * (0.70f + envelope * 0.75f);
            h.go.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private float PickAngle()
        {
            if (preferredAngles.Length == 0 || Random.value < 0.18f)
                return Random.Range(0f, 360f);

            return preferredAngles[Random.Range(0, preferredAngles.Length)] + Random.Range(-12f, 12f);
        }

        private Color ColorForAngle(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float warm = Mathf.Clamp01(Mathf.Max(
                Mathf.Pow((Mathf.Cos(rad - 0.2f) + 1f) * 0.5f, 1.6f),
                Mathf.Pow((Mathf.Sin(rad) + 1f) * 0.5f, 2.2f) * 0.50f));
            Color cool = new Color(0.0f, 0.75f, 1.65f, 1f);
            Color violet = new Color(0.55f, 0.05f, 1.35f, 1f);
            Color warmColor = new Color(1.65f, 0.10f, 0.82f, 1f);
            return Color.Lerp(Color.Lerp(cool, violet, 0.35f), warmColor, warm);
        }

        private void SetVisible(Hotspot h, bool visible)
        {
            if (h.renderer != null)
                h.renderer.enabled = visible;
        }

        private void ScheduleNext(float minDelayOverride = -1f)
        {
            float minDelay = minDelayOverride >= 0f ? minDelayOverride : intervalMin;
            nextFlashTime = Time.time + Random.Range(minDelay, intervalMax);
        }
    }
}
