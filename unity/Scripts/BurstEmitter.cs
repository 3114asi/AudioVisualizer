using UnityEngine;

/// <summary>
/// Следит за нижними частотными полосами и на превышении burstThreshold
/// выпускает всплеск частиц-стриков наружу от кольца.
///
/// Particle System настроен на StretchedBillboard + velocityScale,
/// что растягивает частицы по вектору скорости — эффект светового стрика.
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
        var shape    = _ps.shape;
        shape.radius = settings.baseRadius;      // испускаем с края кольца
        shape.radiusThickness = 0.05f;           // только с поверхности, не из объёма
        _ps.Play();
    }

    private void Update()
    {
        if (spectrum?.Bands == null) return;
        if (Time.time - _lastBurst < settings.burstCooldown) return;

        // Суммируем 4 нижних полосы (бас)
        float bass = 0f;
        for (int i = 0; i < Mathf.Min(4, spectrum.Bands.Length); i++)
            bass += spectrum.Bands[i];

        if (bass > settings.burstThreshold)
        {
            // Число частиц масштабируется с интенсивностью удара
            int count = Mathf.RoundToInt(Mathf.Lerp(8f, 64f, bass / (settings.burstThreshold * 2f)));
            _ps.Emit(count);
            _lastBurst = Time.time;
        }
    }

    private void ConfigureSystem()
    {
        var main = _ps.main;
        main.maxParticles    = 256;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.25f, 0.70f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(settings.burstForce * 0.4f, settings.burstForce);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor      = new ParticleSystem.MinMaxGradient(settings.colorLeft, settings.colorRight);
        main.gravityModifier = 0f;

        _ps.emission.enabled = false; // только ручной Emit()

        var shape = _ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;

        // StretchedBillboard растягивает частицы по вектору скорости → световые стрики
        var renderer         = _ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode  = ParticleSystemRenderMode.StretchedBillboard;
        renderer.velocityScale = 0.15f;
        renderer.lengthScale   = 1f;
    }
}
