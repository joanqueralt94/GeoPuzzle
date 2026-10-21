using System.IO;
using GeoPuzzle.Core;
using GeoPuzzle.Map;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GeoPuzzle.Editor
{
    /// <summary>
    /// One-click project configuration, so settings live in code review instead of in someone's memory.
    /// Safe to run again: it only overwrites the settings listed here and never replaces existing scenes.
    /// </summary>
    public static class ProjectSetup
    {
        public const string CompanyName = "Joan Queralt";
        public const string ProductName = "GeoPuzzle";
        public const string AndroidPackageId = "com.joanqueralt.geopuzzle";
        const int AndroidTargetApi = 36;

        const string ScenesFolder = "Assets/_Project/Scenes";
        const string CountriesJsonPath = "Assets/_Project/Data/countries.json";
        static readonly string[] SceneNames = { "Boot", "MainMenu", "Game" };

        [MenuItem("GeoPuzzle/Setup/Run Full Setup", priority = 0)]
        public static void RunAll()
        {
            ApplyEditorSettings();
            ApplyAndroidSettings();
            CreateScenes();
            Debug.Log("GeoPuzzle setup finished");
        }

        [MenuItem("GeoPuzzle/Setup/Apply Editor Settings")]
        public static void ApplyEditorSettings()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        }

        [MenuItem("GeoPuzzle/Setup/Apply Android Settings")]
        public static void ApplyAndroidSettings()
        {
            var android = NamedBuildTarget.Android;

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(android, AndroidPackageId);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)AndroidTargetApi;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            // Landscape on either side; portrait is off until game design decides otherwise.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            EditorUserBuildSettings.buildAppBundle = true;
        }

        [MenuItem("GeoPuzzle/Setup/Create Scenes")]
        public static void CreateScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Directory.CreateDirectory(ScenesFolder);
            foreach (var name in SceneNames)
            {
                var path = $"{ScenesFolder}/{name}.unity";
                if (File.Exists(path))
                    continue;

                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.orthographic = true;
                    cam.orthographicSize = 5f;
                }

                if (name == "Boot")
                    AddBootstrap();
                else if (name == "Game" && cam != null)
                    cam.gameObject.AddComponent<MapCameraController>();

                EditorSceneManager.SaveScene(scene, path);
            }

            var buildScenes = new EditorBuildSettingsScene[SceneNames.Length];
            for (var i = 0; i < SceneNames.Length; i++)
                buildScenes[i] = new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames[i]}.unity", true);
            EditorBuildSettings.scenes = buildScenes;

            EditorSceneManager.OpenScene($"{ScenesFolder}/Boot.unity");
        }

        static void AddBootstrap()
        {
            var bootstrap = new GameObject("Bootstrap").AddComponent<Bootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("countriesJson").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(CountriesJsonPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
