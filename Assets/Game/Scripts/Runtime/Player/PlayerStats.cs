using System;
using System.Collections;
using System.Globalization;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Progression;
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

        /// <summary>How a stat reads on screen.</summary>
        private enum DisplayKind
        {
            /// <summary>Upgrade total as a signed percentage: "+8%".</summary>
            Bonus,
            /// <summary>Absolute percentage: "15%".</summary>
            Percent,
            /// <summary>Signed flat upgrade total: "+1", "+0.5/s".</summary>
            Count,
            /// <summary>Absolute number: "240".</summary>
            Absolute
        }

        private readonly struct StatInfo
        {
            public readonly string Name;
            public readonly DisplayKind Kind;
            public readonly string Suffix;
            /// <summary>Display value that fills a card's comparison bar.</summary>
            public readonly float BarMax;
            /// <summary>Whole-number stats are drawn as this many segments instead of a bar.</summary>
            public readonly int Segments;

            public StatInfo(string name, DisplayKind kind, float barMax, string suffix = "", int segments = 0)
            {
                Name = name;
                Kind = kind;
                BarMax = barMax;
                Suffix = suffix;
                Segments = segments;
            }
        }

        // Indexed by StatType.
        private static readonly StatInfo[] Info =
        {
            new("Move speed", DisplayKind.Bonus, 60f),
            new("Max health", DisplayKind.Absolute, 400f),
            new("Damage", DisplayKind.Bonus, 100f),
            new("Critical chance", DisplayKind.Percent, 50f),
            new("Critical damage", DisplayKind.Percent, 300f),
            new("Cast speed", DisplayKind.Bonus, 60f),
            new("Area", DisplayKind.Bonus, 60f),
            new("Duration", DisplayKind.Bonus, 60f),
            new("Projectiles", DisplayKind.Count, 3f, segments: 3),
            new("Pierce", DisplayKind.Count, 3f, segments: 3),
            new("Pickup radius", DisplayKind.Bonus, 100f),
            new("Armor", DisplayKind.Count, 8f),
            new("Regeneration", DisplayKind.Count, 4f, "/s"),
            new("Experience gain", DisplayKind.Bonus, 50f),
            new("Luck", DisplayKind.Bonus, 60f),
            new("Heal on kill", DisplayKind.Count, 4f, " HP"),
        };

        public static int StatCount => Info.Length;

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

        /// <summary>Card preview for adding <paramref name="amount"/> to <paramref name="stat"/>: "5% → 15%" plus bar fills.</summary>
        public StatPreview Preview(StatType stat, float amount)
        {
            var info = Info[(int)stat];
            var current = GetDisplayValue(stat);
            var delta = info.Kind is DisplayKind.Bonus or DisplayKind.Percent ? amount * 100f : amount;
            var next = current + delta;
            if (stat == StatType.CritChance)
                next = Mathf.Min(100f, next);

            var gain = FormatSigned(delta, info.Kind is DisplayKind.Bonus or DisplayKind.Percent ? "%" : info.Suffix);
            var bar = info.Segments > 0 ? StatBarStyle.Segmented : StatBarStyle.Continuous;
            return new StatPreview(info.Name, Format(info, next), Format(info, current), gain, bar,
                Mathf.Clamp01(current / info.BarMax), Mathf.Clamp01(next / info.BarMax), info.Segments);
        }

        public static string GetStatName(StatType stat) => Info[(int)stat].Name;

        /// <summary>Current value as shown on screen, e.g. "+8%", "15%", "240".</summary>
        public string FormatCurrent(StatType stat) => Format(Info[(int)stat], GetDisplayValue(stat));

        private float Bonus(StatType stat) => _bonuses[(int)stat];

        private float GetDisplayValue(StatType stat) => stat switch
        {
            StatType.MaxHealth => health.Max,
            StatType.CritChance => CritChance * 100f,
            StatType.CritDamage => CritMultiplier * 100f,
            _ => Info[(int)stat].Kind == DisplayKind.Bonus ? Bonus(stat) * 100f : Bonus(stat)
        };

        // Invariant culture so numbers read the same in every locale (e.g. "0.5", not "0,5").
        private static string Format(in StatInfo info, float value) => info.Kind switch
        {
            DisplayKind.Bonus => FormatSigned(value, "%"),
            DisplayKind.Percent => value.ToString("0", CultureInfo.InvariantCulture) + "%",
            DisplayKind.Count => FormatSigned(value, info.Suffix),
            _ => value.ToString("0", CultureInfo.InvariantCulture) + info.Suffix
        };

        private static string FormatSigned(float value, string suffix) =>
            (value < 0f ? "-" : "+") + Mathf.Abs(value).ToString("0.#", CultureInfo.InvariantCulture) + suffix;

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
