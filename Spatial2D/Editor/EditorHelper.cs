using UnityEngine;
using UnityEditor;

namespace Spatial
{
#if UNITY_EDITOR
    public static class EditorHelper
    {
        public static void DrawRectGizmos(Rect rect)
        {
            var bmin = rect.min;
            var bmax = rect.max;
            Gizmos.DrawLine(bmin, new Vector2(bmax.x, bmin.y));
            Gizmos.DrawLine(new Vector2(bmax.x, bmin.y), bmax);
            Gizmos.DrawLine(bmax, new Vector2(bmin.x, bmax.y));
            Gizmos.DrawLine(new Vector2(bmin.x, bmax.y), bmin);
        }

        public static void DrawRectDotted(Rect rect, float width = 2)
        {
            var bmin = rect.min;
            var bmax = rect.max;
            Handles.DrawDottedLine(bmin, new Vector2(bmax.x, bmin.y), width);
            Handles.DrawDottedLine(new Vector2(bmax.x, bmin.y), bmax, width);
            Handles.DrawDottedLine(bmax, new Vector2(bmin.x, bmax.y), width);
            Handles.DrawDottedLine(new Vector2(bmin.x, bmax.y), bmin, width);
        }
    }
#endif
}
