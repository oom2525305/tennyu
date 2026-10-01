using System;
using UnityEngine;
using Tenyu.SceneFlow;

namespace Tenyu.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StageLoader : MonoBehaviour
    {
        [Serializable]
        private sealed class StageEntry
        {
            [Min(1)]
            public int stageId = 1;

            public GameObject stagePrefab;
            public GameObject hardStagePrefab;
        }

        [Header("ステージ一覧")]
        [SerializeField]
        private StageEntry[] stages = new StageEntry[0];

        [Header("GameSceneを直接再生するときのテスト")]
        [Tooltip("オンの場合、年表で選んだIDよりテスト用IDを優先します。")]
        [SerializeField]
        private bool useEditorTestStage;

        [SerializeField, Min(1)]
        private int editorTestStageId = 1;

        [SerializeField] private StageDifficulty editorTestDifficulty;

        public GameObject CurrentStage { get; private set; }

        private void Awake()
        {
            int stageId = GameSession.SelectedStageId;

#if UNITY_EDITOR
            if (useEditorTestStage)
            {
                stageId = editorTestStageId;
                GameSession.SelectDifficulty(editorTestDifficulty);
            }
#endif

            GameObject prefab = FindStagePrefab(stageId);

            if (prefab == null)
            {
                Debug.LogError(
                    $"ステージID {stageId}（{GameSession.SelectedDifficulty}）のPrefabが未登録です。" +
                    "StageLoaderのStagesを確認してください。",
                    this
                );

                return;
            }

            if (!prefab.activeSelf)
            {
                Debug.LogError(
                    $"{prefab.name} のルートが非アクティブです。" +
                    "Prefab編集画面でルートのチェックをオンにしてください。",
                    this
                );

                return;
            }

            // クリア記録なども、実際に読み込むステージIDにそろえる。
            GameSession.SelectStage(stageId);

            CurrentStage = Instantiate(
                prefab,
                Vector3.zero,
                Quaternion.identity
            );

            CurrentStage.name = prefab.name;

            Debug.Log(
                $"ステージ {stageId}（{GameSession.SelectedDifficulty}）を読み込みました。",
                this
            );
        }

        private GameObject FindStagePrefab(int stageId)
        {
            if (stages == null)
            {
                return null;
            }

            GameObject result = null;
            bool found = false;

            foreach (StageEntry entry in stages)
            {
                if (entry == null || entry.stageId != stageId)
                {
                    continue;
                }

                if (found)
                {
                    Debug.LogError(
                        $"ステージID {stageId} が重複しています。",
                        this
                    );

                    return null;
                }

                found = true;
                result = GameSession.SelectedDifficulty == StageDifficulty.Hard
                    ? entry.hardStagePrefab : entry.stagePrefab;
            }

            return result;
        }
    }
}