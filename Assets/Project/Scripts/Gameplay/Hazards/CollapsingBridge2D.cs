using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Tenyu.Gameplay
{
    // 橋に入ってから崩落までの猶予。風で押し出すタイミングを考える仕掛け。
    public sealed class CollapsingBridge2D : MonoBehaviour
    {
        [SerializeField] private StageDirector stageDirector;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Vector2 size = new Vector2(1.6f, 1.5f);
        [SerializeField, Min(0.1f)] private float collapseDelay = 3.6f;
        [SerializeField] private LayerMask targetLayers = 1 << 6;
        private readonly List<Collider2D> hits = new List<Collider2D>();
        private readonly List<LineRenderer> boards = new List<LineRenderer>();
        private TMP_Text label;
        private float remaining;
        private bool triggered, collapsed;

        private void Awake()
        {
            // 仮背景にも危険区域と橋の幅が分かる、板と縄の簡易表示。
            Line("谷", new Vector3(-size.x / 2, 0), new Vector3(size.x / 2, 0), size.y,
                new Color(0.12f, 0.14f, 0.14f), 1);
            for (int i = 0; i <= 8; i++)
            {
                float x = Mathf.Lerp(-size.x / 2, size.x / 2, i / 8f);
                boards.Add(Line("板", new Vector3(x, -size.y / 2), new Vector3(x, size.y / 2),
                    size.x / 11, new Color(0.65f, 0.46f, 0.27f), 2));
            }
            foreach (float side in new[] { -1f, 1f })
                Line("縄", new Vector3(-size.x / 2, side * size.y / 2),
                    new Vector3(size.x / 2, side * size.y / 2), 0.05f, new Color(0.3f, 0.22f, 0.13f), 3);
            var labelObject = new GameObject("崩落予告", typeof(TextMeshPro));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0, size.y / 2 + 0.5f, -0.1f);
            labelObject.transform.rotation = Quaternion.identity;
            label = labelObject.GetComponent<TextMeshPro>(); label.font = font;
            label.fontSize = 3; label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(3, 0.7f);
            label.GetComponent<MeshRenderer>().sortingOrder = 30;
            ResetBridge();
        }
        private LineRenderer Line(string name, Vector3 a, Vector3 b, float width, Color color, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = lineMaterial;
            line.useWorldSpace = false; line.positionCount = 2;
            line.SetPosition(0, a); line.SetPosition(1, b);
            line.startWidth = line.endWidth = width; line.startColor = line.endColor = color;
            line.sortingOrder = order; return line;
        }
        private void OnEnable() { if (stageDirector != null) stageDirector.StageReset += ResetBridge; }
        private void OnDisable() { if (stageDirector != null) stageDirector.StageReset -= ResetBridge; }
        private void ResetBridge()
        {
            remaining = collapseDelay; triggered = collapsed = false;
            foreach (var board in boards) { board.enabled = true; board.startColor = board.endColor = new Color(0.65f, 0.46f, 0.27f); }
            UpdateLabel();
        }
        private void FixedUpdate()
        {
            if (stageDirector == null || stageDirector.Phase != StagePhase.Playing) return;
            ContactFilter2D filter = new ContactFilter2D(); filter.SetLayerMask(targetLayers); filter.useTriggers = false;
            hits.Clear();
            Physics2D.OverlapBox(transform.position, size, transform.eulerAngles.z, filter, hits);
            ProtectedTarget2D target = null;
            foreach (var hit in hits)
            {
                target = hit.GetComponentInParent<ProtectedTarget2D>();
                if (target != null) break;
            }
            if (target != null) triggered = true;
            if (!triggered) return;
            remaining = Mathf.Max(0, remaining - StageTime.FixedDeltaTime);
            if (!collapsed && remaining <= 0)
            {
                collapsed = true;
                for (int i = 0; i < boards.Count; i++) boards[i].enabled = i % 3 == 0;
            }
            if (!collapsed)
                foreach (var board in boards)
                    board.startColor = board.endColor = Color.Lerp(new Color(0.8f, 0.2f, 0.1f), new Color(0.65f, 0.46f, 0.27f), remaining / collapseDelay);
            UpdateLabel();
            if (collapsed && target != null) target.ReceiveHazard("橋の崩落");
        }
        private void UpdateLabel()
        {
            if (label == null) return;
            label.text = collapsed ? "崩落" : triggered ? remaining.ToString("0.0") + "秒" : "崩れる橋";
            label.color = collapsed || triggered ? new Color(1f, 0.65f, 0.3f) : Color.white;
        }
    }
}
