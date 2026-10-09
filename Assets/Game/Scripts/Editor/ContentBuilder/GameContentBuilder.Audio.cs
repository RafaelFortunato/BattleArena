using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleArena.Audio;
using BattleArena.Combat;
using BattleArena.Enemies;
using BattleArena.Interaction;
using BattleArena.Missions;
using BattleArena.Pickups;
using BattleArena.Progression;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BattleArena.Editor.ContentBuilder
{
    /// <summary>
    /// Sound effects (generated with Stable Audio 3 in ComfyUI, takes named "{sound}_{n}.wav"): their import
    /// settings, one <see cref="SoundEvent"/> per sound, and the wiring that hangs each one on the prefab, component
    /// or scene object that plays it.
    /// </summary>
    internal static partial class GameContentBuilder
    {
        private const string SfxFolder = Root + "/Audio/Sfx";
        private const string SoundEventsFolder = DataFolder + "/Audio";
        private const string MusicFolder = Root + "/Audio/Music";
        // "Battle Theme 4" by Alexandr Zhelanov (CC-BY, credited in the README) and "Determined Pursuit" by Emma_MA (CC0).
        private const string MatchMusic = MusicFolder + "/battle_theme_4.mp3";
        private const string BossMusic = MusicFolder + "/determined_pursuit_loop.wav";

        // Heard flat (menus, stings, the player's own state): keep the stereo image. Everything
        // placed in the world is folded to mono, which 3D panning wants anyway, and it halves the memory.
        private static readonly HashSet<string> StereoClips = new()
        {
            "ui_click", "ui_hover", "level_up", "upgrade_pick", "mission_start", "mission_complete", "mission_failed",
            "victory", "defeat", "boss_roar", "player_hurt", "player_death"
        };

        private sealed class SoundSet
        {
            private readonly Dictionary<string, SoundEvent> _sounds = new();

            public SoundEvent this[string name] => _sounds[name];

            public void Add(string name, SoundEvent sound) => _sounds[name] = sound;
        }

        /// <summary>Creates every sound event and hangs the object-specific ones on their prefabs.</summary>
        private static SoundSet BuildAudio()
        {
            AssetDatabase.Refresh();
            var s = new SoundSet();

            // Player spells. The auto-attack bolt fires constantly, so it sits well under everything else.
            Sound(s, "bolt_shoot", 0.28f, 0.12f, 0.92f, 1.1f);
            Sound(s, "bolt_hit", 0.32f, 0.07f, 0.9f, 1.1f);
            Sound(s, "arcane_lance_cast", 0.7f, 0.1f);
            Sound(s, "arcane_hit", 0.5f, 0.06f);
            Sound(s, "arcane_orbit_cast", 0.6f, 0.3f);
            Sound(s, "fireball_cast", 0.7f, 0.1f);
            Sound(s, "fireball_explosion", 0.75f, 0.08f);
            Sound(s, "chain_lightning", 0.6f, 0.1f);
            // One zap per enemy the chain reaches; the jumps are 0.1 s apart, so the limit sits just under that.
            Sound(s, "lightning_hit", 0.4f, 0.05f, 0.9f, 1.15f);
            Sound(s, "frost_nova", 0.8f, 0.2f);
            Sound(s, "meteor_fall", 0.45f, 0.2f);
            Sound(s, "meteor_impact", 0.8f, 0.12f);
            Sound(s, "phase_step", 0.7f, 0.1f);
            Sound(s, "toxic_miasma", 0.7f, 0.2f);

            // Combat feedback. Hits arrive in volleys (and ticks from lingering spells), so they are quiet and limited.
            Sound(s, "enemy_hit", 0.28f, 0.07f, 0.88f, 1.12f);
            Sound(s, "crit_hit", 0.5f, 0.1f);
            Sound(s, "enemy_death", 0.45f, 0.06f, 0.9f, 1.15f);
            Sound(s, "player_hurt", 0.8f, 0.25f);
            Sound(s, "player_death", 0.9f, 0f, 1f, 1f);

            // Enemy attacks. The boss reuses the wisp's bolt, pitched down to sound bigger.
            Sound(s, "arrow_shoot", 0.45f, 0.08f);
            Sound(s, "arrow_hit", 0.45f, 0.06f);
            Sound(s, "enemy_magic_shoot", 0.45f, 0.08f);
            Sound(s, "enemy_magic_hit", 0.45f, 0.06f);
            Sound(s, "boss_orb_shoot", 0.6f, 0.12f, 0.7f, 0.8f, clip: "enemy_magic_shoot");
            Sound(s, "boss_orb_hit", 0.6f, 0.08f, 0.7f, 0.8f, clip: "enemy_magic_hit");
            Sound(s, "bomb_throw", 0.45f, 0.1f);
            Sound(s, "bomb_explosion", 0.7f, 0.08f);
            Sound(s, "ground_slam", 0.85f, 0.1f);
            Sound(s, "enemy_windup", 0.5f, 0.15f);
            Sound(s, "curse_cast", 0.55f, 0.15f);
            Sound(s, "curse_eruption", 0.65f, 0.1f);
            Sound(s, "dash_charge", 0.6f, 0.2f);

            // Pickups and interactables. Gems come in streams, so a wide pitch spread keeps them sparkling.
            Sound(s, "gem_pickup", 0.35f, 0.05f, 0.95f, 1.25f);
            Sound(s, "shard_pickup", 0.7f, 0.1f);
            Sound(s, "chest_appear", 0.6f, 0.2f);
            Sound(s, "chest_open", 0.8f, 0.2f);
            Sound(s, "well_heal", 0.8f, 0.2f);

            // Interface and match flow (always played flat).
            Sound(s, "ui_click", 0.5f, 0.03f);
            Sound(s, "ui_hover", 0.3f, 0.03f);
            Sound(s, "level_up", 0.8f, 0.2f, 1f, 1f);
            Sound(s, "upgrade_pick", 0.7f, 0.2f, 1f, 1f);
            Sound(s, "mission_start", 0.7f, 0.5f, 1f, 1f);
            Sound(s, "mission_complete", 0.8f, 0.5f, 1f, 1f);
            Sound(s, "mission_failed", 0.7f, 0.5f, 1f, 1f);
            Sound(s, "boss_roar", 0.9f, 1f, 0.95f, 1f);
            Sound(s, "victory", 0.9f, 1f, 1f, 1f);
            Sound(s, "defeat", 0.9f, 1f, 1f, 1f);

            AttachPrefabSounds(s);
            return s;
        }

        private static void AttachPrefabSounds(SoundSet s)
        {
            // Objects whose appearance is the sound: projectiles launching, effects bursting.
            var spawnSounds = new (string prefab, string sound)[]
            {
                ("Projectiles/PlayerBolt", "bolt_shoot"), ("Vfx/Spells/BoltHit", "bolt_hit"),
                ("Projectiles/ArcaneLance", "arcane_lance_cast"), ("Vfx/Spells/ArcaneHit", "arcane_hit"),
                ("Projectiles/ArcaneOrbit", "arcane_orbit_cast"),
                ("Projectiles/Fireball", "fireball_cast"), ("Vfx/Spells/FireballExplosion", "fireball_explosion"),
                ("Vfx/Spells/ChainLightning", "chain_lightning"), ("Vfx/Spells/LightningHit", "lightning_hit"),
                ("Vfx/Spells/FrostNova", "frost_nova"),
                ("Projectiles/MeteorStrike", "meteor_fall"), ("Vfx/Spells/MeteorImpact", "meteor_impact"),
                ("Vfx/Spells/PhaseDepart", "phase_step"), ("Projectiles/ToxicMiasma", "toxic_miasma"),
                ("Vfx/CritSpark", "crit_hit"),
                ("Projectiles/EnemyArrow", "arrow_shoot"), ("Vfx/Spells/ArrowHit", "arrow_hit"),
                ("Projectiles/WispBolt", "enemy_magic_shoot"), ("Vfx/Spells/WispBoltHit", "enemy_magic_hit"),
                ("Projectiles/BossOrb", "boss_orb_shoot"), ("Vfx/Spells/BossOrbHit", "boss_orb_hit"),
                ("Projectiles/GoblinBomb", "bomb_throw"), ("Vfx/BombExplosion", "bomb_explosion"),
                ("Vfx/BossSlamImpact", "ground_slam"),
                ("Projectiles/ShamanCurse", "curse_cast"), ("Vfx/CurseEruption", "curse_eruption"),
                ("Vfx/DashTelegraph", "dash_charge"), ("Vfx/WellSplash", "well_heal"),
            };
            foreach (var (prefab, sound) in spawnSounds)
                EditPrefab($"{PrefabsFolder}/{prefab}.prefab", root =>
                {
                    var emitter = root.GetComponent<SpawnSound>() ?? root.AddComponent<SpawnSound>();
                    SerializedSetter.For(emitter).Set("sound", s[sound]).Apply();
                });

            // The chest sparkle is shared by chests appearing and opening and shards being collected, so those
            // sounds live on the components instead (the spawner's is set with the scene).
            EditPrefab($"{PrefabsFolder}/Interactables/TreasureChest.prefab",
                root => SerializedSetter.For(root.GetComponent<TreasureChest>()).Set("openSound", s["chest_open"]).Apply());
            EditPrefab($"{PrefabsFolder}/Missions/ArcaneShard.prefab",
                root => SerializedSetter.For(root.GetComponent<MissionShard>()).Set("collectSound", s["shard_pickup"]).Apply());
            EditPrefab($"{PrefabsFolder}/Pickups/ExperienceGem.prefab",
                root => SerializedSetter.For(root.GetComponent<ExperienceGem>()).Set("collectSound", s["gem_pickup"]).Apply());

            // The magical charge-up swell, for enemies whose wind-up is a spell or a fuse. Archers and the boar have
            // their own launch sounds, and a magic swell would misdescribe a bow being drawn.
            foreach (var enemy in new[] { "ArcaneWisp", "DarkShaman", "OgreWarlord", "BombBeetle" })
                EditPrefab($"{EnemyPrefabsFolder}/{enemy}.prefab", root =>
                {
                    foreach (var attack in root.GetComponents<TelegraphedAttack>())
                        SerializedSetter.For(attack).Set("windupSound", s["enemy_windup"]).Apply();
                });
        }

        /// <summary>Adds the audio manager, the event-driven game sounds and the UI click sounds to the scene.</summary>
        private static void BuildSceneAudio(SoundSet s, Health playerHealth)
        {
            var audioObject = new GameObject("Audio");
            var manager = audioObject.AddComponent<AudioManager>();
            SerializedSetter.For(manager)
                // Starting levels for a new player; saved settings override them from then on.
                .Set("sfxVolume", 0.5f)
                .Set("musicVolume", 0.25f)
                .Set("music", LoadMusic(MatchMusic))
                .Set("uiClick", s["ui_click"])
                .Set("uiHover", s["ui_hover"])
                .Apply();

            var gameAudio = audioObject.AddComponent<GameAudio>();
            SerializedSetter.For(gameAudio)
                .Set("playerHealth", playerHealth)
                .Set("levelUpChoices", Object.FindFirstObjectByType<LevelUpController>())
                .Set("enemyHit", s["enemy_hit"])
                .Set("enemyDeath", s["enemy_death"])
                .Set("playerHurt", s["player_hurt"])
                .Set("playerDeath", s["player_death"])
                .Set("levelUp", s["level_up"])
                .Set("upgradePicked", s["upgrade_pick"])
                .Set("missionStarted", s["mission_start"])
                .Set("missionCompleted", s["mission_complete"])
                .Set("missionFailed", s["mission_failed"])
                .Set("bossAppears", s["boss_roar"])
                .Set("bossMusic", LoadMusic(BossMusic))
                .Set("victory", s["victory"])
                .Set("defeat", s["defeat"])
                .Apply();

            Object.FindFirstObjectByType<UnityEngine.UIElements.UIDocument>().gameObject.AddComponent<UIClickSound>();
            SerializedSetter.For(Object.FindFirstObjectByType<ChestSpawner>()).Set("spawnSound", s["chest_appear"]).Apply();
        }

        private static void Sound(SoundSet set, string name, float volume, float retrigger,
            float minPitch = 0.95f, float maxPitch = 1.05f, string clip = null)
        {
            var sound = LoadOrCreateAsset<SoundEvent>($"{SoundEventsFolder}/SFX_{ToPascal(name)}.asset");
            SerializedSetter.For(sound)
                .Set("clips", LoadSoundClips(clip ?? name).Cast<Object>().ToArray())
                .Set("volume", volume)
                .Set("minRetriggerInterval", retrigger)
                .Set("pitchRange", new Vector2(minPitch, maxPitch))
                .Apply();
            EditorUtility.SetDirty(sound);
            set.Add(name, sound);
        }

        /// <summary>The takes of one sound, imported for the web: Vorbis, mono unless heard flat, decompressed up front (they are all short).</summary>
        private static AudioClip[] LoadSoundClips(string name)
        {
            var paths = Directory.GetFiles(SfxFolder, $"{name}_*.wav").Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();
            if (paths.Length == 0)
                Debug.LogWarning($"No takes found for sound '{name}' in {SfxFolder}");

            foreach (var path in paths)
            {
                if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
                    continue;

                var mono = !StereoClips.Contains(name);
                var settings = importer.defaultSampleSettings;
                if (importer.forceToMono == mono && settings.loadType == AudioClipLoadType.DecompressOnLoad &&
                    settings.compressionFormat == AudioCompressionFormat.Vorbis)
                    continue;

                importer.forceToMono = mono;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.6f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }

            return paths.Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(c => c != null).ToArray();
        }

        /// <summary>
        /// A music track imported for the web: stereo Vorbis kept compressed in memory (decompressed, a few minutes
        /// of music would cost tens of megabytes), loaded in the background so it never stalls the first frame.
        /// </summary>
        private static AudioClip LoadMusic(string path)
        {
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
            {
                Debug.LogWarning($"No music found at {path}");
                return null;
            }

            var settings = importer.defaultSampleSettings;
            if (importer.forceToMono || !importer.loadInBackground || settings.loadType != AudioClipLoadType.CompressedInMemory ||
                settings.compressionFormat != AudioCompressionFormat.Vorbis || !Mathf.Approximately(settings.quality, 0.5f))
            {
                importer.forceToMono = false;
                importer.loadInBackground = true;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.5f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void EditPrefab(string path, Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string ToPascal(string snake) =>
            string.Concat(snake.Split('_').Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
    }
}
