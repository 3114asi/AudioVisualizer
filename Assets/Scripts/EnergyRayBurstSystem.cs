using System.Collections.Generic;
using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class EnergyRayBurstSystem : MonoBehaviour
    {
        [Header("Pool")]
        public int rayCount = 34;
        public float sphereRadius = 2.0f;
        public float rayLengthMin = 0.45f;
        public float rayLengthMax = 2.35f;
        public float rayWidthMin = 0.004f;
        public float rayWidthMax = 0.020f;

        [Header("Burst Timing")]
        public float burstIntervalMin = 0.08f;
        public float burstIntervalMax = 0.24f;
        public int burstCountMin = 1;
        public int burstCountMax = 4;
        public float flashDuration = 0.30f;
        public float maxIntensity = 7.2f;
        public float longRayChance = 0.08f;

        [Header("Preferred Angles (degrees)")]
        public float[] preferredAngles = { 0f, 14f, -18f, 178f, 205f, 232f, 84f, 268f, 318f };

        private sealed class RayInstance
        {
            public GameObject go;
            public Renderer renderer;
            public float age;
            public float lifetime;
            public bool active;
            public float baseIntensity;
            public float startLength;
            public MaterialPropertyBlock mpb;
        }

        private readonly List<RayInstance> rays = new List<RayInstance>();
        private float nextBurstTime;
        private Mesh quadMesh;

        private void OnEnable()
        {
            EnsureInitialized();
            ScheduleNext(0.05f);
        }

        private void Update()
        {
            EnsureInitialized();

            if (Time.time >= nextBurstTime)
            {
                int count = Random.Range(burstCountMin, burstCountMax + 1);
                float angle = PickWeightedAngle();
                for (int i = 0; i < count; i++)
                    FireRayAt(angle + Random.Range(-15f, 15f), 1.0f, 1.0f);

                ScheduleNext();
            }

            UpdateActive(Time.deltaTime);
        }

        public void EnsureInitialized()
        {
            if (quadMesh == null)
                CreateQuadMesh();

            if (rays.Count > 0)
                return;

            RebindSerializedRays();
            if (rays.Count > 0)
                return;

            CreateRays();
        }

        public void FireBurstAroundAngle(float angleDeg, int count, float intensityScale)
        {
            EnsureInitialized();
            for (int i = 0; i < count; i++)
            {
                FireRayAt(angleDeg + Random.Range(-10f, 10f), intensityScale, Random.Range(0.75f, 1.25f));
            }
        }

        public void PreviewAtTime(float time)
        {
            EnsureInitialized();

            foreach (RayInstance r in rays)
            {
                r.active = false;
                SetRayVisible(r, false);
            }

            float[] previewAngles = { 0f, 11f, 176f, 204f, 231f, 82f, 270f, 318f };
            int visible = Mathf.Min(previewAngles.Length, rays.Count);
            for (int i = 0; i < visible; i++)
            {
                float phase = Mathf.Repeat(time * 0.37f + i * 0.143f, 1.0f);
                if (phase > 0.72f && i > 2)
                    continue;

                float age01 = Mathf.Repeat(phase * 1.35f, 1.0f);
                ConfigureRay(rays[i], previewAngles[i] + Mathf.Sin(time * 1.7f + i) * 8f,
                    Mathf.Lerp(0.70f, 1.15f, Mathf.Sin(time + i) * 0.5f + 0.5f),
                    Mathf.Lerp(0.75f, 1.3f, age01));
                rays[i].active = true;
                rays[i].age = age01 * rays[i].lifetime;
                SetRayVisible(rays[i], true);
                UpdateRayVisual(rays[i], age01);
            }
        }

        private void CreateQuadMesh()
        {
            quadMesh = new Mesh();
            quadMesh.name = "EnergyRayQuad";
            quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3( 0.5f, 0f, 0f),
                new Vector3( 0.5f, 1f, 0f),
                new Vector3(-0.5f, 1f, 0f),
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

        private void RebindSerializedRays()
        {
            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Ray_"))
                    continue;

                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer == null)
                    continue;

                rays.Add(new RayInstance
                {
                    go = child.gameObject,
                    renderer = renderer,
                    age = 999f,
                    lifetime = flashDuration,
                    active = false,
                    baseIntensity = maxIntensity,
                    startLength = 1f,
                    mpb = new MaterialPropertyBlock()
                });
            }
        }

        private void CreateRays()
        {
            Shader rayShader = Shader.Find("AudioVisualizer/EnergyRay");
            if (rayShader == null)
            {
                Debug.LogWarning("[EnergyRayBurst] EnergyRay shader not found");
                return;
            }

            for (int i = 0; i < rayCount; i++)
            {
                GameObject go = new GameObject($"Ray_{i:00}");
                go.transform.SetParent(transform, false);

                MeshFilter mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = quadMesh;

                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                Material mat = new Material(rayShader);
                mat.renderQueue = 3070;
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                RayInstance inst = new RayInstance
                {
                    go = go,
                    renderer = mr,
                    age = 999f,
                    lifetime = flashDuration,
                    active = false,
                    baseIntensity = maxIntensity,
                    startLength = 1f,
                    mpb = new MaterialPropertyBlock()
                };

                SetRayVisible(inst, false);
                rays.Add(inst);
            }
        }

        private void UpdateActive(float deltaTime)
        {
            foreach (RayInstance r in rays)
            {
                if (!r.active)
                    continue;

                r.age += deltaTime;
                float age01 = Mathf.Clamp01(r.age / r.lifetime);
                UpdateRayVisual(r, age01);

                if (age01 >= 1f)
                {
                    r.active = false;
                    SetRayVisible(r, false);
                }
            }
        }

        private void UpdateRayVisual(RayInstance r, float age01)
        {
            float envelope = Mathf.Sin((1f - age01) * Mathf.PI) * Mathf.Pow(1f - age01, 0.8f);
            envelope = Mathf.Clamp01(envelope);

            r.renderer.GetPropertyBlock(r.mpb);
            r.mpb.SetFloat("_Intensity", r.baseIntensity * envelope);
            r.renderer.SetPropertyBlock(r.mpb);

            Vector3 ls = r.go.transform.localScale;
            ls.y = Mathf.Lerp(r.startLength * 0.28f, r.startLength, envelope);
            r.go.transform.localScale = ls;
        }

        private void FireRayAt(float angleDeg, float intensityScale, float lifetimeScale)
        {
            RayInstance available = rays.Find(r => !r.active);
            if (available == null)
                return;

            ConfigureRay(available, angleDeg, intensityScale, lifetimeScale);
            available.age = 0f;
            available.active = true;
            SetRayVisible(available, true);
        }

        private void ConfigureRay(RayInstance ray, float angleDeg, float intensityScale, float lifetimeScale)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 startPos = dir * sphereRadius;

            bool longRay = Random.value < longRayChance || intensityScale > 1.25f;
            float length = longRay
                ? Random.Range(rayLengthMax * 0.65f, rayLengthMax * 1.22f)
                : Random.Range(rayLengthMin, rayLengthMax * 0.65f);
            float width = Random.Range(rayWidthMin, rayWidthMax) * (longRay ? 0.72f : 1.0f);
            Color color = ColorForAngle(angleDeg, intensityScale);

            ray.go.transform.localPosition = startPos;
            ray.go.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);
            ray.go.transform.localScale = new Vector3(width, length, 1f);

            ray.renderer.GetPropertyBlock(ray.mpb);
            ray.mpb.SetColor("_Color", color);
            ray.mpb.SetFloat("_Intensity", maxIntensity * intensityScale);
            ray.mpb.SetFloat("_Length", length);
            ray.mpb.SetFloat("_Width", width);
            ray.mpb.SetFloat("_Softness", Random.Range(2.4f, 5.5f));
            ray.renderer.SetPropertyBlock(ray.mpb);

            ray.lifetime = flashDuration * Random.Range(0.55f, 1.45f) * lifetimeScale;
            ray.baseIntensity = maxIntensity * intensityScale * (longRay ? 0.85f : 1.0f);
            ray.startLength = length;
        }

        private float PickWeightedAngle()
        {
            if (preferredAngles.Length == 0 || Random.value < 0.20f)
                return Random.Range(0f, 360f);

            int index = Random.Range(0, preferredAngles.Length);
            return preferredAngles[index] + Random.Range(-13f, 13f);
        }

        private Color ColorForAngle(float angleDeg, float intensityScale)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float warm = Mathf.Clamp01(Mathf.Max(
                Mathf.Pow((Mathf.Cos(rad - 0.2f) + 1f) * 0.5f, 1.6f),
                Mathf.Pow((Mathf.Sin(rad) + 1f) * 0.5f, 2.1f) * 0.45f));
            Color cool = new Color(0.0f, 0.72f, 1.65f, 1f);
            Color blue = new Color(0.05f, 0.18f, 1.35f, 1f);
            Color warmColor = new Color(1.45f, 0.10f, 0.78f, 1f);
            Color c = Color.Lerp(Color.Lerp(blue, cool, 0.65f), warmColor, warm);
            c *= Mathf.Lerp(0.9f, 1.25f, intensityScale - 1.0f);
            c.a = 1f;
            return c;
        }

        private void SetRayVisible(RayInstance r, bool visible)
        {
            if (r.renderer != null)
                r.renderer.enabled = visible;
        }

        private void ScheduleNext(float minDelayOverride = -1f)
        {
            float minDelay = minDelayOverride >= 0f ? minDelayOverride : burstIntervalMin;
            nextBurstTime = Time.time + Random.Range(minDelay, burstIntervalMax);
        }
    }
}
