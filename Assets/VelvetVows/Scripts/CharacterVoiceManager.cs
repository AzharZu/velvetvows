using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VelvetVows
{
    public sealed class CharacterVoiceManager : MonoBehaviour
    {
        [Header("Evelyn")]
        public AudioClip evelynCalm, evelynNice, evelynShy, evelynScared, evelynPostKillLaugh;
        [Header("Adrian")]
        public AudioClip adrianCalm, adrianSoft, adrianQuestion;
        [Header("Celeste")]
        public AudioClip celesteFriendly, celesteIrritated, celesteScared;
        [Header("Isabelle and Prefect")]
        public AudioClip isabelleOrdinary, isabelleWorried, prefectCalm, prefectPoint;
        [Header("Additional character recordings")]
        public AudioClip amelieVoice, margotVoice, prefectDetectSomething, prefectFoundBody;
        [Header("Effects")]
        public AudioClip murderImpact, cleaning;
        public AudioClip gameOver;
        [Header("Mix")]
        [Range(0, 1)] public float voiceVolume = .68f;
        [Range(0, 1)] public float sfxVolume = .72f;

        public AudioClip LastVoiceClip { get; private set; }
        public AudioClip LastSfxClip { get; private set; }
        public int ImpactCount { get; private set; }
        public int CleanupCount { get; private set; }
        public int PostKillLaughCount { get; private set; }
        public bool VoicePlaying => voiceSource != null && voiceSource.isPlaying;
        public string ActiveSpeaker { get; private set; }
        public AcademyCharacterPresence SpeakingActor { get; private set; }
        public float SpeechLevel { get; private set; }
        private readonly float[] speechSamples = new float[256];
        private readonly float[] speechSpectrum = new float[512];
        private readonly float[] vowels = new float[5];
        private static readonly Vector2[] formants = { new(750, 1250), new(300, 2400), new(350, 900), new(500, 2000), new(500, 1050) };
        public float VowelWeight(int index) => index >= 0 && index < vowels.Length ? vowels[index] : 0;
        public float VoiceRemainingSeconds => voiceFade != null ? 1.8f : VoicePlaying ? Mathf.Max(0,
            (segmentEndSeconds > 0 ? segmentEndSeconds : voiceSource.clip.length) - voiceSource.time) : 0;
        public float LastVoiceSegmentStart { get; private set; }
        public float LastVoiceSegmentEnd { get; private set; }
        public bool SfxPlaying => sfxSource != null && sfxSource.isPlaying;
        public bool GameOverPlaying => gameOverSource != null && gameOverSource.isPlaying;
        public int GameOverCount { get; private set; }

        private AudioSource voiceSource, sfxSource, gameOverSource;
        private float lastVoiceAt = -100f;
        private float laughUntil;
        private Phase2AStoryManager story;
        private AudioClip pendingPrefectIncident;
        private int amelieVariation, margotVariation;
        private Coroutine voiceFade;
        private readonly Dictionary<string, float> characterLastAt = new Dictionary<string, float>();
        private readonly Dictionary<string, AudioClip> characterLastClip = new Dictionary<string, AudioClip>();
        private readonly Dictionary<AudioClip, float> clipGains = new Dictionary<AudioClip, float>();
        private bool gameOverStarted;
        private float segmentEndSeconds, segmentStopAt;

        private void Awake()
        {
            story = FindFirstObjectByType<Phase2AStoryManager>();
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0;
            voiceSource.volume = voiceVolume;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0;
            sfxSource.volume = sfxVolume;
            gameOverSource = gameObject.AddComponent<AudioSource>();
            gameOverSource.playOnAwake = false;
            gameOverSource.loop = false;
            gameOverSource.spatialBlend = 0;
            gameOverSource.volume = sfxVolume;
        }

        private void Update()
        {
            float level = 0;
            if (VoicePlaying)
            {
                voiceSource.GetOutputData(speechSamples, 0);
                foreach (float sample in speechSamples) level += sample * sample;
                level = Mathf.Clamp01(Mathf.Sqrt(level / speechSamples.Length) * 9f);
            }
            SpeechLevel = Mathf.MoveTowards(SpeechLevel, level, Time.unscaledDeltaTime * (level > SpeechLevel ? 15 : 9));
            AnalyseVowels(level);
            if (segmentEndSeconds > 0 && VoicePlaying &&
                (voiceSource.time >= segmentEndSeconds || Time.unscaledTime >= segmentStopAt)) StopVoice();
            if (laughUntil > 0 && Time.time >= laughUntil)
            {
                laughUntil = 0;
                StopVoice();
            }
        }

        private void AnalyseVowels(float level)
        {
            // Acoustic approximation, independent of caption language. No text is treated as a translation.
            if (VoicePlaying && level > .025f) voiceSource.GetSpectrumData(speechSpectrum, 0, FFTWindow.BlackmanHarris);
            float bin = AudioSettings.outputSampleRate * .5f / speechSpectrum.Length;
            float f1 = 500, f2 = 1500, low = 0, high = 0;
            if (level > .025f)
                for (int i = 2; i < speechSpectrum.Length && i * bin < 3000; i++)
                {
                    float hz = i * bin;
                    if (hz >= 230 && hz < 950 && speechSpectrum[i] > low) { low = speechSpectrum[i]; f1 = hz; }
                    if (hz >= 950 && speechSpectrum[i] > high) { high = speechSpectrum[i]; f2 = hz; }
                }
            float sum = 0;
            for (int i = 0; i < 5; i++)
            {
                var delta = new Vector2((f1 - formants[i].x) / 350, (f2 - formants[i].y) / 650);
                sum += Mathf.Exp(-delta.sqrMagnitude * 2);
            }
            for (int i = 0; i < 5; i++)
            {
                var delta = new Vector2((f1 - formants[i].x) / 350, (f2 - formants[i].y) / 650);
                float target = VoicePlaying && level > .025f ? Mathf.Exp(-delta.sqrMagnitude * 2) / Mathf.Max(.001f, sum) : 0;
                vowels[i] = Mathf.MoveTowards(vowels[i], target, Time.unscaledDeltaTime * 9);
            }
        }

        public void PlayDialogue(string speaker, string line)
        {
            if (voiceFade != null) { StopCoroutine(voiceFade); voiceFade = null; }
            if (VoicePlaying) voiceFade = StartCoroutine(FadeIntoDialogue(speaker, line));
            else StartDialogueCue(speaker, line);
        }

        private IEnumerator FadeIntoDialogue(string speaker, string line)
        {
            float volume = voiceSource.volume;
            for (float time = 0; time < .085f; time += Time.unscaledDeltaTime)
            { voiceSource.volume = volume * (1f - time / .085f); yield return null; }
            voiceFade = null;
            StartDialogueCue(speaker, line);
        }

        private void StartDialogueCue(string speaker, string line)
        {
            StopVoice();
            ActiveSpeaker = speaker;
            ResolveSpeakingActor();
            // Internal monologues use their continuous Japanese narration channel.
            if (speaker != null && speaker.Contains("THOUGHT")) return;
            var clip = Select(speaker, line);
            if (clip == null) return;
            if (speaker.StartsWith("AMELIE") && clip == amelieVoice)
            {
                bool first = amelieVariation++ % 2 == 0;
                PlaySegment(clip, first ? 0f : 11f, first ? 5f : 15f);
                return;
            }
            if (speaker.StartsWith("MARGOT") && clip == margotVoice)
            {
                bool first = margotVariation++ % 2 == 0;
                PlaySegment(clip, first ? 0f : 4f, first ? 3f : 10f);
                return;
            }
            string character = speaker.Split(' ')[0];
            if (clip != prefectDetectSomething && clip != prefectFoundBody &&
                characterLastClip.TryGetValue(character, out var previous) && previous == clip &&
                characterLastAt.TryGetValue(character, out float last) && Time.unscaledTime - last < 8f) return;
            characterLastClip[character] = clip; characterLastAt[character] = Time.unscaledTime;
            PlayVoice(clip, 1f, 1f);
        }

        public void PlayAdrianThought()
        {
            StopVoice();
            ActiveSpeaker = "ADRIAN";
            ResolveSpeakingActor();
            if (adrianCalm != null && (LastVoiceClip != adrianCalm || Time.time - lastVoiceAt >= 2.4f))
                PlayVoice(adrianCalm, .985f, 1.015f);
        }

        public void PlayPostKillLaugh()
        {
            if (evelynPostKillLaugh == null) return;
            StopVoice();
            ActiveSpeaker = "EVELYN";
            ResolveSpeakingActor();
            PlayVoice(evelynPostKillLaugh, .98f, 1.02f);
            PostKillLaughCount++;
            laughUntil = Time.time + 1.6f;
        }

        public void PlayImpact()
        {
            if (murderImpact == null) return;
            sfxSource.Stop();
            sfxSource.clip = murderImpact;
            sfxSource.volume = sfxVolume;
            sfxSource.Play();
            LastSfxClip = murderImpact;
            ImpactCount++;
        }

        public void PlayCleaning()
        {
            if (cleaning == null) return;
            sfxSource.Stop();
            sfxSource.clip = cleaning;
            sfxSource.volume = sfxVolume * .7f;
            sfxSource.Play();
            LastSfxClip = cleaning;
            CleanupCount++;
        }

        public void StopVoice()
        {
            if (voiceFade != null) { StopCoroutine(voiceFade); voiceFade = null; }
            laughUntil = 0;
            segmentEndSeconds = 0;
            ActiveSpeaker = null;
            SpeakingActor = null;
            SpeechLevel = 0;
            if (voiceSource != null) voiceSource.Stop();
        }
        public void StopSfx() { if (sfxSource != null) sfxSource.Stop(); }
        private void ResolveSpeakingActor()
        {
            SpeakingActor = null;
            if (string.IsNullOrEmpty(ActiveSpeaker) || ActiveSpeaker.Contains("THOUGHT")) return;
            Vector3 origin = Camera.main != null ? Camera.main.transform.position : transform.position;
            float nearest = float.PositiveInfinity;
            foreach (var actor in FindObjectsByType<AcademyCharacterPresence>(FindObjectsSortMode.None))
            {
                if (!actor.enabled || !ActiveSpeaker.StartsWith(actor.CharacterName)) continue;
                float distance = (actor.transform.position - origin).sqrMagnitude;
                if (distance < nearest) { nearest = distance; SpeakingActor = actor; }
            }
        }
        public void StopAll()
        {
            StopVoice(); StopSfx(); StopGameOver(); pendingPrefectIncident = null;
            characterLastAt.Clear(); characterLastClip.Clear();
            amelieVariation = margotVariation = 0;
        }

        public void PlayGameOver()
        {
            if (gameOverStarted || gameOver == null) return;
            FindFirstObjectByType<Phase3ADiaryIntro>()?.Skip();
            FindFirstObjectByType<Phase3BMissingStudents>()?.SkipMonologue();
            StopVoice(); StopSfx();
            gameOverStarted = true;
            gameOverSource.clip = gameOver;
            gameOverSource.Play();
            GameOverCount++;
        }
        public void StopGameOver()
        {
            if (gameOverSource != null) gameOverSource.Stop();
            gameOverStarted = false;
        }

        public void QueuePrefectIncident(bool serious)
        {
            pendingPrefectIncident = serious ? prefectFoundBody : prefectDetectSomething;
        }

        private void PlayVoice(AudioClip clip, float minimumPitch, float maximumPitch)
        {
            LastVoiceSegmentStart = LastVoiceSegmentEnd = 0;
            voiceSource.clip = clip;
            voiceSource.pitch = 1f;
            voiceSource.volume = voiceVolume * Gain(clip);
            voiceSource.Play();
            LastVoiceClip = clip;
            lastVoiceAt = Time.time;
        }

        private void PlaySegment(AudioClip clip, float start, float end)
        {
            if (end > clip.length || end <= start) return;
            voiceSource.clip = clip;
            voiceSource.pitch = 1f;
            voiceSource.volume = voiceVolume * Gain(clip);
            voiceSource.time = start;
            segmentEndSeconds = end;
            segmentStopAt = Time.unscaledTime + end - start;
            voiceSource.Play();
            LastVoiceClip = clip;
            LastVoiceSegmentStart = start;
            LastVoiceSegmentEnd = end;
            lastVoiceAt = Time.time;
        }

        private float Gain(AudioClip clip)
        {
            if (clipGains.TryGetValue(clip, out float gain)) return gain;
            gain = 1f;
            if (clip.length <= 22f && clip.loadState == AudioDataLoadState.Loaded)
            {
                var samples = new float[clip.samples * clip.channels];
                if (clip.GetData(samples, 0))
                {
                    double energy = 0; int count = 0; float peak = 0;
                    foreach (float sample in samples)
                    {
                        float level = Mathf.Abs(sample); peak = Mathf.Max(peak, level);
                        if (level < .01f) continue;
                        energy += sample * sample; count++;
                    }
                    if (count > 0)
                        gain = Mathf.Min(Mathf.Clamp(.12f / Mathf.Sqrt((float)(energy / count)), .65f, 1.4f),
                            peak > .001f ? .9f / peak : 1f);
                }
            }
            clipGains[clip] = gain;
            return gain;
        }

        private AudioClip Select(string speaker, string line)
        {
            if (string.IsNullOrEmpty(speaker)) return null;
            line ??= "";
            var state = GameManager.Instance != null ? GameManager.Instance.State : GameState.Introduction;
            var stage = story != null ? story.Stage : Phase2AStage.NotStarted;
            if (speaker.StartsWith("EVELYN"))
            {
                if (state == GameState.Phase2A && stage == Phase2AStage.Confront)
                    return line == "Answer me." || line == "You don't deserve him." ? evelynScared : evelynCalm;
                if (line == "Y-yes. Thank you." || line == "M-me?" || line == "Yes." ||
                    line == "Then we’re the same." || line.Contains("Adrian would never")) return evelynShy;
                if (line == "You're welcome." || line == "I'm glad it found its way back." ||
                    line == "Here you are.") return evelynNice;
                if (line == "What are you saying?" || line == "You… knew?") return evelynScared;
                return evelynCalm;
            }
            if (speaker.StartsWith("ADRIAN"))
            {
                if (line == "Be careful, Evelyn." || line == "I’ve been looking for you." ||
                    line == "You did it for me." || line == "And I did it for you." ||
                    line == "Perhaps we always were.") return adrianSoft;
                return line.Contains("?") ? adrianQuestion : adrianCalm;
            }
            if (speaker.StartsWith("CELESTE"))
            {
                if (stage == Phase2AStage.Confront)
                    return line == "What?" || line == "Evelyn, you're scaring me." ? celesteScared : celesteIrritated;
                if (line == "Sorry, Evelyn. We don't really know each other.") return celesteIrritated;
                return celesteFriendly;
            }
            if (speaker.StartsWith("ISABELLE"))
            {
                if (line.Contains("disappeared") || line.Contains("Lucien") ||
                    line.Contains("Celeste?") || line.Contains("answered anyone") ||
                    line.Contains("keep looking")) return isabelleWorried;
                return isabelleOrdinary;
            }
            if (speaker.StartsWith("AMELIE")) return amelieVoice;
            if (speaker.StartsWith("MARGOT")) return margotVoice;
            if (speaker.StartsWith("PREFECT"))
            {
                if (pendingPrefectIncident != null)
                {
                    var incident = pendingPrefectIncident;
                    pendingPrefectIncident = null;
                    return incident;
                }
                return line.Contains("what have you done") || line.Contains("blood") ? prefectPoint : prefectCalm;
            }
            return null;
        }
    }
}
