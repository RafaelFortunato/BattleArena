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

        public int SlotCount => slotUnlockLevels.Length;

        Transform ISpellCaster.Transform => transform;
        Health ISpellCaster.Health => health;
        Vector3 ISpellCaster.Facing => controller.Facing;
        void ISpellCaster.Teleport(Vector3 position) => controller.Teleport(position);

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

        private void OnEnable() => input.SpellRequested += TryCast;

        private void OnDisable() => input.SpellRequested -= TryCast;

        // ---------------------------------------------------------------- Queries

        public SpellDefinition GetSpell(int slot) => _spells[slot];

        public SpellUpgradeState GetUpgrades(int slot) => _upgrades[slot];

        public bool IsUnlocked(int slot) => _spells[slot] != null;

        public int GetUnlockLevel(int slot) => slotUnlockLevels[slot];

        public int GetMaxCharges(int slot) => 1 + _upgrades[slot].TotalInt(SpellUpgradeKind.ExtraCharge);

        public int GetCharges(int slot)
        {
            RefreshCharges(slot, Time.time);
            return _charges[slot];
        }

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

        /// <summary>Length of the current recharge, after cast speed and upgrades.</summary>
        public float GetCooldownDuration(int slot) => _rechargeDuration[slot];

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

        public void Equip(int slot, SpellDefinition spell)
        {
            _spells[slot] = spell;
            _upgrades[slot].Clear();
            _charges[slot] = GetMaxCharges(slot);
            _recharging[slot] = false;
            EventBus<SpellEquippedEvent>.Raise(new SpellEquippedEvent(slot, spell));
        }

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

        private float GetCooldown(int slot)
        {
            var reduction = Mathf.Min(maxCooldownReduction, _upgrades[slot].Total(SpellUpgradeKind.CooldownReduction));
            return _spells[slot].Cooldown * (1f - reduction) / stats.CastSpeedMultiplier;
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
