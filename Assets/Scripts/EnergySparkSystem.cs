using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class EnergySparkSystem : MonoBehaviour
    {
        [Header("Links")]
        public EnergySphereController controller;

        [Header("Particle System")]
        public int maxParticles = 900;
        public float emissionRate = 135f;
        public float sphereRadius = 2.0f;

        [Header("Particle Properties")]
        public float sizeMin = 0.014f;
        public float sizeMax = 0.075f;
        public float lifetimeMin = 1.65f;
        public float lifetimeMax = 4.20f;
        public float speedMin = 0.035f;
        public float speedMax = 0.38f;

        [Header("Distribution")]
        [Range(0f, 1f)] public float ringSpawnChance = 0.64f;
        [Range(0f, 1f)] public float innerSpawnChance = 0.28f;
        public float outsideJitter = 0.14f;
        public float radialBias = 0.65f;
        public float tangentialBias = 0.42f;
        public float turbulence = 0.06f;

        [Header("Emission")]
        public float hdrIntensityMin = 2.4f;
        public float hdrIntensityMax = 9.8f;

        private ParticleSystem ps;
        private float accumulator;

        private void OnEnable()
        {
            EnsureInitialized();
            EnsureController();
        }

        private void Update()
        {
            EnsureInitialized();
            EnsureController();
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
            EnsureController();

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Play(true);

            float envelope = controller != null ? controller.EvaluateEnvelope(time) : 1.0f;
            int count = Mathf.Min(maxParticles, Mathf.RoundToInt(760f * Mathf.Lerp(0.32f, 1.0f, envelope)));
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[count];
            for (int i = 0; i < count; i++)
            {
                particles[i] = BuildPreviewParticle(i, time, envelope);
            }

            ps.SetParticles(particles, count);
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
                    new GradientAlphaKey(0.82f, 0.12f),
                    new GradientAlphaKey(0.58f, 0.70f),
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
            float envelope = controller != null ? controller.CurrentEnvelope : 1.0f;
            if (envelope <= 0.02f)
                return;

            accumulator += emissionRate * Mathf.Lerp(0.18f, 1.0f, Smooth01(envelope)) * deltaTime;
            int emitCount = Mathf.FloorToInt(accumulator);
            accumulator -= emitCount;

            for (int i = 0; i < emitCount; i++)
                EmitOne(Time.time + i * 0.011f, 0f);
        }

        private void EmitOne(float seedTime, float normalizedAge)
        {
            float envelope = controller != null ? controller.CurrentEnvelope : 1.0f;
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
            float speed = Random.Range(GetSpeedMin(), GetSpeedMax());
            Vector3 vel = dir * speed * radialBias
                        + tangent * speed * tangentialBias * Random.Range(-0.8f, 1.0f)
                        + new Vector3(Random.Range(-turbulence, turbulence), Random.Range(-turbulence, turbulence), 0f);

            float lifetime = Random.Range(GetLifetimeMin(), GetLifetimeMax());
            float size = Random.Range(sizeMin, sizeMax) * (distribution < ringSpawnChance ? 1.0f : 0.65f);
            Color color = ColorForAngle(angle) * Random.Range(hdrIntensityMin, hdrIntensityMax) * Mathf.Lerp(0.45f, 1.0f, envelope);
            color.a = Random.Range(0.45f, 1.0f) * Smooth01(envelope);

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
                float swirl = Mathf.Sin(Time.time * 0.72f + i * 0.41f);
                float speed = Mathf.Lerp(GetSpeedMax(), GetSpeedMin(), life01);
                particles[i].velocity += (tangential * swirl * 0.04f + radial * 0.014f) * speed * Time.deltaTime;
            }

            ps.SetParticles(particles, actual);
        }

        private ParticleSystem.Particle BuildPreviewParticle(int index, float time, float envelope)
        {
            float seed = index + 1.0f;
            float cycle = Mathf.Lerp(GetLifetimeMin(), GetLifetimeMax(), Hash01(seed * 3.71f));
            float phase = Repeat01(time / cycle + Hash01(seed * 5.13f));
            float lifeEnvelope = SmoothPulse(phase, 0.10f, 0.64f, 1.0f);

            float anchorChance = Hash01(seed * 7.91f);
            float angle = PickPreviewAngle(seed, anchorChance);
            angle += time * Mathf.Lerp(-10.0f, 13.0f, Hash01(seed * 4.73f));
            angle += Mathf.Sin(time * 0.42f + seed * 1.37f) * 4.5f;

            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 tangent = new Vector3(-dir.y, dir.x, 0f);

            float distribution = Hash01(seed * 9.37f);
            float radius;
            if (distribution < ringSpawnChance)
                radius = sphereRadius + Mathf.Lerp(-outsideJitter * 0.42f, outsideJitter * 1.08f, Hash01(seed * 11.0f));
            else if (distribution < ringSpawnChance + innerSpawnChance)
                radius = sphereRadius * Mathf.Sqrt(Mathf.Lerp(0.10f, 0.84f, Hash01(seed * 13.0f)));
            else
                radius = sphereRadius * Mathf.Lerp(1.02f, 1.30f, Hash01(seed * 17.0f));

            float speed = Mathf.Lerp(GetSpeedMin(), GetSpeedMax(), Hash01(seed * 19.0f));
            Vector3 drift = dir * speed * radialBias * phase * cycle
                          + tangent * speed * tangentialBias * Mathf.Lerp(-0.65f, 0.85f, Hash01(seed * 23.0f)) * phase * cycle;
            Vector3 wobble = new Vector3(
                Mathf.Sin(time * 0.58f + seed) * turbulence,
                Mathf.Cos(time * 0.47f + seed * 1.6f) * turbulence,
                0f);

            float size = Mathf.Lerp(sizeMin, sizeMax, Hash01(seed * 29.0f)) * (distribution < ringSpawnChance ? 1.0f : 0.62f);
            float alpha = Mathf.Clamp01(lifeEnvelope * Smooth01(envelope));
            Color color = ColorForAngle(angle) * Mathf.Lerp(hdrIntensityMin, hdrIntensityMax, Hash01(seed * 31.0f)) * Mathf.Lerp(0.38f, 1.0f, envelope);
            color.a = alpha;

            ParticleSystem.Particle particle = new ParticleSystem.Particle
            {
                position = transform.position + dir * radius + drift + wobble,
                velocity = dir * speed * radialBias + tangent * speed * tangentialBias * 0.3f,
                startLifetime = cycle,
                remainingLifetime = Mathf.Max(0.01f, cycle * (1.0f - phase)),
                startSize = size * Mathf.Lerp(0.82f, 1.12f, alpha),
                startColor = color
            };

            return particle;
        }

        private float PickPreviewAngle(float seed, float anchorChance)
        {
            if (anchorChance < 0.68f)
            {
                float[] anchors = { 0f, 24f, 82f, 176f, 218f, 270f, 318f };
                int idx = Mathf.Clamp(Mathf.FloorToInt(Hash01(seed * 37.0f) * anchors.Length), 0, anchors.Length - 1);
                return anchors[idx] + Mathf.Lerp(-18f, 18f, Hash01(seed * 41.0f));
            }

            return Hash01(seed * 43.0f) * 360f;
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
                mat.SetFloat("_EmissionGain", 2.40f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3090;
            return mat;
        }

        private void EnsureController()
        {
            if (controller == null)
                controller = GetComponentInParent<EnergySphereController>();
        }

        private float GetLifetimeMin()
        {
            return controller != null ? Mathf.Max(0.20f, controller.particleLifetime * 0.52f) : lifetimeMin;
        }

        private float GetLifetimeMax()
        {
            return controller != null ? Mathf.Max(GetLifetimeMin() + 0.10f, controller.particleLifetime * 1.32f) : lifetimeMax;
        }

        private float GetSpeedMin()
        {
            return controller != null ? Mathf.Max(0.005f, controller.particleSpeed * 0.10f) : speedMin;
        }

        private float GetSpeedMax()
        {
            return controller != null ? Mathf.Max(GetSpeedMin() + 0.01f, controller.particleSpeed) : speedMax;
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

        private static float Repeat01(float value)
        {
            return value - Mathf.Floor(value);
        }
    }
}
