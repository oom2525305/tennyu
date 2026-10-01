using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tenyu.Gameplay
{
    // XY平面・ワールド座標でのA*。外部パッケージやBakeは不要。
    [DisallowMultipleComponent]
    public sealed class GridPathfinder2D : MonoBehaviour
    {
        [Header("探索範囲（ワールド単位・回転とScaleは使いません）")]
        [SerializeField] private Vector2 areaSize = new Vector2(24f, 14f);
        [SerializeField, Min(0.1f)] private float cellSize = 0.3f;
        [SerializeField, Range(100, 20000)] private int maximumCells = 10000;

        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(16);
        private readonly SortedSet<OpenNode> open = new SortedSet<OpenNode>();
        private float[] costs;
        private int[] parents;
        private bool[] closed;
        private int columns, rows;
        private Vector2 origin, spacing;
        private bool warnedSize;

        private struct OpenNode : IComparable<OpenNode>
        {
            public int Index;
            public float Score;
            public OpenNode(int index, float score) { Index = index; Score = score; }
            public int CompareTo(OpenNode other)
            {
                int order = Score.CompareTo(other.Score);
                return order != 0 ? order : Index.CompareTo(other.Index);
            }
        }

        public bool Contains(Vector2 position)
        {
            Vector2 relative = position - (Vector2)transform.position;
            return Mathf.Abs(relative.x) <= areaSize.x * 0.5f &&
                   Mathf.Abs(relative.y) <= areaSize.y * 0.5f;
        }

        public bool CanTravel(Rigidbody2D body, Vector2 from, Vector2 to,
            LayerMask obstacleLayers, float skin)
        {
            if (body == null || !Contains(from) || !Contains(to))
                return false;

            Vector2 delta = to - from;
            float distance = delta.magnitude;
            if (distance <= 0.000001f)
                return true;

            Vector2 direction = delta / distance;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(obstacleLayers);
            filter.useTriggers = false;
            hits.Clear();

            // 実際の信長のColliderを仮想位置からCast。Transformは動かさない。
            int count = body.Cast(from, body.rotation, direction,
                filter, hits, distance + skin);

            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider != null &&
                    Vector2.Dot(direction, hits[i].normal) < -0.0001f)
                    return false;
            }
            return true;
        }

        public bool TryFindPath(Rigidbody2D body, Vector2 target,
            LayerMask obstacleLayers, float skin, List<Vector2> result)
        {
            result.Clear();
            if (body == null || !isActiveAndEnabled ||
                !Contains(body.position) || !Contains(target))
                return false;

            Vector2 start = body.position;
            if (CanTravel(body, start, target, obstacleLayers, skin))
            {
                result.Add(target);
                return true;
            }

            float step = Mathf.Max(0.1f, cellSize);
            columns = Mathf.Max(1, Mathf.CeilToInt(areaSize.x / step));
            rows = Mathf.Max(1, Mathf.CeilToInt(areaSize.y / step));
            long cellCount = (long)columns * rows;
            if (cellCount > maximumCells)
            {
                if (!warnedSize)
                    Debug.LogError("探索マスが上限を超えています。Area Sizeを小さくするかCell Sizeを大きくしてください。", this);
                warnedSize = true;
                return false;
            }

            int size = (int)cellCount;
            int startIndex = size;
            int goalIndex = size + 1;
            if (costs == null || costs.Length != size + 2)
            {
                costs = new float[size + 2];
                parents = new int[size + 2];
                closed = new bool[size + 2];
            }

            spacing = new Vector2(areaSize.x / columns, areaSize.y / rows);
            origin = (Vector2)transform.position - areaSize * 0.5f;
            for (int i = 0; i < costs.Length; i++)
            {
                costs[i] = float.PositiveInfinity;
                parents[i] = -1;
                closed[i] = false;
            }
            open.Clear();
            costs[startIndex] = 0f;
            open.Add(new OpenNode(startIndex, Vector2.Distance(start, target)));

            Vector2Int startCell = CellOf(start);
            Vector2Int goalCell = CellOf(target);
            while (open.Count > 0)
            {
                OpenNode entry = open.Min;
                open.Remove(entry);
                int current = entry.Index;
                if (closed[current])
                    continue;
                if (current == goalIndex)
                {
                    int cursor = goalIndex;
                    while (cursor != startIndex)
                    {
                        result.Add(cursor == goalIndex ? target : CellPosition(cursor));
                        cursor = parents[cursor];
                    }
                    result.Reverse();
                    SmoothPath(body, start, obstacleLayers, skin, result);
                    return true;
                }

                closed[current] = true;
                Vector2 from = current == startIndex ? start : CellPosition(current);
                Vector2Int cell = current == startIndex
                    ? startCell : new Vector2Int(current % columns, current / columns);

                // 実際の目標位置に接続する。壁の向こうのマスに勝手に置き換えない。
                if (Mathf.Abs(cell.x - goalCell.x) <= 1 &&
                    Mathf.Abs(cell.y - goalCell.y) <= 1)
                    Relax(current, goalIndex, from, target, target,
                        body, obstacleLayers, skin);

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (current != startIndex && dx == 0 && dy == 0)
                            continue;
                        int x = cell.x + dx;
                        int y = cell.y + dy;
                        if (x < 0 || y < 0 || x >= columns || y >= rows)
                            continue;
                        int next = y * columns + x;
                        Relax(current, next, from, CellPosition(next), target,
                            body, obstacleLayers, skin);
                    }
                }
            }
            return false;
        }

        private void SmoothPath(Rigidbody2D body, Vector2 start, LayerMask walls,
            float skin, List<Vector2> path)
        {
            int read = 0, write = 0;
            Vector2 from = start;
            while (read < path.Count)
            {
                int next = path.Count - 1;
                while (next > read && !CanTravel(body, from, path[next], walls, skin)) next--;
                from = path[next];
                path[write++] = from;
                read = next + 1;
            }
            if (write < path.Count) path.RemoveRange(write, path.Count - write);
        }

        private void Relax(int current, int next, Vector2 from, Vector2 to,
            Vector2 target, Rigidbody2D body, LayerMask mask, float skin)
        {
            if (closed[next])
                return;
            float cost = costs[current] + Vector2.Distance(from, to);
            if (cost >= costs[next])
                return;
            // 斜め移動も経路全体を調べるので、障害物の角をすり抜けない。
            if (!CanTravel(body, from, to, mask, skin))
                return;
            costs[next] = cost;
            parents[next] = current;
            open.Add(new OpenNode(next, cost + Vector2.Distance(to, target)));
        }

        private Vector2Int CellOf(Vector2 point)
        {
            Vector2 offset = point - origin;
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(offset.x / spacing.x), 0, columns - 1),
                Mathf.Clamp(Mathf.FloorToInt(offset.y / spacing.y), 0, rows - 1));
        }

        private Vector2 CellPosition(int index)
        {
            return origin + new Vector2(
                (index % columns + 0.5f) * spacing.x,
                (index / columns + 0.5f) * spacing.y);
        }

        private void OnValidate()
        {
            areaSize.x = Mathf.Max(0.1f, areaSize.x);
            areaSize.y = Mathf.Max(0.1f, areaSize.y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position,
                new Vector3(areaSize.x, areaSize.y, 0f));
        }
    }
}
