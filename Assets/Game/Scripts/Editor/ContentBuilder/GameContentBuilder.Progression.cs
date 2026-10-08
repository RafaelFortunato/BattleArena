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
                    "Hurls a fireball at the nearest enemy that explodes on impact.", new Color(1f, 0.6f, 0.2f), cooldown: 4f, attackRatio: 3.75f, damageUnit: "",
                    s => s.Set("projectilePrefab", fx.Fireball).Set("speed", 16f).Set("explosionRadius", 3.5f).Set("pierce", 0)
                        // Level flight: with Pierce it carries on past its first blast, and a downward shot would hit the floor.
                        .Set("flightHeight", 1.1f)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraProjectiles, K.Area })),
                Spell<ProjectileSpell>("ArcaneLance", "Arcane Lance",
                    "A long-range lance of arcane energy that pierces every enemy in its path.", new Color(0.8f, 0.55f, 1f), cooldown: 3f, attackRatio: 4.5f, damageUnit: "per enemy",
                    s => s.Set("projectilePrefab", fx.ArcaneLance).Set("speed", 24f).Set("pierce", 0).Set("infinitePierce", true)
                        .Set("targetingRange", 40f).Set("explosionRadius", 0f).Set("flightHeight", 1.1f)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraProjectiles })),
                Spell<FrostNovaSpell>("FrostNova", "Frost Nova",
                    "Freezing burst around you that damages and slows every enemy hit.", new Color(0.55f, 0.9f, 1f), cooldown: 8f, attackRatio: 3f, damageUnit: "",
                    s => s.Set("radius", 7f).Set("slowMultiplier", 0.4f).Set("slowDuration", 3f).Set("ringEffect", fx.FrostNova)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.Area, K.Duration })),
                Spell<MeteorShowerSpell>("MeteorShower", "Meteor Shower",
                    "Calls down a volley of meteors on the enemies around you.", new Color(1f, 0.4f, 0.15f), cooldown: 20f, attackRatio: 7.5f, damageUnit: "per meteor",
                    s => s.Set("meteorPrefab", fx.MeteorStrike).Set("meteorCount", 6).Set("meteorsPerProjectileUpgrade", 2).Set("impactRadius", 3f).Set("areaRadius", 12f)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraProjectiles, K.Area })),
                Spell<ChainLightningSpell>("ChainLightning", "Chain Lightning",
                    "Strikes the nearest enemy and jumps to up to 6 foes, weaker each jump.", new Color(0.6f, 0.85f, 1f), cooldown: 5f, attackRatio: 3.5f, damageUnit: "to the first target",
                    s => s.Set("maxTargets", 6).Set("jumpRadius", 7f).Set("damageFalloff", 0.85f).Set("chainEffect", fx.ChainLightning)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.ExtraBounces })),
                Spell<ArcaneOrbitSpell>("ArcaneOrbit", "Arcane Orbit",
                    "Three orbs circle you for 6 seconds, hitting every enemy they touch.", new Color(0.75f, 0.45f, 1f), cooldown: 12f, attackRatio: 1.75f, damageUnit: "per hit",
                    s => s.Set("orbsPrefab", fx.ArcaneOrbit).Set("orbCount", 3).Set("radius", 3.2f).Set("duration", 6f).Set("degreesPerSecond", 220f)
                        .Set("supportedUpgrades", new[] { K.CooldownReduction, K.Damage, K.ExtraProjectiles, K.Duration, K.Area })),
                Spell<ToxicMiasmaSpell>("ToxicMiasma", "Toxic Miasma",
                    "Poison cloud on the nearest enemy that damages and slows for 5 seconds.", new Color(0.55f, 0.95f, 0.3f), cooldown: 10f, attackRatio: 1f, damageUnit: "every 0.5s",
                    s => s.Set("zonePrefab", fx.ToxicMiasma).Set("radius", 4.5f).Set("duration", 5f).Set("tickInterval", 0.5f).Set("slowMultiplier", 0.7f)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction, K.Damage, K.Area, K.Duration })),
                Spell<PhaseStepSpell>("PhaseStep", "Phase Step",
                    "Blink forward and become immune to damage for 2 seconds. Deals no damage.", new Color(0.85f, 0.6f, 1f), cooldown: 6f, attackRatio: 0f, damageUnit: "",
                    s => s.Set("distance", 11f).Set("invulnerabilityDuration", 2f).Set("arrivalRingRadius", 1.5f)
                        .Set("departEffect", fx.PhaseDepart).Set("arrivalRing", fx.PhaseArrival)
                        .Set("supportedUpgrades", new[] { K.ExtraCharge, K.CooldownReduction })),
            };
        }

        /// <param name="attackRatio">Damage as a share of the player's Attack: 3.75 deals 375%.</param>
        /// <param name="damageUnit">What one share of that damage is, for the card ("per meteor"); empty for a single hit.</param>
        private static T Spell<T>(string file, string displayName, string description, Color color, float cooldown, float attackRatio,
            string damageUnit, System.Func<SerializedSetter, SerializedSetter> configure) where T : SpellDefinition
        {
            var spell = LoadOrCreateAsset<T>($"{SpellsFolder}/{file}.asset");
            var setter = SerializedSetter.For(spell)
                .Set("displayName", displayName)
                .Set("description", description)
                .Set("color", color)
                .Set("icon", LoadIcon("spell-" + ToKebabCase(file)))
                .Set("cooldown", cooldown)
                .Set("attackRatio", attackRatio)
                .Set("damageUnit", damageUnit);
            configure(setter).Apply();
            // A value that matches the field default leaves the asset clean, and a renamed field would then linger in the file.
            EditorUtility.SetDirty(spell);
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
                Upgrade("Power", "Power", StatType.Attack, 1f, UpgradeRarity.Common),
                Upgrade("ArcaneMight", "Arcane Might", StatType.Attack, 2f, UpgradeRarity.Rare),
                Upgrade("Precision", "Precision", StatType.CritChance, 0.03f, UpgradeRarity.Common),
                Upgrade("EagleEye", "Eagle Eye", StatType.CritChance, 0.06f, UpgradeRarity.Rare),
                Upgrade("Brutality", "Brutality", StatType.CritDamage, 0.15f, UpgradeRarity.Common),
                Upgrade("Executioner", "Executioner", StatType.CritDamage, 0.3f, UpgradeRarity.Rare),
                Upgrade("Haste", "Haste", StatType.AttackSpeed, 0.1f, UpgradeRarity.Common),
                Upgrade("Quickening", "Quickening", StatType.AttackSpeed, 0.2f, UpgradeRarity.Rare),
                Upgrade("Focus", "Focus", StatType.CooldownReduction, 0.04f, UpgradeRarity.Common),
                Upgrade("Clarity", "Clarity", StatType.CooldownReduction, 0.08f, UpgradeRarity.Rare),
                Upgrade("Expansion", "Expansion", StatType.Area, 0.08f, UpgradeRarity.Common),
                Upgrade("Colossus", "Colossus", StatType.Area, 0.15f, UpgradeRarity.Rare),
                Upgrade("Persistence", "Persistence", StatType.Duration, 0.1f, UpgradeRarity.Common),
                Upgrade("Longevity", "Longevity", StatType.Duration, 0.2f, UpgradeRarity.Rare),
                Upgrade("Multishot", "Multishot", StatType.Multishot, 1f, UpgradeRarity.Epic),
                Upgrade("Penetration", "Penetration", StatType.Pierce, 1f, UpgradeRarity.Rare, weight: 0.7f),
                Upgrade("Magnetism", "Magnetism", StatType.PickupRadius, 0.5f, UpgradeRarity.Common),
                Upgrade("IronSkin", "Iron Skin", StatType.Armor, 10f, UpgradeRarity.Common),
                Upgrade("Fortress", "Fortress", StatType.Armor, 25f, UpgradeRarity.Rare),
                Upgrade("Regeneration", "Regeneration", StatType.Regeneration, 0.5f, UpgradeRarity.Common),
                Upgrade("TrollBlood", "Troll Blood", StatType.Regeneration, 1f, UpgradeRarity.Rare),
                Upgrade("Wisdom", "Wisdom", StatType.ExperienceGain, 0.1f, UpgradeRarity.Common),
                Upgrade("Insight", "Insight", StatType.ExperienceGain, 0.2f, UpgradeRarity.Rare),
                Upgrade("Fortune", "Fortune", StatType.Luck, 0.15f, UpgradeRarity.Common, weight: 0.8f),
                Upgrade("Serendipity", "Serendipity", StatType.Luck, 0.3f, UpgradeRarity.Rare),

                // Epics: roughly double the rare card for the same stat. Nine at the default epic weight put an
                // epic on about 12% of level-ups (more with Luck), each individual epic under 2%.
                Upgrade("Frenzy", "Frenzy", StatType.AttackSpeed, 0.4f, UpgradeRarity.Epic),
                Upgrade("TimeWarp", "Time Warp", StatType.CooldownReduction, 0.15f, UpgradeRarity.Epic),
                Upgrade("Archmage", "Archmage", StatType.Attack, 4f, UpgradeRarity.Epic),
                Upgrade("Cataclysm", "Cataclysm", StatType.Area, 0.3f, UpgradeRarity.Epic),
                Upgrade("Eternity", "Eternity", StatType.Duration, 0.3f, UpgradeRarity.Epic),
                Upgrade("Deadeye", "Deadeye", StatType.CritChance, 0.12f, UpgradeRarity.Epic),
                Upgrade("Annihilation", "Annihilation", StatType.CritDamage, 0.6f, UpgradeRarity.Epic),
                Upgrade("Impaler", "Impaler", StatType.Pierce, 2f, UpgradeRarity.Epic),
            };

            // Mission rewards that improve one spell. Each is stronger than the best shared card for the same effect
            // (Time Warp -15% cooldown, Cataclysm/Eternity +30% area/duration), since it only helps one spell.
            SpellUpgradeDefinition[] spellUpgrades =
            {
                SpellUpgrade("ExtraCharge", "Extra Charge", K.ExtraCharge, 1f, maxRank: 2, weight: 0.8f),
                SpellUpgrade("QuickRecharge", "Quick Recharge", K.CooldownReduction, 0.2f, maxRank: 3),
                SpellUpgrade("Empowered", "Empowered", K.Damage, 0.4f, maxRank: 5),
                SpellUpgrade("SplitCast", "Split Cast", K.ExtraProjectiles, 1f, maxRank: 3, weight: 0.9f),
                SpellUpgrade("Lingering", "Lingering", K.Duration, 0.5f, maxRank: 3),
                SpellUpgrade("Amplified", "Amplified", K.Area, 0.5f, maxRank: 3),
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
                .Set("icon", LoadIcon(kind switch
                {
                    K.ExtraCharge => "upgrade-charge",
                    K.CooldownReduction => "stat-cooldown-reduction",
                    K.Damage => "stat-attack",
                    K.ExtraProjectiles => "stat-multishot",
                    K.Duration => "stat-duration",
                    K.Area => "stat-area",
                    _ => "spell-chain-lightning"
                }))
                .Apply();
            return upgrade;
        }

        private static StatUpgradeDefinition Upgrade(string file, string displayName, StatType stat, float amount, UpgradeRarity rarity,
            float weight = -1f)
        {
            // Rarer cards show up less often unless a weight is given explicitly.
            if (weight < 0f)
                weight = rarity switch { UpgradeRarity.Rare => 0.45f, UpgradeRarity.Epic => 0.098f, _ => 1f };

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
