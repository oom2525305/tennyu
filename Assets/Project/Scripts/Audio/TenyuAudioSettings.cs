using System;
using UnityEngine;

namespace Tenyu.Audio
{
    public static class TenyuAudioSettings
    {
        private const string MusicKey = "Tenyu.Audio.Music";
        private const string EffectsKey = "Tenyu.Audio.Effects";
        public static event Action Changed;
        public static float Music => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
        public static float Effects => Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, 1f));
        public static void SetMusic(float value) { Set(MusicKey, value); }
        public static void SetEffects(float value) { Set(EffectsKey, value); }
        private static void Set(string key, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            Changed?.Invoke();
        }
        public static void Save() { PlayerPrefs.Save(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEvents() { Changed = null; }
    }
}
