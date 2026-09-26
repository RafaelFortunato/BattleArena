using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Enemies;
using BattleArena.Interaction;
using BattleArena.Pickups;
using BattleArena.Player;
using BattleArena.Pooling;
using BattleArena.Progression;
using BattleArena.Spells;
using BattleArena.Vfx;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using TMPro;

namespace BattleArena.Editor.ContentBuilder
{
    internal static partial class GameContentBuilder
    {
        /// <summary>Assets the scene builder needs to reference.</summary>
        private sealed class ContentSet
        {
            public InputReader Input;
            public GameObject Player;
            public SpawnSchedule Schedule;
            public EnemyDefinition Boss;
            public HealingShrine Shrine;
            public TreasureChest Chest;
            public TimedRelease ChestSpawnEffect;
            public LevelUpConfig LevelUp;
            public TextMeshPro DamageLabel;
            public TimedRelease CritEffect;
        }

        private static ContentSet BuildContent(MaterialSet m)
        {
            // Effects
            var hitSpark = BuildBurst("HitSpark", m, new Color(0.6f, 0.9f, 1f), 10, 5f, 0.25f, 0.3f);
            var explosion = BuildBurst("FireballExplosion", m, new Color(1f, 0.55f, 0.15f), 40, 9f, 0.7f, 0.5f, radius: 0.5f);
            var deathPuff = BuildBurst("EnemyDeath", m, new Color(1f, 0.9f, 0.6f), 16, 4f, 0.4f, 0.45f, radius: 0.4f);
            var meteorImpact = BuildBurst("MeteorImpact", m, new Color(1f, 0.45f, 0.1f), 36, 4f, 0.5f, 0.6f, radius: 0.6f, fromGround: true);
            var slamImpact = BuildBurst("BossSlamImpact", m, new Color(0.85f, 0.3f, 1f), 36, 4f, 0.5f, 0.6f, radius: 0.6f, fromGround: true);
            var orbImpact = BuildBurst("BossOrbImpact", m, new Color(0.85f, 0.4f, 1f), 8, 3f, 0.25f, 0.3f);
            var frostRing = BuildRing("FrostNovaRing", m.FrostRing);
            var critSpark = BuildBurst("CritSpark", m, new Color(1f, 0.7f, 0.15f), 18, 7f, 0.35f, 0.35f, radius: 0.2f);
            var chestSparkle = BuildBurst("ChestSparkle", m, new Color(1f, 0.85f, 0.35f), 30, 5f, 0.4f, 0.6f, radius: 0.6f);

            // Projectiles and telegraphed strikes
            var playerBolt = BuildProjectile("PlayerBolt", m.PlayerBolt, 0.35f, GameLayers.PlayerProjectile, 1.2f, hitSpark);
            var fireball = BuildProjectile("Fireball", m.Fireball, 0.8f, GameLayers.PlayerProjectile, 1.6f, explosion);
            var bossOrb = BuildProjectile("BossOrb", m.BossOrb, 0.55f, GameLayers.EnemyProjectile, 4f, orbImpact);
            var meteorStrike = BuildStrike("MeteorStrike", m.SpellTelegraph, m.SpellTelegraphFill, meteorImpact, m.Meteor);
            var bossSlam = BuildStrike("BossSlam", m.EnemyTelegraph, m.EnemyTelegraphFill, slamImpact, null);

            var gem = BuildGem(m);

            // Enemies
            var gruntPrefab = BuildEnemy("Grunt", m.Grunt, 1f, gem, deathPuff, m, EnemyShape.Grunt);
            var runnerPrefab = BuildEnemy("Runner", m.Runner, 0.75f, gem, deathPuff, m, EnemyShape.Runner);
            var brutePrefab = BuildEnemy("Brute", m.Brute, 1.5f, gem, deathPuff, m, EnemyShape.Brute);
            var bossPrefab = BuildBoss(m, gem, deathPuff, bossSlam, bossOrb);

            var grunt = EnemyData("Grunt", "Goblin Grunt", gruntPrefab, health: 18f, speed: 3.2f, damage: 5f, range: 1.3f, cooldown: 1f, xp: 1);
            var runner = EnemyData("Runner", "Swift Imp", runnerPrefab, health: 10f, speed: 5.2f, damage: 4f, range: 1.1f, cooldown: 0.8f, xp: 1);
            var brute = EnemyData("Brute", "Ogre Brute", brutePrefab, health: 75f, speed: 2.3f, damage: 14f, range: 2f, cooldown: 1.4f, xp: 4);
            var boss = EnemyData("Boss", "Archlich Morvath", bossPrefab.GetComponent<Enemy>(), health: 12000f, speed: 2.7f, damage: 20f, range: 3.4f, cooldown: 1.2f, xp: 0);

            var schedule = BuildSpawnSchedule(grunt, runner, brute);

            // Spells and level-up cards
            var spells = BuildSpells(m, fireball, frostRing, meteorStrike);
            var levelUp = BuildLevelUpConfig(spells);

            // Input
            var input = LoadOrCreateAsset<InputReader>($"{DataFolder}/InputReader.asset");
            SerializedSetter.For(input)
                .Set("actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>($"{SettingsFolder}/GameControls.inputactions"))
                .Apply();

            var player = BuildPlayer(m, input, playerBolt);

            return new ContentSet
            {
                Input = input,
                Player = player,
                Schedule = schedule,
                Boss = boss,
                Shrine = BuildShrine(m),
                Chest = BuildChest(m, chestSparkle),
                ChestSpawnEffect = chestSparkle,
                LevelUp = levelUp,
                DamageLabel = BuildDamageLabel(m),
                CritEffect = critSpark,
            };
        }

        // ---------------------------------------------------------------- Effects

        private static TimedRelease BuildBurst(string name, MaterialSet m, Color color, int count, float speed, float size,
            float lifetime, float radius = 0.15f, bool fromGround = false)
        {
            var root = CreateRoot(name, GameLayers.Default);
            var particles = root.AddComponent<ParticleSystem>();

            var main = particles.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = count * 2;
            main.gravityModifier = fromGround ? 0.6f : 0f;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = particles.shape;
            shape.shapeType = fromGround ? ParticleSystemShapeType.Hemisphere : ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            if (fromGround)
                shape.rotation = new Vector3(-90f, 0f, 0f);

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = m.Particles;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            var release = root.AddComponent<TimedRelease>();
            SerializedSetter.For(release).Set("lifetime", lifetime + 0.3f).Apply();

            return SavePrefab<TimedRelease>(root, $"{PrefabsFolder}/Vfx/{name}.prefab");
        }

        private static ExpandingRing BuildRing(string name, Material material)
        {
            var root = CreateRoot(name, GameLayers.Default);
            var disc = AddVisual(root.transform, PrimitiveType.Cylinder, material, new Vector3(0f, 0.08f, 0f), new Vector3(1f, 0.02f, 1f));
            DisableShadows(disc);
            root.AddComponent<ExpandingRing>();
            return SavePrefab<ExpandingRing>(root, $"{PrefabsFolder}/Vfx/{name}.prefab");
        }

        // ---------------------------------------------------------------- Projectiles & strikes

        private static Projectile BuildProjectile(string name, Material material, float diameter, int layer, float lifetime, TimedRelease impact,
            float length = 0f)
        {
            var root = CreateRoot(name, layer);

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = diameter * 0.5f;

            var body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.None;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.linearDamping = 0f;

            // Elongated projectiles use a capsule lying along the flight direction (+Z).
            var visual = length > 0f
                ? AddVisual(root.transform, PrimitiveType.Capsule, material, Vector3.zero, new Vector3(diameter, length * 0.5f, diameter), new Vector3(90f, 0f, 0f))
                : AddVisual(root.transform, PrimitiveType.Sphere, material, Vector3.zero, Vector3.one * diameter);
            DisableShadows(visual);

            var projectile = root.AddComponent<Projectile>();
            SerializedSetter.For(projectile).Set("lifetime", lifetime).Set("impactEffect", impact).Apply();

            return SavePrefab<Projectile>(root, $"{PrefabsFolder}/Projectiles/{name}.prefab");
        }

        private static DelayedAreaStrike BuildStrike(string name, Material outlineMaterial, Material fillMaterial, TimedRelease impact, Material fallingMaterial)
        {
            var root = CreateRoot(name, GameLayers.Default);
            var outline = AddVisual(root.transform, PrimitiveType.Cylinder, outlineMaterial, new Vector3(0f, 0.04f, 0f), new Vector3(1f, 0.01f, 1f), name: "Outline");
            var fill = AddVisual(root.transform, PrimitiveType.Cylinder, fillMaterial, new Vector3(0f, 0.06f, 0f), new Vector3(0f, 0.01f, 0f), name: "Fill");
            DisableShadows(outline);
            DisableShadows(fill);

            GameObject falling = null;
            if (fallingMaterial != null)
            {
                falling = AddVisual(root.transform, PrimitiveType.Sphere, fallingMaterial, new Vector3(0f, 12f, 0f), Vector3.one * 1.3f, name: "Meteor");
                falling.SetActive(false);
            }

            var strike = root.AddComponent<DelayedAreaStrike>();
            SerializedSetter.For(strike)
                .Set("outline", outline.transform)
                .Set("fill", fill.transform)
                .Set("fallingObject", falling != null ? falling.transform : null)
                .Set("impactEffect", impact)
                .Apply();

            return SavePrefab<DelayedAreaStrike>(root, $"{PrefabsFolder}/Projectiles/{name}.prefab");
        }

        private static ExperienceGem BuildGem(MaterialSet m)
        {
            var root = CreateRoot("ExperienceGem", GameLayers.Pickup);

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.4f;

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var visual = AddVisual(root.transform, PrimitiveType.Cube, m.Gem, Vector3.zero, Vector3.one * 0.3f, new Vector3(45f, 0f, 45f));
            DisableShadows(visual);

            root.AddComponent<ExperienceGem>();
            return SavePrefab<ExperienceGem>(root, $"{PrefabsFolder}/Pickups/ExperienceGem.prefab");
        }

        // ---------------------------------------------------------------- Enemies

        private enum EnemyShape { Grunt, Runner, Brute, Boss }

        private static Enemy BuildEnemy(string name, Material bodyMaterial, float scale, ExperienceGem gem, TimedRelease deathEffect,
            MaterialSet m, EnemyShape shape)
        {
            var root = CreateEnemyRoot(name, bodyMaterial, scale, gem, deathEffect, m, shape);
            return SavePrefab<Enemy>(root, $"{PrefabsFolder}/Enemies/{name}.prefab");
        }

        private static GameObject BuildBoss(MaterialSet m, ExperienceGem gem, TimedRelease deathEffect, DelayedAreaStrike slam, Projectile orb)
        {
            var root = CreateEnemyRoot("Boss", m.Boss, 2.6f, gem, deathEffect, m, EnemyShape.Boss);
            var origin = AddChild(root.transform, "ProjectileOrigin", new Vector3(0f, 0.42f, 0.2f));

            var boss = root.AddComponent<BossController>();
            SerializedSetter.For(boss)
                .Set("slamPrefab", slam)
                .Set("projectilePrefab", orb)
                .Set("projectileOrigin", origin)
                .Apply();

            return SavePrefab<Enemy>(root, $"{PrefabsFolder}/Enemies/Boss.prefab").gameObject;
        }

        private static GameObject CreateEnemyRoot(string name, Material bodyMaterial, float scale, ExperienceGem gem,
            TimedRelease deathEffect, MaterialSet m, EnemyShape shape)
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

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.height = 1.8f;
            capsule.radius = 0.45f;

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            root.AddComponent<Health>();

            var visual = AddChild(root.transform, "Visual", Vector3.zero);
            var bodyRenderer = AddVisual(visual, PrimitiveType.Capsule, bodyMaterial, new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 0.9f, 0.9f), name: "Body")
                .GetComponent<Renderer>();
            // Tiny details don't need shadows; the sphere primitive is comparatively dense.
            DisableShadows(AddVisual(visual, PrimitiveType.Sphere, m.EnemyEyes, new Vector3(-0.17f, 1.35f, 0.36f), Vector3.one * 0.16f, name: "EyeL"));
            DisableShadows(AddVisual(visual, PrimitiveType.Sphere, m.EnemyEyes, new Vector3(0.17f, 1.35f, 0.36f), Vector3.one * 0.16f, name: "EyeR"));

