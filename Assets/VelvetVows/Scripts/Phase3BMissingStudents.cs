using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VelvetVows
{
    // One overlay presents the optional Library dossier and Adrian's later internal monologue.
    public sealed class Phase3BMissingStudents : MonoBehaviour
    {
        public Sprite newspaperCollage;
        public GameManager game;
        public GameHUD hud;
        public CharacterVoiceManager voice;
        [Tooltip("Original Japanese monologue. Only the approved short range plays; English captions are not asserted translations.")]
        public AudioClip unverifiedJapaneseNarration;
        public JapaneseSegmentRange missingBoysSegment = new JapaneseSegmentRange();

        public bool DossierOpen { get; private set; }
        public bool IsMonologuePlaying { get; private set; }
        public bool MonologuePlayed { get; private set; }
        public string CurrentThought { get; private set; } = "";
        public int DossierLineIndex { get; private set; }

        private GameObject canvasObject, dossierLayout, monologueLayout;
        private CanvasGroup group;
        private Text dossierText, monologueText;
        private Button closeButton, skipButton;
        private float lastInput, lastClose = -1f;
        private bool skipMonologue;
        private Font serif;
        private JapaneseSegmentPlayer japaneseSegments;
        private static readonly Color Ivory = new Color(.96f, .88f, .77f);
        private static readonly Color Muted = new Color(.77f, .65f, .64f);
        private static readonly string[] EvelynThoughts = {
            "Nathaniel Clarke…",
            "He used to follow me around.",
            "They said he transferred. Without saying goodbye?",
            "Oliver Bennett…",
            "He used to help me with my assignments.",
            "Apparently, he stopped coming to class.",
            "And Lucien…",
            "He asked me out just before he disappeared.",
            "Why does everyone keep leaving?",
            "Adrian would never abandon me."
        };
        private static readonly string[] AdrianThoughts = {
            "Nathaniel Clarke.",
            "Such a persistent fool.",
            "Fortunately, he transferred.",
            "Oliver Bennett.",
            "He mistook her kindness for affection.",
            "How unfortunate that he had to leave.",
            "She has such a beautiful smile.",
            "It’s a shame some people mistake it for an invitation."
        };

        private void Awake()
        {
            serif = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Baskerville" }, 28);
            if (serif == null) serif = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            japaneseSegments = gameObject.AddComponent<JapaneseSegmentPlayer>();
            BuildUI();
        }

        public bool CanOpen(GameManager current) =>
            newspaperCollage != null && current == game && current.State == GameState.Phase2A &&
            !DossierOpen && !DialogueManager.IsOpen && Time.time - lastClose > .4f;

        public void OpenDossier()
        {
            if (!CanOpen(game)) return;
            DossierOpen = true;
            DossierLineIndex = 0;
            lastInput = Time.time;
            dossierText.text = EvelynThoughts[0];
            dossierLayout.SetActive(true);
            monologueLayout.SetActive(false);
            canvasObject.SetActive(true);
            group.alpha = 1;
            game.SetPhase3BDossier(true);
            Debug.Log("[Phase3B] Library dossier opened", this);
        }

        public void CloseDossier()
        {
            if (!DossierOpen) return;
            DossierOpen = false;
            lastClose = Time.time;
            canvasObject.SetActive(false);
            game.SetPhase3BDossier(false);
            Debug.Log("[Phase3B] Library dossier closed", this);
        }

        private void Update()
        {
            if (!DossierOpen || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) { CloseDossier(); return; }
            if (Time.time - lastInput < .3f) return;
            if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                lastInput = Time.time;
                if (++DossierLineIndex >= EvelynThoughts.Length) CloseDossier();
                else dossierText.text = EvelynThoughts[DossierLineIndex];
            }
        }

        public IEnumerator PlayMonologue()
        {
            if (MonologuePlayed || IsMonologuePlaying) yield break;
            MonologuePlayed = true;
            IsMonologuePlaying = true;
            skipMonologue = false;
            dossierLayout.SetActive(false);
            monologueLayout.SetActive(true);
            canvasObject.SetActive(true);
            group.alpha = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            monologueText.text = "";
            yield return Fade(0, 1, .55f);
            bool japanesePlaying = japaneseSegments.TryPlay(unverifiedJapaneseNarration, missingBoysSegment);
            if (japanesePlaying) voice?.StopVoice();
            for (int i = 0; i < AdrianThoughts.Length && !skipMonologue; i++)
            {
                CurrentThought = AdrianThoughts[i];
                monologueText.text = CurrentThought;
                if (!japaneseSegments.IsPlaying && (i == 0 || i == 3 || i == 6)) voice?.PlayAdrianThought();
                yield return Hold(i == AdrianThoughts.Length - 1 ? 2.1f : 1.75f);
                if (i == 2 || i == 5) yield return Hold(.65f);
            }
            while (!skipMonologue && japaneseSegments.IsPlaying) yield return null;
            if (!skipMonologue) yield return Fade(1, 0, .55f);
            group.alpha = 0;
            canvasObject.SetActive(false);
            voice?.StopVoice();
            japaneseSegments.Stop();
            IsMonologuePlaying = false;
            CurrentThought = "COMPLETE";
            Debug.Log("[Phase3B] Adrian monologue completed" + (skipMonologue ? " (skipped)" : ""), this);
        }

        public void SkipMonologue()
        {
            if (!IsMonologuePlaying) return;
            skipMonologue = true;
            voice?.StopVoice();
            japaneseSegments.Stop();
        }

        private IEnumerator Hold(float seconds)
        {
            float until = Time.time + seconds;
            while (!skipMonologue && Time.time < until) yield return null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0;
            while (!skipMonologue && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            group.alpha = to;
        }

        private void BuildUI()
        {
            canvasObject = new GameObject("Missing students cinematic canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            group = canvasObject.GetComponent<CanvasGroup>();
            Panel("Black backdrop", canvasObject.transform, new Color(.022f, .012f, .02f, 1), Vector2.zero, Vector2.one);

            dossierLayout = new GameObject("Library dossier layout", typeof(RectTransform));
            dossierLayout.transform.SetParent(canvasObject.transform, false);
            Stretch(dossierLayout.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Picture("Missing students newspaper collage", dossierLayout.transform, new Vector2(.03f, .09f), new Vector2(.76f, .94f));
            Panel("Thought panel", dossierLayout.transform, new Color(.09f, .035f, .05f, .98f),
                new Vector2(.77f, .11f), new Vector2(.98f, .87f));
            var title = Label("Dossier title", dossierLayout.transform, 30, Ivory,
                new Vector2(.78f, .69f), new Vector2(.97f, .83f));
            title.text = "PRIVATE CLIPPINGS";
            var name = Label("Evelyn thought label", dossierLayout.transform, 20, Muted,
                new Vector2(.79f, .57f), new Vector2(.96f, .64f));
            name.text = "EVELYN · THOUGHT";
            dossierText = Label("Evelyn dossier reaction", dossierLayout.transform, 28, Ivory,
                new Vector2(.79f, .30f), new Vector2(.96f, .57f));
            var hint = Label("Dossier controls", dossierLayout.transform, 19, Muted,
                new Vector2(.78f, .16f), new Vector2(.97f, .24f));
            hint.text = "E / SPACE  ·  CONTINUE\nESC  ·  CLOSE";
            closeButton = MakeButton("CLOSE", dossierLayout.transform, new Vector2(.81f, .90f), new Vector2(.97f, .97f));
            closeButton.onClick.AddListener(CloseDossier);

            monologueLayout = new GameObject("Adrian monologue layout", typeof(RectTransform));
            monologueLayout.transform.SetParent(canvasObject.transform, false);
            Stretch(monologueLayout.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Picture("Clippings behind Adrian thoughts", monologueLayout.transform,
                new Vector2(.12f, .12f), new Vector2(.88f, .93f));
            Panel("Monologue burgundy veil", monologueLayout.transform,
                new Color(.032f, .005f, .016f, .68f), Vector2.zero, Vector2.one);
            var speaker = Label("Adrian internal label", monologueLayout.transform, 25, Muted,
                new Vector2(.2f, .63f), new Vector2(.8f, .72f));
            speaker.text = "ADRIAN · THOUGHT";
            monologueText = Label("Adrian internal words", monologueLayout.transform, 47, Ivory,
                new Vector2(.17f, .35f), new Vector2(.83f, .61f));
            skipButton = MakeButton("SKIP MONOLOGUE", monologueLayout.transform,
                new Vector2(.79f, .91f), new Vector2(.97f, .98f));
            skipButton.onClick.AddListener(SkipMonologue);
            canvasObject.SetActive(false);
        }

        private Image Picture(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var image = Panel(name, parent, Color.white, min, max);
            image.sprite = newspaperCollage;
            image.preserveAspect = true;
            return image;
        }

        private static Image Panel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var image = go.GetComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return image;
        }

        private Text Label(string name, Transform parent, int size, Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var label = go.GetComponent<Text>();
            label.font = serif; label.fontSize = size; label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private Button MakeButton(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            go.GetComponent<Image>().color = new Color(.19f, .065f, .095f, .97f);
            var label = Label(name + " label", go.transform, 22, Ivory, Vector2.zero, Vector2.one);
            label.text = name;
            return go.GetComponent<Button>();
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
