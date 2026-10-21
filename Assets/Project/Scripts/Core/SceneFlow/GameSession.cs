namespace Tenyu.SceneFlow
{
    public enum StageDifficulty { Normal, Hard }

    public static class GameSession
    {
        public static int SelectedStageId
        {
            get;
            private set;
        } = 1;

        public static StageDifficulty SelectedDifficulty { get; private set; }

        public static void SelectDifficulty(StageDifficulty difficulty)
        {
            SelectedDifficulty = difficulty;
        }

        public static void SelectStage(int stageId)
        {
            SelectedStageId =
                UnityEngine.Mathf.Max(1, stageId);
        }

        public static void Reset()
        {
            SelectedStageId = 1;
            SelectedDifficulty = StageDifficulty.Normal;
        }
    }
}