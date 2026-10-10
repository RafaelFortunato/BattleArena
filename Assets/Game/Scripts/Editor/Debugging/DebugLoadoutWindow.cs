using BattleArena.Core;
using BattleArena.Player;
using BattleArena.Spells;
using UnityEditor;
using UnityEngine;

namespace BattleArena.Editor.Debugging
{
    /// <summary>
    /// Editor-only test loadout: pick spells for Q / E / R and start a match with them already equipped, optionally
    /// skipping the main menu. Choices persist in EditorPrefs; nothing here ships in a build.
    /// Open from <c>BattleArena &gt; Debug Loadout</c>.
    /// </summary>
    public sealed class DebugLoadoutWindow : EditorWindow
    {
        private const string SlotKey = "BattleArena.DebugLoadout.Slot";
        private const string EnabledKey = "BattleArena.DebugLoadout.Enabled";
        private const string SkipMenuKey = "BattleArena.DebugLoadout.SkipMenu";
        private static readonly string[] SlotNames = { "Q", "E", "R" };

        /// <summary>Opens the window.</summary>
        [MenuItem("BattleArena/Debug Loadout")]
        private static void Open() => GetWindow<DebugLoadoutWindow>("Debug Loadout");

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, false);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        private static bool SkipMenu
        {
            get => EditorPrefs.GetBool(SkipMenuKey, true);
            set => EditorPrefs.SetBool(SkipMenuKey, value);
        }

        /// <summary>The spell saved for <paramref name="slot"/>, or null.</summary>
        private static SpellDefinition GetSlot(int slot)
        {
            var path = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(SlotKey + slot, string.Empty));
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SpellDefinition>(path);
        }

        /// <summary>Saves the spell for <paramref name="slot"/>.</summary>
        private static void SetSlot(int slot, SpellDefinition spell)
        {
            var guid = spell != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(spell)) : string.Empty;
            EditorPrefs.SetString(SlotKey + slot, guid);
        }

        /// <summary>Draws the slot pickers, the options and the play and equip buttons.</summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Spells to start the match with", EditorStyles.boldLabel);
            for (var slot = 0; slot < SlotNames.Length; slot++)
            {
                var current = GetSlot(slot);
                var picked = (SpellDefinition)EditorGUILayout.ObjectField(SlotNames[slot], current, typeof(SpellDefinition), false);
                if (picked != current)
                    SetSlot(slot, picked);
            }

            EditorGUILayout.Space();
            SkipMenu = EditorGUILayout.Toggle(new GUIContent("Skip main menu", "Start the match as soon as Play is pressed."), SkipMenu);
            Enabled = EditorGUILayout.Toggle(new GUIContent("Apply on every Play", "Also use this loadout when Play is pressed from the toolbar."), Enabled);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Play with this loadout", GUILayout.Height(28)))
                {
                    SessionState.SetBool(EnabledKey, true);
                    EditorApplication.isPlaying = true;
                }
            }

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Equip now (running game)"))
                    EquipLoadout();
            }

            EditorGUILayout.HelpBox("Empty slots stay as they are and unlock normally at levels 5, 10 and 15.", MessageType.None);
        }

        // ---------------------------------------------------------------- Play mode hook

        /// <summary>Hooks play mode changes when the editor loads.</summary>
        [InitializeOnLoadMethod]
        private static void Register() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private static bool _subscribed;

        /// <summary>
        /// Arms the loadout for this run when entering play mode (the window's button arms a single run, the toggle
        /// every run) and skips the menu if asked.
        /// </summary>
        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    var armed = Enabled || SessionState.GetBool(EnabledKey, false);
                    SessionState.SetBool(EnabledKey, false);
                    if (!armed)
                        return;

                    EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
                    _subscribed = true;
                    if (SkipMenu)
                        EditorApplication.delayCall += StartMatch;
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    if (_subscribed)
                        EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
                    _subscribed = false;
                    break;
            }
        }

        /// <summary>Starts the match from the main menu.</summary>
        private static void StartMatch()
        {
            var gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager != null && gameManager.State == GameState.MainMenu)
                gameManager.StartMatch();
        }

        /// <summary>Equips the loadout as the match starts.</summary>
        private static void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.Previous != GameState.MainMenu || evt.Current != GameState.Playing)
                return;
            EquipLoadout();
        }

        /// <summary>Puts the saved spells in their slots; empty slots are left alone.</summary>
        private static void EquipLoadout()
        {
            var caster = Object.FindAnyObjectByType<SpellCaster>();
            if (caster == null)
                return;

            for (var slot = 0; slot < SlotNames.Length && slot < caster.SlotCount; slot++)
            {
                var spell = GetSlot(slot);
                if (spell != null)
                    caster.Equip(slot, spell);
            }
        }
    }
}
