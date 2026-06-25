using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Подключает все UI-элементы к AudioCaptureController и VisualizerSettings.
/// Прикрепить к корневому GameObject Canvas или отдельному UIController.
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("Controllers")]
    public VisualizerSettings     settings;
    public AudioCaptureController capture;

    [Header("Status Text")]
    public TextMeshProUGUI titleText;   // "AUDIO VISUALIZER"
    public TextMeshProUGUI statusText;  // "Ready" / "Running" / "Stopped"
    public TextMeshProUGUI logoText;    // текст внутри кольца (World Space Canvas)
    public TextMeshProUGUI fpsText;     // счётчик кадров

    [Header("Buttons")]
    public Button startButton;
    public Button stopButton;
    public Button fpsToggleButton;      // переключает 30 / 60 FPS

    [Header("Mode Toggles")]
    public Toggle micToggle;
    public Toggle internalToggle;

    [Header("Sliders")]
    public Slider sensitivitySlider;    // диапазон 0.5–3.0, default 1.35
    public Slider bassSlider;           // регулирует burstThreshold

    private bool  _highFps = true;
    private float _fpsTimer;
    private int   _frameCount, _shownFps;

    private void Start()
    {
        titleText?.SetText("AUDIO VISUALIZER");
        statusText?.SetText("Ready");
        logoText?.SetText("EDISK");

        // Sensitivity
        sensitivitySlider.minValue = 0.5f;
        sensitivitySlider.maxValue = 3.0f;
        sensitivitySlider.value    = settings.sensitivity;
        sensitivitySlider.onValueChanged.AddListener(v =>
        {
            settings.sensitivity = v;
            statusText?.SetText($"Sensitivity {v:F2}");
        });

        // Bass adjust (burstThreshold)
        bassSlider.minValue = 0.1f;
        bassSlider.maxValue = 4.0f;
        bassSlider.value    = settings.burstThreshold;
        bassSlider.onValueChanged.AddListener(v => settings.burstThreshold = v);

        // Mode toggles
        micToggle?.onValueChanged.AddListener(on
            => { if (on) capture.SetMode(CaptureMode.Microphone); });
        internalToggle?.onValueChanged.AddListener(on
            => { if (on) capture.SetMode(CaptureMode.InternalAudio); });

        // Buttons
        startButton?.onClick.AddListener(OnStart);
        stopButton?.onClick.AddListener(OnStop);
        fpsToggleButton?.onClick.AddListener(OnFpsToggle);

        Application.targetFrameRate = 60;
    }

    private void Update()
    {
        _frameCount++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 1f)
        {
            _shownFps   = _frameCount;
            _frameCount = 0;
            _fpsTimer   = 0f;
        }
        fpsText?.SetText($"FPS {_shownFps}");
    }

    private void OnStart()
    {
        capture.StartCapture();
        statusText?.SetText("Running");
    }

    private void OnStop()
    {
        capture.StopCapture();
        statusText?.SetText("Stopped");
    }

    private void OnFpsToggle()
    {
        _highFps = !_highFps;
        Application.targetFrameRate = _highFps ? 60 : 30;
        fpsToggleButton.GetComponentInChildren<TextMeshProUGUI>()
            ?.SetText(_highFps ? "60 FPS" : "30 FPS");
    }
}
