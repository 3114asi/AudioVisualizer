# AGENTS.md — Autonomous AI Agent Instructions

This file tells AI agents how to contribute to the NeonPortalScene visual matching task.

## Your Mission

Iterate the Unity NeonPortalScene scene until `ref/current.png` achieves >98% SSIM similarity to `ref/1.png`. Work autonomously — no user questions, no stopping until converged or improvement is impossible.

## Before You Start

1. **Read `AI_CONTEXT.md`** — complete technical context, current state, key constraints
2. **View `ref/1.png`** — the target reference image
3. **View `ref/current.png`** — the current render state
4. **Check `Assets/Editor/VisualMatchTool.cs`** — existing iterations 001–012 show what's been tried

## Iteration Loop

Each iteration must:

```
1. Analyze current.png vs ref/1.png visually
2. Identify the top 2-3 differences
3. Add a new iteration method to VisualMatchTool.cs
4. Run the iteration in Unity batch mode (exit code 0 = success)
5. Capture screenshot
6. Run comparison metrics
7. Record the delta (improvement or regression)
8. If improved: continue to next iteration
9. If regressed: analyze why, add a corrective iteration
```

## Unity Batch Mode Commands

```powershell
# Run iteration NNN
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod Ediskrad.AudioVisualizer.Editor.VisualMatchTool.IterationNNN -logFile -

# Capture screenshot
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod Ediskrad.AudioVisualizer.Editor.VisualMatchTool.CaptureScreenshot -logFile -

# Compare
py tools/compare_quick.py
```

## Adding a New Iteration

Add a `[MenuItem]` method to `Assets/Editor/VisualMatchTool.cs` following the existing pattern (Iteration001–Iteration012). Key helpers available:

- `LoadScene()` — opens NeonPortalScene
- `LoadMat(path)` — loads a material asset
- `RebuildVolumeProfile(...)` — rebuilds post-processing stack
- `SaveAll()` — saves all dirty assets + scene
- `CaptureScreenshot()` — renders to `ref/current.png`

## Tuning Priorities (ordered by SSIM impact)

### High Impact
1. **Ring gradient** — `M_NeonRing.mat` ColorB.R (current=0.45). Target: right BGR ratio R/B≈0.22. Avoid >1.0 (causes all-pink).
2. **Ring size** — scale ring object by 1.03x to match r_norm 0.348
3. **Atmospheric clouds** — bottom atmosphere richness. Mist intensity 0.18-0.25 range.

### Medium Impact  
4. **Mountain silhouettes** — Low Horizon Glow at Z=0.5 illuminates mountains. Keep mountains at Z=0.1, mist at Z≈-0.35.
5. **Bloom settings** — scatter=0.50, threshold=0.55 working well. Avoid scatter >0.60 (dome effect).
6. **Post-exposure** — currently -0.32. Reference appears slightly brighter; try -0.25.

### Lower Impact
7. **Star particles** — Verify particle systems are simulated (`ps.Simulate(4f)`)
8. **Center beam** — Water reflection Width=0.12. Try 0.15.
9. **Vignette** — Currently 0.40. Reference has moderate vignette.

## Do NOT Change

- Ring mesh rotation (`localEulerAngles.z = 90`) — verified correct UV direction
- Bloom scatter above 0.60 — causes bloom dome (the purple sky bug)
- Mountain Z > 0.1 unless also adjusting horizon glow Z

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
  procedural_compare.py         # Extended analysis
AI_CONTEXT.md                   # Technical context (READ THIS FIRST)
AGENTS.md                       # This file
```

## Python Environment

Python 3.13 with: `pip install opencv-python scikit-image numpy`

ffmpeg must be in PATH for video frame extraction.
