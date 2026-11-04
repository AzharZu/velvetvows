using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelvetVows.Editor
{
    public static class Phase3E1Fixer
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string BackupPath = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3E1.unity";

        [MenuItem("Velvet Vows/Apply Phase 3E1 Geometry Fixes")]
        public static void Fix()
        {
            if (!File.Exists(BackupPath)) throw new IOException("Phase 3E1 backup is required");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = SceneManager.GetActiveScene().GetRootGameObjects().First(g => g.name == "Velvet Vows Main").transform;
            var gothic = Find(root, "WORLD/GOTHIC_ACADEMY");
            if (gothic.Find("Phase 3E1 Library threshold") != null)
                throw new IOException("Phase 3E1 geometry was already installed");
            var threshold = new GameObject("Phase 3E1 Library threshold").transform;
            threshold.SetParent(gothic, false);

            // The previous window and dark backing looked like a solid wall, but
            // had no collider. An existing Gothic portal opens onto the corridor.
            Find(root, "WORLD/GOTHIC_ACADEMY/Grand Hall/Walls and windows/West pointed window").gameObject.SetActive(false);
            Find(root, "WORLD/GOTHIC_ACADEMY/Grand Hall/Window backing/West dark exterior").gameObject.SetActive(false);
            var portal = Find(root, "WORLD/GOTHIC_ACADEMY/Connected wings/Library/Pointed walls and windows/Library East portal");
            var hallPortal = Object.Instantiate(portal.gameObject, threshold);
            hallPortal.name = "Visible Library corridor portal";
            hallPortal.transform.SetPositionAndRotation(new Vector3(-8f, 0, 6f), portal.rotation);
            var stone = Find(root, "WORLD/GOTHIC_ACADEMY/Connected wings/Library/Pointed walls and windows/Library East 0-0 solid stone");
            var infill = Object.Instantiate(stone.gameObject, threshold);
            infill.name = "Library entrance south stone infill";
            infill.transform.SetPositionAndRotation(new Vector3(-8f, 0, 1.5f), stone.rotation);
            Box(threshold, "South entrance wall collider", new Vector3(-8f, 3f, 1.5f), new Vector3(.28f, 6f, 3f));
            PortalSides(threshold, -8f, 6f, "Hall");
            PortalSides(threshold, -10f, 10f, "Library");
            Box(threshold, "Continuous entrance floor collider", new Vector3(-8f, -.12f, 6f),
                new Vector3(1.2f, .22f, 3.2f));
            var plaque = Find(root, "ENVIRONMENT/Visual Polish 2.1/Academy door plaques/LIBRARY plaque");
            var hallPlaque = Object.Instantiate(plaque.gameObject, threshold);
            hallPlaque.name = "LIBRARY entrance plaque";
            hallPlaque.transform.SetPositionAndRotation(new Vector3(-7.86f, 2.72f, 6f), plaque.rotation);

            var composition = Find(root, "ENVIRONMENT/Visual Polish 2.1/Library reading composition");
            var sideTable = composition.Find("Reading corner side table");
            sideTable.position = new Vector3(-22.65f, 0, 18.45f);
            var returnedBooks = Find(root, "SYSTEMS/Core Gameplay/PHASE 2A STORY/Clara's returned books");
            returnedBooks.position = new Vector3(-23.1f, returnedBooks.position.y, 18.45f);
            RestOn(returnedBooks, .54f);
            foreach (Transform child in composition)
            {
                if (child.name != "Open academy book") continue;
                if (child.position.x < -21.5f) child.position = new Vector3(child.position.x, child.position.y, 18.65f);
                RestOn(child, child.position.x < -21.5f ? .54f : .65f);
            }
            var sketchbook = Find(root, "SYSTEMS/Core Gameplay/PHASE 2A STORY/A. V. sketchbook");
            var sketchTable = Object.Instantiate(sideTable.gameObject, composition);
            sketchTable.name = "Sketchbook side table";
            sketchTable.transform.position = new Vector3(-18.8f, 0, 20.2f);
            var tableBounds = RenderBounds(sketchTable.transform);
            var tableCollider = sketchTable.AddComponent<BoxCollider>();
            tableCollider.center = sketchTable.transform.InverseTransformPoint(tableBounds.center);
            tableCollider.size = tableBounds.size;
            RestOn(sketchbook, tableBounds.max.y);
            RestHallFlowers(root);

            var story = Object.FindFirstObjectByType<Phase2AStoryManager>(FindObjectsInactive.Include);
            var celesteAnimator = story.celeste.GetComponentInChildren<Animator>();
            if (celesteAnimator.transform == story.celeste) throw new IOException("Celeste model is not a separate visual transform");
            celesteAnimator.transform.localPosition += Vector3.down * .08f;
            var book = story.adrianHeldBook;
            var bookSize = RenderBounds(book.transform).size;
            float largest = Mathf.Max(bookSize.x, bookSize.y, bookSize.z);
            if (largest > .001f) book.transform.localScale *= .27f / largest;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save Phase 3E1 scene");
            Debug.Log("[Phase3E1] Corrected Library entrance, prop support, Celeste feet and Adrian's held book");
        }

        [MenuItem("Velvet Vows/Refine Phase 3E1 Prop Support")]
        public static void Refine()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = SceneManager.GetActiveScene().GetRootGameObjects().First(g => g.name == "Velvet Vows Main").transform;
            RestHallFlowers(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save Phase 3E1 prop refinement");
            Debug.Log("[Phase3E1] Hall vases aligned with their side tables");
        }

        private static void RestHallFlowers(Transform root)
        {
            var hall = Find(root, "ENVIRONMENT/Visual Polish 2.1/Grand Hall composition");
            foreach (float x in new[] { -5.6f, 5.6f })
            {
                var table = hall.Cast<Transform>().First(t => t.name == "Portrait side table" &&
                    Mathf.Abs(t.position.x - x) < .2f);
                var vase = hall.Cast<Transform>().First(t => t.name == "Academy flowers" &&
                    Mathf.Abs(t.position.x - x) < .2f);
                RestOn(vase, RenderBounds(table).max.y);
            }
        }

        private static Transform Find(Transform root, string path) =>
            root.Find(path) ?? throw new IOException("Missing scene object: " + path);

        private static void PortalSides(Transform parent, float x, float z, string label)
        {
            Box(parent, label + " portal south jamb", new Vector3(x, 3f, z - 2.25f), new Vector3(.3f, 6f, 1.5f));
            Box(parent, label + " portal north jamb", new Vector3(x, 3f, z + 2.25f), new Vector3(.3f, 6f, 1.5f));
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<BoxCollider>().size = size;
        }

        private static Bounds RenderBounds(Transform target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new IOException("No renderer on " + target.name);
            var result = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) result.Encapsulate(renderer.bounds);
            return result;
        }

        private static void RestOn(Transform prop, float surfaceY)
        {
            var bounds = RenderBounds(prop);
            prop.position += Vector3.up * (surfaceY + .005f - bounds.min.y);
        }

        [MenuItem("Velvet Vows/Audit Phase 3E1 Geometry")]
        public static void Audit()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var report = new StringBuilder();
            foreach (var obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!obj.scene.IsValid()) continue;
                var p = obj.transform.position;
                string name = obj.name.ToLowerInvariant();
                bool intro = p.x > -6 && p.x < 1 && p.z > -9 && p.z < -3 &&
                    (name.Contains("sofa") || name.Contains("chair") || name.Contains("table") || name.Contains("wall") || name.Contains("floor"));
                bool passage = p.x > -13 && p.x < -7 && p.z > 5 && p.z < 16 &&
                    (name.Contains("library") || name.Contains("corridor") || name.Contains("door") || name.Contains("wall") || name.Contains("floor"));
                bool book = p.x > -27 && p.x < -9 && p.z > 7 && p.z < 25 &&
                    (name.Contains("book") || name.Contains("table") || name.Contains("shelf") || name.Contains("sofa"));
                bool celeste = name == "celeste";
                if (!(intro || passage || book || celeste)) continue;
                var renderers = obj.GetComponentsInChildren<Renderer>(true);
                var colliders = obj.GetComponentsInChildren<Collider>(true);
                var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(p, Vector3.zero);
                foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                var cb = colliders.Length > 0 ? colliders[0].bounds : new Bounds(p, Vector3.zero);
                foreach (var c in colliders.Skip(1)) cb.Encapsulate(c.bounds);
                report.AppendLine($"{(intro ? "INTRO" : passage ? "PASS" : book ? "BOOK" : "CELESTE")}\t{obj.name}\t{Path(obj.transform)}\tpos={p:F2}\trot={obj.transform.eulerAngles:F0}\trender={bounds.center:F2}/{bounds.size:F2}\tcolliders={colliders.Length}:{cb.center:F2}/{cb.size:F2}\tactive={obj.activeInHierarchy}");
            }
            var story = Object.FindFirstObjectByType<Phase2AStoryManager>(FindObjectsInactive.Include);
            foreach (var actor in new[] { story.celeste, story.adrian, story.player })
                report.AppendLine($"ACTOR\t{actor.name}\tpos={actor.position:F3}\tmodel={actor.GetComponentInChildren<Animator>().transform.position:F3}\tcontroller={actor.GetComponent<CharacterController>() != null}");
            foreach (var prop in new[] { story.openingBooks, story.adrianHeldBook, story.sketchbook })
            {
                var rs = prop.GetComponentsInChildren<Renderer>(true);
                var b = rs[0].bounds;
                foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
                report.AppendLine($"STORYPROP\t{prop.name}\tpos={prop.transform.position:F3}\tbounds={b.center:F3}/{b.size:F3}\tscale={prop.transform.localScale:F5}");
            }
            var cameraPoint = new Vector3(-6.55f, 1.65f, 7.8f);
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (r.bounds.SqrDistance(cameraPoint) < 9f && r.bounds.center.y < 4f)
                    report.AppendLine($"NEAR\t{Path(r.transform)}\tbounds={r.bounds.center:F2}/{r.bounds.size:F2}\tdist={Mathf.Sqrt(r.bounds.SqrDistance(cameraPoint)):F2}");
            File.WriteAllText("/tmp/VelvetVows_Phase3E1_geometry.txt", report.ToString());
            Debug.Log("[Phase3E1] Wrote geometry audit with " + report.ToString().Split('\n').Length + " lines");
        }

        private static string Path(Transform t)
        {
            var value = t.name;
            while (t.parent != null) { t = t.parent; value = t.name + "/" + value; }
            return value;
        }
    }
}
