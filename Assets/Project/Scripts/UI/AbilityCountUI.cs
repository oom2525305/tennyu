using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityCountUI : MonoBehaviour
{
    [SerializeField] private DivinePowerSystem2D powerSystem;
    [SerializeField] private DivinePowerType powerType;

    [Header("UI")]
    [SerializeField] private TMP_Text countText;
    [SerializeField] private AbilityDragButton dragButton;

    [Header("クールタイム表示（ボタンの子を設定）")]
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TMP_Text cooldownText;

    private void Awake()
    {
        if (cooldownFill != null)
        {
            cooldownFill.raycastTarget = false;
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = (int)Image.Origin360.Top;
            cooldownFill.fillClockwise = true;
        }

        if (cooldownText != null)
            cooldownText.raycastTarget = false;
    }

    private void OnEnable()
    {
        if (powerSystem != null)
            powerSystem.RemainingUsesChanged += HandleRemainingUsesChanged;

        Refresh();
    }

    private void Start()
    {
        // 他オブジェクトのAwakeでの残数初期化を反映。
        Refresh();
    }

    private void LateUpdate()
    {
        // ポーズ中でも表示は更新。回復時間はPowerSystem側で管理。
        Refresh();
    }

    private void OnDisable()
    {
        if (powerSystem != null)
            powerSystem.RemainingUsesChanged -= HandleRemainingUsesChanged;
    }

    private void HandleRemainingUsesChanged(
        DivinePowerType changedPower, int remainingUses)
    {
        if (changedPower == powerType)
            Refresh();
    }

    private void Refresh()
    {
        int uses = powerSystem != null
            ? powerSystem.GetRemainingUses(powerType) : 0;

        float remaining = powerSystem != null
            ? powerSystem.GetRemainingCooldown(powerType) : 0f;

        if (countText != null)
            countText.text = $"×{uses}";

        if (dragButton != null)
            dragButton.SetUsable(uses > 0);

        // 使い切った後は回復待ちと誤解されないよう秒数を隠す。
        bool showCooldown = uses > 0 && remaining > 0f;

        if (cooldownFill != null)
        {
            cooldownFill.enabled = showCooldown;
            cooldownFill.fillAmount = showCooldown
                ? powerSystem.GetCooldown01(powerType) : 0f;
        }

        if (cooldownText != null)
        {
            cooldownText.enabled = showCooldown;
            cooldownText.text = showCooldown
                ? Mathf.CeilToInt(remaining).ToString() : "";
        }
    }
}
