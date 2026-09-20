using System;
using UnityEngine;

namespace Tenyu.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class RouteWalker2D : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField]
        private StageDirector stageDirector;

        [SerializeField]
        private Transform[] waypoints;

        [SerializeField]
        private Transform visualTransform;

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [Header("方向別スプライト")]
        [SerializeField]
        private Sprite downSprite;

        [SerializeField]
        private Sprite upSprite;

        [SerializeField]
        private Sprite leftSprite;

        [SerializeField]
        private Sprite rightSprite;

        [Header("通常移動")]
        [SerializeField, Min(0.1f)]
        private float moveSpeed = 2f;

        [SerializeField, Min(0.001f)]
        private float arrivalDistance = 0.03f;

        [Header("風による移動")]
        [SerializeField, Min(0.1f)]
        private float maxWindSpeed = 8f;

        [SerializeField, Min(0f)]
        private float windDeceleration = 7f;

        [Header("失敗時")]
        [SerializeField]
        private float fallAngle = -90f;

        public bool HasFinishedRoute
        {
            get;
            private set;
        }

        public event Action RouteFinished;

        private Rigidbody2D rb;

        private Vector2 initialPosition;
        private Vector2 lastDirection =
            Vector2.down;

        private Vector2 windVelocity;

        private float initialRigidbodyRotation;
        private Quaternion initialVisualRotation;
        private Vector3 initialVisualLocalPosition;
        private Vector3 initialVisualLocalScale;

        private int nextWaypointIndex;
        private bool hasFallen;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            initialPosition = rb.position;
            initialRigidbodyRotation = rb.rotation;

            if (visualTransform == null &&
                spriteRenderer != null)
            {
                visualTransform =
                    spriteRenderer.transform;
            }

            if (spriteRenderer == null &&
                visualTransform != null)
            {
                spriteRenderer =
                    visualTransform
                        .GetComponent<SpriteRenderer>();
            }

            if (visualTransform != null)
            {
                initialVisualRotation =
                    visualTransform.localRotation;

                initialVisualLocalPosition =
                    visualTransform.localPosition;

                initialVisualLocalScale =
                    visualTransform.localScale;
            }
        }

        private void OnEnable()
        {
            if (stageDirector != null)
            {
                stageDirector.StageReset +=
                    ResetWalker;
            }
        }

        private void Start()
        {
            ResetWalker();
        }

        private void FixedUpdate()
        {
            if (stageDirector == null)
            {
                return;
            }

            if (stageDirector.Phase ==
                StagePhase.Failed)
            {
                FallSideways();
                return;
            }

            if (stageDirector.Phase !=
                StagePhase.Playing)
            {
                return;
            }

            MoveCharacter();
        }

        private void MoveCharacter()
        {
            float deltaTime =
                StageTime.FixedDeltaTime;

            Vector2 currentPosition =
                rb.position;

            Vector2 nextPosition =
                currentPosition;

            Transform targetWaypoint =
                GetCurrentWaypoint();

            if (targetWaypoint != null &&
                !HasFinishedRoute)
            {
                Vector2 targetPosition =
                    targetWaypoint.position;

                Vector2 difference =
                    targetPosition -
                    currentPosition;

                if (difference.sqrMagnitude >
                    0.001f)
                {
                    Vector2 routeDirection =
                        difference.normalized;

                    Vector2 fourDirection =
                        GetFourDirection(
                            routeDirection
                        );

                    if (fourDirection !=
                        lastDirection)
                    {
                        lastDirection =
                            fourDirection;

                        UpdateDirectionSprite();
                    }

                    nextPosition =
                        Vector2.MoveTowards(
                            currentPosition,
                            targetPosition,
                            moveSpeed * deltaTime
                        );
                }

                // ルート移動に風の移動を加算
                nextPosition +=
                    windVelocity * deltaTime;

                float distance =
                    Vector2.Distance(
                        nextPosition,
                        targetPosition
                    );

                if (distance <=
                    arrivalDistance)
                {
                    nextPosition =
                        targetPosition;

                    AdvanceWaypoint();
                }
            }
            else
            {
                // ルート終了後も風では動かせる
                nextPosition +=
                    windVelocity * deltaTime;
            }

            rb.MovePosition(nextPosition);

            // 風の勢いを徐々に弱める
            windVelocity =
                Vector2.MoveTowards(
                    windVelocity,
                    Vector2.zero,
                    windDeceleration *
                    deltaTime
                );
        }

        private Transform GetCurrentWaypoint()
        {
            if (HasFinishedRoute ||
                waypoints == null ||
                waypoints.Length == 0)
            {
                return null;
            }

            while (nextWaypointIndex <
                waypoints.Length)
            {
                Transform waypoint =
                    waypoints[nextWaypointIndex];

                if (waypoint != null)
                {
                    return waypoint;
                }

                nextWaypointIndex++;
            }

            FinishRoute();
            return null;
        }

        private void AdvanceWaypoint()
        {
            nextWaypointIndex++;

            if (nextWaypointIndex <
                waypoints.Length)
            {
                return;
            }

            FinishRoute();
        }

        private void FinishRoute()
        {
            if (HasFinishedRoute)
            {
                return;
            }

            HasFinishedRoute = true;
            RouteFinished?.Invoke();
        }

        public void ApplyWind(
            Vector2 direction,
            float impulse)
        {
            if (direction.sqrMagnitude <=
                0.001f)
            {
                return;
            }

            windVelocity +=
                direction.normalized * impulse;

            if (windVelocity.magnitude >
                maxWindSpeed)
            {
                windVelocity =
                    windVelocity.normalized *
                    maxWindSpeed;
            }
        }

        public void ResetWalker()
        {
            HasFinishedRoute = false;
            hasFallen = false;
            windVelocity = Vector2.zero;

            // 前回プレイ時の物理速度を完全に消す
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.rotation = initialRigidbodyRotation;

            if (waypoints != null &&
                waypoints.Length > 0 &&
                waypoints[0] != null)
            {
                rb.position =
                    waypoints[0].position;

                nextWaypointIndex = 1;
            }
            else
            {
                rb.position = initialPosition;
                nextWaypointIndex = 0;
            }

            lastDirection = Vector2.down;

            if (visualTransform != null)
            {
                visualTransform.localPosition =
                    initialVisualLocalPosition;

                visualTransform.localRotation =
                    initialVisualRotation;

                visualTransform.localScale =
                    initialVisualLocalScale;
            }

            UpdateDirectionSprite();

            // Time.timeScaleが0でも表示とColliderを即座に同期する
            Physics2D.SyncTransforms();
        }

        public void FallSideways()
        {
            if (hasFallen)
            {
                return;
            }

            hasFallen = true;
            windVelocity = Vector2.zero;

            if (visualTransform == null)
            {
                return;
            }

            float directionAngle =
                lastDirection.x < 0f
                    ? Mathf.Abs(fallAngle)
                    : -Mathf.Abs(fallAngle);

            visualTransform.localRotation =
                initialVisualRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    directionAngle
                );
        }

        private void UpdateDirectionSprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (lastDirection == Vector2.up)
            {
                spriteRenderer.sprite =
                    upSprite;

                return;
            }

            if (lastDirection ==
                Vector2.left)
            {
                spriteRenderer.sprite =
                    leftSprite;

                return;
            }

            if (lastDirection ==
                Vector2.right)
            {
                spriteRenderer.sprite =
                    rightSprite;

                return;
            }

            spriteRenderer.sprite =
                downSprite;
        }

        private Vector2 GetFourDirection(
            Vector2 direction)
        {
            if (Mathf.Abs(direction.x) >
                Mathf.Abs(direction.y))
            {
                return direction.x >= 0f
                    ? Vector2.right
                    : Vector2.left;
            }

            return direction.y >= 0f
                ? Vector2.up
                : Vector2.down;
        }

        private void OnDisable()
        {
            if (stageDirector != null)
            {
                stageDirector.StageReset -=
                    ResetWalker;
            }
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null ||
                waypoints.Length == 0)
            {
                return;
            }

            Gizmos.color =
                new Color(
                    0.75f,
                    0.2f,
                    0.12f
                );

            for (int i = 0;
                i < waypoints.Length;
                i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(
                    waypoints[i].position,
                    0.15f
                );

                if (i + 1 <
                        waypoints.Length &&
                    waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(
                        waypoints[i].position,
                        waypoints[i + 1]
                            .position
                    );
                }
            }
        }
    }
}
