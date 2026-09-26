using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Player;
using BattleArena.Spells;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>
    /// Binds the UI Toolkit HUD to gameplay events. Labels are only rewritten when their value changes,
    /// and cooldown overlays use the panel scheduler only while a spell is recharging.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudController : MonoBehaviour
    {
        private const long CooldownRefreshMs = 50;
        private const long NotificationDurationMs = 2600;
        private const long LevelUpPulseMs = 250;

        [SerializeField] private GameManager gameManager;
        [SerializeField] private Health playerHealth;
        [SerializeField] private SpellCaster spellCaster;

        private VisualElement _hud;
        private Label _levelLabel;
        private VisualElement _healthFill;
        private Label _healthLabel;
        private VisualElement _xpFill;
        private Label _xpLabel;
        private Label _timerLabel;
        private Label _killsLabel;
        private VisualElement _bossPanel;
        private Label _bossName;
        private VisualElement _bossFill;
        private Label _notification;
        private Label _interactPrompt;
        private VisualElement _missionPanel;
        private Label _missionTitle;
        private Label _missionDescription;
        private Label _missionTimer;
        private Label _missionStatus;
        private VisualElement _missionFill;
        private int _shownMissionSeconds = -1;
        private SpellSlotView[] _spellSlots;

        private IVisualElementScheduledItem _cooldownTicker;
        private IVisualElementScheduledItem _notificationHide;
        private Health _bossHealth;
        private int _shownSeconds = -1;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            QueryElements(root);

            _cooldownTicker = root.schedule.Execute(RefreshCooldowns).Every(CooldownRefreshMs);
            _cooldownTicker.Pause();
            _notificationHide = _notification.schedule.Execute(() => _notification.RemoveFromClassList("visible"));

            playerHealth.Changed += OnPlayerHealthChanged;
            gameManager.KillsChanged += OnKillsChanged;
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
            EventBus<MatchTimeChangedEvent>.Subscribe(OnMatchTimeChanged);
            EventBus<ExperienceChangedEvent>.Subscribe(OnExperienceChanged);
            EventBus<PlayerLevelUpEvent>.Subscribe(OnLevelUp);
            EventBus<SpellEquippedEvent>.Subscribe(OnSpellEquipped);
            EventBus<SpellCastEvent>.Subscribe(OnSpellCast);
            EventBus<SpellUpgradedEvent>.Subscribe(OnSpellUpgraded);
            EventBus<BossSpawnedEvent>.Subscribe(OnBossSpawned);
            EventBus<InteractionTargetChangedEvent>.Subscribe(OnInteractionTargetChanged);
            EventBus<NotificationEvent>.Subscribe(OnNotification);
            EventBus<MissionStartedEvent>.Subscribe(OnMissionStarted);
            EventBus<MissionProgressEvent>.Subscribe(OnMissionProgress);
            EventBus<MissionEndedEvent>.Subscribe(OnMissionEnded);
        }

        // Start runs after every Awake, so the player's Health is initialised by now.
        private void Start() => OnPlayerHealthChanged(playerHealth);

        private void OnDisable()
        {
            playerHealth.Changed -= OnPlayerHealthChanged;
            gameManager.KillsChanged -= OnKillsChanged;
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            EventBus<MatchTimeChangedEvent>.Unsubscribe(OnMatchTimeChanged);
            EventBus<ExperienceChangedEvent>.Unsubscribe(OnExperienceChanged);
            EventBus<PlayerLevelUpEvent>.Unsubscribe(OnLevelUp);
            EventBus<SpellEquippedEvent>.Unsubscribe(OnSpellEquipped);
            EventBus<SpellCastEvent>.Unsubscribe(OnSpellCast);
            EventBus<SpellUpgradedEvent>.Unsubscribe(OnSpellUpgraded);
            EventBus<BossSpawnedEvent>.Unsubscribe(OnBossSpawned);
            EventBus<InteractionTargetChangedEvent>.Unsubscribe(OnInteractionTargetChanged);
            EventBus<NotificationEvent>.Unsubscribe(OnNotification);
            EventBus<MissionStartedEvent>.Unsubscribe(OnMissionStarted);
            EventBus<MissionProgressEvent>.Unsubscribe(OnMissionProgress);
            EventBus<MissionEndedEvent>.Unsubscribe(OnMissionEnded);
            UnbindBoss();
        }

        private void QueryElements(VisualElement root)
        {
            _hud = root.Q("hud");
            _levelLabel = root.Q<Label>("level-label");
            _healthFill = root.Q("health-fill");
            _healthLabel = root.Q<Label>("health-label");
            _xpFill = root.Q("xp-fill");
            _xpLabel = root.Q<Label>("xp-label");
            _timerLabel = root.Q<Label>("timer-label");
            _killsLabel = root.Q<Label>("kills-label");
            _bossPanel = root.Q("boss-panel");
            _bossName = root.Q<Label>("boss-name");
            _bossFill = root.Q("boss-fill");
            _notification = root.Q<Label>("notification");
            _interactPrompt = root.Q<Label>("interact-prompt");
            _missionPanel = root.Q("mission-panel");
            _missionTitle = root.Q<Label>("mission-title");
            _missionDescription = root.Q<Label>("mission-description");
            _missionTimer = root.Q<Label>("mission-timer");
            _missionStatus = root.Q<Label>("mission-status");
            _missionFill = root.Q("mission-fill");

            _spellSlots = new SpellSlotView[spellCaster.SlotCount];
            for (var i = 0; i < _spellSlots.Length; i++)
            {
                // Slots start empty; they are bound when a spell is picked on the level-up screen.
                _spellSlots[i] = new SpellSlotView(root.Q($"spell-{i}"), spellCaster.GetUnlockLevel(i));
            }
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            var showHud = evt.Current != GameState.MainMenu;
            _hud.EnableInClassList("hidden", !showHud);
            if (evt.Current == GameState.BossFight)
            {
                _timerLabel.text = "BOSS";
                _timerLabel.AddToClassList("boss-phase");
            }
        }

        private void OnMatchTimeChanged(MatchTimeChangedEvent evt)
        {
            var seconds = Mathf.CeilToInt(evt.Remaining);
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                _timerLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";
            }
        }

        private void OnKillsChanged(int kills) => _killsLabel.text = $"{kills} defeated";

        private void OnPlayerHealthChanged(Health health)
        {
            SetFill(_healthFill, health.Normalized);
            _healthLabel.text = $"{Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}";
        }

        private void OnExperienceChanged(ExperienceChangedEvent evt)
        {
            _levelLabel.text = evt.Level.ToString();
            SetFill(_xpFill, evt.Required > 0 ? (float)evt.Current / evt.Required : 1f);
            _xpLabel.text = $"{evt.Current} / {evt.Required} XP";
        }

        private void OnLevelUp(PlayerLevelUpEvent evt)
        {
            _levelLabel.AddToClassList("level-up");
            _levelLabel.schedule.Execute(() => _levelLabel.RemoveFromClassList("level-up")).StartingIn(LevelUpPulseMs);
        }

        private void OnSpellEquipped(SpellEquippedEvent evt) => _spellSlots[evt.SlotIndex].Bind(evt.Spell);

        private void OnSpellCast(SpellCastEvent evt)
        {
            _spellSlots[evt.SlotIndex].PulseCast();
            _cooldownTicker.Resume();
        }

        // Charge upgrades change the counter immediately, even without a cast.
        private void OnSpellUpgraded(SpellUpgradedEvent evt) => _cooldownTicker.Resume();

        private void RefreshCooldowns()
        {
            var anyRecharging = false;
            for (var i = 0; i < _spellSlots.Length; i++)
            {
                if (!spellCaster.IsUnlocked(i))
                    continue;

                _spellSlots[i].SetState(spellCaster.GetCharges(i), spellCaster.GetMaxCharges(i),
                    spellCaster.GetCooldownRemaining(i), spellCaster.GetCooldownDuration(i));
                anyRecharging |= spellCaster.IsRecharging(i);
            }

            if (!anyRecharging)
                _cooldownTicker.Pause();
        }

        private void OnBossSpawned(BossSpawnedEvent evt)
        {
            UnbindBoss();
            _bossHealth = evt.Health;
            _bossHealth.Changed += OnBossHealthChanged;
            _bossName.text = evt.DisplayName.ToUpperInvariant();
            _bossPanel.RemoveFromClassList("hidden");
            OnBossHealthChanged(_bossHealth);
        }

        private void OnBossHealthChanged(Health health) => SetFill(_bossFill, health.Normalized);

        private void UnbindBoss()
        {
            if (_bossHealth == null)
                return;
            _bossHealth.Changed -= OnBossHealthChanged;
            _bossHealth = null;
        }

        private void OnInteractionTargetChanged(InteractionTargetChangedEvent evt)
        {
            var target = evt.Target;
            _interactPrompt.EnableInClassList("hidden", target == null);
            if (target != null)
                _interactPrompt.text = $"[F]  {target.Prompt}";
        }

        private void OnNotification(NotificationEvent evt) => ShowNotification(evt.Message);

        private void OnMissionStarted(MissionStartedEvent evt)
        {
            _missionTitle.text = evt.Title;
            _missionDescription.text = evt.Description;
            _shownMissionSeconds = -1;
            _missionPanel.RemoveFromClassList("hidden");
        }

        private void OnMissionProgress(MissionProgressEvent evt)
        {
            SetFill(_missionFill, evt.Progress);
            // Missions cache their status strings, so a reference check avoids redundant text updates.
            if (!ReferenceEquals(_missionStatus.text, evt.Status))
                _missionStatus.text = evt.Status;

            var seconds = Mathf.CeilToInt(evt.TimeRemaining);
            if (seconds == _shownMissionSeconds)
                return;
            _shownMissionSeconds = seconds;
            _missionTimer.text = $"{seconds / 60}:{seconds % 60:00}";
        }

        private void OnMissionEnded(MissionEndedEvent _) => _missionPanel.AddToClassList("hidden");

        private void ShowNotification(string message)
        {
            _notification.text = message;
            _notification.AddToClassList("visible");
            _notificationHide.ExecuteLater(NotificationDurationMs);
        }

        private static void SetFill(VisualElement fill, float normalized) =>
            fill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);

        /// <summary>View wrapper for one Q/E/R slot.</summary>
        private sealed class SpellSlotView
        {
            private readonly VisualElement _root;
            private readonly Label _name;
            private readonly VisualElement _cooldownOverlay;
            private readonly Label _cooldownText;
            private readonly Label _chargesText;
            private int _shownSeconds = -1;
            private int _shownCharges = -1;

            public SpellSlotView(VisualElement root, int unlockLevel)
            {
                _root = root;
                _name = root.Q<Label>(className: "spell-name");
                _cooldownOverlay = root.Q(className: "spell-cooldown");
                _cooldownText = root.Q<Label>(className: "spell-cooldown-text");
                _chargesText = root.Q<Label>(className: "spell-charges");
                root.Q<Label>(className: "spell-lock").text = $"LV {unlockLevel}";
            }

            public void Bind(SpellDefinition spell)
            {
                _root.EnableInClassList("locked", spell == null);
                if (spell == null)
                    return;
                _name.text = spell.DisplayName;
                _name.style.color = spell.Color;
            }

            public void PulseCast()
            {
                _root.AddToClassList("cast");
                _root.schedule.Execute(() => _root.RemoveFromClassList("cast")).StartingIn(120);
            }

            /// <summary>
            /// The cooldown overlay covers the slot only when no charge is left; with charges in stock
            /// the slot stays usable and just shows the count.
            /// </summary>
            public void SetState(int charges, int maxCharges, float remaining, float total)
            {
                var shownCharges = maxCharges > 1 ? charges : -1;
                if (shownCharges != _shownCharges)
                {
                    _shownCharges = shownCharges;
                    _chargesText.text = shownCharges >= 0 ? shownCharges.ToString() : string.Empty;
                }

                var empty = charges <= 0;
                var normalized = empty && total > 0f ? remaining / total : 0f;
                _cooldownOverlay.style.height = Length.Percent(normalized * 100f);
                _root.EnableInClassList("cooling", empty);

                var seconds = empty ? Mathf.CeilToInt(remaining) : 0;
                if (seconds == _shownSeconds)
                    return;
                _shownSeconds = seconds;
                _cooldownText.text = seconds > 0 ? seconds.ToString() : string.Empty;
            }
        }
    }
}
