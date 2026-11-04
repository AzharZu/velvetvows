using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarterAssets;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VelvetVows.Editor
{
    /// <summary>Visual-only, additive polish for the accepted 2.0 scene.</summary>
    public static class VelvetVowsVisualPolish21
    {
        private const string Live = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string Backup = "Assets/VelvetVows/Scenes/VelvetVows_Main_PreVisualPolish.unity";
        private const string Vv = "Assets/VelvetVows/";
        private const string Home = "Assets/VNB - Home Set/Prefabs/";
        private const string Kit = "Assets/Medieval Room Kit/Prefab/";
        private static Transform root, polish;
        private static Material wood, stone, charcoal, brass, burgundy, cream, green, blue, gold, rose, portrait, stillLife;
        private static Font font;
        private static readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();

        [MenuItem("Velvet Vows/Apply Visual Polish 2.1")]
        public static void Apply()
        {
            if (!File.Exists(Backup)) throw new InvalidOperationException("PreVisualPolish backup missing");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Live, OpenSceneMode.Single);
            root = GameObject.Find("Velvet Vows Main")?.transform ?? throw new InvalidOperationException("Main scene root missing");
            if (root.Find("ENVIRONMENT/Visual Polish 2.1") != null)
            { Debug.Log("[VelvetVows] Visual Polish 2.1 already applied."); return; }
            RequireGameplay();
            LoadMaterials();
            polish = Group("Visual Polish 2.1", root.Find("ENVIRONMENT"));
            RaiseCeilings();
            RemoveDecorativeChests();
            MovePhysicsChests();
            ReplaceLabels();
            GrandHall();
            Library();
            ArtStudio();
            Gallery();
            MusicWing();
            Ballroom();
            LayoutHud();
            ValidateInMemory();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[VelvetVows] Visual Polish 2.1 saved in " + Live);
        }

        private static Transform Group(string name, Transform parent)
        {
            if (parent == null) throw new InvalidOperationException("Missing parent for " + name);
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }
        private static Material Existing(string name) => AssetDatabase.LoadAssetAtPath<Material>(Vv + "Materials/" + name + ".mat") ??
            throw new InvalidOperationException("Missing material " + name);
        private static Material Variant(string name, Color color)
        {
            string path = Vv + "Materials/VisualPolish/" + name + ".mat";
            Directory.CreateDirectory(Vv + "Materials/VisualPolish");
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .18f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static void LoadMaterials()
        {
            wood = Existing("Dark Walnut"); stone = Existing("Limestone");
            charcoal = Existing("Charcoal Stone"); brass = Existing("Aged Brass");
            burgundy = Existing("Burgundy Velvet"); rose = Existing("Black Rose");
            cream = Variant("Ivory Plaster", new Color(.72f, .66f, .55f));
            green = Variant("Library Green", new Color(.12f, .19f, .16f));
            blue = Variant("Archive Slate", new Color(.18f, .22f, .27f));
            gold = Variant("Dull Gold", new Color(.56f, .42f, .2f));
            portrait = AssetDatabase.LoadAssetAtPath<Material>(Vv + "Materials/Paintings/Gallery Portrait.mat");
            stillLife = AssetDatabase.LoadAssetAtPath<Material>(Vv + "Materials/Paintings/Studio Still Life.mat");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        private static GameObject Block(string name, Transform parent, Vector3 pos, Vector3 size,
            Material material, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent);
            go.transform.position = pos; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }
        private static void ImportMaterials(GameObject obj)
        {
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                var shared = renderer.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    var original = shared[i];
                    if (original == null) continue;
                    string source = AssetDatabase.GetAssetPath(original);
                    if (source.StartsWith(Vv, StringComparison.Ordinal)) continue;
                    if (!converted.TryGetValue(original, out var copy))
                    {
                        string safe = string.Concat(source.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
                        string path = Vv + "Materials/ImportedVariants/" + safe + ".mat";
                        copy = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (copy == null)
                        {
                            Directory.CreateDirectory(Vv + "Materials/ImportedVariants");
                            copy = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            copy.name = original.name + " URP";
                            var texture = original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") :
                                original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") : null;
                            if (texture != null) copy.SetTexture("_BaseMap", texture);
                            Color tint = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") :
                                original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white;
                            copy.SetColor("_BaseColor", tint);
                            AssetDatabase.CreateAsset(copy, path);
                        }
                        converted.Add(original, copy);
                    }
                    shared[i] = copy;
                }
                renderer.sharedMaterials = shared;
            }
        }
        private static GameObject Prop(string assetPath, Transform parent, string name, Vector3 pos,
            float yaw = 0, bool floorAlign = true, bool solid = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) ??
                throw new InvalidOperationException("Missing decoration prefab " + assetPath);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.name = name; obj.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            ImportMaterials(obj);
            if (floorAlign)
            {
                var renderers = obj.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0) obj.transform.position += Vector3.up * (pos.y - renderers.Min(r => r.bounds.min.y));
            }
            if (!solid)
                foreach (var collider in obj.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
            else if (obj.GetComponentsInChildren<Collider>(true).Length == 0)
            {
                var renderers = obj.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    var collider = obj.AddComponent<BoxCollider>();
                    collider.center = obj.transform.InverseTransformPoint(bounds.center);
                    collider.size = bounds.size;
                }
            }
            foreach (var child in obj.GetComponentsInChildren<Transform>(true)) child.gameObject.isStatic = true;
            return obj;
        }
        private static void Light(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = Group(name, parent).gameObject;
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point; light.color = color; light.intensity = intensity;
            light.range = range; light.shadows = LightShadows.None;
        }
        private static void Artwork(Transform parent, string name, Vector3 pos, float yaw,
            Vector2 size, Material image)
        {
            var frame = Group(name, parent);
            frame.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var canvas = GameObject.CreatePrimitive(PrimitiveType.Quad);
            canvas.name = "Canvas"; canvas.transform.SetParent(frame, false);
            canvas.transform.localPosition = new Vector3(0, 0, -.02f);
            canvas.transform.localScale = new Vector3(size.x, size.y, 1);
            canvas.GetComponent<Renderer>().sharedMaterial = image ?? cream;
            UnityEngine.Object.DestroyImmediate(canvas.GetComponent<Collider>());
            foreach (float sign in new[] { -1f, 1f })
            {
                var vertical = Block("Frame", frame, pos,
                    new Vector3(.09f, size.y + .14f, .1f), wood);
                vertical.transform.localPosition = new Vector3(sign * (size.x + .08f) * .5f, 0, 0);
                vertical.transform.localRotation = Quaternion.identity;
                var horizontal = Block("Frame", frame, pos,
                    new Vector3(size.x + .15f, .09f, .1f), wood);
                horizontal.transform.localPosition = new Vector3(0, sign * (size.y + .08f) * .5f, 0);
                horizontal.transform.localRotation = Quaternion.identity;
            }
        }
        private static void Plaque(Transform parent, string title, Vector3 pos, float yaw, float width = 1.8f)
        {
            var group = Group(title + " plaque", parent);
            group.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var board = Block("Dark wood plaque", group, pos,
                new Vector3(width, .38f, .09f), wood);
            board.transform.localPosition = Vector3.zero; board.transform.localRotation = Quaternion.identity;
            var lower = Block("Brass plaque edge", group, pos,
                new Vector3(width + .08f, .025f, .1f), brass);
            lower.transform.localPosition = new Vector3(0, -.19f, 0); lower.transform.localRotation = Quaternion.identity;
            var label = Group("Inscription", group).gameObject.AddComponent<TextMesh>();
            label.text = title; label.font = font; label.fontSize = 60;
            label.characterSize = title.Length > 15 ? .024f : .045f;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = new Color(.93f, .82f, .57f);
            label.transform.localPosition = new Vector3(0, 0, -.058f);
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        private static void RaiseOldCeiling(string path, float height)
        {
            var room = root.Find(path) ?? throw new InvalidOperationException("Room missing: " + path);
            var ceiling = room.Find("Modular ceiling") ?? throw new InvalidOperationException("Ceiling missing: " + path);
            foreach (Transform tile in ceiling)
            {
                var p = tile.position; p.y += height - 3.08f; tile.position = p;
                if (PrefabUtility.IsPartOfPrefabInstance(tile))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(tile);
            }
        }
        private static void RaisePrimitiveRoom(string path, float height)
        {
            var room = root.Find(path) ?? throw new InvalidOperationException("Room missing: " + path);
            foreach (Transform child in room)
            {
                if (child.name == "Ceiling" || child.name == "High ceiling")
                {
                    var p = child.position; p.y = height; child.position = p;
                }
                else if (child.name.EndsWith("wall", StringComparison.OrdinalIgnoreCase) ||
                         child.name.StartsWith("South wall") || child.name.StartsWith("North wall"))
                {
                    var p = child.position; p.y = height * .5f; child.position = p;
                    var scale = child.localScale; scale.y = height; child.localScale = scale;
                }
            }
        }
        private static void UpperPerimeter(Transform parent, string name, float xmin, float xmax,
            float zmin, float zmax, float from, float to, Material surface)
        {
            var wall = Group(name, parent);
            float h = to - from, mid = (to + from) * .5f;
            Block("North upper wall", wall, new Vector3((xmin + xmax) * .5f, mid, zmax),
                new Vector3(xmax - xmin, h, .23f), surface);
            Block("South upper wall", wall, new Vector3((xmin + xmax) * .5f, mid, zmin),
                new Vector3(xmax - xmin, h, .23f), surface);
            Block("West upper wall", wall, new Vector3(xmin, mid, (zmin + zmax) * .5f),
                new Vector3(.23f, h, zmax - zmin), surface);
            Block("East upper wall", wall, new Vector3(xmax, mid, (zmin + zmax) * .5f),
                new Vector3(.23f, h, zmax - zmin), surface);
        }
        private static void RaiseCeilings()
        {
            RaiseOldCeiling("ENVIRONMENT/GrandHall", 5.5f);
            RaiseOldCeiling("ENVIRONMENT/ArtStudio", 4.8f);
            RaiseOldCeiling("ENVIRONMENT/RestrictedGallery", 5f);
            RaisePrimitiveRoom("ENVIRONMENT/World 2.0 Additions/Library", 4.9f);
            RaisePrimitiveRoom("ENVIRONMENT/World 2.0 Additions/MusicWing", 5f);
            RaisePrimitiveRoom("ENVIRONMENT/World 2.0 Additions/Grand Hall extension", 5.5f);
            RaisePrimitiveRoom("ENVIRONMENT/World 2.0 Additions/Gallery archive extension", 5f);
            RaisePrimitiveRoom("ENVIRONMENT/World 2.0 Additions/Ballroom extension", 7.5f);
            var ballCeiling = root.Find("ENVIRONMENT/World 2.0 Additions/Ballroom extension");
            foreach (Transform child in ballCeiling)
                if (child.name == "Ballroom high ceiling")
                { var p = child.position; p.y = 7.5f; child.position = p; }
            var oldUpper = root.Find("ENVIRONMENT/World 2.0 Additions/Gothic finish/Ballroom upper stonework/West upper wall");
            if (oldUpper != null)
            {
                oldUpper.position = new Vector3(12, 5.3f, 0);
                oldUpper.localScale = new Vector3(.28f, 4.4f, 24);
            }
            var galleryExtension = root.Find("ENVIRONMENT/World 2.0 Additions/Gallery archive extension");
            for (int x = -6; x <= 2; x += 4)
                Block("Archive high ceiling", galleryExtension, new Vector3(x, 5, 26),
                    new Vector3(4, .18f, 4), charcoal);
            UpperPerimeter(polish, "Grand Hall upper stonework", -8, 8, -12, 8, 3.08f, 5.5f, stone);
            UpperPerimeter(polish, "Studio upper plaster", -24, -12, -8, 4, 3.08f, 4.8f, cream);
            UpperPerimeter(polish, "Archive upper stonework", -8, 4, 12, 28, 3.08f, 5f, blue);
            UpperPerimeter(polish, "Ballroom upper stonework", 12, 32, -12, 12, 5.4f, 7.5f, charcoal);
            var lintels = Group("High doorway lintels", polish);
            Block("Library east lintel", lintels, new Vector3(-10,4,10), new Vector3(.25f,1.8f,4), charcoal);
            Block("Library west lintel", lintels, new Vector3(-26,4,14), new Vector3(.25f,1.8f,4), charcoal);
            Block("Music Wing lintel", lintels, new Vector3(-30,4.05f,14), new Vector3(.25f,1.9f,4), charcoal);
        }
        private static void RemoveDecorativeChests()
        {
            var remove = new HashSet<GameObject>();
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                var instance = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (instance == null || instance.GetComponentInParent<Rigidbody>() != null) continue;
                var source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
                if (source != null && AssetDatabase.GetAssetPath(source) == Kit + "Chests/Chests.prefab")
                    remove.Add(instance);
            }
            foreach (var chest in remove) UnityEngine.Object.DestroyImmediate(chest);
            Debug.Log("[VelvetVows] Decorative medieval chests removed: " + remove.Count);
        }
        private static void PutChestOnFloor(Rigidbody body, float x, float z)
        {
            var collider = body.GetComponent<BoxCollider>() ?? throw new InvalidOperationException("Physics chest collider missing");
            body.transform.position = new Vector3(x, 3, z);
            Physics.SyncTransforms();
            float floor = float.NegativeInfinity;
            foreach (var hit in Physics.RaycastAll(new Vector3(x, 2.5f, z), Vector3.down, 4f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider != collider && hit.normal.y > .9f && hit.point.y <= .5f)
                    floor = Mathf.Max(floor, hit.point.y);
            if (float.IsNegativeInfinity(floor)) throw new InvalidOperationException("No floor below chest at " + x + "," + z);
            float bottomFromPivot = collider.bounds.min.y - body.transform.position.y;
            body.transform.position = new Vector3(x, floor - bottomFromPivot + .01f, z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(body.transform);
        }
        private static void MovePhysicsChests()
        {
            var chests = UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)
                .Where(rb => !rb.isKinematic && rb.name == "Pushable academy chest")
                .OrderBy(rb => rb.transform.position.x).ToArray();
            if (chests.Length != 3) throw new InvalidOperationException("Expected three existing physics chests");
            PutChestOnFloor(chests[0], -10, -1);
            PutChestOnFloor(chests[1], -6, 26);
            PutChestOnFloor(chests[2], 2, 26);
        }
        private static void ReplaceLabels()
        {
            var names = new HashSet<string> { "LIBRARY", "MUSIC WING", "RESTRICTED / STAFF ONLY",
                "THE BLACK ROSE BALL", "VALMONT ACADEMY OF ARTS" };
            foreach (var mesh in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if (names.Contains(mesh.gameObject.name)) UnityEngine.Object.DestroyImmediate(mesh.gameObject);
            var plaques = Group("Academy door plaques", polish);
            Plaque(plaques, "LIBRARY", new Vector3(-10.12f, 2.72f, 10), -90);
            Plaque(plaques, "MUSIC WING", new Vector3(-30.12f, 2.72f, 14), -90);
            Plaque(plaques, "ART STUDIO", new Vector3(-11.88f, 2.72f, -2), -90);
            Plaque(plaques, "RESTRICTED ARCHIVE / STAFF ONLY", new Vector3(-2, 2.72f, 12.12f), 0, 2.1f);
            Plaque(plaques, "BALLROOM", new Vector3(12.12f, 2.72f, -2), 90);
        }
        private static void Easel(Transform parent, string name, Vector3 at, Material image, float yaw)
        {
            var stand = Group(name, parent);
            stand.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            foreach (float offset in new[] { -.36f, .36f })
            {
                var leg = Block("Angled easel leg", stand, at + Vector3.up * .95f,
                    new Vector3(.07f, 1.9f, .08f), wood);
                leg.transform.localPosition = new Vector3(offset, .95f, .08f);
                leg.transform.localRotation = Quaternion.Euler(0, 0, offset < 0 ? -10 : 10);
            }
            var shelf = Block("Canvas ledge", stand, at + Vector3.up * .75f,
                new Vector3(1.05f, .08f, .2f), wood);
            shelf.transform.localPosition = new Vector3(0, .75f, -.12f);
            Artwork(stand, "Unfinished student canvas", at + Vector3.up * 1.4f,
                yaw, new Vector2(.9f, 1.2f), image);
        }
        private static void RoseArrangement(Transform parent, string name, Vector3 at)
        {
            var bouquet = Group(name, parent);
            foreach (int i in new[] { -1, 0, 1 })
            {
                float x = i * .18f;
                Block("Black rose stem", bouquet, at + new Vector3(x, .37f, 0),
                    new Vector3(.025f, .65f, .025f), green);
                var flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = "Black rose bloom"; flower.transform.SetParent(bouquet);
                flower.transform.position = at + new Vector3(x, .74f + (i == 0 ? .12f : 0), 0);
                flower.transform.localScale = new Vector3(.22f, .18f, .22f);
                flower.GetComponent<Renderer>().sharedMaterial = rose;
                UnityEngine.Object.DestroyImmediate(flower.GetComponent<Collider>());
                flower.isStatic = true;
            }
            Block("Brass vase", bouquet, at + Vector3.up * .17f,
                new Vector3(.36f, .34f, .36f), brass);
        }
        private static void MusicStand(Transform parent, string name, Vector3 at, float yaw)
        {
            var stand = Group(name, parent);
            stand.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var leg = Block("Music stand pole", stand, at + Vector3.up * .7f,
                new Vector3(.045f, 1.3f, .045f), wood);
            leg.transform.localPosition = Vector3.up * .7f;
            var desk = Block("Sheet rest", stand, at + Vector3.up * 1.34f,
                new Vector3(.65f, .45f, .05f), wood);
            desk.transform.localPosition = Vector3.up * 1.34f;
            var sheet = Block("Handwritten score", stand, at + Vector3.up * 1.35f,
                new Vector3(.48f, .35f, .018f), cream);
            sheet.transform.localPosition = new Vector3(0, 1.35f, -.04f);
        }
        private static void Chandelier(Transform parent, string name, Vector3 at)
        {
            var lamp = Group(name, parent);
            Block("Chain", lamp, at + Vector3.up * .42f, new Vector3(.04f, .85f, .04f), brass);
            Block("Brass hub", lamp, at, new Vector3(.42f, .15f, .42f), brass);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                var arm = Block("Chandelier arm", lamp, at + new Vector3(Mathf.Cos(a) * .56f, 0, Mathf.Sin(a) * .56f),
                    new Vector3(1.05f, .045f, .045f), brass);
                arm.transform.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                Block("Ivory candle", lamp, at + new Vector3(Mathf.Cos(a) * 1.08f, .18f, Mathf.Sin(a) * 1.08f),
                    new Vector3(.07f, .32f, .07f), cream);
            }
        }
        private static void GrandHall()
        {
            var room = Group("Grand Hall composition", polish);
            foreach (int x in new[] { -6, 6 })
                foreach (int z in new[] { -9, 2 })
                    Prop(Kit + "Column/Column_1.prefab", room, "Academy hall column", new Vector3(x, 0, z), 0, true, true);
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -5, 4 })
                    Prop(Home + "Building/Curtains.prefab", room, "Window drapery", new Vector3(x < 0 ? -7.2f : 7.2f, 0, z),
                        x < 0 ? 90 : -90);
            for (int i = 0; i < 3; i++)
            {
                Artwork(room, "Academy portrait west " + i, new Vector3(-7.74f, 2.05f, -6 + i * 4.5f), 90,
                    new Vector2(1.05f, 1.45f), portrait);
                Artwork(room, "Academy portrait east " + i, new Vector3(7.74f, 2.05f, -6 + i * 4.5f), -90,
                    new Vector2(1.05f, 1.45f), portrait);
            }
            Artwork(room, "Founder focal portrait", new Vector3(-3.4f, 2.2f, 7.7f), 180,
                new Vector2(1.8f, 2.4f), portrait);
            Artwork(room, "Black Rose Ball poster", new Vector3(2, 1.8f, -11.72f), 0,
                new Vector2(1.2f, 1.65f), burgundy);
            foreach (float x in new[] { -5.6f, 5.6f })
            {
                Prop(Home + "Livingroom/Sofa.prefab", room, "Hall waiting bench", new Vector3(x, 0, -4),
                    x < 0 ? 90 : -90, true, true);
                Prop(Home + "Livingroom/Coffee Table.prefab", room, "Portrait side table", new Vector3(x, 0, 3),
                    0, true, true);
                Prop(Home + "Decoration/Vase 1.prefab", room, "Academy flowers", new Vector3(x, .7f, 3), 0, false);
            }
            Block("Burgundy runner", room, new Vector3(0, .015f, -4), new Vector3(2.7f, .025f, 7.5f), burgundy);
            Block("Trophy display plinth", room, new Vector3(-3.8f, .55f, 5.6f), new Vector3(.85f, 1.1f, .85f), wood);
            Block("Young Artists trophy", room, new Vector3(-3.8f, 1.3f, 5.6f), new Vector3(.22f, .55f, .22f), gold);
            RoseArrangement(room, "Black Rose Ball flowers", new Vector3(4.8f, .7f, -9.5f));
            Chandelier(room, "Entrance chandelier", new Vector3(0, 4.75f, -4));
            Light(room, "Hall focal wash", new Vector3(-3.3f, 4.1f, 5.5f), new Color(1,.86f,.7f), 1.5f, 8);
        }
        private static void Library()
        {
            var room = Group("Library reading composition", polish);
            foreach (int z in new[] { 10, 14, 18, 22 })
                Prop(Kit + "Shelf/Large_Shelf.prefab", room, "Library wall shelves", new Vector3(-24.1f, 0, z), 90, true, true);
            foreach (int z in new[] { 14, 18, 22 })
                Prop(Kit + "Shelf/Large_Shelf.prefab", room, "Library wall shelves", new Vector3(-12, 0, z), -90, true, true);
            Prop(Home + "Livingroom/Modern Shelves.prefab", room, "Librarian shelves", new Vector3(-19, 0, 22.5f), 0, true, true);
            foreach (var xy in new[] { new Vector2(-21, 13.5f), new Vector2(-15.2f, 19.5f) })
                Prop(Home + "Livingroom/Round Table.prefab", room, "Reading table", new Vector3(xy.x, 0, xy.y),
                    0, true, true);
            foreach (var xy in new[] { new Vector2(-21.9f, 14.6f), new Vector2(-20.1f, 17.4f),
                new Vector2(-16.8f, 18.8f), new Vector2(-14, 20.7f), new Vector2(-17, 13.2f),
                new Vector2(-14.3f, 15.2f) })
                Prop(Home + "Livingroom/Chair 2.prefab", room, "Reading chair", new Vector3(xy.x, 0, xy.y));
            Prop(Home + "Livingroom/Sofa.prefab", room, "Quiet reading corner", new Vector3(-23, 0, 21), 90);
            Prop(Home + "Livingroom/Coffee Table.prefab", room, "Reading corner side table", new Vector3(-22, 0, 19));
            foreach (var xy in new[] { new Vector2(-21, 13.5f), new Vector2(-15.2f, 19.5f),
                new Vector2(-22, 19), new Vector2(-19, 16) })
                Prop(Home + "Decoration/Book (Open).prefab", room, "Open academy book",
                    new Vector3(xy.x, .85f, xy.y), 0, false);
            foreach (var xy in new[] { new Vector2(-24, 10), new Vector2(-24, 18),
                new Vector2(-12, 18), new Vector2(-19, 22) })
                Prop(Home + "Decoration/Books I.prefab", room, "Book collection",
                    new Vector3(xy.x, 1.2f, xy.y), 0, false);
            Prop(Home + "Decoration/Lamp 2.prefab", room, "Second reading lamp", new Vector3(-15.2f, .82f, 19.5f), 0, false);
            Block("Deep green reading rug", room, new Vector3(-19, .015f, 16), new Vector3(5.2f, .025f, 5.5f), green);
            Artwork(room, "Library academy portrait", new Vector3(-18, 2.25f, 23.7f), 180,
                new Vector2(1.25f, 1.7f), portrait);
            Light(room, "Amber reading corner", new Vector3(-22, 3.2f, 19), new Color(1,.72f,.44f), 1.6f, 6);
            Light(room, "Amber east table", new Vector3(-15, 3.2f, 18), new Color(1,.76f,.5f), 1.4f, 6);
        }
        private static void ArtStudio()
        {
            var room = Group("Art Studio work in progress", polish);
            foreach (var pose in new[] { new Vector3(-22.5f,0,2), new Vector3(-15,0,2),
                new Vector3(-22.5f,0,-6.5f), new Vector3(-15,0,-6.5f) })
                Easel(room, "Student easel", pose, stillLife, pose.z > 0 ? 180 : 0);
            foreach (var xy in new[] { new Vector2(-22.8f,-1), new Vector2(-16.2f,3),
                new Vector2(-23,-6), new Vector2(-14,0) })
                Prop(Home + "Livingroom/Stool.prefab", room, "Paint-stained stool", new Vector3(xy.x,0,xy.y));
            Artwork(room, "Studio centerpiece still life", new Vector3(-19,2.3f,-7.72f), 0,
                new Vector2(1.7f,2.1f), stillLife);
            Artwork(room, "Student study west", new Vector3(-23.7f,1.85f,-1), 90,
                new Vector2(.85f,1.1f), stillLife);
            Artwork(room, "Student study east", new Vector3(-12.25f,1.85f,1), -90,
                new Vector2(.85f,1.1f), portrait);
            Artwork(room, "Canvas leaning near wall", new Vector3(-22.8f,.95f,3.65f), 180,
                new Vector2(.9f,1.2f), stillLife);
            foreach (var xy in new[] { new Vector2(-20,-4), new Vector2(-16,2) })
            {
                Prop(Home + "Decoration/Book (Open).prefab", room, "Working sketchbook",
                    new Vector3(xy.x,.82f,xy.y), 0, false);
                Prop(Home + "Kid Room/Pen.prefab", room, "Drawing pencil",
                    new Vector3(xy.x+.35f,.82f,xy.y), 0, false);
                Block("Pigment jar", room, new Vector3(xy.x-.4f,.95f,xy.y),
                    new Vector3(.18f,.25f,.18f), burgundy);
            }
            Prop(Home + "Bedroom/Drawer 2.prefab", room, "Second art-supply cabinet", new Vector3(-22.4f,0,1.5f),
                90, true, true);
            Prop(Home + "Decoration/Plant Deco 1.prefab", room, "Studio window plant", new Vector3(-14,0,-6));
            Light(room, "Studio daylight pool", new Vector3(-18,4.1f,1.7f), new Color(1,.94f,.82f), 1.5f, 8);
        }
        private static void Gallery()
        {
            var room = Group("Restricted portrait gallery", polish);
            var oldStorageShelf = root.Find("ENVIRONMENT/RestrictedGallery/Archive additions/Archive shelves");
            if (oldStorageShelf != null) UnityEngine.Object.DestroyImmediate(oldStorageShelf.gameObject);
            for (int i = 0; i < 4; i++)
            {
                float z = 14.5f + i * 3.2f;
                Artwork(room, "Missing student portrait west " + i, new Vector3(-7.72f,2.1f,z), 90,
                    new Vector2(i % 2 == 0 ? 1.1f : .9f, i % 2 == 0 ? 1.55f : 1.3f), portrait);
                Artwork(room, "Academy portrait east " + i, new Vector3(3.72f,2.1f,z), -90,
                    new Vector2(1.0f,1.45f), portrait);
            }
            Artwork(room, "Founder portrait north", new Vector3(-2,2.2f,27.72f), 180,
                new Vector2(1.6f,2f), portrait);
            foreach (var xy in new[] { new Vector2(-5,16), new Vector2(1,17), new Vector2(-4,21) })
            {
                Block("Evidence display table", room, new Vector3(xy.x,.34f,xy.y),
                    new Vector3(1.45f,.68f,.95f), wood);
                Block("Brass display lip", room, new Vector3(xy.x,.71f,xy.y),
                    new Vector3(1.5f,.045f,1f), brass);
            }
            foreach (var evidence in UnityEngine.Object.FindObjectsByType<EvidenceItem>(FindObjectsSortMode.None))
            {
                var p = evidence.transform.position; p.y = .76f; evidence.transform.position = p;
                PrefabUtility.RecordPrefabInstancePropertyModifications(evidence.transform);
            }
            foreach (int x in new[] { -5, 2 })
            {
                Block("Sculpture pedestal", room, new Vector3(x,.58f,24), new Vector3(.85f,1.15f,.85f), charcoal);
                var bust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bust.name = "Abstract academy bust"; bust.transform.SetParent(room);
                bust.transform.position = new Vector3(x,1.35f,24);
                bust.transform.localScale = new Vector3(.48f,.62f,.42f);
                bust.GetComponent<Renderer>().sharedMaterial = stone;
                UnityEngine.Object.DestroyImmediate(bust.GetComponent<Collider>());
                bust.isStatic = true;
            }
            Prop(Home + "Decoration/Books II.prefab", room, "Student archives", new Vector3(-5,.8f,16), 0, false);
            Prop(Home + "Decoration/Book (Open).prefab", room, "Archive reading copy", new Vector3(-4,.8f,21), 0, false);
            foreach (int z in new[] { 15, 22, 26 })
                Light(room, "Portrait wall pool", new Vector3(-5,3.9f,z),
                    new Color(.8f,.82f,1f), 1.0f, 5);
            Light(room, "Evidence warm pool", new Vector3(-1,3.4f,18), new Color(1,.78f,.55f), 1.4f, 6);
        }
        private static void MusicWing()
        {
            var room = Group("Music Wing rehearsal", polish);
            Block("Burgundy piano rug", room, new Vector3(-36,.012f,12),
                new Vector3(5.2f,.025f,4.4f), burgundy);
            foreach (var xy in new[] { new Vector2(-40,10), new Vector2(-38,10),
                new Vector2(-34,10), new Vector2(-32,10), new Vector2(-40,18),
                new Vector2(-34,18), new Vector2(-32,18) })
                Prop(Home + "Livingroom/Chair 3.prefab", room, "Rehearsal chair",
                    new Vector3(xy.x,0,xy.y), xy.y < 14 ? 0 : 180);
            foreach (var xy in new[] { new Vector2(-40,12), new Vector2(-33,12),
                new Vector2(-40,16), new Vector2(-33,16) })
                MusicStand(room, "Student music stand", new Vector3(xy.x,0,xy.y),
                    xy.y < 14 ? 0 : 180);
            Artwork(room, "Academy composer portrait", new Vector3(-36,2.25f,19.72f), 180,
                new Vector2(1.25f,1.75f), portrait);
            Artwork(room, "Recital painting west", new Vector3(-41.72f,2.15f,14), 90,
                new Vector2(1.0f,1.4f), portrait);
            Artwork(room, "Music Wing painting south", new Vector3(-38,2.15f,8.28f), 0,
                new Vector2(1.0f,1.4f), stillLife);
            Prop(Home + "Livingroom/Coffee Table.prefab", room, "Recital side table", new Vector3(-39,0,16));
            foreach (var xy in new[] { new Vector2(-36,11.55f), new Vector2(-39,16),
                new Vector2(-34,17) })
                Prop(Home + "Decoration/Book (Open).prefab", room, "Handwritten music score",
                    new Vector3(xy.x,.85f,xy.y), 0, false);
            RoseArrangement(room, "Music Wing rose", new Vector3(-39,.75f,16));
            Light(room, "Piano key light", new Vector3(-36,3.8f,11), new Color(1,.81f,.63f), 1.6f, 7);
        }
        private static void Ballroom()
        {
            var room = Group("Ballroom gala composition", polish);
            Block("Raised recital platform", room, new Vector3(28,.1f,5.8f),
                new Vector3(6.2f,.2f,3.8f), wood, true);
            Block("Burgundy stage carpet", room, new Vector3(28,.215f,5.8f),
                new Vector3(5.8f,.025f,3.4f), burgundy);
            Block("Recital grand piano body", room, new Vector3(28,1.25f,6.2f),
                new Vector3(2.5f,.35f,1.45f), charcoal);
            Block("Recital grand piano keyboard", room, new Vector3(28,1.15f,5.18f),
                new Vector3(1.85f,.1f,.34f), cream);
            var lid = Block("Recital piano raised lid", room, new Vector3(28,1.6f,6.5f),
                new Vector3(2.5f,.1f,1.1f), charcoal);
            lid.transform.rotation = Quaternion.Euler(-17,0,0);
            foreach (float x in new[] { 27.1f, 28.9f })
                foreach (float z in new[] { 5.6f, 6.7f })
                    Block("Recital piano leg", room, new Vector3(x,.7f,z),
                        new Vector3(.12f,1f,.12f), wood);
            Prop(Home + "Livingroom/Stool.prefab", room, "Pianist bench", new Vector3(28,.22f,4.5f));
            foreach (var xy in new[] { new Vector2(14.5f,-8), new Vector2(29.5f,-8),
                new Vector2(14.5f,8), new Vector2(29.5f,8), new Vector2(15,-1),
                new Vector2(29,-1) })
                RoseArrangement(room, "Black Rose gala arrangement", new Vector3(xy.x,.78f,xy.y));
            foreach (int x in new[] { 15, 29 })
                foreach (int z in new[] { -9, 9 })
                    Prop(Home + "Building/Curtains (Open Side).prefab", room, "Ballroom window velvet",
                        new Vector3(x,0,z), z < 0 ? 0 : 180);
            foreach (var xy in new[] { new Vector2(16,-11.72f), new Vector2(22,-11.72f),
                new Vector2(28,-11.72f) })
                Artwork(room, "Ballroom society portrait", new Vector3(xy.x,2.45f,xy.y), 0,
                    new Vector2(1.3f,1.8f), portrait);
            Artwork(room, "Ballroom founder portrait", new Vector3(31.72f,2.4f,-3), -90,
                new Vector2(1.45f,1.9f), portrait);
            Artwork(room, "Ballroom gala painting", new Vector3(31.72f,2.4f,2), -90,
                new Vector2(1.25f,1.65f), stillLife);
            for (int x = 16; x <= 28; x += 4)
            {
                Block("Burgundy gala wall panel", room, new Vector3(x,4.65f,11.72f),
                    new Vector3(2.8f,4.5f,.08f), burgundy);
                foreach (float offset in new[] { -1.44f, 1.44f })
                    Block("Gilded wall molding", room, new Vector3(x+offset,4.65f,11.66f),
                        new Vector3(.055f,4.65f,.08f), gold);
            }
            foreach (int z in new[] { -7, 7 })
                foreach (int x in new[] { 16, 28 })
                    Prop(Home + "Decoration/Vase 2.prefab", room, "Ballroom edge vase",
                        new Vector3(x,.75f,z), 0, false);
            foreach (int x in new[] { 15, 29 })
            {
                Prop(Home + "Livingroom/Round Table.prefab", room, "Ballroom seating table",
                    new Vector3(x,0,0), 0, true, true);
                foreach (float z in new[] { -1.6f, 1.6f })
                    Prop(Home + "Livingroom/Chair 3.prefab", room, "Ballroom guest chair",
                        new Vector3(x,0,z), z < 0 ? 0 : 180);
            }
            int guestIndex = 0;
            foreach (var position in new[] { new Vector3(17,0,-9), new Vector3(29,0,-9),
                new Vector3(12.5f,0,10.5f), new Vector3(31,0,9) })
            {
                string prefabName = position.x < 20
                    ? (position.z < 0 ? "Amelie Casual Student" : "Clara Casual Student")
                    : (position.z < 0 ? "Male Student Burgundy" : "Male Student Slate");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Vv + "Prefabs/NPC/" + prefabName + ".prefab") ??
                    throw new InvalidOperationException("Ballroom guest prefab missing: " + prefabName);
                var guest = (GameObject)PrefabUtility.InstantiatePrefab(prefab, room);
                guest.name = "Ballroom gala guest " + (++guestIndex);
                guest.transform.SetPositionAndRotation(position, Quaternion.Euler(0, position.z < 0 ? 20 : 200, 0));
                foreach (var collider in guest.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
            }
            foreach (int z in new[] { -7, 0, 7 })
                Chandelier(room, "Brass gala chandelier", new Vector3(22,6.4f,z));
            var originalCenter = root.Find("ENVIRONMENT/Ballroom/Ball furnishing additions/Ballroom warm center");
            if (originalCenter != null)
            {
                var centerLight = originalCenter.GetComponent<Light>();
                if (centerLight != null) { centerLight.intensity = 2.2f; centerLight.range = 13; }
            }
            Light(room, "Ballroom dance floor key", new Vector3(20,5.5f,-1), new Color(1,.86f,.7f), 2.1f, 12);
            Light(room, "West gala seating glow", new Vector3(15,4.2f,7), new Color(1,.7f,.52f), 1.2f, 6);
            Light(room, "East gala seating glow", new Vector3(29,4.2f,-7), new Color(1,.7f,.52f), 1.2f, 6);
            var ambientGuest = root.Find("CHARACTERS/AmbientNPCs/Ballroom guest");
            if (ambientGuest != null)
            {
                ambientGuest.position = new Vector3(30,0,-7);
                PrefabUtility.RecordPrefabInstancePropertyModifications(ambientGuest);
            }
        }
        private static T HudRef<T>(GameHUD hud, string field) where T : UnityEngine.Object
        {
            return new SerializedObject(hud).FindProperty(field)?.objectReferenceValue as T ??
                throw new InvalidOperationException("HUD field missing: " + field);
        }
        private static void Position(Text text, Vector2 anchor, Vector2 at, Vector2 size, int fontSize)
        {
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchor; rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = at; rect.sizeDelta = size;
            text.fontSize = fontSize;
            text.color = new Color(.95f,.89f,.78f);
            if (text.GetComponent<Outline>() == null)
            {
                var outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.04f,.025f,.035f,.82f);
                outline.effectDistance = new Vector2(1,-1);
            }
        }
        private static void LayoutHud()
        {
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>() ??
                throw new InvalidOperationException("Working HUD missing");
            Position(HudRef<Text>(hud,"suspicionText"), new Vector2(0,1), new Vector2(38,-28),
                new Vector2(320,42), 20);
            Position(HudRef<Text>(hud,"objectiveText"), new Vector2(.5f,1), new Vector2(0,-24),
                new Vector2(1000,54), 24);
            Position(HudRef<Text>(hud,"timerText"), new Vector2(1,1), new Vector2(-38,-28),
                new Vector2(410,42), 20);
            Position(HudRef<Text>(hud,"evidenceText"), new Vector2(0,1), new Vector2(38,-98),
                new Vector2(260,34), 17);
            Position(HudRef<Text>(hud,"roseText"), new Vector2(1,1), new Vector2(-38,-98),
                new Vector2(260,34), 17);
            Position(HudRef<Text>(hud,"interactionText"), new Vector2(.5f,0), new Vector2(0,80),
                new Vector2(760,48), 25);
            var bar = HudRef<Image>(hud,"suspicionFill").transform.parent.GetComponent<RectTransform>();
            bar.anchorMin = bar.anchorMax = new Vector2(0,1);
            bar.pivot = new Vector2(0,1);
            bar.anchoredPosition = new Vector2(38,-72);
            bar.sizeDelta = new Vector2(280,12);
            var dialogue = hud.transform.Find("World Dialogue Panel");
            if (dialogue != null)
            {
                foreach (var text in dialogue.GetComponentsInChildren<Text>(true))
                    text.color = new Color(.97f,.92f,.83f);
                var background = dialogue.GetComponent<Image>();
                if (background != null) background.color = new Color(.045f,.03f,.05f,.96f);
            }
        }
        private static void RequireGameplay()
        {
            if (UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length != 1 ||
                UnityEngine.Object.FindObjectsByType<GameHUD>(FindObjectsSortMode.None).Length != 1 ||
                UnityEngine.Object.FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None).Length != 1 ||
                UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length != 1 ||
                UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.CompareTag("MainCamera")) != 1 ||
                UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None).Length != 1)
                throw new InvalidOperationException("Accepted gameplay singleton/camera count changed");
            if (UnityEngine.Object.FindFirstObjectByType<EvidenceManager>().Total != 3 ||
                UnityEngine.Object.FindObjectsByType<EvidenceItem>(FindObjectsSortMode.None).Length != 3 ||
                UnityEngine.Object.FindFirstObjectByType<BlackRoseSpawner>().SpawnCount != 6 ||
                UnityEngine.Object.FindObjectsByType<DialogueTrigger>(FindObjectsSortMode.None).Length != 3 ||
                UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Count(r => !r.isKinematic) < 3 ||
                root.Find("ENVIRONMENT/Ballroom/Ending_EvelynPoint") == null ||
                root.Find("ENVIRONMENT/Ballroom/Ending_AdrianPoint") == null)
                throw new InvalidOperationException("Accepted gameplay content/reference count changed");
        }
        private static void ValidateInMemory()
        {
            RequireGameplay();
            if (polish == null || polish.childCount < 7) throw new InvalidOperationException("Visual polish groups missing");
            var giantLabels = new HashSet<string> { "LIBRARY", "MUSIC WING", "RESTRICTED / STAFF ONLY",
                "THE BLACK ROSE BALL", "VALMONT ACADEMY OF ARTS" };
            if (UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)
                .Any(t => giantLabels.Contains(t.gameObject.name)))
                throw new InvalidOperationException("Old floating room label remains");
            foreach (var renderer in polish.GetComponentsInChildren<Renderer>(true))
                if (renderer.sharedMaterials.Any(material => material == null))
                    throw new InvalidOperationException("Missing visual-polish material on " + renderer.name);
            Physics.SyncTransforms();
            foreach (var point in new[] { new Vector3(-10,1,6), new Vector3(-10,1,10),
                new Vector3(-28,1,14), new Vector3(-18,1,-2) })
            {
                var blockers = Physics.OverlapSphere(point, .32f, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore).Where(c => !c.isTrigger).ToArray();
                if (blockers.Length > 0)
                    Debug.LogWarning("[VelvetVows] Route clearance check near " + point + ": " +
                        string.Join(", ", blockers.Select(c => c.name)));
            }
        }
        [MenuItem("Velvet Vows/Validate Saved Visual Polish 2.1")]
        public static void ValidateSaved()
        {
            EditorSceneManager.OpenScene(Live, OpenSceneMode.Single);
            root = GameObject.Find("Velvet Vows Main")?.transform ?? throw new InvalidOperationException("Live root missing");
            polish = root.Find("ENVIRONMENT/Visual Polish 2.1") ?? throw new InvalidOperationException("Saved polish missing");
            ValidateInMemory();
            var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            foreach (var field in new[] { "playerMovement", "playerInput", "hud", "celeste", "suspicion",
                "timer", "evidence", "roses", "ballroomDoor", "endingEvelynPoint", "endingAdrianPoint",
                "adrian", "music" })
                if (new SerializedObject(manager).FindProperty(field)?.objectReferenceValue == null)
                    throw new InvalidOperationException("GameManager reference missing: " + field);
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
            var start = new SerializedObject(hud).FindProperty("startPanel")?.objectReferenceValue as GameObject;
            if (start == null || !start.activeSelf || !hud.GetComponent<Canvas>().enabled)
                throw new InvalidOperationException("Start screen serialized inactive");
            Debug.Log("[VelvetVows] Saved Visual Polish 2.1 validated in live scene: core references intact; " +
                "3 evidence, 6 rose spawns, 3 dynamic chests, 3 dialogue triggers, no giant labels.");
        }
        public static void RenderAuditViews()
        {
            EditorSceneManager.OpenScene(Live, OpenSceneMode.Single);
            var cameraObject = new GameObject("Temporary 2.1 audit camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 64;
            foreach (var view in new[] {
                new { name = "hall", eye = new Vector3(3,1.8f,-4), look = new Vector3(0,1.5f,5) },
                new { name = "library", eye = new Vector3(-19,1.8f,10), look = new Vector3(-19,1.5f,22) },
                new { name = "studio", eye = new Vector3(-18,1.8f,-6), look = new Vector3(-18,1.5f,2) },
                new { name = "gallery", eye = new Vector3(-2,1.8f,14), look = new Vector3(-2,1.5f,25) },
                new { name = "music", eye = new Vector3(-36,1.8f,18), look = new Vector3(-36,1.5f,11) },
                new { name = "ballroom", eye = new Vector3(22,1.8f,-8), look = new Vector3(22,1.5f,8) } })
            {
                camera.transform.position = view.eye;
                camera.transform.LookAt(view.look);
                Render(camera, "/tmp/velvetvows-2_1-" + view.name + ".png");
            }
            var evelynAnchor = GameObject.Find("Ending_EvelynPoint")?.transform;
            var adrianAnchor = GameObject.Find("Ending_AdrianPoint")?.transform;
            if (evelynAnchor != null && adrianAnchor != null)
            {
                var evelynPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Vv + "Prefabs/Evelyn Moreau.prefab");
                var adrianPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Vv + "Prefabs/Adrian Valmont.prefab");
                var evelyn = (GameObject)PrefabUtility.InstantiatePrefab(evelynPrefab);
                var adrian = (GameObject)PrefabUtility.InstantiatePrefab(adrianPrefab);
                Vector3 face = adrianAnchor.position - evelynAnchor.position; face.y = 0;
                evelyn.transform.SetPositionAndRotation(evelynAnchor.position, Quaternion.LookRotation(face.normalized));
                adrian.transform.SetPositionAndRotation(adrianAnchor.position, Quaternion.LookRotation(-face.normalized));
                camera.transform.position = (evelynAnchor.position + adrianAnchor.position) * .5f +
                    Vector3.Cross(Vector3.up, face.normalized) * 2.45f + Vector3.up * 1.6f;
                camera.transform.LookAt((evelynAnchor.position + adrianAnchor.position) * .5f + Vector3.up * 1.2f);
                camera.fieldOfView = 42;
                Render(camera, "/tmp/velvetvows-2_1-ending.png");
                UnityEngine.Object.DestroyImmediate(evelyn);
                UnityEngine.Object.DestroyImmediate(adrian);
            }
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
        private static void Render(Camera camera, string path)
        {
            var target = new RenderTexture(1280,720,24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
