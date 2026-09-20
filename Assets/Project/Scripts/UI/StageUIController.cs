using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tenyu.Gameplay
{
    public sealed class StageUIController : MonoBehaviour
    {
        [Header("ゲーム管理")]
        [SerializeField]
        private StageDirector stageDirector;

        [Header("通常HUD")]
        [SerializeField]
        private TMP_Text timerText;

        [SerializeField]
        private Button pauseButton;

        [Header("観察フェーズ")]
        [SerializeField]
        private GameObject observationPanel;

        [SerializeField]
        private Button startButton;

        [Header("一時停止")]
        [SerializeField]
        private GameObject pausePanel;

        [SerializeField]
        private Button resumeButton;

        [SerializeField]
        private Button pauseRetryButton;

        [Header("クリア")]
        [SerializeField]
        private GameObject clearPanel;

        [SerializeField]
        private Button clearRetryButton;

        [Header("失敗")]
        [SerializeField]
        private GameObject failedPanel;

        [SerializeField]
        private TMP_Text failedReasonText;

        [SerializeField]
        private Button failedRetryButton;

        private void Awake()
        {
            RegisterButtons();
        }

        private void OnEnable()
        {
            if (stageDirector == null)
                return;

            stageDirector.PhaseChanged += HandlePhaseChanged;
            stageDirector.TimeChanged += HandleTimeChanged;
            stageDirector.StageFailed += HandleStageFailed;
        }

        private void Start()
        {
            if (stageDirector == null)
            {
                Debug.LogError(
                    "StageUIControllerにStageDirectorが設定されていません。",
                    this
                );

                return;
            }

            HandlePhaseChanged(stageDirector.Phase);
            HandleTimeChanged(stageDirector.RemainingTime);
        }

        private void RegisterButtons()
        {
            if (stageDirector == null)
                return;

            if (startButton != null)
                startButton.onClick.AddListener(
                    stageDirector.BeginStage
                );

            if (pauseButton != null)
                pauseButton.onClick.AddListener(
                    stageDirector.PauseStage
                );

            if (resumeButton != null)
                resumeButton.onClick.AddListener(
                    stageDirector.ResumeStage
                );

            if (pauseRetryButton != null)
                pauseRetryButton.onClick.AddListener(
                    stageDirector.ResetStage
                );

            if (clearRetryButton != null)
                clearRetryButton.onClick.AddListener(
                    stageDirector.ResetStage
                );

            if (failedRetryButton != null)
                failedRetryButton.onClick.AddListener(
                    stageDirector.ResetStage
                );
        }

        private void HandlePhaseChanged(StagePhase phase)
        {
            SetActive(
                observationPanel,
                phase == StagePhase.Observation
            );

            SetActive(
                pausePanel,
                phase == StagePhase.Paused
            );

            SetActive(
                clearPanel,
                phase == StagePhase.Cleared
            );

            SetActive(
                failedPanel,
                phase == StagePhase.Failed
            );

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(
                    phase == StagePhase.Playing
                );
            }
        }

        private void HandleTimeChanged(float remainingTime)
        {
            if (timerText == null)
                return;

            timerText.text = remainingTime.ToString("0.0");
        }

        private void HandleStageFailed(string reason)
        {
            if (failedReasonText != null)
                failedReasonText.text = reason;
        }

        private void SetActive(
            GameObject target,
            bool active
        )
        {
            if (target != null)
                target.SetActive(active);
        }

        private void OnDisable()
        {
            if (stageDirector == null)
                return;

            stageDirector.PhaseChanged -= HandlePhaseChanged;
            stageDirector.TimeChanged -= HandleTimeChanged;
            stageDirector.StageFailed -= HandleStageFailed;
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveAllListeners();

            if (pauseButton != null)
                pauseButton.onClick.RemoveAllListeners();

            if (resumeButton != null)
                resumeButton.onClick.RemoveAllListeners();

            if (pauseRetryButton != null)
                pauseRetryButton.onClick.RemoveAllListeners();

            if (clearRetryButton != null)
                clearRetryButton.onClick.RemoveAllListeners();

            if (failedRetryButton != null)
                failedRetryButton.onClick.RemoveAllListeners();
        }
    }
}