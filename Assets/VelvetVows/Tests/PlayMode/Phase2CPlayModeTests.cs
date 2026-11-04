using System.Collections;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VelvetVows.Tests
{
    public sealed class Phase2CPlayModeTests
    {
        private const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string Captures = "/tmp/VelvetVows_Phase2C_Captures";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string name) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == name);
        private static object Field(object actor, string name) => actor.GetType().GetField(name, Flags).GetValue(actor);
        private static object Value(object actor, string name) => actor.GetType().GetProperty(name, Flags).GetValue(actor);
        private static void Call(object actor, string name) => actor.GetType().GetMethod(name, Flags).Invoke(actor, null);
        private static IEnumerator CleanupHelper(string name, params object[] arguments) =>
            (IEnumerator)typeof(Phase2BPlayModeTests).GetMethod(name, BindingFlags.Static | Flags).Invoke(null, arguments);
        private static bool Flag(object actor, string name) => (bool)Value(actor, name);
        [UnityTest]
        public IEnumerator FailureStillShowsTheFallenRose()
        {
            yield return CleanupHelper("StartCleanup");
            var cleanup = Component("Phase2BCleanupManager");
            cleanup.GetType().GetMethod("Fail", Flags).Invoke(cleanup, new object[] { "Caught by the Prefect." });
            yield return new WaitForSeconds(3.1f);
            var title = (UnityEngine.UI.Text)Field(cleanup, "outcomeTitle");
            Assert.AreEqual("THE FALLEN ROSE", title.text);
            Assert.IsTrue(((GameObject)Field(cleanup, "outcomePanel")).activeInHierarchy);
            Assert.AreEqual("Phase2BOutcome", Value(Component("GameManager"), "State").ToString());
        }
        [UnityTest]
        public IEnumerator HighReputationChangesIsabellesResponse()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var reputation = Component("Phase2AReputation");
            reputation.GetType().GetMethod("Award", Flags).Invoke(reputation, new object[] { "ball-test", 5 });
            var ball = Component("Phase2CBallDirector");
            ball.GetType().GetField("dialogueSeconds", Flags).SetValue(ball, .35f);
            Call(Component("GameManager"), "EndPhase2B");
            Call(ball, "BeginBall");
            var dialogue = Component("DialogueManager");
            var body = (UnityEngine.UI.Text)Field(dialogue, "bodyText");
            float until = Time.time + 20f;
            while (body.text != "I suppose… perhaps you’re right." && Time.time < until) yield return null;
            Assert.AreEqual("I suppose… perhaps you’re right.", body.text);
            Assert.AreEqual("Phase2CBall", Value(Component("GameManager"), "State").ToString());
        }
        [UnityTest]
        public IEnumerator SuccessfulPhysicalCleanupActuallyOpensTheBall()
        {
            yield return CleanupHelper("StartCleanup");
            var cleanup = Component("Phase2BCleanupManager");
            yield return CleanupHelper("PressE");
            float until = Time.time + 12f;
            while (!Flag(cleanup, "BodyHidden") && Time.time < until) yield return null;
            Assert.IsTrue(Flag(cleanup, "BodyHidden"));
            yield return CleanupHelper("WalkTo", new Vector3(-18f, 0, -5.3f), 16f);
            yield return CleanupHelper("PressE");
            yield return CleanupHelper("WalkTo", new Vector3(-17.2f, 0, -3.1f), 16f);
            yield return CleanupHelper("PressE");
            until = Time.time + 7f;
            while (!Flag(cleanup, "BloodCleaned") && Time.time < until) yield return null;
            Assert.IsTrue(Flag(cleanup, "BloodCleaned"));
            yield return CleanupHelper("WalkTo", new Vector3(-17.2f, 0, -2f), 16f);
            yield return CleanupHelper("WalkTo", new Vector3(-20.9f, 0, -2f), 16f);
            yield return CleanupHelper("PressE");
            yield return CleanupHelper("WalkTo", new Vector3(-18.75f, 0, -5.05f), 16f);
            yield return CleanupHelper("PressE");
            until = Time.time + 35f; // Includes the Phase 3B monologue before the existing ball.
            while (Value(Component("GameManager"), "State").ToString() != "Phase2CBall" && Time.time < until)
                yield return null;
            Assert.AreEqual("Phase2CBall", Value(Component("GameManager"), "State").ToString());
            Assert.IsTrue(((GameObject)Field(Component("Phase2CBallDirector"), "guests")).activeInHierarchy);
            Debug.Log("[Phase2C Test] Real murder and four physical cleanup actions reached the Ball");
        }
        private static IEnumerator WaitBeat(MonoBehaviour ball, string beat, float seconds = 20)
        {
            float until = Time.time + seconds;
            while (ball != null && (string)Value(ball, "Beat") != beat && Time.time < until) yield return null;
            Assert.NotNull(ball);
            Assert.AreEqual(beat, (string)Value(ball, "Beat"), "Ball chapter stalled before " + beat);
        }
        private static IEnumerator Capture(string name)
        {
            // Phase 3D owns the four final visual captures. Retain this call site
            // for the original Phase 2C flow test without producing duplicates.
            yield break;
        }
        [UnityTest]
        public IEnumerator BallPlaysThroughRevelationAndButtonsResetStory()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var game = Component("GameManager");
            var ball = Component("Phase2CBallDirector");
            var hud = Component("GameHUD");
            Assert.NotNull(game); Assert.NotNull(ball); Assert.NotNull(hud);
            ball.GetType().GetField("dialogueSeconds", Flags).SetValue(ball, .09f);
            hud.GetType().GetMethod("ShowStart", Flags).Invoke(hud, new object[] { false });
            Call(game, "EndPhase2B");
            Call(ball, "BeginBall");
            yield return WaitBeat(ball, "Establishing");
            Assert.AreEqual("Phase2CBall", Value(game, "State").ToString());
            Assert.AreEqual(10, ((GameObject)Field(ball, "guests")).transform.childCount);
            Assert.IsTrue(((GameObject)Field(ball, "guests")).activeInHierarchy);
            foreach (Transform guest in ((GameObject)Field(ball, "guests")).transform)
                Assert.IsTrue(guest.gameObject.activeInHierarchy, guest.name + " stayed inactive");
            yield return Capture("01_ballroom_establishing");
            yield return new WaitForSeconds(3.4f);
            yield return Capture("02_evelyn_formal");
            yield return WaitBeat(ball, "Isabelle");
            yield return new WaitForSeconds(.8f);
            yield return Capture("03_isabelle_conversation");
            yield return WaitBeat(ball, "Adrian");
            yield return new WaitForSeconds(.8f);
            yield return Capture("04_adrian_approaching");
            yield return WaitBeat(ball, "Dance");
            yield return new WaitForSeconds(1.4f);
            yield return Capture("05_dance");
            yield return WaitBeat(ball, "Revelation");
            yield return Capture("06_revelation");
            yield return WaitBeat(ball, "Ending");
            Assert.IsTrue((bool)Value(ball, "Resolved"));
            Assert.IsTrue((bool)Value(Field(ball, "lovesick"), "Suppressed"));
            Assert.IsTrue(((GameObject)Field(ball, "finalPanel")).activeInHierarchy);
            Assert.AreEqual("Phase2CEnding", Value(game, "State").ToString());
            yield return Capture("07_a_perfect_duet");
            ((UnityEngine.UI.Button)Field(ball, "playAgainButton")).onClick.Invoke();
            yield return null; yield return null;
            Assert.AreEqual("Phase2A", Value(Component("GameManager"), "State").ToString());
            var menuBall = Component("Phase2CBallDirector");
            ((UnityEngine.UI.Button)Field(menuBall, "mainMenuButton")).onClick.Invoke();
            yield return null; yield return null;
            Assert.AreEqual("Introduction", Value(Component("GameManager"), "State").ToString());
            Debug.Log("[Phase2C Test] Complete ball, lovesick resolution, Play Again and Main Menu passed");
        }
    }
}