            switch (shape)
            {
                case EnemyShape.Runner:
                    AddVisual(visual, PrimitiveType.Cube, bodyMaterial, new Vector3(-0.3f, 1.85f, 0f), new Vector3(0.1f, 0.35f, 0.1f), new Vector3(0f, 0f, 20f), "EarL");
                    AddVisual(visual, PrimitiveType.Cube, bodyMaterial, new Vector3(0.3f, 1.85f, 0f), new Vector3(0.1f, 0.35f, 0.1f), new Vector3(0f, 0f, -20f), "EarR");
                    break;
                case EnemyShape.Brute:
                    AddVisual(visual, PrimitiveType.Cube, m.Stone, new Vector3(-0.35f, 1.75f, 0.1f), new Vector3(0.12f, 0.3f, 0.12f), new Vector3(0f, 0f, 30f), "HornL");
                    AddVisual(visual, PrimitiveType.Cube, m.Stone, new Vector3(0.35f, 1.75f, 0.1f), new Vector3(0.12f, 0.3f, 0.12f), new Vector3(0f, 0f, -30f), "HornR");
                    AddVisual(visual, PrimitiveType.Sphere, bodyMaterial, new Vector3(-0.55f, 1.1f, 0f), Vector3.one * 0.4f, name: "FistL");
                    AddVisual(visual, PrimitiveType.Sphere, bodyMaterial, new Vector3(0.55f, 1.1f, 0f), Vector3.one * 0.4f, name: "FistR");
                    break;
                case EnemyShape.Boss:
                    for (var i = 0; i < 5; i++)
                    {
                        var angle = i * 72f * Mathf.Deg2Rad;
                        var offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 0.3f;
                        AddVisual(visual, PrimitiveType.Cube, m.Gold, new Vector3(offset.x, 1.9f, offset.z), new Vector3(0.1f, 0.3f, 0.1f), name: $"CrownSpike{i}");
                    }
                    AddVisual(visual, PrimitiveType.Cylinder, m.Gold, new Vector3(0f, 1.78f, 0f), new Vector3(0.7f, 0.05f, 0.7f), name: "CrownBand");
                    AddVisual(visual, PrimitiveType.Sphere, m.BossOrb, new Vector3(0.6f, 1.1f, 0.2f), Vector3.one * 0.3f, name: "HandOrb");
                    break;
            }

