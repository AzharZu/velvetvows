using UnityEngine;

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
        public float VoiceRemainingSeconds => VoicePlaying ? Mathf.Max(0,
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
        private int celesteVariation;
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
            if (segmentEndSeconds > 0 && VoicePlaying &&
                (voiceSource.time >= segmentEndSeconds || Time.unscaledTime >= segmentStopAt)) StopVoice();
            if (laughUntil > 0 && Time.time >= laughUntil)
            {
                laughUntil = 0;
                StopVoice();
            }
        }

        public void PlayDialogue(string speaker, string line)
        {
            // A new subtitle always owns the one voice channel, even if it is silent.
            StopVoice();
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
            if (clip != prefectDetectSomething && clip != prefectFoundBody &&
                clip == LastVoiceClip && Time.time - lastVoiceAt < 2.4f) return;
            PlayVoice(clip, speaker != null && speaker.StartsWith("ADRIAN") ? .985f : .96f,
                speaker != null && speaker.StartsWith("ADRIAN") ? 1.015f : 1.04f);
        }

        public void PlayAdrianThought()
        {
            StopVoice();
            if (adrianCalm != null && (LastVoiceClip != adrianCalm || Time.time - lastVoiceAt >= 2.4f))
                PlayVoice(adrianCalm, .985f, 1.015f);
        }

        public void PlayPostKillLaugh()
        {
            if (evelynPostKillLaugh == null) return;
            StopVoice();
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
            laughUntil = 0;
            segmentEndSeconds = 0;
            if (voiceSource != null) voiceSource.Stop();
        }
        public void StopSfx() { if (sfxSource != null) sfxSource.Stop(); }
        public void StopAll()
        { StopVoice(); StopSfx(); StopGameOver(); pendingPrefectIncident = null; }

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
            voiceSource.pitch = Random.Range(minimumPitch, maximumPitch);
            voiceSource.volume = voiceVolume;
            voiceSource.Play();
            LastVoiceClip = clip;
            lastVoiceAt = Time.time;
        }

        private void PlaySegment(AudioClip clip, float start, float end)
        {
            if (end > clip.length || end <= start) return;
            voiceSource.clip = clip;
            voiceSource.pitch = 1f;
            voiceSource.volume = voiceVolume;
            voiceSource.time = start;
            segmentEndSeconds = end;
            segmentStopAt = Time.unscaledTime + end - start;
            voiceSource.Play();
            LastVoiceClip = clip;
            LastVoiceSegmentStart = start;
            LastVoiceSegmentEnd = end;
            lastVoiceAt = Time.time;
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
                // Both recordings belong to Celeste; alternating complete takes avoids clipped syllables.
                return celesteVariation++ % 2 == 0 ? celesteFriendly : celesteIrritated;
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
