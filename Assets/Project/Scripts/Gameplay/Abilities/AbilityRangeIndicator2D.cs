using UnityEngine;

namespace Tenyu.Gameplay
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class AbilityRangeIndicator2D :
        MonoBehaviour
    {
        [SerializeField, Range(16, 128)]
        private int segments = 64;

        [SerializeField, Min(0.01f)]
        private float lineWidth = 0.05f;

        [SerializeField]
        private Color indicatorColor =
            new Color(1f, 0.75f, 0.15f, 0.85f);

        private LineRenderer lineRenderer;
        private float currentRadius = -1f;

        private void Awake()
        {
            lineRenderer =
                GetComponent<LineRenderer>();

            SetupLineRenderer();
            Hide();
        }

        public void Show(
            Vector2 worldPosition,
            float radius)
        {
            if (lineRenderer == null)
                return;

            transform.position = new Vector3(
                worldPosition.x,
                worldPosition.y,
                0f
            );

            if (!Mathf.Approximately(
                currentRadius,
                radius))
            {
                currentRadius = radius;
                CreateCircle(radius);
            }

            lineRenderer.enabled = true;
        }

        public void Hide()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        private void SetupLineRenderer()
        {
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = segments;

            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            ApplyColor();
        }

        private void CreateCircle(float radius)
        {
            if (lineRenderer == null)
                return;

            lineRenderer.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle =
                    i / (float)segments *
                    Mathf.PI * 2f;

                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f
                );

                lineRenderer.SetPosition(
                    i,
                    position
                );
            }
        }

        private void ApplyColor()
        {
            if (lineRenderer == null)
                return;

            lineRenderer.startColor =
                indicatorColor;

            lineRenderer.endColor =
                indicatorColor;
        }

        private void OnValidate()
        {
            if (lineRenderer == null)
            {
                lineRenderer =
                    GetComponent<LineRenderer>();
            }

            if (lineRenderer == null)
                return;

            SetupLineRenderer();

            if (currentRadius > 0f)
            {
                CreateCircle(currentRadius);
            }
        }
    }
}