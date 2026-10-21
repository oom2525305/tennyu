using System;
using UnityEngine;
using UnityEngine.Events;
using Tenyu.Gameplay;

[Serializable]
public sealed class DivinePowerInfluenceSettings
{
    [Header("0 = 無効 / 1 = 標準 / 2 = 2倍")]

    [SerializeField, Range(0f, 2f)]
    private float wind = 1f;

    [SerializeField, Range(0f, 2f)]
    private float thunder = 1f;

    [SerializeField, Range(0f, 2f)]
    private float rain = 1f;

    [SerializeField, Range(0f, 2f)]
    private float ghost = 1f;

    public float GetMultiplier(DivinePowerType powerType)
    {
        switch (powerType)
        {
            case DivinePowerType.Wind:
                return wind;

            case DivinePowerType.Thunder:
                return thunder;

            case DivinePowerType.Rain:
                return rain;

            case DivinePowerType.Ghost:
                return ghost;

            default:
                return 0f;
        }
    }
}

[Serializable]
public sealed class DivinePowerStrengthEvent : UnityEvent<float>
{
}

public class DivinePowerReceiver2D : MonoBehaviour
{
    [Header("能力の影響倍率")]
    [SerializeField]
    private DivinePowerInfluenceSettings influence =
        new DivinePowerInfluenceSettings();

    [Header("移動用参照")]
    [SerializeField]
    private Rigidbody2D targetRigidbody;

    [SerializeField]
    private RouteWalker2D routeWalker;

    [Header("能力を受けたときのイベント")]
    [SerializeField]
    private DivinePowerStrengthEvent onThunder;

    [SerializeField]
    private DivinePowerStrengthEvent onRain;

    [SerializeField]
    private DivinePowerStrengthEvent onGhost;

    // A custom mover handles the impulse instead of Rigidbody force when subscribed.
    public event Action<Vector2, float> WindReceived;
    public event Action<float> ThunderReceived;
    public event Action<float> RainReceived;
    public event Action<float> GhostReceived;

    private void Reset()
    {
        targetRigidbody = GetComponent<Rigidbody2D>();
        routeWalker = GetComponent<RouteWalker2D>();
    }

    public float GetInfluence(DivinePowerType powerType)
    {
        if (influence == null)
            return 0f;

        return influence.GetMultiplier(powerType);
    }

    public bool CanReceive(DivinePowerType powerType)
    {
        return GetInfluence(powerType) > 0f;
    }

    public void ReceiveWind(Vector2 direction, float impulse)
    {
        float multiplier =
            GetInfluence(DivinePowerType.Wind);

        if (multiplier <= 0f)
            return;

        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.right;

        direction.Normalize();

        float appliedImpulse =
            Mathf.Max(0f, impulse) * multiplier;

        if (routeWalker != null)
        {
            routeWalker.ApplyWind(
                direction,
                appliedImpulse
            );

            return;
        }

        if (WindReceived != null)
        {
            WindReceived.Invoke(direction, appliedImpulse);
            return;
        }

        if (targetRigidbody != null)
        {
            targetRigidbody.AddForce(
                direction * appliedImpulse,
                ForceMode2D.Impulse
            );
        }
    }

    public void ReceiveThunder(float strength = 1f)
    {
        float appliedStrength = CalculateStrength(
            DivinePowerType.Thunder,
            strength
        );

        if (appliedStrength <= 0f)
            return;

        onThunder?.Invoke(appliedStrength);
        ThunderReceived?.Invoke(appliedStrength);
    }

    public void ReceiveRain(float strength = 1f)
    {
        float appliedStrength = CalculateStrength(
            DivinePowerType.Rain,
            strength
        );

        if (appliedStrength <= 0f)
            return;

        onRain?.Invoke(appliedStrength);
        RainReceived?.Invoke(appliedStrength);
    }

    public void ReceiveGhost(float strength = 1f)
    {
        float appliedStrength = CalculateStrength(
            DivinePowerType.Ghost,
            strength
        );

        if (appliedStrength <= 0f)
            return;

        onGhost?.Invoke(appliedStrength);
        GhostReceived?.Invoke(appliedStrength);
    }

    private float CalculateStrength(
        DivinePowerType powerType,
        float baseStrength
    )
    {
        float multiplier = GetInfluence(powerType);

        return Mathf.Max(0f, baseStrength) * multiplier;
    }
}