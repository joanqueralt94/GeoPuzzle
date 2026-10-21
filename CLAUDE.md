# GeoPuzzle

Unity 6.3 LTS, 2D (URP 2D Renderer), Android first. The project root is the Unity project; `Tools/` holds offline Python scripts outside the build.

- Game code lives in `Assets/_Project/Scripts/<Area>` with one asmdef per area (`GeoPuzzle.Core`, `.Data`, `.Map`, `.Quiz`). `Data` depends on nothing; keep it that way.
- Countries are keyed by ISO 3166-1 alpha-3 ids everywhere. Player-facing text is a Localization key, never a literal string in data.
- Editor-wide and Android settings are applied by `Assets/_Project/Editor/ProjectSetup.cs`; change settings there rather than by hand.
- No Unity editor runs in cloud sessions. Code in `Data` and `Quiz` can be compile-checked and its tests run with Mono + NUnitLite using small UnityEngine stubs.
- Art, audio and fonts go through Git LFS (see `.gitattributes`). Never commit keystores.
