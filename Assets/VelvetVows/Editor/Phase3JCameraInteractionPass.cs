using System;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
using Object=UnityEngine.Object;
namespace VelvetVows.Editor
{
    public static class Phase3JCameraInteractionPass
    {
        const string Scene="Assets/VelvetVows/Scenes/VelvetVows_Main.unity";
        [MenuItem("Velvet Vows/Art direction/Stage 2 - camera and interactions")]
        public static void Apply()
        {
            const string backup="Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase3J.unity";
            if(!File.Exists(backup))File.Copy(Scene,backup);
            AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene(Scene);
            var root=scene.GetRootGameObjects().First(g=>g.name=="Velvet Vows Main").transform;
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers=tags.FindProperty("layers");int layer=LayerMask.NameToLayer("Academy Solid");
            if(layer<0)
            {
                layer=Enumerable.Range(8,24).First(i=>string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue));
                layers.GetArrayElementAtIndex(layer).stringValue="Academy Solid";tags.ApplyModifiedPropertiesWithoutUndo();
            }
            int mask=1<<layer,solids=0;
            var story=Object.FindFirstObjectByType<Phase2AStoryManager>();var cleanup=Object.FindFirstObjectByType<Phase2BCleanupManager>();
            foreach(var c in root.GetComponentsInChildren<Collider>(true))
            {
                if(c.isTrigger)continue;string path=PathOf(c.transform);
                if(path.Contains("/ENVIRONMENT/")||path.Contains("/GOTHIC_ACADEMY/")||c.GetComponentInParent<Phase2AStudioDoor>()!=null||
                    path.Contains("/PHASE 2B CLEANUP/"))
                {var go=new SerializedObject(c.gameObject);go.FindProperty("m_Layer").intValue=layer;go.ApplyModifiedPropertiesWithoutUndo();solids++;}
            }
            story.studioDoor.gameObject.layer=layer;
            foreach(var player in root.GetComponentsInChildren<ThirdPersonController>(true))
            {var movement=new SerializedObject(player);movement.FindProperty("GroundLayers").intValue|=mask;movement.ApplyModifiedPropertiesWithoutUndo();}
            var follow=Object.FindFirstObjectByType<CinemachineThirdPersonFollow>();
            follow.CameraDistance=3.0f;follow.ShoulderOffset=new Vector3(.34f,.15f,0);follow.VerticalArmLength=.22f;follow.CameraSide=1;
            follow.Damping=new Vector3(.08f,.12f,.10f);
            follow.AvoidObstacles=new CinemachineThirdPersonFollow.ObstacleSettings{
                Enabled=true,CollisionFilter=mask,IgnoreTag="Player",CameraRadius=.29f,DampingIntoCollision=0,DampingFromCollision=.35f};
            var decollider=follow.GetComponent<CinemachineDecollider>()??follow.gameObject.AddComponent<CinemachineDecollider>();
            decollider.CameraRadius=.27f;decollider.TerrainResolution.Enabled=false;
            decollider.Decollision=new CinemachineDecollider.DecollisionSettings{
                Enabled=true,ObstacleLayers=mask,Damping=0,SmoothingTime=0,
                UseFollowTarget=new CinemachineDecollider.DecollisionSettings.FollowTargetSettings{Enabled=true,YOffset=0}};
            var camera=Camera.main;camera.nearClipPlane=.06f;
            var vcam=follow.GetComponent<CinemachineCamera>();var lens=vcam.Lens;lens.NearClipPlane=.06f;lens.FieldOfView=55;vcam.Lens=lens;
            var comfort=camera.GetComponent<AcademyCameraComfort>()??camera.gameObject.AddComponent<AcademyCameraComfort>();
            comfort.Configure(story.player.GetComponentInChildren<Animator>());
            var interactor=story.player.GetComponent<PlayerInteractor>();var settings=new SerializedObject(interactor);
            settings.FindProperty("interactionLayers").intValue=1|mask;settings.FindProperty("obstructionLayers").intValue=mask;settings.ApplyModifiedPropertiesWithoutUndo();
            foreach(var item in root.GetComponentsInChildren<Phase2AInteractable>(true))
            {
                Transform visual=item.transform;bool bounds=false;Vector3 point=Vector3.up*1.2f;
                switch(item.Action)
                {
                    case Phase2AAction.BooksPickup:visual=story.libraryBooks.transform;bounds=true;break;
                    case Phase2AAction.ArtPickup:visual=story.artMaterials.transform;bounds=true;break;
                    case Phase2AAction.LostPickup:visual=story.lostPendant.transform;bounds=true;break;
                    case Phase2AAction.RibbonPickup:visual=story.blackRibbon.transform;bounds=true;break;
                    case Phase2AAction.Sketchbook:visual=story.sketchbook.transform;bounds=true;break;
                    case Phase2AAction.StudioDoor:visual=story.studioDoor.transform;point=visual.InverseTransformPoint(new Vector3(-12,1.15f,-3));break;
                }
                Anchor(item.gameObject,visual,point,bounds);
            }
            foreach(var item in root.GetComponentsInChildren<Phase2BInteractable>(true))
            {
                var action=(Phase2BAction)new SerializedObject(item).FindProperty("action").enumValueIndex;
                Transform visual=item.transform;bool bounds=true;Vector3 point=Vector3.zero;
                switch(action)
                {
                    case Phase2BAction.Body:visual=cleanup.celeste;break;
                    case Phase2BAction.Cloth:visual=cleanup.worldCloth.transform;break;
                    case Phase2BAction.Blood:visual=cleanup.blood.transform;break;
                    case Phase2BAction.Disposal:visual=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Studio disposal bin");break;
                    case Phase2BAction.Clothing:visual=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Studio wash basin");break;
                    case Phase2BAction.Door:visual=story.studioDoor.transform;bounds=false;point=visual.InverseTransformPoint(new Vector3(-12,1.15f,-3));break;
                }
                Anchor(item.gameObject,visual,point,bounds);
            }
            SetupUI(root,interactor);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
            Debug.Log("[Phase3J] Saved camera collision and interaction presentation; solid colliders="+solids+"; layer="+layer);
        }
        static void Anchor(GameObject go,Transform visual,Vector3 point,bool bounds)
        {(go.GetComponent<InteractionFocusAnchor>()??go.AddComponent<InteractionFocusAnchor>()).Configure(visual,point,bounds);}
        static void SetupUI(Transform root,PlayerInteractor interactor)
        {
            var hud=Object.FindFirstObjectByType<GameHUD>();var settings=new SerializedObject(hud);
            var text=(Text)settings.FindProperty("interactionText").objectReferenceValue;var canvas=text.GetComponentInParent<Canvas>();
            var parent=text.transform.parent;
            if(parent.name=="Interaction prompt card")parent=parent.parent;
            var old=parent.Find("Interaction prompt card");if(old!=null){text.transform.SetParent(parent,true);Object.DestroyImmediate(old.gameObject);}
            var card=UI(parent,"Interaction prompt card",new Vector2(820,66),new Color(.025f,.018f,.03f,.88f));
            card.anchorMin=card.anchorMax=new Vector2(.5f,0);card.anchoredPosition=new Vector2(0,105);
            text.transform.SetParent(card,false);var rt=(RectTransform)text.transform;rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(790,58);
            text.alignment=TextAnchor.MiddleCenter;text.fontSize=29;text.raycastTarget=false;
            var line=UI(card,"Brass prompt rule",new Vector2(760,2),new Color(.55f,.40f,.22f,.65f));line.anchoredPosition=new Vector2(0,-30);
            hud.ConfigureInteractionBackdrop(card.gameObject);card.gameObject.SetActive(false);
            var oldMarker=parent.Find("Interaction focus marker");if(oldMarker!=null)Object.DestroyImmediate(oldMarker.gameObject);
            var marker=new GameObject("Interaction focus marker",typeof(RectTransform)).GetComponent<RectTransform>();marker.SetParent(parent,false);marker.sizeDelta=new Vector2(26,26);
            foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-1f,1f})
            {
                var horizontal=UI(marker,"Focus bracket",new Vector2(7,2),new Color(.89f,.72f,.40f,.95f));horizontal.anchoredPosition=new Vector2(x*9,y*12);
                var vertical=UI(marker,"Focus bracket",new Vector2(2,7),new Color(.89f,.72f,.40f,.95f));vertical.anchoredPosition=new Vector2(x*12,y*9);
            }
            var indicator=parent.GetComponent<InteractionFocusIndicator>()??parent.gameObject.AddComponent<InteractionFocusIndicator>();indicator.Configure(interactor,marker,canvas);marker.gameObject.SetActive(false);
        }
        static RectTransform UI(Transform parent,string name,Vector2 size,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Image));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.sizeDelta=size;go.GetComponent<Image>().color=color;go.GetComponent<Image>().raycastTarget=false;return rt;}
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    }
}
