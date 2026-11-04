using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace VelvetVows.Tests
{
    public sealed class FinalDialoguePlayModeTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static MonoBehaviour Component(string name) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.GetType().Name == name);
        private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        private static object Value(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
        private static Button Button(string name) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.name == name);
        private static IEnumerator Until(Func<bool> ready, float seconds, string context)
        {
            float limit = Time.realtimeSinceStartup + seconds;
            while (!ready() && Time.realtimeSinceStartup < limit) yield return null;
            Assert.IsTrue(ready(), "Timed out: " + context);
        }
        private static IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }
        private static void Click(Button button)
        {
            Assert.IsTrue(button.IsActive() && button.IsInteractable(), button.name + " disabled");
            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            var point = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * .5f);
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Assert.IsTrue(hits.Count > 0 && (hits[0].gameObject == button.gameObject ||
                hits[0].gameObject.transform.IsChildOf(button.transform)), button.name + " blocked by UI");
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
        private static void Move(Transform actor, Vector3 location)
        {
            var cc = actor.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            actor.position = location;
            if (cc != null) cc.enabled = true;
        }
        private static IEnumerator Scripted(MonoBehaviour dialogue, List<string> shown, string[] expected)
        {
            yield return null;
            var speaker = (Text)Field(dialogue, "speakerText");
            var body = (Text)Field(dialogue, "bodyText");
            int beginning = shown.Count - 1;
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], speaker.text + "|" + body.text, "Dialogue order at line " + i);
                Assert.AreEqual(beginning + i + 1, shown.Count, "Subtitle skipped or duplicated");
                Click(Button("Continue"));
                yield return null;
            }
            CollectionAssert.AreEqual(expected, shown.Skip(beginning).Take(expected.Length).ToArray());
            Assert.IsFalse((bool)Value(dialogue, "IsOpen"), "Conversation closed before its final line");
        }

        [UnityTest]
        public IEnumerator IntroEavesdropsConfrontationAndNarration()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Load();
            var dialogue = Component("DialogueManager");
            var story = Component("Phase2AStoryManager");
            var voice = Component("CharacterVoiceManager");
            var diary = Component("Phase3ADiaryIntro");
            var shown = new List<string>();
            dialogue.GetType().GetEvent("LineShown", Flags).AddEventHandler(dialogue,
                new Action<string, string>((who, words) => shown.Add(who + "|" + words)));
            Button("BEGIN Button").onClick.Invoke();
            var body = (Text)Field(dialogue, "bodyText");
            yield return Until(() => body.text == "Are you alright?", 30, "Adrian first meeting");
            yield return null;
            Click(Button("Continue"));
            yield return Until(() => body.text == "Y-yes. Thank you.", 2, "Evelyn first reply");
            yield return null;
            Click(Button("Continue"));
            yield return Until(() => body.text == "Be careful, Evelyn.", 3, "Adrian warning");
            yield return null;
            Click(Button("Continue"));
            yield return Until(() => (bool)Value(diary, "IsPlaying"), 21, "diary introduction");
            CollectionAssert.AreEqual(new[] {
                "ISABELLE|Nobody even wants her here.", "AMELIE|Ha…", "ADRIAN|Get away.",
                "ISABELLE|Whatever. Let’s go.",
                "ADRIAN|Are you alright?", "EVELYN|Y-yes. Thank you.",
                "ADRIAN|Be careful, Evelyn.", "ADRIAN|Evelyn, isn’t it?"
            }, shown.Take(8).ToArray());

            var segments = Field(diary, "japaneseSegments");
            yield return Until(() => (string)Value(diary, "CurrentBeat") == "THE LAST HAPPY SUMMER", 5, "childhood audio");
            Assert.IsTrue((bool)Value(segments, "IsPlaying"));
            Assert.AreEqual(1, Value(segments, "StartedSegments"));
            yield return Until(() => (string)Value(diary, "CurrentBeat") == "THE GIRL NOBODY WANTED", 10, "second childhood beat");
            Assert.IsTrue((bool)Value(segments, "IsPlaying"));
            Assert.AreEqual(1, Value(segments, "StartedSegments"));
            yield return Until(() => (string)Value(diary, "CurrentBeat") == "THE FIRST PERSON WHO STAYED", 11, "third childhood beat");
            Assert.IsTrue((bool)Value(segments, "IsPlaying"));
            Assert.AreEqual(1, Value(segments, "StartedSegments"));
            yield return Until(() => (string)Value(diary, "CurrentBeat") == "ADRIAN VALMONT", 17, "love audio");
            Assert.IsTrue((bool)Value(segments, "IsPlaying"));
            Assert.AreEqual(2, Value(segments, "StartedSegments"));
            yield return Until(() => (string)Value(diary, "CurrentBeat") == "DIARY", 17, "diary audio");
            Assert.IsTrue((bool)Value(segments, "IsPlaying"));
            Assert.AreEqual(3, Value(segments, "StartedSegments"));
            yield return Until(() => Value(story, "Stage").ToString() == "MissingStudent", 35, "Isabelle and Amelie");
            Assert.IsFalse((bool)Value(segments, "IsPlaying"));
            Assert.IsNotNull(Field(diary, "storyComposition"));
            yield return Scripted(dialogue, shown, new[] {
                "ISABELLE|Did you hear? Another student disappeared.",
                "AMELIE|Lucien Morel. Nobody has seen him since Friday.",
                "EVELYN|Lucien? He was annoying.",
                "ISABELLE|Didn't he ask you out last week?",
                "EVELYN|He asked me to study mathematics with him. I said no.",
                "AMELIE|That's the third disappearance this semester.",
                "EVELYN|People leave this academy all the time."
            });
            yield return Until(() => (bool)Field(story, "isabelleReady"), 8, "Isabelle meets Celeste");
            var player = (Transform)Field(story, "player");
            var celeste = (Transform)Field(story, "celeste");
            var isabelle = (Transform)Field(story, "isabelle");
            Move(player, (celeste.position + isabelle.position) * .5f + Vector3.back);
            yield return Until(() => Value(story, "Stage").ToString() == "Overhearing" && (bool)Value(dialogue, "IsOpen"), 4, "Celeste and Isabelle");
            yield return Scripted(dialogue, shown, new[] {
                "CELESTE|I'm going to ask Adrian to the Black Rose Ball.",
                "ISABELLE|Seriously? Tomorrow?", "CELESTE|Why not? He's single.",
                "ISABELLE|What if he refuses?", "CELESTE|Then at least I'll know.",
                "EVELYN · THOUGHT|She's going to confess?", "EVELYN · THOUGHT|No. He wouldn't accept… would he?",
                "EVELYN · THOUGHT|Girls like her have always had everything.",
                "EVELYN · THOUGHT|Friends. Money. A family.",
                "EVELYN · THOUGHT|And now she wants him, too.",
                "EVELYN · THOUGHT|<color=#761B32>But not him.</color>"
            });
            yield return Until(() => Value(story, "Stage").ToString() == "WatchAdrian", 4, "Library objective");
            var adrian = (Transform)Field(story, "adrian");
            Move(player, adrian.position + Vector3.back * 3.5f);
            yield return Until(() => Value(story, "Stage").ToString() == "Watching" && (bool)Value(dialogue, "IsOpen"), 7, "Adrian and Celeste");
            yield return Scripted(dialogue, shown, new[] {
                "CELESTE|Adrian! Are you attending the Ball tomorrow?", "ADRIAN|I suppose so.",
                "CELESTE|With someone?", "ADRIAN|Not yet.", "CELESTE|Then perhaps we could…",
                "ODETTE|Adrian? Professor Laurent is waiting.", "ADRIAN|Excuse me, Celeste.",
                "EVELYN · THOUGHT|He doesn't have a partner.", "EVELYN · THOUGHT|She might actually ask him."
            });
            yield return Until(() => Value(story, "Stage").ToString() == "Errands", 24, "control returns after Library");
            Assert.IsTrue(Camera.main.GetComponents<Behaviour>().First(x => x.GetType().Name == "CinemachineBrain").enabled);
            Assert.IsTrue(player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "ThirdPersonController").enabled);

            // Exercise the existing confrontation dialogue directly; the full errands route is outside this focused audit.
            Move(player, new Vector3(-17.2f, 0, -2.7f));
            Move(celeste, new Vector3(-19.2f, 0, -2.7f));
            story.GetType().GetField("<Stage>k__BackingField", Flags).SetValue(story,
                Enum.Parse(story.GetType().Assembly.GetType("VelvetVows.Phase2AStage"), "Confront"));
            story.GetType().GetField("phase2B", Flags).SetValue(story, null);
            story.GetType().GetMethod("ConfrontCeleste", Flags).Invoke(story, null);
            yield return Until(() => (bool)Value(dialogue, "IsOpen"), 3, "Art Studio confrontation");
            yield return Scripted(dialogue, shown, new[] {
                "CELESTE|Where is the other thing you wanted to show me?", "EVELYN|Celeste… do you love Adrian?",
                "CELESTE|What?", "EVELYN|Answer me.",
                "CELESTE|I barely know him. I just wanted to ask him to the Ball.",
                "EVELYN|You don't deserve him.", "CELESTE|Evelyn, you're scaring me."
            });
            Assert.AreEqual("Complete", Value(story, "Stage").ToString());

            var missing = Component("Phase3BMissingStudents");
            missing.StartCoroutine((IEnumerator)missing.GetType().GetMethod("PlayMonologue", Flags).Invoke(missing, null));
            yield return Until(() => (bool)Value(missing, "IsMonologuePlaying"), 2, "Adrian monologue");
            var adrianAudio = Field(missing, "japaneseSegments");
            yield return Until(() => (bool)Value(adrianAudio, "IsPlaying"), 3, "Adrian Japanese segment");
            Assert.AreEqual(1, Value(adrianAudio, "StartedSegments"));
            Assert.IsFalse((bool)Value(voice, "VoicePlaying"), "Generic Adrian voice overlaps narration");
            yield return Until(() => !(bool)Value(missing, "IsMonologuePlaying"), 30, "Adrian segment ends");
            Assert.IsFalse((bool)Value(adrianAudio, "IsPlaying"));
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator SkipIntroAndNpcVoiceWindows()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Load();
            var voice = Component("CharacterVoiceManager");
            voice.GetType().GetMethod("PlayDialogue", Flags).Invoke(voice, new object[] { "AMELIE", "First" });
            Assert.AreEqual(0f, Value(voice, "LastVoiceSegmentStart"));
            Assert.AreEqual(5f, Value(voice, "LastVoiceSegmentEnd"));
            voice.GetType().GetMethod("PlayDialogue", Flags).Invoke(voice, new object[] { "AMELIE", "Second" });
            Assert.AreEqual(11f, Value(voice, "LastVoiceSegmentStart"));
            Assert.AreEqual(15f, Value(voice, "LastVoiceSegmentEnd"));
            yield return new WaitForSecondsRealtime(4.2f);
            Assert.IsFalse((bool)Value(voice, "VoicePlaying"));
            voice.GetType().GetMethod("PlayDialogue", Flags).Invoke(voice, new object[] { "MARGOT", "First" });
            Assert.AreEqual(0f, Value(voice, "LastVoiceSegmentStart"));
            Assert.AreEqual(3f, Value(voice, "LastVoiceSegmentEnd"));
            yield return new WaitForSecondsRealtime(3.2f);
            Assert.IsFalse((bool)Value(voice, "VoicePlaying"));
            voice.GetType().GetMethod("PlayDialogue", Flags).Invoke(voice, new object[] { "MARGOT", "Second" });
            Assert.AreEqual(4f, Value(voice, "LastVoiceSegmentStart"));
            Assert.AreEqual(10f, Value(voice, "LastVoiceSegmentEnd"));
            voice.GetType().GetMethod("StopVoice", Flags).Invoke(voice, null);
            Button("BEGIN Button").onClick.Invoke();
            var diary = Component("Phase3ADiaryIntro");
            yield return Until(() => (bool)Value(diary, "IsPlaying"), 35, "skip intro button");
            yield return Until(() => (string)Value(diary, "CurrentBeat") == "THE LAST HAPPY SUMMER", 5, "skip active narration");
            Assert.IsTrue((bool)Value(Field(diary, "japaneseSegments"), "IsPlaying"));
            Click(Button("SKIP INTRO"));
            yield return Until(() => !(bool)Value(diary, "IsPlaying"), 2, "skip closes diary");
            Assert.IsFalse((bool)Value(Field(diary, "japaneseSegments"), "IsPlaying"));
            var story = Component("Phase2AStoryManager");
            yield return Until(() => Value(story, "Stage").ToString() == "MissingStudent", 3, "post-skip dialogue");
            var dialogue = Component("DialogueManager");
            Assert.IsTrue((bool)Value(dialogue, "IsOpen"));
            yield return null;
            for (int i = 0; i < 7; i++) { Click(Button("Continue")); yield return null; }
            Assert.AreEqual("Overhear", Value(story, "Stage").ToString());
            var player = (Transform)Field(story, "player");
            Assert.IsTrue(player.GetComponents<MonoBehaviour>().First(x => x.GetType().Name == "ThirdPersonController").enabled);
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator ChoiceSpeakerAndBallroomLineOrder()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Load();
            var dialogue = Component("DialogueManager");
            Component("GameHUD").GetType().GetMethod("ShowStart", Flags).Invoke(Component("GameHUD"), new object[] { false });
            var lines = new List<string>();
            dialogue.GetType().GetEvent("LineShown", Flags).AddEventHandler(dialogue,
                new Action<string, string>((who, words) => lines.Add(who + "|" + words)));
            var assembly = dialogue.GetType().Assembly;
            var lineType = assembly.GetType("VelvetVows.DialogueLine");
            var choiceType = assembly.GetType("VelvetVows.DialogueChoice");
            var oneLine = Array.CreateInstance(lineType, 1);
            oneLine.SetValue(Activator.CreateInstance(lineType, new object[] { "CELESTE", "Are you coming?" }), 0);
            var oneChoice = Array.CreateInstance(choiceType, 1);
            oneChoice.SetValue(Activator.CreateInstance(choiceType,
                new object[] { "Of course.", "CELESTE: I'll see you there.", 0f }), 0);
            dialogue.GetType().GetMethod("Begin", Flags).Invoke(dialogue, new object[] { null, oneLine, oneChoice });
            yield return null;
            ((Button[])Field(dialogue, "choiceButtons"))[0].onClick.Invoke();
            Assert.AreEqual("EVELYN|Of course.", lines.Last());
            yield return null;
            Click(Button("Continue"));
            Assert.AreEqual("CELESTE|I'll see you there.", lines.Last());
            yield return null;
            Click(Button("Continue"));
            Assert.IsFalse((bool)Value(dialogue, "IsOpen"));
            lines.Clear();

            var game = Component("GameManager");
            var ball = Component("Phase2CBallDirector");
            game.GetType().GetMethod("EndPhase2B", Flags).Invoke(game, null);
            ball.GetType().GetMethod("BeginBall", Flags).Invoke(ball, null);
            var bodyText = (Text)Field(dialogue, "bodyText");
            foreach (var diaryLine in new[] { "I knew you were watching me.",
                "But you weren’t the only one watching.", "I kept the things you left behind." })
            {
                yield return Until(() => bodyText.text == diaryLine, 180, "Adrian diary page");
                yield return null;
                Click(Button("Continue"));
            }
            yield return Until(() => (string)Value(ball, "Beat") == "Ending", 180, "Ballroom ending");
            string[] expected = {
                "ISABELLE|Evelyn! Have you seen Celeste?", "EVELYN|No. Why?",
                "ISABELLE|She hasn’t answered anyone since yesterday.", "EVELYN|Perhaps she went home.",
                "ISABELLE|Without telling anyone?", "EVELYN|You know how unpredictable she can be.",
                "ISABELLE|Perhaps. I’ll keep looking for her.",
                "ADRIAN|I’ve been looking for you.", "EVELYN|M-me?", "ADRIAN|Who else?",
                "ADRIAN|May I have this dance?", "EVELYN|Yes.",
                "ADRIAN|Do you remember Lucien?", "EVELYN|Lucien?",
                "ADRIAN|He wouldn’t leave you alone.", "EVELYN|What are you saying?",
                "ADRIAN|Neither would the others.", "EVELYN|You… knew?",
                "ADRIAN|About Celeste? Of course.",
                "ADRIAN|I knew you were watching me.",
                "ADRIAN|But you weren’t the only one watching.",
                "ADRIAN|I kept the things you left behind.",
                "EVELYN|You… kept my things?", "ADRIAN|Every single one.",
                "EVELYN|If you knew… why didn’t you tell me?", "ADRIAN|Because I was afraid.",
                "EVELYN|You? Afraid?", "ADRIAN|Everyone wanted my name. My fortune. My future.",
                "ADRIAN|But you wanted me.", "ADRIAN|I wanted you too, long before the Ball.",
                "ADRIAN|I wanted you to come to me willingly.",
                "EVELYN|You could have just asked.", "ADRIAN|And risk hearing you say no?",
                "EVELYN|You never had to be afraid.",
                "ADRIAN|You did it for me.", "ADRIAN|And I did it for you.",
                "ADRIAN|I made sure those other boys could never pull you away.",
                "EVELYN|Then we’re the same.",
                "ADRIAN|Perhaps we always were."
            };
            CollectionAssert.AreEqual(expected, lines);
            Assert.AreEqual("Phase2CEnding", Value(game, "State").ToString());
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
