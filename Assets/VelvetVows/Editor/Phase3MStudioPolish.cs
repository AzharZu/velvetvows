using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VelvetVows.Editor
{
    public static class Phase3MStudioPolish
    {
        private const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        [MenuItem("Velvet Vows/Art direction/Stage 5 - Studio details")]
        public static void Apply()
        {
            const string backup = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3M.unity";
            if (!File.Exists(backup)) File.Copy(Scene, backup);
            var scene = EditorSceneManager.OpenScene(Scene);
            var root = scene.GetRootGameObjects().First(x => x.name == "Velvet Vows Main").transform;
            var environment = root.Find("ENVIRONMENT");
            var old = environment.Find("Phase 3M Studio details");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var detail = new GameObject("Phase 3M Studio details").transform;
            detail.SetParent(environment, false);
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/VelvetVows/Materials/Phase3I/Oiled_walnut.mat");
            var brass = AssetDatabase.LoadAssetAtPath<Material>("Assets/VelvetVows/Materials/Phase3I/Antique_brass.mat");
            if (wood == null || brass == null) throw new InvalidOperationException("Studio materials are missing");
            var bench = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "Linen supply bench");
            var renderer = bench.GetComponent<Renderer>();
            if (renderer == null) throw new InvalidOperationException("Linen bench renderer missing");
            var bounds = renderer.bounds; renderer.enabled = false; // Keep its existing collision and cloth position.
            var center = bounds.center; var size = bounds.size;
            Piece(detail, "Linen bench top", new Vector3(center.x, bounds.max.y - .035f, center.z),
                new Vector3(size.x, .07f, size.z), wood);
            foreach (float x in new[] { -.37f, .37f }) foreach (float z in new[] { -.36f, .36f })
                Piece(detail, "Linen bench leg", new Vector3(center.x + size.x*x, bounds.min.y + (size.y-.07f)*.5f, center.z + size.z*z),
                    new Vector3(.09f, size.y-.07f, .09f), wood);
            var basin = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "Studio wash basin");
            foreach (var tap in basin.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Tap" || t.name.StartsWith("Faucet")))
                foreach (var r in tap.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = brass;
            var lightObject = new GameObject("Studio soft face fill", typeof(Light));
            lightObject.transform.SetParent(detail, false); lightObject.transform.position = new Vector3(-18.5f, 3.4f, -3f);
            var light = lightObject.GetComponent<Light>(); light.type = LightType.Point; light.range = 8f;
            light.intensity = .55f; light.color = new Color(1f, .86f, .72f); light.shadows = LightShadows.None;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, Scene); AssetDatabase.SaveAssets();
        }
        private static void Piece(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube); piece.name = name;
            Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.transform.SetParent(parent, false); piece.transform.position = position; piece.transform.localScale = size;
            piece.GetComponent<Renderer>().sharedMaterial = material;
        }
        public static void BuildMac()
        {
            Directory.CreateDirectory("Builds/Phase3M");
            var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene },
                locationPathName = "Builds/Phase3M/VelvetVows.app", target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development });
            File.WriteAllText("Phase3M_Captures/build_summary.txt", build.summary.result + "\nErrors: " + build.summary.totalErrors + "\nBytes: " + build.summary.totalSize);
            if (build.summary.result != BuildResult.Succeeded) throw new Exception("Standalone build failed");
        }
        public static void BuildMacPhase3N()
        {
            Directory.CreateDirectory("Builds/Phase3N");
            var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene },
                locationPathName = "Builds/Phase3N/VelvetVows.app", target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development });
            File.WriteAllText("Phase3N_Captures/build_summary.txt", build.summary.result + "\nErrors: " + build.summary.totalErrors + "\nBytes: " + build.summary.totalSize);
            if (build.summary.result != BuildResult.Succeeded) throw new Exception("Standalone build failed");
        }
        public static void BuildMacPhase3O()
        {
            Directory.CreateDirectory("Builds/Phase3O");
            var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene },
                locationPathName = "Builds/Phase3O/VelvetVows.app", target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development });
            Directory.CreateDirectory("Phase3O_Captures");
            File.WriteAllText("Phase3O_Captures/build_summary.txt", build.summary.result + "\nErrors: " + build.summary.totalErrors + "\nBytes: " + build.summary.totalSize);
            if (build.summary.result != BuildResult.Succeeded) throw new Exception("Standalone build failed");
        }
    }
}
