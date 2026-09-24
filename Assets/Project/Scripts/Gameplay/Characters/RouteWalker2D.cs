using System;
using System.Collections.Generic;
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

        [Header("自動迂回")]
        [SerializeField] private GridPathfinder2D pathfinder;
        [SerializeField, Min(0.1f)] private float repathInterval = 0.5f;
        [SerializeField] private bool showPlannedPath = true;

        private readonly List<Vector2> plannedPath = new List<Vector2>();
        private int pathIndex;
        private float repathTimer;
        private Transform pathTarget;
        private Vector2 lastPathGoal;
        private bool pathRequested;

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

        [Header("通行禁止")]
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField, Min(0.001f)] private float obstacleSkin = 0.02f;

        private readonly List<RaycastHit2D> obstacleHits = new List<RaycastHit2D>(16);

        // 移動経路全体を体の形で調べる。トリガーは壁として扱わない。
        private Vector2 ClampMovement(Vector2 desired, out bool blocked)
        {
            blocked = false;
            Vector2 current = rb.position;
            Vector2 movement = desired - current;
            float distance = movement.magnitude;
            if (distance <= 0.000001f || obstacleLayers.value == 0)
                return desired;

            Vector2 direction = movement / distance;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(obstacleLayers);
            filter.useTriggers = false;

            obstacleHits.Clear();
            int count = rb.Cast(direction, filter, obstacleHits, distance + obstacleSkin);
            float allowedDistance = distance;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = obstacleHits[i];
                if (hit.collider == null)
                    continue;

                // 壁から離れる方向や壁に平行な移動は妨げない。
                if (Vector2.Dot(direction, hit.normal) >= -0.0001f)
                    continue;

                allowedDistance = Mathf.Min(
                    allowedDistance, Mathf.Max(0f, hit.distance - obstacleSkin));
            }

            blocked = allowedDistance < distance;
            return current + direction * allowedDistance;
        }

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
            if (pathfinder == null)
                Debug.LogError("RouteWalker2DのPathfinderにステージ内のGridPathfinder2Dを設定してください。", this);
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

        [Header("幽霊への反応")]
        [SerializeField] private bool affectedByGhost = true;
        [SerializeField, Min(0.01f)] private float ghostStoppingDistance = 0.25f;

        private void ClearPath()
        {
            plannedPath.Clear();
            pathIndex = 0;
            repathTimer = 0f;
            pathTarget = null;
            pathRequested = false;
        }

        private Vector2 FollowPath(Transform target, float stopDistance, float dt)
        {
            Vector2 current = rb.position;
            Vector2 goal = target.position;
            if (pathfinder == null || !pathfinder.isActiveAndEnabled)
                return current;

            repathTimer -= dt;
            bool changed = !pathRequested || target != pathTarget ||
                (goal - lastPathGoal).sqrMagnitude > 0.0001f;

            if (changed || repathTimer <= 0f)
            {
                pathfinder.TryFindPath(rb, goal, obstacleLayers, obstacleSkin, plannedPath);
                pathIndex = 0;
                pathTarget = target;
                lastPathGoal = goal;
                pathRequested = true;
                repathTimer = repathInterval;
            }

            if (plannedPath.Count == 0)
                return current;

            // 直線で到達できる先の点まで省略。角の曲がり過ぎを減らす。
            int furthest = Mathf.Min(plannedPath.Count - 1, pathIndex + 8);
            for (int i = furthest; i > pathIndex; i--)
            {
                if (pathfinder.CanTravel(rb, current, plannedPath[i],
                    obstacleLayers, obstacleSkin))
                {
                    pathIndex = i;
                    break;
                }
            }

            while (pathIndex < plannedPath.Count - 1 &&
                Vector2.Distance(current, plannedPath[pathIndex]) <= 0.001f)
                pathIndex++;

            Vector2 nextPoint = plannedPath[pathIndex];
            if (!pathfinder.CanTravel(rb, current, nextPoint, obstacleLayers, obstacleSkin))
                return current; // 道が塞がれた。次の定期再探索まで待つ。

            Vector2 delta = nextPoint - current;
            float distance = delta.magnitude;
            float allowed = distance;
            if (pathIndex == plannedPath.Count - 1)
                allowed = Mathf.Max(0f, distance - stopDistance);

            float travel = Mathf.Min(moveSpeed * dt, allowed);
            if (travel <= 0.000001f || distance <= 0.000001f)
                return current;

            Vector2 direction = delta / distance;
            lastDirection = GetFourDirection(direction);
            UpdateDirectionSprite();
            return current + direction * travel;
        }

        private void MoveCharacter()
        {
            float dt = StageTime.FixedDeltaTime;
            Vector2 current = rb.position;
            Vector2 next = current;

            GhostLure2D ghost = affectedByGhost && !HasFinishedRoute && !hasFallen
                ? GhostLure2D.FindFor(stageDirector, current) : null;

            if (ghost != null)
            {
                next = FollowPath(ghost.transform, ghostStoppingDistance, dt);
            }
            else
            {
                Transform waypoint = GetCurrentWaypoint();
                if (waypoint != null && !HasFinishedRoute)
                {
                    // 実際の物理位置が到着してから経由地点を進める。
                    if (Vector2.Distance(current, waypoint.position) <= arrivalDistance)
                    {
                        AdvanceWaypoint();
                        ClearPath();
                        if (stageDirector.Phase != StagePhase.Playing)
                            return;
                    }
                    else
                    {
                        next = FollowPath(waypoint, 0f, dt);
                    }
                }
            }

            // 風は指定方向へ押す。通常移動に戻るときに再探索する。
            next += windVelocity * dt;
            next = ClampMovement(next, out bool blocked);
            rb.MovePosition(next);
            windVelocity = Vector2.MoveTowards(
                windVelocity, Vector2.zero, windDeceleration * dt);
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

            ClearPath();
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
            ClearPath();
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
            if (showPlannedPath && Application.isPlaying && rb != null)
            {
                Gizmos.color = Color.green;
                Vector3 previous = rb.position;
                for (int i = pathIndex; i < plannedPath.Count; i++)
                {
                    Vector3 point = plannedPath[i];
                    Gizmos.DrawLine(previous, point);
                    previous = point;
                }
            }

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
