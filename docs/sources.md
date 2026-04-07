# Source Notes

This repo keeps only clearly public-domain source material in scope for the first art/data pass.

## Public-domain art references

### Player icon reference

- Asset: `Top down player sprite sheet (Julia)`
- Source: OpenGameArt
- License: `CC0`
- Page: `https://opengameart.org/content/top-down-player-sprite-sheet-julia`
- Local reference file: `assets/public-domain/julia-player-cc0.png`
- Runtime mirror for Linux: `assets/public-domain/julia-player-cc0.rgba`

### Overworld tile reference

- Asset: `16x16 Overworld Tiles`
- Source: OpenGameArt
- License: `CC0`
- Page: `https://opengameart.org/content/16x16-overworld-tiles-0`
- Local reference file: `assets/public-domain/overworld-tiles-cc0.png`
- Runtime mirror for Linux: `assets/public-domain/overworld-tiles-cc0.rgba`

## Public-domain map data

### Natural Earth

- Source: Natural Earth
- License: Public domain
- Homepage: `https://www.naturalearthdata.com/`
- Suggested use in `notima`: derive macro-world shapes, coastlines, or biome seeds for later overworld generation tools

This first scaffold uses a handcrafted sample overworld JSON in `assets/data/overworld.json`, but the repo is structured so a later importer can transform Natural Earth data into a higher-level fantasy overworld seed.
