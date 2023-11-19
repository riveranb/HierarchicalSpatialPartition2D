using UnityEngine;

namespace Spatial
{
    public static class SpatialHelper
    {
        private static Vector3[] _PointBuffer = new Vector3[8];

        public static void CreateRectFromCamera(Camera cam, out Rect rect)
        {
            rect = new Rect();
            // must set size first
            rect.size = new Vector2((cam.orthographicSize + cam.orthographicSize) * cam.aspect,
                cam.orthographicSize + cam.orthographicSize);
            // must set center after size
            rect.center = cam.transform.position;
        }

        public static void Bounds2DToRect(Bounds bound, out Rect rect)
        {
            rect = new Rect();
            rect.size = new Vector2(bound.size.x, bound.size.y);
            rect.center = bound.center; // must set center after size
        }

        public static Rect CalculateRectBound(Transform root, bool calculateAll = true, float minsize = 0.02f)
        {
            var bound3d = CalculateRenderBound3D(root, calculateAll);
            Bounds2DToRect(bound3d, out Rect rect);
            if (rect.size == Vector2.zero)
            {
                rect.size = new Vector2(minsize, minsize); // force tiny size
            }
            return rect;
        }

        public static Bounds CalculateRenderBound3D(Transform root, bool includeAll = true)
        {
            Bounds bound3d = new Bounds();
            bound3d.center = root.position;
            bound3d.extents = Vector3.zero;
            // include disabled by default
            var renderers = root.GetComponentsInChildren<Renderer>(includeAll);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer)
                {
                    continue;
                }

                if (bound3d.extents == Vector3.zero)
                {
                    bound3d = renderers[i].bounds;
                }
                else
                {
                    bound3d.Encapsulate(renderers[i].bounds);
                }
            }
            return bound3d;
        }

        public static void ApplyTransformOnBounds(Transform t, ref Bounds bounds)
        {
            var inputMin = bounds.min;
            var inputMax = bounds.max;
            bounds.size = Vector3.zero;
            bounds.center = t.TransformPoint(new Vector3(inputMin.x, inputMin.y, inputMin.z));
            bounds.Encapsulate(t.TransformPoint(new Vector3(inputMin.x, inputMin.y, inputMax.z)));
            bounds.Encapsulate(t.TransformPoint(new Vector3(inputMin.x, inputMax.y, inputMin.z)));
            bounds.Encapsulate(t.TransformPoint(new Vector3(inputMin.x, inputMax.y, inputMax.z)));
            bounds.Encapsulate(t.TransformPoint(new Vector3(inputMax.x, inputMin.y, inputMin.z)));
            bounds.Encapsulate(t.TransformPoint(new Vector3(inputMax.x, inputMin.y, inputMax.z)));
            bounds.Encapsulate(t.TransformPoint(new Vector3(inputMax.x, inputMax.y, inputMin.z)));
            bounds.Encapsulate(t.TransformPoint(new Vector3(inputMax.x, inputMax.y, inputMax.z)));
        }

        public static void CalculateTransformedRectBound(Transform t, Bounds input, out Rect rect)
        {
            ApplyTransformOnBounds(t, ref input);
            Bounds2DToRect(input, out rect);
        }
    }
}

