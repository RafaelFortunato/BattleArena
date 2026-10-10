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

        /// <summary>
        /// Finds the HUD elements, sets up the scheduled refreshes and subscribes to every event the HUD shows.
        /// </summary>
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

        /// <summary>
        /// Shows the player's starting health. Start runs after every Awake, so the player's Health is initialised by
        /// now.
        /// </summary>
        private void Start() => OnPlayerHealthChanged(playerHealth);

        /// <summary>Removes every subscription.</summary>
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

        /// <summary>
        /// Looks up the HUD's elements and creates the spell slots, which start locked until a spell is picked.
        /// </summary>
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
                _spellSlots[i] = new SpellSlotView(root.Q($"spell-{i}"), spellCaster.GetUnlockLevel(i));
        }

        /// <summary>
        /// Shows the HUD only during combat (card, pause and end screens get the screen to themselves) and hides the
        /// timeline in the boss fight.
        /// </summary>
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            _hud.EnableInClassList("hidden", !evt.Current.IsCombat());
            if (evt.Current == GameState.BossFight)
                _timeline.SetVisible(false);
        }

        /// <summary>Advances the match timeline.</summary>
        private void OnMatchTimeChanged(MatchTimeChangedEvent evt) => _timeline.SetTime(evt.Elapsed, evt.Remaining);

        /// <summary>
        /// Updates the health bar and label. The pale trail catches up after a short delay (USS transition), showing
        /// the chunk just lost.
        /// </summary>
        private void OnPlayerHealthChanged(Health health)
        {
            SetFill(_healthFill, health.Normalized);
            SetFill(_healthTrail, health.Normalized);
            _healthLabel.text = $"{Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}";
        }

        /// <summary>Updates the level badge, the XP bar and the badge ring, which moves on level-ups only.</summary>
        private void OnExperienceChanged(ExperienceChangedEvent evt)
        {
            _levelLabel.text = evt.Level.ToString();
            SetFill(_xpFill, evt.Required > 0 ? (float)evt.Current / evt.Required : 1f);
            // Whole levels only, so this only changes value (and repaints) on a level-up.
            _levelRing.Value = evt.ProgressToMaxLevel;
        }

        /// <summary>Pulses the level badge.</summary>
        private void OnLevelUp(PlayerLevelUpEvent evt)
        {
            _levelBadge.AddToClassList("level-up");
            _levelPulseEnd.ExecuteLater(LevelUpPulseMs);
        }

        /// <summary>Shows the new spell in its slot.</summary>
        private void OnSpellEquipped(SpellEquippedEvent evt) => _spellSlots[evt.SlotIndex].Bind(evt.Spell);

        /// <summary>Pulses the slot and starts refreshing cooldowns.</summary>
        private void OnSpellCast(SpellCastEvent evt)
        {
            _spellSlots[evt.SlotIndex].PulseCast();
            _cooldownTicker.Resume();
        }

        /// <summary>Refreshes the slots: charge upgrades change the counter immediately, even without a cast.</summary>
        private void OnSpellUpgraded(SpellUpgradedEvent evt) => _cooldownTicker.Resume();

        /// <summary>
        /// Updates every unlocked slot's charges and cooldown sweep; stops refreshing once nothing is recharging.
        /// </summary>
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

        /// <summary>Shows the boss panel bound to the boss's health and hides the timeline.</summary>
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

        /// <summary>
        /// Updates the boss health bar and label. The pale trail catches up after a short delay (USS transition),
        /// showing the chunk just lost.
        /// </summary>
        private void OnBossHealthChanged(Health health)
        {
            SetFill(_bossFill, health.Normalized);
            SetFill(_bossTrail, health.Normalized);
            _bossHealthLabel.text = string.Format(CultureInfo.InvariantCulture, "{0:N0} / {1:N0}",
                Mathf.CeilToInt(health.Current), Mathf.CeilToInt(health.Max));
        }

        /// <summary>Stops following the boss's health.</summary>
        private void UnbindBoss()
        {
            if (_bossHealth == null)
                return;
            _bossHealth.Changed -= OnBossHealthChanged;
            _bossHealth = null;
        }

        // ---------------------------------------------------------------- Interaction and announcements

        /// <summary>Shows the interaction prompt with the target's verb, or hides it.</summary>
        private void OnInteractionTargetChanged(InteractionTargetChangedEvent evt)
        {
            var target = evt.Target;
            _interactPrompt.EnableInClassList("hidden", target == null);
            if (target != null)
                _interactLabel.text = target.Prompt;
        }

        /// <summary>Shows a plain announcement.</summary>
        private void OnNotification(NotificationEvent evt) => Announce(null, evt.Message);

        /// <summary>
        /// Shows an announcement, with a chip label when <paramref name="chip"/> is set, and schedules it to fade.
        /// </summary>
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

        /// <summary>Shows the mission panel, adds a timeline marker and announces the mission.</summary>
        private void OnMissionStarted(MissionStartedEvent evt)
        {
            _missionTitle.text = evt.Title;
            _missionDescription.text = evt.Description;
            _shownMissionSeconds = -1;
            _missionPanel.RemoveFromClassList("hidden");
            _timeline.AddMissionMarker();
            Announce("NEW MISSION", evt.Title);
        }

        /// <summary>Updates the mission bar, status and countdown, rewriting text only when it changed.</summary>
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

        /// <summary>Hides the mission panel and marks the timeline with the result.</summary>
        private void OnMissionEnded(MissionEndedEvent evt)
        {
            _missionPanel.AddToClassList("hidden");
            _timeline.CompleteMissionMarker(evt.Succeeded);
        }

        /// <summary>Sets a bar's fill width as a percentage of its track.</summary>
        private static void SetFill(VisualElement fill, float normalized) =>
            fill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
    }
}
