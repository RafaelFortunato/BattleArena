using System.IO;
using System.Linq;
using BattleArena.Core;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace BattleArena.Editor.ContentBuilder
{
    internal static partial class GameContentBuilder
    {
        private const string EnvironmentPrefabsFolder = PrefabsFolder + "/Environment";
        private const string ModelsFolder = Root + "/Art/Models";
        private const string GroundTexturePath = Root + "/Art/Textures/GroundGrass.png";
        private const float WallSegmentLength = 6f;

        /// <summary>
        /// Environment props of the autumn churchyard. Each is a prefab with its collider on the root and its
        /// visuals under a "Model" child, so art can be swapped by editing the prefab. Prefabs are only generated
        /// when missing: replaced art survives rebuilds (delete a prefab to regenerate it).
        /// Only large obstacles block movement and projectiles; paths and leaf litter are flat decoration.
        /// </summary>
        private sealed class EnvironmentProps
        {
            public GameObject AutumnTree, LanternPine, Boulder, Slab, CelticCross, Headstone, WallSegment, GroundPaths, LeafLitter;
        }

        private static EnvironmentProps BuildEnvironmentProps(MaterialSet m) => new()
        {
            AutumnTree = PropPrefab("AutumnTree", root =>
            {
                AddModel(root, "Trees/AutumnTree.fbx", m.AutumnWood, m.LeafRustDark, m.LeafRust, m.LeafOchre);
                AddCapsule(root, 0.55f, 2.4f);
            }),
            LanternPine = PropPrefab("LanternPine", root =>
            {
                AddModel(root, "Trees/LanternPine.fbx", m.PineBark, m.PineNeedles, m.PineNeedlesDark, m.LanternIron, m.LanternGlow);
                // The lowest boughs spread wide and low, so the obstacle covers more than the trunk.
                AddCapsule(root, 0.9f, 2.4f);
            }),
            Boulder = PropPrefab("Boulder", root => AddMeshCollider(root, AddModel(root, "Rocks/RockBoulder.fbx", m.RockStone, m.RockMoss))),
            Slab = PropPrefab("Slab", root => AddMeshCollider(root, AddModel(root, "Rocks/RockSlab.fbx", m.RockStone, m.RockMoss))),
            // Grave markers are built twice the source size: few, big and easy to read (and to steer around).
            CelticCross = PropPrefab("CelticCross", root =>
            {
                AddModel(root, "Props/CelticCross.fbx", 2f, m.GraveStone, m.PropStoneDark);
                AddBox(root, new Vector3(1.3f, 2.6f, 0.8f));
            }),
            Headstone = PropPrefab("Headstone", root =>
            {
                AddModel(root, "Props/Headstone.fbx", 2f, m.GraveStone, m.PropStoneDark, m.Moss);
                AddBox(root, new Vector3(1.6f, 2f, 0.75f));
            }),
            WallSegment = PropPrefab("WallSegment", root =>
            {
                AddModel(root, "Props/StoneWall.fbx", m.PropStone, m.PropStoneDark, m.PropStoneLight, m.Moss);
                AddBox(root, new Vector3(WallSegmentLength, 1.6f, 0.92f));
                // Wall tops are wide enough to bake as walkable islands; exclude them.
                var modifier = root.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = NavMesh.GetAreaFromName("Not Walkable");
            }),
            // Flat ground decoration: lifted slightly above the grass, no collider, no shadow.
            GroundPaths = GroundPathsPrefab(m),
            LeafLitter = PropPrefab("LeafLitter", root => AddDecal(root, "Props/LeafLitter.fbx", 0.03f, m.LeafRust, m.LeafOchre, m.LeafRustDark)),
        };

        /// <summary>The path network follows the scene layout, so its mesh is regenerated on every build.</summary>
        private static GameObject GroundPathsPrefab(MaterialSet m)
        {
            var mesh = BuildPathMesh();
            return PropPrefab("GroundPaths", root => AddDecal(root, mesh, 0.02f, m.Dirt));
        }

        private static GameObject PropPrefab(string name, System.Action<GameObject> build)
        {
            var path = $"{EnvironmentPrefabsFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var root = CreateRoot(name, GameLayers.Default);
            build(root);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);

            EnsureFolder(EnvironmentPrefabsFolder);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Mesh AddModel(GameObject root, string modelPath, params Material[] materials) =>
            AddModel(root, modelPath, 1f, materials);

        /// <summary>Adds the model under a "Model" child; materials follow the Blender slot order.</summary>
        private static Mesh AddModel(GameObject root, string modelPath, float scale, params Material[] materials)
        {
            var model = AddModelRoot(root, scale);
            var path = $"{ModelsFolder}/{modelPath}";
            var mesh = LoadModelMesh(path);
            if (mesh == null)
            {
                Debug.LogWarning($"Missing model {path}");
                return null;
            }

            AddMeshPart(model, Path.GetFileNameWithoutExtension(path), mesh, materials);
            return mesh;
        }

        private static Transform AddModelRoot(GameObject root, float scale = 1f)
        {
            var model = new GameObject("Model") { layer = root.layer }.transform;
            model.SetParent(root.transform, false);
            model.localScale = Vector3.one * scale;
            return model;
        }

        private static GameObject AddMeshPart(Transform parent, string name, Mesh mesh, params Material[] materials)
        {
            var part = new GameObject(name) { layer = parent.gameObject.layer };
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return part;
        }

        private static void AddDecal(GameObject root, string modelPath, float lift, params Material[] materials)
        {
            AddModel(root, modelPath, materials);
            FinishDecal(root, lift);
        }

        private static void AddDecal(GameObject root, Mesh mesh, float lift, params Material[] materials)
        {
            AddMeshPart(AddModelRoot(root), mesh.name, mesh, materials);
            FinishDecal(root, lift);
        }

        private static void FinishDecal(GameObject root, float lift)
        {
            var model = root.transform.Find("Model");
            model.localPosition = new Vector3(0f, lift, 0f);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void AddCapsule(GameObject root, float radius, float height)
        {
            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, height * 0.5f, 0f);
            collider.radius = radius;
            collider.height = height;
        }

        private static void AddBox(GameObject root, Vector3 size)
        {
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, size.y * 0.5f, 0f);
            collider.size = size;
        }

        private static void AddMeshCollider(GameObject root, Mesh mesh)
        {
            if (mesh != null)
                root.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>Imports a prop model as geometry only (materials come from the builder) and returns its mesh.</summary>
        private static Mesh LoadModelMesh(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                return null;

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None || importer.importAnimation ||
                importer.animationType != ModelImporterAnimationType.None || importer.importCameras || importer.importLights)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importBlendShapes = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
        }

        private static GameObject PlaceProp(GameObject prefab, Transform parent, Vector3 position, float yaw, Vector3 scale)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            instance.transform.localScale = scale;
            return instance;
        }

        /// <summary>Lines a wall with segments, centred on <paramref name="center"/>.</summary>
        private static void PlaceWall(GameObject segment, Transform parent, Vector3 center, float length, float yaw)
        {
            var count = Mathf.CeilToInt(length / WallSegmentLength);
            var direction = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            var start = center - direction * (count - 1) * WallSegmentLength * 0.5f;
            for (var i = 0; i < count; i++)
                PlaceProp(segment, parent, start + direction * (i * WallSegmentLength), yaw, Vector3.one);
        }
    }
}
