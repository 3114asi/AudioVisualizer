# AI_CONTEXT.md — NeonPortalScene Visual Matching Context

> **For AI agents**: This is the canonical source of truth. Read this before making any changes.

## Project Goal
Make `Assets/Scenes/NeonPortalScene.unity` match reference `ref/1.png` with SSIM >98%.
**Current best (2026-06-26): Iteration071 active, SSIM=0.8643 (HDR neon ring + bloom).**
Max SSIM variant is Iteration068 (0.8657, sharp ring no bloom); Iter071 is preferred
because the updated ТЗ requires HDR Bloom/glow on the ring (см. ring focus below).

### Updated ТЗ (ring focus)
ref/ТЗ.txt was replaced: the goal is now to make the RING indistinguishable from
1.png — analyze diameter, thickness, circle shape, core brightness, HDR Bloom,
glow radius, inner/outer glow, glow falloff, color temperature, magenta/violet/blue,
blow-outs, antialiasing, ring-vs-background contrast. Tune Emission, Bloom
(Intensity/Threshold/Scatter), HDR, Glow Radius/Falloff/Opacity, Ring Thickness,
Core Width, Gradient Colors, Additive Alpha, shader falloff, smoothstep, exposure.

### Practical ceiling of this method ≈ 0.866
- Backdrop alone: raw bg.png vs 1.png = 0.8628 (the structural ceiling — bg.png's
  background differs from 1.png, which is lit by the ring).
- Ring variations all land 0.862–0.866 (±0.002 = "imperceptible" per ТЗ stop rule).
- To exceed ~0.866 you'd need the ring's INNER glow (ref radial r170-185 ~55, ours
  ~4) — bloom pushes outward, not inward. URP bloom scatter/threshold edits did NOT
  take effect after the first build (cause unconfirmed; PortalPulseController is NOT
  the culprit — its Update only runs in Play mode). 0.98 is unreachable with bg.png.

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

**Old procedural ceiling (~0.634) BROKEN.** New best **SSIM=0.8657** (Iter068 sharp) /
0.8643 (Iter071 neon glow, preferred per ТЗ). Method ceiling ≈ 0.866.

Current saved scene is `Iteration071`. Next iteration: `Iteration073`.

### Remaining gap to 0.98 (from Iter068, SSIM=0.8657)
- Backdrop is essentially maxed (raw bg.png vs 1.png = 0.8628; the live ring
  lifts it slightly above that).
- The ref ring has a soft bloom HALO + a blue→pink gradient our sharp LDR ring
  lacks. A *targeted* bloom (HDR ring + high threshold so only the ring blooms,
  backdrop untouched) is the most promising next axis, but risks regressing the
  backdrop — tune carefully.
- Global post-processing on the backdrop always regresses; keep it OFF.

---

## Scene Object Map (Iter053 current state)

| Object | World Pos | Scale | Z | Notes |
|--------|-----------|-------|---|-------|
| HDR Energy Ring | (0.12, -0.43, -0.6) | (1.028,1.028,1) | -0.6 | Ring mesh, localEulerAngles.z=90 |
| Blue Violet Background Glow | (0,0.47,3) | (10.5,11,1) | 3 | Far background |
| Low Horizon Glow | (0,-5.42,0.5) | (14,4.85,1) | 0.5 | BEHIND mountains — valley-reveal position |
| Dark Mountain Silhouettes | (0,-6.72,0.1) | (10,1.55,1) | 0.1 | Valley reveal: lower mountains expose horizon |
| Purple Blue Water Reflection | (0.12,-6.53,-0.2) | (2.45,2.45,1) | -0.2 | Pink-tinted center strip (Iter050) |
| Animated Volumetric Mist 00-07 | various | (2.8,1.2,1) | -0.35 | **DO NOT ENLARGE** — regresses |
| Magenta Plasma Dust | (0,0.25,-0.18) | (1,1,1) | — | OFF-SCREEN Donut R=7.4 |
| Star Dust Field | (0,1.7,-0.45) | — | — | size (0.012,0.030) dimmed colors |
| Random Radial Light Shafts | (0,1.15,-0.25) | (1,1,1) | — | **DISABLED** — always regresses |

## Z-Order (back to front)
```
Z=3    → Background Glow
Z=0.5  → Low Horizon Glow (BEHIND mountains)
Z=0.1  → Mountain Silhouettes
Z=-0.2 → Water Reflection
Z=-0.35→ Mist quads (IN FRONT of mountains — creates cloud atmosphere)
Z=-0.6 → Ring (frontmost)
```

---

## Materials (Iter053 current best state)

| Material | Key Properties |
|----------|---------------|
| M_NeonRing.mat | ColorA=(0,0,5.2), ColorB=(0.18,0,4.5), Intensity=1.15, SegContrast=0.20 |
| M_VioletMist.mat | Color=(0.22,0,1.0,0.4), Intensity=0.22, Softness=2 — DON'T CHANGE |
| M_RadialRay.mat | Color=(0.15,0.05,1.5,0.15), Intensity=0 (ZERO!), Softness=5 — DON'T ENABLE |
| M_WaterReflection.mat | ColorA=(0.3,0,4.0), ColorB=(4.0,0,3.0), Intensity=1.15, Width=0.16 — PINK TINT |
| M_MountainSilhouette.mat | Color=(0.005,0.006,0.025,1) — near black |

