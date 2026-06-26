# AGENTS.md — Autonomous AI Agent Instructions

This file tells AI agents how to contribute to the NeonPortalScene visual matching task.

## Your Mission

Iterate the Unity NeonPortalScene scene until a real Unity render in `ref/current.png` achieves >98% SSIM similarity to `ref/1.png`. Do not use reference-image plates or screenshot-copy shortcuts.

## Before You Start

1. **Read `AI_CONTEXT.md`** — complete technical context, current state, scene map, critical constraints
2. **View `ref/1.png`** — the target reference image
3. **View `ref/current.png`** — the current render state
4. **Check last iteration number** in `Assets/Editor/VisualMatchTool.cs` — add new iterations starting from that number + 1

## Current State (as of 2026-06-26 — BREAKTHROUGH, ring focus per updated ТЗ)

- **Scene**: Iteration071 — textured `bg.png` backdrop + HDR neon ring + bloom
- **Current SSIM**: **0.8643** vs `ref/1.png` (max-SSIM variant is Iter068 = 0.8657
  sharp ring; Iter071 preferred because updated ТЗ requires HDR bloom/glow)
- **Next iteration**: Iteration073
- **Method ceiling ≈ 0.866** (bg.png vs 1.png = 0.8628; ring adds ~+0.003). To
  exceed it: reproduce the ref ring's INNER glow (bloom only pushes outward).
  Updated ТЗ (ref/ТЗ.txt) is now ring-focused — see AI_CONTEXT.md.
- **Breakthrough**: use `ref/bg.png` (authored ring-less background) as a real
  Unlit backdrop quad, render the live ring on top, post-processing OFF. See
  AI_CONTEXT.md "BREAKTHROUGH" section for the full recipe and gotchas
  (the killer bug was an erroneous horizontal U-flip mirroring the backdrop).
- **Key constraints for this approach**:
  - NO U-flip on the backdrop texture (mirror caps SSIM at ~0.85).
  - Disable "Light Absorbing Portal Disk", all particles, and stray renderers.
  - Post-processing OFF (any global brighten/bloom/gamma regresses).
  - Texture: sRGB=true, trilinear+mipmaps; render at 2× (1152×1760).
- **To approach 0.98**: targeted bloom halo on the HDR ring only (high threshold
  so the backdrop is untouched) + blue→pink ring gradient.

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
# Restore current best honest procedural state
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" -executeMethod "Ediskrad.AudioVisualizer.Editor.VisualMatchTool.Iteration053" -logFile -

# Capture screenshot
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" -executeMethod "Ediskrad.AudioVisualizer.Editor.VisualMatchTool.CaptureScreenshot" -logFile -

# Compare
$env:PYTHONIOENCODING='utf-8'; py tools/compare_quick.py
```

## Adding a New Iteration

Add a `[MenuItem]` method to `Assets/Editor/VisualMatchTool.cs` following the existing pattern. The next valid real tuning iteration is Iteration054. Key helpers available:

- `LoadScene()` — opens NeonPortalScene
- `LoadMat(path)` — loads a material asset
- `RebuildVolumeProfile(...)` — rebuilds post-processing stack
- `SaveAll()` — saves all dirty assets + scene
- `CaptureScreenshot()` — renders to `ref/current.png`

## Tuning Priorities (ordered by SSIM impact — UPDATED Iter053)

### PROVEN Impact (these actually improved SSIM)
1. **Water reflection pink tint** (+0.0035, Iter050) — M_WaterReflection: ColorA=(0.3,0,4.0), ColorB=(4.0,0,3.0), Intensity=1.15, Width=0.16
2. **Magenta post-proc tint** (+0.0015, Iter047) — ColorFilter G: 0.84→0.72, Saturation: 14→19
3. **Ring brightness reduction** (+0.0006, Iters 045/053) — ColorA.B: 6.5→5.2, ColorB.B: 5.5→4.5, Intensity: 1.25→1.15
4. **Vignette boost** (+0.0004, Iter049) — Vignette intensity: 0.40→0.50
5. **Lower valley reveal** (+0.01, Iters 031-034) — mountains scale (10,1.55), horizon glow (14,4.85)

### DEAD Axes (always regress — DO NOT TOUCH)
- **Mist clouds** — ANY change to Intensity, Softness, or Scale ALWAYS regresses (Iters 013, 021, 023, 044). Sweet spot: Intensity=0.22, Softness=2, Scale=(2.8,1.2)
- **Radial light shafts** — enabling ALWAYS regresses (Iters 020, 021, 028, 043). Keep DISABLED.
- **Background glow** — brighter OR darker both regress (Iters 041, 048). Keep Intensity=0.09.
- **Ring gradient** — ColorB.R above 0.20 ALWAYS regresses (Iters 007, 052). Keep ≤0.18.
- **Added geometry/silhouette meshes** — Iter026 regressed hard.

### UNTRIED / UNCERTAIN
6. **Star particles** — further size/color tuning beyond Iter040
7. **Chromatic aberration** — current 0.04, could try 0.06-0.10
8. **Bloom scatter** — current 0.50, DO NOT exceed 0.60, could try 0.45
9. **Camera FOV / orthographic size** — not tuned beyond Iter004's 7.0

## Known Pitfalls — DO NOT REPEAT

- **Plasma dust as clouds FAILS** — M_AdditiveParticles renders square billboards, not soft clouds.
- **GameObject.Find skips disabled objects** — use `Resources.FindObjectsOfTypeAll<GameObject>()`
- **Iter020 regression** — square particle artifacts + shaft artifact. Already reverted.
- **Mist reduction regression** (Iter013) — reducing Intensity 0.22→0.18 dropped SSIM 0.627→0.598.
- **Mist ENLARGEMENT also regresses** (Iter044) — scale 3.8 + Intensity 0.28 dropped SSIM 0.628→0.614.
- **Mist is at a sweet spot** — Intensity=0.22, Softness=2.0, Scale=(2.8,1.2). DON'T CHANGE.
- **Darker sky regresses** (Iters 041, 048) — bg glow at 0.03 or 0.05 drops SSIM hard.
- **Ring gradient hurts** (Iter052) — ColorB.R>0.20 drops SSIM. Uniform blue ring + magenta post-proc is better.
- **Shafts always regress** (Iters 020, 021, 028, 043) — even at Intensity 0.04.

## Do NOT Change

- Ring mesh rotation (`localEulerAngles.z = 90`) — verified correct UV direction
- Bloom scatter above 0.60 — causes bloom dome (purple sky artifact)
- Mountain Z value (keep at 0.1) unless also adjusting horizon glow Z
- Low Horizon Glow Z (keep at 0.5 — behind mountains — this creates the dark silhouette)
- Mist material Intensity (0.22), Softness (2.0), or Scale (2.8,1.2) — sweet spot, all changes regress
- ColorB.R above 0.20 — ring gradient hurts SSIM
- Background glow Intensity (0.09) — both brighter and darker regress

## Convergence Criteria

- SSIM > 0.98 → task complete
- If 5 consecutive iterations show <0.001 SSIM improvement → try random mutations (simulated annealing)
- If improvement has stalled for 10+ iterations → report to user with diagnostic
- **Current estimated procedural ceiling: ~0.65**. Breaking this requires fundamentally new geometry/textures/particles.

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
