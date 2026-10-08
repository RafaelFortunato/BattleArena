using System;
using System.Linq;
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
        /// <summary>Global size of every enemy prefab, on top of each enemy's own scale.</summary>
        private const float EnemySizeMultiplier = 1.2f;

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

        /// <summary>Enemy assets other builders need (the scene, missions).</summary>
        private sealed class EnemyRoster
        {
            public SpawnSchedule Schedule;
            public EnemyDefinition Boss;
            public EnemyDefinition GoldenGoblin;
            public EnemyDefinition Warlord;
            public EnemyDefinition[] Camp;
        }

        /// <summary>
        /// Builds every enemy: prefab (visual + behaviour components) and definition (stats), then the
        /// spawn schedule that introduces them over the match.
        /// </summary>
        private static EnemyRoster BuildEnemyRoster(MaterialSet m, ExperienceGem gem,
            TimedRelease deathEffect, DelayedAreaStrike bossSlam, Projectile bossOrb)
        {
            var kit = new EnemyKit(m, gem, deathEffect);

            // Enemy weapons
            var arrowHit = BuildSparkHit("ArrowHit", new Color(1f, 0.65f, 0.3f), 0.8f);
            var wispHit = BuildSparkHit("WispBoltHit", new Color(0.45f, 0.82f, 1f), 0.9f);
            var bombBlast = BuildBurst("BombExplosion", m, new Color(1f, 0.45f, 0.1f), 36, 5f, 0.55f, 0.55f, radius: 0.5f, fromGround: true);
            var curseEruption = BuildBurst("CurseEruption", m, new Color(0.45f, 1f, 0.3f), 30, 4f, 0.5f, 0.6f, radius: 0.6f, fromGround: true);
            var arrow = BuildEnemyArrow(m, arrowHit);
            var wispBolt = BuildWispBolt(wispHit);
            var bomb = BuildStrike("GoblinBomb", m.StrikeEnemyRing, m.StrikeEnemyFill, bombBlast, t => BombVisual(t, m));
            var curse = BuildStrike("ShamanCurse", m.StrikeEnemyRing, m.StrikeEnemyFill, curseEruption);
            var beetleBlast = BuildStrike("BeetleBlast", m.StrikeEnemyRing, m.StrikeEnemyFill, bombBlast);
            var dashLine = BuildLineTelegraph(m);
            var warlordSlam = BuildStrike("WarlordSlam", m.StrikeEnemyRing, m.StrikeEnemyFill, bombBlast);

            // --- Melee chasers
            var grunt = Define("Grunt", "Goblin Grunt", SaveEnemy(Melee(kit, "Grunt", 1f, 2.3f, v => Monster(v, new MonsterSpec("Orc", 1.75f, 3.2f, attack: "Weapon", material: m.MonsterGrunt), m, "Grunt"))),
                health: 22f, speed: 3f, damage: 5f, range: 1.3f, cooldown: 1f, xp: 1);

            var runner = Define("Runner", "Swift Imp", SaveEnemy(Melee(kit, "Runner", 0.75f, 2.3f, v => Monster(v, new MonsterSpec("Demon", 1.8f, 5.2f, move: "Run"), m, "Runner"))),
                health: 72f, speed: 5.2f, damage: 13f, range: 1.1f, cooldown: 0.8f, xp: 3);

            var brute = Define("Brute", "Ogre Brute", SaveEnemy(Melee(kit, "Brute", 1.5f, 2.3f, v => Monster(v, new MonsterSpec("Yeti", 1.75f, 2.3f), m, "Brute"))),
                health: 96f, speed: 2.3f, damage: 12f, range: 2f, cooldown: 1.4f, xp: 4);

            // --- Swarm Bat: fast, weaves side to side while closing in.
            var batRoot = NewEnemy(kit, "SwarmBat", 0.9f, 2.1f, v => Monster(v, MonsterSpec.Flyer("Armabee", 1.0f, 0.6f, 6f), m, "SwarmBat"));
            Behaviour<ZigZagMovement>(batRoot, ("amplitude", 2.5f), ("frequency", 1.1f));
            batRoot.AddComponent<ContactAttack>();
            var bat = Define("SwarmBat", "Swarm Bat", SaveEnemy(batRoot), health: 54f, speed: 6f, damage: 10f, range: 1.1f, cooldown: 0.8f, xp: 3);

            // --- Goblin Archer: keeps its distance and fires aimed arrows.
            var archerRoot = NewEnemy(kit, "GoblinArcher", 0.9f, 2.3f, v => Monster(v, new MonsterSpec("Tribal", 1.8f, 3.4f, attack: "Weapon"), m, "GoblinArcher"));
            Behaviour<KeepDistanceMovement>(archerRoot, ("preferredDistance", 9f), ("tolerance", 2f));
            Behaviour<ProjectileAttack>(archerRoot,
                ("range", 13f), ("cooldown", 2.6f), ("windup", 0.55f), ("baseDamage", 10f),
                ("windupIndicator", WindupIndicator(archerRoot, m.WindupGlow, new Vector3(0.35f, 1.2f, 0.55f), 0.28f)),
                ("projectilePrefab", arrow), ("muzzle", AddChild(archerRoot.transform, "Muzzle", new Vector3(0.35f, 1.2f, 0.6f))),
                ("projectileSpeed", 12f), ("projectileCount", 1));
            archerRoot.AddComponent<ContactAttack>();
            var archer = Define("GoblinArcher", "Goblin Archer", SaveEnemy(archerRoot), health: 48f, speed: 3f, damage: 4f, range: 1.2f, cooldown: 1f, xp: 2);

            // --- Arcane Wisp: circles the player and fires a three-bolt spread.
            var wispRoot = NewEnemy(kit, "ArcaneWisp", 1f, 2.4f, v => Monster(v, MonsterSpec.Flyer("Ghost", 1.35f, 0.35f, 4.2f, attack: "Punch"), m, "ArcaneWisp"));
            Behaviour<OrbitMovement>(wispRoot, ("radius", 7.5f), ("leadAngle", 35f));
            Behaviour<ProjectileAttack>(wispRoot,
                ("range", 11f), ("cooldown", 3f), ("windup", 0.45f), ("baseDamage", 23f),
                ("windupIndicator", WindupIndicator(wispRoot, m.WispBolt, new Vector3(0f, 1.4f, 0f), 1.15f)),
                ("projectilePrefab", wispBolt), ("muzzle", AddChild(wispRoot.transform, "Muzzle", new Vector3(0f, 1.3f, 0.5f))),
                ("projectileSpeed", 9f), ("projectileCount", 3), ("spreadAngle", 22f));
            wispRoot.AddComponent<ContactAttack>();
            var wisp = Define("ArcaneWisp", "Arcane Wisp", SaveEnemy(wispRoot), health: 168f, speed: 4.2f, damage: 10f, range: 1.1f, cooldown: 1f, xp: 5);

            // --- Goblin Bomber: lobs bombs that land where the player stood.
            var bomberRoot = NewEnemy(kit, "GoblinBomber", 1f, 2.3f, v => Monster(v, new MonsterSpec("Monkroose", 1.75f, 3.1f, attack: "Weapon"), m, "GoblinBomber"));
            Behaviour<KeepDistanceMovement>(bomberRoot, ("preferredDistance", 10f), ("tolerance", 2f));
            Behaviour<LobbedBombAttack>(bomberRoot,
                ("range", 14f), ("cooldown", 3.6f), ("windup", 0.6f), ("baseDamage", 16f),
                ("windupIndicator", WindupIndicator(bomberRoot, m.BombBody, new Vector3(0.45f, 1.55f, 0.2f), 0.45f)),
                ("bombPrefab", bomb), ("muzzle", AddChild(bomberRoot.transform, "Muzzle", new Vector3(0.45f, 1.55f, 0.2f))),
                ("flightTime", 1.1f), ("blastRadius", 2.3f));
            bomberRoot.AddComponent<ContactAttack>();
            var bomber = Define("GoblinBomber", "Goblin Bomber", SaveEnemy(bomberRoot), health: 66f, speed: 2.9f, damage: 5f, range: 1.2f, cooldown: 1f, xp: 3);

            // --- Dark Shaman: stays far back and curses the ground around the player.
            var shamanRoot = NewEnemy(kit, "DarkShaman", 1.1f, 2.4f, v => Monster(v, new MonsterSpec("BlueDemon", 1.85f, 2.5f, attack: "Weapon"), m, "DarkShaman"));
            Behaviour<KeepDistanceMovement>(shamanRoot, ("preferredDistance", 12f), ("tolerance", 2.5f));
            Behaviour<GroundStrikeAttack>(shamanRoot,
                ("range", 15f), ("cooldown", 4.5f), ("windup", 0.8f), ("baseDamage", 18f),
                ("windupIndicator", WindupIndicator(shamanRoot, m.CurseGlow, new Vector3(0.5f, 2.1f, 0.2f), 0.7f)),
                ("strikePrefab", curse), ("strikeRadius", 3f), ("strikeDelay", 1.2f), ("strikeCount", 2), ("scatter", 4f));
            shamanRoot.AddComponent<ContactAttack>();
            var shaman = Define("DarkShaman", "Dark Shaman", SaveEnemy(shamanRoot), health: 84f, speed: 2.5f, damage: 5f, range: 1.2f, cooldown: 1f, xp: 4);

            // --- Boar Charger: telegraphs a line, then dashes along it.
            var boarRoot = NewEnemy(kit, "BoarCharger", 1f, 1.9f, v => Monster(v, new MonsterSpec("Dino", 1.5f, 3.3f, move: "Run"), m, "BoarCharger"));
            boarRoot.AddComponent<ChaseMovement>();
            boarRoot.AddComponent<ContactAttack>();
            Behaviour<DashAttack>(boarRoot,
                ("range", 9f), ("cooldown", 4f), ("windup", 0.75f), ("baseDamage", 38f),
                ("windupIndicator", WindupIndicator(boarRoot, m.WindupGlow, new Vector3(0f, 0.85f, 1.2f), 0.5f)),
                ("telegraphPrefab", dashLine), ("minRange", 3f), ("dashDistance", 11f), ("dashSpeed", 20f), ("hitRadius", 1.4f));
            var boar = Define("BoarCharger", "Boar Charger", SaveEnemy(boarRoot), health: 264f, speed: 3.3f, damage: 20f, range: 1.6f, cooldown: 1.2f, xp: 6);

            // --- Bomb Beetle: rushes in and explodes after a short fuse.
            var beetleRoot = NewEnemy(kit, "BombBeetle", 0.85f, 1.4f, v => Monster(v, MonsterSpec.Blob("GreenSpikyBlob", 0.95f, 4.4f), m, "BombBeetle"));
            beetleRoot.AddComponent<ChaseMovement>();
            Behaviour<SelfDestructAttack>(beetleRoot,
                ("range", 2.4f), ("cooldown", 5f), ("windup", 0.9f), ("baseDamage", 38f),
                ("windupIndicator", WindupIndicator(beetleRoot, m.WindupGlow, new Vector3(0f, 0.5f, 0f), 1.3f)),
                ("blastPrefab", beetleBlast), ("blastRadius", 3f));
            var beetle = Define("BombBeetle", "Bomb Beetle", SaveEnemy(beetleRoot), health: 60f, speed: 4.4f, damage: 0f, range: 1f, cooldown: 1f, xp: 3);

            // --- Slimes: the big one splits into two small ones.
            var smallSlime = Define("SmallSlime", "Slimeling", SaveEnemy(Melee(kit, "SmallSlime", 0.7f, 1.5f, v => Monster(v, MonsterSpec.Blob("PinkBlob", 1.05f, 3.4f), m, "SmallSlime"))),
                health: 12f, speed: 3f, damage: 3f, range: 1.1f, cooldown: 1f, xp: 1);
            var bigSlimeRoot = Melee(kit, "BigSlime", 1.35f, 1.5f, v => Monster(v, MonsterSpec.Blob("GreenBlob", 1.1f, 2.1f), m, "BigSlime"));
            Behaviour<SplitOnDeath>(bigSlimeRoot, ("splitInto", smallSlime), ("count", 2), ("spread", 1.2f));
            var bigSlime = Define("BigSlime", "Giant Slime", SaveEnemy(bigSlimeRoot), health: 54f, speed: 2.1f, damage: 7f, range: 1.8f, cooldown: 1.2f, xp: 3);

            // --- Storm Wyvern: flies over obstacles faster than the player, but turns very slowly (35°/s at 9.5 m/s is a
            // ~31 m turning circle, about 5 s for a U-turn), so it overshoots and makes long loops back.
            var wyvernRoot = NewEnemy(kit, "StormWyvern", 1f, 3f, v => Monster(v, MonsterSpec.Flyer("Dragon", 1.3f, 1.0f, 9.5f), m, "StormWyvern"), flying: true);
            Behaviour<FlyingMovement>(wyvernRoot, ("turnRateDegrees", 35f), ("arenaHalfExtent", ArenaHalfSize - 1.5f));
            wyvernRoot.AddComponent<ContactAttack>();
            var wyvern = Define("StormWyvern", "Storm Wyvern", SaveEnemy(wyvernRoot), health: 192f, speed: 9.5f, damage: 23f, range: 1.3f, cooldown: 1f, xp: 6);

            // --- Mission targets (never spawned by the schedule)
            var goblinRoot = NewEnemy(kit, "GoldenGoblin", 0.85f, 2.3f, v =>
                Monster(v, new MonsterSpec("Orc", 1.75f, 4.6f, move: "Run", attack: "Weapon", material: m.MonsterGold), m, "GoldenGoblin"));
            Behaviour<FleeMovement>(goblinRoot, ("fleeDistance", 14f), ("fleeStep", 6f), ("wanderRadius", 6f));
            var goldenGoblin = Define("GoldenGoblin", "Golden Goblin", SaveEnemy(goblinRoot), health: 108f, speed: 4.6f, damage: 0f, range: 1f, cooldown: 1f, xp: 25);

            var warlordRoot = NewEnemy(kit, "OgreWarlord", 2.1f, 2.4f, v => Monster(v, new MonsterSpec("Orc_Skull", 1.8f, 2.6f, attack: "Weapon"), m, "OgreWarlord"));
            warlordRoot.AddComponent<ChaseMovement>();
            warlordRoot.AddComponent<ContactAttack>();
            Behaviour<GroundStrikeAttack>(warlordRoot,
                ("range", 8f), ("cooldown", 4f), ("windup", 0.9f), ("baseDamage", 22f),
                ("windupIndicator", WindupIndicator(warlordRoot, m.WindupGlow, new Vector3(0f, 1.9f, 0f), 0.5f)),
                ("strikePrefab", warlordSlam), ("strikeRadius", 3.5f), ("strikeDelay", 1f), ("strikeCount", 3), ("scatter", 3.5f));
            var warlord = Define("OgreWarlord", "Ogre Warlord", SaveEnemy(warlordRoot), health: 480f, speed: 2.6f, damage: 18f, range: 2.6f, cooldown: 1.3f, xp: 30);

            // --- Boss
            var bossRoot = NewEnemy(kit, "Boss", 2.6f, 0f, v => Monster(v, MonsterSpec.Flyer("Ghost_Skull", 1.6f, 0.25f, 2.7f, attack: "Punch"), m, "Boss"));
            bossRoot.AddComponent<ChaseMovement>();
            bossRoot.AddComponent<ContactAttack>();
            Behaviour<BossController>(bossRoot, ("slamPrefab", bossSlam), ("projectilePrefab", bossOrb),
                ("attackInterval", 2.8f), ("slamCount", 4), ("slamDamage", 50f), ("projectilesPerRing", 20), ("projectileDamage", 30f),
                ("projectileOrigin", AddChild(bossRoot.transform, "ProjectileOrigin", new Vector3(0f, 0.42f, 0.2f))));
            var boss = Define("Boss", "Archlich Morvath", SaveEnemy(bossRoot), health: 24000f, speed: 3f, damage: 36f, range: 3.4f, cooldown: 1.2f, xp: 0);

            // --- Spawn schedule. Enemies never scale; danger rises by enemy type, one tier at a time:
            //   Tier 1 (0:00) slow melee: grunt, giant slime, ogre brute.
            //   Tier 2 (2:30) slow ranged: archer, bomber, dark shaman.
            //   Tier 3 (4:30) fast swarmers: swift imp, swarm bat, bomb beetle.
            //   Tier 4 (6:30) elites: boar charger, arcane wisp, storm wyvern.
            // Older tiers thin out as new ones arrive; the last stage spawns only tiers 3 and 4. Ranged enemies
            // (archer, bomber, shaman, wisp) stay at 20-30% of each phase so melee pressure leads the fight.
            var schedule = LoadOrCreateAsset<SpawnSchedule>($"{DataFolder}/SpawnSchedule.asset");
            schedule.SetPhases(new[]
            {
                Phase(0f, interval: 1.6f, perTick: 1, maxAlive: 12, (grunt, 0.75f), (bigSlime, 0.25f)),
                Phase(75f, interval: 1.3f, perTick: 2, maxAlive: 25, (grunt, 0.55f), (bigSlime, 0.25f), (brute, 0.2f)),
                Phase(150f, interval: 1.2f, perTick: 2, maxAlive: 35,
                    (grunt, 0.3f), (bigSlime, 0.2f), (brute, 0.2f), (archer, 0.12f), (bomber, 0.1f), (shaman, 0.08f)),
                Phase(270f, interval: 1.1f, perTick: 2, maxAlive: 45,
                    (brute, 0.18f), (runner, 0.25f), (bat, 0.2f), (beetle, 0.12f), (archer, 0.1f), (bomber, 0.08f), (shaman, 0.07f)),
                Phase(390f, interval: 1.05f, perTick: 3, maxAlive: 55,
                    (runner, 0.2f), (bat, 0.16f), (beetle, 0.1f), (boar, 0.2f), (wyvern, 0.12f), (archer, 0.06f), (shaman, 0.06f), (wisp, 0.1f)),
                Phase(510f, interval: 1f, perTick: 3, maxAlive: 65,
                    (runner, 0.16f), (bat, 0.14f), (beetle, 0.12f), (boar, 0.2f), (wisp, 0.2f), (wyvern, 0.18f)),
            });
            EditorUtility.SetDirty(schedule);

            return new EnemyRoster
            {
                Schedule = schedule,
                Boss = boss,
                GoldenGoblin = goldenGoblin,
                Warlord = warlord,
                Camp = new[] { grunt, grunt, grunt, grunt, grunt, grunt, archer, archer, archer, brute, brute },
            };
        }

        // ---------------------------------------------------------------- Construction helpers

        /// <summary>Common enemy root: agent, collider, health, core component, damage flash and health bar.</summary>
        private static GameObject NewEnemy(in EnemyKit kit, string name, float scale, float healthBarHeight,
            Func<Transform, Renderer> buildVisual, bool flying = false)
        {
            var root = CreateRoot(name, GameLayers.Enemy);
            // Scaling the root keeps the model, collider, agent, health bar and attack origins in proportion.
            root.transform.localScale = Vector3.one * (scale * EnemySizeMultiplier);

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
            var flashRenderers = visual.GetComponentsInChildren<Renderer>().Where(r => r is not ParticleSystemRenderer).ToArray();
            SerializedSetter.For(flash).Set("renderers", flashRenderers.Length > 0 ? flashRenderers : new[] { bodyRenderer })
                .Set("flashMaterial", kit.M.Flash).Apply();

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
        private static Enemy SaveEnemy(GameObject root) => SavePrefab<Enemy>(root, $"{EnemyPrefabsFolder}/{root.name}.prefab");

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
