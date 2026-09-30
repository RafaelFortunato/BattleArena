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
            public Material PineBark, PineNeedles, PineNeedlesDark, LanternIron, LanternGlow;
            public Material PropStone, PropStoneDark, PropStoneLight, GraveStone, Moss, Slate, HealingWater, WellDepth, Dirt;
            public Material AutumnWood, LeafRust, LeafRustDark, LeafOchre, RockStone, RockMoss;
            public Material HuskyBody, HuskyCrystal;
            public Material Grunt, Runner, Brute, Boss, EnemyEyes;
            public Material Bat, Archer, Wisp, Bomber, Shaman, Boar, Beetle, Slime, Wyvern;
            public Material WindupGlow, CurseGlow, BombBody, EnemyArrow, WispBolt;
            public Material Flash;
            public Material PlayerBolt, Fireball, BossOrb, Meteor, Gem, ArcaneOrb;
            public Material EnemyTelegraph, EnemyTelegraphFill, StrikeEnemyRing, StrikeEnemyFill;
            public Material Particles;
            public Material HealthBar, DamageText;
            public Material MissionZone, MissionZoneFill, ArcaneShard, PointerGlow;
        }

        private static MaterialSet BuildMaterials()
        {
            return new MaterialSet
            {
                // Painted grass tiles every ~8 m across the 100 m ground plane.
                Ground = Lit("Ground", Color.white, smoothness: 0.05f, baseMap: LoadTexture(GroundTexturePath), tiling: new Vector2(12f, 12f)),
                GroundPatch = Lit("GroundPatch", Hex("#3F5A33"), smoothness: 0.05f),
                Stone = Lit("Stone", Hex("#8A8D93"), smoothness: 0.15f),
                Wall = Lit("Wall", Hex("#5A5C66"), smoothness: 0.1f),
                Wood = Lit("Wood", Hex("#7A4E2A"), smoothness: 0.2f),
                Leaves = Lit("Leaves", Hex("#2F6B3A"), smoothness: 0.05f),
                Gold = Lit("Gold", Hex("#E2B243"), smoothness: 0.6f, metallic: 0.8f),

                // Lantern pine (Blender model); slot order matches the FBX sub-meshes.
                // A touch lighter than the Blender source so they still read as teal under the arena's daylight.
                PineBark = Lit("PineBark", Hex("#3A3245"), smoothness: 0.1f),
                PineNeedles = Lit("PineNeedles", Hex("#3F7480"), smoothness: 0.15f),
                PineNeedlesDark = Lit("PineNeedlesDark", Hex("#2A5264"), smoothness: 0.15f),
                LanternIron = Lit("LanternIron", Hex("#1A181E"), smoothness: 0.5f, metallic: 0.6f),
                LanternGlow = Lit("LanternGlow", Hex("#FFE3A0"), emission: Hex("#FFB14A") * 7f),

                // Autumn churchyard props (Blender models); muted, slightly warm.
                PropStone = Lit("PropStone", Hex("#8E8A82"), smoothness: 0.1f),
                PropStoneDark = Lit("PropStoneDark", Hex("#6C6862"), smoothness: 0.1f),
                PropStoneLight = Lit("PropStoneLight", Hex("#A6A298"), smoothness: 0.1f),
                GraveStone = Lit("GraveStone", Hex("#9A968D"), smoothness: 0.1f),
                Moss = Lit("Moss", Hex("#66714A"), smoothness: 0.05f),
                Slate = Lit("Slate", Hex("#4A4852"), smoothness: 0.25f),
                // Glowing water marks a well that is ready to heal; the dark shaft shows once it is drained.
                HealingWater = Lit("HealingWater", Hex("#63D8E6"), smoothness: 0.9f, emission: Hex("#23B5CC") * 1.4f),
                WellDepth = Lit("WellDepth", Hex("#151A19"), smoothness: 0.1f),
                // UVs are in world units (see DirtTileSize), so the texture keeps its scale on every path.
                Dirt = DirtMaterial(),
                AutumnWood = Lit("AutumnWood", Hex("#6B4A30"), smoothness: 0.1f),
                LeafRust = Lit("LeafRust", Hex("#9A6440"), smoothness: 0.1f),
                LeafRustDark = Lit("LeafRustDark", Hex("#74492F"), smoothness: 0.1f),
                LeafOchre = Lit("LeafOchre", Hex("#A88650"), smoothness: 0.1f),
                RockStone = Lit("RockStone", Hex("#716C66"), smoothness: 0.1f),
                RockMoss = Lit("RockMoss", Hex("#5F6A45"), smoothness: 0.05f),

                // Colours (fur markings, cloth, gold) are baked into one atlas in Blender.
                HuskyBody = Lit("WizardHuskyBody", Color.white, smoothness: 0.15f, baseMap: LoadTexture(HuskyAlbedoPath)),
                HuskyCrystal = Lit("WizardHuskyCrystal", Hex("#B8FFF6"), smoothness: 0.8f, emission: Hex("#3CE8D8") * 3.5f),

                Grunt = Lit("EnemyGrunt", Hex("#7DAE45"), smoothness: 0.2f),
                Runner = Lit("EnemyRunner", Hex("#D9A534"), smoothness: 0.2f),
                Brute = Lit("EnemyBrute", Hex("#A8453B"), smoothness: 0.2f),
                Boss = Lit("EnemyBoss", Hex("#4D2470"), smoothness: 0.35f),
                EnemyEyes = Lit("EnemyEyes", Hex("#FFE3A0"), emission: Hex("#FF5A2A") * 4f),
                Bat = Lit("EnemyBat", Hex("#3B2A4A"), smoothness: 0.3f),
                Archer = Lit("EnemyArcher", Hex("#5E7A3A"), smoothness: 0.2f),
                Wisp = Lit("EnemyWisp", Hex("#B8F4FF"), emission: Hex("#35C8FF") * 2.5f),
                Bomber = Lit("EnemyBomber", Hex("#8C8A3C"), smoothness: 0.2f),
                Shaman = Lit("EnemyShaman", Hex("#3A2455"), smoothness: 0.3f),
                Boar = Lit("EnemyBoar", Hex("#6B4128"), smoothness: 0.2f),
                Beetle = Lit("EnemyBeetle", Hex("#5A1414"), smoothness: 0.7f, metallic: 0.3f),
                Slime = Lit("EnemySlime", Hex("#7CDB4A"), smoothness: 0.85f),
                Wyvern = Lit("EnemyWyvern", Hex("#2F6F8F"), smoothness: 0.45f),
                WindupGlow = Lit("EnemyWindupGlow", Hex("#FFB070"), emission: Hex("#FF4A10") * 6f),
                CurseGlow = Lit("EnemyCurseGlow", Hex("#C8FFB0"), emission: Hex("#4DFF3A") * 4f),
                BombBody = Lit("BombBody", Hex("#1C1C22"), smoothness: 0.6f),
                EnemyArrow = Lit("EnemyArrow", Hex("#FFD8A0"), emission: Hex("#FF8A20") * 4f),
                WispBolt = Lit("WispBolt", Hex("#D8FAFF"), emission: Hex("#30B8FF") * 5f),

                Flash = Lit("HitFlash", Color.white, emission: Color.white * 1.5f),

                PlayerBolt = Lit("PlayerBolt", Hex("#BFF3FF"), emission: Hex("#38C8FF") * 5f),
                Fireball = Lit("Fireball", Hex("#FFC060"), emission: Hex("#FF6A00") * 6f),
                BossOrb = Lit("BossOrb", Hex("#F0A0FF"), emission: Hex("#C030FF") * 5f),
                Meteor = Lit("Meteor", Hex("#5A2B18"), emission: Hex("#FF4A00") * 3f),
                Gem = Lit("ExperienceGem", Hex("#A6FFD8"), emission: Hex("#20E0A0") * 3f),
                ArcaneOrb = Lit("ArcaneOrb", Hex("#E2C8FF"), emission: Hex("#9A4DFF") * 5f),


                EnemyTelegraph = Transparent("EnemyTelegraph", new Color(1f, 0.2f, 0.25f, 0.18f)),
                EnemyTelegraphFill = Transparent("EnemyTelegraphFill", new Color(1f, 0.15f, 0.2f, 0.4f)),
                StrikeEnemyRing = Transparent("TelegraphEnemyRing", new Color(1f, 0.25f, 0.25f, 0.9f), LoadVfxTexture("vfx_ring_thin")),
                StrikeEnemyFill = Transparent("TelegraphEnemyFill", new Color(1f, 0.15f, 0.2f, 0.4f), LoadVfxTexture("vfx_disc")),

                Particles = ParticleMaterial(),
                MissionZone = Transparent("MissionZone", new Color(1f, 0.8f, 0.3f, 0.14f)),
                MissionZoneFill = Transparent("MissionZoneFill", new Color(1f, 0.8f, 0.25f, 0.32f)),
                ArcaneShard = Lit("ArcaneShard", Hex("#F2D0FF"), emission: Hex("#C04DFF") * 5f),
                PointerGlow = Lit("MissionPointer", Hex("#FFE9A8"), emission: Hex("#FFB020") * 3f),
                HealthBar = HealthBarMaterial(),
                DamageText = DamageTextMaterial(),
            };
        }

        private static Material Lit(string name, Color baseColor, float smoothness = 0.3f, float metallic = 0f, Color? emission = null,
            Texture2D baseMap = null, Vector2? tiling = null)
        {
            var material = LoadOrCreateMaterial(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", tiling ?? Vector2.one);

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

        private static Material Transparent(string name, Color color, Texture2D texture = null)
        {
            var material = LoadOrCreateMaterial(name, "Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            BaseShaderGUI.SetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Lit and alpha-blended by vertex colour, so the fringe of the path mesh (vertex alpha fading to 0) blends
        /// the dirt softly into the grass. The URP Lit shader ignores vertex colour; the particle Lit shader uses it.
        /// </summary>
        private static Material DirtMaterial()
        {
            var material = LoadOrCreateMaterial("Dirt", "Universal Render Pipeline/Particles/Lit");
            material.SetTexture("_BaseMap", LoadTexture(DirtTexturePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.05f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ReceiveShadows", 1f);
            // Draw before every other transparent (queue 2950 vs 3000), so effects on the ground land on top of the
            // paths instead of being blended under them. URP recomputes the queue from this offset.
            material.SetFloat("_QueueOffset", -50f);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            BaseShaderGUI.SetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material ParticleMaterial()
        {
            var material = LoadOrCreateMaterial("Particles", "Universal Render Pipeline/Particles/Unlit");
            // Soft glow with a hot core: every generic burst reads as light rather than a blurry square.
            material.SetTexture("_BaseMap", LoadVfxTexture("vfx_glow"));

            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f); // Additive: glows nicely with bloom.
            BaseShaderGUI.SetupMaterialBlendMode(material);
            BaseShaderGUI.SetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material HealthBarMaterial()
        {
            var material = LoadOrCreateMaterial("EnemyHealthBar", "BattleArena/HealthBar");
            material.SetColor("_FillColor", Hex("#E0443E"));
            material.SetColor("_BackColor", new Color(0.05f, 0.05f, 0.08f, 0.75f));
            material.SetColor("_BorderColor", new Color(0f, 0f, 0f, 0.9f));
            material.SetVector("_Size", new Vector4(1.2f, 0.16f, 0f, 0f));
            material.SetFloat("_Border", 0.025f);
            // Bars differ only by the per-instance _Fill, so they batch into instanced draws.
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Copy of the default TMP font material that draws on top of everything, with an outline for readability.</summary>
        private static Material DamageTextMaterial()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(DefaultFontPath);
            var material = LoadOrCreateMaterial("DamageText", "TextMeshPro/Mobile/Distance Field Overlay");
            material.CopyPropertiesFromMaterial(font.material);
            material.SetColor("_OutlineColor", new Color(0.08f, 0.04f, 0.02f, 1f));
            // A thin outline plus a slightly dilated face keeps the numbers bright and readable.
            material.SetFloat("_OutlineWidth", 0.2f);
            material.SetFloat("_FaceDilate", 0.25f);
            material.EnableKeyword("OUTLINE_ON");
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

        /// <summary>Tiling colour texture: repeat wrap, mipmaps and some anisotropy for the tilted camera.</summary>
        private static Texture2D LoadTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return null;

            if (importer.wrapMode != TextureWrapMode.Repeat || importer.anisoLevel != 4 || importer.maxTextureSize != 1024)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.anisoLevel = 4;
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
    }
}
