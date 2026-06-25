using UnityEngine;

/// <summary>
/// Эмитирует вторичные частицы-искры при ударе баса.
/// Каждая искра:
///   — испускается с поверхности кольца под случайным углом;
///   — летит радиально наружу;
///   — получает цвет по своему углу (CYAN слева, MAGENTA справа);
///   — затухает за 0.25-0.70 сек (alpha fade через Over Lifetime).
///
/// FIX #2: рендер Billboard + круговые частицы (не LineRenderer, не StretchedBillboard-спицы).
/// Для красивого свечения назначить материал с аддитивным блендингом (Additive / Screen).
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class BurstEmitter : MonoBehaviour
{
    public VisualizerSettings settings;
    public SpectrumProcessor  spectrum;

    private ParticleSystem _ps;
    private float          _lastBurst;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        ConfigureSystem();
    }

    private void Start()
    {
        _ps.shape.radius = settings.baseRadius;
        _ps.shape.radiusThickness = 0.02f; // только с поверхности кольца
        _ps.Play();
    }

    private void Update()
    {
        if (spectrum?.Bands == null) return;
        if (Time.time - _lastBurst < settings.burstCooldown) return;

        // Суммируем первые 4 полосы (бас)
        float bass = 0f;
        int   bassCount = Mathf.Min(4, spectrum.Bands.Length);
        for (int i = 0; i < bassCount; i++) bass += spectrum.Bands[i];

        if (bass < settings.burstThreshold) return;

        EmitSparks(bass);
        _lastBurst = Time.time;
    }

    // ── Emission ──────────────────────────────────────────────────────────────

    private void EmitSparks(float bass)
    {
        float normalizedBass = Mathf.Clamp01(bass / (settings.burstThreshold * 2f));
        int count = Mathf.RoundToInt(
            Mathf.Lerp(4f, settings.burstMaxSparks, normalizedBass));

        // Emit с overrideParams для разнообразия скоростей и цветов по углу
        var emitParams = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            // Случайный угол на кольце
            float angle = Random.Range(0f, Mathf.PI * 2f);

            // Позиция — на поверхности кольца
            float px = Mathf.Cos(angle) * settings.baseRadius;
            float py = Mathf.Sin(angle) * settings.baseRadius;
            emitParams.position = new Vector3(px, py, 0f);

            // Скорость — радиально наружу + небольшой разброс
            float speed = Random.Range(
                settings.burstForce * 0.4f, settings.burstForce * (1f + normalizedBass));
            float spreadAngle = angle + Random.Range(-0.25f, 0.25f);
            emitParams.velocity = new Vector3(
                Mathf.Cos(spreadAngle) * speed,
                Mathf.Sin(spreadAngle) * speed, 0f);

            // FIX #3: цвет по углу — CYAN слева (angle≈π), MAGENTA справа (angle≈0)
            float blend = (Mathf.Cos(angle) + 1f) * 0.5f;
            Color sparkColor = Color.Lerp(settings.colorLeft, settings.colorRight, blend);
            emitParams.startColor = sparkColor;

            // Размер: крупнее на пике
            emitParams.startSize = Random.Range(0.04f, 0.14f + normalizedBass * 0.10f);

            // Время жизни: короче чем у стриков — искры быстро гаснут
            emitParams.startLifetime = Random.Range(0.20f, 0.60f);

            _ps.Emit(emitParams, 1);
        }
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    private void ConfigureSystem()
    {
        var main = _ps.main;
        main.maxParticles    = 512;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.20f, 0.60f);
        main.startSpeed      = 0f;           // скорость задаём через EmitParams.velocity
        main.startSize       = 0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        _ps.emission.enabled = false; // только ручной Emit

        var shape = _ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;

        // FIX #2: Billboard — круговые светящиеся точки, не растянутые спицы
        var rend = _ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;

        // Alpha Over Lifetime: 1 → 0 (плавное затухание)
        var col = _ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Size Over Lifetime: 1 → 0.3 (немного уменьшаются к концу)
        var sizeOL = _ps.sizeOverLifetime;
        sizeOL.enabled = true;
        AnimationCurve sizeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f);
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
    }
}
