using UnityEngine;

namespace Tenyu.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class HazardHitbox2D : MonoBehaviour
    {
        [Header("災い")]
        [SerializeField]
        private string hazardName = "落石";

        [SerializeField]
        private StageDirector stageDirector;

        [Header("接触設定")]
        [SerializeField]
        private bool consumeAfterHit = true;

        private Collider2D hitbox;
        private bool consumed;

        private void Awake()
        {
            hitbox = GetComponent<Collider2D>();
            hitbox.isTrigger = true;
        }

        private void OnEnable()
        {
            if (stageDirector != null)
                stageDirector.StageReset += ResetHazard;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed)
                return;

            if (stageDirector == null ||
                stageDirector.Phase != StagePhase.Playing)
            {
                return;
            }

            ProtectedTarget2D target =
                other.GetComponentInParent<ProtectedTarget2D>();

            if (target == null)
                return;

            target.ReceiveHazard(hazardName);

            if (consumeAfterHit)
            {
                consumed = true;
                hitbox.enabled = false;
            }
        }

        public void Arm()
        {
            consumed = false;
            hitbox.enabled = true;
        }

        public void Disarm()
        {
            hitbox.enabled = false;
        }

        private void ResetHazard()
        {
            consumed = false;
            hitbox.enabled = true;
        }

        private void OnDisable()
        {
            if (stageDirector != null)
                stageDirector.StageReset -= ResetHazard;
        }
    }
}