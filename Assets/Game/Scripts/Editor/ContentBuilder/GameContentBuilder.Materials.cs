using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleArena.Editor.ContentBuilder
{
    /// <summary>Builder part: materials.</summary>
    internal static partial class GameContentBuilder
    {
        /// <summary>Every material the builder assigns, created or updated once per build.</summary>
        private sealed class MaterialSet
        {
            /// <summary>Arena ground, and the gold trim on chests and bombs.</summary>
            public Material Ground, Gold;
            /// <summary>Lantern pine.</summary>
            public Material PineBark, PineNeedles, PineNeedlesDark, LanternIron, LanternGlow;
            /// <summary>Graves, walls, wells and other stone props.</summary>
            public Material PropStone, PropStoneDark, PropStoneLight, GraveStone, Moss, Slate, HealingWater, WellDepth, Dirt;
            /// <summary>Autumn trees and rocks.</summary>
            public Material AutumnWood, LeafRust, LeafRustDark, LeafOchre, RockStone, RockMoss;
            /// <summary>The player character.</summary>
            public Material HuskyBody, HuskyCrystal;
            /// <summary>Monster atlases (shared, the Grunt's recolour and the black dragon).</summary>
            public Material MonsterAtlas, MonsterGrunt, MonsterBlackDragon;
            /// <summary>Chest, arrows and attack telegraphs.</summary>
            public Material ChestWood, ChestIron, Fletching, TelegraphLane, TelegraphLaneFill;
            /// <summary>The goblin bomb's casing.</summary>
            public Material BombBody;
            /// <summary>Hit flash.</summary>
            public Material Flash;
            /// <summary>Meteor cores and experience gems.</summary>
            public Material Meteor, Gem;
            /// <summary>Enemy strike markers.</summary>
            public Material StrikeEnemyRing, StrikeEnemyFill;
            /// <summary>Generic particle bursts.</summary>
            public Material Particles;
            /// <summary>Health bars, damage numbers and the frost overlay.</summary>
            public Material HealthBar, DamageText, FrostOverlay;
            /// <summary>Mission zones, shards and the objective pointer.</summary>
            public Material MissionZone, MissionZoneFill, ArcaneShard, MissionCrystal, PointerGlow;
        }

        /// <summary>Creates or updates every material.</summary>
        private static MaterialSet BuildMaterials()
        {
            return new MaterialSet
            {
                // Painted grass tiles every ~8 m across the 100 m ground plane.
                Ground = Lit("Ground", Color.white, smoothness: 0.05f, baseMap: LoadTexture(GroundTexturePath), tiling: new Vector2(GroundScale * 1.2f, GroundScale * 1.2f)),
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
                // The staff orb is purple on the Tripo model; glow to match.
                HuskyCrystal = Lit("WizardHuskyCrystal", Hex("#E4CCFF"), smoothness: 0.8f, emission: Hex("#9B4DFF") * 3.5f),

                // Quaternius monsters share one flat-colour atlas (saturated for the game); the grunt gets its own
                // copy with a warm skin that reads against the grass, and the boss a black one.
                MonsterAtlas = MonsterMaterial("MonsterAtlas", MonsterAtlasPath, Color.white, 0.2f, 0f),
                MonsterGrunt = MonsterMaterial("MonsterGrunt", MonsterGruntAtlasPath, Color.white, 0.2f, 0f),
                // A touch glossier than the rest, so the black hide still shows its shape under the lights.
                MonsterBlackDragon = MonsterMaterial("MonsterBlackDragon", MonsterBlackDragonAtlasPath, Color.white, 0.35f, 0f),
                BombBody = Lit("BombBody", Hex("#1C1C22"), smoothness: 0.6f),

                Flash = Lit("HitFlash", Color.white, emission: Color.white * 1.5f),

                Meteor = Lit("Meteor", Hex("#5A2B18"), emission: Hex("#FF4A00") * 3f),
                Gem = Lit("ExperienceGem", Hex("#A6FFD8"), emission: Hex("#20E0A0") * 3f),

                TelegraphLane = Transparent("TelegraphEnemyLane", new Color(1f, 0.28f, 0.25f, 0.9f), LoadVfxTexture("vfx_lane")),
                TelegraphLaneFill = Transparent("TelegraphEnemyLaneFill", new Color(1f, 0.2f, 0.2f, 0.45f), LoadVfxTexture("vfx_lane")),
                StrikeEnemyRing = Transparent("TelegraphEnemyRing", new Color(1f, 0.25f, 0.25f, 0.9f), LoadVfxTexture("vfx_ring_thin")),
                StrikeEnemyFill = Transparent("TelegraphEnemyFill", new Color(1f, 0.15f, 0.2f, 0.4f), LoadVfxTexture("vfx_disc")),

                Particles = ParticleMaterial(),
                MissionZone = Transparent("MissionZone", new Color(1f, 0.8f, 0.3f, 0.95f), LoadVfxTexture("vfx_ring_thin")),
                MissionZoneFill = Transparent("MissionZoneFill", new Color(1f, 0.78f, 0.25f, 0.4f), LoadVfxTexture("vfx_disc")),
                ChestWood = Lit("ChestWood", Hex("#7A4E2E"), smoothness: 0.2f),
                ChestIron = Lit("ChestIron", Hex("#3C3E45"), smoothness: 0.45f, metallic: 0.6f),
                Fletching = Lit("ArrowFletching", Hex("#C8402E"), smoothness: 0.3f),
                ArcaneShard = Lit("ArcaneShard", Hex("#F2D0FF"), emission: Hex("#C04DFF") * 5f),
                MissionCrystal = Lit("MissionCrystal", Hex("#B070E8"), emission: Hex("#9A3BE0") * 1.2f),
                PointerGlow = Lit("MissionPointer", Hex("#FFE9A8"), emission: Hex("#FFB020") * 3f),
                HealthBar = HealthBarMaterial(),
                FrostOverlay = FrostOverlayMaterial(),
                DamageText = DamageTextMaterial(),
            };
        }

        /// <summary>Creates or updates a URP Lit material.</summary>
        private static Material Lit(string name, Color baseColor, float smoothness = 0.3f, float metallic = 0f, Color? emission = null,
            Texture2D baseMap = null, Vector2? tiling = null)
        {
            var material = LoadOrCreateMaterial(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", tiling ?? Vector2.one);

            // URP re-derives _EMISSION whenever it validates a material, and only keeps it when the GI flags are
            // emissive, so a hand-set keyword with GI flags "None" silently switches off. The scene has no realtime
            // GI, so RealtimeEmissive costs nothing.
            material.SetColor("_EmissionColor", emission ?? Color.black);
            material.globalIlluminationFlags = emission.HasValue
                ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            BaseShaderGUI.SetMaterialKeywords(material);

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Creates or updates a transparent URP Unlit material.</summary>
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
            // Plain alpha blending, not premultiplied: with premultiply the fog colour is mixed in after the alpha
            // scaling, so the fading fringe glows with fog-coloured outlines. The dirt is near-matte anyway.
            material.SetFloat("_BlendModePreserveSpecular", 0f);
            material.SetFloat("_ReceiveShadows", 1f);
            // Draw before every other transparent (queue 2950 vs 3000), so effects on the ground land on top of the
            // paths instead of being blended under them. URP recomputes the queue from this offset.
            material.SetFloat("_QueueOffset", -50f);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            BaseShaderGUI.SetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>The shared additive particle material.</summary>
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

        /// <summary>Icy fresnel rim drawn over frost-slowed enemies (see EnemyChillVisual).</summary>
        private static Material FrostOverlayMaterial()
        {
            var material = LoadOrCreateMaterial("FrostOverlay", "BattleArena/FrostFresnel");
            material.SetColor("_RimColor", new Color(0.45f, 0.85f, 1.6f));
            material.SetFloat("_RimPower", 2.2f);
            material.SetFloat("_Fill", 0.12f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>The world-space health bar material.</summary>
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

        /// <summary>
        /// Baloo 2 ExtraBold (the UI's display face) as a TextMesh Pro font, so damage numbers match the timer and titles.
        /// Baked once with only the glyphs damage numbers use, into a static atlas: builds need no font file at runtime.
        /// </summary>
        private static TMPro.TMP_FontAsset LoadOrCreateDamageFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(DamageFontPath);
            if (existing != null)
                return existing;

            var source = AssetDatabase.LoadAssetAtPath<Font>(DamageFontSourcePath);
            var font = TMPro.TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                512, 512, TMPro.AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
            font.name = "Baloo2-ExtraBold SDF";
            font.TryAddCharacters(DamageFontCharacters, out _);
            font.atlasPopulationMode = TMPro.AtlasPopulationMode.Static;

            AssetDatabase.CreateAsset(font, DamageFontPath);
            font.atlasTexture.name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return font;
        }

        /// <summary>Copy of the damage font's material that draws on top of everything, with an outline for readability.</summary>
        private static Material DamageTextMaterial()
        {
            var font = LoadOrCreateDamageFont();
            var material = LoadOrCreateMaterial("DamageText", "TextMeshPro/Mobile/Distance Field Overlay");
            material.CopyPropertiesFromMaterial(font.material);
            material.SetColor("_OutlineColor", new Color(0.08f, 0.04f, 0.02f, 1f));
            // A thin outline keeps the numbers readable over effects; Baloo is already heavy, so the face is barely dilated.
            material.SetFloat("_OutlineWidth", 0.2f);
            material.SetFloat("_FaceDilate", 0.1f);
            material.EnableKeyword("OUTLINE_ON");
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Loads the material <paramref name="name"/> with <paramref name="shaderName"/>, creating it if needed.
        /// </summary>
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

        /// <summary>Parses "#RRGGBB"; magenta flags a typo.</summary>
        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
    }
}
