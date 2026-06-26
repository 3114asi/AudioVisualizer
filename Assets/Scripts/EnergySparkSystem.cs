using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class EnergySparkSystem : MonoBehaviour
    {
        [Header("Particle System")]
        public int maxParticles = 900;
        public float emissionRate = 280f;
        public float sphereRadius = 2.0f;

        [Header("Particle Properties")]
        public float sizeMin = 0.014f;
        public float sizeMax = 0.075f;
        public float lifetimeMin = 0.28f;
        public float lifetimeMax = 1.65f;
        public float speedMin = 0.08f;
        public float speedMax = 1.25f;

        [Header("Distribution")]
        [Range(0f, 1f)] public float ringSpawnChance = 0.64f;
        [Range(0f, 1f)] public float innerSpawnChance = 0.28f;
        public float outsideJitter = 0.14f;
        public float radialBias = 0.65f;
        public float tangentialBias = 0.42f;
        public float turbulence = 0.18f;

        [Header("Emission")]
        public float hdrIntensityMin = 1.8f;
        public float hdrIntensityMax = 7.2f;

        private ParticleSystem ps;
        private float accumulator;

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            EnsureInitialized();
            EmitContinuous(Time.deltaTime);
            UpdateParticleVelocities();
        }

        public void EnsureInitialized()
        {
            if (ps == null)
                ps = GetComponent<ParticleSystem>();

            if (ps == null)
                CreateParticleSystem();
        }

        public void PreviewAtTime(float time)
        {
            EnsureInitialized();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Play(true);

            Random.State state = Random.state;
            Random.InitState(4107 + Mathf.FloorToInt(time * 100f));

            int count = Mathf.Min(maxParticles, 620);
            for (int i = 0; i < count; i++)
            {
                float age = Random.value;
                EmitOne(time + i * 0.017f, age);
            }

            Random.state = state;
            ps.Simulate(0.02f, true, false, true);
        }

        private void CreateParticleSystem()
        {
            ps = gameObject.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles = maxParticles;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.loop = true;
            main.playOnAwake = true;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.enabled = false;

            var sizeOverLT = ps.sizeOverLifetime;
            sizeOverLT.enabled = true;
            sizeOverLT.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.45f, 1f, 0f));

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
                    new GradientAlphaKey(1f, 0.10f),
                    new GradientAlphaKey(0.7f, 0.62f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLT.color = grad;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateSparkMaterial();
            renderer.sortingFudge = 0.15f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void EmitContinuous(float deltaTime)
        {
            accumulator += emissionRate * deltaTime;
            int emitCount = Mathf.FloorToInt(accumulator);
            accumulator -= emitCount;

            for (int i = 0; i < emitCount; i++)
                EmitOne(Time.time + i * 0.011f, 0f);
        }

        private void EmitOne(float seedTime, float normalizedAge)
        {
            float angle = PickSpawnAngle(seedTime);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 tangent = new Vector3(-dir.y, dir.x, 0f);

            float distribution = Random.value;
            float radius;
            if (distribution < ringSpawnChance)
                radius = sphereRadius + Random.Range(-outsideJitter * 0.45f, outsideJitter);
            else if (distribution < ringSpawnChance + innerSpawnChance)
                radius = sphereRadius * Mathf.Sqrt(Random.Range(0.10f, 0.84f));
            else
                radius = sphereRadius * Random.Range(1.02f, 1.32f);

            Vector3 pos = transform.position + dir * radius;
            float speed = Random.Range(speedMin, speedMax);
            Vector3 vel = dir * speed * radialBias
                        + tangent * speed * tangentialBias * Random.Range(-0.8f, 1.0f)
                        + new Vector3(Random.Range(-turbulence, turbulence), Random.Range(-turbulence, turbulence), 0f);

            float lifetime = Random.Range(lifetimeMin, lifetimeMax);
            float size = Random.Range(sizeMin, sizeMax) * (distribution < ringSpawnChance ? 1.0f : 0.65f);
            Color color = ColorForAngle(angle) * Random.Range(hdrIntensityMin, hdrIntensityMax);
            color.a = Random.Range(0.45f, 1.0f);

            ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = vel,
                startLifetime = lifetime,
                startSize = size,
                startColor = color
            };

            ps.Emit(ep, 1);

            if (normalizedAge > 0f)
            {
                int count = ps.particleCount;
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[count];
                int actual = ps.GetParticles(particles);
                if (actual > 0)
                {
                    int last = actual - 1;
                    particles[last].remainingLifetime = Mathf.Max(0.01f, lifetime * (1f - normalizedAge));
                    particles[last].position += vel * lifetime * normalizedAge;
                    ps.SetParticles(particles, actual);
                }
            }
        }

        private void UpdateParticleVelocities()
        {
            int count = ps.particleCount;
            if (count == 0)
                return;

            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[count];
            int actual = ps.GetParticles(particles);
            Vector3 center = transform.position;

            for (int i = 0; i < actual; i++)
            {
                Vector3 rel = particles[i].position - center;
                Vector3 radial = rel.sqrMagnitude > 0.0001f ? rel.normalized : Vector3.right;
                Vector3 tangential = new Vector3(-radial.y, radial.x, 0f);
                float life01 = 1f - particles[i].remainingLifetime / Mathf.Max(particles[i].startLifetime, 0.001f);
                float swirl = Mathf.Sin(Time.time * 2.4f + i * 0.41f);
                float speed = Mathf.Lerp(speedMax, speedMin, life01);
                particles[i].velocity += (tangential * swirl * 0.08f + radial * 0.03f) * speed * Time.deltaTime;
            }

            ps.SetParticles(particles, actual);
        }

        private float PickSpawnAngle(float seedTime)
        {
            if (Random.value < 0.68f)
            {
                float[] anchors = { 0f, 24f, 82f, 176f, 218f, 270f, 318f };
                return anchors[Random.Range(0, anchors.Length)] + Random.Range(-18f, 18f);
            }

            return Random.Range(0f, 360f) + Mathf.Sin(seedTime * 0.7f) * 8f;
        }

        private Color ColorForAngle(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float warm = Mathf.Clamp01(Mathf.Max(
                Mathf.Pow((Mathf.Cos(rad - 0.2f) + 1f) * 0.5f, 1.6f),
                Mathf.Pow((Mathf.Sin(rad) + 1f) * 0.5f, 2.2f) * 0.46f));
            Color cyan = new Color(0.0f, 0.82f, 1.70f, 1f);
            Color blue = new Color(0.04f, 0.12f, 1.35f, 1f);
            Color magenta = new Color(1.55f, 0.08f, 0.82f, 1f);
            Color cool = Color.Lerp(blue, cyan, Mathf.Clamp01((-Mathf.Sin(rad) + 1f) * 0.48f));
            return Color.Lerp(cool, magenta, warm);
        }

        private Material CreateSparkMaterial()
        {
            Shader shader = Shader.Find("AudioVisualizer/EnergyParticle");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material mat = new Material(shader);
            if (mat.HasProperty("_EmissionGain"))
                mat.SetFloat("_EmissionGain", 2.05f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3090;
            return mat;
        }
    }
}
