# AudioVisualizer

Android-приложение `com.ediskrad.audiovisualizer` с неоновым аудиовизуализатором в стиле референса `reference/1.png`.

## Что реализовано

- Jetpack Compose UI с полноэкранной сценой.
- Реактивное неоновое кольцо, лучи и частицы на `Canvas`.
- FFT 1024, сглаживание спектра, вычисление `bass`, `mids`, `highs`.
- Два режима входа:
  - `Microphone`
  - `Internal Audio` через `MediaProjection` + `AudioPlaybackCapture`
- Минималистичный overlay с `Start`, `Stop`, выбором режима, `Sensitivity`, `FPS`.
- Совместимость с Android 10+ и актуальным порядком запуска `MediaProjection` для Android 14/15.

## Структура

- `app/src/main/java/com/ediskrad/audiovisualizer/audio` — аудио-абстракции и анализ.
- `app/src/main/java/com/ediskrad/audiovisualizer/audio/capture` — провайдеры захвата.
- `app/src/main/java/com/ediskrad/audiovisualizer/ui` — Compose UI и отрисовка.
- `app/src/main/java/com/ediskrad/audiovisualizer/visualizer` — состояние и orchestration.
- `app/src/main/java/com/ediskrad/audiovisualizer/MediaProjectionForegroundService.kt` — foreground service для корректного internal audio capture.
- `docs/ARCHITECTURE.md` — техническая архитектура.
- `AGENTS.md` и `AI_CONTEXT.md` — быстрый контекст для ИИ-ассистентов.

## Ключевые сценарии

### Microphone

1. Пользователь выбирает `Microphone`.
2. При `Start` приложение запрашивает `RECORD_AUDIO`, если нужно.
3. `MicrophoneAudioProvider` читает PCM из `AudioRecord`.
4. `SpectrumAnalyzer` считает FFT и отдает данные в UI.

### Internal Audio

1. Пользователь выбирает `Internal Audio`.
2. При `Start` приложение вызывает `createScreenCaptureIntent()`.
3. После подтверждения поднимается `MediaProjectionForegroundService`.
4. Уже внутри foreground service создается `MediaProjection`.
5. `PlaybackCaptureAudioProvider` читает системный PCM через `AudioPlaybackCaptureConfiguration`.

Это важно: на части устройств и прошивок получение `MediaProjection` из `Activity` до полноценного foreground service приводит к сбоям или отказу доступа.

## Сборка

```bash
./gradlew assembleDebug
```

Windows:

```powershell
.\gradlew.bat assembleDebug
```

Готовый APK:

- `app/build/outputs/apk/debug/app-debug.apk`

## Требования

- Android Studio Jellyfish+ или совместимая версия.
- Android SDK установлен локально.
- `minSdk = 29`
- JDK 17+.

## Internal Audio

- При выборе `Internal Audio` система запросит screen capture consent.
- Захват работает только для приложений, которые разрешают playback capture на Android 10+.
- Некоторые стриминговые приложения могут не отдавать аудио в capture API по политике платформы.
- На Android 14+ для этого сценария обязателен foreground service типа `mediaProjection`.

## Совместимость

- `minSdk 29`, `targetSdk 35`.
- Проверенный сценарий сборки: Windows + Android Studio + локальный Android SDK.
- Для Windows-пути с не-ASCII символами включен `android.overridePathCheck=true`.
- Для `compileSdk = 35` добавлен `android.suppressUnsupportedCompileSdk=35`, чтобы текущая версия AGP не шумела при синхронизации.

## Unity-реализация

Директория `unity/` содержит альтернативную реализацию на Unity 6 (URP):

- Кольцо из дискретных светящихся частиц с аддитивным блендингом и URP Bloom.
- Градиент по углу: cyan (#3FC6FF) → magenta (#E040FB).
- Bass-burst: стрики наружу от кольца при ударах баса.
- Те же два режима входа: Microphone и Internal Audio (scaffold).

**Скрипты:** `unity/Scripts/`
**Инструкция по сборке:** `unity/UNITY_SETUP.md`

## Для разработчиков и ИИ

- Быстрый архитектурный контекст: `AI_CONTEXT.md`
- Инструкции по изменению проекта: `AGENTS.md`
- Подробная архитектура: `docs/ARCHITECTURE.md`
- Unity-реализация: `unity/UNITY_SETUP.md`

## Примечание по пути проекта

Проект находится в директории с не-ASCII символами, поэтому в `gradle.properties` включен `android.overridePathCheck=true`. Это нужно для корректной сборки на Windows.
