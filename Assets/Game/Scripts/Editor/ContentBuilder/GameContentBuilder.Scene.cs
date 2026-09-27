using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Enemies;
using BattleArena.Player;
using BattleArena.Progression;
using BattleArena.UI;
using BattleArena.Vfx;
using BattleArena.Interaction;
using BattleArena.Missions;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using BindingMode = Unity.Cinemachine.TargetTracking.BindingMode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BattleArena.Editor.ContentBuilder
{
    internal static partial class GameContentBuilder
    {
        private const float ArenaHalfSize = 40f;
        private const int LayoutSeed = 1337;

        private static readonly Vector3[] ShrinePositions = { new(-18f, 0f, 14f), new(20f, 0f, -16f) };

        private static void BuildScene(Scene scene, MaterialSet m, ContentSet content)
        {
            BuildLighting();
            var environment = BuildEnvironment(m);
            BakeNavMesh(environment);

            foreach (var position in ShrinePositions)
                PlacePrefab(content.Shrine.gameObject, position, Quaternion.identity);

            var player = PlacePrefab(content.Player, Vector3.zero, Quaternion.identity);
            var playerHealth = player.GetComponent<Health>();

            var gameManager = BuildSystems(m, content, player, playerHealth, out var levelUp);
            BuildCamera(player.transform);
            BuildUserInterface(gameManager, levelUp, content.LevelUp, player, playerHealth);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static GameObject PlacePrefab(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        // ---------------------------------------------------------------- Lighting & post

        private static void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Hex("#FFF0D2");
            sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#8FA3C7");
            RenderSettings.ambientEquatorColor = Hex("#5E6B55");
            RenderSettings.ambientGroundColor = Hex("#2A2826");

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = BuildVolumeProfile();
        }

        private static VolumeProfile BuildVolumeProfile()
        {
            var profile = LoadOrCreateAsset<VolumeProfile>($"{SettingsFolder}/ArenaVolumeProfile.asset");

            var bloom = GetOrAddOverride<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.6f);

            GetOrAddOverride<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);

            var vignette = GetOrAddOverride<Vignette>(profile);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            var colorAdjustments = GetOrAddOverride<ColorAdjustments>(profile);
            colorAdjustments.postExposure.Override(0.2f);
            colorAdjustments.saturation.Override(12f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T GetOrAddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
                return component;

            component = profile.Add<T>(overrides: true);
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // ---------------------------------------------------------------- Environment

        private static GameObject BuildEnvironment(MaterialSet m)
        {
            var environment = new GameObject("Environment");
            var root = environment.transform;

            // The visible ground reaches past the walls, but only the floor inside them is walkable: its collider
            // is what the NavMesh is baked from, so enemies can never be placed outside the arena.
            var ground = AddVisual(root, PrimitiveType.Plane, m.Ground, Vector3.zero, new Vector3(10f, 1f, 10f), name: "Ground");
            ground.layer = GameLayers.Ground;
            var floor = new GameObject("Floor") { layer = GameLayers.Ground };
            floor.transform.SetParent(root, false);
            var floorCollider = floor.AddComponent<BoxCollider>();
            floorCollider.center = new Vector3(0f, -0.25f, 0f);
            floorCollider.size = new Vector3(ArenaHalfSize * 2f, 0.5f, ArenaHalfSize * 2f);

            // Flat decoration (no colliders)
            AddVisual(root, PrimitiveType.Cylinder, m.Stone, new Vector3(0f, 0.02f, 0f), new Vector3(18f, 0.02f, 18f), name: "CentralPlaza");
            AddVisual(root, PrimitiveType.Cylinder, m.GroundPatch, new Vector3(0f, 0.03f, 0f), new Vector3(15f, 0.02f, 15f), name: "PlazaInlay");

            // Walls
            var wallLength = ArenaHalfSize * 2f + 4f;
            GameObject[] walls =
            {
                AddSolid(root, PrimitiveType.Cube, m.Wall, new Vector3(0f, 1.5f, ArenaHalfSize + 1f), new Vector3(wallLength, 3f, 2f), "WallNorth"),
                AddSolid(root, PrimitiveType.Cube, m.Wall, new Vector3(0f, 1.5f, -ArenaHalfSize - 1f), new Vector3(wallLength, 3f, 2f), "WallSouth"),
                AddSolid(root, PrimitiveType.Cube, m.Wall, new Vector3(ArenaHalfSize + 1f, 1.5f, 0f), new Vector3(2f, 3f, wallLength), "WallEast"),
                AddSolid(root, PrimitiveType.Cube, m.Wall, new Vector3(-ArenaHalfSize - 1f, 1.5f, 0f), new Vector3(2f, 3f, wallLength), "WallWest"),
            };
            // Wall tops are wide enough to bake as walkable islands; exclude them.
            foreach (var wall in walls)
            {
                var modifier = wall.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = NavMesh.GetAreaFromName("Not Walkable");
            }

            // Scattered obstacles, deterministic so the layout is stable between rebuilds.
            var random = new System.Random(LayoutSeed);
            var obstacles = new GameObject("Obstacles").transform;
            obstacles.SetParent(root, false);

            PlaceObstacles(random, 22, position => AddRock(obstacles, m, random, position));
            PlaceObstacles(random, 18, position => AddTree(obstacles, m, position));
            PlaceObstacles(random, 8, position => AddSolid(obstacles, PrimitiveType.Cube, m.Stone,
                position + Vector3.up * 1.5f, new Vector3(1.2f, 3f, 1.2f), "Pillar", new Vector3(0f, (float)random.NextDouble() * 90f, 0f)));

            foreach (var renderer in environment.GetComponentsInChildren<Renderer>())
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);

            return environment;
        }

        private static void PlaceObstacles(System.Random random, int count, System.Action<Vector3> place)
        {
            const float margin = 4f;
            var placed = 0;
            for (var attempt = 0; placed < count && attempt < count * 20; attempt++)
            {
                var x = (float)(random.NextDouble() * 2 - 1) * (ArenaHalfSize - margin);
                var z = (float)(random.NextDouble() * 2 - 1) * (ArenaHalfSize - margin);
                var position = new Vector3(x, 0f, z);

                if (position.magnitude < 11f || IsNear(position, ShrinePositions, 5f))
                    continue;

                place(position);
                placed++;
            }
        }

        private static bool IsNear(Vector3 position, Vector3[] points, float distance)
        {
            foreach (var point in points)
                if ((point - position).sqrMagnitude < distance * distance)
                    return true;
            return false;
        }

        private static void AddRock(Transform parent, MaterialSet m, System.Random random, Vector3 position)
        {
            var scale = new Vector3(Range(random, 1.8f, 3.6f), Range(random, 1.2f, 2.4f), Range(random, 1.8f, 3.6f));
            AddSolid(parent, PrimitiveType.Sphere, m.Stone, position + Vector3.up * scale.y * 0.3f, scale, "Rock",
                new Vector3(0f, Range(random, 0f, 360f), 0f));
        }

        private static void AddTree(Transform parent, MaterialSet m, Vector3 position)
        {
            var tree = new GameObject("Tree").transform;
            tree.SetParent(parent, false);
            tree.localPosition = position;
            AddSolid(tree, PrimitiveType.Cylinder, m.Wood, new Vector3(0f, 1.2f, 0f), new Vector3(0.6f, 1.2f, 0.6f), "Trunk");
            AddVisual(tree, PrimitiveType.Sphere, m.Leaves, new Vector3(0f, 3.3f, 0f), new Vector3(3f, 2.6f, 3f), name: "Canopy");
        }

        private static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        private static void BakeNavMesh(GameObject environment)
        {
            var surface = environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = (1 << GameLayers.Default) | (1 << GameLayers.Ground);
            surface.BuildNavMesh();

            var path = $"{ScenesFolder}/Arena/NavMesh-Environment.asset";
            EnsureFolder($"{ScenesFolder}/Arena");
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
        }

        // ---------------------------------------------------------------- Systems, camera, UI

        private static GameManager BuildSystems(MaterialSet m, ContentSet content, GameObject player, Health playerHealth, out LevelUpController levelUp)
        {
            var systems = new GameObject("Systems");

            var spawner = systems.AddComponent<EnemySpawner>();
            SerializedSetter.For(spawner)
                .Set("schedule", content.Schedule)
                .Set("player", player.transform)
                .Set("boss", content.Boss)
                // Twelve enemy types: keep the up-front instantiation (and load hitch) modest.
                .Set("prewarmPerEnemyType", 12)
                .Set("arenaHalfExtent", ArenaHalfSize - 2f)
                .Apply();

            var director = systems.AddComponent<EnemyDirector>();
            SerializedSetter.For(director).Set("player", playerHealth).Apply();

            var gameManager = systems.AddComponent<GameManager>();
            SerializedSetter.For(gameManager)
                .Set("input", content.Input)
                .Set("playerHealth", playerHealth)
                .Set("spawner", spawner)
                .Apply();

            var chests = systems.AddComponent<ChestSpawner>();
            SerializedSetter.For(chests)
                .Set("chestPrefab", content.Chest)
                .Set("player", player.transform)
                .Set("spawnEffect", content.ChestSpawnEffect)
                .Apply();

            var damageNumbers = systems.AddComponent<DamageNumberManager>();
            SerializedSetter.For(damageNumbers)
                .Set("labelPrefab", content.DamageLabel)
                .Set("criticalEffect", content.CritEffect)
                .Apply();

            levelUp = systems.AddComponent<LevelUpController>();
            SerializedSetter.For(levelUp)
                .Set("gameManager", gameManager)
                .Set("input", content.Input)
                .Set("config", content.LevelUp)
                .Set("stats", player.GetComponent<PlayerStats>())
                .Set("spellCaster", player.GetComponent<SpellCaster>())
                .Apply();

            var missions = systems.AddComponent<MissionDirector>();
            SerializedSetter.For(missions)
                .Set("missions", content.Missions)
                .Set("rewards", levelUp)
                .Set("player", player.transform)
                .Set("playerHealth", playerHealth)
                .Set("spawner", spawner)
                .Set("pointer", BuildMissionPointer(m, player.transform))
                .Apply();

            return gameManager;
        }

        private static void BuildCamera(Transform target)
        {
            var offset = new Vector3(0f, 18f, -12f);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#1B2330");
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 120f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<CinemachineBrain>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cameraObject.transform.SetPositionAndRotation(target.position + offset, Quaternion.Euler(56f, 0f, 0f));

            var followObject = new GameObject("Follow Camera");
            followObject.transform.SetPositionAndRotation(target.position + offset, Quaternion.Euler(56f, 0f, 0f));

            var virtualCamera = followObject.AddComponent<CinemachineCamera>();
            virtualCamera.Target.TrackingTarget = target;
            var lens = LensSettings.Default;
            lens.FieldOfView = 40f;
            lens.NearClipPlane = 0.5f;
            lens.FarClipPlane = 120f;
            virtualCamera.Lens = lens;

            var follow = followObject.AddComponent<CinemachineFollow>();
            follow.FollowOffset = offset;
            var tracker = follow.TrackerSettings;
            tracker.BindingMode = BindingMode.WorldSpace;
            tracker.PositionDamping = new Vector3(0.35f, 0.35f, 0.35f);
            follow.TrackerSettings = tracker;
        }

        private static void BuildUserInterface(GameManager gameManager, LevelUpController levelUp, LevelUpConfig levelUpConfig,
            GameObject player, Health playerHealth)
        {
            var panelSettings = LoadOrCreateAsset<PanelSettings>($"{UIFolder}/GamePanelSettings.asset");
            panelSettings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>($"{UIFolder}/GameTheme.tss");
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            EditorUtility.SetDirty(panelSettings);

            var uiObject = new GameObject("GameUI");
            var document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UIFolder}/GameUI.uxml");

            var hud = uiObject.AddComponent<HudController>();
            SerializedSetter.For(hud)
                .Set("gameManager", gameManager)
                .Set("playerHealth", playerHealth)
                .Set("spellCaster", player.GetComponent<SpellCaster>())
                .Apply();

            var menus = uiObject.AddComponent<MenuController>();
            SerializedSetter.For(menus)
                .Set("gameManager", gameManager)
                .Set("progression", player.GetComponent<PlayerProgression>())
                .Set("spellCaster", player.GetComponent<SpellCaster>())
                .Set("stats", player.GetComponent<PlayerStats>())
                .Set("levelUpConfig", levelUpConfig)
                .Apply();

            var levelUpView = uiObject.AddComponent<LevelUpView>();
            SerializedSetter.For(levelUpView)
                .Set("controller", levelUp)
                .Set("cardTemplate", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UIFolder}/UpgradeCard.uxml"))
                .Apply();
        }
    }
}
