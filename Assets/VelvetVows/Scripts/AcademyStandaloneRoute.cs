#if DEVELOPMENT_BUILD || UNITY_EDITOR
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
        private AcademyStandaloneCapture capture;
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
            if (!finished) { File.WriteAllText(report, "Result=FAILED\nError=" + message + "\n" + trace); Application.Quit(1); }
        }
        private IEnumerator Start()
        {
            Application.runInBackground = true;
            yield return FullGame(leave);
            finished = true;
            ScreenCapture.CaptureScreenshot(Path.ChangeExtension(report, "png"));
            yield return new WaitForSecondsRealtime(1);
            File.WriteAllText(report, "Result=PASSED\nBranch=" + (leave ? "LEAVE" : "BURN") + "\nErrors=" + errors + "\nScene=" + SceneManager.GetActiveScene().name + "\nPlatform=" + Application.platform);
            Application.Quit(errors == 0 ? 0 : 1);
        }
        private void OnDestroy() { Application.logMessageReceived -= Log; }
        private IEnumerator FullGame(bool leave)
        {
            Time.timeScale = 1;
            yield return new WaitForSeconds(1);
            story = Component("Phase2AStoryManager"); dialogue = Component("DialogueManager");
            game = Component("GameManager"); cleanup = Component("Phase2BCleanupManager"); ball = Component("Phase2CBallDirector");
            player = (Transform)Field(story, "player");
            var listener = UnityEngine.Object.FindFirstObjectByType<AudioListener>();
            Assert.NotNull(listener);
            capture = listener.gameObject.AddComponent<AcademyStandaloneCapture>();
            capture.BeginRecording();
            capture.StartCoroutine(AutoAdvance());
            yield return new WaitForSeconds(2);
            Click(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).First(b => b.name == "BEGIN Button"));
            yield return Until(() => Stage == "Overhear", 200, "opening, complete diary intro and missing-student conversation");

            yield return Walk(new Vector3(-4.4f, 0, 2.8f), () => Stage == "Overhearing", false);
            yield return Until(() => Stage == "WatchAdrian", 100, "first eavesdrop");
            yield return Walk(new Vector3(-20, 0, 14), () => Stage == "Watching");
            yield return Until(() => Stage == "Errands", 100, "library eavesdrop");

            yield return TalkAt(new Vector3(-15, 0, 17), "Celeste refuses before trust");
            yield return PickupAt(new Vector3(-22.6f, 0, 17.4f), () => (bool)Value(story, "HasBooks"), "library books");
            yield return TalkAt(new Vector3(-21, 0, 21), "deliver books to Clara");
            yield return PickupAt(new Vector3(-19.7f, 0, -2.45f), () => (bool)Value(story, "HasArtMaterials"), "art materials");
            yield return TalkAt(new Vector3(-17, 0, -1), "deliver art materials to Margot");
            yield return Walk(new Vector3(-9.4f, 0, 8.6f));
            yield return Direct(new Vector3(-10.05f, 0, 8.6f), .45f);
            yield return Direct(new Vector3(-14.6f, 0, 13.15f), .65f);
            yield return PickupAt(new Vector3(-16.3f, 0, 11.6f), () => (bool)Value(story, "HasPendant"), "Odette pendant");
            yield return TalkAt(new Vector3(-13.5f, 0, 14.6f), "return pendant to Odette");
            Assert.AreEqual(5, Value(Component("Phase2AReputation"), "Value"));
            yield return TalkAt(new Vector3(-15, 0, 17), "Celeste ribbon request");
            Assert.AreEqual("RibbonSearch", Stage);

            yield return Walk(new Vector3(-18.4f, 0, 20.5f));
            yield return PressE();
            var pages = (GameObject)Field(story, "pagePanel");
            Assert.IsTrue(pages.activeInHierarchy, "Sketchbook did not open through E");
            for (int i = 0; i < 5; i++)
            {
                capture.Note("sketchbook", "Page " + (i + 1));
                yield return new WaitForSeconds(5);
                if (i < 4) Click((Button)Field(story, "pageNext"));
            }
            Click((Button)Field(story, "pageClose"));
            yield return Until(() => !DialogueOpen, 40, "sketchbook thoughts");
            yield return PickupAt(new Vector3(-22, 0, 19.3f), () => (bool)Value(story, "HasRibbon"), "black ribbon");
            yield return new WaitForSeconds(4.2f);
            yield return TalkAt(new Vector3(-15, 0, 17), "return ribbon and invite Celeste");
            Assert.AreEqual("FollowCeleste", Stage);
            yield return Walk(new Vector3(-20.5f, 0, -2.5f), () => Stage == "Witness");
            yield return Until(() => Stage == "Witness", 55, "Celeste follows into Studio");
            yield return TalkAt(new Vector3(-17, 0, -1), "ask Margot to leave", false, 1.3f);
            yield return Until(() => Stage == "CloseDoor", 30, "Margot exits");
            yield return Walk(new Vector3(-14.1f, 0, -2.7f));
            yield return PressE();
            yield return Until(() => Stage == "Confront", 5, "close Studio door");
            yield return Direct(((Transform)Field(story, "celeste")).position, 1.4f);
            yield return PressE();
            yield return Until(() => Value(game, "State").ToString() == "Phase2BCleanup", 90, "confrontation and murder");

            // Complete all four objectives using the player controller and E.
            yield return PressE();
            yield return Until(() => (bool)Value(cleanup, "BodyHidden"), 12, "hide Celeste");
            yield return Direct(new Vector3(-18, 0, -5.3f), .8f);
            yield return PressE();
            Assert.IsTrue((bool)Value(cleanup, "HasCloth"));
            yield return Direct(new Vector3(-17.2f, 0, -3.1f), .8f);
            yield return PressE();
            yield return Until(() => (bool)Value(cleanup, "BloodCleaned"), 8, "clean blood");
            yield return Direct(new Vector3(-17.2f, 0, -2), .8f);
            yield return Direct(new Vector3(-20.9f, 0, -2), .8f);
            yield return PressE();
            Assert.IsTrue((bool)Value(cleanup, "EvidenceDisposed"));
            yield return Direct(new Vector3(-18.75f, 0, -5.05f), .8f);
            yield return PressE();
            yield return Until(() => ((GameObject)Field(cleanup, "choicePanel")).activeInHierarchy, 100,
                "clean clothing, emotional narration, dossier monologue and body choice");
            Assert.IsTrue((bool)Value(cleanup, "ClothingCleaned"));
            yield return new WaitForSeconds(5);
            var choice = (GameObject)Field(cleanup, "choicePanel");
            Click(choice.GetComponentsInChildren<Button>().First(b => b.name.StartsWith(leave ? "LEAVE" : "BURN")));
            capture.Note("choice", leave ? "LEAVE THE BODY" : "BURN THE EVIDENCE");
            if (leave)
            {
                yield return Until(() => (bool)Value(ball,"Heartbroken") &&
                    ((GameObject)Field(ball,"heartbrokenPanel")).activeInHierarchy, 200, "HEARTBROKEN screen");
                Assert.AreEqual(1,Value(Component("CharacterVoiceManager"),"GameOverCount"));
                capture.Note("complete","HEARTBROKEN, normal speed, physical errands and cleanup");
                yield return new WaitForSeconds(7); capture.FinishRecording(); yield break;
            }
            yield return Until(() => (string)Value(ball, "Beat") == "Ending" &&
                Value(game, "State").ToString() == "Phase2CEnding" &&
                ((GameObject)Field(ball, "finalPanel")).activeInHierarchy,
                240, "final A PERFECT DUET screen after the final dance");
            Assert.IsFalse((bool)Value(ball, "Heartbroken"));
            Assert.AreEqual(0, Value(Component("CharacterVoiceManager"), "GameOverCount"));
            capture.Note("complete", "A PERFECT DUET, normal speed, physical errands and cleanup");
            yield return new WaitForSeconds(7);
            capture.FinishRecording();
        }

        private IEnumerator AutoAdvance()
        {
            var text = (Text)Field(dialogue, "bodyText");
            var speaker = (Text)Field(dialogue, "speakerText");
            var button = (Button)Field(dialogue, "continueButton");
            string last = "", state = "";
            int cameraRecoveries = 0;
            float nextCameraProbe = 0;
            float since = Time.realtimeSinceStartup;
            while (capture != null && capture.Recording)
            {
                string current = Value(game, "State") + "/" + Stage + "/" + Value(ball, "Beat");
                if (state != current) { state = current; capture.Note("state", current); }
                var cameraComfort = Camera.main != null ? Camera.main.GetComponent<AcademyCameraComfort>() : null;
                if (cameraComfort != null && cameraComfort.WallRecoveryCount != cameraRecoveries)
                {
                    cameraRecoveries = cameraComfort.WallRecoveryCount;
                    capture.Note("camera-recovery", "player=" + player.position + "; camera=" + Camera.main.transform.position);
                }
                if (Stage == "Errands" && player.position.x > -15 && player.position.x < -8 && player.position.z > 2 && Time.realtimeSinceStartup >= nextCameraProbe)
                {
                    nextCameraProbe = Time.realtimeSinceStartup + 1;
                    var cam = Camera.main; var model = player.GetComponentInChildren<Animator>();
                    var head = model.GetBoneTransform(HumanBodyBones.Head);
                    var seen = cam.WorldToViewportPoint(head.position);
                    bool hit = Physics.SphereCast(cam.transform.position, .08f, cam.transform.forward, out var wall, 3f, LayerMask.GetMask("Academy Solid"), QueryTriggerInteraction.Ignore);
                    capture.Note("camera-probe", "player=" + player.position + "; view=" + seen + "; distance=" + Vector3.Distance(cam.transform.position, head.position).ToString("F2") + "; hit=" + (hit ? wall.collider.name + "/" + wall.distance.ToString("F2") : "none"));
                }
                string line = DialogueOpen ? speaker.text + "|" + text.text : "";
                if (line != last)
                {
                    last = line; since = Time.realtimeSinceStartup;
                    if (line.Length > 0) capture.Note("dialogue", line);
                }
                if (line.Length > 0 && button.gameObject.activeInHierarchy && button.interactable)
                {
                    bool diary = ((Image)Field(ball, "diaryImage"))?.gameObject.activeInHierarchy == true;
                    float reading = diary ? 9f : Mathf.Clamp(1f + text.text.Length / 19f, 2.6f, 6f);
                    if (Time.realtimeSinceStartup - since >= reading)
                    { Click(button); since = Time.realtimeSinceStartup; }
                }
                yield return null;
            }
        }

        private IEnumerator Until(Func<bool> ready, float seconds, string context)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!ready() && Time.realtimeSinceStartup < end) yield return null;
            if (!ready()) capture?.Note("blocked", context + "; state=" + Value(game, "State") + "; stage=" + Stage + "; player=" + player.position);
            Assert.IsTrue(ready(), "Timed out: " + context);
        }

        private IEnumerator Walk(Vector3 destination, Func<bool> arrived = null, bool route = true)
        {
            capture?.Note("walk", destination.ToString());
            if (route) yield return WalkRoute(player, destination, arrived ?? (() => false));
            else yield return WalkPlayerTo(player, destination, 35f, arrived, .85f);
        }
        private IEnumerator Direct(Vector3 destination, float radius) => WalkPlayerTo(player, destination, 25f, null, radius);
        private IEnumerator PickupAt(Vector3 at, Func<bool> pickedUp, string label)
        {
            yield return Walk(at); yield return PressE();
            Assert.IsTrue(pickedUp(), "E did not pick up " + label);
            capture.Note("interaction", label);
            yield return new WaitForSeconds(1.2f);
        }
        private IEnumerator TalkAt(Vector3 at, string label, bool route = true, float radius = .85f)
        {
            if (route) yield return Walk(at); else yield return Direct(at, radius);
            yield return PressE();
            yield return Until(() => DialogueOpen, 3, label + " opens");
            capture.Note("interaction", label);
            yield return Until(() => !DialogueOpen, 75, label + " closes");
        }
        private static void Click(Button button)
        {
            Assert.IsTrue(button.IsActive() && button.IsInteractable(), button.name + " disabled");
            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            var point = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * .5f);
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.IsTrue(hits.Count > 0 && (hits[0].gameObject == button.gameObject ||
                hits[0].gameObject.transform.IsChildOf(button.transform)), button.name + " blocked by UI");
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        private static Transform At(string path)
        {
            var parts = path.Split('/');
            var root = SceneManager.GetActiveScene().GetRootGameObjects().First(g => g.name == parts[0]).transform;
            return parts.Length == 1 ? root : root.Find(path.Substring(parts[0].Length + 1));
        }
        private static MonoBehaviour ComponentNamed(Transform t, string name) =>
            t.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == name);
        private static IEnumerator WalkPlayerTo(Transform player, Vector3 destination, float timeout = 25f, Func<bool> earlyArrival = null, float arrivalRadius = .85f)
        {
            var inputs = player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "StarterAssetsInputs");
            var relay = player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "AcademyPlayerInputRelay");
            relay.enabled = false; // Drive the real ThirdPersonController directly in the batch test.
            var move = inputs.GetType().GetMethod("MoveInput");
            float started = Time.time;
            while ((earlyArrival == null || !earlyArrival()) &&
                   Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(destination.x, destination.z)) > arrivalRadius && Time.time - started < timeout)
            {
                var delta = destination - player.position; delta.y = 0;
                var camera = Camera.main;
                var right = camera.transform.right; right.y = 0; right.Normalize();
                var forward = camera.transform.forward; forward.y = 0; forward.Normalize();
                var direction = new Vector2(Vector3.Dot(delta.normalized, right), Vector3.Dot(delta.normalized, forward));
                move.Invoke(inputs, new object[] { direction });
                yield return null;
            }
            move.Invoke(inputs, new object[] { Vector2.zero });
            Debug.Log($"[Phase2A Test] Walk toward {destination}: now {player.position}, elapsed {Time.time - started:F1}s");
            if (earlyArrival != null && earlyArrival()) yield break;
            Assert.Less(Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(destination.x, destination.z)), arrivalRadius + .25f,
                "Actual ThirdPersonController movement could not reach target");
        }
        private static bool IsArchitecture(Collider collider)
        {
            for (var t = collider.transform; t != null; t = t.parent)
                if (t.name == "GOTHIC_ACADEMY" || t.name == "ENVIRONMENT") return true;
            return false;
        }
        private static bool FreeAt(float x, float z)
        {
            var lower = new Vector3(x, .52f, z);
            var upper = new Vector3(x, 1.48f, z);
            foreach (var collider in Physics.OverlapCapsule(lower, upper, .4f, ~0, QueryTriggerInteraction.Ignore))
                if (IsArchitecture(collider) || collider.GetComponents<MonoBehaviour>().Any(x => x.GetType().Name == "Phase2AFollower")) return false;
            foreach (var hit in Physics.RaycastAll(new Vector3(x, .45f, z), Vector3.down, 1.0f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.normal.y > .55f && IsArchitecture(hit.collider)) return true;
            return false;
        }
        private static List<Vector3> FindRoute(Vector3 start, Vector3 end)
        {
            const float size = .65f, xmin = -44.5f, zmin = -13.5f;
            const int width = 85, height = 68;
            int Index(int x, int z) => z * width + x;
            (int x, int z) Grid(Vector3 p) =>
                (Mathf.Clamp(Mathf.RoundToInt((p.x - xmin) / size), 0, width - 1),
                 Mathf.Clamp(Mathf.RoundToInt((p.z - zmin) / size), 0, height - 1));
            var a = Grid(start); var b = Grid(end);
            Debug.Log($"[Phase2A Test] Navigation probes: start={FreeAt(start.x,start.z)}, end={FreeAt(end.x,end.z)}, hall={FreeAt(0,0)}, hall west={FreeAt(-8,8)}, library door={FreeAt(-10,10)}, music door={FreeAt(-30,14)}");
            var previous = Enumerable.Repeat(-1, width * height).ToArray();
            var walkable = new sbyte[width * height];
            bool Free(int x, int z)
            {
                if (x < 0 || z < 0 || x >= width || z >= height) return false;
                int i = Index(x, z);
                if (walkable[i] == 0) walkable[i] = (sbyte)(FreeAt(xmin + x * size, zmin + z * size) ? 1 : -1);
                return walkable[i] > 0;
            }
            var q = new Queue<int>();
            int startIndex = Index(a.x, a.z); previous[startIndex] = startIndex; q.Enqueue(startIndex);
            int found = -1;
            while (q.Count > 0)
            {
                int i = q.Dequeue(); int x = i % width, z = i / width;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(b.x, b.z)) < 1.5f) { found = i; break; }
                for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int nx = x + dx, nz = z + dz;
                    if (!Free(nx, nz)) continue;
                    if (!FreeAt(xmin + (x + nx) * size * .5f, zmin + (z + nz) * size * .5f)) continue;
                    if (dx != 0 && dz != 0 && (!Free(x + dx, z) || !Free(x, z + dz))) continue;
                    int ni = Index(nx, nz);
                    if (previous[ni] >= 0) continue;
                    previous[ni] = i; q.Enqueue(ni);
                }
            }
            if (found < 0)
            {
                var checks = new[] { new Vector3(-8,0,6), new Vector3(-10,0,8), new Vector3(-10,0,10), new Vector3(-13,0,10), new Vector3(-20,0,14), new Vector3(-26,0,14), new Vector3(-28,0,14), new Vector3(-30,0,14) };
                Debug.Log("[Phase2A Test] Reachability: " + string.Join("; ", checks.Select(p => { var g = Grid(p); return $"{p.x},{p.z}: {previous[Index(g.x,g.z)] >= 0} free={Free(g.x,g.z)}"; })));
                var blockers = Physics.OverlapCapsule(new Vector3(-26,.52f,14), new Vector3(-26,1.48f,14),.25f,~0,QueryTriggerInteraction.Ignore);
                Debug.Log("[Phase2A Test] Gateway blockers: " + string.Join("; ", blockers.Select(c => c.name + " @ " + c.transform.position)));
                throw new AssertionException("No walkable route found from " + start + " to " + end + "; visited=" + previous.Count(v => v >= 0) + "; end-grid-free=" + Free(b.x,b.z));
            }
            var result = new List<Vector3>();
            for (int i = found; i != startIndex; i = previous[i])
                result.Add(new Vector3(xmin + (i % width) * size, 0, zmin + (i / width) * size));
            result.Reverse();
            var compressed = new List<Vector3>();
            for (int i = 0; i < result.Count; i++)
            {
                if (i > 0 && i + 1 < result.Count)
                {
                    var before = result[i] - result[i - 1];
                    var after = result[i + 1] - result[i];
                    if (Vector3.Cross(before, after).sqrMagnitude < .001f) continue;
                }
                compressed.Add(result[i]);
            }
            Debug.Log($"[Phase2A Test] Route {start} → {end}: {compressed.Count} turns: " + string.Join("; ", compressed));
            return compressed;
        }
        private static IEnumerator WalkRoute(Transform player, Vector3 destination, Func<bool> earlyArrival)
        {
            foreach (var point in FindRoute(player.position, destination))
            {
                if (earlyArrival()) yield break;
                yield return WalkPlayerTo(player, point, 12f, earlyArrival, .32f);
            }
            if (!earlyArrival()) yield return WalkPlayerTo(player, destination, 15f, earlyArrival, 1.35f);
        }

        private static IEnumerator PressE()
        {
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            InputSystem.Update();
            Debug.Log($"[Phase2A Test] Injected E: device={keyboard.deviceId}, current={Keyboard.current?.deviceId}, isPressed={keyboard.eKey.isPressed}, wasPressed={keyboard.eKey.wasPressedThisFrame}, mode={InputSystem.settings.updateMode}");
            Assert.IsTrue(keyboard.eKey.isPressed, "Injected E key was not registered by Input System");
            var player = At("Velvet Vows Main/PLAYER/Evelyn Moreau - player controller");
            var interactor = ComponentNamed(player, "PlayerInteractor");
            interactor.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(interactor, null);
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            yield return null;
            foreach (var action in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b => b.GetType().Name == "AcademyActionPerformance"))
                for (float t = 0; t < 8 && (bool)action.GetType().GetProperty("IsBusy").GetValue(action); t += Time.unscaledDeltaTime) yield return null;
        }


        private sealed class AssertionException : Exception { public AssertionException(string message) : base(message) {} }
        private static class Assert
        {
            public static void IsTrue(bool value, string message = "Expected true") { if (!value) throw new AssertionException(message); }
            public static void IsFalse(bool value, string message = "Expected false") { if (value) throw new AssertionException(message); }
            public static void NotNull(object value) { if (value == null) throw new AssertionException("Expected a value"); }
            public static void AreEqual(object expected, object actual, string message = null) { if (!Equals(expected, actual)) throw new AssertionException((message ?? "Values differ") + ": expected " + expected + ", actual " + actual); }
            public static void Less(float actual, float expected, string message = null) { if (!(actual < expected)) throw new AssertionException(message ?? "Distance exceeded"); }
        }
    }
}
#endif
