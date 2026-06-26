#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Ediskrad.AudioVisualizer.Editor
{
    public static class VisualMatchTool
    {
        private const string ScenePath = "Assets/Scenes/NeonPortalScene.unity";
        private const string ProfilePath = "Assets/Materials/VP_NeonPortal.asset";
        private const string RingMatPath = "Assets/Materials/M_NeonRing.mat";
        private const string CoronaMatPath = "Assets/Materials/M_PlasmaCorona.mat";
        private const string MistMatPath = "Assets/Materials/M_VioletMist.mat";
        private const string RayMatPath = "Assets/Materials/M_RadialRay.mat";
        private const string WaterMatPath = "Assets/Materials/M_WaterReflection.mat";

        // ────────────────────────────────────────────────────────────────
        // Iteration 001 – disable spiky corona, clean ring, fix bloom
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 001 – Kill Corona + Bloom Fix")]
        public static void Iteration001()
        {
            LoadScene();
            DisableCoronaObject();
            FixRingMaterial();
            FixCoronaMaterial(); // keep material clean for future re-enable
            RebuildVolumeProfile(1.85f, 0.50f, 0.78f);
            FixPostProcessController(1.85f, 0.45f);
            FixBackgroundGlow(0.13f);
            FixMistMaterial();
            FixWaterReflection();
            FixRayMaterial();
            SaveAll();
            Debug.Log("[VisualMatch] Iteration 001 applied. Bloom activated, corona disabled, ring cleaned.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 002 – fix over-bright ring + ray dimming + bg glow
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 002 – Fix Bloom+Rays+Background")]
        public static void Iteration002()
        {
            LoadScene();

            // Ring: lower intensity so gradient is visible, not white-hot
            Material ring = LoadMat(RingMatPath);
            ring.SetFloat("_Intensity", 1.8f);
            ring.SetFloat("_SegmentContrast", 0.55f);
            ring.SetColor("_ColorA", new Color(0.0f, 1.5f, 8.0f, 1f));   // blue
            ring.SetColor("_ColorB", new Color(7.5f, 0.0f, 5.0f, 1f));   // magenta
            EditorUtility.SetDirty(ring);

            // Bloom: less aggressive so colors survive tone mapping
            RebuildVolumeProfile(
                bloomIntensity:   0.85f,
                bloomThreshold:   0.65f,
                bloomScatter:     0.70f,
                postExposure:    -0.4f,
                contrast:         28f,
                saturation:       18f,
                colorFilter:      new Color(0.68f, 0.80f, 1.0f));
            FixPostProcessController(0.85f, 0.25f);

            // Background glow: much dimmer
            FixBackgroundGlow(0.06f);

            // Ray shafts: nearly invisible (reference has very faint upward rays)
            Material ray = LoadMat(RayMatPath);
            if (ray != null)
            {
                ray.SetFloat("_Intensity", 0.12f);
                ray.SetColor("_Color", new Color(0.15f, 0.05f, 1.5f, 0.15f));
                EditorUtility.SetDirty(ray);
            }

            // Water reflection: narrower, dimmer
            Material water = LoadMat(WaterMatPath);
            water.SetFloat("_Intensity", 0.85f);
            water.SetFloat("_Width", 0.08f);
            EditorUtility.SetDirty(water);

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 002 applied.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 003 – fix ring HDR values, disable rays, dark bg
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 003 – Ring HDR + Disable Rays")]
        public static void Iteration003()
        {
            LoadScene();

            // Ring: much lower HDR values so ACES tonemapping preserves gradient
            // Reference blue channel ~3-5, magenta ~4-6 in linear HDR
            Material ring = LoadMat(RingMatPath);
            ring.SetFloat("_Intensity", 1.05f);
            ring.SetFloat("_SegmentContrast", 0.4f);   // nearly flat (reference = uniform)
            ring.SetFloat("_NoiseStrength", 0.0f);
            ring.SetColor("_ColorA", new Color(0.0f, 1.0f, 5.5f, 1f));   // blue  HDR: B=5.5
            ring.SetColor("_ColorB", new Color(5.5f, 0.0f, 3.5f, 1f));   // magenta HDR: R=5.5
            EditorUtility.SetDirty(ring);

            // Bloom: moderate so glow appears without whiting out ring
            RebuildVolumeProfile(
                bloomIntensity:  0.70f,
                bloomThreshold:  0.72f,
                bloomScatter:    0.68f,
                postExposure:   -0.55f,
                contrast:        30f,
                saturation:      20f,
                colorFilter:     new Color(0.70f, 0.78f, 1.0f));
            FixPostProcessController(0.70f, 0.20f);

            // Background glow: very dim atmospheric blue
            FixBackgroundGlow(0.04f);

            // Disable the radial ray bursts object entirely
            GameObject shafts = GameObject.Find("Random Radial Light Shafts");
            if (shafts != null) shafts.SetActive(false);

            // Also zero out radial ray material
            Material ray = LoadMat(RayMatPath);
            if (ray != null) { ray.SetFloat("_Intensity", 0.0f); EditorUtility.SetDirty(ray); }

            // Mist: dark indigo at bottom corners
            Material mist = LoadMat(MistMatPath);
            mist.SetColor("_Color", new Color(0.35f, 0.0f, 1.5f, 0.28f));
            mist.SetFloat("_Intensity", 0.32f);
            EditorUtility.SetDirty(mist);

            // Water: narrow bright vertical streak
            Material water = LoadMat(WaterMatPath);
            water.SetFloat("_Width", 0.06f);
            water.SetFloat("_Intensity", 0.65f);
            water.SetColor("_ColorA", new Color(0f, 1.0f, 5.0f, 1f));
            water.SetColor("_ColorB", new Color(5.0f, 0f, 4.0f, 1f));
            EditorUtility.SetDirty(water);

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 003 applied.");
        }

        // ────────────────────────────────────────────────────────────────
        // Screenshot helper (Editor-only render to file, batch-mode safe)
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Capture Current Screenshot")]
        public static void CaptureScreenshot()
        {
            LoadScene();
            Camera cam = Object.FindObjectOfType<Camera>();
            if (cam == null) { Debug.LogWarning("[VisualMatch] No camera found."); return; }
            // Prewarm all particle systems so they appear fully spawned
            foreach (ParticleSystem ps in Object.FindObjectsOfType<ParticleSystem>())
            {
                ps.Simulate(4f, true, true);
            }
            string path = "ref/current.png";
            Directory.CreateDirectory("ref");
            RenderToFile(cam, path, 576, 880);
            Debug.Log($"[VisualMatch] Screenshot saved → {path}");
        }

        // Run Iteration001 then immediately capture screenshot (for CI)
        [MenuItem("Tools/AudioVisualizer/Visual Match/Apply Iter001 + Screenshot")]
        public static void Iteration001AndScreenshot()
        {
            Iteration001();
            CaptureScreenshot();
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 004 – fix ring SIZE (orth_size) and POSITION (Y)
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 004 – Fix Ring Size+Position")]
        public static void Iteration004()
        {
            LoadScene();

            // --- Camera: reduce orthographic size so ring fills ~35% of width ---
            Camera cam = Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.orthographicSize = 7.0f;  // was 8.8 → ring 25% larger
                EditorUtility.SetDirty(cam.gameObject);
                Debug.Log("[VisualMatch] Camera orth_size → 7.0");
            }

            // --- Reposition ring-related objects to match reference y=0.531 ---
            // With orth_size=7.0 and image height 880: scale=62.86 px/unit
            // Reference ring cy = 0.531*880 = 467px = 27px below center
            // Ring world Y = -27/62.86 = -0.43
            const float ringY = -0.43f;
            const float ringZ = -0.6f;
            const float diskZ = -0.55f;

            RepositionObject("HDR Energy Ring",           new Vector3(0f, ringY, ringZ));
            RepositionObject("Light Absorbing Portal Disk", new Vector3(0f, ringY, diskZ));
            RepositionObject("Procedural Plasma Corona",  new Vector3(0f, ringY, -0.65f));
            RepositionObject("Purple Blue Water Reflection", new Vector3(0f, ringY - 6.1f, -0.2f));
            RepositionObject("Low Horizon Glow",          new Vector3(0f, ringY - 5.1f, -0.3f));
            RepositionObject("Dark Mountain Silhouettes", new Vector3(0f, ringY - 6.2f, -0.05f));

            // Update RingEnergyFlow orbitCenter to match new ring Y
            RingEnergyFlow flow = Object.FindObjectOfType<RingEnergyFlow>();
            if (flow != null)
            {
                flow.orbitCenter = new Vector3(0f, ringY, -0.12f);
                EditorUtility.SetDirty(flow.gameObject);
                Debug.Log($"[VisualMatch] RingEnergyFlow orbitCenter.y → {ringY}");
            }

            // Also shift the orbiting plasma points
            for (int i = 0; i < 16; i++)
            {
                GameObject pt = GameObject.Find($"Orbiting Plasma Point {i:00}");
                if (pt != null)
                {
                    pt.transform.localPosition = new Vector3(0f, ringY, -0.12f);
                    EditorUtility.SetDirty(pt);
                }
            }

            // Mist layers: shift down proportionally
            for (int i = 0; i < 8; i++)
            {
                GameObject mist = GameObject.Find($"Animated Volumetric Mist {i:00}");
                if (mist != null)
                {
                    Vector3 p = mist.transform.localPosition;
                    // Shift by the same delta as the ring (0.25 - (-0.43) = -0.68)
                    mist.transform.localPosition = new Vector3(p.x, p.y - 0.68f, p.z);
                    EditorUtility.SetDirty(mist);
                }
            }

            // Background glow: center on new ring Y
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Vector3 p = bg.transform.position;
                bg.transform.position = new Vector3(p.x, ringY + 0.9f, p.z);
                EditorUtility.SetDirty(bg);
            }

            // Accent lights: shift to match
            RepositionLight("Cyan Rim Light",    new Vector3(-3f, ringY + 1.8f, -3f));
            RepositionLight("Magenta Rim Light", new Vector3(3f,  ringY + 1.1f, -3f));

            // Ray shafts host: shift
            GameObject shafts = GameObject.Find("Random Radial Light Shafts");
            if (shafts != null)
            {
                Vector3 p = shafts.transform.position;
                shafts.transform.position = new Vector3(p.x, ringY, p.z);
                EditorUtility.SetDirty(shafts);
            }

            // --- Ring material: tune colors for cleaner gradient ---
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                // Slightly larger HDR ratio so ACES shows colour difference
                // Blue left: B>>R (pure cyan-blue)
                // Magenta right: R>B>G (hot pink)
                ring.SetColor("_ColorA", new Color(0.0f, 0.8f, 4.5f, 1f));  // cyan-blue
                ring.SetColor("_ColorB", new Color(4.5f, 0.0f, 3.0f, 1f));  // pink-magenta
                ring.SetFloat("_Intensity", 1.15f);
                ring.SetFloat("_SegmentContrast", 0.35f);
                EditorUtility.SetDirty(ring);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 004 applied: ring size + position corrected.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 005 – tight bloom, fix ring color gradient, dim mist
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 005 – Tight Bloom + Ring Color Fix")]
        public static void Iteration005()
        {
            LoadScene();

            // --- Bloom: tighten scatter so glow stays around ring, not dome ---
            // scatter=0.38 → compact halo; was 0.68 which spread widely upward
            RebuildVolumeProfile(
                bloomIntensity:  0.60f,
                bloomThreshold:  0.60f,
                bloomScatter:    0.38f,
                postExposure:   -0.45f,
                contrast:        28f,
                saturation:      18f,
                colorFilter:     new Color(0.72f, 0.80f, 1.0f));
            FixPostProcessController(0.60f, 0.18f);

            // --- Ring color: fix B:R ratio to match reference ---
            // Ref left BGR=[121,1,6]:  B dominant → ColorA=(R:0, G:0, B:4.5) pure blue
            // Ref right BGR=[126,0,28]: B:R≈4.5:1 → ColorB=(R:1.2, G:0, B:4.5) blue-magenta
            // Previous ColorB=(4.5,0,3.0) had R=4.5 causing too-pink right side
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 4.5f, 1f));   // pure blue
                ring.SetColor("_ColorB", new Color(1.2f, 0.0f, 4.5f, 1f));   // blue+pink=magenta
                ring.SetFloat("_Intensity", 1.15f);
                ring.SetFloat("_SegmentContrast", 0.30f);
                EditorUtility.SetDirty(ring);
            }

            // --- Ring X position: shift right +0.18 to match ref cx=0.512 ---
            // Current cx=0.499, ref=0.512 → delta=0.013*576px=7.5px → 7.5*(14.0/880)=0.12 world units
            const float ringX = 0.12f;
            const float ringY = -0.43f;
            const float ringZ = -0.6f;
            const float diskZ = -0.55f;

            RepositionObject("HDR Energy Ring",             new Vector3(ringX, ringY, ringZ));
            RepositionObject("Light Absorbing Portal Disk", new Vector3(ringX, ringY, diskZ));
            RepositionObject("Procedural Plasma Corona",    new Vector3(ringX, ringY, -0.65f));
            RepositionObject("Purple Blue Water Reflection", new Vector3(ringX, ringY - 6.1f, -0.2f));

            RingEnergyFlow flow = Object.FindObjectOfType<RingEnergyFlow>();
            if (flow != null)
            {
                flow.orbitCenter = new Vector3(ringX, ringY, -0.12f);
                EditorUtility.SetDirty(flow.gameObject);
            }
            for (int i = 0; i < 16; i++)
            {
                GameObject pt = GameObject.Find($"Orbiting Plasma Point {i:00}");
                if (pt != null) { pt.transform.localPosition = new Vector3(ringX, ringY, -0.12f); EditorUtility.SetDirty(pt); }
            }

            // --- Mist: reduce intensity to stop additive accumulation ---
            // 8 mist layers × 0.32 intensity = 2.56 total → triggers bloom dome
            // 8 × 0.12 = 0.96 → just below bloom threshold
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.20f, 0.0f, 0.9f, 0.25f));
                mist.SetFloat("_Intensity", 0.12f);
                EditorUtility.SetDirty(mist);
            }

            // --- Background glow embedded instance: dim Color as well ---
            // The embedded material _Intensity=0.04 but _Color=(0.9,0,2.6,0.45) is bright
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.15f, 0.0f, 0.7f, 0.25f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.03f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 005 applied: bloom tightened, ring color fixed, mist dimmed.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 006 – rotate ring 90° so gradient aligns LEFT=blue RIGHT=magenta
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 006 – Rotate Ring + Fix Gradient")]
        public static void Iteration006()
        {
            LoadScene();

            // --- Rotate HDR Energy Ring +90° around Z ---
            // UV.x=0 starts at TOP of mesh; +90° CCW moves that to LEFT side
            // After rotation: LEFT=UV.x=0 (blue/ColorA), RIGHT=UV.x=0.5 (magenta/ColorB)
            GameObject ringObj = GameObject.Find("HDR Energy Ring");
            if (ringObj != null)
            {
                Vector3 euler = ringObj.transform.localEulerAngles;
                ringObj.transform.localEulerAngles = new Vector3(euler.x, euler.y, 90f);
                EditorUtility.SetDirty(ringObj);
                Debug.Log("[VisualMatch] HDR Energy Ring rotated Z=90");
            }

            // --- Ring colors: match reference B:R ratios ---
            // At left (uv.x=0, t≈0.11): lerp(ColorA, ColorB, 0.11)  → ref BGR=[121,1,6]  R/B=0.050
            // At right(uv.x=0.5, t≈0.59): lerp(ColorA, ColorB, 0.59) → ref BGR=[126,0,28] R/B=0.222
            // Solved: ColorA=(0,0,5.5), ColorB=(2.0,0,5.0) gives left R/B≈0.040, right R/B≈0.215
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 5.5f, 1f));   // pure blue (left)
                ring.SetColor("_ColorB", new Color(2.0f, 0.0f, 5.0f, 1f));   // blue+red=magenta (right)
                ring.SetFloat("_Intensity", 1.20f);
                ring.SetFloat("_SegmentContrast", 0.25f);  // softer segments → smoother gradient
                EditorUtility.SetDirty(ring);
            }

            // --- Bloom: slightly wider scatter for visible ring glow ---
            // Prev scatter=0.38 too tight (ring looks thin); ref has visible glow halo
            RebuildVolumeProfile(
                bloomIntensity:  0.65f,
                bloomThreshold:  0.58f,
                bloomScatter:    0.45f,
                postExposure:   -0.35f,
                contrast:        26f,
                saturation:      16f,
                colorFilter:     new Color(0.75f, 0.82f, 1.0f));
            FixPostProcessController(0.65f, 0.18f);

            // --- Mist: slightly brighter for visible bottom clouds ---
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.28f, 0.0f, 1.2f, 0.30f));
                mist.SetFloat("_Intensity", 0.18f);
                EditorUtility.SetDirty(mist);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 006 applied: ring rotated 90°, gradient aligned LEFT=blue RIGHT=magenta.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 007 – fix ColorB.R to match reference R ratios
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 007 – Fix ColorB Red")]
        public static void Iteration007()
        {
            LoadScene();

            // Keep ring rotation at Z=90 (verified correct gradient direction)
            // Fix ColorB.R: 2.0 → 0.3
            // Analysis: at left (t≈0.11): R_HDR=0.033 → after contrast(26) crushes to ~0 → left R≈0 ≈ ref 6
            //           at right (t≈0.59): R_HDR=0.165 → after contrast → colorFilter(0.75) → avg ≈ 29 ≈ ref 28
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 5.5f, 1f));   // pure blue
                ring.SetColor("_ColorB", new Color(0.3f, 0.0f, 4.5f, 1f));   // subtle red+blue=violet-magenta
                ring.SetFloat("_Intensity", 1.20f);
                ring.SetFloat("_SegmentContrast", 0.25f);
                EditorUtility.SetDirty(ring);
                Debug.Log("[VisualMatch] Ring ColorB.R reduced: 2.0 → 0.3");
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 007 applied.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 008 – balanced ring color, visible mountains, mist tune
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 008 – Ring Color + Mountains + Mist")]
        public static void Iteration008()
        {
            LoadScene();

            // --- Ring: ColorB.R 0.3 → 0.8 for visible pink right side ---
            // ColorB.R=0.3 was all-blue; 2.0 was all-pink; 0.8 is balanced
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 5.5f, 1f));   // pure blue
                ring.SetColor("_ColorB", new Color(0.8f, 0.0f, 4.5f, 1f));   // blue+pink=violet-magenta
                ring.SetFloat("_Intensity", 1.20f);
                ring.SetFloat("_SegmentContrast", 0.25f);
                EditorUtility.SetDirty(ring);
            }

            // --- Background glow: increase to make mountains visible as silhouettes ---
            // Mountains are dark (color≈0.005) — need atmosphere behind them for contrast
            // Current _Intensity=0.03 gives B_HDR=0.021 → too dim for visible mountains
            // Target: B_LDR≈40-50 for subtle atmospheric background
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.20f, 0.0f, 0.9f, 0.35f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.055f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

            // --- Mist: increase visibility while staying below bloom threshold ---
            // Max mist overlap ≈ 3 layers → 3 × Color.B × Intensity < 0.58 (bloom threshold)
            // 3 × 1.0 × 0.15 = 0.45 → safe
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.25f, 0.0f, 1.0f, 0.32f));
                mist.SetFloat("_Intensity", 0.15f);
                EditorUtility.SetDirty(mist);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 008 applied: ring color balanced, mountains visible, mist tuned.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 009 – fine-tune ColorB.R and boost ring brightness
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 009 – ColorB.R Tune + Brightness")]
        public static void Iteration009()
        {
            LoadScene();

            // Iter008 right R=71 (need 28), left R=36 (need 6). Lower ColorB.R: 0.8 → 0.45
            // Iter007 with ColorB.R=0.3 had ring detected at bottom (ring too dim).
            // Also: both B values (106/107) are below reference (121/126) → boost B by raising ColorA.B
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));   // brighter pure blue
                ring.SetColor("_ColorB", new Color(0.45f, 0.0f, 5.5f, 1f)); // brighter subtle red
                ring.SetFloat("_Intensity", 1.25f);
                ring.SetFloat("_SegmentContrast", 0.20f);  // very smooth segments
                EditorUtility.SetDirty(ring);
            }

            // Bloom: slightly more scatter for wider glow matching reference
            RebuildVolumeProfile(
                bloomIntensity:  0.68f,
                bloomThreshold:  0.55f,
                bloomScatter:    0.50f,
                postExposure:   -0.32f,
                contrast:        25f,
                saturation:      14f,
                colorFilter:     new Color(0.78f, 0.84f, 1.0f));
            FixPostProcessController(0.68f, 0.20f);

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 009 applied.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 010 – reduce ColorB.R further, boost mountains/atmosphere
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 010 – ColorB.R Fine Tune + Atmosphere")]
        public static void Iteration010()
        {
            LoadScene();

            // ColorB.R 0.45→0.25: right R=47*0.25/0.45=26≈ref 28; left R=23*0.25/0.45=13 (ref 6)
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.25f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.25f);
                ring.SetFloat("_SegmentContrast", 0.20f);
                EditorUtility.SetDirty(ring);
            }

            // Background glow: raise to 0.08 to make mountains visible as silhouettes
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.18f, 0.0f, 0.85f, 0.35f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.08f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

            // Also scale mountain object up – it might be too small relative to visible area
            // Mountain at Y=-6.63, with orth_size=7 visible to Y=-7. Scale to fill bottom ~30%
            GameObject mountain = GameObject.Find("Dark Mountain Silhouettes");
            if (mountain != null)
            {
                mountain.transform.localScale = new Vector3(12f, 5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.0f, -0.05f);
                EditorUtility.SetDirty(mountain);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 010 applied.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 011 – fix mountain scale, ColorB.R 0.35, restore hist
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 011 – Mountain Fix + Ring Tune")]
        public static void Iteration011()
        {
            LoadScene();

            // Ring: ColorB.R 0.25→0.35 for slightly more visible gradient while keeping B≈ref
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.35f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.25f);
                ring.SetFloat("_SegmentContrast", 0.20f);
                EditorUtility.SetDirty(ring);
            }

            // Mountain: fix scale (10,2.5,1) so peaks visible without large dark rectangle
            // Scale 12×5 was creating large dark block hurting histogram
            // Peaks: mesh local y=+0.5 → world Y = -6.0+1.25=-4.75 (18% from bottom, matches ref)
            GameObject mountain = GameObject.Find("Dark Mountain Silhouettes");
            if (mountain != null)
            {
                mountain.transform.localScale = new Vector3(10f, 2.5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
                EditorUtility.SetDirty(mountain);
            }

            // Background glow: keep at 0.08 for visible atmosphere
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.15f, 0.0f, 0.75f, 0.30f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.07f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 011 applied.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 012 – Horizon Glow Behind Mountains + Ring Gradient
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 012 – Horizon Glow + Ring Gradient")]
        public static void Iteration012()
        {
            LoadScene();

            // 1. Ring: ColorB.R = 0.45 (Iter009 level — visible violet-pink right side)
            //    Iter011 with 0.35 gave all-blue ring + failed ring detection (circ=0.017)
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.45f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.28f);
                ring.SetFloat("_SegmentContrast", 0.18f);
                EditorUtility.SetDirty(ring);
            }

            // 2. Low Horizon Glow: move BEHIND mountains (Z=0.5 > mountains Z=0.1)
            //    At Z=0.5 mountains occlude it → dark mountain peaks against bright horizon ✓
            //    Scale up and raise to Y=-5.0 so mountain tops are near quad center (max radial)
            GameObject horizonGlow = GameObject.Find("Low Horizon Glow");
            if (horizonGlow != null)
            {
                horizonGlow.transform.localPosition = new Vector3(0f, -5.0f, 0.5f);
                horizonGlow.transform.localScale    = new Vector3(14f, 4f, 1f);
                EditorUtility.SetDirty(horizonGlow);
            }

            // 3. Shared mist: boost for richer atmosphere and visible horizon
            //    M_VioletMist.mat affects all mist layers incl. Low Horizon Glow
            //    Intensity=0.22: horizon center → B_HDR=0.22 → SDR≈56 (visible mountain contrast)
            //    Ring area: 4 overlap layers → 0.88 HDR → moderate bloom matches reference glow
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.22f, 0.0f, 1.0f, 0.40f));
                mist.SetFloat("_Intensity", 0.22f);
                EditorUtility.SetDirty(mist);
            }

            // 4. Background glow: slight intensity boost for sky atmosphere
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.15f, 0.0f, 0.80f, 0.35f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.09f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

            // 5. Water reflection: widen beam for visible bottom-center pink glow
            //    Reference shows bright pinkish glow between mountains at very bottom
            Material water = LoadMat(WaterMatPath);
            if (water != null)
            {
                water.SetFloat("_Intensity", 0.80f);
                water.SetFloat("_Width", 0.12f);
                EditorUtility.SetDirty(water);
            }

            // 6. Mountains: same as Iter011 (scale ok, Z=0.1 in front of horizon glow at Z=0.5)
            GameObject mountain = GameObject.Find("Dark Mountain Silhouettes");
            if (mountain != null)
            {
                mountain.transform.localScale    = new Vector3(10f, 2.5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
                EditorUtility.SetDirty(mountain);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 012 applied: horizon glow behind mountains, ring ColorB.R=0.45.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 013 – Ring Size +2% + Reduced Haze + ColorB.R Tune
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 013 – Ring Size + Haze Reduction")]
        public static void Iteration013()
        {
            LoadScene();

            // 1. Ring: slight scale +2% to match ref r_norm 0.348 (current 0.345)
            //    ColorB.R 0.45→0.50 for slightly more vivid right-side violet gradient
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.50f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.30f);
                ring.SetFloat("_SegmentContrast", 0.18f);
                EditorUtility.SetDirty(ring);
            }
            // Scale ring object by 1.02x
            GameObject ringObj = GameObject.Find("HDR Energy Ring");
            if (ringObj != null)
            {
                ringObj.transform.localScale = new Vector3(1.02f, 1.02f, 1.02f);
                EditorUtility.SetDirty(ringObj);
            }

            // 2. Reduce mist intensity: 0.22→0.18 (less diffuse haze, better histogram)
            //    Reference sky is darker; too much blue haze hurt histogram (0.881→0.869)
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.18f, 0.0f, 0.90f, 0.35f));
                mist.SetFloat("_Intensity", 0.18f);
                EditorUtility.SetDirty(mist);
            }

            // 3. Horizon glow: narrower for more concentrated bottom glow (matches ref)
            //    Less spread = cleaner mountain silhouettes, more dramatic contrast
            GameObject horizonGlow = GameObject.Find("Low Horizon Glow");
            if (horizonGlow != null)
            {
                horizonGlow.transform.localPosition = new Vector3(0f, -5.2f, 0.5f);
                horizonGlow.transform.localScale    = new Vector3(10f, 3f, 1f);
                EditorUtility.SetDirty(horizonGlow);
            }

            // 4. Background glow: slight reduction for darker top sky (ref top is very dark)
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.12f, 0.0f, 0.75f, 0.30f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.08f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

            // 5. Water reflection: widen slightly for visible bottom center beam
            Material water = LoadMat(WaterMatPath);
            if (water != null)
            {
                water.SetFloat("_Intensity", 0.80f);
                water.SetFloat("_Width", 0.14f);
                EditorUtility.SetDirty(water);
            }

            // 6. Mountains: unchanged (optimal position established in Iter011)
            GameObject mountain = GameObject.Find("Dark Mountain Silhouettes");
            if (mountain != null)
            {
                mountain.transform.localScale    = new Vector3(10f, 2.5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
                EditorUtility.SetDirty(mountain);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 013 applied: ring +2%, mist reduced, horizon glow narrowed.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 014 – Restore atmosphere + keep ColorB.R=0.50
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 014 – Restore Atmosphere + Vivid Ring")]
        public static void Iteration014()
        {
            LoadScene();

            // Keep ring improvements from Iter013 (ColorB.R=0.50, scale=1.02)
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.50f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.30f);
                ring.SetFloat("_SegmentContrast", 0.18f);
                EditorUtility.SetDirty(ring);
            }
            GameObject ringObj = GameObject.Find("HDR Energy Ring");
            if (ringObj != null)
            {
                ringObj.transform.localScale = new Vector3(1.02f, 1.02f, 1.02f);
                EditorUtility.SetDirty(ringObj);
            }

            // Restore mist intensity 0.18→0.22: reference has rich atmospheric clouds
            // Iter013 showed SSIM dropped 0.627→0.598 with less mist (too sparse vs ref)
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.20f, 0.0f, 0.95f, 0.38f));
                mist.SetFloat("_Intensity", 0.22f);
                EditorUtility.SetDirty(mist);
            }

            // Restore horizon glow to wider form for richer bottom atmosphere
            GameObject horizonGlow = GameObject.Find("Low Horizon Glow");
            if (horizonGlow != null)
            {
                horizonGlow.transform.localPosition = new Vector3(0f, -5.0f, 0.5f);
                horizonGlow.transform.localScale    = new Vector3(12f, 3.5f, 1f);
                EditorUtility.SetDirty(horizonGlow);
            }

            // Background glow: subtle boost for atmospheric sky
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.14f, 0.0f, 0.78f, 0.32f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.09f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

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
                mountain.transform.localScale    = new Vector3(10f, 2.5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
                EditorUtility.SetDirty(mountain);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 014 applied: atmosphere restored, ColorB.R=0.50, ring scale=1.02.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 015 – Restore horizon (14,4) + PostExposure tune
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 015 – Horizon Restore + Exposure Tune")]
        public static void Iteration015()
        {
            LoadScene();

            // Ring: keep best settings from Iter013/014
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.50f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.30f);
                ring.SetFloat("_SegmentContrast", 0.18f);
                EditorUtility.SetDirty(ring);
            }
            GameObject ringObj = GameObject.Find("HDR Energy Ring");
            if (ringObj != null)
            {
                ringObj.transform.localScale = new Vector3(1.02f, 1.02f, 1.02f);
                EditorUtility.SetDirty(ringObj);
            }

            // Mist: restore to Iter012 color/intensity (best SSIM was with 0.22)
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.22f, 0.0f, 1.0f, 0.40f));
                mist.SetFloat("_Intensity", 0.22f);
                EditorUtility.SetDirty(mist);
            }

            // Horizon glow: restore to (14,4) from Iter012 — that was the SSIM peak
            GameObject horizonGlow = GameObject.Find("Low Horizon Glow");
            if (horizonGlow != null)
            {
                horizonGlow.transform.localPosition = new Vector3(0f, -5.0f, 0.5f);
                horizonGlow.transform.localScale    = new Vector3(14f, 4f, 1f);
                EditorUtility.SetDirty(horizonGlow);
            }

            // Post-processing: postExposure -0.32→-0.28 for slightly brighter output
            // Reference ring and atmosphere look slightly brighter than our current render
            // bloom scatter 0.50→0.52 for slightly more radial glow spread
            RebuildVolumeProfile(
                bloomIntensity:  0.68f,
                bloomThreshold:  0.55f,
                bloomScatter:    0.52f,
                postExposure:   -0.28f,
                contrast:        24f,
                saturation:      14f,
                colorFilter:     new Color(0.80f, 0.86f, 1.0f));
            FixPostProcessController(0.68f, 0.20f);

            // Background glow: back to Iter012 levels
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.15f, 0.0f, 0.80f, 0.35f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.09f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

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
                mountain.transform.localScale    = new Vector3(10f, 2.5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
                EditorUtility.SetDirty(mountain);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 015 applied: horizon (14,4) restored, post-exposure -0.28.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 016 – Exact Iter012 post-proc + ring scale 1.02 + ColorB.R 0.50
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 016 – Optimal Post-Proc + Ring Scale")]
        public static void Iteration016()
        {
            LoadScene();

            // Ring: best gradient from Iter013/014, plus size fix
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.50f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.28f);
                ring.SetFloat("_SegmentContrast", 0.18f);
                EditorUtility.SetDirty(ring);
            }
            GameObject ringObj = GameObject.Find("HDR Energy Ring");
            if (ringObj != null)
            {
                ringObj.transform.localScale = new Vector3(1.02f, 1.02f, 1.02f);
                EditorUtility.SetDirty(ringObj);
            }

            // Mist: exact Iter012 settings (best SSIM was with these)
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.22f, 0.0f, 1.0f, 0.40f));
                mist.SetFloat("_Intensity", 0.22f);
                EditorUtility.SetDirty(mist);
            }

            // Horizon glow: (14,4) from Iter012 — best SSIM width
            GameObject horizonGlow = GameObject.Find("Low Horizon Glow");
            if (horizonGlow != null)
            {
                horizonGlow.transform.localPosition = new Vector3(0f, -5.0f, 0.5f);
                horizonGlow.transform.localScale    = new Vector3(14f, 4f, 1f);
                EditorUtility.SetDirty(horizonGlow);
            }

            // Post-processing: EXACT Iter009/012 settings (the SSIM peak post-proc)
            // Iter015 changed scatter→0.52, exposure→-0.28 → SSIM dropped 0.627→0.620
            RebuildVolumeProfile(
                bloomIntensity:  0.68f,
                bloomThreshold:  0.55f,
                bloomScatter:    0.50f,
                postExposure:   -0.32f,
                contrast:        25f,
                saturation:      14f,
                colorFilter:     new Color(0.78f, 0.84f, 1.0f));
            FixPostProcessController(0.68f, 0.20f);

            // Background glow: same as Iter012
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.15f, 0.0f, 0.80f, 0.35f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.09f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

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
                mountain.transform.localScale    = new Vector3(10f, 2.5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
                EditorUtility.SetDirty(mountain);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 016: exact Iter012 post-proc, ring scale=1.02, ColorB.R=0.50.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 017 – Revert ring scale to 1.0 (test if scale was regression)
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 017 – Revert Ring Scale")]
        public static void Iteration017()
        {
            LoadScene();

            // Ring: revert scale to 1.0 (Iter013-016 had 1.02 which may have caused SSIM drop)
            //       Keep ColorB.R=0.50 (Better than 0.45 for histogram)
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.50f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.28f);
                ring.SetFloat("_SegmentContrast", 0.18f);
                EditorUtility.SetDirty(ring);
            }
            GameObject ringObj = GameObject.Find("HDR Energy Ring");
            if (ringObj != null)
            {
                ringObj.transform.localScale = Vector3.one;  // revert to default 1.0
                EditorUtility.SetDirty(ringObj);
            }

            // All other settings: exact Iter012 (best SSIM=0.627)
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.22f, 0.0f, 1.0f, 0.40f));
                mist.SetFloat("_Intensity", 0.22f);
                EditorUtility.SetDirty(mist);
            }

            GameObject horizonGlow = GameObject.Find("Low Horizon Glow");
            if (horizonGlow != null)
            {
                horizonGlow.transform.localPosition = new Vector3(0f, -5.0f, 0.5f);
                horizonGlow.transform.localScale    = new Vector3(14f, 4f, 1f);
                EditorUtility.SetDirty(horizonGlow);
            }

            RebuildVolumeProfile(
                bloomIntensity:  0.68f,
                bloomThreshold:  0.55f,
                bloomScatter:    0.50f,
                postExposure:   -0.32f,
                contrast:        25f,
                saturation:      14f,
                colorFilter:     new Color(0.78f, 0.84f, 1.0f));
            FixPostProcessController(0.68f, 0.20f);

            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.15f, 0.0f, 0.80f, 0.35f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.09f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

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
                mountain.transform.localScale    = new Vector3(10f, 2.5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
                EditorUtility.SetDirty(mountain);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 017: ring scale reverted to 1.0, ColorB.R=0.50.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 018 – Denser Mist + Higher Saturation
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 018 – Dense Mist + Saturation")]
        public static void Iteration018()
        {
            LoadScene();

            // Ring: back to Iter012 ColorB.R=0.45 (Iter017 with 0.50 gave same SSIM)
            // Revert Intensity to 1.25 for slightly less bloom overlap
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.45f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.25f);
                ring.SetFloat("_SegmentContrast", 0.20f);
                EditorUtility.SetDirty(ring);
            }
            GameObject ringObj = GameObject.Find("HDR Energy Ring");
            if (ringObj != null)
            {
                ringObj.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(ringObj);
            }

            // Mist: increase from 0.22→0.27 for denser atmospheric clouds
            // Reference has much richer purple cloud formations below ring
            // Scatter=0.50 should contain bloom to ring area (not full dome)
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.25f, 0.0f, 1.05f, 0.45f));
                mist.SetFloat("_Intensity", 0.27f);
                EditorUtility.SetDirty(mist);
            }

            // Horizon glow: (14,4) from Iter012
            GameObject horizonGlow = GameObject.Find("Low Horizon Glow");
            if (horizonGlow != null)
            {
                horizonGlow.transform.localPosition = new Vector3(0f, -5.0f, 0.5f);
                horizonGlow.transform.localScale    = new Vector3(14f, 4f, 1f);
                EditorUtility.SetDirty(horizonGlow);
            }

            // Post-proc: increase saturation 14→18 for more vivid purple-violet colors
            // Reference atmosphere and ring look more saturated than current
            RebuildVolumeProfile(
                bloomIntensity:  0.68f,
                bloomThreshold:  0.55f,
                bloomScatter:    0.50f,
                postExposure:   -0.32f,
                contrast:        25f,
                saturation:      18f,
                colorFilter:     new Color(0.78f, 0.84f, 1.0f));
            FixPostProcessController(0.68f, 0.20f);

            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.15f, 0.0f, 0.80f, 0.35f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.09f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

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
                mountain.transform.localScale    = new Vector3(10f, 2.5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
                EditorUtility.SetDirty(mountain);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 018: mist 0.27, saturation 18, ColorB.R=0.45.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 020 – Enable RadialShafts + fix Plasma Dust position + ring R
        // Root causes: (1) "Random Radial Light Shafts" m_IsActive=0 → enable it
        //              (2) Magenta Plasma Dust donut radius=7.4 (off-screen) → Box
        //              (3) Ring ColorB.R=0.45 too high vs ref R/B=0.22 → 0.26
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 020 – RadialShafts + Clouds + RingR")]
        public static void Iteration020()
        {
            LoadScene();

            // 1. Enable Radial Light Shafts (was m_IsActive=0 — GameObject.Find skips disabled)
            var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject shafts = System.Array.Find(allGOs, go =>
                go.name == "Random Radial Light Shafts" && go.scene.IsValid());
            if (shafts != null)
            {
                shafts.SetActive(true);
                EditorUtility.SetDirty(shafts);
                Debug.Log("[VisualMatch] Random Radial Light Shafts: ENABLED");
            }
            else
                Debug.LogWarning("[VisualMatch] 'Random Radial Light Shafts' not found!");

            // 2. Fix Magenta Plasma Dust: was Donut radius=7.4 (off-screen, invisible)
            // Convert to Box emitter at bottom of scene with slow-drifting large particles
            GameObject plasmaDust = GameObject.Find("Magenta Plasma Dust");
            if (plasmaDust != null)
            {
                plasmaDust.transform.localPosition = new Vector3(0f, -5.0f, -0.3f);

                ParticleSystem ps = plasmaDust.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startSpeed    = new ParticleSystem.MinMaxCurve(0.18f, 0.04f);
                    main.startSize     = new ParticleSystem.MinMaxCurve(1.4f, 0.55f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(7.0f, 3.5f);

                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale     = new Vector3(9.5f, 2.5f, 0.1f);

                    var emission = ps.emission;
                    emission.rateOverTime = 120f;

                    EditorUtility.SetDirty(ps);
                    Debug.Log("[VisualMatch] Magenta Plasma Dust: Box emitter at Y=-5, size 1.4, speed 0.18");
                }
            }

            // 3. Ring: reduce ColorB.R to match ref R/B = 0.22
            // Curr right BGR=[126,0,48] vs Ref=[126,0,28] → scale R by 28/48=0.58
            // ColorB.R: 0.45 * 0.58 = 0.26
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.26f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.25f);
                ring.SetFloat("_SegmentContrast", 0.20f);
                EditorUtility.SetDirty(ring);
            }

            // Ring scale: bump 2.7% to close r_norm gap (0.339 → target 0.348)
            GameObject ringObj = GameObject.Find("HDR Energy Ring");
            if (ringObj != null)
            {
                ringObj.transform.localScale = new Vector3(1.028f, 1.028f, 1f);
                EditorUtility.SetDirty(ringObj);
            }

            // 4. Carry forward best baseline settings
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.22f, 0.0f, 1.0f, 0.40f));
                mist.SetFloat("_Intensity", 0.22f);
                EditorUtility.SetDirty(mist);
            }

            GameObject horizonGlow = GameObject.Find("Low Horizon Glow");
            if (horizonGlow != null)
            {
                horizonGlow.transform.localPosition = new Vector3(0f, -5.0f, 0.5f);
                horizonGlow.transform.localScale    = new Vector3(14f, 4f, 1f);
                EditorUtility.SetDirty(horizonGlow);
            }

            RebuildVolumeProfile(
                bloomIntensity:  0.68f,
                bloomThreshold:  0.55f,
                bloomScatter:    0.50f,
                postExposure:   -0.32f,
                contrast:        25f,
                saturation:      14f,
                colorFilter:     new Color(0.78f, 0.84f, 1.0f));
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
                mountain.transform.localScale    = new Vector3(10f, 2.5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
                EditorUtility.SetDirty(mountain);
            }

            // Star dust size fix (from Iter019)
            GameObject starDust = GameObject.Find("Star Dust Field");
            if (starDust != null)
            {
                ParticleSystem ps = starDust.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.048f, 0.018f);
                    EditorUtility.SetDirty(ps);
                }
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 020: radial shafts enabled, plasma clouds at bottom, ring ColorB.R=0.26.");
        }

        // ────────────────────────────────────────────────────────────────
        // Iteration 019 – Fix Star Dust particle size (was sub-pixel 0.006)
        // ────────────────────────────────────────────────────────────────
        [MenuItem("Tools/AudioVisualizer/Visual Match/Iteration 019 – Fix Star Size")]
        public static void Iteration019()
        {
            LoadScene();

            // Ring: exact Iter012 settings (best SSIM baseline)
            Material ring = LoadMat(RingMatPath);
            if (ring != null)
            {
                ring.SetColor("_ColorA", new Color(0.0f, 0.0f, 6.5f, 1f));
                ring.SetColor("_ColorB", new Color(0.45f, 0.0f, 5.5f, 1f));
                ring.SetFloat("_Intensity", 1.25f);
                ring.SetFloat("_SegmentContrast", 0.20f);
                EditorUtility.SetDirty(ring);
            }

            // Mist + horizon: exact Iter012 settings
            Material mist = LoadMat(MistMatPath);
            if (mist != null)
            {
                mist.SetColor("_Color", new Color(0.22f, 0.0f, 1.0f, 0.40f));
                mist.SetFloat("_Intensity", 0.22f);
                EditorUtility.SetDirty(mist);
            }

            GameObject horizonGlow = GameObject.Find("Low Horizon Glow");
            if (horizonGlow != null)
            {
                horizonGlow.transform.localPosition = new Vector3(0f, -5.0f, 0.5f);
                horizonGlow.transform.localScale    = new Vector3(14f, 4f, 1f);
                EditorUtility.SetDirty(horizonGlow);
            }

            // Star Dust Field: increase particle size from 0.006 (sub-pixel) to 0.045
            // Reference has visible star dots: ~2-3 pixels = 3/62.9 pix/unit ≈ 0.048 world units
            // Current size 0.006 → 0.38 pixel = INVISIBLE
            GameObject starDust = GameObject.Find("Star Dust Field");
            if (starDust != null)
            {
                ParticleSystem ps = starDust.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.048f, 0.018f);
                    EditorUtility.SetDirty(ps);
                    Debug.Log("[VisualMatch] Star Dust Field: startSize 0.006→0.048");
                }
            }

            // Magenta Plasma Dust: also likely too small, boost it
            GameObject plasmaDust = GameObject.Find("Magenta Plasma Dust");
            if (plasmaDust != null)
            {
                ParticleSystem ps = plasmaDust.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.025f);
                    EditorUtility.SetDirty(ps);
                }
            }

            // Post-processing: exact Iter012 settings
            RebuildVolumeProfile(
                bloomIntensity:  0.68f,
                bloomThreshold:  0.55f,
                bloomScatter:    0.50f,
                postExposure:   -0.32f,
                contrast:        25f,
                saturation:      14f,
                colorFilter:     new Color(0.78f, 0.84f, 1.0f));
            FixPostProcessController(0.68f, 0.20f);

            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg != null)
            {
                Renderer bgRend = bg.GetComponent<Renderer>();
                if (bgRend != null)
                {
                    bgRend.sharedMaterial.SetColor("_Color", new Color(0.15f, 0.0f, 0.80f, 0.35f));
                    bgRend.sharedMaterial.SetFloat("_Intensity", 0.09f);
                    EditorUtility.SetDirty(bgRend.sharedMaterial);
                }
            }

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
                mountain.transform.localScale    = new Vector3(10f, 2.5f, 1f);
                mountain.transform.localPosition = new Vector3(0f, -6.2f, 0.1f);
                EditorUtility.SetDirty(mountain);
            }

            SaveAll();
            Debug.Log("[VisualMatch] Iteration 019: star dust size fixed 0.006→0.048.");
        }

        // ═══════════════════════════════════════════════════════════════
        //  Private helpers
        // ═══════════════════════════════════════════════════════════════

        private static void LoadScene()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void DisableCoronaObject()
        {
            GameObject corona = GameObject.Find("Procedural Plasma Corona");
            if (corona != null && corona.activeSelf)
            {
                corona.SetActive(false);
                Debug.Log("[VisualMatch] Procedural Plasma Corona → disabled.");
            }
        }

        private static void FixRingMaterial()
        {
            Material ring = LoadMat(RingMatPath);
            ring.SetFloat("_NoiseStrength", 0.0f);        // no vertex spikes
            ring.SetFloat("_SegmentContrast", 0.7f);      // smoother
            ring.SetFloat("_Intensity", 3.5f);            // brighter
            ring.SetColor("_ColorA", new Color(0.0f, 2.0f, 10.0f, 1f));   // deep cyan/blue
            ring.SetColor("_ColorB", new Color(9.5f, 0.0f, 5.5f, 1f));    // hot magenta
            EditorUtility.SetDirty(ring);
            Debug.Log("[VisualMatch] M_NeonRing fixed (NoiseStrength=0).");
        }

        private static void FixCoronaMaterial()
        {
            Material corona = LoadMat(CoronaMatPath);
            corona.SetFloat("_NoiseStrength", 0.0f);
            corona.SetFloat("_Intensity", 0.0f);   // invisible while disabled
            corona.SetFloat("_SegmentContrast", 0.5f);
            EditorUtility.SetDirty(corona);
        }

        private static void RebuildVolumeProfile(
            float bloomIntensity = 1.85f, float bloomThreshold = 0.50f, float bloomScatter = 0.78f,
            float postExposure = -0.25f, float contrast = 26f, float saturation = 16f,
            Color colorFilter = default)
        {
            if (colorFilter == default) colorFilter = new Color(0.72f, 0.82f, 1f);

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            // Clear stale components and re-add
            profile.components.Clear();

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.intensity.Override(bloomIntensity);
            bloom.threshold.Override(bloomThreshold);
            bloom.scatter.Override(bloomScatter);

            Tonemapping tone = profile.Add<Tonemapping>(true);
            tone.active = true;
            tone.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.active = true;
            color.postExposure.Override(postExposure);
            color.contrast.Override(contrast);
            color.saturation.Override(saturation);
            color.colorFilter.Override(colorFilter);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.Override(0.40f);
            vignette.smoothness.Override(0.65f);

            ChromaticAberration aberration = profile.Add<ChromaticAberration>(true);
            aberration.active = true;
            aberration.intensity.Override(0.04f);

            EditorUtility.SetDirty(profile);

            // Wire up to Volume in scene
            Volume vol = Object.FindObjectOfType<Volume>();
            if (vol != null)
            {
                vol.sharedProfile = profile;
                EditorUtility.SetDirty(vol);
                Debug.Log("[VisualMatch] VP_NeonPortal profile rebuilt and attached to Volume.");
            }
            else
            {
                Debug.LogWarning("[VisualMatch] No Volume found in scene – profile rebuilt but not attached.");
            }
        }

        private static void FixPostProcessController(float baseBloom, float pulseBloom)
        {
            // PortalPulseController lives on the Neon Portal Scene Root
            PortalPulseController pulse = Object.FindObjectOfType<PortalPulseController>();
            if (pulse == null) return;
            pulse.baseBloomIntensity = baseBloom;
            pulse.pulseBloomIntensity = pulseBloom;
            pulse.postProcessVolume = Object.FindObjectOfType<Volume>();
            EditorUtility.SetDirty(pulse);
            Debug.Log($"[VisualMatch] PortalPulseController bloom={baseBloom}±{pulseBloom}");
        }

        private static void FixBackgroundGlow(float intensity)
        {
            GameObject bg = GameObject.Find("Blue Violet Background Glow");
            if (bg == null) return;
            Renderer rend = bg.GetComponent<Renderer>();
            if (rend == null) return;
            // Use instance material so we don't dirty the shared asset for this quad
            rend.sharedMaterial.SetFloat("_Intensity", intensity);
            EditorUtility.SetDirty(rend.sharedMaterial);
        }

        private static void FixMistMaterial()
        {
            Material mist = LoadMat(MistMatPath);
            // Shift toward deep indigo – matches ref's subtle dark-blue atmospheric haze
            mist.SetColor("_Color", new Color(0.5f, 0.0f, 2.2f, 0.38f));
            mist.SetFloat("_Intensity", 0.42f);
            EditorUtility.SetDirty(mist);
        }

        private static void FixWaterReflection()
        {
            Material water = LoadMat(WaterMatPath);
            water.SetFloat("_Width", 0.12f);       // narrower center band
            water.SetFloat("_Intensity", 1.5f);    // brighter streak
            water.SetColor("_ColorA", new Color(0f, 1.8f, 8.5f, 1f));
            water.SetColor("_ColorB", new Color(8f, 0f, 6.5f, 1f));
            EditorUtility.SetDirty(water);
        }

        private static void FixRayMaterial()
        {
            Material ray = LoadMat(RayMatPath);
            if (ray == null) return;
            // Faint blue-white shafts upward
            ray.SetColor("_Color", new Color(0.3f, 0.1f, 2.5f, 0.28f));
            ray.SetFloat("_Softness", 5.0f);
            ray.SetFloat("_Intensity", 0.55f);
            EditorUtility.SetDirty(ray);
        }

        private static void SaveAll()
        {
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.Refresh();
        }

        private static Material LoadMat(string path)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) Debug.LogWarning($"[VisualMatch] Material not found: {path}");
            return mat;
        }

        private static void RepositionObject(string name, Vector3 pos)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null) return;
            obj.transform.localPosition = pos;
            EditorUtility.SetDirty(obj);
        }

        private static void RepositionLight(string name, Vector3 pos)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null) return;
            obj.transform.position = pos;
            EditorUtility.SetDirty(obj);
        }

        private static void RenderToFile(Camera camera, string path, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            RenderTexture prev = camera.targetTexture;
            RenderTexture prevActive = RenderTexture.active;
            camera.targetTexture = rt;
            RenderTexture.active = rt;
            camera.Render();
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            camera.targetTexture = prev;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }
    }
}
#endif
