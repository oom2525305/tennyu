namespace Tenyu.SceneFlow
{
    public static class GameSession
    {
        public static int SelectedStageId
        {
            get;
            private set;
        } = 1;

        public static void SelectStage(int stageId)
        {
            SelectedStageId =
                UnityEngine.Mathf.Max(1, stageId);
        }

        public static void Reset()
        {
            SelectedStageId = 1;
        }
    }
}