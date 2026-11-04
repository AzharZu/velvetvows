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
    public sealed class AnimationCompatibilityPlayModeTests
    {
        [UnityTest]
        public IEnumerator RunActualSceneAndCaptureEverySequence()
        {
            EditorSceneManager.LoadSceneInPlayMode(
                "Assets/VelvetVows/Scenes/VelvetVows_AnimationTest.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var root = GameObject.Find("ANIMATION TEST - isolated from main gameplay");
            Assert.NotNull(root, "Animation test scene did not load");
            var director = root.GetComponents<MonoBehaviour>().FirstOrDefault(x => x.GetType().Name == "AnimationTestDirector");
            Assert.NotNull(director);
            var type = director.GetType();
            AnimationClip Clip(string name) => (AnimationClip)type.GetField(name).GetValue(director);
            AnimationClip[] Clips(string name) => (AnimationClip[])type.GetField(name).GetValue(director);
            void Select(int i) => type.GetMethod("Select").Invoke(director, new object[] { i });
            Assert.IsTrue(Clip("attack").humanMotion);
            Assert.IsTrue(Clip("damage").humanMotion);
            Assert.IsTrue(Clip("death").humanMotion);
            foreach (var clip in Clips("femaleSteps")) Assert.IsTrue(clip.humanMotion);
            foreach (var clip in Clips("maleSteps")) Assert.IsTrue(clip.humanMotion);
            type.GetField("autoRun").SetValue(director, false);
            Select(0);
            yield return new WaitForSeconds(.44f);
            yield return Capture("00a_Attack.png");
            yield return new WaitForSeconds(.42f);
            yield return Capture("00b_Reaction.png");
            yield return new WaitForSeconds(Clip("attack").length * .34f + Clip("damage").length * .76f + Clip("death").length - .86f + .3f);
            var celeste = (GameObject)type.GetField("attackCeleste").GetValue(director);
            var pos = celeste.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.Less(Vector3.Distance(pos, celeste.transform.position), .001f, "Death pose/root drifted after clip ended");
            Assert.IsTrue(celeste.activeInHierarchy);
            float bottom = float.PositiveInfinity;
            foreach (var renderer in celeste.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var baked = new Mesh();
                renderer.BakeMesh(baked);
                var matrix = renderer.transform.localToWorldMatrix;
                foreach (var vertex in baked.vertices) bottom = Mathf.Min(bottom, matrix.MultiplyPoint3x4(vertex).y);
                Object.Destroy(baked);
            }
            Assert.That(bottom, Is.InRange(-.03f, .08f), "Death mesh is not aligned with floor");
            yield return Capture("00_AttackDeath.png");
            for (int i = 1; i <= 8; i++)
            {
                Select(i);
                if (i <= 6)
                {
                    yield return new WaitForSeconds(.65f);
                    yield return Capture($"{i:00}_Dance{i:00}_A.png");
                    yield return new WaitForSeconds(1.55f);
                    yield return Capture($"{i:00}_Dance{i:00}_B.png");
                    yield return new WaitForSeconds(.8f);
                }
                else
                {
                    int pose = i - 7;
                    var femaleBegin = Clips("femalePoseBegin")[pose];
                    var maleBegin = Clips("malePoseBegin")[pose];
                    yield return new WaitForSeconds(.8f);
                    yield return Capture($"{i:00}_Pose{pose + 1:00}_Begin.png");
                    yield return new WaitForSeconds(Mathf.Max(femaleBegin.length, maleBegin.length) - .8f + .5f);
                    yield return Capture($"{i:00}_Pose{pose + 1:00}_Loop.png");
                    yield return new WaitForSeconds(3.9f);
                    yield return Capture($"{i:00}_Pose{pose + 1:00}_Stop.png");
                }
            }
            Debug.Log("[AnimationTest] Play Mode sequence passed and screenshots captured");
        }

        private static IEnumerator Capture(string name)
        {
            yield return null;
            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "AnimationTestCaptures");
            Directory.CreateDirectory(folder);
            var camera = Camera.main;
            Assert.NotNull(camera);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(folder, name), texture.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.Destroy(target);
            Object.Destroy(texture);
            yield return null;
        }
    }
}
