using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Tenyu.Audio;

public class TitleScreenController : MonoBehaviour, IPointerClickHandler
{
    [Header("次に開くシーン")]
    [SerializeField] private string nextSceneName = "TimelineScene";

    [Header("音")]
    [SerializeField] private TenyuSceneAudio sceneAudio;

    [Header("タップ案内")]
    [SerializeField] private CanvasGroup tapGuideGroup;
    [SerializeField] private float pulseSpeed = 2.5f;

    [Range(0f, 1f)]
    [SerializeField] private float minimumAlpha = 0.35f;

    [Header("画面切り替え")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.5f;

    private Image tapCatcherImage;
    private bool isTransitioning;

    private void Awake()
    {
        // ポーズ状態からタイトルに戻った場合への対策
        Time.timeScale = 1f;

        tapCatcherImage = GetComponent<Image>();

        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        AnimateTapGuide();
    }

    private void AnimateTapGuide()
    {
        if (tapGuideGroup == null || isTransitioning)
            return;

        float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;

        tapGuideGroup.alpha =
            Mathf.Lerp(minimumAlpha, 1f, wave);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isTransitioning || eventData.button != PointerEventData.InputButton.Left)
            return;

        if (sceneAudio == null) sceneAudio = TenyuSceneAudio.FindFor(this);
        if (sceneAudio != null) sceneAudio.PlayButtonSound();
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        isTransitioning = true;

        if (tapCatcherImage != null)
            tapCatcherImage.raycastTarget = false;

        if (tapGuideGroup != null)
            tapGuideGroup.alpha = 0f;

        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;

            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                fadeOverlay.alpha =
                    Mathf.Clamp01(elapsed / fadeDuration);

                yield return null;
            }

            fadeOverlay.alpha = 1f;
        }

        yield return SceneManager.LoadSceneAsync(nextSceneName);
    }
}
