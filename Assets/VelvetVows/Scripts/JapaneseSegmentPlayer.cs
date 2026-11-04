using System;
using UnityEngine;

namespace VelvetVows
{
    [Serializable]
    public sealed class JapaneseSegmentRange
    {
        [Tooltip("Enable only for approved timing. English story captions are not asserted translations.")]
        public bool timingApproved;
        public float startSeconds;
        public float endSeconds;
    }

    // Plays a verified excerpt without editing or duplicating the source MP3.
    public sealed class JapaneseSegmentPlayer : MonoBehaviour
    {
        public bool IsPlaying => source != null && source.isPlaying;
        public int StartedSegments { get; private set; }
        private AudioSource source;
        private float endSeconds;
        private float stopAt;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
        }

        public bool TryPlay(AudioClip clip, JapaneseSegmentRange range)
        {
            Stop();
            if (clip == null || range == null || !range.timingApproved ||
                range.startSeconds < 0 || range.endSeconds <= range.startSeconds ||
                range.endSeconds > clip.length) return false;
            source.clip = clip;
            source.time = range.startSeconds;
            endSeconds = range.endSeconds;
            stopAt = Time.unscaledTime + range.endSeconds - range.startSeconds;
            source.Play();
            StartedSegments++;
            return true;
        }

        public void Stop()
        {
            if (source != null) source.Stop();
        }

        private void Update()
        {
            if (IsPlaying && (source.time >= endSeconds || Time.unscaledTime >= stopAt)) Stop();
        }
    }
}
