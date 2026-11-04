using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VelvetVows.Editor
{
    public static class Phase2CInstaller
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string BackupPath = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase2C.unity";
        private const string AnimRoot = "Assets/Kevin Iglesias/Human Animations/Animations/";
        private static Font font;
        [MenuItem("Velvet Vows/Install Phase 2C Black Rose Ball")]
        public static void Install()
        {
            if (!System.IO.File.Exists(BackupPath)) throw new Exception("Phase 2C scene backup is required");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (GameObject.Find("PHASE 2C BLACK ROSE BALL") != null) throw new Exception("Phase 2C already installed");
            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var story = UnityEngine.Object.FindFirstObjectByType<Phase2AStoryManager>();
            var cleanup = UnityEngine.Object.FindFirstObjectByType<Phase2BCleanupManager>();
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
            var dialogue = UnityEngine.Object.FindFirstObjectByType<DialogueManager>();
            if (game == null || story == null || cleanup == null || hud == null || dialogue == null)
                throw new Exception("Accepted Phase 2A / 2B scene components are missing");
            var root = new GameObject("PHASE 2C BLACK ROSE BALL");
            root.transform.SetParent(game.transform, false);
            var director = root.AddComponent<Phase2CBallDirector>();
            director.game = game; director.story = story; director.hud = hud; director.dialogue = dialogue;
            director.player = story.player; director.isabelle = story.isabelle; director.adrian = story.adrian;
            director.isabelleMotion = story.isabelleMotion; director.adrianMotion = story.adrianMotion;
            director.lovesick = story.lovesick; director.reputation = story.reputation;
            cleanup.ball = director;
            director.femalePoseBegin = Poses("Female", "HumanF", "Begin");
            director.femalePoseLoop = Poses("Female", "HumanF", "Loop");
            director.femalePoseStop = Poses("Female", "HumanF", "Stop");
            director.malePoseBegin = Poses("Male", "HumanM", "Begin");
            director.malePoseLoop = Poses("Male", "HumanM", "Loop");
            director.malePoseStop = Poses("Male", "HumanM", "Stop");
            director.piano = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Pocket Keys Vol 2 (Free)/WAV/LLT_04_Pocket_Keys_Vol_2_(Free)_Loop_60bpm.wav");
            if (director.piano == null) throw new Exception("Pocket Keys ballroom loop missing");
            var guests = new GameObject("Ball guests - ten existing academy prefabs");
            guests.transform.SetParent(root.transform, false);
            director.guests = guests;
            string[] prefabs = {
                "Assets/VelvetVows/Prefabs/NPC/Amelie Casual Student.prefab",
                "Assets/VelvetVows/Prefabs/NPC/Male Student Slate.prefab",
                "Assets/VelvetVows/Prefabs/NPC/Clara Casual Student.prefab",
                "Assets/VelvetVows/Prefabs/NPC/Male Student Burgundy.prefab",
                "Assets/VelvetVows/Prefabs/NPC/Margot Casual Student.prefab",
                "Assets/VelvetVows/Prefabs/NPC/Odette Casual Student.prefab",
                "Assets/VelvetVows/Prefabs/NPC/Sabine Casual Student.prefab",
                "Assets/VelvetVows/Prefabs/NPC/Isabelle Casual Student.prefab",
                "Assets/VelvetVows/Prefabs/NPC/Clara Casual Student.prefab",
                "Assets/VelvetVows/Prefabs/NPC/Male Student Slate.prefab"
            };
            Vector3[] positions = {
                new Vector3(17.2f,0,2.1f), new Vector3(18.45f,0,2.0f),
                new Vector3(22.2f,0,2.15f), new Vector3(23.45f,0,2.0f),
                new Vector3(16.25f,0,-2.2f), new Vector3(17.45f,0,-2.2f),
                new Vector3(23.2f,0,-2.2f), new Vector3(24.45f,0,-2.2f),
                new Vector3(15.1f,0,.65f), new Vector3(25.1f,0,.65f)
            };
            for (int i = 0; i < prefabs.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]);
                if (asset == null) throw new Exception("Guest prefab missing: " + prefabs[i]);
                var actor = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                actor.name = "Ball guest " + (i + 1).ToString("00");
                actor.transform.SetParent(guests.transform, true);
                actor.transform.SetPositionAndRotation(positions[i], Quaternion.Euler(0, i % 2 == 0 ? 62 : 242, 0));
                foreach (var collider in actor.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
                foreach (var behaviour in actor.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
                var animator = actor.GetComponentInChildren<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                    throw new Exception("Guest prefab lacks a Humanoid avatar: " + prefabs[i]);
                bool male = i == 1 || i == 3 || i == 9;
                string sex = male ? "Male" : "Female";
                string prefix = male ? "HumanM" : "HumanF";
                string path;
                string clipName;
                if (i < 4)
                {
                    clipName = prefix + "@Dance0" + (i < 2 ? "5" : "6");
                    path = AnimRoot + sex + "/Social/Dance/Steps/" + clipName + ".fbx";
                    clipName += " - Loop";
                }
                else
                {
                    clipName = prefix + (i < 8 ? "@Talk01" : "@Idle01");
                    path = AnimRoot + sex + (i < 8 ? "/Social/Conversation/" : "/Idles/") + clipName + ".fbx";
                }
                var guest = actor.AddComponent<Phase2CBallGuest>();
                guest.animator = animator; guest.clip = Clip(path, clipName); guest.phase = (i * .173f) % 1f;
            }
            guests.SetActive(false);
            var lightObject = new GameObject("Warm ball ceiling glow");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.position = new Vector3(20, 4.4f, 0);
            var ballLight = lightObject.AddComponent<Light>();
            ballLight.type = LightType.Point; ballLight.color = new Color(1f, .72f, .46f);
            ballLight.intensity = 2.6f; ballLight.range = 12f; ballLight.shadows = LightShadows.None;
            ballLight.enabled = false; director.ballLight = ballLight;
            font = cleanup.outcomeTitle.font;
            var panel = new GameObject("A Perfect Duet ending", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(hud.transform, false);
            var panelImage = panel.GetComponent<Image>(); panelImage.color = new Color(.045f, .03f, .055f, .88f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(.5f,.5f); panelRect.anchorMax = new Vector2(.5f,.5f);
            panelRect.sizeDelta = new Vector2(790, 410); panelRect.anchoredPosition = Vector2.zero;
            Label("A PERFECT DUET", panel.transform, 42, new Vector2(0,90), new Vector2(740,75));
            Label("Some hearts were never meant to be innocent.", panel.transform, 20, new Vector2(0,17), new Vector2(740,56));
            director.playAgainButton = Button("PLAY AGAIN", panel.transform, new Vector2(-120,-105));
            director.mainMenuButton = Button("MAIN MENU", panel.transform, new Vector2(120,-105));
            panel.SetActive(false); director.finalPanel = panel;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Could not save Phase 2C scene");
            Debug.Log("[Phase2C] Installed Black Rose Ball, ten guests and ending UI.");
        }
        private static AnimationClip[] Poses(string sex, string prefix, string phase) =>
            Enumerable.Range(1, 2).Select(i => {
                string name = prefix + "@DancePose" + i.ToString("00") + " - " + phase;
                return Clip(AnimRoot + sex + "/Social/Dance/Poses/" + name + ".fbx", name);
            }).ToArray();
        private static AnimationClip Clip(string path, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name == name)
            ?? throw new Exception("Animation missing: " + path + " / " + name);
        private static void Label(string words, Transform parent, int size, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(words, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = dimensions; rect.anchoredPosition = position;
            var text = go.GetComponent<Text>(); text.text = words; text.font = font; text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter; text.color = new Color(.96f,.87f,.75f); text.raycastTarget = false;
        }
        private static Button Button(string words, Transform parent, Vector2 position)
        {
            var go = new GameObject(words, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(205,50); rect.anchoredPosition = position;
            go.GetComponent<Image>().color = new Color(.30f,.12f,.20f,.98f);
            Label(words, go.transform, 19, Vector2.zero, new Vector2(195,48));
            return go.GetComponent<Button>();
        }
    }
}
