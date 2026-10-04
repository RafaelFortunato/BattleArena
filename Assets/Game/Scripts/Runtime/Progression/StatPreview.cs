namespace BattleArena.Progression
{
    public enum StatBarStyle
    {
        None,
        /// <summary>One bar: current value, then the gain on top of it.</summary>
        Continuous,
        /// <summary>One segment per step (e.g. projectiles or upgrade ranks).</summary>
        Segmented
    }

    /// <summary>How a card presents the stat it changes: "Critical chance · 5% → 15%" plus a comparison bar.</summary>
    public readonly struct StatPreview
    {
        public readonly string Label;
        /// <summary>Value after taking the card.</summary>
        public readonly string Value;
        public readonly string Current;
        public readonly string Gain;
        public readonly StatBarStyle Bar;
        /// <summary>0..1 fills for continuous bars; filled segments / <see cref="Segments"/> for segmented ones.</summary>
        public readonly float CurrentFill;
        public readonly float AfterFill;
        public readonly int Segments;
        /// <summary>Extra line under a preview without a bar, e.g. a new spell's damage in numbers and its cooldown.</summary>
        public readonly string Note;

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
