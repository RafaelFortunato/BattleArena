using System.Globalization;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Player;
using BattleArena.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>
    /// Binds the UI Toolkit HUD to gameplay events. Labels are only rewritten when their value changes,
    /// and cooldown sweeps use the panel scheduler only while a spell is recharging.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudController : MonoBehaviour
    {
        private const long CooldownRefreshMs = 50;
        private const long AnnouncementDurationMs = 2600;
        private const long LevelUpPulseMs = 250;

        [SerializeField] private GameManager gameManager;
        [SerializeField] private Health playerHealth;
        [SerializeField] private SpellCaster spellCaster;

        private VisualElement _hud;
        private MatchTimelineView _timeline;
        private SpellSlotView[] _spellSlots;

        private VisualElement _levelBadge;
        private Label _levelLabel;
        private RadialProgressElement _levelRing;
        private VisualElement _xpFill;
        private VisualElement _healthFill;
        private VisualElement _healthTrail;
        private Label _healthLabel;

        private VisualElement _bossPanel;
        private Label _bossName;
        private VisualElement _bossFill;
        private VisualElement _bossTrail;
        private Label _bossHealthLabel;

        private VisualElement _missionPanel;
        private Label _missionTitle;
        private Label _missionDescription;
        private Label _missionTimer;
        private Label _missionStatus;
        private VisualElement _missionFill;
        private int _shownMissionSeconds = -1;

        private VisualElement _announcement;
        private Label _announcementChip;
        private Label _announcementText;
        private VisualElement _interactPrompt;
        private Label _interactLabel;

        private IVisualElementScheduledItem _cooldownTicker;
        private IVisualElementScheduledItem _announcementHide;
        private IVisualElementScheduledItem _levelPulseEnd;
        private Health _bossHealth;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            QueryElements(root);

            _cooldownTicker = root.schedule.Execute(RefreshCooldowns).Every(CooldownRefreshMs);
            _cooldownTicker.Pause();
            _announcementHide = _announcement.schedule.Execute(() => _announcement.RemoveFromClassList("visible"));
            _announcementHide.Pause();
            _levelPulseEnd = _levelBadge.schedule.Execute(() => _levelBadge.RemoveFromClassList("level-up"));
            _levelPulseEnd.Pause();

            playerHealth.Changed += OnPlayerHealthChanged;
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
            _timeline = new MatchTimelineView(root);

            _levelBadge = root.Q("level-badge");
            _levelLabel = root.Q<Label>("level-label");
            _levelRing = root.Q<RadialProgressElement>("level-ring");
            _xpFill = root.Q("xp-fill");
            _healthFill = root.Q("health-fill");
            _healthTrail = root.Q("health-trail");
            _healthLabel = root.Q<Label>("health-label");

            _bossPanel = root.Q("boss-panel");
            _bossName = root.Q<Label>("boss-name");
            _bossFill = root.Q("boss-fill");
            _bossTrail = root.Q("boss-trail");
            _bossHealthLabel = root.Q<Label>("boss-health-label");

            _missionPanel = root.Q("mission-panel");
            _missionTitle = root.Q<Label>("mission-title");
            _missionDescription = root.Q<Label>("mission-description");
            _missionTimer = root.Q<Label>("mission-timer");
            _missionStatus = root.Q<Label>("mission-status");
            _missionFill = root.Q("mission-fill");

            _announcement = root.Q("announcement");
            _announcementChip = root.Q<Label>("announcement-chip");
            _announcementText = root.Q<Label>("announcement-text");
            _interactPrompt = root.Q("interact-prompt");
            _interactLabel = root.Q<Label>("interact-label");

            _spellSlots = new SpellSlotView[spellCaster.SlotCount];
            for (var i = 0; i < _spellSlots.Length; i++)
            {
                // Slots start locked; they are bound when a spell is picked on the card screen.
                _spellSlots[i] = new SpellSlotView(root.Q($"spell-{i}"), spellCaster.GetUnlockLevel(i));
            }
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            // Only shown during combat; card, pause and end screens get the screen to themselves.
            _hud.EnableInClassList("hidden", !evt.Current.IsCombat());
            if (evt.Current == GameState.BossFight)
                _timeline.SetVisible(false);
        }

        private void OnMatchTimeChanged(MatchTimeChangedEvent evt) => _timeline.SetTime(evt.Elapsed, evt.Remaining);

        private void OnPlayerHealthChanged(Health health)
        {
            // The pale trail catches up after a short delay (USS transition), showing the chunk just lost.
            SetFill(_healthFill, health.Normalized);
            SetFill(_healthTrail, health.Normalized);
            _healthLabel.text = $"{Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}";
        }

        private void OnExperienceChanged(ExperienceChangedEvent evt)
        {
            _levelLabel.text = evt.Level.ToString();
            SetFill(_xpFill, evt.Required > 0 ? (float)evt.Current / evt.Required : 1f);
            // Whole levels only, so this only changes value (and repaints) on a level-up.
            _levelRing.Value = evt.ProgressToMaxLevel;
        }

        private void OnLevelUp(PlayerLevelUpEvent evt)
        {
            _levelBadge.AddToClassList("level-up");
            _levelPulseEnd.ExecuteLater(LevelUpPulseMs);
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

        // ---------------------------------------------------------------- Boss

        private void OnBossSpawned(BossSpawnedEvent evt)
        {
            UnbindBoss();
            _bossHealth = evt.Health;
            _bossHealth.Changed += OnBossHealthChanged;
            _bossName.text = evt.DisplayName.ToUpperInvariant();
            _timeline.SetVisible(false);
            _bossPanel.RemoveFromClassList("hidden");
            OnBossHealthChanged(_bossHealth);
        }

        private void OnBossHealthChanged(Health health)
        {
            // The pale trail catches up after a short delay (USS transition), showing the chunk just lost.
            SetFill(_bossFill, health.Normalized);
            SetFill(_bossTrail, health.Normalized);
            _bossHealthLabel.text = string.Format(CultureInfo.InvariantCulture, "{0:N0} / {1:N0}",
                Mathf.CeilToInt(health.Current), Mathf.CeilToInt(health.Max));
        }

        private void UnbindBoss()
        {
            if (_bossHealth == null)
                return;
            _bossHealth.Changed -= OnBossHealthChanged;
            _bossHealth = null;
        }

        // ---------------------------------------------------------------- Interaction and announcements

        private void OnInteractionTargetChanged(InteractionTargetChangedEvent evt)
        {
            var target = evt.Target;
            _interactPrompt.EnableInClassList("hidden", target == null);
            if (target != null)
                _interactLabel.text = target.Prompt;
        }

        private void OnNotification(NotificationEvent evt) => Announce(null, evt.Message);

        private void Announce(string chip, string text)
        {
            _announcement.EnableInClassList("plain", chip == null);
            if (chip != null)
                _announcementChip.text = chip;
            _announcementText.text = text;
            _announcement.AddToClassList("visible");
            _announcementHide.ExecuteLater(AnnouncementDurationMs);
        }

        // ---------------------------------------------------------------- Missions

        private void OnMissionStarted(MissionStartedEvent evt)
        {
            _missionTitle.text = evt.Title;
            _missionDescription.text = evt.Description;
            _shownMissionSeconds = -1;
            _missionPanel.RemoveFromClassList("hidden");
            _timeline.AddMissionMarker();
            Announce("NEW MISSION", evt.Title);
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

        private void OnMissionEnded(MissionEndedEvent evt)
        {
            _missionPanel.AddToClassList("hidden");
            _timeline.CompleteMissionMarker(evt.Succeeded);
        }

        private static void SetFill(VisualElement fill, float normalized) =>
            fill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
    }
}
