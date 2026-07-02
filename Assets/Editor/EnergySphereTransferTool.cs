#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Ediskrad.AudioVisualizer.Editor
{
    public static class EnergySphereTransferTool
    {
        private const string SourceScenePath = "Assets/Scenes/NeonPortalScene.unity";
        private const string TargetScenePath = "Assets/Scenes/EnergySphereScene.unity";
        private const string TargetRootName = "EnergySphereRoot";
        private const string OutputPath = "ref/current.png";
        private const int CaptureWidth = 1152;
        private const int CaptureHeight = 1760;
        private const float TargetFallbackRingRadius = 2.0f;
        private const string PlayCapturePendingKey = "EnergySphereTransfer.PlayCapturePending";
        private const string ExitAfterPlayCaptureKey = "EnergySphereTransfer.ExitAfterPlayCapture";
        private const string HasPreviousPlayModeOptionsKey = "EnergySphereTransfer.HasPreviousPlayModeOptions";
        private const string PreviousPlayModeOptionsEnabledKey = "EnergySphereTransfer.PreviousPlayModeOptionsEnabled";
        private const string PreviousPlayModeOptionsKey = "EnergySphereTransfer.PreviousPlayModeOptions";
        private static int playModeFrameCount;

        static EnergySphereTransferTool()
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

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Dump Neon Portal Sphere Candidates")]
        public static void DumpNeonPortalSphereCandidates()
        {
            Scene source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            List<GameObject> candidates = FindSourceSphereRoots(source).ToList();

            Debug.Log($"[EnergySphereTransfer] Source candidates: {candidates.Count}");
            foreach (GameObject go in candidates)
            {
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
                string shaders = string.Join(", ", renderers
                    .SelectMany(r => r.sharedMaterials)
                    .Where(m => m != null && m.shader != null)
                    .Select(m => m.shader.name)
                    .Distinct());

                Debug.Log(
                    $"[EnergySphereTransfer] candidate '{go.name}' activeSelf={go.activeSelf} " +
                    $"activeInHierarchy={go.activeInHierarchy} pos={go.transform.position} " +
                    $"scale={go.transform.localScale} renderers={renderers.Length} shaders=[{shaders}]");
            }
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Transfer Neon Portal Sphere")]
        public static void TransferNeonPortalSphere()
        {
            Scene source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            List<GameObject> sourceRoots = FindSourceSphereRoots(source).ToList();
            if (sourceRoots.Count == 0)
            {
                Debug.LogError("[EnergySphereTransfer] No active sphere roots found in NeonPortalScene.");
                return;
            }

            Vector3 sourceCenter = DetectSourceCenter(sourceRoots);

            Scene target = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);
            GameObject oldRoot = FindInScene(target, TargetRootName);
            if (oldRoot == null)
            {
                Debug.LogError("[EnergySphereTransfer] EnergySphereRoot not found in EnergySphereScene.");
                return;
            }

            Transform parent = oldRoot.transform.parent;
            int siblingIndex = oldRoot.transform.GetSiblingIndex();
            Vector3 targetLocalPosition = oldRoot.transform.localPosition;
            Quaternion targetLocalRotation = oldRoot.transform.localRotation;
            Vector3 targetLocalScale = oldRoot.transform.localScale;
            Vector3 targetWorldPosition = oldRoot.transform.position;
            float targetRingRadius = DetectTargetRingRadius(oldRoot);
            int targetLayer = oldRoot.layer;

            Object.DestroyImmediate(oldRoot);

            GameObject newRoot = new GameObject(TargetRootName);
            SceneManager.MoveGameObjectToScene(newRoot, target);
            newRoot.layer = targetLayer;
            newRoot.transform.SetParent(parent, false);
            newRoot.transform.localPosition = targetLocalPosition;
            newRoot.transform.localRotation = targetLocalRotation;
            newRoot.transform.localScale = targetLocalScale;
            newRoot.transform.SetSiblingIndex(Mathf.Min(siblingIndex, newRoot.transform.parent == null
                ? target.rootCount - 1
                : newRoot.transform.parent.childCount - 1));

            foreach (GameObject sourceRoot in sourceRoots)
            {
                GameObject copy = Object.Instantiate(sourceRoot);
                copy.name = sourceRoot.name;
                SceneManager.MoveGameObjectToScene(copy, target);
                copy.SetActive(sourceRoot.activeSelf);

                Vector3 worldOffset = sourceRoot.transform.position - sourceCenter;
                Quaternion worldRotation = sourceRoot.transform.rotation;
                Vector3 localScale = sourceRoot.transform.localScale;

                copy.transform.SetParent(newRoot.transform, false);
                copy.transform.position = targetWorldPosition + worldOffset;
                copy.transform.rotation = worldRotation;
                copy.transform.localScale = localScale;

                RetargetWorldSpaceRingMaterials(copy, targetWorldPosition, targetRingRadius);
                EditorUtility.SetDirty(copy);
            }

            EditorUtility.SetDirty(newRoot);
            EditorSceneManager.MarkSceneDirty(target);
            EditorSceneManager.SaveScene(target);
            EditorSceneManager.CloseScene(source, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[EnergySphereTransfer] Transferred {sourceRoots.Count} NeonPortal sphere roots to {TargetScenePath}.");
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Transfer Neon Portal Sphere + Capture")]
        public static void TransferAndCapture()
        {
            TransferNeonPortalSphere();
            ApplyPreviousEnergySphereAnimation();
            CaptureTargetToCurrent();
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Transfer Neon Portal Sphere + Play Mode Capture")]
        public static void TransferAndPlayModeCapture()
        {
            TransferNeonPortalSphere();
            ApplyPreviousEnergySphereAnimation();
            StartPlayModeCapture();
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Apply Previous Sphere Animation")]
        public static void ApplyPreviousEnergySphereAnimation()
        {
            Scene target = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            GameObject root = FindInScene(target, TargetRootName);
            if (root == null)
            {
                Debug.LogError("[EnergySphereTransfer] EnergySphereRoot not found in EnergySphereScene.");
                return;
            }

            Renderer ringRenderer = FindChildRenderer(root, "Multi Layer Ring Quad");
            if (ringRenderer == null)
            {
                Debug.LogError("[EnergySphereTransfer] Current Multi Layer Ring Quad not found under EnergySphereRoot.");
                return;
            }

            RemoveChildIfPresent(root.transform, "InnerEnergyMesh");
            RemoveChildIfPresent(root.transform, "RayBurstSystem");
            RemoveChildIfPresent(root.transform, "HotspotSystem");
            RemoveChildIfPresent(root.transform, "SparkParticleSystem");
            RemoveChildIfPresent(root.transform, "EnergyBloomAtmosphere");

            Renderer innerRenderer = CreateInnerMesh(root, TargetFallbackRingRadius);
            InnerEnergyMesh innerMesh = innerRenderer != null ? innerRenderer.GetComponent<InnerEnergyMesh>() : null;

            EnergySphereController controller = root.GetComponent<EnergySphereController>();
            if (controller == null)
                controller = root.AddComponent<EnergySphereController>();
            ConfigurePreviousController(controller, root.transform, innerRenderer, innerMesh);

            NeonRingEnergyAnimator ringAnimator = root.GetComponent<NeonRingEnergyAnimator>();
            if (ringAnimator == null)
                ringAnimator = root.AddComponent<NeonRingEnergyAnimator>();
            ringAnimator.ringRenderer = ringRenderer;
            ringAnimator.controller = controller;
            ringAnimator.baseRadius = TargetFallbackRingRadius;
            ringAnimator.baseWarmAngle = 0.16f;

            EnergyRayBurstSystem rays = CreateRayBursts(root, controller, TargetFallbackRingRadius);
            EnergyHotspotSystem hotspots = CreateHotspots(root, controller, rays, TargetFallbackRingRadius);
            EnergySparkSystem sparks = CreateSparks(root, controller, TargetFallbackRingRadius);
            EnergyAtmosphereParticles atmosphere = CreateAtmosphere(root, controller, TargetFallbackRingRadius);

            rays.EnsureInitialized();
            hotspots.EnsureInitialized();
            sparks.EnsureInitialized();
            atmosphere.EnsureInitialized();

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(ringAnimator);
            EditorSceneManager.MarkSceneDirty(target);
            EditorSceneManager.SaveScene(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[EnergySphereTransfer] Applied previous EnergySphereScene animation/effects to current Neon ring.");
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Apply Previous Sphere Animation + Play Mode Capture")]
        public static void ApplyPreviousAnimationAndPlayModeCapture()
        {
            ApplyPreviousEnergySphereAnimation();
            StartPlayModeCapture();
        }

        private static void StartPlayModeCapture()
        {
            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

            playModeFrameCount = 0;
            SessionState.SetBool(PlayCapturePendingKey, true);
            SessionState.SetBool(ExitAfterPlayCaptureKey, false);
            StoreAndSetFastPlayModeOptions();
            EditorApplication.playModeStateChanged -= HandlePlayModeCaptureState;
            EditorApplication.playModeStateChanged += HandlePlayModeCaptureState;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Capture Target To ref/current.png")]
        public static void CaptureTargetToCurrent()
        {
            if (SceneManager.GetActiveScene().path != TargetScenePath)
                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

            Camera cam = Object.FindObjectOfType<Camera>();
            if (cam == null)
            {
                Debug.LogError("[EnergySphereTransfer] No camera found in EnergySphereScene.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            RenderToFile(cam, OutputPath, CaptureWidth, CaptureHeight);
            Debug.Log("[EnergySphereTransfer] Capture saved: " + OutputPath);
        }

        private static IEnumerable<GameObject> FindSourceSphereRoots(Scene scene)
        {
            List<GameObject> matched = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .Where(go => go.activeInHierarchy)
                .Where(go => !IsCameraOrEnvironment(go))
                .Where(go => IsSphereEffectName(go.name) || HasOwnSphereMaterial(go))
                .ToList();

            foreach (GameObject go in matched)
            {
                if (!HasMatchedAncestor(go, matched))
                    yield return go;
            }
        }

        private static bool IsCameraOrEnvironment(GameObject go)
        {
            string n = go.name.ToLowerInvariant();
            if (go.GetComponent<Camera>() != null)
                return true;

            return n.Contains("camera")
                || n.Contains("volume")
                || n.Contains("backdrop")
                || n.Contains("background")
                || n.Contains("mountain")
                || n.Contains("sky")
                || n.Contains("water")
                || n.Contains("reflection")
                || n.Contains("mist")
                || n.Contains("cloud")
                || n.Contains("horizon")
                || n.Contains("valley")
                || n.Contains("star");
        }

        private static bool IsSphereEffectName(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("multi layer ring quad")
                || n.Contains("hdr energy ring")
                || n.StartsWith("impulse ray")
                || n.Contains("radial cyan sparks")
                || n.Contains("energy sphere")
                || n.Contains("sphere ring")
                || n.Contains("portal sphere")
                || n.Contains("neon sphere");
        }

        private static bool HasOwnSphereMaterial(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponents<Renderer>())
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null || mat.shader == null)
                        continue;

                    string shader = mat.shader.name.ToLowerInvariant();
                    string matName = mat.name.ToLowerInvariant();
                    if (shader.Contains("neon ring multi-layer")
                        || shader.Contains("neonportalring")
                        || shader.Contains("energyspherering")
                        || matName.Contains("neonring")
                        || matName.Contains("energy ring"))
                        return true;
                }
            }

            return false;
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
            if (playModeFrameCount < 4)
                return;

            PrepareCurrentEnergySpherePreview(2.85f);
            CaptureTargetToCurrent();
            SessionState.SetBool(PlayCapturePendingKey, false);
            SessionState.SetBool(ExitAfterPlayCaptureKey, true);
            EditorApplication.update -= PlayModeCaptureUpdate;
            EditorApplication.isPlaying = false;
        }

        private static void StoreAndSetFastPlayModeOptions()
        {
            SessionState.SetBool(HasPreviousPlayModeOptionsKey, true);
            SessionState.SetBool(PreviousPlayModeOptionsEnabledKey, EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(PreviousPlayModeOptionsKey, (int)EditorSettings.enterPlayModeOptions);

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        }

        private static void RestorePlayModeOptions()
        {
            if (!SessionState.GetBool(HasPreviousPlayModeOptionsKey, false))
                return;

            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(PreviousPlayModeOptionsEnabledKey, false);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(PreviousPlayModeOptionsKey, 0);
            SessionState.SetBool(HasPreviousPlayModeOptionsKey, false);
        }

        private static Renderer CreateInnerMesh(GameObject parent, float ringRadius)
        {
            GameObject go = new GameObject("InnerEnergyMesh");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = new Vector3(0.0f, 0.0f, -0.54f);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateLargeQuad(ringRadius * 2.15f);

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("AudioVisualizer/InnerEnergySphere");
            if (shader == null)
            {
                Debug.LogWarning("[EnergySphereTransfer] InnerEnergySphere shader not found.");
                return mr;
            }

            Material mat = new Material(shader);
            mat.renderQueue = 3020;
            mat.SetFloat("_RingCenterX", 0.0f);
            mat.SetFloat("_RingCenterY", 0.0f);
            mat.SetFloat("_InnerRadius", ringRadius * 0.965f);
            mat.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.10f, 1f));
            mat.SetFloat("_BaseIntensity", 0.10f);
            mat.SetFloat("_EdgeFade", 0.20f);
            mat.SetColor("_Layer1Color", new Color(0.0f, 0.55f, 1.40f, 1f));
            mat.SetFloat("_Layer1Speed", 0.18f);
            mat.SetFloat("_Layer1Freq", 3.0f);
            mat.SetFloat("_Layer1Intensity", 0.18f);
            mat.SetColor("_Layer2Color", new Color(0.65f, 0.04f, 1.20f, 1f));
            mat.SetFloat("_Layer2Speed", -0.15f);
            mat.SetFloat("_Layer2Freq", 5.5f);
            mat.SetFloat("_Layer2Intensity", 0.12f);
            mat.SetColor("_Layer3Color", new Color(1.10f, 0.08f, 0.75f, 1f));
            mat.SetFloat("_Layer3Speed", 0.11f);
            mat.SetFloat("_Layer3Freq", 8.0f);
            mat.SetFloat("_Layer3Intensity", 0.08f);
            mat.SetColor("_GridColor", new Color(0.05f, 0.58f, 1.25f, 1f));
            mat.SetFloat("_GridFreq", 16.0f);
            mat.SetFloat("_GridIntensity", 0.075f);
            mat.SetFloat("_DotIntensity", 0.78f);
            mat.SetFloat("_NoiseAmp", 0.14f);
            mat.SetFloat("_NoiseFreq", 5.5f);
            mat.SetFloat("_NoiseSpeed", 0.12f);
            mat.SetFloat("_PulseSpeed", 0.30f);
            mat.SetFloat("_PulseAmp", 0.12f);
            mat.SetFloat("_Exposure", 0.72f);

            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            InnerEnergyMesh mesh = go.AddComponent<InnerEnergyMesh>();
            mesh.targetRenderer = mr;
            mesh.radiusScale = 0.965f;
            mesh.exposure = 0.72f;
            mesh.gridIntensity = 0.075f;
            mesh.dotIntensity = 0.78f;

            EditorUtility.SetDirty(go);
            return mr;
        }

        private static EnergyRayBurstSystem CreateRayBursts(GameObject parent, EnergySphereController controller, float ringRadius)
        {
            GameObject go = new GameObject("RayBurstSystem");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;

            EnergyRayBurstSystem rays = go.AddComponent<EnergyRayBurstSystem>();
            rays.controller = controller;
            rays.rayCount = 34;
            rays.sphereRadius = ringRadius;
            rays.rayLengthMin = 0.45f;
            rays.rayLengthMax = 2.35f;
            rays.rayWidthMin = 0.004f;
            rays.rayWidthMax = 0.020f;
            rays.burstIntervalMin = 0.28f;
            rays.burstIntervalMax = 0.95f;
            rays.burstCountMin = 1;
            rays.burstCountMax = 3;
            rays.flashDuration = 1.10f;
            rays.maxIntensity = 5.6f;
            rays.longRayChance = 0.06f;
            return rays;
        }

        private static EnergyHotspotSystem CreateHotspots(GameObject parent, EnergySphereController controller, EnergyRayBurstSystem rays, float ringRadius)
        {
            GameObject go = new GameObject("HotspotSystem");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;

            EnergyHotspotSystem hotspots = go.AddComponent<EnergyHotspotSystem>();
            hotspots.controller = controller;
            hotspots.linkedRays = rays;
            hotspots.hotspotCount = 12;
            hotspots.sphereRadius = ringRadius;
            hotspots.hotspotSizeMin = 0.12f;
            hotspots.hotspotSizeMax = 0.34f;
            hotspots.hotspotIntensity = 6.0f;
            hotspots.flashDuration = 1.05f;
            hotspots.intervalMin = 0.45f;
            hotspots.intervalMax = 1.25f;
            hotspots.flashesPerEventMin = 1;
            hotspots.flashesPerEventMax = 2;
            return hotspots;
        }

        private static EnergySparkSystem CreateSparks(GameObject parent, EnergySphereController controller, float ringRadius)
        {
            GameObject go = new GameObject("SparkParticleSystem");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;

            EnergySparkSystem sparks = go.AddComponent<EnergySparkSystem>();
            sparks.controller = controller;
            sparks.maxParticles = 900;
            sparks.emissionRate = 135f;
            sparks.sphereRadius = ringRadius;
            sparks.sizeMin = 0.014f;
            sparks.sizeMax = 0.075f;
            sparks.lifetimeMin = 1.65f;
            sparks.lifetimeMax = 4.20f;
            sparks.speedMin = 0.035f;
            sparks.speedMax = 0.38f;
            sparks.ringSpawnChance = 0.64f;
            sparks.innerSpawnChance = 0.28f;
            sparks.outsideJitter = 0.14f;
            sparks.radialBias = 0.65f;
            sparks.tangentialBias = 0.42f;
            sparks.turbulence = 0.06f;
            return sparks;
        }

        private static EnergyAtmosphereParticles CreateAtmosphere(GameObject parent, EnergySphereController controller, float ringRadius)
        {
            GameObject go = new GameObject("EnergyBloomAtmosphere");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;

            EnergyAtmosphereParticles atmosphere = go.AddComponent<EnergyAtmosphereParticles>();
            atmosphere.controller = controller;
            atmosphere.maxParticles = 220;
            atmosphere.emissionRate = 22f;
            atmosphere.sphereRadius = ringRadius;
            atmosphere.spreadRadius = 3.05f;
            atmosphere.sizeMin = 0.040f;
            atmosphere.sizeMax = 0.170f;
            return atmosphere;
        }

        private static void ConfigurePreviousController(EnergySphereController ctrl, Transform root, Renderer inner, InnerEnergyMesh innerMesh)
        {
            ctrl.ringRenderer = null;
            ctrl.innerRenderer = inner;
            ctrl.innerMesh = innerMesh;
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
            ctrl.ringRadius = TargetFallbackRingRadius;
            ctrl.ringPulseAmp = 0.018f;
            ctrl.ringPulseSpeed = 0.55f;
            ctrl.ringNoiseAmp = 0.018f;
            ctrl.ringNoiseSpeed = 0.18f;
            ctrl.globalPulseSpeed = 0.30f;
            ctrl.globalPulseAmp = 0.08f;
            ctrl.corePulseAmp = 0.18f;
            ctrl.warmAngleDriftSpeed = 0.10f;
            ctrl.warmAngleDriftAmp = 0.18f;
            ctrl.baseWarmAngle = 0.28f;
            ctrl.hotspotIntensityBase = 6.2f;
            ctrl.hotspotCycleSpeed = 0.48f;
            ctrl.hotspotWidth = 0.085f;
            ctrl.masterIntensity = 1.0f;
            ctrl.intensityBreathSpeed = 0.30f;
            ctrl.intensityBreathAmp = 0.06f;
        }

        private static void PrepareCurrentEnergySpherePreview(float time)
        {
            foreach (EnergySphereController controller in Object.FindObjectsOfType<EnergySphereController>())
                controller.ApplyState(time);

            foreach (NeonRingEnergyAnimator animator in Object.FindObjectsOfType<NeonRingEnergyAnimator>())
                animator.ApplyState(time);

            foreach (EnergyRayBurstSystem rays in Object.FindObjectsOfType<EnergyRayBurstSystem>())
                rays.PreviewAtTime(time);

            foreach (EnergyHotspotSystem hotspots in Object.FindObjectsOfType<EnergyHotspotSystem>())
                hotspots.PreviewAtTime(time);

            foreach (EnergySparkSystem sparks in Object.FindObjectsOfType<EnergySparkSystem>())
                sparks.PreviewAtTime(time);

            foreach (EnergyAtmosphereParticles atmosphere in Object.FindObjectsOfType<EnergyAtmosphereParticles>())
                atmosphere.PreviewAtTime(time);

            SceneView.RepaintAll();
        }

        private static Mesh CreateLargeQuad(float size)
        {
            float h = size * 0.5f;
            Mesh mesh = new Mesh();
            mesh.name = "LargeQuad";
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

        private static Renderer FindChildRenderer(GameObject root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child.GetComponent<Renderer>();
            }

            return null;
        }

        private static void RemoveChildIfPresent(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        private static bool HasMatchedAncestor(GameObject go, List<GameObject> matched)
        {
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                if (matched.Contains(parent.gameObject))
                    return true;

                parent = parent.parent;
            }

            return false;
        }

        private static Vector3 DetectSourceCenter(List<GameObject> roots)
        {
            foreach (Renderer renderer in roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)))
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null || !mat.HasProperty("_RingCenterX") || !mat.HasProperty("_RingCenterY"))
                        continue;

                    return new Vector3(mat.GetFloat("_RingCenterX"), mat.GetFloat("_RingCenterY"), 0.0f);
                }
            }

            GameObject ring = roots.FirstOrDefault(r => r.name == "Multi Layer Ring Quad");
            return ring != null ? new Vector3(ring.transform.position.x, ring.transform.position.y, 0.0f) : Vector3.zero;
        }

        private static float DetectTargetRingRadius(GameObject oldRoot)
        {
            EnergySphereController controller = oldRoot.GetComponentInChildren<EnergySphereController>(true);
            if (controller != null && controller.ringRadius > 0.0f)
                return controller.ringRadius;

            // If this tool is re-run after a previous transfer, the old
            // procedural controller is already gone. Keep the original
            // EnergySphereScene composition instead of reusing the larger
            // NeonPortalScene radius.
            if (oldRoot.transform.Find("Multi Layer Ring Quad") != null)
                return TargetFallbackRingRadius;

            foreach (Renderer renderer in oldRoot.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null || !mat.HasProperty("_RingRadius"))
                        continue;

                    string shader = mat.shader != null ? mat.shader.name : string.Empty;
                    if (!shader.Contains("Neon Ring Multi-Layer"))
                        return mat.GetFloat("_RingRadius");
                }
            }

            return TargetFallbackRingRadius;
        }

        private static void RetargetWorldSpaceRingMaterials(GameObject root, Vector3 targetWorldPosition, float targetRingRadius)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null || !mat.HasProperty("_RingCenterX") || !mat.HasProperty("_RingCenterY"))
                        continue;

                    Material copy = Object.Instantiate(mat);
                    copy.name = mat.name + "_EnergySphereScene";
                    copy.SetFloat("_RingCenterX", targetWorldPosition.x);
                    copy.SetFloat("_RingCenterY", targetWorldPosition.y);
                    if (copy.HasProperty("_RingRadius") && targetRingRadius > 0.0f)
                        copy.SetFloat("_RingRadius", targetRingRadius);

                    mats[i] = copy;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = mats;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform direct = root.transform.Find(name);
                if (root.name == name)
                    return root;
                if (direct != null)
                    return direct.gameObject;

                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                        return child.gameObject;
                }
            }

            return null;
        }

        private static void RenderToFile(Camera cam, string path, int w, int h)
        {
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
    }
}
#endif
