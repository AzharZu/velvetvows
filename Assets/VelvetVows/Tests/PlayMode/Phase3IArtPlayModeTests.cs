using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace VelvetVows.Tests
{
    public sealed class Phase3IArtPlayModeTests
    {
        private bool previousAsync;
        [UnityTearDown]
        public IEnumerator RestoreShaderCompilation(){ShaderUtil.allowAsyncCompilation=previousAsync;yield return null;}
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static MonoBehaviour Get(string name)=>Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(b=>b.GetType().Name==name);
        static object Field(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
        static object Value(object o,string n)=>o.GetType().GetProperty(n,Flags).GetValue(o);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        [UnityTest,Timeout(120000)]
        public IEnumerator InteriorsPresenceAndMotionBlend()
        {
            previousAsync=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale=1;Application.runInBackground=true;yield return new WaitForSeconds(.6f);
            var story=Get("Phase2AStoryManager");var hud=Get("GameHUD");var dialogue=Get("DialogueManager");
            Call(hud,"ShowStart",false);Call(hud,"ShowGameplay",true);
            Camera.main.GetComponents<Behaviour>().First(b=>b.GetType().Name=="CinemachineBrain").enabled=false;
            var caretaker=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(b=>b.GetType().Name=="PatrolNPC"&&b.name.Contains("Caretaker"));
            var caretakerAnimator=(Animator)Field(caretaker,"modelAnimator");
            Assert.IsTrue(caretakerAnimator.isActiveAndEnabled);Assert.IsTrue(caretakerAnimator.avatar.isHuman);
            Assert.IsTrue(caretakerAnimator.transform.IsChildOf(caretaker.transform.Find("Academy Caretaker visual")));
            Assert.IsFalse(caretaker.transform.Find("Legacy Caretaker peasant visual").gameObject.activeSelf);
            var stage=GameObject.Find("Phase 3I art direction");Assert.NotNull(stage);
            Assert.AreEqual(3,stage.GetComponentsInChildren<Light>().Count(l=>l.name=="Warm reading pool"));
            var pads=stage.GetComponentsInChildren<Transform>().Where(t=>t.name=="Reading desk felt").ToArray();Assert.AreEqual(2,pads.Length);
            foreach(var pad in pads)Assert.That(pad.position.y,Is.EqualTo(.771f).Within(.005f));
            Frame(new Vector3(-17,2.7f,10),new Vector3(-21,1.2f,19),62);yield return Capture("01_library");
            Frame(new Vector3(-15.5f,1.75f,-7.1f),new Vector3(-18,1.1f,-.8f),65);yield return Capture("02_studio");
            var prefect=Get("Phase2BPrefectInvestigation");((Behaviour)Field(prefect,"normalPatrol")).enabled=false;
            Frame(prefect.transform.position+new Vector3(-1.3f,1.6f,-2.4f),prefect.transform.position+Vector3.up*1.15f,42);yield return Capture("03_prefect");
            var spawner=Get("BlackRoseSpawner");Assert.AreEqual(6,(int)Value(spawner,"SpawnCount"));
            var bloom=spawner.GetComponentsInChildren<MeshFilter>().First(m=>m.name=="Bloom");
            Assert.Greater(bloom.sharedMesh.vertexCount,800,"Rose must have layered petals rather than a primitive sphere");
            foreach(var normal in bloom.sharedMesh.normals)Assert.Greater(normal.sqrMagnitude,.9f,"Petal normals must remain valid");
            var center=bloom.GetComponent<Renderer>().bounds.center;
            Frame(center+new Vector3(.32f,.25f,-.36f),center,42);yield return Capture("05_black_rose");
            var player=(Transform)Field(story,"player");var adrian=(Transform)Field(story,"adrian");adrian.gameObject.SetActive(true);
            Move(player,new Vector3(-17.8f,.02f,-4.2f));Move(adrian,new Vector3(-17.1f,.02f,-2.4f));
            player.rotation=Quaternion.identity;adrian.rotation=Quaternion.Euler(0,180,0);
            var presence=player.GetComponentsInChildren<MonoBehaviour>().First(b=>b.GetType().Name=="AcademyCharacterPresence");
            Assert.AreEqual("EVELYN",Value(presence,"CharacterName"));
            var motion=Field(story,"adrianMotion");Call(motion,"Idle");yield return new WaitForSeconds(.25f);
            Call(motion,"Talk");Assert.IsTrue((bool)Value(motion,"IsBlending"),"Talk must blend from Idle");
            yield return new WaitForSeconds(.3f);Assert.IsFalse((bool)Value(motion,"IsBlending"));
            Call(motion,"Walk");yield return new WaitForSeconds(.3f);Call(motion,"Idle");yield return new WaitForSeconds(.3f);
            Call(dialogue,"ShowAdvanceableCinematicLine","ADRIAN","Are you alright?");yield return new WaitForSeconds(.65f);
            Assert.Greater((float)Value(presence,"GazeWeight"),.6f,"Evelyn must attend to the nearby speaker");
            Frame(new Vector3(-15.3f,1.6f,-2.8f),player.position+Vector3.up*1.25f,42);yield return Capture("04_character_attention");
            Call(dialogue,"HideCinematicLine");yield return new WaitForSeconds(.6f);
            Assert.AreEqual(0f,(float)Value(presence,"GazeWeight"),"Gaze must release after dialogue");
            yield return new WaitForSeconds(6.3f);Assert.Greater((int)Value(presence,"BlinkCount"),0,"Imported eyelid shapes must blink");
            var animator=player.GetComponentInChildren<Animator>();var head=animator.GetBoneTransform(HumanBodyBones.Head);
            Assert.That(head.localScale.x,Is.InRange(.90f,.94f));
            foreach(var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b.GetType().Name=="AcademyCharacterPresence"))
            {
                var a=component.GetComponent<Animator>();var h=a.GetBoneTransform(HumanBodyBones.Head);
                Assert.IsFalse(float.IsNaN(h.rotation.x),"Invalid head rotation on "+component.name);
            }
            Debug.Log("[Phase3I] Library and Studio captured; blink, bounded attention and animation crossfades passed.");
        }
        static void Move(Transform t,Vector3 p){var c=t.GetComponent<CharacterController>();if(c!=null)c.enabled=false;t.position=p;if(c!=null)c.enabled=true;Physics.SyncTransforms();}
        static void Frame(Vector3 p,Vector3 target,float fov){Camera.main.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));Camera.main.fieldOfView=fov;}
        static IEnumerator Capture(string name)
        {
            if(Application.isBatchMode)yield break;yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.GetFullPath("Phase3I_Captures/"+name+".jpg"),image.EncodeToJPG(90));
            int invalid=0;foreach(var pixel in image.GetPixels32())
                if(pixel.r<30&&pixel.g>235&&pixel.b>235 || pixel.r>235&&pixel.g<30&&pixel.b>235)invalid++;
            Assert.Less(invalid/(float)(image.width*image.height),.0002f,"Shader compilation or missing-material colors in "+name);
            Object.Destroy(image);
        }
    }
}
