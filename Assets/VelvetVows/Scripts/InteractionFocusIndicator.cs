using UnityEngine;
using UnityEngine.UI;
namespace VelvetVows
{
    [DefaultExecutionOrder(21000)]
    public sealed class InteractionFocusIndicator : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private RectTransform marker;
        [SerializeField] private Canvas canvas;
        public void Configure(PlayerInteractor owner,RectTransform indicator,Canvas parent)
        {interactor=owner;marker=indicator;canvas=parent;}
        private void LateUpdate()
        {
            var camera=Camera.main;
            bool visible=interactor!=null&&interactor.FocusedTarget!=null&&camera!=null&&!DialogueManager.IsOpen;
            Vector3 point=visible?camera.WorldToViewportPoint(interactor.FocusPoint):Vector3.zero;
            visible=visible&&point.z>camera.nearClipPlane&&point.x>.02f&&point.x<.98f&&point.y>.04f&&point.y<.96f;
            marker.gameObject.SetActive(visible);
            if(!visible)return;
            var screen=camera.ViewportToScreenPoint(point);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,
                canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local);
            marker.anchoredPosition=local;
        }
        private void OnDisable(){if(marker!=null)marker.gameObject.SetActive(false);}
    }
}
