using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VelvetVows.Editor
{
    public static class Phase3LAnimationPass
    {
        const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        const string Root = "Assets/Kevin Iglesias/Human Animations/Animations/";
        const string Destination = "Assets/VelvetVows/Animations/Phase3L/";
        static AnimationClip Clip(string path) => AssetDatabase.LoadAllAssetsAtPath(Root + path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
        [MenuItem("Velvet Vows/Art direction/Stage 4 - complete character animation")]
        public static void Apply()
        {
            Directory.CreateDirectory("Phase3L_Captures"); Directory.CreateDirectory(Destination);
            const string backup = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3L.unity";
            if (!File.Exists(backup)) File.Copy(Scene, backup);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(Scene);
            int count = 0;
            foreach (var p in Object.FindObjectsByType<AcademyCharacterPresence>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var a = p.GetComponent<Animator>(); if (a == null || a.avatar == null || !a.avatar.isHuman) continue;
                if (a.GetComponent<AcademyBodyPerformance>() == null) a.gameObject.AddComponent<AcademyBodyPerformance>();
                bool male = a.name.Contains("Akio"); string sex = male ? "Male" : "Female", prefix = male ? "HumanM" : "HumanF";
                int idle = p.CharacterName == "CELESTE" || p.CharacterName == "ADRIAN" ? 2 : 1;
                var selected = Clip(sex + "/Idles/" + prefix + "@Idle0" + idle + ".fbx");
                var motion = a.GetComponentInParent<Phase2AActorMotion>();
                if (motion != null)
                {
                    var settings = new SerializedObject(motion); settings.FindProperty("idleA").objectReferenceValue = selected;
                    settings.FindProperty("variedIdle").boolValue = false; settings.ApplyModifiedPropertiesWithoutUndo();
                    motion.ConfigureTurns(Clip(sex + "/Movement/Turn/" + prefix + "@Turn01_Left.fbx"), Clip(sex + "/Movement/Turn/" + prefix + "@Turn01_Right.fbx"));
                    EditorUtility.SetDirty(motion);
                }
                if (a.runtimeAnimatorController is AnimatorController source)
                {
                    string path = Destination + source.name.Replace(" Phase3L", "") + " Phase3L.controller";
                    var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                    if (controller == null) { AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path); controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path); }
                    foreach (var layer in controller.layers)
                        foreach (var child in layer.stateMachine.states)
                        {
                            foreach (var transition in child.state.transitions) { transition.duration = .18f; EditorUtility.SetDirty(transition); }
                            if (child.state.motion is BlendTree tree)
                            {
                                var children = tree.children;
                                for (int i = 0; i < children.Length; i++)
                                {
                                    if (children[i].threshold < .1f) children[i].motion = selected;
                                    else if (children[i].motion is AnimationClip clip)
                                    {
                                        float nominal = clip.averageSpeed.magnitude;
                                        if (nominal < .1f) nominal = children[i].threshold < 3 ? 1.35f : 4.4f;
                                        children[i].timeScale = Mathf.Clamp(children[i].threshold / nominal, .65f, 1.95f);
                                    }
                                }
                                tree.children = children; EditorUtility.SetDirty(tree);
                            }
                        }
                    a.runtimeAnimatorController = controller;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(a); EditorUtility.SetDirty(a);
                }
                foreach (var mesh in a.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var materials = mesh.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        bool iris = mesh.name == "Casual1_Body" && i == 6 || mesh.name == "Body" && i == 2 || mesh.name == "all_mesh" && i == 1;
                        bool highlight = mesh.name == "Casual1_Body" && i == 1 || mesh.name == "Body" && i == 3 || mesh.name == "all_mesh" && i == 2;
                        if ((!iris && !highlight) || materials[i] == null) continue;
                        string path = Destination + p.CharacterName + " " + mesh.name + " eye " + i + ".mat";
                        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (material == null) { material = new Material(materials[i]); AssetDatabase.CreateAsset(material, path); }
                        if(iris && mesh.name=="Casual1_Body")
                        {
                            material.shader=Shader.Find("VelvetVows/Academy Iris");
                            material.SetFloat("_IrisScale",.82f); material.SetFloat("_DualAtlas",mesh.name=="Casual1_Body"?1:0);
                            material.SetFloat("_Cutoff",.25f); material.renderQueue=2451;
                        }
                        else if(iris)
                        {
                            // VRoid uses its original packed iris UVs, not the two-eye AnimeGirls atlas.
                            material.shader=Shader.Find("Universal Render Pipeline/Lit");
                            material.SetFloat("_AlphaClip",1);material.EnableKeyword("_ALPHATEST_ON");material.SetFloat("_Cutoff",.25f);
                            material.renderQueue=2450;
                        }
                        material.SetColor("_BaseColor", iris ? new Color(.76f,.70f,.63f,1) : new Color(.5f,.5f,.5f,1));
                        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .12f);
                        materials[i] = material; EditorUtility.SetDirty(material);
                    }
                    mesh.sharedMaterials = materials; PrefabUtility.RecordPrefabInstancePropertyModifications(mesh);
                }
                count++;
            }
            var story = Object.FindFirstObjectByType<Phase2AStoryManager>();
            if (story.player.GetComponent<AcademyActionPerformance>() == null) story.player.gameObject.AddComponent<AcademyActionPerformance>();
            if (story.player.GetComponent<AcademyGrounding>() == null) story.player.gameObject.AddComponent<AcademyGrounding>();
            var ball = Object.FindFirstObjectByType<Phase2CBallDirector>();
            ball.femaleDanceStep = Clip("Female/Social/Dance/Steps/HumanF@Dance01.fbx");
            ball.maleDanceStep = Clip("Male/Social/Dance/Steps/HumanM@Dance01.fbx");
            EditorUtility.SetDirty(ball);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Phase3L_Captures/apply_summary.txt", "Humanoid performances: " + count + "\nAction performer installed on Evelyn.\n");
            Debug.Log("[Phase3L] Saved animation pass on " + count + " humanoids.");
        }
        public static void BuildMac()
        {
            Directory.CreateDirectory("Builds/Phase3L");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene },
                locationPathName = "Builds/Phase3L/VelvetVows.app", target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
            File.WriteAllText("Phase3L_Captures/build_summary.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nBytes: " + report.summary.totalSize);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Standalone build failed");
        }
    }
}
