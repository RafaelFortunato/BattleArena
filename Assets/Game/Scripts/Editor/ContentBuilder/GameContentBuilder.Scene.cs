using System.Collections.Generic;
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

        // Autumn churchyard layout. The healing wells sit on the dirt path loop (see GameContentBuilder.Paths).
        private static readonly Vector3[] WellPositions = { new(-18f, 0f, 14f), new(20f, 0f, -16f) };
        private static readonly (Vector3 center, float length, float yaw)[] RuinWalls =
        {
            (new Vector3(-25f, 0f, 24f), 12f, 8f),
            (new Vector3(28f, 0f, 8f), 12f, 82f),
            (new Vector3(-8f, 0f, -28f), 6f, -6f),
        };
        private static readonly (Vector3 center, float yaw)[] GraveGroups =
        {
            (new Vector3(13f, 0f, 14f), 12f),
            (new Vector3(-26f, 0f, -8f), -80f),
            (new Vector3(12f, 0f, -26f), 4f),
            (new Vector3(-4f, 0f, 26f), -6f),
        };

        private static void BuildScene(Scene scene, MaterialSet m, ContentSet content, SoundSet sounds)
        {
            BuildLighting();
            var environment = BuildEnvironment(m, content.Well);
            BakeNavMesh(environment);

            var player = PlacePrefab(content.Player, Vector3.zero, Quaternion.identity);
            var playerHealth = player.GetComponent<Health>();

            var gameManager = BuildSystems(m, content, player, playerHealth, out var levelUp);
            BuildCamera(player.transform);
            BuildUserInterface(gameManager, levelUp, content.LevelUp, player, playerHealth);
            BuildSceneAudio(sounds, playerHealth);

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

        private static GameObject BuildEnvironment(MaterialSet m, HealingWell well)
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

            // Props are prefab instances so their art can be replaced in one place.
            var props = BuildEnvironmentProps(m);
            PlaceProp(props.GroundPaths, root, Vector3.zero, 0f, Vector3.one);

            var walls = new GameObject("Walls").transform;
            walls.SetParent(root, false);
            var wallLength = ArenaHalfSize * 2f + 4f;
            PlaceWall(props.WallSegment, walls, new Vector3(0f, 0f, ArenaHalfSize + 1f), wallLength, 0f);
            PlaceWall(props.WallSegment, walls, new Vector3(0f, 0f, -ArenaHalfSize - 1f), wallLength, 0f);
            PlaceWall(props.WallSegment, walls, new Vector3(ArenaHalfSize + 1f, 0f, 0f), wallLength, 90f);
            PlaceWall(props.WallSegment, walls, new Vector3(-ArenaHalfSize - 1f, 0f, 0f), wallLength, 90f);

            // Only large, readable obstacles: small clutter would eat projectiles in the middle of a fight.
            var random = new System.Random(LayoutSeed);
            var reserved = new List<(Vector3 position, float radius)>(PathReservations());
            foreach (var position in WellPositions)
                reserved.Add((position, 6f));

            var landmarks = new GameObject("Landmarks").transform;
            landmarks.SetParent(root, false);
            foreach (var (center, length, yaw) in RuinWalls)
            {
                PlaceWall(props.WallSegment, landmarks, center, length, yaw);
                reserved.Add((center, length * 0.5f + 3f));
            }

            // A few big grave markers in small groups rather than a field of little headstones.
            foreach (var (center, yaw) in GraveGroups)
            {
                var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                for (var i = -1; i <= 1; i++)
                {
                    var marker = i == 0 ? props.Headstone : props.CelticCross;
                    // +180°: the carved face of the imported markers points away from the camera otherwise.
                    PlaceProp(marker, landmarks, center + right * (i * 2.6f), yaw + 180f + Range(random, -8f, 8f), Vector3.one);
                }
                reserved.Add((center, 6f));
            }

            var obstacles = new GameObject("Obstacles").transform;
            obstacles.SetParent(root, false);
            PlaceObstacles(random, 16, reserved, position => PlaceProp(props.AutumnTree, obstacles, position, Range(random, 0f, 360f),
                Vector3.one * Range(random, 0.95f, 1.25f)));
            PlaceObstacles(random, 4, reserved, position => PlaceProp(props.LanternPine, obstacles, position, Range(random, 0f, 360f),
                Vector3.one * Range(random, 0.95f, 1.15f)));
            PlaceObstacles(random, 5, reserved, position => PlaceProp(props.Boulder, obstacles, position, Range(random, 0f, 360f),
                Vector3.one * Range(random, 1.8f, 2.4f)));
            PlaceObstacles(random, 3, reserved, position => PlaceProp(props.Slab, obstacles, position, Range(random, 0f, 360f),
                Vector3.one * Range(random, 1.8f, 2.2f)));

            // Leaf litter is flat decoration and may sit anywhere.
            var decoration = new GameObject("Decoration").transform;
            decoration.SetParent(root, false);
            for (var i = 0; i < 22; i++)
            {
                var position = new Vector3(Range(random, -1f, 1f), 0f, Range(random, -1f, 1f)) * (ArenaHalfSize - 2f);
                PlaceProp(props.LeafLitter, decoration, position, Range(random, 0f, 360f), Vector3.one * Range(random, 1f, 1.6f));
            }

            foreach (var renderer in environment.GetComponentsInChildren<Renderer>())
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);

            // Wells join the environment before the NavMesh bake so enemies path around them, but after the
            // static batching pass: their water moves.
            foreach (var position in WellPositions)
                PlaceProp(well.gameObject, root, position, 0f, Vector3.one);

            return environment;
        }

        /// <summary>Scatters props away from the spawn area, reserved spots and each other.</summary>
        private static void PlaceObstacles(System.Random random, int count, List<(Vector3 position, float radius)> reserved,
            System.Action<Vector3> place)
        {
            const float margin = 4f;
            const float spacing = 3.5f;
            var placed = 0;
            for (var attempt = 0; placed < count && attempt < count * 30; attempt++)
            {
                var x = (float)(random.NextDouble() * 2 - 1) * (ArenaHalfSize - margin);
                var z = (float)(random.NextDouble() * 2 - 1) * (ArenaHalfSize - margin);
                var position = new Vector3(x, 0f, z);

                if (position.magnitude < 11f || IsReserved(position, reserved))
                    continue;

                place(position);
                reserved.Add((position, spacing));
                placed++;
            }
        }

        private static bool IsReserved(Vector3 position, List<(Vector3 position, float radius)> reserved)
        {
            foreach (var (point, radius) in reserved)
                if ((point - position).sqrMagnitude < radius * radius)
                    return true;
            return false;
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
                .Set("playerHealth", playerHealth)
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
                .Set("gameManager", gameManager)
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
