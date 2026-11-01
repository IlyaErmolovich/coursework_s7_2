using UnityEngine;

namespace WinterDuel.Environment
{
    /// <summary>Three static soft bands formed by mesh geometry and vertex alpha.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class VertexHaze : MonoBehaviour
    {
        [Header("Coverage and detail")]
        [Min(1)] public float width = 80;
        [Range(32, 256)] public int horizontalSegments = 128;
        [Range(12, 48)] public int edgeSegments = 20;

        [Header("Static haze")]
        public Color hazeColor = new Color(0.79f, 0.77f, 0.83f, 1);
        [Range(0, 0.3f)] public float opacity = 0.065f;
        public float height = 0.25f;
        [Range(0.1f, 2)] public float edgeSoftness = 0.45f;
        [Range(0.4f, 3)] public float bandSpacing = 1.2f;
        [Range(0, 1)] public float edgeIrregularity = 0.3f;

        [Header("Standard material")]
        public Material material;
        public int sortingOrder = -90;
        private Mesh generatedMesh;

        private void OnEnable() => Rebuild();

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            horizontalSegments = Mathf.Clamp(horizontalSegments, 32, 256);
            edgeSegments = Mathf.Clamp(edgeSegments, 12, 48);
            edgeSoftness = Mathf.Clamp(edgeSoftness, 0.1f, 2);
            opacity = Mathf.Clamp(opacity, 0, 0.3f);
#if UNITY_EDITOR
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

        [ContextMenu("Rebuild Static Haze")]
        public void Rebuild()
        {
            if (generatedMesh == null)
                generatedMesh = new Mesh { name = "Static haze (vertex alpha)", hideFlags = HideFlags.HideAndDontSave };
            generatedMesh.Clear();
            int row = horizontalSegments + 1;
            int bandVertices = row * (edgeSegments + 1);
            var vertices = new Vector3[bandVertices * 3];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[horizontalSegments * edgeSegments * 6 * 3];
            Color tint = QualitySettings.activeColorSpace == ColorSpace.Linear ? hazeColor.linear : hazeColor;
            int triangle = 0;
            for (int band = 0; band < 3; band++)
            {
                float weight = band == 0 ? 1 : band == 1 ? 0.48f : 0.23f;
                float softness = edgeSoftness * (band == 0 ? 1 : band == 1 ? 0.78f : 0.55f);
                for (int y = 0; y <= edgeSegments; y++)
                {
                    float d = Mathf.Lerp(-3, 3, (float)y / edgeSegments);
                    for (int x = 0; x <= horizontalSegments; x++)
                    {
                        float px = ((float)x / horizontalSegments - 0.5f) * width;
                        float bend = edgeIrregularity * (0.65f * Mathf.Sin(px * 0.29f + 1.1f) + 0.35f * Mathf.Sin(px * 0.61f));
                        float center = band == 0 ? height + bend : band == 1
                            ? height + bandSpacing + bend * 0.6f + 0.12f * Mathf.Sin(px * 0.19f)
                            : height + bandSpacing * 2.15f - bend * 0.4f;
                        int index = band * bandVertices + y * row + x;
                        vertices[index] = new Vector3(px, center + d * softness, 0);
                        float patches = 0.74f + 0.26f * Mathf.Sin(px * 0.14f + 0.6f);
                        Color color = tint;
                        color.a = y == 0 || y == edgeSegments ? 0
                            : hazeColor.a * opacity * weight * patches * Mathf.Exp(-d * d);
                        colors[index] = color;
                        uv[index] = new Vector2((float)x / horizontalSegments, (float)y / edgeSegments);
                        if (x == horizontalSegments || y == edgeSegments) continue;
                        triangles[triangle++] = index;
                        triangles[triangle++] = index + row;
                        triangles[triangle++] = index + 1;
                        triangles[triangle++] = index + 1;
                        triangles[triangle++] = index + row;
                        triangles[triangle++] = index + row + 1;
                    }
                }
            }
            generatedMesh.vertices = vertices;
            generatedMesh.colors = colors;
            generatedMesh.uv = uv;
            generatedMesh.triangles = triangles;
            generatedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = generatedMesh;
            var renderer = GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
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
