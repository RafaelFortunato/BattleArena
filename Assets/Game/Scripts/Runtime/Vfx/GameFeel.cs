using BattleArena.Combat;
using BattleArena.Core;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>
    /// Screen shake and hit-stop for the match's big moments that are already announced elsewhere: the player being
    /// hit, a heavy enemy going down and the boss arriving. Area strikes (meteors, slams, bombs) shake on their own,
    /// where they land. Ordinary hits do neither: with dozens landing a second, they would turn into constant judder.
    /// </summary>
    public sealed class GameFeel : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;

        [Header("Player hit")]
        [Tooltip("Trauma for any hit on the player, plus this many times the share of max health it took.")]
        [SerializeField] private Vector2 hurtShake = new(0.25f, 2f);
        [Tooltip("A hit taking at least this share of max health also freezes play briefly.")]
        [SerializeField, Range(0f, 1f)] private float heavyHitFraction = 0.1f;
        [SerializeField, Min(0f)] private float heavyHitStop = 0.07f;

        [Header("Kills")]
        [Tooltip("Enemies whose base health is at least this count as heavy (Ogre Warlord, the boss).")]
        [SerializeField, Min(0f)] private float heavyEnemyHealth = 400f;
        [SerializeField, Range(0f, 1f)] private float heavyKillShake = 0.45f;
        [SerializeField, Min(0f)] private float heavyKillStop = 0.08f;

        [Header("Boss")]
        [SerializeField, Range(0f, 1f)] private float bossArrivalShake = 0.6f;

        private void OnEnable()
        {
            playerHealth.Damaged += OnPlayerDamaged;
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
            EventBus<BossSpawnedEvent>.Subscribe(OnBossSpawned);
        }

        private void OnDisable()
        {
            playerHealth.Damaged -= OnPlayerDamaged;
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            EventBus<BossSpawnedEvent>.Unsubscribe(OnBossSpawned);
        }

        private void OnPlayerDamaged(Health health, DamageResult damage)
        {
            var fraction = damage.Amount / health.Max;
            ScreenShake.Add(Mathf.Min(0.8f, hurtShake.x + hurtShake.y * fraction));
            if (fraction >= heavyHitFraction)
                HitStop.Trigger(heavyHitStop);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (evt.Definition == null || evt.Definition.MaxHealth < heavyEnemyHealth)
                return;
            ScreenShake.AddAt(evt.Position, heavyKillShake);
            HitStop.Trigger(heavyKillStop);
        }

        private void OnBossSpawned(BossSpawnedEvent evt) => ScreenShake.Add(bossArrivalShake);
    }
}
