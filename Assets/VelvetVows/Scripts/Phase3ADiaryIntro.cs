using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VelvetVows
{
    // The art stays as imported sprites; this component only builds the presentation UI.
    public sealed class Phase3ADiaryIntro : MonoBehaviour
    {
        public Sprite adrianPolaroid;
        public Sprite evelynDiary;
        [Header("Finished three-photograph diary composition")]
        public Sprite storyComposition;
        [Tooltip("Original Japanese narration. Only verified short ranges may play.")]
        public AudioClip unverifiedJapaneseNarration;
        public JapaneseSegmentRange historySegment = new JapaneseSegmentRange();
        public JapaneseSegmentRange loveSegment = new JapaneseSegmentRange();
        public JapaneseSegmentRange diarySegment = new JapaneseSegmentRange();

        public bool IsPlaying { get; private set; }
        public string CurrentBeat { get; private set; } = "";
        public float Elapsed { get; private set; }

        private CanvasGroup group;
        private Image photograph, diary, wash, backdrop;
        private RawImage childhoodPhoto;
        private RectTransform photoRect, diaryRect;
        private Text heading, narration, caption, subtitle, credit, prompt;
        private Button skipButton;
        private bool skipRequested;
        private bool continueRequested;
        private int lastAdvanceFrame = -1;
        private int visual;
        private float visualTime;
        private Font serif;
        private JapaneseSegmentPlayer japaneseSegments;
        private static readonly Color Ivory = new Color(.95f, .87f, .76f);
        private static readonly Color Muted = new Color(.76f, .62f, .59f);

        private void Awake()
        {
            serif = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Baskerville" }, 28);
            if (serif == null) serif = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            japaneseSegments = gameObject.AddComponent<JapaneseSegmentPlayer>();
            BuildCanvas();
        }

        public IEnumerator Play()
        {
            if (IsPlaying) yield break;
            IsPlaying = true;
            skipRequested = false;
            Elapsed = 0;
            visual = 0;
            visualTime = 0;
            group.gameObject.SetActive(true);
            group.alpha = 0;
            group.interactable = group.blocksRaycasts = true;
            skipButton.interactable = true;
            DialogueManager.EnsureUiInput();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            photograph.gameObject.SetActive(false);
            diary.gameObject.SetActive(false);
            wash.gameObject.SetActive(false);
            childhoodPhoto.gameObject.SetActive(false);
            caption.text = subtitle.text = credit.text = narration.text = "";
            heading.text = "EVELYN MOREAU";
            CurrentBeat = "BEFORE HIM";
            yield return Fade(0, 1, .6f);
            yield return Hold(1f);
            heading.text = "";
            japaneseSegments.TryPlay(unverifiedJapaneseNarration, historySegment);
            float childhoodStart = Time.unscaledTime;
            yield return ChildhoodBeat("THE LAST HAPPY SUMMER", 0, new[] {
                "Once, I had everything.", "A home. A family. People who loved me.", "I thought it would last forever." });
            yield return ChildhoodBeat("THE GIRL NOBODY WANTED", 1, new[] {
                "Then the accident took everything away.", "No family. No one waiting for me.", "At the academy, I was nobody." });
            yield return ChildhoodBeat("THE FIRST PERSON WHO STAYED", 2, new[] {
                "Until one day…", "Someone finally saw me." });
            // The 80–106 second recording remains continuous across all three photographs.
            while (!skipRequested && Time.unscaledTime - childhoodStart < 26f) yield return null;
            childhoodPhoto.gameObject.SetActive(false);
            narration.text = ""; heading.text = "UNTIL HIM.";
            if (!skipRequested) yield return Fade(1, 0, .45f);

            if (!skipRequested)
            {
                CurrentBeat = "ADRIAN VALMONT";
                japaneseSegments.TryPlay(unverifiedJapaneseNarration, loveSegment);
                heading.text = "ADRIAN VALMONT";
                photograph.gameObject.SetActive(true);
                visual = 1; visualTime = 0;
                // This caption is already printed in the supplied photograph.
                caption.text = "";
                yield return Fade(0, 1, .55f);
                yield return Lines(new[] {
                    "Adrian Valmont.",
                    "The heir to one of the wealthiest families in the country.",
                    "Brilliant. Elegant. An exceptional fencer.",
                    "Everyone at the academy adored him.",
                    "And somehow…",
                    "He noticed me."
                }, 1.8f, 2.35f);
                yield return Fade(1, 0, .55f);
            }

            if (!skipRequested)
            {
                CurrentBeat = "DIARY";
                japaneseSegments.TryPlay(unverifiedJapaneseNarration, diarySegment);
                photograph.gameObject.SetActive(false);
                diary.gameObject.SetActive(true);
                visual = 2; visualTime = 0;
                heading.text = ""; caption.text = "";
                yield return Fade(0, 1, .6f);
                yield return Lines(new[] {
                    "At first, I only wanted to see him again.",
                    "Then I wanted to know everything about him.",
                    "His favourite places. His habits. The things he left behind.",
                    "I kept them safe."
                }, 2f);
                narration.text = "";
                CurrentBeat = "MY LITTLE COLLECTION";
                heading.text = "MY LITTLE COLLECTION";
                yield return Hold(4.5f);
            }

            if (!skipRequested)
            {
                CurrentBeat = "OBSESSION";
                heading.text = "";
                wash.gameObject.SetActive(true);
                yield return Lines(new[] {
                    "Because one day, he’ll understand.",
                    "We belong together."
                }, 1.4f, 1.9f);
                yield return Fade(1, 0, .65f);
            }

            if (!skipRequested)
            {
                CurrentBeat = "TITLE";
                visual = 0;
                photograph.gameObject.SetActive(false);
                diary.gameObject.SetActive(false);
                wash.gameObject.SetActive(false);
                heading.text = "VELVET VOWS";
                narration.text = "";
                subtitle.text = "Love is a beautiful obsession.";
                credit.text = "Created by Azhar";
                yield return Fade(0, 1, .55f);
                yield return Hold(2.3f);
                yield return Fade(1, 0, .65f);
            }
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.gameObject.SetActive(false);
            japaneseSegments.Stop();
            IsPlaying = false;
            CurrentBeat = "COMPLETE";
            Debug.Log("[Phase3A] Diary intro completed in " + Elapsed.ToString("F1") + " seconds" +
                      (skipRequested ? " (skipped)" : ""), this);
        }

        private void Update()
        {
            if (!IsPlaying) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { Skip(); return; }
            Elapsed += Time.deltaTime;
            visualTime += Time.deltaTime;
            if (visual == 1)
            {
                photoRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.045f, Mathf.Clamp01(visualTime / 13f));
                photoRect.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-1.2f, .7f, Mathf.Clamp01(visualTime / 13f)));
            }
            else if (visual == 2)
            {
                float amount = Mathf.Clamp01(visualTime / 21f);
                diaryRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.24f, amount);
                diaryRect.anchoredPosition = new Vector2(Mathf.Lerp(0, -115, amount), 0);
            }
        }

        public void Skip()
        {
            if (!IsPlaying) return;
            skipRequested = true;
            japaneseSegments.Stop();
            group.alpha = 0;
            group.blocksRaycasts = false;
            skipButton.interactable = false;
        }

        public IEnumerator RememberedName()
        {
            group.gameObject.SetActive(true);
            group.blocksRaycasts = false;
            group.alpha = 0;
            backdrop.color = new Color(.025f, .013f, .022f, .67f);
            photograph.gameObject.SetActive(false);
            diary.gameObject.SetActive(false);
            childhoodPhoto.gameObject.SetActive(false);
            wash.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            narration.text = caption.text = subtitle.text = credit.text = prompt.text = "";
            heading.rectTransform.anchorMin = new Vector2(.08f, .36f);
            heading.rectTransform.anchorMax = new Vector2(.92f, .64f);
            heading.supportRichText = true;
            heading.text = "He remembered <color=#761B32>MY NAME</color>.";
            for (float t = 0; t < .3f; t += Time.unscaledDeltaTime)
            { group.alpha = Mathf.Clamp01(t / .3f); yield return null; }
            group.alpha = 1;
            for (float t = 0; t < 1.5f; t += Time.unscaledDeltaTime)
            { heading.rectTransform.localScale = Vector3.one * Mathf.Lerp(1, 1.12f, Mathf.SmoothStep(0, 1, t / 1.5f)); yield return null; }
            for (float t = 0; t < .25f; t += Time.unscaledDeltaTime)
            { group.alpha = 1 - Mathf.Clamp01(t / .25f); yield return null; }
            group.gameObject.SetActive(false);
            heading.rectTransform.localScale = Vector3.one;
            heading.rectTransform.anchorMin = new Vector2(.07f, .72f);
            heading.rectTransform.anchorMax = new Vector2(.93f, .92f);
            backdrop.color = new Color(.025f, .013f, .022f, 1);
            skipButton.gameObject.SetActive(true);
            prompt.text = "E / SPACE  ·  CONTINUE";
        }

        private IEnumerator Lines(string[] lines, float seconds, float lastSeconds = -1)
        {
            for (int i = 0; i < lines.Length && !skipRequested; i++)
            {
                narration.text = lines[i];
                yield return Hold(i == lines.Length - 1 && lastSeconds > 0 ? lastSeconds : seconds);
            }
        }

        private IEnumerator ChildhoodBeat(string beat, int photoIndex, string[] words)
        {
            if (skipRequested) yield break;
            CurrentBeat = beat;
            heading.text = beat;
            if (childhoodPhoto.gameObject.activeSelf)
            {
                childhoodPhoto.CrossFadeAlpha(0, .3f, true);
                yield return new WaitForSecondsRealtime(.3f);
            }
            if (skipRequested) yield break;
            childhoodPhoto.gameObject.SetActive(storyComposition != null);
            if (storyComposition != null)
            {
                childhoodPhoto.texture = storyComposition.texture;
                childhoodPhoto.uvRect = new Rect(photoIndex / 3f, 0, 1f / 3f, 1);
                childhoodPhoto.canvasRenderer.SetAlpha(0);
                childhoodPhoto.CrossFadeAlpha(1, .35f, true);
            }
            yield return Lines(words, 2.1f);
        }

        private IEnumerator Hold(float seconds)
        {
            float started = Time.unscaledTime;
            int startedFrame = Time.frameCount;
            while (!skipRequested && Time.unscaledTime - started < seconds)
            {
                bool key = Keyboard.current != null &&
                    (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
                if (Time.frameCount > startedFrame && Time.frameCount != lastAdvanceFrame &&
                    (key || continueRequested))
                { lastAdvanceFrame = Time.frameCount; continueRequested = false; break; }
                yield return null;
            }
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            float elapsed = 0;
            while (!skipRequested && elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }
            if (!skipRequested) group.alpha = to;
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("Evelyn Secret Diary - cinematic canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            group = canvasObject.GetComponent<CanvasGroup>();
            backdrop = Panel("Black burgundy backdrop", canvasObject.transform, new Color(.025f, .013f, .022f, 1),
                Vector2.zero, Vector2.one);
            var childhoodObject = new GameObject("Diary photograph crop", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            childhoodObject.transform.SetParent(canvasObject.transform, false);
            childhoodPhoto = childhoodObject.GetComponent<RawImage>();
            childhoodPhoto.raycastTarget = false;
            var cropRect = childhoodPhoto.rectTransform;
            cropRect.anchorMin = cropRect.anchorMax = new Vector2(.5f, .51f);
            // The source is 1774 × 887; each third has a 2:3 aspect ratio.
            cropRect.sizeDelta = new Vector2(600, 900);
            cropRect.anchoredPosition = Vector2.zero;
            childhoodPhoto.gameObject.SetActive(false);
            photograph = Picture("Adrian Polaroid", canvasObject.transform, adrianPolaroid,
                new Vector2(.20f, .13f), new Vector2(.80f, .91f));
            photoRect = photograph.rectTransform;
            diary = Picture("Evelyn diary", canvasObject.transform, evelynDiary,
                new Vector2(.09f, .10f), new Vector2(.91f, .94f));
            diaryRect = diary.rectTransform;
            wash = Panel("Subtle burgundy wash", canvasObject.transform,
                new Color(.28f, .015f, .07f, .15f), Vector2.zero, Vector2.one);
            Panel("Narration gradient backdrop", canvasObject.transform,
                new Color(.035f, .012f, .026f, .88f), new Vector2(0, 0), new Vector2(1, .25f));
            heading = Label("Scene heading", canvasObject.transform, 60, Ivory,
                new Vector2(.07f, .72f), new Vector2(.93f, .92f));
            narration = Label("Evelyn narration", canvasObject.transform, 39, Ivory,
                new Vector2(.09f, .055f), new Vector2(.91f, .205f));
            caption = Label("Polaroid caption", canvasObject.transform, 25, Muted,
                new Vector2(.52f, .28f), new Vector2(.92f, .41f));
            subtitle = Label("Title subtitle", canvasObject.transform, 28, Ivory,
                new Vector2(.1f, .39f), new Vector2(.9f, .49f));
            credit = Label("Title credit", canvasObject.transform, 21, Muted,
                new Vector2(.1f, .32f), new Vector2(.9f, .39f));
            prompt = Label("Advance hint", canvasObject.transform, 20, Muted,
                new Vector2(.72f, .015f), new Vector2(.92f, .055f));
            prompt.text = "E / SPACE  ·  CONTINUE";
            var advance = new GameObject("CONTINUE", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            advance.transform.SetParent(canvasObject.transform, false);
            var advanceRect = advance.GetComponent<RectTransform>();
            advanceRect.anchorMin = new Vector2(.72f, .005f); advanceRect.anchorMax = new Vector2(.94f, .07f);
            advanceRect.offsetMin = advanceRect.offsetMax = Vector2.zero;
            advance.GetComponent<Image>().color = new Color(.16f, .065f, .09f, .25f);
            advance.GetComponent<Button>().onClick.AddListener(() => continueRequested = true);
            var skip = new GameObject("SKIP INTRO", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            skip.transform.SetParent(canvasObject.transform, false);
            var rect = skip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.80f, .92f); rect.anchorMax = new Vector2(.97f, .98f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            skip.GetComponent<Image>().color = new Color(.16f, .065f, .09f, .94f);
            skipButton = skip.GetComponent<Button>();
            skipButton.onClick.AddListener(Skip);
            var skipText = Label("SKIP INTRO label", skip.transform, 23, Ivory, Vector2.zero, Vector2.one);
            skipText.text = "SKIP INTRO";
            canvasObject.SetActive(false);
        }

        private static Image Panel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return image;
        }

        private static Image Picture(string name, Transform parent, Sprite sprite, Vector2 min, Vector2 max)
        {
            var image = Panel(name, parent, Color.white, min, max);
            image.sprite = sprite;
            image.preserveAspect = true;
            if (sprite == null) Debug.LogError("Phase3A image missing: " + name);
            return image;
        }

        private Text Label(string name, Transform parent, int size, Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = go.GetComponent<Text>();
            label.font = serif; label.fontSize = size; label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }
    }
}
