using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VelvetVows.Editor
{
    public static class Phase3KCharacterPass
    {
        const string Scene = "Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        [MenuItem("Velvet Vows/Art direction/Stage 3 - character performance and sound")]
        public static void Apply()
        {
            Directory.CreateDirectory("Phase3K_Captures");
            const string backup = "Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3K.unity";
            if (!File.Exists(backup)) File.Copy(Scene, backup);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(Scene);
            Directory.CreateDirectory("Assets/VelvetVows/Materials/Phase3K");
            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.Contains("Footstep"))
                .OrderBy(p => p).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();
            var sb = new StringBuilder();
            foreach (var p in Object.FindObjectsByType<AcademyCharacterPresence>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var a = p.GetComponent<Animator>();
                if (a == null || a.avatar == null || !a.avatar.isHuman) continue;
                if (p.GetComponent<AcademyCharacterExpression>() == null) p.gameObject.AddComponent<AcademyCharacterExpression>();
                var steps = p.GetComponent<AcademyFootsteps>() ?? p.gameObject.AddComponent<AcademyFootsteps>();
                steps.Configure(clips); EditorUtility.SetDirty(steps);
                sb.AppendLine(p.CharacterName + " | " + p.name);
                foreach (var renderer in p.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        var mat = renderer.sharedMaterials[i];
                        var original = source != null && source.sharedMaterials.Length > i ? source.sharedMaterials[i] : null;
                        sb.AppendLine(renderer.name + " slot " + i + " | " + (mat != null ? mat.name : "null") + " | source " + (original != null ? original.name : "unknown"));
                        if (mat == null) continue;
                        string key = p.CharacterName + "_" + renderer.name + "_" + i;
                        // Ambient duplicates retain their existing costume variations.
                        if (p.CharacterName == "STUDENT" && renderer.name != "Casual1_Body" && renderer.name != "Casual1" || p.name.Contains("Legacy")) continue;
                        string asset = "Assets/VelvetVows/Materials/Phase3K/" + key + ".mat";
                        var copy = AssetDatabase.LoadAssetAtPath<Material>(asset);
                        if (copy == null) { copy = new Material(mat) { name = key }; AssetDatabase.CreateAsset(copy, asset); }
                        string label = original != null ? original.name.ToUpperInvariant() : "";
                        if (renderer.name == "Casual1_Body")
                        {
                            // Original face cards use alpha masks. Opaque conversion produced black blocks around eyes.
                            copy.SetFloat("_AlphaClip", 1); copy.SetFloat("_Cutoff", .35f);
                            copy.EnableKeyword("_ALPHATEST_ON"); copy.renderQueue = 2450;
                            copy.SetOverrideTag("RenderType", "TransparentCutout");
                        }
                        bool hair = mat.shader.name.Contains("Matte Hair");
                        bool cloth = label.Contains("CLOTH") || renderer.name == "Casual1" || renderer.name == "Body" && i >= 7;
                        if (hair)
                        {
                            var tone = p.CharacterName == "CELESTE" ? new Color(.32f,.25f,.16f) :
                                p.CharacterName == "AMELIE" ? new Color(.19f,.13f,.085f) :
                                p.CharacterName == "MARGOT" ? new Color(.28f,.13f,.09f) :
                                p.CharacterName == "CARETAKER" ? new Color(.35f,.32f,.29f) :
                                p.CharacterName == "EVELYN" ? new Color(.24f,.16f,.115f) : new Color(.13f,.105f,.09f);
                            copy.SetColor("_BaseColor", tone);
                        }
                        else if (cloth)
                        {
                            copy.shader = Shader.Find("VelvetVows/Academy Tailored Fabric");
                            copy.SetFloat("_Cutoff", .3f); copy.SetFloat("_Cull", 0);
                            copy.SetFloat("_PlainCloth", renderer.name == "Casual1" ? 1 : 0);
                            Color coat = p.CharacterName == "CELESTE" ? new Color(.53f,.37f,.39f) :
                                p.CharacterName == "AMELIE" ? new Color(.39f,.46f,.40f) :
                                p.CharacterName == "MARGOT" ? new Color(.49f,.40f,.36f) :
                                p.CharacterName == "PREFECT" ? new Color(.44f,.46f,.49f) :
                                p.CharacterName == "CARETAKER" ? new Color(.30f,.31f,.32f) : new Color(.50f,.49f,.53f);
                            if (label.Contains("TIE")) coat = p.CharacterName == "CELESTE" ? new Color(.45f,.36f,.23f) : new Color(.40f,.13f,.20f);
                            if (label.Contains("SHOES")) coat = new Color(.22f,.21f,.23f);
                            copy.SetColor("_BaseColor", coat);
                            if (copy.HasProperty("_Smoothness")) copy.SetFloat("_Smoothness", label.Contains("SHOES") ? .28f : .10f);
                        }
                        else if (label.Contains("EYEIRIS")) copy.SetColor("_BaseColor", new Color(.72f,.71f,.66f));
                        else if (label.Contains("SKIN") && copy.HasProperty("_Smoothness")) copy.SetFloat("_Smoothness", .22f);
                        EditorUtility.SetDirty(copy); materials[i] = copy;
                    }
                    renderer.sharedMaterials = materials;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
            }
            File.WriteAllText("Phase3K_Captures/cast_materials.txt", sb.ToString());
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Phase3K] Saved facial performance and spatial footsteps for the academy cast.");
        }
    }
}
