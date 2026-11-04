using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelvetVows.Editor
{
    public static class AnimationTestSceneBuilder
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_AnimationTest.unity";
        private const string KasumiPath = "Assets/Visutronik/Characters/Volleyball - Sky Strikers/Kasumi Hoshino/Prefab/Kasumi - School.prefab";
        private const string RinPath = "Assets/Visutronik/Characters/Volleyball - Sky Strikers/Rin Kobayashi/Prefab/Rin - School.prefab";
        private const string AkioPath = "Assets/Novacorp Studios/Characters/Hikari Series/Akio/Highschool Uniform/Prefab/Akio Highschool Uniform.prefab";
        private const string AnimRoot = "Assets/Kevin Iglesias/Human Animations/Animations/";

        [MenuItem("Velvet Vows/Build Isolated Animation Test Scene")]
        public static void Build()
        {
            // An additive scene ensures the user's open main scene is neither saved nor edited.
            Scene previous = SceneManager.GetActiveScene();
            // Batchmode starts on an untitled unsaved scene, which Unity cannot keep
            // open while adding another. The editor's saved working scene stays additive.
            bool hasSavedWorkingScene = !string.IsNullOrEmpty(previous.path);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                hasSavedWorkingScene ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            try
            {
                var root = new GameObject("ANIMATION TEST - isolated from main gameplay");
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Neutral floor";
                floor.transform.SetParent(root.transform);
                floor.transform.SetPositionAndRotation(new Vector3(0, -.12f, 1.1f), Quaternion.identity);
                floor.transform.localScale = new Vector3(18, .2f, 15);

                var lightGo = new GameObject("Key light");
                lightGo.transform.SetParent(root.transform);
                lightGo.transform.rotation = Quaternion.Euler(38, -25, 0);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.6f;
                RenderSettings.ambientLight = new Color(.47f, .47f, .52f);

                var camGo = new GameObject("Test Camera");
                camGo.tag = "MainCamera";
                camGo.transform.SetParent(root.transform);
                camGo.transform.position = new Vector3(0, 2.55f, -7.8f);
                camGo.transform.LookAt(new Vector3(0, 1.05f, .7f));
                var camera = camGo.AddComponent<Camera>();
                camera.fieldOfView = 43f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.15f, .16f, .2f);
                camGo.AddComponent<AudioListener>();

                var director = root.AddComponent<AnimationTestDirector>();
                director.autoRun = false;
                director.attackEvelyn = Actor(KasumiPath, "ATTACK Evelyn - Kasumi", new Vector3(-.65f, 0, 0), 90, root.transform, scene);
                director.attackCeleste = Actor(RinPath, "ATTACK Celeste - Rin", new Vector3(.65f, 0, 0), -90, root.transform, scene);
                director.danceFemales = new[]
                {
                    Actor(KasumiPath, "PAIR A Evelyn - Kasumi", new Vector3(-.6f, 0, 0), 90, root.transform, scene),
                    Actor(RinPath, "PAIR B Rin", new Vector3(-4.25f, 0, 2.8f), 90, root.transform, scene),
                    Actor(KasumiPath, "PAIR C Kasumi", new Vector3(3.05f, 0, 2.8f), 90, root.transform, scene)
                };
                director.danceMales = new[]
                {
                    Actor(AkioPath, "PAIR A Adrian - Akio", new Vector3(.6f, 0, 0), -90, root.transform, scene),
                    Actor(AkioPath, "PAIR B Akio", new Vector3(-3.05f, 0, 2.8f), -90, root.transform, scene),
                    Actor(AkioPath, "PAIR C Akio", new Vector3(4.25f, 0, 2.8f), -90, root.transform, scene)
                };
                director.attack = Clip("Female/Combat/1H/HumanF@Attack1H01_R.fbx", "HumanF@Attack1H01_R");
                director.damage = Clip("Female/Combat/HumanF@CombatDamage01.fbx", "HumanF@CombatDamage01");
                director.death = Clip("Female/Combat/HumanF@Death01.fbx", "HumanF@Death01");
                director.femaleSteps = Enumerable.Range(1, 6).Select(i => Clip($"Female/Social/Dance/Steps/HumanF@Dance{i:00}.fbx", $"HumanF@Dance{i:00} - Loop")).ToArray();
                director.maleSteps = Enumerable.Range(1, 6).Select(i => Clip($"Male/Social/Dance/Steps/HumanM@Dance{i:00}.fbx", $"HumanM@Dance{i:00} - Loop")).ToArray();
                director.femalePoseBegin = Poses("Female", "HumanF", "Begin");
                director.femalePoseLoop = Poses("Female", "HumanF", "Loop");
                director.femalePoseStop = Poses("Female", "HumanF", "Stop");
                director.malePoseBegin = Poses("Male", "HumanM", "Begin");
                director.malePoseLoop = Poses("Male", "HumanM", "Loop");
                director.malePoseStop = Poses("Male", "HumanM", "Stop");

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Could not save animation test scene");
                Debug.Log("[AnimationTest] Created " + ScenePath + " without changing the open scene.");
            }
            finally
            {
                if (hasSavedWorkingScene)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
            }
        }

        private static AnimationClip[] Poses(string sex, string prefix, string phase) =>
            Enumerable.Range(1, 2).Select(i =>
                Clip($"{sex}/Social/Dance/Poses/{prefix}@DancePose{i:00} - {phase}.fbx", $"{prefix}@DancePose{i:00} - {phase}")).ToArray();

        private static AnimationClip Clip(string relative, string name)
        {
            string path = AnimRoot + relative;
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name == name);
            if (clip == null) throw new Exception("Missing imported clip " + name + " at " + path);
            if (!clip.humanMotion) throw new Exception("Clip is not Humanoid: " + path);
            return clip;
        }

        private static GameObject Actor(string path, string name, Vector3 position, float yaw, Transform parent, Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new Exception("Missing character prefab " + path);
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            actor.name = name;
            actor.transform.SetParent(parent);
            actor.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var animator = actor.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new Exception("Invalid Humanoid avatar on " + path);
            animator.applyRootMotion = false;
            ApplyCharacterMaterials(actor, path == KasumiPath ? "Evelyn" : path == RinPath ? "Celeste" : "Adrian");
            return actor;
        }

        private static void ApplyCharacterMaterials(GameObject actor, string character)
        {
            string folder = "Assets/VelvetVows/Materials/Characters/" + character;
            var converted = new Dictionary<string, Material>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat != null) converted[Normalize(mat.name)] = mat;
            }
            int replaced = 0, missing = 0;
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (converted.TryGetValue(Normalize(mats[i].name), out var replacement))
                    { mats[i] = replacement; replaced++; }
                    else missing++;
                }
                renderer.sharedMaterials = mats;
            }
            Debug.Log($"[AnimationTest] {character} URP material mapping: {replaced} replaced, {missing} unmatched");
        }

        private static string Normalize(string value) =>
            new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }
}
