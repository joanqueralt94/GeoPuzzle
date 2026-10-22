"""Rasterise a flag for every country in countries.json.

Usage:
    pip install -r Tools/flags/requirements.txt
    python3 Tools/flags/build_flags.py

Flags come from flag-icons (MIT licence, https://github.com/lipis/flag-icons),
downloaded from the npm registry at a pinned version. Each 4:3 SVG becomes
Assets/_Project/Art/Flags/flag_<iso2>.png, the name countries.json uses.
"""
import json
import shutil
import sys
import tarfile
import urllib.request
from pathlib import Path

import resvg_py

ROOT = Path(__file__).resolve().parents[2]
VERSION = "7.6.0"
URL = f"https://registry.npmjs.org/flag-icons/-/flag-icons-{VERSION}.tgz"
SOURCES = Path(__file__).resolve().parent / "data_sources"
ARCHIVE = SOURCES / f"flag-icons-{VERSION}.tgz"
COUNTRIES = ROOT / "Assets/_Project/Data/countries.json"
OUT = ROOT / "Assets/_Project/Art/Flags"
WIDTH, HEIGHT = 256, 192


def main():
    SOURCES.mkdir(parents=True, exist_ok=True)
    if not ARCHIVE.exists():
        print(f"downloading {URL}")
        urllib.request.urlretrieve(URL, ARCHIVE)

    countries = json.loads(COUNTRIES.read_text())["countries"]
    OUT.mkdir(parents=True, exist_ok=True)
    missing = []
    with tarfile.open(ARCHIVE) as archive:
        for country in countries:
            code = country["iso2"].lower()
            try:
                svg = archive.extractfile(f"package/flags/4x3/{code}.svg").read()
            except KeyError:
                missing.append(country["id"])
                continue
            png = resvg_py.svg_to_bytes(svg_string=svg.decode("utf-8"), width=WIDTH, height=HEIGHT,
                                        skip_system_fonts=True)
            (OUT / f"{country['flag']}.png").write_bytes(png)
        licence = archive.extractfile("package/LICENSE").read()
    (OUT / "LICENSE-flag-icons.txt").write_bytes(licence)

    if missing:
        print(f"no flag for: {', '.join(missing)}")
        return 1
    print(f"wrote {len(countries)} flags to {OUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
