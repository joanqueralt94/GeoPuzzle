using System.IO;
using System.Linq;
using GeoPuzzle.Core;
using GeoPuzzle.Map;
using GeoPuzzle.UI;
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
        const string FlagLibraryPath = "Assets/_Project/Data/FlagLibrary.asset";
        static readonly string[] EnglishTextPaths =
        {
            "Assets/_Project/Data/Localization/countries.en.json",
            "Assets/_Project/Data/Localization/ui.en.json",
        };
        static readonly string[] SceneNames = { "Boot", "MainMenu", "Game", "FlagQuiz" };

        [MenuItem("GeoPuzzle/Setup/Run Full Setup", priority = 0)]
        public static void RunAll()
        {
            ApplyEditorSettings();
            ApplyAndroidSettings();
            CreateScenes();
            AddWorldMapToGameScene();
            BuildFlagLibrary();
            SetUpScreens();
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

        static void AddBootstrap() => AssignBootstrapData(new GameObject("Bootstrap").AddComponent<Bootstrap>());

        static void AssignBootstrapData(Bootstrap bootstrap)
        {
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("countriesJson").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(CountriesJsonPath);
            serialized.FindProperty("economyConfigJson").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyConfigPath);
            var texts = serialized.FindProperty("englishTexts");
            texts.arraySize = EnglishTextPaths.Length;
            for (var i = 0; i < EnglishTextPaths.Length; i++)
                texts.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<TextAsset>(EnglishTextPaths[i]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Collects every flag sprite into the FlagLibrary asset the quiz screens read.</summary>
        [MenuItem("GeoPuzzle/Setup/Build Flag Library")]
        public static FlagLibrary BuildFlagLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<FlagLibrary>(FlagLibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<FlagLibrary>();
                AssetDatabase.CreateAsset(library, FlagLibraryPath);
            }

            var entries = AssetDatabase.FindAssets("t:Sprite", new[] { FlagImporter.FlagsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .Select(path => new FlagLibrary.Entry
                {
                    flag = Path.GetFileNameWithoutExtension(path),
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path),
                })
                .Where(entry => entry.sprite != null)
                .ToList();
            library.SetEntries(entries);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"Flag library: {entries.Count} flags");
            return library;
        }

        /// <summary>
        /// Wires the Boot texts, the menu and the Flag Quizz screen, and makes every playable scene load
        /// Boot first when started directly in the editor.
        /// </summary>
        [MenuItem("GeoPuzzle/Setup/Set Up Screens")]
        public static void SetUpScreens()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditScene("Boot", () =>
            {
                var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
                if (bootstrap == null)
                    AddBootstrap();
                else
                    AssignBootstrapData(bootstrap);
            });
            EditScene("MainMenu", () =>
            {
                EnsureRequireBoot();
                if (Object.FindAnyObjectByType<MainMenuController>() == null)
                    new GameObject("MainMenu").AddComponent<MainMenuController>();
            });
            EditScene("Game", EnsureRequireBoot);
            EditScene("FlagQuiz", () =>
            {
                EnsureRequireBoot();
                var controller = Object.FindAnyObjectByType<FlagQuizController>();
                if (controller == null)
                    controller = new GameObject("FlagQuiz").AddComponent<FlagQuizController>();
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("flags").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<FlagLibrary>(FlagLibraryPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });

            EditorSceneManager.OpenScene($"{ScenesFolder}/Boot.unity");
        }

        static void EditScene(string name, System.Action edit)
        {
            var path = $"{ScenesFolder}/{name}.unity";
            if (!File.Exists(path))
            {
                Debug.LogError($"{name} scene is missing; run Create Scenes first");
                return;
            }
            var scene = EditorSceneManager.OpenScene(path);
            edit();
            EditorSceneManager.SaveScene(scene);
        }

        static void EnsureRequireBoot()
        {
            if (Object.FindAnyObjectByType<RequireBoot>() == null)
                new GameObject("RequireBoot").AddComponent<RequireBoot>();
        }
    }
}
