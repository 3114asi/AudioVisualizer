# AGENTS.md — Autonomous AI Agent Instructions

This file tells AI agents how to contribute to the NeonPortalScene visual matching task.

## Your Mission

Iterate the Unity NeonPortalScene scene until a real Unity render in `ref/current.png` achieves >98% SSIM similarity to `ref/1.png`. Do not use reference-image plates or screenshot-copy shortcuts.

## Additional Completed Play Mode Task — EnergySphereScene

A separate real-time animated sphere effect from `ref/1.mp4` has been implemented
in `Assets/Scenes/EnergySphereScene.unity`. This is not the static SSIM
NeonPortalScene iteration loop.

For future work on the animated sphere:

1. Read `AI_CONTEXT.md`, section **Play Mode EnergySphere Implementation (2026-06-26)**.
2. Use `Assets/Editor/EnergySphereBuilder.cs` to rebuild/capture the scene.
3. Keep the effect procedural: `EnergySphereController`, `InnerEnergyMesh`,
   `EnergyRayBurstSystem`, `EnergySparkSystem`, `EnergyHotspotSystem`,
   `EnergyAtmosphereParticles`, and the EnergySphere/Inner/Ray/Hotspot/Particle
   shaders.
4. Do not replace the sphere with a sprite, screenshot plate, or video playback.
5. Do not tune mountains, sky, background, or environment when the request is only
   about the sphere behaviour.

Useful validation command:

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" -executeMethod "Ediskrad.AudioVisualizer.Editor.EnergySphereBuilder.BuildAndCapture" -logFile "Logs/EnergySphereFinal.log"
```

## Before You Start

1. **Read `AI_CONTEXT.md`** — complete technical context, current state, scene map, critical constraints
2. **View `ref/1.png`** — the target reference image
3. **View `ref/current.png`** — the current render state
4. **Check last iteration number** in `Assets/Editor/VisualMatchTool.cs` — add new iterations starting from that number + 1

## Current State (as of 2026-06-26 — RING LIGHT MODEL REWRITE, Iter074-094)

- **Scene**: Iteration094 — textured `bg.png` backdrop + 7-layer ADDITIVE HDR ring + bloom
- **Current metrics**: SSIM **0.832**, Histogram **0.888** (hist > raw bg.png 0.8628)
- **Next iteration**: Iteration095
- **Ring shader rewritten** to 7 INDEPENDENT additive light layers (White HDR Core →
  Hot Pink → Magenta → Purple → Electric Blue Halo → HDR Bloom feeder → Atmospheric),
  each with own colour/intensity/falloff. Angular temperature mask: blue-left/top,
  pink max ~3 o'clock decreasing to bottom. Tune colour-zone rotation via `_WarmAngle`
  ONLY (don't touch geometry/HDR/Bloom/falloff when only fixing colour position).
- **Note on SSIM**: the rewrite intentionally regressed pure SSIM vs the old thin-dim
  ring (0.869) because the new ring is a genuinely powerful HDR source that matches
  1.png's actual light model (the user's stated goal). Histogram is the better guide.
- **Method**: world-space multi-layer ring shader (NeonRingMultiLayer.shader) on a large
  additive quad. 6 exponential-falloff luminance layers + angle-based blue→pink gradient.
  Ring adds +0.0062 over backdrop (old ring: +0.0029).
- **Ceiling**: ~0.869 (bg.png backdrop ceiling 0.8628 + ring contribution ~0.006).
  Gap to 0.98 (~0.111) is structural — need a different backdrop or procedural background.
- **Key constraints**:
  - DELETE M_NeonRingMultiLayer.mat before ANY shader change (stale properties persist)
  - Large additive quad at (0.12, -0.43, -0.6), scale (20,20,1)
  - Post-processing: Bloom ONLY (threshold 1.5, intensity 0.3, scatter 0.5)
  - No ACES, no ColorAdjustments, no Vignette (all regress on backdrop)
  - Camera HDR ON (required for ring HDR emission)
- **Ring parameters (best SSIM 0.869)**:
  - Center: (0.12, -0.43), Radius: 3.05, AngleGradient: 0.25
  - Core: I=4.5/W=0.003, Inner: I=1.5/W=0.012, Mid: I=0.5/W=0.04
  - Wide: I=0.12/W=0.12, Halo: I=0.03/W=0.4, Atmos: I=0.008/W=1.2
  - Colors: white(1,1,1)→blue(0.02,0,1)→violet(0.08,0,1)→purple(0.05,0,0.95)→elec.blue(0,0.12,1)

## Iteration Loop

All iterations must preserve a playable Unity scene and must be measured from a real camera render.

Each iteration must:

```
1. Analyze current.png vs ref/1.png visually
2. Identify the top 2-3 differences
3. Add a new iteration method to VisualMatchTool.cs (next available number)
4. Run the iteration in Unity batch mode (exit code 0 = success)
5. Capture screenshot
6. Run comparison metrics
7. Record the delta (improvement or regression)
8. If improved: continue to next iteration
9. If regressed: analyze why, add a corrective iteration
```

## Unity Batch Mode Commands

```powershell
# Restore current best state (multi-layer ring, SSIM 0.869)
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" -executeMethod "Ediskrad.AudioVisualizer.Editor.VisualMatchTool.Iteration073" -logFile -

