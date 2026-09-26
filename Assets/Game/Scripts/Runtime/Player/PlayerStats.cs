using System;
using System.Collections;
using System.Globalization;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Spells;
using UnityEngine;

namespace BattleArena.Player
{
    public enum StatType
    {
        MoveSpeed,
        MaxHealth,
        Damage,
        CritChance,
        CritDamage,
        CastSpeed,
        Area,
        Duration,
        Multishot,
        Pierce,
        PickupRadius,
        Armor,
        Regeneration,
        ExperienceGain,
        Luck,
        LifeOnKill
    }

    /// <summary>
    /// Character stats improved by level-up cards. Damage sources ask this component for a
    /// <see cref="DamageProfile"/> or <see cref="SpellModifiers"/>, so upgrades apply to auto attacks
    /// and spells alike without those systems knowing about individual cards.
    /// </summary>
    public sealed class PlayerStats : MonoBehaviour
    {
        private const float RegenerationTick = 0.5f;

        /// <summary>How a stat is shown on cards: a multiplier of 100%, an absolute percentage, or a flat number.</summary>
        private enum DisplayKind { Multiplier, Percent, Flat }

        private readonly struct StatInfo
        {
            public readonly string Name;
            public readonly DisplayKind Kind;
            public readonly string Suffix;

            public StatInfo(string name, DisplayKind kind, string suffix = "")
            {
                Name = name;
                Kind = kind;
                Suffix = suffix;
            }
        }

        // Indexed by StatType.
        private static readonly StatInfo[] Info =
        {
            new("Movement Speed", DisplayKind.Multiplier),
            new("Max Health", DisplayKind.Flat),
            new("Damage", DisplayKind.Multiplier),
            new("Critical Chance", DisplayKind.Percent),
            new("Critical Damage", DisplayKind.Percent),
            new("Cast Speed", DisplayKind.Multiplier),
            new("Area", DisplayKind.Multiplier),
            new("Duration", DisplayKind.Multiplier),
            new("Projectiles", DisplayKind.Flat),
            new("Pierce", DisplayKind.Flat),
            new("Pickup Radius", DisplayKind.Multiplier),
            new("Armor", DisplayKind.Flat),
            new("Regeneration", DisplayKind.Flat, " HP/s"),
            new("Experience Gain", DisplayKind.Multiplier),
            new("Luck", DisplayKind.Percent),
            new("Heal on Kill", DisplayKind.Flat, " HP"),
        };

        [SerializeField] private Health health;
        [SerializeField, Range(0f, 1f)] private float baseCritChance = 0.05f;
        [SerializeField, Min(1f)] private float baseCritMultiplier = 1.5f;

        // Upgrade totals, indexed by StatType. Fractions for percentage stats (0.1 = +10%), flat values otherwise.
        private readonly float[] _bonuses = new float[Info.Length];
        private Coroutine _regeneration;

        public float MoveSpeedMultiplier => 1f + Bonus(StatType.MoveSpeed);
        public float DamageMultiplier => 1f + Bonus(StatType.Damage);
        public float CritChance => Mathf.Clamp01(baseCritChance + Bonus(StatType.CritChance));
        public float CritMultiplier => baseCritMultiplier + Bonus(StatType.CritDamage);
        /// <summary>Divides spell cooldowns and the auto attack interval.</summary>
        public float CastSpeedMultiplier => 1f + Bonus(StatType.CastSpeed);
        public float AreaMultiplier => 1f + Bonus(StatType.Area);
        public float DurationMultiplier => 1f + Bonus(StatType.Duration);
        public int ExtraProjectiles => Mathf.RoundToInt(Bonus(StatType.Multishot));
        public int ExtraPierce => Mathf.RoundToInt(Bonus(StatType.Pierce));
        public float PickupRadiusMultiplier => 1f + Bonus(StatType.PickupRadius);
        public float ExperienceMultiplier => 1f + Bonus(StatType.ExperienceGain);
        /// <summary>0..1+, raises the odds of rare and epic cards.</summary>
        public float Luck => Bonus(StatType.Luck);

        public event Action<StatType> Changed;

        private void OnEnable() => EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);

        private void OnDisable()
        {
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            _regeneration = null;
        }

        public DamageProfile CreateDamage(float baseDamage) =>
            new(baseDamage * DamageMultiplier, CritChance, CritMultiplier);

        public SpellModifiers CreateSpellModifiers() =>
            new(DamageMultiplier, CritChance, CritMultiplier, AreaMultiplier, DurationMultiplier, ExtraProjectiles, ExtraPierce);

        public void Apply(StatType stat, float amount)
        {
            _bonuses[(int)stat] += amount;

            switch (stat)
            {
                case StatType.MaxHealth:
                    health.IncreaseMax(amount);
                    break;
                case StatType.Armor:
                    health.Armor = Bonus(StatType.Armor);
                    break;
                case StatType.Regeneration:
                    _regeneration ??= StartCoroutine(Regenerate());
                    break;
            }

            Changed?.Invoke(stat);
        }

        /// <summary>e.g. "Critical Chance\n5% → 10%".</summary>
        public string DescribeUpgrade(StatType stat, float amount)
        {
            var info = Info[(int)stat];
            var current = GetDisplayValue(stat);
            var next = info.Kind == DisplayKind.Flat ? current + amount : current + amount * 100f;
            if (stat == StatType.CritChance)
                next = Mathf.Min(100f, next);

            return $"{info.Name}\n{Format(info, current)}  →  {Format(info, next)}";
        }

        /// <summary>Overview shown on the level-up screen.</summary>
        public string BuildSummary() => string.Format(CultureInfo.InvariantCulture,
            "Damage {0:0}%   ·   Crit {1:0}% ({2:0}% dmg)   ·   Cast Speed {3:0}%   ·   Area {4:0}%   ·   Move {5:0}%   ·   Armor {6:0}   ·   HP {7:0}",
            DamageMultiplier * 100f, CritChance * 100f, CritMultiplier * 100f, CastSpeedMultiplier * 100f,
            AreaMultiplier * 100f, MoveSpeedMultiplier * 100f, Bonus(StatType.Armor), health.Max);

        public static string GetStatName(StatType stat) => Info[(int)stat].Name;

        private float Bonus(StatType stat) => _bonuses[(int)stat];

        private float GetDisplayValue(StatType stat)
        {
            var info = Info[(int)stat];
            return stat switch
            {
                StatType.MaxHealth => health.Max,
                StatType.CritChance => CritChance * 100f,
                StatType.CritDamage => CritMultiplier * 100f,
                _ => info.Kind switch
                {
                    DisplayKind.Multiplier => (1f + Bonus(stat)) * 100f,
                    DisplayKind.Percent => Bonus(stat) * 100f,
                    _ => Bonus(stat)
                }
            };
        }

        // Invariant culture so numbers read the same in every locale (e.g. "0.5", not "0,5").
        private static string Format(in StatInfo info, float value) => info.Kind switch
        {
            DisplayKind.Flat => value.ToString("0.#", CultureInfo.InvariantCulture) + info.Suffix,
            _ => value.ToString("0", CultureInfo.InvariantCulture) + "%"
        };

        private void OnEnemyKilled(EnemyKilledEvent _)
        {
            var heal = Bonus(StatType.LifeOnKill);
            if (heal > 0f)
                health.Heal(heal);
        }

        private IEnumerator Regenerate()
        {
            var wait = new WaitForSeconds(RegenerationTick);
            while (true)
            {
                yield return wait;
                health.Heal(Bonus(StatType.Regeneration) * RegenerationTick);
            }
        }
    }
}
