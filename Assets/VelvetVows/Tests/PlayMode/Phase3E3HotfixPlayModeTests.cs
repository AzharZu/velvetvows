using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace VelvetVows.Tests
{
    public sealed class Phase3E3HotfixPlayModeTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string name) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == name);
        private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        private static object Value(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
        private static Button Button(string name) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.name == name);
        private static IEnumerator Until(Func<bool> ready, float seconds)
        {
            float limit = Time.realtimeSinceStartup + seconds;
            while (!ready() && Time.realtimeSinceStartup < limit) yield return null;
            Assert.IsTrue(ready(), "Timed out waiting for hotfix story state");
        }
        private static IEnumerator LoadAndBegin()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Button("BEGIN Button").onClick.Invoke();
        }
        private static void PointerClick(Button button)
        {
            Assert.IsTrue(button.IsActive() && button.IsInteractable(), button.name + " is disabled");
            var rect = (RectTransform)button.transform;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var point = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * .5f);
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.IsTrue(hits.Count > 0 &&
                (hits[0].gameObject == button.gameObject || hits[0].gameObject.transform.IsChildOf(button.transform)),
                button.name + " is blocked by another UI raycast target");
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
        private static IEnumerator Press(Key key)
        {
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            Component("DialogueManager").GetType().GetMethod("Update", Flags).Invoke(Component("DialogueManager"), null);
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null;
        }

        [UnityTest]
        public IEnumerator SkipIntroPointerReturnsToStoryAndControls()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadAndBegin();
            var diary = Component("Phase3ADiaryIntro");
            var story = Component("Phase2AStoryManager");
            yield return Until(() => (bool)Value(diary, "IsPlaying"), 35f);
            Assert.AreEqual(1f, Time.timeScale);
            PointerClick(Button("SKIP INTRO"));
            yield return Until(() => !(bool)Value(diary, "IsPlaying"), 2f);
            Assert.AreEqual("COMPLETE", Value(diary, "CurrentBeat"));
            Assert.IsFalse((bool)Value(Field(diary, "japaneseSegments"), "IsPlaying"));
            yield return Until(() => Value(story, "Stage").ToString() == "MissingStudent", 3f);
            Assert.IsTrue((bool)Value(Component("DialogueManager"), "IsOpen"));
            for (int i = 0; i < 7; i++) { PointerClick(Button("Continue")); yield return null; }
            yield return Until(() => Value(story, "Stage").ToString() == "Overhear", 3f);
            Assert.IsTrue(Camera.main.GetComponents<Behaviour>().First(x => x.GetType().Name == "CinemachineBrain").enabled);
            var player = (Transform)Field(story, "player");
            Assert.IsTrue(player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "ThirdPersonController").enabled);
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator FirstMeetingAndNormalDiaryProgression()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadAndBegin();
            var story = Component("Phase2AStoryManager");
            var dialogue = Component("DialogueManager");
            var diary = Component("Phase3ADiaryIntro");
            var body = (Text)Field(dialogue, "bodyText");
            yield return Until(() => body.text == "Are you alright?", 18f);
            PointerClick(Button("Continue"));
            yield return Until(() => body.text == "Y-yes. Thank you.", 2f);
            yield return Press(Key.E);
            yield return Until(() => body.text == "Be careful, Evelyn.", 3f);
            yield return Press(Key.Space);
            yield return Until(() => (bool)Value(diary, "IsPlaying"), 12f);
            Assert.IsFalse((bool)Value(Field(diary, "japaneseSegments"), "IsPlaying"));
            yield return Until(() => Value(story, "Stage").ToString() == "MissingStudent", 80f);
            Assert.AreEqual("COMPLETE", Value(diary, "CurrentBeat"));
            yield return Press(Key.E);
            Assert.AreEqual(2, (int)Field(dialogue, "next"));
            yield return Press(Key.Space);
            Assert.AreEqual(3, (int)Field(dialogue, "next"));
            for (int i = 0; i < 5; i++) { PointerClick(Button("Continue")); yield return null; }
            yield return Until(() => Value(story, "Stage").ToString() == "Overhear", 3f);
            var player = (Transform)Field(story, "player");
            Assert.IsTrue(player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "ThirdPersonController").enabled);
            Assert.IsTrue(Camera.main.GetComponents<Behaviour>().First(x => x.GetType().Name == "CinemachineBrain").enabled);
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
