using System.Collections.Generic;
using UnityEngine;

namespace Tenyu.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(DivinePowerReceiver2D))]
    public sealed class WindRoadblock2D : MonoBehaviour
    {
        [SerializeField] private StageDirector stageDirector;
        [SerializeField] private LayerMask wallLayers;
        [SerializeField, Min(0.1f)] private float maximumSpeed = 3f;
        [SerializeField, Min(0.1f)] private float deceleration = 5f;
        private Rigidbody2D body;
        private DivinePowerReceiver2D receiver;
        private Vector2 spawn, velocity;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            receiver = GetComponent<DivinePowerReceiver2D>();
            spawn = body.position;
        }
        private void OnEnable()
        {
            receiver.WindReceived += ReceiveWind;
            if (stageDirector != null) stageDirector.StageReset += ResetBlock;
        }
        private void ReceiveWind(Vector2 direction, float impulse)
        {
            if (stageDirector == null || stageDirector.Phase != StagePhase.Playing) return;
            velocity = Vector2.ClampMagnitude(velocity + direction.normalized * impulse, maximumSpeed);
        }
        private void FixedUpdate()
        {
            if (stageDirector == null || stageDirector.Phase != StagePhase.Playing) return;
            float dt = StageTime.FixedDeltaTime;
            Vector2 delta = velocity * dt;
            float distance = delta.magnitude;
            if (distance < 0.000001f) return;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(wallLayers);
            filter.useTriggers = false;
            hits.Clear();
            Vector2 direction = delta / distance;
            body.Cast(direction, filter, hits, distance + 0.02f);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.rigidbody == body ||
                    Vector2.Dot(direction, hit.normal) >= -0.0001f) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.02f));
            }
            // A kinematic obstacle must not move into Nobunaga and trap him in a wall.
            hits.Clear();
            filter.SetLayerMask(1 << gameObject.layer);
            body.Cast(direction, filter, hits, delta.magnitude + 0.02f);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.rigidbody == body ||
                    hit.collider.GetComponentInParent<ProtectedTarget2D>() == null ||
                    Vector2.Dot(direction, hit.normal) >= -0.0001f) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.02f));
            }
            body.MovePosition(body.position + direction * distance);
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration * dt);
        }
        private void ResetBlock()
        {
            if (body == null) return;
            body.position = spawn;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            velocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
        private void OnDisable()
        {
            if (receiver != null) receiver.WindReceived -= ReceiveWind;
            if (stageDirector != null) stageDirector.StageReset -= ResetBlock;
            velocity = Vector2.zero;
        }
    }
}
