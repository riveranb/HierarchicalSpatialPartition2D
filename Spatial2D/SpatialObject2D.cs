using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spatial
{
    [DisallowMultipleComponent]
    public class SpatialObject2D : MonoBehaviour, ISpatial2D
    {
        #region variables
        public static float _SizeMinimum = 0.001f;

        public Transform overrideTransform = null;
        public Bounds bounds;
        public Rect rect;
        public int priority = 0; // higher processed first

#if UNITY_EDITOR
        private Renderer[] cachedRenderers = null;
#endif
        #endregion

        #region properties
        public bool Spatializable => bounds.size != Vector3.zero;
        #endregion

        #region ISpatial2D
        public int Priority => priority;
        public Rect RectBound
        {
            get => rect;
            set { rect = value; }
        }

        public void CalculateBounds()
        {
            bounds.center = overrideTransform == null ? transform.position : overrideTransform.position;
            bounds.extents = Vector3.zero;
            var renderers = GetComponentsInChildren<Renderer>(true); // include disabled
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer)
                {
                    continue;
                }

                if (bounds.extents == Vector3.zero)
                {
                    bounds = renderers[i].bounds;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            rect.min = bounds.min;
            rect.max = bounds.max;
            if (rect.size == Vector2.zero)
            {
                rect.size = new Vector2(_SizeMinimum, _SizeMinimum); // force tiny size
            }

#if UNITY_EDITOR
            cachedRenderers = renderers;
#endif
        }
        #endregion

        private void Start()
        {
            CalculateBounds();
        }

#if UNITY_EDITOR
        public void EnableRenderers(bool enabled)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                cachedRenderers[i].enabled = enabled;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (rect.size == Vector2.zero)
            {
                return;
            }

            if (Selection.activeObject == gameObject ||
                System.Array.IndexOf(Selection.gameObjects, gameObject) >= 0)
            {
                EditorHelper.DrawRectGizmos(rect);
            }
        }
#endif

        public virtual SpatialUnit2D ToSpatialUnit()
        {
            var unit = new SpatialUnit2D()
            {
                RectBound = this.RectBound,
                cell = new Vector2Int(-1, -1),
                priority = (sbyte)this.priority,
                parent = transform.parent,
                namekey = gameObject.name,
                worldPos = transform.position,
            };

            return unit;
        }
    }
}

