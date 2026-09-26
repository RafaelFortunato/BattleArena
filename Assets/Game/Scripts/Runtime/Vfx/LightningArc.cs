using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>Pooled jagged line between a series of points. Paired with <see cref="TimedRelease"/> for its lifetime.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LightningArc : MonoBehaviour
    {
        [SerializeField, Min(0)] private int segmentsPerLink = 4;
        [SerializeField, Min(0f)] private float jitter = 0.45f;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
        }

        /// <summary>Draws the arc through the first <paramref name="count"/> points.</summary>
        public void Show(Vector3[] points, int count)
        {
            if (count < 2)
            {
                _line.positionCount = 0;
                return;
            }

            var steps = segmentsPerLink + 1;
            _line.positionCount = (count - 1) * steps + 1;

            var index = 0;
            for (var link = 0; link < count - 1; link++)
            {
                var from = points[link];
                var to = points[link + 1];
                for (var step = 0; step < steps; step++)
                {
                    var point = Vector3.Lerp(from, to, (float)step / steps);
                    if (step > 0)
                        point += Random.insideUnitSphere * jitter;
                    _line.SetPosition(index++, point);
                }
            }
            _line.SetPosition(index, points[count - 1]);
        }
    }
}
