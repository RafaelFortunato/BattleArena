using System;

namespace BattleArena.Spells
{
    /// <summary>Upgrades collected for one spell slot: rank per kind and the summed amount.</summary>
    public sealed class SpellUpgradeState
    {
        private static readonly int KindCount = Enum.GetValues(typeof(SpellUpgradeKind)).Length;

        private readonly int[] _ranks = new int[KindCount];
        private readonly float[] _totals = new float[KindCount];

        /// <summary>How many upgrades of <paramref name="kind"/> were taken.</summary>
        public int Rank(SpellUpgradeKind kind) => _ranks[(int)kind];

        /// <summary>Summed amount of every <paramref name="kind"/> upgrade taken.</summary>
        public float Total(SpellUpgradeKind kind) => _totals[(int)kind];

        /// <summary><see cref="Total"/> rounded, for count-based upgrades (charges, projectiles, bounces).</summary>
        public int TotalInt(SpellUpgradeKind kind) => (int)Math.Round(_totals[(int)kind]);

        /// <summary>Records one more upgrade of <paramref name="kind"/> worth <paramref name="amount"/>.</summary>
        public void Add(SpellUpgradeKind kind, float amount)
        {
            _ranks[(int)kind]++;
            _totals[(int)kind] += amount;
        }

        /// <summary>Forgets every upgrade, e.g. when a new spell is equipped in the slot.</summary>
        public void Clear()
        {
            Array.Clear(_ranks, 0, _ranks.Length);
            Array.Clear(_totals, 0, _totals.Length);
        }
    }
}
