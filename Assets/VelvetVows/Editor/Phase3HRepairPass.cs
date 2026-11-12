using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace VelvetVows.Editor
{
    public static partial class Phase3HRepairPass
    {
        private const string Materials = "Assets/VelvetVows/Materials/Phase3H/";
        private static Transform root, repairs;
        private static Material stone, walnut, brass;

        [MenuItem("Velvet Vows/Repair academy geometry, layout and characters")]
        public static void Apply()
        {
            const string backup = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3H.unity";
            if (!File.Exists(backup)) File.Copy(ScenePath, backup);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            root = scene.GetRootGameObjects().First(g => g.name == "Velvet Vows Main").transform;
            Directory.CreateDirectory(Materials);
            var old = root.Find("ENVIRONMENT/Phase 3H repaired academy");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            repairs = new GameObject("Phase 3H repaired academy").transform;
            repairs.SetParent(root.Find("ENVIRONMENT"), false);
            var sourceStone = root.Find("WORLD/GOTHIC_ACADEMY/Connected wings/Library/Pointed walls and windows/Library East 0-0 solid stone")
                .GetComponentInChildren<Renderer>().sharedMaterial;
            stone = CopyMaterial(sourceStone, "Vestibule stone", Color.white);
            walnut = SolidMaterial("Smoked walnut", new Color(.13f, .085f, .065f));
            brass = SolidMaterial("Aged brass", new Color(.42f, .30f, .14f));
            RemovePaintings();
            RepairEnvelope();
            RefurnishLibrary();
            RefurnishStudio();
            RefineCharacters();
            RefinePrefect();
            var voice = Object.FindFirstObjectByType<CharacterVoiceManager>();
            voice.voiceVolume = .56f;
            voice.sfxVolume = .64f;
            EditorUtility.SetDirty(voice);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save repaired main scene");
            AssetDatabase.SaveAssets();
            Refine();
            Debug.Log("[Phase3H] Saved closed academy envelope, clean layout, removed decorative paintings and refined characters.");
        }

        private static void RemovePaintings()
        {
            var prefixes = new[] { "Academy paintings", "Visible Studio Still Life", "Visible Gallery Portrait",
                "Artist easel", "Student easel", "Studio portrait study", "Studio still life wall", "Studio second study",
                "Studio centerpiece still life", "Student study", "Canvas leaning near wall", "Unfinished student canvas",
                "Unfinished still life study", "Recital painting", "Music Wing painting", "Ballroom gala painting",
                "Library academy portrait", "Missing student portrait west", "Academy portrait east", "Founder portrait north" };
            int removed = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string path = PathOf(t);
                if (!(path.Contains("/ENVIRONMENT/") || path.Contains("/WORLD/GOTHIC_ACADEMY/"))) continue;
                if (!prefixes.Any(p => t.name.StartsWith(p, StringComparison.OrdinalIgnoreCase))) continue;
                t.gameObject.SetActive(false); removed++;
            }
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string path = PathOf(renderer.transform);
                if (!(path.Contains("/ENVIRONMENT/") || path.Contains("/WORLD/GOTHIC_ACADEMY/"))) continue;
                if (!renderer.sharedMaterials.Any(m => m != null &&
                    (AssetDatabase.GetAssetPath(m).Contains("/Paintings/") || m.name.Contains("Still Life")))) continue;
                var holder = renderer.transform.parent;
                if (holder != null && holder.Cast<Transform>().Any(c => c.name == "Frame")) holder.gameObject.SetActive(false);
                else renderer.gameObject.SetActive(false);
            }
            SetInactive("SYSTEMS/Core Gameplay/World 2.0 Interactions/Studio painting");
            Debug.Log("[Phase3H] Decorative artwork roots removed: " + removed);
        }

        private static void RepairEnvelope()
        {
            var shell = Group("Continuous room collision envelope", repairs);
            // Door intervals correspond to existing playable portals. All other perimeter sections are solid.
            Envelope(shell, "Library", -26, -10, 8, 24,
                new[] { new Vector2(12.4f, 15.6f) }, new[] { new Vector2(8.4f, 11.6f) });
            Envelope(shell, "Studio", -24, -12, -8, 4, null, new[] { new Vector2(-4, -2) });
            Envelope(shell, "Grand Hall", -8, 8, -12, 8,
                new[] { new Vector2(-4, 0), new Vector2(4.4f, 7.6f) }, new[] { new Vector2(-4, 0) },
                null, new[] { new Vector2(-4, 0) });
            Envelope(shell, "Archive", -8, 4, 12, 28, null, null, new[] { new Vector2(-4, 0) });
            Envelope(shell, "Ballroom", 12, 32, -12, 12, new[] { new Vector2(-4, 0) });
            Envelope(shell, "Music Wing", -42, -30, 8, 20, null, new[] { new Vector2(12, 16) });

            var vestibule = Group("Enclosed Library vestibule", repairs);
            Block(vestibule, "Library vestibule south wall", new Vector3(-10, 3.15f, 4.0f), new Vector3(4.25f, 6.3f, .28f), stone, true);
            Block(vestibule, "Library vestibule west wall", new Vector3(-12, 3.15f, 6), new Vector3(.28f, 6.3f, 4), stone, true);
            Block(vestibule, "Library vestibule east return", new Vector3(-8, 3.15f, 9.8f), new Vector3(.28f, 6.3f, 4.4f), stone, true);
            Block(vestibule, "Library vestibule north return", new Vector3(-9, 3.15f, 12), new Vector3(2.25f, 6.3f, .28f), stone, true);
            Block(vestibule, "Library vestibule continuous ceiling", new Vector3(-10, 6.35f, 8), new Vector3(4.25f, .22f, 8.25f), stone, true);
            Collision(vestibule, "Library vestibule continuous foundation", new Vector3(-10, -.14f, 8), new Vector3(4.25f, .22f, 8.25f));
            Collision(shell, "Music passage south", new Vector3(-28, 4, 12), new Vector3(4, 8, .3f));
            Collision(shell, "Music passage north", new Vector3(-28, 4, 16), new Vector3(4, 8, .3f));
            Collision(shell, "Music passage floor", new Vector3(-28, -.15f, 14), new Vector3(4.2f, .22f, 4));
            foreach (float x in new[] { -10f, 10f })
            {
                Collision(shell, "Lower passage south " + x, new Vector3(x, 4, -4), new Vector3(4, 8, .3f));
                Collision(shell, "Lower passage north " + x, new Vector3(x, 4, 0), new Vector3(4, 8, .3f));
                Collision(shell, "Lower passage floor " + x, new Vector3(x, -.15f, -2), new Vector3(4.2f, .22f, 4));
            }
            foreach (float x in new[] { -4f, 0f })
                Collision(shell, "Archive passage side " + x, new Vector3(x, 4, 10), new Vector3(.3f, 8, 4));
            Collision(shell, "Archive passage floor", new Vector3(-2, -.15f, 10), new Vector3(4, .22f, 4.2f));
        }

        private static void Envelope(Transform parent, string name, float xmin, float xmax, float zmin, float zmax,
            Vector2[] west = null, Vector2[] east = null, Vector2[] south = null, Vector2[] north = null)
        {
            Edge(parent, name + " west", xmin, zmin, zmax, west, true);
            Edge(parent, name + " east", xmax, zmin, zmax, east, true);
            Edge(parent, name + " south", zmin, xmin, xmax, south, false);
            Edge(parent, name + " north", zmax, xmin, xmax, north, false);
            Collision(parent, name + " seam-free foundation", new Vector3((xmin + xmax) / 2, -.18f, (zmin + zmax) / 2),
                new Vector3(xmax - xmin, .25f, zmax - zmin));
            Collision(parent, name + " roof collision", new Vector3((xmin + xmax) / 2, 8.1f, (zmin + zmax) / 2),
                new Vector3(xmax - xmin, .25f, zmax - zmin));
        }
        private static void Edge(Transform parent, string name, float fixedAxis, float min, float max, Vector2[] doors, bool alongZ)
        {
            float cursor = min;
            foreach (var opening in (doors ?? Array.Empty<Vector2>()).OrderBy(d => d.x).Concat(new[] { new Vector2(max, max) }))
            {
                if (opening.x > cursor)
                {
                    float middle = (cursor + opening.x) / 2;
                    Collision(parent, name + " solid " + cursor,
                        alongZ ? new Vector3(fixedAxis, 4, middle) : new Vector3(middle, 4, fixedAxis),
                        alongZ ? new Vector3(.28f, 8, opening.x - cursor) : new Vector3(opening.x - cursor, 8, .28f));
                }
                cursor = Mathf.Max(cursor, opening.y);
            }
        }

        private static void RefurnishLibrary()
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Library aisle shelf" || t.name == "Library wall shelves" || t.name == "Librarian shelves" ||
                    t.name == "Reading chair one" || t.name == "Reading chair two") t.gameObject.SetActive(false);
            }
            var library = Group("Library fitted bookcases", repairs);
            Bookcase(library, new Vector3(-25.35f, 0, 16.2f), 90);
            Bookcase(library, new Vector3(-25.35f, 0, 21.6f), 90);
            Bookcase(library, new Vector3(-10.65f, 0, 16.8f), 270);
            Bookcase(library, new Vector3(-10.65f, 0, 22f), 270);
            var composition = root.Find("ENVIRONMENT/Visual Polish 2.1/Library reading composition");
            var chairs = composition.Cast<Transform>().Where(t => t.name == "Reading chair").ToArray();
            var positions = new[] { new Vector3(-22.1f, 0, 13.5f), new Vector3(-19.9f, 0, 13.5f),
                new Vector3(-16.3f, 0, 19.5f), new Vector3(-14.1f, 0, 19.5f) };
            for (int i = 0; i < chairs.Length; i++)
            {
                if (i >= positions.Length) { chairs[i].gameObject.SetActive(false); continue; }
                PlaceOnFloor(chairs[i], positions[i]);
                chairs[i].rotation = Quaternion.Euler(0, i % 2 == 0 ? 90 : 270, 0);
            }
            foreach (Transform child in composition)
                if (child.name == "Book collection") child.gameObject.SetActive(false);
            foreach (var name in new[] { "Trophy display plinth", "Young Artists trophy", "Exhibition table", "Exhibition vase" })
                foreach (var t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name)) t.gameObject.SetActive(false);
            SetInactive("ENVIRONMENT/GrandHall/Formal furniture and decoration/Closed west academy door");
        }

        private static void Bookcase(Transform parent, Vector3 position, float yaw)
        {
            var cabinet = Group("Fitted walnut bookcase", parent);
            cabinet.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            LocalBlock(cabinet, "Back", new Vector3(0, 1.3f, -.23f), new Vector3(2, 2.6f, .07f), walnut);
            foreach (float x in new[] { -.98f, .98f }) LocalBlock(cabinet, "Upright", new Vector3(x, 1.3f, 0), new Vector3(.08f, 2.6f, .52f), walnut);
            for (int shelf = 0; shelf < 5; shelf++)
            {
                float y = .08f + shelf * .6f;
                LocalBlock(cabinet, "Shelf", new Vector3(0, y, 0), new Vector3(2, .07f, .52f), walnut);
                if (shelf == 4) continue;
                for (int book = 0; book < 12; book++)
                {
                    float height = .31f + ((book + shelf) % 3) * .045f;
                    var colours = new[] { new Color(.20f,.055f,.075f), new Color(.09f,.14f,.12f), new Color(.23f,.17f,.11f), new Color(.34f,.29f,.22f) };
                    var material = SolidMaterial("Leather spine " + ((book + shelf) % 4), colours[(book + shelf) % 4]);
                    LocalBlock(cabinet, "Bound volume", new Vector3(-.83f + book * .146f, y + .04f + height / 2, .04f),
                        new Vector3(.115f, height, .31f), material);
                }
            }
            var collider = cabinet.gameObject.AddComponent<BoxCollider>(); collider.center = new Vector3(0, 1.3f, 0);
            collider.size = new Vector3(2.08f, 2.6f, .58f);
        }

        private static void RefurnishStudio()
        {
            var table = root.Find("ENVIRONMENT/ArtStudio/Studio tables, books and supplies/Painting worktable two");
            if (table != null) PlaceOnFloor(table, new Vector3(-19.3f, 0, 1.25f));
            var story = Object.FindFirstObjectByType<Phase2AStoryManager>();
            story.witness.SetPositionAndRotation(new Vector3(-17, 0, 0), Quaternion.Euler(0, 250, 0));
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Working sketchbook" || t.name == "Drawing pencil" || t.name == "Pigment jar" ||
                    t.name == "Studio chair") t.gameObject.SetActive(false);
            }
            var stools = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Paint-stained stool").ToArray();
            for (int i = 0; i < stools.Length; i++)
            {
                if (i >= 2) stools[i].gameObject.SetActive(false);
                else PlaceOnFloor(stools[i], new Vector3(-20.1f + 1.4f * i, 0, .05f));
            }
            var cleanup = Object.FindFirstObjectByType<Phase2BCleanupManager>();
            TintProp(cleanup.worldCloth.transform, "Linen cloth", new Color(.75f,.70f,.60f));
            var bin = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Studio disposal bin");
            if (bin != null) TintProp(bin, "Dark iron bin", new Color(.19f,.18f,.16f));
        }

        private static void RefineCharacters()
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                if (animator.avatar == null || !animator.avatar.isHuman) continue;
                string path = PathOf(animator.transform);
                bool male = animator.name.Contains("Akio");
                bool prefect = path.Contains("Prefect -");
                string role = path.Contains("/PLAYER/") ? "Evelyn" : path.Contains("Celeste - story") ? "Celeste" :
                    prefect ? "Prefect" : path.Contains("Adrian Valmont - ballroom") ? "Adrian" :
                    path.Contains("Isabelle") ? "Isabelle" : path.Contains("Amelie") ? "Amelie" :
                    path.Contains("Margot") ? "Margot" : male ? "Student male" : "Student female";
                var presentation = animator.GetComponent<AcademyCharacterPresentation>() ?? animator.gameObject.AddComponent<AcademyCharacterPresentation>();
                presentation.Configure(animator, male ? .84f : .92f);
                foreach (var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var original = materials[i]; if (original == null) continue;
                        if (prefect && (i == 7 || i == 8))
                            original = AssetDatabase.LoadAssetAtPath<Material>("Assets/VelvetVows/Materials/ImportedVariants/Male Student Burgundy_Body_" + i + ".mat");
                        bool hair = male ? i == 0 : original.name.IndexOf("hair", StringComparison.OrdinalIgnoreCase) >= 0;
                        string materialName = role + " " + renderer.name + " " + i;
                        var copy = CopyMaterial(original, materialName, Color.white);
                        if (hair)
                        {
                            copy.shader = Shader.Find("VelvetVows/Academy Matte Hair");
                            copy.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
                            Color colour = role == "Evelyn" ? new Color(.27f,.18f,.13f) : role == "Celeste" ? new Color(.38f,.29f,.17f) :
                                male ? new Color(.12f,.10f,.085f) : new Color(.22f,.14f,.10f);
                            copy.SetColor("_BaseColor", colour); copy.SetFloat("_Cutoff", .25f); copy.SetFloat("_Cull", 0);
                        }
                        else
                        {
                            bool cloth = original.name.Contains("CLOTH") || renderer.name == "Casual1" || male && (i == 7 || i == 8);
                            if (cloth) copy.SetColor("_BaseColor", prefect ? new Color(.32f,.34f,.37f) :
                                role == "Celeste" ? new Color(.46f,.42f,.39f) : new Color(.40f,.37f,.36f));
                            if (copy.HasProperty("_Smoothness")) copy.SetFloat("_Smoothness", cloth ? .06f : .17f);
                            if (copy.HasProperty("_Metallic")) copy.SetFloat("_Metallic", 0);
                            if (copy.HasProperty("_SpecularHighlights")) copy.SetFloat("_SpecularHighlights", 0);
                        }
                        EditorUtility.SetDirty(copy); materials[i] = copy;
                    }
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
                }
                EditorUtility.SetDirty(presentation);
            }
            foreach (var motion in root.GetComponentsInChildren<Phase2AActorMotion>(true))
            {
                var settings = new SerializedObject(motion); settings.FindProperty("variedIdle").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void RefinePrefect()
        {
            var prefect = Object.FindFirstObjectByType<Phase2BPrefectInvestigation>();
            prefect.transform.SetPositionAndRotation(new Vector3(4.1f, 0, 3f), Quaternion.Euler(0, 180, 0));
            var patrol = new SerializedObject(prefect.normalPatrol);
            var waypoints = patrol.FindProperty("waypoints");
            var points = new[] { new Vector3(4.1f,0,3f), new Vector3(4.1f,0,-2.5f),
                new Vector3(2.5f,0,-2.5f), new Vector3(2.5f,0,3f) };
            for (int i = 0; i < waypoints.arraySize; i++)
                ((Transform)waypoints.GetArrayElementAtIndex(i).objectReferenceValue).position = points[i % points.Length];
            patrol.FindProperty("walkSpeed").floatValue = 1.0f; patrol.ApplyModifiedPropertiesWithoutUndo();
            var animator = prefect.GetComponentInChildren<Animator>();
            var chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest);
            var previous = chest.Find("Prefect brass insignia"); if (previous != null) Object.DestroyImmediate(previous.gameObject);
            var badge = Block(chest, "Prefect brass insignia", chest.position + prefect.transform.forward * .135f +
                prefect.transform.right * .08f + Vector3.up * .03f,
                new Vector3(.035f,.05f,.008f), brass, false);
            var boneScale = chest.lossyScale;
            badge.transform.localScale = new Vector3(.035f / Mathf.Abs(boneScale.x),
                .05f / Mathf.Abs(boneScale.y), .008f / Mathf.Abs(boneScale.z));
            badge.transform.rotation = prefect.transform.rotation;
            EditorUtility.SetDirty(prefect);
        }

        public static void Refine()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            root = scene.GetRootGameObjects().First(g => g.name == "Velvet Vows Main").transform;
            repairs = root.Find("ENVIRONMENT/Phase 3H repaired academy");
            brass = AssetDatabase.LoadAssetAtPath<Material>(Materials + "Aged_brass.mat");
            walnut = AssetDatabase.LoadAssetAtPath<Material>(Materials + "Smoked_walnut.mat");
            var oldChest = Object.FindFirstObjectByType<Phase2BPrefectInvestigation>().GetComponentInChildren<Animator>()
                .GetBoneTransform(HumanBodyBones.Chest);
            var obsoleteBadge = oldChest.Find("Prefect brass insignia");
            if (obsoleteBadge != null) Object.DestroyImmediate(obsoleteBadge.gameObject);
            var northStone = root.Find("WORLD/GOTHIC_ACADEMY/Grand Hall/Walls and windows/North left stonework");
            if (northStone != null)
            {
                var renderer = northStone.GetComponentInChildren<Renderer>();
                float width = renderer.bounds.size.x;
                var scale = northStone.localScale; scale.x *= 4f / width; northStone.localScale = scale;
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string path = PathOf(t);
                if (path.Contains("/ENVIRONMENT/") && (t.name == "Modular perimeter" || t.name == "Modular ceiling"))
                    t.gameObject.SetActive(false);
                if ((path.Contains("/World 2.0 Additions/Library/") || path.Contains("/World 2.0 Additions/MusicWing/")) &&
                    t.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0) t.gameObject.SetActive(false);
                if (t.name == "Academy flowers" || t.name == "Exhibition vase") t.gameObject.SetActive(false);
            }
            foreach (var mesh in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = mesh.GetComponent<Renderer>();
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                string path = PathOf(mesh.transform);
                if (path.Contains("/WORLD/GOTHIC_ACADEMY/") && mesh.sharedMesh != null && mesh.GetComponent<Collider>() == null)
                    mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
                if (path.Contains("/ENVIRONMENT/") && (path.Contains("/Modular perimeter/") ||
                    path.Contains("/Modular ceiling/") || path.Contains("/Hall stone column ")))
                    renderer.enabled = false;
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Studio materials shelf" || t.name == "Pigment cabinet" || t.name == "Second art-supply cabinet" ||
                    t.name == "Studio supply barrel" || t.name == "Artist stool") t.gameObject.SetActive(false);
            var cleanup = Object.FindFirstObjectByType<Phase2BCleanupManager>();
            cleanup.wardrobe.rotation = Quaternion.Euler(0, 90, 0);
            cleanup.worldCloth.transform.position = new Vector3(-18f, .53f, -5.5f);
            var oldBench = repairs.Find("Linen supply bench");
            if (oldBench != null) Object.DestroyImmediate(oldBench.gameObject);
            Block(repairs, "Linen supply bench", new Vector3(-18f, .25f, -5.5f), new Vector3(.8f,.5f,.55f), walnut, true);
            foreach (var renderer in cleanup.worldCloth.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material != null) { material.SetTexture("_BaseMap", null); material.SetColor("_BaseColor", new Color(.72f,.68f,.58f)); EditorUtility.SetDirty(material); }
            foreach (var renderer in cleanup.heldCloth.GetComponentsInChildren<Renderer>(true))
                if (renderer.transform != cleanup.clothStain.transform)
                    renderer.sharedMaterial = SolidMaterial("Held linen cloth", new Color(.72f,.68f,.58f));
            cleanup.clothStain.GetComponent<Renderer>().sharedMaterial =
                cleanup.clothingMarks.GetComponentInChildren<Renderer>(true).sharedMaterial;
            RefinePrefect();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("[Phase3H] Added exact visible-architecture collision, hid duplicate old wall surfaces, cleared duplicate cabinets and refined the Prefect route.");
        }

        private static Transform Group(string name, Transform parent)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
        private static void Collision(Transform parent, string name, Vector3 position, Vector3 size)
        { var t = Group(name, parent); t.position = position; t.gameObject.AddComponent<BoxCollider>().size = size; }
        private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, true); go.transform.position = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private static void LocalBlock(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var go = Block(parent, name, Vector3.zero, size, material, false);
            go.transform.localPosition = position; go.transform.localRotation = Quaternion.identity;
        }
        private static Material SolidMaterial(string name, Color colour) => CopyMaterial(null, name, colour);
        private static Material CopyMaterial(Material original, string name, Color tint)
        {
            var safe = string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
            string path = Materials + safe + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            { material = original != null ? new Material(original) : new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            else if (original != null) material.CopyPropertiesFromMaterial(original);
            material.name = name;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .12f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0);
            EditorUtility.SetDirty(material); return material;
        }
        private static void TintProp(Transform prop, string name, Color tint)
        {
            foreach (var renderer in prop.GetComponentsInChildren<Renderer>(true))
            { var shared = renderer.sharedMaterials; for (int i = 0; i < shared.Length; i++) if (shared[i] != null)
                    shared[i] = CopyMaterial(shared[i], name + " " + i, tint); renderer.sharedMaterials = shared; }
        }
        private static void PlaceOnFloor(Transform target, Vector3 position)
        {
            target.position = position;
            var rs = target.GetComponentsInChildren<Renderer>(true);
            if (rs.Length > 0) target.position += Vector3.up * (position.y - rs.Min(r => r.bounds.min.y));
        }
        private static void SetInactive(string path) { var t = root.Find(path); if (t != null) t.gameObject.SetActive(false); }
    }
}
