using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace VelvetVows.Tests
{
    public sealed class Phase2APlayModeTests
    {
        private static Transform At(string path)
        {
            var parts = path.Split('/');
            var root = SceneManager.GetActiveScene().GetRootGameObjects().First(g => g.name == parts[0]).transform;
            return parts.Length == 1 ? root : root.Find(path.Substring(parts[0].Length + 1));
        }
        private static MonoBehaviour ComponentNamed(Transform t, string name) =>
            t.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == name);
        private static object Value(object target, string property) => target.GetType().GetProperty(property).GetValue(target);
        private static IEnumerator SkipDiaryForLegacyRoute(MonoBehaviour story)
        {
            var intro = story.GetType().GetField("secretDiary").GetValue(story);
            Assert.NotNull(intro);
            float until = Time.time + 45f;
            while (!(bool)Value(intro, "IsPlaying") && Time.time < until) yield return null;
            Assert.IsTrue((bool)Value(intro, "IsPlaying"), "The new diary intro did not start");
            intro.GetType().GetMethod("Skip").Invoke(intro, null);
            until = Time.time + 5f;
            while (Value(story, "Stage").ToString() != "MissingStudent" && Time.time < until) yield return null;
            Assert.AreEqual("MissingStudent", Value(story, "Stage").ToString());
        }
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

        [UnityTest]
        public IEnumerator OpeningRunsAndPlayerControlReturns()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var core = At("Velvet Vows Main/SYSTEMS/Core Gameplay");
            var manager = ComponentNamed(core, "GameManager");
            var story = ComponentNamed(core, "Phase2AStoryManager");
            Assert.NotNull(manager); Assert.NotNull(story);
            var begin = At("Gameplay Canvas/Start Panel/BEGIN Button").GetComponent<Button>();
            Assert.IsTrue(begin.gameObject.activeInHierarchy);
            begin.onClick.Invoke();
            Assert.AreEqual("Opening", Value(story, "Stage").ToString());
            yield return SkipDiaryForLegacyRoute(story);
            Assert.AreEqual("MissingStudent", Value(story, "Stage").ToString());
            var dialoguePanel = At("Gameplay Canvas/World Dialogue Panel");
            Assert.IsTrue(dialoguePanel.gameObject.activeInHierarchy);
            var continueButton = At("Gameplay Canvas/World Dialogue Panel/Continue").GetComponent<Button>();
            for (int i = 0; i < 7; i++) { continueButton.onClick.Invoke(); yield return null; }
            Assert.AreEqual("Overhear", Value(story, "Stage").ToString());
            var player = At("Velvet Vows Main/PLAYER/Evelyn Moreau - player controller");
            var controller = player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "ThirdPersonController");
            Assert.IsTrue(controller.enabled, "Player movement was not restored after opening dialogue");
            Debug.Log("[Phase2A Test] Opening, missing-student conversation, and control restoration passed");
        }

        [UnityTest]
        public IEnumerator CompletePlayerDrivenPhase2ARoute()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var story = ComponentNamed(At("Velvet Vows Main/SYSTEMS/Core Gameplay"), "Phase2AStoryManager");
            At("Gameplay Canvas/Start Panel/BEGIN Button").GetComponent<Button>().onClick.Invoke();
            yield return SkipDiaryForLegacyRoute(story);
            var continueButton = At("Gameplay Canvas/World Dialogue Panel/Continue").GetComponent<Button>();
            for (int i = 0; i < 7; i++) { continueButton.onClick.Invoke(); yield return null; }
            yield return new WaitForSeconds(5f);
            var player = At("Velvet Vows Main/PLAYER/Evelyn Moreau - player controller");
            yield return WalkPlayerTo(player, new Vector3(-4.4f, 0, 2.8f), 25f,
                () => Value(story, "Stage").ToString() == "Overhearing");
            Assert.AreEqual("Overhearing", Value(story, "Stage").ToString());
            Assert.IsTrue(At("Gameplay Canvas/World Dialogue Panel").gameObject.activeInHierarchy);
            for (int i = 0; i < 7; i++) { continueButton.onClick.Invoke(); yield return null; }
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual("WatchAdrian", Value(story, "Stage").ToString());
            yield return WalkRoute(player, new Vector3(-20f, 0, 14f),
                () => Value(story, "Stage").ToString() == "Watching");
            Assert.AreEqual("Watching", Value(story, "Stage").ToString());
            yield return new WaitForSeconds(2f);
            for (int i = 0; i < 9; i++) { continueButton.onClick.Invoke(); yield return null; }
            yield return new WaitForSeconds(12f);
            Assert.AreEqual("Errands", Value(story, "Stage").ToString());
            yield return WalkRoute(player, new Vector3(-15f, 0, 17f), () => false);
            yield return PressE();
            Assert.IsTrue(At("Gameplay Canvas/World Dialogue Panel").gameObject.activeInHierarchy,
                "Celeste should respond at Reputation 0");
            Assert.AreEqual("Errands", Value(story, "Stage").ToString(), "Trust gate opened too early");
            continueButton.onClick.Invoke(); yield return null;
            Debug.Log("[Phase2A Test] Physical approach, observation and Reputation 0 refusal passed");
            yield return WalkRoute(player, new Vector3(-22.6f, 0, 17.4f), () => false);
            yield return PressE();
            Assert.IsTrue((bool)Value(story, "HasBooks"), "The Library book pickup was not reachable through E");
            yield return WalkRoute(player, new Vector3(-21f, 0, 21f), () => false);
            yield return PressE();
            Assert.IsTrue(At("Gameplay Canvas/World Dialogue Panel").gameObject.activeInHierarchy);
            for (int i = 0; i < 2; i++) { continueButton.onClick.Invoke(); yield return null; }
            var reputation = ComponentNamed(At("Velvet Vows Main/SYSTEMS/Core Gameplay"), "Phase2AReputation");
            Assert.AreEqual(2, Value(reputation, "Value"));
            Debug.Log("[Phase2A Test] Library book errand and +2 reputation passed");
            yield return WalkRoute(player, new Vector3(-19.7f, 0, -2.45f), () => false);
            yield return PressE();
            Assert.IsTrue((bool)Value(story, "HasArtMaterials"), "Art materials were not reachable through E");
            yield return WalkRoute(player, new Vector3(-17f, 0, -1f), () => false);
            yield return PressE();
            Assert.IsTrue(At("Gameplay Canvas/World Dialogue Panel").gameObject.activeInHierarchy);
            for (int i = 0; i < 2; i++) { continueButton.onClick.Invoke(); yield return null; }
            Assert.AreEqual(4, Value(reputation, "Value"));
            Debug.Log("[Phase2A Test] Art materials errand and +2 reputation passed");
            // Cross the existing diagonal Library opening; the straight lower edge has a floor seam.
            yield return WalkRoute(player, new Vector3(-9.4f, 0, 8.6f), () => false);
            yield return WalkPlayerTo(player, new Vector3(-10.05f, 0, 8.6f), 8f, null, .45f);
            yield return WalkPlayerTo(player, new Vector3(-14.6f, 0, 13.15f), 15f, null, .65f);
            yield return WalkRoute(player, new Vector3(-16.3f, 0, 11.6f), () => false);
            yield return PressE();
            Assert.IsTrue((bool)Value(story, "HasPendant"), "Pendant was not reachable through E");
            yield return WalkRoute(player, new Vector3(-13.5f, 0, 14.6f), () => false);
            yield return PressE();
            Assert.IsTrue(At("Gameplay Canvas/World Dialogue Panel").gameObject.activeInHierarchy);
            for (int i = 0; i < 2; i++) { continueButton.onClick.Invoke(); yield return null; }
            Assert.AreEqual(5, Value(reputation, "Value"));
            Debug.Log("[Phase2A Test] Pendant errand and exact Reputation 5 passed");
            yield return WalkRoute(player, new Vector3(-15f, 0, 17f), () => false);
            yield return PressE();
            Assert.IsTrue(At("Gameplay Canvas/World Dialogue Panel").gameObject.activeInHierarchy);
            for (int i = 0; i < 4; i++) { continueButton.onClick.Invoke(); yield return null; }
            Assert.AreEqual("RibbonSearch", Value(story, "Stage").ToString());
            Debug.Log("[Phase2A Test] Reputation 5 trust gate and ribbon request passed");
            yield return WalkRoute(player, new Vector3(-18.4f, 0, 20.5f), () => false);
            yield return PressE();
            var page = At("Gameplay Canvas/Sketchbook pages");
            Assert.IsTrue(page.gameObject.activeInHierarchy, "Optional sketchbook could not be opened");
            var pageBody = At("Gameplay Canvas/Sketchbook pages/Temporary page text").GetComponent<Text>();
            var pageNext = At("Gameplay Canvas/Sketchbook pages/NEXT button").GetComponent<Button>();
            for (int i = 0; i < 4; i++) { pageNext.onClick.Invoke(); yield return null; }
            StringAssert.Contains("She will never have to worry", pageBody.text);
            At("Gameplay Canvas/Sketchbook pages/CLOSE button").GetComponent<Button>().onClick.Invoke(); yield return null;
            for (int i = 0; i < 4; i++) { continueButton.onClick.Invoke(); yield return null; }
            Assert.IsTrue((bool)Value(story, "SketchbookSeen"));
            Debug.Log("[Phase2A Test] Optional five-page sketchbook passed");
            yield return WalkRoute(player, new Vector3(-22f, 0, 19.3f), () => false);
            yield return PressE();
            Assert.IsTrue((bool)Value(story, "HasRibbon"));
            Assert.AreEqual("RibbonReturn", Value(story, "Stage").ToString());
            yield return WalkRoute(player, new Vector3(-15f, 0, 17f), () => false);
            yield return PressE();
            Assert.IsTrue(At("Gameplay Canvas/World Dialogue Panel").gameObject.activeInHierarchy);
            for (int i = 0; i < 7; i++) { continueButton.onClick.Invoke(); yield return null; }
            Assert.AreEqual("FollowCeleste", Value(story, "Stage").ToString());
            Debug.Log("[Phase2A Test] Physical ribbon return and Celeste follow start passed");
            yield return WalkRoute(player, new Vector3(-20.5f, 0, -2.5f), () => Value(story, "Stage").ToString() == "Witness");
            float followWait = Time.time;
            while (Value(story, "Stage").ToString() == "FollowCeleste" && Time.time - followWait < 55f) yield return null;
            Assert.AreEqual("Witness", Value(story, "Stage").ToString(), "Celeste did not follow the player's corridor route into the Art Studio");
            var celeste = At("Velvet Vows Main/SYSTEMS/Core Gameplay/Gameplay Actors/Celeste - story route");
            Assert.Less(Vector3.Distance(celeste.position, player.position), 4.5f);
            Assert.Less(celeste.position.x, -17.2f, "Celeste must enter the Studio before the witness objective starts");
            Debug.Log("[Phase2A Test] Celeste physically followed into Art Studio: " + celeste.position);
            yield return WalkPlayerTo(player, new Vector3(-17f, 0, -1f), 10f, null, 1.3f);
            yield return PressE();
            Assert.IsTrue(At("Gameplay Canvas/World Dialogue Panel").gameObject.activeInHierarchy);
            for (int i = 0; i < 4; i++) { continueButton.onClick.Invoke(); yield return null; }
            Assert.AreEqual("WitnessLeaving", Value(story, "Stage").ToString());
            float witnessWait = Time.time;
            while (Value(story, "Stage").ToString() == "WitnessLeaving" && Time.time - witnessWait < 18f) yield return null;
            Assert.AreEqual("CloseDoor", Value(story, "Stage").ToString());
            var witness = At("Velvet Vows Main/CHARACTERS/AmbientNPCs/Margot");
            Assert.Greater(witness.position.x, -10f, "Witness did not physically leave the Art Studio");
            Debug.Log("[Phase2A Test] Witness physically left Art Studio: " + witness.position);
            yield return WalkRoute(player, new Vector3(-14.1f, 0, -2f), () => false);
            yield return PressE();
            yield return new WaitForSeconds(2f);
            Assert.AreEqual("Confront", Value(story, "Stage").ToString());
            yield return WalkPlayerTo(player, celeste.position, 10f, null, 1.4f);
            yield return PressE();
            float openingShotUntil = Time.time + 1.5f;
            while (!At("Gameplay Canvas/World Dialogue Panel").gameObject.activeInHierarchy &&
                   Time.time < openingShotUntil) yield return null;
            Assert.IsTrue(At("Gameplay Canvas/World Dialogue Panel").gameObject.activeInHierarchy);
            for (int i = 0; i < 7; i++) { continueButton.onClick.Invoke(); yield return null; }
            Assert.AreEqual("Complete", Value(story, "Stage").ToString());
            var murder = ComponentNamed(At("Velvet Vows Main/SYSTEMS/Core Gameplay"), "Phase2BMurderSequence");
            Assert.NotNull(murder, "Phase 2B must start after confrontation");
            Assert.AreEqual("Phase2BCinematic", Value(ComponentNamed(At("Velvet Vows Main/SYSTEMS/Core Gameplay"), "GameManager"), "State").ToString());
            Assert.IsFalse(At("Gameplay Canvas/Phase 2A complete").gameObject.activeInHierarchy);
            Debug.Log("[Phase2A Test] Full player route and automatic Phase 2B integration passed");
        }
    }
}
