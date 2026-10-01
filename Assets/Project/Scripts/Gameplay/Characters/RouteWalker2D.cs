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
        [Tooltip("現在位置から最後の経由地点へ直接探索。風と障害物で進路が変わるステージ用。")]
        [SerializeField] private bool navigateDirectlyToGoal;
        [Tooltip("経路計画時だけ加える壁との余裕。実際の移動判定はObstacle Skinを使用。")]
        [SerializeField, Min(0f)] private float pathClearance;
        private float PathSkin => obstacleSkin + pathClearance;

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

        [SerializeField] private bool advanceWaypointsWhenPushed;

        [SerializeField, Min(0f)]
        private float windDeceleration = 7f;

        [Header("通行禁止")]
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField, Min(0.001f)] private float obstacleSkin = 0.02f;

        private readonly List<RaycastHit2D> obstacleHits = new List<RaycastHit2D>(16);

        [Header("失敗時")]
        [SerializeField]
        private Sprite failedSprite;

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
        private bool windWasActive;

        private float initialRigidbodyRotation;
        private Quaternion initialVisualRotation;
        private Vector3 initialVisualLocalPosition;
        private Vector3 initialVisualLocalScale;

        private int nextWaypointIndex;
        private bool hasFallen;
        private bool keepFailedSpriteUntilPlaying;

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

        // 時間停止中でも失敗用の表示へ切り替える。
        private void LateUpdate()
        {
            if (stageDirector != null && stageDirector.Phase == StagePhase.Failed)
                FallSideways();

            if (keepFailedSpriteUntilPlaying && stageDirector != null &&
                stageDirector.Phase == StagePhase.Playing && !hasFallen)
            {
                keepFailedSpriteUntilPlaying = false;
                UpdateDirectionSprite();
            }
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

            if (!hasFallen)
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
                (goal - lastPathGoal).sqrMagnitude > 0.01f;
            bool inspectRoute = repathTimer <= 0f;
            bool blocked = false;
            if (inspectRoute && plannedPath.Count > 0)
            {
                Vector2 from = current;
                for (int i = pathIndex; i < plannedPath.Count; i++)
                {
                    if (!pathfinder.CanTravel(rb, from, plannedPath[i], obstacleLayers, PathSkin))
                    {
                        blocked = true;
                        break;
                    }
                    from = plannedPath[i];
                }
                if (!blocked) repathTimer = repathInterval;
            }

            // Validate the whole route cheaply. Rebuild only when a target/path actually changes.
            if (changed || (inspectRoute && (plannedPath.Count == 0 || blocked)))
            {
                pathfinder.TryFindPath(rb, goal, obstacleLayers, PathSkin, plannedPath);
                pathIndex = 0;
                pathTarget = target;
                lastPathGoal = goal;
                pathRequested = true;
                repathTimer = repathInterval;
            }

            if (plannedPath.Count == 0)
                return current;

            // A smoothed corner can only be skipped if the next segment clears the body.
            // Proximity alone used to cut into the corner, stop, and restart A* every interval.
            while (pathIndex < plannedPath.Count - 1 &&
                Vector2.Distance(current, plannedPath[pathIndex]) <= 0.025f &&
                pathfinder.CanTravel(rb, current, plannedPath[pathIndex + 1], obstacleLayers, PathSkin))
                pathIndex++;

            Vector2 nextPoint = plannedPath[pathIndex];
            if (!pathfinder.CanTravel(rb, current, nextPoint, obstacleLayers, PathSkin))
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

            bool windDriving = windVelocity.sqrMagnitude > 0.1225f;
            if (!windDriving && windWasActive)
            {
                windVelocity = Vector2.zero;
                ClearPath(); // Choose a route from where the player actually pushed us.
            }
            windWasActive = windDriving;

            GhostLure2D ghost = affectedByGhost && !HasFinishedRoute && !hasFallen
                ? GhostLure2D.FindFor(stageDirector, current) : null;

            // The automatic walker must not pull against the player's wind.
            if (!windDriving && ghost != null)
            {
                next = FollowPath(ghost.transform, ghostStoppingDistance, dt);
            }
            else if (!windDriving)
            {
                Transform waypoint = GetCurrentWaypoint();
                if (waypoint != null && !HasFinishedRoute)
                {
                    // Consume arrived points in the same tick; no one-frame stop at each corner.
                    while (waypoint != null && !HasFinishedRoute &&
                        Vector2.Distance(current, waypoint.position) <= arrivalDistance)
                    {
                        AdvanceWaypoint();
                        ClearPath();
                        if (stageDirector.Phase != StagePhase.Playing) return;
                        waypoint = GetCurrentWaypoint();
                    }
                    if (waypoint != null && !HasFinishedRoute)
                        next = FollowPath(waypoint, 0f, dt);
                }
            }

            // 風は指定方向へ押す。通常移動に戻るときに再探索する。
            next += windVelocity * dt;
            next = KinematicMotion2D.Resolve(rb, next - current, obstacleLayers, obstacleSkin, obstacleHits);
            // Forward wind may cross an intermediate waypoint. Do not walk back to it.
            if (advanceWaypointsWhenPushed && ghost == null && windVelocity.sqrMagnitude > 0.001f)
            {
                while (nextWaypointIndex > 0 && nextWaypointIndex < waypoints.Length - 1)
                {
                    Transform point = waypoints[nextWaypointIndex];
                    Transform previous = waypoints[nextWaypointIndex - 1];
                    if (point == null || previous == null || pathfinder == null) break;
                    Vector2 forward = point.position - previous.position;
                    if (Vector2.Dot(next - (Vector2)point.position, forward) < 0f ||
                        !pathfinder.CanTravel(rb, next, point.position, obstacleLayers, obstacleSkin)) break;
                    AdvanceWaypoint();
                    ClearPath();
                }
            }
            if ((next - current).sqrMagnitude > 0.000001f)
            {
                lastDirection = GetFourDirection(next - current);
                UpdateDirectionSprite();
            }
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

        // Switch a stage route without teleporting or restoring a failed character.
        public void SetRoute(Transform[] route)
        {
            if (route == null || route.Length == 0) return;
            waypoints = route;
            nextWaypointIndex = navigateDirectlyToGoal ? route.Length - 1 : 0;
            HasFinishedRoute = false;
            ClearPath();
        }

        public void ResetWalker()
        {
            // リトライ後は開始操作まで失敗イラストを維持する。
            keepFailedSpriteUntilPlaying = failedSprite != null &&
                (hasFallen || keepFailedSpriteUntilPlaying ||
                 (stageDirector != null && stageDirector.Phase == StagePhase.Failed));
            ClearPath();
            HasFinishedRoute = false;
            hasFallen = false;
            windVelocity = Vector2.zero;
            windWasActive = false;

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

                nextWaypointIndex = navigateDirectlyToGoal ? waypoints.Length - 1 : 1;
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

            if (keepFailedSpriteUntilPlaying && spriteRenderer != null)
                spriteRenderer.sprite = failedSprite;
            else
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
            ClearPath();
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // 既存の被弾処理との互換性のためメソッド名は維持する。
            // 回転は加えず、専用イラストをそのまま表示する。
            if (spriteRenderer != null && failedSprite != null)
                spriteRenderer.sprite = failedSprite;
        }

        private void UpdateDirectionSprite()
        {
            if (spriteRenderer == null || hasFallen || keepFailedSpriteUntilPlaying)
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
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y) * 1.15f)
            {
                return direction.x >= 0f
                    ? Vector2.right
                    : Vector2.left;
            }

            if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x) * 1.15f)
                return direction.y >= 0f ? Vector2.up : Vector2.down;
            return lastDirection;
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

            if (navigateDirectlyToGoal)
            {
                // The old red waypoint polyline was not the route this mode actually follows.
                Gizmos.color = Color.cyan;
                if (waypoints[0] != null) Gizmos.DrawWireSphere(waypoints[0].position, 0.2f);
                Gizmos.color = Color.yellow;
                if (waypoints[waypoints.Length - 1] != null)
                    Gizmos.DrawWireSphere(waypoints[waypoints.Length - 1].position, 0.3f);
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
