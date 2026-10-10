using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI.Elements
{
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

        /// <summary>Adds a closed hexagon fitted to <paramref name="r"/> to the painter's current path.</summary>
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
