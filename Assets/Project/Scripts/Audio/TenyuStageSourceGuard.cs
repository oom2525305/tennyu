using UnityEngine;
using Tenyu.Gameplay;

namespace Tenyu.Audio
{
    // 既存のFallingRock2D.impactSoundなどを、リトライ・ポーズに対応させる。
    [RequireComponent(typeof(AudioSource))]
    public sealed class TenyuStageSourceGuard : MonoBehaviour
    {
        [SerializeField] private StageDirector stageDirector;
        private AudioSource source;
        private bool paused;
        private void Awake() { source = GetComponent<AudioSource>(); }
        private void OnEnable()
        {
            if (stageDirector == null) stageDirector = FindFirstObjectByType<StageDirector>();
            if (stageDirector == null) return;
            stageDirector.PhaseChanged += OnPhase;
            stageDirector.StageReset += StopSound;
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
