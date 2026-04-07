from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "assets" / "original"
CELL = 32
PITCH = 33
ENEMY_CELL = 24
ENEMY_PITCH = 25


def make_canvas(width: int, height: int) -> Image.Image:
    return Image.new("RGBA", (width, height), (0, 0, 0, 0))


def draw_diamond(draw: ImageDraw.ImageDraw, top_x: int, top_y: int, half_w: int, half_h: int, fill: tuple[int, int, int, int], outline: tuple[int, int, int, int]) -> None:
    points = [
        (top_x, top_y),
        (top_x + half_w, top_y + half_h),
        (top_x, top_y + (half_h * 2)),
        (top_x - half_w, top_y + half_h),
    ]
    draw.polygon(points, fill=fill, outline=outline)


def draw_plains(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (118, 172, 96, 255), (77, 113, 60, 255))
    draw.line((16, y + 4, 16, y + 8), fill=(163, 207, 120, 255), width=1)
    draw.line((12, y + 10, 20, y + 10), fill=(145, 194, 111, 255), width=1)
    draw.line((9, y + 12, 12, y + 9), fill=(214, 226, 148, 255))
    draw.line((20, y + 13, 23, y + 10), fill=(214, 226, 148, 255))
    for px, py in ((8, 13), (23, 12), (13, 17), (18, 15)):
        draw.point((px, y + py), fill=(201, 230, 124, 255))


def draw_forest(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (86, 133, 88, 255), (59, 91, 60, 255))
    for cx, cy, size in ((11, y + 8, 4), (17, y + 7, 5), (22, y + 10, 4)):
        draw.ellipse((cx - size, cy - size, cx + size, cy + size), fill=(44, 95, 54, 255), outline=(30, 64, 34, 255))
    draw.rectangle((15, y + 11, 17, y + 17), fill=(88, 62, 44, 255))
    draw.line((12, y + 18, 20, y + 18), fill=(27, 50, 31, 255))


def draw_water(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (91, 152, 213, 255), (56, 97, 151, 255))
    for wave_y in (8, 12, 16):
        draw.arc((7, y + wave_y - 2, 16, y + wave_y + 2), 200, 340, fill=(189, 224, 255, 255))
        draw.arc((16, y + wave_y - 1, 25, y + wave_y + 3), 200, 340, fill=(189, 224, 255, 255))
    draw.line((10, y + 18, 22, y + 14), fill=(215, 238, 255, 180))


def draw_mountains(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (114, 121, 137, 255), (80, 86, 102, 255))
    draw.polygon([(8, y + 16), (14, y + 6), (18, y + 16)], fill=(142, 150, 167, 255), outline=(76, 81, 93, 255))
    draw.polygon([(15, y + 17), (21, y + 7), (26, y + 17)], fill=(170, 176, 191, 255), outline=(89, 95, 110, 255))
    draw.polygon([(13, y + 9), (14, y + 6), (15, y + 9)], fill=(235, 240, 248, 255))
    draw.polygon([(20, y + 10), (21, y + 7), (22, y + 10)], fill=(235, 240, 248, 255))
    draw.line((7, y + 17, 26, y + 17), fill=(68, 74, 87, 255))


def draw_fen(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (104, 131, 87, 255), (72, 92, 62, 255))
    draw.ellipse((10, y + 10, 22, y + 18), fill=(84, 119, 98, 255), outline=(60, 86, 71, 255))
    draw.line((9, y + 8, 7, y + 14), fill=(174, 187, 114, 255))
    draw.line((22, y + 7, 24, y + 14), fill=(174, 187, 114, 255))
    draw.line((16, y + 7, 16, y + 14), fill=(188, 202, 126, 255))
    draw.point((13, y + 13), fill=(212, 230, 183, 255))
    draw.point((19, y + 15), fill=(212, 230, 183, 255))


def draw_road(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (124, 170, 100, 255), (81, 110, 64, 255))
    draw.polygon([(16, y + 4), (23, y + 9), (15, y + 18), (9, y + 13)], fill=(181, 151, 109, 255), outline=(132, 104, 70, 255))
    draw.line((16, y + 6, 16, y + 17), fill=(215, 190, 145, 255))
    draw.line((13, y + 8, 19, y + 15), fill=(140, 113, 80, 255))


