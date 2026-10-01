using UnityEngine;
using UnityEngine.Events;

namespace Tenyu.Gameplay
{
    [RequireComponent(typeof(DivinePowerReceiver2D))]
    public sealed class FlammableObject2D : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField]
        private StageDirector stageDirector;

        [SerializeField]
        private DivinePowerReceiver2D powerReceiver;

        [SerializeField]
        private SpriteRenderer bodyRenderer;

        [SerializeField]
        private GameObject fireVisual;

        [SerializeField]
        private GameObject burnedVisual;

        [SerializeField]
        private Collider2D blockingCollider;

        [SerializeField] private bool preventIgnitionWhileWet;

        [Header("着火設定")]
        [SerializeField, Min(0.01f)]
        private float ignitionThreshold = 1f;

        [SerializeField, Min(0f)]
        private float burnDuration = 6f;

        [Header("雨による消火設定")]
        [SerializeField, Min(0.01f)]
        private float extinguishThreshold = 1f;

        [Header("燃え尽きた後")]
        [SerializeField]
        private bool hideBodyAfterBurn;

        [SerializeField]
        private bool disableColliderAfterBurn;

        [SerializeField]
        private Color burnedColor =
            new Color(0.25f, 0.22f, 0.2f, 1f);

        [Header("イベント")]
        [SerializeField]
        private UnityEvent onIgnited;

        [SerializeField]
        private UnityEvent onExtinguished;

        [SerializeField]
        private UnityEvent onBurnCompleted;

        public bool IsBurning { get; private set; }

        public bool HasBurnedOut { get; private set; }

        private float currentHeat;
        private float currentExtinguishPower;
        private float burnTimer;

        private Color initialBodyColor = Color.white;
        private bool initialRendererEnabled;
        private bool initialColliderEnabled;

        private void Reset()
        {
            powerReceiver =
                GetComponent<DivinePowerReceiver2D>();

            bodyRenderer =
                GetComponent<SpriteRenderer>();

            blockingCollider =
                GetComponent<Collider2D>();
        }

        private void Awake()
        {
            if (powerReceiver == null)
            {
                powerReceiver =
                    GetComponent<DivinePowerReceiver2D>();
            }

            if (bodyRenderer != null)
            {
                initialBodyColor = bodyRenderer.color;
                initialRendererEnabled =
                    bodyRenderer.enabled;
            }

            if (blockingCollider != null)
            {
                initialColliderEnabled =
                    blockingCollider.enabled;
            }
        }

        private void OnEnable()
        {
            if (powerReceiver != null)
            {
                powerReceiver.ThunderReceived +=
                    ReceiveThunder;

                powerReceiver.RainReceived +=
                    ReceiveRain;
            }

            if (stageDirector != null)
            {
                stageDirector.StageReset += ResetState;
            }
        }

        private void Start()
        {
            ResetState();
        }

        private void Update()
        {
            if (!IsBurning)
                return;

            if (stageDirector != null &&
                stageDirector.Phase != StagePhase.Playing)
            {
                return;
            }

            if (burnDuration <= 0f)
                return;

            burnTimer += StageTime.DeltaTime;

            if (burnTimer >= burnDuration)
            {
                CompleteBurn();
            }
        }

        private void ReceiveThunder(float strength)
        {
            if (IsBurning || HasBurnedOut)
                return;

            if (preventIgnitionWhileWet && TryGetComponent(out WettableObject2D wettable) && wettable.IsWet)
                return;

            currentHeat += Mathf.Max(0f, strength);

            if (currentHeat >= ignitionThreshold)
            {
                Ignite();
            }
        }

        private void ReceiveRain(float strength)
        {
            Debug.Log(
                $"{gameObject.name}が雨を受けました " +
                $"Strength:{strength} " +
                $"IsBurning:{IsBurning}"
            );

            if (HasBurnedOut)
                return;

            float rainStrength =
                Mathf.Max(0f, strength);

            currentHeat = Mathf.Max(
                0f,
                currentHeat - rainStrength
            );

            if (!IsBurning)
                return;

            currentExtinguishPower += rainStrength;

            Debug.Log(
                $"消火蓄積:{currentExtinguishPower} / " +
                $"{extinguishThreshold}"
            );

            if (currentExtinguishPower >=
                extinguishThreshold)
            {
                Debug.Log($"{gameObject.name}を消火します");
                Extinguish();
            }
        }

        public void Ignite()
        {
            if (IsBurning || HasBurnedOut)
                return;

            IsBurning = true;
            burnTimer = 0f;
            currentExtinguishPower = 0f;

            if (fireVisual != null)
            {
                fireVisual.SetActive(true);
            }

            onIgnited?.Invoke();
        }

        public void Extinguish()
        {
            if (!IsBurning)
                return;

            IsBurning = false;
            currentHeat = 0f;
            currentExtinguishPower = 0f;
            burnTimer = 0f;

            if (fireVisual != null)
            {
                fireVisual.SetActive(false);
            }

            onExtinguished?.Invoke();
        }

        private void CompleteBurn()
        {
            IsBurning = false;
            HasBurnedOut = true;

            if (fireVisual != null)
            {
                fireVisual.SetActive(false);
            }

            if (burnedVisual != null)
            {
                burnedVisual.SetActive(true);
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.color = burnedColor;

                if (hideBodyAfterBurn)
                {
                    bodyRenderer.enabled = false;
                }
            }

            if (blockingCollider != null &&
                disableColliderAfterBurn)
            {
                blockingCollider.enabled = false;
            }

            onBurnCompleted?.Invoke();
        }

        public void ResetState()
        {
            IsBurning = false;
            HasBurnedOut = false;

            currentHeat = 0f;
            currentExtinguishPower = 0f;
            burnTimer = 0f;

            if (fireVisual != null)
            {
                fireVisual.SetActive(false);
            }

            if (burnedVisual != null)
            {
                burnedVisual.SetActive(false);
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.color = initialBodyColor;
                bodyRenderer.enabled =
                    initialRendererEnabled;
            }

            if (blockingCollider != null)
            {
                blockingCollider.enabled =
                    initialColliderEnabled;
            }
        }

        private void OnDisable()
        {
            if (powerReceiver != null)
            {
                powerReceiver.ThunderReceived -=
                    ReceiveThunder;

                powerReceiver.RainReceived -=
                    ReceiveRain;
            }

            if (stageDirector != null)
            {
                stageDirector.StageReset -= ResetState;
            }
        }
    }
}