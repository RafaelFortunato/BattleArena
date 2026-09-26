using BattleArena.Core;
using BattleArena.Spells;
using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>
    /// Owns the Q/E/R spell slots: unlocks them on level-up and casts them on input.
    /// Cooldowns are stored as timestamps, so nothing runs per frame.
    /// </summary>
    public sealed class SpellCaster : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerController controller;
        [SerializeField] private Transform castOrigin;
        [SerializeField] private SpellDefinition[] spells = new SpellDefinition[InputReader.SpellSlotCount];

        private float[] _readyTimes;
        private bool[] _unlocked;

        public int SlotCount => spells.Length;

        private void Awake()
        {
            _readyTimes = new float[spells.Length];
            _unlocked = new bool[spells.Length];
        }

        private void OnEnable()
        {
            input.SpellRequested += TryCast;
            EventBus<PlayerLevelUpEvent>.Subscribe(OnLevelUp);
        }

        private void OnDisable()
        {
            input.SpellRequested -= TryCast;
            EventBus<PlayerLevelUpEvent>.Unsubscribe(OnLevelUp);
        }

        public SpellDefinition GetSpell(int slot) => spells[slot];

        public bool IsUnlocked(int slot) => _unlocked[slot];

        public float GetCooldownRemaining(int slot) => Mathf.Max(0f, _readyTimes[slot] - Time.time);

        public void TryCast(int slot)
        {
            if (slot < 0 || slot >= spells.Length || !_unlocked[slot] || Time.time < _readyTimes[slot])
                return;

            var spell = spells[slot];
            var context = new SpellContext(transform, castOrigin.position, controller.Facing,
                progression.DamageMultiplier, GameLayers.EnemyMask);

            if (!spell.Cast(context))
                return;

            _readyTimes[slot] = Time.time + spell.Cooldown;
            EventBus<SpellCastEvent>.Raise(new SpellCastEvent(slot, spell.Cooldown));
        }

        private void OnLevelUp(PlayerLevelUpEvent evt)
        {
            for (var i = 0; i < spells.Length; i++)
            {
                if (_unlocked[i] || spells[i] == null || evt.Level < spells[i].UnlockLevel)
                    continue;

                _unlocked[i] = true;
                EventBus<SpellUnlockedEvent>.Raise(new SpellUnlockedEvent(i, spells[i]));
            }
        }
    }
}
