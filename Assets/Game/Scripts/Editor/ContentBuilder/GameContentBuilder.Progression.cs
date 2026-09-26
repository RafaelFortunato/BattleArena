using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Player;
using BattleArena.Pooling;
using BattleArena.Progression;
using BattleArena.Spells;
using BattleArena.Vfx;
using UnityEngine;
using UnityEngine.Rendering;
using K = BattleArena.Spells.SpellUpgradeKind;

namespace BattleArena.Editor.ContentBuilder
{
    internal static partial class GameContentBuilder
    {
        private const string SpellsFolder = DataFolder + "/Spells";
        private const string UpgradesFolder = DataFolder + "/Upgrades";

        // ---------------------------------------------------------------- Spells

        private static SpellDefinition[] BuildSpells(MaterialSet m, Projectile fireballProjectile, ExpandingRing frostRing, DelayedAreaStrike meteorStrike)
        {
            var arcaneSpark = BuildBurst("ArcaneSpark", m, new Color(0.75f, 0.45f, 1f), 10, 4f, 0.3f, 0.3f);
            var lightningHit = BuildBurst("LightningHit", m, new Color(0.6f, 0.85f, 1f), 14, 6f, 0.3f, 0.3f);
            var phaseBurst = BuildBurst("PhaseBurst", m, new Color(0.75f, 0.45f, 1f), 30, 5f, 0.45f, 0.45f, radius: 0.5f);
            var arcaneRing = BuildRing("ArcaneRing", m.ArcaneRing);
            var lightningArc = BuildLightningArc(m);
            var orbit = BuildOrbitingOrbs(m, arcaneSpark);
            var toxicZone = BuildToxicZone(m);
            var lance = BuildProjectile("ArcaneLance", m.ArcaneOrb, 0.4f, GameLayers.PlayerProjectile, 1.2f, arcaneSpark, length: 1.8f);

            return new SpellDefinition[]
            {
                Spell<ProjectileSpell>("Fireball", "Fireball",
                    "Hurls a fireball at the nearest enemy that explodes on impact.", new Color(1f, 0.6f, 0.2f), cooldown: 4f, damage: 45f,
                    s => s.Set("projectilePrefab", fireballProjectile).Set("speed", 16f).Set("explosionRadius", 3.5f).Set("pierce", 0)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraProjectiles, K.Area })),
                Spell<ProjectileSpell>("ArcaneLance", "Arcane Lance",
                    "A lance of pure arcane energy that pierces every enemy in its path.", new Color(0.8f, 0.55f, 1f), cooldown: 3f, damage: 55f,
                    s => s.Set("projectilePrefab", lance).Set("speed", 28f).Set("pierce", 50).Set("explosionRadius", 0f)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraProjectiles })),
                Spell<FrostNovaSpell>("FrostNova", "Frost Nova",
                    "Freezing burst around you that damages and slows every enemy it touches.", new Color(0.55f, 0.9f, 1f), cooldown: 8f, damage: 35f,
                    s => s.Set("radius", 7f).Set("slowMultiplier", 0.4f).Set("slowDuration", 3f).Set("ringEffect", frostRing)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.Area, K.Duration })),
                Spell<MeteorShowerSpell>("MeteorShower", "Meteor Shower",
                    "Calls down a volley of meteors on the enemies around you.", new Color(1f, 0.4f, 0.15f), cooldown: 14f, damage: 90f,
                    s => s.Set("meteorPrefab", meteorStrike).Set("meteorCount", 10).Set("impactRadius", 3f).Set("areaRadius", 12f)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraProjectiles, K.Area })),
                Spell<ChainLightningSpell>("ChainLightning", "Chain Lightning",
                    "Lightning strikes the nearest enemy and jumps between up to 6 foes, weakening with each jump.", new Color(0.6f, 0.85f, 1f), cooldown: 5f, damage: 40f,
                    s => s.Set("maxTargets", 6).Set("jumpRadius", 7f).Set("damageFalloff", 0.85f).Set("arcEffect", lightningArc).Set("hitEffect", lightningHit)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraBounces })),
                Spell<ArcaneOrbitSpell>("ArcaneOrbit", "Arcane Orbit",
                    "Three arcane orbs circle you for 6 seconds, damaging every enemy they touch.", new Color(0.75f, 0.45f, 1f), cooldown: 12f, damage: 22f,
                    s => s.Set("orbsPrefab", orbit).Set("orbCount", 3).Set("radius", 3.2f).Set("duration", 6f).Set("degreesPerSecond", 220f)
                        .Set("supportedUpgrades", new[] { K.CooldownReduction, K.Damage, K.ExtraProjectiles, K.Duration, K.Area })),
                Spell<ToxicMiasmaSpell>("ToxicMiasma", "Toxic Miasma",
                    "A poison cloud on the nearest enemy that damages and slows everything inside for 5 seconds.", new Color(0.55f, 0.95f, 0.3f), cooldown: 10f, damage: 12f,
                    s => s.Set("zonePrefab", toxicZone).Set("radius", 4.5f).Set("duration", 5f).Set("tickInterval", 0.5f).Set("slowMultiplier", 0.7f)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.Area, K.Duration })),
                Spell<PhaseStepSpell>("PhaseStep", "Phase Step",
                    "Blink forward through danger, becoming briefly invulnerable and blasting enemies where you land.", new Color(0.85f, 0.6f, 1f), cooldown: 6f, damage: 30f,
                    s => s.Set("distance", 7f).Set("blastRadius", 3.5f).Set("invulnerabilityDuration", 0.6f).Set("departEffect", phaseBurst).Set("arrivalRing", arcaneRing)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.Area })),
            };
        }

        private static T Spell<T>(string file, string displayName, string description, Color color, float cooldown, float damage,
            System.Func<SerializedSetter, SerializedSetter> configure) where T : SpellDefinition
        {
            var spell = LoadOrCreateAsset<T>($"{SpellsFolder}/{file}.asset");
            var setter = SerializedSetter.For(spell)
                .Set("displayName", displayName)
                .Set("description", description)
                .Set("color", color)
                .Set("cooldown", cooldown)
                .Set("baseDamage", damage);
            configure(setter).Apply();
            return spell;
        }

        // ---------------------------------------------------------------- Level-up cards

        private static LevelUpConfig BuildLevelUpConfig(SpellDefinition[] spells)
        {
            StatUpgradeDefinition[] upgrades =
            {
                Upgrade("SwiftBoots", "Swift Boots", StatType.MoveSpeed, 0.08f, UpgradeRarity.Common),
                Upgrade("WindWalker", "Wind Walker", StatType.MoveSpeed, 0.15f, UpgradeRarity.Rare),
                Upgrade("Vitality", "Vitality", StatType.MaxHealth, 20f, UpgradeRarity.Common),
                Upgrade("GiantsHeart", "Giant's Heart", StatType.MaxHealth, 45f, UpgradeRarity.Rare),
                Upgrade("Power", "Power", StatType.Damage, 0.1f, UpgradeRarity.Common),
                Upgrade("ArcaneMight", "Arcane Might", StatType.Damage, 0.2f, UpgradeRarity.Rare),
                Upgrade("Precision", "Precision", StatType.CritChance, 0.05f, UpgradeRarity.Common),
                Upgrade("EagleEye", "Eagle Eye", StatType.CritChance, 0.1f, UpgradeRarity.Rare),
                Upgrade("Brutality", "Brutality", StatType.CritDamage, 0.25f, UpgradeRarity.Common),
                Upgrade("Executioner", "Executioner", StatType.CritDamage, 0.5f, UpgradeRarity.Rare),
                Upgrade("Haste", "Haste", StatType.CastSpeed, 0.08f, UpgradeRarity.Common),
                Upgrade("Quickening", "Quickening", StatType.CastSpeed, 0.15f, UpgradeRarity.Rare),
                Upgrade("Expansion", "Expansion", StatType.Area, 0.1f, UpgradeRarity.Common),
                Upgrade("Colossus", "Colossus", StatType.Area, 0.2f, UpgradeRarity.Rare),
                Upgrade("Persistence", "Persistence", StatType.Duration, 0.12f, UpgradeRarity.Common),
                Upgrade("Multishot", "Multishot", StatType.Multishot, 1f, UpgradeRarity.Epic, weight: 0.8f),
                Upgrade("Penetration", "Penetration", StatType.Pierce, 1f, UpgradeRarity.Rare, weight: 0.7f),
                Upgrade("Magnetism", "Magnetism", StatType.PickupRadius, 0.25f, UpgradeRarity.Common),
                Upgrade("IronSkin", "Iron Skin", StatType.Armor, 1f, UpgradeRarity.Common),
                Upgrade("Fortress", "Fortress", StatType.Armor, 2f, UpgradeRarity.Rare),
                Upgrade("Regeneration", "Regeneration", StatType.Regeneration, 0.5f, UpgradeRarity.Common),
                Upgrade("TrollBlood", "Troll Blood", StatType.Regeneration, 1.2f, UpgradeRarity.Rare),
                Upgrade("Wisdom", "Wisdom", StatType.ExperienceGain, 0.1f, UpgradeRarity.Common),
                Upgrade("Fortune", "Fortune", StatType.Luck, 0.15f, UpgradeRarity.Common, weight: 0.8f),
                Upgrade("SoulHarvest", "Soul Harvest", StatType.LifeOnKill, 1f, UpgradeRarity.Rare, weight: 0.7f),
            };

            SpellUpgradeDefinition[] spellUpgrades =
            {
                SpellUpgrade("ExtraCharge", "Extra Charge", K.ExtraCharge, 1f, maxRank: 2, weight: 0.8f),
                SpellUpgrade("QuickRecharge", "Quick Recharge", K.CooldownReduction, 0.15f, maxRank: 3),
                SpellUpgrade("Empowered", "Empowered", K.Damage, 0.3f, maxRank: 5),
                SpellUpgrade("SplitCast", "Split Cast", K.ExtraProjectiles, 1f, maxRank: 3, weight: 0.9f),
                SpellUpgrade("Lingering", "Lingering", K.Duration, 0.3f, maxRank: 3),
                SpellUpgrade("Amplified", "Amplified", K.Area, 0.2f, maxRank: 3),
                SpellUpgrade("ArcJump", "Arc Jump", K.ExtraBounces, 2f, maxRank: 3),
            };

            var config = LoadOrCreateAsset<LevelUpConfig>($"{DataFolder}/LevelUpConfig.asset");
            SerializedSetter.For(config)
                .Set("choiceCount", 3)
                .Set("statUpgrades", upgrades)
                .Set("spellPool", spells)
                .Set("spellUpgrades", spellUpgrades)
                .Apply();
            return config;
        }

        private static SpellUpgradeDefinition SpellUpgrade(string file, string displayName, SpellUpgradeKind kind, float amount,
            int maxRank, float weight = 1f)
        {
            var upgrade = LoadOrCreateAsset<SpellUpgradeDefinition>($"{DataFolder}/SpellUpgrades/{file}.asset");
            SerializedSetter.For(upgrade)
                .Set("displayName", displayName)
                .Set("kind", kind)
                .Set("amount", amount)
                .Set("maxRank", maxRank)
                .Set("weight", weight)
                .Apply();
            return upgrade;
        }

        private static StatUpgradeDefinition Upgrade(string file, string displayName, StatType stat, float amount, UpgradeRarity rarity,
            float weight = -1f)
        {
            // Rarer cards show up less often unless a weight is given explicitly.
            if (weight < 0f)
                weight = rarity switch { UpgradeRarity.Rare => 0.45f, UpgradeRarity.Epic => 0.2f, _ => 1f };

            var upgrade = LoadOrCreateAsset<StatUpgradeDefinition>($"{UpgradesFolder}/{file}.asset");
            SerializedSetter.For(upgrade)
                .Set("displayName", displayName)
                .Set("stat", (int)stat)
                .Set("amount", amount)
                .Set("rarity", (int)rarity)
                .Set("weight", weight)
                .Apply();
            return upgrade;
        }

        // ---------------------------------------------------------------- Spell prefabs

        private static LightningArc BuildLightningArc(MaterialSet m)
        {
            var root = CreateRoot("LightningArc", GameLayers.Default);

            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = m.Particles;
            line.widthMultiplier = 0.22f;
            line.numCapVertices = 2;
            line.positionCount = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.85f, 0.95f, 1f), 0f), new GradientColorKey(new Color(0.4f, 0.7f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            line.colorGradient = gradient;

            root.AddComponent<LightningArc>();
            var release = root.AddComponent<TimedRelease>();
            SerializedSetter.For(release).Set("lifetime", 0.18f).Apply();

            return SavePrefab<LightningArc>(root, $"{PrefabsFolder}/Vfx/LightningArc.prefab");
        }

        private static OrbitingOrbs BuildOrbitingOrbs(MaterialSet m, TimedRelease hitEffect)
        {
            // Holds the maximum orb count; the spell enables 3 plus any Split Cast upgrades.
            const int orbCount = 6;
            var root = CreateRoot("ArcaneOrbit", GameLayers.Default);
            var orbs = new ContactDamage[orbCount];

            for (var i = 0; i < orbCount; i++)
            {
                var orb = AddChild(root.transform, $"Orb{i}", Vector3.forward, GameLayers.PlayerProjectile).gameObject;
                var trigger = orb.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.55f;
                var body = orb.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;

                DisableShadows(AddVisual(orb.transform, PrimitiveType.Sphere, m.ArcaneOrb, Vector3.zero, Vector3.one * 0.7f));

                orbs[i] = orb.AddComponent<ContactDamage>();
                SerializedSetter.For(orbs[i]).Set("hitEffect", hitEffect).Apply();
            }

            var orbiting = root.AddComponent<OrbitingOrbs>();
            SerializedSetter.For(orbiting).Set("orbs", orbs).Apply();
            return SavePrefab<OrbitingOrbs>(root, $"{PrefabsFolder}/Projectiles/ArcaneOrbit.prefab");
        }

        private static DamageZone BuildToxicZone(MaterialSet m)
        {
            var root = CreateRoot("ToxicMiasma", GameLayers.Default);
            var area = AddVisual(root.transform, PrimitiveType.Cylinder, m.ToxicZone, new Vector3(0f, 0.05f, 0f), new Vector3(1f, 0.01f, 1f), name: "Area");
            DisableShadows(area);

            var bubbles = AddChild(root.transform, "Bubbles", new Vector3(0f, 0.1f, 0f)).gameObject;
            var particles = bubbles.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startColor = new Color(0.5f, 1f, 0.3f, 0.7f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var emission = particles.emission;
            emission.rateOverTime = 28f;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.rotation = new Vector3(90f, 0f, 0f);
            shape.radius = 4.5f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // All three axes must use the same curve mode (random between two constants).
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            var particleRenderer = bubbles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = m.Particles;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            var zone = root.AddComponent<DamageZone>();
            SerializedSetter.For(zone).Set("area", area.transform).Set("particles", particles).Apply();
            return SavePrefab<DamageZone>(root, $"{PrefabsFolder}/Projectiles/ToxicMiasma.prefab");
        }
    }
}
