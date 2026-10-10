using BattleArena.Combat;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BattleArena.Vfx
{
    /// <summary>
    /// Drives the post-process vignette from the player's health: the screen edges flash red when hit, and below a
    /// health threshold they throb like a heartbeat that quickens as health drops. It reuses the vignette the scene
    /// already renders, so it costs nothing extra on WebGL.
    /// </summary>
    public sealed class HurtVignette : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Volume volume;
        [Tooltip("What the edges tint toward. The vignette multiplies, so this reads as a dark red rim.")]
        [SerializeField] private Color hurtColor = new(0.6f, 0.03f, 0.05f);

        [Header("Hit flash")]
        [Tooltip("Vignette intensity added at the peak of a hit flash.")]
        [SerializeField, Min(0f)] private float hitIntensity = 0.2f;
        [SerializeField, Min(0.01f)] private float hitFadeSeconds = 0.45f;

        [Header("Low health")]
        [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.3f;
        [Tooltip("Vignette intensity added at the top of each heartbeat.")]
        [SerializeField, Min(0f)] private float lowHealthIntensity = 0.14f;
        [Tooltip("Seconds per heartbeat: at the threshold, and close to death.")]
        [SerializeField] private Vector2 heartbeatPeriod = new(1.1f, 0.6f);

        private Vignette _vignette;
        private float _baseIntensity;
        private Color _baseColor;
        private float _flash;
        private float _beatPhase;

        /// <summary>
        /// Finds the vignette in the volume's profile and remembers its look; volume.profile is a per-play copy, so
        /// none of this writes back into the profile asset.
        /// </summary>
        private void Start()
        {
            if (volume == null || !volume.profile.TryGet(out _vignette))
            {
                enabled = false;
                return;
            }
            _baseIntensity = _vignette.intensity.value;
            _baseColor = _vignette.color.value;
            _vignette.intensity.overrideState = true;
            _vignette.color.overrideState = true;
        }

        /// <summary>Listens for hits on the player.</summary>
        private void OnEnable() => playerHealth.Damaged += OnDamaged;

        /// <summary>Stops listening for hits.</summary>
        private void OnDisable() => playerHealth.Damaged -= OnDamaged;

        /// <summary>Starts a hit flash.</summary>
        private void OnDamaged(Health health, DamageResult damage) => _flash = 1f;

        /// <summary>Blends the vignette between its normal look, the hit flash and the low-health heartbeat.</summary>
        private void Update()
        {
            if (_vignette == null)
                return;

            // Unscaled, so a hit-stop doesn't hold the flash; paused screens freeze the heartbeat below.
            var deltaTime = Time.unscaledDeltaTime;
            _flash = Mathf.Max(0f, _flash - deltaTime / hitFadeSeconds);

            var beat = 0f;
            var health = playerHealth.Normalized;
            if (playerHealth.IsAlive && health < lowHealthThreshold && Time.timeScale > 0f)
            {
                var danger = 1f - health / lowHealthThreshold;
                _beatPhase = (_beatPhase + deltaTime / Mathf.Lerp(heartbeatPeriod.x, heartbeatPeriod.y, danger)) % 1f;
                beat = Heartbeat(_beatPhase) * Mathf.Lerp(0.6f, 1f, danger);
            }

            var flash = _flash * _flash;
            _vignette.color.value = Color.Lerp(_baseColor, hurtColor, Mathf.Max(flash, beat));
            _vignette.intensity.value = _baseIntensity + hitIntensity * flash + lowHealthIntensity * beat;
        }

        /// <summary>A "lub-dub": two quick thumps at the start of each beat, then rest.</summary>
        private static float Heartbeat(float phase) => Mathf.Max(Thump(phase, 0f), 0.7f * Thump(phase, 0.2f));

        /// <summary>One heartbeat thump: a half sine starting at <paramref name="start"/> into the beat.</summary>
        private static float Thump(float phase, float start)
        {
            var t = (phase - start) / 0.13f;
            return t is < 0f or > 1f ? 0f : Mathf.Sin(t * Mathf.PI);
        }
    }
}
