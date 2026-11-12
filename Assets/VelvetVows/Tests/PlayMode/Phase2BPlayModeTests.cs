using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VelvetVows.Tests
{
    public sealed class Phase2BPlayModeTests
    {
        private const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string type) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == type);
        private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        private static object Value(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
        private static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
        private static string State() => Value(Component("GameManager"), "State").ToString();
        private static bool Flag(object target, string name) => (bool)Value(target, name);
        private static IEnumerator StartCleanup()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var game = Component("GameManager"); var hud = Component("GameHUD"); var murder = Component("Phase2BMurderSequence");
            // Mirror the story prerequisite: Margot has left and the Studio door is shut.
            var story = Component("Phase2AStoryManager");
            ((Transform)Field(story, "witness")).position = new Vector3(-8f, 0, -2f);
            var studioDoor = Field(story, "studioDoor");
            yield return (IEnumerator)studioDoor.GetType().GetMethod("CloseVisible", Flags).Invoke(studioDoor, null);
            var player = (Transform)Value(game, "Player"); var celeste = (Transform)Field(murder, "celeste");
            Call(hud, "ShowStart", false);
            var playerController = player.GetComponent<CharacterController>(); playerController.enabled = false;
            player.SetPositionAndRotation(new Vector3(-15.8f, 0, -2.7f), Quaternion.Euler(0, -90, 0)); playerController.enabled = true;
            var celesteController = celeste.GetComponent<CharacterController>(); celesteController.enabled = false;
            celeste.SetPositionAndRotation(new Vector3(-17.2f, 0, -2.95f), Quaternion.Euler(0, 90, 0)); celesteController.enabled = true;
            Call(murder, "Begin");
            float start = Time.time;
            while (State() != "Phase2BCleanup" && Time.time - start < 20f) yield return null;
            Assert.AreEqual("Phase2BCleanup", State(), "Attack did not reach cleanup");
        }
        private static IEnumerator PressE()
        {
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update();
            Assert.IsTrue(keyboard.eKey.isPressed);
            Call(Component("PlayerInteractor"), "Update"); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null;
            foreach (var action in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b => b.GetType().Name == "AcademyActionPerformance"))
                for (float t = 0; t < 8 && (bool)action.GetType().GetProperty("IsBusy").GetValue(action); t += Time.unscaledDeltaTime) yield return null;
        }
        private static IEnumerator WalkTo(Vector3 destination, float timeout = 16f)
        {
            var player = (Transform)Value(Component("GameManager"), "Player");
            var inputs = player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "StarterAssetsInputs");
            var relay = player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "AcademyPlayerInputRelay");
            relay.enabled = false;
            float start = Time.time;
            while (Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(destination.x, destination.z)) > .8f && Time.time - start < timeout)
            {
                var delta = destination - player.position; delta.y = 0;
                var right = Camera.main.transform.right; right.y = 0; right.Normalize();
                var forward = Camera.main.transform.forward; forward.y = 0; forward.Normalize();
                Call(inputs, "MoveInput", new Vector2(Vector3.Dot(delta.normalized, right), Vector3.Dot(delta.normalized, forward)));
                yield return null;
            }
            Call(inputs, "MoveInput", Vector2.zero);
            Debug.Log($"[Phase2B Test] Walk to {destination}: reached {player.position} in {Time.time - start:F1}s");
            Assert.Less(Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(destination.x, destination.z)), 1.05f,
                "Player could not walk to cleanup point " + destination);
        }
        [UnityTest]
        public IEnumerator MurderShowsPersistentBodyAndStartsTimer()
        {
            yield return StartCleanup();
            var murder = Component("Phase2BMurderSequence"); var cleanup = Component("Phase2BCleanupManager");
            Assert.IsTrue(((GameObject)Field(murder, "bodyInteraction")).activeInHierarchy);
            Assert.IsTrue(((GameObject)Field(murder, "bloodPool")).activeInHierarchy);
            Assert.IsTrue(((GameObject)Field(murder, "clothingMarks")).activeInHierarchy);
            Assert.IsTrue(((GameObject)Field(murder, "attackProp")).activeInHierarchy);
            Assert.Greater((float)Value(cleanup, "Remaining"), 115f);
            var celeste = (Transform)Field(murder, "celeste"); var corpse = celeste.position;
            yield return new WaitForSeconds(2f);
            Assert.Less(Vector3.Distance(corpse, celeste.position), .03f, "Death pose root drifted");
            Debug.Log("[Phase2B Test] Attack, death pose, blood and timer passed");
        }
        [UnityTest]
        public IEnumerator PlayerCanCleanAllFourObjectives()
        {
            yield return StartCleanup();
            var cleanup = Component("Phase2BCleanupManager");
            yield return PressE();
            float t = Time.time; while (!Flag(cleanup, "BodyHidden") && Time.time - t < 12f) yield return null;
            Assert.IsTrue(Flag(cleanup, "BodyHidden"), "Body hiding did not finish");
            yield return WalkTo(new Vector3(-18f, 0, -5.3f)); yield return PressE();
            Assert.IsTrue(Flag(cleanup, "HasCloth"));
            yield return WalkTo(new Vector3(-17.2f, 0, -3.1f)); yield return PressE();
            t = Time.time; while (!Flag(cleanup, "BloodCleaned") && Time.time - t < 7f) yield return null;
            Assert.IsTrue(Flag(cleanup, "BloodCleaned")); Assert.IsTrue(Flag(cleanup, "StainedCloth"));
            yield return WalkTo(new Vector3(-17.2f, 0, -2.0f));
            yield return WalkTo(new Vector3(-20.9f, 0, -2.0f)); yield return PressE();
            Assert.IsTrue(Flag(cleanup, "EvidenceDisposed"));
            yield return WalkTo(new Vector3(-18.75f, 0, -5.05f)); yield return PressE();
            t = Time.time; while (State() != "Phase2BOutcome" && Time.time - t < 6f) yield return null;
            Assert.IsTrue(Flag(cleanup, "ClothingCleaned")); Assert.AreEqual("Phase2BOutcome", State());
            Assert.Greater((float)Value(cleanup, "Remaining"), 0);
            Debug.Log("[Phase2B Test] Four physical E interactions and success passed");
        }
        [UnityTest]
        public IEnumerator TimeoutFailureAndRetryRestoreEvidence()
        {
            yield return StartCleanup();
            var cleanup = Component("Phase2BCleanupManager");
            cleanup.GetType().GetField("<Remaining>k__BackingField", Flags).SetValue(cleanup, .1f);
            yield return new WaitForSeconds(.3f);
            Assert.AreEqual("Phase2BOutcome", State());
            yield return new WaitForSeconds(7.0f);
            Assert.IsTrue(((GameObject)Field(cleanup, "outcomePanel")).activeInHierarchy);
            StringAssert.Contains("THE FALLEN ROSE", ((UnityEngine.UI.Text)Field(cleanup, "outcomeTitle")).text);
            Assert.AreEqual(1, Value(Component("CharacterVoiceManager"), "GameOverCount"));
            ((UnityEngine.UI.Button)Field(cleanup, "retryButton")).onClick.Invoke(); yield return null;
            Assert.AreEqual("Phase2BCleanup", State());
            Assert.IsFalse(Flag(cleanup, "BodyHidden")); Assert.IsFalse(Flag(cleanup, "BloodCleaned"));
            Assert.IsFalse(Flag(cleanup, "EvidenceDisposed")); Assert.IsFalse(Flag(cleanup, "ClothingCleaned"));
            Assert.IsTrue(((GameObject)Field(Component("Phase2BMurderSequence"), "bodyInteraction")).activeInHierarchy);
            Assert.Greater((float)Value(cleanup, "Remaining"), 119f);
            Assert.IsFalse((bool)Value(Component("CharacterVoiceManager"), "GameOverPlaying"));
            var prefect = Component("Phase2BPrefectInvestigation");
            var patrol = (MonoBehaviour)Field(prefect, "normalPatrol");
            Assert.IsTrue(patrol.enabled, "Prefect patrol did not restart on retry");
            Assert.LessOrEqual((int)Field(patrol, "next"), 1, "Prefect route did not reset deterministically");
            Debug.Log("[Phase2B Test] Timeout failure and retry passed");
        }
        [UnityTest]
        public IEnumerator ExposedBodyIsDetectedThroughOpenDoor()
        {
            yield return StartCleanup();
            var cleanup = Component("Phase2BCleanupManager");
            var prefect = Component("Phase2BPrefectInvestigation");
            var door = Field(cleanup, "studioDoor");
            yield return (IEnumerator)door.GetType().GetMethod("CloseVisible", Flags).Invoke(door, null);
            var body = (Transform)Field(prefect, "body");
            var player = (Transform)Field(cleanup, "player");
            var playerCollider = player.GetComponent<CharacterController>(); playerCollider.enabled = false;
            player.position = new Vector3(-18f, 0, -5f); playerCollider.enabled = true;
            ((GameObject)Field(cleanup, "attackProp")).SetActive(false);
            ((GameObject)Field(cleanup, "clothingMarks")).SetActive(false);
            prefect.transform.position = new Vector3(-11.5f, 0, -3f);
            var towards = body.position - prefect.transform.position; towards.y = 0;
            prefect.transform.rotation = Quaternion.LookRotation(towards);
            Call(prefect, "Tick", 91f);
            Assert.AreEqual("Phase2BCleanup", State(), "Closed Studio door should block the Prefect's view");
            Call(cleanup, "Interact", Enum.Parse(cleanup.GetType().Assembly.GetType("VelvetVows.Phase2BAction"), "Door"));
            yield return new WaitForSeconds(1.4f);
            prefect.transform.position = new Vector3(-11.5f, 0, -3f);
            prefect.transform.rotation = Quaternion.LookRotation(towards);
            Call(prefect, "Tick", 91f);
            Assert.AreEqual("Phase2BOutcome", State(), "Exposed body through open door was not detected");
            yield return new WaitForSeconds(7.0f);
            StringAssert.Contains("Celeste was found", ((UnityEngine.UI.Text)Field(cleanup, "outcomeSubtitle")).text);
        }
        [UnityTest]
        public IEnumerator VisibleBloodIsDetectedAfterBodyHidden()
        {
            yield return StartCleanup();
            var cleanup = Component("Phase2BCleanupManager");
            var prefect = Component("Phase2BPrefectInvestigation");
            var actionType = cleanup.GetType().Assembly.GetType("VelvetVows.Phase2BAction");
            var door = Field(cleanup, "studioDoor");
            yield return (IEnumerator)door.GetType().GetMethod("CloseVisible", Flags).Invoke(door, null);
            Call(cleanup, "Interact", Enum.Parse(actionType, "Body"));
            float start = Time.time; while (!Flag(cleanup, "BodyHidden") && Time.time - start < 12f) yield return null;
            Assert.IsTrue(Flag(cleanup, "BodyHidden"));
            var player = (Transform)Field(cleanup, "player");
            var playerCollider = player.GetComponent<CharacterController>(); playerCollider.enabled = false;
            player.position = new Vector3(-18f, 0, -5f); playerCollider.enabled = true;
            ((GameObject)Field(cleanup, "attackProp")).SetActive(false);
            ((GameObject)Field(cleanup, "clothingMarks")).SetActive(false);
            var pool = (Transform)Field(prefect, "bloodPool");
            prefect.transform.position = new Vector3(-11.5f, 0, -3f);
            var towards = pool.position - prefect.transform.position; towards.y = 0;
            prefect.transform.rotation = Quaternion.LookRotation(towards);
            Call(cleanup, "Interact", Enum.Parse(actionType, "Door"));
            yield return new WaitForSeconds(1.4f);
            prefect.transform.position = new Vector3(-11.5f, 0, -3f);
            prefect.transform.rotation = Quaternion.LookRotation(towards);
            Call(prefect, "Tick", 91f);
            Assert.AreEqual("Phase2BOutcome", State(), "Visible blood through open door was not detected");
            yield return new WaitForSeconds(7.0f);
            StringAssert.Contains("blood was discovered", ((UnityEngine.UI.Text)Field(cleanup, "outcomeSubtitle")).text);
        }
        [UnityTest]
        public IEnumerator PrefectPatrolsThenWalksToStudioEntrance()
        {
            yield return StartCleanup();
            var cleanup = Component("Phase2BCleanupManager");
            var prefect = Component("Phase2BPrefectInvestigation");
            var patrol = (MonoBehaviour)Field(prefect, "normalPatrol");
            Assert.IsTrue(patrol.enabled, "Existing Prefect patrol should continue during early cleanup");
            var initial = prefect.transform.position;
            yield return new WaitForSeconds(1.0f);
            Assert.Greater(Vector3.Distance(initial, prefect.transform.position), .2f, "Prefect did not patrol before approach");
            var door = Field(cleanup, "studioDoor");
            yield return (IEnumerator)door.GetType().GetMethod("CloseVisible", Flags).Invoke(door, null);
            Call(prefect, "Tick", 90f);
            Assert.IsFalse(patrol.enabled, "Normal patrol should hand off to the Studio approach");
            yield return new WaitForSeconds(29f);
            Assert.Less(prefect.transform.position.x, -10.5f, "Prefect did not reach the Studio entrance in the final 30 seconds");
            Assert.AreEqual("Phase2BCleanup", State(), "Closed Studio door should prevent sight through the wall");
            Debug.Log("[Phase2B Test] Prefect patrolled and reached Studio entrance without teleporting");
        }
    }
}
