using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VelvetVows.Editor
{
    public static class Phase2BInstaller
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string BackupPath = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase2B.unity";
        private const string Anim = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/";
        private const string MaterialRoot = "Assets/VelvetVows/Materials/Phase2B";
        private static readonly Color Ivory = new Color(.94f, .87f, .75f);
        private static readonly Color Rose = new Color(.75f, .35f, .46f);
        private static Font font;

        [MenuItem("Velvet Vows/Install Phase 2B Murder and Cleanup")]
        public static void Install()
        {
            if (!System.IO.File.Exists(BackupPath)) throw new Exception("Phase 2B backup must exist before installation");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (GameObject.Find("PHASE 2B CLEANUP") != null) throw new Exception("Phase 2B already installed");
            var story = UnityEngine.Object.FindFirstObjectByType<Phase2AStoryManager>();
            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
            var dialogue = UnityEngine.Object.FindFirstObjectByType<DialogueManager>();
            if (story == null || game == null || hud == null || dialogue == null) throw new Exception("Accepted Phase 2A scene is missing");
            var core = game.transform;
            var root = new GameObject("PHASE 2B CLEANUP"); root.transform.SetParent(core, false);
            var murder = core.gameObject.AddComponent<Phase2BMurderSequence>();
            var cleanup = core.gameObject.AddComponent<Phase2BCleanupManager>();
            var prefectObject = Find("Velvet Vows Main/CHARACTERS/AmbientNPCs/Prefect - Lucien's classmate");
            var prefect = prefectObject.gameObject.AddComponent<Phase2BPrefectInvestigation>();
            story.phase2B = murder;
            murder.game = game; murder.hud = hud; murder.lovesick = story.lovesick; murder.cleanup = cleanup;
            murder.evelyn = story.player; murder.celeste = story.celeste;
            murder.evelynAnimator = story.player.GetComponentInChildren<Animator>();
            murder.celesteAnimator = story.celeste.GetComponentInChildren<Animator>();
            murder.celesteMotion = story.celesteMotion;
            murder.playerAnimation = story.player.GetComponent<AcademyCharacterAnimation>();
            murder.celesteController = story.celeste.GetComponent<CharacterController>();
            murder.attack = Clip(Anim + "1H/HumanF@Attack1H01_R.fbx", "HumanF@Attack1H01_R");
            murder.damage = Clip(Anim + "HumanF@CombatDamage01.fbx", "HumanF@CombatDamage01");
            murder.death = Clip(Anim + "HumanF@Death01.fbx", "HumanF@Death01");
            murder.cinematicCamera = Camera.main;
            cleanup.game = game; cleanup.hud = hud; cleanup.dialogue = dialogue; cleanup.murder = murder;
            cleanup.player = story.player; cleanup.celeste = story.celeste; cleanup.studioDoor = story.studioDoor;
            prefect.cleanup = cleanup; prefect.normalPatrol = prefectObject.GetComponent<PatrolNPC>();
            prefect.player = story.player; prefect.body = story.celeste;
            prefect.studioDoor = story.studioDoor;
            prefect.animator = prefectObject.GetComponentInChildren<Animator>();
            prefect.approachSpeed = .9f;
            cleanup.prefect = prefect;

            var bloodMaterial = CreateBloodMaterial();
            var markMaterial = CreateUrpMaterial("Clothing blood marks", new Color(.23f, .025f, .045f, 1), false);
            var steel = CreateUrpMaterial("Palette knife steel", new Color(.36f, .37f, .39f, 1), false);
            var wood = RequireMaterial("Assets/VelvetVows/Materials/Dark Walnut.mat");

            var hand = murder.evelynAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null) throw new Exception("Evelyn's right Humanoid hand is missing");
            var prop = new GameObject("Art Studio palette knife"); prop.transform.SetParent(hand, false);
            Primitive(PrimitiveType.Cube, "Wooden handle", prop.transform, new Vector3(0, 0, .07f), new Vector3(.035f, .035f, .18f), wood);
            Primitive(PrimitiveType.Cube, "Short steel blade", prop.transform, new Vector3(0, 0, .245f), new Vector3(.04f, .012f, .20f), steel);
            murder.attackProp = prop; cleanup.attackProp = prop; prefect.attackProp = prop;
            var chest = murder.evelynAnimator.GetBoneTransform(HumanBodyBones.UpperChest) ?? murder.evelynAnimator.GetBoneTransform(HumanBodyBones.Chest);
            if (chest == null) throw new Exception("Evelyn's Humanoid chest is missing");
            var marks = new GameObject("Reversible blood marks on Evelyn's uniform"); marks.transform.SetParent(chest, false);
            Primitive(PrimitiveType.Cube, "Jacket stain", marks.transform, new Vector3(.08f, .02f, .20f), new Vector3(.13f, .08f, .009f), markMaterial);
            Primitive(PrimitiveType.Cube, "Small jacket stain", marks.transform, new Vector3(-.08f, -.07f, .21f), new Vector3(.06f, .045f, .009f), markMaterial);
            murder.clothingMarks = marks; cleanup.clothingMarks = marks; prefect.clothingMarks = marks;

            var bodyInteraction = new GameObject("Celeste body interaction"); bodyInteraction.transform.SetParent(root.transform);
            bodyInteraction.transform.position = new Vector3(-17.2f, .5f, -2.95f);
            AddInteraction(bodyInteraction, cleanup, Phase2BAction.Body, 1.05f);
            murder.bodyInteraction = bodyInteraction;
            var pool = MakeBloodPool(root.transform, bloodMaterial);
            murder.bloodPool = pool; cleanup.blood = pool.GetComponent<Phase2BBloodEvidence>(); prefect.bloodPool = pool.transform;
            AddInteraction(pool, cleanup, Phase2BAction.Blood, 1.1f);

            var wardrobe = Prefab("Assets/VNB - Home Set/Prefabs/Bedroom/Wardrobe.prefab", "Studio storage wardrobe", new Vector3(-22.6f, 0, -2.2f), root.transform, scene);
            PrefabUtility.UnpackPrefabInstance(wardrobe, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            cleanup.wardrobe = wardrobe.transform;
            cleanup.wardrobeLeftDoor = MakeDoorPivot(wardrobe.transform, "Door (1)", -.29f);
            cleanup.wardrobeRightDoor = MakeDoorPivot(wardrobe.transform, "Door (2)", .29f);
            var cloth = Prefab("Assets/VNB - Home Set/Prefabs/Bathroom/Towel Pile.prefab", "Clean studio cloth", new Vector3(-18.0f, .11f, -5.5f), root.transform, scene);
            cleanup.worldCloth = cloth; AddInteraction(cloth, cleanup, Phase2BAction.Cloth, .9f);
            var leftHand = murder.evelynAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            var heldCloth = Prefab("Assets/VNB - Home Set/Prefabs/Bathroom/Towel Pile.prefab", "Held studio cloth", Vector3.zero, leftHand, scene);
            heldCloth.transform.localPosition = new Vector3(0, .06f, .06f); heldCloth.transform.localRotation = Quaternion.Euler(40, 0, 0);
            heldCloth.transform.localScale *= .55f;
            foreach (var collider in heldCloth.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var stain = Primitive(PrimitiveType.Cube, "Visible cloth stain", heldCloth.transform, new Vector3(0, .10f, 0), new Vector3(.14f, .006f, .13f), markMaterial);
            cleanup.heldCloth = heldCloth; cleanup.clothStain = stain;
            var bin = Prefab("Assets/VNB - Home Set/Prefabs/Kitchen/Trash Bin.prefab", "Studio disposal bin", new Vector3(-20.9f, 0, -.95f), root.transform, scene);
            AddInteraction(bin, cleanup, Phase2BAction.Disposal, .95f);
            var sink = Prefab("Assets/VNB - Home Set/Prefabs/Bathroom/Sink.prefab", "Studio wash basin", new Vector3(-18.75f, 0, -.95f), root.transform, scene);
            AddInteraction(sink, cleanup, Phase2BAction.Clothing, 1.05f);

            var canvas = hud.GetComponent<Canvas>();
            font = Find("Gameplay Canvas/Gameplay HUD/Objective").GetComponent<Text>().font;
            var checklistPanel = Panel("Phase 2B checklist", canvas.transform, new Color(.065f, .055f, .075f, .84f));
            var checklistRect = checklistPanel.GetComponent<RectTransform>();
            checklistRect.anchorMin = checklistRect.anchorMax = checklistRect.pivot = new Vector2(1, 1);
            checklistRect.sizeDelta = new Vector2(335, 180); checklistRect.anchoredPosition = new Vector2(-28, -185);
            var checklist = Label("Cleanup objectives", checklistPanel.transform, 17, Ivory, TextAnchor.UpperLeft);
            Stretch(checklist.rectTransform); checklist.rectTransform.offsetMin = new Vector2(17, 14); checklist.rectTransform.offsetMax = new Vector2(-12, -14);
            cleanup.checklist = checklist; cleanup.checklistPanel = checklistPanel;
            var outcome = Panel("Phase 2B outcome", canvas.transform, new Color(.07f, .055f, .075f, .97f));
            Box(outcome.GetComponent<RectTransform>(), new Vector2(680, 310), Vector2.zero);
            var title = Label("Phase 2B outcome title", outcome.transform, 36, Ivory, TextAnchor.MiddleCenter);
            Box(title.rectTransform, new Vector2(640, 88), new Vector2(0, 65));
            var subtitle = Label("Phase 2B outcome subtitle", outcome.transform, 20, Ivory, TextAnchor.MiddleCenter);
            Box(subtitle.rectTransform, new Vector2(610, 90), new Vector2(0, -22));
            var retry = Button("RETRY", outcome.transform, new Vector2(180, 45), new Vector2(0, -116));
            cleanup.outcomePanel = outcome; cleanup.outcomeTitle = title; cleanup.outcomeSubtitle = subtitle; cleanup.retryButton = retry;
            checklistPanel.SetActive(false); outcome.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Could not save Phase 2B scene");
            Debug.Log("[Phase2B] Installed murder and cleanup into the accepted Phase 2A scene.");
        }
        private static Transform Find(string path)
        {
            var parts = path.Split('/');
            var root = GameObject.Find(parts[0]);
            var result = parts.Length == 1 ? root?.transform : root?.transform.Find(path.Substring(parts[0].Length + 1));
            if (result == null) throw new Exception("Scene object missing: " + path);
            return result;
        }
        private static AnimationClip Clip(string path, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name == name)
            ?? throw new Exception("Humanoid clip missing: " + path + " / " + name);
        private static Material RequireMaterial(string path) => AssetDatabase.LoadAssetAtPath<Material>(path) ?? throw new Exception("Material missing: " + path);
        private static Material CreateBloodMaterial()
        {
            System.IO.Directory.CreateDirectory(MaterialRoot);
            var path = MaterialRoot + "/Dark crimson pool.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? throw new Exception("URP Unlit missing");
            if (material == null)
            {
                material = new Material(shader) { name = "Dark crimson pool" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(.26f, .018f, .038f, 1));
            material.SetFloat("_Surface", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            material.SetFloat("_ZWrite", 1);
            material.SetFloat("_Cull", 0);
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetShaderPassEnabled("DepthOnly", true);
            material.SetShaderPassEnabled("SHADOWCASTER", true);
            material.renderQueue = 2001;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(material);
            return material;
        }
        private static Material CreateUrpMaterial(string name, Color color, bool transparent)
        {
            System.IO.Directory.CreateDirectory(MaterialRoot);
            var path = MaterialRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? throw new Exception("URP Lit missing");
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .12f);
            if (transparent)
            {
                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", 0);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.renderQueue = 3000;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static GameObject Prefab(string path, string name, Vector3 position, Transform parent, Scene scene)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new Exception("Prefab missing: " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            go.name = name; go.transform.SetParent(parent, true); go.transform.position = position;
            var palette1 = RequireMaterial("Assets/VelvetVows/Materials/Phase2A/Palette 1 URP.mat");
            var palette2 = RequireMaterial("Assets/VelvetVows/Materials/Phase2A/Palette 2 URP.mat");
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].shader != null && mats[i].shader.name == "Standard")
                        mats[i] = mats[i].name == "Palette 2" ? palette2 : palette1;
                r.sharedMaterials = mats;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            return go;
        }
        private static Transform MakeDoorPivot(Transform wardrobe, string doorName, float hingeOffset)
        {
            var door = wardrobe.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == doorName);
            if (door == null) throw new Exception("Wardrobe door missing: " + doorName);
            var pivot = new GameObject(doorName + " visible hinge").transform;
            pivot.SetParent(door.parent, false);
            pivot.localPosition = door.localPosition + Vector3.right * hingeOffset;
            door.SetParent(pivot, true);
            return pivot;
        }
        private static GameObject MakeBloodPool(Transform parent, Material material)
        {
            var go = new GameObject("Celeste's blood pool", typeof(MeshFilter), typeof(MeshRenderer), typeof(Phase2BBloodEvidence));
            go.transform.SetParent(parent); go.transform.position = new Vector3(-17.2f, .045f, -3f);
            const int count = 24;
            var vertices = new Vector3[count + 1]; var triangles = new int[count * 3];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                float angle = 2 * Mathf.PI * i / count;
                float wobble = 1 + .07f * Mathf.Sin(i * 2.7f) + .05f * Mathf.Sin(i * 5.3f);
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * .78f * wobble, 0, Mathf.Sin(angle) * .56f * wobble);
                triangles[i * 3] = 0; triangles[i * 3 + 1] = (i + 1) % count + 1; triangles[i * 3 + 2] = i + 1;
            }
            const string meshPath = "Assets/VelvetVows/Materials/Phase2B/BloodPoolMesh.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Phase 2B irregular blood pool", vertices = vertices, triangles = triangles };
                mesh.RecalculateNormals();
                AssetDatabase.CreateAsset(mesh, meshPath);
            }
            else
            {
                mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals();
                EditorUtility.SetDirty(mesh);
            }
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>(); if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }
        private static void AddInteraction(GameObject go, Phase2BCleanupManager cleanup, Phase2BAction action, float radius)
        {
            var trigger = go.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = radius;
            var interaction = go.AddComponent<Phase2BInteractable>(); interaction.Configure(cleanup, action);
        }
        private static GameObject Panel(string name, Transform parent, Color color)
        { var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
          var image = go.GetComponent<Image>(); image.color = color; return go; }
        private static Text Label(string name, Transform parent, int size, Color color, TextAnchor align)
        { var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false);
          var t = go.GetComponent<Text>(); t.font = font; t.fontSize = size; t.color = color; t.alignment = align; return t; }
        private static Button Button(string caption, Transform parent, Vector2 size, Vector2 position)
        { var go = Panel(caption + " button", parent, new Color(.27f, .14f, .21f, 1)); Box(go.GetComponent<RectTransform>(), size, position);
          var button = go.AddComponent<Button>(); var label = Label(caption, go.transform, 18, Ivory, TextAnchor.MiddleCenter);
          label.text = caption; Stretch(label.rectTransform); label.raycastTarget = false; return button; }
        private static void Box(RectTransform r, Vector2 size, Vector2 position)
        { r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.sizeDelta = size; r.anchoredPosition = position; }
        private static void Stretch(RectTransform r)
        { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; }
    }
}
