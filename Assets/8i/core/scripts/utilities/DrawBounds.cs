using UnityEngine;
using System.Collections;

public static class DrawBounds
{
    public static void Draw(Bounds bounds, Transform transform)
    {
        Vector3 v3Center = bounds.center;
		Vector3 v3Extents = bounds.extents;

        Vector3 v3FrontTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top left corner
        Vector3 v3FrontTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top right corner
        Vector3 v3FrontBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom left corner
        Vector3 v3FrontBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom right corner
        Vector3 v3BackTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top left corner
        Vector3 v3BackTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top right corner
        Vector3 v3BackBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom left corner
        Vector3 v3BackBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom right corner

        v3FrontTopLeft = transform.TransformPoint(v3FrontTopLeft);
        v3FrontTopRight = transform.TransformPoint(v3FrontTopRight);
        v3FrontBottomLeft = transform.TransformPoint(v3FrontBottomLeft);
        v3FrontBottomRight = transform.TransformPoint(v3FrontBottomRight);
        v3BackTopLeft = transform.TransformPoint(v3BackTopLeft);
        v3BackTopRight = transform.TransformPoint(v3BackTopRight);
        v3BackBottomLeft = transform.TransformPoint(v3BackBottomLeft);
        v3BackBottomRight = transform.TransformPoint(v3BackBottomRight);

        Gizmos.DrawLine(v3FrontTopLeft, v3FrontTopRight);
        Gizmos.DrawLine(v3FrontTopRight, v3FrontBottomRight);
        Gizmos.DrawLine(v3FrontBottomRight, v3FrontBottomLeft);
        Gizmos.DrawLine(v3FrontBottomLeft, v3FrontTopLeft);

        Gizmos.DrawLine(v3BackTopLeft, v3BackTopRight);
        Gizmos.DrawLine(v3BackTopRight, v3BackBottomRight);
        Gizmos.DrawLine(v3BackBottomRight, v3BackBottomLeft);
        Gizmos.DrawLine(v3BackBottomLeft, v3BackTopLeft);

        Gizmos.DrawLine(v3FrontTopLeft, v3BackTopLeft);
        Gizmos.DrawLine(v3FrontTopRight, v3BackTopRight);
        Gizmos.DrawLine(v3FrontBottomRight, v3BackBottomRight);
        Gizmos.DrawLine(v3FrontBottomLeft, v3BackBottomLeft);
    }
}
