using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarterAssets;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VelvetVows.Editor
{
    public static class VelvetVowsMainBuilder
    {
        private const string Root = "Assets/VelvetVows";
        private const string Kit = "Assets/Medieval Room Kit/Prefab/";
        private const string Starters = "Assets/Starter Assets/Runtime/ThirdPersonController/Prefabs/";
        private const string Actions = "Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions";
        private const string Kasumi = "Assets/Visutronik/Characters/Volleyball - Sky Strikers/Kasumi Hoshino/";
        private const string Rin = "Assets/Visutronik/Characters/Volleyball - Sky Strikers/Rin Kobayashi/";
        private const string Akio = "Assets/Novacorp Studios/Characters/Hikari Series/Akio/Highschool Uniform/";
        private const string Motions = "Assets/Kevin Iglesias/Human Animations/Animations/";
        private static readonly Dictionary<Material, Material> MaterialCopies = new Dictionary<Material, Material>();
        private static Mesh paintingQuad;
        private static Material portraitMaterial, stillLifeMaterial, frameMaterial;
        private static Transform root, playerGroup, characterGroup, environment, lighting, cameras;
        private static AnimatorController femaleController, maleController, npcFemaleController;

        [MenuItem("Velvet Vows/Generate Main Scene")]
        public static void Generate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolders();
            PrepareMaterials(Kasumi + "Materials/School", "Evelyn");
            PrepareMaterials(Rin + "Materials/School", "Celeste");
            PrepareMaterials(Akio + "Materials", "Adrian");
            PreparePaintings();
            femaleController = CreateController("Evelyn Locomotion", "Female", true);
            npcFemaleController = CreateController("Celeste Locomotion", "Female", false);
            maleController = CreateController("Adrian Locomotion", "Male", false);
            var evelyn = CreateCharacterPrefab("Evelyn Moreau", Kasumi + "Prefab/Kasumi - School.prefab", femaleController);
            var celeste = CreateCharacterPrefab("Celeste", Rin + "Prefab/Rin - School.prefab", npcFemaleController);
            var adrian = CreateCharacterPrefab("Adrian Valmont", Akio + "Prefab/Akio Highschool Uniform.prefab", maleController);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            root = new GameObject("Velvet Vows Main").transform;
            Group("SYSTEMS", root);
            playerGroup = Group("PLAYER", root);
            characterGroup = Group("CHARACTERS", root);
            environment = Group("ENVIRONMENT", root);
            lighting = Group("LIGHTING", root);
            cameras = Group("CAMERAS", root);
            Group("UI", root);
            BuildEnvironment();
            BuildCharacters(evelyn, celeste, adrian);
            BuildLighting();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.43f, .40f, .37f);
            RenderSettings.fog = false;
            Validate();
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/VelvetVows_Main.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Velvet Vows main scene generated: " + scene.path);
        }

        private static void EnsureFolders()
        {
            foreach (string path in new[] { Root + "/Animations", Root + "/Materials/Characters",
                         Root + "/Materials/Characters/Evelyn", Root + "/Materials/Characters/Celeste",
                         Root + "/Materials/Characters/Adrian", Root + "/Materials/Paintings" })
                Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }

        private static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent);
            return group;
        }

        private static GameObject RequirePrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing prefab: " + path);
            return prefab;
        }

        private static void PrepareMaterials(string sourceFolder, string role)
        {
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null) throw new InvalidOperationException("URP Lit shader unavailable");
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { sourceFolder }))
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (source == null) continue;
                string destination = Root + "/Materials/Characters/" + role + "/" + source.name + ".mat";
                var copy = AssetDatabase.LoadAssetAtPath<Material>(destination);
                if (copy == null)
                {
                    copy = new Material(urp) { name = source.name };
                    AssetDatabase.CreateAsset(copy, destination);
                }
                copy.shader = urp;
                var texture = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
                if (texture == null) throw new InvalidOperationException("Character texture missing: " + AssetDatabase.GetAssetPath(source));
                copy.SetTexture("_BaseMap", texture);
                copy.SetColor("_BaseColor", source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
                bool cutout = source.name.Contains("EYE") || source.name.Contains("FACE") || source.name.Contains("HAIR");
                copy.SetFloat("_AlphaClip", cutout ? 1 : 0);
                copy.SetFloat("_Cutoff", .35f);
                if (cutout) { copy.EnableKeyword("_ALPHATEST_ON"); copy.renderQueue = 2450; }
                else { copy.DisableKeyword("_ALPHATEST_ON"); copy.renderQueue = -1; }
                EditorUtility.SetDirty(copy);
                MaterialCopies[source] = copy;
            }
        }

        private static void PreparePaintings()
        {
            string meshPath = Root + "/Art/Painting Surface.asset";
            paintingQuad = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (paintingQuad == null)
            {
                paintingQuad = new Mesh { name = "Painting Surface" };
                paintingQuad.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0),
                    new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0) };
                paintingQuad.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
                paintingQuad.triangles = new[] { 0, 1, 2, 2, 1, 3 };
                paintingQuad.RecalculateNormals();
                paintingQuad.RecalculateBounds();
                AssetDatabase.CreateAsset(paintingQuad, meshPath);
            }
            portraitMaterial = PaintingMaterial("Gallery Portrait", Root + "/Art/Gallery Patron Portrait.png");
            stillLifeMaterial = PaintingMaterial("Studio Still Life", Root + "/Art/Studio Still Life.png");
            frameMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval Room Kit/Materials/M_Door.mat");
            if (frameMaterial == null) throw new InvalidOperationException("Imported wood material for frames is missing");
        }

        private static Material PaintingMaterial(string name, string imagePath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            if (texture == null) throw new InvalidOperationException("Painting texture missing: " + imagePath);
            string path = Root + "/Materials/Paintings/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AnimationClip Clip(string sex, string category, string filename)
        {
            string path = Motions + sex + "/" + category + "/" + filename + ".fbx";
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(x => !x.name.StartsWith("__preview__"));
            if (clip == null) throw new InvalidOperationException("Humanoid motion clip missing: " + path);
            return clip;
        }

        private static AnimatorController CreateController(string name, string sex, bool player)
        {
            string path = Root + "/Animations/" + name + ".controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            if (player)
            {
                controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
                controller.AddParameter("Jump", AnimatorControllerParameterType.Bool);
                controller.AddParameter("FreeFall", AnimatorControllerParameterType.Bool);
            }
            var stateMachine = controller.layers[0].stateMachine;
            var locomotion = stateMachine.AddState("Idle / Walk / Run");
            var tree = new BlendTree { name = name + " Blend", blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(Clip(sex, "Idles", "Human" + (sex == "Female" ? "F" : "M") + "@Idle01"), 0);
            tree.AddChild(Clip(sex, "Movement/Walk", "Human" + (sex == "Female" ? "F" : "M") + "@Walk01_Forward"), player ? 2.6f : 1f);
            if (player)
                tree.AddChild(Clip(sex, "Movement/Run", "HumanF@Run01_Forward"), 5.6f);
            locomotion.motion = tree;
            stateMachine.defaultState = locomotion;
            if (player)
            {
                var jump = stateMachine.AddState("Jump");
                jump.motion = Clip(sex, "Movement/Jump", "HumanF@Jump01");
                var fall = stateMachine.AddState("Fall");
                fall.motion = Clip(sex, "Movement/Jump", "HumanF@Fall01");
                var toJump = stateMachine.AddAnyStateTransition(jump);
                toJump.hasExitTime = false;
                toJump.duration = .08f;
                toJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
                var toFall = jump.AddTransition(fall);
                toFall.hasExitTime = false;
                toFall.duration = .08f;
                toFall.AddCondition(AnimatorConditionMode.If, 0, "FreeFall");
                var toGround = fall.AddTransition(locomotion);
                toGround.hasExitTime = false;
                toGround.duration = .1f;
                toGround.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject CreateCharacterPrefab(string role, string sourcePath, AnimatorController controller)
        {
            var wrapper = new GameObject(role);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(sourcePath));
            model.transform.SetParent(wrapper.transform, false);
            model.transform.localPosition = Vector3.zero;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !MaterialCopies.TryGetValue(materials[i], out var replacement))
                        throw new InvalidOperationException(role + " has an unmapped material: " + renderer.name);
                    materials[i] = replacement;
                }
                renderer.sharedMaterials = materials;
            }
            var animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException(role + " does not have a valid Humanoid avatar");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            string path = Root + "/Prefabs/" + role + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, path);
            UnityEngine.Object.DestroyImmediate(wrapper);
            if (prefab == null) throw new InvalidOperationException("Could not save character prefab: " + path);
            return prefab;
        }

        private static Bounds BoundsOf(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Prefab has no renderer: " + instance.name);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static GameObject Place(string relativePath, Transform parent, string name,
            float x, float z, float yaw = 0, bool solid = false, bool floor = false, float baseY = 0)
        {
            var wrapper = new GameObject(name);
            wrapper.transform.SetParent(parent);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(Kit + relativePath));
            model.transform.SetParent(wrapper.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            Bounds bounds = BoundsOf(model);
            Vector3 delta = new Vector3(x - bounds.center.x,
                baseY - (floor ? bounds.max.y : bounds.min.y), z - bounds.center.z);
            wrapper.transform.position += delta;
            bounds = BoundsOf(model);
            if (solid || floor)
            {
                var collider = wrapper.AddComponent<BoxCollider>();
                Vector3 center = floor ? new Vector3(bounds.center.x, baseY - .12f, bounds.center.z) : bounds.center;
                collider.center = wrapper.transform.InverseTransformPoint(center);
                collider.size = floor ? new Vector3(bounds.size.x, .24f, bounds.size.z) : bounds.size;
            }
            return wrapper;
        }

        private static void Room(Transform zone, int xMin, int xMax, int zMin, int zMax,
            string openingSide, int openingCenter, bool windows, float ceilingY)
        {
            var floors = Group("Modular floors", zone);
            var walls = Group("Modular perimeter", zone);
            var ceilings = Group("Modular ceiling", zone);
            for (int x = xMin + 2; x < xMax; x += 4)
                for (int z = zMin + 2; z < zMax; z += 4)
                {
                    Place("Floor/Floor_4x4.prefab", floors, "Stone and wood floor " + x + "," + z, x, z, 0, false, true);
                    Place("Ceiling/SM_Ceiling_4x4.prefab", ceilings, "Ceiling panel " + x + "," + z,
                        x, z, 0, false, false, ceilingY);
                }
            for (int x = xMin + 2; x < xMax; x += 4)
            {
                if (openingSide != "South" || x != openingCenter)
                    Wall(walls, "South", x, zMin, 0, windows && x % 8 == 2);
                if (openingSide != "North" || x != openingCenter)
                    Wall(walls, "North", x, zMax, 0, windows && x % 8 == 2);
            }
            for (int z = zMin + 2; z < zMax; z += 4)
            {
                if (openingSide != "West" || z != openingCenter)
                    Wall(walls, "West", xMin, z, 90, windows && z % 8 == 2);
                if (openingSide != "East" || z != openingCenter)
                    Wall(walls, "East", xMax, z, 90, windows && z % 8 == 2);
            }
        }

        private static void Wall(Transform parent, string side, float x, float z, float yaw, bool window)
        {
            Place(window ? "Wall/Wall_4m_Window.prefab" : "Wall/Wall_4m.prefab", parent,
                side + (window ? " tall window wall " : " stone wall ") + x + "," + z, x, z, yaw, true);
        }

        private static void BuildEnvironment()
        {
            var hall = Group("GrandHall", environment);
            var studio = Group("ArtStudio", environment);
            var gallery = Group("RestrictedGallery", environment);
            var ballroom = Group("Ballroom", environment);
            var corridors = Group("Corridors", environment);
            Room(hall, -8, 8, -8, 8, "West", -2, true, 3.08f);
            // Second hall opening to the gallery and third to the ballroom.
            AddHallOpenings(hall);
            Room(studio, -24, -12, -8, 4, "East", -2, true, 3.08f);
            Room(gallery, -8, 4, 12, 24, "South", -2, false, 3.08f);
            Room(ballroom, 12, 28, -8, 8, "West", -2, true, 3.08f);
            BuildCorridors(corridors);
            FurnishHall(hall);
            FurnishStudio(studio);
            FurnishGallery(gallery);
            FurnishBallroom(ballroom);
        }

        private static void AddHallOpenings(Transform hall)
        {
            // Remove only two modular wall wrappers; the 4 m openings align with corridor tiles.
            foreach (Transform child in hall.Find("Modular perimeter"))
                if (child.name == "North stone wall -2,8" || child.name == "East stone wall 8,-2" ||
                    child.name == "North tall window wall -2,8" || child.name == "East tall window wall 8,-2")
                { UnityEngine.Object.DestroyImmediate(child.gameObject); break; }
            foreach (Transform child in hall.Find("Modular perimeter"))
                if (child.name.Contains(" 8,-2") && child.name.StartsWith("East"))
                { UnityEngine.Object.DestroyImmediate(child.gameObject); break; }
        }

        private static void BuildCorridors(Transform corridors)
        {
            Place("Floor/Floor_4x4.prefab", corridors, "West corridor floor", -10, -2, 0, false, true);
            Place("Floor/Floor_4x4.prefab", corridors, "East corridor floor", 10, -2, 0, false, true);
            Place("Floor/Floor_4x4.prefab", corridors, "North corridor floor", -2, 10, 0, false, true);
            foreach (int x in new[] { -10, 10 })
            {
                Wall(corridors, "Corridor south", x, -4, 0, false);
                Wall(corridors, "Corridor north", x, 0, 0, false);
            }
            Wall(corridors, "Corridor west", -4, 10, 90, false);
            Wall(corridors, "Corridor east", 0, 10, 90, false);
            foreach (var point in new[] { new Vector2(-10, -2), new Vector2(10, -2), new Vector2(-2, 10) })
                Place("Ceiling/SM_Ceiling_4x4.prefab", corridors, "Corridor ceiling " + point,
                    point.x, point.y, 0, false, false, 3.08f);
        }

        private static void FurnishHall(Transform hall)
        {
            var props = Group("Formal furniture and decoration", hall);
            foreach (int x in new[] { -6, 6 })
                foreach (int z in new[] { -6, 6 })
                    Place("Column/Column_1.prefab", props, "Hall stone column " + x + "," + z, x, z, 0, true);
            Place("Table_Chairs/Chair.prefab", props, "Entrance seat west", -5, -3, 90, true);
            Place("Table_Chairs/Chair.prefab", props, "Entrance seat east", 5, -3, -90, true);
            Place("Fireplace/Fireplace.prefab", props, "Hall fireplace", 2, 7.3f, 180, true);
            Place("Door/Door.prefab", props, "Closed west academy door", -7.75f, 4, 90, true);
            Place("Wall-mounted_torch/Wall-mounted_torch.prefab", props, "Hall torch west", -7.65f, -5, 90);
            Place("Wall-mounted_torch/Wall-mounted_torch.prefab", props, "Hall torch east", 7.65f, 4, -90);
            Place("Door/Door.prefab", props, "Closed academy entrance", 0, -7.7f, 0, true);
            Painting(props, "Academy patron portrait", portraitMaterial, new Vector3(4, 1.65f, -7.76f), 0);
        }

        private static void FurnishStudio(Transform studio)
        {
            var props = Group("Studio tables, books and supplies", studio);
            Place("Table_Chairs/Table.prefab", props, "Painting worktable one", -20, -4, 0, true);
            Place("Table_Chairs/Table.prefab", props, "Painting worktable two", -16, 2, 0, true);
            Place("Table_Chairs/Chair.prefab", props, "Studio chair", -20, -2, 25, true);
            Place("Shelf/Large_Shelf.prefab", props, "Studio materials shelf", -22.5f, 2, 90, true);
            Place("Chests/Chests.prefab", props, "Stored art supplies", -22, -6, 0, true);
            Place("Books/Books_3.prefab", props, "Sketchbooks", -17.2f, 2, 0, false, false, .95f);
            Place("Barrel/Barrel.prefab", props, "Studio supply barrel", -14, -6, 0, true);
            Place("Wall-mounted_torch/Wall-mounted_torch.prefab", props, "Studio torch", -23.6f, 0, 90);
            Painting(props, "Unfinished still life study", stillLifeMaterial, new Vector3(-18, 1.65f, -7.76f), 0);
        }

        private static void FurnishGallery(Transform gallery)
        {
            var props = Group("Old gallery furnishings", gallery);
            Place("Shelf/Large_Shelf.prefab", props, "Gallery archive shelf west", -6.5f, 17, 90, true);
            Place("Shelf/Large_Shelf.prefab", props, "Gallery archive shelf east", 2.5f, 20, -90, true);
            Place("Chests/Chests.prefab", props, "Locked gallery chest", -5.8f, 22, 0, true);
            Place("Door/Door_1.prefab", props, "Sealed gallery door", -6, 23.7f, 180, true);
            Place("Wall-mounted_torch/Wall-mounted_torch.prefab", props, "Gallery torch", -7.6f, 14, 90);
            Painting(props, "Restricted portrait one", portraitMaterial, new Vector3(3.76f, 1.65f, 16), -90);
            Painting(props, "Restricted portrait two", portraitMaterial, new Vector3(-7.76f, 1.65f, 20), 90);
        }

        private static void Painting(Transform parent, string name, Material artwork, Vector3 position, float yaw)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent);
            group.position = position;
            group.rotation = Quaternion.Euler(0, yaw, 0);
            PaintingPart(group, "Oil canvas", artwork, new Vector3(0, 0, .01f), new Vector3(1.28f, 1.88f, 1));
            PaintingPart(group, "Wood frame left", frameMaterial, new Vector3(-.69f, 0, .02f), new Vector3(.10f, 2.05f, 1));
            PaintingPart(group, "Wood frame right", frameMaterial, new Vector3(.69f, 0, .02f), new Vector3(.10f, 2.05f, 1));
            PaintingPart(group, "Wood frame top", frameMaterial, new Vector3(0, 1, .02f), new Vector3(1.48f, .10f, 1));
            PaintingPart(group, "Wood frame bottom", frameMaterial, new Vector3(0, -1, .02f), new Vector3(1.48f, .10f, 1));
        }

        private static void PaintingPart(Transform parent, string name, Material material, Vector3 localPosition, Vector3 scale)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = paintingQuad;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void FurnishBallroom(Transform ballroom)
        {
            var props = Group("Symmetrical ballroom furnishings", ballroom);
            foreach (int x in new[] { 14, 26 })
                foreach (int z in new[] { -6, 6 })
                    Place("Column/Column_2.prefab", props, "Ballroom column " + x + "," + z, x, z, 0, true);
            Place("Fireplace/Fireplace.prefab", props, "Ballroom focal fireplace", 20, 7.2f, 180, true);
            Place("Table_Chairs/Chair_1.prefab", props, "Ballroom seat north", 16, 5, 0, true);
            Place("Table_Chairs/Chair_1.prefab", props, "Ballroom seat south", 24, -5, 180, true);
            Place("Door/Door.prefab", props, "Closed ballroom wing door", 27.7f, -2, -90, true);
            Place("Wall-mounted_torch/Wall-mounted_torch.prefab", props, "Ballroom torch north", 16, 7.7f, 180);
            Place("Wall-mounted_torch/Wall-mounted_torch.prefab", props, "Ballroom torch south", 24, -7.7f, 0);
        }

        private static void BuildCharacters(GameObject evelynPrefab, GameObject celestePrefab, GameObject adrianPrefab)
        {
            var starter = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(Starters + "PlayerCapsule.prefab"));
            starter.name = "Evelyn Moreau - player controller";
            starter.transform.SetParent(playerGroup);
            starter.transform.position = new Vector3(0, .02f, -2);
            var oldVisual = starter.transform.Find("Capsule");
            if (oldVisual != null) oldVisual.gameObject.SetActive(false);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(evelynPrefab, starter.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            var modelAnimator = visual.GetComponentInChildren<Animator>(true);
            var controller = starter.GetComponent<ThirdPersonController>();
            var capsule = starter.GetComponent<CharacterController>();
            var input = starter.GetComponent<PlayerInput>();
            var starterInputs = starter.GetComponent<StarterAssetsInputs>();
            if (controller == null || capsule == null || input == null || starterInputs == null || modelAnimator == null)
                throw new InvalidOperationException("Evelyn player prefab lacks required Starter Assets or Animator components");
            capsule.stepOffset = .3f;
            controller.MoveSpeed = 2.6f;
            controller.SprintSpeed = 5.6f;
            controller.GroundLayers = 1;
            controller.CinemachineCameraTarget = starter.transform.Find("PlayerCameraRoot")?.gameObject;
            if (controller.CinemachineCameraTarget == null) throw new InvalidOperationException("Player camera target missing");
            controller.CinemachineCameraTarget.transform.localPosition = new Vector3(0, 1.55f, 0);
            controller.TopClamp = 65;
            controller.BottomClamp = -32;
            controller.CameraAngleOverride = 4;
            input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Actions);
            input.defaultActionMap = "Player";
            input.defaultControlScheme = "KeyboardMouse";
            input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            starter.AddComponent<VelvetVows.AcademyPlayerInputRelay>().Configure(input, starterInputs);
            starter.AddComponent<VelvetVows.AcademyCharacterAnimation>().Configure(modelAnimator, controller, capsule);

            PlaceNpc(celestePrefab, "Celeste - art studio", characterGroup, -18, -2, 90);
            PlaceNpc(adrianPrefab, "Adrian Valmont - ballroom", characterGroup, 20, 3, 180);
            var main = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(Starters + "MainCamera.prefab"));
            main.transform.SetParent(cameras);
            main.transform.position = starter.transform.position + new Vector3(0, 2.1f, -4.8f);
            main.GetComponent<Camera>().nearClipPlane = .08f;
            var rig = new GameObject("Evelyn Third Person Camera (Cinemachine 3)");
            rig.transform.SetParent(cameras);
            rig.transform.position = main.transform.position;
            var cm = rig.AddComponent<CinemachineCamera>();
            cm.Follow = controller.CinemachineCameraTarget.transform;
            cm.LookAt = controller.CinemachineCameraTarget.transform;
            cm.Lens.FieldOfView = 55;
            cm.Lens.NearClipPlane = .08f;
            var follow = rig.AddComponent<CinemachineThirdPersonFollow>();
            follow.ShoulderOffset = new Vector3(.3f, .2f, 0);
            follow.VerticalArmLength = .25f;
            follow.CameraSide = .5f;
            follow.CameraDistance = 4.8f;
            follow.Damping = new Vector3(.12f, .18f, .22f);
            follow.AvoidObstacles.Enabled = true;
            follow.AvoidObstacles.CollisionFilter = 1;
            follow.AvoidObstacles.IgnoreTag = "Player";
            follow.AvoidObstacles.CameraRadius = .22f;
        }

        private static void PlaceNpc(GameObject prefab, string name, Transform parent, float x, float z, float yaw)
        {
            var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            npc.name = name;
            npc.transform.position = new Vector3(x, 0, z);
            npc.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var animator = npc.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException(name + " has no Animator Controller");
            var bounds = BoundsOf(npc);
            npc.transform.position += Vector3.up * -bounds.min.y;
            var collider = npc.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0, .85f, 0);
            collider.height = 1.7f;
            collider.radius = .32f;
        }

        private static void BuildLighting()
        {
            var daylight = new GameObject("Cool window daylight");
            daylight.transform.SetParent(lighting);
            daylight.transform.rotation = Quaternion.Euler(40, -35, 0);
            var sun = daylight.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(.73f, .78f, .84f);
            sun.intensity = .55f;
            foreach (var info in new[] {
                new Vector4(-4, 2.7f, -4, 8), new Vector4(4, 2.7f, 4, 8),
                new Vector4(-18, 2.7f, -2, 9), new Vector4(-2, 2.7f, 18, 5),
                new Vector4(16, 2.7f, -4, 8), new Vector4(24, 2.7f, 4, 8),
                new Vector4(-10, 2.7f, -2, 5), new Vector4(10, 2.7f, -2, 5),
                new Vector4(-2, 2.7f, 10, 5) })
            {
                var lamp = new GameObject("Warm academy lamp " + info.x + "," + info.z);
                lamp.transform.SetParent(lighting);
                lamp.transform.position = new Vector3(info.x, info.y, info.z);
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1, .72f, .46f);
                light.intensity = info.w;
                light.range = 8;
            }
        }

        [MenuItem("Velvet Vows/Validate Main Scene")]
        public static void Validate()
        {
            var errors = new List<string>();
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonController>();
            if (player == null) errors.Add("Evelyn ThirdPersonController missing");
            else
            {
                var input = player.GetComponent<PlayerInput>();
                if (input == null || input.actions == null || input.defaultActionMap != "Player")
                    errors.Add("Evelyn PlayerInput or actions missing");
                else
                {
                    if (AssetDatabase.GetAssetPath(input.actions) != Actions) errors.Add("Wrong player Input Actions asset");
                    foreach (var required in new[] { "<Keyboard>/w", "<Keyboard>/a", "<Keyboard>/s", "<Keyboard>/d",
                                 "<Keyboard>/leftShift", "<Keyboard>/space", "<Pointer>/delta" })
                        if (!input.actions.actionMaps.SelectMany(map => map.actions)
                            .SelectMany(action => action.bindings).Any(binding => binding.effectivePath == required))
                            errors.Add("Missing input binding " + required);
                }
                if (player.GetComponent<CharacterController>() == null || player.GetComponent<StarterAssetsInputs>() == null ||
                    player.GetComponent<VelvetVows.AcademyPlayerInputRelay>() == null ||
                    !player.GetComponent<VelvetVows.AcademyPlayerInputRelay>().HasReferences)
                    errors.Add("Evelyn controller or input relay missing");
                if (player.GetComponent<VelvetVows.AcademyCharacterAnimation>() == null)
                    errors.Add("Evelyn animation bridge missing");
                if (player.CinemachineCameraTarget == null) errors.Add("Evelyn camera target missing");
            }
            var mainCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            var cmCameras = UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            if (mainCameras.Length != 1 || mainCameras[0].GetComponent<CinemachineBrain>() == null)
                errors.Add("Exactly one Main Camera with CinemachineBrain required");
            if (cmCameras.Length != 1 || player == null || player.CinemachineCameraTarget == null ||
                cmCameras[0].Follow != player.CinemachineCameraTarget.transform ||
                cmCameras[0].LookAt != player.CinemachineCameraTarget.transform ||
                cmCameras[0].GetComponent<CinemachineThirdPersonFollow>() == null ||
                !cmCameras[0].GetComponent<CinemachineThirdPersonFollow>().AvoidObstacles.Enabled)
                errors.Add("Cinemachine 3 follow setup invalid");
            var animators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            if (animators.Length != 3) errors.Add("Expected Evelyn, Celeste and Adrian Animators");
            foreach (var animator in animators)
                if (animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid ||
                    animator.runtimeAnimatorController == null || animator.applyRootMotion)
                    errors.Add("Invalid humanoid Animator: " + animator.name);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || material.shader.name != "Universal Render Pipeline/Lit" ||
                        material.GetTexture("_BaseMap") == null)
                        errors.Add("Invalid character material or texture: " + renderer.name);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || !material.shader.isSupported ||
                        (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") == null))
                        errors.Add("Missing or unsupported environment material/texture: " + renderer.name);
            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                    errors.Add("Missing script on " + transform.name);
            foreach (string zone in new[] { "GrandHall", "ArtStudio", "RestrictedGallery", "Ballroom", "Corridors" })
                if (GameObject.Find("Velvet Vows Main/ENVIRONMENT/" + zone) == null)
                    errors.Add("Missing zone: " + zone);
            Physics.SyncTransforms();
            foreach (var point in new[] { new Vector2(0, -4), new Vector2(-10, -2), new Vector2(-18, -2),
                         new Vector2(-2, 10), new Vector2(-2, 18), new Vector2(10, -2), new Vector2(20, 0) })
                if (!Physics.Raycast(new Vector3(point.x, 2, point.y), Vector3.down, 3, 1, QueryTriggerInteraction.Ignore))
                    errors.Add("No floor collider at " + point);
            foreach (var passage in new[] { new[] { new Vector2(-6, -2), new Vector2(-14, -2) },
                         new[] { new Vector2(-2, 6), new Vector2(-2, 14) },
                         new[] { new Vector2(6, -2), new Vector2(14, -2) } })
            {
                var start = new Vector3(passage[0].x, 1, passage[0].y);
                var end = new Vector3(passage[1].x, 1, passage[1].y);
                if (Physics.Raycast(start, (end - start).normalized, Vector3.Distance(start, end), 1,
                        QueryTriggerInteraction.Ignore))
                    errors.Add("Blocked passage " + passage[0] + " to " + passage[1]);
            }
            if (errors.Count > 0) throw new InvalidOperationException("Main scene validation failed: " + string.Join("; ", errors));
            Debug.Log("Velvet Vows main scene validation passed: character, animation, camera, materials, zones and route floor samples.");
        }
    }
}
