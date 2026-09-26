using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleArena.Editor.ContentBuilder
{
    internal static partial class GameContentBuilder
    {
        /// <summary>Every material used by the placeholder art.</summary>
        private sealed class MaterialSet
        {
            public Material Ground, GroundPatch, Stone, Wall, Wood, Leaves, Gold;
            public Material PlayerRobe, PlayerSkin, PlayerHat, StaffGlow;
            public Material Grunt, Runner, Brute, Boss, EnemyEyes;
            public Material Flash;
            public Material PlayerBolt, Fireball, BossOrb, Meteor, Gem;
            public Material CrystalReady, CrystalDepleted;
            public Material EnemyTelegraph, EnemyTelegraphFill, SpellTelegraph, SpellTelegraphFill, FrostRing;
            public Material Particles;
        }

        private static MaterialSet BuildMaterials()
        {
            return new MaterialSet
            {
                Ground = Lit("Ground", Hex("#4E6B3F"), smoothness: 0.05f),
                GroundPatch = Lit("GroundPatch", Hex("#3F5A33"), smoothness: 0.05f),
                Stone = Lit("Stone", Hex("#8A8D93"), smoothness: 0.15f),
                Wall = Lit("Wall", Hex("#5A5C66"), smoothness: 0.1f),
                Wood = Lit("Wood", Hex("#7A4E2A"), smoothness: 0.2f),
                Leaves = Lit("Leaves", Hex("#2F6B3A"), smoothness: 0.05f),
                Gold = Lit("Gold", Hex("#E2B243"), smoothness: 0.6f, metallic: 0.8f),

                PlayerRobe = Lit("PlayerRobe", Hex("#3560C9"), smoothness: 0.25f),
                PlayerSkin = Lit("PlayerSkin", Hex("#F0C8A0"), smoothness: 0.3f),
                PlayerHat = Lit("PlayerHat", Hex("#5B34B8"), smoothness: 0.25f),
                StaffGlow = Lit("StaffGlow", Hex("#9FE8FF"), emission: Hex("#4FD2FF") * 3f),

                Grunt = Lit("EnemyGrunt", Hex("#7DAE45"), smoothness: 0.2f),
                Runner = Lit("EnemyRunner", Hex("#D9A534"), smoothness: 0.2f),
                Brute = Lit("EnemyBrute", Hex("#A8453B"), smoothness: 0.2f),
                Boss = Lit("EnemyBoss", Hex("#4D2470"), smoothness: 0.35f),
                EnemyEyes = Lit("EnemyEyes", Hex("#FFE3A0"), emission: Hex("#FF5A2A") * 4f),

                Flash = Lit("HitFlash", Color.white, emission: Color.white * 1.5f),

                PlayerBolt = Lit("PlayerBolt", Hex("#BFF3FF"), emission: Hex("#38C8FF") * 5f),
                Fireball = Lit("Fireball", Hex("#FFC060"), emission: Hex("#FF6A00") * 6f),
                BossOrb = Lit("BossOrb", Hex("#F0A0FF"), emission: Hex("#C030FF") * 5f),
                Meteor = Lit("Meteor", Hex("#5A2B18"), emission: Hex("#FF4A00") * 3f),
                Gem = Lit("ExperienceGem", Hex("#A6FFD8"), emission: Hex("#20E0A0") * 3f),

                CrystalReady = Lit("CrystalReady", Hex("#B6FFC8"), emission: Hex("#30FF80") * 3f),
                CrystalDepleted = Lit("CrystalDepleted", Hex("#6A7078"), smoothness: 0.4f),

                EnemyTelegraph = Transparent("EnemyTelegraph", new Color(1f, 0.2f, 0.25f, 0.18f)),
                EnemyTelegraphFill = Transparent("EnemyTelegraphFill", new Color(1f, 0.15f, 0.2f, 0.4f)),
                SpellTelegraph = Transparent("SpellTelegraph", new Color(1f, 0.55f, 0.1f, 0.15f)),
                SpellTelegraphFill = Transparent("SpellTelegraphFill", new Color(1f, 0.5f, 0.05f, 0.35f)),
                FrostRing = Transparent("FrostRing", new Color(0.55f, 0.9f, 1f, 0.45f)),

                Particles = ParticleMaterial(),
            };
        }

        private static Material Lit(string name, Color baseColor, float smoothness = 0.3f, float metallic = 0f, Color? emission = null)
        {
            var material = LoadOrCreateMaterial(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);

            if (emission.HasValue)
            {
                material.SetColor("_EmissionColor", emission.Value);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Transparent(string name, Color color)
        {
            var material = LoadOrCreateMaterial(name, "Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            BaseShaderGUI.SetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material ParticleMaterial()
        {
            var material = LoadOrCreateMaterial("Particles", "Universal Render Pipeline/Particles/Unlit");
            var template = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.defaultParticleMaterial : null;
            if (template != null && template.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", template.GetTexture("_BaseMap"));

            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f); // Additive: glows nicely with bloom.
            BaseShaderGUI.SetupMaterialBlendMode(material);
            BaseShaderGUI.SetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string name, string shaderName)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var shader = Shader.Find(shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.shader = shader;
                return material;
            }

            EnsureFolder(MaterialsFolder);
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
    }
}
