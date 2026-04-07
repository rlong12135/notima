# notima

`notima` is a Stride prototype for a grid-based roguelike RPG in the spirit of the Ultima III overworld: a world map, tile movement by keyboard, blocked and walkable terrain, and a code-first layout that can grow into towns, encounters, and party systems.

Current prototype features:

- keyboard movement on a world grid
- sample overworld loaded from JSON
- walkable and blocked terrain
- landmarks for towns, shrines, ruins, and a keep
- Linux-tested Stride runtime setup
- public-domain art and map-data sources documented for later sprite and world-generation integration

Controls:

- `Arrow Keys` or `WASD`: move one tile at a time
- `Enter`: inspect the current tile
- `R`: reload the sample map and reset to the start
- `Esc`: quit

Run on Linux:

```bash
export STRIDE_NUGET_SOURCE=/home/rlong/Applications/stride/bin/packages
dotnet run --project /home/rlong/notima/src/Notima.Stride.Linux/Notima.Stride.Linux.csproj
```

Source notes:

- Public-domain player sprite reference: `assets/public-domain/julia-player-cc0.png`
- Public-domain overworld tiles reference: `assets/public-domain/overworld-tiles-cc0.png`
- Public-domain map-data source notes: `docs/sources.md`

