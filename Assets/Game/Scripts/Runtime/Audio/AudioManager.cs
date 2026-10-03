using System.Collections;
using System.Collections.Generic;
using BattleArena.Core;
using UnityEngine;

namespace BattleArena.Audio
{
    /// <summary>
    /// Voice pool and mixer for every sound in the game.
    ///
    /// Gameplay code never touches an AudioSource: it hands over a <see cref="SoundEvent"/> and, for world
    /// sounds, a position, and this decides whether the sound is allowed right now and which voice plays it.
    /// Centralising that is what keeps a busy wave (a dozen bolts landing as a meteor hits and a pack dies)
    /// from turning into clipped mush, via the per-event rate limit and a hard cap on live voices.
    ///
    /// The entry points are static and silent when no manager is in the scene, so gameplay can announce
    /// sounds without caring whether the audio layer exists.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class AudioManager : Singleton<AudioManager>
    {
        [Header("Voices")]
        [Tooltip("Simultaneous sounds. Once they're all busy the oldest is recycled, so a burst of new events always " +
                 "beats whatever is already fading out.")]
        [SerializeField, Min(1)] private int voiceCount = 32;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.35f;

        [Header("World space")]
        // Measured from the AudioListener, which rides the camera about 22 units above and behind the player.
        // Anything on screen is therefore 18-40 units away, so the full-volume radius has to cover the camera
        // height or nothing would ever play at full level.
        [Tooltip("Inside this distance from the listener a world sound plays at full level. Covers the camera height, " +
                 "so everything around the player is heard in full.")]
        [SerializeField, Min(0f)] private float fullVolumeDistance = 26f;
        [Tooltip("Distance at which a world sound has faded out completely: a little past the edge of the screen.")]
        [SerializeField, Min(0f)] private float silenceDistance = 60f;
        [Tooltip("How positional world sounds are. 1 is fully 3D; below that a fraction stays flat stereo and never " +
                 "fades, which keeps a distant event present in the mix rather than lost. A sound can override this.")]
        [SerializeField, Range(0f, 1f)] private float worldSpatialBlend = 0.6f;

        [Header("Ambience")]
        [Tooltip("Looping bed under everything else (wind, birdsong). Optional.")]
        [SerializeField] private AudioClip ambienceLoop;

        // Sounds that are the same everywhere they happen: a button click is a button click whichever panel it is on.
        [Header("Shared cues")]
        [Tooltip("Any plain button press.")]
        [SerializeField] private SoundEvent uiClick;
        [Tooltip("The pointer moving onto a button.")]
        [SerializeField] private SoundEvent uiHover;

        [Header("Debug")]
        [Tooltip("Log every sound that actually plays. Rate-limited requests are not logged, so the console shows " +
                 "what was heard rather than what was asked for.")]
        [SerializeField] private bool logPlays;

        // Player-set levels live here rather than in a settings UI, so audio starts at the saved level on the very
        // first frame. The serialized fields double as the defaults.
        private const string SfxVolumeKey = "audio.sfxVolume";
        private const string AmbienceVolumeKey = "audio.ambienceVolume";

        private AudioSource[] _voices;
        private float[] _voiceStartedAt;
        private AudioSource _ambienceSource;

        /// <summary>Temporary multiplier on the ambience bed, 1 when nothing is ducking it.</summary>
        private float _ambienceDuck = 1f;
        private Coroutine _duckRoutine;

        /// <summary>
        /// Last time each event was allowed through, for the per-event rate limit. Kept here rather than on the
        /// SoundEvent asset because assets outlive a play session.
        /// </summary>
        private readonly Dictionary<SoundEvent, float> _lastPlayedAt = new();

        /// <summary>One running loop and the event that started it, so a live edit can be pushed onto the voice.</summary>
        private sealed class LoopVoice
        {
            public AudioSource Source;
            public SoundEvent Sound;
        }

        // Loops get their own voices rather than borrowing from the pool: the pool steals its oldest voice when
        // everything is busy, which suits one-shots but would cut a running loop dead.
        private readonly Dictionary<Transform, LoopVoice> _loops = new();

        /// <summary>Current SFX level, 0-1. Safe to read with no manager in the scene.</summary>
        public static float SfxVolume => Instance != null ? Instance.sfxVolume : 0f;

        /// <summary>Current ambience level, 0-1. Safe to read with no manager in the scene.</summary>
        public static float AmbienceVolume => Instance != null ? Instance.ambienceVolume : 0f;

        /// <summary>A plain button press.</summary>
        public static void PlayClick() => PlayUI(Instance != null ? Instance.uiClick : null);

        /// <summary>The pointer moving onto a button.</summary>
        public static void PlayHover() => PlayUI(Instance != null ? Instance.uiHover : null);

        /// <summary>Plays a sound at a point in the arena. Silent if no manager exists.</summary>
        public static void PlayAt(SoundEvent sound, Vector3 position)
        {
            if (Instance != null)
                Instance.Play(sound, position, true);
        }

        /// <summary>
        /// Plays a sound flat, with no position: menu clicks, the level-up flourish, anything the player should hear
        /// identically wherever it happened. Silent if no manager exists.
        /// </summary>
        public static void PlayUI(SoundEvent sound)
        {
            if (Instance != null)
                Instance.Play(sound, Vector3.zero, false);
        }

        /// <summary>
        /// Starts a sound looping at <paramref name="owner"/>, or does nothing if that owner already loops it.
        /// The owner is the key as well as the position, so a caller never holds a handle and cannot leak one.
        /// </summary>
        public static void StartLoop(SoundEvent sound, Transform owner)
        {
            if (Instance == null || sound == null || owner == null || !sound.HasClips)
                return;
            Instance.BeginLoop(sound, owner);
        }

        /// <summary>Stops whatever <paramref name="owner"/> had looping. Safe to call twice.</summary>
        public static void StopLoop(Transform owner)
        {
            if (Instance == null || owner == null)
                return;
            Instance.EndLoop(owner);
        }

        /// <summary>Re-reads a sound's distances onto any loop playing it. Called by SoundEvent when edited.</summary>
        public static void RefreshRolloff(SoundEvent sound)
        {
            if (Instance == null || sound == null)
                return;

            foreach (var entry in Instance._loops.Values)
                if (entry.Source != null && entry.Sound == sound)
                    Instance.ApplyRolloff(entry.Source, sound);
        }

        /// <summary>
        /// Sets the SFX level immediately. Sounds already playing keep their level: they are all short, and
        /// re-scaling live voices would make a slider drag audibly lurch.
        /// </summary>
        public static void SetSfxVolume(float value)
        {
            if (Instance != null)
                Instance.sfxVolume = Mathf.Clamp01(value);
        }

        /// <summary>Sets the ambience level immediately, re-scaling the live bed so the change is heard at once.</summary>
        public static void SetAmbienceVolume(float value)
        {
            if (Instance == null)
                return;
            Instance.ambienceVolume = Mathf.Clamp01(value);
            Instance.ApplyAmbienceVolume();
        }

        /// <summary>Writes the current levels to PlayerPrefs; call once when a settings panel closes, not per tick.</summary>
        public static void SaveVolumePrefs()
        {
            if (Instance == null)
                return;
            PlayerPrefs.SetFloat(SfxVolumeKey, Instance.sfxVolume);
            PlayerPrefs.SetFloat(AmbienceVolumeKey, Instance.ambienceVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Pulls the ambience bed down for a moment so something else can be heard over it (the victory fanfare),
        /// then eases it back. Unscaled time throughout, because end screens freeze the clock. A second call
        /// replaces the first rather than stacking.
        /// </summary>
        public static void DuckAmbience(float toFraction, float holdSeconds, float fadeSeconds = 0.8f)
        {
            if (Instance == null)
                return;
            if (Instance._duckRoutine != null)
                Instance.StopCoroutine(Instance._duckRoutine);
            Instance._duckRoutine = Instance.StartCoroutine(
                Instance.DuckRoutine(Mathf.Clamp01(toFraction), holdSeconds, Mathf.Max(fadeSeconds, 0.01f)));
        }

        /// <summary>
        /// Restores saved levels, then builds the voice pool and starts the ambience, in that order so the bed
        /// starts at the player's own volume. The guard makes a second bind (after a domain reload) harmless.
        /// </summary>
        protected override void OnBind()
        {
            if (_voices != null)
                return;

            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume));
            ambienceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(AmbienceVolumeKey, ambienceVolume));
            BuildVoices();
            StartAmbience();
        }