            var enemy = root.AddComponent<Enemy>();
            SerializedSetter.For(enemy).Set("experienceGemPrefab", gem).Set("deathEffect", deathEffect).Apply();

            var flash = root.AddComponent<DamageFlash>();
            SerializedSetter.For(flash).Set("renderers", new[] { bodyRenderer }).Set("flashMaterial", m.Flash).Apply();

            // The boss uses the HUD bar instead of a world-space one.
            if (shape != EnemyShape.Boss)
                AddHealthBar(root, m, height: 2.3f);

            return root;
        }

        private static void AddHealthBar(GameObject root, MaterialSet m, float height)
        {
            // The shader billboards the quad and sizes it in world units, so the object's scale doesn't matter.
            var bar = AddVisual(root.transform, PrimitiveType.Quad, m.HealthBar, new Vector3(0f, height, 0f), Vector3.one, name: "HealthBar");
            DisableShadows(bar);
            var barRenderer = bar.GetComponent<Renderer>();
            barRenderer.enabled = false;

            var healthBar = root.AddComponent<WorldHealthBar>();
            SerializedSetter.For(healthBar).Set("health", root.GetComponent<Health>()).Set("bar", barRenderer).Apply();
        }

        private static TextMeshPro BuildDamageLabel(MaterialSet m)
        {
            var root = CreateRoot("DamageNumber", GameLayers.Default);
            var text = root.AddComponent<TextMeshPro>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);
            text.fontSharedMaterial = m.DamageText;
            text.fontSize = 6f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(4f, 1.5f);
            text.text = "0";

