using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelvetVows.Editor
{
    public static class Phase3E3AudioBinder
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string AudioPath = "Assets/VelvetVows/Audio/Voices/";

        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var voice = Object.FindFirstObjectByType<CharacterVoiceManager>();
            var diary = Object.FindFirstObjectByType<Phase3ADiaryIntro>();
            var missing = Object.FindFirstObjectByType<Phase3BMissingStudents>();
            if (voice == null || diary == null || missing == null)
                throw new IOException("Phase 3E-3 scene components missing");

            voice.amelieVoice = Clip("npc1.mp3");
            voice.margotVoice = Clip("npc2.mp3");
            voice.prefectDetectSomething = Clip("prefect_detect_smth.wav");
            voice.prefectFoundBody = Clip("prefect_found_body.wav");
            diary.unverifiedJapaneseNarration = Clip("evelyn_tells_astory.mp3");
            missing.unverifiedJapaneseNarration = Clip("adrian_tellstory.mp3");
            SetRange(diary.historySegment, 80f, 106f);
            SetRange(diary.loveSegment, 17f, 26f);
            SetRange(diary.diarySegment, 218f, 229f);
            SetRange(missing.missingBoysSegment, 30f, 50f);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save Phase 3E-3 main scene");
            AssetDatabase.SaveAssets();
            Debug.Log("[FinalDialogue] Saved main scene with approved Japanese segment ranges and character audio bindings.");
        }

        private static AudioClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + name);
            if (clip == null) throw new IOException("Missing audio asset: " + name);
            return clip;
        }

        private static void SetRange(JapaneseSegmentRange range, float start, float end)
        {
            if (range == null) throw new IOException("Japanese narration range missing");
            range.timingApproved = true;
            range.startSeconds = start;
            range.endSeconds = end;
        }
    }
}
