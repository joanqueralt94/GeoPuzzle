"""Build GeoPuzzle's world map and country list from Natural Earth.

Usage:
    pip install -r Tools/map_pipeline/requirements.txt
    python3 Tools/map_pipeline/build_map.py

Needs Node.js for mapshaper (run through npx), which simplifies the shapes
while keeping shared borders identical between neighbouring countries.

Writes:
    Assets/_Project/Data/Map/world_map.json  shapes, triangles, label points
    Assets/_Project/Data/countries.json      one record per playable country
"""
import json
import math
import subprocess
import sys
import urllib.request
from pathlib import Path

import mapbox_earcut as earcut
import numpy as np
from shapely.geometry import MultiPolygon, Polygon, shape
from shapely.ops import polylabel
from shapely.strtree import STRtree

ROOT = Path(__file__).resolve().parents[2]
SOURCES = Path(__file__).resolve().parent / "data_sources"
SOURCE_URL = ("https://raw.githubusercontent.com/nvkelso/natural-earth-vector/"
              "master/geojson/ne_10m_admin_0_countries.geojson")
SOURCE = SOURCES / "ne_10m_admin_0_countries.geojson"
SIMPLIFIED = SOURCES / "simplified.geojson"
MAP_OUT = ROOT / "Assets/_Project/Data/Map/world_map.json"
COUNTRIES_OUT = ROOT / "Assets/_Project/Data/countries.json"

MAPSHAPER = "mapshaper@0.6.102"
SIMPLIFY = "6%"
EXCLUDED = {"ATA"}  # Antarctica: not a country, and huge in a cylindrical projection

# Map units: longitude -180..180 maps to x -18..18.
HALF_WIDTH = 18.0
# Island parts smaller than this (map units squared) are dropped, unless they are
# the country's largest part. Keeps tiny islets from bloating the mesh.
MIN_PART_AREA = 0.0004
# Playable countries whose main shape is smaller than this (map units) count as "small":
# the game lets a finger-sized tap near their label pick them over a bigger neighbour.
SMALL_COUNTRY_SIZE = 0.24
DECIMALS = 4

KIND_TAGS = {
    "Sovereign country": "sovereign",
    "Country": "territory",
    "Dependency": "dependency",
    "Disputed": "disputed",
    "Indeterminate": "indeterminate",
    "Sovereignty": "sovereign",
    "Lease": "lease",
}


def kind_tag(p):
    # A "Country" whose admin is its own sovereign (France, USA) is a sovereign state, not a territory.
    if p["TYPE"] in ("Sovereign country", "Country") and p["ADMIN"] == p["SOVEREIGNT"]:
        return "sovereign"
    return KIND_TAGS.get(p["TYPE"], "other")


def download():
    if SOURCE.exists():
        return
    SOURCES.mkdir(parents=True, exist_ok=True)
    print(f"downloading {SOURCE_URL}")
    urllib.request.urlretrieve(SOURCE_URL, SOURCE)


def simplify():
    excluded = " && ".join(f"ADM0_A3 != '{code}'" for code in sorted(EXCLUDED))
    cmd = ["npx", "-y", MAPSHAPER, str(SOURCE),
           "-filter", excluded,
           "-simplify", SIMPLIFY, "keep-shapes",
           "-o", "format=geojson", "precision=0.00001", str(SIMPLIFIED)]
    subprocess.run(cmd, check=True)


def miller(lon, lat):
    x = lon / 180.0 * HALF_WIDTH
    phi = math.radians(max(-89.0, min(89.0, lat)))
    y = 1.25 * math.log(math.tan(math.pi / 4 + 0.4 * phi))
    return x, y / math.pi * HALF_WIDTH


def equal_area_km2(geom):
    """Area on a sphere via Lambert cylindrical equal-area (x = R*lon, y = R*sin(lat))."""
    radius = 6371.0088

    def project(poly):
        def ring(coords):
            return [(math.radians(lon) * radius, math.sin(math.radians(lat)) * radius) for lon, lat in coords]
        return Polygon(ring(poly.exterior.coords), [ring(r.coords) for r in poly.interiors])

    return sum(project(p).area for p in parts(geom))


