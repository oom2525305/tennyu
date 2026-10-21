using UnityEngine;
using Tenyu.SceneFlow;

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

            return IsCleared(stageId - 1) || IsCleared(stageId - 1, StageDifficulty.Hard);
        }

        public static bool IsCleared(int stageId, StageDifficulty difficulty = StageDifficulty.Normal)
        {
            return PlayerPrefs.GetInt(
                GetClearKey(stageId, difficulty),
                0
            ) == 1;
        }

        public static void SetCleared(int stageId, StageDifficulty difficulty = StageDifficulty.Normal)
        {
            PlayerPrefs.SetInt(
                GetClearKey(stageId, difficulty),
                1
            );

            PlayerPrefs.Save();
        }

        public static void ResetCleared(int stageId, StageDifficulty difficulty = StageDifficulty.Normal)
        {
            PlayerPrefs.DeleteKey(
                GetClearKey(stageId, difficulty)
            );

            PlayerPrefs.Save();
        }

        private static string GetClearKey(int stageId, StageDifficulty difficulty)
        {
            return ClearKeyPrefix + stageId + (difficulty == StageDifficulty.Hard ? "_Hard" : "");
        }
    }
}