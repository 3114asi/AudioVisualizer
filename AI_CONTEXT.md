# AI_CONTEXT.md — NeonPortalScene Visual Matching Project

> **For AI agents**: This file is the canonical source of truth for the Unity NeonPortalScene visual matching task. Read this before making any changes.

## Project Goal

Make the Unity scene `Assets/Scenes/NeonPortalScene.unity` visually match the reference video `ref/1.mp4` (reference still: `ref/1.png`) to >98% SSIM similarity. The task runs as an autonomous AI iteration loop.

## Reference Image Description

The reference (`ref/1.png`) shows:
- **Ring**: Large neon circle, center ~53% down, ~69% wide. LEFT side = electric blue/cyan. RIGHT side = violet/magenta. Top has small magenta accent.
- **Background**: Very dark (near-black) sky with subtle blue-purple atmospheric glow radiating from center
- **Mountains**: Dark jagged silhouettes visible at the bottom, on both sides, against the atmospheric glow
- **Atmosphere**: Purple/blue cloud formations at bottom; concentrated pinkish glow at very bottom center
- **Particles**: Small bright star-like particles scattered in dark sky area
- **Ring metrics**: cx_norm=0.512, cy_norm=0.531, r_norm=0.348, circularity=0.882
- **Ring colors** (BGR): left=[121,1,6] (pure blue), right=[126,0,28] (blue-violet with slight red)

## Technical Stack

| Component | Details |
|-----------|---------|
| Unity | 2022.3.52f1 LTS, Windows |
| Render Pipeline | URP (Universal Render Pipeline) |
| Camera | Orthographic, HDR, size=7.0 |
| Post-processing | ACES Tonemapping + Bloom + ColorAdjustments + Vignette + ChromaticAberration |
| Shader type | `Blend One One` (additive) for neon effects |
| Automation | Unity batch mode: `-batchmode -quit -executeMethod` |
| Metrics | Python + OpenCV + scikit-image SSIM via `tools/compare_quick.py` |

## Current State (Iteration 012, 2026-06-26)

| Metric | Value | Target |
|--------|-------|--------|
| SSIM vs ref | 0.6268 | 0.98 |
| Histogram vs ref | 0.7085 | ~0.95 |
| Best SSIM vs video | 0.6605 | 0.98 |
| Best Hist vs video | 0.8691 | ~0.95 |
| Ring circularity | 0.899 | 0.890 |
| Ring r_norm | 0.345 | 0.348 |

### Iteration History (SSIM progression)
- Iter001-004: initial fixes, SSIM ~0.51
- Iter005: bloom/mist fix, SSIM 0.555
- Iter006: ring rotation Z=90°, SSIM 0.580
- Iter008: ring color balance, SSIM 0.591
- Iter010: atmosphere boost, SSIM 0.621
- Iter011: mountain fix, SSIM 0.602 (regression)
- Iter012: horizon glow behind mountains + ColorB.R=0.45, SSIM 0.627

## Key Files

### Unity Editor Scripts
- `Assets/Editor/VisualMatchTool.cs` — **Main iteration tool**. Contains Iterations 001–012. Run via Unity batch mode.
- `Assets/Editor/NeonPortalProjectBootstrap.cs` — Auto-setup script

### Materials
- `Assets/Materials/M_NeonRing.mat` — Ring material. Current: ColorA=(0,0,6.5), ColorB=(0.45,0,5.5), Intensity=1.28, SegmentContrast=0.18
- `Assets/Materials/M_VioletMist.mat` — Shared mist (8 orbit layers + Low Horizon Glow). Current: Color=(0.22,0,1.0,0.40), Intensity=0.22
- `Assets/Materials/M_WaterReflection.mat` — Bottom center glow. Current: ColorA=(0,1,5), ColorB=(5,0,4), Intensity=0.80, Width=0.12
- `Assets/Materials/VP_NeonPortal.asset` — URP Volume Profile. Bloom(0.68,0.55,0.50), ACES, ColorAdjustments(postExposure=-0.32, contrast=25, sat=14, filter=(0.78,0.84,1.0)), Vignette(0.40), ChromaticAberration(0.04)

### Shaders
- `Assets/Shaders/NeonPortalRing.shader` — Ring shader. UV.x=0 at TOP of mesh (clockwise). Ring rotated Z=+90° in scene so UV.x=0 is at LEFT.
- `Assets/Shaders/NeonMist.shader` — Mist shader. Additive blend. Radial falloff: `pow(saturate(1 - dot(c,c)*4), Softness)`. 8 layers stack additively.