        private void BuildVoices()
        {
            // A domain reload wipes the array but leaves the voice objects parented here; clear them rather than
            // ending up with two pools.
            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            _voices = new AudioSource[voiceCount];
            _voiceStartedAt = new float[voiceCount];
            for (var i = 0; i < voiceCount; i++)
            {
                var source = new GameObject($"Voice {i}").AddComponent<AudioSource>();
                source.transform.SetParent(transform, false);
                source.playOnAwake = false;
                ApplyRolloff(source, null);
                _voices[i] = source;
                _voiceStartedAt[i] = float.NegativeInfinity;
            }
        }

        /// <summary>Starts the looping bed, flat stereo: a bed that panned with the action would draw attention to itself.</summary>
        private void StartAmbience()
        {
            if (ambienceLoop == null)
                return;

            _ambienceSource = new GameObject("Ambience").AddComponent<AudioSource>();
            _ambienceSource.transform.SetParent(transform, false);
            _ambienceSource.clip = ambienceLoop;
            _ambienceSource.loop = true;
            _ambienceSource.playOnAwake = false;
            _ambienceSource.spatialBlend = 0f;
            ApplyAmbienceVolume();
            _ambienceSource.Play();
        }

        /// <summary>The bed's actual level: the player's setting times the current duck, kept apart so a duck never touches the setting.</summary>
        private void ApplyAmbienceVolume()
        {
            if (_ambienceSource != null)
                _ambienceSource.volume = ambienceVolume * masterVolume * _ambienceDuck;
        }

