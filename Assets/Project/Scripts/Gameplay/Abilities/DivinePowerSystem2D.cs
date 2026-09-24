using System;
using System.Collections.Generic;
using UnityEngine;
using Tenyu.Gameplay;

public class DivinePowerSystem2D : MonoBehaviour
{
    private struct ThunderConductionNode
    {
        public DivinePowerReceiver2D Receiver;
        public float Strength;
        public int Depth;

        public ThunderConductionNode(
            DivinePowerReceiver2D receiver,
            float strength,
            int depth)
        {
            Receiver = receiver;
            Strength = strength;
            Depth = depth;
        }
    }

    [Header("ゲーム管理")]
    [SerializeField]
    private StageDirector stageDirector;

    [Header("発動範囲")]
    [SerializeField, Min(0.1f)]
    private float effectRadius = 2f;

    [SerializeField]
    private LayerMask receiverLayers;

    [Header("風")]
    [SerializeField, Min(0f)]
    private float windImpulse = 6f;

    [SerializeField, Min(0)]
    private int maxWindUses = 3;

    [Header("雷")]
    [SerializeField, Min(0f)]
    private float thunderStrength = 1f;

    [SerializeField, Min(0)]
    private int maxThunderUses = 2;

    [Header("濡れた対象への雷伝播")]
    [SerializeField, Min(0.1f)]
    private float wetConductionRadius = 2.5f;

    [SerializeField, Range(0f, 1f)]
    private float conductionStrengthMultiplier = 0.75f;

    [SerializeField, Min(0)]
    private int maximumConductionDepth = 3;

    [SerializeField, Min(0)]
    private int maximumConductionTargets = 8;

    [Header("雨")]
    [SerializeField, Min(0f)]
    private float rainStrength = 1f;

    [SerializeField, Min(0)]
    private int maxRainUses = 2;

    [SerializeField]
    private GameObject rainEffectPrefab;

    [Header("クールタイム（ゲーム内秒）")]
    [SerializeField, Min(0f)] private float windCooldown = 5f;
    [SerializeField, Min(0f)] private float thunderCooldown = 5f;
    [SerializeField, Min(0f)] private float rainCooldown = 5f;

    private float remainingWindCooldown;
    private float remainingThunderCooldown;
    private float remainingRainCooldown;

    private void Update()
    {
        if (!CanCastAbilities)
            return;

        float deltaTime = StageTime.DeltaTime;
        remainingGhostCooldown = Mathf.Max(0f, remainingGhostCooldown - deltaTime);
        remainingWindCooldown = Mathf.Max(0f, remainingWindCooldown - deltaTime);
        remainingThunderCooldown = Mathf.Max(0f, remainingThunderCooldown - deltaTime);
        remainingRainCooldown = Mathf.Max(0f, remainingRainCooldown - deltaTime);
    }

    public float GetCooldownDuration(DivinePowerType powerType)
    {
        switch (powerType)
        {
            case DivinePowerType.Ghost: return ghostCooldown;
            case DivinePowerType.Wind: return windCooldown;
            case DivinePowerType.Thunder: return thunderCooldown;
            case DivinePowerType.Rain: return rainCooldown;
            default: return 0f;
        }
    }

    public float GetRemainingCooldown(DivinePowerType powerType)
    {
        switch (powerType)
        {
            case DivinePowerType.Ghost: return remainingGhostCooldown;
            case DivinePowerType.Wind: return remainingWindCooldown;
            case DivinePowerType.Thunder: return remainingThunderCooldown;
            case DivinePowerType.Rain: return remainingRainCooldown;
            default: return 0f;
        }
    }

    public float GetCooldown01(DivinePowerType powerType)
    {
        float duration = GetCooldownDuration(powerType);
        return duration > 0f
            ? Mathf.Clamp01(GetRemainingCooldown(powerType) / duration)
            : 0f;
    }

    public bool CanCast(DivinePowerType powerType)
    {
        return CanCastAbilities &&
               (powerType != DivinePowerType.Ghost ||
                (ghostPrefab != null && ghostPrefab.gameObject.activeSelf && ghostPrefab.enabled)) &&
               GetRemainingUses(powerType) > 0 &&
               GetRemainingCooldown(powerType) <= 0f;
    }

    private void StartCooldown(DivinePowerType powerType)
    {
        switch (powerType)
        {
            case DivinePowerType.Ghost:
                remainingGhostCooldown = Mathf.Max(0f, ghostCooldown);
                break;
            case DivinePowerType.Wind:
                remainingWindCooldown = Mathf.Max(0f, windCooldown);
                break;
            case DivinePowerType.Thunder:
                remainingThunderCooldown = Mathf.Max(0f, thunderCooldown);
                break;
            case DivinePowerType.Rain:
                remainingRainCooldown = Mathf.Max(0f, rainCooldown);
                break;
        }
    }

