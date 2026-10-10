using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Interaction;
using BattleArena.Missions;
using BattleArena.Pickups;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace BattleArena.Editor.ContentBuilder
{
    /// <summary>
    /// Gameplay props and markers: treasure chest, XP gem, mission zones, arcane shard and pointer, enemy projectiles,
    /// the charger's dash lane and attack wind-up effects. Meshes are modelled in Blender (Art/Models/Props); markers
    /// are textured quads and particles.
    /// </summary>
    internal static partial class GameContentBuilder
    {
        private static readonly Color ArcanePurple = new(0.78f, 0.49f, 1f);
        private static readonly Color MissionGold = new(1f, 0.78f, 0.3f);
        private static readonly Color EnemyRed = new(1f, 0.28f, 0.25f);

        // ---------------------------------------------------------------- Pickups and interactables

        /// <summary>Builds the experience gem prefab.</summary>
        private static ExperienceGem BuildGem(MaterialSet m, TimedRelease collectEffect)
        {
            var root = CreateRoot("ExperienceGem", GameLayers.Pickup);

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.4f;

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            AddModel(root, "Props/Gem.fbx", 1.1f, m.Gem);
            DisableModelShadows(root);

            SerializedSetter.For(root.AddComponent<ExperienceGem>()).Set("collectEffect", collectEffect).Apply();
            return SavePrefab<ExperienceGem>(root, $"{PrefabsFolder}/Pickups/ExperienceGem.prefab");
        }

        /// <summary>Builds the treasure chest prefab.</summary>
        private static TreasureChest BuildChest(MaterialSet m, TimedRelease openEffect)
        {
            var root = CreateRoot("TreasureChest", GameLayers.Interactable);
            AddInteractionTrigger(root, 2.2f);

            // Slots: wood, iron, gold. The front (lock) faces the camera; the lid hinges on the back edge.
            AddModel(root, "Props/ChestBody.fbx", m.ChestWood, m.ChestIron, m.Gold);
            var lidPivot = AddChild(root.transform, "LidPivot", new Vector3(0f, 0.52f, 0.36f));
            AddModel(lidPivot.gameObject, "Props/ChestLid.fbx", m.ChestWood, m.ChestIron, m.Gold);

            // The blocker stays on the Default layer so the chest stops the player.
            var blocker = new GameObject("Blocker") { layer = GameLayers.Default };
            blocker.transform.SetParent(root.transform, false);
            var collider = blocker.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.36f, 0f);
            collider.size = new Vector3(1.12f, 0.72f, 0.76f);

            // Chests spawn at runtime, so they can't be baked into the NavMesh; enemies steer around them instead.
            var obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = new Vector3(0f, 0.4f, 0f);
            obstacle.size = new Vector3(1.2f, 0.8f, 0.85f);
            obstacle.carving = false;

            var chest = root.AddComponent<TreasureChest>();
            SerializedSetter.For(chest).Set("lid", lidPivot).Set("openAngle", 110f).Set("openEffect", openEffect).Apply();
            return SavePrefab<TreasureChest>(root, $"{PrefabsFolder}/Interactables/TreasureChest.prefab");
        }

        // ---------------------------------------------------------------- Missions

        /// <summary>Ground circle: glowing ring, a slowly turning rune inside it and a disc that fills with progress.</summary>
        private static MissionZone BuildMissionZone(string name, Material ringMaterial, Material fillMaterial, Material runeMaterial)
        {
            var root = CreateRoot(name, GameLayers.Default);
            var outline = FlatQuad(root.transform, "Outline", ringMaterial, 0.04f);
            var fill = FlatQuad(root.transform, "Fill", fillMaterial, 0.05f);
            fill.localScale = new Vector3(0f, 1f, 0f);

            // Lives as long as the zone; the holder's scale sizes it with the outline.
            new Fx(outline, "Rune", runeMaterial, Vector3.up * 0.01f)
                .Horizontal().Burst(1).Life(9999f, 9999f).Size(0.94f, 0.94f).Spin(18f, 18f)
                .ColorOverLife(Fade(0f, 0f, 0.0002f, 1f, 1f, 1f));
            root.AddComponent<VfxRestart>();

            var zone = root.AddComponent<MissionZone>();
            SerializedSetter.For(zone).Set("outline", outline).Set("fill", fill).Apply();
            return SavePrefab<MissionZone>(root, $"{PrefabsFolder}/Missions/{name}.prefab");
        }

        /// <summary>Builds the arcane shard prefab.</summary>
        private static MissionShard BuildMissionShard(MaterialSet m, TimedRelease collectEffect)
        {
            var root = CreateRoot("ArcaneShard", GameLayers.Pickup);

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            // Crystal cluster on a rock, resting on the ground (the root floats just under a metre up).
            AddModel(root, "Props/CrystalCluster.fbx", 1.25f, m.MissionCrystal, m.RockStone);
            root.transform.Find("Model").localPosition = new Vector3(0f, -0.26f, 0f);

            var t = root.transform;
            new Fx(t, "GroundGlow", FxMaterial("ShardGlow", "vfx_disc", ArcanePurple, 0.9f), new Vector3(0f, -0.9f, 0f))
                .Horizontal().Loop(1f).Rate(1.2f).Life(2.5f, 2.5f).Size(3f, 3f).Max(4).ColorOverLife(Fade(0f, 0f, 0.3f, 0.5f, 0.7f, 0.5f, 1f, 0f));
            new Fx(t, "Motes", FxMaterial("ShardMote", "vfx_glow", ArcanePurple, 2.5f), new Vector3(0f, -0.8f, 0f))
                .Loop().Rate(10f).Circle(0.7f).Life(1.2f, 2f).Size(0.08f, 0.16f).World()
                .Velocity(new Vector3(0f, 0.6f, 0f), new Vector3(0f, 1.2f, 0f)).ColorOverLife(Fade(0f, 0f, 0.2f, 1f, 1f, 0f));
            new Fx(t, "Twinkles", FxMaterial("ShardStar", "vfx_star", Color.Lerp(ArcanePurple, Color.white, 0.4f), 2.5f), new Vector3(0f, 0.1f, 0f))
                .Loop().Rate(3f).Sphere(0.55f).Life(0.5f, 0.8f).Size(0.15f, 0.3f).Rotation().ColorOverLife(Fade(0f, 0f, 0.3f, 1f, 1f, 0f));
            root.AddComponent<VfxRestart>();

            var shard = root.AddComponent<MissionShard>();
            SerializedSetter.For(shard).Set("collectEffect", collectEffect).Apply();
            return SavePrefab<MissionShard>(root, $"{PrefabsFolder}/Missions/ArcaneShard.prefab");
        }

        /// <summary>Scene object: a gold chevron that circles the player and points at the mission objective.</summary>
        private static MissionPointer BuildMissionPointer(MaterialSet m, Transform player)
        {
            var root = new GameObject("MissionPointer");
            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(root.transform, false);
            AddModel(arrow, "Props/MissionChevron.fbx", 1.3f, m.PointerGlow);
            DisableModelShadows(arrow);

            var pointer = root.AddComponent<MissionPointer>();
            SerializedSetter.For(pointer).Set("player", player).Set("arrow", arrow).Apply();
            return pointer;
        }

        // ---------------------------------------------------------------- Enemy attacks

        /// <summary>Builds the goblin archer's arrow.</summary>
        private static Projectile BuildEnemyArrow(MaterialSet m, TimedRelease hit)
        {
            var root = ProjectileRoot("EnemyArrow", 0.15f, GameLayers.EnemyProjectile);
            AddModel(root, "Props/EnemyArrow.fbx", 1.25f, m.AutumnWood, m.LanternIron, m.Fletching);
            DisableModelShadows(root);
            AddTrail(root.transform, FxMaterial("ArrowTrail", "vfx_trail", new Color(1f, 0.82f, 0.55f), 1.3f), 0.14f, 0.12f);
            return SaveProjectile(root, 2.5f, hit);
        }

        /// <summary>
        /// Gold glitter around the Treasure Bunny: stars and small glints drifting up off its body. Emitted in world
        /// space, so it leaves a sparkling trail as it runs. Enough to spot it across the arena, cheap on WebGL.
        /// </summary>
        private static void AddTreasureSparkles(Transform enemy)
        {
            new Fx(enemy, "TreasureSparkles", FxMaterial("TreasureStar", "vfx_star", MissionGold, 2.6f), Vector3.up * 1.1f)
                .World().Loop().Rate(16f).Sphere(0.75f).Life(0.55f, 0.95f).Size(0.26f, 0.46f).Rotation().Spin(-90f, 90f)
                .Velocity(Vector3.up * 0.4f, Vector3.up * 1.1f).Max(32)
                .SizeOverLife(Curve(0f, 0.3f, 0.3f, 1f, 1f, 0.2f)).ColorOverLife(Fade(0f, 0f, 0.2f, 1f, 0.7f, 1f, 1f, 0f));
            new Fx(enemy, "TreasureGlints", FxMaterial("TreasureGlint", "vfx_glow", new Color(1f, 0.92f, 0.6f), 3f), Vector3.up * 1.1f)
                .World().Loop().Rate(18f).Sphere(0.85f).Life(0.25f, 0.45f).Size(0.07f, 0.13f).Max(20)
                .ColorOverLife(Fade(0f, 0f, 0.3f, 1f, 1f, 0f));
        }

        /// <summary>
        /// Builds the arcane wisp's bolt, magenta-purple like the boss's orbs (enemy magic) to stand apart from the
        /// player's cyan bolts.
        /// </summary>
        private static Projectile BuildWispBolt(TimedRelease hit)
        {
            var color = new Color(0.82f, 0.3f, 1f);
            var root = ProjectileRoot("WispBolt", 0.2f, GameLayers.EnemyProjectile);
            var t = root.transform;
            GlowCore(t, "WispGlow", color, 0.95f, 1.3f);
            GlowCore(t, "WispCore", Color.white, 0.25f, 2f);
            AddTrail(t, FxMaterial("WispTrail", "vfx_trail", color, 2f), 0.2f, 0.32f);
            new Fx(t, "Motes", FxMaterial("WispMote", "vfx_glow", color, 2.5f))
                .World().Loop().RatePerMeter(3f).Sphere(0.15f).Life(0.25f, 0.45f).Size(0.06f, 0.12f)
                .Velocity(-Vector3.one * 0.4f, Vector3.one * 0.4f).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            return SaveProjectile(root, 3f, hit);
        }

        /// <summary>Builds the boss's orb.</summary>
        private static Projectile BuildBossOrb(TimedRelease hit)
        {
            var color = new Color(0.8f, 0.35f, 1f);
            var root = ProjectileRoot("BossOrb", 0.28f, GameLayers.EnemyProjectile);
            var t = root.transform;
            new Fx(t, "Shadow", FxMaterial("BossOrbShadow", "vfx_smoke_sheet", new Color(0.12f, 0.02f, 0.18f, 0.8f), additive: false))
                .Loop(0.5f).Rate(24f).Life(0.25f, 0.35f).Size(0.9f, 1.1f).Rotation().Flipbook().Max(10).ColorOverLife(Fade(0f, 0f, 0.3f, 1f, 1f, 0f));
            GlowCore(t, "OrbGlow", color, 1.3f, 2.2f);
            GlowCore(t, "OrbCore", Color.Lerp(color, Color.white, 0.6f), 0.45f, 3f);
            AddTrail(t, FxMaterial("BossOrbTrail", "vfx_trail", color, 1.8f), 0.25f, 0.5f);
            return SaveProjectile(root, 4f, hit);
        }

        /// <summary>Lit-fuse bomb for lobbed strikes; the holder is what <see cref="DelayedAreaStrike"/> moves.</summary>
        private static Transform BombVisual(Transform strikeRoot, MaterialSet m)
        {
            const float scale = 1.1f;
            var holder = new GameObject("Projectile") { layer = strikeRoot.gameObject.layer };
            holder.transform.SetParent(strikeRoot, false);
            holder.transform.localPosition = new Vector3(0f, 12f, 0f);
            AddModel(holder, "Props/Bomb.fbx", scale, m.BombBody, m.Gold, m.AutumnWood);

            // Fuse tip: Blender (0.12, 0.03, 0.44) arrives turned 180 degrees about Y.
            var tip = new Vector3(-0.12f, 0.44f, -0.03f) * scale;
            new Fx(holder.transform, "FuseSparks", FxMaterial("FuseSpark", "vfx_glow", new Color(1f, 0.75f, 0.3f), 3f), tip)
                .Loop().Rate(40f).Sphere(0.02f).Speed(1f, 2.5f).Life(0.15f, 0.3f).Size(0.04f, 0.08f).Gravity(0.5f).Stretched(2f, 0.05f)
                .ColorOverLife(Fade(0f, 1f, 1f, 0f));
            new Fx(holder.transform, "FuseGlow", FxMaterial("FuseGlow", "vfx_glow", new Color(1f, 0.6f, 0.2f), 2.5f), tip)
                .Loop(0.3f).Rate(20f).Life(0.1f, 0.15f).Size(0.25f, 0.35f).Max(6).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            holder.AddComponent<VfxRestart>();
            return holder.transform;
        }

        /// <summary>Charger's dash warning: a chevron lane that fills along its length.</summary>
        private static LineTelegraph BuildLineTelegraph(MaterialSet m)
        {
            var root = CreateRoot("DashTelegraph", GameLayers.Default);
            var outline = FlatQuad(root.transform, "Outline", m.TelegraphLane, 0.04f);
            var fill = FlatQuad(root.transform, "Fill", m.TelegraphLaneFill, 0.05f);

            var telegraph = root.AddComponent<LineTelegraph>();
            SerializedSetter.For(telegraph).Set("outline", outline).Set("fill", fill).Apply();
            return SavePrefab<LineTelegraph>(root, $"{PrefabsFolder}/Vfx/DashTelegraph.prefab");
        }

        /// <summary>Glow colour of most enemy wind-ups.</summary>
        private static readonly Color WindupGlowColor = Hex("#FF4A10");

        /// <summary>
        /// Charging effect shown while an attack winds up: a pulsing glow where the attack comes from, sparks
        /// drawn into it, and a ring closing in around the enemy's feet.
        /// </summary>
        /// <param name="root">Enemy the indicator is added to.</param>
        /// <param name="key">Names the generated effect materials, so enemies with the same key share them.</param>
        /// <param name="color">Glow colour.</param>
        /// <param name="localPosition">Where the attack comes from, relative to the enemy.</param>
        /// <param name="size">Scale of the glow and sparks.</param>
        private static GameObject WindupIndicator(GameObject root, string key, Color color, Vector3 localPosition, float size)
        {
            var indicator = new GameObject("WindupIndicator") { layer = root.layer };
            indicator.transform.SetParent(root.transform, false);
            indicator.transform.localPosition = localPosition;
            var t = indicator.transform;

            new Fx(t, "Glow", FxMaterial($"Windup{key}Glow", "vfx_glow", color, 2.5f))
                .Loop(0.3f).Rate(16f).Life(0.2f, 0.26f).Size(size * 1.8f, size * 2.2f).Max(8)
                .SizeOverLife(Curve(0f, 0.5f, 1f, 1.1f)).ColorOverLife(Fade(0f, 0f, 0.35f, 1f, 1f, 0f));
            new Fx(t, "Sparks", FxMaterial($"Windup{key}Spark", "vfx_glow", Color.Lerp(color, Color.white, 0.5f), 3f))
                .Loop().Rate(28f).Sphere(size * 1.4f, 0.1f).Speed(-3f * size, -1.5f * size).Life(0.2f, 0.3f).Size(0.05f, 0.09f)
                .Stretched(2.5f, 0.05f).ColorOverLife(Fade(0f, 0f, 0.3f, 1f, 1f, 0f));
            var groundOffset = new Vector3(-localPosition.x, 0.06f - localPosition.y, -localPosition.z);
            new Fx(t, "Ring", FxMaterial($"Windup{key}Ring", "vfx_ring", color, 1.8f), groundOffset)
                .Horizontal().Loop(0.5f).Rate(2.5f).Life(0.45f, 0.45f).Size(2.4f, 2.4f).Max(3)
                .SizeOverLife(Curve(0f, 1f, 1f, 0.3f)).ColorOverLife(Fade(0f, 0f, 0.25f, 1f, 1f, 0f));

            indicator.SetActive(false);
            return indicator;
        }

        /// <summary>Stops every renderer under <paramref name="root"/> from casting or receiving shadows.</summary>
        private static void DisableModelShadows(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }
}
