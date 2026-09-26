using BattleArena.Core;
using BattleArena.Pooling;
using TMPro;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>
    /// Floating damage numbers above enemies. A fixed set of TextMeshPro labels is created up front and
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

        private struct Entry
        {
            public Transform Transform;
            public Vector3 Origin;
            public float Age;
            public float Scale;
            public bool IsCritical;
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

        private void OnEnable() => EventBus<EnemyDamagedEvent>.Subscribe(Show);

        private void OnDisable() => EventBus<EnemyDamagedEvent>.Unsubscribe(Show);

        private void Show(EnemyDamagedEvent evt)
        {
            if (evt.Amount < 0.5f)
                return;

            if (evt.IsCritical && criticalEffect != null)
                PoolManager.Spawn(criticalEffect, evt.Position, Quaternion.identity);

            // When saturated, drop the number rather than evicting one mid-animation.
            if (_activeCount >= capacity)
                return;

            var index = _activeCount++;
            var label = _labels[index];
            var jitter = Random.insideUnitCircle * horizontalJitter;

            ref var entry = ref _entries[index];
            entry.Origin = evt.Position + new Vector3(jitter.x, 0.2f, jitter.y);
            entry.Age = 0f;
            entry.IsCritical = evt.IsCritical;
            entry.Scale = evt.IsCritical ? criticalScale : normalScale;

            // SetText with a numeric argument formats without allocating a string.
            if (evt.IsCritical)
                label.SetText("{0:0}!", evt.Amount);
            else
                label.SetText("{0:0}", evt.Amount);
            label.color = evt.IsCritical ? criticalColor : normalColor;

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

                // Ease-out rise, punch for crits, shrink away at the end.
                var rise = 1f - (1f - t) * (1f - t);
                var scale = entry.Scale;
                if (entry.IsCritical)
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
