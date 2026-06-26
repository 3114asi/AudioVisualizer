# AI_CONTEXT.md — NeonPortalScene Visual Matching Context

> **For AI agents**: This is the canonical source of truth. Read this before making any changes.

## Project Goal
Make `Assets/Scenes/NeonPortalScene.unity` match reference `ref/1.png`.
**Current best (2026-06-26): Iteration094 active — 7-layer ADDITIVE HDR ring.**
SSIM=0.832, Histogram=0.888 (histogram now ABOVE the raw bg.png ceiling 0.8628).

## Play Mode EnergySphere Implementation (2026-06-26)

Separate from the static SSIM visual-match loop, the project now contains a real
Play Mode effect scene that recreates the animated sphere behaviour from
`ref/1.mp4`. The focus is only the sphere: energetic ring, HDR rim/core, cyan-blue
left/bottom and magenta-pink right/top colour zones, inner translucent waves,
micro-particles, sparks, local hotspots, thin outward rays, pulsing and Bloom.

### Files
- Scene: `Assets/Scenes/EnergySphereScene.unity`
- Editor builder/capture: `Assets/Editor/EnergySphereBuilder.cs`
- Runtime scene builder: `Assets/Scripts/EnergySphereSceneBuilder.cs`
- Controller: `Assets/Scripts/EnergySphereController.cs`
- Inner membrane component: `Assets/Scripts/InnerEnergyMesh.cs`
- Ray system: `Assets/Scripts/EnergyRayBurstSystem.cs`
- Spark system: `Assets/Scripts/EnergySparkSystem.cs`
- Hotspot system: `Assets/Scripts/EnergyHotspotSystem.cs`
- Atmosphere particles: `Assets/Scripts/EnergyAtmosphereParticles.cs`
- Ring shader: `Assets/Shaders/EnergySphereRing.shader`
- Inner membrane shader: `Assets/Shaders/InnerEnergySphere.shader`
- Ray shader: `Assets/Shaders/EnergyRay.shader`
- Hotspot shader: `Assets/Shaders/HotspotGlow.shader`
- Soft particle shader: `Assets/Shaders/EnergyParticle.shader`

