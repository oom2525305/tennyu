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

        public void Initialize(StageDirector stageDirector, float duration, float lureRadius)
        {
            director = stageDirector;
            remainingTime = Mathf.Max(0.1f, duration);
            radius = Mathf.Max(0.1f, lureRadius);
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
