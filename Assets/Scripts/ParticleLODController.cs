using UnityEngine;

namespace Ediskrad.AudioVisualizer
{
    public sealed class ParticleLODController : MonoBehaviour
    {
        [System.Serializable]
        public struct ParticleTier
        {
            public int maxParticles;
            public float emissionMultiplier;
        }

        public ParticleSystem[] particleSystems;
        public ParticleTier low = new ParticleTier { maxParticles = 3000, emissionMultiplier = 0.28f };
        public ParticleTier medium = new ParticleTier { maxParticles = 6500, emissionMultiplier = 0.55f };
        public ParticleTier high = new ParticleTier { maxParticles = 10000, emissionMultiplier = 0.82f };
        public ParticleTier ultra = new ParticleTier { maxParticles = 15000, emissionMultiplier = 1f };
        public bool autoApplyOnStart = true;

        private void Start()
        {
            if (autoApplyOnStart)
            {
                ApplyQuality(QualitySettings.GetQualityLevel());
            }
        }

        public void ApplyQuality(int qualityLevel)
        {
            ParticleTier tier = qualityLevel <= 0 ? low : qualityLevel == 1 ? medium : qualityLevel == 2 ? high : ultra;

            if (particleSystems == null) return;
            foreach (ParticleSystem system in particleSystems)
            {
                if (system == null) continue;
                ParticleSystem.MainModule main = system.main;
                main.maxParticles = Mathf.Max(32, tier.maxParticles / Mathf.Max(1, particleSystems.Length));

                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTimeMultiplier *= tier.emissionMultiplier;
            }
        }
    }
}
