using System;
using UnityEngine;

namespace Tenyu.Gameplay
{
    public enum StagePhase
    {
        Observation, // 観察中
        Playing,     // 通常プレイ
        Paused,      // 一時停止
        Cleared,
        Failed
    }

    [DisallowMultipleComponent]
    public sealed class StageDirector : MonoBehaviour
    {
        [Header("ステージ設定")]
        [SerializeField, Min(1f)]
        private float stageDuration = 45f;

        [SerializeField] private bool clearOnTimeout = true;

        [Header("能力ドラッグ中")]
        [SerializeField, Range(0.05f, 1f)]
        private float aimingTimeScale = 0.2f;

        public StagePhase Phase { get; private set; }
        public float RemainingTime { get; private set; }
        public float RemainingTime01 =>
            Mathf.Clamp01(RemainingTime / stageDuration);

        public bool IsAiming { get; private set; }

        public event Action<StagePhase> PhaseChanged;
        public event Action<float> TimeChanged;
        public event Action<bool> AimingChanged;
        public event Action StageReset;
        public event Action StageCleared;
        public event Action<string> StageFailed;

        private float defaultFixedDeltaTime;

        private void Awake()
        {
            defaultFixedDeltaTime = Time.fixedDeltaTime;
            ResetStage();
        }

        private void Update()
        {
            if (Phase != StagePhase.Playing)
                return;

            RemainingTime =
                Mathf.Max(0f, RemainingTime - StageTime.DeltaTime);

            TimeChanged?.Invoke(RemainingTime);

            if (RemainingTime <= 0f)
            {
                if (clearOnTimeout) ClearStage();
                else FailStage("時間切れ：門まで信長を逃がせませんでした");
            }
        }

        // 観察画面の「タップで時を動かす」から呼ぶ
        public void BeginStage()
        {
            if (Phase != StagePhase.Observation)
                return;

            IsAiming = false;
            ChangePhase(StagePhase.Playing);
            ApplyTimeScale(1f);
        }

        // 能力アイコンをドラッグし始めたとき
        public void BeginAiming()
        {
            if (Phase != StagePhase.Playing || IsAiming)
                return;

            IsAiming = true;
            ApplyTimeScale(aimingTimeScale);
            AimingChanged?.Invoke(true);
        }

        // 能力を発動、またはドラッグをキャンセルしたとき
        public void EndAiming()
        {
            if (Phase != StagePhase.Playing || !IsAiming)
                return;

            IsAiming = false;
            ApplyTimeScale(1f);
            AimingChanged?.Invoke(false);
        }

        public void PauseStage()
        {
            if (Phase != StagePhase.Playing)
                return;

            ChangePhase(StagePhase.Paused);
            ApplyTimeScale(0f);
        }

        public void ResumeStage()
        {
            if (Phase != StagePhase.Paused)
                return;

            ChangePhase(StagePhase.Playing);

            ApplyTimeScale(
                IsAiming ? aimingTimeScale : 1f
            );
        }

        public void TogglePause()
        {
            switch (Phase)
            {
                case StagePhase.Playing:
                    PauseStage();
                    break;

                case StagePhase.Paused:
                    ResumeStage();
                    break;
            }
        }

        public void ClearStage()
        {
            if (IsResultPhase())
                return;

            IsAiming = false;
            StageTime.Reset();
            ChangePhase(StagePhase.Cleared);
            ApplyTimeScale(0f);

            StageCleared?.Invoke();
        }

        public void FailStage(string cause)
        {
            if (IsResultPhase())
                return;

            IsAiming = false;
            StageTime.Reset();
            ChangePhase(StagePhase.Failed);
            ApplyTimeScale(0f);

            StageFailed?.Invoke(cause);
        }

        // シーン再読み込みを使わず初期状態へ戻す
        public void ResetStage()
        {
            // ドラッグ中にリトライされても、能力用のスロー状態を残さない
            StageTime.Reset();

            IsAiming = false;
            RemainingTime = stageDuration;

            ApplyTimeScale(0f);
            ChangePhase(StagePhase.Observation);

            StageReset?.Invoke();
            TimeChanged?.Invoke(RemainingTime);
            AimingChanged?.Invoke(false);
        }

        private bool IsResultPhase()
        {
            return Phase == StagePhase.Cleared ||
                   Phase == StagePhase.Failed;
        }

        private void ChangePhase(StagePhase nextPhase)
        {
            if (Phase == nextPhase)
                return;

            Phase = nextPhase;
            PhaseChanged?.Invoke(Phase);
        }

        private void ApplyTimeScale(float scale)
        {
            Time.timeScale = scale;

            // スロー中の2D物理を滑らかに保つ
            Time.fixedDeltaTime =
                defaultFixedDeltaTime * Mathf.Max(scale, 0.01f);
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }
    }
}
