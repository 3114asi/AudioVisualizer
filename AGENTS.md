# AGENTS

## Purpose

This file is for AI coding agents and human contributors who need a fast, reliable project entry point.

## Read Order

1. `AI_CONTEXT.md`
2. `docs/ARCHITECTURE.md`
3. `README.md`
4. `unity/UNITY_SETUP.md` — если работаешь с Unity-реализацией

## Non-Negotiable Rules

- Preserve package name: `com.ediskrad.audiovisualizer`.
- Preserve `minSdk = 29` unless explicitly changing platform support.
- Do not commit `local.properties`.
- Do not remove `MediaProjectionForegroundService` unless you also redesign and verify the full internal-audio flow on-device.
- Rebuild with `.\gradlew.bat assembleDebug` after non-trivial changes.

## Code Map

- UI: `ui/`
- Audio pipeline: `audio/`
- State orchestration: `visualizer/`
- Internal audio lifecycle: `MainActivity.kt`, `MediaProjectionForegroundService.kt`, `MediaProjectionRepository.kt`

## Safe Change Areas

- visual styling in `VisualizerCanvas.kt`
- text and control layout in `VisualizerScreen.kt`
- spectrum smoothing and band mapping in `SpectrumAnalyzer.kt`

## High-Risk Areas

- `MediaProjection` lifecycle
- `AudioRecord` configuration
- permissions and foreground service startup order

## Validation Checklist

- app launches
- `Microphone` mode starts
- `Internal Audio` mode starts after screen-capture consent
- `Stop` releases capture without crash
- debug build still assembles
