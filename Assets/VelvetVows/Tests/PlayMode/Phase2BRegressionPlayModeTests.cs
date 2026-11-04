using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace VelvetVows.Tests
{
    public sealed class Phase2BRegressionPlayModeTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string type) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == type);
        private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        private static object Value(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);

        [UnityTest]
        public IEnumerator LovesickAndBlackRoseStillWorkAfterPhase2BInstall()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var game = Component("GameManager");
            var story = Component("Phase2AStoryManager");
            var lovesick = Component("Phase2ALovesick");
            var roses = Component("BlackRoseSpawner");
            game.GetType().GetMethod("ChangeState", Flags).Invoke(game, new[] { Enum.Parse(game.GetType().Assembly.GetType("VelvetVows.GameState"), "Phase2A"), "" });
            var player = (Transform)Field(story, "player");
            var adrian = (Transform)Field(story, "adrian");
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.position = adrian.position + new Vector3(1.5f, 0, 0);
            controller.enabled = true;
            yield return new WaitForSeconds(.65f);
            var tint = (Image)Field(lovesick, "tint");
            Assert.Greater(tint.color.a, .001f, "Lovesick tint no longer responds to Adrian proximity");
            Assert.IsFalse((bool)Value(lovesick, "Suppressed"));

            var spawnPoints = (System.Collections.IList)Value(roses, "SpawnPoints");
            Assert.Greater(spawnPoints.Count, 0);
            var point = (Transform)spawnPoints[0];
            var rose = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(x => x.GetType().Name == "BlackRoseCollectible" && Vector3.Distance(x.transform.position, point.position) < 1f);
            Assert.NotNull(rose, "Spawned Black Rose was not found");
            int before = (int)Value(game, "RoseCount");
            var playerCollider = player.GetComponent<Collider>();
            rose.GetType().GetMethod("OnTriggerEnter", Flags).Invoke(rose, new object[] { playerCollider });
            yield return null;
            Assert.AreEqual(before + 1, (int)Value(game, "RoseCount"), "Black Rose collection stopped updating the HUD/game state");
            Debug.Log("[Phase2B Regression] Lovesick proximity and spawned Black Rose collection passed");
        }
    }
}
