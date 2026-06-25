# Unity Setup — AudioVisualizer

Unity 6 (6000.x), URP, Android ARM64, portrait.

## Поток данных

```
AudioSource.GetSpectrumData(1024, Blackman)
        │
        ▼
SpectrumProcessor → float[] Bands [64]   ← лог. rebinning + attack/release
        │
        ├──▶ ParticleRingController
        │         particle[i].pos   = circle(angle_i, baseRadius + Bands[i] * sensitivity)
        │         particle[i].color = Lerp(colorLeft, colorRight, cosine(i/count))
        │         particle[i].size  = Lerp(sizeMin, sizeMax, Bands[i])
        │
        └──▶ BurstEmitter
                  if sum(Bands[0..3]) > burstThreshold → Emit(N) стриков наружу
```

## Иерархия сцены

```
AudioVisualizer (Empty)
├── AudioEngine (Empty)
│     ├── AudioCaptureController.cs
│     ├── SpectrumProcessor.cs
│     └── AudioSource  ← добавляется RequireComponent автоматически
│
├── ParticleRing (Empty, pos = 0,0,0)
│     ├── ParticleRingController.cs
│     └── Particle System  ← материал RingParticle (Additive)
│
├── BurstEmitter (Empty, pos = 0,0,0)
│     ├── BurstEmitter.cs
│     └── Particle System  ← материал BurstParticle (Additive, StretchedBillboard)
│
├── Main Camera (pos = 0,0,-10)
│     └── Volume  ← URP Bloom
│
└── Canvas (Screen Space – Overlay)
      └── UIController.cs + UI-элементы
```

## Шаг 1 — ScriptableObject

1. `Assets → Create → AudioVisualizer → Settings`
2. Сохранить как `VisualizerSettings.asset`
3. Настроить параметры под сцену (baseRadius, sensitivity и т.д.)

## Шаг 2 — AudioEngine

1. Create Empty → `AudioEngine`
2. Add Component: `AudioCaptureController`, `SpectrumProcessor`
3. В обоих скриптах поле `Settings` → перетащить `VisualizerSettings.asset`

## Шаг 3 — ParticleRing

1. Create Empty → `ParticleRing`, position `(0, 0, 0)`
2. Add Component → `Particle System`, `ParticleRingController`
3. В `ParticleRingController`:
   - `Settings` → `VisualizerSettings.asset`
   - `Spectrum` → GameObject `AudioEngine`

## Шаг 4 — BurstEmitter

1. Create Empty → `BurstEmitter`, position `(0, 0, 0)`
2. Add Component → `Particle System`, `BurstEmitter`
3. В `BurstEmitter`:
   - `Settings` → `VisualizerSettings.asset`
   - `Spectrum` → GameObject `AudioEngine`

## Шаг 5 — Материалы (URP Additive)

### RingParticle (для кольца)
1. `Assets → Create → Material` → `RingParticle`
2. Shader: `Universal Render Pipeline / Particles / Unlit`
3. Surface Type: `Transparent`
4. Blending Mode: `Additive`
5. Base Map: белый или мягкое circular glow texture
6. Перетащить на `ParticleSystemRenderer` компонента `ParticleRing`

### BurstParticle (для стриков)
1. Дублировать `RingParticle` → `BurstParticle`
2. Те же настройки, перетащить на `BurstEmitter`

## Шаг 6 — URP Bloom

1. `Main Camera` → Add Component → `Volume`
2. Profile → `New` → `PostProcessing`
3. `Add Override → Bloom`
4. Включить: `Intensity` ≈ 2.0, `Threshold` ≈ 0.8, `Scatter` ≈ 0.7
5. На камере: включить чекбокс `Post Processing`

> В Unity 6: Project Settings → Graphics → URP Renderer Data → убедиться что Post Processing включён.

## Шаг 7 — Камера

- Position: `(0, 0, -10)`
- Projection: `Orthographic`, Size: `5`
- Background: `Solid Color` → `#03040D`

## Шаг 8 — UI Canvas

1. `GameObject → UI → Canvas` (Screen Space – Overlay)
2. Добавить дочерние элементы:
   - `TextMeshPro` → "AUDIO VISUALIZER" (верх)
   - `TextMeshPro` → статус ("Ready")
   - `Toggle Group` с двумя `Toggle`: Microphone / Internal Audio
   - `Slider` → Sensitivity (min 0.5, max 3.0)
   - `Slider` → Bass
   - `Button` → Start, Stop, FPS Toggle
   - `TextMeshPro` → FPS
3. Create Empty на Canvas → Add Component `UIController`
4. Подключить все ссылки в инспекторе

### Логотип внутри кольца
- Отдельный Canvas с `Render Mode = World Space`
- Добавить `TextMeshPro` с текстом `EDISK`
- Position: `(0, 0, -0.1)`, масштаб под диаметр кольца
- Подключить к полю `logoText` в `UIController`

## Шаг 9 — Android Build

1. `File → Build Settings → Android → Switch Platform`
2. `Player Settings`:
   - `Other Settings → Scripting Backend: IL2CPP`
   - `Target Architectures: ARM64`
   - `Minimum API Level: 24`
   - `Target API Level: 35`
   - `Internet Access: Required`
   - `Microphone Usage Description`: добавить строку
3. Подключить телефон, включить USB Debugging
4. `Build and Run` → Unity установит APK напрямую

## Internal Audio — статус

Захват системного звука (`Internal Audio`) сейчас является scaffold с заглушкой (падает на микрофон).
Для полной реализации нужен нативный Kotlin-плагин — см. TODO-комментарии в `AudioCaptureController.cs`.

Готовый пример нативной части: `../../app/src/main/java/com/ediskrad/audiovisualizer/audio/capture/PlaybackCaptureAudioProvider.kt`

## Параметры Inspector

Все параметры живут в `VisualizerSettings.asset` и изменяются в Play Mode без перекомпиляции:

| Параметр | Назначение | Default |
|---|---|---|
| `particleCount` | Кол-во точек кольца | 130 |
| `baseRadius` | Базовый радиус | 3.0 |
| `maxRadialOffset` | Макс. выброс частицы | 1.5 |
| `colorLeft` | Цвет левой дуги (cyan) | #3FC6FF |
| `colorRight` | Цвет правой дуги (magenta) | #E040FB |
| `sensitivity` | Усиление FFT | 1.35 |
| `bandCount` | Полос после rebinning | 64 |
| `attack` | Скорость нарастания | 0.30 |
| `release` | Скорость спада | 0.08 |
| `burstThreshold` | Порог выброса (бас) | 0.60 |
| `burstForce` | Скорость стриков | 6.0 |
| `burstCooldown` | Пауза между выбросами | 0.10 s |
