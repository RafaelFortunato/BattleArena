using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI.Elements
{
    /// <summary>
    /// Top art panel of an upgrade card: a tinted panel whose bottom edge slants up to the right, filled with
    /// a repeating <see cref="CardPattern"/> and underlined by an accent stroke. Geometry is rebuilt only when
    /// the size, colour or pattern changes.
    /// </summary>
    [UxmlElement]
    public partial class CardArtElement : VisualElement
    {
        private const float RightEdgeRatio = 0.815f; // Right side height relative to the left side.
        private const float EdgeWidth = 5f;
        private static readonly Color Base = new(0.078f, 0.086f, 0.122f);
        private static readonly Vector2[] Polygon = new Vector2[4];

        private Color _accent = new(0.3f, 0.64f, 1f);
        private CardPattern _pattern = CardPattern.Hexagons;

        /// <summary>The card's accent colour; tints the panel, pattern and edge stroke.</summary>
        public Color Accent
        {
            get => _accent;
            set
            {
                _accent = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>The background pattern.</summary>
        [UxmlAttribute]
        public CardPattern Pattern
        {
            get => _pattern;
            set
            {
                _pattern = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>Creates the element; it ignores the pointer and draws itself.</summary>
        public CardArtElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        /// <summary>Draws the slanted panel, its pattern and the accent stroke along the slanted edge.</summary>
        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var painter = context.painter2D;
            var w = rect.width;
            var h = rect.height;
            var right = h * RightEdgeRatio;

            Polygon[0] = new Vector2(0f, 0f);
            Polygon[1] = new Vector2(w, 0f);
            Polygon[2] = new Vector2(w, right);
            Polygon[3] = new Vector2(0f, h);

            painter.BeginPath();
            painter.MoveTo(Polygon[0]);
            for (var i = 1; i < Polygon.Length; i++)
                painter.LineTo(Polygon[i]);
            painter.ClosePath();
            painter.fillColor = Color.Lerp(Base, _accent, 0.2f);
            painter.Fill();

            switch (_pattern)
            {
                case CardPattern.Dots:
                    DrawDots(painter, w, h);
                    break;
                case CardPattern.Hexagons:
                    DrawHexagons(painter, w, h);
                    break;
                case CardPattern.Diamonds:
                    DrawDiamonds(painter, w, h);
                    break;
            }

            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, h - EdgeWidth * 0.5f));
            painter.LineTo(new Vector2(w, right - EdgeWidth * 0.5f));
            painter.strokeColor = _accent;
            painter.lineWidth = EdgeWidth;
            painter.lineCap = LineCap.Butt;
            painter.Stroke();
        }

        /// <summary>Fills the panel with a staggered grid of dots.</summary>
        private void DrawDots(Painter2D painter, float w, float h)
        {
            const float spacing = 18.33f;
            const float radius = 2.08f;

            painter.BeginPath();
            for (var y = spacing * 0.5f; y < h; y += spacing)
            for (var x = spacing * 0.5f; x < w; x += spacing)
            {
                if (y + radius > EdgeY(x, w, h))
                    continue;
                painter.MoveTo(new Vector2(x + radius, y));
                painter.Arc(new Vector2(x, y), radius, 0f, 360f);
                painter.ClosePath();
            }
            painter.fillColor = WithAlpha(_accent, 0.24f);
            painter.Fill();
        }

        /// <summary>Fills the panel with a honeycomb of hexagon outlines.</summary>
        private void DrawHexagons(Painter2D painter, float w, float h)
        {
            // 36 × 62 tile: a hexagon plus the stem that links it to the one below.
            const float tileW = 30f;
            const float tileH = 51.67f;

            painter.BeginPath();
            for (var ty = 0f; ty < h; ty += tileH)
            for (var tx = 0f; tx < w; tx += tileW)
            {
                Segment(painter, tx + 15f, ty + 0.83f, tx + 29.17f, ty + 9.17f, w, h);
                Segment(painter, tx + 29.17f, ty + 9.17f, tx + 29.17f, ty + 25.83f, w, h);
                Segment(painter, tx + 29.17f, ty + 25.83f, tx + 15f, ty + 34.17f, w, h);
                Segment(painter, tx + 15f, ty + 34.17f, tx + 0.83f, ty + 25.83f, w, h);
                Segment(painter, tx + 0.83f, ty + 25.83f, tx + 0.83f, ty + 9.17f, w, h);
                Segment(painter, tx + 0.83f, ty + 9.17f, tx + 15f, ty + 0.83f, w, h);
                Segment(painter, tx + 15f, ty + 34.17f, tx + 15f, ty + 51.67f, w, h);
            }
            painter.strokeColor = WithAlpha(_accent, 0.24f);
            painter.lineWidth = 1.67f;
            painter.lineCap = LineCap.Round;
            painter.Stroke();
        }

        /// <summary>Fills the panel with a lattice of diamond outlines.</summary>
        private void DrawDiamonds(Painter2D painter, float w, float h)
        {
            // A lattice of the two diagonal families x ± y = 20 + 40k, with a sparkle in every cell.
            const float spacing = 40f;

            painter.BeginPath();
            for (var c = spacing * 0.5f - Mathf.Ceil(h / spacing) * spacing; c < w; c += spacing)
                Segment(painter, c, 0f, c + h, h, w, h); // x - y = c
            for (var s = spacing * 0.5f; s < w + h; s += spacing)
                Segment(painter, s, 0f, s - h, h, w, h); // x + y = s
            painter.strokeColor = WithAlpha(_accent, 0.24f);
            painter.lineWidth = 1.67f;
            painter.lineCap = LineCap.Round;
            painter.Stroke();

            painter.BeginPath();
            for (var y = spacing * 0.5f; y < h; y += spacing)
            for (var x = spacing * 0.5f; x < w; x += spacing)
            {
                if (y + 6f > EdgeY(x, w, h))
                    continue;
                painter.MoveTo(new Vector2(x, y - 6f));
                painter.LineTo(new Vector2(x + 1.6f, y - 1.6f));
                painter.LineTo(new Vector2(x + 6f, y));
                painter.LineTo(new Vector2(x + 1.6f, y + 1.6f));
                painter.LineTo(new Vector2(x, y + 6f));
                painter.LineTo(new Vector2(x - 1.6f, y + 1.6f));
                painter.LineTo(new Vector2(x - 6f, y));
                painter.LineTo(new Vector2(x - 1.6f, y - 1.6f));
                painter.ClosePath();
            }
            painter.fillColor = WithAlpha(Color.Lerp(_accent, Color.white, 0.45f), 0.4f);
            painter.Fill();
        }

        /// <summary>Y of the slanted bottom edge at <paramref name="x"/>.</summary>
        private static float EdgeY(float x, float w, float h) => h + (h * RightEdgeRatio - h) * (x / w);

        /// <summary>Adds the part of segment a→b that lies inside the panel polygon (Cyrus–Beck clipping).</summary>
        private static void Segment(Painter2D painter, float ax, float ay, float bx, float by, float w, float h)
        {
            var a = new Vector2(ax, ay);
            var d = new Vector2(bx - ax, by - ay);
            var tEnter = 0f;
            var tExit = 1f;

            // Polygon is convex and wound clockwise in screen space; inward normals point inside.
            for (var i = 0; i < Polygon.Length; i++)
            {
                var p0 = Polygon[i];
                var edge = Polygon[(i + 1) % Polygon.Length] - p0;
                var normal = new Vector2(-edge.y, edge.x); // Inward for clockwise winding with y down.
                var denominator = Vector2.Dot(normal, d);
                var numerator = Vector2.Dot(normal, a - p0);

                if (Mathf.Abs(denominator) < 1e-6f)
                {
                    if (numerator < 0f)
                        return; // Parallel and outside.
                    continue;
                }

                var t = -numerator / denominator;
                if (denominator > 0f)
                    tEnter = Mathf.Max(tEnter, t);
                else
                    tExit = Mathf.Min(tExit, t);

                if (tEnter > tExit)
                    return;
            }

            painter.MoveTo(a + d * tEnter);
            painter.LineTo(a + d * tExit);
        }

        /// <summary><paramref name="color"/> with its alpha replaced.</summary>
        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
