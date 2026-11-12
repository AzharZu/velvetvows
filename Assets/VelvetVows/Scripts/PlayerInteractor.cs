using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VelvetVows
{
    public interface IInteractable
    {
        bool CanInteract(GameManager game);
        string Prompt { get; }
        void Interact(GameManager game);
    }

    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Min(.5f)] public float interactionDistance = 2.2f;
        [SerializeField] private LayerMask interactionLayers = 1;
        [SerializeField] private LayerMask obstructionLayers = Physics.DefaultRaycastLayers;
        private readonly Collider[] nearby = new Collider[128];
        private readonly RaycastHit[] sightHits = new RaycastHit[64];
        private readonly List<MonoBehaviour> components = new();
        private readonly HashSet<MonoBehaviour> considered = new();
        private bool interactWasHeld;
        public MonoBehaviour FocusedTarget { get; private set; }
        public Vector3 FocusPoint { get; private set; }
        public int InteractionCount { get; private set; }

        private void Update()
        {
            bool held = Keyboard.current != null && Keyboard.current.eKey.isPressed;
            bool pressed = held && !interactWasHeld;
            interactWasHeld = held;
            var game = GameManager.Instance;
            if (game == null || DialogueManager.IsOpen || !InteractiveState(game.State))
            { ClearFocus(game); return; }
            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up,
                interactionDistance, nearby, interactionLayers, QueryTriggerInteraction.Collide);
            MonoBehaviour best = null;
            float bestScore = float.MaxValue;
            Vector3 bestPoint = default;
            considered.Clear();
            var camera = Camera.main;
            for (int i = 0; i < count; i++)
            {
                var collider = nearby[i];
                if (collider == null || collider.transform.IsChildOf(transform)) continue;
                components.Clear();collider.GetComponentsInParent(false,components);
                foreach (var behaviour in components)
                {
                    if (!behaviour.isActiveAndEnabled || !considered.Add(behaviour) ||
                        !(behaviour is IInteractable candidate) || !candidate.CanInteract(game)) continue;
                    var anchor=behaviour.GetComponent<InteractionFocusAnchor>();
                    var point=anchor!=null?anchor.Point:collider.bounds.center;
                    if (!HasClearPath(behaviour,point,anchor)) continue;
                    float score=(collider.transform.position-transform.position).sqrMagnitude;
                    // Distance remains primary; a small preference reduces flicker between adjacent props.
                    if(behaviour==FocusedTarget)score-=.18f;
                    if(camera!=null)
                    {
                        var direction=(point-camera.transform.position).normalized;
                        score+=(1-Vector3.Dot(camera.transform.forward,direction))*.18f;
                    }
                    if(score<bestScore){bestScore=score;best=behaviour;bestPoint=point;}
                }
            }
            FocusedTarget=best;FocusPoint=bestPoint;
            game.SetInteraction(best==null?"":"[E] "+((IInteractable)best).Prompt);
            if(best!=null&&pressed)
            {
                ((IInteractable)best).Interact(game);InteractionCount++;
                if(best==null||!best.gameObject.activeInHierarchy||DialogueManager.IsOpen||
                    !((IInteractable)best).CanInteract(game))ClearFocus(game);
            }
        }
        public bool HasClearPath(MonoBehaviour target,Vector3 point,InteractionFocusAnchor anchor=null)
        {
            var origin=transform.position+Vector3.up*1.25f;
            var delta=point-origin;float distance=delta.magnitude;
            if(distance<.01f)return true;
            int count=Physics.RaycastNonAlloc(origin,delta/distance,sightHits,distance-.025f,
                obstructionLayers,QueryTriggerInteraction.Ignore);
            if(count==sightHits.Length)return false;
            for(int i=0;i<count;i++)
            {
                var item=sightHits[i].transform;
                if(item.IsChildOf(transform)||item==target.transform||item.IsChildOf(target.transform)||
                    anchor!=null&&anchor.Owns(item))continue;
                return false;
            }
            return true;
        }
        private static bool InteractiveState(GameState state)=>state==GameState.Phase2A||
            state==GameState.Phase2BCleanup||state==GameState.FindCeleste||state==GameState.FollowCeleste||
            state==GameState.EscapeStudio||state==GameState.InvestigateGallery||state==GameState.ReachBallroom;
        private void ClearFocus(GameManager game){FocusedTarget=null;FocusPoint=default;game?.SetInteraction("");}
        private void OnDisable()=>ClearFocus(GameManager.Instance);
    }
}
