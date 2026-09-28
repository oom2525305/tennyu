using UnityEngine;
using Tenyu.Gameplay;

namespace Tenyu.Audio
{
    // 雨・炎の表示オブジェクトに追加する。
    [DisallowMultipleComponent]
    public sealed class TenyuEffectLoopSound : MonoBehaviour
    {
        [SerializeField] private StageDirector stageDirector;
        [SerializeField] private AudioClip loopClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.3f;
        [Tooltip("ParticleSystem.Stopで消えるエフェクトは、そのParticleSystemを設定")]
        [SerializeField] private ParticleSystem watchedParticles;
        private AudioSource source;
        private bool paused;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.clip = loopClip;
            source.volume = volume;
        }
        private void OnEnable()
        {
            // Instantiateされる雨Prefabからシーン内の管理を解決する。
            if (stageDirector == null)
                stageDirector = FindFirstObjectByType<StageDirector>();
            if (stageDirector != null)
            {
                stageDirector.PhaseChanged += OnPhase;
                stageDirector.StageReset += StopSound;
            }
        }
        private void OnPhase(StagePhase phase)
        {
            if (phase == StagePhase.Paused)
            {
                if (source.isPlaying) { source.Pause(); paused = true; }
            }
            else if (phase != StagePhase.Playing) StopSound();
        }
        private void LateUpdate()
        {
            if (stageDirector == null || loopClip == null) { StopSound(); return; }
            if (stageDirector.Phase == StagePhase.Paused) return;
            bool visible = watchedParticles == null || watchedParticles.isPlaying;
            if (stageDirector.Phase != StagePhase.Playing || !visible)
            {
                StopSound();
                return;
            }
            if (paused) { source.UnPause(); paused = false; }
            if (!source.isPlaying) source.Play();
        }
        private void StopSound()
        {
            if (source != null) source.Stop();
            paused = false;
        }
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
