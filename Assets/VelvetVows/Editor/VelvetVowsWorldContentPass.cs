using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace VelvetVows.Editor
{
    /// <summary>One-time additive migration of the accepted main scene. Never calls the old builders.</summary>
    public static class VelvetVowsWorldContentPass
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string BackupPath = "Assets/VelvetVows/Scenes/VelvetVows_Main_v1_WORKING.unity";
        private const string Home = "Assets/VNB - Home Set/Prefabs/";
        private const string Kit = "Assets/Medieval Room Kit/Prefab/";
        private const string Vv = "Assets/VelvetVows/";
        private static Material stone, wood, burgundy, brass, paper;
        private static readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
        private static Transform root, world, characters, gameplay, points;
        private static GameManager game;
        private static Font font;

        [MenuItem("Velvet Vows/Apply World & Content Pass 2.0")]
        public static void Apply()
        {
            if (!File.Exists(BackupPath)) throw new InvalidOperationException("Working scene backup missing. Migration stopped.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            root = GameObject.Find("Velvet Vows Main")?.transform ?? throw new InvalidOperationException("Live scene root missing");
            if (root.Find("ENVIRONMENT/World 2.0 Additions") != null)
            { Debug.Log("[VelvetVows] World 2.0 already applied; no changes made."); return; }
            game = UnityEngine.Object.FindFirstObjectByType<GameManager>() ?? throw new InvalidOperationException("Working GameManager missing");
            if (UnityEngine.Object.FindFirstObjectByType<ThirdPersonController>() == null ||
                UnityEngine.Object.FindFirstObjectByType<GameHUD>() == null)
                throw new InvalidOperationException("Working player or HUD missing");
            stone = Mat("Limestone"); wood = Mat("Dark Walnut"); burgundy = Mat("Burgundy Velvet");
            brass = Mat("Aged Brass"); paper = Mat("Painting Ochre");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            world = Group("World 2.0 Additions", root.Find("ENVIRONMENT"));
            characters = Group("AmbientNPCs", root.Find("CHARACTERS"));
            gameplay = Group("World 2.0 Interactions", root.Find("SYSTEMS"));
            points = Group("Ambient Waypoints", gameplay);
            ExtendExistingRooms();
            BuildNewRooms();
            DecorateExisting();
            FinishRooms();
            BuildPiano();
            BuildNPCs();
            BuildInspectables();
            BuildDialogueUI();
            BuildMusic();
            RefreshEvidencePresentation();
            ExtendRoses();
            Validate();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[VelvetVows] World & Content Pass 2.0 saved to " + ScenePath);
        }

        private static Transform Group(string name, Transform parent)
        {
            if (parent == null) throw new InvalidOperationException("Missing parent for " + name);
            var result = new GameObject(name).transform;
            result.SetParent(parent, false);
            return result;
        }
        private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Vv + "Materials/" + name + ".mat") ??
            throw new InvalidOperationException("Missing material " + name);
        private static GameObject Prefab(string path, Transform parent, string name, Vector3 position, float yaw = 0)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new InvalidOperationException("Missing prefab " + path);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            obj.name = name;
            obj.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            ConvertMaterials(obj);
            return obj;
        }
        private static void ConvertMaterials(GameObject obj)
        {
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                var shared = renderer.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    var original = shared[i];
                    if (original == null) continue;
                    string path = AssetDatabase.GetAssetPath(original);
                    if (path.StartsWith(Vv, StringComparison.Ordinal)) continue;
                    if (!converted.TryGetValue(original, out var copy))
                    {
                        string dir = Vv + "Materials/ImportedVariants";
                        Directory.CreateDirectory(dir);
                        string safe = string.Concat(path.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
                        string dest = dir + "/" + safe + ".mat";
                        copy = AssetDatabase.LoadAssetAtPath<Material>(dest);
                        if (copy == null)
                        {
                            copy = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            copy.name = original.name + " URP";
                            var texture = original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") :
                                original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") : null;
                            if (texture != null) copy.SetTexture("_BaseMap", texture);
                            Color color = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") :
                                original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white;
                            copy.SetColor("_BaseColor", color);
                            AssetDatabase.CreateAsset(copy, dest);
                        }
                        converted.Add(original, copy);
                    }
                    shared[i] = copy;
                }
                renderer.sharedMaterials = shared;
            }
        }
        private static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size,
            Material material, bool collider = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.SetParent(parent);
            obj.transform.position = position; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        private static void RemoveWalls(Transform zone, Func<string, bool> test)
        {
            var perimeter = zone.Find("Modular perimeter");
            if (perimeter == null) throw new InvalidOperationException("Perimeter missing in " + zone.name);
            var remove = perimeter.Cast<Transform>().Where(child => test(child.name)).ToArray();
            if (remove.Length == 0) throw new InvalidOperationException("Expected opening wall not found in " + zone.name);
            foreach (var child in remove) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        private static void ExtendExistingRooms()
        {
            var hall = root.Find("ENVIRONMENT/GrandHall");
            var gallery = root.Find("ENVIRONMENT/RestrictedGallery");
            var ballroom = root.Find("ENVIRONMENT/Ballroom");
            RemoveWalls(hall, name => name.StartsWith("South") && name.EndsWith(",-8"));
            RemoveWalls(hall, name => name.StartsWith("West") && name.EndsWith("-8,6"));
            var h = Group("Grand Hall extension", world);
            for (int x = -6; x <= 6; x += 4) Block("Floor", h, new Vector3(x, -.12f, -10), new Vector3(4, .22f, 4), stone);
            Block("South wall west", h, new Vector3(-4, 1.55f, -12), new Vector3(8, 3.1f, .25f), stone);
            Block("South wall east", h, new Vector3(4, 1.55f, -12), new Vector3(8, 3.1f, .25f), stone);
            Block("West wall", h, new Vector3(-8, 1.55f, -10), new Vector3(.25f, 3.1f, 4), stone);
            Block("East wall", h, new Vector3(8, 1.55f, -10), new Vector3(.25f, 3.1f, 4), stone);
            foreach (int x in new[] { -6, -2, 2, 6 }) Block("High ceiling", h, new Vector3(x, 3.2f, -10), new Vector3(4, .18f, 4), wood, false);
            var entrance = root.Find("ENVIRONMENT/GrandHall/Formal furniture and decoration/Closed academy entrance");
            if (entrance != null) entrance.position += Vector3.back * 4;
            RemoveWalls(gallery, name => name.StartsWith("North") && name.EndsWith(",24"));
            var g = Group("Gallery archive extension", world);
            for (int x = -6; x <= 2; x += 4) Block("Floor", g, new Vector3(x, -.12f, 26), new Vector3(4, .22f, 4), stone);
            Block("North wall", g, new Vector3(-2, 1.55f, 28), new Vector3(12, 3.1f, .25f), stone);
            Block("West wall", g, new Vector3(-8, 1.55f, 26), new Vector3(.25f, 3.1f, 4), stone);
            Block("East wall", g, new Vector3(4, 1.55f, 26), new Vector3(.25f, 3.1f, 4), stone);
            RemoveWalls(ballroom, name => (name.StartsWith("East") && name.Contains("28,")) ||
                (name.StartsWith("North") && name.EndsWith(",8")) ||
                (name.StartsWith("South") && name.EndsWith(",-8")));
            var b = Group("Ballroom extension", world);
            for (int x = 14; x <= 30; x += 4)
                foreach (int z in new[] { -10, 10 })
                    Block("Dance floor", b, new Vector3(x, -.12f, z), new Vector3(4, .22f, 4), stone);
            for (int z = -6; z <= 6; z += 4)
                Block("Dance floor", b, new Vector3(30, -.12f, z), new Vector3(4, .22f, 4), stone);
            Block("East wall", b, new Vector3(32, 2.7f, 0), new Vector3(.25f, 5.4f, 24), stone);
            Block("North wall", b, new Vector3(22, 2.7f, 12), new Vector3(20, 5.4f, .25f), stone);
            Block("South wall", b, new Vector3(22, 2.7f, -12), new Vector3(20, 5.4f, .25f), stone);
            foreach (int x in new[] { 14, 18, 22, 26, 30 })
                foreach (int z in new[] { -10, -6, -2, 2, 6, 10 })
                    Block("Ballroom high ceiling", b, new Vector3(x, 5.45f, z), new Vector3(4, .2f, 4), wood, false);
            var oldCeiling = ballroom.Find("Modular ceiling");
            if (oldCeiling != null) oldCeiling.gameObject.SetActive(false);
            var oldDoor = root.Find("ENVIRONMENT/Ballroom/Symmetrical ballroom furnishings/Closed ballroom wing door");
            if (oldDoor != null) oldDoor.position += Vector3.right * 4;
            var endingEvelyn = ballroom.Find("Ending_EvelynPoint");
            var endingAdrian = ballroom.Find("Ending_AdrianPoint");
            if (endingEvelyn == null || endingAdrian == null) throw new InvalidOperationException("Ending anchors missing");
        }

        private static void BuildNewRooms()
        {
            var corridor = Group("Connecting corridors", world);
            Block("Library corridor floor", corridor, new Vector3(-10, -.12f, 8), new Vector3(4, .22f, 8), stone);
            Block("Library corridor outer wall", corridor, new Vector3(-12, 1.55f, 6), new Vector3(.25f, 3.1f, 4), stone);
            Block("Library corridor north wall", corridor, new Vector3(-10, 1.55f, 12), new Vector3(4, 3.1f, .25f), stone);
            Room("Library", -26, -10, 8, 24, -10, 10);
            Block("Music corridor floor", corridor, new Vector3(-28, -.12f, 14), new Vector3(4, .22f, 4), stone);
            Block("Music corridor north wall", corridor, new Vector3(-28, 1.55f, 16), new Vector3(4, 3.1f, .25f), stone);
            Block("Music corridor south wall", corridor, new Vector3(-28, 1.55f, 12), new Vector3(4, 3.1f, .25f), stone);
            Room("MusicWing", -42, -30, 8, 20, -30, 14);
            var continuation = Group("DecorativeExterior", world);
            Block("Inaccessible upper landing", continuation, new Vector3(28, 3.4f, 8), new Vector3(6, .28f, 4), wood);
            Prefab(Home + "Building/Stairs/Stairs  Steps.prefab", continuation, "Upper landing stair silhouette", new Vector3(27, 0, 8), 90);
            Prefab(Home + "Building/Stairs/Stairs Handrail Angled.prefab", continuation, "Upper landing rail", new Vector3(27, 0, 8), 90);
            Prefab(Kit + "Door/Door.prefab", continuation, "Locked Conservatory door", new Vector3(-15.8f, 0, -7.7f));
        }
        private static Transform Room(string name, int xmin, int xmax, int zmin, int zmax, int entranceX, int entranceZ)
        {
            var room = Group(name, world);
            for (int x = xmin + 2; x < xmax; x += 4)
                for (int z = zmin + 2; z < zmax; z += 4)
                {
                    Block("Floor", room, new Vector3(x, -.12f, z), new Vector3(4, .22f, 4), stone);
                    Block("Ceiling", room, new Vector3(x, 3.2f, z), new Vector3(4, .18f, 4), wood, false);
                }
            for (int x = xmin + 2; x < xmax; x += 4)
            {
                Block("South wall", room, new Vector3(x, 1.55f, zmin), new Vector3(4, 3.1f, .25f), stone);
                Block("North wall", room, new Vector3(x, 1.55f, zmax), new Vector3(4, 3.1f, .25f), stone);
            }
            for (int z = zmin + 2; z < zmax; z += 4)
            {
                if (xmin != entranceX || Mathf.Abs(z - entranceZ) > 2)
                    Block("West wall", room, new Vector3(xmin, 1.55f, z), new Vector3(.25f, 3.1f, 4), stone);
                if (xmax != entranceX || Mathf.Abs(z - entranceZ) > 2)
                    Block("East wall", room, new Vector3(xmax, 1.55f, z), new Vector3(.25f, 3.1f, 4), stone);
            }
            return room;
        }

        private static GameObject FloorPrefab(string path, Transform parent, string name, float x, float z, float yaw = 0)
        {
            var obj = Prefab(path, parent, name, new Vector3(x, 0, z), yaw);
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                float bottom = renderers.Min(r => r.bounds.min.y);
                obj.transform.position += Vector3.up * -bottom;
            }
            if (obj.GetComponentsInChildren<Collider>(true).Length == 0 && renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                var collider = obj.AddComponent<BoxCollider>();
                collider.center = obj.transform.InverseTransformPoint(bounds.center);
                collider.size = bounds.size;
            }
            return obj;
        }
        private static void Light(Transform parent, string name, Vector3 at, Color tint, float intensity, float range)
        {
            var go = Group(name, parent).gameObject;
            go.transform.position = at;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point; light.color = tint; light.intensity = intensity;
            light.range = range; light.shadows = LightShadows.None;
        }
        private static void Sign(Transform parent, string text, Vector3 position, float yaw, float size = .2f)
        {
            var go = Group(text, parent).gameObject;
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text; mesh.font = font; mesh.fontSize = 60; mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.color = new Color(.86f, .71f, .42f);
            mesh.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
        private static void DecorateExisting()
        {
            var hall = Group("Academy furnishings", root.Find("ENVIRONMENT/GrandHall"));
            FloorPrefab(Home + "Building/Curtains.prefab", hall, "Burgundy drapery", -5, 6);
            FloorPrefab(Home + "Livingroom/Round Table.prefab", hall, "Exhibition table", 5, 2);
            FloorPrefab(Home + "Decoration/Vase 1.prefab", hall, "Exhibition vase", 5, 2.1f);
            FloorPrefab(Home + "Decoration/Plant Deco 1.prefab", hall, "Hall plant", 6, -9);
            FloorPrefab(Home + "Livingroom/Chair 3.prefab", hall, "Hall reading chair", -5, -9, 45);
            Sign(hall, "VALMONT ACADEMY OF ARTS", new Vector3(0, 2.35f, -11.85f), 180, .12f);
            Light(hall, "Hall warm lantern", new Vector3(0, 2.8f, -8), new Color(1, .82f, .62f), 2.2f, 11);
            var gallery = Group("Archive additions", root.Find("ENVIRONMENT/RestrictedGallery"));
            FloorPrefab(Home + "Livingroom/Modern Shelves.prefab", gallery, "Archive shelves", -5, 25);
            FloorPrefab(Home + "Decoration/Books II.prefab", gallery, "Archive books", -4.7f, 25);
            Sign(gallery, "RESTRICTED / STAFF ONLY", new Vector3(-2, 2.5f, 12.18f), 0, .09f);
            Light(gallery, "Archive cool light", new Vector3(-1, 2.8f, 24), new Color(.52f, .64f, .9f), 1.3f, 8);
            var studio = Group("Artist workstations", root.Find("ENVIRONMENT/ArtStudio"));
            FloorPrefab(Home + "Livingroom/Stool.prefab", studio, "Artist stool", -18, 0);
            FloorPrefab(Home + "Bedroom/Drawer 2.prefab", studio, "Pigment cabinet", -22, -3);
            FloorPrefab(Home + "Decoration/Plant Deco 1.prefab", studio, "Window plant", -14, 2);
            for (int i = 0; i < 2; i++)
            {
                var easel = Group("Artist easel " + (i + 1), studio);
                float x = i == 0 ? -17 : -21;
                Block("Canvas", easel, new Vector3(x, 1.35f, i == 0 ? 2.3f : -5),
                    new Vector3(1.2f, 1.5f, .08f), paper, false);
                Block("Stand", easel, new Vector3(x, .9f, i == 0 ? 2.45f : -4.85f),
                    new Vector3(.08f, 1.8f, .1f), wood, false);
            }
            Light(studio, "Studio window light", new Vector3(-18, 2.8f, 1), new Color(1, .91f, .78f), 2.6f, 9);
            var library = world.Find("Library");
            foreach (var pair in new[] { new Vector2(-23, 12), new Vector2(-23, 20),
                         new Vector2(-16, 12), new Vector2(-16, 20) })
                FloorPrefab(Kit + "Shelf/Large_Shelf.prefab", library,
                    "Library aisle shelf", pair.x, pair.y, 90);
            FloorPrefab(Home + "Livingroom/Modern Shelves.prefab", library, "Library tall shelves", -24, 22);
            FloorPrefab(Home + "Livingroom/Round Table.prefab", library, "Reading table", -19, 16);
            FloorPrefab(Home + "Livingroom/Chair 2.prefab", library, "Reading chair one", -18, 14.5f, 180);
            FloorPrefab(Home + "Livingroom/Chair 2.prefab", library, "Reading chair two", -20, 17.5f);
            FloorPrefab(Home + "Decoration/Book (Open).prefab", library, "Open manuscript", -19, 16);
            FloorPrefab(Home + "Decoration/Lamp 2.prefab", library, "Reading lamp", -19.5f, 16);
            FloorPrefab(Home + "Decoration/Painting 2.prefab", library, "Library portrait", -24, 21);
            Sign(library, "LIBRARY", new Vector3(-10.25f, 2.45f, 10), -90);
            Light(library, "Reading pool", new Vector3(-19, 2.6f, 16), new Color(1, .73f, .45f), 2.5f, 8);
            var music = world.Find("MusicWing");
            FloorPrefab(Home + "Livingroom/Sofa.prefab", music, "Music wing sofa", -39, 18);
            FloorPrefab(Home + "Livingroom/Coffee Table.prefab", music, "Music wing coffee table", -37, 17);
            FloorPrefab(Home + "Decoration/Rug Tile.prefab", music, "Music wing rug", -36, 14);
            FloorPrefab(Home + "Decoration/Lamp 2.prefab", music, "Music wing lamp", -40, 11);
            FloorPrefab(Home + "Decoration/Painting Small 1.prefab", music, "Composer portrait", -41, 18);
            Sign(music, "MUSIC WING", new Vector3(-30.25f, 2.45f, 14), -90);
            Light(music, "Piano amber light", new Vector3(-36, 2.7f, 13), new Color(1, .72f, .48f), 2.3f, 8);
            var ballroom = Group("Ball furnishing additions", root.Find("ENVIRONMENT/Ballroom"));
            foreach (var pair in new[] { new Vector2(15, -9), new Vector2(29, -9),
                         new Vector2(15, 9), new Vector2(29, 9) })
            {
                FloorPrefab(Home + "Livingroom/Round Table.prefab", ballroom, "Ballroom edge table", pair.x, pair.y);
                FloorPrefab(Home + "Decoration/Vase 2.prefab", ballroom, "Black Rose arrangement", pair.x, pair.y);
            }
            FloorPrefab(Home + "Building/Curtains (Open Side).prefab", ballroom, "Ballroom drapery", 31, 7);
            Sign(ballroom, "THE BLACK ROSE BALL", new Vector3(22, 4.2f, 11.8f), 0, .12f);
            Light(ballroom, "Ballroom warm center", new Vector3(22, 4.8f, 0), new Color(1, .72f, .49f), 3.1f, 16);
        }
        private static void BuildPiano()
        {
            var piano = Group("Stylized grand piano", world.Find("MusicWing"));
            Block("Body", piano, new Vector3(-36, 1.1f, 12), new Vector3(2.3f, .35f, 1.5f), wood);
            Block("Raised lid", piano, new Vector3(-36, 1.55f, 12.35f), new Vector3(2.3f, .1f, 1.2f), wood, false)
                .transform.rotation = Quaternion.Euler(-18, 0, 0);
            Block("Keyboard", piano, new Vector3(-36, 1.02f, 11.05f), new Vector3(1.8f, .12f, .38f), paper);
            for (int i = 0; i < 13; i++)
                Block("Black key", piano, new Vector3(-36.75f + i * .125f, 1.1f, 11.04f),
                    new Vector3(.05f, .04f, .18f), wood, false);
            foreach (float x in new[] { -36.85f, -35.15f })
                foreach (float z in new[] { 11.45f, 12.55f })
                    Block("Piano leg", piano, new Vector3(x, .53f, z), new Vector3(.12f, 1.05f, .12f), wood);
            FloorPrefab(Home + "Livingroom/Stool.prefab", piano, "Piano bench", -36, 10.3f);
            Block("Music sheet", piano, new Vector3(-36, 1.55f, 11.55f), new Vector3(.55f, .6f, .02f), paper, false);
        }

        private static void FramedArtwork(Transform parent, string name, Vector3 center, float yaw)
        {
            var frame = Group(name, parent);
            frame.SetPositionAndRotation(center, Quaternion.Euler(0, yaw, 0));
            Block("Canvas", frame, center, new Vector3(.9f, 1.2f, .05f), paper, false);
            foreach (float x in new[] { -.5f, .5f })
            {
                var edge = Block("Frame", frame, center, new Vector3(.08f, 1.35f, .09f), wood, false);
                edge.transform.localPosition = new Vector3(x, 0, -.02f);
            }
            foreach (float y in new[] { -.65f, .65f })
            {
                var edge = Block("Frame", frame, center, new Vector3(1.08f, .08f, .09f), wood, false);
                edge.transform.localPosition = new Vector3(0, y, -.02f);
            }
        }
        private static void FinishRooms()
        {
            if (world.Find("Gothic finish") != null) return;
            var finish = Group("Gothic finish", world);
            var charcoal = Mat("Charcoal Stone");
            foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.gameObject.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    renderer.sharedMaterial == stone)
                    renderer.sharedMaterial = charcoal;
            var library = Group("Library paneling", finish);
            foreach (int x in new[] { -24, -20, -16, -12 })
                foreach (int z in new[] { 8, 24 })
                    Block("Dark wood dado", library, new Vector3(x, .85f, z == 8 ? 8.16f : 23.84f),
                        new Vector3(3.8f, 1.5f, .13f), wood, false);
            foreach (int z in new[] { 10, 14, 18, 22 })
            {
                if (z != 14) Block("West dado", library, new Vector3(-25.84f, .85f, z),
                    new Vector3(.13f, 1.5f, 3.8f), wood, false);
                if (z != 10) Block("East dado", library, new Vector3(-10.16f, .85f, z),
                    new Vector3(.13f, 1.5f, 3.8f), wood, false);
            }
            FramedArtwork(library, "Founder study", new Vector3(-18, 2.05f, 23.68f), 180);
            var music = Group("Music wing paneling", finish);
            foreach (int x in new[] { -40, -36, -32 })
                foreach (int z in new[] { 8, 20 })
                    Block("Dark wood dado", music, new Vector3(x, .85f, z == 8 ? 8.16f : 19.84f),
                        new Vector3(3.8f, 1.5f, .13f), wood, false);
            foreach (int z in new[] { 10, 14, 18 })
            {
                Block("West dado", music, new Vector3(-41.84f, .85f, z),
                    new Vector3(.13f, 1.5f, 3.8f), wood, false);
                if (z != 14) Block("East dado", music, new Vector3(-30.16f, .85f, z),
                    new Vector3(.13f, 1.5f, 3.8f), wood, false);
            }
            FramedArtwork(music, "Recital portrait", new Vector3(-36, 2.05f, 19.68f), 180);
            var ballroom = Group("Ballroom upper stonework", finish);
            Block("West upper wall", ballroom, new Vector3(12, 4.3f, 0),
                new Vector3(.28f, 2.4f, 24), charcoal);
            foreach (int x in new[] { 14, 30 })
                foreach (int z in new[] { -10, 10 })
                    FloorPrefab(Kit + "Column/Column_2.prefab", ballroom, "Ballroom outer column", x, z);
        }

        private static GameObject Derived(string sourcePath, string name, Color tint)
        {
            string directory = Vv + "Prefabs/NPC";
            Directory.CreateDirectory(directory);
            string path = directory + "/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) ??
                throw new InvalidOperationException("NPC source missing: " + sourcePath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = name;
            ConvertMaterials(instance);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var shared = renderer.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    var copy = new Material(shared[i]);
                    copy.name = name + " " + renderer.name + " " + i;
                    Color color = copy.HasProperty("_BaseColor") ? copy.GetColor("_BaseColor") : Color.white;
                    bool skin = renderer.name.IndexOf("face", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                renderer.name.IndexOf("skin", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!skin) copy.SetColor("_BaseColor", color * tint);
                    string matPath = Vv + "Materials/ImportedVariants/" + name + "_" +
                        renderer.name.Replace('/', '_') + "_" + i + ".mat";
                    Directory.CreateDirectory(Vv + "Materials/ImportedVariants");
                    AssetDatabase.CreateAsset(copy, AssetDatabase.GenerateUniqueAssetPath(matPath));
                    shared[i] = copy;
                }
                renderer.sharedMaterials = shared;
            }
            var animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                bool male = sourcePath.Contains("Adrian") || sourcePath.Contains("Nolant");
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    Vv + "Animations/" + (male ? "Adrian" : "Celeste") + " Locomotion.controller");
                animator.applyRootMotion = false;
            }
            var saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved ?? throw new InvalidOperationException("Could not create NPC variant " + name);
        }
        private static Transform Point(string name, Vector3 position)
        { var t = Group(name, points); t.position = position; return t; }
        private static GameObject PlaceNpc(GameObject prefab, string name, Vector3 position, float yaw,
            Vector3[] route = null, bool after = false)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, characters);
            obj.name = name; obj.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var animator = obj.GetComponentInChildren<Animator>(true);
            var behavior = obj.AddComponent<AmbientNPC>();
            var waypoints = new List<Transform>();
            if (route != null)
                for (int i = 0; i < route.Length; i++) waypoints.Add(Point(name + " waypoint " + i, route[i]));
            behavior.Configure(animator, waypoints, after);
            if (obj.GetComponent<Collider>() == null)
            {
                var collider = obj.AddComponent<CapsuleCollider>();
                collider.isTrigger = true; collider.height = 1.8f;
                collider.center = Vector3.up * .9f; collider.radius = .45f;
            }
            return obj;
        }
        private static void BuildNPCs()
        {
            var casual = "Assets/AnimeGirls/Casual1/Casual1.prefab";
            var nolant = "Assets/Stylized NPC - Peasant Nolant/Prefabs/Peasant Nolant Brown(Free Version).prefab";
            var adrian = Vv + "Prefabs/Adrian Valmont.prefab";
            var caretakerPrefab = Derived(nolant, "Academy Caretaker", new Color(.75f, .63f, .53f));
            var existingPatrol = UnityEngine.Object.FindFirstObjectByType<PatrolNPC>() ??
                throw new InvalidOperationException("Working gallery patrol missing");
            foreach (Transform child in existingPatrol.transform)
                if (child.name != "Detection Area") child.gameObject.SetActive(false);
            var caretaker = (GameObject)PrefabUtility.InstantiatePrefab(caretakerPrefab, existingPatrol.transform);
            caretaker.name = "Academy Caretaker visual";
            caretaker.transform.localPosition = Vector3.zero;
            caretaker.transform.localRotation = Quaternion.identity;
            existingPatrol.name = "Academy Caretaker - restricted patrol";
            var route = new List<Transform>();
            foreach (var name in new[] { "Patrol south", "Patrol north", "Patrol east" })
            {
                var point = root.Find("SYSTEMS/Core Gameplay/Waypoints and Spawn Points/" + name);
                if (point == null) throw new InvalidOperationException("Existing patrol point missing: " + name);
                route.Add(point);
            }
            existingPatrol.Configure(route, caretaker.GetComponentInChildren<Animator>(true));
            PrefabUtility.RecordPrefabInstancePropertyModifications(existingPatrol);
            var studentColors = new[] {
                new Color(.78f,.68f,.58f), new Color(.59f,.68f,.8f), new Color(.76f,.57f,.66f),
                new Color(.56f,.69f,.58f), new Color(.75f,.73f,.53f), new Color(.65f,.58f,.75f) };
            var students = new GameObject[6];
            var studentPositions = new[] {
                new Vector3(-3,0,-1), new Vector3(-5,0,-9), new Vector3(-17,0,0),
                new Vector3(-34,0,17), new Vector3(15,0,-8), new Vector3(-21,0,22) };
            var studentNames = new[] { "Isabelle", "Amelie", "Margot", "Odette", "Sabine", "Clara" };
            for (int i = 0; i < students.Length; i++)
            {
                var variant = Derived(casual, studentNames[i] + " Casual Student", studentColors[i]);
                students[i] = PlaceNpc(variant, studentNames[i], studentPositions[i], i * 55);
            }
            var male1 = Derived(adrian, "Male Student Burgundy", new Color(.72f,.55f,.61f));
            var male2 = Derived(adrian, "Male Student Slate", new Color(.52f,.6f,.76f));
            PlaceNpc(male1, "Lucien's classmate", new Vector3(-4,0,4), 120,
                new[] { new Vector3(-4,0,4), new Vector3(2,0,4), new Vector3(2,0,0) });
            PlaceNpc(male2, "Ballroom guest", new Vector3(27,0,9), 180);
            var existingAdrian = root.Find("CHARACTERS/Adrian Valmont - ballroom");
            if (existingAdrian == null) throw new InvalidOperationException("Working Adrian actor missing");
            existingAdrian.position = new Vector3(-37, .08551699f, 14);
            existingAdrian.rotation = Quaternion.Euler(0, 90, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(existingAdrian);
            AddDialogue(existingAdrian.gameObject, "Speak to Adrian",
                new[] { new DialogueLine("ADRIAN", "Evelyn."), new DialogueLine("EVELYN", "You remembered."),
                    new DialogueLine("ADRIAN", "I always remember you.") }, null,
                new[] { new DialogueLine("ADRIAN", "The Ball begins soon. I'll be at the piano.") }, null,
                true, true, true, true);
            AddDialogue(students[0], "Speak to Isabelle",
                new[] { new DialogueLine("CELESTE", "Tonight. After his performance."),
                    new DialogueLine("ISABELLE", "You're really going to tell him?"),
                    new DialogueLine("CELESTE", "I have to.") }, null,
                new[] { new DialogueLine("ISABELLE", "Evelyn, have you seen Celeste?") },
                new[] { new DialogueChoice("Not since this morning.", "ISABELLE: I hope she's all right."),
                    new DialogueChoice("She said she was going to the library.", "ISABELLE: I'll look there.", -5),
                    new DialogueChoice("Why would I know?", "ISABELLE: I only asked.", 10) },
                true, true, true, false);
            var celeste = UnityEngine.Object.FindFirstObjectByType<CelesteSequence>() ??
                throw new InvalidOperationException("Working Celeste missing");
            AddDialogue(celeste.gameObject, "Speak to Celeste",
                new[] { new DialogueLine("CELESTE", "Evelyn! Are you coming to the Ball tonight?"),
                    new DialogueLine("CELESTE", "I'm meeting Adrian after his performance.") },
                new[] { new DialogueChoice("Of course.", "CELESTE: I'll see you there."),
                    new DialogueChoice("I haven't decided.", "CELESTE: There's still time."),
                    new DialogueChoice("Are you?", "CELESTE: Yes. I need to tell him something.") },
                null, null, true, false, true, false, true);
        }
        private static void AddDialogue(GameObject obj, string prompt, DialogueLine[] before,
            DialogueChoice[] beforeChoices, DialogueLine[] after, DialogueChoice[] afterChoices,
            bool beforeEnabled = true, bool afterEnabled = true, bool onceBefore = false,
            bool onceAfter = false, bool route = false)
        {
            if (obj.GetComponent<Collider>() == null)
            {
                var collider = obj.AddComponent<CapsuleCollider>();
                collider.isTrigger = true; collider.height = 1.8f;
                collider.center = Vector3.up * .9f; collider.radius = .45f;
            }
            var trigger = obj.GetComponent<DialogueTrigger>() ?? obj.AddComponent<DialogueTrigger>();
            trigger.Configure(prompt, before, beforeChoices, after, afterChoices,
                beforeEnabled, afterEnabled, onceBefore, onceAfter, route);
        }

        private static void Inspect(string title, string action, string text, string reaction,
            Vector3 position, bool before = true, bool after = true)
        {
            var obj = Group(title, gameplay).gameObject;
            obj.transform.position = position;
            var collider = obj.AddComponent<SphereCollider>();
            collider.isTrigger = true; collider.radius = .55f;
            obj.AddComponent<InspectableObject>().Configure(title, action, text, reaction, before, after);
        }
        private static void BuildInspectables()
        {
            Inspect("Black Rose Ball poster", "Examine", "BLACK ROSE BALL\nTonight, 8:00 PM\nAdrian Valmont — Piano Performance",
                "Everyone will be watching him.", new Vector3(1, 1, -9));
            Inspect("Academy trophy", "Examine", "VALMONT ACADEMY\nYoung Artists Exhibition", "Another name on the wall.",
                new Vector3(5, 1, 2));
            Inspect("Hall fireplace", "Examine", "A marble hearth beneath the founder's portrait.", "Too warm.",
                new Vector3(2, 1, 6.5f));
            Inspect("Guest book", "Read", "Guests: Valmont, Morel, Bell, Moreau, Isabelle...", "Some names have been crossed out.",
                new Vector3(-19, 1, 16));
            Inspect("Open manuscript", "Read", "The academy's oldest exhibition catalogue.", "They kept everything.",
                new Vector3(-19, 1, 16.4f));
            Inspect("Locked conservatory door", "Try Door", "Locked.", "Another room they don't want us to see.",
                new Vector3(-15, 1, -6.7f));
            Inspect("Studio painting", "Examine", "Celeste's unfinished study.", "She always used too much red.",
                new Vector3(-18, 1, -6));
            Inspect("Celeste's letter", "Read", "Adrian,\nThere is something I have wanted to tell you…", "No.",
                new Vector3(-20, 1, -4), false, true);
            Inspect("Grand piano", "Play", "A few notes drift through the empty room.", "Adrian would have played it better.",
                new Vector3(-36, 1, 10.6f));
            Inspect("Music sheet", "Examine", "A handwritten arrangement signed: A. Valmont", "He practiced this for months.",
                new Vector3(-36, 1.5f, 11.5f));
            Inspect("Old photograph", "Examine", "Three former students at the winter recital.", "Lucien was smiling then.",
                new Vector3(-4, 1, 25), false, true);
            Inspect("Archive sculpture", "Examine", "An academy laureate, carved in dark stone.", "Prestige has its price here.",
                new Vector3(2, 1, 25), false, true);
            Inspect("Black Rose decoration", "Examine", "Dark flowers frame the ballroom entrance.", "They chose his favorite flower.",
                new Vector3(15, 1, 7));
        }
        private static RectTransform UiObject(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 anchored, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.anchoredPosition = anchored; rect.sizeDelta = size;
            return rect;
        }
        private static Text UiText(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 anchored, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var rect = UiObject(name, parent, min, max, anchored, size);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font; label.fontSize = fontSize; label.alignment = alignment;
            label.color = new Color(.96f, .9f, .78f); label.raycastTarget = false;
            return label;
        }
        private static Button UiButton(string name, Transform parent, Vector2 anchored,
            Vector2 size, out Text label)
        {
            var rect = UiObject(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), anchored, size);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.42f, .13f, .2f, .98f);
            var button = rect.gameObject.AddComponent<Button>();
            label = UiText("Label", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20, TextAnchor.MiddleCenter);
            label.text = name;
            return button;
        }
        private static void BuildDialogueUI()
        {
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
            var panel = UiObject("World Dialogue Panel", hud.transform,
                new Vector2(.13f, .02f), new Vector2(.87f, .42f), Vector2.zero, Vector2.zero);
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(.06f, .04f, .08f, .94f);
            var speaker = UiText("Speaker", panel, new Vector2(.04f, .68f), new Vector2(.5f, .95f),
                Vector2.zero, Vector2.zero, 26, TextAnchor.MiddleLeft);
            var body = UiText("Dialogue", panel, new Vector2(.04f, .29f), new Vector2(.96f, .7f),
                Vector2.zero, Vector2.zero, 23, TextAnchor.MiddleLeft);
            var next = UiButton("Continue", panel, new Vector2(260, -100), new Vector2(170, 42), out _);
            var buttons = new Button[3]; var labels = new Text[3];
            for (int i = 0; i < 3; i++)
                buttons[i] = UiButton("Response " + (i + 1), panel,
                    new Vector2(-140, -48 - i * 48), new Vector2(370, 42), out labels[i]);
            var manager = gameplay.gameObject.AddComponent<DialogueManager>();
            manager.Configure(panel.gameObject, speaker, body, next, buttons, labels);
            panel.gameObject.SetActive(false);
        }
        private static void BuildMusic()
        {
            var parent = Group("AUDIO", root);
            var music = Group("MusicManager", parent).gameObject.AddComponent<MusicManager>();
            string audio = "Assets/Pocket Keys Vol 2 (Free)/WAV/";
            music.Configure(
                AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "LLT_04_Pocket_Keys_Vol_2_(Free)_Loop_60bpm.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "LLT_03_Pocket_Keys_Vol_2_(Free)_Loop_80bpm.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "LLT_01_Pocket_Keys_Vol_2_(Free)_Loop_50bpm.wav"));
            game.SetMusic(music);
        }
        private static void RefreshEvidencePresentation()
        {
            var items = UnityEngine.Object.FindObjectsByType<EvidenceItem>(FindObjectsSortMode.None)
                .OrderBy(item => item.name).ToArray();
            if (items.Length != 3) throw new InvalidOperationException("Expected exactly three working evidence items");
            var manager = UnityEngine.Object.FindFirstObjectByType<EvidenceManager>();
            foreach (var item in items)
            {
                foreach (Transform child in item.transform) child.gameObject.SetActive(false);
                string name = item.name.ToLowerInvariant();
                if (name.Contains("lucien"))
                {
                    item.Configure(manager, "Lucien Morel record",
                        "LUCIEN MOREL — Winner, Young Artists Exhibition\nSTUDENT DISAPPEARS FROM VALMONT ACADEMY\n\nEvelyn: He disappeared last winter.");
                    FloorPrefab(Home + "Decoration/Painting Small 1.prefab", item.transform, "Lucien portrait", item.transform.position.x, item.transform.position.z);
                }
                else if (name.Contains("thomas"))
                {
                    item.Configure(manager, "Thomas Bell photograph",
                        "Thomas Bell was last seen leaving the Music Wing.\nHandwritten: He looked at her too much.\n\nEvelyn: …Adrian?");
                    FloorPrefab(Home + "Decoration/Painting Small 2.prefab", item.transform, "Thomas photograph", item.transform.position.x, item.transform.position.z);
                }
                else
                {
                    item.Configure(manager, "Adrian's sketchbook",
                        "Evelyn reading. Evelyn by the window. Evelyn asleep in the library.\nI never posed for these.\n\nThey kept looking at her. I made sure they stopped.");
                    FloorPrefab(Home + "Decoration/Book (Open).prefab", item.transform, "Adrian sketchbook", item.transform.position.x, item.transform.position.z);
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            }
        }
        private static void ExtendRoses()
        {
            var spawner = UnityEngine.Object.FindFirstObjectByType<BlackRoseSpawner>();
            if (spawner == null || spawner.RosePrefab == null) throw new InvalidOperationException("Working Black Rose spawner missing");
            var spawn = new List<Transform>(spawner.SpawnPoints);
            spawn.Add(Point("Rose library", new Vector3(-22, .8f, 21)));
            spawn.Add(Point("Rose music wing", new Vector3(-40, .8f, 10)));
            spawner.Configure(spawner.RosePrefab, spawn);
        }
        private static void Validate()
        {
            if (UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length != 1 ||
                UnityEngine.Object.FindObjectsByType<GameHUD>(FindObjectsSortMode.None).Length != 1)
                throw new InvalidOperationException("Duplicate or missing GameManager/HUD");
            if (UnityEngine.Object.FindFirstObjectByType<EvidenceManager>().Total != 3)
                throw new InvalidOperationException("Evidence counter changed");
            if (UnityEngine.Object.FindFirstObjectByType<BlackRoseSpawner>().SpawnCount != 6)
                throw new InvalidOperationException("Expected six Black Rose spawn points");
            if (UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Count(r => !r.isKinematic) < 3)
                throw new InvalidOperationException("Three dynamic Rigidbody objects required");
            if (UnityEngine.Object.FindObjectsByType<InspectableObject>(FindObjectsSortMode.None).Length != 13)
                throw new InvalidOperationException("Inspectable count wrong");
            if (UnityEngine.Object.FindFirstObjectByType<DialogueManager>() == null ||
                UnityEngine.Object.FindFirstObjectByType<MusicManager>() == null)
                throw new InvalidOperationException("Dialogue or music manager missing");
        }

        [MenuItem("Velvet Vows/Validate Saved World 2.0")]
        public static void ValidateSaved()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var liveRoot = GameObject.Find("Velvet Vows Main")?.transform ??
                throw new InvalidOperationException("Saved live scene root missing");
            foreach (var path in new[] { "ENVIRONMENT/World 2.0 Additions/Library",
                "ENVIRONMENT/World 2.0 Additions/MusicWing", "ENVIRONMENT/GrandHall",
                "ENVIRONMENT/ArtStudio", "ENVIRONMENT/RestrictedGallery", "ENVIRONMENT/Ballroom",
                "SYSTEMS/World 2.0 Interactions", "CHARACTERS/AmbientNPCs", "AUDIO/MusicManager" })
                if (liveRoot.Find(path) == null) throw new InvalidOperationException("Saved area missing: " + path);
            var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
            if (manager == null || hud == null ||
                UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length != 1 ||
                UnityEngine.Object.FindObjectsByType<GameHUD>(FindObjectsSortMode.None).Length != 1 ||
                UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length != 1)
                throw new InvalidOperationException("Core singleton count changed");
            foreach (var field in new[] { "playerMovement", "playerInput", "hud", "celeste", "suspicion",
                "timer", "evidence", "roses", "ballroomDoor", "endingEvelynPoint", "endingAdrianPoint",
                "adrian", "music" })
                if (new SerializedObject(manager).FindProperty(field)?.objectReferenceValue == null)
                    throw new InvalidOperationException("GameManager serialized reference missing: " + field);
            foreach (var field in new[] { "startPanel", "gameplayPanel", "beginButton", "victoryPanel", "defeatPanel" })
                if (new SerializedObject(hud).FindProperty(field)?.objectReferenceValue == null)
                    throw new InvalidOperationException("HUD serialized reference missing: " + field);
            var start = (GameObject)new SerializedObject(hud).FindProperty("startPanel").objectReferenceValue;
            if (!hud.gameObject.activeInHierarchy || !start.activeSelf || !hud.GetComponent<Canvas>().enabled)
                throw new InvalidOperationException("Saved start UI inactive");
            if (UnityEngine.Object.FindFirstObjectByType<ThirdPersonController>() == null ||
                UnityEngine.Object.FindFirstObjectByType<PlayerInput>() == null ||
                UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>() == null || Camera.main == null)
                throw new InvalidOperationException("Accepted player or camera missing");
            var dialogue = UnityEngine.Object.FindFirstObjectByType<DialogueManager>();
            var music = UnityEngine.Object.FindFirstObjectByType<MusicManager>();
            if (dialogue == null || music == null) throw new InvalidOperationException("New managers missing");
            foreach (var field in new[] { "panel", "speakerText", "bodyText", "continueButton", "choiceButtons" })
                if (new SerializedObject(dialogue).FindProperty(field) == null)
                    throw new InvalidOperationException("Dialogue serialized field missing: " + field);
            foreach (var field in new[] { "panel", "speakerText", "bodyText", "continueButton" })
                if (new SerializedObject(dialogue).FindProperty(field).objectReferenceValue == null)
                    throw new InvalidOperationException("Dialogue serialized reference missing: " + field);
            foreach (var field in new[] { "exploration", "investigation", "finale" })
                if (new SerializedObject(music).FindProperty(field)?.objectReferenceValue == null)
                    throw new InvalidOperationException("Music clip missing: " + field);
            if (UnityEngine.Object.FindFirstObjectByType<EvidenceManager>().Total != 3 ||
                UnityEngine.Object.FindObjectsByType<EvidenceItem>(FindObjectsSortMode.None).Length != 3 ||
                UnityEngine.Object.FindFirstObjectByType<BlackRoseSpawner>().SpawnCount != 6 ||
                UnityEngine.Object.FindObjectsByType<InspectableObject>(FindObjectsSortMode.None).Length != 13 ||
                UnityEngine.Object.FindObjectsByType<DialogueTrigger>(FindObjectsSortMode.None).Length != 3 ||
                UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Count(r => !r.isKinematic) < 3)
                throw new InvalidOperationException("Saved gameplay counts changed");
            Debug.Log("[VelvetVows] Saved live scene validated: one player/camera/GameManager/HUD/EventSystem; " +
                "3 evidence, 6 roses, 13 inspectables, 3 dialogues, 3+ dynamic Rigidbody objects.");
            var patrol = UnityEngine.Object.FindFirstObjectByType<PatrolNPC>();
            var linkedAnimator = new SerializedObject(patrol).FindProperty("modelAnimator").objectReferenceValue as Animator;
            var actor = patrol.transform.Find("Academy Caretaker visual")?.GetComponentInChildren<Animator>(true) ??
                throw new InvalidOperationException("Caretaker animator missing");
            Debug.Log("[VelvetVows] Caretaker animator: " + actor.name +
                ", active=" + actor.isActiveAndEnabled +
                ", avatar=" + (actor.avatar != null ? actor.avatar.name : "null") +
                ", valid=" + (actor.avatar != null && actor.avatar.isValid) +
                ", human=" + (actor.avatar != null && actor.avatar.isHuman) +
                ", controller=" + (actor.runtimeAnimatorController != null ? actor.runtimeAnimatorController.name : "null"));
            Debug.Log("[VelvetVows] Patrol animator reference: " +
                (linkedAnimator != null ? linkedAnimator.name + "/active=" + linkedAnimator.isActiveAndEnabled : "null"));
            if (linkedAnimator != actor) throw new InvalidOperationException("Patrol animator does not reference caretaker");
            foreach (var item in UnityEngine.Object.FindObjectsByType<EvidenceItem>(FindObjectsSortMode.None))
                if (string.IsNullOrEmpty(item.Clue) || !item.Clue.Contains("Evelyn"))
                    throw new InvalidOperationException("Saved evidence text missing: " + item.name);
        }

    }
}
