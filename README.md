# AudioVisualizer Neon Portal

Unity/Android проект `com.ediskrad.audiovisualizer` с процедурной сценой неонового космического портала в вертикальном формате 9:16.

## Требования

- Unity 2022 LTS или новее.
- Android Build Support.
- При первом открытии Unity установит зависимости из `Packages/manifest.json`: URP, Shader Graph и Visual Effect Graph.

## Запуск

1. Откройте корневую папку проекта в Unity Hub или Unity Editor.
2. Дождитесь импорта пакетов.
3. Сцена `Assets/Scenes/NeonPortalScene.unity` создается автоматически editor-bootstrap-скриптом.
4. Если сцену нужно пересобрать вручную: `Tools > AudioVisualizer > Rebuild Neon Portal Scene`.
5. Откройте `NeonPortalScene` и нажмите Play.

## Android настройки

Bootstrap задает:

- Package name: `com.ediskrad.audiovisualizer`.
- Target Platform: Android.
- Graphics API: Vulkan.
- Color Space: Linear.
- Texture compression: ASTC.
- Scripting Backend: IL2CPP.
- Architecture: ARM64.
- HDR на камере включен.
- Вертикальная композиция под 1152 x 1760 и 9:16.

## Качество

В `ProjectSettings/QualitySettings.asset` заведены профили `Low`, `Medium`, `High`, `Ultra`.

- `Low`: меньше частиц, ниже LOD, сниженный resolution scale.
- `Medium`: умеренный bloom/частицы для массовых Android-устройств.
- `High`: целевой профиль для современных устройств.
- `Ultra`: максимум частиц и более плотные прозрачные слои.

Скрипт `ParticleLODController` автоматически применяет лимиты частиц по текущему quality level. Диапазон сцены: примерно 3000-15000 частиц.

## Эффекты сцены

- Фон: почти черный космос с мягким сине-фиолетовым свечением и звездной пылью.
- Центральный объект: абсолютно черный диск без деталей, имитирующий затмение/портал.
- Энергетическое кольцо: HDR emission, cyan слева и magenta/фиолетовый справа/снизу, Fresnel-like glow, пульсация и бегущие сегменты.
- Плазменная корона: процедурный неровный annulus mesh с animated noise/displacement в шейдере.
- Частицы: звездная пыль, радиальные искры, плазменные точки и редкие крупные вспышки на additive material.
- Световые лучи: короткие radial shaft planes, которые вспыхивают случайными импульсами.
- Дымка: прозрачные noise planes над нижней частью сцены и горами.
- Низ сцены: темные горные силуэты, пурпурно-синяя подсветка горизонта и дрожащее отражение на воде.
- Постобработка: Bloom, ACES tonemapping, cold color grading, vignette, слабая chromatic aberration.
- Анимация: 10-секундный loop с пульсацией кольца, flow по окружности, движением частиц, вспышками лучей, дымкой, ripple-отражением и медленным camera push-in.

## Скрипты

- `PortalPulseController.cs`: управляет пульсацией emission, accent lights и Bloom.
- `RingEnergyFlow.cs`: двигает shader flow и плазменные точки по окружности.
- `RadialBurstSpawner.cs`: включает случайные вспышки радиальных лучей.
- `ParticleLODController.cs`: снижает лимиты частиц по quality level.
- `WaterReflectionAnimator.cs`: анимирует дрожание отражения на воде.
- `CameraCinematicMotion.cs`: добавляет медленный cinematic push-in.

## Шейдеры

Проект подключает Shader Graph/VFX Graph пакеты для URP. Сгенерированная сцена использует легкие URP-compatible shader assets в `Assets/Shaders`, сделанные как production-friendly эквиваленты Shader Graph: emission ring, additive mist/rays и water reflection. Такой вариант проще контролировать в git и стабильнее для Android.
