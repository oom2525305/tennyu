using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Tenyu.Gameplay
{
    /// <summary>Stage-local pursuer. All movement and attack clocks use the ability slow clock.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(DivinePowerReceiver2D))]
    public sealed class AssassinChaser2D : MonoBehaviour
    {
        [SerializeField] private StageDirector stageDirector;
        [SerializeField] private ProtectedTarget2D target;
        [SerializeField] private GridPathfinder2D pathfinder;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private AbilityRangeIndicator2D attackIndicator;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private string statusPrefix;
        [SerializeField] private AudioSource warningSource;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField, Min(0.1f)] private float moveSpeed = 1.05f;
        [SerializeField, Min(0f)] private float startDelay = 1.2f;
        [SerializeField, Min(0.1f)] private float attackDistance = 0.7f;
        [SerializeField, Min(0.1f)] private float strikeReach = 1.55f;
        [SerializeField, Min(0.1f)] private float warningDuration = 1.1f;
        [SerializeField, Min(0.1f)] private float recoveryDuration = 1.5f;
        [SerializeField, Min(0.1f)] private float windStagger = 1.2f;
        [SerializeField, Min(0.1f)] private float maxWindSpeed = 4f;
        [SerializeField, Min(0.1f)] private float windDeceleration = 7f;

        private Rigidbody2D body;
        private DivinePowerReceiver2D receiver;
        private Vector2 spawn, windVelocity;
        private readonly List<Vector2> path = new List<Vector2>();
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        private int pathIndex;
        private float delay, recovery, warning, repath;
        private Transform pathTarget;
        private bool windingUp;
        private const float Skin = 0.02f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            receiver = GetComponent<DivinePowerReceiver2D>();
            spawn = body.position;
        }

        private void OnEnable()
        {
            receiver.WindReceived += ReceiveWind;
            if (stageDirector == null) return;
            stageDirector.StageReset += ResetChaser;
            stageDirector.PhaseChanged += OnPhaseChanged;
        }

        private void Start() { ResetChaser(); }

        private void ResetChaser()
        {
            if (body == null) return;
            body.position = spawn;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            windVelocity = Vector2.zero;
            delay = startDelay;
            recovery = 0f;
            ClearPath();
            StopWarning();
            SetStatus("暗殺者を足止めして、右上の門へ");
            Physics2D.SyncTransforms();
        }

        private void OnPhaseChanged(StagePhase phase)
        {
            if (phase == StagePhase.Paused)
            {
                if (warningSource != null) warningSource.Pause();
            }
            else if (phase == StagePhase.Playing)
            {
                if (warningSource != null) warningSource.UnPause();
            }
            else
            {
                StopWarning();
                windVelocity = Vector2.zero;
            }
        }

        private void FixedUpdate()
        {
            if (stageDirector == null || stageDirector.Phase != StagePhase.Playing ||
                target == null || pathfinder == null) return;
            float dt = StageTime.FixedDeltaTime;
            Vector2 next = body.position;
            GhostLure2D ghost = receiver.CanReceive(DivinePowerType.Ghost)
                ? GhostLure2D.FindReachableFor(stageDirector, body, pathfinder, obstacleLayers, Skin) : null;
            if (delay > 0f)
            {
                delay = Mathf.Max(0f, delay - dt);
                SetStatus("背後に暗殺者！風と幽霊で逃がせ");
            }
            else if (recovery > 0f)
            {
                recovery = Mathf.Max(0f, recovery - dt);
                SetStatus("暗殺者がよろめいている！");
            }
            else if (ghost != null)
            {
                StopWarning();
                next = Follow(ghost.transform, 0.25f, dt);
                SetStatus("幽霊に誘導中…信長も引き寄せるので注意");
            }
            else if (windingUp)
            {
                warning -= dt;
                if (attackIndicator != null) attackIndicator.Show(body.position, strikeReach);
                if (warning <= 0f)
                {
                    bool hit = CanStrike(strikeReach);
                    StopWarning();
                    recovery = recoveryDuration;
                    if (hit) target.ReceiveHazard("暗殺者の斬撃");
                }
            }
            else if (CanStrike(attackDistance))
            {
                windingUp = true;
                warning = warningDuration;
                ClearPath();
                if (attackIndicator != null) attackIndicator.Show(body.position, strikeReach);
                if (warningSource != null && warningSound != null)
                    warningSource.PlayOneShot(warningSound);
                if (visual != null) visual.color = new Color(1f, 0.45f, 0.35f);
                SetStatus("斬撃予告！風で離すか、幽霊でそらせ");
            }
            else
            {
                next = Follow(target.transform, attackDistance * 0.8f, dt);
                SetStatus("暗殺者が追跡中！右上の門へ逃げろ");
            }
            if (stageDirector.Phase != StagePhase.Playing) return;
            next = ClampMovement(next + windVelocity * dt);
            body.MovePosition(next);
            windVelocity = Vector2.MoveTowards(windVelocity, Vector2.zero, windDeceleration * dt);
        }

        private bool CanStrike(float distance)
        {
            return Vector2.Distance(body.position, target.transform.position) <= distance &&
                pathfinder.CanTravel(body, body.position, target.transform.position, obstacleLayers, Skin);
        }

        private Vector2 Follow(Transform destination, float stopDistance, float dt)
        {
            Vector2 current = body.position;
            // Chase the current target directly when visible, without rebuilding the grid.
            if (pathfinder.CanTravel(body, current, destination.position, obstacleLayers, Skin))
            {
                Vector2 offset = (Vector2)destination.position - current;
                if (visual != null && Mathf.Abs(offset.x) > 0.01f) visual.flipX = offset.x < 0f;
                pathTarget = null;
                return current + offset.normalized * Mathf.Min(moveSpeed * dt,
                    Mathf.Max(0f, offset.magnitude - stopDistance));
            }
            repath -= dt;
            if (pathTarget != destination || repath <= 0f)
            {
                pathfinder.TryFindPath(body, destination.position, obstacleLayers, Skin, path);
                pathTarget = destination;
                pathIndex = 0;
                repath = 0.6f;
            }
            if (path.Count == 0) return current;
            while (pathIndex < path.Count - 1 &&
                Vector2.Distance(current, path[pathIndex]) < 0.02f &&
                pathfinder.CanTravel(body, current, path[pathIndex + 1], obstacleLayers, Skin))
                pathIndex++;
            Vector2 delta = path[pathIndex] - current;
            float allowed = Mathf.Max(0f, delta.magnitude - (pathIndex == path.Count - 1 ? stopDistance : 0f));
            if (visual != null && Mathf.Abs(delta.x) > 0.01f) visual.flipX = delta.x < 0f;
            return current + delta.normalized * Mathf.Min(moveSpeed * dt, allowed);
        }

        private Vector2 ClampMovement(Vector2 desired)
        {
            return KinematicMotion2D.Resolve(body, desired - body.position, obstacleLayers, Skin, hits);
        }

        private void ReceiveWind(Vector2 direction, float impulse)
        {
            if (stageDirector == null || stageDirector.Phase != StagePhase.Playing) return;
            StopWarning();
            recovery = windStagger;
            windVelocity = Vector2.ClampMagnitude(windVelocity + direction.normalized * impulse, maxWindSpeed);
            ClearPath();
        }

        private void ClearPath() { path.Clear(); pathIndex = 0; pathTarget = null; repath = 0f; }
        private void SetStatus(string message)
        {
            string text = statusPrefix + message;
            if (statusText != null && statusText.text != text) statusText.text = text;
        }
        private void StopWarning()
        {
            windingUp = false;
            warning = 0f;
            if (attackIndicator != null) attackIndicator.Hide();
            if (warningSource != null) warningSource.Stop();
            if (visual != null) visual.color = idleColor;
        }
        private void OnDisable()
        {
            if (receiver != null) receiver.WindReceived -= ReceiveWind;
            if (stageDirector != null)
            {
                stageDirector.StageReset -= ResetChaser;
                stageDirector.PhaseChanged -= OnPhaseChanged;
            }
            StopWarning();
        }
    }
}
