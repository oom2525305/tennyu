using System;
using UnityEngine;

namespace Tenyu.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ProtectedTarget2D : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField]
        private StageDirector stageDirector;

        public bool HasBeenHit { get; private set; }

        public event Action<string> Hit;

        private void OnEnable()
        {
            if (stageDirector != null)
                stageDirector.StageReset += ResetTarget;
        }

        /// <summary>
        /// 災いから呼び出す。
        /// </summary>
        public void ReceiveHazard(string hazardName)
        {
            if (stageDirector == null)
                return;

            if (stageDirector.Phase != StagePhase.Playing)
                return;

            if (HasBeenHit)
                return;

            HasBeenHit = true;

            string cause = string.IsNullOrWhiteSpace(hazardName)
                ? "災い"
                : hazardName;

            Hit?.Invoke(cause);

            stageDirector.FailStage(
                $"{cause}が信長を直撃"
            );
        }

        private void ResetTarget()
        {
            HasBeenHit = false;
        }

        private void OnDisable()
        {
            if (stageDirector != null)
                stageDirector.StageReset -= ResetTarget;
        }
    }
}