using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VelvetVows.Editor
{
    public static class Phase3MStudioAudit
    {
        public static void WriteInventory()
        {
            EditorSceneManager.OpenScene("Assets/VelvetVows/Scenes/VelvetVows_Main.unity");
            var items = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.bounds.center.x < -12 && r.bounds.center.x > -24 &&
                    r.bounds.center.z > -8 && r.bounds.center.z < 4 && r.bounds.center.y < 2)
                .OrderBy(r => r.bounds.center.x)
                .Select(r => $"{PathOf(r.transform)} | center={r.bounds.center} size={r.bounds.size} material={r.sharedMaterial?.name}");
            Directory.CreateDirectory("Phase3M_Captures");
            File.WriteAllLines("Phase3M_Captures/studio_inventory.txt", items);
        }
        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }
}
