using UnityEngine;
using UnityEngine.SceneManagement;
using Tenyu.Gameplay;

namespace Tenyu.SceneFlow
{
    public sealed class SceneNavigator :
        MonoBehaviour
    {
        private const string TitleSceneName =
            "Title";

        private const string TimelineSceneName =
            "TimelineScene";

        private const string GameSceneName =
            "GameScene";

        private bool isLoading;

        public void LoadTitle()
        {
            GameSession.Reset();
            LoadScene(TitleSceneName);
        }

        public void LoadTimeline()
        {
            LoadScene(TimelineSceneName);
        }

        public void LoadGame(int stageId)
        {
            GameSession.SelectStage(stageId);
            LoadScene(GameSceneName);
        }

        public void ReloadCurrentScene()
        {
            Scene activeScene =
                SceneManager.GetActiveScene();

            LoadScene(activeScene.name);
        }

        private void LoadScene(string sceneName)
        {
            if (isLoading)
                return;

            isLoading = true;

            Time.timeScale = 1f;
            StageTime.Reset();

            SceneManager.LoadSceneAsync(
                sceneName
            );
        }
    }
}