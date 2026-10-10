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
        [Tooltip("Shards try to land at least this far from each other.")]
        [SerializeField, Min(0f)] private float minShardSpacing = 12f;

        /// <summary>Creates the state for one attempt.</summary>
        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        /// <summary>Run-time state: the shards still lying in the arena.</summary>
        private sealed class Instance : MissionInstance
        {
            private readonly CollectShardsMission _mission;
            private readonly List<MissionShard> _shards = new();
            private int _total;

            /// <summary>Creates the state for one attempt.</summary>
            public Instance(CollectShardsMission mission, MissionContext context) : base(mission, context) => _mission = mission;

            /// <summary>Share of the shards collected.</summary>
            public override float Progress => _total > 0 ? 1f - (float)_shards.Count / _total : 0f;

            /// <summary>Shards collected against the total.</summary>
            public override string ProgressText => CachedText("Shards collected  {0} / {1}", _total - _shards.Count, _total);

            /// <summary>Points at the shard closest to the player.</summary>
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

            /// <summary>Scatters the shards around the player, spaced apart; fails when none could be placed.</summary>
            protected override void OnBegin()
            {
                var placed = new List<Vector3>();
                for (var i = 0; i < _mission.shardCount; i++)
                {
                    if (!TryFindSpacedPoint(placed, out var position))
                        continue;

                    placed.Add(position);

                    var shard = PoolManager.Spawn(_mission.shardPrefab, position + Vector3.up * _mission.heightOffset, Quaternion.identity);
                    shard.Collected += OnShardCollected;
                    _shards.Add(shard);
                }

                _total = _shards.Count;
                if (_total == 0)
                    Fail();
            }

            /// <summary>
            /// Samples the ring around the player a few times and keeps the first point clear of the other shards;
            /// failing that, the most isolated one.
            /// </summary>
            private bool TryFindSpacedPoint(List<Vector3> placed, out Vector3 position)
            {
                const int Candidates = 16;
                var found = false;
                var bestGap = -1f;
                position = default;
                for (var attempt = 0; attempt < Candidates; attempt++)
                {
                    if (!Context.Spawner.TryFindSpawnPoint(Context.Player.position, _mission.minDistance, _mission.maxDistance, out var candidate))
                        continue;

                    var gap = float.MaxValue;
                    foreach (var other in placed)
                        gap = Mathf.Min(gap, Vector3.Distance(candidate, other));

                    if (gap > bestGap)
                    {
                        bestGap = gap;
                        position = candidate;
                        found = true;
                    }

                    if (gap >= _mission.minShardSpacing)
                        break;
                }
                return found;
            }

            /// <summary>Counts a collected shard and succeeds once all are gone.</summary>
            private void OnShardCollected(MissionShard shard)
            {
                shard.Collected -= OnShardCollected;
                _shards.Remove(shard);
                if (_shards.Count == 0)
                    Succeed();
            }

            /// <summary>Despawns any shards left uncollected.</summary>
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
