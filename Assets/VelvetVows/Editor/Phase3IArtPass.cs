using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace VelvetVows.Editor
{
    public static partial class Phase3IArtPass
    {
        const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        public static void Inspect()
        {
            EditorSceneManager.OpenScene(Scene);
            var sb = new StringBuilder();
            foreach (var a in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                sb.AppendLine("ACTOR " + PathOf(a.transform));
                foreach(var r in a.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                { sb.AppendLine("MESH " + r.name); for(int i=0;i<r.sharedMesh.blendShapeCount;i++) sb.AppendLine(i+" "+r.sharedMesh.GetBlendShapeName(i)); }
            }
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var p=PathOf(r.transform);
                if((p.Contains("Library")||p.Contains("Studio")) && !p.Contains("Bound volume") && !p.Contains("Stone floor") && !p.Contains("Pointed walls"))
                    sb.AppendLine("PROP "+p+" "+r.bounds+" "+string.Join(",",r.sharedMaterials.Select(m=>m!=null?m.name:"null")));
            }
            foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Medieval Room Kit/Prefab","Assets/VNB - Home Set/Prefabs/Decoration"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                if(!path.Contains("Shelf")&&!path.Contains("Table_Chairs")&&!path.Contains("Lamp")&&!path.Contains("Book"))continue;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1)) b.Encapsulate(r.bounds);
                sb.AppendLine("ASSET "+path+" "+b);Object.DestroyImmediate(go);
            }
            File.WriteAllText("Phase3I_Captures/inventory.txt",sb.ToString());
        }
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    }
}
