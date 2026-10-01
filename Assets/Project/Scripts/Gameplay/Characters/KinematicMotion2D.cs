using System.Collections.Generic;
using UnityEngine;

namespace Tenyu.Gameplay
{
    // Sweep, then slide along the wall. Recast the remainder from the contact position.
    public static class KinematicMotion2D
    {
        public static Vector2 Resolve(Rigidbody2D body, Vector2 displacement,
            LayerMask walls, float skin, List<RaycastHit2D> hits)
        {
            Vector2 position = body.position;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(walls);
            filter.useTriggers = false;
            for (int pass = 0; pass < 3 && displacement.sqrMagnitude > 0.00000001f; pass++)
            {
                float distance = displacement.magnitude;
                Vector2 direction = displacement / distance;
                float allowed = distance;
                Vector2 normal = Vector2.zero;
                hits.Clear();
                body.Cast(position, body.rotation, direction, filter, hits, distance + skin);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider == null || hit.rigidbody == body ||
                        Vector2.Dot(direction, hit.normal) >= -0.0001f) continue;
                    float travel = Mathf.Max(0f, hit.distance - skin);
                    if (travel >= allowed) continue;
                    allowed = travel;
                    normal = hit.normal;
                }
                position += direction * allowed;
                if (normal == Vector2.zero) break;
                displacement -= direction * allowed;
                displacement -= normal * Mathf.Min(0f, Vector2.Dot(displacement, normal));
            }
            return position;
        }
    }
}
