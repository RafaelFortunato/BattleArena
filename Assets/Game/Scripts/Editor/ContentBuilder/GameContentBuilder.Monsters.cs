using System.Collections.Generic;
using System.Linq;
using BattleArena.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BattleArena.Editor.ContentBuilder
{
    /// <summary>
    /// Enemy models from Quaternius' "Ultimate Monsters" (CC0): rigged and animated FBX files sharing one colour
    /// atlas. Each enemy gets its model scaled to a target height and an override of a shared animator controller
    /// (Idle / Move / Attack / Hit) mapped to that model's clips.
    /// </summary>
    internal static partial class GameContentBuilder
    {
        private const string MonstersFolder = Root + "/Art/Characters/Monsters";
        private const string MonsterAnimFolder = MonstersFolder + "/Animation";
        private const string MonsterAtlasPath = MonstersFolder + "/Atlas_Monsters_Vivid.png";
        private const string MonsterGruntAtlasPath = MonstersFolder + "/Atlas_Monsters_Grunt.png";
        /// <summary>Self-illumination from the atlas, so colours keep their punch in shade and under ACES tonemapping.</summary>
        private const float MonsterEmission = 0.28f;

        private static readonly string[] LoopingClips = { "Idle", "Walk", "Run", "Flying_Idle", "Fast_Flying" };

        /// <summary>Which model an enemy uses and how it is animated.</summary>
        private readonly struct MonsterSpec
        {
            public readonly string Model;
            public readonly float Height;
            /// <summary>Height of the model's lowest point; above zero for hovering monsters.</summary>
            public readonly float Hover;
            public readonly string Idle, Move, Attack, Hit;
            /// <summary>Movement speed at which the move clip plays at its authored rate.</summary>
            public readonly float ReferenceSpeed;
            public readonly Material Material;

            public MonsterSpec(string model, float height, float referenceSpeed, string move = "Walk", string attack = "Punch",
                string idle = "Idle", string hit = "HitReact", float hover = 0f, Material material = null)
            {
                Model = model;
                Height = height;
                ReferenceSpeed = referenceSpeed;
                Move = move;
                Attack = attack;
                Idle = idle;
                Hit = hit;
                Hover = hover;
                Material = material;
            }

            public static MonsterSpec Flyer(string model, float height, float hover, float referenceSpeed, string attack = "Headbutt") =>
                new(model, height, referenceSpeed, move: "Fast_Flying", attack: attack, idle: "Flying_Idle", hover: hover);

            public static MonsterSpec Blob(string model, float height, float referenceSpeed) =>
                new(model, height, referenceSpeed, attack: "Bite_Front", hit: "HitRecieve");
        }

        private static AnimatorController _monsterController;

        /// <summary>
        /// Adds the monster model under <paramref name="visual"/> (a child of the enemy root) and an
        /// <see cref="EnemyAnimator"/> on the root. Returns the main renderer.
        /// </summary>
        private static Renderer Monster(Transform visual, in MonsterSpec spec, MaterialSet m, string enemyName)
        {
            var path = $"{MonstersFolder}/{spec.Model}.fbx";
            var model = LoadMonsterModel(path);
            if (model == null)
            {
                Debug.LogWarning($"Missing monster model {path}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
            instance.name = spec.Model;
            SetLayerRecursively(instance, visual.gameObject.layer);

            var renderers = instance.GetComponentsInChildren<Renderer>();
            var material = spec.Material != null ? spec.Material : m.MonsterAtlas;
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = material;
                if (renderer is SkinnedMeshRenderer skinned)
                    skinned.updateWhenOffscreen = false;
            }

            // Normalise: target height, centred on the root, feet on the ground (or hovering).
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            // Measured in the visual's space: the enemy root is already scaled.
            var localMin = visual.InverseTransformPoint(bounds.min);
            var localMax = visual.InverseTransformPoint(bounds.max);
            var localCenter = (localMin + localMax) * 0.5f;
            var scale = spec.Height / Mathf.Max(0.01f, localMax.y - localMin.y);
            instance.transform.localScale *= scale;
            instance.transform.localPosition = new Vector3(-localCenter.x * scale, spec.Hover - localMin.y * scale, -localCenter.z * scale);

            var animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var clips = LoadClips(path);
            animator.runtimeAnimatorController = BuildMonsterOverride(enemyName, clips, spec);

            var root = visual.parent.gameObject;
            var driver = root.AddComponent<EnemyAnimator>();
            SerializedSetter.For(driver)
                .Set("animator", animator)
                .Set("referenceSpeed", spec.ReferenceSpeed)
                .Set("attackClipLength", Clip(clips, spec.Attack, spec.Idle).length)
                .Apply();

            return renderers.OrderByDescending(r => r.bounds.size.sqrMagnitude).First();
        }

        /// <summary>Generic rig, geometry only (materials come from the builder), clips named without the armature prefix.</summary>
        private static GameObject LoadMonsterModel(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                return null;

            var clips = importer.clipAnimations;
            var configured = importer.animationType == ModelImporterAnimationType.Generic &&
                             importer.materialImportMode == ModelImporterMaterialImportMode.None &&
                             clips.Length > 0 && clips.All(c => !c.name.Contains("|"));
            if (!configured)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importBlendShapes = false;
                importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    clip.name = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                    clip.loopTime = LoopingClips.Contains(clip.name);
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static AnimationClip[] LoadClips(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();

        private static AnimationClip Clip(AnimationClip[] clips, string name, string fallback)
        {
            var clip = clips.FirstOrDefault(c => c.name == name) ?? clips.FirstOrDefault(c => c.name == fallback);
            if (clip == null || clip.name != name)
                Debug.LogWarning($"Monster clip '{name}' not found; using '{clip?.name}'.");
            return clip;
        }

        /// <summary>Shared layout every monster uses: Idle ⇄ Move on Speed, Attack and Hit on triggers.</summary>
        private static AnimatorController MonsterController()
        {
            if (_monsterController != null)
                return _monsterController;

            EnsureFolder(MonsterAnimFolder);
            var path = $"{MonsterAnimFolder}/Monster.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);

            // Placeholder clips that every override replaces; taken from the orc.
            var baseClips = LoadClips($"{MonstersFolder}/Orc.fbx");
            controller.parameters = new AnimatorControllerParameter[0];
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter { name = "MoveRate", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            controller.AddParameter(new AnimatorControllerParameter { name = "AttackRate", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states.ToArray())
                machine.RemoveState(state.state);
            foreach (var transition in machine.anyStateTransitions.ToArray())
                machine.RemoveAnyStateTransition(transition);

            var idle = machine.AddState("Idle");
            idle.motion = Clip(baseClips, "Idle", "Idle");
            var move = machine.AddState("Move");
            move.motion = Clip(baseClips, "Walk", "Idle");
            move.speedParameterActive = true;
            move.speedParameter = "MoveRate";
            var attack = machine.AddState("Attack");
            attack.motion = Clip(baseClips, "Punch", "Idle");
            attack.tag = "Attack";
            attack.speedParameterActive = true;
            attack.speedParameter = "AttackRate";
            var hit = machine.AddState("Hit");
            hit.motion = Clip(baseClips, "HitReact", "Idle");
            hit.tag = "Hit";
            machine.defaultState = idle;

            var toMove = idle.AddTransition(move);
            toMove.hasExitTime = false;
            toMove.duration = 0.12f;
            toMove.AddCondition(AnimatorConditionMode.Greater, 0.35f, "Speed");
            var toIdle = move.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.18f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.25f, "Speed");

            var toAttack = machine.AddAnyStateTransition(attack);
            toAttack.hasExitTime = false;
            toAttack.duration = 0.06f;
            toAttack.canTransitionToSelf = false;
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            var attackDone = attack.AddTransition(idle);
            attackDone.hasExitTime = true;
            attackDone.exitTime = 0.92f;
            attackDone.duration = 0.1f;

            var toHit = machine.AddAnyStateTransition(hit);
            toHit.hasExitTime = false;
            toHit.duration = 0.05f;
            toHit.canTransitionToSelf = false;
            toHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            var hitDone = hit.AddTransition(idle);
            hitDone.hasExitTime = true;
            hitDone.exitTime = 0.85f;
            hitDone.duration = 0.1f;

            EditorUtility.SetDirty(controller);
            _monsterController = controller;
            return controller;
        }

        private static AnimatorOverrideController BuildMonsterOverride(string enemyName, AnimationClip[] clips, in MonsterSpec spec)
        {
            var controller = MonsterController();
            var path = $"{MonsterAnimFolder}/{enemyName}.overrideController";
            var overrides = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (overrides == null)
            {
                overrides = new AnimatorOverrideController(controller);
                AssetDatabase.CreateAsset(overrides, path);
            }
            overrides.runtimeAnimatorController = controller;

            var byState = new Dictionary<string, AnimationClip>
            {
                ["Idle"] = Clip(clips, spec.Idle, spec.Idle),
                ["Move"] = Clip(clips, spec.Move, spec.Idle),
                ["Attack"] = Clip(clips, spec.Attack, spec.Idle),
                ["Hit"] = Clip(clips, spec.Hit, spec.Idle),
            };
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (var state in controller.layers[0].stateMachine.states)
                if (state.state.motion is AnimationClip placeholder && byState.TryGetValue(state.state.name, out var clip))
                    pairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(placeholder, clip));
            overrides.ApplyOverrides(pairs);
            EditorUtility.SetDirty(overrides);
            return overrides;
        }

        /// <summary>
        /// The colour atlas is a grid of flat swatches: point sampling, no mips and no compression, so colours never
        /// bleed or shift (it is tiny either way).
        /// </summary>
        private static Texture2D LoadMonsterAtlas(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return null;
            if (importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material MonsterMaterial(string name, string atlasPath, Color tint, float smoothness, float metallic)
        {
            var atlas = LoadMonsterAtlas(atlasPath);
            var material = Lit(name, tint, smoothness: smoothness, metallic: metallic, emission: tint * MonsterEmission, baseMap: atlas);
            material.SetTexture("_EmissionMap", atlas);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