### Scene Objects (key positions)
| Object | Position | Scale | Z | Notes |
|--------|----------|-------|---|-------|
| HDR Energy Ring | (0.12, -0.43, -0.6) | (1,1,1) | -0.6 | Z=90° rotation |
| Blue Violet Background Glow | (0, 0.47, 3) | (10.5,11,1) | 3 | Embedded M_VioletMist Instance |
| Low Horizon Glow | (0, -5.0, 0.5) | (14,4,1) | 0.5 | Shares M_VioletMist.mat, behind mountains |
| Dark Mountain Silhouettes | (0, -6.2, 0.1) | (10,2.5,1) | 0.1 | Opaque black mesh |
| Purple Blue Water Reflection | (0.12, -6.53, -0.2) | (2.45,2.45,1) | -0.2 | M_WaterReflection.mat |

### Render Z-order (front to back, -Z = closest to camera)
Mist layers (Z≈-0.35) → Mountains (Z=0.1) → Low Horizon Glow (Z=0.5) → Background Glow (Z=3)

**Key constraint**: Mountains block Background Glow and Horizon Glow, creating dark silhouettes. Mist renders in front of everything (additive over mountains).

### Tools
- `tools/compare_quick.py` — Comparison script. Usage: `py tools/compare_quick.py` from project root
- `tools/procedural_compare.py` — Extended comparison tool

### Reference Assets
- `ref/1.png` — Target reference still frame
- `ref/2.png` — Second reference frame
- `ref/bg.png` — Background reference
- `ref/1.mp4` — Reference video (not tracked in git, too large)
- `ref/current.png` — Latest render output (updated each iteration)
- `ref/keyframes/frame_0X.png` — 5 keyframes extracted from video

## How to Run an Iteration

```powershell
# 1. Apply iteration changes
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" `
  -batchmode -quit -projectPath . `
  -executeMethod Ediskrad.AudioVisualizer.Editor.VisualMatchTool.Iteration012 `
  -logFile -

# 2. Capture screenshot
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" `
  -batchmode -quit -projectPath . `
  -executeMethod Ediskrad.AudioVisualizer.Editor.VisualMatchTool.CaptureScreenshot `
  -logFile -

# 3. Compare metrics
py tools/compare_quick.py
```

## Critical Technical Knowledge

### Ring Shader UV Convention
- UV.x = 0 at TOP of mesh, increases clockwise
- After Z=+90° rotation: UV.x=0 is at LEFT, UV.x=0.5 is at RIGHT
- ColorA = left/start color, ColorB = right/end color
- Color gradient: `lerp(ColorA, ColorB, smoothstep(0.10, 0.92, UV.x) * 0.9 + verticalBias * 0.22)`

### Bloom Accumulation (Critical)
- 8 mist layers stack additively → risk of bloom dome
- Each layer at Intensity=I contributes B_HDR = Color.B × I × radial_falloff
- Bloom threshold = 0.55. If sum > 0.55 → bloom fires → purple dome at center
- Safe limit: 3 overlapping layers × I × Color.B < 0.55
- Current: 3 × 0.22 × 1.0 = 0.66 → slightly above threshold → moderate bloom (intentional, matches reference glow)

### ACES Tonemapping Effect on Colors
- ACES compresses HDR. High-B values saturate faster than high-R values.
- At HDR B=6.5, R=0: ACES output → B≈240, R≈0 in SDR
- At HDR B=5.5, R=0.45: right-side blend@0.49 → R_SDR≈30 (visible violet tint)
- ColorFilter=(0.78,0.84,1.0) reduces R by 22%, G by 16%, B unchanged

### Mountain Silhouette System
- Mountains (Z=0.1, opaque ZWrite) block Low Horizon Glow (Z=0.5) and Background Glow (Z=3)
- Mist layers (Z≈-0.35) render IN FRONT of mountains (additive, adds haze over peaks)
- For clear silhouettes: Horizon Glow must be bright enough, mist intensity must be moderate

## Remaining Visual Differences (Priority Order)

1. **Ring gradient strength** — right side needs slightly more red/magenta (target R/B ratio = 28/126 ≈ 0.22; current ~0.30 at right)
2. **Ring size** — slightly small (r=0.345 vs ref 0.348 — need +0.9%)  
3. **Mountain silhouette clarity** — mountains visible but atmospheric haze slightly too diffuse
4. **Atmospheric clouds** — bottom purple cloud formations less prominent than reference
5. **Star particles** — small bright particles scattered in dark sky (particles active but may need Simulate())
6. **Center vertical beam** — faint vertical accent visible in reference below ring

## Next Iteration Suggestions

For Iter013:
- Ring scale: slightly increase ring object scale by 1.03x for r_norm 0.345→0.348
- ColorB.R: try 0.50 for slightly more vivid right gradient
- Mist: reduce to 0.18 if bottom haze is too diffuse
- Water reflection Width: 0.14 for more visible bottom beam
- Check if particles are rendering (use `ps.Simulate(4f)` in CaptureScreenshot)
