using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VelvetVows.Tests
{
    public sealed class Phase3CPlayModeTests
    {
        private const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string name) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == name);
        private static object Field(object actor, string name) => actor.GetType().GetField(name, Flags).GetValue(actor);
        private static object Value(object actor, string name) => actor.GetType().GetProperty(name, Flags).GetValue(actor);
        private static void Call(object actor, string name, params object[] args) =>
            actor.GetType().GetMethod(name, Flags).Invoke(actor, args);
        private static IEnumerator CleanupHelper(string name, params object[] args) =>
            (IEnumerator)typeof(Phase2BPlayModeTests).GetMethod(name, BindingFlags.Static | Flags).Invoke(null, args);

        [UnityTest]
        public IEnumerator ImportedClipsMapToTheCorrectSpeakersAndMusicContinues()
        {
            EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var voice = Component("CharacterVoiceManager");
            var dialogue = Component("DialogueManager");
            string[] fields = { "evelynCalm", "evelynNice", "evelynShy", "evelynScared", "evelynPostKillLaugh",
                "adrianCalm", "adrianSoft", "adrianQuestion", "celesteFriendly", "celesteIrritated", "celesteScared",
                "isabelleOrdinary", "isabelleWorried", "prefectCalm", "prefectPoint", "murderImpact", "cleaning" };
            foreach (string field in fields) Assert.NotNull(Field(voice, field), "Missing imported MP3: " + field);
            var music = Component("MusicManager");
            Call(music, "PlayExploration");
            yield return new WaitForSeconds(2.3f);
            var musicSource = (AudioSource)Field(music, "source");
            Assert.IsTrue(musicSource.isPlaying);

            void Expect(string speaker, string line, string clip)
            {
                Call(dialogue, "ShowCinematicLine", speaker, line);
                Assert.AreEqual(clip, ((AudioClip)Value(voice, "LastVoiceClip")).name);
                Assert.IsTrue((bool)Value(voice, "VoicePlaying"), speaker + " did not play");
            }
            Expect("EVELYN", "Y-yes. Thank you.", "evelyn_shy");
            Expect("ADRIAN", "Are you alright?", "adrian_question");
            Expect("ADRIAN", "Be careful, Evelyn.", "adryan_soft");
            Expect("CELESTE", "You found it! Thank you, Evelyn.", "selesty_friendly");
            var story = Component("Phase2AStoryManager");
            story.GetType().GetField("<Stage>k__BackingField", Flags).SetValue(story,
                Enum.Parse(story.GetType().Assembly.GetType("VelvetVows.Phase2AStage"), "Confront"));
            Expect("CELESTE", "Evelyn, you're scaring me.", "selesty_scared");
            Expect("ISABELLE", "Did you hear? Another student disappeared.", "isabelle_worried");
            Expect("PREFECT", "Evelyn… what have you done?", "prefect_point");
            Call(dialogue, "ShowCinematicLine", "AMELIE", "I heard something.");
            Assert.AreEqual("npc1", ((AudioClip)Value(voice, "LastVoiceClip")).name);
            Assert.IsTrue((bool)Value(voice, "VoicePlaying"), "Amelie must use her assigned voice");
            Assert.IsTrue(musicSource.isPlaying, "Dialogue interrupted background music");
            Call(dialogue, "HideCinematicLine");
            Assert.IsFalse((bool)Value(voice, "VoicePlaying"));

            var ball = Component("Phase2CBallDirector");
            Call(ball, "PlayAgain");
            yield return null; yield return null;
            var fresh = Component("CharacterVoiceManager");
            Assert.AreNotSame(voice, fresh);
            Assert.AreEqual(0, (int)Value(fresh, "ImpactCount"));
            Assert.IsFalse((bool)Value(fresh, "VoicePlaying"));
        }

        [UnityTest]
        public IEnumerator MurderImpactAndBriefPostKillLaughTriggerOnce()
        {
            yield return CleanupHelper("StartCleanup");
            var voice = Component("CharacterVoiceManager");
            Assert.AreEqual(1, (int)Value(voice, "ImpactCount"));
            Assert.AreEqual(1, (int)Value(voice, "PostKillLaughCount"));
            Assert.AreEqual("sound_of_killing", ((AudioClip)Value(voice, "LastSfxClip")).name);
            Assert.IsFalse((bool)Value(voice, "VoicePlaying"), "Laugh should be stopped before cleanup starts");
        }

        [UnityTest]
        public IEnumerator BloodCleaningPlaysEffectAndFailureRetryClearsAudio()
        {
            yield return CleanupHelper("StartCleanup");
            var cleanup = Component("Phase2BCleanupManager");
            var voice = Component("CharacterVoiceManager");
            yield return CleanupHelper("PressE");
            float until = Time.time + 12f;
            while (!(bool)Value(cleanup, "BodyHidden") && Time.time < until) yield return null;
            Assert.IsTrue((bool)Value(cleanup, "BodyHidden"));
            Assert.AreEqual(1, (int)Value(voice, "CleanupCount"), "Body hiding should play the existing cleanup sound once");
            yield return CleanupHelper("WalkTo", new Vector3(-18f, 0, -5.3f), 16f);
            yield return CleanupHelper("PressE");
            yield return CleanupHelper("WalkTo", new Vector3(-17.2f, 0, -3.1f), 16f);
            yield return CleanupHelper("PressE");
            Assert.AreEqual(2, (int)Value(voice, "CleanupCount"), "Blood cleaning should play the sound once more");
            Assert.AreEqual("cleaning_after_kill", ((AudioClip)Value(voice, "LastSfxClip")).name);
            until = Time.time + 7f;
            while (!(bool)Value(cleanup, "BloodCleaned") && Time.time < until) yield return null;
            Assert.IsTrue((bool)Value(cleanup, "BloodCleaned"));
            Assert.IsFalse((bool)Value(voice, "SfxPlaying"), "19-second clip must stop with the cleaning action");
            Call(voice, "PlayCleaning");
            Assert.IsTrue((bool)Value(voice, "SfxPlaying"));
            Call(cleanup, "Fail", "Audio retry test");
            Assert.IsFalse((bool)Value(voice, "SfxPlaying"));
            yield return new WaitForSeconds(3f);
            Call(cleanup, "Retry");
            Assert.IsFalse((bool)Value(voice, "VoicePlaying"));
            Assert.IsFalse((bool)Value(voice, "SfxPlaying"));
            Assert.AreEqual("Phase2BCleanup", Value(Component("GameManager"), "State").ToString());
        }
    }
}
