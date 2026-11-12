using System.Collections;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace VelvetVows.Tests
{
    public sealed class RecordedPlaythroughTests
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private PlaythroughCapture capture;
        private MonoBehaviour story, dialogue, game, cleanup, ball;
        private Transform player;
        private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        private static object Value(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
        private static MonoBehaviour Component(string name) => UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.GetType().Name == name);
        private string Stage => Value(story, "Stage").ToString();
        private bool DialogueOpen => (bool)Value(dialogue, "IsOpen");

        private static void OpenGameView()
        {
            var viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(viewType);
            view.Show(); view.Focus();
            Application.runInBackground = true;
        }

        [UnityTest, Timeout(1800000), Explicit("Manual full-game recording; requires ffmpeg_path.txt and a visible Game View.")]
        public IEnumerator FullGameFromBeginToPerfectDuet()
            => FullGame(false);

        [UnityTest, Timeout(1800000), Explicit("Full physical route to HEARTBROKEN with recording.")]
        public IEnumerator FullGameFromBeginToHeartbroken()
            => FullGame(true);

        private IEnumerator FullGame(bool leave)
        {
            OpenGameView();
            Time.timeScale = 1;
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSeconds(1);
            story = Component("Phase2AStoryManager"); dialogue = Component("DialogueManager");
            game = Component("GameManager"); cleanup = Component("Phase2BCleanupManager"); ball = Component("Phase2CBallDirector");
            player = (Transform)Field(story, "player");
            var listener = UnityEngine.Object.FindFirstObjectByType<AudioListener>();
            Assert.NotNull(listener);
            capture = listener.gameObject.AddComponent<PlaythroughCapture>();
            capture.BeginRecording();
            capture.StartCoroutine(AutoAdvance());
            yield return new WaitForSeconds(2);
            Click(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).First(b => b.name == "BEGIN Button"));
            yield return Until(() => Stage == "Overhear", 200, "opening, complete diary intro and missing-student conversation");

            yield return Walk(new Vector3(-4.4f, 0, 2.8f), () => Stage == "Overhearing", false);
            yield return Until(() => Stage == "WatchAdrian", 100, "first eavesdrop");
            yield return Walk(new Vector3(-20, 0, 14), () => Stage == "Watching");
            yield return Until(() => Stage == "Errands", 100, "library eavesdrop");

            yield return TalkAt(new Vector3(-15, 0, 17), "Celeste refuses before trust");
            yield return PickupAt(new Vector3(-22.6f, 0, 17.4f), () => (bool)Value(story, "HasBooks"), "library books");
            yield return TalkAt(new Vector3(-21, 0, 21), "deliver books to Clara");
            yield return PickupAt(new Vector3(-19.7f, 0, -2.45f), () => (bool)Value(story, "HasArtMaterials"), "art materials");
            yield return TalkAt(new Vector3(-17, 0, -1), "deliver art materials to Margot");
            yield return Walk(new Vector3(-9.4f, 0, 8.6f));
            yield return Direct(new Vector3(-10.05f, 0, 8.6f), .45f);
            yield return Direct(new Vector3(-14.6f, 0, 13.15f), .65f);
            yield return PickupAt(new Vector3(-16.3f, 0, 11.6f), () => (bool)Value(story, "HasPendant"), "Odette pendant");
            yield return TalkAt(new Vector3(-13.5f, 0, 14.6f), "return pendant to Odette");
            Assert.AreEqual(5, Value(Component("Phase2AReputation"), "Value"));
            yield return TalkAt(new Vector3(-15, 0, 17), "Celeste ribbon request");
            Assert.AreEqual("RibbonSearch", Stage);

            yield return Walk(new Vector3(-18.4f, 0, 20.5f));
            yield return PressE();
            var pages = (GameObject)Field(story, "pagePanel");
            Assert.IsTrue(pages.activeInHierarchy, "Sketchbook did not open through E");
            for (int i = 0; i < 5; i++)
            {
                capture.Note("sketchbook", "Page " + (i + 1));
                yield return new WaitForSeconds(5);
                if (i < 4) Click((Button)Field(story, "pageNext"));
            }
            Click((Button)Field(story, "pageClose"));
            yield return Until(() => !DialogueOpen, 40, "sketchbook thoughts");
            yield return PickupAt(new Vector3(-22, 0, 19.3f), () => (bool)Value(story, "HasRibbon"), "black ribbon");
            yield return new WaitForSeconds(4.2f);
            yield return TalkAt(new Vector3(-15, 0, 17), "return ribbon and invite Celeste");
            Assert.AreEqual("FollowCeleste", Stage);
            yield return Walk(new Vector3(-20.5f, 0, -2.5f), () => Stage == "Witness");
            yield return Until(() => Stage == "Witness", 55, "Celeste follows into Studio");
            yield return TalkAt(new Vector3(-17, 0, -1), "ask Margot to leave", false, 1.3f);
            yield return Until(() => Stage == "CloseDoor", 30, "Margot exits");
            yield return Walk(new Vector3(-14.1f, 0, -2.7f));
            yield return PressE();
            yield return Until(() => Stage == "Confront", 5, "close Studio door");
            yield return Direct(((Transform)Field(story, "celeste")).position, 1.4f);
            yield return PressE();
            yield return Until(() => Value(game, "State").ToString() == "Phase2BCleanup", 90, "confrontation and murder");

            // Complete all four objectives using the player controller and E.
            yield return PressE();
            yield return Until(() => (bool)Value(cleanup, "BodyHidden"), 12, "hide Celeste");
            yield return Direct(new Vector3(-18, 0, -5.3f), .8f);
            yield return PressE();
            Assert.IsTrue((bool)Value(cleanup, "HasCloth"));
            yield return Direct(new Vector3(-17.2f, 0, -3.1f), .8f);
            yield return PressE();
            yield return Until(() => (bool)Value(cleanup, "BloodCleaned"), 8, "clean blood");
            yield return Direct(new Vector3(-17.2f, 0, -2), .8f);
            yield return Direct(new Vector3(-20.9f, 0, -2), .8f);
            yield return PressE();
            Assert.IsTrue((bool)Value(cleanup, "EvidenceDisposed"));
            yield return Direct(new Vector3(-18.75f, 0, -5.05f), .8f);
            yield return PressE();
            yield return Until(() => ((GameObject)Field(cleanup, "choicePanel")).activeInHierarchy, 100,
                "clean clothing, emotional narration, dossier monologue and body choice");
            Assert.IsTrue((bool)Value(cleanup, "ClothingCleaned"));
            yield return new WaitForSeconds(5);
            var choice = (GameObject)Field(cleanup, "choicePanel");
            Click(choice.GetComponentsInChildren<Button>().First(b => b.name.StartsWith(leave ? "LEAVE" : "BURN")));
            capture.Note("choice", leave ? "LEAVE THE BODY" : "BURN THE EVIDENCE");
            if (leave)
            {
                yield return Until(() => (bool)Value(ball,"Heartbroken") &&
                    ((GameObject)Field(ball,"heartbrokenPanel")).activeInHierarchy, 200, "HEARTBROKEN screen");
                Assert.AreEqual(1,Value(Component("CharacterVoiceManager"),"GameOverCount"));
                capture.Note("complete","HEARTBROKEN, normal speed, physical errands and cleanup");
                yield return new WaitForSeconds(7); capture.FinishRecording(); yield break;
            }
            yield return Until(() => (string)Value(ball, "Beat") == "Ending" &&
                Value(game, "State").ToString() == "Phase2CEnding" &&
                ((GameObject)Field(ball, "finalPanel")).activeInHierarchy,
                240, "final A PERFECT DUET screen after the final dance");
            Assert.IsFalse((bool)Value(ball, "Heartbroken"));
            Assert.AreEqual(0, Value(Component("CharacterVoiceManager"), "GameOverCount"));
            capture.Note("complete", "A PERFECT DUET, normal speed, physical errands and cleanup");
            yield return new WaitForSeconds(7);
            capture.FinishRecording();
        }

        [UnityTearDown]
        public IEnumerator StopCapture()
        {
            if (capture != null) capture.FinishRecording();
            yield return null;
        }

        private IEnumerator AutoAdvance()
        {
            var text = (Text)Field(dialogue, "bodyText");
            var speaker = (Text)Field(dialogue, "speakerText");
            var button = (Button)Field(dialogue, "continueButton");
            string last = "", state = "";
            float since = Time.realtimeSinceStartup;
            while (capture != null && capture.Recording)
            {
                string current = Value(game, "State") + "/" + Stage + "/" + Value(ball, "Beat");
                if (state != current) { state = current; capture.Note("state", current); }
                string line = DialogueOpen ? speaker.text + "|" + text.text : "";
                if (line != last)
                {
                    last = line; since = Time.realtimeSinceStartup;
                    if (line.Length > 0) capture.Note("dialogue", line);
                }
                if (line.Length > 0 && button.gameObject.activeInHierarchy && button.interactable)
                {
                    bool diary = ((Image)Field(ball, "diaryImage"))?.gameObject.activeInHierarchy == true;
                    float reading = diary ? 9f : Mathf.Clamp(1f + text.text.Length / 19f, 2.6f, 6f);
                    if (Time.realtimeSinceStartup - since >= reading)
                    { Click(button); since = Time.realtimeSinceStartup; }
                }
                yield return null;
            }
        }

        private IEnumerator Until(Func<bool> ready, float seconds, string context)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!ready() && Time.realtimeSinceStartup < end) yield return null;
            if (!ready()) capture?.Note("blocked", context + "; state=" + Value(game, "State") + "; stage=" + Stage + "; player=" + player.position);
            Assert.IsTrue(ready(), "Timed out: " + context);
        }

        private IEnumerator Walk(Vector3 destination, Func<bool> arrived = null, bool route = true)
        {
            capture?.Note("walk", destination.ToString());
            if (route)
                yield return (IEnumerator)typeof(Phase2APlayModeTests).GetMethod("WalkRoute", Flags)
                    .Invoke(null, new object[] { player, destination, arrived ?? (() => false) });
            else
                yield return (IEnumerator)typeof(Phase2APlayModeTests).GetMethod("WalkPlayerTo", Flags)
                    .Invoke(null, new object[] { player, destination, 35f, arrived, .85f });
        }
        private IEnumerator Direct(Vector3 destination, float radius) =>
            (IEnumerator)typeof(Phase2APlayModeTests).GetMethod("WalkPlayerTo", Flags)
                .Invoke(null, new object[] { player, destination, 25f, null, radius });
        private static IEnumerator PressE() => (IEnumerator)typeof(Phase2APlayModeTests)
            .GetMethod("PressE", Flags).Invoke(null, null);
        private IEnumerator PickupAt(Vector3 at, Func<bool> pickedUp, string label)
        {
            yield return Walk(at); yield return PressE();
            Assert.IsTrue(pickedUp(), "E did not pick up " + label);
            capture.Note("interaction", label);
            yield return new WaitForSeconds(1.2f);
        }
        private IEnumerator TalkAt(Vector3 at, string label, bool route = true, float radius = .85f)
        {
            if (route) yield return Walk(at); else yield return Direct(at, radius);
            yield return PressE();
            yield return Until(() => DialogueOpen, 3, label + " opens");
            capture.Note("interaction", label);
            yield return Until(() => !DialogueOpen, 75, label + " closes");
        }
        private static void Click(Button button)
        {
            Assert.IsTrue(button.IsActive() && button.IsInteractable(), button.name + " disabled");
            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            var point = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * .5f);
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.IsTrue(hits.Count > 0 && (hits[0].gameObject == button.gameObject ||
                hits[0].gameObject.transform.IsChildOf(button.transform)), button.name + " blocked by UI");
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest, Explicit("Manual capture setup probe; requires a visible Game View.")]
        public IEnumerator RecordingProbe()
        {
            var viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(viewType);
            view.position = new Rect(30, 30, 1280, 760);
            view.Show(); view.Focus();
            Screen.SetResolution(1280, 720, false);
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSeconds(2);
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.NotNull(texture);
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Playthrough_Recording"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "capture_probe.png"), texture.EncodeToPNG());
            Debug.Log($"[Recording] Screen capture {texture.width}x{texture.height}");
            UnityEngine.Object.Destroy(texture);
        }
    }
}
