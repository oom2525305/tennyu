using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Tenyu.SceneFlow;

namespace Tenyu.Timeline
{
    public sealed class TimelineUIController :
        MonoBehaviour
    {
        [Header("シーン遷移")]
        [SerializeField]
        private SceneNavigator sceneNavigator;

        [Header("詳細ウィンドウ")]
        [SerializeField]
        private GameObject modalOverlay;

        [SerializeField]
        private GameObject stageDetailWindow;

        [Header("ステージ情報")]
        [SerializeField]
        private TMP_Text stageNameText;

        [SerializeField]
        private TMP_Text yearText;

        [SerializeField]
        private TMP_Text descriptionText;

        [SerializeField]
        private TMP_Text difficultyText;

        [SerializeField]
        private TMP_Text clearStatusText;

        [SerializeField]
        private Image stageIconImage;

        [Header("ボタン")]
        [SerializeField]
        private Button challengeButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Button outsideCloseButton;

        private TimelineStageData selectedStage;

        private void Awake()
        {
            RegisterButtons();
            CloseDetails();
        }

        private void RegisterButtons()
        {
            if (challengeButton != null)
            {
                challengeButton.onClick.AddListener(
                    ChallengeSelectedStage
                );
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(
                    CloseDetails
                );
            }

            if (outsideCloseButton != null)
            {
                outsideCloseButton.onClick.AddListener(
                    CloseDetails
                );
            }
        }

        public void OpenDetails(
            TimelineStageData stageData)
        {
            if (stageData == null)
                return;

            selectedStage = stageData;

            if (stageNameText != null)
            {
                stageNameText.text =
                    stageData.StageName;
            }

            if (yearText != null)
            {
                yearText.text =
                    stageData.Year;
            }

            if (descriptionText != null)
            {
                descriptionText.text =
                    stageData.Description;
            }

            if (difficultyText != null)
            {
                difficultyText.text =
                    CreateDifficultyText(
                        stageData.Difficulty
                    );
            }

            if (clearStatusText != null)
            {
                bool isCleared =
                    StageProgress.IsCleared(
                        stageData.StageId
                    );

                clearStatusText.text =
                    isCleared
                        ? "攻略済み"
                        : "未攻略";
            }

            if (stageIconImage != null)
            {
                stageIconImage.sprite =
                    stageData.StageIcon;

                stageIconImage.enabled =
                    stageData.StageIcon != null;
            }

            if (modalOverlay != null)
            {
                modalOverlay.SetActive(true);
            }

            if (stageDetailWindow != null)
            {
                stageDetailWindow.SetActive(true);
            }
        }

        public void CloseDetails()
        {
            selectedStage = null;

            if (stageDetailWindow != null)
            {
                stageDetailWindow.SetActive(false);
            }

            if (modalOverlay != null)
            {
                modalOverlay.SetActive(false);
            }
        }

        private void ChallengeSelectedStage()
        {
            if (selectedStage == null ||
                sceneNavigator == null)
            {
                return;
            }

            sceneNavigator.LoadGame(
                selectedStage.StageId
            );
        }

        private string CreateDifficultyText(
            int difficulty)
        {
            int value = Mathf.Clamp(
                difficulty,
                1,
                5
            );

            return
                new string('★', value) +
                new string('☆', 5 - value);
        }

        private void OnDestroy()
        {
            if (challengeButton != null)
            {
                challengeButton.onClick.RemoveListener(
                    ChallengeSelectedStage
                );
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(
                    CloseDetails
                );
            }

            if (outsideCloseButton != null)
            {
                outsideCloseButton.onClick.RemoveListener(
                    CloseDetails
                );
            }
        }
    }
}