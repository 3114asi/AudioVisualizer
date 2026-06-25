using UnityEngine;

/// <summary>
/// ScriptableObject — единственное место для всех параметров визуализатора.
/// Создать: Assets → Create → AudioVisualizer → Settings.
/// Все поля изменяемы в Inspector в Play Mode без перекомпиляции.
/// </summary>
[CreateAssetMenu(fileName = "VisualizerSettings", menuName = "AudioVisualizer/Settings")]
public class VisualizerSettings : ScriptableObject
{
    [Header("Ring Geometry")]
    /// Количество световых точек, равномерно распределённых по кольцу
    public int particleCount = 130;
    /// Базовый радиус кольца в мировых единицах
    public float baseRadius = 3f;
    /// Максимальный радиальный выброс частицы при пиковой амплитуде
    public float maxRadialOffset = 1.5f;

    [Header("Colors")]
    /// Цвет левой дуги — cyan (#3FC6FF)
    public Color colorLeft  = new Color(0.247f, 0.776f, 1.000f);
    /// Цвет правой дуги — magenta (#E040FB)
    public Color colorRight = new Color(0.878f, 0.251f, 0.984f);

    [Header("Audio")]
    /// Множитель, применяемый к значениям полос FFT
    [Range(0.5f, 3f)] public float sensitivity = 1.35f;
    /// Размер FFT-окна (степень двойки); больше = лучше разрешение, выше CPU
    public int fftSize = 1024;
    /// Количество частотных полос после логарифмического rebinning
    [Range(32, 128)] public int bandCount = 64;

    [Header("Smoothing")]
    /// Скорость нарастания полосы — выше = быстрее attack, резче реакция
    [Range(0.01f, 1f)] public float attack  = 0.30f;
    /// Скорость спада полосы — ниже = дольше «хвост» после пика
    [Range(0.01f, 1f)] public float release = 0.08f;

    [Header("Particle Size")]
    /// Размер частицы кольца в тишине
    public float particleSizeMin = 0.06f;
    /// Размер частицы кольца при пиковой амплитуде
    public float particleSizeMax = 0.22f;

    [Header("Bass Burst")]
    /// Сумма 4 нижних полос, при превышении которой срабатывает выброс
    [Range(0f, 4f)] public float burstThreshold = 0.60f;
    /// Начальная скорость частиц выброса (стриков)
    public float burstForce    = 6f;
    /// Минимальная пауза между выбросами в секундах
    public float burstCooldown = 0.10f;
}
