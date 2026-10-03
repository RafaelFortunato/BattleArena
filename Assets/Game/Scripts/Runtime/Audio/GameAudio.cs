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

        [Header("Match end")]
        [SerializeField] private SoundEvent victory;
        [SerializeField] private SoundEvent defeat;
        [Tooltip("How long the ambience stays pulled down under the victory or defeat sting.")]
        [SerializeField, Min(0f)] private float endStingDuckSeconds = 4f;

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

        private void OnPlayerDamaged(Health health, DamageResult damage)
        {
            if (health.IsAlive)
                AudioManager.PlayUI(playerHurt);
        }

        private void OnPlayerDied(Health health) => AudioManager.PlayUI(playerDeath);

        private void OnEnemyDamaged(EnemyDamagedEvent evt) => AudioManager.PlayAt(enemyHit, evt.Position);

        private void OnEnemyKilled(EnemyKilledEvent evt) => AudioManager.PlayAt(enemyDeath, evt.Position);

        private void OnLevelUp(PlayerLevelUpEvent evt) => AudioManager.PlayUI(levelUp);

        private void OnCardChosen(int index) => AudioManager.PlayUI(upgradePicked);

        private void OnMissionStarted(MissionStartedEvent evt) => AudioManager.PlayUI(missionStarted);

        private void OnMissionEnded(MissionEndedEvent evt) => AudioManager.PlayUI(evt.Succeeded ? missionCompleted : missionFailed);

        private void OnBossSpawned(BossSpawnedEvent evt) => AudioManager.PlayUI(bossAppears);

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

            AudioManager.DuckAmbience(0.3f, endStingDuckSeconds);
            AudioManager.PlayUI(sting);
        }
    }
}
