using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BattleArena.Editor.ContentBuilder
{
    internal static partial class GameContentBuilder
    {
        private const string PathMeshPath = ModelsFolder + "/Props/GroundPaths.asset";
        private const string DirtTexturePath = Root + "/Art/Textures/GroundDirt.png";
        /// <summary>World size of one repeat of the dirt texture.</summary>
        private const float DirtTileSize = 5f;
        /// <summary>Width of the soft band where the dirt fades into the grass.</summary>
        private const float PathFringe = 0.9f;

        // Dirt path network, in arena XZ. A loop runs past both healing wells; spokes join it to the spawn
        // clearing, and two of them carry on to clearings in front of grave groups, so every path leads somewhere.
        private static readonly Vector2[] PathLoop =
        {
            new(-18f, 14f), new(-8f, 19.5f), new(4f, 19f), new(14f, 19.8f), new(22f, 12f), new(23.5f, 0f),
            new(20f, -16f), new(8f, -21.5f), new(-6f, -21f), new(-18f, -17f), new(-23.5f, -4f), new(-22f, 6f),
        };
        private const float LoopWidth = 2.6f;

        private static readonly (Vector2[] points, float width)[] PathSpokes =
        {
            (new Vector2[] { new(0f, 0f), new(-6f, 5.5f), new(-12f, 10.5f), new(-18f, 14f) }, 2.2f),
            (new Vector2[] { new(0f, 0f), new(7f, -5f), new(14f, -11.5f), new(20f, -16f) }, 2.2f),
            (new Vector2[] { new(0f, 0f), new(1f, 7f), new(-1.5f, 13.5f), new(-4f, 19.4f), new(-4f, 23f) }, 2f),
            (new Vector2[] { new(0f, 0f), new(6f, 3.5f), new(11f, 8f), new(13f, 10.8f) }, 1.8f),
        };

        /// <summary>Open ground at the spawn, around each well and in front of grave groups.</summary>
        private static readonly (Vector2 center, float radius)[] PathClearings =
        {
            (new Vector2(0f, 0f), 4.5f),
            (new Vector2(-18f, 14f), 4.2f), (new Vector2(20f, -16f), 4.2f),
            (new Vector2(-4f, 23.4f), 2.4f), (new Vector2(13f, 11f), 2.4f),
        };

        private sealed class PathMeshWriter
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<Color> Colors = new();
            public readonly List<int> Triangles = new();

            /// <param name="alpha">Dirt opacity: 1 on the path, fading to 0 at the outer edge of the fringe.</param>
            public int Add(Vector2 point, float alpha = 1f)
            {
                Vertices.Add(new Vector3(point.x, 0f, point.y));
                Colors.Add(new Color(1f, 1f, 1f, alpha));
                return Vertices.Count - 1;
            }

            public void Quad(int a, int b, int c, int d)
            {
                Triangle(a, b, c);
                Triangle(a, c, d);
            }

            /// <summary>Adds a triangle wound to face up, whatever order the corners come in.</summary>
            public void Triangle(int a, int b, int c)
            {
                var normal = Vector3.Cross(Vertices[b] - Vertices[a], Vertices[c] - Vertices[a]);
                Triangles.Add(a);
                if (normal.y >= 0f)
                {
                    Triangles.Add(b);
                    Triangles.Add(c);
                }
                else
                {
                    Triangles.Add(c);
                    Triangles.Add(b);
                }
            }
        }

        /// <summary>Builds the dirt path mesh (updated in place, so the prefab keeps its reference).</summary>
        private static Mesh BuildPathMesh()
        {
            var writer = new PathMeshWriter();
            var seed = 0f;

            var loop = SampleSpline(PathLoop, closed: true);
            AddRibbon(writer, loop, LoopWidth, seed += 17f);
            foreach (var (points, width) in PathSpokes)
            {
                var samples = SampleSpline(points, closed: false);
                AddRibbon(writer, samples, width, seed += 17f);
                // Round off both ends where they meet the loop or a clearing.
                AddBlob(writer, samples[0], width * 0.62f, seed += 17f);
                AddBlob(writer, samples[^1], width * 0.62f, seed += 17f);
            }
            foreach (var (center, radius) in PathClearings)
                AddBlob(writer, center, radius, seed += 17f);

            var uvs = new List<Vector2>(writer.Vertices.Count);
            foreach (var vertex in writer.Vertices)
                uvs.Add(new Vector2(vertex.x, vertex.z) / DirtTileSize);

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(PathMeshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "GroundPaths" };
                AssetDatabase.CreateAsset(mesh, PathMeshPath);
            }

            mesh.Clear();
            mesh.SetVertices(writer.Vertices);
            mesh.SetColors(writer.Colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(writer.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        /// <summary>Points along the network that trees and rocks must stay clear of.</summary>
        private static IEnumerable<(Vector3 position, float radius)> PathReservations()
        {
            const float clearance = 2.2f;
            foreach (var point in SampleSpline(PathLoop, closed: true, spacing: 2f))
                yield return (new Vector3(point.x, 0f, point.y), LoopWidth * 0.5f + clearance);
            foreach (var (points, width) in PathSpokes)
            foreach (var point in SampleSpline(points, closed: false, spacing: 2f))
                yield return (new Vector3(point.x, 0f, point.y), width * 0.5f + clearance);
            foreach (var (center, radius) in PathClearings)
                yield return (new Vector3(center.x, 0f, center.y), radius + clearance);
        }

        /// <summary>Catmull-Rom spline through the control points, resampled at even spacing.</summary>
        private static List<Vector2> SampleSpline(IReadOnlyList<Vector2> points, bool closed, float spacing = 0.6f)
        {
            var count = points.Count;
            Vector2 At(int i) => closed ? points[(i % count + count) % count] : points[Mathf.Clamp(i, 0, count - 1)];

            var dense = new List<Vector2>();
            var segments = closed ? count : count - 1;
            for (var s = 0; s < segments; s++)
            for (var step = 0; step < 24; step++)
            {
                float t = step / 24f, t2 = t * t, t3 = t2 * t;
                Vector2 p0 = At(s - 1), p1 = At(s), p2 = At(s + 1), p3 = At(s + 2);
                dense.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3));
            }
            dense.Add(closed ? dense[0] : points[count - 1]);

            var result = new List<Vector2> { dense[0] };
            var carried = 0f;
            for (var i = 1; i < dense.Count; i++)
            {
                carried += Vector2.Distance(dense[i - 1], dense[i]);
                if (carried < spacing)
                    continue;
                result.Add(dense[i]);
                carried = 0f;
            }
            if (closed)
                result[^1] = result[0];
            else if (result[^1] != dense[^1])
                result.Add(dense[^1]);
            return result;
        }

        /// <summary>
        /// A strip along the samples whose two edges wobble independently, with a fringe on each side that fades
        /// the dirt into the grass: solid edge, a patchy middle ring, then fully transparent.
        /// </summary>
        private static void AddRibbon(PathMeshWriter writer, List<Vector2> samples, float width, float seed)
        {
            var distance = 0f;
            int[] previous = null;
            for (var i = 0; i < samples.Count; i++)
            {
                if (i > 0)
                    distance += Vector2.Distance(samples[i - 1], samples[i]);

                var ahead = samples[Mathf.Min(i + 1, samples.Count - 1)] - samples[Mathf.Max(i - 1, 0)];
                var side = new Vector2(-ahead.y, ahead.x).normalized;
                var left = width * 0.5f * EdgeWobble(seed, distance);
                var right = width * 0.5f * EdgeWobble(seed + 7.3f, distance);

                // Columns from the outer edge of the left fringe to the outer edge of the right one.
                var column = new int[6];
                column[0] = writer.Add(samples[i] + side * (left + FringeWidth(seed + 1.1f, distance)), 0f);
                column[1] = writer.Add(samples[i] + side * (left + FringeWidth(seed + 2.2f, distance) * 0.4f), FringeAlpha(seed + 3.3f, distance));
                column[2] = writer.Add(samples[i] + side * left);
                column[3] = writer.Add(samples[i] - side * right);
                column[4] = writer.Add(samples[i] - side * (right + FringeWidth(seed + 4.4f, distance) * 0.4f), FringeAlpha(seed + 5.5f, distance));
                column[5] = writer.Add(samples[i] - side * (right + FringeWidth(seed + 6.6f, distance)), 0f);

                if (previous != null)
                    for (var k = 0; k < 5; k++)
                        writer.Quad(previous[k], previous[k + 1], column[k + 1], column[k]);
                previous = column;
            }
        }

        /// <summary>A roughly round patch with an uneven edge.</summary>
        private static void AddBlob(PathMeshWriter writer, Vector2 center, float radius, float seed)
        {
            var segments = Mathf.Clamp(Mathf.RoundToInt(radius * 12f), 16, 48);
            var middle = writer.Add(center);
            var rings = new int[segments][];
            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                // Sample the noise around a circle so the outline closes seamlessly.
                float Noise(float offset) => Mathf.PerlinNoise(seed + offset + direction.x * 1.3f, seed + offset + direction.y * 1.3f);
                var edge = radius * (1f + 0.18f * (Noise(0f) * 2f - 1f));
                rings[i] = new[]
                {
                    writer.Add(center + direction * edge),
                    writer.Add(center + direction * (edge + PathFringe * (0.5f + Noise(3f)) * 0.4f), 0.35f + 0.4f * Noise(5f)),
                    writer.Add(center + direction * (edge + PathFringe * (0.5f + Noise(7f))), 0f),
                };
            }
            for (var i = 0; i < segments; i++)
            {
                var ring = rings[i];
                var next = rings[(i + 1) % segments];
                writer.Triangle(middle, ring[0], next[0]);
                writer.Quad(ring[0], next[0], next[1], ring[1]);
                writer.Quad(ring[1], next[1], next[2], ring[2]);
            }
        }

        private static float FringeWidth(float seed, float distance) =>
            PathFringe * (0.5f + Mathf.PerlinNoise(seed, distance * 0.45f));

        private static float FringeAlpha(float seed, float distance) =>
            0.35f + 0.4f * Mathf.PerlinNoise(seed, distance * 0.6f);

        private static float EdgeWobble(float seed, float distance) =>
            1f + 0.2f * (Mathf.PerlinNoise(seed, distance * 0.3f) * 2f - 1f) + 0.06f * (Mathf.PerlinNoise(seed + 3.1f, distance * 1.4f) * 2f - 1f);
    }
}
