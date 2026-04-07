# Source Notes

This repo keeps clearly licensed reference material in scope for expansion, while the current runtime hero, tile, and dungeon-render sheets are original repo-owned generated assets.

## Current runtime art

- `assets/original/notima_isometric_hero.png`
- `assets/original/notima_isometric_tiles.png`
- generated locally by `tools/generate_original_art.py`
- matching raw runtime mirrors:
  - `assets/original/notima_isometric_hero.rgba`
  - `assets/original/notima_isometric_tiles.rgba`
- `assets/original/notima_grim_wall.png`
- `assets/original/notima_grim_floor.png`
- `assets/original/notima_grim_ceiling.png`
- `assets/original/notima_grim_portraits.png`
- `assets/original/notima_grim_creatures.png`
- generated locally by `tools/generate_grimrock_assets.py`
- matching raw runtime mirrors:
  - `assets/original/notima_grim_wall.rgba`
  - `assets/original/notima_grim_floor.rgba`
  - `assets/original/notima_grim_ceiling.rgba`
  - `assets/original/notima_grim_portraits.rgba`
  - `assets/original/notima_grim_creatures.rgba`

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

## Open-licensed equipment reference

- Source: `System Reference Document 5.1`
- Publisher: Wizards of the Coast / D&D Beyond
- License: `CC-BY-4.0`
- Page: `https://www.dndbeyond.com/srd`
- Use in `notima`: mundane weapon and armor names are derived from the open-licensed SRD equipment list, with game-specific attack, defense, cost, and loot values tuned for this project’s combat model.

## Open sound assets

### Footsteps

- Asset: `Footsteps`
- Source: OpenGameArt
- Author: `GboxMikeFozzy`
- License: `CC0`
- Page: `https://opengameart.org/content/footsteps-0`
- Local runtime files:
  - `assets/audio/01-footstep.ogg`
  - `assets/audio/02-footstep.ogg`
  - `assets/audio/03-footstep.ogg`
  - `assets/audio/04-footstep.ogg`

### Bell

- Asset: `Pleasing Bell Sound Effect`
- Source: OpenGameArt
- Author: `Spring Spring`
- License: `CC0`
- Page: `https://opengameart.org/content/pleasing-bell-sound-effect`
- Local runtime file:
  - `assets/audio/pleasing-bell.wav`

### Combat hit

- Asset: `Metal Interactions`
- Source: OpenGameArt
- Author: `GboxMikeFozzy`
- License: `CC0`
- Page: `https://opengameart.org/content/metal-interactions`
- Local runtime file:
  - `assets/audio/metal-hit.wav`
  - `assets/audio/metal-hit-2.wav`
  - `assets/audio/metal-hit-3.wav`

### Combat miss

- Asset: `Swish - bamboo stick weapon swhoshes`
- Source: OpenGameArt
- Author: `qubodup`
- License: `CC0`
- Page: `https://opengameart.org/content/swish-bamboo-stick-weapon-swhoshes`
- Local runtime file:
  - `assets/audio/swish-miss.ogg`

### Victory fanfare

- Asset: `Trumpet Fanfare`
- Source: OpenGameArt
- Author: `David McKee (ViRiX Dreamcore)`
- License: `CC0`
- Page: `https://opengameart.org/content/trumpet-fanfare`
- Local runtime file:
  - `assets/audio/castlefanfare.ogg`

### Spell cast

- Asset: `Magic Spell SFX`
- Source: OpenGameArt
- Author: `JaggedStone`
- License: `CC0`
- Local runtime file:
  - `assets/audio/magical_1.ogg`

### Defeat portal

- Asset: `Teleport Spell`
- Source: OpenGameArt
- Author: `Ogrebane`
- License: `CC0`
- Local runtime file:
  - `assets/audio/teleport.wav`

### Chest creak

- Asset: `100 CC0 wood/metal SFX`
- Source: OpenGameArt
- Author: `qubodup`
- License: `CC0`
- Local runtime file:
  - `assets/audio/door_open_01.ogg`
