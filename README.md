# notima

`notima` is a Stride prototype for a grid-based roguelike RPG in the spirit of the Ultima III overworld: a world map, tile movement by keyboard, blocked and walkable terrain, and a code-first layout that can grow into towns, encounters, and party systems.

Current prototype features:

- keyboard movement on a world grid
- 2.5D isometric overworld rendering using repo-owned generated isometric art
- sample overworld loaded from JSON
- walkable and blocked terrain
- party stats: health, food, gold, level, steps
- landmark interactions for towns, harbors, camps, keeps, shrines, ruins, and the dungeon entrance
- lightweight random encounters with attack and retreat flow
- individual party-member HP and positional encounter targeting
- Linux-tested Stride runtime setup
- public-domain art and map-data references documented for later expansion

Controls:

- `Arrow Keys` or `WASD`: move one tile at a time
- `Enter` or `Space`: inspect or interact with the current tile
- `R` during an encounter: attempt retreat
- `R`: reload the sample map and reset to the start
- `Esc`: quit

Run on Linux:

```bash
export STRIDE_NUGET_SOURCE=/home/rlong/Applications/stride/bin/packages
dotnet run --project /home/rlong/notima/src/Notima.Stride.Linux/Notima.Stride.Linux.csproj
```

Source notes:

- Current runtime art:
  - `assets/original/notima_isometric_hero.png`
  - `assets/original/notima_isometric_tiles.png`
  - generated from `tools/generate_original_art.py`
- Public-domain isometric hero reference retained: `assets/public-domain/isometric_hero_dezrasdragons.png`
- Public-domain isometric tile reference retained: `assets/public-domain/isometric_tiles_dezrasdragons.png`
- Public-domain fallback/topdown references remain in `assets/public-domain/`
- Linux runtime uses derived raw RGBA mirrors next to the PNGs to avoid the current Stride/Linux image-loader limitation
- Public-domain map-data source notes: `docs/sources.md`
