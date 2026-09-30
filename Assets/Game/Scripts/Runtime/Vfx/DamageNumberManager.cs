using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Pooling;
using TMPro;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>
    /// Floating damage numbers above enemies, and in red above the player when it is hit. A fixed set of TextMeshPro labels is created up front and
    /// animated from this single Update (rise + scale), so hits never instantiate or allocate. Only
    /// transforms change per frame; text meshes are rebuilt once per number, when it is shown.
    /// </summary>
    public sealed class DamageNumberManager : MonoBehaviour
    {
        [SerializeField] private TextMeshPro labelPrefab;
        [SerializeField, Min(1)] private int capacity = 80;

        [Header("Motion")]
        [SerializeField, Min(0.05f)] private float lifetime = 0.75f;
        [SerializeField, Min(0f)] private float riseDistance = 1.3f;
        [SerializeField, Min(0f)] private float horizontalJitter = 0.45f;
        [SerializeField, Range(0f, 1f)] private float shrinkStart = 0.65f;

        [Header("Normal hit")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField, Min(0.1f)] private float normalScale = 1f;

        [Header("Critical hit")]
        [SerializeField] private Color criticalColor = new(1f, 0.6f, 0.1f);
        [SerializeField, Min(0.1f)] private float criticalScale = 1.6f;
        [Tooltip("Scale multiplier over the first part of a critical number's life (punch-in effect).")]
        [SerializeField] private AnimationCurve criticalPunch = new(new Keyframe(0f, 2.2f), new Keyframe(0.18f, 0.9f), new Keyframe(0.3f, 1f));
        [SerializeField] private TimedRelease criticalEffect;

        [Header("Player hit")]
        [SerializeField] private Health playerHealth;
        [SerializeField] private Color playerHitColor = new(1f, 0.22f, 0.2f);
        [SerializeField, Min(0.1f)] private float playerHitScale = 1.3f;
        [Tooltip("Height of player damage numbers above the player's feet.")]
        [SerializeField, Min(0f)] private float playerHitHeight = 2.2f;

        private struct Entry
        {
            public Transform Transform;
            public Vector3 Origin;
            public float Age;
            public float Scale;
            public bool Punch;
        }

        // Active entries are packed at the front of the array; the rest are free.
        private Entry[] _entries;
        private TextMeshPro[] _labels;
        private int _activeCount;
        private Quaternion _facing = Quaternion.identity;

        private void Awake()
        {
            _entries = new Entry[capacity];
            _labels = new TextMeshPro[capacity];
            for (var i = 0; i < capacity; i++)
            {
                var label = Instantiate(labelPrefab, transform);
                label.gameObject.SetActive(false);
                _labels[i] = label;
                _entries[i].Transform = label.transform;
            }
        }

        private void Start()
        {
            // The gameplay camera never rotates, so facing it once is enough.
            var mainCamera = Camera.main;
            if (mainCamera != null)
                _facing = mainCamera.transform.rotation;
        }

        private void OnEnable()
        {
            EventBus<EnemyDamagedEvent>.Subscribe(ShowEnemyHit);
            if (playerHealth != null)
                playerHealth.Damaged += ShowPlayerHit;
        }

        private void OnDisable()
        {
            EventBus<EnemyDamagedEvent>.Unsubscribe(ShowEnemyHit);
            if (playerHealth != null)
                playerHealth.Damaged -= ShowPlayerHit;
        }

        private void ShowEnemyHit(EnemyDamagedEvent evt)
        {
            if (evt.IsCritical && criticalEffect != null && evt.Amount >= 0.5f)
                PoolManager.Spawn(criticalEffect, evt.Position, Quaternion.identity);

            if (evt.IsCritical)
                Show(evt.Position, evt.Amount, "{0:0}!", true, criticalColor, criticalScale);
            else
                Show(evt.Position, evt.Amount, "{0:0}", false, normalColor, normalScale);
        }

        private void ShowPlayerHit(Health health, DamageResult damage) =>
            Show(health.transform.position + Vector3.up * playerHitHeight, damage.Amount, "-{0:0}", true, playerHitColor, playerHitScale);

        /// <param name="format">TextMeshPro numeric format; formats without allocating a string.</param>
        /// <param name="punch">Plays the punch-in scale effect (critical hits and player hits).</param>
        private void Show(Vector3 position, float amount, string format, bool punch, Color color, float scale)
        {
            // When saturated, drop the number rather than evicting one mid-animation.
            if (amount < 0.5f || _activeCount >= capacity)
                return;

            var index = _activeCount++;
            var label = _labels[index];
            var jitter = Random.insideUnitCircle * horizontalJitter;

            ref var entry = ref _entries[index];
            entry.Origin = position + new Vector3(jitter.x, 0.2f, jitter.y);
            entry.Age = 0f;
            entry.Punch = punch;
            entry.Scale = scale;

            label.SetText(format, amount);
            label.color = color;

            entry.Transform.SetPositionAndRotation(entry.Origin, _facing);
            entry.Transform.localScale = Vector3.one * entry.Scale;
            label.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_activeCount == 0)
                return;

            var deltaTime = Time.deltaTime;
            for (var i = _activeCount - 1; i >= 0; i--)
            {
                ref var entry = ref _entries[i];
                entry.Age += deltaTime;
                var t = entry.Age / lifetime;
                if (t >= 1f)
                {
                    Recycle(i);
                    continue;
                }

                // Ease-out rise, punch for crits and player hits, shrink away at the end.
                var rise = 1f - (1f - t) * (1f - t);
                var scale = entry.Scale;
                if (entry.Punch)
                    scale *= criticalPunch.Evaluate(t);
                if (t > shrinkStart)
                    scale *= 1f - (t - shrinkStart) / (1f - shrinkStart);

                entry.Transform.position = entry.Origin + Vector3.up * (riseDistance * rise);
                entry.Transform.localScale = new Vector3(scale, scale, scale);
            }
        }

        private void Recycle(int index)
        {
            _labels[index].gameObject.SetActive(false);

            // Swap the finished entry with the last active one to keep the active range packed.
            var last = --_activeCount;
            if (index == last)
                return;

            (_entries[index], _entries[last]) = (_entries[last], _entries[index]);
            (_labels[index], _labels[last]) = (_labels[last], _labels[index]);
        }
    }
}