def draw_structure(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (116, 164, 101, 255), (76, 105, 64, 255))
    draw.polygon([(9, y + 12), (16, y + 6), (23, y + 12), (16, y + 17)], fill=(179, 91, 82, 255), outline=(112, 52, 48, 255))
    draw.rectangle((10, y + 12, 22, y + 20), fill=(225, 212, 179, 255), outline=(112, 96, 81, 255))
    draw.rectangle((14, y + 14, 18, y + 20), fill=(88, 66, 54, 255))
    draw.rectangle((18, y + 9, 21, y + 13), fill=(194, 189, 176, 255), outline=(96, 92, 86, 255))
    draw.line((11, y + 20, 21, y + 20), fill=(88, 78, 66, 255))


def draw_keep(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (112, 152, 96, 255), (74, 101, 63, 255))
    draw.rectangle((10, y + 11, 22, y + 20), fill=(182, 185, 191, 255), outline=(92, 96, 104, 255))
    draw.rectangle((11, y + 8, 14, y + 12), fill=(168, 172, 179, 255), outline=(92, 96, 104, 255))
    draw.rectangle((18, y + 8, 21, y + 12), fill=(168, 172, 179, 255), outline=(92, 96, 104, 255))
    draw.rectangle((14, y + 14, 18, y + 20), fill=(92, 79, 73, 255))
    draw.line((10, y + 11, 22, y + 11), fill=(220, 224, 230, 255))


def draw_ruins(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (118, 148, 96, 255), (76, 98, 62, 255))
    draw.rectangle((10, y + 14, 15, y + 19), fill=(157, 145, 135, 255), outline=(91, 82, 75, 255))
    draw.rectangle((17, y + 11, 22, y + 19), fill=(172, 156, 142, 255), outline=(98, 88, 80, 255))
    draw.line((11, y + 14, 14, y + 11), fill=(94, 78, 70, 255))
    draw.line((18, y + 15, 22, y + 12), fill=(94, 78, 70, 255))
    draw.point((15, y + 18), fill=(201, 168, 149, 255))


def draw_shrine(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (111, 149, 104, 255), (73, 98, 67, 255))
    draw.polygon([(16, y + 6), (20, y + 11), (16, y + 16), (12, y + 11)], fill=(219, 197, 241, 255), outline=(118, 92, 144, 255))
    draw.rectangle((15, y + 16, 17, y + 20), fill=(205, 187, 163, 255), outline=(102, 88, 75, 255))
    draw.line((16, y + 8, 16, y + 14), fill=(251, 241, 255, 255))
    draw.line((13, y + 11, 19, y + 11), fill=(251, 241, 255, 255))


def draw_harbor(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (103, 151, 184, 255), (68, 101, 124, 255))
    draw.polygon([(9, y + 12), (16, y + 7), (21, y + 11), (15, y + 16)], fill=(208, 182, 141, 255), outline=(110, 84, 56, 255))
    draw.line((19, y + 7, 19, y + 17), fill=(92, 70, 49, 255))
    draw.polygon([(19, y + 8), (24, y + 11), (19, y + 13)], fill=(241, 236, 219, 255), outline=(135, 128, 116, 255))
    draw.line((10, y + 17, 22, y + 13), fill=(225, 241, 255, 190))


def draw_camp(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (124, 159, 103, 255), (82, 104, 67, 255))
    draw.polygon([(11, y + 16), (16, y + 10), (21, y + 16)], fill=(205, 155, 103, 255), outline=(112, 73, 41, 255))
    draw.line((14, y + 18, 18, y + 18), fill=(255, 176, 94, 255))
    draw.point((16, y + 16), fill=(255, 241, 165, 255))


def draw_dungeon(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (108, 137, 102, 255), (72, 91, 67, 255))
    draw.polygon([(11, y + 12), (16, y + 8), (21, y + 12), (21, y + 19), (11, y + 19)], fill=(92, 88, 97, 255), outline=(47, 46, 53, 255))
    draw.rectangle((14, y + 13, 18, y + 19), fill=(26, 26, 32, 255))
    draw.line((13, y + 12, 19, y + 12), fill=(150, 147, 160, 255))


