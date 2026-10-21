using System;
using System.Collections.Generic;
using UnityEngine;
using Tenyu.Gameplay;

namespace Tenyu.Audio
{
    [DisallowMultipleComponent]
    public sealed class TenyuSceneAudio : MonoBehaviour
    {
        [Header("タイトル・年表では未設定でOK")]
        [SerializeField] private StageDirector stageDirector;
        [SerializeField] private DivinePowerSystem2D powerSystem;
        [Header("BGM")]
        [SerializeField] private AudioClip bgm;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] private float pausedBgmMultiplier = 0.4f;
        [Header("操作・結果")]
        [SerializeField] private AudioClip buttonSound;
        [SerializeField] private AudioClip beginSound;
        [SerializeField] private AudioClip clearSound;
        [SerializeField] private AudioClip failSound;
        [Header("能力（雨は雨エフェクト側でループ再生）")]
        [SerializeField] private AudioClip windSound;
        [SerializeField] private AudioClip thunderSound;
        [SerializeField] private AudioClip ghostSound;
        [SerializeField, Range(0f, 1f)] private float seVolume = 0.7f;

        private AudioSource music;
        private AudioSource ui;
        private AudioSource ability;
        private StagePhase previousPhase;
        private bool ready;
        private readonly Dictionary<DivinePowerType, int> counts =
            new Dictionary<DivinePowerType, int>();

        // 共通UI Prefabはシーン参照を保存できないため、同じシーン内だけで解決する。
        public static TenyuSceneAudio FindFor(Component owner)
        {
            foreach (TenyuSceneAudio candidate in
                FindObjectsByType<TenyuSceneAudio>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene == owner.gameObject.scene &&
                    candidate.isActiveAndEnabled)
                    return candidate;
            }
            return null;
        }

        private void Awake()
        {
            music = MakeSource(true);
            ui = MakeSource(false);
            ability = MakeSource(false);
        }

        private AudioSource MakeSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private void OnEnable()
        {
            TenyuAudioSettings.Changed += ApplyVolumes;
            ApplyVolumes();
            if (stageDirector != null)
            {
                stageDirector.PhaseChanged += OnPhaseChanged;
                stageDirector.StageReset += OnReset;
            }
            if (powerSystem != null)
                powerSystem.RemainingUsesChanged += OnUsesChanged;
            if (ready) Initialize();
        }

        private void Start()
        {
            ready = true;
            Initialize();
        }

        private void Initialize()
        {
            SnapshotCounts();
            previousPhase = stageDirector != null
                ? stageDirector.Phase : StagePhase.Observation;
            if (previousPhase != StagePhase.Cleared && previousPhase != StagePhase.Failed)
                StartMusic();
            if (previousPhase == StagePhase.Paused)
                ApplyVolumes();
        }

        private void SnapshotCounts()
        {
            counts.Clear();
            if (powerSystem == null) return;
            foreach (DivinePowerType type in Enum.GetValues(typeof(DivinePowerType)))
                counts[type] = powerSystem.GetRemainingUses(type);
        }

        private void StartMusic()
        {
            if (music == null) return;
            if (music.clip != bgm) music.clip = bgm;
            ApplyVolumes();
            if (bgm != null && !music.isPlaying) music.Play();
        }

        private void OnPhaseChanged(StagePhase phase)
        {
            StagePhase old = previousPhase;
            previousPhase = phase;
            if (!ready) return;
            ApplyVolumes();
            switch (phase)
            {
                case StagePhase.Observation:
                    StopTransientSounds();
                    StartMusic();
                    break;
                case StagePhase.Playing:
                    ability.UnPause();
                    if (old == StagePhase.Observation) PlayUI(beginSound);
                    break;
                case StagePhase.Paused:
                    ability.Pause();
                    ApplyVolumes();
                    break;
                case StagePhase.Cleared:
                    music.Stop();
                    ability.Stop();
                    ui.Stop();
                    PlayUI(clearSound);
                    break;
                case StagePhase.Failed:
                    music.Stop();
                    // 雷・着地など失敗原因となった短い音は最後まで鳴らす。
                    ability.UnPause();
                    ui.Stop();
                    PlayUI(failSound);
                    break;
            }
        }

        private void OnReset()
        {
            SnapshotCounts();
            if (!ready) return;
            StopTransientSounds();
            StartMusic();
        }

        private void OnUsesChanged(DivinePowerType type, int remaining)
        {
            bool consumed = counts.TryGetValue(type, out int previous) && remaining < previous;
            counts[type] = remaining;
            // 補充、初期化、ドラッグのキャンセルでは鳴らさない。
            if (!ready || !consumed || stageDirector == null ||
                stageDirector.Phase != StagePhase.Playing) return;
            AudioClip clip = null;
            switch (type)
            {
                case DivinePowerType.Wind: clip = windSound; break;
                case DivinePowerType.Thunder: clip = thunderSound; break;
                case DivinePowerType.Ghost: clip = ghostSound; break;
            }
            if (clip != null) ability.PlayOneShot(clip, seVolume);
        }

        public void PlayButtonSound() { PlayUI(buttonSound); }
        public void PlayUI(AudioClip clip)
        {
            if (ui != null && clip != null && isActiveAndEnabled)
                ui.PlayOneShot(clip, seVolume);
        }

        private void ApplyVolumes()
        {
            if (music != null) music.volume = bgmVolume * TenyuAudioSettings.Music *
                (stageDirector != null && stageDirector.Phase == StagePhase.Paused ? pausedBgmMultiplier : 1f);
            if (ui != null) ui.volume = TenyuAudioSettings.Effects;
            if (ability != null) ability.volume = TenyuAudioSettings.Effects;
        }

        private void StopTransientSounds()
        {
            ui.Stop();
            ability.Stop();
        }

        private void OnDisable()
        {
            TenyuAudioSettings.Changed -= ApplyVolumes;
            if (stageDirector != null)
            {
                stageDirector.PhaseChanged -= OnPhaseChanged;
                stageDirector.StageReset -= OnReset;
            }
            if (powerSystem != null)
                powerSystem.RemainingUsesChanged -= OnUsesChanged;
            if (music != null) music.Stop();
            if (ui != null) ui.Stop();
            if (ability != null) ability.Stop();
        }
    }
}
