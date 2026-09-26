using BattleArena.Combat;
using BattleArena.Enemies;
using BattleArena.Interaction;
using BattleArena.Spells;
using UnityEngine;

namespace BattleArena.Core
{
    public readonly struct GameStateChangedEvent : IGameEvent
    {
        public readonly GameState Previous;
        public readonly GameState Current;

        public GameStateChangedEvent(GameState previous, GameState current)
        {
            Previous = previous;
            Current = current;
        }
    }

    public readonly struct MatchTimeChangedEvent : IGameEvent
    {
        public readonly float Elapsed;
        public readonly float Remaining;

        public MatchTimeChangedEvent(float elapsed, float remaining)
        {
            Elapsed = elapsed;
            Remaining = remaining;
        }
    }

    public readonly struct BossSpawnedEvent : IGameEvent
    {
        public readonly string DisplayName;
        public readonly Health Health;

        public BossSpawnedEvent(string displayName, Health health)
        {
            DisplayName = displayName;
            Health = health;
        }
    }

    public readonly struct EnemyKilledEvent : IGameEvent
    {
        public readonly EnemyDefinition Definition;
        public readonly Vector3 Position;

        public EnemyKilledEvent(EnemyDefinition definition, Vector3 position)
        {
            Definition = definition;
            Position = position;
        }
    }

    public readonly struct ExperienceChangedEvent : IGameEvent
    {
        public readonly int Level;
        public readonly int Current;
        public readonly int Required;

        public ExperienceChangedEvent(int level, int current, int required)
        {
            Level = level;
            Current = current;
            Required = required;
        }
    }

    public readonly struct PlayerLevelUpEvent : IGameEvent
    {
        public readonly int Level;

        public PlayerLevelUpEvent(int level) => Level = level;
    }

    public readonly struct SpellUnlockedEvent : IGameEvent
    {
        public readonly int SlotIndex;
        public readonly SpellDefinition Spell;

        public SpellUnlockedEvent(int slotIndex, SpellDefinition spell)
        {
            SlotIndex = slotIndex;
            Spell = spell;
        }
    }

    public readonly struct SpellCastEvent : IGameEvent
    {
        public readonly int SlotIndex;
        public readonly float Cooldown;

        public SpellCastEvent(int slotIndex, float cooldown)
        {
            SlotIndex = slotIndex;
            Cooldown = cooldown;
        }
    }

    public readonly struct InteractionTargetChangedEvent : IGameEvent
    {
        /// <summary>The interactable in range, or null when there is none.</summary>
        public readonly IInteractable Target;

        public InteractionTargetChangedEvent(IInteractable target) => Target = target;
    }

    public readonly struct NotificationEvent : IGameEvent
    {
        public readonly string Message;

        public NotificationEvent(string message) => Message = message;
    }
}
