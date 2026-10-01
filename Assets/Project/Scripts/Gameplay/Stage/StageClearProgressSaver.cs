using UnityEngine;
using Tenyu.SceneFlow;
using Tenyu.Timeline;

namespace Tenyu.Gameplay
{
    [RequireComponent(typeof(StageDirector))]
    public sealed class StageClearProgressSaver :
        MonoBehaviour
    {
        [SerializeField]
        private StageDirector stageDirector;

        private void Reset()
        {
            stageDirector =
                GetComponent<StageDirector>();
        }

        private void Awake()
        {
            if (stageDirector == null)
            {
                stageDirector =
                    GetComponent<StageDirector>();
            }
        }

        private void OnEnable()
        {
            if (stageDirector != null)
            {
                stageDirector.StageCleared +=
                    SaveClearProgress;
            }
        }

        private void SaveClearProgress()
        {
            StageProgress.SetCleared(
                GameSession.SelectedStageId, GameSession.SelectedDifficulty
            );
        }

        private void OnDisable()
        {
            if (stageDirector != null)
            {
                stageDirector.StageCleared -=
                    SaveClearProgress;
            }
        }
    }
}