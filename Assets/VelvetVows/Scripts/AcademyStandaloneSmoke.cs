using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelvetVows
{
    // Opt-in diagnostic for the built player; never runs in a regular game session.
    public sealed class AcademyStandaloneSmoke : MonoBehaviour
    {
        private int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (!Environment.GetCommandLineArgs().Contains("-velvet-smoke")) return;
            var go = new GameObject("Standalone validation"); DontDestroyOnLoad(go); go.AddComponent<AcademyStandaloneSmoke>();
        }
        private void Awake() => Application.logMessageReceived += Log;
        private void Log(string text, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        private IEnumerator Start()
        {
            Application.runInBackground=true;
            yield return new WaitForSecondsRealtime(1);
            GameManager.Instance?.Begin();
            yield return new WaitForSecondsRealtime(6);
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args,"-velvet-smoke");
            string report = index + 1 < args.Length ? args[index+1] : Path.Combine(Application.persistentDataPath,"standalone_smoke.txt");
            bool valid = SceneManager.GetActiveScene().name == "VelvetVows_Main" && GameManager.Instance != null && Camera.main != null &&
                FindFirstObjectByType<AcademyActionPerformance>() != null && FindFirstObjectByType<AcademyGrounding>() != null &&
                FindObjectsByType<AcademyBodyPerformance>(FindObjectsSortMode.None).Length > 5;
            ScreenCapture.CaptureScreenshot(Path.ChangeExtension(report,"png"));
            yield return new WaitForSecondsRealtime(1);
            File.WriteAllText(report, "Scene=" + SceneManager.GetActiveScene().name + "\nValid=" + valid + "\nErrors=" + errors +
                "\nPlatform=" + Application.platform + "\nVersion=" + Application.unityVersion);
            Application.Quit(valid && errors == 0 ? 0 : 1);
        }
        private void OnDestroy() => Application.logMessageReceived -= Log;
    }
}
