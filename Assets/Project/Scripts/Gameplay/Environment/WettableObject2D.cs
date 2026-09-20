using UnityEngine;
using UnityEngine.Events;

namespace Tenyu.Gameplay
{
    [RequireComponent(typeof(DivinePowerReceiver2D))]
    public sealed class WettableObject2D : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField]
        private StageDirector stageDirector;

        [SerializeField]
        private DivinePowerReceiver2D powerReceiver;

        [SerializeField]
        private GameObject wetVisual;

        [Header("濡れ時間")]
        [SerializeField, Min(0.1f)]
        private float wetDurationPerStrength = 5f;

        [SerializeField, Min(0.1f)]
        private float maximumWetDuration = 12f;

        [Header("イベント")]
        [SerializeField]
        private UnityEvent onBecameWet;

        [SerializeField]
        private UnityEvent onDried;

        public bool IsWet { get; private set; }

        public float RemainingWetTime =>
            remainingWetTime;

        private float remainingWetTime;

        private void Reset()
        {
            powerReceiver =
                GetComponent<DivinePowerReceiver2D>();
        }

        private void Awake()
        {
            if (powerReceiver == null)
            {
                powerReceiver =
                    GetComponent<DivinePowerReceiver2D>();
            }

            SetWetVisual(false);
        }

        private void OnEnable()
        {
            if (powerReceiver != null)
            {
                powerReceiver.RainReceived +=
                    ReceiveRain;
            }

            if (stageDirector != null)
            {
                stageDirector.StageReset +=
                    ResetState;
            }
        }

        private void Start()
        {
            ResetState();
        }

        private void Update()
        {
            if (!IsWet)
                return;

            if (stageDirector != null &&
                stageDirector.Phase !=
                StagePhase.Playing)
            {
                return;
            }

            remainingWetTime -=
                StageTime.DeltaTime;

            if (remainingWetTime <= 0f)
            {
                Dry();
            }
        }

        private void ReceiveRain(float strength)
        {
            if (strength <= 0f)
                return;

            float addedDuration =
                wetDurationPerStrength *
                strength;

            remainingWetTime = Mathf.Min(
                remainingWetTime +
                addedDuration,
                maximumWetDuration
            );

            if (IsWet)
                return;

            IsWet = true;

            SetWetVisual(true);

            onBecameWet?.Invoke();
        }

        public void Dry()
        {
            if (!IsWet)
                return;

            IsWet = false;
            remainingWetTime = 0f;

            SetWetVisual(false);

            onDried?.Invoke();
        }

        public void ResetState()
        {
            IsWet = false;
            remainingWetTime = 0f;

            SetWetVisual(false);
        }

        private void SetWetVisual(bool visible)
        {
            if (wetVisual != null)
            {
                wetVisual.SetActive(visible);
            }
        }

        private void OnDisable()
        {
            if (powerReceiver != null)
            {
                powerReceiver.RainReceived -=
                    ReceiveRain;
            }

            if (stageDirector != null)
            {
                stageDirector.StageReset -=
                    ResetState;
            }
        }

        [ContextMenu("Test Become Wet")]
        private void TestBecomeWet()
        {
            ReceiveRain(1f);
        }

        [ContextMenu("Test Dry")]
        private void TestDry()
        {
            Dry();
        }
    }
}