def draw_path(draw: ImageDraw.ImageDraw, y: int) -> None:
    draw_diamond(draw, 16, y + 2, 15, 8, (126, 173, 100, 255), (80, 111, 63, 255))
    draw.line((12, y + 8, 17, y + 12), fill=(189, 163, 122, 255), width=2)
    draw.line((17, y + 12, 21, y + 17), fill=(189, 163, 122, 255), width=2)


def build_tile_sheet() -> Image.Image:
    image = make_canvas(CELL, PITCH * 15)
    draw = ImageDraw.Draw(image)
    draw_forest(draw, PITCH * 0)
    draw_plains(draw, PITCH * 1)
    draw_mountains(draw, PITCH * 2)
    draw_structure(draw, PITCH * 3)
    draw_fen(draw, PITCH * 4)
    draw_path(draw, PITCH * 5)
    draw_road(draw, PITCH * 6)
    draw_water(draw, PITCH * 7)
    draw_keep(draw, PITCH * 8)
    draw_ruins(draw, PITCH * 9)
    draw_shrine(draw, PITCH * 10)
    draw_harbor(draw, PITCH * 11)
    draw_camp(draw, PITCH * 12)
    draw_dungeon(draw, PITCH * 13)
    draw_plains(draw, PITCH * 14)
    return image


def draw_shadow(draw: ImageDraw.ImageDraw, x: int, y: int) -> None:
    draw.ellipse((x + 8, y + 24, x + 24, y + 29), fill=(0, 0, 0, 48))


