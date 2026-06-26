#if UNITY_EDITOR
using System.IO;
using Ediskrad.AudioVisualizer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Ediskrad.AudioVisualizer.Editor
{
    [InitializeOnLoad]
    public static class NeonPortalProjectBootstrap
    {
        private const string ScenePath = "Assets/Scenes/NeonPortalScene.unity";
        private const string BootstrapKey = "Ediskrad.AudioVisualizer.NeonPortalBootstrap.v1";

        static NeonPortalProjectBootstrap()
        {
            EditorApplication.delayCall += AutoBootstrap;
        }

        [MenuItem("Tools/AudioVisualizer/Rebuild Neon Portal Scene")]
        public static void RebuildScene()
        {
            EnsureFolders();
            ConfigureAndroidProject();
            BuildScene();
            SessionState.SetBool(BootstrapKey, true);
        }

        [MenuItem("Tools/AudioVisualizer/Fix URP Pink Materials")]
        public static void FixUrpPinkMaterials()
        {
            EnsureFolders();
            ConfigureAndroidProject();
            RebuildCompatibleMaterials();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("AudioVisualizer: compatible shaders assigned. If Game view was pink, wait for shader reload or reopen NeonPortalScene.");
        }

        [MenuItem("Tools/AudioVisualizer/Run Automated Smoke Test")]
        public static void RunAutomatedSmokeTest()
        {
            EnsureFolders();
            ConfigureAndroidProject();
            RebuildCompatibleMaterials();
            BuildScene();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = Object.FindObjectOfType<Camera>();
            bool hasCamera = camera != null;
            bool hasRing = GameObject.Find("HDR Energy Ring") != null;
            bool hasDisk = GameObject.Find("Light Absorbing Portal Disk") != null;
            bool hasParticles = Object.FindObjectsOfType<ParticleSystem>().Length >= 4;
            bool shadersSupported = AreSceneShadersSupported();

            string screenshotPath = "ref/automated_smoke.png";
            Directory.CreateDirectory("ref");
            bool renderLooksValid = hasCamera && RenderValidationScreenshot(camera, screenshotPath);

            string reportPath = "Logs/NeonPortalSmokeTest.json";
            Directory.CreateDirectory("Logs");
            string json =
                "{\n" +
                $"  \"scene\": \"{scene.path}\",\n" +
                $"  \"hasCamera\": {hasCamera.ToString().ToLowerInvariant()},\n" +
                $"  \"hasRing\": {hasRing.ToString().ToLowerInvariant()},\n" +
                $"  \"hasDisk\": {hasDisk.ToString().ToLowerInvariant()},\n" +
                $"  \"hasParticles\": {hasParticles.ToString().ToLowerInvariant()},\n" +
                $"  \"shadersSupported\": {shadersSupported.ToString().ToLowerInvariant()},\n" +
                $"  \"renderLooksValid\": {renderLooksValid.ToString().ToLowerInvariant()},\n" +
                $"  \"screenshot\": \"{screenshotPath.Replace("\\", "/")}\"\n" +
                "}\n";
            File.WriteAllText(reportPath, json);

            bool sceneContentValid = hasRing && hasDisk && hasParticles;
            if (!hasCamera || !sceneContentValid || !shadersSupported || !renderLooksValid)
            {
                Debug.LogError($"AudioVisualizer smoke test failed. See {reportPath}");
            }
            else
            {
                Debug.Log($"AudioVisualizer smoke test passed. Screenshot: {screenshotPath}, report: {reportPath}");
            }
        }

        [MenuItem("Tools/AudioVisualizer/Build Reference Video Match Scene")]
        public static void BuildReferenceVideoMatchScene()
        {
            EnsureFolders();
            ConfigureAndroidProject();
            BuildScene();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("AudioVisualizer: NeonPortalScene rebuilt with procedural objects.");
        }

        private static void AutoBootstrap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EnsureFolders();
            ConfigureAndroidProject();
            if (!File.Exists(ScenePath))
            {
                BuildScene();
                SessionState.SetBool(BootstrapKey, true);
            }
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Scenes", "Assets/Materials", "Assets/Shaders", "Assets/VFX", "Assets/Particles",
                "Assets/Settings",
                "Assets/Scripts", "Assets/Textures", "Assets/Prefabs", "Assets/Editor"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        private static void ConfigureAndroidProject()
        {
            PlayerSettings.companyName = "Ediskrad";
            PlayerSettings.productName = "AudioVisualizer Neon Portal";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.ediskrad.audiovisualizer");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.MTRendering = true;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        }

        private static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "NeonPortalScene";

            Material ringMaterial = CreateMaterial("M_NeonRing", Shader.Find("AudioVisualizer/Neon Portal Ring Additive"));
            ringMaterial.SetColor("_ColorA", new Color(0.0f, 2.9f, 9.0f, 1f));
            ringMaterial.SetColor("_ColorB", new Color(6.4f, 0.0f, 6.1f, 1f));
            ringMaterial.SetFloat("_Intensity", 2.45f);
            ringMaterial.SetFloat("_NoiseStrength", 0.32f);

            Material coronaMaterial = CreateMaterial("M_PlasmaCorona", Shader.Find("AudioVisualizer/Neon Portal Ring Additive"));
            coronaMaterial.SetColor("_ColorA", new Color(0.0f, 1.4f, 5.5f, 1f));
            coronaMaterial.SetColor("_ColorB", new Color(7.0f, 0.0f, 6.0f, 1f));
            coronaMaterial.SetFloat("_Intensity", 0.45f);
            coronaMaterial.SetFloat("_NoiseStrength", 0.35f);

            Material mistMaterial = CreateMaterial("M_VioletMist", Shader.Find("AudioVisualizer/Neon Mist Additive"));
            mistMaterial.SetColor("_Color", new Color(0.9f, 0.0f, 2.6f, 0.45f));

            Material rayMaterial = CreateMaterial("M_RadialRay", Shader.Find("AudioVisualizer/Neon Mist Additive"));
            rayMaterial.SetColor("_Color", new Color(0.35f, 0.05f, 2.8f, 0.35f));
            rayMaterial.SetFloat("_Softness", 4.5f);

            Material waterMaterial = CreateMaterial("M_WaterReflection", Shader.Find("AudioVisualizer/Water Reflection Additive"));
            Material blackMaterial = CreateMaterial("M_LightAbsorbingBlack", Shader.Find("AudioVisualizer/Portal Black Occluder"));
            blackMaterial.SetColor("_Color", new Color(0f, 0f, 0.001f, 1f));
            blackMaterial.renderQueue = (int)RenderQueue.Transparent + 90;
            Material mountainMaterial = CreateColorMaterial("M_MountainSilhouette", new Color(0.005f, 0.006f, 0.025f, 1f), false);
            Material starMaterial = CreateColorMaterial("M_AdditiveParticles", new Color(0.25f, 0.75f, 1.0f, 1f), true);

            GameObject root = new GameObject("Neon Portal Scene Root");
            GameObject cameraObject = new GameObject("Camera 9x16 HDR");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.001f, 0.001f, 0.008f, 1f);
            camera.orthographic = true;
            camera.allowHDR = true;
            camera.orthographicSize = 8.8f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<CameraCinematicMotion>();
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;

            GameObject volumeObject = new GameObject("Cinematic Post Process Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = CreateVolumeProfile();

            GameObject backgroundGlow = CreateQuad("Blue Violet Background Glow", new Vector3(0f, 1.2f, 3f), new Vector3(10.5f, 11f, 1f), mistMaterial, root.transform);
            backgroundGlow.GetComponent<Renderer>().material.SetFloat("_Intensity", 0.22f);

            GameObject disk = CreateMeshObject("Light Absorbing Portal Disk", CreateCircleMesh(2.91f, 192), blackMaterial, root.transform);
            disk.transform.position = new Vector3(0f, 0.25f, -0.55f);

            GameObject ring = CreateMeshObject("HDR Energy Ring", CreateAnnulusMesh(2.97f, 3.12f, 256), ringMaterial, root.transform);
            ring.transform.position = new Vector3(0f, 0.25f, -0.6f);

            GameObject corona = CreateMeshObject("Procedural Plasma Corona", CreateAnnulusMesh(3.12f, 3.48f, 256, 0.18f), coronaMaterial, root.transform);
            corona.transform.position = new Vector3(0f, 0.25f, -0.65f);

            Transform[] orbitPoints = CreateOrbitingPoints(root.transform, starMaterial, 16);
            CreateRayBursts(root.transform, rayMaterial);
            CreateMistLayers(root.transform, mistMaterial);
            CreateMountainsAndWater(root.transform, mountainMaterial, waterMaterial, mistMaterial);
            ParticleSystem[] particles = CreateParticleSystems(root.transform, starMaterial);

            Light leftLight = CreateAccentLight("Cyan Rim Light", new Vector3(-3f, 1.4f, -3f), new Color(0f, 0.7f, 1f));
            Light rightLight = CreateAccentLight("Magenta Rim Light", new Vector3(3f, 0.7f, -3f), new Color(1f, 0f, 0.85f));

            PortalPulseController pulse = root.AddComponent<PortalPulseController>();
            pulse.emissionRenderers = new[] { ring.GetComponent<Renderer>(), corona.GetComponent<Renderer>() };
            pulse.accentLights = new[] { leftLight, rightLight };
            pulse.postProcessVolume = volume;
            pulse.baseEmission = 1.45f;
            pulse.pulseEmission = 0.75f;
            pulse.baseBloomIntensity = 0.72f;
            pulse.pulseBloomIntensity = 0.35f;

            RingEnergyFlow flow = root.AddComponent<RingEnergyFlow>();
            flow.flowRenderers = pulse.emissionRenderers;
            flow.orbitingEnergyPoints = orbitPoints;
            flow.orbitRadius = 3.26f;

            ParticleLODController lod = root.AddComponent<ParticleLODController>();
            lod.particleSystems = particles;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static VolumeProfile CreateVolumeProfile()
        {
            const string path = "Assets/Materials/VP_NeonPortal.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            Bloom bloom = GetOrAdd<Bloom>(profile);
            bloom.active = true;
            bloom.intensity.Override(1.25f);
            bloom.threshold.Override(0.72f);
            bloom.scatter.Override(0.72f);

            Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(-0.15f);
            color.contrast.Override(24f);
            color.saturation.Override(14f);
            color.colorFilter.Override(new Color(0.72f, 0.82f, 1f));

            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.62f);

            ChromaticAberration aberration = GetOrAdd<ChromaticAberration>(profile);
            aberration.intensity.Override(0.035f);

            return profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            return profile.TryGet(out T component) ? component : profile.Add<T>();
        }

        private static Material CreateMaterial(string name, Shader shader)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }
            return material;
        }

        private static Material CreateColorMaterial(string name, Color color, bool additive)
        {
            Material material = CreateMaterial(name, Shader.Find(additive ? "Legacy Shaders/Particles/Additive" : "Unlit/Color"));
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 2.5f);
            if (additive)
            {
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            return material;
        }

        private static void RebuildCompatibleMaterials()
        {
            CreateMaterial("M_NeonRing", Shader.Find("AudioVisualizer/Neon Portal Ring Additive"));
            CreateMaterial("M_PlasmaCorona", Shader.Find("AudioVisualizer/Neon Portal Ring Additive"));
            CreateMaterial("M_VioletMist", Shader.Find("AudioVisualizer/Neon Mist Additive"));
            CreateMaterial("M_RadialRay", Shader.Find("AudioVisualizer/Neon Mist Additive"));
            CreateMaterial("M_WaterReflection", Shader.Find("AudioVisualizer/Water Reflection Additive"));
            CreateColorMaterial("M_AdditiveParticles", new Color(0.25f, 0.75f, 1.0f, 1f), true);
            Material blackMaterial = CreateMaterial("M_LightAbsorbingBlack", Shader.Find("AudioVisualizer/Portal Black Occluder"));
            blackMaterial.SetColor("_Color", new Color(0f, 0f, 0.001f, 1f));
            blackMaterial.renderQueue = (int)RenderQueue.Transparent + 90;
            CreateColorMaterial("M_MountainSilhouette", new Color(0.005f, 0.006f, 0.025f, 1f), false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool AreSceneShadersSupported()
        {
            Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || !material.shader.isSupported)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool RenderValidationScreenshot(Camera camera, string path)
        {
            const int width = 576;
            const int height = 880;
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2
            };

            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(path, png);

            Color32[] pixels = texture.GetPixels32();
            int bright = 0;
            int unityMagenta = 0;
            for (int i = 0; i < pixels.Length; i += 17)
            {
                Color32 pixel = pixels[i];
                if (pixel.r > 35 || pixel.g > 35 || pixel.b > 35)
                {
                    bright++;
                }

                if (pixel.r > 220 && pixel.g < 45 && pixel.b > 180)
                {
                    unityMagenta++;
                }
            }

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);

            float magentaRatio = unityMagenta / Mathf.Max(1f, pixels.Length / 17f);
            return bright > 40 && magentaRatio < 0.04f;
        }

        private static GameObject CreateMeshObject(string name, Mesh mesh, Material material, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            return obj;
        }

        private static GameObject CreateQuad(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        private static Mesh CreateCircleMesh(float radius, int segments)
        {
            Vector3[] vertices = new Vector3[segments + 1];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                uv[i + 1] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i == segments - 1 ? 1 : i + 2;
            }
            return FinalizeMesh("Circle", vertices, uv, triangles);
        }

        private static Mesh CreateAnnulusMesh(float innerRadius, float outerRadius, int segments, float irregularity = 0f)
        {
            Vector3[] vertices = new Vector3[segments * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                float angle = t * Mathf.PI * 2f;
                float noise = 1f + irregularity * (Mathf.Sin(t * 59f) * 0.5f + Mathf.Sin(t * 131f) * 0.28f);
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices[i * 2] = dir * innerRadius;
                vertices[i * 2 + 1] = dir * outerRadius * noise;
                uv[i * 2] = new Vector2(t, 0f);
                uv[i * 2 + 1] = new Vector2(t, 1f);

                int next = (i + 1) % segments;
                int tri = i * 6;
                triangles[tri] = i * 2;
                triangles[tri + 1] = next * 2;
                triangles[tri + 2] = i * 2 + 1;
                triangles[tri + 3] = i * 2 + 1;
                triangles[tri + 4] = next * 2;
                triangles[tri + 5] = next * 2 + 1;
            }
            return FinalizeMesh("Annulus", vertices, uv, triangles);
        }

        private static Mesh FinalizeMesh(string name, Vector3[] vertices, Vector2[] uv, int[] triangles)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Transform[] CreateOrbitingPoints(Transform parent, Material material, int count)
        {
            Transform[] points = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                GameObject point = CreateQuad($"Orbiting Plasma Point {i:00}", new Vector3(0f, 1.15f, -0.12f), Vector3.one * 0.14f, material, parent);
                points[i] = point.transform;
            }
            return points;
        }

        private static void CreateRayBursts(Transform parent, Material material)
        {
            GameObject host = new GameObject("Random Radial Light Shafts");
            host.transform.SetParent(parent, false);
            host.transform.position = new Vector3(0f, 1.15f, -0.25f);
            RadialBurstSpawner spawner = host.AddComponent<RadialBurstSpawner>();
            float[] angles = { 90f, 125f, 55f, 270f, 160f, 25f };
            foreach (float angle in angles)
            {
                GameObject ray = CreateQuad($"Impulse Ray {angle:000}", Vector3.zero, new Vector3(0.42f, 5.4f, 1f), material, host.transform);
                ray.transform.localPosition = Quaternion.Euler(0f, 0f, angle) * Vector3.up * 3.1f;
                ray.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                spawner.bursts.Add(new RadialBurstSpawner.RayBurst
                {
                    renderer = ray.GetComponent<Renderer>(),
                    transform = ray.transform,
                    angle = angle
                });
            }
        }

        private static void CreateMistLayers(Transform parent, Material material)
        {
            for (int i = 0; i < 8; i++)
            {
                float x = Mathf.Lerp(-4.5f, 4.5f, i / 7f);
                GameObject mist = CreateQuad($"Animated Volumetric Mist {i:00}", new Vector3(x, -4.4f + Mathf.Sin(i) * 0.35f, -0.35f), new Vector3(2.8f, 1.2f, 1f), material, parent);
                mist.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-22f, 22f, i / 7f));
            }
        }

        private static void CreateMountainsAndWater(Transform parent, Material mountainMaterial, Material waterMaterial, Material mistMaterial)
        {
            GameObject mountains = CreateMeshObject("Dark Mountain Silhouettes", CreateMountainMesh(), mountainMaterial, parent);
            mountains.transform.position = new Vector3(0f, -6.6f, -0.05f);

            GameObject water = CreateQuad("Purple Blue Water Reflection", new Vector3(0f, -6.55f, -0.2f), new Vector3(2.45f, 2.45f, 1f), waterMaterial, parent);
            WaterReflectionAnimator animator = water.AddComponent<WaterReflectionAnimator>();
            animator.reflectionRenderer = water.GetComponent<Renderer>();
            animator.intensity = 0.34f;

            GameObject horizonGlow = CreateQuad("Low Horizon Glow", new Vector3(0f, -5.55f, -0.3f), new Vector3(6.8f, 1.2f, 1f), mistMaterial, parent);
            horizonGlow.GetComponent<Renderer>().sharedMaterial.SetFloat("_Intensity", 0.46f);
        }

        private static Mesh CreateMountainMesh()
        {
            int peaks = 28;
            Vector3[] vertices = new Vector3[peaks * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(peaks - 1) * 6];
            for (int i = 0; i < peaks; i++)
            {
                float t = i / (float)(peaks - 1);
                float x = Mathf.Lerp(-5.8f, 5.8f, t);
                float height = Mathf.PerlinNoise(t * 7.5f, 0.22f) * 1.7f + Mathf.Sin(t * Mathf.PI * 4f) * 0.28f;
                vertices[i * 2] = new Vector3(x, -1.5f, 0f);
                vertices[i * 2 + 1] = new Vector3(x, height, 0f);
                uv[i * 2] = new Vector2(t, 0f);
                uv[i * 2 + 1] = new Vector2(t, 1f);
                if (i < peaks - 1)
                {
                    int tri = i * 6;
                    triangles[tri] = i * 2;
                    triangles[tri + 1] = i * 2 + 1;
                    triangles[tri + 2] = i * 2 + 2;
                    triangles[tri + 3] = i * 2 + 1;
                    triangles[tri + 4] = i * 2 + 3;
                    triangles[tri + 5] = i * 2 + 2;
                }
            }
            return FinalizeMesh("Mountains", vertices, uv, triangles);
        }

        private static ParticleSystem[] CreateParticleSystems(Transform parent, Material material)
        {
            return new[]
            {
                CreateParticles("Star Dust Field", parent, material, 9000, 720f, 11f, new Vector3(0f, 0.1f, 1.2f), new Vector3(11.3f, 16.2f, 1f), 0.006f),
                CreateParticles("Radial Cyan Sparks", parent, material, 3300, 390f, 2.3f, new Vector3(0f, 0.25f, -0.15f), new Vector3(6.6f, 6.6f, 1f), 0.021f),
                CreateParticles("Magenta Plasma Dust", parent, material, 4300, 460f, 3.1f, new Vector3(0f, 0.25f, -0.18f), new Vector3(7.4f, 7.4f, 1f), 0.026f),
                CreateParticles("Rare Large Flares", parent, material, 900, 30f, 1.4f, new Vector3(0f, 0.25f, -0.22f), new Vector3(6.7f, 6.7f, 1f), 0.07f)
            };
        }

        private static ParticleSystem CreateParticles(string name, Transform parent, Material material, int maxParticles, float rate, float speed, Vector3 position, Vector3 shapeScale, float size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 3.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.35f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.35f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.1f, 0.8f, 1f, 1f), new Color(1f, 0.05f, 0.95f, 1f));
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 3.05f;
            shape.radiusThickness = 0.06f;
            shape.scale = shapeScale;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.radial = new ParticleSystem.MinMaxCurve(speed * 0.25f, speed);

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0f, 0.8f, 1f), 0.35f), new GradientColorKey(new Color(1f, 0f, 0.9f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = 1f;
            ps.Play();
            return ps;
        }

        private static Light CreateAccentLight(string name, Vector3 position, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;
            Light light = obj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 8f;
            light.intensity = 1.4f;
            return light;
        }
    }
}
#endif
