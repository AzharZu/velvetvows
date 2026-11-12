using UnityEngine;
namespace VelvetVows
{
    public sealed class InteractionFocusAnchor : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Vector3 localPoint;
        [SerializeField] private bool centerOnBounds;
        private Renderer[] renderers;
        public void Configure(Transform target, Vector3 point, bool useBounds)
        { visual=target;localPoint=point;centerOnBounds=useBounds;renderers=null; }
        public Vector3 Point
        {
            get
            {
                var target=visual!=null?visual:transform;
                if(!centerOnBounds)return target.TransformPoint(localPoint);
                if(renderers==null)renderers=target.GetComponentsInChildren<Renderer>(true);
                Bounds bounds=default;bool found=false;
                foreach(var r in renderers)
                {
                    if(r==null||!r.enabled||!r.gameObject.activeInHierarchy)continue;
                    if(!found){bounds=r.bounds;found=true;}else bounds.Encapsulate(r.bounds);
                }
                return found?bounds.center:target.TransformPoint(localPoint);
            }
        }
        public bool Owns(Transform item)=>visual!=null&&(item==visual||item.IsChildOf(visual));
    }
}
