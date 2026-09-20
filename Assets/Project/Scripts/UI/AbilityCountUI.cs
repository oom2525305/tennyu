using TMPro;
using UnityEngine;

public class AbilityCountUI : MonoBehaviour
{
    [SerializeField] private DivinePowerSystem2D powerSystem;
    [SerializeField] private DivinePowerType powerType;

    [Header("UI")]
    [SerializeField] private TMP_Text countText;
    [SerializeField] private AbilityDragButton dragButton;

    private void OnEnable()
    {
        if (powerSystem == null)
        {
            return;
        }

        powerSystem.RemainingUsesChanged +=
            HandleRemainingUsesChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (powerSystem != null)
        {
            powerSystem.RemainingUsesChanged -=
                HandleRemainingUsesChanged;
        }
    }

    private void HandleRemainingUsesChanged(
        DivinePowerType changedPower,
        int remainingUses)
    {
        if (changedPower != powerType)
        {
            return;
        }

        UpdateUI(remainingUses);
    }

    private void Refresh()
    {
        UpdateUI(
            powerSystem.GetRemainingUses(powerType)
        );
    }

    private void UpdateUI(int remainingUses)
    {
        if (countText != null)
        {
            countText.text = $"×{remainingUses}";
        }

        if (dragButton != null)
        {
            dragButton.SetUsable(remainingUses > 0);
        }
    }
}