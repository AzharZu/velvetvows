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

namespace VelvetVows.Tests
{
    public sealed class Phase3E2PlayModeTests
    {
        [TearDown] public void RestoreLogChecking() => LogAssert.ignoreFailingMessages = false;
        private const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string type) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == type);
        private static object Field(object actor, string name) => actor.GetType().GetField(name, Flags).GetValue(actor);
        private static object Value(object actor, string name) => actor.GetType().GetProperty(name, Flags | BindingFlags.Static).GetValue(actor);
        private static void Call(object actor, string name, params object[] args) => actor.GetType().GetMethod(name, Flags).Invoke(actor, args);
        private static IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Call(Component("GameHUD"), "ShowStart", false);
        }
        private static IEnumerator Capture(string name)
        {
            if (Application.isBatchMode) yield break;
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Phase3E2_Captures"));
            Directory.CreateDirectory(folder);
            yield return new WaitForSeconds(.25f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            yield return new WaitForSeconds(.25f);
        }
        private static void Frame(Vector3 position, Vector3 target, float fov)
        {
            var camera = Camera.main;
            camera.GetComponents<Behaviour>().First(x => x.GetType().Name == "CinemachineBrain").enabled = false;
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            camera.fieldOfView = fov;
        }
        [UnityTest]
        public IEnumerator StudioSinkPaintingsDoorAndScreenshots()
        {
            yield return Load();
            var sink = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Studio wash basin");
            Assert.NotNull(sink);
            Assert.That(sink.position.z, Is.EqualTo(-6.55f).Within(.05f));
            Assert.NotNull(sink.GetComponent<SphereCollider>());
            var students = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == "Student easel").ToArray();
            Assert.IsTrue(students.All(x => !x.gameObject.activeInHierarchy));
            var extraTable = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(t => t.name == "Painting worktable one");
            Assert.IsFalse(extraTable.gameObject.activeInHierarchy);
            var door = Component("Phase2AStudioDoor");
            var leaf = door.transform.Find("Hinge/Dark walnut door leaf");
            Assert.NotNull(leaf);
            StringAssert.Contains("Studio Walnut Door", leaf.GetComponent<Renderer>().sharedMaterial.name);
            Assert.IsTrue(leaf.GetComponentsInChildren<Collider>(true).All(c => c.transform == leaf || !c.enabled));
            Frame(new Vector3(-15.6f, 2.6f, -1.4f), new Vector3(-19.2f, 1.2f, -2.8f), 56);
            yield return Capture("01_art_studio");
        }
        [UnityTest]
        public IEnumerator CelesteAndEvelynAreFramedAndCameraRestores()
        {
            // Unity's Package Manager window intermittently logs a Hub token error
            // when this graphical test opens; it is unrelated to scene playback.
            LogAssert.ignoreFailingMessages = true;
            yield return Load();
            var story = Component("Phase2AStoryManager"); var game = Component("GameManager");
            Call(game, "ChangeState", Enum.Parse(game.GetType().Assembly.GetType("VelvetVows.GameState"), "Phase2A"), "Confront Celeste");
            var player = (Transform)Field(story, "player"); var celeste = (Transform)Field(story, "celeste");
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
            player.SetPositionAndRotation(new Vector3(-17.2f, 0, -2.7f), Quaternion.identity); cc.enabled = true;
            celeste.SetPositionAndRotation(new Vector3(-19.2f, 0, -2.7f), Quaternion.identity);
            Call(story, "ConfrontCeleste");
            yield return new WaitForSeconds(1f);
            var dialogue = Component("DialogueManager");
            Assert.IsTrue((bool)Value(Component("DialogueManager"), "IsOpen"));
            var face = Camera.main.WorldToViewportPoint(celeste.position + Vector3.up * 1.45f);
            Assert.That(face.z, Is.GreaterThan(0));
            Assert.That(face.x, Is.InRange(.2f, .8f));
            Assert.That(face.y, Is.InRange(.15f, .85f));
            Call(dialogue, "Continue"); yield return null;
            var evelynFace = Camera.main.WorldToViewportPoint(player.position + Vector3.up * 1.45f);
            Assert.That(evelynFace.x, Is.InRange(.2f, .8f));
            Assert.That(evelynFace.y, Is.InRange(.15f, .85f));
            for (int i = 0; i < 6; i++) Call(dialogue, "Continue");
            yield return new WaitForSeconds(11f);
            Assert.IsTrue(Camera.main.GetComponents<Behaviour>().First(x => x.GetType().Name == "CinemachineBrain").enabled, "Gameplay camera did not return after the attack");
        }
        [UnityTest]
        public IEnumerator PrefectWarnsOnceAndResumesPatrol()
        {
            yield return Load();
            var game = Component("GameManager"); var story = Component("Phase2AStoryManager");
            Call(game, "ChangeState", Enum.Parse(game.GetType().Assembly.GetType("VelvetVows.GameState"), "Phase2A"), "Explore the academy");
            story.GetType().GetField("<Stage>k__BackingField", Flags).SetValue(story, Enum.Parse(story.GetType().Assembly.GetType("VelvetVows.Phase2AStage"), "Overhear"));
            var prefect = Component("Phase2BPrefectInvestigation");
            var player = (Transform)Value(game, "Player");
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
            player.position = prefect.transform.position + Vector3.back * 1.25f; cc.enabled = true;
            yield return new WaitForSeconds(.65f);
            Assert.IsTrue((bool)Value(prefect, "FirstWarningShown"));
            Assert.IsTrue((bool)Value(Component("DialogueManager"), "IsOpen"));
            var coat = prefect.GetComponentInChildren<SkinnedMeshRenderer>();
            StringAssert.Contains("Prefect Ivory Uniform", coat.sharedMaterials[7].name);
            Frame(prefect.transform.position + new Vector3(2.1f, 1.85f, -2.4f), prefect.transform.position + Vector3.up * 1.25f, 45);
            var dialogue = Component("DialogueManager");
            Call(dialogue, "Continue"); Call(dialogue, "Continue");
            yield return null;
            Assert.IsTrue((bool)Value(prefect, "WarningActive"));
            yield return new WaitForSeconds(3.8f);
            Assert.IsFalse((bool)Value(prefect, "WarningActive"));
            Assert.IsTrue(((Behaviour)Field(prefect, "normalPatrol")).enabled);
            Assert.IsFalse((bool)Value(Component("DialogueManager"), "IsOpen"));
        }
        [UnityTest]
        public IEnumerator ClothingCleaningRunsFiveSecondsWhileTimerContinues()
        {
            yield return (IEnumerator)typeof(Phase2BPlayModeTests).GetMethod("StartCleanup", BindingFlags.Static | Flags).Invoke(null, null);
            var cleanup = Component("Phase2BCleanupManager");
            var voice = Component("CharacterVoiceManager");
            var action = Enum.Parse(cleanup.GetType().Assembly.GetType("VelvetVows.Phase2BAction"), "Clothing");
            var start = Time.time; var remaining = (float)Value(cleanup, "Remaining");
            Call(cleanup, "Interact", action);
            yield return new WaitForSeconds(3.8f);
            Assert.IsFalse((bool)Value(cleanup, "ClothingCleaned"));
            Assert.IsTrue(((GameObject)Field(cleanup, "clothingMarks")).activeSelf);
            Assert.That(remaining - (float)Value(cleanup, "Remaining"), Is.GreaterThan(3.3f));
            while (!(bool)Value(cleanup, "ClothingCleaned") && Time.time - start < 6.5f) yield return null;
            Assert.IsTrue((bool)Value(cleanup, "ClothingCleaned"));
            Assert.That(Time.time - start, Is.InRange(4.8f, 6.1f));
            Assert.IsFalse(((GameObject)Field(cleanup, "clothingMarks")).activeSelf);
            Assert.That((int)Value(voice, "CleanupCount"), Is.EqualTo(1));
        }
    }
}
