using UnityEngine;

namespace Tenyu.Timeline
{
    public static class StageProgress
    {
        private const string ClearKeyPrefix =
            "StageCleared_";

        public static bool IsCleared(int stageId)
        {
            return PlayerPrefs.GetInt(
                GetClearKey(stageId),
                0
            ) == 1;
        }

        public static void SetCleared(int stageId)
        {
            PlayerPrefs.SetInt(
                GetClearKey(stageId),
                1
            );

            PlayerPrefs.Save();
        }

        public static void ResetCleared(int stageId)
        {
            PlayerPrefs.DeleteKey(
                GetClearKey(stageId)
            );

            PlayerPrefs.Save();
        }

        private static string GetClearKey(
            int stageId)
        {
            return ClearKeyPrefix + stageId;
        }
    }
}