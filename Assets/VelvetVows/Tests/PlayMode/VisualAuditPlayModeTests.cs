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
    public sealed class VisualAuditPlayModeTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator CaptureWorldAndCheckMaterials()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var camera = Camera.main;
            Assert.NotNull(camera);
            var brain = camera.GetComponents<MonoBehaviour>().FirstOrDefault(x => x.GetType().Name == "CinemachineBrain");
            if (brain != null) brain.enabled = false;

            Capture(camera, "01_first_meeting", new Vector3(1.35f, 1.75f, -7.9f),
                new Vector3(0, .9f, -5.3f), 41);
            Capture(camera, "02_studio_inside", new Vector3(-15.5f, 1.75f, -7.1f),
                new Vector3(-17f, 1.1f, -3f), 65);
            Identify(camera, "yellow floor prop", .20f, .14f);
            Identify(camera, "white floor prop", .33f, .49f);
            Capture(camera, "03_studio_door", new Vector3(-9.5f, 1.7f, -3f),
                new Vector3(-13f, 1.2f, -3f), 62);
            Identify(camera, "brown door slab", .35f, .5f);

            var components = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var game = components.First(x => x.GetType().Name == "GameManager");
            var ball = components.First(x => x.GetType().Name == "Phase2CBallDirector");
            game.GetType().GetMethod("EndPhase2B", Flags).Invoke(game, null);
            ball.GetType().GetMethod("BeginBall", Flags).Invoke(ball, null);
            yield return new WaitForSeconds(1.5f);
            if (brain != null) brain.enabled = false;
            Capture(camera, "04_ballroom", new Vector3(20.1f, 3.3f, -6.4f),
                new Vector3(20.1f, 1.3f, 0), 52);

            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int missing = 0, unsupported = 0;
            foreach (var renderer in renderers.Where(r => r.gameObject.scene.IsValid()))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        missing++;
                        Debug.LogWarning("[VisualAudit] Missing material: " + PathOf(renderer.transform));
                    }
                    else if (material.shader == null || !material.shader.isSupported ||
                        material.shader.name == "Hidden/InternalErrorShader")
                    {
                        unsupported++;
                        Debug.LogWarning("[VisualAudit] Unsupported shader: " +
                            PathOf(renderer.transform) + " / " + material.name);
                    }
                }
            }
            Debug.Log($"[VisualAudit] Scene renderers={renderers.Length}, missing materials={missing}, unsupported shaders={unsupported}");
            Assert.AreEqual(0, missing, "Scene has renderers with missing materials");
            Assert.AreEqual(0, unsupported, "Scene has renderers with unsupported shaders");
        }

        private static string PathOf(Transform transform) => transform.parent == null ? transform.name :
            PathOf(transform.parent) + "/" + transform.name;

        private static void Identify(Camera camera, string label, float x, float y)
        {
            var hits = Physics.RaycastAll(camera.ViewportPointToRay(new Vector3(x, y)), 30f)
                .OrderBy(hit => hit.distance).Take(5)
                .Select(hit => PathOf(hit.transform) + " @" + hit.distance.ToString("F2"));
            Debug.Log("[VisualAudit] " + label + ": " + string.Join(" | ", hits));
        }

        private static void Capture(Camera camera, string name, Vector3 position, Vector3 lookAt, float fov)
        {
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
            camera.fieldOfView = fov;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../VisualAudit_Captures"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.Destroy(target);
            Object.Destroy(texture);
        }
    }
}
