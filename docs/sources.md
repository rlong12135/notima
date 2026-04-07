# Source Notes

This repo keeps only clearly public-domain source material in scope for the first art/data pass.

## Public-domain art references

### Isometric hero sprite reference

- Asset: `Isometric Classic Hero + Tiles (32x32)` hero sheet
- Source: OpenGameArt
- License: `CC0`
- Page: `https://opengameart.org/content/isometric-classic-hero-tiles-32x32`
- Local reference file: `assets/public-domain/isometric_hero_dezrasdragons.png`
- Runtime mirror for Linux: `assets/public-domain/isometric_hero_dezrasdragons.rgba`

### Isometric overworld tile reference

- Asset: `Isometric Classic Hero + Tiles (32x32)` tile sheet
- Source: OpenGameArt
- License: `CC0`
- Page: `https://opengameart.org/content/isometric-classic-hero-tiles-32x32`
- Local reference file: `assets/public-domain/isometric_tiles_dezrasdragons.png`
- Runtime mirror for Linux: `assets/public-domain/isometric_tiles_dezrasdragons.rgba`

### Additional unrestricted fallback references

- `Top down player sprite sheet (Julia)`, OpenGameArt, CC0
- `16x16 Overworld Tiles`, OpenGameArt, CC0
- `Isometric Miniature Dungeon`, Kenney on OpenGameArt, CC0
- `basic isometric tiles 128x128`, OpenGameArt, CC0

## Public-domain map data

### Natural Earth

- Source: Natural Earth
- License: Public domain
- Homepage: `https://www.naturalearthdata.com/`
- Suggested use in `notima`: derive macro-world shapes, coastlines, or biome seeds for later overworld generation tools

This first scaffold uses a handcrafted sample overworld JSON in `assets/data/overworld.json`, but the repo is structured so a later importer can transform Natural Earth data into a higher-level fantasy overworld seed.