def parts(geom):
    return list(geom.geoms) if isinstance(geom, MultiPolygon) else [geom]


def assign_ids(features):
    """ISO 3166-1 alpha-3 where Natural Earth has one, its own ADM0_A3 code otherwise."""
    props = [f["properties"] for f in features]
    claims = {}
    for p in props:
        code = p["ISO_A3"] if p["ISO_A3"] != "-99" else p["ISO_A3_EH"]
        if code != "-99":
            claims.setdefault(code, []).append(p)
    ids = []
    for p in props:
        code = p["ISO_A3"] if p["ISO_A3"] != "-99" else p["ISO_A3_EH"]
        owners = claims.get(code, [])
        if code != "-99" and (len(owners) == 1 or p["ADM0_A3"] == code):
            ids.append((code, True))
        else:
            # Kosovo has no official ISO code but is widely listed as a country.
            ids.append((p["ADM0_A3"], p["ADM0_A3"] == "KOS"))
    return ids


def round_area(km2):
    return round(km2, 2) if km2 < 100 else round(km2)


def rnd(v):
    return round(v, DECIMALS)


def build_polygon(poly):
    """Projects one polygon; returns flat vertices, ring start indices and triangles."""
    rings = [poly.exterior.coords] + [r.coords for r in poly.interiors]
    vertices, ring_starts, rings_xy = [], [], []
    for ring in rings:
        pts = [miller(lon, lat) for lon, lat in list(ring)[:-1]]
        if len(pts) < 3:
            continue
        ring_starts.append(len(vertices) // 2)
        rings_xy.append(pts)
        for x, y in pts:
            vertices += [rnd(x), rnd(y)]
    if not rings_xy:
        return None
    coords = [(vertices[i], vertices[i + 1]) for i in range(0, len(vertices), 2)]
    ends, total = [], 0
    for pts in rings_xy:
        total += len(pts)
        ends.append(total)
    triangles = earcut.triangulate_float64(np.array(coords, dtype=np.float64), np.array(ends, dtype=np.uint32)).tolist()
    return {"vertices": vertices, "ringStarts": ring_starts, "triangles": triangles}


def projected(poly):
    return Polygon([miller(*c) for c in poly.exterior.coords],
                   [[miller(*c) for c in r.coords] for r in poly.interiors])


def main():
    download()
    simplify()
    features = json.loads(SIMPLIFIED.read_text())["features"]
    ids = assign_ids(features)
    geoms = [shape(f["geometry"]) for f in features]

    map_countries, records = [], []
    total_vertices = 0
    for feature, (cid, playable), geom in zip(features, ids, geoms):
        p = feature["properties"]
        proj_parts = sorted(((projected(part), part) for part in parts(geom)),
                            key=lambda pair: pair[0].area, reverse=True)
        kept = [pair for i, pair in enumerate(proj_parts) if i == 0 or pair[0].area >= MIN_PART_AREA]

        polygons = []
        for proj_poly, part in kept:
            built = build_polygon(part)
            if built:
                polygons.append(built)
                total_vertices += len(built["vertices"]) // 2

        main_part = kept[0][0]
        label = polylabel(main_part, tolerance=0.01)
        union = MultiPolygon([pp for pp, _ in kept]) if len(kept) > 1 else main_part
        minx, miny, maxx, maxy = union.bounds
        size = max(main_part.bounds[2] - main_part.bounds[0], main_part.bounds[3] - main_part.bounds[1])
        map_countries.append({
            "id": cid,
            "playable": playable,
            "bounds": [rnd(minx), rnd(miny), rnd(maxx), rnd(maxy)],
            "label": [rnd(label.x), rnd(label.y)],
            "small": playable and size < SMALL_COUNTRY_SIZE,
            "polygons": polygons,
        })
        if playable:
            # ISO_A2 is "CN-TW" for Taiwan and "-99" for France/Norway; ISO_A2_EH has the plain code.
            iso2 = p["ISO_A2"] if len(p["ISO_A2"]) == 2 else p["ISO_A2_EH"]
            records.append({
                "id": cid,
                "iso2": iso2,
                "nameKey": f"country.{cid}",
                "capitalKey": f"capital.{cid}",
                "continent": p["CONTINENT"],
                "subregion": p["SUBREGION"],
                "population": int(p["POP_EST"] or 0),
                "areaKm2": round_area(equal_area_km2(geom)),
                "neighbours": [],
                "flag": f"flag_{iso2.lower()}",
                "difficulty": 1,
                "tags": [kind_tag(p)],
            })

    add_neighbours(records, features, ids, geoms)
    merge_existing(records)

    xs = [c["bounds"][0] for c in map_countries] + [c["bounds"][2] for c in map_countries]
    ys = [c["bounds"][1] for c in map_countries] + [c["bounds"][3] for c in map_countries]
    world = {
        "projection": "miller",
        "source": "Natural Earth 1:10m Admin 0 countries (public domain)",
        "bounds": [rnd(min(xs)), rnd(min(ys)), rnd(max(xs)), rnd(max(ys))],
        "countries": sorted(map_countries, key=lambda c: c["id"]),
    }
    MAP_OUT.parent.mkdir(parents=True, exist_ok=True)
    MAP_OUT.write_text(json.dumps(world, separators=(",", ":")) + "\n")
    records.sort(key=lambda r: r["id"])
    COUNTRIES_OUT.write_text(json.dumps({"countries": records}, indent=2, ensure_ascii=False) + "\n")

    check(world, records)
    print(f"{len(map_countries)} map shapes ({total_vertices} vertices), {len(records)} playable countries")
    print(f"wrote {MAP_OUT.relative_to(ROOT)} ({MAP_OUT.stat().st_size // 1024} KB) and {COUNTRIES_OUT.relative_to(ROOT)}")


def add_neighbours(records, features, ids, geoms):
    """Countries that share a land border, from the simplified shapes (borders are shared exactly)."""
    by_id = {r["id"]: r for r in records}
    playable = [(cid, g) for (cid, ok), g in zip(ids, geoms) if ok]
    tree = STRtree([g for _, g in playable])
    for i, (cid, geom) in enumerate(playable):
        for j in tree.query(geom, predicate="intersects"):
            other = playable[j][0]
            if other != cid and geom.intersection(playable[j][1]).length > 0:
                by_id[cid]["neighbours"].append(other)
    for r in records:
        r["neighbours"] = sorted(set(r["neighbours"]))


def merge_existing(records):
    """Keeps hand-tuned fields (difficulty, extra tags) from the current countries.json."""
    if not COUNTRIES_OUT.exists():
        return
    existing = {c["id"]: c for c in json.loads(COUNTRIES_OUT.read_text())["countries"]}
    for r in records:
        old = existing.get(r["id"])
        if not old:
            continue
        r["difficulty"] = old.get("difficulty", r["difficulty"])
        extra = [t for t in old.get("tags", []) if t not in KIND_TAGS.values() and t != "sample"]
        r["tags"] = r["tags"] + [t for t in extra if t not in r["tags"]]


def check(world, records):
    ids = [c["id"] for c in world["countries"]]
    assert len(ids) == len(set(ids)), "duplicate map ids"
    record_ids = {r["id"] for r in records}
    playable = {c["id"] for c in world["countries"] if c["playable"]}
    assert record_ids == playable, "countries.json and playable map shapes differ"
    for c in world["countries"]:
        assert c["polygons"], f"{c['id']} has no shape"
        for poly in c["polygons"]:
            assert len(poly["triangles"]) % 3 == 0 and poly["triangles"], f"{c['id']} triangulation failed"
    for r in records:
        for n in r["neighbours"]:
            assert n in record_ids, f"{r['id']} neighbour {n} missing"


if __name__ == "__main__":
    sys.exit(main())
