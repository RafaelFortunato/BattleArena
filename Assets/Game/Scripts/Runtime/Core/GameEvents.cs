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

    /// <summary>An enemy took damage; used for floating damage numbers.</summary>
    public readonly struct EnemyDamagedEvent : IGameEvent
    {
        public readonly Vector3 Position;
        public readonly float Amount;
        public readonly bool IsCritical;

        public EnemyDamagedEvent(Vector3 position, float amount, bool isCritical)
        {
            Position = position;
            Amount = amount;
            IsCritical = isCritical;
        }
    }

    public readonly struct ExperienceChangedEvent : IGameEvent
    {
        public readonly int Level;
        public readonly int Current;
        public readonly int Required;
        public readonly int MaxLevel;

        public ExperienceChangedEvent(int level, int current, int required, int maxLevel)
        {
            Level = level;
            Current = current;
            Required = required;
            MaxLevel = maxLevel;
        }

        /// <summary>
        /// Levels gained so far as a share of the climb to the cap: 0 at level 1, 1 at the cap. Whole levels only, so
        /// it moves on level-ups and not with every gem.
        /// </summary>
        public float ProgressToMaxLevel =>
            MaxLevel <= 1 ? 1f : Mathf.Clamp01((float)(Level - 1) / (MaxLevel - 1));
    }

    /// <summary>A treasure chest was opened; it rewards a choice of upgrade cards.</summary>
    public readonly struct TreasureOpenedEvent : IGameEvent
    {
    }

    public readonly struct PlayerLevelUpEvent : IGameEvent
    {
        public readonly int Level;

        public PlayerLevelUpEvent(int level) => Level = level;
    }

    public readonly struct SpellEquippedEvent : IGameEvent
    {
        public readonly int SlotIndex;
        public readonly SpellDefinition Spell;

        public SpellEquippedEvent(int slotIndex, SpellDefinition spell)
        {
            SlotIndex = slotIndex;
            Spell = spell;
        }
    }

    public readonly struct SpellUpgradedEvent : IGameEvent
    {
        public readonly int SlotIndex;
        public readonly SpellUpgradeKind Kind;

        public SpellUpgradedEvent(int slotIndex, SpellUpgradeKind kind)
        {
            SlotIndex = slotIndex;
            Kind = kind;
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

    public readonly struct MissionStartedEvent : IGameEvent
    {
        public readonly string Title;
        public readonly string Description;

        public MissionStartedEvent(string title, string description)
        {
            Title = title;
            Description = description;
        }
    }

    public readonly struct MissionProgressEvent : IGameEvent
    {
        public readonly float Progress;
        public readonly string Status;
        public readonly float TimeRemaining;

        public MissionProgressEvent(float progress, string status, float timeRemaining)
        {
            Progress = progress;
            Status = status;
            TimeRemaining = timeRemaining;
        }
    }

    public readonly struct MissionEndedEvent : IGameEvent
    {
        public readonly bool Succeeded;

        public MissionEndedEvent(bool succeeded) => Succeeded = succeeded;
    }

    public readonly struct NotificationEvent : IGameEvent
    {
        public readonly string Message;

        public NotificationEvent(string message) => Message = message;
    }
}
