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
        const string EconomyConfigPath = "Assets/_Project/Data/Config/economy_config.json";
        const string WorldMapPath = "Assets/_Project/Data/Map/world_map.json";
        const string MapMaterialPath = "Assets/_Project/Art/Materials/Map.mat";
        static readonly string[] SceneNames = { "Boot", "MainMenu", "Game" };

        [MenuItem("GeoPuzzle/Setup/Run Full Setup", priority = 0)]
        public static void RunAll()
        {
            ApplyEditorSettings();
            ApplyAndroidSettings();
            CreateScenes();
            AddWorldMapToGameScene();
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

        [MenuItem("GeoPuzzle/Setup/Add World Map To Game Scene")]
        public static void AddWorldMapToGameScene()
        {
            var scenePath = $"{ScenesFolder}/Game.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogError("Game scene is missing; run Create Scenes first");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(scenePath);
            if (Object.FindAnyObjectByType<WorldMapView>() == null)
            {
                var mapObject = new GameObject("WorldMap");
                var view = mapObject.AddComponent<WorldMapView>();
                mapObject.AddComponent<MapTapInput>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("mapJson").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<TextAsset>(WorldMapPath);
                serialized.FindProperty("material").objectReferenceValue = GetOrCreateMapMaterial();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                if (cam.GetComponent<MapCameraController>() == null)
                    cam.gameObject.AddComponent<MapCameraController>();
            }

            EditorSceneManager.SaveScene(scene);
        }

        static Material GetOrCreateMapMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MapMaterialPath);
            if (existing != null)
                return existing;
            Directory.CreateDirectory(Path.GetDirectoryName(MapMaterialPath));
            var material = new Material(Shader.Find("Sprites/Default")) { name = "Map" };
            AssetDatabase.CreateAsset(material, MapMaterialPath);
            return material;
        }

        static void AddBootstrap()
        {
            var bootstrap = new GameObject("Bootstrap").AddComponent<Bootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("countriesJson").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(CountriesJsonPath);
            serialized.FindProperty("economyConfigJson").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyConfigPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
