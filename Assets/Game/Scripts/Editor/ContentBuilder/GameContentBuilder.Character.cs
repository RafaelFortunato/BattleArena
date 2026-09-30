using System.Linq;
using BattleArena.Player;
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

        private sealed class CharacterVisual
        {
            public Renderer Body;
            public Transform Muzzle;
        }

        /// <summary>Adds the animated husky under <paramref name="root"/> and wires its animator to the movement.</summary>
        private static CharacterVisual AddWizardHusky(GameObject root, MaterialSet m, PlayerController movement)
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
            animator.runtimeAnimatorController = BuildHuskyController();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var driver = root.AddComponent<PlayerAnimator>();
            SerializedSetter.For(driver).Set("animator", animator).Set("controller", movement).Apply();

            var muzzle = instance.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "crystal");
            if (muzzle == null)
                Debug.LogWarning("WizardHusky has no 'crystal' bone; spells will fire from the character root.");
            return new CharacterVisual { Body = body, Muzzle = muzzle != null ? muzzle : root.transform };
        }

        /// <summary>Generic rig, looping clips named after their Blender actions, geometry only (materials come from here).</summary>
        private static GameObject LoadCharacterModel(string path)
        {
            if (AssetImporter.GetAtPath(path) is ModelImporter importer)
            {
                var clips = importer.clipAnimations;
                var configured = importer.animationType == ModelImporterAnimationType.Generic &&
                                 importer.materialImportMode == ModelImporterMaterialImportMode.None &&
                                 clips.Length > 0 && clips.All(c => c.loopTime && !c.name.Contains("|"));
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
                        clip.loopTime = true;
                        clip.name = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                    }
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>Idle while standing, Run while moving (its playback rate follows the movement speed).</summary>
        private static AnimatorController BuildHuskyController()
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

            EditorUtility.SetDirty(controller);
            return controller;
        }
    }
}
