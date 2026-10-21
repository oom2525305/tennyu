using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Tenyu.SceneFlow;

namespace Tenyu.Timeline
{
    [RequireComponent(typeof(Button))]
    public sealed class TimelineStageButton : MonoBehaviour
    {
        [Header("ステージ")]
        [SerializeField]
        private TimelineStageData stageData;

        [SerializeField]
        private TimelineUIController timelineUI;

        [Header("表示")]
        [SerializeField]
        private TMP_Text stageNameText;

        [SerializeField]
        private TMP_Text yearText;

        [SerializeField]
        private GameObject clearMark;

        [Header("ロック表示")]
        [SerializeField]
        private GameObject lockIcon;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (stageData == null)
            {
                button.interactable = false;

                if (stageNameText != null)
                    stageNameText.text = "";

                if (yearText != null)
                    yearText.text = "";

                if (clearMark != null)
                    clearMark.SetActive(false);

                if (lockIcon != null)
                    lockIcon.SetActive(false);

                return;
            }

            bool isUnlocked =
                StageProgress.IsUnlocked(stageData.StageId);

            bool isCleared =
                StageProgress.IsCleared(stageData.StageId) ||
                StageProgress.IsCleared(stageData.StageId, StageDifficulty.Hard);

            if (stageNameText != null)
                stageNameText.text = stageData.StageName;

            if (yearText != null)
                yearText.text = stageData.Year;

            // 未解放のボタンは操作できない。
            button.interactable = isUnlocked;

            if (lockIcon != null)
                lockIcon.SetActive(!isUnlocked);

            if (clearMark != null)
                clearMark.SetActive(isUnlocked && isCleared);
        }

        private void HandleClick()
        {
            if (stageData == null || timelineUI == null)
                return;

            // 呼び出し時にも解放状態を確認。
            if (!StageProgress.IsUnlocked(stageData.StageId))
            {
                Refresh();
                return;
            }

            timelineUI.OpenDetails(stageData);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }
    }
}