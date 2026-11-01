using UnityEngine;

namespace WinterDuel.Environment
{
    /// <summary>Editable sky gradient rendered with the stock sprite unlit material.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class VertexGradientSky : MonoBehaviour
    {
        [Header("Coverage (local units)")]
        [Min(1)] public float width = 80;
        public float bottomHeight = -15;
        public float ceilingHeight = 15;
        [Range(16, 256)] public int verticalSegments = 128;

        [Header("Vertical gradient")]
        public Color upperColor = new Color(0.34f, 0.40f, 0.52f, 1);
        public Color middleColor = new Color(0.55f, 0.59f, 0.67f, 1);
        public Color horizonColor = new Color(0.72f, 0.70f, 0.76f, 1);
        public float horizonHeight = -1;
        public float middleHeight = 2.5f;
        public float upperHeight = 6;

        [Header("Standard material")]
        public Material material;
        public int sortingOrder = -100;
        private Mesh generatedMesh;

        private void OnEnable() => Rebuild();

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            ceilingHeight = Mathf.Max(bottomHeight + 1, ceilingHeight);
            verticalSegments = Mathf.Clamp(verticalSegments, 16, 256);
            middleHeight = Mathf.Max(horizonHeight + 0.01f, middleHeight);
            upperHeight = Mathf.Max(middleHeight + 0.01f, upperHeight);
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

        public Color EvaluateColor(float y)
        {
            // Interpolate in linear space, as the former sky material did.
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Color horizon = linear ? horizonColor.linear : horizonColor;
            Color middle = linear ? middleColor.linear : middleColor;
            Color upper = linear ? upperColor.linear : upperColor;
            float lowerT = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(horizonHeight, middleHeight, y));
            float upperT = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(middleHeight, upperHeight, y));
            Color result = Color.Lerp(Color.Lerp(horizon, middle, lowerT), upper, upperT);
            result.a = 1;
            return result;
        }

        [ContextMenu("Rebuild Sky Gradient")]
        public void Rebuild()
        {
            if (generatedMesh == null)
                generatedMesh = new Mesh { name = "Sky gradient (vertex colors)", hideFlags = HideFlags.HideAndDontSave };
            generatedMesh.Clear();
            var vertices = new Vector3[(verticalSegments + 1) * 2];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[verticalSegments * 6];
            for (int i = 0; i <= verticalSegments; i++)
            {
                float t = (float)i / verticalSegments;
                float y = Mathf.Lerp(bottomHeight, ceilingHeight, t);
                vertices[i * 2] = new Vector3(-width / 2, y, 0);
                vertices[i * 2 + 1] = new Vector3(width / 2, y, 0);
                colors[i * 2] = colors[i * 2 + 1] = EvaluateColor(y);
                uv[i * 2] = new Vector2(0, t);
                uv[i * 2 + 1] = new Vector2(1, t);
                if (i == verticalSegments) continue;
                int v = i * 2, k = i * 6;
                triangles[k] = v; triangles[k + 1] = v + 2; triangles[k + 2] = v + 1;
                triangles[k + 3] = v + 1; triangles[k + 4] = v + 2; triangles[k + 5] = v + 3;
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
