using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class EnergyAtmosphereParticles : MonoBehaviour
    {
        [Header("Links")]
        public EnergySphereController controller;

        [Header("Settings")]
        public int maxParticles = 220;
        public float emissionRate = 22f;
        public float sphereRadius = 2.0f;
        public float spreadRadius = 3.05f;

        [Header("Particle")]
        public float sizeMin = 0.040f;
        public float sizeMax = 0.170f;
        public float lifetimeMin = 2.8f;
        public float lifetimeMax = 7.5f;

        [Header("Colors")]
        public Color colorA = new Color(0.02f, 0.18f, 1.05f, 0.35f);
        public Color colorB = new Color(0.82f, 0.05f, 0.78f, 0.28f);

        private ParticleSystem ps;

        private void OnEnable()
        {
            EnsureInitialized();
            EnsureController();
        }

        private void Update()
        {
            EnsureInitialized();
            EnsureController();
            float envelope = controller != null ? controller.CurrentEnvelope : 1.0f;
            var emission = ps.emission;
            emission.rateOverTime = emissionRate * Mathf.Lerp(0.18f, 1.0f, Smooth01(envelope));
        }

        public void EnsureInitialized()
        {
            if (ps == null)
                ps = GetComponent<ParticleSystem>();

            if (ps == null)
                CreatePS();
        }

        public void PreviewAtTime(float time)
        {
            EnsureInitialized();
            EnsureController();
            ps.Clear(true);
            ps.Simulate(Mathf.Max(0.1f, time), true, true, true);

            float envelope = controller != null ? controller.EvaluateEnvelope(time) : 1.0f;
            ScaleExistingParticles(envelope);
        }

        private void CreatePS()
        {
            ps = gameObject.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles = maxParticles;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.10f);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = emissionRate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = spreadRadius;
            shape.radiusThickness = 0.48f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.radial = new ParticleSystem.MinMaxCurve(-0.02f, 0.07f);
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-0.045f, 0.045f);

            var sizeOverLT = ps.sizeOverLifetime;
            sizeOverLT.enabled = true;
            sizeOverLT.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.4f, 1f, 0f));

            var colorOverLT = ps.colorOverLifetime;
            colorOverLT.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.32f, 0.2f),
                    new GradientAlphaKey(0.24f, 0.76f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLT.color = grad;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.20f;
            noise.frequency = 0.42f;
            noise.scrollSpeed = 0.08f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateMat();
            renderer.sortingFudge = -0.2f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private Material CreateMat()
        {
            Shader s = Shader.Find("AudioVisualizer/EnergyParticle");
            if (s == null)
                s = Shader.Find("Particles/Standard Unlit");
            if (s == null)
                s = Shader.Find("Sprites/Default");

            Material m = new Material(s);
            if (m.HasProperty("_EmissionGain"))
                m.SetFloat("_EmissionGain", 0.36f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_ZWrite", 0);
            m.renderQueue = 3030;
            return m;
        }

        private void ScaleExistingParticles(float envelope)
        {
            int count = ps.particleCount;
            if (count == 0)
                return;

            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[count];
            int actual = ps.GetParticles(particles);
            float alphaScale = Smooth01(envelope);
            for (int i = 0; i < actual; i++)
            {
                Color32 c = particles[i].startColor;
                c.a = (byte)Mathf.Clamp(Mathf.RoundToInt(c.a * alphaScale), 0, 255);
                particles[i].startColor = c;
            }

            ps.SetParticles(particles, actual);
        }

        private void EnsureController()
        {
            if (controller == null)
                controller = GetComponentInParent<EnergySphereController>();
        }

        private static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3.0f - 2.0f * x);
        }
    }
}
