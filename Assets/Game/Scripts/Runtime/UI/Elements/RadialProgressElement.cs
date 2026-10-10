using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI.Elements
{
    /// <summary>
    /// A ring that fills clockwise from 12 o'clock to show <see cref="Value"/> (0..1), drawn on the largest circle that
    /// fits the element. Only the filled arc is drawn: the empty track belongs to whatever sits underneath.
    /// Style: <c>--ring-color</c> and <c>--ring-width</c>.
    /// </summary>
    [UxmlElement]
    public partial class RadialProgressElement : VisualElement
    {
        private const float StartAngle = -90f; // 12 o'clock in screen space (y points down).
        private static readonly CustomStyleProperty<Color> ColorProperty = new("--ring-color");
        private static readonly CustomStyleProperty<float> WidthProperty = new("--ring-width");

        private float _value;
        private Color _color = Color.white;
        private float _width = 4f;

        /// <summary>Filled share of the ring, 0..1.</summary>
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

        /// <summary>
        /// Creates the element; it ignores the pointer, draws itself and reads its colour and width from USS.
        /// </summary>
        public RadialProgressElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(evt =>
            {
                if (evt.customStyle.TryGetValue(ColorProperty, out var color))
                    _color = color;
                if (evt.customStyle.TryGetValue(WidthProperty, out var width))
                    _width = width;
                MarkDirtyRepaint();
            });
        }

        /// <summary>
        /// Strokes the filled arc. Round ends read as a bead of light travelling round; a full ring needs none.
        /// </summary>
        private void Draw(MeshGenerationContext context)
        {
            if (_value <= 0f)
                return;

            var rect = contentRect;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f - _width * 0.5f;
            if (radius <= 0f)
                return;

            var painter = context.painter2D;
            painter.strokeColor = _color;
            painter.lineWidth = _width;
            painter.lineCap = _value >= 0.999f ? LineCap.Butt : LineCap.Round;
            painter.BeginPath();
            painter.Arc(rect.center, radius, Angle.Degrees(StartAngle), Angle.Degrees(StartAngle + 360f * _value));
            painter.Stroke();
        }
    }
}
