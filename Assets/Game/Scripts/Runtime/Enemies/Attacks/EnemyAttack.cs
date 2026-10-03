using BattleArena.Audio;
using BattleArena.Combat;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>Strategy for one way an enemy hurts the player. An enemy may have several.</summary>
    [RequireComponent(typeof(Enemy))]
    public abstract class EnemyAttack : MonoBehaviour, IPoolable
    {
        protected Enemy Owner { get; private set; }

        protected virtual void Awake() => Owner = GetComponent<Enemy>();

        public abstract void Tick(in EnemyTickContext context);

        public virtual void OnSpawned() { }

        public virtual void OnDespawned() { }

        protected float PlanarSqrDistanceTo(Vector3 position)
        {
            var offset = position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        /// <summary>Planar squared distance from a point to a segment; used to catch fast movers between ticks.</summary>
        protected static float SqrDistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            point.y = a.y = b.y = 0f;
            var segment = b - a;
            var lengthSqr = segment.sqrMagnitude;
            var t = lengthSqr > 0.0001f ? Mathf.Clamp01(Vector3.Dot(point - a, segment) / lengthSqr) : 0f;
            return (a + segment * t - point).sqrMagnitude;
        }

        /// <summary>Damage scaled by the enemy's difficulty multiplier. Enemy hits never crit.</summary>
        protected DamageProfile ScaledDamage(float baseDamage) => DamageProfile.Flat(baseDamage * Owner.DamageMultiplier);
    }

    /// <summary>
    /// Attack with a readable rhythm: in range and off cooldown → wind-up (enemy stops, faces the
    /// player, shows an indicator) → execute → optional active phase → cooldown. Implemented as a
    /// timestamp state machine on the director tick, so it never allocates.
    /// </summary>
    public abstract class TelegraphedAttack : EnemyAttack
    {
        private enum Phase { Ready, WindingUp, Active }

        [SerializeField, Min(0.5f)] private float range = 10f;
        [SerializeField, Min(0.1f)] private float cooldown = 3f;
        [SerializeField, Min(0f)] private float windup = 0.5f;
        [Tooltip("Optional object shown during the wind-up (e.g. a glowing charge at the weapon).")]
        [SerializeField] private GameObject windupIndicator;
        [SerializeField] private SoundEvent windupSound;
        [SerializeField, Min(0f)] protected float baseDamage = 10f;

        private Phase _phase;
        private float _phaseEndsAt;
        private float _readyAt;

        protected float Range => range;
        protected float Windup => windup;

        /// <summary>Seconds the enemy stays held after executing (e.g. a dash). Zero for instant attacks.</summary>
        protected virtual float ActiveDuration => 0f;

        public override void OnSpawned()
        {
            _phase = Phase.Ready;
            SetIndicator(false);
            // Stagger first attacks so a freshly spawned group doesn't fire in unison.
            _readyAt = Time.time + Random.Range(0.5f, cooldown);
        }

        public override void OnDespawned()
        {
            if (_phase == Phase.Active)
                EndActive();
            _phase = Phase.Ready;
            SetIndicator(false);
        }

        public sealed override void Tick(in EnemyTickContext context)
        {
            switch (_phase)
            {
                case Phase.Ready:
                    if (context.Now < _readyAt || Owner.IsHeld || !CanStart(context))
                        return;
                    _phase = Phase.WindingUp;
                    _phaseEndsAt = context.Now + windup;
                    Owner.Hold();
                    Owner.Face(context.TargetPosition);
                    Owner.NotifyAttack(windup);
                    SetIndicator(true);
                    AudioManager.PlayAt(windupSound, transform.position);
                    OnWindupStarted(context);
                    break;

                case Phase.WindingUp:
                    if (context.Now < _phaseEndsAt)
                        return;
                    SetIndicator(false);
                    Execute(context);
                    if (!Owner.isActiveAndEnabled)
                        return; // The attack removed its owner (self-destruct).
                    if (ActiveDuration > 0f)
                    {
                        _phase = Phase.Active;
                        _phaseEndsAt = context.Now + ActiveDuration;
                        return;
                    }
                    Finish(context.Now);
                    break;

                case Phase.Active:
                    TickActive(context);
                    if (context.Now >= _phaseEndsAt)
                    {
                        EndActive();
                        Finish(context.Now);
                    }
                    break;
            }
        }

        /// <summary>Extra start condition on top of range and cooldown.</summary>
        protected virtual bool CanStart(in EnemyTickContext context) =>
            PlanarSqrDistanceTo(context.TargetPosition) <= range * range;

        protected virtual void OnWindupStarted(in EnemyTickContext context) { }

        protected abstract void Execute(in EnemyTickContext context);

        protected virtual void TickActive(in EnemyTickContext context) { }

        protected virtual void EndActive() { }

        private void Finish(float now)
        {
            _phase = Phase.Ready;
            _readyAt = now + cooldown;
            Owner.Release();
        }

        private void SetIndicator(bool visible)
        {
            if (windupIndicator != null)
                windupIndicator.SetActive(visible);
        }
    }
}
