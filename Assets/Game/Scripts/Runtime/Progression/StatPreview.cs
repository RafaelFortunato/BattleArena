namespace BattleArena.Progression
{
    /// <summary>How a card presents the stat it changes: "Critical chance · 5% → 15%" plus a comparison bar.</summary>
    public readonly struct StatPreview
    {
        /// <summary>The stat's name.</summary>
        public readonly string Label;

        /// <summary>Value after taking the card.</summary>
        public readonly string Value;

        /// <summary>Value before taking the card.</summary>
        public readonly string Current;

        /// <summary>The change, e.g. "+10%".</summary>
        public readonly string Gain;

        /// <summary>Which comparison bar to draw.</summary>
        public readonly StatBarStyle Bar;

        /// <summary>0..1 fills for continuous bars; filled segments / <see cref="Segments"/> for segmented ones.</summary>
        public readonly float CurrentFill;

        /// <summary>Fill after taking the card, in the same units as <see cref="CurrentFill"/>.</summary>
        public readonly float AfterFill;

        /// <summary>Number of segments of a segmented bar.</summary>
        public readonly int Segments;

        /// <summary>Extra line under a preview without a bar, e.g. a new spell's damage in numbers and its cooldown.</summary>
        public readonly string Note;

        /// <summary>Creates a preview with a comparison bar.</summary>
        public StatPreview(string label, string value, string current, string gain, StatBarStyle bar,
            float currentFill, float afterFill, int segments = 0, string note = null)
        {
            Note = note;
            Label = label;
            Value = value;
            Current = current;
            Gain = gain;
            Bar = bar;
            CurrentFill = currentFill;
            AfterFill = afterFill;
            Segments = segments;
        }

        /// <summary>A label and value with no comparison bar, e.g. a new spell's damage, plus an optional note line.</summary>
        public static StatPreview Plain(string label, string value, string note = null) =>
            new(label, value, null, null, StatBarStyle.None, 0f, 0f, note: note);
    }
}
