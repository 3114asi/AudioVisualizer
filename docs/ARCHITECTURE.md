# Architecture

## Overview

`AudioVisualizer` — нативное Android-приложение на Kotlin и Jetpack Compose.

Цель:

- визуализировать аудиосигнал в реальном времени;
- поддерживать `Microphone` и `Internal Audio`;
- оставаться простым для дальнейшего расширения.

## Layers

### UI

Папка: `app/src/main/java/com/ediskrad/audiovisualizer/ui`

- `VisualizerScreen.kt` — layout и control overlay.
- `VisualizerCanvas.kt` — отрисовка сцены, кольца, лучей, частиц и фоновой композиции.
- `AudioVisualizerApp.kt` — связывает `ViewModel` с Compose.

### State

Папка: `app/src/main/java/com/ediskrad/audiovisualizer/visualizer`

- `VisualizerViewModel.kt` — orchestration между UI и audio providers.
- `VisualizerState.kt` — immutable UI state.

### Audio

Папка: `app/src/main/java/com/ediskrad/audiovisualizer/audio`

- `AudioProvider` — контракт аудиоисточника.
- `AudioFrame` — готовый кадр для визуализатора.
- `VisualizerConfig` — настройки FFT и сглаживания.
- `SpectrumAnalyzer` — FFT, rebinning и energy bands.

Папка: `audio/capture`

- `MicrophoneAudioProvider` — `AudioRecord` от микрофона.
- `PlaybackCaptureAudioProvider` — `AudioRecord` + `AudioPlaybackCaptureConfiguration`.
- `BaseAudioProvider` — общий lifecycle и фоновой поток чтения.

Папка: `audio/fft`

- `ComplexFft` — собственная реализация FFT.
- `HannWindow` — оконная функция перед FFT.

### MediaProjection

- `MainActivity.kt` отвечает за permission flow и запуск screen capture intent.
- `MediaProjectionForegroundService.kt` стартует foreground service типа `mediaProjection`.
- `MediaProjectionRepository.kt` передает `MediaProjection` из service в `Activity`/`ViewModel`.

## Data Flow

### Microphone

`MainActivity` -> `VisualizerViewModel.start()` -> `MicrophoneAudioProvider` -> `SpectrumAnalyzer` -> `VisualizerState` -> `VisualizerCanvas`

### Internal Audio

`MainActivity` -> `createScreenCaptureIntent()` -> `MediaProjectionForegroundService` -> `MediaProjectionRepository` -> `VisualizerViewModel.attachProjection()` -> `PlaybackCaptureAudioProvider` -> `SpectrumAnalyzer` -> `VisualizerState`

## Important Constraints

### MediaProjection ordering

Для Android 14/15 и части MIUI-прошивок важно:

1. получить user consent;
2. поднять foreground service типа `mediaProjection`;
3. создавать `MediaProjection` уже после `startForeground`.

Нарушение этого порядка может приводить к отказу доступа или падению сценария `Internal Audio`.

### Performance

- Нет создания объектов в горячем UI-цикле рендера, кроме контролируемых Compose-перерисовок.
- FFT и чтение PCM вынесены в фоновые потоки.
- Для визуализации используется `Canvas`, без тяжелых внешних зависимостей.

## Extension Points

- добавить пресеты цветов и тем;
- вынести параметры визуализации в отдельный persistent settings layer;
- добавить release build, baseline profiles и профилирование;
- заменить `Canvas`-рендер на OpenGL/Vulkan при необходимости более тяжелых эффектов.

## Unity Implementation (`unity/`)

Параллельная реализация на Unity 6 URP с иной визуальной моделью:
кольцо из дискретных частиц (не сплошная линия) с аддитивным блендингом и URP Bloom.

### Архитектура Unity

```
AudioCaptureController (+ AudioSource)
        │
        ▼
SpectrumProcessor — логарифмический rebinning FFT → float[] Bands[64]
        │
        ├──▶ ParticleRingController — Particle System (Billboard, Additive)
        │         SetParticles() каждый кадр: pos = f(Bands[i]), color = gradient(angle)
        │
        └──▶ BurstEmitter — Particle System (StretchedBillboard, Additive)
                  Emit() при sum(Bands[0..3]) > burstThreshold
```

### Ключевые файлы Unity

| Файл | Роль |
|---|---|
| `VisualizerSettings.cs` | ScriptableObject — все параметры |
| `AudioCaptureController.cs` | Mic input + Internal Audio scaffold |
| `SpectrumProcessor.cs` | FFT → bands + smoothing |
| `ParticleRingController.cs` | Кольцо частиц с цветом по углу |
| `BurstEmitter.cs` | Bass-burst стрики |
| `UIController.cs` | UI overlay |
| `UNITY_SETUP.md` | Полная инструкция по сборке сцены |
