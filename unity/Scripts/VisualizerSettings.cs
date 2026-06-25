using UnityEngine;

/// <summary>
/// ScriptableObject — единственное место для всех параметров визуализатора.
/// Создать: Assets → Create → AudioVisualizer → Settings.
/// Все поля изменяемы в Inspector в Play Mode без перекомпиляции.
///
/// ВАЖНО: colorLeft/colorRight — HDR-цвета (intensity > 1).
/// Активировать HDR в Inspector: нажать кружок рядом с полем цвета → HDR.
/// Рекомендуемые значения intensity: 2-4 (даст визуальный «блум»).
/// </summary>
[CreateAssetMenu(fileName = "VisualizerSettings", menuName = "AudioVisualizer/Settings")]
public class VisualizerSettings : ScriptableObject
{
    [Header("Ring Geometry")]
    public int   particleCount   = 130;
    public float baseRadius      = 3f;
    /// Максимальный сдвиг наружу = maxRadialOffsetFraction * baseRadius.
    /// 0.18 = 18 % — частица не улетит дальше 18 % радиуса при любой амплитуде.
    [Range(0.05f, 0.50f)]
    public float maxRadialOffsetFraction = 0.18f;

    [Header("Colors (HDR — поставьте intensity 2-4 в Inspector)")]
    [ColorUsage(true, true)]   // showAlpha=true, hdr=true
    public Color colorLeft  = new Color(0.247f, 0.776f, 1.000f) * 2.5f; // CYAN  #3FC6FF ×2.5
    [ColorUsage(true, true)]
    public Color colorRight = new Color(0.878f, 0.251f, 0.984f) * 2.5f; // MAGENTA #E040FB ×2.5

    [Header("Audio")]
    [Range(0.5f, 4f)]  public float sensitivity = 1.35f;
    public int fftSize   = 1024;
    [Range(32, 128)]   public int bandCount    = 64;

    [Header("Smoothing — temporal")]
    [Range(0.01f, 1f)] public float attack  = 0.30f;
    [Range(0.01f, 1f)] public float release = 0.08f;
    /// Temporal lerp для позиции частиц кольца (0.15 = плавный, 1 = мгновенный).
    [Range(0.05f, 1f)] public float particlePositionLerp = 0.15f;

    [Header("Smoothing — spatial (по индексу полос)")]
    /// Число соседних полос для усреднения с каждой стороны (0 = выкл).
    /// 3 рекомендуется: убирает DC-spike сверху без потери детализации.
    [Range(0, 8)]      public int spatialSmoothHalfWidth = 3;

    [Header("Particle Size")]
    public float particleSizeMin = 0.06f;
    public float particleSizeMax = 0.22f;

    [Header("Bass Burst")]
    [Range(0f, 2f)] public float burstThreshold = 0.30f;
    public float burstForce    = 6f;
    public float burstCooldown = 0.10f;
    [Range(1, 64)]  public int   burstMaxSparks  = 48;
}
