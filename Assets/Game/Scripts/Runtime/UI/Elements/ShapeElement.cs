using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI.Elements
{
    public enum ShapeKind
    {
        /// <summary>Pointy-top hexagon, the game's signature frame (spell slots, badges).</summary>
        Hexagon,
        /// <summary>Wide plate with pointed ends, used behind the match timer.</summary>
        Plate,
        TriangleUp,
        TriangleDown
    }

    /// <summary>
    /// Vector frame drawn with <see cref="Painter2D"/>. Colours come from USS custom properties so states
    /// (ready, cooling, locked, rarity…) are pure style-sheet changes:
    /// <c>--shape-fill</c>, <c>--shape-border</c>, <c>--shape-border-width</c>, <c>--shape-inner</c>
    /// (an optional inset outline) and <c>--shape-inner-inset</c>.
    /// </summary>
    [UxmlElement]
    public partial class ShapeElement : VisualElement
    {
        private static readonly CustomStyleProperty<Color> FillProperty = new("--shape-fill");
        private static readonly CustomStyleProperty<Color> BorderProperty = new("--shape-border");
        private static readonly CustomStyleProperty<float> BorderWidthProperty = new("--shape-border-width");
        private static readonly CustomStyleProperty<Color> InnerProperty = new("--shape-inner");
        private static readonly CustomStyleProperty<float> InnerInsetProperty = new("--shape-inner-inset");

        private ShapeKind _kind;
        private Color _fill = new(0.08f, 0.09f, 0.12f, 0.92f);
        private Color _border = Color.white;
        private float _borderWidth = 4f;
        private Color _inner = Color.clear;
        private float _innerInset = 10f;
        // Data-driven colours (e.g. a spell's colour) that USS cannot express; they win over the style sheet.
        private Color? _fillOverride;
        private Color? _borderOverride;

        [UxmlAttribute]
        public ShapeKind Kind
        {
            get => _kind;
            set
            {
                _kind = value;
                MarkDirtyRepaint();
            }
        }

        public ShapeElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        }

        /// <summary>Overrides the style sheet colours; pass null to fall back to USS again.</summary>
        public void SetColors(Color? fill, Color? border)
        {
            _fillOverride = fill;
            _borderOverride = border;
            MarkDirtyRepaint();
        }

        private void OnStyleResolved(CustomStyleResolvedEvent evt)
        {
            var style = evt.customStyle;
            if (style.TryGetValue(FillProperty, out var fill)) _fill = fill;
            if (style.TryGetValue(BorderProperty, out var border)) _border = border;
            if (style.TryGetValue(BorderWidthProperty, out var width)) _borderWidth = width;
            if (style.TryGetValue(InnerProperty, out var inner)) _inner = inner;
            if (style.TryGetValue(InnerInsetProperty, out var inset)) _innerInset = inset;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var painter = context.painter2D;
            painter.lineJoin = LineJoin.Round;

            var fill = _fillOverride ?? _fill;
            var border = _borderOverride ?? _border;

            // Keep the stroke inside the element bounds.
            var outer = Inset(rect, _borderWidth * 0.5f);
            Trace(painter, _kind, outer);
            if (fill.a > 0f)
            {
                painter.fillColor = fill;
                painter.Fill();
            }
            if (_borderWidth > 0f && border.a > 0f)
            {
                painter.strokeColor = border;
                painter.lineWidth = _borderWidth;
                painter.Stroke();
            }

            if (_inner.a > 0f)
            {
                Trace(painter, _kind, Inset(rect, _innerInset));
                painter.strokeColor = _inner;
                painter.lineWidth = Mathf.Max(1f, _borderWidth * 0.5f);
                painter.Stroke();
            }
        }

        private static Rect Inset(Rect rect, float amount) =>
            new(rect.x + amount, rect.y + amount, Mathf.Max(0f, rect.width - amount * 2f), Mathf.Max(0f, rect.height - amount * 2f));

        /// <summary>Begins and closes a path for <paramref name="kind"/> fitted to <paramref name="r"/>.</summary>
        public static void Trace(Painter2D painter, ShapeKind kind, Rect r)
        {
            painter.BeginPath();
            switch (kind)
            {
                case ShapeKind.Hexagon:
                    HexGeometry.Trace(painter, r);
                    return;
                case ShapeKind.Plate:
                    var chamfer = r.height * 0.25f;
                    painter.MoveTo(new Vector2(r.xMin + chamfer, r.yMin));
                    painter.LineTo(new Vector2(r.xMax - chamfer, r.yMin));
                    painter.LineTo(new Vector2(r.xMax, r.center.y));
                    painter.LineTo(new Vector2(r.xMax - chamfer, r.yMax));
                    painter.LineTo(new Vector2(r.xMin + chamfer, r.yMax));
                    painter.LineTo(new Vector2(r.xMin, r.center.y));
                    break;
                case ShapeKind.TriangleUp:
                    painter.MoveTo(new Vector2(r.center.x, r.yMin));
                    painter.LineTo(new Vector2(r.xMax, r.yMax));
                    painter.LineTo(new Vector2(r.xMin, r.yMax));
                    break;
                case ShapeKind.TriangleDown:
                    painter.MoveTo(new Vector2(r.xMin, r.yMin));
                    painter.LineTo(new Vector2(r.xMax, r.yMin));
                    painter.LineTo(new Vector2(r.center.x, r.yMax));
                    break;
            }
            painter.ClosePath();
        }
    }

    /// <summary>Pointy-top hexagon math shared by frames and the cooldown sweep.</summary>
    public static class HexGeometry
    {
        private static readonly Vector2[] Corners = new Vector2[6];

        /// <summary>Corners clockwise from the top, fitted to <paramref name="r"/>. Returns a shared buffer.</summary>
        public static Vector2[] GetCorners(Rect r)
        {
            var quarter = r.height * 0.25f;
            Corners[0] = new Vector2(r.center.x, r.yMin);
            Corners[1] = new Vector2(r.xMax, r.yMin + quarter);
            Corners[2] = new Vector2(r.xMax, r.yMax - quarter);
            Corners[3] = new Vector2(r.center.x, r.yMax);
            Corners[4] = new Vector2(r.xMin, r.yMax - quarter);
            Corners[5] = new Vector2(r.xMin, r.yMin + quarter);
            return Corners;
        }

        public static void Trace(Painter2D painter, Rect r)
        {
            var corners = GetCorners(r);
            painter.MoveTo(corners[0]);
            for (var i = 1; i < corners.Length; i++)
                painter.LineTo(corners[i]);
            painter.ClosePath();
        }
    }
}
