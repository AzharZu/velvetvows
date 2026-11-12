from pathlib import Path
source=Path('Assets/VelvetVows/Tests/PlayMode/RecordedPlaythroughTests.cs').read_text()
helpers=Path('Assets/VelvetVows/Tests/PlayMode/Phase2APlayModeTests.cs').read_text()
route=source[source.index('        private IEnumerator FullGame(bool leave)'):source.index('        [UnityTest, Explicit("Manual capture setup probe')]
route=route[:route.index('        [UnityTearDown]')]+route[route.index('        private IEnumerator AutoAdvance()'):]
route=route.replace('            OpenGameView();\n','').replace('            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",\n                new LoadSceneParameters(LoadSceneMode.Single));\n','')
route=route.replace('AddComponent<PlaythroughCapture>()','AddComponent<StandaloneRouteCapture>()')
a=route.index('            if (route)\n                yield return (IEnumerator)typeof(Phase2APlayModeTests)')
b=route.index('        private IEnumerator PickupAt',a)
route=route[:a]+'''            if (route) yield return WalkRoute(player, destination, arrived ?? (() => false));
            else yield return WalkPlayerTo(player, destination, 35f, arrived, .85f);
        }
        private IEnumerator Direct(Vector3 destination, float radius) => WalkPlayerTo(player, destination, 25f, null, radius);
'''+route[b:]
helper=helpers[helpers.index('        private static Transform At('):helpers.index('        [UnityTest]\n        public IEnumerator OpeningRuns')]
helper=helper[:helper.index('        private static IEnumerator SkipDiaryForLegacyRoute')]+helper[helper.index('        private static IEnumerator WalkPlayerTo'):]
header='''#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace VelvetVows
{
    // Opt-in physical route for checking the actual standalone player.
    public sealed class AcademyStandaloneRoute : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private StandaloneRouteCapture capture;
        private MonoBehaviour story, dialogue, game, cleanup, ball;
        private Transform player;
        private static string report;
        private bool finished;
        private int errors;
        private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        private static object Value(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
        private static MonoBehaviour Component(string name) => UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.GetType().Name == name);
        private string Stage => Value(story, "Stage").ToString();
        private bool DialogueOpen => (bool)Value(dialogue, "IsOpen");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-velvet-route");
            if (index < 0 || index + 2 >= args.Length) return;
            report = args[index + 2];
            var go = new GameObject("Standalone physical route"); DontDestroyOnLoad(go);
            var runner = go.AddComponent<AcademyStandaloneRoute>(); runner.leave = args[index + 1] == "leave";
        }
        private bool leave;
        private void Awake() { Application.logMessageReceived += Log; }
        private void Log(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Assert && type != LogType.Exception) return;
            errors++;
            if (!finished) { File.WriteAllText(report, "Result=FAILED\\nError=" + message + "\\n" + trace); Application.Quit(1); }
        }
        private IEnumerator Start()
        {
            Application.runInBackground = true;
            yield return FullGame(leave);
            finished = true;
            ScreenCapture.CaptureScreenshot(Path.ChangeExtension(report, "png"));
            yield return new WaitForSecondsRealtime(1);
            File.WriteAllText(report, "Result=PASSED\\nBranch=" + (leave ? "LEAVE" : "BURN") + "\\nErrors=" + errors + "\\nScene=" + SceneManager.GetActiveScene().name + "\\nPlatform=" + Application.platform);
            Application.Quit(errors == 0 ? 0 : 1);
        }
        private void OnDestroy() { Application.logMessageReceived -= Log; }
'''
footer='''
        private sealed class AssertionException : Exception { public AssertionException(string message) : base(message) {} }
        private static class Assert
        {
            public static void IsTrue(bool value, string message = "Expected true") { if (!value) throw new AssertionException(message); }
            public static void IsFalse(bool value, string message = "Expected false") { if (value) throw new AssertionException(message); }
            public static void NotNull(object value) { if (value == null) throw new AssertionException("Expected a value"); }
            public static void AreEqual(object expected, object actual, string message = null) { if (!Equals(expected, actual)) throw new AssertionException((message ?? "Values differ") + ": expected " + expected + ", actual " + actual); }
            public static void Less(float actual, float expected, string message = null) { if (!(actual < expected)) throw new AssertionException(message ?? "Distance exceeded"); }
        }
        private sealed class StandaloneRouteCapture : MonoBehaviour
        {
            public bool Recording { get; private set; }
            public void BeginRecording() { Recording = true; File.WriteAllText(report + ".timeline", "seconds\\tkind\\tdetail\\n"); }
            public void Note(string kind, string detail) { if (Recording) File.AppendAllText(report + ".timeline", Time.realtimeSinceStartup.ToString("F2") + "\\t" + kind + "\\t" + detail.Replace('\\n', ' ') + "\\n"); }
            public void FinishRecording() { Note("recording", "stop"); Recording = false; }
        }
    }
}
#endif
'''
Path('Assets/VelvetVows/Scripts/AcademyStandaloneRoute.cs').write_text(header+route+helper+footer)
