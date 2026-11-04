using UnityEngine;
using UnityEngine.UI;

namespace VelvetVows
{
    public sealed class Phase2ALovesick : MonoBehaviour
    {
        [SerializeField] private Transform player, adrian;
        [SerializeField] private Image tint;
        [SerializeField] private Text thought;
        [SerializeField, Range(2f, 9f)] private float radius = 4.5f;
        [SerializeField, Range(0f, .25f)] private float intensity = .075f;
        [SerializeField, Range(3f, 30f)] private float thoughtCooldown = 11f;
        private float nextThought, thoughtExpiry, forcedUntil, alpha;
        private int thoughtIndex;
        private static readonly string[] Thoughts = { "He's right there…", "Don't embarrass yourself.", "He would never choose someone like me." };
        public bool Suppressed { get; set; }
        public bool CinematicPulse { get; set; }
        public bool HideCinematicThoughts { get; set; }
        public void Resolve()
        {
            CinematicPulse = false;
            HideCinematicThoughts = false;
            Suppressed = true;
            forcedUntil = 0;
            if (thought != null) thought.text = "";
            thoughtExpiry = 0;
        }
        public void Configure(Transform evelyn, Transform adrianActor, Image overlay, Text thoughtLabel)
        { player = evelyn; adrian = adrianActor; tint = overlay; thought = thoughtLabel; }
        public void Pulse(float seconds) => forcedUntil = Time.time + seconds;
        private void Update()
        {
            if (player == null || adrian == null || tint == null) return;
            bool near = Vector3.Distance(player.position, adrian.position) <= radius;
            bool active = !Suppressed && (near || Time.time < forcedUntil) && (!DialogueManager.IsOpen || CinematicPulse);
            float target = active ? intensity * (.8f + .2f * Mathf.Sin(Time.time * 2.4f)) : 0;
            alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime * .18f);
            var color = tint.color; color.a = alpha; tint.color = color;
            if (!active)
            {
                if (thought != null && thoughtExpiry > 0) thought.text = "";
                thoughtExpiry = 0;
                return;
            }
            if (HideCinematicThoughts && thought != null)
            { thought.text = ""; thoughtExpiry = 0; }
            if (near && !HideCinematicThoughts && Time.time >= nextThought)
            {
                thought.text = Thoughts[thoughtIndex++ % Thoughts.Length];
                nextThought = Time.time + thoughtCooldown;
                thoughtExpiry = Time.time + 3.8f;
            }
            if (thought != null && thoughtExpiry > 0 && Time.time > thoughtExpiry)
            { thought.text = ""; thoughtExpiry = 0; }
        }
    }
}
