#if UNITY_EDITOR
using System.IO;
using Ediskrad.AudioVisualizer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ediskrad.AudioVisualizer.Editor
{
    public static class EnergySphereBuilder
    {
        private const string ScenePath = "Assets/Scenes/EnergySphereScene.unity";
        private const string VideoFramesPath = "ref/energy_sphere_video_frames";
        private const int CaptureWidth = 1152;
        private const int CaptureHeight = 1760;
        private const int VideoFps = 30;
        private const float VideoDurationSeconds = 10.0f;

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Build Scene")]
        public static void BuildScene()
        {
            if (!Directory.Exists("Assets/Scenes"))
                Directory.CreateDirectory("Assets/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnergySphereSceneBuilder.BuildScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[EnergySphere] Scene built: " + ScenePath);
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Open Scene")]
        public static void OpenScene()
        {
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                Debug.Log("[EnergySphere] Scene opened: " + ScenePath);
            }
            else
            {
                Debug.LogWarning("[EnergySphere] Scene not found: " + ScenePath);
            }
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Rebuild + Open")]
        public static void RebuildAndOpen()
        {
            BuildScene();
            OpenScene();
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Capture Screenshot")]
        public static void CaptureScreenshot()
        {
            EnsureScene();
            Camera cam = Object.FindObjectOfType<Camera>();
            if (cam == null)
            {
                Debug.LogWarning("[EnergySphere] Camera not found");
                return;
            }

            PreparePreview(2.85f);

            string path = "ref/energy_sphere_current.png";
            Directory.CreateDirectory("ref");
            RenderToFile(cam, path, CaptureWidth, CaptureHeight);
            Debug.Log("[EnergySphere] Screenshot saved: " + path);
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Capture Preview Sequence")]
        public static void CapturePreviewSequence()
        {
            EnsureScene();
            Camera cam = Object.FindObjectOfType<Camera>();
            if (cam == null)
            {
                Debug.LogWarning("[EnergySphere] Camera not found");
                return;
            }

            Directory.CreateDirectory("ref/energy_preview");
            float[] times = { 0.15f, 1.05f, 2.15f, 3.25f, 4.40f };
            for (int i = 0; i < times.Length; i++)
            {
                PreparePreview(times[i]);
                RenderToFile(cam, $"ref/energy_preview/frame_{i:00}.png", CaptureWidth, CaptureHeight);
            }

            Debug.Log("[EnergySphere] Preview sequence saved: ref/energy_preview/frame_00..04.png");
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Build + Capture")]
        public static void BuildAndCapture()
        {
            BuildScene();
            CaptureScreenshot();
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Build + Capture Sequence")]
        public static void BuildAndCaptureSequence()
        {
            BuildScene();
            CapturePreviewSequence();
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Export 10s Video Frames")]
        public static void ExportTenSecondVideoFrames()
        {
            EnsureScene();
            Camera cam = Object.FindObjectOfType<Camera>();
            if (cam == null)
            {
                Debug.LogWarning("[EnergySphere] Camera not found");
                return;
            }

            ResetDirectory(VideoFramesPath);

            int frameCount = Mathf.RoundToInt(VideoDurationSeconds * VideoFps);
            for (int i = 0; i < frameCount; i++)
            {
                float time = i / (float)VideoFps;
                PreparePreview(time);
                RenderToFile(cam, $"{VideoFramesPath}/frame_{i:0000}.png", CaptureWidth, CaptureHeight);
            }

            Debug.Log($"[EnergySphere] Video frames saved: {VideoFramesPath}/frame_0000..{frameCount - 1:0000}.png");
        }

        [MenuItem("Tools/AudioVisualizer/Energy Sphere/Build + Export 10s Video Frames")]
        public static void BuildAndExportTenSecondVideoFrames()
        {
            BuildScene();
            ExportTenSecondVideoFrames();
        }

        private static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
                BuildScene();

            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);
        }

        private static void PreparePreview(float time)
        {
            foreach (EnergySphereController controller in Object.FindObjectsOfType<EnergySphereController>())
                controller.ApplyState(time);

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

        private static void ResetDirectory(string path)
        {
            string fullPath = Path.GetFullPath(path);
            string refRoot = Path.GetFullPath("ref");
            if (!fullPath.StartsWith(refRoot))
            {
                Debug.LogError("[EnergySphere] Refusing to reset directory outside ref/: " + fullPath);
                return;
            }

            if (Directory.Exists(fullPath))
                Directory.Delete(fullPath, true);

            Directory.CreateDirectory(fullPath);
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

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);

            Object.DestroyImmediate(tex);
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
        }
    }
}
#endif
