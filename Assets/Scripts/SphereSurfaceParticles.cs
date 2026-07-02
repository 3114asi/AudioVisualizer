using UnityEngine;
using UnityEngine.Rendering;

namespace Ediskrad.AudioVisualizer
{
    public sealed class SphereSurfaceParticles : MonoBehaviour
    {
        [Header("Links")]
        public EnergySphereController controller;

        [Header("Surface")]
        public float sphereRadius = 2.84f;
        [Range(24, 360)] public int latitudeBands = 170;
        [Range(48, 512)] public int longitudeBands = 320;
        [Range(0f, 0.18f)] public float radialNoise = 0.040f;
        [Range(0f, 0.16f)] public float waveDisplacement = 0.055f;
        [Range(0f, 0.08f)] public float membraneDrift = 0.018f;

        [Header("Dots")]
        public float dotSizeMin = 0.0048f;
        public float dotSizeMax = 0.0098f;
        public float baseAlpha = 0.032f;
        public float ribbonAlpha = 0.92f;
        public float rimAlpha = 0.28f;
        public float emissionGain = 3.65f;
        public float hdrIntensity = 4.55f;

        [Header("Motion")]
        public float flowSpeed = 0.070f;
        public float waveSpeed = 0.115f;
        public float twinkleSpeed = 0.42f;

        private const int MaxSafeParticles = 65000;
        private ParticleSystem ps;
        private ParticleSystem.Particle[] particles;
        private float[] uValues;
        private float[] vValues;
        private float[] seeds;
        private int activeCount;

        private void OnEnable()
        {
            EnsureInitialized();
            EnsureController();
            UpdateSurface(Time.time);
        }

        private void Update()
        {
            EnsureInitialized();
            EnsureController();
            UpdateSurface(Time.time);
        }

        public void PreviewAtTime(float time)
        {
            EnsureInitialized();
            EnsureController();
            UpdateSurface(time);
        }

        public void EnsureInitialized()
        {
            EnsureParticleSystem();
            EnsureBuffers();
        }

        private void EnsureParticleSystem()
        {
            if (ps == null)
                ps = GetComponent<ParticleSystem>();

            if (ps != null)
            {
                ConfigureParticleSystem();
                return;
            }

            ps = gameObject.AddComponent<ParticleSystem>();
            ConfigureParticleSystem();
        }

        private void ConfigureParticleSystem()
        {
            int desired = Mathf.Min(MaxSafeParticles, Mathf.Max(1, latitudeBands * longitudeBands));

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = desired;
            main.startLifetime = 120f;
            main.startSpeed = 0f;
            main.startSize = dotSizeMax;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = false;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = false;

            var noise = ps.noise;
            noise.enabled = false;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = false;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = false;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingFudge = 0.04f;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 0.18f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enableGPUInstancing = true;

            Material mat = renderer.sharedMaterial;
            if (mat == null || mat.shader == null || mat.shader.name != "AudioVisualizer/EnergyParticle")
                mat = CreateSurfaceMaterial();
            if (mat.HasProperty("_EmissionGain"))
                mat.SetFloat("_EmissionGain", emissionGain);
            if (mat.HasProperty("_Softness"))
                mat.SetFloat("_Softness", 5.6f);
            mat.renderQueue = 3075;
            renderer.sharedMaterial = mat;

            if (!ps.isPlaying)
                ps.Play(true);
        }

