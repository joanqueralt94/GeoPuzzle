# Map pipeline

`build_map.py` turns Natural Earth Admin 0 countries (1:10m, public domain) into the game's map and country list.

```
pip install -r Tools/map_pipeline/requirements.txt
python3 Tools/map_pipeline/build_map.py
```

It also needs Node.js, because it runs [mapshaper](https://github.com/mbloch/mapshaper) through `npx` to simplify the shapes while keeping shared borders identical.

What it does:

1. Downloads Natural Earth into `data_sources/` (git-ignored) on the first run.
2. Drops Antarctica and simplifies all shapes to 6% of their points.
3. Projects to Miller cylindrical: longitude -180..180 becomes x -18..18.
4. Drops tiny islets (keeping each country's largest part), triangulates every polygon, and finds a label point inside each country.
5. Writes `Assets/_Project/Data/Map/world_map.json` and `Assets/_Project/Data/countries.json`.

Ids are ISO 3166-1 alpha-3 codes where they exist. Kosovo uses `KOS`. Patches without a country code (military bases, Somaliland, Northern Cyprus, disputed reefs) are drawn as grey, non-playable land.

`countries.json` gets continent, subregion, population, area and land neighbours from the data. Hand-edited `difficulty` values and extra tags are kept when the script runs again.

Tunables at the top of the script: `SIMPLIFY` (detail), `MIN_PART_AREA` (smallest islet kept) and `SMALL_COUNTRY_SIZE` (which countries get a tap dot).
