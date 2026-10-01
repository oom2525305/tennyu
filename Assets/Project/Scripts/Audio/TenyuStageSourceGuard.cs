using UnityEngine;
using Tenyu.Gameplay;

namespace Tenyu.Audio
{
    // 既存のFallingRock2D.impactSoundなどを、リトライ・ポーズに対応させる。
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public sealed class TenyuStageSourceGuard : MonoBehaviour
    {
        [SerializeField] private StageDirector stageDirector;
        [Tooltip("予告音はオン。失敗原因の着地音を最後まで鳴らす場合はオフ")]
        [SerializeField] private bool stopOnResult;
        private AudioSource source;
        private bool paused;
        private void Awake() { source = GetComponent<AudioSource>(); }
        private void OnEnable()
        {
            if (stageDirector == null) stageDirector = FindFirstObjectByType<StageDirector>();
            if (stageDirector == null) return;
            stageDirector.PhaseChanged += OnPhase;
            stageDirector.StageReset += StopSound;
            OnPhase(stageDirector.Phase);
        }
        private void OnPhase(StagePhase phase)
        {
            if (phase == StagePhase.Paused && source.isPlaying)
            {
                source.Pause();
                paused = true;
            }
            else if (phase == StagePhase.Playing && paused)
            {
                source.UnPause();
                paused = false;
            }
            else if (phase == StagePhase.Observation) StopSound();
            else if (phase == StagePhase.Cleared || phase == StagePhase.Failed)
            {
                if (stopOnResult || source.loop) StopSound();
                else if (paused)
                {
                    source.UnPause();
                    paused = false;
                }
            }
        }
        private void StopSound() { source.Stop(); paused = false; }
        private void OnDisable()
        {
            if (stageDirector != null)
            {
                stageDirector.PhaseChanged -= OnPhase;
                stageDirector.StageReset -= StopSound;
            }
            StopSound();
        }
    }
}
