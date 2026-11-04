using System;
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
    public sealed class Phase3E3PlayModeTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string name) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == name);
        private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        private static object Value(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
        private static bool Stage(MonoBehaviour story, string name) => Value(story, "Stage").ToString() == name;
        private static IEnumerator Until(Func<bool> ready, float seconds)
        {
            float limit = Time.time + seconds;
            while (!ready() && Time.time < limit) yield return null;
            Assert.IsTrue(ready(), "Timed out waiting for cinematic stage");
        }
        private static void Relocate(Transform player, Vector3 point)
        {
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.position = point;
            cc.enabled = true;
        }
        private static IEnumerator Capture(string name)
        {
            if (Application.isBatchMode) yield break;
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Phase3E3_Captures"));
            Directory.CreateDirectory(folder);
            if (File.Exists(Path.Combine(folder, name + ".png"))) yield break;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            yield return new WaitForEndOfFrame();
        }

        [UnityTest]
        public IEnumerator EavesdropRouteRestoresControlAndKeepsVoicesDistinct()
        {
            LogAssert.ignoreFailingMessages = true;
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var story = Component("Phase2AStoryManager");
            var voice = Component("CharacterVoiceManager");
            var dialogue = Component("DialogueManager");
            var player = (Transform)Field(story, "player");
            var brain = Camera.main.GetComponents<Behaviour>().First(x => x.GetType().Name == "CinemachineBrain");
            var next = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.name == "Continue");
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.name == "BEGIN Button").onClick.Invoke();
            var diary = Component("Phase3ADiaryIntro");
            yield return Until(() => (bool)Value(diary, "IsPlaying"), 45f);
            diary.GetType().GetMethod("Skip", Flags).Invoke(diary, null);
            yield return Until(() => Stage(story, "MissingStudent"), 6f);
            Assert.That(player.position.x, Is.LessThan(-6.5f), "Evelyn must be behind the hall column");
            Assert.IsFalse(brain.enabled);
            Assert.AreEqual("evelyn_tells_astory", ((AudioClip)Field(diary, "unverifiedJapaneseNarration")).name);
            Assert.AreEqual("adrian_tellstory", ((AudioClip)Field(Component("Phase3BMissingStudents"), "unverifiedJapaneseNarration")).name);
            Assert.AreEqual("isabelle_worried", ((AudioClip)Value(voice, "LastVoiceClip")).name);
            yield return Capture("01_isabelle_amelie");
            next.onClick.Invoke(); yield return null;
            Assert.AreEqual("npc1", ((AudioClip)Value(voice, "LastVoiceClip")).name);
            next.onClick.Invoke(); next.onClick.Invoke();
            Assert.AreEqual(4, (int)Field(dialogue, "next"), "Each distinct click must advance one line");
            yield return null;
            for (int i = 0; i < 4; i++) { next.onClick.Invoke(); yield return null; }
            yield return Until(() => Stage(story, "Overhear"), 3f);
            Assert.IsTrue(brain.enabled);
            yield return Until(() => (bool)Field(story, "isabelleReady"), 8f);
            var celeste = (Transform)Field(story, "celeste");
            var isabelle = (Transform)Field(story, "isabelle");
            Relocate(player, (celeste.position + isabelle.position) * .5f + Vector3.back);
            yield return Until(() => Stage(story, "Overhearing"), 3f);
            yield return new WaitForSeconds(.25f);
            Assert.That(player.position.z, Is.GreaterThan(6.4f), "Evelyn must be behind the second hall column");
            Assert.IsFalse(brain.enabled);
            for (int i = 0; i < 7; i++) { next.onClick.Invoke(); yield return null; }
            yield return Until(() => Stage(story, "WatchAdrian"), 3f);
            Assert.IsTrue(brain.enabled);
            var adrian = (Transform)Field(story, "adrian");
            Relocate(player, adrian.position + Vector3.back * 3.5f);
            yield return Until(() => Stage(story, "Watching"), 3f);
            yield return Until(() => (bool)Value(dialogue, "IsOpen"), 6f);
            Assert.That(player.position.x, Is.LessThan(-21.8f), "Evelyn must be by the Library post");
            next.onClick.Invoke(); yield return null;
            Assert.AreEqual("adryan_calm", ((AudioClip)Value(voice, "LastVoiceClip")).name);
            yield return Capture("02_adrian_celeste");
            for (int i = 0; i < 8; i++) { next.onClick.Invoke(); yield return null; }
            yield return Until(() => Stage(story, "Errands"), 22f);
            Assert.IsTrue(brain.enabled);
            Assert.IsTrue(player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "ThirdPersonController").enabled);
            voice.GetType().GetMethod("QueuePrefectIncident", Flags).Invoke(voice, new object[] { false });
            voice.GetType().GetMethod("PlayDialogue", Flags).Invoke(voice, new object[] { "PREFECT", "Suspicious behavior." });
            Assert.AreEqual("prefect_detect_smth", ((AudioClip)Value(voice, "LastVoiceClip")).name);
            voice.GetType().GetMethod("QueuePrefectIncident", Flags).Invoke(voice, new object[] { true });
            voice.GetType().GetMethod("PlayDialogue", Flags).Invoke(voice, new object[] { "PREFECT", "A body." });
            Assert.AreEqual("prefect_found_body", ((AudioClip)Value(voice, "LastVoiceClip")).name);
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
