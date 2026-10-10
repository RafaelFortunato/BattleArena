using BattleArena.Audio;
using UnityEngine;

namespace BattleArena.Enemies.Attacks
{
    /// <summary>
    /// Attack with a readable rhythm: in range and off cooldown → wind-up (enemy stops, faces the
    /// player, shows an indicator) → execute → optional active phase → cooldown. Implemented as a
    /// timestamp state machine on the director tick, so it never allocates.
    /// </summary>
    public abstract class TelegraphedAttack : EnemyAttack
    {
        /// <summary>Where the attack is in its cycle.</summary>
        private enum Phase
        {
            /// <summary>Waiting for the cooldown and a chance to start.</summary>
            Ready,
            /// <summary>Telegraphing; the enemy is held in place.</summary>
            WindingUp,
            /// <summary>Executed and still running (e.g. a dash in progress).</summary>
            Active
        }

        [SerializeField, Min(0.5f)] private float range = 10f;
        [SerializeField, Min(0.1f)] private float cooldown = 3f;
        [SerializeField, Min(0f)] private float windup = 0.5f;
        [Tooltip("Optional object shown during the wind-up (e.g. a glowing charge at the weapon).")]
        [SerializeField] private GameObject windupIndicator;
        [SerializeField] private SoundEvent windupSound;
        [SerializeField, Min(0f)] private float baseDamage = 10f;

        private Phase _phase;
        private float _phaseEndsAt;
        private float _readyAt;

        /// <summary>Seconds the wind-up lasts.</summary>
        protected float Windup => windup;

        /// <summary>Damage of one hit before the enemy's difficulty multiplier.</summary>
        protected float BaseDamage => baseDamage;

        /// <summary>Seconds the enemy stays held after executing (e.g. a dash). Zero for instant attacks.</summary>
        protected virtual float ActiveDuration => 0f;

        /// <summary>Starts ready but staggered, so a freshly spawned group doesn't fire in unison.</summary>
        public override void OnSpawned()
        {
            _phase = Phase.Ready;
            SetIndicator(false);
            _readyAt = Time.time + Random.Range(0.5f, cooldown);
        }

        /// <summary>Ends a running active phase and hides the indicator.</summary>
        public override void OnDespawned()
        {
            if (_phase == Phase.Active)
                EndActive();
            _phase = Phase.Ready;
            SetIndicator(false);
        }

        /// <summary>Advances the state machine: start the wind-up, execute when it ends, then run any active phase.</summary>
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

        /// <summary>Called when the wind-up begins, e.g. to show a telegraph on the ground.</summary>
        protected virtual void OnWindupStarted(in EnemyTickContext context) { }

        /// <summary>Delivers the attack at the end of the wind-up.</summary>
        protected abstract void Execute(in EnemyTickContext context);

        /// <summary>Called on every tick of the active phase.</summary>
        protected virtual void TickActive(in EnemyTickContext context) { }

        /// <summary>Called once when the active phase ends (or the enemy despawns during it).</summary>
        protected virtual void EndActive() { }

        /// <summary>Starts the cooldown and gives the enemy back to its movement.</summary>
        private void Finish(float now)
        {
            _phase = Phase.Ready;
            _readyAt = now + cooldown;
            Owner.Release();
        }

        /// <summary>Shows or hides the optional wind-up indicator.</summary>
        private void SetIndicator(bool visible)
        {
            if (windupIndicator != null)
                windupIndicator.SetActive(visible);
        }
    }
}
