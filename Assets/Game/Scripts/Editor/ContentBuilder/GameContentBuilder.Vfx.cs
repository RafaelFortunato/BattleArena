using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleArena.Editor.ContentBuilder
{
    /// <summary>
    /// Spell visuals: projectiles with glowing cores, trails and particle wakes; area effects authored for a radius of
    /// 1 and scaled by <see cref="AreaEffect"/>; a travelling chain lightning. Everything is Shuriken particles, line
    /// renderers and meshes so it runs on WebGL2.
    /// </summary>
    internal static partial class GameContentBuilder
    {
        private const string SpellVfxFolder = PrefabsFolder + "/Vfx/Spells";

        private static readonly Color BoltCyan = new(0.36f, 0.94f, 0.9f);
        private static readonly Color FireYellow = new(1f, 0.83f, 0.42f);
        private static readonly Color FireOrange = new(1f, 0.48f, 0.1f);
        private static readonly Color FireRed = new(0.72f, 0.14f, 0.04f);
        private static readonly Color ArcaneViolet = new(0.69f, 0.44f, 1f);
        private static readonly Color ArcanePink = new(0.88f, 0.69f, 1f);
        private static readonly Color FrostBlue = new(0.6f, 0.91f, 1f);
        private static readonly Color ToxicGreen = new(0.49f, 1f, 0.29f);
        private static readonly Color LightningBlue = new(0.56f, 0.85f, 1f);

        private sealed class SpellVfx
        {
            public Projectile PlayerBolt, Fireball, ArcaneLance;
            public DelayedAreaStrike MeteorStrike;
            public AreaEffect FrostNova, PhaseArrival;
            public TimedRelease PhaseDepart;
            public ChainLightningEffect ChainLightning;
            public OrbitingOrbs ArcaneOrbit;
            public DamageZone ToxicMiasma;
        }

        private static SpellVfx BuildSpellVfx(MaterialSet m)
        {
            VfxMaterials.Clear();
            var boltHit = BuildSparkHit("BoltHit", BoltCyan, 1f);
            var arcaneHit = BuildSparkHit("ArcaneHit", ArcaneViolet, 1.1f, stars: true);
            var lightningHit = BuildSparkHit("LightningHit", LightningBlue, 1.4f);

            return new SpellVfx
            {
                PlayerBolt = BuildBoltProjectile(boltHit),
                Fireball = BuildFireballProjectile(BuildExplosion("FireballExplosion", 3.5f, scorch: true)),
                ArcaneLance = BuildLanceProjectile(m, arcaneHit),
                MeteorStrike = BuildMeteorStrike(m, BuildExplosion("MeteorImpact", 1f, scorch: true, debris: true)),
                FrostNova = BuildFrostNova(),
                ChainLightning = BuildChainLightning(lightningHit, BuildShockMark()),
                ArcaneOrbit = BuildArcaneOrbit(m, arcaneHit),
                ToxicMiasma = BuildToxicMiasma(),
                PhaseDepart = BuildPhaseDepart(),
                PhaseArrival = BuildPhaseArrival(),
            };
        }

        // ---------------------------------------------------------------- Shared pieces

        private static GameObject FxRoot(string name) => CreateRoot(name, GameLayers.Default);

        private static T SaveFx<T>(GameObject root) where T : Component =>
            SavePrefab<T>(root, $"{SpellVfxFolder}/{root.name}.prefab");

        private static TimedRelease SaveTimed(GameObject root, float lifetime)
        {
            var release = root.AddComponent<TimedRelease>();
            SerializedSetter.For(release).Set("lifetime", lifetime).Apply();
            root.AddComponent<VfxRestart>();
            return SaveFx<TimedRelease>(root);
        }

        private static AreaEffect SaveArea(GameObject root, float lifetime)
        {
            var effect = root.AddComponent<AreaEffect>();
            SerializedSetter.For(effect).Set("lifetime", lifetime).Apply();
            root.AddComponent<VfxRestart>();
            return SaveFx<AreaEffect>(root);
        }

        /// <summary>Flash, stretched sparks and a small ring: the generic "something was hit" beat.</summary>
        private static TimedRelease BuildSparkHit(string name, Color color, float scale, bool stars = false)
        {
            var root = FxRoot(name);
            var t = root.transform;
            new Fx(t, "Flash", FxMaterial(name + "Flash", "vfx_glow", color, 2.5f))
                .Burst(1).Life(0.12f, 0.12f).Size(1.4f * scale, 1.4f * scale).SizeOverLife(Curve(0f, 0.6f, 1f, 1.2f)).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            new Fx(t, "Sparks", FxMaterial(name + "Spark", "vfx_glow", color, 3f))
                .Burst(12).Sphere(0.1f).Speed(5f * scale, 10f * scale).Life(0.15f, 0.32f).Size(0.08f, 0.14f).Drag(3f)
                .Stretched(2.5f, 0.04f).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            new Fx(t, "Ring", FxMaterial(name + "Ring", "vfx_ring", color, 1.8f))
                .Burst(1).Life(0.22f, 0.22f).Size(1.6f * scale, 1.6f * scale).SizeOverLife(Curve(0f, 0.15f, 1f, 1f)).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            if (stars)
            {
                new Fx(t, "Stars", FxMaterial(name + "Star", "vfx_star", ArcanePink, 2.5f))
                    .Burst(6).Sphere(0.3f).Speed(0.5f, 1.8f).Life(0.35f, 0.6f).Size(0.18f, 0.35f).Rotation().ColorOverLife(Fade(0f, 1f, 0.6f, 1f, 1f, 0f));
            }
            return SaveTimed(root, 0.7f);
        }

        private static GameObject ProjectileRoot(string name, float radius, int layer = GameLayers.PlayerProjectile)
        {
            var root = CreateRoot(name, layer);
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            var body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.linearDamping = 0f;
            return root;
        }

        private static Projectile SaveProjectile(GameObject root, float lifetime, TimedRelease impact)
        {
            var projectile = root.AddComponent<Projectile>();
            SerializedSetter.For(projectile).Set("lifetime", lifetime).Set("impactEffect", impact).Apply();
            root.AddComponent<VfxRestart>();
            return SavePrefab<Projectile>(root, $"{PrefabsFolder}/Projectiles/{root.name}.prefab");
        }

        /// <summary>A looping glow that always sits on its parent (the "head" of projectiles and orbs).</summary>
        private static void GlowCore(Transform parent, string name, Color color, float size, float intensity)
        {
            new Fx(parent, name, FxMaterial(name, "vfx_glow", color, intensity))
                .Loop(0.5f).Rate(40f).Life(0.1f, 0.14f).Size(size * 0.85f, size).Max(12).ColorOverLife(Fade(0f, 0.6f, 0.3f, 1f, 1f, 0f));
        }

        /// <summary>
        /// Shield bubble shown around the player while Phase Step's immunity lasts: a steady translucent shell, thin
        /// ripples running out across it and sparkles on its surface. Starts hidden; ImmunityCue toggles it.
        /// </summary>
        private static GameObject BuildImmunityBubble(Transform player)
        {
            var bubble = new GameObject("ImmunityBubble") { layer = player.gameObject.layer };
            bubble.transform.SetParent(player, false);
            bubble.transform.localPosition = new Vector3(0f, 1f, 0f);
            var t = bubble.transform;

            new Fx(t, "Shell", FxMaterial("ImmunityShell", "vfx_bubble", ArcanePink, 1.3f))
                .Loop(1f).Burst(1).Rate(0f).Life(9999f, 9999f).Size(3.1f, 3.1f).Max(1);
            new Fx(t, "Ripples", FxMaterial("ImmunityRipple", "vfx_ring_thin", ArcanePink, 1.8f))
                .Loop(1f).Rate(2.2f).Life(0.9f, 0.9f).Size(3.1f, 3.1f).Max(4)
                .SizeOverLife(Curve(0f, 0.7f, 1f, 1.05f)).ColorOverLife(Fade(0f, 0f, 0.3f, 0.8f, 1f, 0f));
            new Fx(t, "Sparkles", FxMaterial("ImmunitySparkle", "vfx_star", ArcanePink, 2.5f))
                .Loop(1f).Rate(14f).Sphere(1.45f, 0.05f).Life(0.35f, 0.6f).Size(0.12f, 0.22f).Rotation().Max(16)
                .ColorOverLife(Fade(0f, 0f, 0.3f, 1f, 1f, 0f));

            bubble.SetActive(false);
            return bubble;
        }

        // ---------------------------------------------------------------- Projectiles

        private static Projectile BuildBoltProjectile(TimedRelease impact)
        {
            var root = ProjectileRoot("PlayerBolt", 0.2f);
            var t = root.transform;
            GlowCore(t, "BoltGlow", BoltCyan, 0.7f, 2.2f);
            GlowCore(t, "BoltCore", Color.white, 0.28f, 3f);
            AddTrail(t, FxMaterial("BoltTrail", "vfx_trail", BoltCyan, 2f), 0.16f, 0.3f);
            new Fx(t, "Sparkles", FxMaterial("BoltSparkle", "vfx_star", BoltCyan, 2.5f))
                .World().Loop().RatePerMeter(3f).Life(0.25f, 0.45f).Size(0.1f, 0.2f).Rotation().Velocity(-Vector3.one * 0.4f, Vector3.one * 0.4f)
                .ColorOverLife(Fade(0f, 1f, 1f, 0f));
            return SaveProjectile(root, 1.2f, impact);
        }

        private static Projectile BuildFireballProjectile(TimedRelease explosion)
        {
            var root = ProjectileRoot("Fireball", 0.4f);
            var t = root.transform;
            GlowCore(t, "FireGlow", FireOrange, 1.9f, 2f);
            GlowCore(t, "FireCore", FireYellow, 0.8f, 3f);
            new Fx(t, "Flames", FxMaterial("FireFlames", "vfx_fire_sheet", Color.white, 2f))
                .World().Loop().Rate(35f).RatePerMeter(5f).Sphere(0.25f).Life(0.3f, 0.5f).Size(0.7f, 1.1f).Rotation(-20f, 20f).Flipbook()
                .Velocity(new Vector3(-0.3f, 0.8f, -0.3f), new Vector3(0.3f, 1.6f, 0.3f)).SizeOverLife(Curve(0f, 1f, 1f, 0.3f))
                .ColorOverLife(FireGradient());
            new Fx(t, "Smoke", FxMaterial("FireSmoke", "vfx_smoke_sheet", new Color(0.18f, 0.15f, 0.14f, 0.55f), additive: false))
                .World().Loop().RatePerMeter(2.5f).Sphere(0.3f).Life(0.6f, 0.9f).Size(0.6f, 0.9f).Rotation().Flipbook()
                .Velocity(new Vector3(0f, 0.4f, 0f), new Vector3(0f, 1f, 0f)).SizeOverLife(Curve(0f, 0.8f, 1f, 1.8f)).ColorOverLife(Fade(0f, 0f, 0.2f, 1f, 1f, 0f));
            new Fx(t, "Embers", FxMaterial("FireEmber", "vfx_glow", FireYellow, 3f))
                .World().Loop().RatePerMeter(4f).Sphere(0.3f).Life(0.4f, 0.8f).Size(0.06f, 0.12f).Gravity(-0.3f)
                .Velocity(-Vector3.one * 0.8f, Vector3.one * 0.8f).ColorOverLife(Fade(FireYellow, FireRed, 0f, 1f, 1f, 0f));
            AddTrail(t, FxMaterial("FireTrail", "vfx_trail", FireOrange, 1.6f), 0.18f, 0.7f);
            return SaveProjectile(root, 1.6f, explosion);
        }

        private static Gradient FireGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(FireYellow, 0f), new GradientColorKey(FireOrange, 0.35f), new GradientColorKey(FireRed, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 0.85f) });
            return gradient;
        }

        private static Projectile BuildLanceProjectile(MaterialSet m, TimedRelease impact)
        {
            var root = ProjectileRoot("ArcaneLance", 0.35f);
            var t = root.transform;

            // The crystal points along +Z (flight direction); the exported mesh's tip is its longer end.
            var mesh = LoadVfxMesh("ArcaneLance");
            var crystal = new GameObject("Crystal") { layer = root.layer };
            crystal.transform.SetParent(t, false);
            crystal.transform.localScale = new Vector3(1.4f, 1.4f, 2.6f);
            if (mesh != null && Mathf.Abs(mesh.bounds.min.z) > mesh.bounds.max.z)
                crystal.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            crystal.AddComponent<MeshFilter>().sharedMesh = mesh;
            var crystalRenderer = crystal.AddComponent<MeshRenderer>();
            crystalRenderer.sharedMaterial = m.ArcaneShard;
            crystalRenderer.shadowCastingMode = ShadowCastingMode.Off;

            GlowCore(t, "LanceGlow", ArcaneViolet, 1.4f, 1.8f);
            AddTrail(t, FxMaterial("LanceTrail", "vfx_trail", ArcaneViolet, 2.2f), 0.28f, 0.45f);
            new Fx(t, "Sparkles", FxMaterial("LanceSparkle", "vfx_star", ArcanePink, 2.5f))
                .World().Loop().RatePerMeter(4f).Sphere(0.2f).Life(0.3f, 0.6f).Size(0.12f, 0.26f).Rotation()
                .Velocity(-Vector3.one * 0.5f, Vector3.one * 0.5f).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            return SaveProjectile(root, 1.2f, impact);
        }

        // ---------------------------------------------------------------- Explosions

        /// <summary>Fiery explosion sized for <paramref name="radius"/> (1 for effects scaled by the caller).</summary>
        private static TimedRelease BuildExplosion(string name, float radius, bool scorch, bool debris = false)
        {
            var root = FxRoot(name);
            var t = root.transform;
            var r = radius;
            new Fx(t, "Flash", FxMaterial(name + "Flash", "vfx_glow", FireYellow, 1.5f), Vector3.up * 0.5f * r)
                .Burst(1).Life(0.12f, 0.12f).Size(2.2f * r, 2.2f * r).SizeOverLife(Curve(0f, 0.5f, 1f, 1.1f)).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            new Fx(t, "Fireballs", FxMaterial(name + "Fire", "vfx_fire_sheet", Color.white, 1.6f), Vector3.up * 0.4f * r)
                .Burst(16).Sphere(0.25f * r).Speed(2f * r, 4.5f * r).Drag(4f).Life(0.45f, 0.75f).Size(0.9f * r, 1.5f * r).Rotation().Flipbook()
                .Velocity(Vector3.up * 0.3f * r, Vector3.up * 1.2f * r).SizeOverLife(Curve(0f, 0.6f, 0.3f, 1f, 1f, 0.7f)).ColorOverLife(FireGradient());
            new Fx(t, "Smoke", FxMaterial(name + "Smoke", "vfx_smoke_sheet", new Color(0.16f, 0.13f, 0.12f, 0.7f), additive: false), Vector3.up * 0.4f * r)
                .Burst(10).Delay(0.08f).Sphere(0.35f * r).Speed(0.8f * r, 2f * r).Drag(2f).Life(0.9f, 1.5f).Size(1f * r, 1.6f * r).Rotation().Flipbook()
                .Velocity(Vector3.up * 0.4f * r, Vector3.up * 0.9f * r).SizeOverLife(Curve(0f, 0.7f, 1f, 1.6f)).ColorOverLife(Fade(0f, 0f, 0.15f, 1f, 1f, 0f));
            new Fx(t, "Sparks", FxMaterial(name + "Spark", "vfx_glow", FireYellow, 3f), Vector3.up * 0.4f * r)
                .Burst(24).Sphere(0.2f * r).Speed(5f * r, 10f * r).Drag(2f).Gravity(1.2f).Life(0.35f, 0.7f).Size(0.08f, 0.15f)
                .Stretched(3f, 0.05f).ColorOverLife(Fade(FireYellow, FireOrange, 0f, 1f, 1f, 0f));
            new Fx(t, "Shockwave", FxMaterial(name + "Wave", "vfx_ring", FireOrange, 1.6f), Vector3.up * 0.08f)
                .Horizontal().Burst(1).Life(0.35f, 0.35f).Size(2.6f * r, 2.6f * r).SizeOverLife(Curve(0f, 0.1f, 0.4f, 0.85f, 1f, 1f))
                .ColorOverLife(Fade(0f, 1f, 1f, 0f));
            if (scorch)
            {
                new Fx(t, "Scorch", FxMaterial(name + "Scorch", "vfx_scorch", new Color(1f, 1f, 1f, 0.85f), additive: false), Vector3.up * 0.04f)
                    .Horizontal().Burst(1).Life(3.5f, 3.5f).Size(2.2f * r, 2.2f * r).Rotation().ColorOverLife(Fade(0f, 0f, 0.05f, 1f, 0.6f, 1f, 1f, 0f));
            }
            if (debris)
            {
                new Fx(t, "Debris", FxLitMaterial("MeteorDebris", new Color(0.2f, 0.16f, 0.14f), 0.2f, new Color(0.9f, 0.25f, 0.02f)), Vector3.up * 0.3f * r)
                    .Mesh(LoadVfxMesh("MeteorRock")).Burst(10).Hemisphere(0.3f * r).Speed(3f * r, 6f * r).Gravity(2.5f).Life(0.6f, 1f)
                    .Size(0.1f * r, 0.22f * r).Rotation3D(180f).Configure(ps =>
                    {
                        var rotation = ps.rotationOverLifetime;
                        rotation.enabled = true;
                        rotation.separateAxes = true;
                        rotation.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
                        rotation.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
                        rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
                    }).ColorOverLife(Fade(0f, 1f, 0.8f, 1f, 1f, 0f));
            }
            return SaveTimed(root, scorch ? 3.6f : 1.6f);
        }

        // ---------------------------------------------------------------- Meteor Shower

        private static DelayedAreaStrike BuildMeteorStrike(MaterialSet m, TimedRelease impact)
        {
            var root = FxRoot("MeteorStrike");
            var t = root.transform;
            var outline = FlatQuad(t, "Outline", TelegraphMaterial("SpellRune", "vfx_rune_meteor", new Color(1f, 0.55f, 0.15f, 0.6f)), 0.05f);
            var fill = FlatQuad(t, "Fill", TelegraphMaterial("SpellFill", "vfx_disc", new Color(1f, 0.45f, 0.08f, 0.35f)), 0.06f);
            fill.localScale = new Vector3(0f, 1f, 0f);

            var falling = new GameObject("Meteor") { layer = root.layer }.transform;
            falling.SetParent(t, false);
            falling.localPosition = new Vector3(0f, 12f, 0f);
            var rock = new GameObject("Rock") { layer = root.layer };
            rock.transform.SetParent(falling, false);
            rock.transform.localScale = Vector3.one * 1.4f;
            rock.AddComponent<MeshFilter>().sharedMesh = LoadVfxMesh("MeteorRock");
            var rockRenderer = rock.AddComponent<MeshRenderer>();
            rockRenderer.sharedMaterial = Lit("MeteorRock", new Color(0.17f, 0.13f, 0.12f), smoothness: 0.15f, emission: new Color(0.35f, 0.07f, 0.01f));
            rockRenderer.shadowCastingMode = ShadowCastingMode.Off;
            // Molten core shows through the dips of the rock.
            DisableShadows(AddVisual(falling, PrimitiveType.Sphere, m.Meteor, Vector3.zero, Vector3.one * 1.12f, name: "Core"));
            GlowCore(falling, "MeteorGlow", FireOrange, 3.2f, 1.6f);
            new Fx(falling, "FireTrail", FxMaterial("MeteorFlames", "vfx_fire_sheet", Color.white, 2.2f))
                .World().Loop().Rate(45f).Sphere(0.5f).Life(0.35f, 0.55f).Size(1.2f, 1.9f).Rotation().Flipbook()
                .SizeOverLife(Curve(0f, 1f, 1f, 0.4f)).ColorOverLife(FireGradient());
            new Fx(falling, "SmokeTrail", FxMaterial("MeteorSmoke", "vfx_smoke_sheet", new Color(0.14f, 0.12f, 0.11f, 0.6f), additive: false))
                .World().Loop().Rate(14f).Sphere(0.5f).Life(0.8f, 1.2f).Size(1.2f, 1.8f).Rotation().Flipbook()
                .SizeOverLife(Curve(0f, 0.8f, 1f, 2f)).ColorOverLife(Fade(0f, 0f, 0.15f, 1f, 1f, 0f));
            new Fx(falling, "Embers", FxMaterial("MeteorEmber", "vfx_glow", FireYellow, 3f))
                .World().Loop().Rate(30f).Sphere(0.6f).Life(0.4f, 0.8f).Size(0.08f, 0.16f).Velocity(-Vector3.one * 1.5f, Vector3.one * 1.5f)
                .ColorOverLife(Fade(FireYellow, FireRed, 0f, 1f, 1f, 0f));
            falling.gameObject.SetActive(false);

            var strike = root.AddComponent<DelayedAreaStrike>();
            SerializedSetter.For(strike)
                .Set("outline", outline)
                .Set("fill", fill)
                .Set("fallingObject", falling)
                .Set("impactEffect", impact)
                .Apply();
            return SavePrefab<DelayedAreaStrike>(root, $"{PrefabsFolder}/Projectiles/MeteorStrike.prefab");
        }

        /// <summary>
        /// Holder with a unit-diameter quad lying on the ground. <see cref="DelayedAreaStrike"/> scales holders in X/Z,
        /// which a flat quad (rotated to face up) can't take directly.
        /// </summary>
        private static Transform FlatQuad(Transform parent, string name, Material material, float height)
        {
            var holder = new GameObject(name) { layer = parent.gameObject.layer }.transform;
            holder.SetParent(parent, false);
            holder.localPosition = new Vector3(0f, height, 0f);
            DisableShadows(AddVisual(holder, PrimitiveType.Quad, material, Vector3.zero, Vector3.one, new Vector3(90f, 0f, 0f), "Quad"));
            return holder;
        }

        private static Material TelegraphMaterial(string name, string texture, Color color) =>
            Transparent("Telegraph" + name, color, LoadVfxTexture(texture));

        // ---------------------------------------------------------------- Frost Nova

        private static AreaEffect BuildFrostNova()
        {
            var root = FxRoot("FrostNova");
            var t = root.transform;
            new Fx(t, "Rune", FxMaterial("FrostRune", "vfx_rune_frost", FrostBlue, 1.8f), Vector3.up * 0.06f)
                .Horizontal().Burst(1).Life(1.2f, 1.2f).Size(2.3f, 2.3f).Rotation().Spin(35f, 35f)
                .SizeOverLife(Curve(0f, 0.8f, 0.15f, 1f, 1f, 1.05f)).ColorOverLife(Fade(0f, 0f, 0.08f, 1f, 0.6f, 1f, 1f, 0f));
            new Fx(t, "Flash", FxMaterial("FrostFlash", "vfx_disc", FrostBlue, 1.2f), Vector3.up * 0.08f)
                .Horizontal().Burst(1).Life(0.3f, 0.3f).Size(2f, 2f).ColorOverLife(Fade(0f, 0.8f, 1f, 0f));
            new Fx(t, "Shockwave", FxMaterial("FrostWave", "vfx_ring", Color.Lerp(FrostBlue, Color.white, 0.4f), 2f), Vector3.up * 0.1f)
                .Horizontal().Burst(1).Life(0.4f, 0.4f).Size(2.2f, 2.2f).SizeOverLife(Curve(0f, 0.1f, 0.5f, 0.9f, 1f, 1f))
                .ColorOverLife(Fade(0f, 1f, 1f, 0f));
            new Fx(t, "Shards", FxLitMaterial("IceShard", new Color(0.75f, 0.94f, 1f, 0.9f), 0.92f, new Color(0.15f, 0.45f, 0.7f)))
                .Mesh(LoadVfxMesh("IceShard")).Burst(22).Circle(0.9f, 0.25f).Life(0.9f, 1.15f).Rotation3D(22f)
                .Size3D(new Vector2(0.14f, 0.24f), new Vector2(0.18f, 0.34f), new Vector2(0.14f, 0.24f))
                .SizeOverLife(Curve(0f, 0f, 0.08f, 1.1f, 0.15f, 1f, 0.75f, 1f, 1f, 0f)).ColorOverLife(Fade(0f, 1f, 0.8f, 1f, 1f, 0f));
            new Fx(t, "Mist", FxMaterial("FrostMist", "vfx_smoke_sheet", new Color(0.8f, 0.95f, 1f, 0.5f), additive: false), Vector3.up * 0.15f)
                .Burst(18).Circle(0.8f, 0.5f).Speed(0.2f, 0.5f).Life(0.8f, 1.2f).Size(0.3f, 0.5f).Rotation().Flipbook()
                .SizeOverLife(Curve(0f, 0.6f, 1f, 1.4f)).ColorOverLife(Fade(0f, 0f, 0.15f, 1f, 1f, 0f));
            new Fx(t, "Snowflakes", FxMaterial("FrostFlake", "vfx_snowflake", Color.white, 2f), Vector3.up * 0.3f)
                .Burst(30).Circle(1f).Speed(0.1f, 0.3f).Life(0.8f, 1.5f).Size(0.04f, 0.09f).Rotation().Spin(-90f, 90f)
                .Velocity(new Vector3(0f, 0.2f, 0f), new Vector3(0f, 0.6f, 0f), world: false).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            return SaveArea(root, 1.6f);
        }

        // ---------------------------------------------------------------- Chain Lightning

        private static ChainLightningEffect BuildChainLightning(TimedRelease hit, FollowTarget shock)
        {
            var root = FxRoot("ChainLightning");
            var glowMaterial = FxMaterial("LightningGlow", "vfx_beam", LightningBlue, 1.6f);
            var coreMaterial = FxMaterial("LightningCore", "vfx_beam", Color.Lerp(LightningBlue, Color.white, 0.7f), 3.5f);
            var glows = new LineRenderer[ChainLightningEffect.MaxLinks];
            var cores = new LineRenderer[ChainLightningEffect.MaxLinks * 2];
            for (var i = 0; i < glows.Length; i++)
            {
                glows[i] = BoltLine(root.transform, $"Glow{i}", glowMaterial);
                cores[i * 2] = BoltLine(root.transform, $"Core{i}a", coreMaterial);
                cores[i * 2 + 1] = BoltLine(root.transform, $"Core{i}b", coreMaterial);
            }

            var effect = root.AddComponent<ChainLightningEffect>();
            SerializedSetter.For(effect)
                .Set("glows", glows)
                .Set("cores", cores)
                .Set("hitEffect", hit)
                .Set("shockEffect", shock)
                .Apply();
            return SaveFx<ChainLightningEffect>(root);
        }

        private static LineRenderer BoltLine(Transform parent, string name, Material material)
        {
            var go = new GameObject(name) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.numCapVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        /// <summary>Crackling sparks and a flickering glow that stay on an enemy struck by lightning.</summary>
        private static FollowTarget BuildShockMark()
        {
            var root = FxRoot("ShockMark");
            var t = root.transform;
            new Fx(t, "Flicker", FxMaterial("ShockGlow", "vfx_glow", LightningBlue, 1.8f))
                .Rate(22f).Life(0.06f, 0.1f).Size(1.4f, 2.2f).Max(4).ColorOverLife(Fade(0f, 1f, 1f, 0f))
                .Configure(ps => { var main = ps.main; main.duration = 0.7f; });
            new Fx(t, "Crackle", FxMaterial("ShockSpark", "vfx_glow", Color.white, 3f))
                .Rate(70f).Sphere(0.6f, 0.2f).Speed(1f, 3f).Life(0.06f, 0.14f).Size(0.06f, 0.12f).Stretched(4f, 0.05f)
                .Configure(ps => { var main = ps.main; main.duration = 0.7f; });
            // A ring pulse that reads clearly from the top-down camera: "this one was struck".
            new Fx(t, "Pulse", FxMaterial("ShockRing", "vfx_ring", LightningBlue, 2f))
                .Burst(1).Burst(1, 0.25f).Life(0.35f, 0.35f).Size(2f, 2f).SizeOverLife(Curve(0f, 0.3f, 1f, 1f)).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            root.AddComponent<FollowTarget>();
            var release = root.AddComponent<TimedRelease>();
            SerializedSetter.For(release).Set("lifetime", 0.9f).Apply();
            root.AddComponent<VfxRestart>();
            return SaveFx<FollowTarget>(root);
        }

        // ---------------------------------------------------------------- Arcane Orbit

        private static OrbitingOrbs BuildArcaneOrbit(MaterialSet m, TimedRelease hitEffect)
        {
            // Holds the maximum orb count: 3 base + 10 projectiles (the stat cap) + 3 Split Cast upgrades.
            const int orbCount = 16;
            var root = FxRoot("ArcaneOrbit");
            var orbs = new ContactDamage[orbCount];
            var mesh = LoadVfxMesh("CrystalOrb");

            for (var i = 0; i < orbCount; i++)
            {
                var orb = AddChild(root.transform, $"Orb{i}", Vector3.forward, GameLayers.PlayerProjectile).gameObject;
                var trigger = orb.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.55f;
                var body = orb.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;

                var crystal = new GameObject("Crystal") { layer = orb.layer };
                crystal.transform.SetParent(orb.transform, false);
                crystal.transform.localScale = Vector3.one * 0.55f;
                crystal.transform.localRotation = Quaternion.Euler(20f * i, 35f * i, 0f);
                crystal.AddComponent<MeshFilter>().sharedMesh = mesh;
                var crystalRenderer = crystal.AddComponent<MeshRenderer>();
                crystalRenderer.sharedMaterial = m.ArcaneShard;
                crystalRenderer.shadowCastingMode = ShadowCastingMode.Off;

                GlowCore(orb.transform, "OrbGlow", ArcaneViolet, 1.3f, 1.6f);
                AddTrail(orb.transform, FxMaterial("OrbTrail", "vfx_trail", ArcaneViolet, 1.8f), 0.3f, 0.45f);
                new Fx(orb.transform, "Sparkles", FxMaterial("OrbSparkle", "vfx_star", ArcanePink, 2.5f))
                    .World().Loop().Rate(10f).Sphere(0.35f).Life(0.3f, 0.6f).Size(0.1f, 0.22f).Rotation()
                    .ColorOverLife(Fade(0f, 1f, 1f, 0f));

                orbs[i] = orb.AddComponent<ContactDamage>();
                SerializedSetter.For(orbs[i]).Set("hitEffect", hitEffect).Apply();
            }

            var orbiting = root.AddComponent<OrbitingOrbs>();
            SerializedSetter.For(orbiting).Set("orbs", orbs).Apply();
            root.AddComponent<VfxRestart>();
            return SavePrefab<OrbitingOrbs>(root, $"{PrefabsFolder}/Projectiles/ArcaneOrbit.prefab");
        }

        // ---------------------------------------------------------------- Toxic Miasma

        private static DamageZone BuildToxicMiasma()
        {
            var root = FxRoot("ToxicMiasma");
            var area = new GameObject("Area") { layer = root.layer }.transform;
            area.SetParent(root.transform, false);

            // Decals re-emit on a loop and cross-fade, so the zone can last as long as its Duration upgrades allow.
            new Fx(area, "Puddle", FxMaterial("ToxicPuddle", "vfx_disc", new Color(0.2f, 0.45f, 0.08f, 0.5f), additive: false), Vector3.up * 0.04f)
                .Horizontal().Manual().Loop(1.2f).Burst(1).Life(2.4f, 2.4f).Size(2.1f, 2.1f).Rotation().Max(3).ColorOverLife(Fade(0f, 0f, 0.25f, 1f, 0.75f, 1f, 1f, 0f));
            new Fx(area, "Rune", FxMaterial("ToxicRune", "vfx_rune_toxic", ToxicGreen, 0.75f), Vector3.up * 0.06f)
                .Horizontal().Manual().Loop(1.2f).Burst(1).Life(2.4f, 2.4f).Size(2f, 2f).Rotation().Spin(12f, 12f).Max(3)
                .ColorOverLife(Fade(0f, 0f, 0.25f, 1f, 0.75f, 1f, 1f, 0f));
            new Fx(area, "Gas", FxMaterial("ToxicGas", "vfx_smoke_sheet", new Color(0.42f, 0.85f, 0.2f, 0.45f), additive: false), Vector3.up * 0.2f)
                .Manual().Loop().Rate(12f).Circle(0.85f).World().Life(1.2f, 1.8f).Size(0.35f, 0.6f).Rotation().Spin(-20f, 20f).Flipbook()
                .Velocity(new Vector3(0f, 0.08f, 0f), new Vector3(0f, 0.2f, 0f), world: false).SizeOverLife(Curve(0f, 0.6f, 1f, 1.3f))
                .ColorOverLife(Fade(0f, 0f, 0.2f, 1f, 1f, 0f));
            new Fx(area, "Bubbles", FxMaterial("ToxicBubble", "vfx_bubble", ToxicGreen, 1.6f), Vector3.up * 0.05f)
                .Manual().Loop().Rate(30f).Circle(0.9f).Life(0.6f, 1.1f).Size(0.03f, 0.07f)
                .Velocity(new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0.3f, 0f), world: false).SizeOverLife(Curve(0f, 0.3f, 0.85f, 1.2f, 1f, 0f));
            new Fx(area, "Wisps", FxMaterial("ToxicWisp", "vfx_glow", ToxicGreen, 2f), Vector3.up * 0.1f)
                .Manual().Loop().Rate(14f).Circle(0.9f).Life(1f, 1.6f).Size(0.02f, 0.045f).Noise(0.15f, 0.8f)
                .Velocity(new Vector3(0f, 0.25f, 0f), new Vector3(0f, 0.5f, 0f), world: false).ColorOverLife(Fade(0f, 0f, 0.2f, 1f, 1f, 0f));

            var zone = root.AddComponent<DamageZone>();
            SerializedSetter.For(zone).Set("area", area).Apply();
            return SavePrefab<DamageZone>(root, $"{PrefabsFolder}/Projectiles/ToxicMiasma.prefab");
        }

        // ---------------------------------------------------------------- Phase Step

        private static TimedRelease BuildPhaseDepart()
        {
            var root = FxRoot("PhaseDepart");
            var t = root.transform;
            new Fx(t, "Flash", FxMaterial("PhaseFlash", "vfx_glow", ArcaneViolet, 2.5f))
                .Burst(1).Life(0.18f, 0.18f).Size(3f, 3f).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            new Fx(t, "Puffs", FxMaterial("PhasePuff", "vfx_smoke_sheet", ArcaneViolet, 1.5f))
                .Burst(12).Sphere(0.4f).Speed(1.5f, 3.5f).Drag(4f).Life(0.4f, 0.65f).Size(0.7f, 1.1f).Rotation().Flipbook()
                .SizeOverLife(Curve(0f, 0.7f, 1f, 1.3f)).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            new Fx(t, "Streaks", FxMaterial("PhaseStreak", "vfx_glow", ArcanePink, 3f))
                .Burst(18).ConeUp(15f, 0.5f).Speed(6f, 11f).Drag(2f).Life(0.25f, 0.45f).Size(0.08f, 0.14f).Stretched(4f, 0.05f)
                .ColorOverLife(Fade(0f, 1f, 1f, 0f));
            new Fx(t, "Stars", FxMaterial("PhaseStar", "vfx_star", ArcanePink, 2.5f))
                .Burst(14).Sphere(0.7f).Speed(0.3f, 1.2f).Life(0.4f, 0.8f).Size(0.15f, 0.3f).Rotation().ColorOverLife(Fade(0f, 1f, 1f, 0f));
            return SaveTimed(root, 1f);
        }

        private static AreaEffect BuildPhaseArrival()
        {
            var root = FxRoot("PhaseArrival");
            var t = root.transform;
            new Fx(t, "Rune", FxMaterial("PhaseRune", "vfx_rune_arcane", ArcaneViolet, 1.8f), Vector3.up * 0.06f)
                .Horizontal().Burst(1).Life(0.9f, 0.9f).Size(2.3f, 2.3f).Rotation().Spin(-60f, -60f)
                .SizeOverLife(Curve(0f, 0.6f, 0.2f, 1f, 1f, 1.05f)).ColorOverLife(Fade(0f, 0f, 0.08f, 1f, 0.55f, 1f, 1f, 0f));
            new Fx(t, "Shockwave", FxMaterial("PhaseWave", "vfx_ring", ArcanePink, 2f), Vector3.up * 0.1f)
                .Horizontal().Burst(1).Life(0.4f, 0.4f).Size(2.2f, 2.2f).SizeOverLife(Curve(0f, 0.1f, 0.5f, 0.9f, 1f, 1f))
                .ColorOverLife(Fade(0f, 1f, 1f, 0f));
            new Fx(t, "Column", FxMaterial("PhaseColumn", "vfx_glow", ArcaneViolet, 2.2f), Vector3.up * 0.6f)
                .Vertical().Burst(1).Life(0.35f, 0.35f).Size3D(new Vector2(0.5f, 0.5f), new Vector2(1.6f, 1.6f), Vector2.one)
                .SizeOverLife(Curve(0f, 1f, 1f, 0.2f)).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            new Fx(t, "Stars", FxMaterial("PhaseArrivalStar", "vfx_star", ArcanePink, 2.5f), Vector3.up * 0.2f)
                .Burst(16).Circle(0.6f).Speed(0.8f, 2f).Life(0.4f, 0.8f).Size(0.05f, 0.1f).Rotation()
                .Velocity(new Vector3(0f, 0.5f, 0f), new Vector3(0f, 1.2f, 0f), world: false).ColorOverLife(Fade(0f, 1f, 1f, 0f));
            return SaveArea(root, 1.1f);
        }
    }
}
