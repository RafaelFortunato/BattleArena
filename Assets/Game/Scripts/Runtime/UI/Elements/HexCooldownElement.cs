using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI.Elements
{
    /// <summary>
    /// Clock-style cooldown shade clipped to a hexagon: covers <see cref="Value"/> (0..1) of the hex,
    /// ending at 12 o'clock, so it unwinds clockwise as the spell recharges. Colour: <c>--sweep-color</c>.
    /// </summary>
    [UxmlElement]
    public partial class HexCooldownElement : VisualElement
    {
        private const float StartAngle = -90f; // 12 o'clock in screen space (y points down).
        private static readonly CustomStyleProperty<Color> ColorProperty = new("--sweep-color");

        private float _value;
        private Color _color = new(0.02f, 0.02f, 0.04f, 0.65f);

        public float Value
        {
            get => _value;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, _value))
                    return;
                _value = value;
                MarkDirtyRepaint();
            }
        }

        public HexCooldownElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(evt =>
            {
                if (evt.customStyle.TryGetValue(ColorProperty, out var color))
                {
                    _color = color;
                    MarkDirtyRepaint();
                }
            });
        }

        private void Draw(MeshGenerationContext context)
        {
            if (_value <= 0f)
                return;

            var rect = contentRect;
            var painter = context.painter2D;
            painter.fillColor = _color;

            if (_value >= 0.999f)
            {
                painter.BeginPath();
                HexGeometry.Trace(painter, rect);
                painter.Fill();
                return;
            }

            // The shade spans from (1 - value) of a turn to the full turn, clockwise.
            var from = StartAngle + (1f - _value) * 360f;
            var to = StartAngle + 360f;
            var center = rect.center;
            var corners = HexGeometry.GetCorners(rect);

            painter.BeginPath();
            painter.MoveTo(center);
            painter.LineTo(PointOnHex(center, corners, from));
            // Corners are clockwise from the top, i.e. in increasing angle order starting at -90°.
            for (var i = 1; i < corners.Length; i++)
            {
                var angle = Normalize(Mathf.Atan2(corners[i].y - center.y, corners[i].x - center.x) * Mathf.Rad2Deg);
                if (angle > from && angle < to)
                    painter.LineTo(corners[i]);
            }
            painter.LineTo(corners[0]);
            painter.ClosePath();
            painter.Fill();
        }

        /// <summary>Maps an angle into [StartAngle, StartAngle + 360).</summary>
        private static float Normalize(float angle)
        {
            while (angle < StartAngle) angle += 360f;
            while (angle >= StartAngle + 360f) angle -= 360f;
            return angle;
        }

        /// <summary>Where a ray from the centre at <paramref name="degrees"/> leaves the hexagon.</summary>
        private static Vector2 PointOnHex(Vector2 center, Vector2[] corners, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            var best = float.MaxValue;

            for (var i = 0; i < corners.Length; i++)
            {
                var a = corners[i];
                var edge = corners[(i + 1) % corners.Length] - a;
                var denominator = Cross(direction, edge);
                if (Mathf.Abs(denominator) < 1e-5f)
                    continue;

                var offset = a - center;
                var t = Cross(offset, edge) / denominator;
                var u = Cross(offset, direction) / denominator;
                if (t > 0f && u >= -1e-4f && u <= 1.0001f && t < best)
                    best = t;
            }

            return best < float.MaxValue ? center + direction * best : center;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