def draw_character_frame(draw: ImageDraw.ImageDraw, x: int, y: int, direction: str, frame: int, role: str) -> None:
    role_palettes = {
        "warrior": {
            "main_dark": (95, 48, 49, 255),
            "main_mid": (151, 77, 82, 255),
            "main_light": (213, 135, 139, 255),
            "trim": (224, 212, 172, 255),
            "skin": (226, 191, 158, 255),
            "hair": (82, 48, 34, 255),
            "boot": (67, 47, 39, 255),
            "accent": (178, 178, 188, 255),
            "outline": (22, 28, 41, 255),
        },
        "cleric": {
            "main_dark": (88, 98, 70, 255),
            "main_mid": (141, 156, 106, 255),
            "main_light": (203, 214, 154, 255),
            "trim": (247, 231, 191, 255),
            "skin": (227, 198, 163, 255),
            "hair": (108, 76, 51, 255),
            "boot": (77, 58, 47, 255),
            "accent": (248, 236, 197, 255),
            "outline": (22, 28, 41, 255),
        },
        "rogue": {
            "main_dark": (51, 73, 88, 255),
            "main_mid": (77, 111, 130, 255),
            "main_light": (135, 175, 196, 255),
            "trim": (206, 216, 220, 255),
            "skin": (219, 184, 149, 255),
            "hair": (61, 42, 31, 255),
            "boot": (55, 43, 37, 255),
            "accent": (136, 108, 85, 255),
            "outline": (22, 28, 41, 255),
        },
        "mage": {
            "main_dark": (82, 55, 107, 255),
            "main_mid": (122, 88, 164, 255),
            "main_light": (177, 143, 215, 255),
            "trim": (238, 214, 161, 255),
            "skin": (229, 194, 169, 255),
            "hair": (69, 55, 88, 255),
            "boot": (59, 45, 63, 255),
            "accent": (104, 194, 210, 255),
            "outline": (22, 28, 41, 255),
        },
    }
    palette = role_palettes[role]

    step = (-1, 0, 1)[frame]
    draw_shadow(draw, x, y)

    body = [
        (x + 16, y + 6),
        (x + 21, y + 10),
        (x + 20, y + 20),
        (x + 16, y + 24),
        (x + 12, y + 20),
        (x + 11, y + 10),
    ]
    draw.polygon(body, fill=palette["main_mid"], outline=palette["outline"])

    if direction == "down":
        draw.ellipse((x + 12, y + 4, x + 20, y + 12), fill=palette["skin"], outline=palette["outline"])
        draw.rectangle((x + 13, y + 4, x + 19, y + 7), fill=palette["hair"])
        draw.line((x + 13, y + 15, x + 19, y + 15), fill=palette["trim"])
        draw.line((x + 14 - step, y + 24, x + 14 + step, y + 29), fill=palette["boot"], width=2)
        draw.line((x + 18 + step, y + 24, x + 18 - step, y + 29), fill=palette["boot"], width=2)
    elif direction == "up":
        draw.ellipse((x + 12, y + 4, x + 20, y + 12), fill=palette["hair"], outline=palette["outline"])
        draw.line((x + 13, y + 15, x + 19, y + 15), fill=palette["trim"])
        draw.line((x + 14 + step, y + 24, x + 14 - step, y + 29), fill=palette["boot"], width=2)
        draw.line((x + 18 - step, y + 24, x + 18 + step, y + 29), fill=palette["boot"], width=2)
        draw.rectangle((x + 20, y + 12, x + 23, y + 18), fill=palette["accent"], outline=palette["outline"])
    elif direction == "left":
        draw.polygon([(x + 16, y + 4), (x + 11, y + 8), (x + 15, y + 13), (x + 20, y + 9)], fill=palette["skin"], outline=palette["outline"])
        draw.polygon([(x + 16, y + 4), (x + 12, y + 7), (x + 16, y + 8)], fill=palette["hair"])
        draw.line((x + 12, y + 16, x + 18, y + 14), fill=palette["trim"])
        draw.line((x + 15 + step, y + 24, x + 12 + step, y + 29), fill=palette["boot"], width=2)
        draw.line((x + 19 - step, y + 24, x + 17 - step, y + 29), fill=palette["boot"], width=2)
        draw.rectangle((x + 19, y + 12, x + 23, y + 18), fill=palette["accent"], outline=palette["outline"])
    else:
        draw.polygon([(x + 16, y + 4), (x + 21, y + 8), (x + 17, y + 13), (x + 12, y + 9)], fill=palette["skin"], outline=palette["outline"])
        draw.polygon([(x + 16, y + 4), (x + 20, y + 7), (x + 16, y + 8)], fill=palette["hair"])
        draw.line((x + 14, y + 14, x + 20, y + 16), fill=palette["trim"])
        draw.line((x + 13 - step, y + 24, x + 15 - step, y + 29), fill=palette["boot"], width=2)
        draw.line((x + 18 + step, y + 24, x + 21 + step, y + 29), fill=palette["boot"], width=2)
        draw.rectangle((x + 9, y + 12, x + 13, y + 18), fill=palette["accent"], outline=palette["outline"])

    if role == "warrior":
        draw.line((x + 21, y + 10, x + 25, y + 6), fill=palette["accent"], width=2)
        draw.line((x + 19, y + 12, x + 25, y + 6), fill=palette["outline"])
    elif role == "cleric":
        draw.line((x + 22, y + 9, x + 22, y + 18), fill=palette["accent"], width=2)
        draw.line((x + 20, y + 11, x + 24, y + 11), fill=palette["accent"])
    elif role == "rogue":
        draw.line((x + 10, y + 12, x + 7, y + 20), fill=palette["accent"], width=2)
    else:
        draw.line((x + 22, y + 8, x + 22, y + 19), fill=palette["accent"], width=2)
        draw.ellipse((x + 20, y + 5, x + 24, y + 9), fill=palette["accent"], outline=palette["outline"])

    draw.line((x + 16, y + 12, x + 16, y + 21), fill=palette["main_light"])
    draw.line((x + 13, y + 12, x + 12, y + 20), fill=palette["main_dark"])
    draw.line((x + 19, y + 12, x + 20, y + 20), fill=palette["main_dark"])


def build_character_sheet() -> Image.Image:
    roles = ["warrior", "cleric", "rogue", "mage"]
    image = make_canvas(PITCH * 3, PITCH * 4 * len(roles))
    draw = ImageDraw.Draw(image)
    rows = ["down", "left", "right", "up"]
    for role_index, role in enumerate(roles):
        role_y = role_index * (PITCH * 4)
        for row_index, direction in enumerate(rows):
            for frame in range(3):
                draw_character_frame(draw, frame * PITCH, role_y + (row_index * PITCH), direction, frame, role)
    return image


def draw_enemy_shadow(draw: ImageDraw.ImageDraw, x: int, y: int) -> None:
    draw.ellipse((x + 5, y + 18, x + 19, y + 22), fill=(0, 0, 0, 54))


