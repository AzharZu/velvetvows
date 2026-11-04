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
    public sealed class Phase3DPlayModeTests
    {
        private const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string name) =>
            Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == name);
        private static object Field(object actor, string name) => actor.GetType().GetField(name, Flags).GetValue(actor);
        private static object Value(object actor, string name) => actor.GetType().GetProperty(name, Flags).GetValue(actor);
        private static void Call(object actor, string name) => actor.GetType().GetMethod(name, Flags).Invoke(actor, null);

        [UnityTest]
        public IEnumerator BallroomDanceAndEndingKeepBothLeadsTogether()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var ball = Component("Phase2CBallDirector");
            var game = Component("GameManager");
            var hud = Component("GameHUD");
            Assert.NotNull(ball);
            Assert.NotNull(Field(ball, "portraitLight"));
            Assert.NotNull(Field(ball, "burgundyLight"));
            Assert.NotNull(Field(ball, "ballDecor"));
            Assert.AreEqual(10, ((GameObject)Field(ball, "guests")).transform.childCount);
            Assert.AreEqual(4, ((GameObject)Field(ball, "ballDecor")).transform.childCount);
            ball.GetType().GetField("dialogueSeconds", Flags).SetValue(ball, .25f);
            hud.GetType().GetMethod("ShowStart", Flags).Invoke(hud, new object[] { false });
            Call(game, "EndPhase2B");
            Call(ball, "BeginBall");
            yield return WaitBeat(ball, "Establishing", 12);
            Assert.IsTrue(((Light)Field(ball, "ballLight")).enabled &&
                ((Light)Field(ball, "portraitLight")).enabled &&
                ((Light)Field(ball, "burgundyLight")).enabled);
            yield return Capture("01_ballroom_atmosphere");

            yield return WaitBeat(ball, "Dance", 40);
            yield return new WaitForSeconds(4.4f);
            var player = (Transform)Field(ball, "player");
            var adrian = (Transform)Field(ball, "adrian");
            float separation = Vector3.Distance(player.position, adrian.position);
            Assert.That(separation, Is.InRange(.65f, .9f));
            Assert.Greater(Vector3.Dot(player.forward,
                (adrian.position - player.position).normalized), .6f);
            Assert.Greater(Vector3.Dot(adrian.forward,
                (player.position - adrian.position).normalized), .6f);
            var lovesick = Field(ball, "lovesick");
            Assert.AreEqual("", ((UnityEngine.UI.Text)Field(lovesick, "thought")).text);
            yield return Capture("02_dance_two_shot");

            yield return WaitBeat(ball, "Revelation", 16);
            yield return new WaitForSeconds(1.1f);
            Assert.Less(((Light)Field(ball, "ballLight")).intensity, 1.5f);
            yield return Capture("03_adrian_revelation");

            yield return WaitBeat(ball, "Ending", 35);
            yield return new WaitForSeconds(2.2f);
            Assert.IsTrue((bool)Value(ball, "Resolved"));
            Assert.AreEqual("Phase2CEnding", Value(game, "State").ToString());
            var finalPanel = (GameObject)Field(ball, "finalPanel");
            Assert.IsTrue(finalPanel.activeInHierarchy);
            Assert.AreEqual("Two hearts. One beautiful obsession.",
                finalPanel.transform.Find("Some hearts were never meant to be innocent.")
                    .GetComponent<UnityEngine.UI.Text>().text);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
            yield return Capture("04_a_perfect_duet");
        }

        private static IEnumerator WaitBeat(MonoBehaviour ball, string beat, float timeout)
        {
            float until = Time.time + timeout;
            while ((string)Value(ball, "Beat") != beat && Time.time < until) yield return null;
            Assert.AreEqual(beat, (string)Value(ball, "Beat"));
        }

        private static IEnumerator Capture(string name)
        {
            if (Application.isBatchMode) yield break;
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Phase3D_Captures"));
            Directory.CreateDirectory(directory);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, name + ".png"));
            yield return null;
        }
    }
}
