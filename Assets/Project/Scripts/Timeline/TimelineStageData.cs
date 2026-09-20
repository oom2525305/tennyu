using UnityEngine;

namespace Tenyu.Timeline
{
    [CreateAssetMenu(
        fileName = "StageData_01",
        menuName = "Tenyu/Timeline Stage")]
    public sealed class TimelineStageData :
        ScriptableObject
    {
        [Header("識別情報")]
        [SerializeField, Min(1)]
        private int stageId = 1;

        [Header("表示情報")]
        [SerializeField]
        private string stageName = "桶狭間の戦い";

        [SerializeField]
        private string year = "1560年";

        [SerializeField, TextArea(3, 6)]
        private string description =
            "織田信長を目的地まで導く。";

        [SerializeField, Range(1, 5)]
        private int difficulty = 1;

        [SerializeField]
        private Sprite stageIcon;

        public int StageId => stageId;
        public string StageName => stageName;
        public string Year => year;
        public string Description => description;
        public int Difficulty => difficulty;
        public Sprite StageIcon => stageIcon;
    }
}