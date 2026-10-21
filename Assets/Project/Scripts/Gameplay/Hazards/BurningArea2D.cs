using UnityEngine;

namespace Tenyu.Gameplay
{
    [RequireComponent(typeof(FlammableObject2D))]
    public sealed class BurningArea2D : MonoBehaviour
    {
        [SerializeField] private StageDirector stageDirector;
        [SerializeField] private bool initiallyBurning = true;
        [SerializeField, Min(0.05f)] private float damageRadius = 0.55f;
        [SerializeField] private LayerMask targetLayers;
        private FlammableObject2D flammable;
        private bool resetPending;

        private void Awake() { flammable = GetComponent<FlammableObject2D>(); }
        private void OnEnable()
        {
            if (stageDirector != null) stageDirector.StageReset += ScheduleReset;
            ScheduleReset();
        }
        private void ScheduleReset() { resetPending = true; }
        private void LateUpdate()
        {
            // Run after all Start/StageReset handlers, so component order cannot erase the initial fire.
            if (!resetPending) return;
            resetPending = false;
            if (initiallyBurning) flammable.Ignite();
        }
        private void FixedUpdate()
        {
            if (resetPending || stageDirector == null || stageDirector.Phase != StagePhase.Playing ||
                !flammable.IsBurning) return;
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, damageRadius, targetLayers))
            {
                ProtectedTarget2D target = hit.GetComponentInParent<ProtectedTarget2D>();
                if (target == null) continue;
                target.ReceiveHazard("宿場の炎");
                break;
            }
        }
        private void OnDisable()
        {
            if (stageDirector != null) stageDirector.StageReset -= ScheduleReset;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, damageRadius);
        }
    }
}
