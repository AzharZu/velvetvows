using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace VelvetVows.Tests
{
    public sealed class Phase3HRepairPlayModeTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Get(string name) => Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,
            FindObjectsSortMode.None).First(x => x.GetType().Name == name);
        private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);

        [UnityTest, Timeout(180000)]
        public IEnumerator RepairedCamerasLayoutVoicesAndJumpBoundaries()
        {
            var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            view.Show(); view.Focus();
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale = 1; Application.runInBackground = true;
            yield return new WaitForSeconds(1);
            var story = Get("Phase2AStoryManager"); var game = Get("GameManager");
            var hud = Get("GameHUD"); var dialogue = Get("DialogueManager");
            var player = (Transform)Field(story, "player");
            var brain = Camera.main.GetComponents<Behaviour>().First(b => b.GetType().Name == "CinemachineBrain");
            brain.enabled = false;
            Call(hud, "ShowStart", false); Call(hud, "ShowGameplay", true);

            var fill = (Image)Field(hud, "suspicionFill");
            Call(hud, "SetSuspicion", 0f); yield return null; Canvas.ForceUpdateCanvases();
            var mesh = fill.canvasRenderer.GetMesh();
            Assert.IsTrue(mesh == null || mesh.vertexCount == 0, "At 0% suspicion the red fill must disappear");
            Call(hud, "SetSuspicion", 50f); yield return null; Canvas.ForceUpdateCanvases();
            mesh = fill.canvasRenderer.GetMesh(); float halfWidth = mesh.bounds.size.x;
            Call(hud, "SetSuspicion", 100f); yield return null; Canvas.ForceUpdateCanvases();
            mesh = fill.canvasRenderer.GetMesh();
            Assert.That(halfWidth / mesh.bounds.size.x, Is.EqualTo(.5f).Within(.02f));
            Call(hud, "SetSuspicion", 0f);

            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                string path = PathOf(renderer.transform);
                if (path.Contains("/ENVIRONMENT/") || path.Contains("/WORLD/GOTHIC_ACADEMY/"))
                    Assert.IsFalse(renderer.sharedMaterials.Any(m => m != null && AssetDatabase.GetAssetPath(m).Contains("/Paintings/")),
                        "Decorative picture remains visible at " + path);
            }

            Call(story, "PlacePlayerBehindCover", new Vector3(-6.8f, .02f, 6.8f), Quaternion.Euler(0, 135, 0));
            Call(story, "FrameReaction");
            Debug.Log("[Phase3H] Reaction camera=" + Camera.main.transform.position + "; Evelyn=" + player.position);
            var target = player.position + Vector3.up * 1.35f;
            Assert.IsTrue((bool)Call(story, "CameraPathClear", Camera.main.transform.position, target),
                "The reaction camera still hits architecture");
            var face = Camera.main.WorldToViewportPoint(target);
            Assert.That(face.x, Is.InRange(.15f, .85f)); Assert.That(face.y, Is.InRange(.15f, .85f));
            Call(dialogue, "ShowAdvanceableCinematicLine", "EVELYN · THOUGHT", "Girls like her have always had everything.");
            yield return Capture("01_reaction_camera");
            Call(dialogue, "HideCinematicLine");

            var witness = (Transform)Field(story, "witness");
            var table = GameObject.Find("Painting worktable two");
            var rs = table.GetComponentsInChildren<Renderer>(); var bounds = rs[0].bounds;
            foreach (var r in rs.Skip(1)) bounds.Encapsulate(r.bounds);
            Assert.Greater(Vector3.Distance(bounds.ClosestPoint(witness.position + Vector3.up * .8f), witness.position + Vector3.up * .8f), .5f,
                "Margot intersects the worktable");
            Frame(new Vector3(-15.5f, 1.75f, -7.1f), new Vector3(-17f, 1.1f, -3f), 65);
            yield return Capture("02_studio_layout");
            Frame(new Vector3(-17, 2.7f, 10), new Vector3(-21, 1.2f, 19), 62);
            yield return Capture("03_library_layout");

            var prefect = Get("Phase2BPrefectInvestigation");
            Assert.IsFalse(prefect.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials)
                .Any(m => m != null && m.shader.name == "VelvetVows/Prefect Ivory Uniform"));
            var head = prefect.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
            Assert.That(head.localScale.x, Is.InRange(.80f, .88f));
            var badge = prefect.GetComponentsInChildren<Renderer>().First(r => r.name == "Prefect brass insignia");
            Assert.Less(badge.bounds.size.magnitude, .12f, "The imported skeleton scale enlarged the Prefect badge");
            Frame(prefect.transform.position + new Vector3(-1.3f, 1.6f, -2.4f), prefect.transform.position + Vector3.up * 1.15f, 42);
            yield return Capture("04_prefect");

            foreach (float height in new[] { 1.2f, 3f })
            {
                foreach (float x in new[] { -24f, -21f, -18f, -15f, -12f })
                {
                    Assert.IsTrue(Physics.Raycast(new Vector3(x, height, 9), Vector3.back, 1.5f, ~0, QueryTriggerInteraction.Ignore), "South Library escape at " + x);
                    Assert.IsTrue(Physics.Raycast(new Vector3(x, height, 23), Vector3.forward, 1.5f, ~0, QueryTriggerInteraction.Ignore), "North Library escape at " + x);
                }
                Assert.IsTrue(Physics.Raycast(new Vector3(-11, height, 20), Vector3.right, 1.5f, ~0, QueryTriggerInteraction.Ignore));
                Assert.IsTrue(Physics.Raycast(new Vector3(-25, height, 10), Vector3.left, 1.5f, ~0, QueryTriggerInteraction.Ignore));
            }
            Assert.IsFalse(Physics.Raycast(new Vector3(-11.1f, 1.1f, 10), Vector3.right, 2.2f, ~0, QueryTriggerInteraction.Ignore), "Library entrance was accidentally sealed");
            Assert.IsFalse(Physics.Raycast(new Vector3(-8.8f, 1.1f, 6), Vector3.right, 1.6f, ~0, QueryTriggerInteraction.Ignore), "Hall entrance was accidentally sealed");

            Call(game, "BeginPhase2BCleanup");
            Move(player, new Vector3(-7f, .04f, 6.4f));
            var findRoute = typeof(Phase2APlayModeTests).GetMethod("FindRoute", Flags);
            Assert.NotNull(findRoute.Invoke(null, new object[] { player.position, new Vector3(-20,0,14) }), "Hall to Library route");
            Assert.NotNull(findRoute.Invoke(null, new object[] { new Vector3(-20,0,14), new Vector3(-20,0,-3) }), "Library to Studio route");
            Assert.NotNull(findRoute.Invoke(null, new object[] { new Vector3(-20,0,14), new Vector3(-34,0,14) }), "Library to Music Wing route");
            yield return (IEnumerator)typeof(Phase2APlayModeTests).GetMethod("WalkRoute", Flags)
                .Invoke(null, new object[] { player, new Vector3(-14.6f,0,13.15f), (Func<bool>)(() => false) });
            yield return (IEnumerator)typeof(Phase2APlayModeTests).GetMethod("WalkRoute", Flags)
                .Invoke(null, new object[] { player, new Vector3(-7f,0,6.4f), (Func<bool>)(() => false) });
            var relay = player.GetComponents<MonoBehaviour>().First(b => b.GetType().Name == "AcademyPlayerInputRelay"); relay.enabled = false;
            var inputs = player.GetComponents<MonoBehaviour>().First(b => b.GetType().Name == "StarterAssetsInputs");
            yield return JumpAt(player, inputs, new Vector3(-16, .04f, 9), Vector3.back, () => player.position.z > 8.2f, "Library south perimeter");
            yield return JumpAt(player, inputs, new Vector3(-25, .04f, 10.2f), Vector3.left, () => player.position.x > -25.8f, "Library window");
            yield return JumpAt(player, inputs, new Vector3(-10.5f, .04f, 4.9f), Vector3.back, () => player.position.z > 4.2f, "Library vestibule");

            var voice = Get("CharacterVoiceManager");
            Call(voice, "StopAll"); Call(voice, "PlayDialogue", "CELESTE", "You found it! Thank you, Evelyn.");
            yield return new WaitForSeconds(.15f);
            var source = (AudioSource)Field(voice, "voiceSource");
            Assert.AreEqual(1f, source.pitch); Assert.IsTrue(source.isPlaying);
            Call(voice, "PlayDialogue", "CELESTE", "Of course.");
            yield return new WaitForSeconds(.18f);
            Assert.IsFalse(source.isPlaying, "The same Celeste cue immediately repeated");
            Call(voice, "PlayDialogue", "EVELYN · THOUGHT", "He knows.");
            Assert.IsFalse(source.isPlaying, "Short voice chirps overlap internal narration");
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials)
                    if (material != null) Assert.IsFalse(ShaderUtil.ShaderHasError(material.shader), material.shader.name);
            Debug.Log("[Phase3H] Camera, actual HUD mesh, no paintings, layout, appearance, three physical jumps and voice cues passed.");
        }

        private static IEnumerator JumpAt(Transform player, MonoBehaviour inputs, Vector3 start, Vector3 direction, Func<bool> contained, string label)
        {
            Call(inputs, "MoveInput", Vector2.zero); Call(inputs, "JumpInput", false);
            Move(player, start); yield return new WaitForSeconds(.7f);
            float begun = Time.time, highest = player.position.y;
            while (Time.time - begun < 2.2f)
            {
                var right = Camera.main.transform.right; right.y = 0; right.Normalize();
                var forward = Camera.main.transform.forward; forward.y = 0; forward.Normalize();
                Call(inputs, "MoveInput", new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)));
                Call(inputs, "JumpInput", Time.time - begun < .2f);
                highest = Mathf.Max(highest, player.position.y);
                Assert.IsTrue(contained(), "Player escaped at " + label + ": " + player.position);
                yield return null;
            }
            Call(inputs, "MoveInput", Vector2.zero); Call(inputs, "JumpInput", false);
            Assert.Greater(highest, .3f, "The jump input was not exercised at " + label);
            Debug.Log("[Phase3H] Actual jump blocked: " + label + ", height=" + highest + ", final=" + player.position);
        }
        private static void Move(Transform t, Vector3 position)
        { var cc = t.GetComponent<CharacterController>(); cc.enabled = false; t.position = position; cc.enabled = true; Physics.SyncTransforms(); }
        private static void Frame(Vector3 position, Vector3 target, float fov)
        { Camera.main.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position)); Camera.main.fieldOfView = fov; }
        private static IEnumerator Capture(string name)
        {
            if (Application.isBatchMode) yield break;
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.GetFullPath("Phase3H_Captures/" + name + ".jpg"), texture.EncodeToJPG(88));
            Object.Destroy(texture);
        }
        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }
}
