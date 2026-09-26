using System;

namespace BattleArena.Spells
{
    /// <summary>Ways a single equipped spell can be improved by mission rewards.</summary>
    public enum SpellUpgradeKind
    {
        ExtraCharge,
        CooldownReduction,
        Damage,
        ExtraProjectiles,
        Duration,
        Area,
        ExtraBounces
    }

    /// <summary>Upgrades collected for one spell slot: rank per kind and the summed amount.</summary>
    public sealed class SpellUpgradeState
    {
        private static readonly int KindCount = Enum.GetValues(typeof(SpellUpgradeKind)).Length;

        private readonly int[] _ranks = new int[KindCount];
        private readonly float[] _totals = new float[KindCount];

        public int Rank(SpellUpgradeKind kind) => _ranks[(int)kind];

        public float Total(SpellUpgradeKind kind) => _totals[(int)kind];

        public int TotalInt(SpellUpgradeKind kind) => (int)Math.Round(_totals[(int)kind]);

        public void Add(SpellUpgradeKind kind, float amount)
        {
            _ranks[(int)kind]++;
            _totals[(int)kind] += amount;
        }

        public void Clear()
        {
            Array.Clear(_ranks, 0, _ranks.Length);
            Array.Clear(_totals, 0, _totals.Length);
        }
    }
}
