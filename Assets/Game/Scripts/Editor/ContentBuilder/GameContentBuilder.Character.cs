using System.Collections.Generic;
using System.Linq;
using BattleArena.Player;
using BattleArena.Vfx;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BattleArena.Editor.ContentBuilder
{
    /// <summary>
    /// The wizard husky: a skinned model (modelled, rigged and animated in Blender) with a baked albedo atlas and a
    /// glowing crystal. Its "crystal" bone rides on the staff hand and is where spells and shots come from.
    /// </summary>
    internal static partial class GameContentBuilder
    {
        private const string HuskyFolder = Root + "/Art/Characters/WizardHusky";
        private const string HuskyModelPath = HuskyFolder + "/WizardHusky.fbx";
        private const string HuskyAlbedoPath = HuskyFolder + "/WizardHusky_Albedo.png";
        private const string HuskyControllerPath = HuskyFolder + "/WizardHusky.controller";
        private const string HuskyUpperBodyMaskPath = HuskyFolder + "/WizardHuskyUpperBody.mask";

        /// <summary>Clips that play once; every other take loops.</summary>
        private static readonly HashSet<string> OneShotClips = new() { "Cast" };

        /// <summary>
        /// Bones the Cast clip drives: the torso twist and the staff arm. The legs, tail and free arm stay with
        /// Idle/Run underneath.
        /// </summary>
        private static readonly HashSet<string> CastBones = new() { "spine", "chest", "head", "upperarm.R", "forearm.R", "hand.R", "crystal" };

        /// <summary>Base to tip.</summary>
        private static readonly string[] TailBones = { "tail.1", "tail.2", "tail.3" };

        private sealed class CharacterVisual
        {
            public Renderer Body;
            public Transform Muzzle;
        }

        /// <summary>Adds the animated husky under <paramref name="root"/> and wires its animator to the movement and casts.</summary>
        private static CharacterVisual AddWizardHusky(GameObject root, MaterialSet m, PlayerController movement, FollowTarget castFlare)
        {
            var model = LoadCharacterModel(HuskyModelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            instance.name = "WizardHusky";
            SetLayerRecursively(instance, root.layer);

            var body = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            // Slot order from Blender: baked body atlas, then the staff crystal.
            body.sharedMaterials = new[] { m.HuskyBody, m.HuskyCrystal };

            var animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = BuildHuskyController(model);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var muzzle = instance.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "crystal");
            if (muzzle == null)
                Debug.LogWarning("WizardHusky has no 'crystal' bone; spells will fire from the character root.");
            muzzle = muzzle != null ? muzzle : root.transform;

            var driver = root.AddComponent<PlayerAnimator>();
            SerializedSetter.For(driver)
                .Set("animator", animator)
                .Set("controller", movement)
                .Set("castFlare", castFlare)
                .Set("muzzle", muzzle)
                .Apply();

            // The tail sways procedurally on its own rhythm, over whatever the clips key on it.
            var bones = instance.GetComponentsInChildren<Transform>();
            var tail = TailBones.Select(name => bones.FirstOrDefault(t => t.name == name)).ToArray();
            if (tail.All(t => t != null))
            {
                var sway = instance.AddComponent<TailSway>();
                SerializedSetter.For(sway).Set("segments", tail).Set("body", root.transform).Set("controller", movement).Apply();
            }
            else
            {
                Debug.LogWarning("WizardHusky is missing tail bones; the tail will only follow the clips.");
            }

            return new CharacterVisual { Body = body, Muzzle = muzzle };
        }

        /// <summary>
        /// Generic rig, clips named after their Blender actions (looping unless listed in <see cref="OneShotClips"/>),
        /// geometry only (materials come from here).
        /// </summary>
        private static GameObject LoadCharacterModel(string path)
        {
            if (AssetImporter.GetAtPath(path) is ModelImporter importer)
            {
                var clips = importer.clipAnimations;
                // A new action exported from Blender shows up as an extra default take, so compare counts too.
                var configured = importer.animationType == ModelImporterAnimationType.Generic &&
                                 importer.materialImportMode == ModelImporterMaterialImportMode.None &&
                                 clips.Length > 0 && clips.Length == importer.defaultClipAnimations.Length &&
                                 clips.All(c => !c.name.Contains("|") && c.loopTime == !OneShotClips.Contains(c.name));
                if (!configured)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.importAnimation = true;
                    importer.importCameras = false;
                    importer.importLights = false;
                    importer.importBlendShapes = false;
                    clips = importer.defaultClipAnimations;
                    foreach (var clip in clips)
                    {
                        clip.name = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                        clip.loopTime = !OneShotClips.Contains(clip.name);
                    }
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>
        /// Idle while standing, Run while moving (its playback rate follows the movement speed), and an upper-body
        /// Cast layer that <see cref="PlayerAnimator"/> fades in for each spell.
        /// </summary>
        private static AnimatorController BuildHuskyController(GameObject model)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(HuskyControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(HuskyControllerPath);

            // Rebuilt in place on every run so the asset keeps its GUID.
            controller.parameters = new AnimatorControllerParameter[0];
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter { name = "RunRate", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states.ToArray())
                machine.RemoveState(state.state);

            var clips = AssetDatabase.LoadAllAssetsAtPath(HuskyModelPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview")).ToArray();
            AnimationClip Clip(string name) => clips.FirstOrDefault(c => c.name == name);

            var idle = machine.AddState("Idle");
            idle.motion = Clip("Idle");
            var run = machine.AddState("Run");
            run.motion = Clip("Run");
            run.speedParameterActive = true;
            run.speedParameter = "RunRate";
            machine.defaultState = idle;

            var toRun = idle.AddTransition(run);
            toRun.hasExitTime = false;
            toRun.duration = 0.1f;
            toRun.AddCondition(AnimatorConditionMode.Greater, 0.4f, "Speed");
            var toIdle = run.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.15f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.4f, "Speed");

            // Cast: an override layer limited to the torso and staff arm. Its weight stays at zero until a spell is
            // cast, so the one state can simply sit at the end of its clip in between.
            while (controller.layers.Length > 1)
                controller.RemoveLayer(1);
            controller.AddLayer("Cast");
            var layers = controller.layers;
            layers[1].defaultWeight = 0f;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[1].avatarMask = BuildUpperBodyMask(model);
            controller.layers = layers;
            var castMachine = layers[1].stateMachine;
            var cast = castMachine.AddState("Cast");
            cast.motion = Clip("Cast");
            castMachine.defaultState = cast;
            if (cast.motion == null)
                Debug.LogWarning("WizardHusky has no 'Cast' take; spells will play without a cast animation.");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>A transform mask over the husky's skeleton with only <see cref="CastBones"/> switched on.</summary>
        private static AvatarMask BuildUpperBodyMask(GameObject model)
        {
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(HuskyUpperBodyMaskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, HuskyUpperBodyMaskPath);
            }

            var bones = model.GetComponentsInChildren<Transform>(true).Where(t => t != model.transform).ToArray();
            mask.transformCount = bones.Length;
            for (var i = 0; i < bones.Length; i++)
            {
                mask.SetTransformPath(i, AnimationUtility.CalculateTransformPath(bones[i], model.transform));
                mask.SetTransformActive(i, CastBones.Contains(bones[i].name));
            }

            EditorUtility.SetDirty(mask);
            return mask;
        }
    }
}
