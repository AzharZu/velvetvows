using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelvetVows.Editor
{
    public static class Phase3BInstaller
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string BackupPath = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3B.unity";
        private const string ImagePath = "Assets/VelvetVows/Art/MissingStudents/missing_students_dossier.png";

        [MenuItem("Velvet Vows/Install Phase 3B Missing Students")]
        public static void Install()
        {
            if (!File.Exists(BackupPath) || !SameHash(BackupPath, ScenePath))
                throw new Exception("Phase 3B backup must match the main scene before installation");
            var sprite = ImportSprite(ImagePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var cleanup = UnityEngine.Object.FindFirstObjectByType<Phase2BCleanupManager>();
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
            if (game == null || cleanup == null || hud == null || cleanup.missingStudents != null)
                throw new Exception("Accepted scene systems missing or Phase 3B already installed");
            var book = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(x => x.name == "Open academy book")
                .OrderBy(x => Vector3.Distance(x.position, new Vector3(-15.2f, .85f, 19.5f)))
                .FirstOrDefault();
            if (book == null || Vector3.Distance(book.position, new Vector3(-15.2f, .85f, 19.5f)) > .5f)
                throw new Exception("Existing Library reading-corner book not found");
            var root = new GameObject("PHASE 3B MISSING STUDENTS");
            root.transform.SetParent(game.transform, false);
            var director = root.AddComponent<Phase3BMissingStudents>();
            director.newspaperCollage = sprite;
            director.game = game;
            director.hud = hud;
            cleanup.missingStudents = director;
            var interaction = book.gameObject.AddComponent<Phase3BDossierInteractable>();
            interaction.director = director;
            var sphere = book.gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = .85f / Mathf.Max(.01f, book.lossyScale.x);
            EditorUtility.SetDirty(book.gameObject);
            EditorUtility.SetDirty(cleanup);
            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new Exception("Could not save Phase 3B scene");
            Debug.Log("[Phase3B] Installed Library dossier on existing book and Adrian monologue");
        }

        private static Sprite ImportSprite(string path)
        {
            if (!File.Exists(path)) throw new Exception("Newspaper collage missing: " + path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new Exception("Texture importer missing");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new Exception("Sprite import failed");
        }

        private static bool SameHash(string first, string second)
        {
            if (!File.Exists(second)) return false;
            using var sha = SHA256.Create();
            using var a = File.OpenRead(first);
            byte[] firstHash = sha.ComputeHash(a);
            using var b = File.OpenRead(second);
            byte[] secondHash = sha.ComputeHash(b);
            return firstHash.SequenceEqual(secondHash);
        }
    }
}
