using BattleArena.Combat;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Shows a frost slow while it lasts: an icy fresnel rim drawn over the body through <see cref="DamageFlash"/>,
    /// so hit flashes still work on a chilled enemy.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyChillVisual : MonoBehaviour
    {
        [SerializeField] private DamageFlash flash;
        [SerializeField] private Material frostOverlay;

        private Enemy _enemy;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _enemy.ChillChanged += OnChillChanged;
        }

        private void OnDestroy() => _enemy.ChillChanged -= OnChillChanged;

        private void OnChillChanged(bool chilled) => flash.SetOverlay(chilled ? frostOverlay : null);
    }
}
