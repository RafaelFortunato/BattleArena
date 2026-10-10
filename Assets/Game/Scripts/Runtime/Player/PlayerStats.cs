using System;
using System.Collections;
using System.Globalization;
using BattleArena.Combat;
using BattleArena.Progression;
using BattleArena.Spells;
using UnityEngine;

namespace BattleArena.Player
{
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

        /// <summary>How one stat is named, shown and capped.</summary>
        private readonly struct StatInfo
        {
            /// <summary>Name shown on cards and in the build summary.</summary>
            public readonly string Name;
            /// <summary>How the value reads on screen.</summary>
            public readonly DisplayKind Kind;
            /// <summary>Unit after the number, e.g. "/s".</summary>
            public readonly string Suffix;
            /// <summary>
            /// Highest value the stat can reach, in display units ("+150%" is 150, "500 HP" is 500). Upgrades stop
            /// at it, cards for it stop being offered, and it is what fills a card's comparison bar.
            /// </summary>
            public readonly float Cap;
            /// <summary>Whole-number stats are drawn as this many segments instead of a bar.</summary>
            public readonly int Segments;

            /// <summary>Describes a stat.</summary>
            public StatInfo(string name, DisplayKind kind, float cap, string suffix = "", int segments = 0)
            {
                Name = name;
                Kind = kind;
                Cap = cap;
                Suffix = suffix;
                Segments = segments;
            }
        }

        // Indexed by StatType. The third value is the stat's cap (see StatInfo.Cap).
        private static readonly StatInfo[] Info =
        {
            new("Move speed", DisplayKind.Bonus, 100f),
            new("Max health", DisplayKind.Absolute, 500f),
            new("Attack", DisplayKind.Absolute, 50f),
            new("Critical chance", DisplayKind.Percent, 100f),
            new("Critical damage", DisplayKind.Percent, 500f),
            new("Attack speed", DisplayKind.Bonus, 300f),
            new("Area", DisplayKind.Bonus, 300f),
            new("Duration", DisplayKind.Bonus, 300f),
            // Matches the auto attack's 11-bolt fan (1 + 10).
            new("Projectiles", DisplayKind.Count, 10f, segments: 10),
            new("Pierce", DisplayKind.Count, 10f, segments: 10),
            new("Pickup radius", DisplayKind.Bonus, 500f),
            new("Armor", DisplayKind.Count, 200f),
            new("Regeneration", DisplayKind.Count, 50f, "/s"),
            new("Experience gain", DisplayKind.Bonus, 300f),
            new("Luck", DisplayKind.Bonus, 200f),
            // 75% quarters cooldowns.
            new("Cooldown reduction", DisplayKind.Percent, 75f),
        };

        /// <summary>Number of stats.</summary>
        public static int StatCount => Info.Length;

        [SerializeField] private Health health;
        [Tooltip("Attack before upgrades. Basic attacks deal exactly this; spells deal a percentage of it.")]
        [SerializeField, Min(0f)] private float baseAttack = 10f;
        [SerializeField, Range(0f, 1f)] private float baseCritChance = 0.05f;
        [SerializeField, Min(1f)] private float baseCritMultiplier = 1.5f;

        // Upgrade totals, indexed by StatType. Fractions for percentage stats (0.1 = +10%), flat values otherwise.
        private readonly float[] _bonuses = new float[Info.Length];
        private Coroutine _regeneration;

        /// <summary>Movement speed multiplier.</summary>
        public float MoveSpeedMultiplier => 1f + Bonus(StatType.MoveSpeed);
        /// <summary>Flat damage number everything scales from: basic attacks deal 100%, spells a % of it.</summary>
        public float Attack => baseAttack + Bonus(StatType.Attack);
        /// <summary>Chance for a hit to be critical, 0..1.</summary>
        public float CritChance => Mathf.Clamp01(baseCritChance + Bonus(StatType.CritChance));
        /// <summary>Damage multiplier of critical hits.</summary>
        public float CritMultiplier => baseCritMultiplier + Bonus(StatType.CritDamage);
        /// <summary>Divides the basic attack interval.</summary>
        public float AttackSpeedMultiplier => 1f + Bonus(StatType.AttackSpeed);
        /// <summary>Fraction (0 to 0.75) taken off every spell cooldown.</summary>
        public float CooldownReduction => Bonus(StatType.CooldownReduction);
        /// <summary>Spell area multiplier.</summary>
        public float AreaMultiplier => 1f + Bonus(StatType.Area);
        /// <summary>Spell duration multiplier.</summary>
        public float DurationMultiplier => 1f + Bonus(StatType.Duration);
        /// <summary>Extra projectiles per shot.</summary>
        public int ExtraProjectiles => Mathf.RoundToInt(Bonus(StatType.Multishot));
        /// <summary>Extra targets each projectile passes through.</summary>
        public int ExtraPierce => Mathf.RoundToInt(Bonus(StatType.Pierce));
        /// <summary>Pickup radius multiplier.</summary>
        public float PickupRadiusMultiplier => 1f + Bonus(StatType.PickupRadius);
        /// <summary>Experience gain multiplier.</summary>
        public float ExperienceMultiplier => 1f + Bonus(StatType.ExperienceGain);
        /// <summary>Armor from upgrades.</summary>
        public float Armor => Bonus(StatType.Armor);
        /// <summary>0..1+, raises the odds of rare and epic cards.</summary>
        public float Luck => Bonus(StatType.Luck);

        /// <summary>Raised with the stat after every upgrade.</summary>
        public event Action<StatType> Changed;

