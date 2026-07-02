using System.Collections.Generic;
using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class EnergyRayBurstSystem : MonoBehaviour
    {
        [Header("Links")]
        public EnergySphereController controller;

        [Header("Pool")]
        public int rayCount = 34;
        public float sphereRadius = 2.0f;
        public float rayLengthMin = 0.45f;
        public float rayLengthMax = 2.35f;
        public float rayWidthMin = 0.004f;
        public float rayWidthMax = 0.020f;

        [Header("Burst Timing")]
        public float burstIntervalMin = 0.28f;
        public float burstIntervalMax = 0.95f;
        public int burstCountMin = 1;
        public int burstCountMax = 3;
        public float flashDuration = 1.10f;
        public float maxIntensity = 5.6f;
        public float longRayChance = 0.06f;

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
            EnsureController();
            ScheduleNext(0.18f);
        }

        private void Update()
        {
            EnsureInitialized();
            EnsureController();

            float envelope = controller != null ? controller.CurrentEnvelope : 1.0f;
            if (Time.time >= nextBurstTime && envelope > 0.08f)
            {
                int count = Random.Range(burstCountMin, burstCountMax + 1);
                float angle = PickWeightedAngle();
                for (int i = 0; i < count; i++)
                    FireRayAt(angle + Random.Range(-15f, 15f), Mathf.Lerp(0.35f, 1.0f, envelope), 1.0f);

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
            EnsureController();

            float envelope = controller != null ? controller.CurrentEnvelope : 1.0f;
            if (envelope <= 0.08f)
                return;

            for (int i = 0; i < count; i++)
            {
                FireRayAt(angleDeg + Random.Range(-10f, 10f), intensityScale * Mathf.Lerp(0.35f, 1.0f, envelope), Random.Range(0.85f, 1.25f));
            }
        }

        public void PreviewAtTime(float time)
        {
            EnsureInitialized();
            EnsureController();

            foreach (RayInstance r in rays)
            {
                r.active = false;
                SetRayVisible(r, false);
            }

            float envelope = controller != null ? controller.EvaluateEnvelope(time) : 1.0f;
            float intensity = 1.0f;
            float[] previewAngles = { 0f, 13f, 178f, 206f, 234f, 82f, 270f, 318f };
            int visible = Mathf.Min(previewAngles.Length, rays.Count);
            for (int i = 0; i < visible; i++)
            {
                float cycle = Mathf.Lerp(2.4f, 4.6f, Hash01(i * 17.23f + 1.7f));
                float phase = Mathf.Repeat(time / cycle + i * 0.137f, 1.0f);
                float rayEnvelope = SmoothPulse(phase, 0.16f, 0.42f, 1.0f) * envelope;
                if (rayEnvelope <= 0.01f)
                    continue;

                float age01 = phase;
                float drift = Mathf.Sin(time * 0.54f + i * 1.37f) * 5.5f;
                ConfigurePreviewRay(rays[i], previewAngles[i] + drift,
                    Mathf.Lerp(0.55f, 1.15f, Hash01(i * 9.71f)) * intensity * Mathf.Lerp(0.25f, 1.0f, envelope),
                    Mathf.Lerp(0.90f, 1.30f, Hash01(i * 3.91f)),
                    Hash01(i * 2.13f + 0.4f),
                    Hash01(i * 5.17f + 4.0f));
                rays[i].active = true;
                rays[i].age = age01 * rays[i].lifetime;
                SetRayVisible(rays[i], true);
                UpdateRayVisual(rays[i], age01, rayEnvelope);
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
            float envelope = SmoothPulse(age01, 0.18f, 0.46f, 1.0f);
            UpdateRayVisual(r, age01, envelope);
        }

        private void UpdateRayVisual(RayInstance r, float age01, float envelope)
        {
            envelope = Mathf.Clamp01(envelope);
            r.renderer.GetPropertyBlock(r.mpb);
            r.mpb.SetFloat("_Intensity", r.baseIntensity * envelope);
            r.renderer.SetPropertyBlock(r.mpb);

            Vector3 ls = r.go.transform.localScale;
            ls.y = Mathf.Lerp(r.startLength * 0.22f, r.startLength, Smooth01(envelope));
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
            ray.mpb.SetFloat("_Intensity", maxIntensity * intensityScale * GetRayIntensityScale());
            ray.mpb.SetFloat("_Length", length);
            ray.mpb.SetFloat("_Width", width);
            ray.mpb.SetFloat("_Softness", Random.Range(2.4f, 5.5f));
            ray.renderer.SetPropertyBlock(ray.mpb);

            ray.lifetime = flashDuration * Random.Range(0.78f, 1.45f) * lifetimeScale;
            ray.baseIntensity = maxIntensity * intensityScale * GetRayIntensityScale() * (longRay ? 0.85f : 1.0f);
            ray.startLength = length;
        }

        private void ConfigurePreviewRay(RayInstance ray, float angleDeg, float intensityScale, float lifetimeScale, float lengthSeed, float widthSeed)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 startPos = dir * sphereRadius;

            bool longRay = lengthSeed > 1.0f - longRayChance || intensityScale > 1.05f;
            float length = longRay
                ? Mathf.Lerp(rayLengthMax * 0.68f, rayLengthMax * 1.18f, lengthSeed)
                : Mathf.Lerp(rayLengthMin, rayLengthMax * 0.62f, lengthSeed);
            float width = Mathf.Lerp(rayWidthMin, rayWidthMax, widthSeed) * (longRay ? 0.72f : 1.0f);
            Color color = ColorForAngle(angleDeg, intensityScale);

            ray.go.transform.localPosition = startPos;
            ray.go.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);
            ray.go.transform.localScale = new Vector3(width, length, 1f);

            ray.renderer.GetPropertyBlock(ray.mpb);
            ray.mpb.SetColor("_Color", color);
            ray.mpb.SetFloat("_Intensity", maxIntensity * intensityScale * GetRayIntensityScale());
            ray.mpb.SetFloat("_Length", length);
            ray.mpb.SetFloat("_Width", width);
            ray.mpb.SetFloat("_Softness", Mathf.Lerp(3.0f, 6.0f, Hash01(widthSeed * 13.0f + 1.0f)));
            ray.renderer.SetPropertyBlock(ray.mpb);

            ray.lifetime = flashDuration * lifetimeScale;
            ray.baseIntensity = maxIntensity * intensityScale * GetRayIntensityScale() * (longRay ? 0.85f : 1.0f);
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

        private void EnsureController()
        {
            if (controller == null)
                controller = GetComponentInParent<EnergySphereController>();
        }

        private float GetRayIntensityScale()
        {
            return controller != null ? controller.rayIntensity : 1.0f;
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

        private static float Hash01(float value)
        {
            float h = Mathf.Sin(value * 12.9898f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }
    }
}
