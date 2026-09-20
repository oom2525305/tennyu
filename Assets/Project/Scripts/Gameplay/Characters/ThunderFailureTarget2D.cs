using UnityEngine;

namespace Tenyu.Gameplay
{
    [RequireComponent(typeof(DivinePowerReceiver2D))]
    [RequireComponent(typeof(ProtectedTarget2D))]
    public sealed class ThunderFailureTarget2D :
        MonoBehaviour
    {
        [Header("参照")]
        [SerializeField]
        private DivinePowerReceiver2D powerReceiver;

        [SerializeField]
        private ProtectedTarget2D protectedTarget;

        [Header("ゲームオーバーになる雷の強さ")]
        [SerializeField, Min(0f)]
        private float failureThreshold = 0.1f;

        private void Reset()
        {
            powerReceiver =
                GetComponent<DivinePowerReceiver2D>();

            protectedTarget =
                GetComponent<ProtectedTarget2D>();
        }

        private void Awake()
        {
            if (powerReceiver == null)
            {
                powerReceiver =
                    GetComponent<DivinePowerReceiver2D>();
            }

            if (protectedTarget == null)
            {
                protectedTarget =
                    GetComponent<ProtectedTarget2D>();
            }
        }

        private void OnEnable()
        {
            if (powerReceiver != null)
            {
                powerReceiver.ThunderReceived +=
                    HandleThunderReceived;
            }
        }

        private void OnDisable()
        {
            if (powerReceiver != null)
            {
                powerReceiver.ThunderReceived -=
                    HandleThunderReceived;
            }
        }

        private void HandleThunderReceived(
            float strength)
        {
            if (strength < failureThreshold)
                return;

            if (protectedTarget == null)
                return;

            protectedTarget.ReceiveHazard("落雷");
        }
    }
}