        /// <summary>Forgets the regeneration routine, which stops with the component.</summary>
        private void OnDisable() => _regeneration = null;

        /// <summary>Damage worth <paramref name="attackRatio"/> of Attack (1 = 100%), with the player's crit.</summary>
        public DamageProfile CreateDamage(float attackRatio) =>
            new(Attack * attackRatio, CritChance, CritMultiplier);

        /// <summary>Snapshot of the stats that shape spells, for one cast.</summary>
        public SpellModifiers CreateSpellModifiers() =>
            new(Attack, CritChance, CritMultiplier, AreaMultiplier, DurationMultiplier, ExtraProjectiles, ExtraPierce);

        /// <summary>True when <paramref name="stat"/> has reached its cap, so its cards are no longer offered.</summary>
        public bool IsCapped(StatType stat) => GetDisplayValue(stat) >= Info[(int)stat].Cap - 0.001f;

        /// <summary>Adds an upgrade, stopping at the stat's cap.</summary>
        public void Apply(StatType stat, float amount)
        {
            var info = Info[(int)stat];
            amount = Mathf.Min(amount, Mathf.Max(0f, (info.Cap - GetDisplayValue(stat)) / DisplayScale(info)));
            if (amount <= 0f)
                return;

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

        /// <summary>
        /// Card preview for adding <paramref name="amount"/> to <paramref name="stat"/>: "5% → 15%", with the bar
        /// filling at the stat's cap. A card that would pass the cap shows only what it can still add.
        /// </summary>
        public StatPreview Preview(StatType stat, float amount)
        {
            if (stat == StatType.Armor)
                return ArmorPreview(amount);

            var info = Info[(int)stat];
            var current = GetDisplayValue(stat);
            var next = Mathf.Min(info.Cap, current + amount * DisplayScale(info));

            var gain = FormatSigned(next - current, info.Kind is DisplayKind.Bonus or DisplayKind.Percent ? "%" : info.Suffix);
            var bar = info.Segments > 0 ? StatBarStyle.Segmented : StatBarStyle.Continuous;
            return new StatPreview(info.Name, Format(info, next), Format(info, current), gain, bar,
                Mathf.Clamp01(current / info.Cap), Mathf.Clamp01(next / info.Cap), info.Segments);
        }

        /// <summary>
        /// Armor reads as the damage reduction it gives, since that is what the player cares about and armor has
        /// diminishing returns: "Damage reduction +20%", filling the bar at the reduction the armor cap gives.
        /// </summary>
        private StatPreview ArmorPreview(float amount)
        {
            var cap = Info[(int)StatType.Armor].Cap;
            var now = Mathf.RoundToInt(DamageReduction(Armor));
            var after = Mathf.RoundToInt(DamageReduction(Mathf.Min(cap, Armor + amount)));
            var max = DamageReduction(cap);
            return new StatPreview("Damage reduction", after + "%", now + "%", "+" + (after - now) + "%",
                StatBarStyle.Continuous, now / max, after / max);

            static float DamageReduction(float armor) => (1f - Health.ArmorDamageMultiplier(armor)) * 100f;
        }

        /// <summary>Display units per upgrade unit: percentage stats store fractions (0.1) but read as 10%.</summary>
        private static float DisplayScale(in StatInfo info) => info.Kind is DisplayKind.Bonus or DisplayKind.Percent ? 100f : 1f;

        /// <summary>Name of <paramref name="stat"/> as shown to the player.</summary>
        public static string GetStatName(StatType stat) => Info[(int)stat].Name;

        /// <summary>Highest value <paramref name="stat"/> can reach, in display units.</summary>
        public static float GetCap(StatType stat) => Info[(int)stat].Cap;

        /// <summary>Current value as shown on screen, e.g. "+8%", "15%", "240".</summary>
        public string FormatCurrent(StatType stat) => Format(Info[(int)stat], GetDisplayValue(stat));

        /// <summary>Upgrade total of <paramref name="stat"/>.</summary>
        private float Bonus(StatType stat) => _bonuses[(int)stat];

        /// <summary>Current value of <paramref name="stat"/> in display units.</summary>
        private float GetDisplayValue(StatType stat) => stat switch
        {
            StatType.MaxHealth => health.Max,
            StatType.Attack => Attack,
            StatType.CritChance => CritChance * 100f,
            StatType.CritDamage => CritMultiplier * 100f,
            _ => Info[(int)stat].Kind is DisplayKind.Bonus or DisplayKind.Percent ? Bonus(stat) * 100f : Bonus(stat)
        };

        /// <summary>
        /// Formats a value as the stat reads on screen, in the invariant culture so numbers read the same in every
        /// locale ("0.5", not "0,5").
        /// </summary>
        private static string Format(in StatInfo info, float value) => info.Kind switch
        {
            DisplayKind.Bonus => FormatSigned(value, "%"),
            DisplayKind.Percent => value.ToString("0", CultureInfo.InvariantCulture) + "%",
            DisplayKind.Count => FormatSigned(value, info.Suffix),
            _ => value.ToString("0", CultureInfo.InvariantCulture) + info.Suffix
        };

        /// <summary>Formats a signed change, e.g. "+0.5/s".</summary>
        private static string FormatSigned(float value, string suffix) =>
            (value < 0f ? "-" : "+") + Mathf.Abs(value).ToString("0.#", CultureInfo.InvariantCulture) + suffix;

        /// <summary>Heals the player on a fixed tick by the regeneration rate.</summary>
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
