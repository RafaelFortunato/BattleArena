using System.Collections.Generic;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>Live enemies, with O(1) add/remove. Iterated by <see cref="EnemyDirector"/>.</summary>
    public static class EnemyRegistry
    {
        private static readonly List<Enemy> ActiveEnemies = new(256);

        public static IReadOnlyList<Enemy> Active => ActiveEnemies;
        public static int Count => ActiveEnemies.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => ActiveEnemies.Clear();

        internal static void Register(Enemy enemy)
        {
            if (enemy.RegistryIndex >= 0)
                return;

            enemy.RegistryIndex = ActiveEnemies.Count;
            ActiveEnemies.Add(enemy);
        }

        internal static void Unregister(Enemy enemy)
        {
            var index = enemy.RegistryIndex;
            if (index < 0)
                return;

            var lastIndex = ActiveEnemies.Count - 1;
            var last = ActiveEnemies[lastIndex];
            ActiveEnemies[index] = last;
            last.RegistryIndex = index;
            ActiveEnemies.RemoveAt(lastIndex);
            enemy.RegistryIndex = -1;
        }
    }
}
