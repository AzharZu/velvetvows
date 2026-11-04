using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelvetVows.Editor
{
    public static class Phase3AInstaller
    {
        private const string ScenePath = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        private const string BackupPath = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3A.unity";
        private const string ArtRoot = "Assets/VelvetVows/Art/Intro/";

        [MenuItem("Velvet Vows/Install Phase 3A Secret Diary")]
        public static void Install()
        {
            if (!File.Exists(BackupPath)) throw new Exception("Phase 3A backup is required");
            if (!File.Exists(ScenePath)) throw new Exception("Main scene missing");
            if (!EqualFiles(BackupPath, ScenePath))
                throw new Exception("Main scene changed after Phase 3A backup. Review before installing.");
            Sprite photo = ImportSprite(ArtRoot + "adrian_polaroid.png");
            Sprite diary = ImportSprite(ArtRoot + "evelyn_secret_diary.png");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var story = UnityEngine.Object.FindFirstObjectByType<Phase2AStoryManager>();
            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (story == null || game == null || story.secretDiary != null)
                throw new Exception("Phase 2A opening missing or Phase 3A already installed");
            var root = new GameObject("PHASE 3A EVELYN SECRET DIARY");
            root.transform.SetParent(game.transform, false);
            var intro = root.AddComponent<Phase3ADiaryIntro>();
            intro.adrianPolaroid = photo;
            intro.evelynDiary = diary;
            story.secretDiary = intro;
            EditorUtility.SetDirty(story);
            EditorUtility.SetDirty(intro);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new Exception("Could not save Phase 3A scene");
            Debug.Log("[Phase3A] Installed secret diary and both imported sprites in " + ScenePath);
        }

        private static Sprite ImportSprite(string path)
        {
            if (!File.Exists(path)) throw new Exception("Generated intro image missing: " + path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new Exception("Texture importer missing: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path)
                ?? throw new Exception("Sprite import failed: " + path);
        }

        private static bool EqualFiles(string first, string second)
        {
            using var sha = SHA256.Create();
            using var a = File.OpenRead(first);
            byte[] firstHash = sha.ComputeHash(a);
            using var b = File.OpenRead(second);
            byte[] secondHash = sha.ComputeHash(b);
            if (firstHash.Length != secondHash.Length) return false;
            for (int i = 0; i < firstHash.Length; i++)
                if (firstHash[i] != secondHash[i]) return false;
            return true;
        }
    }
}
