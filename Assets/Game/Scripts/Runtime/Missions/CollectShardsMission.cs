using System.Collections.Generic;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>Arcane shards appear around the arena; collect all of them in time.</summary>
    [CreateAssetMenu(fileName = "CollectShardsMission", menuName = "BattleArena/Missions/Collect Shards")]
    public sealed class CollectShardsMission : MissionDefinition
    {
        [SerializeField] private MissionShard shardPrefab;
        [SerializeField, Min(1)] private int shardCount = 3;
        [SerializeField, Min(1f)] private float minDistance = 10f;
        [SerializeField, Min(1f)] private float maxDistance = 24f;
        [SerializeField, Min(0f)] private float heightOffset = 1f;

        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        private sealed class Instance : MissionInstance
        {
            private readonly CollectShardsMission _mission;
            private readonly List<MissionShard> _shards = new();
            private int _total;

            public Instance(CollectShardsMission mission, MissionContext context) : base(mission, context) => _mission = mission;

            public override float Progress => _total > 0 ? 1f - (float)_shards.Count / _total : 0f;

            public override string ProgressText => CachedText("Shards collected  {0} / {1}", _total - _shards.Count, _total);

            public override bool TryGetTarget(out Vector3 position)
            {
                position = default;
                var bestSqr = float.MaxValue;
                var player = Context.Player.position;
                foreach (var shard in _shards)
                {
                    var sqr = (shard.transform.position - player).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        position = shard.transform.position;
                    }
                }
                return _shards.Count > 0;
            }

            protected override void OnBegin()
            {
                for (var i = 0; i < _mission.shardCount; i++)
                {
                    if (!Context.Spawner.TryFindSpawnPoint(Context.Player.position, _mission.minDistance, _mission.maxDistance, out var position))
                        continue;

                    var shard = PoolManager.Spawn(_mission.shardPrefab, position + Vector3.up * _mission.heightOffset, Quaternion.identity);
                    shard.Collected += OnShardCollected;
                    _shards.Add(shard);
                }

                _total = _shards.Count;
                if (_total == 0)
                    Fail();
            }

            private void OnShardCollected(MissionShard shard)
            {
                shard.Collected -= OnShardCollected;
                _shards.Remove(shard);
                if (_shards.Count == 0)
                    Succeed();
            }

            public override void Cleanup()
            {
                foreach (var shard in _shards)
                {
                    shard.Collected -= OnShardCollected;
                    shard.Despawn();
                }
                _shards.Clear();
            }
        }
    }
}
