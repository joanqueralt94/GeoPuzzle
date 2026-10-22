using System.Collections.Generic;

namespace GeoPuzzle.Map
{
    /// <summary>Finds which playable country is under a point on the map.</summary>
    public class MapHitTester
    {
        readonly List<MapCountry> playable = new List<MapCountry>();
        readonly List<MapCountry> small = new List<MapCountry>();

        public MapHitTester(IEnumerable<MapCountry> countries)
        {
            foreach (var country in countries)
            {
                if (!country.playable)
                    continue;
                playable.Add(country);
                if (country.small)
                    small.Add(country);
            }
        }

        /// <summary>
        /// Returns the country id at (x, y), or null for sea and non-playable land.
        /// A small country whose label is within <paramref name="fingerRadius"/> wins over the shape under
        /// the finger, so microstates stay tappable; zooming in shrinks the radius in map units.
        /// </summary>
        public string Find(float x, float y, float fingerRadius)
        {
            MapCountry nearest = null;
            var nearestDistanceSq = fingerRadius * fingerRadius;
            foreach (var country in small)
            {
                var dx = country.label[0] - x;
                var dy = country.label[1] - y;
                var distanceSq = dx * dx + dy * dy;
                if (distanceSq <= nearestDistanceSq)
                {
                    nearest = country;
                    nearestDistanceSq = distanceSq;
                }
            }
            if (nearest != null)
                return nearest.id;

            foreach (var country in playable)
            {
                if (!country.BoundsContain(x, y))
                    continue;
                foreach (var polygon in country.polygons)
                    if (Contains(polygon, x, y))
                        return country.id;
            }
            return null;
        }

        /// <summary>Even-odd test across all rings, so holes (lakes, enclaves) count as outside.</summary>
        public static bool Contains(MapPolygon polygon, float x, float y)
        {
            var inside = false;
            var v = polygon.vertices;
            for (var ring = 0; ring < polygon.ringStarts.Length; ring++)
            {
                var start = polygon.ringStarts[ring];
                var end = polygon.RingEnd(ring);
                for (int i = start, j = end - 1; i < end; j = i++)
                {
                    float xi = v[2 * i], yi = v[2 * i + 1], xj = v[2 * j], yj = v[2 * j + 1];
                    if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                        inside = !inside;
                }
            }
            return inside;
        }
    }
}
