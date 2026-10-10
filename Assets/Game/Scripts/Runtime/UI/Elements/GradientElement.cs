using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI.Elements
{
    /// <summary>
    /// Linear gradient drawn as vertex colours, for screen scrims. Colours come from USS:
    /// <c>--gradient-from</c>, <c>--gradient-to</c> and an optional <c>--gradient-mid</c> at
    /// <c>--gradient-mid-at</c> (0..1).
    /// </summary>
    [UxmlElement]
    public partial class GradientElement : VisualElement
    {
        private static readonly CustomStyleProperty<Color> FromProperty = new("--gradient-from");
        private static readonly CustomStyleProperty<Color> MidProperty = new("--gradient-mid");
        private static readonly CustomStyleProperty<float> MidAtProperty = new("--gradient-mid-at");
        private static readonly CustomStyleProperty<Color> ToProperty = new("--gradient-to");
        // Triangles must wind clockwise on screen; the two layouts mirror each other, hence two index sets.
        private static readonly ushort[] HorizontalIndices = { 0, 2, 1, 0, 3, 2, 3, 4, 2, 3, 5, 4 };
        private static readonly ushort[] VerticalIndices = { 0, 1, 2, 0, 2, 3, 3, 2, 4, 3, 4, 5 };

        private readonly Vertex[] _vertices = new Vertex[6];
        private GradientDirection _direction;
        private Color _from = Color.black;
        private Color _to = Color.clear;
        private Color? _mid;
        private float _midAt = 0.5f;

        /// <summary>Which way the gradient runs.</summary>
        [UxmlAttribute]
        public GradientDirection Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>Creates the element; it ignores the pointer, draws itself and reads its colours from USS.</summary>
        public GradientElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        }

        /// <summary>Reads the gradient colours and midpoint from the custom USS properties.</summary>
        private void OnStyleResolved(CustomStyleResolvedEvent evt)
        {
            var style = evt.customStyle;
            if (style.TryGetValue(FromProperty, out var from)) _from = from;
            if (style.TryGetValue(ToProperty, out var to)) _to = to;
            _mid = style.TryGetValue(MidProperty, out var mid) ? mid : null;
            if (style.TryGetValue(MidAtProperty, out var midAt)) _midAt = Mathf.Clamp01(midAt);
            MarkDirtyRepaint();
        }

        /// <summary>Draws the gradient as three bands of vertex-coloured quads.</summary>
        private void Draw(MeshGenerationContext context)
        {
            var r = contentRect;
            if (r.width <= 0f || r.height <= 0f)
                return;

            var mid = _mid ?? Color.Lerp(_from, _to, _midAt);
            // Three bands of two vertices each: start, middle, end.
            if (_direction == GradientDirection.Horizontal)
            {
                var x = Mathf.Lerp(r.xMin, r.xMax, _midAt);
                Set(0, r.xMin, r.yMin, _from);
                Set(1, r.xMin, r.yMax, _from);
                Set(3, x, r.yMin, mid);
                Set(2, x, r.yMax, mid);
                Set(5, r.xMax, r.yMin, _to);
                Set(4, r.xMax, r.yMax, _to);
            }
            else
            {
                var y = Mathf.Lerp(r.yMax, r.yMin, _midAt);
                Set(0, r.xMax, r.yMax, _from);
                Set(1, r.xMin, r.yMax, _from);
                Set(3, r.xMax, y, mid);
                Set(2, r.xMin, y, mid);
                Set(5, r.xMax, r.yMin, _to);
                Set(4, r.xMin, r.yMin, _to);
            }

            var indices = _direction == GradientDirection.Horizontal ? HorizontalIndices : VerticalIndices;
            var mesh = context.Allocate(_vertices.Length, indices.Length);
            mesh.SetAllVertices(_vertices);
            mesh.SetAllIndices(indices);
        }

        /// <summary>Writes one vertex of the gradient mesh.</summary>
        private void Set(int index, float x, float y, Color color) =>
            _vertices[index] = new Vertex { position = new Vector3(x, y, Vertex.nearZ), tint = color };
    }
}
