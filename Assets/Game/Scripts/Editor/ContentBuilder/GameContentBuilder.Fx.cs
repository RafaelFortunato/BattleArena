using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleArena.Editor.ContentBuilder
{
    /// <summary>Shared building blocks for particle effects: textures, particle materials and a fluent particle-system builder.</summary>
    internal static partial class GameContentBuilder
    {
        private const string VfxTexturesFolder = Root + "/Art/Vfx/Textures";
        private const string VfxMeshesFolder = Root + "/Art/Vfx/Meshes";

        private static readonly Dictionary<string, Material> VfxMaterials = new();

        /// <summary>
        /// Unlit particle material (URP Particles/Unlit). The tint is multiplied by <paramref name="intensity"/> so bright
        /// effects exceed 1 and pick up bloom; particle colours still multiply on top for fades.
        /// </summary>
        private static Material FxMaterial(string name, string texture, Color tint, float intensity = 1f, bool additive = true)
        {
            if (VfxMaterials.TryGetValue(name, out var cached))
                return cached;

            var material = LoadOrCreateMaterial("Vfx" + name, "Universal Render Pipeline/Particles/Unlit");
            material.SetTexture("_BaseMap", LoadVfxTexture(texture));
            material.SetColor("_BaseColor", new Color(tint.r * intensity, tint.g * intensity, tint.b * intensity, tint.a));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            BaseShaderGUI.SetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
            VfxMaterials[name] = material;
            return material;
        }

        /// <summary>Lit, alpha-blended particle material for mesh particles (ice shards, debris) that fade with particle colour.</summary>
        private static Material FxLitMaterial(string name, Color color, float smoothness, Color? emission = null)
        {
            if (VfxMaterials.TryGetValue(name, out var cached))
                return cached;

            var material = LoadOrCreateMaterial("Vfx" + name, "Universal Render Pipeline/Particles/Lit");
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            if (emission.HasValue)
            {
                material.SetColor("_EmissionColor", emission.Value);
                material.EnableKeyword("_EMISSION");
            }
            BaseShaderGUI.SetupMaterialBlendMode(material);
            BaseShaderGUI.SetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
            VfxMaterials[name] = material;
            return material;
        }

        /// <summary>Loads a VFX texture, fixing its import settings (alpha, clamp, size) when needed.</summary>
        private static Texture2D LoadVfxTexture(string name)
        {
            var path = $"{VfxTexturesFolder}/{name}.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                Debug.LogWarning($"Missing VFX texture {path}");
                return null;
            }

            var maxSize = name.EndsWith("_sheet") || name.StartsWith("vfx_rune") ? 1024 : 256;
            if (!importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp || importer.maxTextureSize != maxSize)
            {
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = maxSize;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Loads a VFX mesh (e.g. an ice shard).</summary>
        private static Mesh LoadVfxMesh(string name) => LoadModelMesh($"{VfxMeshesFolder}/{name}.fbx");

        /// <summary>A smooth curve through (time, value) pairs.</summary>
        private static AnimationCurve Curve(params float[] timeValuePairs)
        {
            var keys = new Keyframe[timeValuePairs.Length / 2];
            for (var i = 0; i < keys.Length; i++)
                keys[i] = new Keyframe(timeValuePairs[i * 2], timeValuePairs[i * 2 + 1]);
            var curve = new AnimationCurve(keys);
            for (var i = 0; i < keys.Length; i++)
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            for (var i = 0; i < keys.Length; i++)
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            return curve;
        }

        /// <summary>
        /// A gradient from <paramref name="start"/> to <paramref name="end"/> with alpha keys from (time, alpha) pairs.
        /// </summary>
        private static Gradient Fade(Color start, Color end, params float[] timeAlphaPairs)
        {
            var alphas = new GradientAlphaKey[timeAlphaPairs.Length / 2];
            for (var i = 0; i < alphas.Length; i++)
                alphas[i] = new GradientAlphaKey(timeAlphaPairs[i * 2 + 1], timeAlphaPairs[i * 2]);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) }, alphas);
            return gradient;
        }

        /// <summary>A white gradient with alpha keys from (time, alpha) pairs.</summary>
        private static Gradient Fade(params float[] timeAlphaPairs) => Fade(Color.white, Color.white, timeAlphaPairs);

        /// <summary>Adds a tapering, fading trail.</summary>
        private static TrailRenderer AddTrail(Transform parent, Material material, float time, float width, float minVertexDistance = 0.1f)
        {
            var trail = parent.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = time;
            trail.widthMultiplier = width;
            trail.widthCurve = Curve(0f, 1f, 1f, 0f);
            trail.colorGradient = Fade(0f, 1f, 1f, 0f);
            trail.minVertexDistance = minVertexDistance;
            trail.numCapVertices = 2;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            return trail;
        }

        /// <summary>Fluent builder for one particle system. Defaults: one-shot, local space, hierarchy scaling, no shape.</summary>
        private sealed class Fx
        {
            /// <summary>The particle system being configured.</summary>
            public readonly ParticleSystem System;
            private readonly ParticleSystemRenderer _renderer;

            /// <summary>
            /// Creates a particle system child with <paramref name="material"/> and the builder's defaults.
            /// </summary>
            public Fx(Transform parent, string name, Material material, Vector3 localPosition = default)
            {
                var go = new GameObject(name) { layer = parent.gameObject.layer };
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPosition;
                System = go.AddComponent<ParticleSystem>();
                _renderer = go.GetComponent<ParticleSystemRenderer>();
                _renderer.sharedMaterial = material;
                _renderer.shadowCastingMode = ShadowCastingMode.Off;
                _renderer.receiveShadows = false;
                _renderer.maxParticleSize = 4f;

                var main = System.main;
                main.playOnAwake = true;
                main.loop = false;
                main.duration = 1f;
                main.startSpeed = 0f;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.stopAction = ParticleSystemStopAction.None;
                var emission = System.emission;
                emission.rateOverTime = 0f;
                var shape = System.shape;
                shape.enabled = false;
            }

            /// <summary>Particle lifetime range in seconds.</summary>
            public Fx Life(float min, float max) { var m = System.main; m.startLifetime = new ParticleSystem.MinMaxCurve(min, max); return this; }
            /// <summary>Start size range.</summary>
            public Fx Size(float min, float max) { var m = System.main; m.startSize = new ParticleSystem.MinMaxCurve(min, max); return this; }
            /// <summary>Start speed range.</summary>
            public Fx Speed(float min, float max) { var m = System.main; m.startSpeed = new ParticleSystem.MinMaxCurve(min, max); return this; }
            /// <summary>Maximum live particles.</summary>
            public Fx Max(int count) { var m = System.main; m.maxParticles = count; return this; }
            /// <summary>Gravity multiplier.</summary>
            public Fx Gravity(float g) { var m = System.main; m.gravityModifier = g; return this; }
            /// <summary>Simulates in world space, so particles stay behind when the object moves.</summary>
            public Fx World() { var m = System.main; m.simulationSpace = ParticleSystemSimulationSpace.World; return this; }
            /// <summary>Delay before emission starts.</summary>
            public Fx Delay(float seconds) { var m = System.main; m.startDelay = seconds; return this; }

            /// <summary>Loops the system with the given cycle length.</summary>
            public Fx Loop(float duration = 1f)
            {
                var m = System.main;
                m.loop = true;
                m.duration = duration;
                return this;
            }

            /// <summary>Play only when told to (e.g. zones started by their component).</summary>
            public Fx Manual() { var m = System.main; m.playOnAwake = false; return this; }

            /// <summary>Random start rotation range.</summary>
            public Fx Rotation(float minDeg = 0f, float maxDeg = 360f)
            {
                var m = System.main;
                m.startRotation = new ParticleSystem.MinMaxCurve(minDeg * Mathf.Deg2Rad, maxDeg * Mathf.Deg2Rad);
                return this;
            }

            /// <summary>Separate start size ranges per axis (for meshes).</summary>
            public Fx Size3D(Vector2 x, Vector2 y, Vector2 z)
            {
                var m = System.main;
                m.startSize3D = true;
                m.startSizeX = new ParticleSystem.MinMaxCurve(x.x, x.y);
                m.startSizeY = new ParticleSystem.MinMaxCurve(y.x, y.y);
                m.startSizeZ = new ParticleSystem.MinMaxCurve(z.x, z.y);
                return this;
            }

            /// <summary>Random 3D start rotation, tilted up to <paramref name="tiltDeg"/> (for meshes).</summary>
            public Fx Rotation3D(float tiltDeg)
            {
                var m = System.main;
                m.startRotation3D = true;
                var t = tiltDeg * Mathf.Deg2Rad;
                m.startRotationX = new ParticleSystem.MinMaxCurve(-t, t);
                m.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                m.startRotationZ = new ParticleSystem.MinMaxCurve(-t, t);
                return this;
            }

            /// <summary>Emits <paramref name="count"/> particles at once at <paramref name="time"/>.</summary>
            public Fx Burst(int count, float time = 0f)
            {
                var e = System.emission;
                var bursts = new ParticleSystem.Burst[e.burstCount + 1];
                e.GetBursts(bursts);
                bursts[^1] = new ParticleSystem.Burst(time, (short)count);
                e.SetBursts(bursts);
                return this;
            }

            /// <summary>Continuous emission per second.</summary>
            public Fx Rate(float perSecond) { var e = System.emission; e.rateOverTime = perSecond; return this; }
            /// <summary>Emission per metre travelled (trails behind moving objects).</summary>
            public Fx RatePerMeter(float perMeter) { var e = System.emission; e.rateOverDistance = perMeter; return this; }

            /// <summary>Emits from a sphere.</summary>
            public Fx Sphere(float radius, float thickness = 1f)
            {
                var s = System.shape;
                s.enabled = true;
                s.shapeType = ParticleSystemShapeType.Sphere;
                s.radius = radius;
                s.radiusThickness = thickness;
                return this;
            }

            /// <summary>Emits from an upward hemisphere.</summary>
            public Fx Hemisphere(float radius)
            {
                var s = System.shape;
                s.enabled = true;
                s.shapeType = ParticleSystemShapeType.Hemisphere;
                s.radius = radius;
                s.rotation = new Vector3(-90f, 0f, 0f);
                return this;
            }

            /// <summary>Flat circle on the ground; particles move outward along it when they have speed.</summary>
            public Fx Circle(float radius, float thickness = 1f)
            {
                var s = System.shape;
                s.enabled = true;
                s.shapeType = ParticleSystemShapeType.Circle;
                s.radius = radius;
                s.radiusThickness = thickness;
                s.rotation = new Vector3(90f, 0f, 0f);
                return this;
            }

            /// <summary>Cone opening upward.</summary>
            public Fx ConeUp(float angle, float radius)
            {
                var s = System.shape;
                s.enabled = true;
                s.shapeType = ParticleSystemShapeType.Cone;
                s.angle = angle;
                s.radius = radius;
                s.rotation = new Vector3(-90f, 0f, 0f);
                return this;
            }

            /// <summary>Size multiplier over each particle's life.</summary>
            public Fx SizeOverLife(AnimationCurve curve)
            {
                var s = System.sizeOverLifetime;
                s.enabled = true;
                s.size = new ParticleSystem.MinMaxCurve(1f, curve);
                return this;
            }

            /// <summary>Colour and alpha over each particle's life.</summary>
            public Fx ColorOverLife(Gradient gradient)
            {
                var c = System.colorOverLifetime;
                c.enabled = true;
                c.color = gradient;
                return this;
            }

            /// <summary>Rotation speed range in degrees per second.</summary>
            public Fx Spin(float minDeg, float maxDeg)
            {
                var r = System.rotationOverLifetime;
                r.enabled = true;
                r.z = new ParticleSystem.MinMaxCurve(minDeg * Mathf.Deg2Rad, maxDeg * Mathf.Deg2Rad);
                return this;
            }

            /// <summary>Extra velocity range over life, in world or local space.</summary>
            public Fx Velocity(Vector3 min, Vector3 max, bool world = true)
            {
                var v = System.velocityOverLifetime;
                v.enabled = true;
                v.space = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
                v.x = new ParticleSystem.MinMaxCurve(min.x, max.x);
                v.y = new ParticleSystem.MinMaxCurve(min.y, max.y);
                v.z = new ParticleSystem.MinMaxCurve(min.z, max.z);
                return this;
            }

            /// <summary>Slows particles down over their life.</summary>
            public Fx Drag(float drag)
            {
                var l = System.limitVelocityOverLifetime;
                l.enabled = true;
                l.drag = drag;
                l.multiplyDragByParticleSize = false;
                l.multiplyDragByParticleVelocity = true;
                return this;
            }

            /// <summary>Turbulence that wobbles the particles.</summary>
            public Fx Noise(float strength, float frequency = 1f)
            {
                var n = System.noise;
                n.enabled = true;
                n.strength = strength;
                n.frequency = frequency;
                n.quality = ParticleSystemNoiseQuality.Medium;
                return this;
            }

            /// <summary>Plays the texture as a tiles × tiles flipbook over each particle's life.</summary>
            public Fx Flipbook(int tiles = 4)
            {
                var t = System.textureSheetAnimation;
                t.enabled = true;
                t.numTilesX = tiles;
                t.numTilesY = tiles;
                t.animation = ParticleSystemAnimationType.WholeSheet;
                return this;
            }

            /// <summary>Stretches particles along their velocity.</summary>
            public Fx Stretched(float lengthScale, float speedScale = 0.02f)
            {
                _renderer.renderMode = ParticleSystemRenderMode.Stretch;
                _renderer.lengthScale = lengthScale;
                _renderer.velocityScale = speedScale;
                return this;
            }

            /// <summary>Flat on the ground (decals, shockwaves). Draws behind other effects.</summary>
            public Fx Horizontal()
            {
                _renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                _renderer.sortingFudge = 20f;
                return this;
            }

            /// <summary>Billboards that stay upright.</summary>
            public Fx Vertical() { _renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard; return this; }

            /// <summary>Renders each particle as <paramref name="mesh"/>.</summary>
            public Fx Mesh(Mesh mesh)
            {
                _renderer.renderMode = ParticleSystemRenderMode.Mesh;
                _renderer.mesh = mesh;
                _renderer.alignment = ParticleSystemRenderSpace.Local;
                return this;
            }

            /// <summary>Escape hatch for settings the builder doesn't cover.</summary>
            public Fx Configure(Action<ParticleSystem> configure)
            {
                configure(System);
                return this;
            }
        }
    }
}
