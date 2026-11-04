using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VelvetVows.Editor
{
    public static class Phase3CInstaller
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string BackupPath = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3C.unity";
        private const string Voices = "Assets/VelvetVows/Audio/Voices/";

        [MenuItem("Velvet Vows/Install Phase 3C Voices and Effects")]
        public static void Install()
        {
            if (!File.Exists(BackupPath) || !SameHash(BackupPath, ScenePath))
                throw new Exception("Phase 3C backup must match the main scene before installation");
            var scene = EditorSceneManager.OpenScene(ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var dialogue = UnityEngine.Object.FindFirstObjectByType<DialogueManager>();
            var murder = UnityEngine.Object.FindFirstObjectByType<Phase2BMurderSequence>();
            var cleanup = UnityEngine.Object.FindFirstObjectByType<Phase2BCleanupManager>();
            var ball = UnityEngine.Object.FindFirstObjectByType<Phase2CBallDirector>();
            var mystery = UnityEngine.Object.FindFirstObjectByType<Phase3BMissingStudents>();
            if (game == null || dialogue == null || murder == null || cleanup == null || ball == null || mystery == null ||
                UnityEngine.Object.FindFirstObjectByType<CharacterVoiceManager>() != null)
                throw new Exception("Accepted scene systems missing or Phase 3C already installed");
            var go = new GameObject("PHASE 3C CHARACTER VOICES AND EFFECTS");
            go.transform.SetParent(game.transform, false);
            var voice = go.AddComponent<CharacterVoiceManager>();
            voice.evelynCalm = Clip("evelyn_calm.mp3");
            voice.evelynNice = Clip("evelyn_nice.mp3");
            voice.evelynShy = Clip("evelyn_shy.mp3");
            voice.evelynScared = Clip("evelyn_scared.mp3");
            voice.evelynPostKillLaugh = Clip("evelyn_laugh_after_kill.mp3");
            voice.adrianCalm = Clip("adryan_calm.mp3");
            voice.adrianSoft = Clip("adryan_soft.mp3");
            voice.adrianQuestion = Clip("adrian_question.mp3");
            voice.celesteFriendly = Clip("selesty_friendly.mp3");
            voice.celesteIrritated = Clip("selesty_irritated.mp3");
            voice.celesteScared = Clip("selesty_scared.mp3");
            voice.isabelleOrdinary = Clip("Isabelle_ordinary.mp3");
            voice.isabelleWorried = Clip("isabelle_worried.mp3");
            voice.prefectCalm = Clip("prefect_calm.mp3");
            voice.prefectPoint = Clip("prefect_point.mp3");
            voice.murderImpact = Clip("sound_of_killing.mp3");
            voice.cleaning = Clip("cleaning_after_kill.mp3");
            var serializedDialogue = new SerializedObject(dialogue);
            serializedDialogue.FindProperty("voice").objectReferenceValue = voice;
            serializedDialogue.ApplyModifiedPropertiesWithoutUndo();
            murder.voice = voice;
            cleanup.voice = voice;
            ball.voice = voice;
            mystery.voice = voice;
            EditorUtility.SetDirty(dialogue);
            EditorUtility.SetDirty(murder);
            EditorUtility.SetDirty(cleanup);
            EditorUtility.SetDirty(ball);
            EditorUtility.SetDirty(mystery);
            EditorUtility.SetDirty(voice);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Could not save Phase 3C scene");
            Debug.Log("[Phase3C] Installed 17 existing MP3 clips and wired dialogue, murder, cleanup, monologue and ball");
        }

        private static AudioClip Clip(string file)
        {
            string path = Voices + file;
            if (!File.Exists(path)) throw new Exception("Voice file missing: " + path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path) ?? throw new Exception("Audio import failed: " + path);
        }
        private static bool SameHash(string first, string second)
        {
            if (!File.Exists(second)) return false;
            using var sha = SHA256.Create();
            using var a = File.OpenRead(first);
            byte[] x = sha.ComputeHash(a);
            using var b = File.OpenRead(second);
            return x.SequenceEqual(sha.ComputeHash(b));
        }
    }
}
