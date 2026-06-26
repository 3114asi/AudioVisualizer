# AI_CONTEXT.md — NeonPortalScene Visual Matching Context

> **For AI agents**: This is the canonical source of truth. Read this before making any changes.

## Project Goal
Make `Assets/Scenes/NeonPortalScene.unity` match reference `ref/1.png` with SSIM >98%.
Scene is reverted to **Iter012 baseline** (SSIM=0.627) as of 2026-06-26.
Iter019 and Iter020 are in VisualMatchTool.cs but caused regressions — ignore them.
**Next step: implement Iter021** (see bottom of this file).

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
| 012  | **0.627** | **CURRENT BEST** — horizon glow Z=0.5, ring ColorA/B tuned |
| 019  | 0.626 | star dust size: 0.006→0.048 (minimal change) |
| 020  | 0.465 | REGRESSION — reverted. Plasma dust squares + shaft artifact |

---

## Scene Object Map (Iter012 restored state)

| Object | World Pos | Scale | Z | Notes |
|--------|-----------|-------|---|-------|
| HDR Energy Ring | (0.12, -0.43, -0.6) | (1,1,1) | -0.6 | Ring mesh, localEulerAngles.z=90 |
| Blue Violet Background Glow | (0,0.47,3) | (10.5,11,1) | 3 | Far background |
| Low Horizon Glow | (0,-5.0,0.5) | (14,4,1) | 0.5 | BEHIND mountains — correct |
| Dark Mountain Silhouettes | (0,-6.2,0.1) | (10,2.5,1) | 0.1 | Blocks horizon/bg glow |
| Purple Blue Water Reflection | (0.12,-6.53,-0.2) | (2.45,2.45,1) | -0.2 | Center strip |
| Animated Volumetric Mist 00 | (-4.5,-5.08,-0.35) | (2.8,1.2,1) | -0.35 | In front of mountains |
| Animated Volumetric Mist 01 | (-3.21,-4.79,-0.35) | (2.8,1.2,1) | -0.35 | |
| Animated Volumetric Mist 02 | (-1.93,-4.76,-0.35) | (2.8,1.2,1) | -0.35 | |
| Animated Volumetric Mist 05 | (1.93,-5.42,-0.35) | (2.8,1.2,1) | -0.35 | |
| Animated Volumetric Mist 07 | (4.5,-4.85,-0.35) | (2.8,1.2,1) | -0.35 | |
| Magenta Plasma Dust | (0,0.25,-0.18) | (1,1,1) | — | Particle: Donut shape R=7.4 (OFF-SCREEN) |
| Star Dust Field | in scene | — | — | Particle: size=0.048 after Iter019 |
| Random Radial Light Shafts | (0,1.15,-0.25) | (1,1,1) | — | **DISABLED** (m_IsActive=0) |

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

## Materials (Iter012 state)

| Material | Key Properties |
|----------|---------------|
| M_NeonRing.mat | ColorA=(0,0,6.5), ColorB=(0.45,0,5.5), Intensity=1.28, SegContrast=0.18 |
| M_VioletMist.mat | Color=(0.22,0,1.0,0.4), Intensity=0.22, Softness=2 |
| M_RadialRay.mat | Color=(0.15,0.05,1.5,0.15), **Intensity=0 (ZERO!)**, Softness=5 |
| M_WaterReflection.mat | Intensity=0.80, Width=0.14 |

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

### Ring Color Target (from metrics sampling)
- Ref left BGR=[121, 1, 6] → R/B=0.05
- Ref right BGR=[126, 0, 28] → R/B=0.22
- Curr right BGR=[126, 0, 48] → R/B=0.38 (too high)
- **Fix**: ColorB.R: 0.45 → 0.26

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

### Mist Cloud Enhancement
- 8 "Animated Volumetric Mist" objects exist (00–07) at Z=-0.35, scale (2.8, 1.2, 1)
- Current mist is too subtle (clouds barely visible in render)
- Reference requires much more prominent clouds
- Fix: scale up to (5.0, 2.5, 1) and increase Intensity: 0.22 → 0.38
- M_VioletMist._Softness=2 → can lower to 1.5 for wider visible cloud area

---

## Current SSIM Gap Analysis
- Overall SSIM: 0.627 (target: >0.98)
- Ring r_norm: 0.339 (target 0.348 = +2.7% too small)
- Ring circularity: 0.019 (detection failing — thin annulus; ref has 0.890)

**Top remaining differences** (ref/1.png vs ref/current.png):
1. **No visible clouds** — mist objects too small/dim (HIGH IMPACT)
2. **No light rays** — radial shafts disabled + material intensity=0 (HIGH IMPACT)
3. **Ring right side too red** — ColorB.R=0.45 vs target 0.26 (MEDIUM)
4. **Ring slightly too small** — scale to (1.028, 1.028, 1) (LOW)

---

## Iteration 021 Plan (IMPLEMENT THIS NEXT)

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