    [Header("幽霊")]
    [SerializeField] private GhostLure2D ghostPrefab;
    [SerializeField, Min(0)] private int maxGhostUses = 2;
    [SerializeField, Min(0f)] private float ghostCooldown = 5f;
    [SerializeField, Min(0.1f)] private float ghostDuration = 4f;
    [SerializeField, Min(0.1f)] private float ghostRadius = 3f;
    private int remainingGhostUses;
    private float remainingGhostCooldown;
    private GhostLure2D activeGhost;

    public float GetEffectRadius(DivinePowerType powerType)
    {
        return powerType == DivinePowerType.Ghost ? ghostRadius : effectRadius;
    }

    private void ClearGhost()
    {
        if (activeGhost != null)
        {
            activeGhost.gameObject.SetActive(false);
            Destroy(activeGhost.gameObject);
            activeGhost = null;
        }
    }

    [Header("テスト")]
    [SerializeField]
    private DivinePowerType testPower;

    [SerializeField]
    private Vector2 testDirection =
        Vector2.right;

    public event Action<DivinePowerType, int>
        RemainingUsesChanged;

    private int remainingWindUses;
    private int remainingThunderUses;
    private int remainingRainUses;

    private readonly List<GameObject>
        spawnedEffects = new List<GameObject>();

    public float EffectRadius => effectRadius;

    public bool CanCastAbilities =>
        stageDirector != null &&
        stageDirector.Phase == StagePhase.Playing;

    private void Awake()
    {
        ResetUses();
    }

    private void OnEnable()
    {
        if (stageDirector != null)
        {
            stageDirector.StageReset +=
                HandleStageReset;
        }
    }

    private void OnDisable()
    {
        if (stageDirector != null)
        {
            stageDirector.StageReset -=
                HandleStageReset;
        }

        ClearSpawnedEffects();
    }

    public bool Cast(
        DivinePowerType powerType,
        Vector2 worldPosition,
        Vector2 direction)
    {
        if (!CanCast(powerType))
        {
            return false;
        }

        if (powerType == DivinePowerType.Ghost)
        {
            ClearGhost();
            activeGhost = Instantiate(ghostPrefab, worldPosition, Quaternion.identity);
            activeGhost.Initialize(stageDirector, ghostDuration, ghostRadius);
            StartCooldown(powerType);
            ConsumeUse(powerType);
            return true;
        }

        // 方向未指定の風は発動しない。
        if (powerType == DivinePowerType.Wind &&
            direction.sqrMagnitude <= 0.001f)
            return false;

        // 通知でUIが更新される前に、再使用を制限する。
        StartCooldown(powerType);
        ConsumeUse(powerType);

        if (powerType == DivinePowerType.Rain)
        {
            CreateRainEffect(worldPosition);
        }

        List<DivinePowerReceiver2D>
            directReceivers =
                FindReceivers(
                    worldPosition,
                    effectRadius
                );

        switch (powerType)
        {
            case DivinePowerType.Wind:
                ApplyWind(
                    directReceivers,
                    direction
                );
                break;

            case DivinePowerType.Thunder:
                ApplyThunder(
                    directReceivers
                );
                break;

            case DivinePowerType.Rain:
                ApplyRain(
                    directReceivers
                );
                break;
        }

        return true;
    }

