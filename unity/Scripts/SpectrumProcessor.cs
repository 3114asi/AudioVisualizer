using UnityEngine;

/// <summary>
/// Читает FFT из AudioCaptureController, сворачивает спектр в bandCount полос
/// через логарифмический rebinning, применяет attack/release сглаживание.
/// Результат — float[] Bands — читается ParticleRingController и BurstEmitter.
/// </summary>
[RequireComponent(typeof(AudioCaptureController))]
public class SpectrumProcessor : MonoBehaviour
{
    public VisualizerSettings settings;

    /// Сглаженные частотные полосы, length == settings.bandCount.
    public float[] Bands { get; private set; }

    private AudioCaptureController _capture;
    private float[] _rawSpectrum;
    private float[] _smoothed;

    private void Awake()  => _capture = GetComponent<AudioCaptureController>();

    private void Start()
    {
        Bands        = new float[settings.bandCount];
        _smoothed    = new float[settings.bandCount];
        _rawSpectrum = new float[settings.fftSize];
    }

    private void Update()
    {
        if (!_capture.IsRunning) return;
        _capture.GetSpectrumData(_rawSpectrum, FFTWindow.BlackmanHarris);
        ReduceToBands();
        Smooth();
    }

    // Логарифмический rebinning: низкие частоты получают столько же полос, сколько высокие.
    // Линейное деление сильно недопредставляет бас (первые ~5 бинов из 512).
    private void ReduceToBands()
    {
        int half = _rawSpectrum.Length / 2; // только первая половина FFT значима
        for (int b = 0; b < settings.bandCount; b++)
        {
            float lo = Mathf.Pow(half, (float)b       / settings.bandCount);
            float hi = Mathf.Pow(half, (float)(b + 1) / settings.bandCount);
            int binStart = Mathf.FloorToInt(lo);
            int binEnd   = Mathf.Min(Mathf.CeilToInt(hi), half - 1);

            float sum = 0f; int count = 0;
            for (int i = binStart; i <= binEnd; i++) { sum += _rawSpectrum[i]; count++; }

            Bands[b] = (count > 0 ? sum / count : 0f) * settings.sensitivity;
        }
    }

    private void Smooth()
    {
        for (int i = 0; i < Bands.Length; i++)
        {
            float rate   = Bands[i] > _smoothed[i] ? settings.attack : settings.release;
            _smoothed[i] = Mathf.Lerp(_smoothed[i], Bands[i], rate);
            Bands[i]     = _smoothed[i];
        }
    }
}
