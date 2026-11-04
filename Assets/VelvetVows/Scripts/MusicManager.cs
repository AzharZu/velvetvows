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

        public void Configure(AudioClip normal, AudioClip tense, AudioClip ending)
        { exploration = normal; investigation = tense; finale = ending; }

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0;
            source.volume = volume;
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
            float start = source.volume;
            for (float elapsed = 0; elapsed < 2f; elapsed += Time.unscaledDeltaTime)
            { source.volume = Mathf.Lerp(start, 0, elapsed / 2f); yield return null; }
            source.Stop();
            source.volume = volume;
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
            float start = source.volume;
            for (float elapsed = 0; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, 0, elapsed / Mathf.Max(.01f, fadeSeconds));
                yield return null;
            }
            source.Stop();
            source.clip = clip;
            source.Play();
            for (float elapsed = 0; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(0, volume, elapsed / Mathf.Max(.01f, fadeSeconds));
                yield return null;
            }
            source.volume = volume;
            transition = null;
        }
    }
}
