using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace VelvetVows.Tests
{
    public sealed class Phase3JCameraInteractionPlayModeTests
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        bool previousAsync;
        static MonoBehaviour Get(string name)=>Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(b=>b.GetType().Name==name);
        static object Field(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
        static object Value(object o,string n)=>o.GetType().GetProperty(n,Flags).GetValue(o);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        static void SetProperty(object o,string n,string value)
        {var f=o.GetType().GetField("<"+n+">k__BackingField",Flags);f.SetValue(o,Enum.Parse(f.FieldType,value));}
        [UnityTearDown]public IEnumerator Restore(){ShaderUtil.allowAsyncCompilation=previousAsync;Time.timeScale=1;yield return null;}
        [UnityTest,Timeout(180000)]
        public IEnumerator CameraOrbitsAndPhysicalInteractionsRespectWallsAndDoors()
        {
            previousAsync=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale=1;Application.runInBackground=true;yield return new WaitForSeconds(.8f);
            var game=Get("GameManager");var story=Get("Phase2AStoryManager");var hud=Get("GameHUD");
            var player=(Transform)Field(story,"player");var controller=player.GetComponents<MonoBehaviour>().First(b=>b.GetType().Name=="ThirdPersonController");
            var relay=player.GetComponents<MonoBehaviour>().First(b=>b.GetType().Name=="AcademyPlayerInputRelay");
            var interactor=player.GetComponents<MonoBehaviour>().First(b=>b.GetType().Name=="PlayerInteractor");
            SetProperty(game,"State","Phase2A");SetProperty(story,"Stage","Errands");Call(game,"SetPhase2AControl",true);relay.enabled=false;
            Call(hud,"ShowStart",false);Call(hud,"ShowGameplay",true);
            var brain=Camera.main.GetComponents<Behaviour>().First(b=>b.GetType().Name=="CinemachineBrain");brain.enabled=true;
            var solid=LayerMask.GetMask("Academy Solid");Assert.AreNotEqual(0,solid);
            bool capturedClose=false;
            foreach(var position in new[]{new Vector3(-18,.02f,8.8f),new Vector3(-5,.02f,5),new Vector3(-10.5f,.02f,6.2f)})
            {
                Move(player,position);
                foreach(float pitch in new[]{-30f,12f,70f})
                foreach(float yaw in new[]{0f,45f,90f,135f,180f,225f,270f,315f})
                {
                    Aim(controller,yaw,pitch);yield return new WaitForSeconds(.22f);
                    var collisions=Physics.OverlapSphere(Camera.main.transform.position,.23f,solid,QueryTriggerInteraction.Ignore);
                    Assert.IsEmpty(collisions,"Camera penetrated architecture at "+position+" yaw="+yaw+": "+string.Join(",",collisions.Select(c=>c.name)));
                    Assert.IsFalse(Physics.Linecast(player.position+Vector3.up*1.3f,Camera.main.transform.position,solid,QueryTriggerInteraction.Ignore),"Architecture lies between the camera and Evelyn");
                    Assert.IsTrue((bool)Field(controller,"Grounded"),"Camera collision layers broke ground detection");
                    var viewport=Camera.main.WorldToViewportPoint(player.position+Vector3.up*1.3f);
                    // At the close first-person limit the head can fall behind the near plane.
                    // In that case all avatar renderers must be hidden, rather than obstructing the view.
                    bool hidden=(bool)Value(Get("AcademyCameraComfort"),"AvatarHidden");
                    Assert.IsTrue(viewport.z>.05f||hidden,"Visible avatar behind the camera at "+position+" yaw="+yaw+" pitch="+pitch);
                    if(viewport.z<=.05f&&!capturedClose){capturedClose=true;yield return Capture("05_close_camera_vertical");}
                }
            }
            Move(player,new Vector3(-18,.02f,8.8f));Aim(controller,0,12);yield return new WaitForSeconds(.6f);yield return Capture("01_camera_library_wall");
            brain.enabled=false;
            var target=player.position+Vector3.up*1.35f;var preferred=target+new Vector3(0,.25f,1.8f);
            var obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);obstruction.name="QA portrait obstacle";
            obstruction.transform.SetParent(GameObject.Find("ENVIRONMENT").transform,true);obstruction.transform.position=preferred;obstruction.transform.localScale=new Vector3(.6f,2,.6f);obstruction.layer=LayerMask.NameToLayer("Academy Solid");Physics.SyncTransforms();
            Call(story,"FrameConversation",preferred,target,42f);
            Assert.IsTrue((bool)Call(story,"CameraPathClear",Camera.main.transform.position,target),"Dialogue framing did not avoid a blocked preferred position");
            yield return Capture("04_dialogue_camera_fallback");Object.Destroy(obstruction);yield return null;brain.enabled=true;
            var comfort=Camera.main.GetComponents<MonoBehaviour>().First(b=>b.GetType().Name=="AcademyCameraComfort");
            var head=player.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
            Camera.main.transform.position=head.position+Vector3.forward*.25f;Call(comfort,"LateUpdate");Assert.IsTrue((bool)Value(comfort,"AvatarHidden"));
            brain.enabled=false;Call(comfort,"LateUpdate");Assert.IsFalse((bool)Value(comfort,"AvatarHidden"),"A manual reaction portrait must keep Evelyn visible");brain.enabled=true;

            // Use the real quest prop and real E input; a temporary solid wall must prevent pickup.
            var books=(GameObject)Field(story,"libraryBooks");var bookAction=books.GetComponents<MonoBehaviour>().First(b=>b.GetType().Name=="Phase2AInteractable");
            Move(player,books.transform.position+Vector3.right*1.35f);Aim(controller,270,12);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA interaction wall";wall.layer=LayerMask.NameToLayer("Academy Solid");
            wall.transform.position=(player.position+books.transform.position)*.5f+Vector3.up;wall.transform.localScale=new Vector3(.12f,2,1.4f);Physics.SyncTransforms();
            yield return new WaitForSeconds(.2f);yield return PressE(interactor);
            Assert.IsFalse((bool)Value(story,"HasBooks"),"Books were picked up through a wall");
            Object.Destroy(wall);yield return null;Physics.SyncTransforms();yield return new WaitForSeconds(.3f);
            Assert.AreSame(bookAction,Value(interactor,"FocusedTarget"),"Visible books did not receive focus");
            yield return Capture("02_books_focus");
            int before=(int)Value(interactor,"InteractionCount");yield return PressE(interactor,true);
            Assert.IsTrue((bool)Value(story,"HasBooks"));yield return new WaitForSeconds(.25f);Call(interactor,"Update");
            Assert.AreEqual(before+1,(int)Value(interactor,"InteractionCount"),"Held E repeated the pickup");yield return ReleaseE();
            Assert.IsFalse(books.activeSelf);Assert.IsNull(Value(interactor,"FocusedTarget"));

            // Move a real pickup behind the actual Studio door, then open that door.
            var art=(GameObject)Field(story,"artMaterials");art.transform.position=new Vector3(-13.15f,.02f,-3);
            var door=Field(story,"studioDoor");yield return (IEnumerator)Call(door,"CloseVisible");
            Move(player,new Vector3(-10.95f,.02f,-3));Aim(controller,270,12);Physics.SyncTransforms();yield return new WaitForSeconds(.25f);
            yield return PressE(interactor);Assert.IsFalse((bool)Value(story,"HasArtMaterials"),"Art materials were taken through the closed Studio door");
            yield return (IEnumerator)Call(door,"OpenVisible");Physics.SyncTransforms();yield return new WaitForSeconds(.3f);
            Assert.NotNull(Value(interactor,"FocusedTarget"),"The open doorway still blocks interaction");
            Call(hud,"HideClue");yield return Capture("03_open_studio_door");yield return PressE(interactor);Assert.IsTrue((bool)Value(story,"HasArtMaterials"));
            var dialogue=Get("DialogueManager");Call(dialogue,"ShowAdvanceableCinematicLine","EVELYN · THOUGHT","I should keep moving.");yield return null;
            Assert.IsNull(Value(interactor,"FocusedTarget"));var marker=GameObject.Find("Interaction focus marker");Assert.IsNull(marker,"Focus marker must hide during dialogue");
            Call(dialogue,"HideCinematicLine");
            Debug.Log("[Phase3J] 72 camera angles, close-camera avatar visibility, physical E pickup, held-key suppression and closed/open Studio door passed.");
        }
        static void Aim(MonoBehaviour controller,float yaw,float pitch)
        {controller.GetType().GetField("_cinemachineTargetYaw",Flags).SetValue(controller,yaw);controller.GetType().GetField("_cinemachineTargetPitch",Flags).SetValue(controller,pitch);}
        static void Move(Transform t,Vector3 p){var c=t.GetComponent<CharacterController>();c.enabled=false;t.position=p;c.enabled=true;Physics.SyncTransforms();}
        static IEnumerator ReleaseE(){var k=Keyboard.current??InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(k,new KeyboardState());InputSystem.Update();yield return null;}
        static IEnumerator PressE(MonoBehaviour interactor,bool hold=false)
        {yield return ReleaseE();var k=Keyboard.current;InputSystem.QueueStateEvent(k,new KeyboardState(Key.E));InputSystem.Update();Call(interactor,"Update");yield return null;if(!hold)yield return ReleaseE();
         foreach(var action in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b.GetType().Name=="AcademyActionPerformance"))
            for(float t=0;t<8&&(bool)Value(action,"IsBusy");t+=Time.unscaledDeltaTime)yield return null;}
        static IEnumerator Capture(string name)
        {if(Application.isBatchMode)yield break;yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.GetFullPath("Phase3J_Captures/"+name+".jpg"),image.EncodeToJPG(90));Object.Destroy(image);}
    }
}
