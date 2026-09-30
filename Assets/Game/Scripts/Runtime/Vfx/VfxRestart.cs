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

        private void Awake()
        {
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            _trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        public void OnSpawned()
        {
            foreach (var trail in _trails)
                trail.Clear();
            foreach (var particles in _particles)
            {
                particles.Clear(false);
                if (particles.main.playOnAwake)
                    particles.Play(false);
            }
        }

        public void OnDespawned()
        {
            foreach (var particles in _particles)
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>Clears the trails, e.g. after moving the object into place.</summary>
        public void ClearTrails()
        {
            foreach (var trail in _trails)
                trail.Clear();
        }
    }
}
