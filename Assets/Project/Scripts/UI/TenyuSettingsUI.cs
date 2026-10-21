using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tenyu.Audio;
using Tenyu.Gameplay;

namespace Tenyu.UI
{
    // シーンのAudioに配置。UIだけを生成し、AudioやEventSystemは増やさない。
    [DisallowMultipleComponent]
    public sealed class TenyuSettingsUI : MonoBehaviour
    {
        [SerializeField] private StageDirector stageDirector;
        [SerializeField] private TenyuSceneAudio sceneAudio;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite buttonSprite;
        private GameObject overlay;
        private Button openButton;
        private Slider musicSlider, effectsSlider;
        private TMP_Text musicValue, effectsValue;
        private bool resumeOnClose;
        private GameObject previousSelection;
        private readonly Color ink = new Color(0.16f, 0.13f, 0.1f, 1f);

        private void Start()
        {
            RectTransform root = Rect("SettingsCanvas", transform, Vector2.zero, Vector2.zero);
            Canvas canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;
            root.gameObject.AddComponent<GraphicRaycaster>();
            openButton = Button(root, "設定", new Vector2(-174, -44), new Vector2(108, 52), Open);
            var openRect = (RectTransform)openButton.transform;
            openRect.anchorMin = openRect.anchorMax = Vector2.one;

            RectTransform shade = Rect("SettingsOverlay", root, Vector2.zero, Vector2.zero);
            Stretch(shade);
            shade.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.72f);
            overlay = shade.gameObject;
            RectTransform panel = Rect("SettingsPanel", shade, Vector2.zero, new Vector2(760, 480));
            Image paper = panel.gameObject.AddComponent<Image>();
            paper.sprite = panelSprite;
            paper.color = new Color(0.96f, 0.93f, 0.85f);
            Text(panel, "音の設定", new Vector2(0, 145), new Vector2(500, 56), 32);
            musicSlider = VolumeRow(panel, "BGM", 65, out musicValue);
            effectsSlider = VolumeRow(panel, "SE", -35, out effectsValue);
            musicSlider.onValueChanged.AddListener(value => { TenyuAudioSettings.SetMusic(value); RefreshLabels(); });
            effectsSlider.onValueChanged.AddListener(value => { TenyuAudioSettings.SetEffects(value); RefreshLabels(); });
            Button preview = Button(panel, "音を試す", new Vector2(-155, -150), new Vector2(220, 64), () => sceneAudio?.PlayButtonSound());
            Button close = Button(panel, "閉じる", new Vector2(155, -150), new Vector2(220, 64), Close);
            // キーボード・ゲームパッドでも背面の挑戦／リトライへ選択を逃がさない。
            musicSlider.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = effectsSlider };
            effectsSlider.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = musicSlider, selectOnDown = preview };
            preview.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = effectsSlider, selectOnRight = close };
            close.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = effectsSlider, selectOnLeft = preview };
            overlay.SetActive(false);
        }

        private void Update()
        {
            // ドラッグ中に設定ボタンが能力配置と競合しないようにする。
            if (openButton != null) openButton.interactable = stageDirector == null || !stageDirector.IsAiming;
        }
        public void Open()
        {
            if (overlay == null || overlay.activeSelf) return;
            resumeOnClose = stageDirector != null && stageDirector.Phase == StagePhase.Playing;
            if (resumeOnClose) stageDirector.PauseStage();
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            musicSlider.SetValueWithoutNotify(TenyuAudioSettings.Music);
            effectsSlider.SetValueWithoutNotify(TenyuAudioSettings.Effects);
            RefreshLabels();
            overlay.SetActive(true);
            musicSlider.Select();
            sceneAudio?.PlayButtonSound();
        }
        public void Close()
        {
            TenyuAudioSettings.Save();
            overlay.SetActive(false);
            if (resumeOnClose && stageDirector != null && stageDirector.Phase == StagePhase.Paused)
                stageDirector.ResumeStage();
            resumeOnClose = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
            sceneAudio?.PlayButtonSound();
        }
        private void OnApplicationFocus(bool focus) { if (!focus) TenyuAudioSettings.Save(); }
        private void OnDestroy() { TenyuAudioSettings.Save(); }
        private void RefreshLabels()
        {
            musicValue.text = Mathf.RoundToInt(TenyuAudioSettings.Music * 100) + "%";
            effectsValue.text = Mathf.RoundToInt(TenyuAudioSettings.Effects * 100) + "%";
        }
        private Slider VolumeRow(Transform parent, string label, float y, out TMP_Text valueText)
        {
            Text(parent, label, new Vector2(-255, y), new Vector2(130, 52), 28);
            valueText = Text(parent, "100%", new Vector2(265, y), new Vector2(110, 52), 26);
            RectTransform area = Rect(label + "Slider", parent, new Vector2(0, y), new Vector2(350, 52));
            Image hit = area.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            RectTransform track = Rect("Track", area, Vector2.zero, new Vector2(350, 8));
            track.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.3f, 0.22f);
            RectTransform handleArea = Rect("HandleArea", area, Vector2.zero, new Vector2(314, 52));
            RectTransform handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(36, 44));
            Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color(0.5f, 0.2f, 0.12f);
            Slider slider = area.gameObject.AddComponent<Slider>();
            slider.handleRect = handle; slider.targetGraphic = handleImage;
            slider.minValue = 0; slider.maxValue = 1; slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }
        private Button Button(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Rect(label, parent, position, size);
            Image image = rect.gameObject.AddComponent<Image>(); image.sprite = buttonSprite;
            image.color = new Color(0.9f, 0.86f, 0.75f);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(action);
            Text(rect, label, Vector2.zero, size, 24);
            return button;
        }
        private TMP_Text Text(Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
        {
            var label = Rect(text, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.fontSize = fontSize;
            label.color = ink; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
