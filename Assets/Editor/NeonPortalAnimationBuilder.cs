#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Ediskrad.AudioVisualizer.Editor
{
    /// <summary>
    /// Injects the full procedural, Play-Mode animated energy sphere from ref/1.mp4
    /// onto the existing world-space "Multi Layer Ring Quad" inside
    /// NeonPortalSceneAnimationNCS, anchored at the ring's real world centre/radius.
    /// Touches ONLY the sphere effect: background, camera, mountains, sky, water and
    /// composition are left untouched. Captures a representative frame to
    /// current_animation.png for the 1.mp4 comparison loop.
    /// </summary>
    public static class NeonPortalAnimationBuilder
    {
        private const string ScenePath = "Assets/Scenes/NeonPortalSceneAnimationNCS.unity";
        private const string AnimationScenePath = "Assets/Scenes/NeonPortalSceneAnimation.unity";
        private const string BlueScenePath = "Assets/Scenes/NeonPortalSceneAnimationBlue.unity";
        private const string RingObjectName = "Multi Layer Ring Quad";
        private const string RootName = "EnergySphereAnimationRoot";
        private const string OutputPath = "current_animation.png";
        private const string RefOutputPath = "ref/current_animation.png";
        private const string BlueOutputPath = "current_blue.png";
        private const string RefBlueOutputPath = "ref/current_blue.png";
        private const int CaptureWidth = 1152;
        private const int CaptureHeight = 1760;

        // The ring sits at world z = -0.6. The camera looks down +Z from z = -10,
        // so smaller z is closer to the camera. Energy layers render just in front
        // of the ring (and in front of the far backdrop at z = 3).
        private const float EnergyZ = -0.65f;

        // Default capture moment: a rich, near-peak frame of the loop (envelope at max,
        // sparks/rays/hotspots well distributed around the ring).
        private const float PeakEnergyTime = 4.95f;

        private const string PlayCapturePendingKey = "NeonPortalAnimation.PlayCapturePending";
        private const string ExitAfterPlayCaptureKey = "NeonPortalAnimation.ExitAfterPlayCapture";
        private const string CaptureOutputPathKey = "NeonPortalAnimation.CaptureOutputPath";
        private const string CaptureRefOutputPathKey = "NeonPortalAnimation.CaptureRefOutputPath";
        private const string HasPrevPlayOptionsKey = "NeonPortalAnimation.HasPrevPlayOptions";
        private const string PrevPlayOptionsEnabledKey = "NeonPortalAnimation.PrevPlayOptionsEnabled";
        private const string PrevPlayOptionsKey = "NeonPortalAnimation.PrevPlayOptions";
        private static int playModeFrameCount;

        static NeonPortalAnimationBuilder()
        {
            if (!SessionState.GetBool(PlayCapturePendingKey, false)
                && !SessionState.GetBool(ExitAfterPlayCaptureKey, false))
                return;

            EditorApplication.playModeStateChanged -= HandlePlayModeCaptureState;
            EditorApplication.playModeStateChanged += HandlePlayModeCaptureState;

            if (EditorApplication.isPlaying)
            {
                playModeFrameCount = 0;
                EditorApplication.update -= PlayModeCaptureUpdate;
                EditorApplication.update += PlayModeCaptureUpdate;
            }
        }

        [MenuItem("Tools/AudioVisualizer/Neon Animation/Build Energy Sphere Animation")]
        public static void BuildEnergySphereAnimation()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildOntoScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NeonPortalAnimation] Built animated energy sphere into " + ScenePath);
        }

        /// <summary>Batch entry: build + deterministic capture of the peak frame.</summary>
        [MenuItem("Tools/AudioVisualizer/Neon Animation/Build + Capture current_animation.png")]
        public static void BuildAndCapture()
        {
            BuildEnergySphereAnimation();
            CaptureAnimationFrame(PeakEnergyTime);
        }

        /// <summary>Build, then run a real Play Mode pass and capture a live frame.</summary>
        [MenuItem("Tools/AudioVisualizer/Neon Animation/Build + Play Mode Capture")]
        public static void BuildAndPlayModeCapture()
        {
            BuildEnergySphereAnimation();
            StartPlayModeCapture(ScenePath, OutputPath, RefOutputPath);
        }

        [MenuItem("Tools/AudioVisualizer/Neon Animation/Play Mode Capture Animation Scene")]
        public static void PlayModeCaptureAnimationScene()
        {
            StartPlayModeCapture(AnimationScenePath, OutputPath, RefOutputPath);
        }

        [MenuItem("Tools/AudioVisualizer/Neon Animation/Play Mode Capture NCS Scene")]
        public static void PlayModeCaptureNcsScene()
        {
            StartPlayModeCapture(ScenePath, OutputPath, RefOutputPath);
        }

        [MenuItem("Tools/AudioVisualizer/Neon Animation/Build Blue Surface Particles")]
        public static void BuildBlueSurfaceParticles()
        {
            Scene scene = EditorSceneManager.OpenScene(BlueScenePath, OpenSceneMode.Single);
            ConfigureBlueSurfaceParticles(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NeonPortalAnimation] Built SphereSurfaceParticles into " + BlueScenePath);
        }

        [MenuItem("Tools/AudioVisualizer/Neon Animation/Build + Play Mode Capture Blue Scene")]
        public static void BuildAndPlayModeCaptureBlueScene()
        {
            BuildBlueSurfaceParticles();
            StartPlayModeCapture(BlueScenePath, BlueOutputPath, RefBlueOutputPath);
        }

        [MenuItem("Tools/AudioVisualizer/Neon Animation/Capture current_animation.png")]
        public static void CaptureCurrent()
        {
            CaptureAnimationFrame(PeakEnergyTime);
        }

        /// <summary>
        /// Renders the full loop as a frame sequence (default 10 s @ 30 fps, matching
        /// ref/1.mp4) to ref/anim_frames/frame_####.png. ffmpeg then muxes them into
        /// ref/current_animation.mp4. Frames are deterministic in time, so the export is
        /// identical to what plays live in Play Mode.
        /// </summary>
        [MenuItem("Tools/AudioVisualizer/Neon Animation/Export Video Frames (10s @ 30fps)")]
        public static void ExportVideoFrames()
        {
            const float duration = 10.0f;
            const int fps = 30;
            const string framesDir = "ref/anim_frames";

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (FindRootByName(scene, RootName) == null)
                BuildOntoScene(scene);

            Camera cam = FindCaptureCamera();
            if (cam == null)
            {
                Debug.LogError("[NeonPortalAnimation] No camera found in scene.");
                return;
            }

            if (Directory.Exists(framesDir))
                Directory.Delete(framesDir, true);
            Directory.CreateDirectory(framesDir);

            int frameCount = Mathf.RoundToInt(duration * fps);
            for (int i = 0; i < frameCount; i++)
            {
                float t = i / (float)fps;
                PreparePreview(t);
                RenderToFileLdr(cam, $"{framesDir}/frame_{i:0000}.png", CaptureWidth, CaptureHeight);
            }
            Debug.Log($"[NeonPortalAnimation] Exported {frameCount} frames to {framesDir} ({duration}s @ {fps}fps).");
        }

        /// <summary>Debug: capture a sequence to verify motion and pick the richest frame.</summary>
        [MenuItem("Tools/AudioVisualizer/Neon Animation/Capture Sequence (debug)")]
        public static void CaptureSequence()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Camera cam = FindCaptureCamera();
            if (cam == null)
            {
                Debug.LogError("[NeonPortalAnimation] No camera found in scene.");
                return;
            }

            Directory.CreateDirectory("ref/anim_seq");
            float[] times = { 0.30f, 1.10f, 1.65f, 4.95f, 8.30f, 11.6f };
            for (int i = 0; i < times.Length; i++)
            {
                PreparePreview(times[i]);
                RenderToFile(cam, $"ref/anim_seq/frame_{i:00}.png", CaptureWidth, CaptureHeight);
            }
            Debug.Log("[NeonPortalAnimation] Sequence captured to ref/anim_seq.");
        }

        private static void CaptureAnimationFrame(float time)
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Camera cam = FindCaptureCamera();
            if (cam == null)
            {
                Debug.LogError("[NeonPortalAnimation] No camera found in scene.");
                return;
            }

            PreparePreview(time);
            RenderToFile(cam, OutputPath, CaptureWidth, CaptureHeight);
            Directory.CreateDirectory("ref");
            RenderToFile(cam, RefOutputPath, CaptureWidth, CaptureHeight);
            Debug.Log("[NeonPortalAnimation] Captured " + OutputPath + " at t=" + time);
        }

        // ---------------------------------------------------------------------
        // Build
        // ---------------------------------------------------------------------

        private static void BuildOntoScene(Scene scene)
        {
            Renderer ringRenderer = FindRingRenderer(scene);
            if (ringRenderer == null)
            {
                Debug.LogError("[NeonPortalAnimation] '" + RingObjectName + "' not found in scene.");
                return;
            }

            Vector2 center = ReadRingCenter(ringRenderer, new Vector2(0.12f, -0.43f));
            float radius = ReadRingRadius(ringRenderer, 3.05f);
            float warmAngle = ReadRingFloat(ringRenderer, "_WarmAngle", 0.16f);

            // Remove a previous build so re-runs are idempotent.
            GameObject existing = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .FirstOrDefault(go => go.name == RootName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            GameObject root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = new Vector3(center.x, center.y, EnergyZ);

            // 1. Ring light animator (drives white core, coloured glow, HDR, warm-angle
            //    drift, instability/deformation) on the existing world-space ring quad.
            NeonRingEnergyAnimator ringAnimator = root.AddComponent<NeonRingEnergyAnimator>();
            ringAnimator.ringRenderer = ringRenderer;
            ringAnimator.baseRadius = radius;
            ringAnimator.baseWarmAngle = warmAngle;

            // 2. Controller: drives inner membrane + provides shared energy envelope.
            //    ringRenderer left null so it does not fight the ring animator.
            EnergySphereController controller = root.AddComponent<EnergySphereController>();
            ringAnimator.controller = controller;

            Renderer innerRenderer = CreateInnerMesh(root, center, radius, warmAngle, out InnerNCSSpectrumSphere innerSphere);
            ConfigureController(controller, root.transform, innerRenderer, innerSphere, radius, warmAngle);

            // 3-6. Rays, sparks, hotspots, atmosphere haze.
            EnergyRayBurstSystem rays = CreateRayBursts(root, controller, radius);
            EnergyHotspotSystem hotspots = CreateHotspots(root, controller, rays, radius);
            EnergySparkSystem sparks = CreateSparks(root, controller, radius);
            EnergyAtmosphereParticles atmosphere = CreateAtmosphere(root, controller, radius);

            rays.EnsureInitialized();
            hotspots.EnsureInitialized();
            sparks.EnsureInitialized();
            atmosphere.EnsureInitialized();

            EditorUtility.SetDirty(root);
        }

        private static void ConfigureController(EnergySphereController ctrl, Transform root,
            Renderer inner, InnerNCSSpectrumSphere innerSphere, float radius, float warmAngle)
        {
            ctrl.ringRenderer = null;
            ctrl.innerRenderer = inner;
            ctrl.innerMesh = null;
            ctrl.innerNCSSphere = innerSphere;
            ctrl.sphereTransform = root;
            ctrl.animationDuration = 3.333333f;
            ctrl.fadeInDuration = 0.85f;
            ctrl.fadeOutDuration = 1.05f;
            ctrl.pulseSpeed = 0.30f;
            ctrl.pulseAmplitude = 0.12f;
            ctrl.emissionMin = 0.16f;
            ctrl.emissionMax = 1.0f;
            ctrl.rayIntensity = 0.62f;
            ctrl.particleLifetime = 3.2f;
            ctrl.particleSpeed = 0.34f;
            ctrl.blueColorWeight = 1.05f;
            ctrl.pinkColorWeight = 0.88f;
            ctrl.ringRadius = radius;
            ctrl.ringPulseAmp = 0.018f;
            ctrl.ringPulseSpeed = 0.55f;
            ctrl.ringNoiseAmp = 0.018f;
            ctrl.ringNoiseSpeed = 0.18f;
            ctrl.globalPulseSpeed = 0.30f;
            ctrl.globalPulseAmp = 0.08f;
            ctrl.corePulseAmp = 0.18f;
            ctrl.warmAngleDriftSpeed = 0.10f;
            ctrl.warmAngleDriftAmp = 0.18f;
            ctrl.baseWarmAngle = warmAngle;
            ctrl.hotspotIntensityBase = 6.2f;
            ctrl.hotspotCycleSpeed = 0.48f;
            ctrl.hotspotWidth = 0.085f;
            ctrl.masterIntensity = 1.0f;
            ctrl.intensityBreathSpeed = 0.30f;
            ctrl.intensityBreathAmp = 0.06f;
        }

        private static Renderer CreateInnerMesh(GameObject parent, Vector2 center, float radius, float warmAngle, out InnerNCSSpectrumSphere innerSphere)
        {
            innerSphere = null;
            GameObject go = new GameObject("InnerNCSSpectrumSphere");
            go.transform.SetParent(parent.transform, false);
            // Slightly behind the front energy layers, on the ring plane.
            go.transform.localPosition = new Vector3(0f, 0f, 0.04f);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateQuad(radius * 2.15f);

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            // NCS Spectrum-style inner membrane: thin spectral ribbons + dust hugging the
            // inner edge, deep-dark centre. Replaces the old thick-loop membrane shader.
            Shader shader = Shader.Find("AudioVisualizer/InnerNCSSpectrum");
            if (shader == null)
            {
                Debug.LogWarning("[NeonPortalAnimation] InnerNCSSpectrum shader not found.");
                return mr;
            }

            Material mat = new Material(shader) { renderQueue = 3020 };
            // World-space centre MUST match the ring (the shader uses world position).
            mat.SetFloat("_RingCenterX", center.x);
            mat.SetFloat("_RingCenterY", center.y);
            mat.SetFloat("_InnerRadius", radius * 0.958f);
            // Zone: deep-dark centre, effect packed against the inner edge.
            mat.SetFloat("_CoreDark", 0.60f);
            mat.SetFloat("_ShellWidth", 0.45f);
            mat.SetFloat("_EdgeFeather", 0.055f);
            mat.SetFloat("_EdgeBias", 2.75f);
            mat.SetFloat("_CoreAbsorb", 0.35f);
            mat.SetFloat("_AbsorbRadius", 0.68f);
            mat.SetFloat("_FresnelPower", 2.3f);
            // Thin spectral ribbons (soft, secondary to the dust fabric).
            mat.SetFloat("_BandIntensity", 0.40f);
            mat.SetFloat("_BandSharpness", 155.0f);
            mat.SetFloat("_WaveAmp", 0.040f);
            mat.SetFloat("_Ribbon1R", 0.954f);
            mat.SetFloat("_Ribbon2R", 0.884f);
            mat.SetFloat("_Ribbon3R", 0.812f);
            mat.SetFloat("_Freq1", 7.0f);
            mat.SetFloat("_Freq2", 11.0f);
            mat.SetFloat("_Freq3", 5.0f);
            mat.SetFloat("_Speed1", 0.30f);
            mat.SetFloat("_Speed2", -0.23f);
            mat.SetFloat("_Speed3", 0.16f);
            mat.SetFloat("_FineThreadIntensity", 0.035f);
            // Very faint spokes: enough for a circular visualizer feel, not a grid.
            mat.SetFloat("_SpokeIntensity", 0.035f);
            mat.SetFloat("_SpokeCount", 112.0f);
            mat.SetFloat("_SpokeSpeed", 0.045f);
            // Fine dust is edge-biased; the centre stays nearly black.
            mat.SetFloat("_DotIntensity", 1.10f);
            mat.SetFloat("_DotDensity", 165.0f);
            mat.SetFloat("_DotThreshold", 0.860f);
            mat.SetFloat("_DotCenterFill", 0.0f);
            mat.SetFloat("_ParticleDrift", 0.055f);
            // Soft energy membrane, dimmer than the outer ring.
            mat.SetFloat("_MembraneFill", 0.060f);
            mat.SetFloat("_MembraneIntensity", 0.48f);
            // Cyan(left/bottom) -> magenta(right/top), matching the ring zones.
            mat.SetColor("_CoolColor", new Color(0.04f, 0.48f, 1.20f, 1f));
            mat.SetColor("_WarmColor", new Color(0.92f, 0.08f, 0.98f, 1f));
            mat.SetFloat("_WarmAngle", warmAngle);
            mat.SetFloat("_WarmSharpness", 1.65f);
            // Motion.
            mat.SetFloat("_NoiseAmp", 0.085f);
            mat.SetFloat("_NoiseFreq", 4.9f);
            mat.SetFloat("_NoiseSpeed", 0.075f);
            mat.SetFloat("_PulseSpeed", 0.24f);
            mat.SetFloat("_PulseAmp", 0.055f);
            mat.SetFloat("_Exposure", 0.90f);

            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            innerSphere = go.AddComponent<InnerNCSSpectrumSphere>();
            innerSphere.targetRenderer = mr;
            innerSphere.radiusScale = 0.958f;
            innerSphere.coreDark = 0.60f;
            innerSphere.shellWidth = 0.45f;
            innerSphere.edgeFeather = 0.055f;
            innerSphere.edgeBias = 2.75f;
            innerSphere.coreAbsorb = 0.35f;
            innerSphere.absorbRadius = 0.68f;
            innerSphere.membraneFill = 0.060f;
            innerSphere.membraneIntensity = 0.48f;
            innerSphere.fresnelPower = 2.3f;
            innerSphere.bandIntensity = 0.40f;
            innerSphere.bandSharpness = 155.0f;
            innerSphere.waveAmp = 0.040f;
            innerSphere.ribbon1R = 0.954f;
            innerSphere.ribbon2R = 0.884f;
            innerSphere.ribbon3R = 0.812f;
            innerSphere.fineThreadIntensity = 0.035f;
            innerSphere.dotIntensity = 1.10f;
            innerSphere.dotDensity = 165.0f;
            innerSphere.dotThreshold = 0.860f;
            innerSphere.dotCenterFill = 0.0f;
            innerSphere.particleDrift = 0.055f;
            innerSphere.noiseAmp = 0.085f;
            innerSphere.noiseFreq = 4.9f;
            innerSphere.noiseSpeed = 0.075f;
            innerSphere.pulseSpeed = 0.24f;
            innerSphere.pulseAmp = 0.055f;
            innerSphere.exposure = 0.90f;

            EditorUtility.SetDirty(go);
            return mr;
        }

        private static EnergyRayBurstSystem CreateRayBursts(GameObject parent, EnergySphereController controller, float radius)
        {
            float s = radius / 2.0f; // spatial scale relative to the original 2.0-radius tuning
            GameObject go = new GameObject("RayBurstSystem");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;

            EnergyRayBurstSystem rays = go.AddComponent<EnergyRayBurstSystem>();
            rays.controller = controller;
            rays.rayCount = 42;
            rays.sphereRadius = radius;
            rays.rayLengthMin = 0.45f * s;
            rays.rayLengthMax = 2.35f * s;
            rays.rayWidthMin = 0.005f;
            rays.rayWidthMax = 0.024f;
            rays.burstIntervalMin = 0.24f;
            rays.burstIntervalMax = 0.85f;
            rays.burstCountMin = 1;
            rays.burstCountMax = 4;
            rays.flashDuration = 1.15f;
            rays.maxIntensity = 6.6f;
            rays.longRayChance = 0.10f;
            return rays;
        }

        private static EnergyHotspotSystem CreateHotspots(GameObject parent, EnergySphereController controller, EnergyRayBurstSystem rays, float radius)
        {
            float s = radius / 2.0f;
            GameObject go = new GameObject("HotspotSystem");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;

            EnergyHotspotSystem hotspots = go.AddComponent<EnergyHotspotSystem>();
            hotspots.controller = controller;
            hotspots.linkedRays = rays;
            hotspots.hotspotCount = 16;
            hotspots.sphereRadius = radius;
            // Compact, bright, white-cored sparkles ON the ring line (not large blobs).
            hotspots.hotspotSizeMin = 0.085f;
            hotspots.hotspotSizeMax = 0.20f;
            hotspots.hotspotIntensity = 8.5f;
            hotspots.flashDuration = 0.95f;
            hotspots.intervalMin = 0.32f;
            hotspots.intervalMax = 0.95f;
            hotspots.flashesPerEventMin = 1;
            hotspots.flashesPerEventMax = 2;
            return hotspots;
        }

        private static EnergySparkSystem CreateSparks(GameObject parent, EnergySphereController controller, float radius)
        {
            float s = radius / 2.0f;
            GameObject go = new GameObject("SparkParticleSystem");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;

            EnergySparkSystem sparks = go.AddComponent<EnergySparkSystem>();
            sparks.controller = controller;
            sparks.maxParticles = 950;
            sparks.emissionRate = 140f;
            sparks.sphereRadius = radius;
            sparks.sizeMin = 0.016f;
            sparks.sizeMax = 0.085f;
            sparks.lifetimeMin = 1.65f;
            sparks.lifetimeMax = 4.20f;
            sparks.speedMin = 0.035f * s;
            sparks.speedMax = 0.38f * s;
            sparks.ringSpawnChance = 0.66f;
            sparks.innerSpawnChance = 0.26f;
            sparks.outsideJitter = 0.14f;
            sparks.radialBias = 0.65f;
            sparks.tangentialBias = 0.42f;
            sparks.turbulence = 0.06f;
            return sparks;
        }

        private static EnergyAtmosphereParticles CreateAtmosphere(GameObject parent, EnergySphereController controller, float radius)
        {
            float s = radius / 2.0f;
            GameObject go = new GameObject("EnergyBloomAtmosphere");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;

            EnergyAtmosphereParticles atmosphere = go.AddComponent<EnergyAtmosphereParticles>();
            atmosphere.controller = controller;
            atmosphere.maxParticles = 240;
            atmosphere.emissionRate = 24f;
            atmosphere.sphereRadius = radius;
            atmosphere.spreadRadius = 3.05f * s;
            atmosphere.sizeMin = 0.040f * s;
            atmosphere.sizeMax = 0.170f * s;
            return atmosphere;
        }

        // ---------------------------------------------------------------------
        // Preview / capture helpers
        // ---------------------------------------------------------------------

        private static void PreparePreview(float time)
        {
            foreach (EnergySphereController c in Object.FindObjectsOfType<EnergySphereController>())
                c.ApplyState(time);
            foreach (NeonRingEnergyAnimator a in Object.FindObjectsOfType<NeonRingEnergyAnimator>())
                a.ApplyState(time);
            foreach (EnergyRayBurstSystem r in Object.FindObjectsOfType<EnergyRayBurstSystem>())
                r.PreviewAtTime(time);
            foreach (EnergyHotspotSystem h in Object.FindObjectsOfType<EnergyHotspotSystem>())
                h.PreviewAtTime(time);
            foreach (EnergySparkSystem sp in Object.FindObjectsOfType<EnergySparkSystem>())
                sp.PreviewAtTime(time);
            foreach (EnergyAtmosphereParticles at in Object.FindObjectsOfType<EnergyAtmosphereParticles>())
                at.PreviewAtTime(time);
            foreach (SphereSurfaceParticles sf in Object.FindObjectsOfType<SphereSurfaceParticles>())
                sf.PreviewAtTime(time);
            SceneView.RepaintAll();
        }

        private static void StartPlayModeCapture(string scenePath, string outputPath, string refOutputPath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            playModeFrameCount = 0;
            SessionState.SetBool(PlayCapturePendingKey, true);
            SessionState.SetBool(ExitAfterPlayCaptureKey, false);
            SessionState.SetString(CaptureOutputPathKey, outputPath);
            SessionState.SetString(CaptureRefOutputPathKey, refOutputPath);
            StoreAndSetFastPlayModeOptions();
            EditorApplication.playModeStateChanged -= HandlePlayModeCaptureState;
            EditorApplication.playModeStateChanged += HandlePlayModeCaptureState;
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeCaptureState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playModeFrameCount = 0;
                EditorApplication.update -= PlayModeCaptureUpdate;
                EditorApplication.update += PlayModeCaptureUpdate;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= PlayModeCaptureUpdate;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update -= PlayModeCaptureUpdate;
                EditorApplication.playModeStateChanged -= HandlePlayModeCaptureState;
                if (SessionState.GetBool(ExitAfterPlayCaptureKey, false))
                {
                    SessionState.SetBool(ExitAfterPlayCaptureKey, false);
                    RestorePlayModeOptions();
                    EditorApplication.delayCall += () => EditorApplication.Exit(0);
                }
            }
        }

        private static void PlayModeCaptureUpdate()
        {
            playModeFrameCount++;
            // Let the live simulation run ~100 frames so rays/sparks/hotspots populate.
            if (playModeFrameCount < 100)
                return;

            Camera cam = FindCaptureCamera();
            if (cam != null)
            {
                string outputPath = SessionState.GetString(CaptureOutputPathKey, OutputPath);
                string refOutputPath = SessionState.GetString(CaptureRefOutputPathKey, RefOutputPath);
                PreparePreview(PeakEnergyTime);
                RenderToFile(cam, outputPath, CaptureWidth, CaptureHeight);
                Directory.CreateDirectory("ref");
                RenderToFile(cam, refOutputPath, CaptureWidth, CaptureHeight);
                Debug.Log("[NeonPortalAnimation] Play Mode capture saved: " + outputPath);
            }

            SessionState.SetBool(PlayCapturePendingKey, false);
            SessionState.SetBool(ExitAfterPlayCaptureKey, true);
            EditorApplication.update -= PlayModeCaptureUpdate;
            EditorApplication.isPlaying = false;
        }

        private static void StoreAndSetFastPlayModeOptions()
        {
            SessionState.SetBool(HasPrevPlayOptionsKey, true);
            SessionState.SetBool(PrevPlayOptionsEnabledKey, EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(PrevPlayOptionsKey, (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        }

        private static void RestorePlayModeOptions()
        {
            if (!SessionState.GetBool(HasPrevPlayOptionsKey, false))
                return;
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(PrevPlayOptionsEnabledKey, false);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(PrevPlayOptionsKey, 0);
            SessionState.SetBool(HasPrevPlayOptionsKey, false);
        }

        // ---------------------------------------------------------------------
        // Scene queries
        // ---------------------------------------------------------------------

        private static Renderer FindRingRenderer(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == RingObjectName)
                        return t.GetComponent<Renderer>();
                }
            }
            return null;
        }

        private static Vector2 ReadRingCenter(Renderer ring, Vector2 fallback)
        {
            Material mat = ring != null ? ring.sharedMaterial : null;
            if (mat != null && mat.HasProperty("_RingCenterX") && mat.HasProperty("_RingCenterY"))
                return new Vector2(mat.GetFloat("_RingCenterX"), mat.GetFloat("_RingCenterY"));
            return fallback;
        }

        private static float ReadRingRadius(Renderer ring, float fallback)
        {
            return ReadRingFloat(ring, "_RingRadius", fallback);
        }

        private static float ReadRingFloat(Renderer ring, string prop, float fallback)
        {
            Material mat = ring != null ? ring.sharedMaterial : null;
            return mat != null && mat.HasProperty(prop) ? mat.GetFloat(prop) : fallback;
        }

        private static GameObject FindRootByName(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .FirstOrDefault(go => go.name == name);
        }

        private static void ConfigureBlueSurfaceParticles(Scene scene)
        {
            GameObject root = FindRootByName(scene, RootName);
            if (root == null)
            {
                Debug.LogError("[NeonPortalAnimation] '" + RootName + "' not found in Blue scene.");
                return;
            }

            Renderer ringRenderer = FindRingRenderer(scene);
            float radius = ReadRingRadius(ringRenderer, 3.05f);
            EnergySphereController controller = root.GetComponent<EnergySphereController>();

            Transform existing = root.transform.Find("SphereSurfaceParticles");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject go = new GameObject("SphereSurfaceParticles");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.035f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            SphereSurfaceParticles surface = go.AddComponent<SphereSurfaceParticles>();
            surface.controller = controller;
            surface.sphereRadius = radius * 0.932f;
            surface.latitudeBands = 200;
            surface.longitudeBands = 320;
            surface.radialNoise = 0.034f;
            surface.waveDisplacement = 0.050f;
            surface.membraneDrift = 0.014f;
            surface.dotSizeMin = 0.0048f;
            surface.dotSizeMax = 0.0098f;
            surface.baseAlpha = 0.032f;
            surface.ribbonAlpha = 0.92f;
            surface.rimAlpha = 0.28f;
            surface.emissionGain = 3.65f;
            surface.hdrIntensity = 4.55f;
            surface.flowSpeed = 0.060f;
            surface.waveSpeed = 0.105f;
            surface.twinkleSpeed = 0.36f;
            surface.EnsureInitialized();

            InnerEnergyMesh legacyInner = root.GetComponentInChildren<InnerEnergyMesh>(true);
            if (legacyInner != null)
            {
                legacyInner.exposure = 0.24f;
                legacyInner.gridIntensity = 0.018f;
                legacyInner.dotIntensity = 0.16f;
                EditorUtility.SetDirty(legacyInner);
            }

            Renderer innerRenderer = legacyInner != null ? legacyInner.targetRenderer : null;
            if (innerRenderer != null && innerRenderer.sharedMaterial != null)
            {
                Material mat = innerRenderer.sharedMaterial;
                if (mat.HasProperty("_Layer1Intensity"))
                    mat.SetFloat("_Layer1Intensity", 0.055f);
                if (mat.HasProperty("_Layer2Intensity"))
                    mat.SetFloat("_Layer2Intensity", 0.040f);
                if (mat.HasProperty("_Layer3Intensity"))
                    mat.SetFloat("_Layer3Intensity", 0.030f);
                if (mat.HasProperty("_GridIntensity"))
                    mat.SetFloat("_GridIntensity", 0.018f);
                if (mat.HasProperty("_DotIntensity"))
                    mat.SetFloat("_DotIntensity", 0.16f);
                EditorUtility.SetDirty(mat);
            }

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(root);
        }

        private static Camera FindCaptureCamera()
        {
            return Object.FindObjectsOfType<Camera>()
                .Where(c => c != null && c.enabled && c.gameObject.activeInHierarchy)
                .OrderByDescending(c => c.depth)
                .FirstOrDefault();
        }

        private static Mesh CreateQuad(float size)
        {
            float h = size * 0.5f;
            Mesh mesh = new Mesh { name = "InnerEnergyQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-h, -h, 0f),
                new Vector3( h, -h, 0f),
                new Vector3( h,  h, 0f),
                new Vector3(-h,  h, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void RenderToFile(Camera cam, string path, int w, int h)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            RenderTexture rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGBFloat);
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture prevTarget = cam.targetTexture;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
        }

        // 8-bit LDR render with MSAA — used for the video frame sequence (smaller/faster
        // than the float capture used for the still).
        private static void RenderToFileLdr(Camera cam, string path, int w, int h)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture prevTarget = cam.targetTexture;

            cam.targetTexture = rt;
            RenderTexture.active = rt;
            cam.Render();

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);
        }
    }
}
#endif
