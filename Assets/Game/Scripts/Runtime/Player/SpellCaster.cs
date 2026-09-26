using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Spells;
using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>
    /// Owns the Q/E/R spell slots. Slots start empty and are filled by level-up choices.
    /// Cooldowns are stored as timestamps, so nothing runs per frame.
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

        private SpellDefinition[] _spells;
        private float[] _readyTimes;
        private float[] _cooldownDurations;

        public int SlotCount => slotUnlockLevels.Length;

        Transform ISpellCaster.Transform => transform;
        Health ISpellCaster.Health => health;
        Vector3 ISpellCaster.Facing => controller.Facing;
        void ISpellCaster.Teleport(Vector3 position) => controller.Teleport(position);

        private void Awake()
        {
            _spells = new SpellDefinition[SlotCount];
            _readyTimes = new float[SlotCount];
            _cooldownDurations = new float[SlotCount];
        }

        private void OnEnable() => input.SpellRequested += TryCast;

        private void OnDisable() => input.SpellRequested -= TryCast;

        public SpellDefinition GetSpell(int slot) => _spells[slot];

        public bool IsUnlocked(int slot) => _spells[slot] != null;

        public int GetUnlockLevel(int slot) => slotUnlockLevels[slot];

        public float GetCooldownRemaining(int slot) => Mathf.Max(0f, _readyTimes[slot] - Time.time);

        /// <summary>Length of the cooldown started by the last cast, after cast speed.</summary>
        public float GetCooldownDuration(int slot) => _cooldownDurations[slot];

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

        public void Equip(int slot, SpellDefinition spell)
        {
            _spells[slot] = spell;
            _readyTimes[slot] = 0f;
            EventBus<SpellEquippedEvent>.Raise(new SpellEquippedEvent(slot, spell));
        }

        public void TryCast(int slot)
        {
            if (slot < 0 || slot >= _spells.Length || _spells[slot] == null || Time.time < _readyTimes[slot])
                return;

            var spell = _spells[slot];
            var context = new SpellContext(this, castOrigin.position, GameLayers.EnemyMask, stats.CreateSpellModifiers());

            if (!spell.Cast(context))
                return;

            var cooldown = spell.Cooldown / stats.CastSpeedMultiplier;
            _cooldownDurations[slot] = cooldown;
            _readyTimes[slot] = Time.time + cooldown;
            EventBus<SpellCastEvent>.Raise(new SpellCastEvent(slot, cooldown));
        }
    }
}
