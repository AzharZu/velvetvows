using System;
using System.Collections;
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
    public sealed class Phase3GPlayModeTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private static MonoBehaviour Get(string name) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == name);
        private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        private static object Value(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
        private static object Invoke(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, Flags).Invoke(target, args);
        private static void SetBacking(object target, string property, object value) =>
            target.GetType().GetField("<" + property + ">k__BackingField", Flags).SetValue(target, value);
        private static IEnumerator Until(Func<bool> ready, float seconds, string name)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!ready() && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsTrue(ready(), "Timed out: " + name);
        }
        private static IEnumerator Load()
        {
            Time.timeScale = 1;
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }
        private static void Move(Transform actor, Vector3 position)
        {
            var cc = actor.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            actor.position = position;
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
        }
        private static void ReadyCleanup()
        {
            var cleanup = Get("Phase2BCleanupManager");
            ((MonoBehaviour)Field(cleanup, "blood")).gameObject.SetActive(true);
            Invoke(cleanup, "BeginChallenge");
            SetBacking(cleanup, "BodyHidden", true);
            SetBacking(cleanup, "EvidenceDisposed", true);
            SetBacking(cleanup, "ClothingCleaned", true);
            SetBacking(Field(cleanup, "blood"), "Cleaned", true);
            ((GameObject)Field(cleanup, "attackProp")).SetActive(false);
            ((GameObject)Field(cleanup, "clothingMarks")).SetActive(false);
        }
        private static IEnumerator BeginChoice(bool leave)
        {
            ReadyCleanup();
            var cleanup = Get("Phase2BCleanupManager");
            cleanup.GetType().GetMethod("CheckSuccess", Flags).Invoke(cleanup, null);
            Time.timeScale = 15;
            var panel = (GameObject)Field(cleanup, "choicePanel");
            yield return Until(() => panel.activeInHierarchy, 55, "body choice after completed cleanup");
            Assert.IsFalse((bool)Value(cleanup, "Active"));
            Assert.IsFalse((bool)Value(cleanup, "ChoiceMade"));
            Invoke(cleanup, "ChooseBodyFate", leave);
            Invoke(cleanup, "ChooseBodyFate", !leave);
            Assert.IsTrue((bool)Value(cleanup, "ChoiceMade"));
            Assert.AreEqual(leave, Value(cleanup, "BodyLeftBehind"), "choice must be single-use");
            var ball = Get("Phase2CBallDirector");
            yield return Until(() => (string)Value(ball, "Beat") != "Dormant", 15, "ballroom begins");
            Assert.AreEqual(leave, Value(ball, "BodyLeftBehind"));
        }
        [UnityTest]
        public IEnumerator HiddenEvidenceIsSafeButVisibleStainsCauseOneFailureSound()
        {
            yield return Load();
            ReadyCleanup();
            var cleanup = Get("Phase2BCleanupManager");
            var prefect = Get("Phase2BPrefectInvestigation");
            Move((Transform)Field(cleanup, "player"), new Vector3(-18f, .02f, -1f));
            Move(prefect.transform, new Vector3(-18f, 0, -2.7f));
            prefect.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            Invoke(prefect, "Tick", 1f);
            Assert.IsTrue((bool)Value(cleanup, "Active"), "hidden evidence must not cause discovery");
            ((GameObject)Field(cleanup, "clothingMarks")).SetActive(true);
            Invoke(prefect, "Tick", 2f);
            Assert.IsFalse((bool)Value(cleanup, "Active"), "visible bloodstained clothing must cause discovery");
            Time.timeScale = 15;
            var voice = Get("CharacterVoiceManager");
            yield return Until(() => (int)Value(voice, "GameOverCount") == 1, 10, "game over audio");
            Invoke(prefect, "Tick", 3f);
            Assert.AreEqual(1, Value(voice, "GameOverCount"));
            Invoke(cleanup, "Retry");
            Assert.IsFalse((bool)Value(voice, "GameOverPlaying"),
                "Retry must stop the previous failure sound");
            Time.timeScale = 1;
        }
        [UnityTest]
        public IEnumerator BurnChoiceShowsThreeDiaryPagesAndReachesPerfectDuet()
        {
            yield return Load();
            yield return BeginChoice(false);
            var ball = Get("Phase2CBallDirector");
            var dialogue = Get("DialogueManager");
            var body = (Text)Field(dialogue, "bodyText");
            foreach (var words in new[] { "I knew you were watching me.",
                "But you weren’t the only one watching.", "I kept the things you left behind." })
            {
                yield return Until(() => body.text == words, 25, words);
                yield return null;
                var button = (Button)Field(dialogue, "continueButton");
                button.onClick.Invoke();
            }
            yield return Until(() => (string)Value(ball, "Beat") == "Ending", 30, "A PERFECT DUET ending");
            Assert.IsTrue((bool)Value(ball, "Resolved"));
            Assert.IsFalse((bool)Value(ball, "Heartbroken"));
            Assert.AreEqual(0, Value(Get("CharacterVoiceManager"), "GameOverCount"));
            Time.timeScale = 1;
        }
        [UnityTest]
        public IEnumerator LeaveChoiceInterruptsBallAndShowsHeartbroken()
        {
            yield return Load();
            yield return BeginChoice(true);
            var ball = Get("Phase2CBallDirector");
            yield return Until(() => (bool)Value(ball, "Heartbroken") &&
                (int)Value(Get("CharacterVoiceManager"), "GameOverCount") == 1,
                30, "HEARTBROKEN interruption");
            Assert.IsFalse((bool)Value(ball, "Resolved"));
            Assert.AreEqual("Heartbroken", Value(ball, "Beat"));
            Assert.IsTrue(((GameObject)Field(ball, "heartbrokenPanel")).activeInHierarchy);
            Assert.AreEqual(1, Value(Get("CharacterVoiceManager"), "GameOverCount"));
            Time.timeScale = 1;
        }
    }
}
