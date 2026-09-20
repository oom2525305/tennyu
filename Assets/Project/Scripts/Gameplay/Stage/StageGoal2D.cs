using Tenyu.Gameplay;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StageGoal2D : MonoBehaviour
{
    [Header("ステージ管理")]
    [SerializeField] private StageDirector stageDirector;

    [Header("ゴール対象")]
    [SerializeField] private ProtectedTarget2D protectedTarget;

    private void Reset()
    {
        Collider2D goalCollider = GetComponent<Collider2D>();
        goalCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProtectedTarget2D enteredTarget =
            other.GetComponentInParent<ProtectedTarget2D>();

        if (enteredTarget == null)
        {
            return;
        }

        if (protectedTarget != null &&
            enteredTarget != protectedTarget)
        {
            return;
        }

        stageDirector.ClearStage();
    }
}