## Post-Processing (Iter053 via RebuildVolumeProfile)

| Setting | Value |
|---------|-------|
| Bloom Intensity | 0.68 |
| Bloom Threshold | 0.55 |
| Bloom Scatter | 0.50 |
| Post-Exposure | -0.28 |
| Contrast | 24 |
| Saturation | 19 |
| ColorFilter | (0.95, 0.72, 1.0) — magenta tint |
| Vignette Intensity | 0.50 |
| Vignette Smoothness | 0.65 |
| Chromatic Aberration | 0.04 |

All use VioletMist shader (GUID: 5cdf3019d22a86d4984ec097e23ac9a0):
```glsl
float brightness = pow(saturate(1.0 - dot(c,c)*4.0), _Softness) * _Intensity;
```
where `c` is UV in [-0.5, +0.5] space.

---

## Critical Technical Knowledge

### Ring UV Convention
- Ring mesh uses Z rotation +90° (localEulerAngles.z = 90)
- UV.x=0 is at TOP of mesh; with Z=+90° rotation, UV.x=0 maps to LEFT of screen
- ColorA = LEFT = pure blue; ColorB = RIGHT = violet/pink

### Ring Color Target (from Iter053 metrics)
- Ref left BGR=[121, 1, 6] → R/B=0.05 (pure blue)
- Ref right BGR=[126, 0, 28] → R/B=0.22 (blue-pink)
- Curr left BGR=[127, 0, 21] → R/B=0.17 (too pink — magenta post-proc bleed)
- Curr right BGR=[131, 0, 21] → R/B=0.16 (not pink enough)
- **Key insight**: uniform ring (ColorB.R≤0.18) scores HIGHER SSIM than gradient ring. Magenta post-proc provides scene-level pink, and SSIM prefers consistent ring color. DO NOT increase ColorB.R above 0.20.

### Bloom Safety
- Threshold=0.55: multiple additive mist quads easily exceed it → more clouds = more bloom = good
- Scatter: do NOT exceed 0.60 (causes purple dome artifact)

### Mountain Silhouette System
- Mountains Z=0.1 block horizon glow (Z=0.5) and background glow (Z=3) → dark silhouette
- Mist Z=-0.35 is in front of mountains → mist glow appears on top of mountain shapes

### Particle Systems — IMPORTANT
- `CaptureScreenshot` calls `ps.Simulate(4f, true, true)` for ALL systems
- **Magenta Plasma Dust**: Donut emitter, radius=7.4 world units. Camera half-width=4.58 → particles spawn OFF-SCREEN. Speed=1.08-3.1 → travel 4.3-12.4 units in 4s → completely invisible. **DO NOT try to use as clouds** — it renders as solid square quads (no circular softness) via M_AdditiveParticles material
- **Star Dust Field**: size 0.048 (3 pixels). Visible as faint dots at current setting.

### Random Radial Light Shafts — CRITICAL DISCOVERY
- Object disabled in scene (`m_IsActive=0`)
- Use `Resources.FindObjectsOfTypeAll<GameObject>()` to find disabled objects — NOT `GameObject.Find`
- 6 burst children (shaft quads) at angles: 90°, 125°, 55°, **270°**, 160°, 25°
- Each quad: scale (0.42, 5.4, 1) = narrow elongated beam (26px wide × 340px tall at camera resolution)
- Material: M_RadialRay.mat, **_Intensity=0** → must set > 0 to see shafts
- **Angle=270° burst** points DOWNWARD and may render outside camera frustum creating a square artifact in upper-right of image. Investigate and potentially disable that renderer.
- Parent position: (0, 1.15, -0.25) — near ring top

### Mist Cloud Enhancement — DO NOT ATTEMPT
- 8 "Animated Volumetric Mist" objects at Z=-0.35, scale (2.8, 1.2, 1), Intensity=0.22, Softness=2
- **Any mist enlargement or intensity change ALWAYS regresses SSIM** (tested in Iters 013, 021, 023, 044)
- Reference clouds require complex shapes that simple circular gradient quads cannot produce
- Mist at 0.22/2.0 is the sweet spot — reducing (Iter013: 0.627→0.598) or increasing (Iter044: 0.628→0.614) both regress

### Water Reflection — MAJOR IMPROVEMENT AXIS
- Pink-tinted water (ColorA.B=4.0, ColorB.R=4.0) at Width=0.16, Intensity=1.15 gave +0.0035 SSIM (Iter050)
- This is the single biggest post-valley improvement
- Water should be pink/magenta, NOT cyan/blue
- DO NOT make it too tight (Width 0.14 regressed) or too pink (Iter051 regressed)

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