        private Material CreateSurfaceMaterial()
        {
            Shader shader = Shader.Find("AudioVisualizer/EnergyParticle");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material mat = new Material(shader)
            {
                name = "M_SphereSurfaceParticles_Runtime",
                renderQueue = 3075
            };
            if (mat.HasProperty("_EmissionGain"))
                mat.SetFloat("_EmissionGain", emissionGain);
            if (mat.HasProperty("_Softness"))
                mat.SetFloat("_Softness", 5.6f);
            mat.SetInt("_SrcBlend", (int)BlendMode.One);
            mat.SetInt("_DstBlend", (int)BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            return mat;
        }

        private void EnsureBuffers()
        {
            int desired = Mathf.Min(MaxSafeParticles, Mathf.Max(1, latitudeBands * longitudeBands));
            if (particles != null && particles.Length == desired)
                return;

            activeCount = desired;
            particles = new ParticleSystem.Particle[activeCount];
            uValues = new float[activeCount];
            vValues = new float[activeCount];
            seeds = new float[activeCount];

            int index = 0;
            for (int y = 0; y < latitudeBands && index < activeCount; y++)
            {
                float v = (y + 0.5f) / latitudeBands;
                for (int x = 0; x < longitudeBands && index < activeCount; x++)
                {
                    float stagger = (y & 1) == 0 ? 0.0f : 0.5f;
                    float u = (x + stagger) / longitudeBands;
                    uValues[index] = u - Mathf.Floor(u);
                    vValues[index] = v;
                    seeds[index] = Hash01((x + 1) * 12.9898f + (y + 1) * 78.233f);
                    index++;
                }
            }
        }

        private void UpdateSurface(float time)
        {
            if (particles == null || activeCount == 0 || ps == null)
                return;

            float energy = controller != null ? Mathf.Clamp01(controller.CurrentEnvelope * controller.CurrentEmission) : 1.0f;
            float energyGain = Mathf.Lerp(0.58f, 1.0f, energy);
            float radius = controller != null && controller.ringRadius > 0.01f
                ? controller.ringRadius * (sphereRadius / 3.05f)
                : sphereRadius;
            float tFlow = time * flowSpeed;
            float tWave = time * waveSpeed;

            for (int i = 0; i < activeCount; i++)
            {
                float u = uValues[i];
                float v = vValues[i];
                float seed = seeds[i];

                float theta = (u + tFlow * (0.12f + seed * 0.08f)) * Mathf.PI * 2.0f;
                float phi = v * Mathf.PI;

                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                float sheet1 = Sheet(theta * 0.92f + cosPhi * 4.75f + tWave * 2.30f, 0.88f);
                float sheet2 = Sheet(theta * 1.16f - cosPhi * 5.90f - tWave * 2.05f + 1.30f, 0.90f);
                float sheet3 = Sheet(theta * 0.58f + sinPhi * 7.80f + tWave * 1.70f + 2.20f, 0.91f);
                float sheet = Mathf.Clamp01(sheet1 * 0.95f + sheet2 * 0.78f + sheet3 * 0.62f);

                float ribbon1 = Ribbon(theta * 1.34f + cosPhi * 5.2f + tWave * 3.8f, 0.086f);
                float ribbon2 = Ribbon(theta * 2.05f - cosPhi * 6.8f - tWave * 2.9f + 1.7f, 0.058f);
                float ribbon3 = Ribbon(theta * 0.78f + sinPhi * 6.2f + tWave * 2.2f + 2.6f, 0.064f);
                float ribbon = Mathf.Clamp01(ribbon1 * 0.80f + ribbon2 * 0.58f + ribbon3 * 0.48f);
                float latNet = Ribbon(phi * 54.0f + Mathf.Sin(theta * 2.0f + tWave * 1.8f) * 0.28f, 0.036f);
                float lonNet = Ribbon(theta * 42.0f + cosPhi * 2.6f - tWave * 1.2f, 0.030f);
                float net = Mathf.Clamp01(latNet * 0.58f + lonNet * 0.36f) * (0.34f + centerPresenceHint(sinPhi) * 0.66f);
                float structure = Mathf.Clamp01(ribbon * 0.64f + sheet * 0.98f + net * 0.34f);

                float slowWave = Mathf.Sin(theta * 3.0f + cosPhi * 7.4f + tWave * 3.7f);
                float fineWave = Mathf.Sin(theta * 9.0f - cosPhi * 11.0f + tWave * 5.3f + seed * 5.0f);
                float radialOffset = radialNoise * (slowWave * 0.62f + fineWave * 0.18f)
                                   + radialNoise * structure * 0.82f;

                float thetaDrift = membraneDrift * Mathf.Sin(tFlow * 5.0f + seed * 19.0f + cosPhi * 2.0f);
                float phiDrift = membraneDrift * 0.65f * Mathf.Cos(tFlow * 4.1f + seed * 23.0f + sinTheta);
                float dTheta = theta + thetaDrift + waveDisplacement * 0.18f * slowWave;
                float dPhi = Mathf.Clamp(phi + phiDrift + waveDisplacement * 0.08f * fineWave, 0.015f, Mathf.PI - 0.015f);

                float sPhi = Mathf.Sin(dPhi);
                Vector3 dir = new Vector3(Mathf.Cos(dTheta) * sPhi, Mathf.Sin(dTheta) * sPhi, Mathf.Cos(dPhi));
                float projected = Mathf.Clamp01(new Vector2(dir.x, dir.y).magnitude);
                float rim = Mathf.Pow(projected, 3.6f);
                float centerPresence = SmoothStep01(0.10f, 0.92f, projected);
                float twinkle = 0.72f + 0.28f * Mathf.Sin(time * twinkleSpeed + seed * 37.0f);

                float alpha = (baseAlpha * (0.36f + centerPresence * 0.64f)
                            + rimAlpha * rim
                            + ribbonAlpha * structure
                            + 0.18f * net)
                            * twinkle * energyGain;
                alpha = Mathf.Clamp(alpha, 0.0f, 1.0f);

                float sizeRibbon = Mathf.Lerp(0.74f, 1.55f, structure);
                float size = Mathf.Lerp(dotSizeMin, dotSizeMax, seed) * sizeRibbon * Mathf.Lerp(0.88f, 1.08f, rim);
                Color color = BlueColor(theta, projected, structure, rim) * (hdrIntensity * Mathf.Lerp(0.72f, 1.62f, structure + rim * 0.35f));
                color.a = alpha;

                particles[i].position = dir * (radius + radialOffset);
                particles[i].velocity = Vector3.zero;
                particles[i].remainingLifetime = 120f;
                particles[i].startLifetime = 120f;
                particles[i].startSize = size;
                particles[i].startColor = color;
            }

            ps.SetParticles(particles, activeCount);
        }

        private static Color BlueColor(float theta, float projected, float ribbon, float rim)
        {
            float cyan = Mathf.Clamp01(0.25f + 0.75f * Mathf.Pow(Mathf.Clamp01(Mathf.Sin(theta + 0.35f) * 0.5f + 0.5f), 1.8f));
            float hot = Mathf.Clamp01(ribbon * 0.75f + rim * 0.42f);
            Color deep = new Color(0.0f, 0.020f, 1.12f, 1f);
            Color electric = new Color(0.0f, 0.18f, 2.35f, 1f);
            Color bright = new Color(0.035f, 0.48f, 3.55f, 1f);
            return Color.Lerp(Color.Lerp(deep, electric, cyan * (0.55f + projected * 0.45f)), bright, hot);
        }

        private static float Sheet(float phase, float threshold)
        {
            float x = 0.5f + 0.5f * Mathf.Sin(phase);
            return SmoothStep01(threshold, 1.0f, x);
        }

        private static float Ribbon(float phase, float width)
        {
            float x = Mathf.Clamp01(1.0f - Mathf.Abs(Mathf.Sin(phase)) / Mathf.Max(width, 0.0001f));
            return x * x * (3.0f - 2.0f * x);
        }

        private static float centerPresenceHint(float sinPhi)
        {
            return Mathf.Clamp01(sinPhi * sinPhi);
        }

        private static float SmoothStep01(float edge0, float edge1, float value)
        {
            float x = Mathf.Clamp01((value - edge0) / Mathf.Max(edge1 - edge0, 0.0001f));
            return x * x * (3.0f - 2.0f * x);
        }

        private static float Hash01(float value)
        {
            float h = Mathf.Sin(value) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        private void EnsureController()
        {
            if (controller == null)
                controller = GetComponentInParent<EnergySphereController>();
        }
    }
}
