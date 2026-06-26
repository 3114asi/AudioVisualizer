# AGENTS.md — Autonomous AI Agent Instructions

This file tells AI agents how to contribute to the NeonPortalScene visual matching task.

## Your Mission

Iterate the Unity NeonPortalScene scene until `ref/current.png` achieves >98% SSIM similarity to `ref/1.png`. Work autonomously — no user questions, no stopping until converged or improvement is impossible.

## Before You Start

1. **Read `AI_CONTEXT.md`** — complete technical context, current state, scene map, critical constraints
2. **View `ref/1.png`** — the target reference image
3. **View `ref/current.png`** — the current render state
4. **Check last iteration number** in `Assets/Editor/VisualMatchTool.cs` — add new iterations starting from that number + 1

## Current State (as of 2026-06-26)

- **Scene**: Iter012 applied, SSIM=0.627
- **Next iteration to implement**: Iter021
- **Iter021 plan is fully documented** in `AI_CONTEXT.md` — implement it first

## Iteration Loop

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
# Run iteration NNN (replace NNN with actual number, e.g., Iteration021)
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" -executeMethod "Ediskrad.AudioVisualizer.Editor.VisualMatchTool.IterationNNN" -logFile -

# Capture screenshot
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" -executeMethod "Ediskrad.AudioVisualizer.Editor.VisualMatchTool.CaptureScreenshot" -logFile -

# Compare
py tools/compare_quick.py
```

## Adding a New Iteration

Add a `[MenuItem]` method to `Assets/Editor/VisualMatchTool.cs` following the existing pattern (Iterations 001–020). Key helpers available:

- `LoadScene()` — opens NeonPortalScene
- `LoadMat(path)` — loads a material asset
- `RebuildVolumeProfile(...)` — rebuilds post-processing stack
- `SaveAll()` — saves all dirty assets + scene
- `CaptureScreenshot()` — renders to `ref/current.png`

## Tuning Priorities (ordered by SSIM impact)

### HIGH Impact (do these first — large visual gap vs reference)
1. **Mist clouds** — scale "Animated Volumetric Mist" objects from (2.8,1.2) to (5.0,2.5)
   AND boost M_VioletMist._Intensity: 0.22 → 0.38, _Softness: 2 → 1.5
2. **Radial light shafts** — enable "Random Radial Light Shafts" (DISABLED in scene, use
   `Resources.FindObjectsOfTypeAll` not `GameObject.Find`) AND set M_RadialRay._Intensity=0.28

### MEDIUM Impact
3. **Ring right-side color** — M_NeonRing ColorB.R: 0.45 → 0.26 (ref R/B = 0.22, curr = 0.38)
4. **Ring size** — scale ring to (1.028, 1.028, 1) to match r_norm target 0.348

### LOW Impact
5. **Star particles** — Star Dust Field size=0.048 (3 pixels). Ref has brighter/more visible stars.
6. **Plasma dust** — Magenta Plasma Dust emitter radius=7.4 (off-screen). **WARNING**: changing
   this to an in-view emitter makes particles render as solid squares (wrong material for clouds).
   DO NOT attempt unless you switch material to M_VioletMist or a soft circular particle shader.

## Known Pitfalls — DO NOT REPEAT

- **Plasma dust as clouds FAILS** — M_AdditiveParticles renders square billboards, not soft clouds.
  The mist objects are the correct approach for cloud atmosphere.
- **GameObject.Find skips disabled objects** — use `Resources.FindObjectsOfTypeAll<GameObject>()`
  filtered by `go.scene.IsValid()` to find the disabled radial shafts object.
- **Iter020 regression** — caused by square particle artifacts + shaft artifact. Already reverted.
- **Ring scale=1.02 regression** (Iters 013-016) — scale change froze optimization. Now fixed.
- **Mist reduction regression** (Iter013) — reducing Intensity 0.22→0.18 dropped SSIM 0.627→0.598.
  Reference needs RICH atmosphere. Do not reduce mist below 0.22.

## Do NOT Change

- Ring mesh rotation (`localEulerAngles.z = 90`) — verified correct UV direction
- Bloom scatter above 0.60 — causes bloom dome (purple sky artifact)
- Mountain Z value (keep at 0.1) unless also adjusting horizon glow Z
- Low Horizon Glow Z (keep at 0.5 — behind mountains — this creates the dark silhouette)

## Convergence Criteria

- SSIM > 0.98 → task complete
- If 5 consecutive iterations show <0.001 SSIM improvement → try random mutations (simulated annealing)
- If improvement has stalled for 10+ iterations → report to user with diagnostic

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
