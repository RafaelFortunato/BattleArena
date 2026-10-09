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
            public HealingWell Well;
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
            var deathPuff = BuildBurst("EnemyDeath", m, new Color(1f, 0.9f, 0.6f), 16, 4f, 0.4f, 0.45f, radius: 0.4f);
            var slamImpact = BuildBurst("BossSlamImpact", m, new Color(0.85f, 0.3f, 1f), 36, 4f, 0.5f, 0.6f, radius: 0.6f, fromGround: true);
            var critSpark = BuildBurst("CritSpark", m, new Color(1f, 0.7f, 0.15f), 18, 7f, 0.35f, 0.35f, radius: 0.2f);
            var chestSparkle = BuildBurst("ChestSparkle", m, new Color(1f, 0.85f, 0.35f), 30, 5f, 0.4f, 0.6f, radius: 0.6f);
            var wellSplash = BuildBurst("WellSplash", m, new Color(0.45f, 0.95f, 1f), 30, 4.5f, 0.35f, 0.6f, radius: 0.7f);
            // Small and quick: a whole wave of gems can be picked up in a second.
            var gemSparkle = BuildBurst("GemSparkle", m, new Color(0.55f, 1f, 0.82f), 6, 2.2f, 0.16f, 0.3f, radius: 0.15f);

            // Projectiles and telegraphed strikes
            var spellVfx = BuildSpellVfx(m);
            var bossOrb = BuildBossOrb(BuildSparkHit("BossOrbHit", new Color(0.8f, 0.35f, 1f), 1.2f));
            var bossSlam = BuildStrike("BossSlam", m.StrikeEnemyRing, m.StrikeEnemyFill, slamImpact, shake: 0.55f);

            var gem = BuildGem(m, gemSparkle);

            // Enemies
            var roster = BuildEnemyRoster(m, gem, deathPuff, bossSlam, bossOrb);
            var missions = BuildMissions(m, roster, collectEffect: chestSparkle);

            // Spells and level-up cards
            var spells = BuildSpells(m, spellVfx);
            var levelUp = BuildLevelUpConfig(spells);

            // Input
            var input = LoadOrCreateAsset<InputReader>($"{DataFolder}/InputReader.asset");
            SerializedSetter.For(input)
                .Set("actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>($"{SettingsFolder}/GameControls.inputactions"))
                .Apply();

            var player = BuildPlayer(m, input, spellVfx.PlayerBolt, spellVfx.CastFlare);

            return new ContentSet
            {
                Input = input,
                Player = player,
                Schedule = roster.Schedule,
                Boss = roster.Boss,
                Missions = missions,
                Well = BuildHealingWell(m, wellSplash),
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

        // ---------------------------------------------------------------- Projectiles & strikes

        /// <param name="shake">Screen shake when it lands (0-1), weaker the further from the player.</param>
        /// <param name="buildFalling">Optional builder for the object that drops onto the target (e.g. a lit bomb).</param>
        private static DelayedAreaStrike BuildStrike(string name, Material outlineMaterial, Material fillMaterial, TimedRelease impact,
            float shake, System.Func<Transform, Transform> buildFalling = null)
        {
            var root = CreateRoot(name, GameLayers.Default);
            var outline = FlatQuad(root.transform, "Outline", outlineMaterial, 0.04f);
            var fill = FlatQuad(root.transform, "Fill", fillMaterial, 0.05f);
            fill.localScale = new Vector3(0f, 1f, 0f);

            var falling = buildFalling?.Invoke(root.transform);
            if (falling != null)
                falling.gameObject.SetActive(false);

            var strike = root.AddComponent<DelayedAreaStrike>();
            SerializedSetter.For(strike)
                .Set("outline", outline)
                .Set("fill", fill)
                .Set("fallingObject", falling)
                .Set("impactEffect", impact)
                .Set("impactShake", shake)
                .Apply();

            return SavePrefab<DelayedAreaStrike>(root, $"{PrefabsFolder}/Projectiles/{name}.prefab");
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
            text.font = LoadOrCreateDamageFont();
            text.fontSharedMaterial = m.DamageText;
            text.fontSize = 6f;
            // Baloo ExtraBold is heavy already; TMP's fake bold would only blur it.
            text.fontStyle = FontStyles.Normal;
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

        private static GameObject BuildPlayer(MaterialSet m, InputReader input, Projectile bolt, FollowTarget castFlare)
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
            SerializedSetter.For(stats).Set("health", health).Set("baseAttack", 10f).Apply();

            var movement = root.AddComponent<PlayerController>();
            SerializedSetter.For(movement).Set("input", input).Set("stats", stats).Set("moveSpeed", 5.2f).Apply();

            var progression = root.AddComponent<PlayerProgression>();
            SerializedSetter.For(progression).Set("stats", stats).Apply();

            // The wizard husky; shots and spells come out of the crystal on its staff.
            var character = AddWizardHusky(root, m, movement, castFlare);
            var muzzle = character.Muzzle;

            var autoAttack = root.AddComponent<AutoAttack>();
            SerializedSetter.For(autoAttack)
                .Set("projectilePrefab", bolt)
                .Set("muzzle", muzzle)
                .Set("stats", stats)
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
                .Set("renderers", new[] { character.Body })
                .Set("flashMaterial", m.Flash)
                .Apply();

            // Shield bubble while Phase Step's immunity lasts.
            var immunityCue = root.AddComponent<ImmunityCue>();
            SerializedSetter.For(immunityCue).Set("bubble", BuildImmunityBubble(root.transform)).Apply();

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

        private static HealingWell BuildHealingWell(MaterialSet m, TimedRelease splash)
        {
            var root = CreateRoot("HealingWell", GameLayers.Interactable);
            AddInteractionTrigger(root, 2.8f);

            // Stone, posts and roof, plus the water as its own part so it can drain and refill.
            AddModel(root, "Props/Well.fbx", m.PropStoneDark, m.PropStone, m.AutumnWood, m.Slate, m.WellDepth);
            var water = AddMeshPart(root.transform.Find("Model"), "Water", LoadModelMesh($"{ModelsFolder}/Props/WellWater.fbx"), m.HealingWater);
            water.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            // The blocker stays on the Default layer, so the well stops movement and is carved out of the NavMesh.
            var blocker = new GameObject("Blocker") { layer = GameLayers.Default };
            blocker.transform.SetParent(root.transform, false);
            var collider = blocker.AddComponent<CapsuleCollider>();
            collider.radius = 1.25f;
            collider.height = 2.4f;
            collider.center = new Vector3(0f, 1.2f, 0f);

            var well = root.AddComponent<HealingWell>();
            SerializedSetter.For(well)
                .Set("water", water.transform)
                .Set("useEffect", splash)
                .Apply();

            return SavePrefab<HealingWell>(root, $"{PrefabsFolder}/Interactables/HealingWell.prefab");
        }

        private static void AddInteractionTrigger(GameObject root, float radius)
        {
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            trigger.center = new Vector3(0f, 1f, 0f);
        }
        private static void DisableShadows(GameObject go)
        {
            var meshRenderer = go.GetComponent<Renderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }
    }
}
