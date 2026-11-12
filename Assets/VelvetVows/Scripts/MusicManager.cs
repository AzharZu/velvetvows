using System.Collections;
using UnityEngine;

namespace VelvetVows
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicManager : MonoBehaviour
    {
        [SerializeField] private AudioClip exploration;
        [SerializeField] private AudioClip investigation;
        [SerializeField] private AudioClip finale;
        [SerializeField, Range(0, 1)] private float volume = .22f;
        [SerializeField, Min(0)] private float fadeSeconds = 1f;
        private AudioSource source;
        private Coroutine transition;
        private float mixVolume, duck = 1;
        private CharacterVoiceManager voice;

        public void Configure(AudioClip normal, AudioClip tense, AudioClip ending)
        { exploration = normal; investigation = tense; finale = ending; }

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0;
            source.volume = volume;
            mixVolume = volume;
            voice = FindFirstObjectByType<CharacterVoiceManager>();
        }

        private void LateUpdate()
        {
            bool speaking = voice != null && voice.VoicePlaying || JapaneseSegmentPlayer.AnyPlaying;
            float target = speaking ? .48f : 1;
            duck = Mathf.MoveTowards(duck, target, Time.unscaledDeltaTime * (speaking ? 3 : .8f));
            source.volume = mixVolume * duck;
        }

        public void PlayExploration() => Change(exploration);
        public void PlayInvestigation() => Change(investigation);
        public void PlayFinale() => Change(finale);
        public void StopForBall()
        {
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(FadeOutForBall());
        }
        private IEnumerator FadeOutForBall()
        {
            float start = mixVolume;
            for (float elapsed = 0; elapsed < 2f; elapsed += Time.unscaledDeltaTime)
            { mixVolume = Mathf.Lerp(start, 0, elapsed / 2f); yield return null; }
            source.Stop();
            mixVolume = volume;
            transition = null;
        }

        private void Change(AudioClip clip)
        {
            if (clip == null || source.clip == clip) return;
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(FadeTo(clip));
        }

        private IEnumerator FadeTo(AudioClip clip)
        {
            float start = mixVolume;
            for (float elapsed = 0; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                mixVolume = Mathf.Lerp(start, 0, elapsed / Mathf.Max(.01f, fadeSeconds));
                yield return null;
            }
            source.Stop();
            source.clip = clip;
            source.Play();
            for (float elapsed = 0; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                mixVolume = Mathf.Lerp(0, volume, elapsed / Mathf.Max(.01f, fadeSeconds));
                yield return null;
            }
            mixVolume = volume;
            transition = null;
        }
    }
}
