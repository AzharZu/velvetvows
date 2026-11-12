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
using Object = UnityEngine.Object;

namespace VelvetVows.Tests
{
    public sealed class Phase3KCharacterPlayModeTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        bool previousAsync;
        PlaythroughCapture capture;
        static MonoBehaviour Get(string name) => Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.GetType().Name == name);
        static object Field(object o, string n) => o.GetType().GetField(n, Flags).GetValue(o);
        static object Value(object o, string n) => o.GetType().GetProperty(n, Flags).GetValue(o);
        static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, Flags).Invoke(o, args);
        static void SetState(object o, string n, string value)
        { var f = o.GetType().GetField("<" + n + ">k__BackingField", Flags); f.SetValue(o, Enum.Parse(f.FieldType, value)); }
        static MonoBehaviour On(Transform t, string type) => t.GetComponentsInChildren<MonoBehaviour>().First(b => b.GetType().Name == type);
        [UnityTearDown] public IEnumerator Restore() { if (capture != null && capture.Recording) capture.FinishRecording(); ShaderUtil.allowAsyncCompilation = previousAsync; Time.timeScale = 1; yield return null; }

        [UnityTest, Timeout(180000)]
        public IEnumerator FacesFollowRecordingSilenceAndPhysicalFootsteps()
        {
            previousAsync = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
            var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")); view.Show(); view.Focus();
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale = 1; Application.runInBackground = true; AudioListener.pause = false;
            yield return new WaitForSeconds(.8f);
            var story = Get("Phase2AStoryManager"); var game = Get("GameManager"); var hud = Get("GameHUD");
            var dialogue = Get("DialogueManager"); var voice = Get("CharacterVoiceManager");
            Call(hud, "ShowStart", false); Call(hud, "ShowGameplay", true);
            Camera.main.GetComponents<Behaviour>().First(b => b.GetType().Name == "CinemachineBrain").enabled = false;
            var player = (Transform)Field(story, "player");
            var expression = On(player, "AcademyCharacterExpression"); var footsteps = On(player, "AcademyFootsteps");
            if (Environment.GetEnvironmentVariable("VELVET_CHARACTER_PREVIEW") == "1")
            {
                capture = Object.FindFirstObjectByType<AudioListener>().gameObject.AddComponent<PlaythroughCapture>();
                capture.BeginRecording(); capture.Note("showcase", "Staged cast portraits with original voices; not a full playthrough.");
            }
            Move(player, new Vector3(-5, .02f, 0)); player.rotation = Quaternion.identity;
            Frame(player);
            Call(dialogue, "ShowAdvanceableCinematicLine", "EVELYN", "Thank you. I am glad you stayed.");
            yield return new WaitForSeconds(.7f);
            Assert.Greater((float)Value(expression, "ExpressionWeight"), 5);
            yield return Capture("01_evelyn");
            Call(dialogue, "HideCinematicLine");
            var celeste = (Transform)Field(story, "celeste"); celeste.gameObject.SetActive(true);
            Move(celeste, new Vector3(-3.5f, .02f, 3)); celeste.rotation = Quaternion.identity;
            Frame(celeste);
            Call(dialogue, "ShowAdvanceableCinematicLine", "CELESTE", "Stop. Please leave me alone.");
            yield return new WaitForSeconds(.75f); yield return Capture("02_celeste");
            Call(dialogue, "HideCinematicLine");
            var amelie = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(b => b.GetType().Name == "AcademyCharacterPresence" && (string)Value(b, "CharacterName") == "AMELIE");
            Frame(amelie.transform); Call(dialogue, "ShowAdvanceableCinematicLine", "AMELIE", "Thank you for helping us.");
            yield return new WaitForSeconds(.7f); yield return Capture("03_amelie");
            Call(dialogue, "HideCinematicLine");
            var prefect = Get("Phase2BPrefectInvestigation"); ((Behaviour)Field(prefect, "normalPatrol")).enabled = false;
            Frame(prefect.transform); Call(dialogue, "ShowAdvanceableCinematicLine", "PREFECT", "Stay quiet. I heard something.");
            yield return new WaitForSeconds(.7f); yield return Capture("04_prefect");
            Call(dialogue, "HideCinematicLine");
            if (capture != null) capture.FinishRecording();

            // A known signal with a silent middle checks actual audio-driven lip movement, not an elapsed-time loop.
            var clip = AudioClip.Create("QA speech and silence", 48000 * 4, 1, 48000, false);
            var data = new float[48000 * 4];
            for (int i = 0; i < data.Length; i++) if (i < 48000 || i > 48000 * 2.2f) data[i] = .18f * Mathf.Sin(i * 2 * Mathf.PI * 220 / 48000);
            clip.SetData(data, 0); voice.GetType().GetField("evelynCalm").SetValue(voice, clip);
            Call(voice, "StopAll"); Frame(player);
            var music = Get("MusicManager"); Call(music, "PlayExploration");
            for (float t = 0; t < 6 && Field(music, "transition") != null; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(.8f);
            var musicSource = (AudioSource)Field(music, "source"); float baseline = musicSource.volume;
            Assert.Greater(baseline, .01f, "Music baseline must finish its crossfade");
            Call(voice, "PlayDialogue", "EVELYN", "A calm line.");
            float maxMouth = 0;
            for (float t = 0; t < .7f; t += Time.deltaTime)
            { maxMouth = Mathf.Max(maxMouth, (float)Value(expression, "MouthWeight")); yield return null; }
            Assert.Greater(maxMouth, 2, "The mouth must follow real audio output");
            Assert.AreEqual(0, (float)Value(On(celeste, "AcademyCharacterExpression"), "MouthWeight"), "Silent characters must not lip-sync another speaker");
            Assert.Less(musicSource.volume, baseline * .65f, "Speech should lower music");
            yield return new WaitForSeconds(.9f);
            Assert.Less((float)Value(expression, "MouthWeight"), .5f, "The mouth must close in a silent portion of a playing recording");
            Call(voice, "StopVoice"); yield return new WaitForSeconds(1);
            Assert.That(musicSource.volume, Is.EqualTo(baseline).Within(.01f), "Music must recover after speech");
            Assert.AreEqual(0, (float)Value(expression, "MouthWeight"));

            // Actual controller walking must make steps; standing still and teleporting must not.
            SetState(game, "State", "Phase2A"); SetState(story, "Stage", "Errands"); Call(game, "SetPhase2AControl", true);
            Move(player, new Vector3(-5, .02f, 3));
            int before = (int)Value(footsteps, "StepCount");
            yield return (IEnumerator)typeof(Phase2APlayModeTests).GetMethod("WalkRoute", Flags).Invoke(null,
                new object[] { player, new Vector3(-5, 0, -2f), (Func<bool>)(() => false) });
            Assert.Greater((int)Value(footsteps, "StepCount"), before + 1);
            // The new contact-based footsteps include the final planted foot while the controller decelerates.
            yield return new WaitForSeconds(.35f);
            int after = (int)Value(footsteps, "StepCount");
            yield return new WaitForSeconds(.6f); Assert.AreEqual(after, Value(footsteps, "StepCount"));
            Move(player, new Vector3(-5, .02f, 5)); yield return null; yield return null;
            Assert.AreEqual(after, Value(footsteps, "StepCount"), "Teleporting must not make a footstep");
            var presence = On(celeste, "AcademyCharacterPresence"); presence.enabled = false;
            yield return null;
            Assert.AreEqual(0, (float)Value(On(celeste, "AcademyCharacterExpression"), "MouthWeight"), "Death must release facial performance");
            Object.Destroy(clip);
            Debug.Log("[Phase3K] Audio-driven lips, silent intervals, isolated speaker, music duck/recovery, real footsteps and death release passed.");
        }
        static void Move(Transform t, Vector3 p) { var c = t.GetComponent<CharacterController>(); if (c != null) c.enabled = false; t.position = p; if (c != null) c.enabled = true; Physics.SyncTransforms(); }
        static void Frame(Transform actor)
        {
            var a = actor.GetComponentInChildren<Animator>(); var target = a.GetBoneTransform(HumanBodyBones.Head).position - Vector3.up * .13f;
            var position = target + a.transform.forward * 1.8f + a.transform.right * .28f + Vector3.up * .03f;
            Camera.main.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position)); Camera.main.fieldOfView = 36;
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame(); Directory.CreateDirectory("Phase3K_Captures");
            var image = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes("Phase3K_Captures/" + name + ".jpg", image.EncodeToJPG(92)); Object.Destroy(image);
            if (capture != null) { capture.Note("portrait", name); yield return new WaitForSecondsRealtime(5); }
        }
    }
}
