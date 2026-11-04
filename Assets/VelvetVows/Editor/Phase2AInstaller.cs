using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VelvetVows.Editor
{
    public static class Phase2AInstaller
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string BackupPath = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase2A.unity";
        private const string AnimRoot = "Assets/Kevin Iglesias/Human Animations/Animations/";
        private static readonly Color Ivory = new Color(.94f, .87f, .75f);
        private static readonly Color Rose = new Color(.75f, .35f, .46f);
        private static readonly Color Deep = new Color(.09f, .07f, .12f, .95f);
        private static Font font;

        [MenuItem("Velvet Vows/Install Phase 2A Story (Preserve Gothic World)")]
        public static void Install()
        {
            if (!System.IO.File.Exists(BackupPath)) throw new Exception("Create the pre Phase 2A backup first.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var old = GameObject.Find("PHASE 2A STORY");
            if (old != null) throw new Exception("Phase 2A already installed. Refusing to duplicate the story.");
            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
            var dialogue = UnityEngine.Object.FindFirstObjectByType<DialogueManager>();
            if (game == null || hud == null || dialogue == null) throw new Exception("Existing gameplay/HUD/dialogue system missing.");
            var core = game.transform;
            var player = game.Player;
            if (player == null) throw new Exception("Existing player missing.");
            var celeste = Find("Velvet Vows Main/SYSTEMS/Core Gameplay/Gameplay Actors/Celeste - story route");
            var adrian = Find("Velvet Vows Main/CHARACTERS/Adrian Valmont - ballroom");
            var isabelle = Find("Velvet Vows Main/CHARACTERS/AmbientNPCs/Isabelle");
            var amelie = Find("Velvet Vows Main/CHARACTERS/AmbientNPCs/Amelie");
            var clara = Find("Velvet Vows Main/CHARACTERS/AmbientNPCs/Clara");
            var margot = Find("Velvet Vows Main/CHARACTERS/AmbientNPCs/Margot");
            var odette = Find("Velvet Vows Main/CHARACTERS/AmbientNPCs/Odette");
            var prefect = Find("Velvet Vows Main/CHARACTERS/AmbientNPCs/Lucien's classmate");
            var canvas = hud.GetComponent<Canvas>();
            font = Find("Gameplay Canvas/Gameplay HUD/Objective").GetComponent<Text>().font;
            var dialoguePanel = Find("Gameplay Canvas/World Dialogue Panel").GetComponent<RectTransform>();
            dialoguePanel.anchorMin = new Vector2(.15f, .03f);
            dialoguePanel.anchorMax = new Vector2(.85f, .27f);
            dialoguePanel.offsetMin = Vector2.zero;
            dialoguePanel.offsetMax = Vector2.zero;

            var root = new GameObject("PHASE 2A STORY");
            root.transform.SetParent(core, false);
            var story = core.gameObject.AddComponent<Phase2AStoryManager>();
            var reputation = core.gameObject.AddComponent<Phase2AReputation>();
            var lovesick = core.gameObject.AddComponent<Phase2ALovesick>();
            game.ConfigurePhase2A(story);
            reputation.Configure(hud);
            story.game = game; story.hud = hud; story.dialogue = dialogue;
            story.reputation = reputation; story.lovesick = lovesick;
            story.player = player; story.celeste = celeste; story.adrian = adrian;
            story.isabelle = isabelle; story.amelie = amelie; story.clara = clara;
            story.margot = margot; story.odette = odette; story.witness = margot;

            // The imported character models and existing Humanoid avatars stay in place.
            story.evelynMotion = Motion(player, true);
            story.evelynMotion.enabled = false;
            story.celesteMotion = Motion(celeste, true);
            story.adrianMotion = Motion(adrian, false);
            story.isabelleMotion = Motion(isabelle, true);
            story.amelieMotion = Motion(amelie, true);
            story.witnessMotion = Motion(margot, true);
            Motion(clara, true);
            Motion(odette, true);
            adrian.SetPositionAndRotation(new Vector3(-22f, .09f, 15f), Quaternion.Euler(0, 90, 0));
            odette.SetPositionAndRotation(new Vector3(-13.2f, 0, 15.2f), Quaternion.Euler(0, 180, 0));
            celeste.SetPositionAndRotation(new Vector3(-5f, 0, 3.4f), Quaternion.Euler(0, 90, 0));
            isabelle.SetPositionAndRotation(new Vector3(-1.1f, 0, -1.3f), Quaternion.Euler(0, 90, 0));
            amelie.SetPositionAndRotation(new Vector3(1.2f, 0, -1.2f), Quaternion.Euler(0, -90, 0));
            var oldCelesteRoute = celeste.GetComponent<CelesteSequence>();
            if (oldCelesteRoute != null) oldCelesteRoute.enabled = false;
            foreach (var oldTalk in new[] { celeste.GetComponent<DialogueTrigger>(), isabelle.GetComponent<DialogueTrigger>(), adrian.GetComponent<DialogueTrigger>() })
                if (oldTalk != null) oldTalk.enabled = false;
            foreach (var evidence in UnityEngine.Object.FindObjectsByType<EvidenceItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                evidence.enabled = false;
            var oldStudio = UnityEngine.Object.FindFirstObjectByType<StudioEventTrigger>();
            if (oldStudio != null) oldStudio.enabled = false;

            var celesteCapsule = celeste.GetComponent<CapsuleCollider>();
            if (celesteCapsule != null) celesteCapsule.isTrigger = true;
            var controller = celeste.gameObject.AddComponent<CharacterController>();
            controller.radius = .22f; controller.height = 1.6f; controller.center = new Vector3(0, .8f, 0);
            controller.skinWidth = .04f; controller.stepOffset = .25f;
            story.follower = celeste.gameObject.AddComponent<Phase2AFollower>();
            story.follower.Configure(player, story.celesteMotion);

            // Existing NPC routes remain; only the old random walker on staged actors is disabled.
            var ambientPrefect = prefect.GetComponent<AmbientNPC>();
            if (ambientPrefect != null) ambientPrefect.enabled = false;
            prefect.name = "Prefect - Lucien's classmate";
            prefect.SetPositionAndRotation(new Vector3(5f, 0, 4f), Quaternion.Euler(0, 180, 0));
            var patrol = prefect.gameObject.AddComponent<PatrolNPC>();
            var patrolPoints = new List<Transform>();
            foreach (var p in new[] { new Vector3(5f, 0, 4f), new Vector3(5f, 0, -5f), new Vector3(3f, 0, -5f), new Vector3(3f, 0, 4f) })
            {
                var wp = new GameObject("Prefect patrol point").transform;
                wp.SetParent(root.transform); wp.position = p; patrolPoints.Add(wp);
            }
            patrol.Configure(patrolPoints, prefect.GetComponentInChildren<Animator>());

            // Real books and in-world task objects. Only the story props are added.
            var simpleBook = RequirePrefab("Assets/VNB - Home Set/Prefabs/Decoration/Simple Book.prefab");
            var books = RequirePrefab("Assets/VNB - Home Set/Prefabs/Decoration/Books I.prefab");
            var openBook = RequirePrefab("Assets/VNB - Home Set/Prefabs/Decoration/Book (Open).prefab");
            story.openingBooks = HeldBook(simpleBook, player.GetComponentInChildren<Animator>(), HumanBodyBones.LeftHand, "Evelyn's carried book", scene);
            story.adrianHeldBook = HeldBook(simpleBook, adrian.GetComponentInChildren<Animator>(), HumanBodyBones.RightHand, "Adrian holds Evelyn's book", scene);
            story.libraryBooks = PrefabProp(books, "Clara's returned books", new Vector3(-23.1f, .8f, 18f), .5f, root.transform, scene);
            AddInteraction(story.libraryBooks, story, Phase2AAction.BooksPickup, .85f);
            AddInteraction(clara.gameObject, story, Phase2AAction.BooksDelivery);
            story.artMaterials = CreateArtMaterials(root.transform);
            AddInteraction(story.artMaterials, story, Phase2AAction.ArtPickup, .85f);
            AddInteraction(margot.gameObject, story, Phase2AAction.ArtDelivery);
            story.lostPendant = CreatePendant(root.transform);
            AddInteraction(story.lostPendant, story, Phase2AAction.LostPickup, .8f);
            AddInteraction(odette.gameObject, story, Phase2AAction.LostDelivery);
            story.blackRibbon = CreateRibbon(root.transform);
            AddInteraction(story.blackRibbon, story, Phase2AAction.RibbonPickup, .85f);
            story.sketchbook = PrefabProp(openBook, "A. V. sketchbook", new Vector3(-18.4f, .8f, 20.2f), .5f, root.transform, scene);
            AddInteraction(story.sketchbook, story, Phase2AAction.Sketchbook, .85f);
            AddInteraction(celeste.gameObject, story, Phase2AAction.CelesteTalk);
            AddInteraction(celeste.gameObject, story, Phase2AAction.RibbonReturn);
            AddInteraction(celeste.gameObject, story, Phase2AAction.Confront);
            AddInteraction(margot.gameObject, story, Phase2AAction.Witness);
            story.studioDoor = CreateStudioDoor(root.transform, story);

            // One canvas and the existing dialogue controller. New overlays are subordinate to it.
            var tint = Image("Phase 2A rose tint", canvas.transform, new Color(.63f, .07f, .15f, 0), true);
            Stretch(tint.rectTransform);
            tint.raycastTarget = false;
            tint.transform.SetAsFirstSibling();
            var thought = Text("Evelyn's thoughts", canvas.transform, 18, Ivory, TextAnchor.MiddleCenter);
            Box(thought.rectTransform, new Vector2(500, 45), new Vector2(0, 93));
            thought.fontStyle = FontStyle.Italic;
            thought.raycastTarget = false;
            var title = Text("Phase 2A title", canvas.transform, 38, Ivory, TextAnchor.MiddleCenter);
            Box(title.rectTransform, new Vector2(570, 170), Vector2.zero);
            title.raycastTarget = false;
            var page = Panel("Sketchbook pages", canvas.transform, new Vector2(510, 355), Deep);
            var pageTitle = Text("Sketchbook mark", page.transform, 26, Rose, TextAnchor.MiddleLeft);
            Box(pageTitle.rectTransform, new Vector2(450, 55), new Vector2(0, 130));
            var pageBody = Text("Temporary page text", page.transform, 21, Ivory, TextAnchor.UpperLeft);
            Box(pageBody.rectTransform, new Vector2(440, 190), new Vector2(0, 7));
            pageBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            pageBody.verticalOverflow = VerticalWrapMode.Overflow;
            var previous = Button("PREVIOUS", page.transform, new Vector2(120, 34), new Vector2(-150, -143));
            var next = Button("NEXT", page.transform, new Vector2(120, 34), new Vector2(0, -143));
            var close = Button("CLOSE", page.transform, new Vector2(120, 34), new Vector2(150, -143));
            page.SetActive(false);
            var complete = Panel("Phase 2A complete", canvas.transform, new Vector2(560, 230), Deep);
            var completeText = Text("Completion message", complete.transform, 34, Ivory, TextAnchor.MiddleCenter);
            Box(completeText.rectTransform, new Vector2(520, 100), new Vector2(0, 35));
            var restart = Button("RESTART", complete.transform, new Vector2(170, 42), new Vector2(0, -72));
            complete.SetActive(false);
            story.titleText = title; story.thoughtText = thought;
            story.pagePanel = page; story.pageTitle = pageTitle; story.pageBody = pageBody;
            story.pagePrevious = previous; story.pageNext = next; story.pageClose = close;
            story.completionPanel = complete; story.completeText = completeText; story.restartButton = restart;
            lovesick.Configure(player, adrian, tint, thought);
            hud.SetReputation(0, 5);
            hud.SetTimerVisible(false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Could not save Phase 2A scene");
            Debug.Log("[Phase2A] Installed social and story gameplay. Gothic world hierarchy untouched.");
        }

        private static Transform Find(string path)
        {
            var rootName = path.Split('/')[0];
            var root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == rootName);
            if (root == null) throw new Exception("Missing scene root " + rootName);
            if (path == rootName) return root.transform;
            var child = root.transform.Find(path.Substring(rootName.Length + 1));
            if (child == null) throw new Exception("Missing scene object " + path);
            return child;
        }
        private static GameObject RequirePrefab(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new Exception("Missing prefab: " + path);
        private static AnimationClip Clip(string relative, string name)
        {
            var path = AnimRoot + relative;
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name == name)
                ?? throw new Exception("Missing Humanoid clip " + path + " :: " + name);
        }
        private static Phase2AActorMotion Motion(Transform actor, bool female)
        {
            var ambient = actor.GetComponent<AmbientNPC>();
            if (ambient != null) ambient.enabled = false;
            var animator = actor.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid)
                throw new Exception("Invalid Humanoid avatar: " + actor.name);
            var p = female ? "HumanF" : "HumanM";
            var sex = female ? "Female" : "Male";
            var motion = actor.gameObject.AddComponent<Phase2AActorMotion>();
            motion.Configure(animator,
                Clip($"{sex}/Idles/{p}@Idle01.fbx", p + "@Idle01"),
                Clip($"{sex}/Idles/{p}@Idle02.fbx", p + "@Idle02"),
                Clip($"{sex}/Social/Conversation/{p}@Talk01.fbx", p + "@Talk01"),
                Clip($"{sex}/Movement/Walk/{p}@Walk01_Forward.fbx", p + "@Walk01_Forward"));
            return motion;
        }
        private static GameObject PrefabProp(GameObject prefab, string name, Vector3 position, float maxSize, Transform parent, Scene scene)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = name; go.transform.SetParent(parent); go.transform.position = position;
            FitSize(go, maxSize);
            MakeUrpCompatible(go);
            return go;
        }
        private static void MakeUrpCompatible(GameObject go)
        {
            const string directory = "Assets/VelvetVows/Materials/Phase2A";
            System.IO.Directory.CreateDirectory(directory);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null || source.shader == null || source.shader.name != "Standard") continue;
                    var targetPath = directory + "/" + source.name + " URP.mat";
                    var replacement = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(targetPath);
                    if (replacement == null)
                    {
                        var shader = Shader.Find("Universal Render Pipeline/Lit");
                        if (shader == null) throw new Exception("URP Lit shader missing");
                        replacement = new UnityEngine.Material(shader) { name = source.name + " URP" };
                        replacement.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
                        replacement.SetColor("_BaseColor", source.GetColor("_Color"));
                        replacement.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                        replacement.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
                        AssetDatabase.CreateAsset(replacement, targetPath);
                    }
                    materials[i] = replacement;
                    changed = true;
                }
                if (changed)
                {
                    renderer.sharedMaterials = materials;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
            }
        }
        private static void FitSize(GameObject go, float maxSize)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest > .001f) go.transform.localScale *= maxSize / largest;
        }
        private static GameObject HeldBook(GameObject prefab, Animator animator, HumanBodyBones hand, string name, Scene scene)
        {
            var bone = animator.GetBoneTransform(hand);
            if (bone == null) throw new Exception("Humanoid hand missing for " + animator.name);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = name;
            FitSize(go, .27f);
            MakeUrpCompatible(go);
            go.transform.SetParent(bone, false);
            go.transform.localPosition = new Vector3(0, .05f, .07f);
            go.transform.localRotation = Quaternion.Euler(0, 0, 75);
            foreach (var collider in go.GetComponentsInChildren<Collider>()) collider.enabled = false;
            return go;
        }
        private static Phase2AInteractable AddInteraction(GameObject go, Phase2AStoryManager story, Phase2AAction action, float radius = 0)
        {
            if (radius > 0)
            {
                var sphere = go.AddComponent<SphereCollider>();
                sphere.isTrigger = true; sphere.radius = radius;
            }
            else if (go.GetComponent<Collider>() == null) throw new Exception("NPC interaction collider missing: " + go.name);
            var result = go.AddComponent<Phase2AInteractable>();
            result.Configure(story, action);
            return result;
        }
        private static GameObject CreateArtMaterials(Transform parent)
        {
            var go = new GameObject("Margot's art materials");
            go.transform.SetParent(parent); go.transform.position = new Vector3(-20.1f, .08f, -2.45f);
            var paper = Primitive(PrimitiveType.Cube, "Drawing paper", go.transform, new Vector3(0, 0, 0), new Vector3(.29f, .018f, .36f), Material("Assets/VelvetVows/Materials/VisualPolish/Ivory Plaster.mat"));
            paper.transform.localRotation = Quaternion.Euler(0, 20, 0);
            for (int i = 0; i < 2; i++)
            {
                var brush = Primitive(PrimitiveType.Cylinder, "Charcoal stick", go.transform, new Vector3(-.07f + i * .08f, .025f, 0), new Vector3(.009f, .14f, .009f), Material("Assets/VelvetVows/Materials/Charcoal Stone.mat"));
                brush.transform.localRotation = Quaternion.Euler(90, 0, i * 13);
            }
            return go;
        }
        private static GameObject CreatePendant(Transform parent)
        {
            var go = new GameObject("Odette's lost pendant"); go.transform.SetParent(parent);
            go.transform.position = new Vector3(-16.4f, .16f, 11.3f);
            var gold = Material("Assets/VelvetVows/Materials/Aged Brass.mat");
            Primitive(PrimitiveType.Sphere, "Pendant setting", go.transform, Vector3.zero, new Vector3(.16f, .16f, .04f), gold);
            Primitive(PrimitiveType.Cube, "Pendant loop", go.transform, new Vector3(0, .1f, 0), new Vector3(.05f, .08f, .035f), gold);
            return go;
        }
        private static GameObject CreateRibbon(Transform parent)
        {
            var go = new GameObject("Celeste's black ribbon"); go.transform.SetParent(parent);
            go.transform.position = new Vector3(-22.2f, .76f, 19.5f);
            var black = Material("Assets/VelvetVows/Materials/Black Rose.mat");
            Primitive(PrimitiveType.Cube, "Knot", go.transform, Vector3.zero, new Vector3(.08f, .06f, .05f), black);
            for (int i = -1; i <= 1; i += 2)
            {
                var loop = Primitive(PrimitiveType.Cube, "Ribbon loop", go.transform, new Vector3(i * .115f, .02f, 0), new Vector3(.19f, .095f, .025f), black);
                loop.transform.localRotation = Quaternion.Euler(0, 0, i * 25);
                var tail = Primitive(PrimitiveType.Cube, "Ribbon tail", go.transform, new Vector3(i * .08f, -.13f, 0), new Vector3(.055f, .23f, .018f), black);
                tail.transform.localRotation = Quaternion.Euler(0, 0, i * 15);
            }
            return go;
        }
        private static Phase2AStudioDoor CreateStudioDoor(Transform parent, Phase2AStoryManager story)
        {
            var root = new GameObject("Art Studio closing door"); root.transform.SetParent(parent);
            root.transform.position = new Vector3(-12.05f, 0, -3.0f);
            var hinge = new GameObject("Hinge").transform; hinge.SetParent(root.transform, false);
            hinge.localRotation = Quaternion.Euler(0, 95, 0);
            var leaf = Primitive(PrimitiveType.Cube, "Dark walnut door leaf", hinge,
                new Vector3(0, 1.35f, 1f), new Vector3(.16f, 2.7f, 2f), Material("Assets/VelvetVows/Materials/Dark Walnut.mat"));
            var brass = Material("Assets/VelvetVows/Materials/Aged Brass.mat");
            Primitive(PrimitiveType.Cube, "Upper brass inset", leaf.transform, new Vector3(.51f, .2f, 0), new Vector3(.03f, .06f, .85f), brass);
            Primitive(PrimitiveType.Cube, "Lower brass inset", leaf.transform, new Vector3(.51f, -.2f, 0), new Vector3(.03f, .06f, .85f), brass);
            Primitive(PrimitiveType.Sphere, "Brass handle", leaf.transform, new Vector3(.6f, 0, .26f), new Vector3(.22f, .09f, .09f), brass);
            var door = root.AddComponent<Phase2AStudioDoor>(); door.Configure(hinge, leaf.GetComponent<Collider>());
            var interaction = new GameObject("Studio door interaction"); interaction.transform.SetParent(root.transform);
            interaction.transform.position = new Vector3(-13.25f, 1f, -2f);
            AddInteraction(interaction, story, Phase2AAction.StudioDoor, 1.1f);
            return door;
        }
        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = localPosition; go.transform.localScale = localScale;
            var renderer = go.GetComponent<Renderer>(); if (renderer != null && material != null) renderer.sharedMaterial = material;
            var collider = go.GetComponent<Collider>(); if (collider != null && parent.name != "Hinge") collider.enabled = false;
            return go;
        }
        private static Material Material(string path) => AssetDatabase.LoadAssetAtPath<Material>(path) ?? throw new Exception("Missing material " + path);

        private static Image Image(string name, Transform parent, Color color, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var result = go.GetComponent<Image>(); result.color = color; result.raycastTarget = raycast;
            return result;
        }
        private static GameObject Panel(string name, Transform parent, Vector2 size, Color color)
        {
            var image = Image(name, parent, color, true);
            Box(image.rectTransform, size, Vector2.zero);
            return image.gameObject;
        }
        private static Text Text(string name, Transform parent, int size, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>(); t.font = font; t.fontSize = size; t.color = color; t.alignment = align; t.supportRichText = true;
            return t;
        }
        private static Button Button(string caption, Transform parent, Vector2 size, Vector2 position)
        {
            var image = Image(caption + " button", parent, new Color(.27f, .14f, .21f, 1), true);
            Box(image.rectTransform, size, position);
            var button = image.gameObject.AddComponent<Button>();
            var label = Text(caption, image.transform, 16, Ivory, TextAnchor.MiddleCenter);
            label.text = caption;
            Stretch(label.rectTransform);
            label.raycastTarget = false;
            return button;
        }
        private static void Box(RectTransform rect, Vector2 size, Vector2 position)
        { rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = size; rect.anchoredPosition = position; }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
    }
}
