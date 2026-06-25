using UnityEngine;

/// <summary>
/// Управляет кольцом из частиц:
///   — PARTICLE_COUNT штук, равномерно по окружности, угол фиксирован;
///   — каждый кадр обновляет только РАДИУС: baseRadius + clamp(amp, 0, maxOffset);
///   — позиция плавится через particlePositionLerp (temporal lerp);
///   — цвет задаётся по (cos(angle)+1)/2 → CYAN слева, MAGENTA справа.
///
/// Fix #1: radialOffset зажат в [0, maxRadialOffsetFraction * baseRadius]
///         + пространственное сглаживание идёт через SpectrumProcessor.
/// Fix #3: ColorAt вычислен через угол, а не через индекс частицы.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ParticleRingController : MonoBehaviour
{
    public VisualizerSettings settings;
    public SpectrumProcessor  spectrum;

    private ParticleSystem            _ps;
    private ParticleSystem.Particle[] _particles;
    private Vector3[]                 _targetPos;  // желаемые позиции (без lerp)
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
        main.startLifetime   = float.MaxValue;
        main.startSpeed      = 0f;
        main.startSize       = settings.particleSizeMin;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        _ps.emission.enabled = false;
        _ps.shape.enabled    = false;
        _ps.Play();
    }

    private void SpawnRing()
    {
        _count      = settings.particleCount;
        _particles  = new ParticleSystem.Particle[_count];
        _targetPos  = new Vector3[_count];

        for (int i = 0; i < _count; i++)
        {
            float angle = ParticleAngle(i);
            Vector3 pos = PosOnCircle(angle, settings.baseRadius);
            _targetPos[i] = pos;
            _particles[i] = new ParticleSystem.Particle
            {
                position          = pos,
                startColor        = ColorAt(angle),
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
        float[] bands    = spectrum.Bands;
        float   maxOff   = settings.maxRadialOffsetFraction * settings.baseRadius;
        float   lerpRate = settings.particlePositionLerp;

        for (int i = 0; i < _count; i++)
        {
            float angle  = ParticleAngle(i);
            int   bIdx   = Mathf.Clamp(
                Mathf.RoundToInt((float)i / _count * (bands.Length - 1)), 0, bands.Length - 1);
            float amp    = Mathf.Clamp01(bands[bIdx]);

            // FIX #1: radialOffset зажат в [0, maxOff] — частица не рвёт кольцо
            float radOff  = Mathf.Clamp(amp, 0f, 1f) * maxOff;
            float r       = settings.baseRadius + radOff;

            // Temporal lerp: плавное движение к цели
            _targetPos[i] = PosOnCircle(angle, r);
            _particles[i].position = Vector3.Lerp(
                _particles[i].position, _targetPos[i], lerpRate);

            // Цвет не зависит от амплитуды — только от угла (FIX #3)
            _particles[i].startColor = ColorAt(angle);
            _particles[i].startSize  = Mathf.Lerp(settings.particleSizeMin, settings.particleSizeMax, amp);
        }
        _ps.SetParticles(_particles, _count);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // angle: старт сверху (−π/2), обходим по часовой стрелке
    private float ParticleAngle(int i)
        => (float)i / _count * Mathf.PI * 2f - Mathf.PI * 0.5f;

    private static Vector3 PosOnCircle(float angle, float r)
        => new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);

    /// FIX #3: (cos(angle)+1)/2 → angle=π (9 часов, ЛЕВАЯ дуга) → 0 → colorLeft=CYAN
    ///                           → angle=0 (3 часа, ПРАВАЯ дуга) → 1 → colorRight=MAGENTA
    private Color ColorAt(float angle)
    {
        float blend = (Mathf.Cos(angle) + 1f) * 0.5f;
        return Color.Lerp(settings.colorLeft, settings.colorRight, blend);
    }
}