        private IEnumerator DuckRoutine(float to, float hold, float fade)
        {
            // Down fast, so the cue is clear from its first note; back up slowly, so the return is not itself an event.
            var from = _ambienceDuck;
            for (var t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
            {
                _ambienceDuck = Mathf.Lerp(from, to, t / 0.15f);
                ApplyAmbienceVolume();
                yield return null;
            }
            _ambienceDuck = to;
            ApplyAmbienceVolume();

            if (hold > 0f)
                yield return new WaitForSecondsRealtime(hold);

            for (var t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                _ambienceDuck = Mathf.Lerp(to, 1f, t / fade);
                ApplyAmbienceVolume();
                yield return null;
            }
            _ambienceDuck = 1f;
            ApplyAmbienceVolume();
            _duckRoutine = null;
        }

        /// <summary>
        /// How every world voice fades with distance, in one place so the pool and the loop voices cannot drift
        /// apart. Linear rather than Unity's logarithmic default: logarithmic collapses within a few units, which is
        /// wrong for an arena seen from above, while linear spends the whole window on distances that are on screen.
        /// </summary>
        private void ApplyRolloff(AudioSource source, SoundEvent sound)
        {
            var near = fullVolumeDistance;
            var far = silenceDistance;
            if (sound != null)
            {
                if (sound.FullVolumeDistanceOverride >= 0f)
                    near = sound.FullVolumeDistanceOverride;
                if (sound.SilenceDistanceOverride >= 0f)
                    far = sound.SilenceDistanceOverride;
            }

            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = near;
            // min >= max makes Unity clamp silently and the sound stops fading altogether.
            source.maxDistance = Mathf.Max(far, near + 0.1f);
        }

        /// <summary>Pushes edited default distances onto voices that already exist, so tuning in play mode is heard.</summary>
        private void OnValidate()
        {
            if (_voices != null)
                foreach (var voice in _voices)
                    if (voice != null)
                        ApplyRolloff(voice, null);

            foreach (var entry in _loops.Values)
                if (entry.Source != null)
                    ApplyRolloff(entry.Source, entry.Sound);
        }

        /// <summary>Central play routine: rate limit, pick a take, acquire a voice, set level, pitch and space.</summary>
        private void Play(SoundEvent sound, Vector3 position, bool positional)
        {
            if (sound == null || !sound.HasClips || !PassesRateLimit(sound))
                return;

            var clip = sound.PickClip();
            if (clip == null)
                return;

            var voice = AcquireVoice();
            voice.transform.position = position;
            voice.clip = clip;
            voice.volume = sound.Volume * sfxVolume * masterVolume;
            voice.pitch = sound.RandomPitch;
            // Per play: voices are pooled, so this one still carries the distances of whatever used it last.
            ApplyRolloff(voice, sound);

            var authored = sound.SpatialBlendOverride;
            voice.spatialBlend = !positional ? 0f : authored >= 0f ? authored : worldSpatialBlend;

            // PlayDelayed rather than a coroutine: the voice is claimed now, so voice stealing sees it as busy.
            if (sound.StartDelay > 0f)
                voice.PlayDelayed(sound.StartDelay);
            else
                voice.Play();

            if (logPlays)
                Debug.Log(positional ? $"[SFX] {sound.name} ({clip.name}) @ {position}" : $"[SFX] {sound.name} ({clip.name}) [UI]");
        }

        /// <summary>True when enough unscaled time has passed since this event last played (level-up freezes timeScale).</summary>
        private bool PassesRateLimit(SoundEvent sound)
        {
            var interval = sound.MinRetriggerInterval;
            if (interval <= 0f)
                return true;

            var now = Time.unscaledTime;
            if (_lastPlayedAt.TryGetValue(sound, out var previous) && now - previous < interval)
                return false;

            _lastPlayedAt[sound] = now;
            return true;
        }

        /// <summary>
        /// A free voice, or the longest-running one when all are busy. Stealing the oldest means a new event always
        /// lands; what it interrupts is furthest into its tail and closest to inaudible already.
        /// </summary>
        private AudioSource AcquireVoice()
        {
            var oldestIndex = 0;
            var oldestTime = float.PositiveInfinity;
            for (var i = 0; i < _voices.Length; i++)
            {
                if (!_voices[i].isPlaying)
                {
                    _voiceStartedAt[i] = Time.unscaledTime;
                    return _voices[i];
                }

                if (_voiceStartedAt[i] < oldestTime)
                {
                    oldestTime = _voiceStartedAt[i];
                    oldestIndex = i;
                }
            }

            _voiceStartedAt[oldestIndex] = Time.unscaledTime;
            return _voices[oldestIndex];
        }

        private void BeginLoop(SoundEvent sound, Transform owner)
        {
            var clip = sound.PickClip();
            if (clip == null)
                return;

            if (_loops.TryGetValue(owner, out var entry) && entry.Source != null)
            {
                if (entry.Source.clip == clip && entry.Source.isPlaying)
                    return;
                entry.Source.Stop();
            }
            else
            {
                var created = new GameObject("Loop").AddComponent<AudioSource>();
                created.transform.SetParent(transform, false);
                created.playOnAwake = false;
                entry = new LoopVoice { Source = created };
                _loops[owner] = entry;
            }

            entry.Sound = sound;
            var voice = entry.Source;
            voice.name = $"Loop {sound.name}";
            voice.transform.position = owner.position;
            voice.clip = clip;
            voice.loop = true;
            voice.volume = sound.Volume * sfxVolume * masterVolume;
            voice.pitch = sound.RandomPitch;
            ApplyRolloff(voice, sound);
            voice.spatialBlend = sound.SpatialBlendOverride >= 0f ? sound.SpatialBlendOverride : worldSpatialBlend;
            voice.Play();

            if (logPlays)
                Debug.Log($"[SFX] {sound.name} looping at {owner.name}");
        }

        private void EndLoop(Transform owner)
        {
            if (!_loops.TryGetValue(owner, out var entry))
                return;

            if (entry.Source != null)
            {
                entry.Source.Stop();
                entry.Source.clip = null;
            }
            _loops.Remove(owner);
        }
    }
}
