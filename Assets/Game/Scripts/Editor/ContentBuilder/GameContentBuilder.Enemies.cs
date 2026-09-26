using System;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Enemies;
using BattleArena.Pickups;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Editor.ContentBuilder
{
    internal static partial class GameContentBuilder
    {
        private const string EnemyPrefabsFolder = PrefabsFolder + "/Enemies";
        private const string EnemyDataFolder = DataFolder + "/Enemies";

        /// <summary>Shared assets every enemy prefab needs.</summary>
        private readonly struct EnemyKit
        {
            public readonly MaterialSet M;
            public readonly ExperienceGem Gem;
            public readonly TimedRelease DeathEffect;

            public EnemyKit(MaterialSet m, ExperienceGem gem, TimedRelease deathEffect)
            {
                M = m;
                Gem = gem;
                DeathEffect = deathEffect;
            }
        }

        /// <summary>
        /// Builds every enemy: prefab (visual + behaviour components) and definition (stats), then the
        /// spawn schedule that introduces them over the match.
        /// </summary>
        private static (SpawnSchedule schedule, EnemyDefinition boss) BuildEnemyRoster(MaterialSet m, ExperienceGem gem,
            TimedRelease deathEffect, DelayedAreaStrike bossSlam, Projectile bossOrb)
        {
            var kit = new EnemyKit(m, gem, deathEffect);

            // Enemy weapons
            var arrowHit = BuildBurst("ArrowHit", m, new Color(1f, 0.6f, 0.2f), 8, 3f, 0.25f, 0.3f);
            var wispHit = BuildBurst("WispBoltHit", m, new Color(0.4f, 0.85f, 1f), 8, 3f, 0.25f, 0.3f);
            var bombBlast = BuildBurst("BombExplosion", m, new Color(1f, 0.45f, 0.1f), 36, 5f, 0.55f, 0.55f, radius: 0.5f, fromGround: true);
            var curseEruption = BuildBurst("CurseEruption", m, new Color(0.45f, 1f, 0.3f), 30, 4f, 0.5f, 0.6f, radius: 0.6f, fromGround: true);
            var arrow = BuildProjectile("EnemyArrow", m.EnemyArrow, 0.22f, GameLayers.EnemyProjectile, 2.5f, arrowHit, length: 0.9f);
            var wispBolt = BuildProjectile("WispBolt", m.WispBolt, 0.4f, GameLayers.EnemyProjectile, 3f, wispHit);
            var bomb = BuildStrike("GoblinBomb", m.EnemyTelegraph, m.EnemyTelegraphFill, bombBlast, m.BombBody, fallingSize: 0.55f);
            var curse = BuildStrike("ShamanCurse", m.EnemyTelegraph, m.EnemyTelegraphFill, curseEruption, null);
            var beetleBlast = BuildStrike("BeetleBlast", m.EnemyTelegraph, m.EnemyTelegraphFill, bombBlast, null);
            var dashLine = BuildLineTelegraph(m);

            // --- Melee chasers
            var grunt = Define("Grunt", "Goblin Grunt", SaveEnemy(Melee(kit, "Grunt", 1f, 2.3f, v => Humanoid(v, m.Grunt, m))),
                health: 18f, speed: 3.2f, damage: 5f, range: 1.3f, cooldown: 1f, xp: 1);

            var runner = Define("Runner", "Swift Imp", SaveEnemy(Melee(kit, "Runner", 0.75f, 2.3f, v =>
                {
                    var body = Humanoid(v, m.Runner, m);
                    AddVisual(v, PrimitiveType.Cube, m.Runner, new Vector3(-0.3f, 1.85f, 0f), new Vector3(0.1f, 0.35f, 0.1f), new Vector3(0f, 0f, 20f), "EarL");
                    AddVisual(v, PrimitiveType.Cube, m.Runner, new Vector3(0.3f, 1.85f, 0f), new Vector3(0.1f, 0.35f, 0.1f), new Vector3(0f, 0f, -20f), "EarR");
                    return body;
                })),
                health: 10f, speed: 5.2f, damage: 4f, range: 1.1f, cooldown: 0.8f, xp: 1);

            var brute = Define("Brute", "Ogre Brute", SaveEnemy(Melee(kit, "Brute", 1.5f, 2.3f, v =>
                {
                    var body = Humanoid(v, m.Brute, m);
                    AddVisual(v, PrimitiveType.Cube, m.Stone, new Vector3(-0.35f, 1.75f, 0.1f), new Vector3(0.12f, 0.3f, 0.12f), new Vector3(0f, 0f, 30f), "HornL");
                    AddVisual(v, PrimitiveType.Cube, m.Stone, new Vector3(0.35f, 1.75f, 0.1f), new Vector3(0.12f, 0.3f, 0.12f), new Vector3(0f, 0f, -30f), "HornR");
                    AddVisual(v, PrimitiveType.Sphere, m.Brute, new Vector3(-0.55f, 1.1f, 0f), Vector3.one * 0.4f, name: "FistL");
                    AddVisual(v, PrimitiveType.Sphere, m.Brute, new Vector3(0.55f, 1.1f, 0f), Vector3.one * 0.4f, name: "FistR");
                    return body;
                })),
                health: 75f, speed: 2.3f, damage: 14f, range: 2f, cooldown: 1.4f, xp: 4);

            // --- Swarm Bat: fast, weaves side to side while closing in.
            var batRoot = NewEnemy(kit, "SwarmBat", 0.9f, 2.1f, v =>
            {
                var body = AddVisual(v, PrimitiveType.Sphere, m.Bat, new Vector3(0f, 1.3f, 0f), Vector3.one * 0.6f, name: "Body");
                AddVisual(v, PrimitiveType.Cube, m.Bat, new Vector3(-0.5f, 1.4f, 0f), new Vector3(0.8f, 0.06f, 0.4f), new Vector3(0f, 0f, 25f), "WingL");
                AddVisual(v, PrimitiveType.Cube, m.Bat, new Vector3(0.5f, 1.4f, 0f), new Vector3(0.8f, 0.06f, 0.4f), new Vector3(0f, 0f, -25f), "WingR");
                AddEyes(v, m, new Vector3(0f, 1.4f, 0.26f), 0.12f, 0.12f);
                return body.GetComponent<Renderer>();
            });
            Behaviour<ZigZagMovement>(batRoot, ("amplitude", 2.5f), ("frequency", 1.1f));
            batRoot.AddComponent<ContactAttack>();
            var bat = Define("SwarmBat", "Swarm Bat", SaveEnemy(batRoot), health: 8f, speed: 6f, damage: 3f, range: 1.1f, cooldown: 0.8f, xp: 1);

            // --- Goblin Archer: keeps its distance and fires aimed arrows.
            var archerRoot = NewEnemy(kit, "GoblinArcher", 0.9f, 2.3f, v =>
            {
                var body = Humanoid(v, m.Archer, m);
                AddVisual(v, PrimitiveType.Cylinder, m.Archer, new Vector3(0f, 1.78f, 0f), new Vector3(0.55f, 0.12f, 0.55f), name: "Hood");
                AddVisual(v, PrimitiveType.Cylinder, m.Wood, new Vector3(0.45f, 1.1f, 0.25f), new Vector3(0.05f, 0.55f, 0.05f), new Vector3(15f, 0f, 0f), "Bow");
                return body;
            });
            Behaviour<KeepDistanceMovement>(archerRoot, ("preferredDistance", 9f), ("tolerance", 2f));
            Behaviour<ProjectileAttack>(archerRoot,
                ("range", 13f), ("cooldown", 2.6f), ("windup", 0.55f), ("baseDamage", 8f),
                ("windupIndicator", WindupIndicator(archerRoot, m.WindupGlow, new Vector3(0.35f, 1.2f, 0.55f), 0.28f)),
                ("projectilePrefab", arrow), ("muzzle", AddChild(archerRoot.transform, "Muzzle", new Vector3(0.35f, 1.2f, 0.6f))),
                ("projectileSpeed", 12f), ("projectileCount", 1));
            archerRoot.AddComponent<ContactAttack>();
            var archer = Define("GoblinArcher", "Goblin Archer", SaveEnemy(archerRoot), health: 16f, speed: 3.4f, damage: 3f, range: 1.2f, cooldown: 1f, xp: 2);

            // --- Arcane Wisp: circles the player and fires a three-bolt spread.
            var wispRoot = NewEnemy(kit, "ArcaneWisp", 1f, 2.4f, v =>
            {
                var core = AddVisual(v, PrimitiveType.Sphere, m.Wisp, new Vector3(0f, 1.4f, 0f), Vector3.one * 0.8f, name: "Core");
                for (var i = 0; i < 3; i++)
                {
                    var angle = i * 120f * Mathf.Deg2Rad;
                    DisableShadows(AddVisual(v, PrimitiveType.Sphere, m.WispBolt, new Vector3(Mathf.Sin(angle) * 0.65f, 1.4f, Mathf.Cos(angle) * 0.65f),
                        Vector3.one * 0.2f, name: $"Mote{i}"));
                }
                return core.GetComponent<Renderer>();
            });
            Behaviour<OrbitMovement>(wispRoot, ("radius", 7.5f), ("leadAngle", 35f));
            Behaviour<ProjectileAttack>(wispRoot,
                ("range", 11f), ("cooldown", 3f), ("windup", 0.45f), ("baseDamage", 6f),
                ("windupIndicator", WindupIndicator(wispRoot, m.WispBolt, new Vector3(0f, 1.4f, 0f), 1.15f)),
                ("projectilePrefab", wispBolt), ("muzzle", AddChild(wispRoot.transform, "Muzzle", new Vector3(0f, 1.3f, 0.5f))),
                ("projectileSpeed", 9f), ("projectileCount", 3), ("spreadAngle", 22f));
            wispRoot.AddComponent<ContactAttack>();
            var wisp = Define("ArcaneWisp", "Arcane Wisp", SaveEnemy(wispRoot), health: 14f, speed: 4.2f, damage: 3f, range: 1.1f, cooldown: 1f, xp: 2);

            // --- Goblin Bomber: lobs bombs that land where the player stood.
            var bomberRoot = NewEnemy(kit, "GoblinBomber", 1f, 2.3f, v =>
            {
                var body = Humanoid(v, m.Bomber, m);
                AddVisual(v, PrimitiveType.Sphere, m.BombBody, new Vector3(0f, 1.2f, -0.45f), Vector3.one * 0.55f, name: "Backpack");
                return body;
            });
            Behaviour<KeepDistanceMovement>(bomberRoot, ("preferredDistance", 10f), ("tolerance", 2f));
            Behaviour<LobbedBombAttack>(bomberRoot,
                ("range", 14f), ("cooldown", 3.6f), ("windup", 0.6f), ("baseDamage", 14f),
                ("windupIndicator", WindupIndicator(bomberRoot, m.BombBody, new Vector3(0.45f, 1.55f, 0.2f), 0.45f)),
                ("bombPrefab", bomb), ("muzzle", AddChild(bomberRoot.transform, "Muzzle", new Vector3(0.45f, 1.55f, 0.2f))),
                ("flightTime", 1.1f), ("blastRadius", 2.3f));
            bomberRoot.AddComponent<ContactAttack>();
            var bomber = Define("GoblinBomber", "Goblin Bomber", SaveEnemy(bomberRoot), health: 22f, speed: 3.1f, damage: 4f, range: 1.2f, cooldown: 1f, xp: 3);

            // --- Dark Shaman: stays far back and curses the ground around the player.
            var shamanRoot = NewEnemy(kit, "DarkShaman", 1.1f, 2.4f, v =>
            {
                var body = Humanoid(v, m.Shaman, m);
                AddVisual(v, PrimitiveType.Cube, m.Stone, new Vector3(0f, 1.4f, 0.42f), new Vector3(0.35f, 0.3f, 0.08f), name: "Mask");
                AddVisual(v, PrimitiveType.Cylinder, m.Wood, new Vector3(0.5f, 1.05f, 0.2f), new Vector3(0.06f, 1f, 0.06f), name: "Staff");
                DisableShadows(AddVisual(v, PrimitiveType.Sphere, m.CurseGlow, new Vector3(0.5f, 2.1f, 0.2f), Vector3.one * 0.25f, name: "StaffOrb"));
                return body;
            });
            Behaviour<KeepDistanceMovement>(shamanRoot, ("preferredDistance", 12f), ("tolerance", 2.5f));
            Behaviour<GroundStrikeAttack>(shamanRoot,
                ("range", 15f), ("cooldown", 4.5f), ("windup", 0.8f), ("baseDamage", 16f),
                ("windupIndicator", WindupIndicator(shamanRoot, m.CurseGlow, new Vector3(0.5f, 2.1f, 0.2f), 0.7f)),
                ("strikePrefab", curse), ("strikeRadius", 3f), ("strikeDelay", 1.2f), ("strikeCount", 2), ("scatter", 4f));
            shamanRoot.AddComponent<ContactAttack>();
            var shaman = Define("DarkShaman", "Dark Shaman", SaveEnemy(shamanRoot), health: 36f, speed: 2.5f, damage: 4f, range: 1.2f, cooldown: 1f, xp: 4);

            // --- Boar Charger: telegraphs a line, then dashes along it.
            var boarRoot = NewEnemy(kit, "BoarCharger", 1f, 1.9f, v =>
            {
                var body = AddVisual(v, PrimitiveType.Cube, m.Boar, new Vector3(0f, 0.65f, 0f), new Vector3(0.9f, 0.8f, 1.5f), name: "Body");
                AddVisual(v, PrimitiveType.Cube, m.Boar, new Vector3(0f, 0.8f, 0.9f), new Vector3(0.65f, 0.6f, 0.55f), name: "Head");
                AddVisual(v, PrimitiveType.Cube, m.Stone, new Vector3(-0.22f, 0.6f, 1.2f), new Vector3(0.08f, 0.08f, 0.3f), new Vector3(-20f, 0f, 0f), "TuskL");
                AddVisual(v, PrimitiveType.Cube, m.Stone, new Vector3(0.22f, 0.6f, 1.2f), new Vector3(0.08f, 0.08f, 0.3f), new Vector3(-20f, 0f, 0f), "TuskR");
                AddEyes(v, m, new Vector3(0f, 0.95f, 1.16f), 0.18f, 0.12f);
                return body.GetComponent<Renderer>();
            });
            boarRoot.AddComponent<ChaseMovement>();
            boarRoot.AddComponent<ContactAttack>();
            Behaviour<DashAttack>(boarRoot,
                ("range", 9f), ("cooldown", 4f), ("windup", 0.75f), ("baseDamage", 16f),
                ("windupIndicator", WindupIndicator(boarRoot, m.WindupGlow, new Vector3(0f, 0.85f, 1.2f), 0.5f)),
                ("telegraphPrefab", dashLine), ("minRange", 3f), ("dashDistance", 11f), ("dashSpeed", 20f), ("hitRadius", 1.4f));
            var boar = Define("BoarCharger", "Boar Charger", SaveEnemy(boarRoot), health: 45f, speed: 3.3f, damage: 8f, range: 1.6f, cooldown: 1.2f, xp: 3);

            // --- Bomb Beetle: rushes in and explodes after a short fuse.
            var beetleRoot = NewEnemy(kit, "BombBeetle", 0.85f, 1.4f, v =>
            {
                var shell = AddVisual(v, PrimitiveType.Sphere, m.Beetle, new Vector3(0f, 0.4f, 0f), new Vector3(1.1f, 0.6f, 1.3f), name: "Shell");
                DisableShadows(AddVisual(v, PrimitiveType.Sphere, m.WindupGlow, new Vector3(0f, 0.68f, -0.1f), new Vector3(0.5f, 0.25f, 0.6f), name: "PowderSac"));
                AddEyes(v, m, new Vector3(0f, 0.45f, 0.6f), 0.15f, 0.12f);
                return shell.GetComponent<Renderer>();
            });
            beetleRoot.AddComponent<ChaseMovement>();
            Behaviour<SelfDestructAttack>(beetleRoot,
                ("range", 2.4f), ("cooldown", 5f), ("windup", 0.9f), ("baseDamage", 22f),
                ("windupIndicator", WindupIndicator(beetleRoot, m.WindupGlow, new Vector3(0f, 0.5f, 0f), 1.3f)),
                ("blastPrefab", beetleBlast), ("blastRadius", 3f));
            var beetle = Define("BombBeetle", "Bomb Beetle", SaveEnemy(beetleRoot), health: 12f, speed: 4.4f, damage: 0f, range: 1f, cooldown: 1f, xp: 2);

            // --- Slimes: the big one splits into two small ones.
            var smallSlime = Define("SmallSlime", "Slimeling", SaveEnemy(Melee(kit, "SmallSlime", 0.7f, 1.5f, v => SlimeBody(v, m))),
                health: 14f, speed: 3.4f, damage: 4f, range: 1.1f, cooldown: 1f, xp: 1);
            var bigSlimeRoot = Melee(kit, "BigSlime", 1.35f, 1.5f, v => SlimeBody(v, m));
            Behaviour<SplitOnDeath>(bigSlimeRoot, ("splitInto", smallSlime), ("count", 2), ("spread", 1.2f));
            var bigSlime = Define("BigSlime", "Giant Slime", SaveEnemy(bigSlimeRoot), health: 55f, speed: 2.1f, damage: 9f, range: 1.8f, cooldown: 1.2f, xp: 3);

            // --- Storm Wyvern: flies over obstacles faster than the player, but turns very slowly.
            var wyvernRoot = NewEnemy(kit, "StormWyvern", 1f, 3f, v =>
            {
                var body = AddVisual(v, PrimitiveType.Capsule, m.Wyvern, new Vector3(0f, 2f, 0f), new Vector3(0.5f, 0.7f, 0.5f), new Vector3(90f, 0f, 0f), "Body");
                AddVisual(v, PrimitiveType.Sphere, m.Wyvern, new Vector3(0f, 2.1f, 0.75f), Vector3.one * 0.42f, name: "Head");
                AddVisual(v, PrimitiveType.Cube, m.Wyvern, new Vector3(-0.9f, 2.1f, -0.1f), new Vector3(1.6f, 0.05f, 0.6f), new Vector3(0f, 0f, 15f), "WingL");
                AddVisual(v, PrimitiveType.Cube, m.Wyvern, new Vector3(0.9f, 2.1f, -0.1f), new Vector3(1.6f, 0.05f, 0.6f), new Vector3(0f, 0f, -15f), "WingR");
                AddVisual(v, PrimitiveType.Cube, m.Wyvern, new Vector3(0f, 2f, -0.9f), new Vector3(0.12f, 0.12f, 0.8f), name: "Tail");
                AddEyes(v, m, new Vector3(0f, 2.18f, 0.93f), 0.12f, 0.1f);
                return body.GetComponent<Renderer>();
            }, flying: true);
            Behaviour<FlyingMovement>(wyvernRoot, ("turnRateDegrees", 75f), ("arenaHalfExtent", ArenaHalfSize - 1.5f));
            wyvernRoot.AddComponent<ContactAttack>();
            var wyvern = Define("StormWyvern", "Storm Wyvern", SaveEnemy(wyvernRoot), health: 20f, speed: 9.5f, damage: 9f, range: 1.3f, cooldown: 1f, xp: 3);

            // --- Boss
            var bossRoot = NewEnemy(kit, "Boss", 2.6f, 0f, v =>
            {
                var body = Humanoid(v, m.Boss, m);
                for (var i = 0; i < 5; i++)
                {
                    var angle = i * 72f * Mathf.Deg2Rad;
                    var offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 0.3f;
                    AddVisual(v, PrimitiveType.Cube, m.Gold, new Vector3(offset.x, 1.9f, offset.z), new Vector3(0.1f, 0.3f, 0.1f), name: $"CrownSpike{i}");
                }
                AddVisual(v, PrimitiveType.Cylinder, m.Gold, new Vector3(0f, 1.78f, 0f), new Vector3(0.7f, 0.05f, 0.7f), name: "CrownBand");
                AddVisual(v, PrimitiveType.Sphere, m.BossOrb, new Vector3(0.6f, 1.1f, 0.2f), Vector3.one * 0.3f, name: "HandOrb");
                return body;
            });
            bossRoot.AddComponent<ChaseMovement>();
            bossRoot.AddComponent<ContactAttack>();
            Behaviour<BossController>(bossRoot, ("slamPrefab", bossSlam), ("projectilePrefab", bossOrb),
                ("projectileOrigin", AddChild(bossRoot.transform, "ProjectileOrigin", new Vector3(0f, 0.42f, 0.2f))));
            var boss = Define("Boss", "Archlich Morvath", SaveEnemy(bossRoot), health: 12000f, speed: 2.7f, damage: 20f, range: 3.4f, cooldown: 1.2f, xp: 0);

            // --- Spawn schedule: new threats are introduced over the ten minutes.
            var schedule = LoadOrCreateAsset<SpawnSchedule>($"{DataFolder}/SpawnSchedule.asset");
            schedule.SetPhases(new[]
            {
                Phase(0f, interval: 1.6f, perTick: 1, maxAlive: 12, (grunt, 1f)),
                Phase(45f, interval: 1.3f, perTick: 2, maxAlive: 25, (grunt, 0.55f), (runner, 0.15f), (bat, 0.3f)),
                Phase(120f, interval: 1.1f, perTick: 2, maxAlive: 40,
                    (grunt, 0.3f), (runner, 0.12f), (bat, 0.18f), (archer, 0.15f), (beetle, 0.12f), (brute, 0.08f), (bigSlime, 0.05f)),
                Phase(210f, interval: 1.0f, perTick: 3, maxAlive: 55,
                    (grunt, 0.2f), (runner, 0.1f), (bat, 0.14f), (archer, 0.12f), (beetle, 0.1f), (brute, 0.08f),
                    (bomber, 0.08f), (boar, 0.07f), (wisp, 0.08f), (wyvern, 0.05f)),
                Phase(300f, interval: 0.9f, perTick: 3, maxAlive: 70,
                    (grunt, 0.14f), (runner, 0.08f), (bat, 0.12f), (archer, 0.1f), (beetle, 0.1f), (brute, 0.09f),
                    (bomber, 0.08f), (boar, 0.08f), (wisp, 0.08f), (wyvern, 0.06f), (shaman, 0.06f), (bigSlime, 0.05f)),
                Phase(420f, interval: 0.85f, perTick: 4, maxAlive: 85,
                    (grunt, 0.1f), (runner, 0.08f), (bat, 0.1f), (archer, 0.1f), (beetle, 0.1f), (brute, 0.11f),
                    (bomber, 0.09f), (boar, 0.09f), (wisp, 0.08f), (wyvern, 0.07f), (shaman, 0.08f), (bigSlime, 0.06f)),
                Phase(510f, interval: 0.8f, perTick: 4, maxAlive: 100,
                    (grunt, 0.08f), (runner, 0.07f), (bat, 0.1f), (archer, 0.1f), (beetle, 0.1f), (brute, 0.12f),
                    (bomber, 0.1f), (boar, 0.1f), (wisp, 0.08f), (wyvern, 0.08f), (shaman, 0.09f), (bigSlime, 0.07f)),
            });
            EditorUtility.SetDirty(schedule);

            return (schedule, boss);
        }

        // ---------------------------------------------------------------- Construction helpers

        /// <summary>Common enemy root: agent, collider, health, core component, damage flash and health bar.</summary>
        private static GameObject NewEnemy(in EnemyKit kit, string name, float scale, float healthBarHeight,
            Func<Transform, Renderer> buildVisual, bool flying = false)
        {
            var root = CreateRoot(name, GameLayers.Enemy);
            root.transform.localScale = Vector3.one * scale;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.45f;
            agent.height = 1.8f;
            agent.baseOffset = 0f;
            agent.angularSpeed = 540f;
            agent.acceleration = 24f;
            agent.autoBraking = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            // Flyers move themselves over obstacles; the agent is kept (and disabled) for a uniform prefab layout.
            agent.enabled = !flying;

            var capsule = root.AddComponent<CapsuleCollider>();
            // Flyers sit higher, but the collider still spans shoulder height so player bolts connect.
            capsule.center = new Vector3(0f, flying ? 1.3f : 0.9f, 0f);
            capsule.height = flying ? 2.2f : 1.8f;
            capsule.radius = 0.45f;

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            root.AddComponent<Health>();

            var visual = AddChild(root.transform, "Visual", Vector3.zero);
            var bodyRenderer = buildVisual(visual);

            var enemy = root.AddComponent<Enemy>();
            SerializedSetter.For(enemy).Set("experienceGemPrefab", kit.Gem).Set("deathEffect", kit.DeathEffect).Apply();

            var flash = root.AddComponent<DamageFlash>();
            SerializedSetter.For(flash).Set("renderers", new[] { bodyRenderer }).Set("flashMaterial", kit.M.Flash).Apply();

            if (healthBarHeight > 0f)
                AddHealthBar(root, kit.M, healthBarHeight);

            return root;
        }

        /// <summary>A plain melee chaser.</summary>
        private static GameObject Melee(in EnemyKit kit, string name, float scale, float healthBarHeight, Func<Transform, Renderer> buildVisual)
        {
            var root = NewEnemy(kit, name, scale, healthBarHeight, buildVisual);
            root.AddComponent<ChaseMovement>();
            root.AddComponent<ContactAttack>();
            return root;
        }

        /// <summary>Adds a component and writes its serialized fields.</summary>
        private static T Behaviour<T>(GameObject root, params (string property, object value)[] settings) where T : Component
        {
            var component = root.AddComponent<T>();
            var setter = SerializedSetter.For(component);
            foreach (var (property, value) in settings)
                setter.Set(property, value);
            setter.Apply();
            return component;
        }

        /// <summary>Glowing marker shown only while an attack winds up.</summary>
        private static GameObject WindupIndicator(GameObject root, Material material, Vector3 localPosition, float size)
        {
            var indicator = AddVisual(root.transform, PrimitiveType.Sphere, material, localPosition, Vector3.one * size, name: "WindupIndicator");
            DisableShadows(indicator);
            indicator.SetActive(false);
            return indicator;
        }

        private static Renderer Humanoid(Transform visual, Material material, MaterialSet m)
        {
            var body = AddVisual(visual, PrimitiveType.Capsule, material, new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 0.9f, 0.9f), name: "Body");
            AddEyes(visual, m, new Vector3(0f, 1.35f, 0.36f), 0.17f, 0.16f);
            return body.GetComponent<Renderer>();
        }

        private static Renderer SlimeBody(Transform visual, MaterialSet m)
        {
            var body = AddVisual(visual, PrimitiveType.Sphere, m.Slime, new Vector3(0f, 0.48f, 0f), new Vector3(1.3f, 0.95f, 1.3f), name: "Body");
            AddEyes(visual, m, new Vector3(0f, 0.7f, 0.55f), 0.22f, 0.14f);
            return body.GetComponent<Renderer>();
        }

        private static void AddEyes(Transform visual, MaterialSet m, Vector3 center, float spacing, float size)
        {
            // Tiny details don't need shadows; the sphere primitive is comparatively dense.
            DisableShadows(AddVisual(visual, PrimitiveType.Sphere, m.EnemyEyes, center + Vector3.left * spacing, Vector3.one * size, name: "EyeL"));
            DisableShadows(AddVisual(visual, PrimitiveType.Sphere, m.EnemyEyes, center + Vector3.right * spacing, Vector3.one * size, name: "EyeR"));
        }

        private static Enemy SaveEnemy(GameObject root) => SavePrefab<Enemy>(root, $"{EnemyPrefabsFolder}/{root.name}.prefab");

        private static LineTelegraph BuildLineTelegraph(MaterialSet m)
        {
            var root = CreateRoot("DashTelegraph", GameLayers.Default);
            var outline = AddVisual(root.transform, PrimitiveType.Cube, m.EnemyTelegraph, Vector3.zero, new Vector3(1f, 0.02f, 1f), name: "Outline");
            var fill = AddVisual(root.transform, PrimitiveType.Cube, m.EnemyTelegraphFill, new Vector3(0f, 0.01f, 0f), new Vector3(1f, 0.02f, 0f), name: "Fill");
            DisableShadows(outline);
            DisableShadows(fill);

            var telegraph = root.AddComponent<LineTelegraph>();
            SerializedSetter.For(telegraph).Set("outline", outline.transform).Set("fill", fill.transform).Apply();
            return SavePrefab<LineTelegraph>(root, $"{PrefabsFolder}/Vfx/DashTelegraph.prefab");
        }

        private static EnemyDefinition Define(string file, string displayName, Enemy prefab, float health, float speed,
            float damage, float range, float cooldown, int xp)
        {
            var definition = LoadOrCreateAsset<EnemyDefinition>($"{EnemyDataFolder}/{file}.asset");
            SerializedSetter.For(definition)
                .Set("displayName", displayName)
                .Set("prefab", prefab)
                .Set("maxHealth", health)
                .Set("moveSpeed", speed)
                .Set("contactDamage", damage)
                .Set("attackRange", range)
                .Set("attackCooldown", cooldown)
                .Set("experienceReward", xp)
                .Apply();
            return definition;
        }

        private static SpawnSchedule.Phase Phase(float start, float interval, int perTick, int maxAlive,
            params (EnemyDefinition enemy, float weight)[] enemies)
        {
            var entries = new SpawnSchedule.WeightedEnemy[enemies.Length];
            for (var i = 0; i < enemies.Length; i++)
                entries[i] = new SpawnSchedule.WeightedEnemy { Enemy = enemies[i].enemy, Weight = enemies[i].weight };

            return new SpawnSchedule.Phase
            {
                StartTime = start,
                SpawnInterval = interval,
                SpawnsPerTick = perTick,
                MaxAlive = maxAlive,
                Enemies = entries,
            };
        }
    }
}
