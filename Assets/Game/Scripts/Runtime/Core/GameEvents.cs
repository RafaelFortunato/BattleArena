using BattleArena.Combat;
using BattleArena.Enemies;
using BattleArena.Interaction;
using BattleArena.Spells;
using UnityEngine;

namespace BattleArena.Core
{
    /// <summary>The game moved from one <see cref="GameState"/> to another.</summary>
    public readonly struct GameStateChangedEvent : IGameEvent
    {
        /// <summary>The state the game left.</summary>
        public readonly GameState Previous;

        /// <summary>The state the game is now in.</summary>
        public readonly GameState Current;

        /// <summary>Creates the event.</summary>
        public GameStateChangedEvent(GameState previous, GameState current)
        {
            Previous = previous;
            Current = current;
        }
    }

    /// <summary>Periodic match clock update for the HUD timer and timeline.</summary>
    public readonly struct MatchTimeChangedEvent : IGameEvent
    {
        /// <summary>Seconds since the match started.</summary>
        public readonly float Elapsed;

        /// <summary>Seconds until the boss arrives.</summary>
        public readonly float Remaining;

        /// <summary>Creates the event.</summary>
        public MatchTimeChangedEvent(float elapsed, float remaining)
        {
            Elapsed = elapsed;
            Remaining = remaining;
        }
    }

    /// <summary>The boss entered the arena; the HUD shows its name and health bar.</summary>
    public readonly struct BossSpawnedEvent : IGameEvent
    {
        /// <summary>The boss's name for the banner.</summary>
        public readonly string DisplayName;

        /// <summary>The boss's health, followed by its health bar.</summary>
        public readonly Health Health;

        /// <summary>Creates the event.</summary>
        public BossSpawnedEvent(string displayName, Health health)
        {
            DisplayName = displayName;
            Health = health;
        }
    }

    /// <summary>An enemy died; missions count kills and audio plays the death sound.</summary>
    public readonly struct EnemyKilledEvent : IGameEvent
    {
        /// <summary>What kind of enemy it was.</summary>
        public readonly EnemyDefinition Definition;

        /// <summary>Where it died.</summary>
        public readonly Vector3 Position;

        /// <summary>Creates the event.</summary>
        public EnemyKilledEvent(EnemyDefinition definition, Vector3 position)
        {
            Definition = definition;
            Position = position;
        }
    }

    /// <summary>An enemy took damage; used for floating damage numbers.</summary>
    public readonly struct EnemyDamagedEvent : IGameEvent
    {
        /// <summary>Where the enemy was hit.</summary>
        public readonly Vector3 Position;

        /// <summary>Damage dealt after armor.</summary>
        public readonly float Amount;

        /// <summary>True for a critical hit.</summary>
        public readonly bool IsCritical;

        /// <summary>Creates the event.</summary>
        public EnemyDamagedEvent(Vector3 position, float amount, bool isCritical)
        {
            Position = position;
            Amount = amount;
            IsCritical = isCritical;
        }
    }

    /// <summary>The player's experience or level changed; drives the XP bar and the level badge.</summary>
    public readonly struct ExperienceChangedEvent : IGameEvent
    {
        /// <summary>Current player level, starting at 1.</summary>
        public readonly int Level;

        /// <summary>Experience gathered towards the next level.</summary>
        public readonly int Current;

        /// <summary>Experience needed for the next level.</summary>
        public readonly int Required;

        /// <summary>The level cap.</summary>
        public readonly int MaxLevel;

        /// <summary>Creates the event.</summary>
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

    /// <summary>The player reached a new level.</summary>
    public readonly struct PlayerLevelUpEvent : IGameEvent
    {
        /// <summary>The level just reached.</summary>
        public readonly int Level;

        /// <summary>Creates the event.</summary>
        public PlayerLevelUpEvent(int level) => Level = level;
    }

    /// <summary>A spell was put into one of the player's spell slots.</summary>
    public readonly struct SpellEquippedEvent : IGameEvent
    {
        /// <summary>The slot that received the spell.</summary>
        public readonly int SlotIndex;

        /// <summary>The equipped spell.</summary>
        public readonly SpellDefinition Spell;

        /// <summary>Creates the event.</summary>
        public SpellEquippedEvent(int slotIndex, SpellDefinition spell)
        {
            SlotIndex = slotIndex;
            Spell = spell;
        }
    }

    /// <summary>An equipped spell gained an upgrade rank.</summary>
    public readonly struct SpellUpgradedEvent : IGameEvent
    {
        /// <summary>The slot of the upgraded spell.</summary>
        public readonly int SlotIndex;

        /// <summary>What was upgraded.</summary>
        public readonly SpellUpgradeKind Kind;

        /// <summary>Creates the event.</summary>
        public SpellUpgradedEvent(int slotIndex, SpellUpgradeKind kind)
        {
            SlotIndex = slotIndex;
            Kind = kind;
        }
    }

    /// <summary>A spell was cast; the HUD starts its cooldown and the character plays the cast animation.</summary>
    public readonly struct SpellCastEvent : IGameEvent
    {
        /// <summary>The slot of the cast spell.</summary>
        public readonly int SlotIndex;

        /// <summary>Seconds until the spell is ready again.</summary>
        public readonly float Cooldown;

        /// <summary>Creates the event.</summary>
        public SpellCastEvent(int slotIndex, float cooldown)
        {
            SlotIndex = slotIndex;
            Cooldown = cooldown;
        }
    }

    /// <summary>The interactable the player can use changed (a well or chest came into or left range).</summary>
    public readonly struct InteractionTargetChangedEvent : IGameEvent
    {
        /// <summary>The interactable in range, or null when there is none.</summary>
        public readonly IInteractable Target;

        /// <summary>Creates the event.</summary>
        public InteractionTargetChangedEvent(IInteractable target) => Target = target;
    }

    /// <summary>A mission began; the HUD announces it.</summary>
    public readonly struct MissionStartedEvent : IGameEvent
    {
        /// <summary>The mission's name.</summary>
        public readonly string Title;

        /// <summary>What the player has to do, with any numbers filled in.</summary>
        public readonly string Description;

        /// <summary>Creates the event.</summary>
        public MissionStartedEvent(string title, string description)
        {
            Title = title;
            Description = description;
        }
    }

    /// <summary>Periodic progress of the running mission for the HUD panel.</summary>
    public readonly struct MissionProgressEvent : IGameEvent
    {
        /// <summary>Completion, 0..1.</summary>
        public readonly float Progress;

        /// <summary>Progress line for the panel, e.g. "Enemies defeated 3 / 20".</summary>
        public readonly string Status;

        /// <summary>Seconds left before the mission fails.</summary>
        public readonly float TimeRemaining;

        /// <summary>Creates the event.</summary>
        public MissionProgressEvent(float progress, string status, float timeRemaining)
        {
            Progress = progress;
            Status = status;
            TimeRemaining = timeRemaining;
        }
    }

    /// <summary>The running mission ended, either way.</summary>
    public readonly struct MissionEndedEvent : IGameEvent
    {
        /// <summary>True when the mission was completed.</summary>
        public readonly bool Succeeded;

        /// <summary>Creates the event.</summary>
        public MissionEndedEvent(bool succeeded) => Succeeded = succeeded;
    }

    /// <summary>A short message for the HUD's notification line.</summary>
    public readonly struct NotificationEvent : IGameEvent
    {
        /// <summary>The text to show.</summary>
        public readonly string Message;

        /// <summary>Creates the event.</summary>
        public NotificationEvent(string message) => Message = message;
    }
}
