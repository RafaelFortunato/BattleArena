using System.Collections.Generic;
using UnityEngine;

namespace BattleArena.Core
{
    /// <summary>Reuses yield instructions so coroutines don't allocate a new WaitForSeconds every iteration.</summary>
    public static class WaitCache
    {
        private static readonly Dictionary<float, WaitForSeconds> Seconds = new();

        /// <summary>
        /// A shared <see cref="WaitForSeconds"/> for <paramref name="seconds"/>, created on first use.
        /// </summary>
        public static WaitForSeconds Get(float seconds)
        {
            if (!Seconds.TryGetValue(seconds, out var wait))
            {
                wait = new WaitForSeconds(seconds);
                Seconds.Add(seconds, wait);
            }
            return wait;
        }
    }
}
