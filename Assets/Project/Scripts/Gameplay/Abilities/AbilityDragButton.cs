using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tenyu.Gameplay;

public class AbilityDragButton :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("能力")]
    [SerializeField]
    private DivinePowerType powerType;

    [SerializeField]
    private DivinePowerSystem2D powerSystem;

    [Header("発動可能範囲")]
    [SerializeField]
    private RectTransform worldCastArea;

    [SerializeField]
    private Camera worldCamera;

    [SerializeField]
    private AbilityRangeIndicator2D rangeIndicator;

    [Header("方向指定")]
    [SerializeField]
    private bool requiresDirection = true;

    [SerializeField]
    private AbilityDirectionGuideUI directionGuide;

    [SerializeField, Min(0.05f)]
    private float placementLockDelay = 0.15f;

    [SerializeField, Min(1f)]
    private float stationaryRadius = 18f;

    [SerializeField, Min(1f)]
    private float directionThreshold = 35f;

    [Header("ドラッグ表示")]
    [SerializeField]
    private Image sourceIcon;

    [SerializeField]
    private CanvasGroup sourceCanvasGroup;

    [SerializeField]
    private Sprite dragCursorSprite;

    [SerializeField]
    private Color dragCursorColor = Color.white;

    [SerializeField]
    private Vector2 dragIconSize =
        new Vector2(64f, 64f);

    private Canvas rootCanvas;
    private RectTransform dragIconRect;

    private bool isDragging;
    private bool isUsable = true;
    private bool lastInteractionState;

    private bool pointerInsideWorld;
    private bool placementCandidateExists;
    private bool castPositionLocked;

    private float stationaryTimer;

    private Vector2 currentPointerPosition;
    private Vector2 placementCandidatePosition;
    private Vector2 lockedScreenPosition;
    private Vector2 lockedWorldPosition;
    private Vector2 selectedScreenDirection;

    private readonly List<RectTransform> blockingUiRects = new List<RectTransform>();

    private void Start()
    {
        CacheBlockingUi();
    }

    private void CacheBlockingUi()
    {
        blockingUiRects.Clear();
        if (rootCanvas == null) return;
        foreach (Selectable control in rootCanvas.GetComponentsInChildren<Selectable>(true))
            AddBlockingUi(control.transform as RectTransform);
        foreach (AbilityDragButton ability in rootCanvas.GetComponentsInChildren<AbilityDragButton>(true))
        {
            AddBlockingUi(ability.transform as RectTransform);
            foreach (TMPro.TMP_Text label in ability.GetComponentsInChildren<TMPro.TMP_Text>(true))
                AddBlockingUi(label.rectTransform);
        }
    }

    private void AddBlockingUi(RectTransform rect)
    {
        if (rect != null && !blockingUiRects.Contains(rect)) blockingUiRects.Add(rect);
    }

    private void Awake()
    {
        Canvas parentCanvas =
            GetComponentInParent<Canvas>();

        if (parentCanvas != null)
        {
            rootCanvas =
                parentCanvas.rootCanvas;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (sourceIcon == null)
        {
            sourceIcon =
                GetComponent<Image>();
        }

        if (sourceCanvasGroup == null)
        {
            sourceCanvasGroup =
                GetComponent<CanvasGroup>();
        }

        RefreshInteractionState();
    }

    private void OnEnable()
    {
        RefreshInteractionState();
    }

    private void Update()
    {
        bool canInteract = CanInteract();

        if (canInteract != lastInteractionState)
        {
            RefreshInteractionState();
        }

        // リトライ・ポーズ・失敗などでPlaying以外になったら
        // ドラッグ中のカーソルと範囲表示を即座に消す
        if (!canInteract)
        {
            if (isDragging)
            {
                FinishDrag();
            }

            return;
        }

        if (!isDragging ||
            !requiresDirection ||
            castPositionLocked ||
            !pointerInsideWorld ||
            !placementCandidateExists)
        {
            return;
        }

        stationaryTimer +=
            Time.unscaledDeltaTime;

        if (stationaryTimer >=
            placementLockDelay)
        {
            LockCastPosition(
                currentPointerPosition
            );
        }
    }

    public void SetUsable(bool usable)
    {
        isUsable = usable;

        if (!CanInteract() && isDragging)
        {
            FinishDrag();
        }

        RefreshInteractionState();
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (!CanInteract() ||
            powerSystem == null ||
            rootCanvas == null)
        {
            return;
        }

        isDragging = true;

        StageTime.BeginAbilityDrag();

        currentPointerPosition =
            eventData.position;

        pointerInsideWorld = false;
        placementCandidateExists = false;
        castPositionLocked = false;
        stationaryTimer = 0f;
        selectedScreenDirection =
            Vector2.zero;

        if (directionGuide != null)
        {
            directionGuide.Hide();
        }

        HideRangeIndicator();

        CreateDragIcon();
        MoveDragIcon(eventData.position);

        if (sourceCanvasGroup != null)
        {
            sourceCanvasGroup.alpha = 0.45f;
        }
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        currentPointerPosition =
            eventData.position;

        pointerInsideWorld =
            IsInsideWorldArea(
                eventData.position,
                eventData.pressEventCamera
            );

        // 雷や雨など方向指定が不要な能力
        if (!requiresDirection)
        {
            MoveDragIcon(eventData.position);

            UpdateRangeIndicator(
                eventData.position
            );

            return;
        }

        // 方向指定地点が固定済み
        if (castPositionLocked)
        {
            MoveDragIcon(
                lockedScreenPosition
            );

            ShowLockedRangeIndicator();

            UpdateSelectedDirection(
                eventData.position
            );

            return;
        }

        MoveDragIcon(eventData.position);

        if (!pointerInsideWorld)
        {
            ResetPlacementCandidate();
            HideRangeIndicator();
            return;
        }

        UpdateRangeIndicator(
            eventData.position
        );

        if (!placementCandidateExists)
        {
            BeginPlacementCandidate(
                eventData.position
            );

            return;
        }

        float distanceFromCandidate =
            Vector2.Distance(
                eventData.position,
                placementCandidatePosition
            );

        if (distanceFromCandidate >
            stationaryRadius * rootCanvas.scaleFactor)
        {
            BeginPlacementCandidate(
                eventData.position
            );
        }
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        if (!CanInteract() || !IsInsideWorldArea(eventData.position, eventData.pressEventCamera))
        {
            FinishDrag();
            return;
        }

        if (requiresDirection)
        {
            EndDirectionalDrag(
                eventData.position
            );
        }
        else
        {
            EndNormalDrag(
                eventData.position,
                eventData.pressEventCamera
            );
        }

        FinishDrag();
    }

    private void EndDirectionalDrag(
        Vector2 pointerPosition)
    {
        if (!castPositionLocked)
        {
            return;
        }

        UpdateSelectedDirection(
            pointerPosition
        );

        if (selectedScreenDirection ==
            Vector2.zero)
        {
            return;
        }

        Vector2 worldDirection =
            ConvertScreenDirectionToWorld(
                selectedScreenDirection
            );

        powerSystem.Cast(
            powerType,
            lockedWorldPosition,
            worldDirection
        );
    }

    private void EndNormalDrag(
        Vector2 pointerPosition,
        Camera eventCamera)
    {
        bool isInsideWorld =
            IsInsideWorldArea(
                pointerPosition,
                eventCamera
            );

        if (!isInsideWorld)
        {
            return;
        }

        if (!TryGetWorldPosition(
            pointerPosition,
            out Vector2 worldPosition))
        {
            return;
        }

        powerSystem.Cast(
            powerType,
            worldPosition,
            Vector2.right
        );
    }

    private void BeginPlacementCandidate(
        Vector2 screenPosition)
    {
        placementCandidateExists = true;
        placementCandidatePosition =
            screenPosition;

        stationaryTimer = 0f;
    }

    private void ResetPlacementCandidate()
    {
        placementCandidateExists = false;
        stationaryTimer = 0f;
    }

    private void LockCastPosition(
        Vector2 screenPosition)
    {
        if (!TryGetWorldPosition(
            screenPosition,
            out Vector2 worldPosition))
        {
            ResetPlacementCandidate();
            return;
        }

        castPositionLocked = true;

        lockedScreenPosition =
            screenPosition;

        lockedWorldPosition =
            worldPosition;

        selectedScreenDirection =
            Vector2.zero;

        MoveDragIcon(
            lockedScreenPosition
        );

        ShowLockedRangeIndicator();

        if (directionGuide != null)
        {
            directionGuide.Show(
                lockedScreenPosition,
                rootCanvas
            );

            directionGuide.SetDirection(
                Vector2.zero
            );
        }
    }

    private void UpdateSelectedDirection(
        Vector2 pointerPosition)
    {
        Vector2 offset =
            pointerPosition -
            lockedScreenPosition;

        if (offset.magnitude < directionThreshold * rootCanvas.scaleFactor)
        {
            selectedScreenDirection =
                Vector2.zero;

            if (directionGuide != null)
            {
                directionGuide.SetDirection(
                    Vector2.zero
                );
            }

            return;
        }

        selectedScreenDirection = offset.normalized;

        if (directionGuide != null)
        {
            directionGuide.SetDirection(
                selectedScreenDirection
            );
        }
    }

    private bool IsInsideWorldArea(
        Vector2 screenPosition,
        Camera eventCamera)
    {
        if (worldCastArea == null)
        {
            return false;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(worldCastArea, screenPosition, eventCamera))
            return false;
        foreach (RectTransform rect in blockingUiRects)
        {
            if (rect != null && rect.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera))
                return false;
        }
        return true;
    }

    private void UpdateRangeIndicator(
        Vector2 screenPosition)
    {
        if (rangeIndicator == null ||
            powerSystem == null ||
            !pointerInsideWorld)
        {
            HideRangeIndicator();
            return;
        }

        if (!TryGetWorldPosition(
            screenPosition,
            out Vector2 worldPosition))
        {
            HideRangeIndicator();
            return;
        }

        rangeIndicator.Show(
            worldPosition,
            powerSystem.GetEffectRadius(powerType)
        );
    }

    private void ShowLockedRangeIndicator()
    {
        if (rangeIndicator == null ||
            powerSystem == null)
        {
            return;
        }

        rangeIndicator.Show(
            lockedWorldPosition,
            powerSystem.GetEffectRadius(powerType)
        );
    }

    private void HideRangeIndicator()
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.Hide();
        }
    }

    private bool CanInteract()
    {
        return isUsable &&
               powerSystem != null &&
               powerSystem.CanCast(powerType);
    }

    private void RefreshInteractionState()
    {
        bool canInteract = CanInteract();
        lastInteractionState = canInteract;

        if (sourceCanvasGroup == null)
        {
            return;
        }

        sourceCanvasGroup.interactable =
            canInteract;

        sourceCanvasGroup.blocksRaycasts =
            canInteract;

        if (!isDragging)
        {
            sourceCanvasGroup.alpha =
                canInteract ? 1f : 0.4f;
        }
    }

    private void CreateDragIcon()
    {
        if (dragCursorSprite == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}に" +
                "ドラッグカーソル画像が" +
                "設定されていません。"
            );

            return;
        }

        GameObject dragObject =
            new GameObject(
                $"{powerType}DragCursor",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        dragObject.transform.SetParent(
            rootCanvas.transform,
            false
        );

        dragIconRect =
            dragObject.GetComponent
                <RectTransform>();

        dragIconRect.sizeDelta =
            dragIconSize;

        dragIconRect.SetAsLastSibling();

        Image cursorImage =
            dragObject.GetComponent<Image>();

        cursorImage.sprite =
            dragCursorSprite;

        cursorImage.color =
            dragCursorColor;

        cursorImage.preserveAspect = true;
        cursorImage.raycastTarget = false;
    }

    private void MoveDragIcon(
        Vector2 screenPosition)
    {
        if (dragIconRect == null)
        {
            return;
        }

        RectTransform canvasRect =
            rootCanvas.transform
                as RectTransform;

        Camera canvasCamera =
            rootCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                canvasCamera,
                out Vector2 localPosition))
        {
            dragIconRect.anchoredPosition =
                localPosition;
        }
    }

    private bool TryGetWorldPosition(
        Vector2 screenPosition,
        out Vector2 worldPosition)
    {
        worldPosition = Vector2.zero;

        if (worldCamera == null)
        {
            return false;
        }

        Ray ray =
            worldCamera.ScreenPointToRay(
                screenPosition
            );

        Plane gamePlane =
            new Plane(
                Vector3.forward,
                Vector3.zero
            );

        if (!gamePlane.Raycast(
            ray,
            out float distance))
        {
            return false;
        }

        worldPosition =
            ray.GetPoint(distance);

        return true;
    }

    private Vector2
        ConvertScreenDirectionToWorld(
            Vector2 screenDirection)
    {
        if (screenDirection.sqrMagnitude <=
            0.001f)
        {
            return Vector2.right;
        }

        Vector3 worldDirection =
            worldCamera.transform.right *
            screenDirection.x +
            worldCamera.transform.up *
            screenDirection.y;

        return new Vector2(
            worldDirection.x,
            worldDirection.y
        ).normalized;
    }

    private void FinishDrag()
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        StageTime.EndAbilityDrag();

        if (dragIconRect != null)
        {
            Destroy(
                dragIconRect.gameObject
            );

            dragIconRect = null;
        }

        if (directionGuide != null)
        {
            directionGuide.Hide();
        }

        HideRangeIndicator();

        pointerInsideWorld = false;
        placementCandidateExists = false;
        castPositionLocked = false;
        stationaryTimer = 0f;

        selectedScreenDirection =
            Vector2.zero;

        if (sourceCanvasGroup != null)
        {
            RefreshInteractionState();
        }
    }

    private void OnDisable()
    {
        if (isDragging)
        {
            FinishDrag();
        }
        else
        {
            HideRangeIndicator();
        }

        RefreshInteractionState();
    }
}
