using System;
using StarterAssets;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace VelvetVows.Editor
{
    /// <summary>Creates the self-contained movement test scene from built-in meshes and Starter Assets.</summary>
    public static class AcademyPrototypeBuilder
    {
        private const string Root = "Assets/VelvetVows";
        private const string StarterPrefabs = "Assets/Starter Assets/Runtime/ThirdPersonController/Prefabs/";
        private const string InputActionsPath = "Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions";
        private static Material stone, paleStone, darkWood, wood, burgundy, brass, glass, canvas, ink, skin, hair;
        private static Transform architecture, furnishings, lighting;

        [MenuItem("Velvet Vows/Generate Academy Prototype")]
        public static void Generate()
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMaterials();
            architecture = Group("Architecture");
            furnishings = Group("Furnishings");
            lighting = Group("Lighting");
            BuildHall();
            BuildCorridorsAndGallery();
            BuildStairs();
            BuildDetails();
            BuildLighting();
            BuildPlayerAndCamera();
            ValidateScene();

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.43f, 0.39f);
            RenderSettings.fog = false;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/AcademyPrototype.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Velvet Vows academy prototype generated: " + scene.path);
        }

        private static Transform Group(string name) => new GameObject(name).transform;

        private static Material Mat(string name, Color color, float smoothness = 0.2f, bool emission = false)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateMaterials()
        {
            stone = Mat("Charcoal Stone", new Color(.23f, .22f, .22f));
            paleStone = Mat("Limestone", new Color(.67f, .60f, .50f));
            darkWood = Mat("Dark Walnut", new Color(.17f, .095f, .065f), .35f);
            wood = Mat("Walnut", new Color(.32f, .18f, .11f), .32f);
            burgundy = Mat("Burgundy Velvet", new Color(.32f, .055f, .085f));
            brass = Mat("Aged Brass", new Color(.63f, .44f, .19f), .68f);
            glass = Mat("Window Glow", new Color(.35f, .48f, .55f), .6f, true);
            canvas = Mat("Painting Ochre", new Color(.48f, .30f, .18f));
            ink = Mat("Ink Blue", new Color(.10f, .16f, .20f));
            skin = Mat("Placeholder Skin", new Color(.72f, .48f, .37f));
            hair = Mat("Placeholder Hair", new Color(.12f, .075f, .05f));
        }

        private static GameObject Box(string name, Vector3 center, Vector3 size, Material material, Transform parent, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Shape(string name, PrimitiveType type, Vector3 center, Vector3 size, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void BuildHall()
        {
            Box("Grand hall floor", new Vector3(0, -.15f, 0), new Vector3(18, .3f, 20), stone, architecture);
            Box("Grand hall ceiling", new Vector3(0, 6.1f, 0), new Vector3(18, .25f, 20), paleStone, architecture);
            Box("North wall", new Vector3(0, 3, 10), new Vector3(18, 6, .35f), paleStone, architecture);
            Box("South wall", new Vector3(0, 3, -10), new Vector3(18, 6, .35f), paleStone, architecture);
            foreach (int side in new[] { -1, 1 })
            {
                Box("Hall side wall north " + side, new Vector3(side * 9, 3, 6), new Vector3(.35f, 6, 8), paleStone, architecture);
                Box("Hall side wall south " + side, new Vector3(side * 9, 3, -6), new Vector3(.35f, 6, 8), paleStone, architecture);
                Box("Corridor lintel " + side, new Vector3(side * 9, 5.1f, 0), new Vector3(.45f, 1.8f, 4), darkWood, architecture);
                for (int z = -6; z <= 6; z += 6)
                {
                    Shape("Fluted column " + side + " " + z, PrimitiveType.Cylinder,
                        new Vector3(side * 7.4f, 2.7f, z), new Vector3(.48f, 2.7f, .48f), paleStone, architecture);
                    Box("Column plinth", new Vector3(side * 7.4f, .25f, z), new Vector3(1.1f, .5f, 1.1f), stone, architecture);
                    Box("Column capital", new Vector3(side * 7.4f, 5.45f, z), new Vector3(1.25f, .35f, 1.25f), paleStone, architecture);
                }
            }
            // Shallow inlaid runner keeps the full central lane open.
            Box("Burgundy runner", new Vector3(0, .015f, -3), new Vector3(3.3f, .025f, 11), burgundy, furnishings, false);
            for (int z = -8; z <= 8; z += 4)
            {
                Box("Ceiling beam", new Vector3(0, 5.9f, z), new Vector3(18, .4f, .35f), darkWood, architecture);
            }
            for (int x = -6; x <= 6; x += 4)
                Window(new Vector3(x, 3.4f, -9.78f), true);
        }

        private static void BuildCorridorsAndGallery()
        {
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * 13;
                Box("Side corridor floor " + side, new Vector3(x, -.15f, 0), new Vector3(8, .3f, 4.8f), stone, architecture);
                Box("Side corridor ceiling " + side, new Vector3(x, 5.15f, 0), new Vector3(8, .25f, 4.8f), darkWood, architecture);
                Box("Corridor north wall " + side, new Vector3(x, 2.55f, 2.4f), new Vector3(8, 5.1f, .3f), paleStone, architecture);
                Box("Corridor south wall " + side, new Vector3(x, 2.55f, -2.4f), new Vector3(8, 5.1f, .3f), paleStone, architecture);
                if (side < 0) Box("West corridor end", new Vector3(-17, 2.55f, 0), new Vector3(.3f, 5.1f, 4.8f), paleStone, architecture);
                else Box("Gallery arch lintel", new Vector3(17, 4.4f, 0), new Vector3(.35f, 1.4f, 4.8f), paleStone, architecture);
                Box("Corridor wainscot north", new Vector3(x, .9f, 2.22f), new Vector3(8, 1.8f, .1f), darkWood, furnishings, false);
                Box("Corridor wainscot south", new Vector3(x, .9f, -2.22f), new Vector3(8, 1.8f, .1f), darkWood, furnishings, false);
                Door(new Vector3(side * 14.5f, 1.3f, side < 0 ? 2.18f : -2.18f));
            }
            Box("Art gallery floor", new Vector3(21, -.15f, 0), new Vector3(8, .3f, 12), wood, architecture);
            Box("Art gallery ceiling", new Vector3(21, 5.15f, 0), new Vector3(8, .25f, 12), paleStone, architecture);
            Box("Gallery east wall", new Vector3(25, 2.55f, 0), new Vector3(.3f, 5.1f, 12), paleStone, architecture);
            Box("Gallery north wall", new Vector3(21, 2.55f, 6), new Vector3(8, 5.1f, .3f), paleStone, architecture);
            Box("Gallery south wall", new Vector3(21, 2.55f, -6), new Vector3(8, 5.1f, .3f), paleStone, architecture);
            Box("Gallery west wall north", new Vector3(17, 2.55f, 4.2f), new Vector3(.3f, 5.1f, 3.6f), paleStone, architecture);
            Box("Gallery west wall south", new Vector3(17, 2.55f, -4.2f), new Vector3(.3f, 5.1f, 3.6f), paleStone, architecture);
            for (int z = -4; z <= 4; z += 4) Painting(new Vector3(24.78f, 2.7f, z), false);
            Painting(new Vector3(21, 2.7f, 5.8f), true);
            Painting(new Vector3(21, 2.7f, -5.8f), true);
            Bench(new Vector3(21, .45f, 0), true);
        }

        private static void BuildStairs()
        {
            // 8 shallow 0.16 m risers; Starter Assets has a 0.25 m step offset.
            for (int i = 0; i < 8; i++)
            {
                float front = 4.4f + i * .43f;
                float top = (i + 1) * .16f;
                Box("Stair step " + (i + 1), new Vector3(0, top / 2, front + .215f),
                    new Vector3(4.4f, top, .43f), paleStone, architecture);
                Box("Stair nosing", new Vector3(0, top + .012f, front + .05f),
                    new Vector3(4.45f, .025f, .08f), brass, furnishings, false);
            }
            Box("Upper landing", new Vector3(0, .64f, 8.85f), new Vector3(4.4f, 1.28f, 1.7f), paleStone, architecture);
            foreach (int side in new[] { -1, 1 })
            {
                Box("Stair side plinth", new Vector3(side * 2.45f, .48f, 6.4f), new Vector3(.35f, .95f, 4.5f), darkWood, architecture);
                Box("Stair newel", new Vector3(side * 2.45f, .75f, 4.4f), new Vector3(.4f, 1.5f, .4f), brass, architecture);
            }
        }

        private static void BuildDetails()
        {
            for (int z = -7; z <= 3; z += 5)
            {
                Bench(new Vector3(-5.5f, .45f, z), false);
                Bench(new Vector3(5.5f, .45f, z), false);
            }
            for (int z = -6; z <= 2; z += 4)
            {
                Painting(new Vector3(-8.78f, 3.1f, z), false);
                Painting(new Vector3(8.78f, 3.1f, z), false);
            }
            Box("Entrance double doors", new Vector3(0, 1.65f, -9.76f), new Vector3(3.1f, 3.3f, .12f), darkWood, furnishings, false);
            Box("Entrance brass seam", new Vector3(0, 1.65f, -9.64f), new Vector3(.035f, 3.15f, .03f), brass, furnishings, false);
            foreach (int side in new[] { -1, 1 })
                Shape("Entrance handle", PrimitiveType.Sphere, new Vector3(side * .22f, 1.45f, -9.55f), new Vector3(.12f, .12f, .12f), brass, furnishings);
            // Busts and pedestals remain out of the main movement lane.
            foreach (int side in new[] { -1, 1 })
            {
                Box("Sculpture pedestal", new Vector3(side * 6.4f, .7f, 8.2f), new Vector3(.9f, 1.4f, .9f), darkWood, furnishings);
                Shape("Sculpture bust", PrimitiveType.Sphere, new Vector3(side * 6.4f, 1.65f, 8.2f), new Vector3(.55f, .72f, .45f), paleStone, furnishings);
            }
        }

        private static void Window(Vector3 position, bool facingZ)
        {
            var size = facingZ ? new Vector3(1.55f, 3.3f, .05f) : new Vector3(.05f, 3.3f, 1.55f);
            Box("Tall frosted window", position, size, glass, furnishings, false);
            Box("Window horizontal mullion", position, facingZ ? new Vector3(1.7f, .07f, .1f) : new Vector3(.1f, .07f, 1.7f), darkWood, furnishings, false);
            Box("Window vertical mullion", position, facingZ ? new Vector3(.07f, 3.5f, .1f) : new Vector3(.1f, 3.5f, .07f), darkWood, furnishings, false);
            Box("Window sill", position + Vector3.down * 1.7f, facingZ ? new Vector3(1.9f, .15f, .35f) : new Vector3(.35f, .15f, 1.9f), paleStone, furnishings, false);
        }

        private static void Painting(Vector3 position, bool facingZ)
        {
            Vector3 frame = facingZ ? new Vector3(1.8f, 2.2f, .12f) : new Vector3(.12f, 2.2f, 1.8f);
            Vector3 inner = facingZ ? new Vector3(1.52f, 1.92f, .14f) : new Vector3(.14f, 1.92f, 1.52f);
            Box("Gilded painting frame", position, frame, brass, furnishings, false);
            Box("Abstract oil study", position + (facingZ ? Vector3.forward : Vector3.right) * .02f, inner, canvas, furnishings, false);
            Shape("Painting dark motif", PrimitiveType.Sphere, position + (facingZ ? Vector3.forward : Vector3.right) * .11f + Vector3.up * .15f,
                facingZ ? new Vector3(.65f, .9f, .04f) : new Vector3(.04f, .9f, .65f), ink, furnishings);
        }

        private static void Door(Vector3 position)
        {
            Box("Walnut panel door", position, new Vector3(1.45f, 2.6f, .12f), darkWood, furnishings, false);
            Box("Door inset", position + new Vector3(0, .25f, -.08f), new Vector3(1.1f, 1.5f, .025f), wood, furnishings, false);
            Shape("Door handle", PrimitiveType.Sphere, position + new Vector3(.48f, -.15f, -.16f), new Vector3(.12f, .12f, .12f), brass, furnishings);
        }

        private static void Bench(Vector3 position, bool alongZ)
        {
            Vector3 seat = alongZ ? new Vector3(.8f, .15f, 2.4f) : new Vector3(2.4f, .15f, .8f);
            Box("Velvet bench seat", position, seat, burgundy, furnishings);
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 offset = alongZ ? new Vector3(0, -.23f, side * .9f) : new Vector3(side * .9f, -.23f, 0);
                Box("Bench leg", position + offset, new Vector3(.18f, .45f, .18f), darkWood, furnishings);
            }
        }

        private static void BuildLighting()
        {
            var sun = new GameObject("Soft window daylight");
            sun.transform.SetParent(lighting);
            sun.transform.rotation = Quaternion.Euler(35, -35, 0);
            var directional = sun.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.color = new Color(.72f, .77f, .82f);
            directional.intensity = .7f;
            foreach (var point in new[] { new Vector3(-4, 5.3f, -5), new Vector3(4, 5.3f, -5),
                         new Vector3(-4, 5.3f, 3), new Vector3(4, 5.3f, 3), new Vector3(21, 4.5f, 0) })
            {
                Shape("Pendant canopy", PrimitiveType.Cylinder, point, new Vector3(.45f, .12f, .45f), brass, lighting);
                Shape("Warm pendant shade", PrimitiveType.Sphere, point + Vector3.down * .35f, new Vector3(.55f, .26f, .55f), brass, lighting);
                var lamp = new GameObject("Warm pendant light");
                lamp.transform.SetParent(lighting);
                lamp.transform.position = point + Vector3.down * .6f;
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1, .71f, .43f);
                light.intensity = 8;
                light.range = 11;
            }
        }

        private static void BuildPlayerAndCamera()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterPrefabs + "PlayerCapsule.prefab");
            var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterPrefabs + "MainCamera.prefab");
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (playerPrefab == null || cameraPrefab == null || actions == null)
                throw new InvalidOperationException("Starter Assets player, Main Camera or Input Actions asset is missing.");

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Academy Student (placeholder)";
            player.transform.position = new Vector3(0, .02f, -4);
            player.transform.rotation = Quaternion.identity;
            var controller = player.GetComponent<ThirdPersonController>();
            var character = player.GetComponent<CharacterController>();
            var playerInput = player.GetComponent<PlayerInput>();
            var starterInputs = player.GetComponent<StarterAssetsInputs>();
            if (controller == null || character == null || playerInput == null || starterInputs == null)
                throw new InvalidOperationException("Starter Assets player prefab is missing a required controller or input component.");
            controller.MoveSpeed = 2.6f;
            controller.SprintSpeed = 5.6f;
            controller.GroundLayers = 1; // Default layer, used by generated floors and steps.
            controller.CinemachineCameraTarget = player.transform.Find("PlayerCameraRoot")?.gameObject;
            if (controller.CinemachineCameraTarget == null)
                throw new InvalidOperationException("Starter Assets player camera target is missing.");
            controller.CinemachineCameraTarget.transform.localPosition = new Vector3(0, 1.62f, 0);
            controller.TopClamp = 65;
            controller.BottomClamp = -32;
            controller.CameraAngleOverride = 4;
            character.stepOffset = .3f; // Existing stair risers are 0.16 m.
            playerInput.actions = actions;
            playerInput.defaultActionMap = "Player";
            playerInput.defaultControlScheme = "KeyboardMouse";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            var relay = player.AddComponent<VelvetVows.AcademyPlayerInputRelay>();
            relay.Configure(playerInput, starterInputs);
            starterInputs.cursorLocked = true;
            starterInputs.cursorInputForLook = true;
            var capsuleVisual = player.transform.Find("Capsule");
            if (capsuleVisual != null) capsuleVisual.gameObject.SetActive(false);
            BuildStudentVisual(player.transform);

            var mainCamera = (GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab);
            mainCamera.transform.position = player.transform.position + new Vector3(.35f, 2.1f, -4.8f);
            mainCamera.transform.rotation = Quaternion.identity;
            mainCamera.GetComponent<Camera>().nearClipPlane = .08f;
            var followObject = new GameObject("Academy Third Person Camera (Cinemachine 3)");
            followObject.transform.position = mainCamera.transform.position;
            var virtualCamera = followObject.AddComponent<CinemachineCamera>();
            virtualCamera.Follow = controller.CinemachineCameraTarget.transform;
            virtualCamera.LookAt = controller.CinemachineCameraTarget.transform;
            virtualCamera.Lens.FieldOfView = 55;
            virtualCamera.Lens.NearClipPlane = .08f;
            var body = followObject.AddComponent<CinemachineThirdPersonFollow>();
            body.ShoulderOffset = new Vector3(.35f, .2f, 0);
            body.VerticalArmLength = .25f;
            body.CameraSide = .5f;
            body.CameraDistance = 4.8f;
            body.Damping = new Vector3(.12f, .18f, .22f);
            body.AvoidObstacles.Enabled = true;
            body.AvoidObstacles.CollisionFilter = 1;
            body.AvoidObstacles.IgnoreTag = "Player";
            body.AvoidObstacles.CameraRadius = .22f;
        }

        [MenuItem("Velvet Vows/Validate Academy Controller")]
        public static void ValidateScene()
        {
            var errors = new System.Collections.Generic.List<string>();
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonController>();
            var input = player != null ? player.GetComponent<PlayerInput>() : null;
            var character = player != null ? player.GetComponent<CharacterController>() : null;
            var starter = player != null ? player.GetComponent<StarterAssetsInputs>() : null;
            var relay = player != null ? player.GetComponent<VelvetVows.AcademyPlayerInputRelay>() : null;
            if (player == null) errors.Add("ThirdPersonController missing");
            if (input == null) errors.Add("PlayerInput missing");
            if (character == null) errors.Add("CharacterController missing");
            if (starter == null) errors.Add("StarterAssetsInputs missing");
            if (relay == null || !relay.HasReferences) errors.Add("Academy input relay or its references missing");
            if (player != null && player.CinemachineCameraTarget == null) errors.Add("Player camera target missing");
            if (character != null && character.stepOffset < .16f)
                errors.Add("CharacterController step offset is too small for the stairs");
            if (player != null && (player.GroundLayers.value & 1) == 0)
                errors.Add("Default floor layer is missing from ThirdPersonController ground layers");
            if (input != null)
            {
                if (input.actions == null) errors.Add("PlayerInput Input Actions asset missing");
                else if (AssetDatabase.GetAssetPath(input.actions) != InputActionsPath)
                    errors.Add("PlayerInput uses the wrong Input Actions asset");
                if (input.defaultActionMap != "Player") errors.Add("PlayerInput default action map must be Player");
                if (input.notificationBehavior != PlayerNotifications.InvokeCSharpEvents)
                    errors.Add("PlayerInput notification behavior is incorrect");
                ValidateAction(input.actions, "Move", new[] { "<Keyboard>/w", "<Keyboard>/a", "<Keyboard>/s", "<Keyboard>/d" }, errors);
                ValidateAction(input.actions, "Sprint", new[] { "<Keyboard>/leftShift" }, errors);
                ValidateAction(input.actions, "Jump", new[] { "<Keyboard>/space" }, errors);
                ValidateAction(input.actions, "Look", new[] { "<Pointer>/delta" }, errors);
            }
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            var main = cameras.Length == 1 ? cameras[0] : null;
            if (main == null || !main.CompareTag("MainCamera") || !main.enabled ||
                main.GetComponent<CinemachineBrain>() == null)
                errors.Add("Exactly one active Main Camera with CinemachineBrain is required");
            if (main != null && player != null && player.CinemachineCameraTarget != null)
            {
                Physics.SyncTransforms();
                var origin = player.CinemachineCameraTarget.transform.position;
                var toCamera = main.transform.position - origin;
                if (Physics.Raycast(origin, toCamera.normalized, toCamera.magnitude, 1, QueryTriggerInteraction.Ignore))
                    errors.Add("Main Camera starts behind a wall or other solid obstacle");
            }
            var rigs = UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            if (rigs.Length != 1) errors.Add("Exactly one Cinemachine 3 camera is required");
            else
            {
                if (player != null && player.CinemachineCameraTarget != null &&
                    rigs[0].Follow != player.CinemachineCameraTarget.transform)
                    errors.Add("Cinemachine Follow target is incorrect");
                if (player != null && player.CinemachineCameraTarget != null &&
                    rigs[0].LookAt != player.CinemachineCameraTarget.transform)
                    errors.Add("Cinemachine LookAt target is incorrect");
                var body = rigs[0].GetComponent<CinemachineThirdPersonFollow>();
                if (body == null || body.CameraDistance < 4 || !body.AvoidObstacles.Enabled)
                    errors.Add("Cinemachine Third Person Follow or camera collision is missing");
            }
            if (errors.Count > 0)
                throw new InvalidOperationException("Academy controller validation failed: " + string.Join("; ", errors));
            Debug.Log("Academy controller validation passed: input, player, camera and follow references are present.");
        }

        private static void ValidateAction(InputActionAsset asset, string name, string[] requiredPaths,
            System.Collections.Generic.List<string> errors)
        {
            var action = asset?.FindAction("Player/" + name, false);
            if (action == null)
            {
                errors.Add("Player/" + name + " action missing");
                return;
            }
            foreach (string path in requiredPaths)
            {
                bool found = false;
                foreach (var binding in action.bindings)
                    if (binding.effectivePath == path) { found = true; break; }
                if (!found) errors.Add("Player/" + name + " binding missing: " + path);
            }
        }

        private static void BuildStudentVisual(Transform player)
        {
            var visual = new GameObject("Female student visual - geometric placeholder").transform;
            visual.SetParent(player);
            visual.localPosition = Vector3.zero;
            Shape("Long burgundy dress", PrimitiveType.Cylinder, player.position + new Vector3(0, .68f, 0),
                new Vector3(.56f, .55f, .48f), burgundy, visual);
            Shape("Fitted jacket", PrimitiveType.Capsule, player.position + new Vector3(0, 1.34f, 0),
                new Vector3(.43f, .38f, .32f), darkWood, visual);
            Shape("Head", PrimitiveType.Sphere, player.position + new Vector3(0, 1.77f, 0),
                new Vector3(.32f, .39f, .31f), skin, visual);
            Shape("Dark bob haircut", PrimitiveType.Sphere, player.position + new Vector3(0, 1.94f, .04f),
                new Vector3(.36f, .27f, .35f), hair, visual);
            foreach (int side in new[] { -1, 1 })
            {
                Shape("Sleeve", PrimitiveType.Capsule, player.position + new Vector3(side * .32f, 1.25f, 0),
                    new Vector3(.17f, .44f, .18f), darkWood, visual);
                Shape("Shoe", PrimitiveType.Cube, player.position + new Vector3(side * .16f, .1f, .12f),
                    new Vector3(.22f, .18f, .42f), hair, visual);
            }
            Box("Brass collar brooch", player.position + new Vector3(0, 1.55f, .18f),
                new Vector3(.12f, .12f, .035f), brass, visual, false);
        }
    }
}
