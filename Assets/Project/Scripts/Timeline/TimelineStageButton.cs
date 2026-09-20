using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tenyu.Timeline
{
    [RequireComponent(typeof(Button))]
    public sealed class TimelineStageButton :
        MonoBehaviour
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

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();

            button.onClick.AddListener(
                HandleClick
            );
        }

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (stageData == null)
                return;

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

            if (clearMark != null)
            {
                clearMark.SetActive(
                    StageProgress.IsCleared(
                        stageData.StageId
                    )
                );
            }
        }

        private void HandleClick()
        {
            if (stageData == null ||
                timelineUI == null)
            {
                return;
            }

            timelineUI.OpenDetails(stageData);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(
                    HandleClick
                );
            }
        }
    }
}