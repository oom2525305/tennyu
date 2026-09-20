using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class GameInitializer : MonoBehaviour
{
    private static GameInitializer instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        ApplySettings();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplySettings();
        }
    }

    private void ApplySettings()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        Time.fixedDeltaTime = 1f / 60f;

        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;

        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;

        Screen.orientation = ScreenOrientation.AutoRotation;
    }
}