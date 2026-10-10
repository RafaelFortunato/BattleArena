using System.IO;
using BattleArena.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BattleArena.Editor.ContentBuilder
{
    /// <summary>
    /// Generates the game's content from code and the imported art: materials, prefabs, data assets, sound events,
    /// UI settings and the Arena scene (<c>BattleArena &gt; Build Game Content</c>). Re-running it updates assets in
    /// place, keeping their GUIDs, so it is safe to run after any change to the builder. Each partial file builds
    /// one area of the game.
    /// </summary>
    internal static partial class GameContentBuilder
    {
        private const string Root = "Assets/Game";
        private const string MaterialsFolder = Root + "/Materials";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string DataFolder = Root + "/Data";
        private const string UIFolder = Root + "/UI";
        private const string SettingsFolder = Root + "/Settings";
        private const string ScenesFolder = Root + "/Scenes";
        private const string ScenePath = ScenesFolder + "/Arena.unity";
        // Damage numbers are world-space TextMesh Pro text, so they need Baloo 2 as a TMP font asset of their own.
        private const string DamageFontSourcePath = UIFolder + "/Fonts/Baloo2-ExtraBold.ttf";
        private const string DamageFontPath = UIFolder + "/Fonts/Baloo2-ExtraBold SDF.asset";
        private const string DamageFontCharacters = "0123456789!-+.%";

        /// <summary>
        /// Rebuilds all game content into a fresh Arena scene, with a progress bar. Unsaved scene changes are offered
        /// for saving first.
        /// </summary>
        [MenuItem("BattleArena/Build Game Content", priority = 0)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                // Open the new scene first: switching scenes unloads unreferenced assets,
                // which would invalidate the prefab references gathered below.
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                EditorUtility.DisplayProgressBar("BattleArena", "Configuring physics layers", 0.05f);
                ConfigurePhysicsLayers();

                EditorUtility.DisplayProgressBar("BattleArena", "Materials", 0.15f);
                var materials = BuildMaterials();

                EditorUtility.DisplayProgressBar("BattleArena", "Prefabs and data", 0.35f);
                var content = BuildContent(materials);

                EditorUtility.DisplayProgressBar("BattleArena", "Sound effects", 0.6f);
                var sounds = BuildAudio();

                EditorUtility.DisplayProgressBar("BattleArena", "Arena scene", 0.7f);
                BuildScene(scene, materials, content, sounds);

                AssetDatabase.SaveAssets();
                Debug.Log("[BattleArena] Game content built. Open Assets/Game/Scenes/Arena.unity and press Play.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ---------------------------------------------------------------- Physics

        /// <summary>
        /// Names the game's physics layers and sets the collision matrix: nothing collides by default, then the pairs
        /// that matter are enabled.
        /// </summary>
        private static void ConfigurePhysicsLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            var layers = tagManager.FindProperty("layers");
            SetLayerName(layers, GameLayers.Ground, "Ground");
            SetLayerName(layers, GameLayers.Player, "Player");
            SetLayerName(layers, GameLayers.Enemy, "Enemy");
            SetLayerName(layers, GameLayers.PlayerProjectile, "PlayerProjectile");
            SetLayerName(layers, GameLayers.EnemyProjectile, "EnemyProjectile");
            SetLayerName(layers, GameLayers.Pickup, "Pickup");
            SetLayerName(layers, GameLayers.Interactable, "Interactable");
            SetLayerName(layers, GameLayers.PlayerSensor, "PlayerSensor");
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            // Start from "nothing collides" for the game layers, then whitelist the pairs that matter.
            int[] gameLayers =
            {
                GameLayers.Default, GameLayers.Ground, GameLayers.Player, GameLayers.Enemy, GameLayers.PlayerProjectile,
                GameLayers.EnemyProjectile, GameLayers.Pickup, GameLayers.Interactable, GameLayers.PlayerSensor
            };
            foreach (var a in gameLayers)
            foreach (var b in gameLayers)
                Physics.IgnoreLayerCollision(a, b, true);

            Collide(GameLayers.Default, GameLayers.Default);
            Collide(GameLayers.Default, GameLayers.Ground);
            Collide(GameLayers.Player, GameLayers.Default);
            Collide(GameLayers.Player, GameLayers.Ground);
            Collide(GameLayers.PlayerProjectile, GameLayers.Enemy);
            Collide(GameLayers.PlayerProjectile, GameLayers.Default);
            Collide(GameLayers.EnemyProjectile, GameLayers.Player);
            Collide(GameLayers.EnemyProjectile, GameLayers.Default);
            Collide(GameLayers.PlayerSensor, GameLayers.Pickup);
            Collide(GameLayers.PlayerSensor, GameLayers.Interactable);
        }

        /// <summary>Names the layer at <paramref name="index"/>.</summary>
        private static void SetLayerName(SerializedProperty layers, int index, string name) =>
            layers.GetArrayElementAtIndex(index).stringValue = name;

        /// <summary>Lets layers <paramref name="a"/> and <paramref name="b"/> collide.</summary>
        private static void Collide(int a, int b) => Physics.IgnoreLayerCollision(a, b, false);

        // ---------------------------------------------------------------- Asset helpers

        /// <summary>Creates <paramref name="path"/> and any missing parent folders.</summary>
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Loads the asset at <paramref name="path"/> or creates it, so GUIDs survive rebuilds.</summary>
        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>Saves a temporary hierarchy as a prefab (overwriting in place) and destroys the temporary.</summary>
        private static T SavePrefab<T>(GameObject root, string path) where T : Component
        {
            EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<T>();
        }

        /// <summary>Creates an empty root object on <paramref name="layer"/>.</summary>
        private static GameObject CreateRoot(string name, int layer) => new(name) { layer = layer };

        /// <summary>Creates a primitive child used purely as a visual (its collider is removed).</summary>
        private static GameObject AddVisual(Transform parent, PrimitiveType type, Material material,
            Vector3 localPosition, Vector3 localScale, Vector3 localEuler = default, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name ?? type.ToString();
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localEulerAngles = localEuler;
            go.transform.localScale = localScale;

            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            return go;
        }

        /// <summary>
        /// Creates an empty child at <paramref name="localPosition"/>, on the parent's layer unless one is given.
        /// </summary>
        private static Transform AddChild(Transform parent, string name, Vector3 localPosition, int? layer = null)
        {
            var child = new GameObject(name) { layer = layer ?? parent.gameObject.layer }.transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        /// <summary>Puts <paramref name="go"/> and all its children on <paramref name="layer"/>.</summary>
        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
