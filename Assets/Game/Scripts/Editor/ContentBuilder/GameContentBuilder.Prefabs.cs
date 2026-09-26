using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Enemies;
using BattleArena.Interaction;
using BattleArena.Pickups;
using BattleArena.Player;
using BattleArena.Pooling;
using BattleArena.Progression;
using BattleArena.Missions;
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
            public MissionDefinition[] Missions;
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
            var roster = BuildEnemyRoster(m, gem, deathPuff, bossSlam, bossOrb);
            var missions = BuildMissions(m, roster, collectEffect: chestSparkle);

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
                Schedule = roster.Schedule,
                Boss = roster.Boss,
                Missions = missions,
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

        private static DelayedAreaStrike BuildStrike(string name, Material outlineMaterial, Material fillMaterial, TimedRelease impact, Material fallingMaterial,
            float fallingSize = 1.3f)
        {
            var root = CreateRoot(name, GameLayers.Default);
            var outline = AddVisual(root.transform, PrimitiveType.Cylinder, outlineMaterial, new Vector3(0f, 0.04f, 0f), new Vector3(1f, 0.01f, 1f), name: "Outline");
            var fill = AddVisual(root.transform, PrimitiveType.Cylinder, fillMaterial, new Vector3(0f, 0.06f, 0f), new Vector3(0f, 0.01f, 0f), name: "Fill");
            DisableShadows(outline);
            DisableShadows(fill);

            GameObject falling = null;
            if (fallingMaterial != null)
            {
                falling = AddVisual(root.transform, PrimitiveType.Sphere, fallingMaterial, new Vector3(0f, 12f, 0f), Vector3.one * fallingSize, name: "Projectile");
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

        // ---------------------------------------------------------------- Enemy helpers shared with the roster

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
