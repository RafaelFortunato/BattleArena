using System.Collections.Generic;

namespace BattleArena.Progression
{
    /// <summary>What the card screen should show.</summary>
    public readonly struct LevelUpPresentation
    {
        /// <summary>Why the choice is shown.</summary>
        public readonly ChoiceKind Kind;

        /// <summary>Small label above the title, e.g. "LEVEL 8" or the completed mission's name.</summary>
        public readonly string Chip;

        /// <summary>The screen's headline, e.g. "LEVEL UP!".</summary>
        public readonly string Title;

        /// <summary>The instruction under the headline, e.g. "Choose an upgrade".</summary>
        public readonly string Subtitle;

        /// <summary>The cards to choose from.</summary>
        public readonly IReadOnlyList<LevelUpOffer> Offers;

        /// <summary>Creates a presentation.</summary>
        public LevelUpPresentation(ChoiceKind kind, string chip, string title, string subtitle, IReadOnlyList<LevelUpOffer> offers)
        {
            Kind = kind;
            Chip = chip;
            Title = title;
            Subtitle = subtitle;
            Offers = offers;
        }
    }
}
