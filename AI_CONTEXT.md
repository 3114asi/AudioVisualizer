# AI Context

## Project Summary

- Name: `AudioVisualizer`
- Package: `com.ediskrad.audiovisualizer`
- Stack: Kotlin, Android SDK, Jetpack Compose, Canvas rendering
- Min SDK: 29
- Target SDK: 35
- Build: Gradle Kotlin DSL

## What The App Does

Приложение рисует неоновый аудиовизуализатор в стиле NCS-like reference:

- центральное кольцо;
- лучи по спектру;
- частицы и glow;
- фоновая ночная сцена.

## Critical Files

- `app/src/main/java/com/ediskrad/audiovisualizer/MainActivity.kt`
- `app/src/main/java/com/ediskrad/audiovisualizer/MediaProjectionForegroundService.kt`
- `app/src/main/java/com/ediskrad/audiovisualizer/MediaProjectionRepository.kt`
- `app/src/main/java/com/ediskrad/audiovisualizer/visualizer/VisualizerViewModel.kt`
- `app/src/main/java/com/ediskrad/audiovisualizer/audio/SpectrumAnalyzer.kt`
- `app/src/main/java/com/ediskrad/audiovisualizer/ui/VisualizerCanvas.kt`

## Internal Audio Rule

Do not move `MediaProjectionManager.getMediaProjection(...)` back into the activity flow before the foreground service is started.

Current project intentionally creates the `MediaProjection` inside `MediaProjectionForegroundService` after `startForeground(...)` because the simpler flow was unstable on the target device.

## Build Notes

- `local.properties` is intentionally ignored.
- `android.overridePathCheck=true` is enabled because the project path contains non-ASCII characters on Windows.
- `android.suppressUnsupportedCompileSdk=35` suppresses AGP warning noise.

## Expected Workflow For Changes

1. Keep UI changes isolated in `ui/`.
2. Keep audio capture and FFT changes isolated in `audio/`.
3. If changing `Internal Audio`, re-test the exact flow:
   - select `Internal Audio`
   - tap `Start`
   - grant `Show screen`
4. Run `.\gradlew.bat assembleDebug`.

## Unity Implementation

Directory `unity/` contains a parallel Unity 6 (URP) implementation:

- `unity/Scripts/VisualizerSettings.cs` — ScriptableObject with all tunable params.
- `unity/Scripts/AudioCaptureController.cs` — Mic + Internal Audio scaffold.
- `unity/Scripts/SpectrumProcessor.cs` — FFT → 64 bands, log rebinning, attack/release smoothing.
- `unity/Scripts/ParticleRingController.cs` — discrete particle ring, color by angle, radial FFT reaction.
- `unity/Scripts/BurstEmitter.cs` — bass-triggered outward streak bursts (StretchedBillboard).
- `unity/Scripts/UIController.cs` — Start/Stop, mode toggle, sensitivity slider, FPS counter.
- `unity/UNITY_SETUP.md` — full scene hierarchy, material, URP Bloom, and Android build instructions.

Internal Audio in Unity is a scaffold (falls back to mic). Full implementation requires a Kotlin plugin
mirroring `audio/capture/PlaybackCaptureAudioProvider.kt` with a JNI bridge.

## Known Limitations

- Some apps cannot be captured due to Android playback capture policy.
- Visualizer uses Compose Canvas, not OpenGL.
- No persistent settings storage yet.
- Unity Internal Audio capture is not yet implemented (scaffold + TODO comments).
