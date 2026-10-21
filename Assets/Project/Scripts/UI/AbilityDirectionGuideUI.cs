using UnityEngine;
using UnityEngine.UI;

public class AbilityDirectionGuideUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private RectTransform guideRoot;

    [SerializeField]
    private Image arrowUp;

    [SerializeField]
    private Image arrowDown;

    [SerializeField]
    private Image arrowLeft;

    [SerializeField]
    private Image arrowRight;

    [Header("表示設定")]
    [SerializeField]
    private Color normalColor =
        new Color(1f, 1f, 1f, 0.35f);

    [SerializeField]
    private Color selectedColor =
        new Color(1f, 1f, 1f, 1f);

    [SerializeField, Min(1f)]
    private float selectedScale = 1.15f;

    private void Awake()
    {
        if (guideRoot == null)
        {
            guideRoot = transform as RectTransform;
        }

        SetDirection(Vector2.zero);
        Hide();
    }

    public void Show(
        Vector2 screenPosition,
        Canvas rootCanvas)
    {
        if (guideRoot == null ||
            rootCanvas == null)
        {
            return;
        }

        guideRoot.gameObject.SetActive(true);
        guideRoot.SetAsLastSibling();

        RectTransform canvasRect =
            rootCanvas.transform as RectTransform;

        Camera canvasCamera =
            rootCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

        bool converted =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    canvasCamera,
                    out Vector2 localPosition
                );

        if (converted)
        {
            guideRoot.anchoredPosition =
                localPosition;
        }

        SetDirection(Vector2.zero);
    }

    public void Hide()
    {
        if (guideRoot != null)
        {
            guideRoot.gameObject.SetActive(false);
        }
    }

    public void SetDirection(Vector2 direction)
    {
        bool aiming = direction.sqrMagnitude > 0.001f;
        if (guideRoot != null)
            guideRoot.localRotation = aiming
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f)
                : Quaternion.identity;
        if (arrowUp != null) arrowUp.gameObject.SetActive(true);
        if (arrowDown != null) arrowDown.gameObject.SetActive(!aiming);
        if (arrowLeft != null) arrowLeft.gameObject.SetActive(!aiming);
        if (arrowRight != null) arrowRight.gameObject.SetActive(!aiming);
        SetArrowState(arrowUp, aiming);
        SetArrowState(arrowDown, false);
        SetArrowState(arrowLeft, false);
        SetArrowState(arrowRight, false);
    }

    private void SetArrowState(
        Image arrow,
        bool selected)
    {
        if (arrow == null)
        {
            return;
        }

        arrow.color =
            selected
                ? selectedColor
                : normalColor;

        float scale =
            selected ? selectedScale : 1f;

        arrow.rectTransform.localScale =
            new Vector3(scale, scale, 1f);
    }
}