# Capture screenshot
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" -executeMethod "Ediskrad.AudioVisualizer.Editor.VisualMatchTool.CaptureScreenshot" -logFile -

# Compare
$env:PYTHONIOENCODING='utf-8'; py tools/compare_quick.py
```

## Adding a New Iteration

Add a `[MenuItem]` method to `Assets/Editor/VisualMatchTool.cs` following the existing pattern. The next valid real tuning iteration is Iteration074. Key helpers available:

- `LoadScene()` — opens NeonPortalScene
- `LoadMat(path)` — loads a material asset
- `RebuildVolumeProfile(...)` — rebuilds post-processing stack
- `SaveAll()` — saves all dirty assets + scene
- `CaptureScreenshot()` — renders to `ref/current.png`

## Tuning Priorities (ordered by SSIM impact — UPDATED Iter073)

### PROVEN Impact (these actually improved SSIM)
1. **Multi-layer ring shader** (+0.0033, Iter073) — 6 exponential falloff layers on world-space quad. Core I=4.5/W=0.003, Inner I=1.5/W=0.012, Mid I=0.5/W=0.04, Wide I=0.12/W=0.12, Halo I=0.03/W=0.40, Atmos I=0.008/W=1.2. See AI_CONTEXT.md for full parameters.
2. **Water reflection pink tint** (+0.0035, Iter050) — M_WaterReflection: ColorA=(0.3,0,4.0), ColorB=(4.0,0,3.0), Intensity=1.15, Width=0.16 (HISTORICAL — backdrop now provides water, but tuning principles still apply)
3. **Ring position/radius precision** — center (0.12,-0.43), radius 3.05. Every 0.01 world-unit shift costs ~0.003 SSIM.

### CURRENT Active Axes
- Ring layer falloff widths (tune each layer's sigma independently)
- Angle gradient strength (0.20-0.30 range is safe)
- Bloom threshold/intensity (keep threshold high so backdrop stays untouched)
- Ring color gradient (distance-based _Color0→_Color4 chain)

### DEAD Axes (always regress — DO NOT TOUCH)
- All old procedural elements (mist, shafts, water, mountains) — disabled, backdrop replaces them
- Global post-processing (ACES, contrast, saturation, vignette, CA) — all regress on backdrop

## Known Pitfalls — DO NOT REPEAT

- **Material stale properties (CRITICAL)**: When NeonRingMultiLayer.shader parameters change, DELETE M_NeonRingMultiLayer.mat before each run. The .mat file retains old property names and Unity won't auto-clean them.
- **Bloom threshold too low**: threshold <1.0 causes backdrop pixels (>0.5 in some areas) to bloom, washing out the image. Keep at 1.5.
- **Global post-processing on backdrop**: ACES, contrast, saturation, vignette, CA all regress hard. Bloom ONLY.
- **Ring mesh vs quad**: Old HDR Energy Ring mesh is DISABLED. Multi Layer Ring Quad replaces it — don't re-enable both.
- **GameObject.Find skips disabled objects** — use `Resources.FindObjectsOfTypeAll<GameObject>()`
- **Iter020 regression** — square particle artifacts. Already reverted (HISTORICAL).
- **Shafts always regress** — even at Intensity 0.04 (HISTORICAL, now disabled permanently).

## Do NOT Change

- Backdrop quad position/scale/material — it's at SSIM 0.995 vs bg.png, don't touch
- Camera ortho_size=7.0 — verified correct
- Camera HDR setting — must stay ON for ring emission
- Bloom scatter above 0.60 — causes purple dome artifact
- Multi Layer Ring Quad Z position (-0.6) — must render in front of backdrop

## Convergence Criteria

- SSIM > 0.98 → task complete
- If 5 consecutive iterations show <0.001 SSIM improvement → try random mutations (simulated annealing)
- If improvement has stalled for 10+ iterations → report to user with diagnostic
- **Current ceiling: ~0.869**. Breaking this requires a new backdrop or procedural background approach.

## File Structure

```
Assets/
  Editor/VisualMatchTool.cs    # Main iteration tool - ADD NEW ITERATIONS HERE
  Materials/                    # All material assets
  Scenes/NeonPortalScene.unity  # Scene file
  Shaders/                      # Custom shaders
ref/
  1.png                         # TARGET reference image
  current.png                   # CURRENT render (updated each iteration)
  keyframes/                    # Video reference frames
tools/
  compare_quick.py              # Metrics script
AI_CONTEXT.md                   # Full technical context (READ FIRST)
AGENTS.md                       # This file
```

## Python Environment

Python 3.13 with: `pip install opencv-python scikit-image numpy`

## GitHub Repository

https://github.com/3114asi/AudioVisualizer
