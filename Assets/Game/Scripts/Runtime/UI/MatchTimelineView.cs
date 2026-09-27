using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>
    /// The countdown plate and the track from match start to the boss. Missions leave a marker where they
    /// started, which turns into a tick when completed.
    /// </summary>
    internal sealed class MatchTimelineView
    {
        private readonly VisualElement _root;
        private readonly Label _timer;
        private readonly VisualElement _fill;
        private readonly VisualElement _markers;
        private readonly VisualElement _playhead;
        private VisualElement _activeMarker;
        private float _progress;
        private int _shownSeconds = -1;

        public MatchTimelineView(VisualElement root)
        {
            _root = root.Q("match-timer");
            _timer = root.Q<Label>("timer-label");
            _fill = root.Q("timeline-fill");
            _markers = root.Q("timeline-markers");
            _playhead = root.Q("timeline-playhead");
        }

        public void SetVisible(bool visible) => _root.EnableInClassList("hidden", !visible);

        public void SetTime(float elapsed, float remaining)
        {
            var seconds = Mathf.CeilToInt(remaining);
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                _timer.text = $"{seconds / 60:00}:{seconds % 60:00}";
            }

            var total = elapsed + remaining;
            _progress = total > 0f ? Mathf.Clamp01(elapsed / total) : 0f;
            var percent = Length.Percent(_progress * 100f);
            _fill.style.width = percent;
            _playhead.style.left = percent;
        }

        public void AddMissionMarker()
        {
            var marker = new VisualElement { pickingMode = PickingMode.Ignore };
            marker.AddToClassList("timeline-marker");
            marker.style.left = Length.Percent(_progress * 100f);

            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("icon");
            icon.AddToClassList("icon-exclamation");
            marker.Add(icon);

            // Keep the playhead drawn on top of every marker.
            _markers.Insert(_markers.IndexOf(_playhead), marker);
            _activeMarker = marker;
        }

        public void CompleteMissionMarker(bool succeeded)
        {
            if (_activeMarker == null)
                return;

            var icon = _activeMarker[0];
            icon.RemoveFromClassList("icon-exclamation");
            icon.AddToClassList(succeeded ? "icon-check" : "icon-exclamation");
            _activeMarker.EnableInClassList("failed", !succeeded);
            _activeMarker = null;
        }
    }
}
