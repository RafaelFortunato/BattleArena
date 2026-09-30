using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Player;
using BattleArena.Pooling;
using BattleArena.Progression;
using BattleArena.Spells;
using BattleArena.Vfx;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using K = BattleArena.Spells.SpellUpgradeKind;

namespace BattleArena.Editor.ContentBuilder
{
    internal static partial class GameContentBuilder
    {
        private const string SpellsFolder = DataFolder + "/Spells";
        private const string UpgradesFolder = DataFolder + "/Upgrades";

        // ---------------------------------------------------------------- Spells

        private static SpellDefinition[] BuildSpells(MaterialSet m, SpellVfx fx)
        {
            return new SpellDefinition[]
            {
                Spell<ProjectileSpell>("Fireball", "Fireball",
                    "Hurls a fireball at the nearest enemy that explodes on impact.", new Color(1f, 0.6f, 0.2f), cooldown: 4f, damage: 45f,
                    s => s.Set("projectilePrefab", fx.Fireball).Set("speed", 16f).Set("explosionRadius", 3.5f).Set("pierce", 0)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraProjectiles, K.Area })),
                Spell<ProjectileSpell>("ArcaneLance", "Arcane Lance",
                    "A lance of pure arcane energy that pierces every enemy in its path.", new Color(0.8f, 0.55f, 1f), cooldown: 3f, damage: 55f,
                    s => s.Set("projectilePrefab", fx.ArcaneLance).Set("speed", 28f).Set("pierce", 50).Set("explosionRadius", 0f)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraProjectiles })),
                Spell<FrostNovaSpell>("FrostNova", "Frost Nova",
                    "Freezing burst around you that damages and slows every enemy it touches.", new Color(0.55f, 0.9f, 1f), cooldown: 8f, damage: 35f,
                    s => s.Set("radius", 7f).Set("slowMultiplier", 0.4f).Set("slowDuration", 3f).Set("ringEffect", fx.FrostNova)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.Area, K.Duration })),
                Spell<MeteorShowerSpell>("MeteorShower", "Meteor Shower",
                    "Calls down a volley of meteors on the enemies around you.", new Color(1f, 0.4f, 0.15f), cooldown: 14f, damage: 90f,
                    s => s.Set("meteorPrefab", fx.MeteorStrike).Set("meteorCount", 10).Set("impactRadius", 3f).Set("areaRadius", 12f)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraProjectiles, K.Area })),
                Spell<ChainLightningSpell>("ChainLightning", "Chain Lightning",
                    "Lightning strikes the nearest enemy and jumps between up to 6 foes, weakening with each jump.", new Color(0.6f, 0.85f, 1f), cooldown: 5f, damage: 40f,
                    s => s.Set("maxTargets", 6).Set("jumpRadius", 7f).Set("damageFalloff", 0.85f).Set("chainEffect", fx.ChainLightning)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraBounces })),
                Spell<ArcaneOrbitSpell>("ArcaneOrbit", "Arcane Orbit",
                    "Three arcane orbs circle you for 6 seconds, damaging every enemy they touch.", new Color(0.75f, 0.45f, 1f), cooldown: 12f, damage: 22f,
                    s => s.Set("orbsPrefab", fx.ArcaneOrbit).Set("orbCount", 3).Set("radius", 3.2f).Set("duration", 6f).Set("degreesPerSecond", 220f)
                        .Set("supportedUpgrades", new[] { K.CooldownReduction, K.Damage, K.ExtraProjectiles, K.Duration, K.Area })),
                Spell<ToxicMiasmaSpell>("ToxicMiasma", "Toxic Miasma",
                    "A poison cloud on the nearest enemy that damages and slows everything inside for 5 seconds.", new Color(0.55f, 0.95f, 0.3f), cooldown: 10f, damage: 12f,
                    s => s.Set("zonePrefab", fx.ToxicMiasma).Set("radius", 4.5f).Set("duration", 5f).Set("tickInterval", 0.5f).Set("slowMultiplier", 0.7f)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.Area, K.Duration })),
                Spell<PhaseStepSpell>("PhaseStep", "Phase Step",
                    "Blink forward through danger, becoming briefly invulnerable and blasting enemies where you land.", new Color(0.85f, 0.6f, 1f), cooldown: 6f, damage: 30f,
                    s => s.Set("distance", 7f).Set("blastRadius", 3.5f).Set("invulnerabilityDuration", 0.6f).Set("departEffect", fx.PhaseDepart).Set("arrivalRing", fx.PhaseArrival)
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
                .Set("icon", LoadIcon("spell-" + ToKebabCase(file)))
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
                .Set("icon", LoadIcon("stat-" + ToKebabCase(stat.ToString())))
                .Apply();
            return upgrade;
        }

        /// <summary>Line icons live in UI/Icons and are named after what they depict, e.g. "stat-move-speed".</summary>
        private static VectorImage LoadIcon(string name)
        {
            var path = $"{UIFolder}/Icons/{name}.svg";
            var icon = AssetDatabase.LoadAssetAtPath<VectorImage>(path);
            if (icon == null)
                Debug.LogWarning($"Missing icon {path}");
            return icon;
        }

        /// <summary>"MeteorShower" → "meteor-shower".</summary>
        private static string ToKebabCase(string pascalCase)
        {
            var builder = new System.Text.StringBuilder(pascalCase.Length + 4);
            for (var i = 0; i < pascalCase.Length; i++)
            {
                var c = pascalCase[i];
                if (char.IsUpper(c) && i > 0)
                    builder.Append('-');
                builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString();
        }
    }
}
