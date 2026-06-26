using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ediskrad.AudioVisualizer
{
    public static class EnergySphereSceneBuilder
    {
        private static readonly Vector3 SphereCenter = Vector3.zero;
        private const float RingRadius = 2.0f;
        private const float RingZ = -0.60f;
        private const float InnerZ = -0.54f;

        public static void BuildScene()
        {
            GameObject root = new GameObject("EnergySphereRoot");
            root.transform.position = SphereCenter;

            Camera cam = SetupCamera();
            SetupPostProcessing(cam);
            SetupLighting();

            Renderer ring = CreateRingQuad(root);
            Renderer inner = CreateInnerMesh(root);
            EnergyRayBurstSystem rays = CreateRayBursts(root);
            EnergyHotspotSystem hotspots = CreateHotspots(root, rays);
            EnergySparkSystem sparks = CreateSparks(root);
            EnergyAtmosphereParticles atmosphere = CreateAtmosphere(root);

            SetupController(root, ring, inner);

            rays.EnsureInitialized();
            hotspots.EnsureInitialized();
            sparks.EnsureInitialized();
            atmosphere.EnsureInitialized();
        }

        private static Camera SetupCamera()
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            Camera cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.62f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.002f, 0.001f, 0.010f, 1f);
            cam.allowHDR = true;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 50f;
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            UniversalAdditionalCameraData camData = camGO.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.volumeTrigger = camGO.transform;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            return cam;
        }

        private static void SetupPostProcessing(Camera cam)
        {
            GameObject volGO = new GameObject("PostProcess Volume");
            Volume vol = volGO.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

            Bloom bloom = profile.Add<Bloom>();
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 2.8f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.32f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.64f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(0.82f, 0.86f, 1.0f);

            Tonemapping tonemap = profile.Add<Tonemapping>();
            tonemap.mode.overrideState = true;
            tonemap.mode.value = TonemappingMode.ACES;

            ColorAdjustments colorAdj = profile.Add<ColorAdjustments>();
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.05f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 10f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 18f;
            colorAdj.colorFilter.overrideState = true;
            colorAdj.colorFilter.value = new Color(0.92f, 0.94f, 1.0f);

            Vignette vignette = profile.Add<Vignette>();
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.18f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.72f;

            ChromaticAberration ca = profile.Add<ChromaticAberration>();
            ca.intensity.overrideState = true;
            ca.intensity.value = 0.045f;

            vol.sharedProfile = profile;
        }

        private static void SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.004f, 0.003f, 0.018f);
        }

        private static Renderer CreateRingQuad(GameObject parent)
        {
            GameObject go = new GameObject("ProceduralSphereRingShader");
            go.transform.SetParent(parent.transform, false);
            go.transform.position = new Vector3(SphereCenter.x, SphereCenter.y, RingZ);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateLargeQuad(14f);

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("AudioVisualizer/EnergySphereRing");
            if (shader == null)
            {
                Debug.LogError("[EnergySphereScene] EnergySphereRing shader not found");
                return mr;
            }

            Material mat = new Material(shader);
            mat.renderQueue = 3040;
            mat.SetFloat("_RingCenterX", SphereCenter.x);
            mat.SetFloat("_RingCenterY", SphereCenter.y);
            mat.SetFloat("_RingRadius", RingRadius);
            mat.SetFloat("_Thickness", 0.020f);
            mat.SetFloat("_CoreWidth", 0.0065f);
            mat.SetFloat("_GlowWidth", 0.066f);
            mat.SetFloat("_OuterHaloWidth", 0.27f);
            mat.SetFloat("_AtmosWidth", 0.90f);
            mat.SetFloat("_CoreIntensity", 15.5f);
            mat.SetFloat("_GlowIntensity", 3.7f);
            mat.SetFloat("_HaloIntensity", 0.72f);
            mat.SetFloat("_AtmosIntensity", 0.10f);
            mat.SetColor("_CoolColor", new Color(0.00f, 0.82f, 1.60f, 1f));
            mat.SetColor("_BlueColor", new Color(0.02f, 0.05f, 1.42f, 1f));
            mat.SetColor("_VioletColor", new Color(0.45f, 0.04f, 1.20f, 1f));
            mat.SetColor("_WarmColor", new Color(1.60f, 0.08f, 0.82f, 1f));
            mat.SetColor("_CoreColor", new Color(1.0f, 0.94f, 1.0f, 1f));
            mat.SetFloat("_WarmAngle", 0.28f);
            mat.SetFloat("_WarmSharpness", 1.65f);
            mat.SetFloat("_AngleStrength", 0.88f);
            mat.SetFloat("_PulseAmp", 0.026f);
            mat.SetFloat("_PulseSpeed", 1.15f);
            mat.SetFloat("_NoiseAmp", 0.018f);
            mat.SetFloat("_NoiseFreq", 5.0f);
            mat.SetFloat("_NoiseSpeed", 0.32f);
            mat.SetFloat("_HotspotWidth", 0.085f);
            mat.SetFloat("_HotspotIntensity", 8.0f);
            mat.SetFloat("_InnerDimRadius", 0.72f);
            mat.SetFloat("_AlphaFalloff", 5.8f);
            mat.SetFloat("_Exposure", 1.0f);

            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        private static Renderer CreateInnerMesh(GameObject parent)
        {
            GameObject go = new GameObject("InnerEnergyMesh");
            go.transform.SetParent(parent.transform, false);
            go.transform.position = new Vector3(SphereCenter.x, SphereCenter.y, InnerZ);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateLargeQuad(RingRadius * 2.15f);

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("AudioVisualizer/InnerEnergySphere");
            if (shader == null)
            {
                Debug.LogError("[EnergySphereScene] InnerEnergySphere shader not found");
                return mr;
            }

            Material mat = new Material(shader);
            mat.renderQueue = 3020;
            mat.SetFloat("_RingCenterX", SphereCenter.x);
            mat.SetFloat("_RingCenterY", SphereCenter.y);
            mat.SetFloat("_InnerRadius", RingRadius * 0.965f);
            mat.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.10f, 1f));
            mat.SetFloat("_BaseIntensity", 0.10f);
            mat.SetFloat("_EdgeFade", 0.20f);
            mat.SetColor("_Layer1Color", new Color(0.0f, 0.55f, 1.40f, 1f));
            mat.SetFloat("_Layer1Speed", 0.28f);
            mat.SetFloat("_Layer1Freq", 3.0f);
            mat.SetFloat("_Layer1Intensity", 0.18f);
            mat.SetColor("_Layer2Color", new Color(0.65f, 0.04f, 1.20f, 1f));
            mat.SetFloat("_Layer2Speed", -0.22f);
            mat.SetFloat("_Layer2Freq", 5.5f);
            mat.SetFloat("_Layer2Intensity", 0.12f);
            mat.SetColor("_Layer3Color", new Color(1.10f, 0.08f, 0.75f, 1f));
            mat.SetFloat("_Layer3Speed", 0.17f);
            mat.SetFloat("_Layer3Freq", 8.0f);
            mat.SetFloat("_Layer3Intensity", 0.08f);
            mat.SetColor("_GridColor", new Color(0.05f, 0.58f, 1.25f, 1f));
            mat.SetFloat("_GridFreq", 16.0f);
            mat.SetFloat("_GridIntensity", 0.075f);
            mat.SetFloat("_DotIntensity", 0.78f);
            mat.SetFloat("_NoiseAmp", 0.14f);
            mat.SetFloat("_NoiseFreq", 5.5f);
            mat.SetFloat("_NoiseSpeed", 0.24f);
            mat.SetFloat("_PulseSpeed", 0.8f);
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

            return mr;
        }

        private static EnergyRayBurstSystem CreateRayBursts(GameObject parent)
        {
            GameObject go = new GameObject("RayBurstSystem");
            go.transform.SetParent(parent.transform, false);
            go.transform.position = SphereCenter;

            EnergyRayBurstSystem rays = go.AddComponent<EnergyRayBurstSystem>();
            rays.rayCount = 34;
            rays.sphereRadius = RingRadius;
            rays.rayLengthMin = 0.45f;
            rays.rayLengthMax = 2.35f;
            rays.rayWidthMin = 0.004f;
            rays.rayWidthMax = 0.020f;
            rays.burstIntervalMin = 0.08f;
            rays.burstIntervalMax = 0.24f;
            rays.burstCountMin = 1;
            rays.burstCountMax = 4;
            rays.flashDuration = 0.30f;
            rays.maxIntensity = 7.2f;
            rays.longRayChance = 0.08f;
            return rays;
        }

        private static EnergyHotspotSystem CreateHotspots(GameObject parent, EnergyRayBurstSystem rays)
        {
            GameObject go = new GameObject("HotspotSystem");
            go.transform.SetParent(parent.transform, false);
            go.transform.position = SphereCenter;

            EnergyHotspotSystem hs = go.AddComponent<EnergyHotspotSystem>();
            hs.linkedRays = rays;
            hs.hotspotCount = 12;
            hs.sphereRadius = RingRadius;
            hs.hotspotSizeMin = 0.12f;
            hs.hotspotSizeMax = 0.34f;
            hs.hotspotIntensity = 9.2f;
            hs.flashDuration = 0.42f;
            hs.intervalMin = 0.10f;
            hs.intervalMax = 0.48f;
            hs.flashesPerEventMin = 1;
            hs.flashesPerEventMax = 3;
            return hs;
        }

        private static EnergySparkSystem CreateSparks(GameObject parent)
        {
            GameObject go = new GameObject("SparkParticleSystem");
            go.transform.SetParent(parent.transform, false);
            go.transform.position = SphereCenter;

            EnergySparkSystem sparks = go.AddComponent<EnergySparkSystem>();
            sparks.maxParticles = 900;
            sparks.emissionRate = 280f;
            sparks.sphereRadius = RingRadius;
            sparks.sizeMin = 0.014f;
            sparks.sizeMax = 0.075f;
            sparks.lifetimeMin = 0.28f;
            sparks.lifetimeMax = 1.65f;
            sparks.speedMin = 0.08f;
            sparks.speedMax = 1.25f;
            sparks.ringSpawnChance = 0.64f;
            sparks.innerSpawnChance = 0.28f;
            sparks.outsideJitter = 0.14f;
            sparks.radialBias = 0.65f;
            sparks.tangentialBias = 0.42f;
            sparks.turbulence = 0.18f;
            return sparks;
        }

        private static EnergyAtmosphereParticles CreateAtmosphere(GameObject parent)
        {
            GameObject go = new GameObject("EnergyBloomAtmosphere");
            go.transform.SetParent(parent.transform, false);
            go.transform.position = SphereCenter;

            EnergyAtmosphereParticles atm = go.AddComponent<EnergyAtmosphereParticles>();
            atm.maxParticles = 220;
            atm.emissionRate = 34f;
            atm.sphereRadius = RingRadius;
            atm.spreadRadius = 3.05f;
            atm.sizeMin = 0.040f;
            atm.sizeMax = 0.170f;
            return atm;
        }

        private static void SetupController(GameObject parent, Renderer ring, Renderer inner)
        {
            EnergySphereController ctrl = parent.AddComponent<EnergySphereController>();
            ctrl.ringRenderer = ring;
            ctrl.innerRenderer = inner;
            ctrl.innerMesh = inner != null ? inner.GetComponent<InnerEnergyMesh>() : null;
            ctrl.sphereTransform = parent.transform;
            ctrl.ringRadius = RingRadius;
            ctrl.ringPulseAmp = 0.026f;
            ctrl.ringPulseSpeed = 1.15f;
            ctrl.ringNoiseAmp = 0.018f;
            ctrl.ringNoiseSpeed = 0.32f;
            ctrl.globalPulseSpeed = 0.85f;
            ctrl.globalPulseAmp = 0.10f;
            ctrl.corePulseAmp = 0.18f;
            ctrl.warmAngleDriftSpeed = 0.10f;
            ctrl.warmAngleDriftAmp = 0.18f;
            ctrl.baseWarmAngle = 0.28f;
            ctrl.hotspotIntensityBase = 8.0f;
            ctrl.hotspotCycleSpeed = 0.72f;
            ctrl.hotspotWidth = 0.085f;
            ctrl.masterIntensity = 1.0f;
            ctrl.intensityBreathSpeed = 0.52f;
            ctrl.intensityBreathAmp = 0.09f;
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
                new Vector3(-h,  h, 0f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
