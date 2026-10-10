using BattleArena.Combat;
using UnityEngine;

namespace BattleArena.Spells
{
    /// <summary>What a spell may ask of whoever casts it. Keeps spells independent of player classes.</summary>
    public interface ISpellCaster
    {
        /// <summary>The caster's transform, for positions and following effects.</summary>
        Transform Transform { get; }

        /// <summary>The caster's health, for spells that protect the caster.</summary>
        Health Health { get; }

        /// <summary>Planar direction the caster is facing.</summary>
        Vector3 Facing { get; }

        /// <summary>Moves the caster instantly to <paramref name="position"/>.</summary>
        void Teleport(Vector3 position);
    }
}
