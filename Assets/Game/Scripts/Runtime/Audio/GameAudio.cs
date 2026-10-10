using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Progression;
using UnityEngine;

namespace BattleArena.Audio
{
    /// <summary>
    /// Voices the game's moments that are already announced on the <see cref="EventBus{T}"/> (hits, kills, level-ups,
    /// missions, the boss, the end of the match) plus the player's own health, so none of those systems needs to
    /// know about audio. Sounds that belong to a specific object (a fireball, a chest) live on that object instead.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private LevelUpController levelUpChoices;

        [Header("Combat")]
        [SerializeField] private SoundEvent enemyHit;
        [SerializeField] private SoundEvent enemyDeath;
        [SerializeField] private SoundEvent playerHurt;
        [SerializeField] private SoundEvent playerDeath;

        [Header("Progression")]
        [SerializeField] private SoundEvent levelUp;
        [Tooltip("A level-up card chosen, whether it grants a spell, a spell upgrade or a stat.")]
        [SerializeField] private SoundEvent upgradePicked;

        [Header("Missions and boss")]
        [SerializeField] private SoundEvent missionStarted;
        [SerializeField] private SoundEvent missionCompleted;
        [SerializeField] private SoundEvent missionFailed;
        [SerializeField] private SoundEvent bossAppears;
        [Tooltip("Crossfaded in when the boss appears, replacing the match music until the scene reloads.")]
        [SerializeField] private AudioClip bossMusic;

        [Header("Match end")]
        [SerializeField] private SoundEvent victory;
        [SerializeField] private SoundEvent defeat;
        [Tooltip("How long the music stays pulled down under the victory or defeat sting.")]
        [SerializeField, Min(0f)] private float endStingDuckSeconds = 4f;

        /// <summary>
        /// Subscribes to the player's health, the level-up choices and every game event that has a sound.
        /// </summary>
        private void OnEnable()
        {
            playerHealth.Damaged += OnPlayerDamaged;
            playerHealth.Died += OnPlayerDied;
            levelUpChoices.Chosen += OnCardChosen;
            EventBus<EnemyDamagedEvent>.Subscribe(OnEnemyDamaged);
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
            EventBus<PlayerLevelUpEvent>.Subscribe(OnLevelUp);
            EventBus<MissionStartedEvent>.Subscribe(OnMissionStarted);
            EventBus<MissionEndedEvent>.Subscribe(OnMissionEnded);
            EventBus<BossSpawnedEvent>.Subscribe(OnBossSpawned);
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
        }

        /// <summary>Removes every subscription made in <see cref="OnEnable"/>.</summary>
        private void OnDisable()
        {
            playerHealth.Damaged -= OnPlayerDamaged;
            playerHealth.Died -= OnPlayerDied;
            levelUpChoices.Chosen -= OnCardChosen;
            EventBus<EnemyDamagedEvent>.Unsubscribe(OnEnemyDamaged);
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            EventBus<PlayerLevelUpEvent>.Unsubscribe(OnLevelUp);
            EventBus<MissionStartedEvent>.Unsubscribe(OnMissionStarted);
            EventBus<MissionEndedEvent>.Unsubscribe(OnMissionEnded);
            EventBus<BossSpawnedEvent>.Unsubscribe(OnBossSpawned);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
        }

        /// <summary>Plays the hurt sound for a hit the player survives; the killing blow has its own sound.</summary>
        private void OnPlayerDamaged(Health health, DamageResult damage)
        {
            if (health.IsAlive)
                AudioManager.PlayUI(playerHurt);
        }

        /// <summary>Plays the player's death sound.</summary>
        private void OnPlayerDied(Health health) => AudioManager.PlayUI(playerDeath);

        /// <summary>Plays the hit sound where the enemy was struck.</summary>
        private void OnEnemyDamaged(EnemyDamagedEvent evt) => AudioManager.PlayAt(enemyHit, evt.Position);

        /// <summary>Plays the death sound where the enemy fell.</summary>
        private void OnEnemyKilled(EnemyKilledEvent evt) => AudioManager.PlayAt(enemyDeath, evt.Position);

        /// <summary>Plays the level-up flourish.</summary>
        private void OnLevelUp(PlayerLevelUpEvent evt) => AudioManager.PlayUI(levelUp);

        /// <summary>Confirms a level-up card pick.</summary>
        private void OnCardChosen(int index) => AudioManager.PlayUI(upgradePicked);

        /// <summary>Announces a new mission.</summary>
        private void OnMissionStarted(MissionStartedEvent evt) => AudioManager.PlayUI(missionStarted);

        /// <summary>Plays the success or failure sting for the mission that just ended.</summary>
        private void OnMissionEnded(MissionEndedEvent evt) => AudioManager.PlayUI(evt.Succeeded ? missionCompleted : missionFailed);

        /// <summary>Plays the boss's entrance roar and crossfades to the boss music.</summary>
        private void OnBossSpawned(BossSpawnedEvent evt)
        {
            AudioManager.PlayUI(bossAppears);
            AudioManager.PlayMusic(bossMusic);
        }

        /// <summary>On victory or defeat, ducks the music and plays the matching sting.</summary>
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            var sting = evt.Current switch
            {
                GameState.Victory => victory,
                GameState.Defeat => defeat,
                _ => null
            };
            if (sting == null)
                return;

            AudioManager.DuckMusic(0.3f, endStingDuckSeconds);
            AudioManager.PlayUI(sting);
        }
    }
}
