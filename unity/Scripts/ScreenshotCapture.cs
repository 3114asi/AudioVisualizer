using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Инструмент захвата скриншотов для итерационной проверки.
///
/// Режимы:
///   F12       — моментальный снимок по требованию
///   Auto      — каждые autoIntervalSec секунд (по умолчанию 3) пока игра запущена
///
/// Файлы сохраняются в Assets/Captures/ (Editor) или persistentDataPath/Captures/ (билд).
/// Имя: frame_YYYYMMDD_HHmmss_fff.png
///
/// Как использовать:
///   1. Добавить компонент на любой GameObject в сцене.
///   2. В Inspector задать нужный autoIntervalSec.
///   3. Запустить Play — первый скриншот будет через 3 сек, затем с заданным интервалом.
///   4. В любой момент нажать F12 для мгновенного снимка.
///   5. Скриншоты появятся в папке Captures (в Editor — в Assets/Captures, нажать Refresh).
///
/// Портретное разрешение: установите Game View в нужное соотношение
/// (напр. 9:19.5 или 1170x2532) — скриншот будет точно таким.
/// </summary>
public class ScreenshotCapture : MonoBehaviour
{
    [Header("Auto Capture")]
    [Tooltip("Интервал автоматических скриншотов в секундах (0 = отключить)")]
    public float autoIntervalSec = 3f;

    [Header("Supersize (только Editor)")]
    [Tooltip("1 = нативное разрешение Game View; 2 = удвоенное")]
    [Range(1, 4)]
    public int superSize = 1;

    private float _nextCapture;
    private string _captureDir;

    private void Start()
    {
        _captureDir = GetCaptureDir();
        EnsureDir(_captureDir);
        _nextCapture = Time.time + autoIntervalSec;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
            Capture("f12");

        if (autoIntervalSec > 0f && Time.time >= _nextCapture)
        {
            Capture("auto");
            _nextCapture = Time.time + autoIntervalSec;
        }
    }

    private void Capture(string tag)
    {
        string ts   = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string name = $"frame_{tag}_{ts}.png";
        string path = Path.Combine(_captureDir, name);
        ScreenCapture.CaptureScreenshot(path, superSize);
        Debug.Log($"[ScreenshotCapture] → {path}");
    }

    private static string GetCaptureDir()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "Captures");
#else
        return Path.Combine(Application.persistentDataPath, "Captures");
#endif
    }

    private static void EnsureDir(string dir)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}
