using UnityEngine;

namespace Tenyu.Timeline
{
    public static class StageProgress
    {
        private const string ClearKeyPrefix =
            "StageCleared_";

        // ステージ1は常に解放。
        // それ以降は直前のステージのクリアが条件。
        public static bool IsUnlocked(int stageId)
        {
            if (stageId < 1)
                return false;

            if (stageId == 1)
                return true;

            return IsCleared(stageId - 1);
        }

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

        private static string GetClearKey(int stageId)
        {
            return ClearKeyPrefix + stageId;
        }
    }
}