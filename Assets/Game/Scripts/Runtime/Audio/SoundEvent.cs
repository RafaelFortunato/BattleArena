using UnityEngine;

namespace BattleArena.Audio
{
    /// <summary>
    /// One authored sound (a fireball launch, a gem pickup) stored as an asset rather than a bare AudioClip on
    /// some prefab. The indirection buys variation (several takes with a pitch wobble, so ten casts in a row
    /// don't read as one sample stuttering), tuning that lives with the sound instead of every call site, and
    /// a rate limit, which is the difference between a busy fight sounding busy and sounding like static.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleArena/Sound Event", fileName = "SFX_New")]
    public sealed class SoundEvent : ScriptableObject
    {
        [Tooltip("Takes for this sound. One is picked per play; with several authored, the same clip never plays twice in a row.")]
        [SerializeField] private AudioClip[] clips;

        [Header("Level")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
        [Tooltip("Random pitch window. A little spread keeps repeated actions from sounding mechanical; too much and " +
                 "the same sound reads as a different object.")]
        [SerializeField] private Vector2 pitchRange = new(0.95f, 1.05f);

        [Header("Rate limit")]
        [Tooltip("Shortest gap between two plays of this event, in seconds. A volley of hits landing together should " +
                 "read as one impact, not the same clip stacked on itself. 0 plays every request.")]
        [SerializeField, Min(0f)] private float minRetriggerInterval = 0.06f;

        [Header("Timing")]
        [Tooltip("Seconds to wait before this is heard. Almost every sound wants 0; it exists for sounds that belong " +
                 "a little way into an animation, and keeping it here means a swapped clip brings its own timing.")]
        [SerializeField, Min(0f)] private float startDelay;

        [Header("Space")]
        [Tooltip("How positional this sound is: 0 is flat stereo (UI), 1 fully positional. Negative takes the " +
                 "manager's default, which is what world sounds normally want.")]
        [SerializeField, Range(-1f, 1f)] private float spatialBlend = -1f;
        [Tooltip("Distance from the listener inside which the sound plays at full level. Negative takes the manager's default.")]
        [SerializeField] private float fullVolumeDistance = -1f;
        [Tooltip("Distance from the listener at which the sound has faded out. Lower it to keep something local, raise " +
                 "it for something the whole arena should notice. Negative takes the manager's default.")]
        [SerializeField] private float silenceDistance = -1f;

        // Play-time bookkeeping, not authored data: serializing it would dirty the asset on every play.
        [System.NonSerialized] private int _lastClipIndex = -1;

        /// <summary>Authored loudness for this event, before the master and category levels.</summary>
        public float Volume => volume;

        /// <summary>Shortest gap between two plays, so one frame's worth of requests can't stack into a spike.</summary>
        public float MinRetriggerInterval => minRetriggerInterval;

        /// <summary>Seconds between the request and the sound being audible. 0 for almost everything.</summary>
        public float StartDelay => startDelay;

        /// <summary>Blend authored on this sound, or a negative value to take the manager's default.</summary>
        public float SpatialBlendOverride => spatialBlend;

        /// <summary>Full-volume radius authored here, or negative to take the manager's default.</summary>
        public float FullVolumeDistanceOverride => fullVolumeDistance;

        /// <summary>Silence radius authored here, or negative to take the manager's default.</summary>
        public float SilenceDistanceOverride => silenceDistance;

        /// <summary>True when at least one take is assigned; an empty event plays nothing.</summary>
        public bool HasClips => clips != null && clips.Length > 0;

        /// <summary>A fresh pitch for one play, inside the authored window.</summary>
        public float RandomPitch => Random.Range(pitchRange.x, pitchRange.y);

        /// <summary>
        /// Picks a clip for one play, never the same one twice running when there are several: back-to-back
        /// repeats are exactly what makes a randomised sound stop sounding randomised.
        /// </summary>
        public AudioClip PickClip()
        {
            if (!HasClips)
                return null;
            if (clips.Length == 1)
                return clips[0];

            var index = Random.Range(0, clips.Length);
            if (index == _lastClipIndex)
                index = (index + 1) % clips.Length;
            _lastClipIndex = index;
            return clips[index];
        }

        /// <summary>Keeps the authored distances valid: a silence radius inside the full-volume one would stop the fade.</summary>
        private void OnValidate()
        {
            if (fullVolumeDistance >= 0f && silenceDistance >= 0f && silenceDistance <= fullVolumeDistance)
                silenceDistance = fullVolumeDistance + 1f;
        }
    }
}
