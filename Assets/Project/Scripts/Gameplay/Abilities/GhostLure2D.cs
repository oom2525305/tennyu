using System.Collections.Generic;
using UnityEngine;

namespace Tenyu.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GhostLure2D : MonoBehaviour
    {
        private static readonly List<GhostLure2D> instances =
            new List<GhostLure2D>();

        private StageDirector director;
        private float remainingTime;
        private float radius;
        private bool initialized;
        private sealed class Reachability
        {
            public Vector2 From, Goal;
            public GridPathfinder2D Grid;
            public int Mask;
            public float Skin, CheckAfter;
            public bool Reachable;
        }
        private readonly Dictionary<Rigidbody2D, Reachability> reachability = new Dictionary<Rigidbody2D, Reachability>();
        private readonly List<Vector2> probePath = new List<Vector2>();

        public void Initialize(StageDirector stageDirector, float duration, float lureRadius)
        {
            director = stageDirector;
            remainingTime = Mathf.Max(0.1f, duration);
            radius = Mathf.Max(0.1f, lureRadius);
            reachability.Clear();
            initialized = true;
        }

        private void OnEnable()
        {
            if (!instances.Contains(this))
                instances.Add(this);
        }

        private void OnDisable()
        {
            instances.Remove(this);
            initialized = false;
            reachability.Clear();
        }

        private void Update()
        {
            if (!initialized)
                return;

            if (director == null ||
                director.Phase == StagePhase.Observation ||
                director.Phase == StagePhase.Cleared ||
                director.Phase == StagePhase.Failed)
            {
                Despawn();
                return;
            }

            if (director.Phase != StagePhase.Playing)
                return;

            remainingTime -= StageTime.DeltaTime;
            if (remainingTime <= 0f)
                Despawn();
        }

        private void Despawn()
        {
            // Destroyの反映を待たず、その場で誘導対象から除外する。
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public static GhostLure2D FindReachableFor(StageDirector stageDirector, Rigidbody2D body,
            GridPathfinder2D grid, LayerMask walls, float skin)
        {
            if (body == null || grid == null || !grid.isActiveAndEnabled) return null;
            GhostLure2D nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (GhostLure2D ghost in instances)
            {
                if (ghost == null || !ghost.isActiveAndEnabled || !ghost.initialized ||
                    ghost.director != stageDirector || ghost.remainingTime <= 0f) continue;
                float distance = ((Vector2)ghost.transform.position - body.position).sqrMagnitude;
                if (distance > ghost.radius * ghost.radius || distance >= nearestDistance) continue;
                if (!ghost.CanReach(body, grid, walls, skin)) continue;
                nearest = ghost;
                nearestDistance = distance;
            }
            return nearest;
        }

        private bool CanReach(Rigidbody2D body, GridPathfinder2D grid, LayerMask walls, float skin)
        {
            Vector2 goal = transform.position;
            // A moved wall must not leave a lure selected inside an obstacle.
            if (!grid.CanStandAt(body, goal, walls, skin)) return false;
            if (grid.CanTravel(body, body.position, goal, walls, skin)) return true;
            if (!reachability.TryGetValue(body, out Reachability check))
            {
                check = new Reachability();
                reachability.Add(body, check);
            }
            if (check.Grid != grid || check.Mask != walls.value || check.Skin != skin ||
                Time.unscaledTime >= check.CheckAfter || (goal - check.Goal).sqrMagnitude > 0.01f ||
                (body.position - check.From).sqrMagnitude > 0.16f)
            {
                check.Reachable = grid.TryFindPath(body, goal, walls, skin, probePath);
                check.From = body.position; check.Goal = goal; check.Grid = grid;
                check.Mask = walls.value; check.Skin = skin;
                check.CheckAfter = Time.unscaledTime + 0.5f;
            }
            return check.Reachable;
        }

        public static GhostLure2D FindFor(StageDirector stageDirector, Vector2 position)
        {
            GhostLure2D nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (GhostLure2D ghost in instances)
            {
                if (ghost == null || !ghost.isActiveAndEnabled ||
                    !ghost.initialized || ghost.director != stageDirector ||
                    ghost.remainingTime <= 0f)
                    continue;

                float distance = ((Vector2)ghost.transform.position - position).sqrMagnitude;
                if (distance <= ghost.radius * ghost.radius && distance < nearestDistance)
                {
                    nearest = ghost;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }
    }
}
