using UnityEngine;

/// <summary>
/// Читает FFT из AudioCaptureController, сворачивает спектр в bandCount полос
/// через логарифмический rebinning, применяет attack/release + пространственное сглаживание.
/// Результат — float[] Bands — читается ParticleRingController и BurstEmitter.
///
/// FakeAudio: синтетический спектр, не требует микрофона.
///   Огибающая = sin²(t/period), period ~2 с.
///   Энергия сосредоточена в низких полосах (экспоненциальный спад по индексу).
///   На пике (Bands[0]≈1) должен выглядеть как "кадр 10" из референса.
/// </summary>
[RequireComponent(typeof(AudioCaptureController))]
public class SpectrumProcessor : MonoBehaviour
{
    public VisualizerSettings settings;

    [Header("Debug / FakeAudio")]
    [Tooltip("Вместо микрофона — синтетический спектр с синусоидальной огибающей (период ~2 с, энергия в басу).")]
    public bool useFakeAudio = false;
    [Tooltip("Период синусоиды FakeAudio в секундах.")]
    public float fakePeriod = 2f;

    /// Сглаженные частотные полосы, length == settings.bandCount.
    public float[] Bands { get; private set; }

    private AudioCaptureController _capture;
    private float[] _rawSpectrum;
    private float[] _smoothed;
    private float[] _spatial;   // промежуточный буфер для пространственного сглаживания

    private void Awake()  => _capture = GetComponent<AudioCaptureController>();

    private void Start()
    {
        int n     = settings.bandCount;
        Bands     = new float[n];
        _smoothed = new float[n];
        _spatial  = new float[n];
        _rawSpectrum = new float[settings.fftSize];
    }

    private void Update()
    {
        if (useFakeAudio)
            FillFakeBands();
        else
        {
            if (!_capture.IsRunning) return;
            _capture.GetSpectrumData(_rawSpectrum, FFTWindow.BlackmanHarris);
            ReduceToBands();
        }
        SpatialSmooth();
        TemporalSmooth();
    }

    // ── Real FFT ──────────────────────────────────────────────────────────────

    // Логарифмический rebinning: низкие частоты получают столько же полос, сколько высокие.
    private void ReduceToBands()
    {
        int half = _rawSpectrum.Length / 2;
        for (int b = 0; b < settings.bandCount; b++)
        {
            float lo     = Mathf.Pow(half, (float)b       / settings.bandCount);
            float hi     = Mathf.Pow(half, (float)(b + 1) / settings.bandCount);
            int binStart = Mathf.FloorToInt(lo);
            int binEnd   = Mathf.Min(Mathf.CeilToInt(hi), half - 1);

            float sum = 0f; int count = 0;
            for (int i = binStart; i <= binEnd; i++) { sum += _rawSpectrum[i]; count++; }
            Bands[b] = (count > 0 ? sum / count : 0f) * settings.sensitivity;
        }
    }

    // ── Fake Audio ────────────────────────────────────────────────────────────

    private void FillFakeBands()
    {
        // sin² даёт плавный подъём 0→1→0 без отрицательных значений
        float phase    = (Time.time % fakePeriod) / fakePeriod; // 0..1
        float envelope = Mathf.Sin(phase * Mathf.PI);           // sin(0..π) → 0→1→0
        envelope       = envelope * envelope;                    // sin² — острее пик

        int n = settings.bandCount;
        for (int b = 0; b < n; b++)
        {
            // Энергия падает экспоненциально от низких к высоким частотам
            float bandFrac  = (float)b / n;
            float bassWeight = Mathf.Exp(-bandFrac * 5f);       // 1→0.0067 по полосам
            Bands[b] = envelope * bassWeight * settings.sensitivity;
        }
    }

    // ── Spatial smoothing (по индексу полос) ─────────────────────────────────

    // Усредняет каждую полосу с ±spatialHalfWidth соседями.
    // Убирает острые одиночные пики (в т.ч. DC-bin сверху).
    private void SpatialSmooth()
    {
        int hw = settings.spatialSmoothHalfWidth;
        if (hw <= 0) { System.Array.Copy(Bands, _spatial, Bands.Length); return; }

        int n = Bands.Length;
        for (int i = 0; i < n; i++)
        {
            float sum = 0f; int cnt = 0;
            for (int d = -hw; d <= hw; d++)
            {
                int idx = Mathf.Clamp(i + d, 0, n - 1);
                sum += Bands[idx]; cnt++;
            }
            _spatial[i] = sum / cnt;
        }
        System.Array.Copy(_spatial, Bands, n);
    }

    // ── Temporal smoothing (attack/release per band) ──────────────────────────

    private void TemporalSmooth()
    {
        for (int i = 0; i < Bands.Length; i++)
        {
            float rate   = Bands[i] > _smoothed[i] ? settings.attack : settings.release;
            _smoothed[i] = Mathf.Lerp(_smoothed[i], Bands[i], rate);
            Bands[i]     = Mathf.Clamp01(_smoothed[i]);
        }
    }
}
