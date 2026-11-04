using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace VelvetVows.Tests
{
    public sealed class Phase3BPlayModeTests
    {
        private const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string Captures = "/tmp/VelvetVows_Phase3B_Captures";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string name) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == name);
        private static object Field(object actor, string name) => actor.GetType().GetField(name, Flags).GetValue(actor);
        private static object Value(object actor, string name) => actor.GetType().GetProperty(name, Flags).GetValue(actor);
        private static void Call(object actor, string name, params object[] args) =>
            actor.GetType().GetMethod(name, Flags).Invoke(actor, args);
        private static IEnumerator CleanupHelper(string name, params object[] args) =>
            (IEnumerator)typeof(Phase2BPlayModeTests).GetMethod(name, BindingFlags.Static | Flags).Invoke(null, args);
        private static IEnumerator Capture(string name)
        {
            if (Application.isBatchMode) yield break;
            Directory.CreateDirectory(Captures);
            yield return new WaitForSeconds(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(Captures, name + ".png"));
            yield return new WaitForSeconds(.1f);
        }
        private static IEnumerator PressKey(Key key, MonoBehaviour receiver)
        {
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            Call(receiver, "Update");
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null;
        }
        private static IEnumerator CleanFourObjectives()
        {
            yield return CleanupHelper("StartCleanup");
            var cleanup = Component("Phase2BCleanupManager");
            yield return CleanupHelper("PressE");
            float until = Time.time + 12f;
            while (!(bool)Value(cleanup, "BodyHidden") && Time.time < until) yield return null;
            Assert.IsTrue((bool)Value(cleanup, "BodyHidden"));
            yield return CleanupHelper("WalkTo", new Vector3(-18f, 0, -5.3f), 16f);
            yield return CleanupHelper("PressE");
            yield return CleanupHelper("WalkTo", new Vector3(-17.2f, 0, -3.1f), 16f);
            yield return CleanupHelper("PressE");
            until = Time.time + 7f;
            while (!(bool)Value(cleanup, "BloodCleaned") && Time.time < until) yield return null;
            Assert.IsTrue((bool)Value(cleanup, "BloodCleaned"));
            yield return CleanupHelper("WalkTo", new Vector3(-17.2f, 0, -2f), 16f);
            yield return CleanupHelper("WalkTo", new Vector3(-20.9f, 0, -2f), 16f);
            yield return CleanupHelper("PressE");
            yield return CleanupHelper("WalkTo", new Vector3(-18.75f, 0, -5.05f), 16f);
            yield return CleanupHelper("PressE");
        }

        [UnityTest]
        public IEnumerator LibraryBookOpensDossierWithEAndRestoresMovementOnClose()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var game = Component("GameManager");
            var hud = Component("GameHUD");
            var dossier = Component("Phase3BMissingStudents");
            Assert.NotNull(Field(dossier, "newspaperCollage"));
            Call(hud, "ShowStart", false);
            Call(game, "ChangeState", Enum.Parse(game.GetType().Assembly.GetType("VelvetVows.GameState"), "Phase2A"), "Visit the Library.");
            Call(game, "SetPhase2AControl", true);
            var player = (Transform)Value(game, "Player");
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.SetPositionAndRotation(new Vector3(-15.2f, 0, 18.2f), Quaternion.Euler(0, 160, 0));
            cc.enabled = true;
            yield return null;
            Assert.IsTrue((bool)dossier.GetType().GetMethod("CanOpen", Flags).Invoke(dossier, new object[] { game }),
                "Dossier director rejected the Phase2A interaction");
            var book = Component("Phase3BDossierInteractable");
            Assert.IsTrue((bool)book.GetType().GetMethod("CanInteract", Flags).Invoke(book, new object[] { game }));
            var trigger = book.GetComponent<SphereCollider>();
            Assert.NotNull(trigger);
            Physics.SyncTransforms();
            var near = Physics.OverlapSphere(player.position + Vector3.up, 2.2f, 1, QueryTriggerInteraction.Collide);
            Assert.Contains(trigger, near, "Dossier book trigger is outside the player's interaction sphere");
            var brain = Camera.main.GetComponents<Behaviour>().First(x => x.GetType().Name == "CinemachineBrain");
            brain.enabled = false;
            Camera.main.transform.position = book.transform.position + new Vector3(1.5f, 1.45f, -1.6f);
            Camera.main.transform.LookAt(book.transform.position + Vector3.up * .08f);
            yield return Capture("01_library_book");
            brain.enabled = true;
            yield return null;
            yield return PressKey(Key.E, Component("PlayerInteractor"));
            Assert.IsTrue((bool)Value(dossier, "DossierOpen"));
            Assert.AreEqual("Phase3BDossier", Value(game, "State").ToString());
            Assert.AreEqual(0, (int)Value(dossier, "DossierLineIndex"));
            Assert.IsFalse(player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "ThirdPersonController").enabled);
            yield return Capture("02_open_dossier");
            yield return new WaitForSeconds(.35f);
            yield return PressKey(Key.E, dossier);
            // Synthetic key events can be missed in a full Play Mode run. Retry
            // the actual key event, while requiring Update to advance the dossier.
            if ((int)Value(dossier, "DossierLineIndex") == 0)
            {
                yield return new WaitForSeconds(.35f);
                yield return PressKey(Key.E, dossier);
            }
            Assert.AreEqual(1, (int)Value(dossier, "DossierLineIndex"));
            yield return PressKey(Key.Escape, dossier);
            Assert.IsFalse((bool)Value(dossier, "DossierOpen"));
            Assert.AreEqual("Phase2A", Value(game, "State").ToString());
            Assert.AreEqual("Visit the Library.", Value(game, "Objective"));
            Assert.IsTrue(player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "ThirdPersonController").enabled);
        }

        [UnityTest]
        public IEnumerator SuccessfulCleanupShowsMonologueThenNextEveningAndBall()
        {
            yield return CleanFourObjectives();
            var dossier = Component("Phase3BMissingStudents");
            float until = Time.time + 12f;
            while (!(bool)Value(dossier, "IsMonologuePlaying") && Time.time < until) yield return null;
            Assert.IsTrue((bool)Value(dossier, "IsMonologuePlaying"));
            until = Time.time + 3f;
            while ((string)Value(dossier, "CurrentThought") == "" && Time.time < until) yield return null;
            Assert.AreNotEqual("", Value(dossier, "CurrentThought"));
            yield return Capture("03_adrian_monologue");
            until = Time.time + 25f;
            while ((string)Value(dossier, "CurrentThought") != "COMPLETE" && Time.time < until) yield return null;
            Assert.AreEqual("COMPLETE", Value(dossier, "CurrentThought"));
            var cleanup = Component("Phase2BCleanupManager");
            var title = (Text)Field(cleanup, "outcomeTitle");
            var panel = (GameObject)Field(cleanup, "outcomePanel");
            until = Time.time + 2f;
            while (!panel.activeInHierarchy && Time.time < until) yield return null;
            Assert.AreEqual("THE NEXT EVENING", title.text);
            yield return Capture("04_next_evening");
            until = Time.time + 8f;
            while (Value(Component("GameManager"), "State").ToString() != "Phase2CBall" && Time.time < until) yield return null;
            Assert.AreEqual("Phase2CBall", Value(Component("GameManager"), "State").ToString());
            Assert.IsTrue((bool)Value(dossier, "MonologuePlayed"));
        }

        [UnityTest]
        public IEnumerator SkippingMonologueStillEntersTheBall()
        {
            yield return CleanFourObjectives();
            var dossier = Component("Phase3BMissingStudents");
            float until = Time.time + 12f;
            while (!(bool)Value(dossier, "IsMonologuePlaying") && Time.time < until) yield return null;
            Assert.IsTrue((bool)Value(dossier, "IsMonologuePlaying"));
            var skip = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.name == "SKIP MONOLOGUE");
            Assert.IsTrue(skip.gameObject.activeInHierarchy);
            skip.onClick.Invoke();
            until = Time.time + 8f;
            while (Value(Component("GameManager"), "State").ToString() != "Phase2CBall" && Time.time < until) yield return null;
            Assert.AreEqual("Phase2CBall", Value(Component("GameManager"), "State").ToString());
        }
    }
}
