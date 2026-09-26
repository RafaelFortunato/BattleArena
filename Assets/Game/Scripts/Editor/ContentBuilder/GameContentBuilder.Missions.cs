using BattleArena.Core;
using BattleArena.Missions;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Editor.ContentBuilder
{
    internal static partial class GameContentBuilder
    {
        private const string MissionsFolder = DataFolder + "/Missions";

        private static MissionDefinition[] BuildMissions(MaterialSet m, EnemyRoster roster, TimedRelease collectEffect)
        {
            var goldZone = BuildMissionZone("HoldZone", m.MissionZone, m.MissionZoneFill);
            var campZone = BuildMissionZone("CampZone", m.EnemyTelegraph, m.EnemyTelegraphFill);
            var shard = BuildMissionShard(m, collectEffect);

            return new MissionDefinition[]
            {
                Mission<HuntMission>("GoldenGoblin", "Golden Goblin",
                    "A greedy goblin is fleeing with treasure. Chase it down before it escapes!", timeLimit: 35f,
                    s => s.Set("target", roster.GoldenGoblin).Set("healthScale", 1f).Set("minSpawnDistance", 14f).Set("maxSpawnDistance", 20f)),
                Mission<HuntMission>("WarlordHunt", "Warlord Hunt",
                    "An Ogre Warlord has entered the arena. Defeat it.", timeLimit: 50f, weight: 0.8f,
                    configure: s => s.Set("target", roster.Warlord).Set("healthScale", 1f).Set("minSpawnDistance", 14f).Set("maxSpawnDistance", 20f)),
                Mission<HoldZoneMission>("HoldTheCircle", "Hold the Circle",
                    "Stand inside the marked circle for 12 seconds.", timeLimit: 40f,
                    s => s.Set("zonePrefab", goldZone).Set("radius", 4f).Set("requiredSeconds", 12f)),
                Mission<ClearAreaMission>("ClearTheCamp", "Clear the Camp",
                    "A war camp has been raised nearby. Defeat every enemy in it.", timeLimit: 50f,
                    s => s.Set("zonePrefab", campZone).Set("camp", roster.Camp).Set("campRadius", 4.5f)),
                Mission<CollectShardsMission>("ArcaneShards", "Arcane Shards",
                    "Collect the 3 arcane shards scattered around the arena.", timeLimit: 45f,
                    s => s.Set("shardPrefab", shard).Set("shardCount", 3)),
                Mission<KillCountMission>("Massacre", "Massacre",
                    "Defeat 30 enemies before time runs out.", timeLimit: 35f,
                    s => s.Set("killCount", 30)),
                Mission<UntouchableMission>("Untouchable", "Untouchable",
                    "Survive 20 seconds without taking any damage.", timeLimit: 21f, weight: 0.7f,
                    configure: s => s.Set("surviveSeconds", 20f)),
            };
        }

        private static T Mission<T>(string file, string title, string description, float timeLimit,
            System.Func<SerializedSetter, SerializedSetter> configure, float weight = 1f) where T : MissionDefinition
        {
            var mission = LoadOrCreateAsset<T>($"{MissionsFolder}/{file}.asset");
            var setter = SerializedSetter.For(mission)
                .Set("title", title)
                .Set("description", description)
                .Set("timeLimit", timeLimit)
                .Set("weight", weight);
            configure(setter).Apply();
            return mission;
        }

        private static MissionZone BuildMissionZone(string name, Material outlineMaterial, Material fillMaterial)
        {
            var root = CreateRoot(name, GameLayers.Default);
            var outline = AddVisual(root.transform, PrimitiveType.Cylinder, outlineMaterial, new Vector3(0f, 0.03f, 0f), new Vector3(1f, 0.01f, 1f), name: "Outline");
            var fill = AddVisual(root.transform, PrimitiveType.Cylinder, fillMaterial, new Vector3(0f, 0.05f, 0f), new Vector3(0f, 0.01f, 0f), name: "Fill");
            DisableShadows(outline);
            DisableShadows(fill);

            var zone = root.AddComponent<MissionZone>();
            SerializedSetter.For(zone).Set("outline", outline.transform).Set("fill", fill.transform).Apply();
            return SavePrefab<MissionZone>(root, $"{PrefabsFolder}/Missions/{name}.prefab");
        }

        private static MissionShard BuildMissionShard(MaterialSet m, TimedRelease collectEffect)
        {
            var root = CreateRoot("ArcaneShard", GameLayers.Pickup);

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            AddVisual(root.transform, PrimitiveType.Cube, m.ArcaneShard, Vector3.zero, new Vector3(0.45f, 0.9f, 0.45f), new Vector3(0f, 45f, 0f), "Crystal");
            DisableShadows(AddVisual(root.transform, PrimitiveType.Cylinder, m.MissionZoneFill, new Vector3(0f, -0.95f, 0f), new Vector3(1.6f, 0.01f, 1.6f), name: "Glow"));

            var shard = root.AddComponent<MissionShard>();
            SerializedSetter.For(shard).Set("collectEffect", collectEffect).Apply();
            return SavePrefab<MissionShard>(root, $"{PrefabsFolder}/Missions/ArcaneShard.prefab");
        }

        /// <summary>Scene object: a gold chevron that circles the player and points at the mission objective.</summary>
        private static MissionPointer BuildMissionPointer(MaterialSet m, Transform player)
        {
            var root = new GameObject("MissionPointer");
            var arrow = new GameObject("Arrow").transform;
            arrow.SetParent(root.transform, false);

            DisableShadows(AddVisual(arrow, PrimitiveType.Cube, m.PointerGlow, new Vector3(-0.22f, 0f, 0f), new Vector3(0.12f, 0.04f, 0.7f), new Vector3(0f, 40f, 0f), "Left"));
            DisableShadows(AddVisual(arrow, PrimitiveType.Cube, m.PointerGlow, new Vector3(0.22f, 0f, 0f), new Vector3(0.12f, 0.04f, 0.7f), new Vector3(0f, -40f, 0f), "Right"));

            var pointer = root.AddComponent<MissionPointer>();
            SerializedSetter.For(pointer).Set("player", player).Set("arrow", arrow.gameObject).Apply();
            return pointer;
        }
    }
}
