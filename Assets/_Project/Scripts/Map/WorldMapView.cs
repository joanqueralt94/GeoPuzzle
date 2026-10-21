using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GeoPuzzle.Map
{
    /// <summary>
    /// Draws the world map from world_map.json: one fill mesh coloured per vertex, a border line mesh,
    /// and a dot for each small country that keeps the same size on screen at any zoom.
    /// </summary>
    public class WorldMapView : MonoBehaviour
    {
        [SerializeField] TextAsset mapJson;
        [Tooltip("Unlit material that uses vertex colours. Sprites/Default is used when empty.")]
        [SerializeField] Material material;
        [SerializeField] Color landColor = new Color(0.56f, 0.76f, 0.47f);
        [SerializeField] Color nonPlayableColor = new Color(0.72f, 0.72f, 0.70f);
        [SerializeField] Color borderColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] Color oceanColor = new Color(0.55f, 0.78f, 0.93f);
        [Tooltip("Small countries get a dot in this colour so they stand out on top of their neighbours")]
        [SerializeField] Color dotColor = new Color(0.27f, 0.50f, 0.25f);
        [Tooltip("Small-country dot radius in screen pixels")]
        [SerializeField] float dotRadiusPixels = 7f;

        const int DotSegments = 12;

        readonly Dictionary<string, Range> fillRanges = new Dictionary<string, Range>();
        readonly Dictionary<string, Range> dotRanges = new Dictionary<string, Range>();
        readonly List<MapCountry> dotCountries = new List<MapCountry>();

        Mesh fillMesh;
        Mesh dotMesh;
        Color[] fillColors;
        Color[] dotColors;
        Vector3[] dotVertices;
        Camera cam;
        float lastDotOrthoSize = -1f;

        public WorldMapFile Data { get; private set; }
        public MapHitTester HitTester { get; private set; }
        public Rect Bounds => Data.BoundsRect;

        struct Range
        {
            public int Start;
            public int Count;
        }

        void Awake()
        {
            if (mapJson == null)
            {
                Debug.LogError("WorldMapView: world_map.json is not assigned");
                enabled = false;
                return;
            }

            Data = WorldMapFile.FromJson(mapJson.text);
            HitTester = new MapHitTester(Data.countries);
            if (material == null)
                material = new Material(Shader.Find("Sprites/Default"));

            BuildFillMesh();
            BuildBorderMesh();
            BuildDotMesh();
        }

        void Start()
        {
            cam = Camera.main;
            if (cam == null)
                return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = oceanColor;
            var controller = cam.GetComponent<MapCameraController>();
            if (controller != null)
                controller.FitTo(Bounds);
        }

        void LateUpdate()
        {
            if (cam != null && !Mathf.Approximately(cam.orthographicSize, lastDotOrthoSize))
                UpdateDotSize();
        }

        public bool HasCountry(string id) => fillRanges.ContainsKey(id);

        /// <summary>Recolours a country, including its dot if it has one.</summary>
        public void SetCountryColor(string id, Color color)
        {
            if (fillRanges.TryGetValue(id, out var fill))
            {
                Fill(fillColors, fill, color);
                fillMesh.colors = fillColors;
            }
            if (dotRanges.TryGetValue(id, out var dot))
            {
                Fill(dotColors, dot, color);
                dotMesh.colors = dotColors;
            }
        }

        public void ResetCountryColor(string id)
        {
            var country = Array.Find(Data.countries, c => c.id == id);
            if (country == null)
                return;
            SetCountryColor(id, country.playable ? landColor : nonPlayableColor);
            if (dotRanges.TryGetValue(id, out var dot))
            {
                Fill(dotColors, dot, dotColor);
                dotMesh.colors = dotColors;
            }
        }

        public Vector2 LabelPosition(string id)
        {
            var country = Array.Find(Data.countries, c => c.id == id);
            return country == null ? Vector2.zero : new Vector2(country.label[0], country.label[1]);
        }

        void BuildFillMesh()
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            foreach (var country in Data.countries)
            {
                var start = vertices.Count;
                var color = country.playable ? landColor : nonPlayableColor;
                foreach (var polygon in country.polygons)
                {
                    var offset = vertices.Count;
                    for (var i = 0; i < polygon.VertexCount; i++)
                    {
                        vertices.Add(new Vector3(polygon.vertices[2 * i], polygon.vertices[2 * i + 1], 0f));
                        colors.Add(color);
                    }
                    foreach (var index in polygon.triangles)
                        triangles.Add(offset + index);
                }
                fillRanges[country.id] = new Range { Start = start, Count = vertices.Count - start };
            }

            fillColors = colors.ToArray();
            fillMesh = NewMesh("Countries");
            fillMesh.SetVertices(vertices);
            fillMesh.colors = fillColors;
            fillMesh.SetTriangles(triangles, 0);
            fillMesh.RecalculateBounds();
            AddChild("Countries", fillMesh, 0);
        }

        void BuildBorderMesh()
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            foreach (var country in Data.countries)
            {
                foreach (var polygon in country.polygons)
                {
                    for (var ring = 0; ring < polygon.ringStarts.Length; ring++)
                    {
                        var start = polygon.ringStarts[ring];
                        var end = polygon.RingEnd(ring);
                        var offset = vertices.Count;
                        for (var i = start; i < end; i++)
                            vertices.Add(new Vector3(polygon.vertices[2 * i], polygon.vertices[2 * i + 1], 0f));
                        var count = end - start;
                        for (var i = 0; i < count; i++)
                        {
                            indices.Add(offset + i);
                            indices.Add(offset + (i + 1) % count);
                        }
                    }
                }
            }

            var colors = new Color[vertices.Count];
            for (var i = 0; i < colors.Length; i++)
                colors[i] = borderColor;

            var mesh = NewMesh("Borders");
            mesh.SetVertices(vertices);
            mesh.colors = colors;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            AddChild("Borders", mesh, 1);
        }

        void BuildDotMesh()
        {
            foreach (var country in Data.countries)
                if (country.small)
                    dotCountries.Add(country);

            var vertsPerDot = DotSegments + 1;
            dotVertices = new Vector3[dotCountries.Count * vertsPerDot];
            dotColors = new Color[dotVertices.Length];
            var triangles = new int[dotCountries.Count * DotSegments * 3];
            for (var d = 0; d < dotCountries.Count; d++)
            {
                var center = d * vertsPerDot;
                dotRanges[dotCountries[d].id] = new Range { Start = center, Count = vertsPerDot };
                for (var v = 0; v < vertsPerDot; v++)
                    dotColors[center + v] = dotColor;
                for (var s = 0; s < DotSegments; s++)
                {
                    var t = (d * DotSegments + s) * 3;
                    triangles[t] = center;
                    triangles[t + 1] = center + 1 + (s + 1) % DotSegments;
                    triangles[t + 2] = center + 1 + s;
                }
            }

            dotMesh = NewMesh("SmallCountryDots");
            dotMesh.vertices = dotVertices;
            dotMesh.colors = dotColors;
            dotMesh.triangles = triangles;
            AddChild("SmallCountryDots", dotMesh, 2);
        }

        void UpdateDotSize()
        {
            lastDotOrthoSize = cam.orthographicSize;
            var radius = dotRadiusPixels * 2f * cam.orthographicSize / Mathf.Max(1, Screen.height);
            var vertsPerDot = DotSegments + 1;
            for (var d = 0; d < dotCountries.Count; d++)
            {
                var label = dotCountries[d].label;
                var center = d * vertsPerDot;
                dotVertices[center] = new Vector3(label[0], label[1], 0f);
                for (var s = 0; s < DotSegments; s++)
                {
                    var angle = s * Mathf.PI * 2f / DotSegments;
                    dotVertices[center + 1 + s] = new Vector3(
                        label[0] + Mathf.Cos(angle) * radius, label[1] + Mathf.Sin(angle) * radius, 0f);
                }
            }
            dotMesh.vertices = dotVertices;
            dotMesh.RecalculateBounds();
        }

        static Mesh NewMesh(string name) => new Mesh { name = name, indexFormat = IndexFormat.UInt32 };

        void AddChild(string name, Mesh mesh, int sortingOrder)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = child.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = sortingOrder;
        }

        static void Fill(Color[] colors, Range range, Color color)
        {
            for (var i = range.Start; i < range.Start + range.Count; i++)
                colors[i] = color;
        }
    }
}
