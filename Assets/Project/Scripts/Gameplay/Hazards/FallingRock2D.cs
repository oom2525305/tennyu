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

        [Header("接近で予告を開始（ハード用）")]
        [SerializeField] private bool triggerOnApproach;
        [SerializeField, Min(0.1f)] private float activationRadius = 2f;
        private bool warningStarted;

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

        [Header("落下直前の予告音（任意）")]
        [SerializeField]
        private AudioSource warningSound;

        [SerializeField, Min(0f)]
        private float warningSoundLeadTime = 2f;

        [Header("神の能力")]
        [SerializeField] private DivinePowerReceiver2D powerReceiver;
        private Vector3 countdownOffset;

        private RockState state;
        private float remainingWarningTime;
        private float fallElapsed;
        private bool warningSoundPlayed;

        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Vector3 shadowBaseScale;

        private void Awake()
        {
            if (countdownText != null && impactPoint != null)
                countdownOffset = countdownText.transform.parent.position - impactPoint.position;
            if (shadowVisual != null)
            {
                shadowBaseScale =
                    shadowVisual.localScale;
            }
        }

        private void OnEnable()
        {
            if (powerReceiver != null)
            {
                powerReceiver.ThunderReceived += ReceiveThunder;
            }
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
            if (!warningStarted)
            {
                foreach (Collider2D hit in Physics2D.OverlapCircleAll(targetPosition, activationRadius, protectedTargetLayers))
                {
                    if (hit.GetComponentInParent<ProtectedTarget2D>() == null) continue;
                    warningStarted = true;
                    break;
                }
                if (!warningStarted) return;
            }

            remainingWarningTime -=
                StageTime.DeltaTime;

            remainingWarningTime =
                Mathf.Max(
                    0f,
                    remainingWarningTime
                );

            UpdateCountdownText();

            if (!warningSoundPlayed && remainingWarningTime > 0f &&
                remainingWarningTime <= warningSoundLeadTime)
            {
                warningSoundPlayed = true;
                if (warningSound != null) warningSound.Play();
            }

            if (remainingWarningTime <= 0f)
            {
                BeginFall();
            }
        }

        private void ReceiveThunder(float strength)
        {
            if (strength > 0f && stageDirector != null &&
                stageDirector.Phase == StagePhase.Playing && state == RockState.Waiting)
                BeginFall();
        }

        private void AlignWarning()
        {
            if (powerReceiver != null) powerReceiver.transform.position = targetPosition;
            if (warningMarker != null) warningMarker.transform.position = targetPosition;
            if (shadowVisual != null) shadowVisual.position = targetPosition;
            if (countdownText != null) countdownText.transform.parent.position = targetPosition + countdownOffset;
        }

        private void BeginFall()
        {
            state = RockState.Falling;
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            fallElapsed = 0f;
            if (warningSound != null) warningSound.Stop();

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
            if (powerReceiver != null) powerReceiver.gameObject.SetActive(false);

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
            // 参照が欠けていても、リトライ時に前回の音・着地エフェクトを残さない。
            StopSounds();
            warningSoundPlayed = false;
            if (impactEffect != null)
                impactEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (impactPoint == null)
            {
                return;
            }

            state = RockState.Waiting;
            warningStarted = !triggerOnApproach;
            if (countdownText != null) countdownText.gameObject.SetActive(true);
            remainingWarningTime = warningTime;
            fallElapsed = 0f;

            targetPosition = impactPoint.position;
            if (powerReceiver != null) powerReceiver.gameObject.SetActive(true);
            AlignWarning();

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

            countdownText.text = warningStarted
                ? $"落石 {remainingWarningTime:0.0}s" : "接近で落石";
        }

        private void OnDisable()
        {
            if (powerReceiver != null)
            {
                powerReceiver.ThunderReceived -= ReceiveThunder;
            }
            StopSounds();
            if (stageDirector != null)
            {
                stageDirector.StageReset -= ResetRock;
            }
        }

        private void StopSounds()
        {
            if (warningSound != null) warningSound.Stop();
            if (impactSound != null) impactSound.Stop();
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
