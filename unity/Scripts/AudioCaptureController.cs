using UnityEngine;

public enum CaptureMode { Microphone, InternalAudio }

/// <summary>
/// Управляет источником аудио: переключает режимы Microphone / Internal Audio,
/// запускает/останавливает захват и предоставляет AudioSource для FFT-анализа.
/// Требует AudioSource на том же GameObject (добавляется автоматически).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioCaptureController : MonoBehaviour
{
    public VisualizerSettings settings;

    public CaptureMode CurrentMode { get; private set; } = CaptureMode.Microphone;
    public bool IsRunning          { get; private set; }

    private AudioSource _src;
    private string      _micDevice;

    private void Awake()
    {
        _src             = GetComponent<AudioSource>();
        _src.loop        = true;
        _src.playOnAwake = false;
    }

    public void SetMode(CaptureMode mode) => CurrentMode = mode;

    public void StartCapture()
    {
        StopCapture();
        IsRunning = true;

        switch (CurrentMode)
        {
            case CaptureMode.Microphone:    StartMic();           break;
            case CaptureMode.InternalAudio: StartInternalAudio(); break;
        }
    }

    public void StopCapture()
    {
        IsRunning = false;
        if (Microphone.IsRecording(_micDevice))
            Microphone.End(_micDevice);
        _src.Stop();
    }

    /// Вызывается SpectrumProcessor каждый кадр для получения FFT-данных.
    public void GetSpectrumData(float[] output, FFTWindow window)
        => _src.GetSpectrumData(output, 0, window);

    // ── Microphone ────────────────────────────────────────────────────────────

    private void StartMic()
    {
        _micDevice  = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        _src.clip   = Microphone.Start(_micDevice, true, 10, 44100);
        // Ждём инициализацию микрофона перед воспроизведением
        while (Microphone.GetPosition(_micDevice) <= 0) { }
        _src.Play();
    }

    // ── Internal Audio (scaffold) ─────────────────────────────────────────────

    private void StartInternalAudio()
    {
        // TODO: Реализовать захват системного звука через нативный Kotlin-плагин.
        //
        // Необходимые шаги для полной реализации:
        //   1. Kotlin/Java foreground service с типом FOREGROUND_SERVICE_TYPE_MEDIA_PROJECTION.
        //   2. Пользовательское согласие через MediaProjectionManager.createScreenCaptureIntent().
        //   3. AudioPlaybackCaptureConfiguration + AudioRecord → читаем PCM float[].
        //   4. JNI-мост: AndroidJavaObject.Call<float[]>() → Unity → AudioClip.SetData().
        //   5. Подключить получившийся AudioClip к _src и вызвать _src.Play().
        //
        // До реализации плагина — используем микрофон как заглушку.
        Debug.LogWarning("[AudioCapture] Internal audio capture не реализован. Используется микрофон.");
        StartMic();
    }
}
