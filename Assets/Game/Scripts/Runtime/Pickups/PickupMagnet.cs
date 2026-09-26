using System.Collections.Generic;
using BattleArena.Player;
using UnityEngine;

namespace BattleArena.Pickups
{
    /// <summary>
    /// Trigger volume around the player. Gems entering it are pulled toward the player and collected.
    /// A single Update moves every attracted gem, instead of one Update per gem, and it early-outs when idle.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PickupMagnet : MonoBehaviour
    {
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private Transform collectPoint;
        [SerializeField, Min(0.1f)] private float pullSpeed = 16f;
        [SerializeField, Min(0.05f)] private float collectDistance = 0.5f;

        private readonly List<ExperienceGem> _attracted = new(64);
        private SphereCollider _trigger;
        private float _baseRadius;

        private void Awake()
        {
            _trigger = GetComponent<SphereCollider>();
            _baseRadius = _trigger.radius;
        }

        private void OnEnable() => stats.Changed += OnStatChanged;

        private void OnDisable() => stats.Changed -= OnStatChanged;

        private void OnStatChanged(StatType stat)
        {
            if (stat == StatType.PickupRadius)
                _trigger.radius = _baseRadius * stats.PickupRadiusMultiplier;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out ExperienceGem gem) || gem.IsAttracted)
                return;

            gem.Attract();
            _attracted.Add(gem);
        }

        private void Update()
        {
            if (_attracted.Count == 0)
                return;

            var target = collectPoint.position;
            var step = pullSpeed * Time.deltaTime;
            var collectSqr = collectDistance * collectDistance;

            for (var i = _attracted.Count - 1; i >= 0; i--)
            {
                var gem = _attracted[i];
                if (!gem.IsAttracted)
                {
                    // Released by something else (e.g. scene cleanup).
                    RemoveAt(i);
                    continue;
                }

                var gemTransform = gem.transform;
                var position = Vector3.MoveTowards(gemTransform.position, target, step);
                if ((target - position).sqrMagnitude <= collectSqr)
                {
                    progression.AddExperience(gem.Value);
                    gem.Collect();
                    RemoveAt(i);
                    continue;
                }

                gemTransform.position = position;
            }
        }

        private void RemoveAt(int index)
        {
            // Order doesn't matter: swap-remove keeps it O(1).
            var last = _attracted.Count - 1;
            _attracted[index] = _attracted[last];
            _attracted.RemoveAt(last);
        }
    }
}
