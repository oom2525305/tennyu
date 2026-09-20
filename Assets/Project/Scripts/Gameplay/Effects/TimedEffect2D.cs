using UnityEngine;

namespace Tenyu.Gameplay
{
    public sealed class TimedEffect2D : MonoBehaviour
    {
        [Header("ゲーム管理")]
        [SerializeField]
        private StageDirector stageDirector;

        [SerializeField, Min(0.1f)]
        private float lifetime = 2f;

        private float elapsedTime;

        private void Awake()
        {
            // Prefabはシーン内参照を保持できないため自動取得する
            if (stageDirector == null)
            {
                stageDirector =
                    FindFirstObjectByType<StageDirector>();
            }
        }

        private void OnEnable()
        {
            elapsedTime = 0f;

            if (stageDirector != null)
            {
                stageDirector.StageReset +=
                    HandleStageReset;
            }
        }

        private void Update()
        {
            elapsedTime += StageTime.DeltaTime;

            if (elapsedTime >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void HandleStageReset()
        {
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (stageDirector != null)
            {
                stageDirector.StageReset -=
                    HandleStageReset;
            }
        }
    }
}
