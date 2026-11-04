using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;

namespace VelvetVows.Tests
{
    public sealed class Phase3E1PlayModeTests
    {
        private const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string name) =>
            Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == name);
        private static object Field(object actor, string name) => actor.GetType().GetField(name, Flags).GetValue(actor);
        private static object Value(object actor, string name) => actor.GetType().GetProperty(name, Flags).GetValue(actor);
        private static IEnumerator CleanupHelper(string name) =>
            (IEnumerator)typeof(Phase2BPlayModeTests).GetMethod(name, BindingFlags.Static | Flags).Invoke(null, null);
        private static Bounds RenderBounds(Transform target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        private static IEnumerator Capture(string name)
        {
            if (Application.isBatchMode) yield break;
            var captures = Path.GetFullPath(Path.Combine(Application.dataPath, "../Phase3E1_Captures"));
            Directory.CreateDirectory(captures);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(captures, name + ".png"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LibraryEntranceAndCelesteDiagnostic()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var hud = Component("GameHUD");
            hud.GetType().GetMethod("ShowStart", Flags).Invoke(hud, new object[] { false });
            var camera = Camera.main;
            foreach (var behaviour in camera.GetComponents<Behaviour>())
                if (behaviour.GetType().Name == "CinemachineBrain") behaviour.enabled = false;
            camera.transform.position = new Vector3(-4.2f, 1.75f, 3.2f);
            camera.transform.LookAt(new Vector3(-8f, 1.6f, 6f));
            camera.fieldOfView = 55;
            yield return Capture("01_library_entrance");
            foreach (var point in new[] { new Vector2(-7.5f, 8), new Vector2(-9, 8),
                new Vector2(-10, 8), new Vector2(-10, 10), new Vector2(-11, 10),
                new Vector2(-12, 10), new Vector2(-12, 12) })
            {
                var hits = Physics.RaycastAll(new Vector3(point.x, 2.5f, point.y), Vector3.down, 4);
                var ground = hits.Where(h => h.point.y < .3f && !h.collider.isTrigger)
                    .OrderByDescending(h => h.point.y).FirstOrDefault();
                Debug.Log($"[Phase3E1 Diagnostic] ground {point}: {ground.collider?.name ?? "NONE"} at {ground.point.y:F2}");
            }
            var story = Component("Phase2AStoryManager");
            var celeste = (Transform)Field(story, "celeste");
            var animator = celeste.GetComponentInChildren<Animator>();
            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Debug.Log($"[Phase3E1 Diagnostic] Celeste root={celeste.position} leftFoot={leftFoot.position} rightFoot={rightFoot.position} controller={celeste.GetComponent<CharacterController>().center}/{celeste.GetComponent<CharacterController>().height}");
            Assert.That(leftFoot.position.y, Is.InRange(-.02f, .05f));
            Assert.That(rightFoot.position.y, Is.InRange(-.02f, .05f));
            camera.transform.position = new Vector3(-3.4f, 1.65f, 1.0f);
            camera.transform.LookAt(celeste.position + Vector3.up * 1.0f);
            yield return Capture("03_celeste_on_floor");
            camera.transform.position = new Vector3(-4.2f, 1.75f, 3.2f);
            camera.transform.LookAt(new Vector3(-8f, 1.6f, 6f));

            var game = Component("GameManager");
            var state = System.Enum.Parse(game.GetType().Assembly.GetType("VelvetVows.GameState"), "Phase2A");
            game.GetType().GetMethod("ChangeState", Flags).Invoke(game, new[] { state, "Visit the Library." });
            game.GetType().GetMethod("SetPhase2AControl", Flags).Invoke(game, new object[] { true });
            var player = (Transform)Value(game, "Player");
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.SetPositionAndRotation(new Vector3(-6f, .02f, 5f), Quaternion.Euler(0, 270, 0));
            controller.enabled = true;
            var inputs = player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "StarterAssetsInputs");
            player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "AcademyPlayerInputRelay").enabled = false;
            foreach (var waypoint in new[] { new Vector3(-7.2f, 0, 5.5f), new Vector3(-9f, 0, 5.5f),
                new Vector3(-9f, 0, 9.5f), new Vector3(-11.5f, 0, 10f),
                new Vector3(-14f, 0, 10f), new Vector3(-11.5f, 0, 10f),
                new Vector3(-9f, 0, 9.5f), new Vector3(-9f, 0, 5.5f), new Vector3(-7.2f, 0, 5.5f),
                new Vector3(-6f, 0, 5f) })
                yield return Walk(player, inputs, waypoint);
        }

        [UnityTest]
        public IEnumerator LibraryBooksRestOnTablesAndHeldBookHasHumanScale()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var root = SceneManager.GetActiveScene().GetRootGameObjects().First(g => g.name == "Velvet Vows Main").transform;
            var composition = root.Find("ENVIRONMENT/Visual Polish 2.1/Library reading composition");
            Assert.NotNull(composition);
            int count = 0;
            foreach (Transform child in composition)
            {
                if (child.name != "Open academy book") continue;
                float surface = child.position.x < -21.5f ? .54f : .65f;
                Assert.That(RenderBounds(child).min.y, Is.EqualTo(surface).Within(.025f), child.name);
                count++;
            }
            Assert.AreEqual(4, count);
            var story = Component("Phase2AStoryManager");
            var returned = (GameObject)Field(story, "libraryBooks");
            var sketchbook = (GameObject)Field(story, "sketchbook");
            Assert.That(RenderBounds(returned.transform).min.y, Is.EqualTo(.54f).Within(.025f));
            Assert.That(RenderBounds(sketchbook.transform).min.y, Is.EqualTo(.54f).Within(.025f));
            var held = (GameObject)Field(story, "adrianHeldBook");
            Assert.Less(RenderBounds(held.transform).size.magnitude, .5f);
            var hall = root.Find("ENVIRONMENT/Visual Polish 2.1/Grand Hall composition");
            foreach (float x in new[] { -5.6f, 5.6f })
            {
                var table = hall.Cast<Transform>().First(t => t.name == "Portrait side table" && Mathf.Abs(t.position.x - x) < .2f);
                var vase = hall.Cast<Transform>().First(t => t.name == "Academy flowers" && Mathf.Abs(t.position.x - x) < .2f);
                Assert.That(RenderBounds(vase).min.y, Is.EqualTo(RenderBounds(table).max.y).Within(.025f));
            }
            var hud = Component("GameHUD");
            hud.GetType().GetMethod("ShowStart", Flags).Invoke(hud, new object[] { false });
            var camera = Camera.main;
            foreach (var behaviour in camera.GetComponents<Behaviour>())
                if (behaviour.GetType().Name == "CinemachineBrain") behaviour.enabled = false;
            camera.transform.position = new Vector3(-19.5f, 1.65f, 15.2f);
            camera.transform.LookAt(new Vector3(-21.3f, .75f, 18.4f));
            camera.fieldOfView = 54;
            yield return Capture("02_library_books_on_tables");
        }

        [UnityTest]
        public IEnumerator HidingBodyUsesFadeWithoutMovingCorpse()
        {
            yield return CleanupHelper("StartCleanup");
            var cleanup = Component("Phase2BCleanupManager");
            var murder = Component("Phase2BMurderSequence");
            var celeste = (Transform)Field(cleanup, "celeste");
            var corpsePosition = celeste.position;
            yield return CleanupHelper("PressE");
            float until = Time.time + 6f;
            while (!(bool)Value(cleanup, "BodyHidden") && Time.time < until) yield return null;
            Assert.IsTrue((bool)Value(cleanup, "BodyHidden"));
            while ((float)Value(Component("GameHUD"), "FadeAlpha") > .01f && Time.time < until) yield return null;
            Assert.Less(Vector3.Distance(celeste.position, corpsePosition), .015f);
            Assert.IsFalse(((GameObject)Field(murder, "bodyInteraction")).activeInHierarchy);
            Assert.AreEqual("Phase2BCleanup", Value(Component("GameManager"), "State").ToString());
            Assert.Greater((float)Value(cleanup, "Remaining"), 100f);
            var wardrobe = (Transform)Field(cleanup, "wardrobe");
            var camera = Camera.main;
            foreach (var behaviour in camera.GetComponents<Behaviour>())
                if (behaviour.GetType().Name == "CinemachineBrain") behaviour.enabled = false;
            camera.transform.position = wardrobe.position + new Vector3(1.7f, 1.65f, 2.0f);
            camera.transform.LookAt(wardrobe.position + Vector3.up * 1.0f);
            yield return Capture("04_body_hidden_after_fade");
        }

        [UnityTest]
        public IEnumerator AdrianExitsClearOfSofaAndDialogueStopsWalking()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var story = Component("Phase2AStoryManager");
            var adrian = (Transform)Field(story, "adrian");
            var hud = Component("GameHUD");
            var game = Component("GameManager");
            game.GetType().GetMethod("Begin", Flags).Invoke(game, null);
            float until = Time.time + 36f;
            bool sawExit = false;
            while (Time.time < until)
            {
                if (adrian.position.x < -3.5f && adrian.position.z < -5.5f) sawExit = true;
                if (adrian.position.x < -4f && adrian.position.z > -5.25f &&
                    adrian.position.z < -3.2f && adrian.position.x > -6.5f)
                    Assert.Fail("Adrian entered the waiting sofa area at " + adrian.position);
                if (sawExit && adrian.position.x < -4.5f && adrian.position.z < -5.8f) break;
                yield return null;
            }
            Assert.IsTrue(sawExit, "Adrian did not reach the clear south aisle");

            var player = (Transform)Value(game, "Player");
            var inputs = player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "StarterAssetsInputs");
            var animation = player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "AcademyCharacterAnimation");
            game.GetType().GetMethod("SetPhase2AControl", Flags).Invoke(game, new object[] { true });
            inputs.GetType().GetMethod("MoveInput", Flags).Invoke(inputs, new object[] { Vector2.up });
            game.GetType().GetMethod("SetDialogueControl", Flags).Invoke(game, new object[] { true });
            yield return null;
            Assert.IsTrue((bool)Field(animation, "cinematicIdle"));
            Assert.AreEqual(Vector2.zero, (Vector2)Field(inputs, "move"));
            var animator = player.GetComponentInChildren<Animator>();
            Assert.Less(animator.GetFloat("Speed"), .05f);
            game.GetType().GetMethod("SetDialogueControl", Flags).Invoke(game, new object[] { false });
            Assert.IsFalse((bool)Field(animation, "cinematicIdle"));
        }

        private static IEnumerator Walk(Transform player, MonoBehaviour inputs, Vector3 target)
        {
            float end = Time.time + 9f;
            while (Vector2.Distance(new Vector2(player.position.x, player.position.z),
                new Vector2(target.x, target.z)) > .55f && Time.time < end)
            {
                var delta = target - player.position; delta.y = 0;
                var right = Camera.main.transform.right; right.y = 0; right.Normalize();
                var forward = Camera.main.transform.forward; forward.y = 0; forward.Normalize();
                inputs.GetType().GetMethod("MoveInput", Flags).Invoke(inputs, new object[] {
                    new Vector2(Vector3.Dot(delta.normalized, right), Vector3.Dot(delta.normalized, forward)) });
                Assert.Greater(player.position.y, -.35f, "Player fell below the Library path");
                yield return null;
            }
            inputs.GetType().GetMethod("MoveInput", Flags).Invoke(inputs, new object[] { Vector2.zero });
            if (Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(target.x, target.z)) >= .7f)
                Debug.Log("[Phase3E1 Navigation] blockers: " + string.Join(", ",
                    Physics.OverlapSphere(player.position + Vector3.up * .8f, .75f,
                        ~0, QueryTriggerInteraction.Ignore).Select(c => c.name)));
            Assert.Less(Vector2.Distance(new Vector2(player.position.x, player.position.z),
                new Vector2(target.x, target.z)), .7f, "Blocked near " + target + " at " + player.position);
        }
    }
}
