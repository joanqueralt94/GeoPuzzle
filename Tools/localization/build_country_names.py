"""Write country names for every country in countries.json, per language.

Usage:
    python3 Tools/localization/build_country_names.py

Names come from the Unicode CLDR (cldr-localenames-full on npm, pinned below;
Unicode License v3). Output: Assets/_Project/Data/Localization/countries.<lang>.json,
a flat map of "country.<ID>" to the name, read by the game's Localizer.
"""
import json
import sys
import tarfile
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
VERSION = "48.2.0"
URL = f"https://registry.npmjs.org/cldr-localenames-full/-/cldr-localenames-full-{VERSION}.tgz"
SOURCES = Path(__file__).resolve().parent / "data_sources"
ARCHIVE = SOURCES / f"cldr-localenames-full-{VERSION}.tgz"
COUNTRIES = ROOT / "Assets/_Project/Data/countries.json"
OUT = ROOT / "Assets/_Project/Data/Localization"

LANGUAGES = ["en", "es", "fr", "zh"]
# CLDR's default names for these are long or awkward in a quiz ("Congo - Kinshasa",
# "Hong Kong SAR China"); its alternative forms read better.
PREFERRED_ALT = {"CD": "variant", "CG": "variant", "HK": "short", "MO": "short", "PS": "short"}


def main():
    SOURCES.mkdir(parents=True, exist_ok=True)
    if not ARCHIVE.exists():
        print(f"downloading {URL}")
        urllib.request.urlretrieve(URL, ARCHIVE)

    countries = json.loads(COUNTRIES.read_text())["countries"]
    OUT.mkdir(parents=True, exist_ok=True)
    with tarfile.open(ARCHIVE) as archive:
        for lang in LANGUAGES:
            data = json.load(archive.extractfile(f"package/main/{lang}/territories.json"))
            territories = data["main"][lang]["localeDisplayNames"]["territories"]
            names, missing = {}, []
            for country in countries:
                code = country["iso2"]
                alt = PREFERRED_ALT.get(code)
                name = territories.get(f"{code}-alt-{alt}") if alt else None
                name = name or territories.get(code)
                if name:
                    names[country["nameKey"]] = name
                else:
                    missing.append(country["id"])
            if missing:
                print(f"{lang}: no name for {', '.join(missing)}")
                return 1
            path = OUT / f"countries.{lang}.json"
            path.write_text(json.dumps(names, indent=2, ensure_ascii=False, sort_keys=True) + "\n")
            print(f"wrote {len(names)} names to {path.relative_to(ROOT)}")
        licence = archive.extractfile("package/LICENSE").read()
    (Path(__file__).resolve().parent / "LICENSE-cldr.txt").write_bytes(licence)
    return 0


if __name__ == "__main__":
    sys.exit(main())
