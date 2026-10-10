using BattleArena.Spells;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.Progression
{
    /// <summary>
    /// One card on the level-up screen. Concrete offers know how to present and apply themselves, so the
    /// view stays a dumb binder.
    /// </summary>
    public abstract class LevelUpOffer
    {
        /// <summary>The card's name.</summary>
        public abstract string Title { get; }

        /// <summary>Short upper-case label on the card badge, e.g. "RARE" or "FIREBALL · Q"; null hides the badge.</summary>
        public abstract string Badge { get; }

        /// <summary>Accent colour of the card.</summary>
        public abstract Color Color { get; }

        /// <summary>Background pattern of the card.</summary>
        public abstract CardMotif Motif { get; }

        /// <summary>Line icon in the card's emblem.</summary>
        public abstract VectorImage Icon { get; }

        /// <summary>Optional line(s) under the title.</summary>
        public virtual string Summary => null;

        /// <summary>The stat the card changes, with its before/after values and comparison bar.</summary>
        public abstract StatPreview Stat { get; }

        /// <summary>For a spell card, which shared stats change the spell; null hides the row.</summary>
        public virtual SpellStats? Scaling => null;

        /// <summary>For a spell upgrade card, the spell it improves (named in the card's header); otherwise null.</summary>
        public virtual SpellDefinition UpgradedSpell => null;

        /// <summary>Grants what the card offers.</summary>
        public abstract void Apply();
    }
}