def draw_wolf_frame(draw: ImageDraw.ImageDraw, x: int, y: int, frame: int) -> None:
    step = (-1, 0, 1)[frame]
    draw_enemy_shadow(draw, x, y)
    draw.polygon([(x + 4, y + 16), (x + 8, y + 11), (x + 15, y + 10), (x + 19, y + 13), (x + 15, y + 18), (x + 8, y + 19)], fill=(105, 115, 134, 255), outline=(39, 44, 55, 255))
    draw.polygon([(x + 15, y + 10), (x + 18, y + 7), (x + 20, y + 11)], fill=(105, 115, 134, 255), outline=(39, 44, 55, 255))
    draw.point((x + 17, y + 12), fill=(232, 91, 91, 255))
    draw.line((x + 7 - step, y + 18, x + 6 - step, y + 23), fill=(51, 54, 63, 255), width=2)
    draw.line((x + 12 + step, y + 18, x + 12 + step, y + 23), fill=(51, 54, 63, 255), width=2)
    draw.line((x + 4, y + 16, x + 1, y + 13 + step), fill=(70, 77, 92, 255), width=2)


def draw_leech_frame(draw: ImageDraw.ImageDraw, x: int, y: int, frame: int) -> None:
    offset = (-1, 1)[frame]
    draw_enemy_shadow(draw, x, y + 1)
    draw.ellipse((x + 5, y + 9, x + 19, y + 18), fill=(98, 148, 96, 255), outline=(45, 78, 44, 255))
    draw.arc((x + 4, y + 11 + offset, x + 12, y + 21 + offset), 200, 355, fill=(182, 224, 161, 255))
    draw.arc((x + 12, y + 10 - offset, x + 20, y + 20 - offset), 185, 340, fill=(182, 224, 161, 255))
    draw.point((x + 17, y + 13), fill=(255, 167, 144, 255))


def draw_bandit_frame(draw: ImageDraw.ImageDraw, x: int, y: int, frame: int) -> None:
    step = (-1, 1)[frame]
    draw_enemy_shadow(draw, x, y)
    draw.polygon([(x + 12, y + 4), (x + 17, y + 8), (x + 16, y + 18), (x + 12, y + 22), (x + 8, y + 18), (x + 7, y + 8)], fill=(121, 80, 58, 255), outline=(47, 33, 28, 255))
    draw.ellipse((x + 9, y + 4, x + 15, y + 10), fill=(236, 198, 162, 255), outline=(47, 33, 28, 255))
    draw.rectangle((x + 10, y + 4, x + 15, y + 6), fill=(72, 42, 35, 255))
    draw.line((x + 8 - step, y + 21, x + 7 - step, y + 24), fill=(54, 39, 33, 255), width=2)
    draw.line((x + 15 + step, y + 21, x + 16 + step, y + 24), fill=(54, 39, 33, 255), width=2)
    draw.line((x + 17, y + 10, x + 21, y + 7), fill=(184, 184, 194, 255), width=2)


def build_enemy_sheet() -> Image.Image:
    image = make_canvas(ENEMY_PITCH * 2, ENEMY_PITCH * 3)
    draw = ImageDraw.Draw(image)
    for frame in range(2):
        draw_wolf_frame(draw, frame * ENEMY_PITCH, 0, frame)
        draw_leech_frame(draw, frame * ENEMY_PITCH, ENEMY_PITCH, frame)
        draw_bandit_frame(draw, frame * ENEMY_PITCH, ENEMY_PITCH * 2, frame)
    return image


def write_outputs(image: Image.Image, stem: str) -> None:
    png_path = ASSET_DIR / f"{stem}.png"
    rgba_path = ASSET_DIR / f"{stem}.rgba"
    image.save(png_path)
    rgba_path.write_bytes(image.tobytes())


def main() -> None:
    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    write_outputs(build_tile_sheet(), "notima_isometric_tiles")
    write_outputs(build_character_sheet(), "notima_isometric_hero")
    write_outputs(build_enemy_sheet(), "notima_enemy_sheet")
    print(f"Wrote assets to {ASSET_DIR}")


if __name__ == "__main__":
    main()