    private List<DivinePowerReceiver2D>
        FindReceivers(
            Vector2 worldPosition,
            float radius)
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                worldPosition,
                radius,
                receiverLayers
            );

        HashSet<DivinePowerReceiver2D>
            uniqueReceivers =
                new HashSet
                    <DivinePowerReceiver2D>();

        List<DivinePowerReceiver2D>
            receivers =
                new List
                    <DivinePowerReceiver2D>();

        foreach (Collider2D hit in hits)
        {
            DivinePowerReceiver2D receiver =
                hit.GetComponentInParent
                    <DivinePowerReceiver2D>();

            if (receiver == null)
                continue;

            if (!uniqueReceivers.Add(receiver))
                continue;

            receivers.Add(receiver);
        }

        return receivers;
    }

    private void ApplyWind(
        List<DivinePowerReceiver2D> receivers,
        Vector2 direction)
    {
        foreach (
            DivinePowerReceiver2D receiver
            in receivers)
        {
            receiver.ReceiveWind(
                direction,
                windImpulse
            );
        }
    }

    private void ApplyRain(
        List<DivinePowerReceiver2D> receivers)
    {
        foreach (
            DivinePowerReceiver2D receiver
            in receivers)
        {
            receiver.ReceiveRain(
                rainStrength
            );
        }
    }

    private void ApplyThunder(
        List<DivinePowerReceiver2D>
            directReceivers)
    {
        HashSet<DivinePowerReceiver2D>
            processedReceivers =
                new HashSet
                    <DivinePowerReceiver2D>();

        Queue<ThunderConductionNode>
            conductionQueue =
                new Queue
                    <ThunderConductionNode>();

        // 最初の雷の範囲内にいる対象
        foreach (
            DivinePowerReceiver2D receiver
            in directReceivers)
        {
            receiver.ReceiveThunder(
                thunderStrength
            );

            processedReceivers.Add(receiver);

            WettableObject2D wettable =
                receiver.GetComponent
                    <WettableObject2D>();

            if (wettable != null &&
                wettable.IsWet)
            {
                conductionQueue.Enqueue(
                    new ThunderConductionNode(
                        receiver,
                        thunderStrength,
                        0
                    )
                );
            }
        }

        int conductedTargetCount = 0;

        while (conductionQueue.Count > 0)
        {
            ThunderConductionNode current =
                conductionQueue.Dequeue();

            if (current.Depth >=
                maximumConductionDepth)
            {
                continue;
            }

            if (conductedTargetCount >=
                maximumConductionTargets)
            {
                break;
            }

            List<DivinePowerReceiver2D>
                nearbyReceivers =
                    FindReceivers(
                        current.Receiver
                            .transform.position,
                        wetConductionRadius
                    );

            foreach (
                DivinePowerReceiver2D receiver
                in nearbyReceivers)
            {
                if (processedReceivers.Contains(
                    receiver))
                {
                    continue;
                }

                WettableObject2D wettable =
                    receiver.GetComponent
                        <WettableObject2D>();

                if (wettable == null ||
                    !wettable.IsWet)
                {
                    continue;
                }

                float conductedStrength =
                    current.Strength *
                    conductionStrengthMultiplier;

                if (conductedStrength <= 0.01f)
                {
                    continue;
                }

                receiver.ReceiveThunder(
                    conductedStrength
                );

                processedReceivers.Add(receiver);

                conductedTargetCount++;

                conductionQueue.Enqueue(
                    new ThunderConductionNode(
                        receiver,
                        conductedStrength,
                        current.Depth + 1
                    )
                );

                if (conductedTargetCount >=
                    maximumConductionTargets)
                {
                    break;
                }
            }
        }
    }

    private void CreateRainEffect(
        Vector2 worldPosition)
    {
        if (rainEffectPrefab == null)
            return;

        GameObject effectInstance = Instantiate(
            rainEffectPrefab,
            worldPosition,
            Quaternion.identity
        );

        spawnedEffects.Add(effectInstance);
    }

    private void HandleStageReset()
    {
        ClearSpawnedEffects();
        ResetUses();
    }

    private void ClearSpawnedEffects()
    {
        ClearGhost();
        for (int i = spawnedEffects.Count - 1;
            i >= 0;
            i--)
        {
            GameObject effect = spawnedEffects[i];

            if (effect != null)
            {
                Destroy(effect);
            }
        }

        spawnedEffects.Clear();
    }

    public int GetRemainingUses(
        DivinePowerType powerType)
    {
        switch (powerType)
        {
            case DivinePowerType.Wind:
                return remainingWindUses;

            case DivinePowerType.Ghost:
                return remainingGhostUses;

            case DivinePowerType.Thunder:
                return remainingThunderUses;

            case DivinePowerType.Rain:
                return remainingRainUses;

            default:
                return 0;
        }
    }

    private void ConsumeUse(
        DivinePowerType powerType)
    {
        switch (powerType)
        {
            case DivinePowerType.Ghost:
                remainingGhostUses = Mathf.Max(0, remainingGhostUses - 1);
                break;
            case DivinePowerType.Wind:
                remainingWindUses =
                    Mathf.Max(
                        remainingWindUses - 1,
                        0
                    );
                break;

            case DivinePowerType.Thunder:
                remainingThunderUses =
                    Mathf.Max(
                        remainingThunderUses - 1,
                        0
                    );
                break;

            case DivinePowerType.Rain:
                remainingRainUses =
                    Mathf.Max(
                        remainingRainUses - 1,
                        0
                    );
                break;
        }

        RemainingUsesChanged?.Invoke(
            powerType,
            GetRemainingUses(powerType)
        );
    }

    public void ResetUses()
    {
        ClearGhost();
        remainingGhostCooldown = 0f;
        remainingGhostUses = maxGhostUses;
        remainingWindCooldown = 0f;
        remainingThunderCooldown = 0f;
        remainingRainCooldown = 0f;

        remainingWindUses =
            maxWindUses;

        remainingThunderUses =
            maxThunderUses;

        remainingRainUses =
            maxRainUses;

        RemainingUsesChanged?.Invoke(DivinePowerType.Ghost, remainingGhostUses);

        RemainingUsesChanged?.Invoke(
            DivinePowerType.Wind,
            remainingWindUses
        );

        RemainingUsesChanged?.Invoke(
            DivinePowerType.Thunder,
            remainingThunderUses
        );

        RemainingUsesChanged?.Invoke(
            DivinePowerType.Rain,
            remainingRainUses
        );
    }

    [ContextMenu("Test Cast")]
    private void TestCast()
    {
        Cast(
            testPower,
            transform.position,
            testDirection
        );
    }

    [ContextMenu("Reset Uses")]
    private void TestResetUses()
    {
        ResetUses();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            new Color(
                0.2f,
                0.55f,
                1f,
                0.5f
            );

        Gizmos.DrawWireSphere(
            transform.position,
            effectRadius
        );
    }
}
