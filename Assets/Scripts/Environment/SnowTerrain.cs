using UnityEngine;

namespace WinterDuel.Environment
{
    /// <summary>A broad, deterministic snow surface, filled down to a fixed bottom.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SnowTerrain : MonoBehaviour
    {
        [Header("Shape (local units)")]
        [Min(1f)] public float width = 40f;
        public float surfaceHeight = -2.8f;
        public float bottomHeight = -8f;
        [Min(0f)] public float amplitude = 0.32f;
        [Min(2f)] public float smoothness = 12f;
        [Range(16, 512)] public int segments = 240;
        public int seed = 7341;

        [Header("Appearance")]
        public Color surfaceColor = new Color(0.9f, 0.97f, 0.99f, 1f);
        public Color bottomColor = new Color(0.75f, 0.88f, 0.94f, 1f);
        public Material snowMaterial;
        public int sortingOrder = 20;

        [Header("Surface collision")]
        [Tooltip("Enable only on the playable foreground. Uses the exact visible mesh samples.")]
        public bool generateCollider = true;

        private Mesh generatedMesh;

        private void OnEnable() => Rebuild();

        private void OnValidate()
        {
            width = Mathf.Max(1f, width);
            amplitude = Mathf.Max(0f, amplitude);
            smoothness = Mathf.Max(2f, smoothness);
            segments = Mathf.Clamp(segments, 16, 512);
            bottomHeight = Mathf.Min(bottomHeight, surfaceHeight - amplitude - 0.1f);
#if UNITY_EDITOR
            // OnValidate can run during asset loading, when component changes are forbidden.
            UnityEditor.EditorApplication.delayCall -= RebuildAfterValidation;
            UnityEditor.EditorApplication.delayCall += RebuildAfterValidation;
#endif
        }

#if UNITY_EDITOR
        private void RebuildAfterValidation()
        {
            if (this != null && isActiveAndEnabled) Rebuild();
        }
#endif

        /// <summary>Continuous deterministic profile, useful when authoring the shape.</summary>
        public float EvaluateLocalHeight(float x)
        {
            float phase = Mathf.Repeat(seed * 0.618034f, 97f);
            // Long waves with unrelated wavelengths avoid repetitive ripples.
            return surfaceHeight + amplitude * (
                0.64f * Mathf.Sin(x / smoothness * 2f * Mathf.PI + phase) +
                0.27f * Mathf.Sin(x / (smoothness * 1.79f) * 2f * Mathf.PI + phase * 1.73f) +
                0.09f * Mathf.Sin(x / (smoothness * 0.63f) * 2f * Mathf.PI + phase * 2.31f));
        }

        /// <summary>Exact point on the sampled mesh/collider, for future character placement.
        /// Pass local X; returns a world-space point, including this object's transform.</summary>
        public Vector3 GetWorldSurfacePoint(float localX)
        {
            float sample = Mathf.InverseLerp(-width * 0.5f, width * 0.5f, localX) * segments;
            int left = Mathf.Min(Mathf.FloorToInt(sample), segments - 1);
            float x0 = -width * 0.5f + width * left / segments;
            float x1 = x0 + width / segments;
            float y = Mathf.Lerp(EvaluateLocalHeight(x0), EvaluateLocalHeight(x1), sample - left);
            return transform.TransformPoint(new Vector3(Mathf.Clamp(localX, -width * 0.5f, width * 0.5f), y));
        }

        [ContextMenu("Rebuild Snow Surface")]
        public void Rebuild()
        {
            if (generatedMesh == null)
            {
                generatedMesh = new Mesh { name = name + " (procedural)", hideFlags = HideFlags.HideAndDontSave };
            }
            generatedMesh.Clear();
            var vertices = new Vector3[(segments + 1) * 2];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            var outline = new Vector2[segments + 3];
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float x = (t - 0.5f) * width;
                float y = EvaluateLocalHeight(x);
                vertices[i * 2] = new Vector3(x, y, 0);
                vertices[i * 2 + 1] = new Vector3(x, bottomHeight, 0);
                colors[i * 2] = QualitySettings.activeColorSpace == ColorSpace.Linear ? surfaceColor.linear : surfaceColor;
                colors[i * 2 + 1] = QualitySettings.activeColorSpace == ColorSpace.Linear ? bottomColor.linear : bottomColor;
                uv[i * 2] = new Vector2(t, 1);
                uv[i * 2 + 1] = new Vector2(t, 0);
                outline[i] = new Vector2(x, y);
                if (i == segments) continue;
                int v = i * 2;
                int k = i * 6;
                triangles[k] = v; triangles[k + 1] = v + 2; triangles[k + 2] = v + 1;
                triangles[k + 3] = v + 2; triangles[k + 4] = v + 3; triangles[k + 5] = v + 1;
            }
            outline[segments + 1] = new Vector2(width * 0.5f, bottomHeight);
            outline[segments + 2] = new Vector2(-width * 0.5f, bottomHeight);
            generatedMesh.vertices = vertices;
            generatedMesh.colors = colors;
            generatedMesh.uv = uv;
            generatedMesh.triangles = triangles;
            generatedMesh.RecalculateBounds();
            generatedMesh.RecalculateNormals();
            GetComponent<MeshFilter>().sharedMesh = generatedMesh;
            var renderer = GetComponent<MeshRenderer>();
            renderer.sharedMaterial = snowMaterial;
            renderer.sortingOrder = sortingOrder;
            var collider = GetComponent<PolygonCollider2D>();
            if (generateCollider)
            {
                if (collider == null) collider = gameObject.AddComponent<PolygonCollider2D>();
                collider.enabled = true;
                collider.pathCount = 1;
                collider.SetPath(0, outline);
            }
            else if (collider != null) collider.enabled = false;
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RebuildAfterValidation;
#endif
            if (generatedMesh == null) return;
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
    }
}
