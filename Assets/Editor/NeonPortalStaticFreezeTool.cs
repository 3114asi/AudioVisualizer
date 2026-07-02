#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Ediskrad.AudioVisualizer.Editor
{
    public static class NeonPortalStaticFreezeTool
    {
        private const string ScenePath = "Assets/Scenes/NeonPortalScene.unity";
        private const string RefOutputPath = "ref/current.png";
        private const string RootOutputPath = "current.png";
        private const int CaptureWidth = 1152;
        private const int CaptureHeight = 1760;
        private const float FirstFrameParticleTime = 1f / 60f;

        private const string PlayCapturePendingKey = "NeonPortalStaticFreeze.PlayCapturePending";
        private const string ExitAfterPlayCaptureKey = "NeonPortalStaticFreeze.ExitAfterPlayCapture";
        private const string HasPreviousPlayModeOptionsKey = "NeonPortalStaticFreeze.HasPreviousPlayModeOptions";
        private const string PreviousPlayModeOptionsEnabledKey = "NeonPortalStaticFreeze.PreviousPlayModeOptionsEnabled";
        private const string PreviousPlayModeOptionsKey = "NeonPortalStaticFreeze.PreviousPlayModeOptions";

        private static readonly HashSet<string> MotionScriptNames = new HashSet<string>
        {
            "CameraCinematicMotion",
            "PortalPulseController",
            "RingEnergyFlow",
            "RadialBurstSpawner",
            "WaterReflectionAnimator",
            "ParticleLODController",
            "EnergySphereController",
            "InnerEnergyMesh",
            "EnergyRayBurstSystem",
            "EnergySparkSystem",
            "EnergyHotspotSystem",
            "EnergyAtmosphereParticles",
            "NeonRingEnergyAnimator"
        };

        private static int playModeFrameCount;

        static NeonPortalStaticFreezeTool()
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

        [MenuItem("Tools/AudioVisualizer/Neon Portal/Freeze Animation To First Frame")]
        public static void FreezeNeonPortalSceneAnimation()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyStaticFirstFrameState(scene, true);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NeonPortalStaticFreeze] Frozen NeonPortalScene animation at first frame.");
        }

        [MenuItem("Tools/AudioVisualizer/Neon Portal/Freeze Animation + Play Mode Capture")]
        public static void FreezeAndPlayModeCapture()
        {
            FreezeNeonPortalSceneAnimation();
            StartPlayModeCapture();
        }

        private static void ApplyStaticFirstFrameState(Scene scene, bool markDirty)
        {
            FreezeAnimators(scene, markDirty);
            FreezeAnimations(scene, markDirty);
            FreezeTimelines(scene, markDirty);
            ApplyStaticMaterialParameters(scene, markDirty);
            FreezeMotionScripts(scene, markDirty);
            FreezeParticleSystems(scene, markDirty);
            FreezeTrails(scene, markDirty);

            if (markDirty)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void FreezeAnimators(Scene scene, bool markDirty)
        {
            foreach (Animator animator in GetSceneObjects<Animator>(scene))
            {
                if (animator == null)
                    continue;

                animator.Rebind();
                animator.Update(0f);
                animator.speed = 0f;
                animator.enabled = false;

                if (markDirty)
                    EditorUtility.SetDirty(animator);
            }
        }

        private static void FreezeAnimations(Scene scene, bool markDirty)
        {
            foreach (Animation animation in GetSceneObjects<Animation>(scene))
            {
                if (animation == null)
                    continue;

                if (animation.clip != null)
                    animation.clip.SampleAnimation(animation.gameObject, 0f);

                animation.Stop();
                animation.playAutomatically = false;
                animation.enabled = false;

                if (markDirty)
                    EditorUtility.SetDirty(animation);
            }
        }

        private static void FreezeTimelines(Scene scene, bool markDirty)
        {
            foreach (PlayableDirector director in GetSceneObjects<PlayableDirector>(scene))
            {
                if (director == null)
                    continue;

                director.time = 0f;
                director.Evaluate();
                director.Stop();
                director.playOnAwake = false;
                director.enabled = false;

                if (markDirty)
                    EditorUtility.SetDirty(director);
            }
        }

        private static void FreezeMotionScripts(Scene scene, bool markDirty)
        {
            foreach (MonoBehaviour script in GetSceneObjects<MonoBehaviour>(scene))
            {
                if (script == null || !ShouldDisableMotionScript(script))
                    continue;

                ApplyMotionScriptFirstFrame(script);
                script.CancelInvoke();
                script.StopAllCoroutines();
                script.enabled = false;

                if (markDirty)
                    EditorUtility.SetDirty(script);
            }
        }

        private static bool ShouldDisableMotionScript(MonoBehaviour script)
        {
            Type type = script.GetType();
            if (MotionScriptNames.Contains(type.Name))
                return true;

            return type.Namespace == "Ediskrad.AudioVisualizer"
                && HasDeclaredMethod(type, "Update")
                || type.Namespace == "Ediskrad.AudioVisualizer"
                && HasDeclaredMethod(type, "FixedUpdate")
                || type.Namespace == "Ediskrad.AudioVisualizer"
                && HasDeclaredMethod(type, "LateUpdate");
        }

        private static bool HasDeclaredMethod(Type type, string methodName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            return type.GetMethod(methodName, flags) != null;
        }

        private static void ApplyMotionScriptFirstFrame(MonoBehaviour script)
        {
            switch (script)
            {
                case PortalPulseController pulse:
                    ApplyPulseFirstFrame(pulse);
                    break;
                case RingEnergyFlow flow:
                    ApplyFlowFirstFrame(flow);
                    break;
                case WaterReflectionAnimator water:
                    ApplyWaterFirstFrame(water);
                    break;
                case EnergySphereController controller:
                    controller.ApplyState(0f);
                    break;
                case InnerEnergyMesh inner:
                    inner.ApplyState(0f, 2f, 1f);
                    break;
                case EnergyRayBurstSystem rays:
                    rays.PreviewAtTime(0f);
                    break;
                case EnergySparkSystem sparks:
                    sparks.PreviewAtTime(0f);
                    break;
                case EnergyHotspotSystem hotspots:
                    hotspots.PreviewAtTime(0f);
                    break;
                case EnergyAtmosphereParticles atmosphere:
                    atmosphere.PreviewAtTime(0f);
                    break;
                case NeonRingEnergyAnimator ringAnimator:
                    ringAnimator.ApplyState(0f);
                    break;
            }
        }

        private static void ApplyPulseFirstFrame(PortalPulseController pulse)
        {
            const float wave = 0.5f;
            float curveValue = pulse.pulseCurve != null ? pulse.pulseCurve.Evaluate(wave) : wave;
            float pulseValue = 1f + curveValue * pulse.pulseEmission;
            float intensity = pulse.baseEmission * pulseValue;

            if (pulse.emissionRenderers != null)
            {
                foreach (Renderer target in pulse.emissionRenderers)
                {
                    ApplyToSharedMaterials(target, material =>
                    {
                        SetFloatIfExists(material, "_Pulse", pulseValue);
                        SetFloatIfExists(material, "_Intensity", intensity);
                    });
                }
            }

            if (pulse.accentLights != null)
            {
                foreach (Light accentLight in pulse.accentLights)
                {
                    if (accentLight == null)
                        continue;

                    accentLight.intensity = intensity * 0.35f;
                    EditorUtility.SetDirty(accentLight);
                }
            }

            if (pulse.postProcessVolume != null
                && pulse.postProcessVolume.profile != null
                && pulse.postProcessVolume.profile.TryGet(out Bloom bloom))
            {
                bloom.intensity.value = pulse.baseBloomIntensity + pulse.pulseBloomIntensity * wave;
                EditorUtility.SetDirty(pulse.postProcessVolume.profile);
            }
        }

        private static void ApplyFlowFirstFrame(RingEnergyFlow flow)
        {
            if (flow.flowRenderers != null)
            {
                foreach (Renderer target in flow.flowRenderers)
                    ApplyToSharedMaterials(target, material => SetFloatIfExists(material, "_FlowOffset", 0f));
            }

            if (flow.orbitingEnergyPoints == null || flow.orbitingEnergyPoints.Length == 0)
                return;

            for (int i = 0; i < flow.orbitingEnergyPoints.Length; i++)
            {
                Transform point = flow.orbitingEnergyPoints[i];
                if (point == null)
                    continue;

                float angle = i * (360f / flow.orbitingEnergyPoints.Length) * Mathf.Deg2Rad;
                point.localPosition = flow.orbitCenter + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * flow.orbitRadius;
                point.localScale = Vector3.one * (0.08f + 0.035f * Mathf.Sin(i));
                EditorUtility.SetDirty(point);
            }
        }

        private static void ApplyWaterFirstFrame(WaterReflectionAnimator water)
        {
            ApplyToSharedMaterials(water.reflectionRenderer, material =>
            {
                SetFloatIfExists(material, "_Ripple", 0f);
                SetFloatIfExists(material, "_Intensity", water.intensity);
            });

            if (water.waterGlowRenderers == null)
                return;

            foreach (Renderer target in water.waterGlowRenderers)
            {
                ApplyToSharedMaterials(target, material =>
                {
                    SetFloatIfExists(material, "_Ripple", 0f);
                    SetFloatIfExists(material, "_Intensity", water.intensity * 0.55f);
                });
            }
        }

        private static void ApplyStaticMaterialParameters(Scene scene, bool markDirty)
        {
            HashSet<Material> touched = new HashSet<Material>();

            foreach (Renderer renderer in GetSceneObjects<Renderer>(scene))
            {
                if (renderer == null)
                    continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || !touched.Add(material))
                        continue;

                    SetFloatIfExists(material, "_FreezeTime", 1f);
                    SetFloatIfExists(material, "_StaticTime", 0f);
                    SetFloatIfExists(material, "_FlowOffset", 0f);
                    SetFloatIfExists(material, "_EffectTime", 0f);
                    SetFloatIfExists(material, "_Ripple", 0f);

                    if (markDirty)
                        EditorUtility.SetDirty(material);
                }
            }
        }

        private static void FreezeParticleSystems(Scene scene, bool markDirty)
        {
            foreach (ParticleSystem particleSystem in GetSceneObjects<ParticleSystem>(scene))
            {
                if (particleSystem == null)
                    continue;

                bool wasActive = particleSystem.gameObject.activeInHierarchy;
                if (wasActive)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particleSystem.Simulate(FirstFrameParticleTime, true, true, true);
                    particleSystem.Pause(true);
                }

                ParticleSystem.MainModule main = particleSystem.main;
                main.playOnAwake = false;
                main.loop = false;
                main.simulationSpeed = 0f;

                ParticleSystem.EmissionModule emission = particleSystem.emission;
                emission.enabled = false;

                ParticleSystem.NoiseModule noise = particleSystem.noise;
                noise.enabled = false;

                if (markDirty)
                    EditorUtility.SetDirty(particleSystem);
            }
        }

        private static void FreezeTrails(Scene scene, bool markDirty)
        {
            foreach (TrailRenderer trail in GetSceneObjects<TrailRenderer>(scene))
            {
                if (trail == null)
                    continue;

                trail.emitting = false;
                if (markDirty)
                    EditorUtility.SetDirty(trail);
            }
        }

        private static void StartPlayModeCapture()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            playModeFrameCount = 0;
            SessionState.SetBool(PlayCapturePendingKey, true);
            SessionState.SetBool(ExitAfterPlayCaptureKey, false);
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
            if (playModeFrameCount < 4)
                return;

            ApplyStaticFirstFrameState(SceneManager.GetActiveScene(), false);
            CaptureStaticToCurrent();

            SessionState.SetBool(PlayCapturePendingKey, false);
            SessionState.SetBool(ExitAfterPlayCaptureKey, true);
            EditorApplication.update -= PlayModeCaptureUpdate;
            EditorApplication.isPlaying = false;
        }

        private static void CaptureStaticToCurrent()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!EditorApplication.isPlaying && scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Camera camera = FindCaptureCamera(scene);
            if (camera == null)
            {
                Debug.LogError("[NeonPortalStaticFreeze] No active camera found in NeonPortalScene.");
                return;
            }

            RenderToFile(camera, RefOutputPath, CaptureWidth, CaptureHeight);
            RenderToFile(camera, RootOutputPath, CaptureWidth, CaptureHeight);
            Debug.Log("[NeonPortalStaticFreeze] Capture saved: " + RefOutputPath + " and " + RootOutputPath);
        }

        private static Camera FindCaptureCamera(Scene scene)
        {
            return GetSceneObjects<Camera>(scene)
                .Where(camera => camera != null && camera.enabled && camera.gameObject.activeInHierarchy)
                .OrderByDescending(camera => camera.depth)
                .FirstOrDefault();
        }

        private static void RenderToFile(Camera camera, string path, int width, int height)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            camera.targetTexture = rt;
            RenderTexture.active = rt;
            camera.Render();

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(rt);
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

        private static IEnumerable<T> GetSceneObjects<T>(Scene scene) where T : Object
        {
            foreach (T obj in Resources.FindObjectsOfTypeAll<T>())
            {
                if (obj == null)
                    continue;

                if (obj is Component component && component.gameObject.scene == scene)
                    yield return obj;
                else if (obj is GameObject gameObject && gameObject.scene == scene)
                    yield return obj;
            }
        }

        private static void ApplyToSharedMaterials(Renderer renderer, Action<Material> apply)
        {
            if (renderer == null)
                return;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                apply(material);
                EditorUtility.SetDirty(material);
            }
        }

        private static void SetFloatIfExists(Material material, string property, float value)
        {
            if (material != null && material.HasProperty(property))
                material.SetFloat(property, value);
        }
    }
}
#endif
