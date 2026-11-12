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
    public sealed class Phase3LAnimationPlayModeTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static MonoBehaviour Get(string type) => Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.GetType().Name == type);
        static object Field(object o, string n) => o.GetType().GetField(n, Flags).GetValue(o);
        static object Value(object o, string n) => o.GetType().GetProperty(n, Flags).GetValue(o);
        static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, Flags).Invoke(o, args);
        static void State(object o, string property, string state)
        { var f = o.GetType().GetField("<" + property + ">k__BackingField", Flags); f.SetValue(o, Enum.Parse(f.FieldType, state)); }
        bool async;
        [UnityTearDown] public IEnumerator Restore() { ShaderUtil.allowAsyncCompilation = async; Time.timeScale = 1; yield return null; }
        [UnityTest, Timeout(150000)]
        public IEnumerator EveryActionCommitsOnceAndCancellationRestoresControl()
        {
            async = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
            var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")); view.Show(); view.Focus();
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale = 1; Application.runInBackground = true; yield return new WaitForSeconds(.8f);
            var game = Get("GameManager"); var story = Get("Phase2AStoryManager"); var hud = Get("GameHUD");
            Call(hud, "ShowStart", false); Call(hud, "ShowGameplay", true);
            var player = (Transform)Field(story, "player"); var performer = Get("AcademyActionPerformance");
            State(game, "State", "Phase2A"); State(story, "Stage", "Errands"); Call(game, "SetPhase2AControl", true);
            var controller = player.GetComponents<MonoBehaviour>().First(b => b.GetType().Name == "ThirdPersonController");
            var relay = player.GetComponents<MonoBehaviour>().First(b => b.GetType().Name == "AcademyPlayerInputRelay"); relay.enabled = false;
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false; player.position = new Vector3(-5,.02f,0); cc.enabled = true;
            Camera.main.GetComponents<Behaviour>().First(b => b.GetType().Name == "CinemachineBrain").enabled = false;
            Camera.main.transform.SetPositionAndRotation(player.position + new Vector3(2,1.8f,2), Quaternion.LookRotation(new Vector3(-2,-.9f,-2)));
            Camera.main.fieldOfView = 45;
            var marker = new GameObject("QA action contact"); marker.transform.position = player.position + Vector3.forward * .6f + Vector3.up * .7f;
            var method = performer.GetType().GetMethod("PlayAction"); var kindType = method.GetParameters()[0].ParameterType;
            foreach (string kind in Enum.GetNames(kindType))
            {
                int impact = 0, complete = 0;
                Assert.IsTrue((bool)method.Invoke(performer, new object[] { Enum.Parse(kindType, kind), marker.transform, (Action)(() => impact++), (Action)(() => complete++) }));
                Assert.IsFalse(controller.enabled, "Input must lock during " + kind);
                Assert.IsFalse((bool)method.Invoke(performer, new object[] { Enum.Parse(kindType, kind), marker.transform, null, null }), "Repeated input must not start a second action");
                yield return new WaitForSeconds(.4f);
                if (kind == "Pickup" || kind == "CleanFloor") yield return Capture(kind);
                for (float t = 0; t < 9 && (bool)Value(performer,"IsBusy"); t += Time.deltaTime) yield return null;
                Assert.IsFalse((bool)Value(performer,"IsBusy")); Assert.AreEqual(1, impact, kind); Assert.AreEqual(1, complete, kind);
                if (kind == "Inspect") Call(game,"SetPhase2AControl",true);
                Assert.IsTrue(controller.enabled, "Input did not recover after " + kind);
            }
            foreach(string kind in Enum.GetNames(kindType))
            {
                int cancelledImpact = 0, cancelledComplete = 0;
                method.Invoke(performer, new object[] { Enum.Parse(kindType,kind),marker.transform,(Action)(()=>cancelledImpact++),(Action)(()=>cancelledComplete++) });
                yield return new WaitForSeconds(.12f); Call(performer,"Cancel"); yield return null;
                Assert.AreEqual(0,cancelledImpact,kind); Assert.AreEqual(0,cancelledComplete,kind); Assert.IsTrue(controller.enabled,"Cancellation stranded input: "+kind);
            }
            var body = player.GetComponentsInChildren<MonoBehaviour>().First(b=>b.GetType().Name=="AcademyBodyPerformance");
            Assert.AreEqual(0,(float)Value(body,"HandIKWeight"));
            var animator = body.GetComponent<Animator>();
            foreach(var bone in new[]{HumanBodyBones.Head,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.RightHand})
            {var t=animator.GetBoneTransform(bone); Assert.IsFalse(float.IsNaN(t.position.x)); Assert.IsFalse(float.IsNaN(t.rotation.x));}
            Object.Destroy(marker);
            // Actual quest item changes at contact, not on the initial key press.
            int count=(int)Value(performer,"Impacts"); Call(story,"Interact",Enum.Parse(story.GetType().Assembly.GetType("VelvetVows.Phase2AAction"),"BooksPickup"));
            Assert.IsFalse((bool)Value(story,"HasBooks"));
            yield return new WaitForSeconds(2f); Assert.IsTrue((bool)Value(story,"HasBooks")); Assert.AreEqual(count+1,Value(performer,"Impacts"));
            Assert.NotNull(Field(performer,"carriedVisual"),"Picked-up books must remain visible in Evelyn's hands");
            Camera.main.transform.SetPositionAndRotation(player.position+new Vector3(1.6f,1.4f,1.6f),Quaternion.LookRotation(new Vector3(-1.6f,-.3f,-1.6f)));
            yield return Capture("CarriedBooks");
            Call(performer,"ClearCarried");
            var shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Vector3 goal = shoulder.position + player.forward * .33f - Vector3.up * .12f;
            Call(body,"SetHands",(Vector3?)goal,(Vector3?)null,1f,0f);
            yield return new WaitForEndOfFrame();
            Assert.Less(Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position,goal),.08f,"Hand IK must reach a reachable contact point");
            Call(body,"ClearHands");
            controller.GetType().GetField("Grounded").SetValue(controller,false);
            controller.enabled=false; yield return new WaitForSeconds(.2f);
            Assert.AreEqual(0f,(float)Value(body,"FootIKWeight"),"Feet must release in the air");
            controller.GetType().GetField("Grounded").SetValue(controller,true); controller.enabled=true;
            Debug.Log("[Phase3L] Eight actions, single impact, cancellation, input recovery, finite humanoid IK and delayed real item pickup passed.");
        }
        [UnityTest, Timeout(60000)]
        public IEnumerator FootGroundingFollowsASmallStepAndReleasesInAir()
        {
            async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale=1;yield return new WaitForSeconds(.8f);
            var story=Get("Phase2AStoryManager");var player=(Transform)Field(story,"player");
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.position=new Vector3(-5,.02f,0);
            var body=player.GetComponentsInChildren<MonoBehaviour>().First(b=>b.GetType().Name=="AcademyBodyPerformance");
            var motor=player.GetComponents<MonoBehaviour>().First(b=>b.GetType().Name=="ThirdPersonController");motor.enabled=false;
            motor.GetType().GetField("Grounded").SetValue(motor,true);
            yield return new WaitForSeconds(.2f);
            yield return new WaitForEndOfFrame();
            var foot=body.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftFoot);float original=foot.position.y;
            var step=GameObject.CreatePrimitive(PrimitiveType.Cube);step.name="QA small step";step.layer=8;
            step.transform.position=new Vector3(foot.position.x,.06f,foot.position.z);step.transform.localScale=new Vector3(.45f,.12f,.45f);Physics.SyncTransforms();
            yield return new WaitForSeconds(.3f);
            yield return new WaitForEndOfFrame();
            Assert.Greater(foot.position.y,original+.035f,"Grounded foot did not follow the raised surface");
            Assert.Greater((float)Value(body,"FootIKWeight"),.9f);
            motor.GetType().GetField("Grounded").SetValue(motor,false);yield return new WaitForSeconds(.2f);
            Assert.AreEqual(0f,(float)Value(body,"FootIKWeight"));Object.Destroy(step);
            Debug.Log("[Phase3L] Raised step surface and airborne IK release passed.");
        }
        [UnityTest, Timeout(60000)]
        public IEnumerator CoupleUsesSynchronizedStepsAndSharedHandContact()
        {
            async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale=1;Application.runInBackground=true;yield return new WaitForSeconds(.8f);
            var ball=Get("Phase2CBallDirector");var hud=Get("GameHUD");Call(hud,"ShowStart",false);Call(hud,"ShowGameplay",false);
            Camera.main.GetComponents<Behaviour>().First(b=>b.GetType().Name=="CinemachineBrain").enabled=false;
            var player=(Transform)Field(ball,"player");var adrian=(Transform)Field(ball,"adrian");adrian.gameObject.SetActive(true);
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;Call(ball,"PlaceCouple",0f);
            var dance=(IEnumerator)Call(ball,"DanceTogether",8f);ball.StartCoroutine(dance);
            yield return new WaitForSeconds(3);
            yield return Capture("Dance");
            var her=player.GetComponentInChildren<Animator>();var his=adrian.GetComponentInChildren<Animator>();
            var herPalm=Vector3.Lerp(her.GetBoneTransform(HumanBodyBones.RightHand).position,her.GetBoneTransform(HumanBodyBones.RightMiddleProximal).position,.5f);
            var hisPalm=Vector3.Lerp(his.GetBoneTransform(HumanBodyBones.LeftHand).position,his.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).position,.5f);
            float gap=Vector3.Distance(herPalm,hisPalm);
            Assert.Less(gap,.06f,"The clasped palms must remain together during the shared step");
            yield return new WaitForSeconds(5.3f);
            Assert.AreEqual(0,(float)Value(player.GetComponentsInChildren<MonoBehaviour>().First(b=>b.GetType().Name=="AcademyBodyPerformance"),"HandIKWeight"));
            Debug.Log("[Phase3L] Synchronized dance contact gap="+gap+"; hand IK released after dance.");
        }
        [UnityTest,Timeout(60000)]
        public IEnumerator MurderCameraStaysInsideStudioNearTheSouthWall()
        {
            async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale=1;yield return new WaitForSeconds(.8f);
            var murder=Get("Phase2BMurderSequence");var hud=Get("GameHUD");Call(hud,"ShowStart",false);Call(hud,"ShowGameplay",true);
            var player=(Transform)Field(murder,"evelyn");var celeste=(Transform)Field(murder,"celeste");celeste.gameObject.SetActive(true);
            foreach(var item in new[]{player,celeste}){var cc=item.GetComponent<CharacterController>();if(cc!=null)cc.enabled=false;}
            player.position=new Vector3(-18,.02f,-6.1f);celeste.position=new Vector3(-17,.02f,-5.5f);Physics.SyncTransforms();
            Call(murder,"Begin");yield return new WaitForSeconds(.6f);
            var actor=(Animator)Field(murder,"evelynAnimator");
            var hand=actor.GetBoneTransform(HumanBodyBones.RightHand);
            var finger=actor.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            var weapon=((GameObject)Field(murder,"attackProp")).transform;
            Assert.AreEqual(hand,weapon.parent,"Weapon must follow the visible hand");
            Assert.Less(Vector3.Distance(weapon.Find("Wooden handle").position,Vector3.Lerp(hand.position,finger.position,.65f)),.015f,"Knife handle is floating outside the palm");
            var target=(player.position+celeste.position)*.5f+Vector3.up;
            var story=Get("Phase2AStoryManager");
            Assert.IsTrue((bool)Call(story,"CameraPathClear",Camera.main.transform.position,target),"Murder camera sees a wall instead of the actors");
            Assert.Greater(Camera.main.transform.position.z,-7.2f);
            yield return Capture("MurderCamera");
            yield return new WaitForSeconds(1.3f);
            yield return Capture("MurderContact");
            yield return new WaitForEndOfFrame();var screenshot=ScreenCapture.CaptureScreenshotAsTexture();
            double total=0,squares=0;int samples=0;
            for(int y=screenshot.height/4;y<screenshot.height*3/4;y+=8)for(int x=screenshot.width/4;x<screenshot.width*3/4;x+=8)
            {var pixel=screenshot.GetPixel(x,y);double light=(pixel.r+pixel.g+pixel.b)/3;total+=light;squares+=light*light;samples++;}
            Object.Destroy(screenshot);double variance=squares/samples-Math.Pow(total/samples,2);
            Assert.Greater(variance,.00015,"The central image is blank or uniformly occluded");
            Debug.Log("[Phase3L] Visible murder camera: central image variance="+variance);
        }
        [UnityTest,Timeout(90000)]
        public IEnumerator SprintAndJumpDriveTheVisibleHumanoidAndRecoverGrounding()
        {
            async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity",new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale=1;Application.runInBackground=true;yield return new WaitForSeconds(.8f);
            var game=Get("GameManager");var story=Get("Phase2AStoryManager");var hud=Get("GameHUD");
            Call(hud,"ShowStart",false);Call(hud,"ShowGameplay",true);State(game,"State","Phase2A");State(story,"Stage","Errands");Call(game,"SetPhase2AControl",true);
            var player=(Transform)Field(story,"player");var cc=player.GetComponent<CharacterController>();
            cc.enabled=false;player.position=new Vector3(-5,.04f,-5.5f);cc.enabled=true;Physics.SyncTransforms();
            player.GetComponents<MonoBehaviour>().First(b=>b.GetType().Name=="AcademyPlayerInputRelay").enabled=false;
            var input=player.GetComponents<MonoBehaviour>().First(b=>b.GetType().Name=="StarterAssetsInputs");
            var motor=player.GetComponents<MonoBehaviour>().First(b=>b.GetType().Name=="ThirdPersonController");
            var animator=player.GetComponentInChildren<Animator>();
            var body=animator.GetComponents<MonoBehaviour>().First(b=>b.GetType().Name=="AcademyBodyPerformance");
            Call(input,"SprintInput",true);
            var route=(IEnumerator)typeof(Phase2APlayModeTests).GetMethod("WalkRoute",Flags).Invoke(null,new object[]{player,new Vector3(1,0,-5.5f),(Func<bool>)(()=>false)});
            bool done=false;float topSpeed=0,animationSpeed=0;
            IEnumerator Walk(){yield return route;done=true;}
            game.StartCoroutine(Walk());
            for(float t=0;t<15&&!done;t+=Time.deltaTime)
            {topSpeed=Mathf.Max(topSpeed,new Vector2(cc.velocity.x,cc.velocity.z).magnitude);animationSpeed=Mathf.Max(animationSpeed,animator.GetFloat("Speed"));yield return null;}
            Assert.IsTrue(done);Assert.Greater(topSpeed,4f);Assert.Greater(animationSpeed,4f);
            Call(input,"SprintInput",false);Call(input,"MoveInput",Vector2.zero);yield return new WaitForSeconds(.8f);
            float floor=player.position.y,maxHeight=floor,minIK=1;bool airborne=false,jumpPose=false;
            Call(input,"JumpInput",true);
            for(float t=0;t<1.5f;t+=Time.deltaTime)
            {
                Call(input,"JumpInput",t<.2f);
                yield return new WaitForEndOfFrame();
                maxHeight=Mathf.Max(maxHeight,player.position.y);airborne|=!(bool)motor.GetType().GetField("Grounded").GetValue(motor);
                jumpPose|=animator.GetBool("Jump");minIK=Mathf.Min(minIK,(float)Value(body,"FootIKWeight"));yield return null;
            }
            Debug.Log("[Phase3L locomotion diagnostic] airborne="+airborne+" jumpPose="+jumpPose+" rise="+(maxHeight-floor)+" minIK="+minIK+" motor="+motor.enabled);
            Assert.IsTrue(airborne,"Jump never left the ground");Assert.IsTrue(jumpPose,"Visible animator did not enter jump pose");Assert.Greater(maxHeight-floor,.35f);Assert.Less(minIK,.1f);
            yield return new WaitForSeconds(.5f);Assert.IsTrue((bool)motor.GetType().GetField("Grounded").GetValue(motor));
            Assert.Greater((float)Value(body,"FootIKWeight"),.9f);
            Debug.Log("[Phase3L] Physical sprint="+topSpeed+" m/s, jump height="+(maxHeight-floor)+" m; visible animator and ground IK recovered.");
        }
        [UnityTest, Timeout(90000)]
        public IEnumerator BodyHidingKeepsBothActorsVisibleAndReleasesControl()
        {
            async = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
            var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")); view.Show(); view.Focus();
            yield return (IEnumerator)typeof(Phase2BPlayModeTests).GetMethod("StartCleanup", Flags).Invoke(null, null);
            var cleanup = Get("Phase2BCleanupManager"); var player = (Transform)Field(cleanup, "player");
            var corpse = (Transform)Field(cleanup, "celeste"); var start = corpse.position;
            var action = Get("AcademyActionPerformance");
            var kind = cleanup.GetType().Assembly.GetType("VelvetVows.Phase2BAction");
            Call(cleanup, "Interact", Enum.Parse(kind, "Body"));
            yield return new WaitForSeconds(2f);
            yield return new WaitForEndOfFrame();
            Assert.IsTrue((bool)Value(action, "IsBusy"));
            Assert.Greater(Vector3.Distance(start, corpse.position), .05f, "The body should visibly move towards the wardrobe");
            var actor = player.GetComponentInChildren<Animator>(); var dead = corpse.GetComponentInChildren<Animator>();
            float ArmDistance(Vector3 hand, HumanBodyBones upper, HumanBodyBones lower)
            {
                var a = dead.GetBoneTransform(upper).position; var b = dead.GetBoneTransform(lower).position;
                var axis = b - a; float t = Mathf.Clamp01(Vector3.Dot(hand - a, axis) / Mathf.Max(.00001f, axis.sqrMagnitude));
                return Vector3.Distance(hand, a + axis * t);
            }
            float DistanceToEitherArm(Vector3 hand) => Mathf.Min(ArmDistance(hand, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm), ArmDistance(hand, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm));
            var rightDistance = DistanceToEitherArm(actor.GetBoneTransform(HumanBodyBones.RightHand).position);
            var leftDistance = DistanceToEitherArm(actor.GetBoneTransform(HumanBodyBones.LeftHand).position);
            var performer = player.GetComponentsInChildren<MonoBehaviour>().First(b => b.GetType().Name == "AcademyBodyPerformance");
            Debug.Log($"[Phase3M] Body contact: player={player.position}, body={corpse.position}, chest={dead.GetBoneTransform(HumanBodyBones.Chest).position}, deadarms={dead.GetBoneTransform(HumanBodyBones.RightUpperArm).position}/{dead.GetBoneTransform(HumanBodyBones.LeftUpperArm).position}, elbows={dead.GetBoneTransform(HumanBodyBones.RightLowerArm).position}/{dead.GetBoneTransform(HumanBodyBones.LeftLowerArm).position}, upperarms={actor.GetBoneTransform(HumanBodyBones.RightUpperArm).position}/{actor.GetBoneTransform(HumanBodyBones.LeftUpperArm).position}, hands={actor.GetBoneTransform(HumanBodyBones.RightHand).position}/{actor.GetBoneTransform(HumanBodyBones.LeftHand).position}, targets={Field(performer,"rightTarget")}/{Field(performer,"leftTarget")}, weight={Value(performer,"HandIKWeight")}, bend={Field(performer,"bend")}, right={rightDistance:F3}, left={leftDistance:F3}");
            Assert.Less(Mathf.Max(rightDistance, leftDistance), .12f, "Hands must stay close to the body while it moves");
            var story = Get("Phase2AStoryManager");
            Assert.IsTrue((bool)Call(story, "CameraPathClear", Camera.main.transform.position, player.position + Vector3.up * .85f), "The player is obscured during body hiding");
            Assert.IsTrue((bool)Call(story, "CameraPathClear", Camera.main.transform.position, corpse.position + Vector3.up * .4f), "The body is obscured during body hiding");
            yield return Capture("BodyHiding");
            float end = Time.time + 6f;
            float worstGrip = 0; int gripSamples = 0;
            var visibleBody = ((GameObject)Field(Get("Phase2BMurderSequence"), "bodyInteraction"));
            while (!(bool)Value(cleanup, "BodyHidden") && Time.time < end)
            {
                yield return new WaitForEndOfFrame();
                if (visibleBody.activeInHierarchy && (float)Value(performer, "HandIKWeight") > .9f)
                {
                    worstGrip = Mathf.Max(worstGrip, DistanceToEitherArm(actor.GetBoneTransform(HumanBodyBones.RightHand).position),
                        DistanceToEitherArm(actor.GetBoneTransform(HumanBodyBones.LeftHand).position));
                    gripSamples++;
                }
            }
            Assert.GreaterOrEqual(gripSamples, 2, "Expected multiple visible grip samples");
            Assert.Less(worstGrip, .12f, "Hands lost contact while the body was moving");
            Debug.Log("[Phase3N] Body grip samples=" + gripSamples + " worst gap=" + worstGrip);
            Assert.IsTrue((bool)Value(cleanup, "BodyHidden"));
            var controller = player.GetComponents<MonoBehaviour>().First(b => b.GetType().Name == "ThirdPersonController");
            Assert.IsTrue(controller.enabled, "Player control must return after hiding the body");
        }
        [UnityTest, Timeout(60000)]
        public IEnumerator SuspicionIdentifiesTheActiveDetectionArea()
        {
            async = ShaderUtil.allowAsyncCompilation;
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var game = Get("GameManager"); var system = Field(game, "suspicion"); var hud = Get("GameHUD");
            var label = (UnityEngine.UI.Text)Field(hud, "suspicionText");
            var zone = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(b => b.GetType().Name == "DetectionZone" && b.GetComponentInParent<MonoBehaviour>() != null && b.name.Contains("Restricted"));
            Call(system, "Begin"); Call(system, "Enter", zone); Call(system, "Adjust", 1f);
            StringAssert.Contains("RESTRICTED AREA", label.text);
            Call(system, "Exit", zone); Call(system, "Adjust", 0f);
            Assert.IsFalse(label.text.Contains("RESTRICTED AREA"));
            Call(system, "Stop");
        }
        [UnityTest, Timeout(150000)]
        public IEnumerator CameraDoorwayRouteDiagnostic()
        {
            async = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
            var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")); view.Show(); view.Focus();
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSeconds(.8f);
            var game = Get("GameManager"); var story = Get("Phase2AStoryManager"); var hud = Get("GameHUD");
            State(game, "State", "Phase2A"); State(story, "Stage", "Errands"); Call(hud, "ShowStart", false); Call(hud, "ShowGameplay", true);
            var player = (Transform)Field(story, "player"); var cc = player.GetComponent<CharacterController>();
            cc.enabled = false; player.position = new Vector3(-17, .02f, -1); cc.enabled = true; Physics.SyncTransforms();
            Call(game, "SetPhase2AControl", true); Get("Phase2BPrefectInvestigation").enabled = false;
            yield return new WaitForSeconds(.5f);
            bool done = false;
            IEnumerator Drive()
            {
                yield return (IEnumerator)typeof(Phase2APlayModeTests).GetMethod("WalkRoute", Flags).Invoke(null, new object[] { player, new Vector3(-9.4f,0,8.6f), (Func<bool>)(() => false) });
                yield return (IEnumerator)typeof(Phase2APlayModeTests).GetMethod("WalkPlayerTo", Flags).Invoke(null, new object[] { player, new Vector3(-10.05f,0,8.6f), 25f, null, .45f });
                yield return (IEnumerator)typeof(Phase2APlayModeTests).GetMethod("WalkPlayerTo", Flags).Invoke(null, new object[] { player, new Vector3(-14.6f,0,13.15f), 25f, null, .65f });
                done = true;
            }
            game.StartCoroutine(Drive());
            float closeTime = 0, maximum = 0; string obstacle = ""; bool captured = false;
            int solid = LayerMask.GetMask("Academy Solid");
            float deadline = Time.time + 90;
            while (!done && Time.time < deadline)
            {
                var camera = Camera.main;
                bool close = Physics.Raycast(camera.transform.position, camera.transform.forward, out var hit, .65f, solid, QueryTriggerInteraction.Ignore);
                closeTime = close ? closeTime + Time.deltaTime : 0;
                if (closeTime > maximum) { maximum = closeTime; obstacle = close ? hit.collider.name : obstacle; }
                if (closeTime > .4f && !captured) { captured = true; yield return Capture("DoorwayBefore"); }
                yield return null;
            }
            Debug.Log("[Phase3N] Doorway route: complete=" + done + " longest near-wall view=" + maximum + " obstacle=" + obstacle);
            Assert.IsTrue(done, "Doorway route failed to complete");
        }
        [UnityTest, Timeout(60000)]
        public IEnumerator CameraRecoversFromPersistentCloseWallAndRespectsCinematics()
        {
            async = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
            EditorSceneManager.LoadSceneInPlayMode("Assets/VelvetVows/Scenes/VelvetVows_Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSeconds(.8f);
            var story = Get("Phase2AStoryManager"); var player = (Transform)Field(story, "player");
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false; player.position = new Vector3(-5,.02f,0); controller.enabled = true;
            var camera = Camera.main; var brain = camera.GetComponents<Behaviour>().First(b => b.GetType().Name == "CinemachineBrain"); brain.enabled = true;
            var comfort = camera.GetComponents<MonoBehaviour>().First(b => b.GetType().Name == "AcademyCameraComfort");
            var head = player.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "QA close camera wall";
            wall.transform.SetParent(GameObject.Find("ENVIRONMENT").transform, true); wall.layer = LayerMask.NameToLayer("Academy Solid");
            wall.transform.position = head.position + Vector3.back * .95f; wall.transform.localScale = new Vector3(1.8f, 2f, .12f); Physics.SyncTransforms();
            int before = (int)Value(comfort, "WallRecoveryCount");
            for (int i = 0; i < 180 && (int)Value(comfort, "WallRecoveryCount") == before; i++)
            {
                camera.transform.SetPositionAndRotation(head.position + Vector3.back * .55f, Quaternion.LookRotation(Vector3.back));
                Call(comfort, "LateUpdate");
            }
            Assert.Greater((int)Value(comfort, "WallRecoveryCount"), before, "Camera remained aimed at the nearby wall");
            Assert.IsTrue((bool)Call(story, "CameraPathClear", camera.transform.position, head.position - Vector3.up * .18f));
            // The recorded doorway issue also occurs with a farther wall and the player outside the frame.
            brain.enabled = false; Call(comfort, "LateUpdate"); brain.enabled = true;
            controller.Move(Vector3.right * .08f);
            wall.transform.position = head.position + Vector3.back * 3.25f; Physics.SyncTransforms();
            before = (int)Value(comfort, "WallRecoveryCount");
            for (int i = 0; i < 180 && (int)Value(comfort, "WallRecoveryCount") == before; i++)
            {
                camera.transform.SetPositionAndRotation(head.position + Vector3.back * 1.8f, Quaternion.LookRotation(Vector3.back));
                Call(comfort, "LateUpdate");
            }
            Assert.Greater((int)Value(comfort, "WallRecoveryCount"), before, "Camera followed the wall while the moving player was offscreen");
            brain.enabled = false;
            var manual = head.position + Vector3.back * .55f;
            camera.transform.SetPositionAndRotation(manual, Quaternion.LookRotation(Vector3.back));
            for (int i = 0; i < 25; i++) { Call(comfort, "LateUpdate"); yield return null; }
            Assert.Less(Vector3.Distance(manual, camera.transform.position), .01f, "Cinematic camera was moved by gameplay recovery");
            Object.Destroy(wall);
        }
        static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame(); Directory.CreateDirectory("Phase3L_Captures");
            var image=ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes("Phase3L_Captures/"+name+".jpg",image.EncodeToJPG(90)); Object.Destroy(image);
        }
    }
}
