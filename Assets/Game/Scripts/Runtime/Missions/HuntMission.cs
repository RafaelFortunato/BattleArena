using BattleArena.Combat;
using BattleArena.Enemies;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>Spawn a special enemy (Golden Goblin, Warlord) and kill it before time runs out, or it escapes.</summary>
    [CreateAssetMenu(fileName = "HuntMission", menuName = "BattleArena/Missions/Hunt")]
    public sealed class HuntMission : MissionDefinition
    {
        [SerializeField] private EnemyDefinition target;
        [SerializeField, Min(0.1f)] private float healthScale = 1f;
        [Tooltip("Extra health per minute of the match, as a fraction of the base (1 = +100% of base health per minute).")]
        [SerializeField, Min(0f)] private float healthGrowthPerMinute;
        [SerializeField, Min(1f)] private float minSpawnDistance = 14f;
        [SerializeField, Min(1f)] private float maxSpawnDistance = 20f;

        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        private sealed class Instance : MissionInstance
        {
            private readonly HuntMission _mission;
            private readonly string _statusFormat;
            private Enemy _enemy;

            public Instance(HuntMission mission, MissionContext context) : base(mission, context)
            {
                _mission = mission;
                _statusFormat = mission.target.DisplayName + " health {1}%";
            }

            public override float Progress => _enemy != null ? 1f - _enemy.Health.Normalized : 1f;

            public override string ProgressText => CachedText(_statusFormat, 0, Mathf.CeilToInt((1f - Progress) * 100f));

            public override bool TryGetTarget(out Vector3 position)
            {
                position = _enemy != null ? _enemy.transform.position : default;
                return _enemy != null;
            }

            protected override void OnBegin()
            {
                var spawner = Context.Spawner;
                if (!spawner.TryFindSpawnPoint(Context.Player.position, _mission.minSpawnDistance, _mission.maxSpawnDistance, out var position))
                {
                    Fail();
                    return;
                }

                var health = _mission.healthScale * (1f + _mission.healthGrowthPerMinute * Context.ElapsedMinutes);
                _enemy = Enemy.Spawn(_mission.target, position, Quaternion.identity, health, 1f);
                _enemy.Health.Died += OnTargetDied;
            }

            private void OnTargetDied(Health _)
            {
                Release();
                Succeed();
            }

            public override void Cleanup()
            {
                if (_enemy == null)
                    return;

                // Out of time: the target escapes.
                var enemy = _enemy;
                Release();
                if (enemy.Health.IsAlive)
                    enemy.Vanish();
            }

            private void Release()
            {
                if (_enemy == null)
                    return;
                _enemy.Health.Died -= OnTargetDied;
                _enemy = null;
            }
        }
    }
}
