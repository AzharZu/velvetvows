using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace VelvetVows.Editor
{
    public static partial class Phase3IArtPass
    {
        const string Mats = "Assets/VelvetVows/Materials/Phase3I/";
        static Transform world, stage;
        static Material wood, brass, green, burgundy, linen, paper, iron;
        [MenuItem("Velvet Vows/Art direction/Stage 1 - characters and interiors")]
        public static void Apply()
        {
            const string backup="Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3I.unity";
            if(!File.Exists(backup)) File.Copy(Scene,backup);
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(Scene);
            world=scene.GetRootGameObjects().First(x=>x.name=="Velvet Vows Main").transform;
            var old=world.Find("ENVIRONMENT/Phase 3I art direction");if(old!=null)Object.DestroyImmediate(old.gameObject);
            stage=Group(world.Find("ENVIRONMENT"),"Phase 3I art direction");
            Directory.CreateDirectory(Mats);
            wood=Mat("Oiled walnut",new Color(.66f,.50f,.37f),AssetDatabase.LoadAssetAtPath<Material>("Assets/VelvetVows/Materials/GothicStructures/Gothic_Wood.mat"));
            brass=Mat("Antique brass",new Color(.48f,.34f,.16f));brass.SetFloat("_Metallic",.25f);brass.SetFloat("_Smoothness",.27f);brass.SetFloat("_Cull",0);
            green=Mat("Library wool",new Color(.065f,.115f,.095f));burgundy=Mat("Oxblood wool",new Color(.19f,.045f,.065f));
            linen=Mat("Unbleached linen",new Color(.57f,.51f,.41f));paper=Mat("Warm paper",new Color(.72f,.66f,.53f));iron=Mat("Blackened iron",new Color(.055f,.065f,.07f));
            Library();Studio();Caretaker();Characters();Rose();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
            Debug.Log("[Phase3I] Saved character presence, animation blending, coherent Library and Studio art direction.");
        }
        static void Library()
        {
            var library=Group(stage,"Library reading rooms");
            var composition=world.Find("ENVIRONMENT/Visual Polish 2.1/Library reading composition");
            foreach(var t in world.GetComponentsInChildren<Transform>(true))
            {
                if(!t.gameObject.activeInHierarchy)continue;
                var path=PathOf(t);
                if(!path.Contains("/Library/ ")&&!path.Contains("/Library/")&&!path.Contains("/Library reading composition/"))continue;
                if(t.name=="Reading lamp"||t.name=="Second reading lamp"||t.name=="Deep green reading rug"||t.name=="Open academy book")t.gameObject.SetActive(false);
            }
            foreach(var table in composition.Cast<Transform>().Where(t=>t.name=="Reading table"))
            {
                Resize(table,new Vector3(1.5f,.76f,1.5f));Floor(table,new Vector3(table.position.x,0,table.position.z));Paint(table,wood);
                float x=table.position.x,z=table.position.z;
                Rug(library,new Vector3(x,.082f,z),new Vector2(3.5f,3.3f));
                Block(library,"Reading desk felt",new Vector3(x,.771f,z),new Vector3(1.04f,.014f,.68f),green);
                Lamp(library,new Vector3(x,.79f,z+.45f));
                Prefab(library,"Assets/VNB - Home Set/Prefabs/Decoration/Book (Open).prefab","Open reference volume",new Vector3(x,.79f,z-.12f),new Vector3(.5f,.06f,.31f),0);
                var book=Prefab(library,"Assets/Medieval Room Kit/Prefab/Books/Books_2.prefab","Reference stack",new Vector3(x+.48f,.78f,z+.08f),new Vector3(.22f,.09f,.29f),12);Paint(book,Mat("Faded leather",new Color(.23f,.11f,.10f)));
            }
            var oldLibrary=world.Find("ENVIRONMENT/World 2.0 Additions/Library");
            var catalogueTable=oldLibrary.Find("Reading table");
            Resize(catalogueTable,new Vector3(1.25f,.76f,1.25f));Floor(catalogueTable,new Vector3(-19,0,16));Paint(catalogueTable,wood);
            Floor(oldLibrary.Find("Open manuscript"),new Vector3(-19,.775f,16));
            foreach(var chair in composition.Cast<Transform>().Where(t=>t.name=="Reading chair"&&t.gameObject.activeSelf))
            { Resize(chair,new Vector3(.53f,1.04f,.58f));Floor(chair,new Vector3(chair.position.x,0,chair.position.z)); }
            var cases=world.Find("ENVIRONMENT/Phase 3H repaired academy/Library fitted bookcases");
            foreach(Transform cabinet in cases)
            {
                foreach(var r in cabinet.GetComponentsInChildren<Renderer>())if(r.name!="Bound volume")r.sharedMaterial=wood;
                var ornament=Group(library,"Bookcase cornice and spine detail");ornament.SetPositionAndRotation(cabinet.position,cabinet.rotation);
                Local(ornament,"Crown lower",new Vector3(0,2.65f,0),new Vector3(2.18f,.12f,.64f),wood);
                Local(ornament,"Crown upper",new Vector3(0,2.76f,0),new Vector3(2.27f,.09f,.70f),wood);
                Local(ornament,"Cornice brass line",new Vector3(0,2.69f,.327f),new Vector3(2.14f,.014f,.012f),brass);
                foreach(var volume in cabinet.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Bound volume"))
                {
                    var p=volume.transform.localPosition;var size=volume.transform.localScale;
                    foreach(float offset in new[]{-.31f,.31f})Local(ornament,"Leather spine band",new Vector3(p.x,p.y+offset*size.y,p.z+size.z*.5f+.002f),new Vector3(size.x*.85f,.012f,.006f),brass);
                }
                Local(ornament,"Bookcase catalogue plate",new Vector3(0,2.48f,.28f),new Vector3(.22f,.055f,.009f),brass);
            }
            // Keep the two doorways and the central quest routes clear.
            Dado(library,new Vector3(-18,.56f,23.72f),new Vector3(15.3f,1.05f,.07f));
            Dado(library,new Vector3(-10.28f,.56f,18.5f),new Vector3(.07f,1.05f,10));
            Dado(library,new Vector3(-25.72f,.56f,20),new Vector3(.07f,1.05f,7));
            var sofa=composition.Find("Quiet reading corner");if(sofa!=null)Paint(sofa,green);
            WindowLight(library,"Cool Library window",new Vector3(-25,4.2f,19),new Vector3(-19,.5f,17));
        }
        static void Studio()
        {
            var room=Group(stage,"Art Studio workroom");
            var table=world.Find("ENVIRONMENT/ArtStudio/Studio tables, books and supplies/Painting worktable two");
            Resize(table,new Vector3(2.25f,.9f,1.8f));Floor(table,new Vector3(-19.3f,0,1.25f));Paint(table,wood);
            Block(room,"Studio linen runner",new Vector3(-19.3f,.912f,1.25f),new Vector3(.95f,.018f,1.4f),linen);
            Lamp(room,new Vector3(-20.03f,.925f,1.78f));
            var papers=Group(room,"Drawing studies - unframed paper");
            for(int i=0;i<3;i++)
            {var sheet=Block(papers,"Study paper",new Vector3(-19.23f+i*.015f,.93f+i*.003f,.98f),new Vector3(.31f,.003f,.42f),paper);sheet.rotation=Quaternion.Euler(0,i*7,0);}
            for(int i=0;i<3;i++)
            {
                var pot=Cylinder(room,"Stoneware pigment pot",new Vector3(-18.55f+i*.17f,.978f,1.82f),.11f,.105f,Mat("Muted pigment "+i,new[]{new Color(.22f,.12f,.10f),new Color(.10f,.18f,.16f),new Color(.24f,.22f,.19f)}[i]));
                Cylinder(room,"Pigment pot lid",pot.position+Vector3.up*.06f,.12f,.012f,iron);
            }
            for(int i=0;i<2;i++)
            {
                var brush=Block(room,"Drawing brush",new Vector3(-19.72f+i*.07f,.93f,.85f),new Vector3(.009f,.009f,.25f),wood);
                brush.rotation=Quaternion.Euler(0,12+i*5,0);
            }
            var shelf=Prefab(room,"Assets/Medieval Room Kit/Prefab/Shelf/Large_Shelf.prefab","Studio wall storage",new Vector3(-23.3f,0,1.7f),new Vector3(1.7f,2.1f,.44f),90);
            // Fitted storage stays against the wall, clear of Margot's route.
            Paint(shelf,wood);
            Dado(room,new Vector3(-18,.5f,-7.72f),new Vector3(11.3f,.95f,.07f));
            foreach(var t in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Paint-stained stool"&&t.gameObject.activeInHierarchy))
            {Resize(t,new Vector3(.43f,.5f,.43f));Floor(t,new Vector3(t.position.x,0,t.position.z));Paint(t,wood);}
            var cleanup=Object.FindFirstObjectByType<Phase2BCleanupManager>();
            Paint(cleanup.wardrobe,wood);
            // Neutralize the modern palette while retaining the existing door pivots and quest colliders.
            var basin=world.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Studio wash basin");Paint(basin,wood);
            WindowLight(room,"Cool Studio window",new Vector3(-13,4.4f,2),new Vector3(-19,.7f,0));
        }
        static void Caretaker()
        {
            var patrol=world.GetComponentsInChildren<PatrolNPC>(true).First(p=>p.name.Contains("Caretaker"));
            var legacy=patrol.transform.Find("Academy Caretaker visual");
            Transform visual=patrol.transform.Find("Adrian Valmont");
            if(visual!=null)
            {
                if(legacy!=null){legacy.name="Legacy Caretaker peasant visual";legacy.gameObject.SetActive(false);}
                visual.name="Academy Caretaker visual";
            }
            else visual=legacy;
            if(visual==null)throw new InvalidOperationException("Caretaker uniform model missing");
            visual.gameObject.SetActive(true);visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;
            visual.localScale=Vector3.one*1.05f;
            var animator=visual.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/VelvetVows/Animations/Adrian Locomotion.controller");
            var settings=new SerializedObject(patrol);settings.FindProperty("modelAnimator").objectReferenceValue=animator;settings.ApplyModifiedPropertiesWithoutUndo();
            var presentation=animator.GetComponent<AcademyCharacterPresentation>()??animator.gameObject.AddComponent<AcademyCharacterPresentation>();presentation.Configure(animator,.82f);
            foreach(var r in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var shared=r.sharedMaterials;
                if(shared.Length>8)
                {
                    var hair=Mat("Caretaker grey hair",new Color(.35f,.32f,.29f),shared[0]);shared[0]=hair;
                    for(int i=7;i<=8;i++)shared[i]=Mat("Caretaker charcoal uniform "+i,new Color(.36f,.37f,.38f),shared[i]);
                    r.sharedMaterials=shared;
                }
            }
        }
        static void Characters()
        {
            foreach(var a in world.GetComponentsInChildren<Animator>(true))
            {
                if(a.avatar==null||!a.avatar.isHuman)continue;
                var path=PathOf(a.transform);string id="STUDENT";
                foreach(var key in new[]{"Evelyn","Adrian","Celeste","Isabelle","Amelie","Margot","Odette","Clara","Prefect","Caretaker"})
                    if(path.IndexOf(key,StringComparison.OrdinalIgnoreCase)>=0){id=key.ToUpperInvariant();break;}
                if(path.Contains("/PLAYER/"))id="EVELYN";
                var presence=a.GetComponent<AcademyCharacterPresence>()??a.gameObject.AddComponent<AcademyCharacterPresence>();presence.Configure(id);
            }
            var prefect=Object.FindFirstObjectByType<Phase2BPrefectInvestigation>();
            var animator=prefect.GetComponentInChildren<Animator>();
            var chest=animator.GetBoneTransform(HumanBodyBones.UpperChest)??animator.GetBoneTransform(HumanBodyBones.Chest);
            var badge=chest.Find("Prefect brass insignia");
            if(badge!=null)
            {
                var meshPath=Mats+"Prefect shield.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                {
                    bool create=mesh==null;
                    if(create)mesh=new Mesh{name="Five point Prefect shield"};else mesh.Clear();
                    mesh.vertices=new[]{new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.13f,0),new Vector3(0,-.5f,0),new Vector3(-.5f,-.13f,0)};
                    mesh.triangles=new[]{0,2,1,0,3,2,0,4,3};mesh.RecalculateNormals();mesh.RecalculateBounds();if(create)AssetDatabase.CreateAsset(mesh,meshPath);else EditorUtility.SetDirty(mesh);
                }
                badge.GetComponent<MeshFilter>().sharedMesh=mesh;badge.GetComponent<Renderer>().sharedMaterial=brass;
            }
            var old=chest.Find("Phase 3I collar insignia");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var insignia=Group(chest,"Phase 3I collar insignia");
            foreach(float side in new[]{-.045f,.045f})
            {
                var pin=Block(insignia,"Prefect collar pin",chest.position+prefect.transform.forward*.1f+prefect.transform.right*side+Vector3.up*.105f,new Vector3(.012f,.022f,.006f),brass);
                pin.rotation=prefect.transform.rotation;
            }
        }
        static void Rose()
        {
            const string prefab="Assets/VelvetVows/Prefabs/Black Rose.prefab";
            const string backup="Assets/VelvetVows/Prefabs/Black Rose_PrePhase3I.prefab";
            if(!File.Exists(backup))File.Copy(prefab,backup);
            var rose=PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                string meshPath=Mats+"Black rose petals.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                {
                    bool create=mesh==null;
                    var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
                    for(int ring=0;ring<3;ring++)for(int petal=0;petal<7;petal++)
                    {
                        int start=vertices.Count;float angle=petal*Mathf.PI*2/7+ring*.47f;
                        float radius=.12f-ring*.027f;
                        for(int v=0;v<6;v++)for(int u=0;u<7;u++)
                        {
                            float along=v/5f,across=u/6f*2-1;
                            float theta=angle+across*(.52f-.17f*along);
                            float r=radius*(.20f+.80f*along)*(1-.12f*across*across);
                            float y=-.035f+along*(.10f+ring*.02f)-Mathf.Pow(along,3)*(.08f-ring*.018f)+across*across*.018f;
                            vertices.Add(new Vector3(Mathf.Cos(theta)*r,y,Mathf.Sin(theta)*r));
                        }
                        for(int v=0;v<5;v++)for(int u=0;u<6;u++)
                        {
                            int a=start+v*7+u,b=a+1,c=a+7,d=c+1;
                            triangles.AddRange(new[]{a,c,b,b,c,d});
                        }
                    }
                    if(create)mesh=new Mesh{name="Layered black rose petals"};else mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();if(create)AssetDatabase.CreateAsset(mesh,meshPath);else EditorUtility.SetDirty(mesh);
                }
                var bloom=rose.transform.Find("Bloom");bloom.localScale=Vector3.one;
                bloom.GetComponent<MeshFilter>().sharedMesh=mesh;
                var petals=Mat("Black rose oxblood petals",new Color(.16f,.025f,.055f));petals.SetFloat("_Cull",0);bloom.GetComponent<Renderer>().sharedMaterial=petals;
                var stem=rose.transform.Find("Stem");stem.localPosition=new Vector3(0,-.13f,0);stem.localScale=new Vector3(.012f,.26f,.012f);stem.GetComponent<Renderer>().sharedMaterial=Mat("Rose stem",new Color(.055f,.11f,.07f));
                PrefabUtility.SaveAsPrefabAsset(rose,prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(rose);}
        }
        static void Rug(Transform parent,Vector3 p,Vector2 size)
        {
            Block(parent,"Oxblood reading rug",p,new Vector3(size.x,.012f,size.y),burgundy);
            foreach(float sign in new[]{-1f,1f})
            {
                Block(parent,"Woven rug border",p+new Vector3(sign*(size.x*.5f-.12f),.008f,0),new Vector3(.035f,.004f,size.y-.19f),linen);
                Block(parent,"Woven rug border",p+new Vector3(0,.008f,sign*(size.y*.5f-.12f)),new Vector3(size.x-.19f,.004f,.035f),linen);
            }
        }
        static void Dado(Transform parent,Vector3 p,Vector3 size)
        {
            Block(parent,"Walnut wall lining",p,size,wood);
            var rail=size;rail.y=.055f;rail.x+=size.x>1?.03f:0;rail.z+=size.z>1?.03f:0;
            Block(parent,"Dado cap",p+Vector3.up*(size.y*.5f),rail,wood);
        }
        static void Lamp(Transform parent,Vector3 p)
        {
            // Purpose-made desk silhouette: a low base, upright and green shade.
            Cylinder(parent,"Reading lamp brass base",p+Vector3.up*.017f,.18f,.034f,brass);
            Cylinder(parent,"Reading lamp stem",p+Vector3.up*.15f,.018f,.27f,brass);
            var shade=Block(parent,"Green enamel desk shade",p+Vector3.up*.29f,new Vector3(.32f,.12f,.17f),green);
            var glow=Mat("Warm lamp diffuser",new Color(.8f,.65f,.39f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.64f,.27f)*1.4f);
            Block(parent,"Warm lamp diffuser",shade.position-Vector3.up*.061f,new Vector3(.27f,.006f,.13f),glow);
            var light=Group(parent,"Warm reading pool").gameObject.AddComponent<Light>();light.transform.position=p+Vector3.up*.25f;
            light.type=LightType.Point;light.color=new Color(1,.72f,.43f);light.intensity=1.7f;light.range=3.8f;light.shadows=LightShadows.None;
        }
        static void WindowLight(Transform parent,string name,Vector3 position,Vector3 target)
        {
            var light=Group(parent,name).gameObject.AddComponent<Light>();light.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            light.type=LightType.Spot;light.color=new Color(.60f,.72f,1);light.intensity=3.6f;light.range=10;light.spotAngle=85;light.innerSpotAngle=50;light.shadows=LightShadows.Soft;light.shadowStrength=.45f;
        }
        static Transform Prefab(Transform parent,string path,string name,Vector3 p,Vector3 size,float yaw)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name=name;go.transform.SetParent(parent,true);
            Resize(go.transform,size);go.transform.rotation=Quaternion.Euler(0,yaw,0);Floor(go.transform,p);
            foreach(var r in go.GetComponentsInChildren<Renderer>())
            {
                var shared=r.sharedMaterials;
                for(int i=0;i<shared.Length;i++)
                {
                    var source=shared[i];if(source==null||source.shader.name.StartsWith("Universal Render Pipeline/"))continue;
                    var converted=Mat("Imported "+source.name,Color.white);
                    converted.SetTexture("_BaseMap",source.mainTexture);shared[i]=converted;
                }
                r.sharedMaterials=shared;
            }
            return go.transform;
        }
        static Transform Group(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
        static Transform Block(Transform parent,string name,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=p;go.transform.localScale=size;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;go.transform.SetParent(parent,true);return go.transform;
        }
        static Transform Cylinder(Transform parent,string name,Vector3 p,float diameter,float height,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.position=p;go.transform.localScale=new Vector3(diameter,height*.5f,diameter);
            Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;go.transform.SetParent(parent,true);return go.transform;
        }
        static void Local(Transform parent,string name,Vector3 p,Vector3 size,Material material)
        {var t=Block(parent,name,parent.TransformPoint(p),size,material);t.rotation=parent.rotation;}
        static Bounds BoundsOf(Transform t)
        {var rs=t.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
        static void Resize(Transform t,Vector3 desired)
        {var b=BoundsOf(t);var s=t.localScale;t.localScale=Vector3.Scale(s,new Vector3(desired.x/b.size.x,desired.y/b.size.y,desired.z/b.size.z));}
        static void Floor(Transform t,Vector3 p)
        {t.position=p;var b=BoundsOf(t);t.position+=new Vector3(p.x-b.center.x,p.y-b.min.y,p.z-b.center.z);}
        static void Paint(Transform t,Material m)
        {foreach(var r in t.GetComponentsInChildren<Renderer>(true)){var shared=r.sharedMaterials;for(int i=0;i<shared.Length;i++)shared[i]=m;r.sharedMaterials=shared;}}
        static Material Mat(string name,Color colour,Material source=null)
        {
            var path=Mats+name.Replace(' ','_')+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=source!=null?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",colour);m.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(m);return m;
        }
    }
}
