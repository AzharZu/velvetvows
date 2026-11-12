using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VelvetVows.Editor
{
    public static partial class Phase3HRepairPass
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        public static void Inspect()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory("Phase3H_Captures");
            var text = new StringBuilder();
            foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (animator.avatar == null || !animator.avatar.isHuman) continue;
                text.AppendLine("ACTOR " + PathOf(animator.transform) + " pos=" + animator.transform.position + " scale=" + animator.transform.localScale);
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                text.AppendLine("HEAD " + (head != null ? PathOf(head) + " " + head.localScale : "missing"));
                foreach (var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    text.AppendLine("MESH " + renderer.name + " bounds=" + renderer.bounds + " blendshapes=" + renderer.sharedMesh.blendShapeCount);
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        var material = renderer.sharedMaterials[i];
                        text.AppendLine("MAT " + i + " " + (material != null ? material.name + " shader=" + material.shader.name +
                            " tex=" + (material.HasProperty("_BaseMap") ? AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")) : "") : "missing"));
                    }
                }
            }
            File.WriteAllText("Phase3H_Captures/actors.txt", text.ToString());
            text.Clear();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name.Contains("Library") || t.name.Contains("corridor") || t.name.Contains("Corridor") ||
                    t.name == "GrandHall" || t.name == "ArtStudio" || t.name == "Ballroom" || t.name == "RestrictedGallery" ||
                    t.name == "Painting worktable two" || t.name == "Margot" || t.name == "Studio workbench")
                {
                    var rs = t.GetComponentsInChildren<Renderer>(true);
                    Bounds bounds = rs.Length > 0 ? rs[0].bounds : new Bounds(t.position, Vector3.zero);
                    foreach (var r in rs.Skip(1)) bounds.Encapsulate(r.bounds);
                    text.AppendLine((t.gameObject.activeInHierarchy ? "ACTIVE " : "off ") + PathOf(t) + " pos=" + t.position + " rot=" + t.eulerAngles + " bounds=" + bounds);
                }
            }
            File.WriteAllText("Phase3H_Captures/geometry.txt", text.ToString());
        }
        public static void InspectDetails()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var text = new StringBuilder();
            var cleanup = Object.FindFirstObjectByType<Phase2BCleanupManager>();
            text.AppendLine("CLOTH " + PathOf(cleanup.worldCloth.transform));
            foreach (var r in cleanup.worldCloth.GetComponentsInChildren<Renderer>(true))
                text.AppendLine("CLOTH RENDERER " + PathOf(r.transform) + " " + r.bounds);
            var target = new Vector3(-6.8f, 1.35f, 6.8f);
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var b = renderer.bounds;
                if (Vector3.Distance(b.ClosestPoint(target), target) < 3 ||
                    Vector3.Distance(b.ClosestPoint(cleanup.worldCloth.transform.position), cleanup.worldCloth.transform.position) < .6f)
                    text.AppendLine(PathOf(renderer.transform) + " bounds=" + b + " collider=" + renderer.GetComponent<Collider>() +
                        " mats=" + string.Join(",", renderer.sharedMaterials.Select(m => m != null ? m.name : "null")));
            }
            File.WriteAllText("Phase3H_Captures/details.txt", text.ToString());
        }
        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }
}
