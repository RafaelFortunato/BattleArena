using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>
    /// Resets the particle systems and trails of a pooled object each time it is spawned, so reused projectiles
    /// and effects never carry trails or particles over from their previous position.
    /// </summary>
    public sealed class VfxRestart : MonoBehaviour, IPoolable
    {
        private ParticleSystem[] _particles;
        private TrailRenderer[] _trails;

        /// <summary>Collects the particle systems and trails.</summary>
        private void Awake() => Collect();

        /// <summary>
        /// Collects the particle systems and trails once; the pool can notify a component on an inactive child (e.g. a
        /// bomb shown only in flight) before its Awake runs.
        /// </summary>
        private void Collect()
        {
            if (_particles != null)
                return;
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            _trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        /// <summary>Clears trails and particles and restarts the particles that play on awake.</summary>
        public void OnSpawned()
        {
            Collect();
            foreach (var trail in _trails)
                trail.Clear();
            foreach (var particles in _particles)
            {
                particles.Clear(false);
                if (particles.main.playOnAwake)
                    particles.Play(false);
            }
        }

        /// <summary>Stops and clears the particles.</summary>
        public void OnDespawned()
        {
            Collect();
            foreach (var particles in _particles)
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>Clears the trails, e.g. after moving the object into place.</summary>
        public void ClearTrails()
        {
            Collect();
            foreach (var trail in _trails)
                trail.Clear();
        }
    }
}
