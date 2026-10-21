# Map pipeline

Offline Python scripts that turn Natural Earth Admin 0 (1:10m) shapes into the game's world map data:

1. Download Natural Earth and join each shape to its ISO 3166-1 alpha-3 code.
2. Simplify to a vertex budget per detail level.
3. Project to Miller cylindrical and compute bounds, a label point and a tap radius per country.
4. Write the file Unity imports into the `WorldMapData` asset.

Downloads go in `data_sources/`, which is git-ignored. Not implemented yet; this is the next milestone.
