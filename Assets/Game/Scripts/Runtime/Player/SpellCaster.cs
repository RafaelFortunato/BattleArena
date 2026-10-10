using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Spells;
using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>
    /// Owns the Q/E/R spell slots: equipping (level-up choices), per-slot upgrades (mission rewards),
    /// charges and cooldowns. Charges recharge one at a time and are resolved lazily from timestamps
    /// whenever they're queried, so nothing runs per frame.
    /// </summary>
    public sealed class SpellCaster : MonoBehaviour, ISpellCaster
    {
        [SerializeField] private InputReader input;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerController controller;
        [SerializeField] private Health health;
        [SerializeField] private Transform castOrigin;
        [Tooltip("Player level at which each slot (Q, E, R) offers a spell choice.")]
        [SerializeField] private int[] slotUnlockLevels = { 5, 10, 15 };
        [Tooltip("Cooldown reduction from upgrades can't go beyond this fraction.")]
        [SerializeField, Range(0f, 0.9f)] private float maxCooldownReduction = 0.6f;

        private SpellDefinition[] _spells;
        private SpellUpgradeState[] _upgrades;
        private int[] _charges;
        private bool[] _recharging;
        private float[] _rechargeReadyAt;
        private float[] _rechargeDuration;

        /// <summary>Number of spell slots.</summary>
        public int SlotCount => slotUnlockLevels.Length;

        /// <summary>The player's Attack, which every spell deals a share of.</summary>
        public float Attack => stats.Attack;

        Transform ISpellCaster.Transform => transform;
        Health ISpellCaster.Health => health;
        Vector3 ISpellCaster.Facing => controller.Facing;
        /// <summary>Moves the player instantly (Phase Step).</summary>
        void ISpellCaster.Teleport(Vector3 position) => controller.Teleport(position);

        /// <summary>Allocates the per-slot state.</summary>
        private void Awake()
        {
            var count = SlotCount;
            _spells = new SpellDefinition[count];
            _upgrades = new SpellUpgradeState[count];
            _charges = new int[count];
            _recharging = new bool[count];
            _rechargeReadyAt = new float[count];
            _rechargeDuration = new float[count];
            for (var i = 0; i < count; i++)
                _upgrades[i] = new SpellUpgradeState();
        }

        /// <summary>Listens for spell keys.</summary>
        private void OnEnable() => input.SpellRequested += TryCast;

        /// <summary>Stops listening for spell keys.</summary>
        private void OnDisable() => input.SpellRequested -= TryCast;

        // ---------------------------------------------------------------- Queries

        /// <summary>The spell in <paramref name="slot"/>, or null when empty.</summary>
        public SpellDefinition GetSpell(int slot) => _spells[slot];

        /// <summary>The upgrades applied to the spell in <paramref name="slot"/>.</summary>
        public SpellUpgradeState GetUpgrades(int slot) => _upgrades[slot];

        /// <summary>True when <paramref name="slot"/> holds a spell.</summary>
        public bool IsUnlocked(int slot) => _spells[slot] != null;

        /// <summary>Player level at which <paramref name="slot"/> opens.</summary>
        public int GetUnlockLevel(int slot) => slotUnlockLevels[slot];

        /// <summary>Charges <paramref name="slot"/> can hold, including Extra Charge upgrades.</summary>
        public int GetMaxCharges(int slot) => 1 + _upgrades[slot].TotalInt(SpellUpgradeKind.ExtraCharge);

        /// <summary>Charges ready in <paramref name="slot"/> right now.</summary>
        public int GetCharges(int slot)
        {
            RefreshCharges(slot, Time.time);
            return _charges[slot];
        }

        /// <summary>True while <paramref name="slot"/> is regaining a charge.</summary>
        public bool IsRecharging(int slot)
        {
            RefreshCharges(slot, Time.time);
            return _recharging[slot];
        }

        /// <summary>Seconds until the next charge is ready (zero when full).</summary>
        public float GetCooldownRemaining(int slot)
        {
            RefreshCharges(slot, Time.time);
            return _recharging[slot] ? Mathf.Max(0f, _rechargeReadyAt[slot] - Time.time) : 0f;
        }

        /// <summary>Length of the current recharge, after cooldown reduction and upgrades.</summary>
        public float GetCooldownDuration(int slot) => _rechargeDuration[slot];

        /// <summary>True when <paramref name="spell"/> is equipped in any slot.</summary>
        public bool Owns(SpellDefinition spell)
        {
            foreach (var owned in _spells)
                if (owned == spell)
                    return true;
            return false;
        }

        /// <summary>Finds the empty slot that opens at <paramref name="level"/>.</summary>
        public bool TryGetSlotUnlockedAt(int level, out int slot)
        {
            for (slot = 0; slot < slotUnlockLevels.Length; slot++)
                if (slotUnlockLevels[slot] == level && _spells[slot] == null)
                    return true;
            slot = -1;
            return false;
        }

        // ---------------------------------------------------------------- Changes

        /// <summary>
        /// Puts <paramref name="spell"/> in <paramref name="slot"/> with full charges and no upgrades.
        /// </summary>
        public void Equip(int slot, SpellDefinition spell)
        {
            _spells[slot] = spell;
            _upgrades[slot].Clear();
            _charges[slot] = GetMaxCharges(slot);
            _recharging[slot] = false;
            EventBus<SpellEquippedEvent>.Raise(new SpellEquippedEvent(slot, spell));
        }

        /// <summary>
        /// Adds an upgrade to the spell in <paramref name="slot"/>; an extra charge is granted at once.
        /// </summary>
        public void ApplyUpgrade(int slot, SpellUpgradeKind kind, float amount)
        {
            RefreshCharges(slot, Time.time);
            _upgrades[slot].Add(kind, amount);

            if (kind == SpellUpgradeKind.ExtraCharge)
            {
                // The new charge is granted immediately.
                _charges[slot] = Mathf.Min(_charges[slot] + Mathf.RoundToInt(amount), GetMaxCharges(slot));
                if (_charges[slot] >= GetMaxCharges(slot))
                    _recharging[slot] = false;
            }

            EventBus<SpellUpgradedEvent>.Raise(new SpellUpgradedEvent(slot, kind));
        }

        /// <summary>Casts the spell in <paramref name="slot"/> if it has a charge, and starts recharging.</summary>
        public void TryCast(int slot)
        {
            if (slot < 0 || slot >= _spells.Length || _spells[slot] == null)
                return;

            var now = Time.time;
            RefreshCharges(slot, now);
            if (_charges[slot] <= 0)
                return;

            var spell = _spells[slot];
            var modifiers = stats.CreateSpellModifiers().With(_upgrades[slot]);
            var context = new SpellContext(this, castOrigin.position, GameLayers.EnemyMask, modifiers);
            if (!spell.Cast(context))
                return;

            _charges[slot]--;
            if (!_recharging[slot])
            {
                _recharging[slot] = true;
                _rechargeDuration[slot] = GetCooldown(slot);
                _rechargeReadyAt[slot] = now + _rechargeDuration[slot];
            }

            EventBus<SpellCastEvent>.Raise(new SpellCastEvent(slot, _rechargeDuration[slot]));
        }

        /// <summary>
        /// Recharge time of <paramref name="slot"/> after its own upgrades and the Cooldown reduction stat, which stack
        /// multiplicatively.
        /// </summary>
        private float GetCooldown(int slot)
        {
            var reduction = Mathf.Min(maxCooldownReduction, _upgrades[slot].Total(SpellUpgradeKind.CooldownReduction));
            // The spell's own Quick Recharge upgrades and the Cooldown reduction stat stack multiplicatively.
            return _spells[slot].Cooldown * (1f - reduction) * (1f - stats.CooldownReduction);
        }

        /// <summary>Converts elapsed recharge time into charges, one recharge after another.</summary>
        private void RefreshCharges(int slot, float now)
        {
            while (_recharging[slot] && now >= _rechargeReadyAt[slot])
            {
                _charges[slot]++;
                if (_charges[slot] >= GetMaxCharges(slot))
                {
                    _charges[slot] = GetMaxCharges(slot);
                    _recharging[slot] = false;
                    return;
                }

                _rechargeDuration[slot] = GetCooldown(slot);
                _rechargeReadyAt[slot] += _rechargeDuration[slot];
            }
        }
    }
}
