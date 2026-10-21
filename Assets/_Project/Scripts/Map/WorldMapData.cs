using System;
using UnityEngine;

namespace GeoPuzzle.Map
{
    /// <summary>
    /// world_map.json as written by Tools/map_pipeline/build_map.py. Coordinates are map units in a
    /// Miller projection, with longitude -180..180 mapped to x -18..18.
    /// </summary>
    [Serializable]
    public class WorldMapFile
    {
        public string projection;
        public string source;
        public float[] bounds = new float[4];
        public MapCountry[] countries = Array.Empty<MapCountry>();

        public static WorldMapFile FromJson(string json) => JsonUtility.FromJson<WorldMapFile>(json);

        public Rect BoundsRect => Rect.MinMaxRect(bounds[0], bounds[1], bounds[2], bounds[3]);
    }

    [Serializable]
    public class MapCountry
    {
        public string id;
        /// <summary>False for slivers that are drawn as land but are not quiz answers (bases, disputed patches).</summary>
        public bool playable;
        /// <summary>True when the country is too small to hit with a finger; tapping near its label selects it.</summary>
        public bool small;
        public float[] bounds = new float[4];
        public float[] label = new float[2];
        public MapPolygon[] polygons = Array.Empty<MapPolygon>();

        public bool BoundsContain(float x, float y) =>
            x >= bounds[0] && x <= bounds[2] && y >= bounds[1] && y <= bounds[3];
    }

    /// <summary>One polygon with holes: all rings' vertices in one flat x,y list, pre-triangulated.</summary>
    [Serializable]
    public class MapPolygon
    {
        public float[] vertices = Array.Empty<float>();
        public int[] ringStarts = Array.Empty<int>();
        public int[] triangles = Array.Empty<int>();

        public int VertexCount => vertices.Length / 2;

        public int RingEnd(int ring) => ring + 1 < ringStarts.Length ? ringStarts[ring + 1] : VertexCount;
    }
}
