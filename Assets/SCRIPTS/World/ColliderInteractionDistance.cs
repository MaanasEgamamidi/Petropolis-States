using UnityEngine;

/// <summary>
/// Safe distance from a world point to a collider surface for interaction range checks.
/// <see cref="Collider.ClosestPoint"/> only works for box/sphere/capsule and convex mesh colliders;
/// other types log a physics warning — this falls back to the collider's world bounds.
/// </summary>
public static class ColliderInteractionDistance
{
    public static float DistanceToPoint(Collider collider, Vector3 worldPoint)
    {
        Vector3 onSurface = ClosestPointOnSurface(collider, worldPoint);
        return Vector3.Distance(onSurface, worldPoint);
    }

    static Vector3 ClosestPointOnSurface(Collider collider, Vector3 worldPoint)
    {
        if (CanUseColliderClosestPoint(collider))
            return collider.ClosestPoint(worldPoint);
        return collider.bounds.ClosestPoint(worldPoint);
    }

    static bool CanUseColliderClosestPoint(Collider c)
    {
        switch (c)
        {
            case BoxCollider:
            case SphereCollider:
            case CapsuleCollider:
                return true;
            case MeshCollider mesh:
                return mesh.convex;
            default:
                return false;
        }
    }
}
