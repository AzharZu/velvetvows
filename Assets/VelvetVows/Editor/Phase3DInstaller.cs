using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VelvetVows.Editor
{
    public static class Phase3DInstaller
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string BackupPath = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3D.unity";

        [MenuItem("Velvet Vows/Install Phase 3D Ballroom Polish")]
        public static void Install()
        {
            if (!File.Exists(BackupPath)) throw new Exception("Phase 3D backup is required");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var ball = UnityEngine.Object.FindFirstObjectByType<Phase2CBallDirector>(FindObjectsInactive.Include);
            if (ball == null || ball.finalPanel == null || ball.ballLight == null || ball.guests == null)
                throw new Exception("Existing Black Rose Ball installation is missing");
            if (ball.transform.Find("Phase 3D ballroom accents") != null)
                throw new Exception("Phase 3D already installed");

            MoveFrontGuests(ball);

            ball.ballLight.range = 8.5f;
            ball.ballLight.intensity = 2.2f;
            ball.portraitLight = AddLight(ball.transform, "Soft portrait key",
                new Vector3(20.1f, 2.65f, -1.4f), new Color(1f, .82f, .64f), 1.65f, 4.7f);
            ball.burgundyLight = AddLight(ball.transform, "Burgundy window accent",
                new Vector3(22.1f, 2.25f, 1.75f), new Color(.65f, .14f, .22f), 1.1f, 5f);

            var decor = new GameObject("Phase 3D ballroom accents");
            decor.transform.SetParent(ball.transform, false);
            var rosePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VelvetVows/Prefabs/Black Rose.prefab");
            if (rosePrefab == null) throw new Exception("Existing Black Rose prefab is missing");
            Vector3[] positions = {
                new Vector3(16.05f, .3f, -2.75f), new Vector3(16.3f, .3f, -2.82f),
                new Vector3(23.9f, .3f, -2.75f), new Vector3(24.15f, .3f, -2.82f)
            };
            foreach (var position in positions)
            {
                var rose = (GameObject)PrefabUtility.InstantiatePrefab(rosePrefab, scene);
                rose.name = "Decorative black rose";
                rose.transform.SetParent(decor.transform, true);
                rose.transform.position = position;
                foreach (var collider in rose.GetComponentsInChildren<Collider>())
                    UnityEngine.Object.DestroyImmediate(collider);
                foreach (var behaviour in rose.GetComponentsInChildren<MonoBehaviour>())
                    behaviour.enabled = false;
            }
            decor.SetActive(false);
            ball.ballDecor = decor;

            var panel = ball.finalPanel;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = panel.GetComponent<Image>();
            image.color = new Color(.09f, .025f, .04f, .82f);
            image.raycastTarget = false;
            if (panel.GetComponent<CanvasGroup>() == null) panel.AddComponent<CanvasGroup>();
            var title = panel.transform.Find("A PERFECT DUET")?.GetComponent<Text>();
            var subtitle = panel.transform.Find("Some hearts were never meant to be innocent.")?.GetComponent<Text>();
            if (title == null || subtitle == null) throw new Exception("Existing ending typography is missing");
            title.fontSize = 52;
            title.color = new Color(1f, .9f, .7f);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 260);
            subtitle.text = "Two hearts. One beautiful obsession.";
            subtitle.fontSize = 23;
            subtitle.color = new Color(.96f, .87f, .8f);
            subtitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 180);
            GoldRule(panel.transform, new Vector2(0, 222));
            GoldRule(panel.transform, new Vector2(0, 142));
            ball.playAgainButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120, -205);
            ball.mainMenuButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, -205);
            foreach (var button in new[] { ball.playAgainButton, ball.mainMenuButton })
            {
                if (button == null) throw new Exception("Existing ending buttons are missing");
                button.GetComponent<Image>().color = new Color(.27f, .07f, .11f, .97f);
                var colors = button.colors;
                colors.highlightedColor = new Color(.84f, .63f, .43f);
                colors.pressedColor = new Color(.56f, .27f, .22f);
                button.colors = colors;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Could not save Phase 3D scene");
            Debug.Log("[Phase3D] Ballroom lights, black roses, and ending presentation installed");
        }

        [MenuItem("Velvet Vows/Refine Phase 3D Framing")]
        public static void Refine()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var ball = UnityEngine.Object.FindFirstObjectByType<Phase2CBallDirector>(FindObjectsInactive.Include);
            if (ball == null || ball.transform.Find("Phase 3D ballroom accents") == null)
                throw new Exception("Install Phase 3D first");
            MoveFrontGuests(ball);
            var panel = ball.finalPanel.transform;
            panel.Find("A PERFECT DUET").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 260);
            panel.Find("Some hearts were never meant to be innocent.")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 180);
            var rules = panel.GetComponentsInChildren<Image>(true);
            int ruleIndex = 0;
            foreach (var rule in rules)
                if (rule.name == "Gold divider")
                    rule.GetComponent<RectTransform>().anchoredPosition =
                        new Vector2(0, ruleIndex++ == 0 ? 222 : 142);
            ball.playAgainButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120, -205);
            ball.mainMenuButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, -205);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Could not save refined Phase 3D scene");
            Debug.Log("[Phase3D] Guest positions and ending title refined");
        }

        private static void MoveFrontGuests(Phase2CBallDirector ball)
        {
            Vector3[] positions = {
                new Vector3(15.7f, 0, 2.2f), new Vector3(16.7f, 0, 2.2f),
                new Vector3(23.5f, 0, 2.2f), new Vector3(24.5f, 0, 2.2f)
            };
            for (int i = 0; i < positions.Length; i++)
                ball.guests.transform.GetChild(i).position = positions[i];
        }

        private static Light AddLight(Transform parent, string name, Vector3 position,
            Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        private static void GoldRule(Transform parent, Vector2 position)
        {
            var go = new GameObject("Gold divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(330, 2);
            rect.anchoredPosition = position;
            var image = go.GetComponent<Image>();
            image.color = new Color(.7f, .53f, .29f, .9f);
            image.raycastTarget = false;
        }
    }
}
