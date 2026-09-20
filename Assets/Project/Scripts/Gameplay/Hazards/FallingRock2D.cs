using TMPro;
using UnityEngine;

namespace Tenyu.Gameplay
{
    public sealed class FallingRock2D : MonoBehaviour
    {
        private enum RockState
        {
            Waiting,
            Falling,
            Finished
        }

        [Header("ゲーム管理")]
        [SerializeField]
        private StageDirector stageDirector;

        [Header("落石オブジェクト")]
        [SerializeField]
        private Transform rockVisual;

        [SerializeField]
        private Transform impactVisual;

        [SerializeField]
        private Transform impactPoint;

        [SerializeField]
        private GameObject warningMarker;

        [SerializeField]
        private Transform shadowVisual;

        [SerializeField]
        private TMP_Text countdownText;

        [Header("時間")]
        [SerializeField, Min(0f)]
        private float warningTime = 12f;

        [SerializeField, Min(0.05f)]
        private float fallDuration = 0.6f;

        [Header("落下設定")]
        [SerializeField, Min(0.1f)]
        private float fallHeight = 5f;

        [SerializeField, Min(0.05f)]
        private float hitRadius = 0.7f;

        [SerializeField]
        private LayerMask protectedTargetLayers = ~0;

        [Header("任意")]
        [SerializeField]
        private ParticleSystem impactEffect;

        [SerializeField]
        private AudioSource impactSound;

        private RockState state;
        private float remainingWarningTime;
        private float fallElapsed;

        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Vector3 shadowBaseScale;

        private void Awake()
        {
            if (shadowVisual != null)
            {
                shadowBaseScale =
                    shadowVisual.localScale;
            }
        }

        private void OnEnable()
        {
            if (stageDirector != null)
            {
                stageDirector.StageReset += ResetRock;
            }
        }

        private void Start()
        {
            ResetRock();
        }

        private void Update()
        {
            if (stageDirector == null)
            {
                return;
            }

            if (stageDirector.Phase != StagePhase.Playing)
            {
                return;
            }

            switch (state)
            {
                case RockState.Waiting:
                    UpdateWarning();
                    break;

                case RockState.Falling:
                    UpdateFalling();
                    break;
            }
        }

        private void UpdateWarning()
        {
            remainingWarningTime -=
                StageTime.DeltaTime;

            remainingWarningTime =
                Mathf.Max(
                    0f,
                    remainingWarningTime
                );

            UpdateCountdownText();

            if (remainingWarningTime <= 0f)
            {
                BeginFall();
            }
        }

        private void BeginFall()
        {
            state = RockState.Falling;
            fallElapsed = 0f;

            if (warningMarker != null)
            {
                warningMarker.SetActive(false);
            }

            if (impactVisual != null)
            {
                impactVisual.gameObject.SetActive(false);
            }

            if (rockVisual != null)
            {
                rockVisual.position = startPosition;
                rockVisual.gameObject.SetActive(true);
            }

            if (shadowVisual != null)
            {
                shadowVisual.gameObject.SetActive(true);

                shadowVisual.localScale =
                    shadowBaseScale * 0.25f;
            }
        }

        private void UpdateFalling()
        {
            if (rockVisual == null)
            {
                return;
            }

            fallElapsed += StageTime.DeltaTime;

            float progress = Mathf.Clamp01(
                fallElapsed / fallDuration
            );

            // 徐々に加速しながら落下
            float curvedProgress =
                progress * progress;

            rockVisual.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                curvedProgress
            );

            if (shadowVisual != null)
            {
                shadowVisual.localScale =
                    Vector3.Lerp(
                        shadowBaseScale * 0.25f,
                        shadowBaseScale,
                        progress
                    );
            }

            if (progress >= 1f)
            {
                Impact();
            }
        }

        private void Impact()
        {
            state = RockState.Finished;

            if (rockVisual != null)
            {
                rockVisual.position = targetPosition;
                rockVisual.gameObject.SetActive(false);
            }

            if (impactVisual != null)
            {
                impactVisual.position = targetPosition;
                impactVisual.gameObject.SetActive(true);
            }

            if (shadowVisual != null)
            {
                shadowVisual.gameObject.SetActive(false);
            }

            if (impactEffect != null)
            {
                impactEffect.transform.position =
                    targetPosition;

                impactEffect.Play();
            }

            if (impactSound != null)
            {
                impactSound.Play();
            }

            DetectProtectedTarget();
        }

        private void DetectProtectedTarget()
        {
            Collider2D[] hits =
                Physics2D.OverlapCircleAll(
                    targetPosition,
                    hitRadius,
                    protectedTargetLayers
                );

            foreach (Collider2D hit in hits)
            {
                ProtectedTarget2D target =
                    hit.GetComponentInParent
                        <ProtectedTarget2D>();

                if (target == null)
                {
                    continue;
                }

                target.ReceiveHazard("落石");
                break;
            }
        }

        public void ResetRock()
        {
            if (impactPoint == null)
            {
                return;
            }

            state = RockState.Waiting;
            remainingWarningTime = warningTime;
            fallElapsed = 0f;

            targetPosition = impactPoint.position;

            startPosition =
                targetPosition +
                Vector3.up * fallHeight;

            if (rockVisual != null)
            {
                rockVisual.position = startPosition;
                rockVisual.gameObject.SetActive(false);
            }

            if (impactVisual != null)
            {
                impactVisual.position = targetPosition;
                impactVisual.gameObject.SetActive(false);
            }

            if (warningMarker != null)
            {
                warningMarker.SetActive(true);
            }

            if (shadowVisual != null)
            {
                shadowVisual.position =
                    targetPosition;

                shadowVisual.localScale =
                    shadowBaseScale * 0.25f;

                shadowVisual.gameObject.SetActive(false);
            }

            UpdateCountdownText();
        }

        private void UpdateCountdownText()
        {
            if (countdownText == null)
            {
                return;
            }

            countdownText.text =
                $"落石 {remainingWarningTime:0.0}s";
        }

        private void OnDisable()
        {
            if (stageDirector != null)
            {
                stageDirector.StageReset -= ResetRock;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (impactPoint == null)
            {
                return;
            }

            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(
                impactPoint.position,
                hitRadius
            );

            Gizmos.DrawLine(
                impactPoint.position,
                impactPoint.position +
                Vector3.up * fallHeight
            );
        }
    }
}