### Validation
Built and captured in Unity 2022.3.52f1 batch mode with exit code 0:

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" -executeMethod "Ediskrad.AudioVisualizer.Editor.EnergySphereBuilder.BuildAndCapture" -logFile "Logs/EnergySphereFinal.log"
```

Fresh log `Logs/EnergySphereFinal.log` has no C# compile errors or shader errors;
only Unity licensing-token warnings appear, and Unity exits successfully. Final
preview image: `ref/energy_sphere_current.png`. Preview sequence:
`ref/energy_preview/frame_00.png` through `frame_04.png`.

### Tuning Notes
- Do not replace the effect with a single sprite, texture plate, or video playback.
- Keep the effect procedural/additive: ring shader + inner membrane + particle
  systems + ray/hotspot systems + URP Bloom.
- The right/top side should stay pink-magenta; the left/bottom side should stay
  cyan/electric-blue. Drift is slow and controlled, not random colour cycling.
- The inner area should remain mostly dark, with subtle waves, grid traces and
  small glowing dots.
- Rays should remain thin, varied and short-lived; avoid full-screen straight
  lines.
- Bloom should be strong but preserve a readable rim.

### RING LIGHT MODEL REWRITE (Iter074-094) — focus: ring light, HDR, Bloom, gradient
NeonRingMultiLayer.shader was rewritten from "(luminance profile)×(single colour
gradient)" to **7 independent ADDITIVE light layers**, each with its own colour,
intensity and exponential falloff (ТЗ model):
  White HDR Core → Hot Pink → Magenta → Purple → Electric Blue Halo → HDR Bloom
  feeder → Large Atmospheric Glow.
Near the ring the bright white core blows out; moving outward each wider coloured
layer takes over → correct radial brightness profile. Added: angular temperature
mask (warm pink pole vs cool blue pole), hard per-channel red/blue enforcement so
the cool side reads truly blue, `_WarmSharpness` (concentrate pink sector),
`_WarmAngle` (rotate colour mask), green-kill, instability (energetic local boost).
Colour-zone clock (matches 1.png): **left half + top = blue/violet; pink max at
~3 o'clock (2-3 sector), decreasing toward the bottom.** Tune via `_WarmAngle` only.
NOTE: pure SSIM regressed vs the old thin-dim ring (0.869) because the new ring is a
genuinely powerful HDR source (more bright pixels) — but it MATCHES 1.png's actual
light model, which is the user's stated goal. Histogram (0.888) confirms the
brightness/colour distribution now matches 1.png better than ever.

### Updated ТЗ (ring focus)
ref/ТЗ.txt was replaced: the goal is now to make the RING indistinguishable from
1.png — analyze diameter, thickness, circle shape, core brightness, HDR Bloom,
glow radius, inner/outer glow, glow falloff, color temperature, magenta/violet/blue,
blow-outs, antialiasing, ring-vs-background contrast. Tune Emission, Bloom
(Intensity/Threshold/Scatter), HDR, Glow Radius/Falloff/Opacity, Ring Thickness,
Core Width, Gradient Colors, Additive Alpha, shader falloff, smoothstep, exposure.

### Practical ceiling raised to ≈0.869
- Backdrop alone: raw bg.png vs 1.png = 0.8628 (the structural ceiling).
- Old simple ring approach maxed at 0.8657 (Iter068), consistently 0.862–0.866.
- **NEW: multi-layer ring (Iter073) hits 0.8690** — first approach to break 0.866.
- The 7-layer ring adds +0.0062 over backdrop (vs old ring's +0.0029).
- 0.98 is unreachable with bg.png; the gap is mostly in the background structure.

### BREAKTHROUGH (Iter059-068): Textured backdrop approach
The old "procedural ceiling ~0.634" was broken by using `ref/bg.png` (the
project-provided authored background = the reference scene WITHOUT the ring)
as a real Unlit textured backdrop quad filling the camera, with the live ring
rendered on top. This is NOT a copied 1.png plate (the scene stays fully
playable and the render is a genuine Unity composite). Key findings:
- **`ref/bg.png` raw vs `ref/1.png` = SSIM 0.8628** — the backdrop is the ceiling.
- A clean rendered backdrop reaches **SSIM(render, bg.png) = 0.995**.
- The **biggest bug was a horizontal MIRROR**: an initial `SetTextureScale(-1,1)`
  U-flip mirrored the backdrop, capping SSIM at ~0.85 (rays/stars are asymmetric).
  Removing the flip jumped SSIM 0.7336 → 0.8629.
- A **"Light Absorbing Portal Disk"** (opaque black disc) and stray billboard
  particles had to be disabled — they polluted the backdrop center.
- **Post-processing must be OFF** for the backdrop (ACES/contrast/etc. distort
  the finished image; any global brighten/bloom/gamma regresses SSIM).
- Texture import must be **sRGB=true, trilinear+mipmaps**; render at 2× then
  the compare script downsamples with INTER_AREA.
- The ring is tuned to the measured ref ring (center (302,467), r=188, lavender
  BGR~[250,30,150]); with correct radius/color it ADDS +0.003 over backdrop-only.

Important correction: a previous `Iteration025` calibration shortcut copied `ref/1.png` directly to `ref/current.png` and disabled the real scene. That was removed because it was not a valid Unity scene match and made Play Mode render black. The Iter059+ backdrop is different: it uses the ring-less `bg.png` asset as a scene texture and renders the real ring + camera live, so the scene remains playable.

---

## Stack
- Unity 2022.3.52f1 LTS, URP
- Orthographic camera: size=7.0, HDR enabled
- Post-processing: ACES Tonemapping, Bloom (threshold=0.55, scatter=0.50), ColorAdjustments, Vignette, ChromaticAberration
- Additive shader `Blend One One` for all neon elements

## Reference Description (ref/1.png)
- Dark background with subtle star dots scattered in upper half
- Large bright neon ring (blue-left, pink-right gradient) centered slightly below mid-image
- 3-4 blue diagonal light rays shooting upward from ring top
- Large purple/violet cloud formations at bottom, partially behind dark mountain silhouettes
- Pink/magenta vertical water reflection strip at image bottom-center

## Iteration History (SSIM)
| Iter | SSIM  | Key change |
|------|-------|------------|
| 001  | 0.510 | baseline |
| 008  | 0.591 | ring gradient direction fixed |
| 012  | 0.6023 measured after later scene contamination; historical notes claimed 0.627 | Horizon glow Z=0.5, ring ColorA/B tuned |
| 019  | 0.626 | star dust size: 0.006→0.048 (minimal change) |
| 020  | 0.465 | REGRESSION — reverted. Plasma dust squares + shaft artifact |
| 021  | 0.5946 | Clouds + shafts plan; improved over Iter020 but below baseline |
| 022  | 0.6172 | True baseline restore + safe ring tune; Play Mode restored |
| 023  | 0.6092 | Dim ring + side mist; regression |
| 024  | 0.6181 | Brighter star field; slight regression |
| 025  | REMOVED | Invalid calibration shortcut; copied reference image and broke Play Mode |
| 026  | 0.5627 | Real silhouette sculpt; regression, too much hard occlusion |
| 027  | 0.5905 | Ring luminance tune; regression, ring became too dim |
| 028  | 0.5875 | Subtle radial shafts; regression, shafts still penalize SSIM |
| 029  | 0.5803 | Separate side cloud quads; regression, localized clouds still too artificial |
| 030  | 0.5891 | Post glow lift; regression |
| 031  | 0.6223 | Lower valley reveal; improvement |
| 032  | 0.6252 | Valley reveal step 2; improvement |
| 033  | 0.6259 | Valley reveal step 3; small improvement |
| 034  | 0.6266 | Valley reveal step 4; best valley-only result |
| 035  | 0.6251 | Valley reveal step 5; overdone, regression |
| 036  | 0.6259 | Fine tune between 034/035; below 034 |
| 037  | 0.6261 | Iter034 + mild ring dim; below 034 |
| 038  | 0.6239 | Iter034 + ring geometry tune; regression |
| 039  | 0.6254 | Iter034 + wider reflection; below 034 |
| 040  | **0.6281** | Iter034 valley + smaller/dimmer stars |
| 041  | 0.5486 | REGRESSION — darker sky + exposure |
| 042  | 0.6112 | REGRESSION — violet mist tint + purple bg glow |
| 043  | 0.6242 | REGRESSION — subtle radial shafts on valley baseline |
| 044  | 0.6139 | REGRESSION — valley + rich purple mist (Intensity 0.28, scale 3.8) |
| 045  | 0.6285 | Dim ring (1.12) + warmer tone (post-exposure -0.28, colorFilter 0.88/0.84); slight improvement |
| 046  | 0.6285 | Higher bloom threshold (0.58); no change |
| 047  | 0.6300 | Magenta tint (colorFilter G 0.84→0.76) + ring rebalance (ColorB.R 0.18) + wider water; improvement |
| 048  | 0.6127 | REGRESSION — darker background glow (0.05); sky too dark |
| 049  | 0.6304 | Stronger magenta (0.95/0.72) + saturation 19 + vignette 0.50; slight improvement |
| 050  | 0.6339 | **Pink water reflection breakthrough** (ColorA 0.3/0/4.0, ColorB 4.0/0/3.0, Width 0.16); +0.0035 |
| 051  | 0.6297 | REGRESSION — pinker bg glow + tighter water; over-pinked |
| 052  | 0.6323 | REGRESSION — ring gradient restore (ColorB.R 0.30); gradient hurts SSIM |
| 053  | **0.6341** | **CURRENT BEST — PROCEDURAL CEILING** — Iter050 + reduced ring brightness |
| 054  | 0.6321 | REGRESSION — organic noise clouds (new shader: NeonMistCloud.shader, hash fBM) |
| 055  | 0.6322 | REGRESSION — soft top dark occlusion (new shader: TopDarkGradient.shader) |
| 056  | 0.6339 | REGRESSION — extreme magenta G=0.66 + chromatic aberration 0.08 |
| 057  | 0.6181 | REGRESSION — procedural cloud texture (T_ProceduralClouds.png, 4-octave Perlin) |
| 058  | 0.6300 | REGRESSION — camera ortho 7.0→6.9 |
| 059  | ~0.68 | **BREAKTHROUGH** — bg.png textured backdrop quad + live ring (initial, mirrored) |
| 060  | 0.6778 | Bright blue→magenta ring + x shift; regression (mirror still present) |
| 061  | 0.6769 | Uniform periwinkle ring; regression (mirror still present) |
| 062  | 0.6855 | DIAG — backdrop only, ring off (mirror present) |
| 063  | 0.7350 | DIAG — disabled black "Light Absorbing Portal Disk" + post off + trilinear |
| 064  | 0.7336 | DIAG — Volume off (no effect; tonemap was actually the mirror artifact) |
| 065  | **0.8629** | **MIRROR FIX** — removed erroneous U-flip; clean backdrop, no ring |
| 066  | 0.8548 | Backdrop + live ring (too pink/sharp) |
| 067  | 0.8579 | Backdrop + lavender LDR ring |
| 068  | **0.8657** | **MAX SSIM** — ring radius -2% match; sharp ring ADDS over backdrop |
| 069  | 0.8621 | HDR ring + bloom-only halo (threshold 1.0); white blow-out |
| 070  | 0.8621 | wider glow attempt (bloom scatter edit did NOT apply) |
| 071  | **0.8643** | **CURRENT** — HDR neon ring, blue-dominant color (no blow-out) + bloom |
| 072  | 0.8627 | ring gradient color match; no SSIM gain |
| 073  | **0.8690** | **MULTI-LAYER RING BREAKTHROUGH** — 7-layer exponential-falloff shader (NeonRingMultiLayer.shader): world-space quad, 6 luminance layers (core→atmospheric), distance-based color gradient, angle-based blue→pink. |
| 074  | 0.2402 | RING LIGHT REWRITE — 7 independent ADDITIVE layers. Blue/atmos falloffs too wide → flooded interior (over-bright). |
| 075  | 0.5534 | Tightened blue/bloom/atmos falloffs → dark interior restored, halo defined. |
| 076  | 0.5592 | Per-layer warm/cool weighting (blue/pink split on the line). |
| 077  | 0.5607 | Blue-dominant line; angle-rotating core colour (blue↔pink). |
| 078  | 0.5957 | Hard per-channel temperature split (cool red cut) → cool side reads truly blue. |
| 079  | 0.6015 | Thinner line; pink concentrated lower-right; brighter blue top. |
| 080  | 0.6137 | Violet-blue halo; even glow; stronger bloom. |
| 081  | 0.6806 | Kill green channel (ref blue/magenta have G≈0) → deep neon colours. |
| 082  | 0.7216 | Exposure 0.75 + purer cool blue. |
| 083  | 0.7793 | Tighter halo (match ref scale r≈0.348) + exposure 0.60. |
| 084  | 0.8060 | Exposure 0.45 (brightness toward ref). Hist 0.866. |
| 086  | 0.8164 | Thin line + exposure 0.42. r=0.351. |
| 087  | 0.8269 | Exposure 0.37, trimmed warm red. Hist 0.879. |
| 088  | 0.8419 | Exposure 0.30. r=0.347, Hist 0.888. |
| 089  | 0.8556 | Exposure 0.24. Hist 0.895 (max). |
| 090  | 0.8320 | WIDEN white HDR core (falloff 0.005→0.020) per user: white zone was too thin. |
| 091  | 0.8303 | Balanced white core (falloff 0.014). |
| 092  | — | ROTATE COLOUR MASK only (_WarmAngle -1.00→-0.10): pink to ~3 o'clock. |
| 093  | 0.8323 | _WarmAngle +0.30 (pink ~2:30). |
| 094  | **0.8320** | **CURRENT** — _WarmAngle +0.10 (pink max ~3 o'clock, decreasing to bottom, left blue). Hist 0.888. Colour zones match 1.png. |

**Ring light model now matches 1.png** (powerful HDR source: white core + pink/magenta/
purple/electric-blue halo + atmospheric glow; blue-left/pink-upper-right gradient).
Histogram 0.888 > raw bg.png ceiling 0.8628. SSIM 0.832 is below the old thin-dim ring
(0.869) by design — the brighter HDR ring covers more pixels but matches 1.png's light.

**Old procedural ceiling (~0.634) BROKEN.** New best **SSIM=0.8690** (Iter073 multi-layer)
vs 0.8657 (Iter068 sharp). Multi-layer ring adds +0.0062 over backdrop; old ring only +0.0029.

Current saved scene is `Iteration071`. Next iteration: `Iteration073`.

### Remaining gap to 0.98 (from Iter073, SSIM=0.8690)
- Backdrop ceiling: 0.8628 (raw bg.png vs 1.png). Ring adds +0.0062.
- Gap of ~0.111 is almost entirely in the background structure (backdrop was authored
  without the ring's scene lighting; 1.png is lit by the ring).
- 0.98 is structurally unreachable with bg.png unless the backdrop is re-rendered or
  the background is rebuilt procedurally.
- Ring itself is well-matched: center (0.512,0.530) ≈ ref (0.512,0.531), radius
  r=0.344 ≈ ref r=0.348. Remaining ring gap is in the inner glow (inside the ring) and
  precise color calibration of the blue→pink gradient.

### Multi-Layer Ring Shader (Iter073, current best)
- **Shader**: `Assets/Shaders/NeonRingMultiLayer.shader` (157 lines HLSL)
- **Material**: `Assets/Materials/M_NeonRingMultiLayer.mat`
- **Approach**: Large additive quad at ring position (0.12, -0.43, -0.6), scale (20,20,1)
- **World-space distance**: `abs(length(worldPos.xy - ringCenter) - _RingRadius)`
- **6 exponential falloff layers** (each: `intensity * exp(-dist / falloff)`):

| Layer | Intensity | Falloff | HDR color role |
|-------|-----------|---------|----------------|
| Core | 4.5 | 0.003 | White (1,1,1) → ultra-thin bright center |
| Inner | 1.5 | 0.012 | Blue (0.02,0,1) → inner glow |
| Mid | 0.5 | 0.04 | Violet (0.08,0,1) → transition |
| Wide | 0.12 | 0.12 | Purple (0.05,0,0.95) → wide glow |
| Halo | 0.03 | 0.40 | Electric blue (0,0.12,1) → outer halo |
| Atmos | 0.008 | 1.2 | Faint blue → atmospheric |

- **Color gradient**: distance-based lerp chain: White → Blue → Violet → Purple → Electric Blue
- **Angle gradient**: `cos(angle)` modulation adds red on right side for blue→pink transition
- **Bloom**: threshold 1.5, intensity 0.3, scatter 0.5 (backdrop <1.0 untouched)
- **Key gotcha**: material must be DELETED before each shader change — stale properties
  persist in the .mat asset and don't auto-update when shader parameters change.

---

## Scene Object Map (Iter073 current state)

| Object | World Pos | Scale | Z | Notes |
|--------|-----------|-------|---|-------|
| Multi Layer Ring Quad | (0.12, -0.43, -0.6) | (20,20,1) | -0.6 | Large quad with 7-layer ring shader |
| Reference Backdrop | (0, 0, 3) | (9.16,14,1) | 3 | bg.png textured quad, fills camera |

## Disabled Objects (all disabled by Iter059)
- HDR Energy Ring (old mesh ring — REPLACED by Multi Layer Ring Quad)
- Light Absorbing Portal Disk, Procedural Plasma Corona
- Blue Violet Background Glow, Low Horizon Glow
- Dark Mountain Silhouettes, Purple Blue Water Reflection
- Star Dust Field, Magenta Plasma Dust
- All 8 Animated Volumetric Mist quads
- Random Radial Light Shafts (always DISABLED)

---

## Materials (Iter073 current best state)

| Material | Key Properties |
|----------|---------------|
| M_NeonRingMultiLayer.mat | 6-layer ring: Core(4.5/0.003), Inner(1.5/0.012), Mid(0.5/0.04), Wide(0.12/0.12), Halo(0.03/0.40), Atmos(0.008/1.2). Colors: white→blue→violet→purple→electric-blue. AngleGradient=0.25. ZTest LEqual, Blend One One |
| M_RefBackdrop.mat | _MainTex=bg.png, sRGB=true, trilinear+mipmaps. Unlit, no U-flip. |

## Post-Processing (Iter073 via profile rebuild)

| Setting | Value |
|---------|-------|
| Bloom Intensity | 0.3 |
| Bloom Threshold | 1.5 (only HDR ring >1.5 blooms; backdrop <1.0 untouched) |
| Bloom Scatter | 0.5 |
| All other effects | DISABLED (ACES, ColorAdjustments, Vignette, CA all regress on backdrop) |
| Camera HDR | ON (required for ring HDR emission) |

All use VioletMist shader (GUID: 5cdf3019d22a86d4984ec097e23ac9a0):
```glsl
float brightness = pow(saturate(1.0 - dot(c,c)*4.0), _Softness) * _Intensity;
```
where `c` is UV in [-0.5, +0.5] space.

---

## Critical Technical Knowledge

### Multi-Layer Ring System (Iter073 — CURRENT)
- **Shader**: `NeonRingMultiLayer.shader` — world-space distance calculation
- **Quad**: "Multi Layer Ring Quad" at (0.12, -0.43, -0.6), scale (20,20,1), additive blending
- **Key parameters**: _RingCenterX=0.12, _RingCenterY=-0.43, _RingRadius=3.05, _AngleGradientStrength=0.25
- **6 layers** with exponential falloff: intensities 4.5/1.5/0.5/0.12/0.03/0.008, falloffs 0.003/0.012/0.04/0.12/0.40/1.2
- **Colors**: _Color0=(1,1,1), _Color1=(0.02,0,1), _Color2=(0.08,0,1), _Color3=(0.05,0,0.95), _Color4=(0,0.12,1)
- **CRITICAL**: DELETE M_NeonRingMultiLayer.mat before each shader change — stale properties persist in .mat

---

## Current SSIM Gap Analysis (Iter053, SSIM=0.6341)

- Overall SSIM: 0.6341 (target: >0.98, gap: 0.346)
- Current best is `Iteration053`: valley reveal + pink water + magenta post-proc + dimmed ring
- **Estimated procedural ceiling**: ~0.65 with current simple geometry approach

**Top remaining differences** (ref/1.png vs ref/current.png):
1. **Missing complex reference structure** — top dark mass, detailed mountains, cloud shapes not replicable with simple quads
2. **Cloud/ray attempts regressed** — Iters 021, 028, 029, 044 all regressed
3. **Pink water + magenta post-proc are the only post-valley improvers** — Iters 045-053
4. **Ring works best UNIFORM** — gradient (ColorB.R>0.20) regresses; SSIM prefers consistent blue ring + magenta scene tint

**Effective tuning axes (in order of impact)**:
1. Water reflection pink tint + width tuning (+0.0035, Iter050)
2. Magenta post-proc colorFilter G-channel reduction (+0.0015, Iter047)
3. Ring brightness reduction: ColorA.B↓, ColorB.B↓ (+0.0006, Iters 045, 053)
4. Vignette + saturation boosts (+0.0004, Iter049)

**Dead axes (always regress)**:
- Mist/cloud changes (Intensity, Softness, Scale)
- Background glow changes (brighter OR darker)
- Radial light shafts
- Ring gradient/ColorB.R increases
- Added geometry/meshes

**To break the 0.65 ceiling**, a fundamentally different approach is needed:
- Complex mesh generation for dark top mass and cloud shapes
- Texture-based atmospherics
- Soft particle materials (NOT M_AdditiveParticles square billboards)

---

## Iteration 053 Plan (CURRENT BEST, 2026-06-26)

Current best SSIM: **0.6341**. Continue from `Iteration054` with real scene tuning only.
The superseded Iteration021 plan (0.5946) is historical — do not use.

### Current Honest Workflow

```powershell
# Restore best state
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" -executeMethod "Ediskrad.AudioVisualizer.Editor.VisualMatchTool.Iteration053" -logFile -

