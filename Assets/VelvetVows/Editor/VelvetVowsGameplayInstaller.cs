using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StarterAssets;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace VelvetVows.Editor
{
    public static class VelvetVowsGameplayInstaller
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string PrefabPath = "Assets/VelvetVows/Prefabs/";
        private const string Kit = "Assets/Medieval Room Kit/Prefab/";
        private static readonly Color Cream = new Color(.96f, .91f, .78f);
        private static readonly Color Burgundy = new Color(.38f, .08f, .13f);
        private static Font font;

        [MenuItem("Velvet Vows/Install Core Gameplay")]
        public static void Install()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = GameObject.Find("Velvet Vows Main")?.transform ?? throw new InvalidOperationException("Existing main scene root missing");
            var decorativeRecords = root.Find("ENVIRONMENT/RestrictedGallery/Old gallery furnishings/Old academy records");
            if (decorativeRecords != null) decorativeRecords.gameObject.SetActive(false);
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonController>() ?? throw new InvalidOperationException("Accepted player missing");
            var relay = player.GetComponent<AcademyPlayerInputRelay>() ?? throw new InvalidOperationException("Accepted input relay missing");
            if (player.CinemachineCameraTarget == null || UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>() == null)
                throw new InvalidOperationException("Accepted camera setup missing");

            var oldGameplay = root.Find("SYSTEMS/Core Gameplay");
            if (oldGameplay != null) UnityEngine.Object.DestroyImmediate(oldGameplay.gameObject);
            var oldUI = UnityEngine.Object.FindFirstObjectByType<GameHUD>()?.transform;
            if (oldUI != null) UnityEngine.Object.DestroyImmediate(oldUI.gameObject);
            var oldEvents = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            foreach (var system in oldEvents) if (system.name == "Velvet Vows EventSystem") UnityEngine.Object.DestroyImmediate(system.gameObject);
            var oldCeleste = root.Find("CHARACTERS/Celeste - art studio");
            if (oldCeleste != null) UnityEngine.Object.DestroyImmediate(oldCeleste.gameObject);

            var systems = Group("Core Gameplay", root.Find("SYSTEMS"));
            var actors = Group("Gameplay Actors", systems);
            var interactables = Group("Interactables", systems);
            var mechanics = Group("Mechanics", systems);
            var points = Group("Waypoints and Spawn Points", systems);
            var ui = BuildUI(root.Find("UI"), out var hud);
            var game = systems.gameObject.AddComponent<GameManager>();
            var suspicion = mechanics.gameObject.AddComponent<SuspicionSystem>();
            var timer = mechanics.gameObject.AddComponent<GameTimer>();
            var evidence = mechanics.gameObject.AddComponent<EvidenceManager>();
            var spawner = mechanics.gameObject.AddComponent<BlackRoseSpawner>();
            if (player.GetComponent<PlayerInteractor>() == null) player.gameObject.AddComponent<PlayerInteractor>();
            var push = player.GetComponent<BasicRigidBodyPush>();
            if (push == null) throw new InvalidOperationException("Starter Assets BasicRigidBodyPush missing");
            push.canPush = true;
            push.pushLayers = 1;
            push.strength = .5f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(push);

            var celestePrefab = MakeCelestePrefab();
            var celesteObject = (GameObject)PrefabUtility.InstantiatePrefab(celestePrefab, actors);
            celesteObject.name = "Celeste - story route";
            celesteObject.transform.position = new Vector3(-5, 0, -2);
            var celeste = celesteObject.GetComponent<CelesteSequence>();
            var celesteAnimator = celesteObject.GetComponentInChildren<Animator>(true);
            var celesteVisual = celesteObject.transform.GetChild(0);
            celeste.Configure(new List<Transform> {
                Point(points, "Celeste hall", -7, -2), Point(points, "Celeste corridor", -10, -2),
                Point(points, "Celeste studio doorway", -14, -2), Point(points, "Celeste studio", -18, -2)
            }, celesteAnimator, celesteVisual);

            var patrolPrefab = MakePatrolPrefab();
            var patrolObject = (GameObject)PrefabUtility.InstantiatePrefab(patrolPrefab, actors);
            patrolObject.name = "Gallery Patrol";
            patrolObject.transform.position = new Vector3(-2, 0, 17);
            patrolObject.GetComponent<PatrolNPC>().Configure(new List<Transform> {
                Point(points, "Patrol south", -2, 16), Point(points, "Patrol north", -2, 22),
                Point(points, "Patrol east", 1, 22)
            }, patrolObject.GetComponentInChildren<Animator>(true));
            patrolObject.GetComponentInChildren<DetectionZone>().Configure(suspicion);

            var evidencePrefab = MakeEvidencePrefab();
            var evidenceItems = new List<EvidenceItem>();
            string[] labels = { "Lucien's record", "Thomas's record", "Adrian's sketchbook" };
            string[] clues = {
                "Missing: Lucien Morel.\nLast seen after the winter recital.",
                "Missing: Thomas Bell.\nHe had asked Evelyn to the spring exhibition.",
                "Adrian's sketchbook. Pages of drawings of Evelyn, made from places she never saw him.\n\nThey kept looking at her. I made sure they stopped."
            };
            Vector3[] locations = { new Vector3(-5, .55f, 16), new Vector3(1, .55f, 17), new Vector3(-4, .55f, 21) };
            for (int i = 0; i < 3; i++)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(evidencePrefab, interactables);
                obj.name = labels[i]; obj.transform.position = locations[i];
                var item = obj.GetComponent<EvidenceItem>();
                item.Configure(evidence, labels[i], clues[i]);
                evidenceItems.Add(item);
            }
            evidence.Configure(evidenceItems);

            var doorPrefab = MakeDoorPrefab();
            var doorObject = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab, mechanics);
            doorObject.name = "Ballroom evidence door";
            doorObject.transform.position = new Vector3(12, 0, -2);
            var door = doorObject.GetComponent<DoorController>();
            door.Configure(doorObject.transform.GetChild(0));

            var studioTrigger = Trigger<StudioEventTrigger>(mechanics, "Art Studio story trigger", new Vector3(-18, 1, -2), new Vector3(6, 2.5f, 6));
            var ballroomTrigger = Trigger<BallroomTrigger>(mechanics, "Ballroom ending trigger", new Vector3(20, 1, 0), new Vector3(7, 2.5f, 8));
            var ballroom = root.Find("ENVIRONMENT/Ballroom") ?? throw new InvalidOperationException("Ballroom group missing");
            var endingEvelyn = EndingPoint(ballroom, "Ending_EvelynPoint", new Vector3(19.1f, .02f, 0));
            var endingAdrian = EndingPoint(ballroom, "Ending_AdrianPoint", new Vector3(20.9f, .08551699f, 0));
            var adrianActor = root.Find("CHARACTERS/Adrian Valmont - ballroom") ??
                throw new InvalidOperationException("Ballroom Adrian missing");
            var evidenceZone = new GameObject("Restricted evidence detection");
            evidenceZone.transform.SetParent(mechanics);
            evidenceZone.transform.position = new Vector3(-4.5f, 1, 20);
            var evidenceSphere = evidenceZone.AddComponent<SphereCollider>();
            evidenceSphere.isTrigger = true; evidenceSphere.radius = 1.8f;
            evidenceZone.AddComponent<DetectionZone>().Configure(suspicion);

            var chestPrefab = MakeChestPrefab();
            MakeDecoratedChestVariant(chestPrefab);
            foreach (var point in new[] { new Vector2(-4, 3), new Vector2(-10, -1), new Vector2(0, 14) })
            {
                var chest = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab, interactables);
                chest.name = "Pushable academy chest";
                PlaceChestOnFloor(chest, point);
            }

            var rosePrefab = MakeRosePrefab();
            var rosePoints = new List<Transform> {
                Point(points, "Rose hall", 3, 3, .8f), Point(points, "Rose studio", -21, -5, .8f),
                Point(points, "Rose gallery", 1, 20, .8f), Point(points, "Rose ballroom", 17, 1, .8f)
            };
            spawner.Configure(rosePrefab, rosePoints);
            game.Configure(player, relay, hud, celeste, suspicion, timer, evidence, spawner, door);
            game.ConfigureEnding(endingEvelyn, endingAdrian, adrianActor);
            ConfigureStartScreen(hud, game);
            Validate(game, hud, player, celeste, patrolObject.GetComponent<PatrolNPC>(), evidence,
                evidenceItems, door, spawner, studioTrigger, ballroomTrigger);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(item => item.path == ScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }
            AssetDatabase.SaveAssets();
            PatchSerializedCanvasScale();
            Debug.Log("Velvet Vows core gameplay installed and validated in " + ScenePath);
        }

        private static Transform Group(string name, Transform parent)
        {
            if (parent == null) throw new InvalidOperationException("Missing scene group for " + name);
            var result = new GameObject(name).transform;
            result.SetParent(parent, false);
            return result;
        }
        private static Transform Point(Transform parent, string name, float x, float z, float y = 0)
        {
            var point = Group(name, parent);
            point.position = new Vector3(x, y, z);
            return point;
        }
        private static Transform EndingPoint(Transform parent, string name, Vector3 position)
        {
            var point = parent.Find(name) ?? Group(name, parent);
            point.position = position;
            return point;
        }
        private static void PlaceChestOnFloor(GameObject chest, Vector2 horizontal)
        {
            var collider = chest.GetComponent<BoxCollider>() ?? throw new InvalidOperationException("Chest collider missing");
            chest.transform.position = new Vector3(horizontal.x, 2.5f, horizontal.y);
            Physics.SyncTransforms();
            float floorY = float.NegativeInfinity;
            foreach (var hit in Physics.RaycastAll(new Vector3(horizontal.x, 1.5f, horizontal.y),
                Vector3.down, 3f, 1, QueryTriggerInteraction.Ignore))
                if (hit.collider != collider && hit.normal.y > .9f && hit.point.y <= 1f)
                    floorY = Mathf.Max(floorY, hit.point.y);
            if (float.IsNegativeInfinity(floorY))
                throw new InvalidOperationException("No floor surface below chest at " + horizontal);
            float bottomFromPivot = collider.bounds.min.y - chest.transform.position.y;
            chest.transform.position = new Vector3(horizontal.x, floorY - bottomFromPivot, horizontal.y);
        }
        private static T Trigger<T>(Transform parent, string name, Vector3 position, Vector3 size) where T : MonoBehaviour
        {
            var obj = Group(name, parent).gameObject;
            obj.transform.position = position;
            var collider = obj.AddComponent<BoxCollider>();
            collider.isTrigger = true; collider.size = size;
            return obj.AddComponent<T>();
        }
        private static GameObject Require(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new InvalidOperationException("Missing prefab: " + path);
        private static GameObject Save(GameObject objectInScene, string name)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(objectInScene, PrefabPath + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(objectInScene);
            return saved ?? throw new InvalidOperationException("Could not save gameplay prefab " + name);
        }
        private static GameObject MakeCelestePrefab()
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(Require(PrefabPath + "Celeste.prefab"));
            obj.name = "Celeste Story";
            obj.AddComponent<CelesteSequence>();
            var collider = obj.AddComponent<CapsuleCollider>();
            collider.isTrigger = true; collider.center = new Vector3(0, .9f, 0); collider.height = 1.8f; collider.radius = .4f;
            return Save(obj, "Celeste Story");
        }
        private static GameObject MakePatrolPrefab()
        {
            var wrapper = new GameObject("Academy Patrol");
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(Require(PrefabPath + "Adrian Valmont.prefab"), wrapper.transform);
            visual.transform.localPosition = Vector3.zero;
            wrapper.AddComponent<PatrolNPC>();
            var body = wrapper.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var sphere = new GameObject("Detection Area"); sphere.transform.SetParent(wrapper.transform, false);
            sphere.transform.localPosition = Vector3.up;
            var collider = sphere.AddComponent<SphereCollider>(); collider.isTrigger = true; collider.radius = 2f;
            sphere.AddComponent<DetectionZone>();
            return Save(wrapper, "Academy Patrol");
        }
        private static GameObject MakeEvidencePrefab()
        {
            var wrapper = new GameObject("Evidence Clue");
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(Require(Kit + "Books/Books_1.prefab"), wrapper.transform);
            visual.transform.localPosition = Vector3.zero;
            var collider = wrapper.AddComponent<SphereCollider>(); collider.isTrigger = true; collider.radius = .8f;
            wrapper.AddComponent<EvidenceItem>();
            return Save(wrapper, "Evidence Clue");
        }
        private static GameObject MakeDoorPrefab()
        {
            var wrapper = new GameObject("Evidence Door");
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(Require(Kit + "Door/Door.prefab"), wrapper.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0, 90, 0);
            var collider = wrapper.AddComponent<BoxCollider>(); collider.size = new Vector3(.7f, 2.7f, 3.5f); collider.center = Vector3.up * 1.35f;
            wrapper.AddComponent<DoorController>();
            return Save(wrapper, "Evidence Door");
        }
        private static GameObject MakeChestPrefab()
        {
            var wrapper = new GameObject("Movable Chest");
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(Require(Kit + "Chests/Chests.prefab"), wrapper.transform);
            visual.transform.localPosition = Vector3.zero;
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Chest visual has no Renderer");
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var collider = wrapper.AddComponent<BoxCollider>();
            collider.center = wrapper.transform.InverseTransformPoint(bounds.center);
            collider.size = bounds.size;
            var body = wrapper.AddComponent<Rigidbody>();
            body.mass = 60;
            body.linearDamping = 2f;
            body.angularDamping = 8f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            return Save(wrapper, "Movable Chest");
        }
        private static void MakeDecoratedChestVariant(GameObject basePrefab)
        {
            var variant = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            variant.name = "Decorated Chest";
            var books = (GameObject)PrefabUtility.InstantiatePrefab(Require(Kit + "Books/Books_1.prefab"), variant.transform);
            books.transform.localPosition = new Vector3(0, .55f, 0);
            Save(variant, "Decorated Chest");
            var saved = Require(PrefabPath + "Decorated Chest.prefab");
            if (PrefabUtility.GetPrefabAssetType(saved) != PrefabAssetType.Variant)
                throw new InvalidOperationException("Decorated Chest did not serialize as a Prefab Variant");
        }
        private static GameObject MakeRosePrefab()
        {
            var wrapper = new GameObject("Black Rose");
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem"; stem.transform.SetParent(wrapper.transform, false);
            stem.transform.localPosition = new Vector3(0, -.18f, 0); stem.transform.localScale = new Vector3(.04f, .25f, .04f);
            UnityEngine.Object.DestroyImmediate(stem.GetComponent<Collider>());
            var bloom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bloom.name = "Bloom"; bloom.transform.SetParent(wrapper.transform, false);
            bloom.transform.localPosition = new Vector3(0, .12f, 0); bloom.transform.localScale = new Vector3(.25f, .2f, .25f);
            UnityEngine.Object.DestroyImmediate(bloom.GetComponent<Collider>());
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/VelvetVows/Materials/Black Rose.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = new Color(.13f, .025f, .06f);
                AssetDatabase.CreateAsset(material, "Assets/VelvetVows/Materials/Black Rose.mat");
            }
            bloom.GetComponent<MeshRenderer>().sharedMaterial = material;
            stem.GetComponent<MeshRenderer>().sharedMaterial = material;
            var collider = wrapper.AddComponent<SphereCollider>(); collider.isTrigger = true; collider.radius = .7f;
            wrapper.AddComponent<BlackRoseCollectible>();
            return Save(wrapper, "Black Rose");
        }

        private static GameObject BuildUI(Transform parent, out GameHUD hud)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) throw new InvalidOperationException("Unity built-in UI font missing");
            var canvasObject = new GameObject("Gameplay Canvas");
            canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.transform.SetParent(null, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            hud = canvasObject.AddComponent<GameHUD>();
            var full = canvasObject.GetComponent<RectTransform>();
            full.localScale = Vector3.one;
            full.localPosition = Vector3.zero;
            full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one; full.offsetMin = Vector2.zero; full.offsetMax = Vector2.zero;

            var start = Panel(canvasObject.transform, "Start Panel", new Color(.04f, .035f, .045f, .88f), Vector2.zero, Vector2.one);
            TextAt(start.transform, "Title", "VELVET VOWS", new Vector2(0, 150), new Vector2(1000, 100), 64);
            TextAt(start.transform, "Subtitle", "A Perfect Duet", new Vector2(0, 72), new Vector2(900, 60), 38);
            TextAt(start.transform, "Controls", "WASD — Move     Shift — Sprint     Space — Jump\nMouse — Camera     E — Interact", new Vector2(0, -55), new Vector2(1100, 100), 28);
            var begin = ButtonAt(start.transform, "BEGIN", new Vector2(0, -190));

            var gameplay = Panel(canvasObject.transform, "Gameplay HUD", Color.clear, Vector2.zero, Vector2.one);
            var objective = TextAt(gameplay.transform, "Objective", "", new Vector2(0, -55), new Vector2(1350, 75), 33,
                new Vector2(.5f, 1), new Vector2(.5f, 1));
            var suspicionText = TextAt(gameplay.transform, "Suspicion", "SUSPICION 0%", new Vector2(195, -45), new Vector2(380, 55), 28,
                new Vector2(0, 1), new Vector2(0, 1));
            var bar = Panel(gameplay.transform, "Suspicion Bar", new Color(.13f, .1f, .1f, .9f), Vector2.zero, Vector2.zero);
            var barRect = bar.GetComponent<RectTransform>(); barRect.anchorMin = barRect.anchorMax = new Vector2(0, 1);
            barRect.anchoredPosition = new Vector2(195, -88); barRect.sizeDelta = new Vector2(350, 18);
            var fill = Panel(bar.transform, "Fill", Burgundy, Vector2.zero, Vector2.one).GetComponent<Image>();
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0;
            var timer = TextAt(gameplay.transform, "Timer", "", new Vector2(-255, -45), new Vector2(500, 55), 28,
                new Vector2(1, 1), new Vector2(1, 1));
            var evidence = TextAt(gameplay.transform, "Evidence", "", new Vector2(190, -140), new Vector2(360, 55), 28,
                new Vector2(0, 1), new Vector2(0, 1));
            var roses = TextAt(gameplay.transform, "Roses", "", new Vector2(-190, -140), new Vector2(360, 55), 28,
                new Vector2(1, 1), new Vector2(1, 1));
            var interaction = TextAt(gameplay.transform, "Interaction", "", new Vector2(0, 105), new Vector2(800, 60), 32,
                new Vector2(.5f, 0), new Vector2(.5f, 0));
            var clue = Panel(canvasObject.transform, "Clue Panel", new Color(.04f, .035f, .045f, .9f),
                new Vector2(.25f, .25f), new Vector2(.75f, .68f));
            var clueText = TextAt(clue.transform, "Clue Text", "", Vector2.zero, new Vector2(750, 320), 31);
            var dialogue = TextAt(canvasObject.transform, "Dialogue", "", new Vector2(0, 120), new Vector2(1300, 100), 35,
                new Vector2(.5f, 0), new Vector2(.5f, 0));
            var victory = Panel(canvasObject.transform, "Victory Panel", new Color(.04f, .035f, .045f, .96f), Vector2.zero, Vector2.one);
            TextAt(victory.transform, "Victory Title", "A PERFECT DUET", new Vector2(0, 130), new Vector2(1200, 90), 58);
            TextAt(victory.transform, "Victory Body", "Two hearts.\nFour victims.\nOne perfect love.\n\nVICTORY", new Vector2(0, -15), new Vector2(1000, 250), 34);
            var again = ButtonAt(victory.transform, "PLAY AGAIN", new Vector2(0, -230));
            var defeat = Panel(canvasObject.transform, "Defeat Panel", new Color(.04f, .035f, .045f, .96f), Vector2.zero, Vector2.one);
            TextAt(defeat.transform, "Defeat Title", "THE FALLEN ROSE", new Vector2(0, 150), new Vector2(1200, 90), 58);
            TextAt(defeat.transform, "Defeat Body", "Evelyn was discovered before she could reach Adrian.\n\nDEFEAT", new Vector2(0, 0), new Vector2(1100, 180), 32);
            var defeatReason = TextAt(defeat.transform, "Defeat Reason", "", new Vector2(0, -120), new Vector2(1000, 65), 25);
            var retry = ButtonAt(defeat.transform, "TRY AGAIN", new Vector2(0, -230));
            var fade = Panel(canvasObject.transform, "Fade", Color.black, Vector2.zero, Vector2.one).GetComponent<Image>();
            fade.raycastTarget = false;
            hud.Configure(start, gameplay, clue, victory, defeat, objective, suspicionText, timer, evidence,
                roses, interaction, clueText, dialogue, defeatReason, fill, fade, begin, again, retry);
            start.SetActive(true);
            gameplay.SetActive(false);
            clue.SetActive(false);
            dialogue.gameObject.SetActive(false);
            victory.SetActive(false);
            defeat.SetActive(false);
            fade.color = new Color(0, 0, 0, 0);

            var events = new GameObject("Velvet Vows EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(parent, false);
            return canvasObject;
        }
        [MenuItem("Velvet Vows/Repair Start Screen")]
        public static void RepairStartScreen()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>() ?? throw new InvalidOperationException("Existing GameManager missing");
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>() ?? throw new InvalidOperationException("Existing gameplay Canvas/HUD missing");
            ConfigureStartScreen(hud, game);
            ValidateStartScreen(hud, game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            PatchSerializedCanvasScale();
            Debug.Log("Velvet Vows start screen repaired and saved in " + ScenePath);
        }
        [MenuItem("Velvet Vows/Open Verified Main Scene")]
        public static void OpenVerifiedMainScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>() ??
                throw new InvalidOperationException("GameManager missing from " + scene.path);
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>() ??
                throw new InvalidOperationException("Gameplay Canvas missing from " + scene.path);
            ValidateStartScreen(hud, game);
            Debug.Log("[VelvetVows] Opened verified scene: " + Path.GetFullPath(ScenePath));
        }
        private static void PatchSerializedCanvasScale()
        {
            // In headless Unity 6 imports, the overlay Canvas RectTransform is driven back to zero
            // during scene serialization. Keep a valid scene value as well as the runtime guard.
            string sceneFile = Path.Combine(Application.dataPath, "VelvetVows/Scenes/VelvetVows_Main.unity");
            string yaml = File.ReadAllText(sceneFile);
            int name = yaml.IndexOf("  m_Name: Gameplay Canvas\n", StringComparison.Ordinal);
            if (name < 0) throw new InvalidOperationException("Serialized gameplay Canvas missing");
            int goStart = yaml.LastIndexOf("--- !u!1 &", name, StringComparison.Ordinal);
            int goEnd = yaml.IndexOf("--- !u!", name, StringComparison.Ordinal);
            string gameObjectBlock = yaml.Substring(goStart, goEnd - goStart);
            string rectHeader = null;
            foreach (Match component in Regex.Matches(gameObjectBlock, @"- component: \{fileID: (\d+)\}"))
            {
                string candidate = "--- !u!224 &" + component.Groups[1].Value + "\n";
                if (yaml.Contains(candidate)) { rectHeader = candidate; break; }
            }
            if (rectHeader == null) throw new InvalidOperationException("Serialized gameplay Canvas RectTransform missing");
            int rectStart = yaml.IndexOf(rectHeader, StringComparison.Ordinal);
            int rectEnd = yaml.IndexOf("--- !u!", rectStart + rectHeader.Length, StringComparison.Ordinal);
            if (rectEnd < 0) rectEnd = yaml.Length;
            string rectBlock = yaml.Substring(rectStart, rectEnd - rectStart);
            var scale = Regex.Match(rectBlock, @"m_LocalScale: \{x: [^}]+\}");
            if (!scale.Success) throw new InvalidOperationException("Serialized gameplay Canvas scale missing");
            rectBlock = rectBlock.Remove(scale.Index, scale.Length).Insert(scale.Index, "m_LocalScale: {x: 1, y: 1, z: 1}");
            yaml = yaml.Remove(rectStart, rectEnd - rectStart).Insert(rectStart, rectBlock);
            File.WriteAllText(sceneFile, yaml);
        }
        private static void ConfigureStartScreen(GameHUD hud, GameManager game)
        {
            var starterInputs = UnityEngine.Object.FindFirstObjectByType<StarterAssetsInputs>() ??
                throw new InvalidOperationException("Starter Assets player inputs missing");
            starterInputs.cursorLocked = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(starterInputs);
            var canvas = hud.GetComponent<Canvas>();
            var rect = hud.GetComponent<RectTransform>();
            if (canvas == null || rect == null) throw new InvalidOperationException("Gameplay Canvas components missing");
            rect.SetParent(null, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
            var scaler = hud.GetComponent<CanvasScaler>() ?? throw new InvalidOperationException("Canvas Scaler missing");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            hud.gameObject.SetActive(true);
            var start = hud.transform.Find("Start Panel")?.gameObject ?? throw new InvalidOperationException("Start Panel missing");
            var gameplay = hud.transform.Find("Gameplay HUD")?.gameObject ?? throw new InvalidOperationException("Gameplay HUD panel missing");
            var clue = hud.transform.Find("Clue Panel")?.gameObject ?? throw new InvalidOperationException("Clue Panel missing");
            var victory = hud.transform.Find("Victory Panel")?.gameObject ?? throw new InvalidOperationException("Victory Panel missing");
            var defeat = hud.transform.Find("Defeat Panel")?.gameObject ?? throw new InvalidOperationException("Defeat Panel missing");
            var dialogue = hud.transform.Find("Dialogue")?.gameObject ?? throw new InvalidOperationException("Dialogue missing");
            var fade = hud.transform.Find("Fade")?.GetComponent<Image>() ?? throw new InvalidOperationException("Fade missing");
            start.SetActive(true);
            gameplay.SetActive(false);
            clue.SetActive(false);
            victory.SetActive(false);
            defeat.SetActive(false);
            dialogue.SetActive(false);
            fade.color = new Color(0, 0, 0, 0);
            fade.raycastTarget = false;
            var begin = start.transform.Find("BEGIN Button")?.GetComponent<Button>() ?? throw new InvalidOperationException("BEGIN Button missing");
            begin.interactable = true;
            var controls = start.transform.Find("Controls")?.GetComponent<Text>() ?? throw new InvalidOperationException("Controls text missing");
            controls.text = "WASD — Move     Shift — Sprint     Space — Jump\nMouse — Camera     E — Interact";
            bool wired = false;
            for (int i = 0; i < begin.onClick.GetPersistentEventCount(); i++)
                if (begin.onClick.GetPersistentTarget(i) == game && begin.onClick.GetPersistentMethodName(i) == nameof(GameManager.Begin)) wired = true;
            if (!wired) UnityEventTools.AddPersistentListener(begin.onClick, game.Begin);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(begin);
            EditorUtility.SetDirty(rect);
        }
        private static void ValidateStartScreen(GameHUD hud, GameManager game)
        {
            var errors = new List<string>();
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var events = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            var canvas = hud.GetComponent<Canvas>();
            var rect = hud.GetComponent<RectTransform>();
            var scaler = hud.GetComponent<CanvasScaler>();
            if (canvases.Length != 1 || !hud.gameObject.activeInHierarchy || canvas == null || !canvas.enabled ||
                canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.sortingOrder < 1)
                errors.Add("Active overlay Canvas setup invalid");
            if (rect == null || !hud.enabled) errors.Add("Gameplay Canvas/HUD component invalid");
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.referenceResolution.x <= 0 || scaler.referenceResolution.y <= 0) errors.Add("Canvas Scaler invalid");
            var start = hud.transform.Find("Start Panel");
            if (start == null || !start.gameObject.activeInHierarchy) errors.Add("Start Panel inactive or missing");
            else
            {
                var startRect = start.GetComponent<RectTransform>();
                var background = start.GetComponent<Image>();
                if (startRect == null || startRect.anchorMin != Vector2.zero || startRect.anchorMax != Vector2.one ||
                    startRect.offsetMin != Vector2.zero || startRect.offsetMax != Vector2.zero)
                    errors.Add("Start Panel does not stretch to screen");
                if (background == null || !background.enabled || background.color.a < .5f) errors.Add("Start Panel background invisible");
                foreach (var name in new[] { "Title", "Subtitle", "Controls" })
                {
                    var label = start.Find(name)?.GetComponent<Text>();
                    if (label == null || !label.gameObject.activeInHierarchy || !label.enabled || label.color.a < .5f ||
                        string.IsNullOrEmpty(label.text) || label.font == null)
                        errors.Add("Start text invisible: " + name);
                }
                var begin = start.Find("BEGIN Button")?.GetComponent<Button>();
                if (begin == null || !begin.gameObject.activeInHierarchy || !begin.interactable ||
                    begin.GetComponent<Image>()?.color.a < .5f) errors.Add("BEGIN Button invisible/inactive");
                else
                {
                    bool wired = false;
                    for (int i = 0; i < begin.onClick.GetPersistentEventCount(); i++)
                        if (begin.onClick.GetPersistentTarget(i) == game &&
                            begin.onClick.GetPersistentMethodName(i) == nameof(GameManager.Begin)) wired = true;
                    if (!wired) errors.Add("BEGIN OnClick is not serialized to GameManager.Begin");
                }
            }
            foreach (var name in new[] { "Gameplay HUD", "Clue Panel", "Victory Panel", "Defeat Panel", "Dialogue" })
                if (hud.transform.Find(name)?.gameObject.activeSelf != false) errors.Add(name + " covers start at scene load");
            var fade = hud.transform.Find("Fade")?.GetComponent<Image>();
            if (fade == null || fade.color.a > .001f || fade.raycastTarget) errors.Add("Fade covers/blocks start at scene load");
            if (events.Length != 1 || events[0].GetComponent<InputSystemUIInputModule>()?.actionsAsset == null)
                errors.Add("EventSystem/Input System UI actions missing");
            var starterInputs = UnityEngine.Object.FindFirstObjectByType<StarterAssetsInputs>();
            if (starterInputs == null || starterInputs.cursorLocked)
                errors.Add("Starter Assets would relock the cursor on Game View focus");
            CheckReferences(hud, new[] { "startPanel", "gameplayPanel", "cluePanel", "victoryPanel", "defeatPanel",
                "objectiveText", "suspicionText", "timerText", "evidenceText", "roseText", "interactionText",
                "clueText", "dialogueText", "defeatReasonText", "suspicionFill", "fadeImage", "beginButton",
                "victoryRestartButton", "defeatRestartButton" }, errors);
            if (errors.Count > 0) throw new InvalidOperationException("Start screen validation failed: " + string.Join("; ", errors));
        }
        private static GameObject Panel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            obj.GetComponent<Image>().color = color;
            return obj;
        }
        private static Text TextAt(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize,
            Vector2? anchor = null, Vector2? pivot = null)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(.5f, .5f);
            rect.pivot = pivot ?? new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var label = obj.GetComponent<Text>();
            label.font = font; label.fontSize = fontSize; label.text = value; label.color = Cream;
            label.alignment = TextAnchor.MiddleCenter; label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow; label.raycastTarget = false;
            return label;
        }
        private static Button ButtonAt(Transform parent, string label, Vector2 position)
        {
            var obj = Panel(parent, label + " Button", Burgundy, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            var rect = obj.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(360, 82);
            var button = obj.AddComponent<Button>();
            TextAt(obj.transform, "Label", label, Vector2.zero, new Vector2(350, 75), 31);
            return button;
        }

        private static void Validate(GameManager game, GameHUD hud, ThirdPersonController player, CelesteSequence celeste,
            PatrolNPC patrol, EvidenceManager evidence, List<EvidenceItem> evidenceItems, DoorController door,
            BlackRoseSpawner roses, StudioEventTrigger studio, BallroomTrigger ballroom)
        {
            var errors = new List<string>();
            if (game == null || hud == null || player.GetComponent<PlayerInteractor>() == null) errors.Add("Gameplay core or player interactor missing");
            if (player.GetComponent<PlayerInput>()?.actions == null || player.GetComponent<CharacterController>() == null ||
                player.GetComponent<StarterAssetsInputs>() == null || !player.GetComponent<AcademyPlayerInputRelay>().HasReferences)
                errors.Add("Accepted player input/controller references missing");
            if (player.CinemachineCameraTarget == null || UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None).Length != 1 ||
                UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length != 1)
                errors.Add("Accepted camera references changed");
            if (celeste == null || celeste.GetComponentInChildren<Animator>(true) == null ||
                celeste.GetComponent<Collider>() == null) errors.Add("Celeste references missing");
            if (patrol == null || patrol.GetComponentInChildren<Animator>(true) == null ||
                patrol.GetComponentInChildren<DetectionZone>() == null || patrol.GetComponent<Rigidbody>() == null)
                errors.Add("Patrol references missing");
            if (evidence.Total != 3 || evidenceItems.Any(item => item == null || item.GetComponent<Collider>() == null)) errors.Add("Evidence setup invalid");
            if (door == null || door.GetComponent<BoxCollider>() == null) errors.Add("Door setup invalid");
            if (roses.RosePrefab == null || roses.SpawnPoints.Count < 3 || roses.SpawnPoints.Any(point => point == null)) errors.Add("Rose spawner setup invalid");
            if (!studio.GetComponent<BoxCollider>().isTrigger || !ballroom.GetComponent<BoxCollider>().isTrigger) errors.Add("Story triggers invalid");
            if (UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Count(body => !body.isKinematic) < 3)
                errors.Add("Three dynamic physics objects required");
            if (UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length != 1) errors.Add("EventSystem setup invalid");
            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                    errors.Add("Missing script on " + transform.name);
            if (errors.Count > 0) throw new InvalidOperationException("Gameplay validation failed: " + string.Join("; ", errors));
        }

        [MenuItem("Velvet Vows/Validate Core Gameplay")]
        public static void ValidateInstalled()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var errors = new List<string>();
            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonController>();
            var celeste = UnityEngine.Object.FindFirstObjectByType<CelesteSequence>();
            var patrol = UnityEngine.Object.FindFirstObjectByType<PatrolNPC>();
            var evidence = UnityEngine.Object.FindFirstObjectByType<EvidenceManager>();
            var rose = UnityEngine.Object.FindFirstObjectByType<BlackRoseSpawner>();
            var door = UnityEngine.Object.FindFirstObjectByType<DoorController>();
            if (game == null || hud == null || player == null || celeste == null || patrol == null || evidence == null || rose == null || door == null)
                errors.Add("A core gameplay component is missing");
            else
            {
                ValidateStartScreen(hud, game);
                CheckReferences(game, new[] { "playerMovement", "playerInput", "hud", "celeste", "suspicion", "timer", "evidence", "roses", "ballroomDoor",
                    "endingEvelynPoint", "endingAdrianPoint", "adrian" }, errors);
                CheckReferences(hud, new[] { "startPanel", "gameplayPanel", "cluePanel", "victoryPanel", "defeatPanel",
                    "objectiveText", "suspicionText", "timerText", "evidenceText", "roseText", "interactionText",
                    "clueText", "dialogueText", "defeatReasonText", "suspicionFill", "fadeImage", "beginButton",
                    "victoryRestartButton", "defeatRestartButton" }, errors);
                CheckReferences(celeste, new[] { "modelAnimator", "visualRoot" }, errors);
                CheckReferences(patrol, new[] { "modelAnimator" }, errors);
                CheckReferences(rose, new[] { "rosePrefab" }, errors);
                CheckReferences(door, new[] { "visual" }, errors);
                CheckList(celeste, "waypoints", 4, errors);
                CheckList(patrol, "waypoints", 3, errors);
                CheckList(evidence, "items", 3, errors);
                CheckList(rose, "spawnPoints", 4, errors);
                if (player.GetComponent<PlayerInput>()?.actions == null || player.GetComponent<PlayerInteractor>() == null ||
                    player.GetComponent<BasicRigidBodyPush>()?.canPush != true) errors.Add("Player gameplay input/push configuration missing");
                if (UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length != 1 ||
                    UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None).Length != 1)
                    errors.Add("Main camera or Cinemachine camera count invalid");
                if (UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Count(body => !body.isKinematic) != 3)
                    errors.Add("Expected exactly three pushable Rigidbodies");
                var endingEvelyn = new SerializedObject(game).FindProperty("endingEvelynPoint")?.objectReferenceValue as Transform;
                var endingAdrian = new SerializedObject(game).FindProperty("endingAdrianPoint")?.objectReferenceValue as Transform;
                if (endingEvelyn != null && endingAdrian != null &&
                    (Vector3.Distance(endingEvelyn.position, endingAdrian.position) < 1.5f ||
                     Vector3.Distance(endingEvelyn.position, endingAdrian.position) > 2f))
                    errors.Add("Ending character spacing must be 1.5–2 meters");
                Physics.SyncTransforms();
                foreach (var body in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Where(item => !item.isKinematic))
                {
                    var box = body.GetComponent<BoxCollider>();
                    if (box == null || !Physics.Raycast(body.position + Vector3.up * .8f, Vector3.down,
                        out var floorHit, 2f, 1, QueryTriggerInteraction.Ignore) ||
                        Mathf.Abs(box.bounds.min.y - floorHit.point.y) > .03f)
                        errors.Add("Chest not grounded: " + body.name);
                }
                if (UnityEngine.Object.FindObjectsByType<StudioEventTrigger>(FindObjectsSortMode.None).Length != 1 ||
                    UnityEngine.Object.FindObjectsByType<BallroomTrigger>(FindObjectsSortMode.None).Length != 1 ||
                    UnityEngine.Object.FindObjectsByType<DetectionZone>(FindObjectsSortMode.None).Length < 2)
                    errors.Add("Trigger mechanics missing");
                var uiModule = UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>();
                if (uiModule == null || uiModule.actionsAsset == null) errors.Add("Input System UI actions missing");
            }
            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                    errors.Add("Missing script on " + transform.name);
            foreach (var name in new[] { "Celeste Story", "Academy Patrol", "Evidence Clue", "Evidence Door", "Movable Chest", "Black Rose" })
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + name + ".prefab") == null)
                    errors.Add("Missing gameplay prefab " + name);
            var variant = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + "Decorated Chest.prefab");
            if (variant == null || PrefabUtility.GetPrefabAssetType(variant) != PrefabAssetType.Variant)
                errors.Add("Decorated Chest Prefab Variant missing");
            if (!EditorBuildSettings.scenes.Any(item => item.path == ScenePath && item.enabled))
                errors.Add("Scene missing from Build Settings; restart cannot reload it");
            if (errors.Count > 0) throw new InvalidOperationException("Installed gameplay validation failed: " + string.Join("; ", errors));
            Debug.Log("Velvet Vows saved gameplay scene, references, prefabs, triggers, physics, input UI and restart build entry validated.");
        }
        private static void CheckReferences(UnityEngine.Object owner, IEnumerable<string> names, List<string> errors)
        {
            var serialized = new SerializedObject(owner);
            foreach (string name in names)
            {
                var property = serialized.FindProperty(name);
                if (property == null || property.objectReferenceValue == null)
                    errors.Add(owner.name + "." + name + " missing");
            }
        }
        private static void CheckList(UnityEngine.Object owner, string name, int count, List<string> errors)
        {
            var property = new SerializedObject(owner).FindProperty(name);
            if (property == null || property.arraySize != count) { errors.Add(owner.name + "." + name + " count invalid"); return; }
            for (int i = 0; i < count; i++)
                if (property.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    errors.Add(owner.name + "." + name + "[" + i + "] missing");
        }
    }
}
