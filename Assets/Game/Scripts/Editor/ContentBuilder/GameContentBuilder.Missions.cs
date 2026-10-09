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
            var goldZone = BuildMissionZone("HoldZone", m.MissionZone, m.MissionZoneFill, FxMaterial("ZoneGoldRune", "vfx_rune_arcane", MissionGold, 1.1f));
            var campZone = BuildMissionZone("CampZone", m.StrikeEnemyRing, m.StrikeEnemyFill, FxMaterial("ZoneCampRune", "vfx_rune_meteor", EnemyRed, 0.9f));
            var shard = BuildMissionShard(m, collectEffect);

            return new MissionDefinition[]
            {
                Mission<HuntMission>("TreasureBunny", "Treasure Bunny",
                    "The Treasure Bunny is fleeing with its loot. Chase it down before it escapes!", timeLimit: 60f,
                    s => s.Set("target", roster.TreasureBunny).Set("healthScale", 1f).Set("healthGrowthPerMinute", 1f)
                        .Set("minSpawnDistance", 14f).Set("maxSpawnDistance", 20f)),
                Mission<HuntMission>("WarlordHunt", "Warlord Hunt",
                    "An Ogre Warlord has entered the arena. Defeat it.", timeLimit: 50f, weight: 0.8f,
                    configure: s => s.Set("target", roster.Warlord).Set("healthScale", 1f).Set("minSpawnDistance", 14f).Set("maxSpawnDistance", 20f)),
                Mission<HoldZoneMission>("HoldTheCircle", "Hold the Circle",
                    "Stand inside the marked circle for 12 seconds.", timeLimit: 40f,
                    s => s.Set("zonePrefab", goldZone).Set("radius", 4f).Set("requiredSeconds", 12f)),
                Mission<ClearAreaMission>("ClearTheCamp", "Blood Ritual",
                    "A blood ritual has begun nearby. Slay {0} enemies inside its circle to stop it.", timeLimit: 60f,
                    s => s.Set("zonePrefab", campZone).Set("camp", roster.Camp).Set("campRadius", 9f)
                        .Set("killsRequired", 12).Set("killsPerMinute", 1.5f)
                        .Set("minDistance", 18f).Set("maxDistance", 26f)),
                Mission<CollectShardsMission>("ArcaneShards", "Arcane Shards",
                    "Collect the 3 arcane shards scattered around the arena.", timeLimit: 45f,
                    s => s.Set("shardPrefab", shard).Set("shardCount", 3)),
                Mission<KillCountMission>("Massacre", "Massacre",
                    "Defeat {0} enemies before time runs out.", timeLimit: 35f,
                    s => s.Set("killCount", 15).Set("killsPerMinute", 2f)),
                Mission<UntouchableMission>("Untouchable", "Untouchable",
                    "Take less than {0} damage.", timeLimit: 21f, weight: 0.7f,
                    configure: s => s.Set("surviveSeconds", 20f).Set("allowedDamageFraction", 0.1f)),
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

    }
}