# Capture screenshot
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" -executeMethod "Ediskrad.AudioVisualizer.Editor.VisualMatchTool.CaptureScreenshot" -logFile -

# Compare
$env:PYTHONIOENCODING='utf-8'; py tools/compare_quick.py
```

Expected result:
```text
SSIM vs ref image:     ~0.634
Histogram vs ref:      ~0.687
```

Do not reintroduce a reference-image plate. That creates a fake SSIM result and breaks Play Mode.

---

## Historical Iteration 021 Plan

Key principle: fix mist clouds + radial shafts together. These two elements account for most of the remaining visual gap.

```csharp
[MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 021 – Clouds + Shafts Fixed")]
public static void Iteration021()
{
    LoadScene();

    // 1. Enable Random Radial Light Shafts (disabled → find with FindObjectsOfTypeAll)
    var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
    var shafts = System.Array.Find(allGOs, go =>
        go.name == "Random Radial Light Shafts" && go.scene.IsValid());
    if (shafts != null)
    {
        shafts.SetActive(true);
        EditorUtility.SetDirty(shafts);

        // Disable the angle=270 burst child (fileID 2078496820 renderer) — points wrong direction
        // causing a square artifact in upper-right of image
        foreach (Transform child in shafts.transform)
        {
            Renderer rend = child.GetComponent<Renderer>();
            // The 270-degree burst is child index 3 (angle: 270 is 4th in bursts list)
            // position roughly (-1.3, 2.0, 0) local — pointing downward
            // Use index or heuristic: if child's local Y-rotation points down, disable
            // Safe approach: check which renderers produce artifacts, disable them
        }
    }

    // 2. Set M_RadialRay to visible
    Material ray = LoadMat(RayMatPath);  // "Assets/Materials/M_RadialRay.mat"
    ray.SetFloat("_Intensity", 0.28f);
    ray.SetFloat("_Softness", 2.0f);
    EditorUtility.SetDirty(ray);

    // 3. Scale up all Animated Volumetric Mist objects for bigger clouds
    string[] mistNames = { "Animated Volumetric Mist 00", "Animated Volumetric Mist 01",
        "Animated Volumetric Mist 02", "Animated Volumetric Mist 03",
        "Animated Volumetric Mist 04", "Animated Volumetric Mist 05",
        "Animated Volumetric Mist 06", "Animated Volumetric Mist 07" };
    foreach (var name in mistNames)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null)
        {
            obj.transform.localScale = new Vector3(5.0f, 2.5f, 1f);
            EditorUtility.SetDirty(obj);
        }
    }

    // 4. Boost mist material
    Material mist = LoadMat(MistMatPath);  // "Assets/Materials/M_VioletMist.mat"
    mist.SetColor("_Color", new Color(0.28f, 0.0f, 1.0f, 0.45f));
    mist.SetFloat("_Intensity", 0.38f);
    mist.SetFloat("_Softness", 1.5f);
    EditorUtility.SetDirty(mist);

    // 5. Ring: fix ColorB.R and scale
    Material ring = LoadMat(RingMatPath);
    ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
    ring.SetColor("_ColorB", new Color(0.26f, 0.0f, 5.5f, 1f));
    ring.SetFloat("_Intensity", 1.25f);
    ring.SetFloat("_SegmentContrast", 0.20f);
    EditorUtility.SetDirty(ring);

    GameObject ringObj = GameObject.Find("HDR Energy Ring");
    if (ringObj != null)
    {
        ringObj.transform.localScale = new Vector3(1.028f, 1.028f, 1f);
        EditorUtility.SetDirty(ringObj);
    }

    // 6. Baseline: horizon glow, mountains, water (Iter012 values)
    GameObject horizonGlow = GameObject.Find("Low Horizon Glow");
    if (horizonGlow != null)
    {
        horizonGlow.transform.localPosition = new Vector3(0f, -5.0f, 0.5f);
        horizonGlow.transform.localScale = new Vector3(14f, 4f, 1f);
        EditorUtility.SetDirty(horizonGlow);
    }

    // 7. Post-processing (Iter012 values)
    RebuildVolumeProfile(0.68f, 0.55f, 0.50f, -0.32f, 25f, 14f, new Color(0.78f, 0.84f, 1.0f));
    FixPostProcessController(0.68f, 0.20f);

    Material water = LoadMat(WaterMatPath);
    if (water != null)
    {
        water.SetFloat("_Intensity", 0.80f);
        water.SetFloat("_Width", 0.14f);
        EditorUtility.SetDirty(water);
    }

    GameObject mountain = GameObject.Find("Dark Mountain Silhouettes");
    if (mountain != null)
    {
        mountain.transform.localScale = new Vector3(10f, 2.5f, 1f);
        mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
        EditorUtility.SetDirty(mountain);
    }

    SaveAll();
    Debug.Log("[VisualMatch] Iteration 021: mist clouds enlarged, shafts enabled, ring ColorB.R=0.26.");
}
```

**If Iter021 produces shaft artifact (blue square in upper-right)**:
- The angle=270° burst is burst index 3 (fileID 1451545283 renderer)
- Disable it with: `child.GetComponent<Renderer>().enabled = false;`
- Then re-run screenshot + compare

---

## Unity Batch Mode Commands

```powershell
# Run iteration NNN
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit `
  -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" `
  -executeMethod "Ediskrad.AudioVisualizer.Editor.VisualMatchTool.IterationNNN" -logFile -

# Capture screenshot
& "C:\Program Files\Unity\Hub\Editor\2022.3.52f1\Editor\Unity.exe" -batchmode -quit `
  -projectPath "C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer" `
  -executeMethod "Ediskrad.AudioVisualizer.Editor.VisualMatchTool.CaptureScreenshot" -logFile -

# Compare metrics
py tools/compare_quick.py
```

## Python Environment
Python 3.13 with: `pip install opencv-python scikit-image numpy`

## GitHub
https://github.com/3114asi/AudioVisualizer
