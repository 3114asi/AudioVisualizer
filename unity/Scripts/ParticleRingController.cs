using UnityEngine;

/// <summary>
/// Управляет кольцом из частиц:
/// — равномерно распределяет particleCount частиц по окружности;
/// — назначает каждой частице цвет через градиент colorLeft→colorRight по углу;
/// — каждый кадр обновляет радиус (baseRadius + Bands[i] * maxRadialOffset) и размер.
///
/// Использует GetParticles/SetParticles — все частицы живут вечно,
/// физика Particle System полностью отключена.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ParticleRingController : MonoBehaviour
{
    public VisualizerSettings settings;
    public SpectrumProcessor  spectrum;

    private ParticleSystem            _ps;
    private ParticleSystem.Particle[] _particles;
    private int                       _count;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        ConfigureSystem();
    }

    private void Start()  => SpawnRing();

    private void LateUpdate()
    {
        if (spectrum?.Bands == null) return;
        UpdateRing();
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    private void ConfigureSystem()
    {
        var main = _ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.maxParticles    = settings.particleCount;
        main.startLifetime   = float.MaxValue; // частицы живут вечно
        main.startSpeed      = 0f;             // физика не нужна
        main.startSize       = settings.particleSizeMin;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        _ps.emission.enabled = false; // эмиссию контролируем вручную
        _ps.shape.enabled    = false;
        _ps.Play(); // должен быть в состоянии Play для SetParticles
    }

    private void SpawnRing()
    {
        _count     = settings.particleCount;
        _particles = new ParticleSystem.Particle[_count];

        for (int i = 0; i < _count; i++)
        {
            float t     = (float)i / _count;
            float angle = t * Mathf.PI * 2f;
            _particles[i] = new ParticleSystem.Particle
            {
                position          = PosOnCircle(angle, settings.baseRadius),
                startColor        = ColorAt(t),
                startSize         = settings.particleSizeMin,
                startLifetime     = float.MaxValue,
                remainingLifetime = float.MaxValue,
            };
        }
        _ps.SetParticles(_particles, _count);
    }

    // ── Per-frame ─────────────────────────────────────────────────────────────

    private void UpdateRing()
    {
        float[] bands = spectrum.Bands;

        for (int i = 0; i < _count; i++)
        {
            float t       = (float)i / _count;
            float angle   = t * Mathf.PI * 2f - Mathf.PI / 2f; // старт сверху (12 часов)
            int   bandIdx = Mathf.Clamp(Mathf.RoundToInt(t * (bands.Length - 1)), 0, bands.Length - 1);
            float amp     = Mathf.Clamp01(bands[bandIdx]);

            _particles[i].position  = PosOnCircle(angle, settings.baseRadius + amp * settings.maxRadialOffset);
            _particles[i].startColor = ColorAt(t);
            _particles[i].startSize = Mathf.Lerp(settings.particleSizeMin, settings.particleSizeMax, amp);
        }
        _ps.SetParticles(_particles, _count);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Vector3 PosOnCircle(float angle, float r)
        => new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);

    /// Плавный градиент colorLeft→colorRight→colorLeft за полный оборот (cosine interpolation).
    private Color ColorAt(float t) // t ∈ [0, 1]
    {
        float blend = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
        return Color.Lerp(settings.colorLeft, settings.colorRight, blend);
    }
}
