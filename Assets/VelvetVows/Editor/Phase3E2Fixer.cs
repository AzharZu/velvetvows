using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelvetVows.Editor
{
    public static class Phase3E2Fixer
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string BackupPath = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3E2.unity";
        private const string MaterialFolder = "Assets/VelvetVows/Materials/Phase3E2";

        [MenuItem("Velvet Vows/Apply Phase 3E2 Studio and Prefect Polish")]
        public static void Apply()
        {
            if (!File.Exists(BackupPath)) throw new IOException("Phase 3E2 scene backup is required");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().First(g => g.name == "Velvet Vows Main").transform;
            var sink = Find(root, "SYSTEMS/Core Gameplay/PHASE 2B CLEANUP/Studio wash basin");
            if (sink.position.z < -5f) throw new IOException("Phase 3E2 studio layout was already applied");
            sink.position = new Vector3(-18.75f, 0, -6.55f);
            Find(root, "ENVIRONMENT/ArtStudio/Studio tables, books and supplies/Painting worktable two")
                .position = new Vector3(-16.4f, -.01f, .45f);
            var duplicateEasels = Find(root, "ENVIRONMENT/Visual Polish 2.1/Art Studio work in progress")
                .GetComponentsInChildren<Transform>(true).Where(t => t.name == "Student easel").ToArray();
            foreach (var easel in duplicateEasels) easel.gameObject.SetActive(false);

            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/VelvetVows/Materials", "Phase3E2");
            var leaf = Find(root, "SYSTEMS/Core Gameplay/PHASE 2A STORY/Art Studio closing door/Hinge/Dark walnut door leaf");
            var leafRenderer = leaf.GetComponent<Renderer>();
            leafRenderer.sharedMaterial = CopyTint(leafRenderer.sharedMaterial, MaterialFolder + "/Studio Walnut Door.mat",
                new Color(.48f, .31f, .19f, 1));
            foreach (var c in leaf.GetComponentsInChildren<Collider>(true))
                if (c.transform != leaf) c.enabled = false;

            var prefect = Find(root, "CHARACTERS/AmbientNPCs/Prefect - Lucien's classmate");
            var body = prefect.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var materials = body.sharedMaterials;
            body.sharedMaterials = materials;
            Polish(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save Phase 3E2 scene");
            AssetDatabase.SaveAssets();
            Debug.Log($"[Phase3E2] Studio sink, door, {duplicateEasels.Length} extra easels and Prefect coat updated");
        }

        [MenuItem("Velvet Vows/Refine Phase 3E2 Studio and Prefect")]
        public static void Refine()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().First(g => g.name == "Velvet Vows Main").transform;
            Polish(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void Polish(Transform root)
        {
            // One worktable and two easels give each activity a clear place.
            // These are visual props; all story and cleanup interactables remain.
            Find(root, "ENVIRONMENT/ArtStudio/Studio tables, books and supplies/Painting worktable one")
                .gameObject.SetActive(false);
            Find(root, "ENVIRONMENT/ArtStudio/Artist workstations/Artist easel 2")
                .position = new Vector3(4.8f, 0, .5f);
            var studioExtras = "ENVIRONMENT/Visual Polish 2.1/Art Studio work in progress/";
            foreach (var name in new[] { "Studio centerpiece still life", "Canvas leaning near wall",
                         "Student study west", "Student study east" })
                Find(root, studioExtras + name).gameObject.SetActive(false);
            Find(root, "ENVIRONMENT/ArtStudio/Studio tables, books and supplies/Unfinished still life study")
                .gameObject.SetActive(false);
            var duplicateSurfaces = Find(root, "WORLD/GOTHIC_ACADEMY/Academy paintings/Existing academy canvas surfaces");
            foreach (var canvas in duplicateSurfaces.GetComponentsInChildren<Renderer>(true))
            {
                var p = canvas.transform.position;
                if (p.x > -24f && p.x < -12f && p.z > -8f && p.z < 4f)
                    canvas.gameObject.SetActive(false);
            }
            var prefect = Find(root, "CHARACTERS/AmbientNPCs/Prefect - Lucien's classmate");
            var body = prefect.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var materials = body.sharedMaterials;
            // Slots 0 and 6 are hair and skin. Keep those original; only the
            // jacket and trousers receive dedicated ivory material variants.
            materials[0] = OriginalUniform(0);
            materials[6] = OriginalUniform(6);
            for (int i = 7; i <= 8; i++)
                materials[i] = BrightenUniform(i);
            body.sharedMaterials = materials;
        }

        private static Material OriginalUniform(int index) => AssetDatabase.LoadAssetAtPath<Material>(
            $"Assets/VelvetVows/Materials/ImportedVariants/Male Student Burgundy_Body_{index}.mat");

        private static Material BrightenUniform(int index)
        {
            var original = OriginalUniform(index);
            var shader = Shader.Find("VelvetVows/Prefect Ivory Uniform");
            if (shader == null) throw new IOException("Prefect ivory shader was not imported");
            var path = MaterialFolder + "/Prefect Ivory Uniform " + index + ".mat";
            var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (copy == null) { copy = new Material(shader); AssetDatabase.CreateAsset(copy, path); }
            copy.shader = shader;
            copy.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
            copy.SetColor("_IvoryColor", new Color(.98f, .93f, .82f, 1));
            EditorUtility.SetDirty(copy);
            return copy;
        }

        private static Material CopyTint(Material original, string path, Color color, bool untextured = false)
        {
            var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (copy == null)
            {
                copy = new Material(original);
                AssetDatabase.CreateAsset(copy, path);
            }
            if (untextured) copy.SetTexture("_BaseMap", null);
            copy.SetColor("_BaseColor", color);
            copy.SetFloat("_Smoothness", .26f);
            EditorUtility.SetDirty(copy);
            return copy;
        }
        private static Transform Find(Transform root, string path) => root.Find(path) ??
            throw new IOException("Scene object missing: " + path);
    }
}