            var textRenderer = root.GetComponent<MeshRenderer>();
            textRenderer.shadowCastingMode = ShadowCastingMode.Off;
            textRenderer.receiveShadows = false;

            return SavePrefab<TextMeshPro>(root, $"{PrefabsFolder}/Vfx/DamageNumber.prefab");
        }

        private static EnemyDefinition EnemyData(string file, string displayName, Enemy prefab, float health, float speed,
            float damage, float range, float cooldown, int xp)
        {
            var definition = LoadOrCreateAsset<EnemyDefinition>($"{DataFolder}/Enemies/{file}.asset");
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

        private static SpawnSchedule BuildSpawnSchedule(EnemyDefinition grunt, EnemyDefinition runner, EnemyDefinition brute)
        {
            var schedule = LoadOrCreateAsset<SpawnSchedule>($"{DataFolder}/SpawnSchedule.asset");
            schedule.SetPhases(new[]
            {
                Phase(0f, interval: 1.6f, perTick: 1, maxAlive: 12, (grunt, 1f)),
                Phase(45f, interval: 1.3f, perTick: 2, maxAlive: 25, (grunt, 0.8f), (runner, 0.2f)),
                Phase(120f, interval: 1.1f, perTick: 2, maxAlive: 40, (grunt, 0.6f), (runner, 0.3f), (brute, 0.1f)),
                Phase(240f, interval: 1.0f, perTick: 3, maxAlive: 60, (grunt, 0.5f), (runner, 0.3f), (brute, 0.2f)),
                Phase(360f, interval: 0.9f, perTick: 3, maxAlive: 80, (grunt, 0.4f), (runner, 0.35f), (brute, 0.25f)),
                Phase(480f, interval: 0.8f, perTick: 4, maxAlive: 100, (grunt, 0.35f), (runner, 0.35f), (brute, 0.3f)),
            });
            EditorUtility.SetDirty(schedule);
            return schedule;
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

        // ---------------------------------------------------------------- Player

        private static GameObject BuildPlayer(MaterialSet m, InputReader input, Projectile bolt)
        {
            var root = CreateRoot("Player", GameLayers.Player);
            root.tag = "Player";

            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.05f;

            var health = root.AddComponent<Health>();
            SerializedSetter.For(health).Set("maxHealth", 150f).Set("invulnerabilityDuration", 0.5f).Apply();

            var stats = root.AddComponent<PlayerStats>();
            SerializedSetter.For(stats).Set("health", health).Apply();

            var movement = root.AddComponent<PlayerController>();
            SerializedSetter.For(movement).Set("input", input).Set("stats", stats).Set("moveSpeed", 6.5f).Apply();

            var progression = root.AddComponent<PlayerProgression>();
            SerializedSetter.For(progression).Set("stats", stats).Apply();

            // Placeholder wizard
            var visual = AddChild(root.transform, "Visual", Vector3.zero);
            var robe = AddVisual(visual, PrimitiveType.Capsule, m.PlayerRobe, new Vector3(0f, 0.85f, 0f), new Vector3(0.9f, 0.85f, 0.9f), name: "Robe");
            var head = AddVisual(visual, PrimitiveType.Sphere, m.PlayerSkin, new Vector3(0f, 1.85f, 0f), Vector3.one * 0.5f, name: "Head");
            AddVisual(visual, PrimitiveType.Cylinder, m.PlayerHat, new Vector3(0f, 2.08f, 0f), new Vector3(0.9f, 0.03f, 0.9f), name: "HatBrim");
            AddVisual(visual, PrimitiveType.Cylinder, m.PlayerHat, new Vector3(0f, 2.25f, 0f), new Vector3(0.46f, 0.15f, 0.46f), name: "HatLow");
            AddVisual(visual, PrimitiveType.Cylinder, m.PlayerHat, new Vector3(0f, 2.49f, -0.04f), new Vector3(0.3f, 0.12f, 0.3f), new Vector3(-8f, 0f, 0f), "HatMid");
            AddVisual(visual, PrimitiveType.Cylinder, m.PlayerHat, new Vector3(0f, 2.68f, -0.1f), new Vector3(0.15f, 0.09f, 0.15f), new Vector3(-18f, 0f, 0f), "HatTip");
            AddVisual(visual, PrimitiveType.Cylinder, m.Wood, new Vector3(0.5f, 1.05f, 0.25f), new Vector3(0.07f, 0.85f, 0.07f), name: "Staff");
            AddVisual(visual, PrimitiveType.Sphere, m.StaffGlow, new Vector3(0.5f, 1.95f, 0.25f), Vector3.one * 0.24f, name: "StaffOrb");

            var muzzle = AddChild(root.transform, "Muzzle", new Vector3(0f, 1.1f, 0.6f));

            var autoAttack = root.AddComponent<AutoAttack>();
            SerializedSetter.For(autoAttack)
                .Set("projectilePrefab", bolt)
                .Set("muzzle", muzzle)
                .Set("stats", stats)
                .Set("baseDamage", 12f)
                .Set("fireInterval", 0.5f)
                .Apply();

            var caster = root.AddComponent<SpellCaster>();
            SerializedSetter.For(caster)
                .Set("input", input)
                .Set("stats", stats)
                .Set("controller", movement)
                .Set("health", health)
                .Set("castOrigin", muzzle)
                .Apply();

            var flash = root.AddComponent<DamageFlash>();
            SerializedSetter.For(flash)
                .Set("renderers", new[] { robe.GetComponent<Renderer>(), head.GetComponent<Renderer>() })
                .Set("flashMaterial", m.Flash)
                .Apply();

            // Sensors: kinematic trigger volumes on their own layer, so they only see pickups/interactables.
            var magnet = CreateSensor(root.transform, "PickupMagnet", 3.5f);
            var pickupMagnet = magnet.AddComponent<PickupMagnet>();
            SerializedSetter.For(pickupMagnet).Set("progression", progression).Set("stats", stats).Set("collectPoint", magnet.transform).Apply();

            var interactorSensor = CreateSensor(root.transform, "Interactor", 2.5f);
            var interactor = interactorSensor.AddComponent<PlayerInteractor>();
            SerializedSetter.For(interactor).Set("input", input).Set("health", health).Set("progression", progression).Apply();

            var path = $"{PrefabsFolder}/Player/Player.prefab";
            EnsureFolder($"{PrefabsFolder}/Player");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateSensor(Transform parent, string name, float radius)
        {
            var sensor = AddChild(parent, name, new Vector3(0f, 1f, 0f), GameLayers.PlayerSensor).gameObject;
            var trigger = sensor.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            var body = sensor.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            return sensor;
        }

        // ---------------------------------------------------------------- Interactables

        private static HealingShrine BuildShrine(MaterialSet m)
        {
            var root = CreateRoot("HealingShrine", GameLayers.Interactable);
            AddInteractionTrigger(root, 2.6f);

            AddSolid(root.transform, PrimitiveType.Cylinder, m.Stone, new Vector3(0f, 0.2f, 0f), new Vector3(2f, 0.2f, 2f), "Base");
            AddSolid(root.transform, PrimitiveType.Cylinder, m.Stone, new Vector3(0f, 0.8f, 0f), new Vector3(0.6f, 0.5f, 0.6f), "Pedestal");
            var crystal = AddVisual(root.transform, PrimitiveType.Cube, m.CrystalReady, new Vector3(0f, 1.9f, 0f), new Vector3(0.55f, 0.8f, 0.55f), new Vector3(0f, 45f, 0f), "Crystal");

            var shrine = root.AddComponent<HealingShrine>();
            SerializedSetter.For(shrine)
                .Set("crystal", crystal.GetComponent<Renderer>())
                .Set("readyMaterial", m.CrystalReady)
                .Set("depletedMaterial", m.CrystalDepleted)
                .Apply();

            return SavePrefab<HealingShrine>(root, $"{PrefabsFolder}/Interactables/HealingShrine.prefab");
        }

        private static TreasureChest BuildChest(MaterialSet m, TimedRelease openEffect)
        {
            var root = CreateRoot("TreasureChest", GameLayers.Interactable);
            AddInteractionTrigger(root, 2.2f);

            AddSolid(root.transform, PrimitiveType.Cube, m.Wood, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f), "Body");
            AddVisual(root.transform, PrimitiveType.Cube, m.Gold, new Vector3(0f, 0.35f, 0f), new Vector3(1.22f, 0.12f, 0.82f), name: "Band");

            var lidPivot = AddChild(root.transform, "LidPivot", new Vector3(0f, 0.7f, -0.4f));
            AddVisual(lidPivot, PrimitiveType.Cube, m.Wood, new Vector3(0f, 0.12f, 0.42f), new Vector3(1.24f, 0.24f, 0.84f), name: "Lid");
            AddVisual(lidPivot, PrimitiveType.Cube, m.Gold, new Vector3(0f, 0.02f, 0.85f), new Vector3(0.18f, 0.22f, 0.06f), name: "Lock");

            // Chests spawn at runtime, so they can't be baked into the NavMesh; enemies steer around them instead.
            var obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = new Vector3(0f, 0.4f, 0f);
            obstacle.size = new Vector3(1.3f, 0.8f, 0.9f);
            obstacle.carving = false;

            var chest = root.AddComponent<TreasureChest>();
            SerializedSetter.For(chest).Set("lid", lidPivot).Set("openEffect", openEffect).Apply();

            return SavePrefab<TreasureChest>(root, $"{PrefabsFolder}/Interactables/TreasureChest.prefab");
        }

        private static void AddInteractionTrigger(GameObject root, float radius)
        {
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            trigger.center = new Vector3(0f, 1f, 0f);
        }

        /// <summary>Primitive that keeps its collider on the Default layer, so it blocks movement and the NavMesh.</summary>
        private static GameObject AddSolid(Transform parent, PrimitiveType type, Material material, Vector3 localPosition,
            Vector3 localScale, string name, Vector3 localEuler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = GameLayers.Default;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localEulerAngles = localEuler;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static void DisableShadows(GameObject go)
        {
            var meshRenderer = go.GetComponent<Renderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }
    }
}
