using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace VelvetVows.Tests
{
    public sealed class Phase3APlayModeTests
    {
        private const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string Captures = "/tmp/VelvetVows_Phase3A_Captures";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string name) =>
            Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == name);
        private static object Value(object actor, string name) => actor.GetType().GetProperty(name, Flags).GetValue(actor);
        private static object Field(object actor, string name) => actor.GetType().GetField(name, Flags).GetValue(actor);
        private static void Call(object actor, string name) => actor.GetType().GetMethod(name, Flags).Invoke(actor, null);
        private static IEnumerator Beat(MonoBehaviour intro, string expected, float timeout = 40f)
        {
            float until = Time.time + timeout;
            while ((string)Value(intro, "CurrentBeat") != expected && Time.time < until) yield return null;
            Assert.AreEqual(expected, (string)Value(intro, "CurrentBeat"));
        }
        private static IEnumerator Capture(string name)
        {
            if (Application.isBatchMode) yield break;
            Directory.CreateDirectory(Captures);
            yield return new WaitForSeconds(.6f);
            ScreenCapture.CaptureScreenshot(Path.Combine(Captures, name + ".png"));
            yield return new WaitForSeconds(.15f);
        }
        private static IEnumerator Begin()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var begin = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.name == "BEGIN Button");
            begin.onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator FullIntroShowsBothImagesAndRestoresPhase2A()
        {
            yield return Begin();
            var intro = Component("Phase3ADiaryIntro");
            var story = Component("Phase2AStoryManager");
            var game = Component("GameManager");
            Assert.NotNull(Field(intro, "adrianPolaroid"));
            Assert.NotNull(Field(intro, "evelynDiary"));
            Assert.NotNull(Field(intro, "storyComposition"));
            yield return Beat(intro, "BEFORE HIM");
            Assert.AreEqual("Phase3AIntro", Value(game, "State").ToString());
            yield return new WaitForSeconds(.7f);
            yield return Capture("01_before_him");
            yield return Beat(intro, "THE LAST HAPPY SUMMER");
            var segments = Field(intro, "japaneseSegments");
            Assert.AreEqual(1, Value(segments, "StartedSegments"));
            yield return Beat(intro, "THE GIRL NOBODY WANTED");
            Assert.AreEqual(1, Value(segments, "StartedSegments"));
            yield return Beat(intro, "THE FIRST PERSON WHO STAYED");
            Assert.AreEqual(1, Value(segments, "StartedSegments"));
            yield return Beat(intro, "ADRIAN VALMONT");
            Assert.IsTrue(((Image)Field(intro, "photograph")).gameObject.activeInHierarchy);
            yield return Capture("02_adrian_polaroid");
            yield return Beat(intro, "DIARY");
            Assert.IsTrue(((Image)Field(intro, "diary")).gameObject.activeInHierarchy);
            yield return Capture("03_evelyn_diary");
            yield return Beat(intro, "MY LITTLE COLLECTION");
            yield return Capture("04_little_collection");
            yield return Beat(intro, "TITLE");
            yield return Capture("05_velvet_vows_title");
            yield return Beat(intro, "COMPLETE");
            float elapsed = (float)Value(intro, "Elapsed");
            Assert.That(elapsed, Is.InRange(58f, 72f), "Intro includes the uninterrupted 26-second childhood narration");
            float until = Time.time + 5f;
            while (Value(story, "Stage").ToString() != "MissingStudent" && Time.time < until) yield return null;
            Assert.AreEqual("MissingStudent", Value(story, "Stage").ToString());
            Assert.AreEqual("Phase2A", Value(game, "State").ToString());
            yield return Capture("06_phase2a_gameplay");
            Debug.Log("[Phase3A Test] Full diary intro " + elapsed.ToString("F1") + "s; both sprites and Phase2A resume passed");
        }

        [UnityTest]
        public IEnumerator SkipIntroReturnsToTheSameOpeningContinuation()
        {
            yield return Begin();
            var intro = Component("Phase3ADiaryIntro");
            var story = Component("Phase2AStoryManager");
            yield return Beat(intro, "BEFORE HIM");
            var button = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.name == "SKIP INTRO");
            Assert.IsTrue(button.gameObject.activeInHierarchy);
            button.onClick.Invoke();
            yield return Beat(intro, "COMPLETE", 3f);
            Assert.IsFalse((bool)Value(Field(intro, "japaneseSegments"), "IsPlaying"));
            float until = Time.time + 5f;
            while (Value(story, "Stage").ToString() != "MissingStudent" && Time.time < until) yield return null;
            Assert.AreEqual("MissingStudent", Value(story, "Stage").ToString());
            Assert.AreEqual("Phase2A", Value(Component("GameManager"), "State").ToString());
        }
    }
}
