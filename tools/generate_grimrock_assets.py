from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import random


ROOT = Path("/home/rlong/notima")
OUT = ROOT / "assets" / "original"
OUT.mkdir(parents=True, exist_ok=True)
rng = random.Random(7)


def save_rgba_png_and_raw(name: str, image: Image.Image) -> None:
    image = image.convert("RGBA")
    png_path = OUT / f"{name}.png"
    raw_path = OUT / f"{name}.rgba"
    image.save(png_path)
    raw_path.write_bytes(image.tobytes())


def make_wall():
    w = h = 256
    img = Image.new("RGBA", (w, h), (46, 50, 56, 255))
    d = ImageDraw.Draw(img)
    for y in range(0, h, 32):
        offset = 0 if (y // 32) % 2 == 0 else 18
        for x in range(-offset, w, 36):
            bw = 34 + rng.randint(-4, 4)
            bh = 24 + rng.randint(-3, 3)
            shade = 72 + rng.randint(-18, 24)
            d.rounded_rectangle((x + 2, y + 3, x + bw, y + bh), radius=4, fill=(shade, shade + 3, shade + 6, 255), outline=(28, 30, 34, 255), width=2)
            d.line((x + 8, y + 8, x + bw - 6, y + 10), fill=(120, 122, 128, 120), width=1)
    for _ in range(350):
        x = rng.randint(0, w - 4)
        y = rng.randint(0, h - 4)
        c = 40 + rng.randint(0, 60)
        d.rectangle((x, y, x + 1, y + 1), fill=(c, c + 4, c + 8, 90))
    img = img.filter(ImageFilter.GaussianBlur(0.25))
    save_rgba_png_and_raw("notima_grim_wall", img)


def make_floor():
    w = h = 256
    img = Image.new("RGBA", (w, h), (54, 46, 39, 255))
    d = ImageDraw.Draw(img)
    for y in range(0, h, 28):
        for x in range(0, w, 28):
            c = 70 + rng.randint(-12, 18)
            d.rectangle((x, y, x + 26, y + 26), fill=(c, c - 6, c - 14, 255), outline=(28, 22, 18, 255), width=2)
            if rng.random() < 0.35:
                d.line((x + 5, y + 18, x + 21, y + 9), fill=(110, 92, 72, 140), width=2)
    save_rgba_png_and_raw("notima_grim_floor", img)


def make_ceiling():
    w = h = 256
    img = Image.new("RGBA", (w, h), (32, 36, 48, 255))
    d = ImageDraw.Draw(img)
    for y in range(0, h, 32):
        for x in range(0, w, 32):
            c = 48 + rng.randint(-10, 14)
            d.ellipse((x + 4, y + 4, x + 28, y + 28), fill=(c, c + 6, c + 16, 255), outline=(18, 20, 28, 255), width=2)
    for _ in range(220):
        x = rng.randint(0, w - 2)
        y = rng.randint(0, h - 2)
        a = rng.randint(30, 100)
        d.rectangle((x, y, x + 1, y + 1), fill=(130, 140, 175, a))
    save_rgba_png_and_raw("notima_grim_ceiling", img)


def make_portraits():
    cell_w, cell_h = 128, 128
    img = Image.new("RGBA", (cell_w * 4, cell_h), (0, 0, 0, 0))
    roles = [
        ("AVA", (148, 116, 92), (168, 132, 98), (112, 84, 66)),
        ("BRI", (100, 116, 142), (132, 154, 184), (78, 90, 112)),
        ("CYR", (120, 84, 94), (170, 112, 124), (84, 56, 60)),
        ("DAS", (112, 108, 78), (160, 152, 102), (82, 76, 52)),
    ]
    for i, (_, skin, cloth, shadow) in enumerate(roles):
        ox = i * cell_w
        d = ImageDraw.Draw(img)
        d.rounded_rectangle((ox + 10, 10, ox + 118, 118), radius=16, fill=(20, 22, 28, 255), outline=(118, 96, 72, 255), width=3)
        d.ellipse((ox + 34, 22, ox + 96, 82), fill=skin + (255,), outline=(36, 26, 22, 255), width=2)
        d.polygon([(ox + 34, 116), (ox + 64, 60), (ox + 94, 116)], fill=cloth + (255,), outline=shadow + (255,))
        d.ellipse((ox + 50, 44, ox + 58, 52), fill=(20, 18, 18, 255))
        d.ellipse((ox + 72, 44, ox + 80, 52), fill=(20, 18, 18, 255))
        d.line((ox + 56, 68, ox + 74, 68), fill=(86, 44, 44, 255), width=2)
        d.arc((ox + 44, 18, ox + 88, 52), 180, 360, fill=shadow + (255,), width=7)
    save_rgba_png_and_raw("notima_grim_portraits", img)


def make_creatures():
    frame_w, frame_h = 128, 128
    img = Image.new("RGBA", (frame_w * 2, frame_h * 3), (0, 0, 0, 0))
    rows = [
        ((88, 86, 96), (164, 166, 180), "wolf"),
        ((54, 108, 82), (118, 184, 140), "leech"),
        ((124, 88, 78), (198, 146, 128), "bandit"),
    ]
    for row, (base, hi, kind) in enumerate(rows):
        for frame in range(2):
            ox = frame * frame_w
            oy = row * frame_h
            d = ImageDraw.Draw(img)
            if kind == "wolf":
                d.polygon([(ox + 20, oy + 96), (ox + 44, oy + 54), (ox + 90, oy + 46), (ox + 104, oy + 88), (ox + 82, oy + 106)], fill=base + (255,), outline=(26, 26, 30, 255))
                d.polygon([(ox + 48, oy + 42), (ox + 58, oy + 20), (ox + 70, oy + 42)], fill=hi + (255,))
                d.polygon([(ox + 66, oy + 40), (ox + 80, oy + 18), (ox + 88, oy + 42)], fill=hi + (255,))
                if frame:
                    d.line((ox + 96, oy + 70, ox + 116, oy + 56), fill=hi + (255,), width=6)
            elif kind == "leech":
                d.ellipse((ox + 24, oy + 38, ox + 104, oy + 94), fill=base + (255,), outline=(18, 28, 22, 255), width=3)
                d.ellipse((ox + 42, oy + 52, ox + 86, oy + 78), fill=hi + (255,))
                d.arc((ox + 40, oy + 66 + (frame * 4), ox + 90, oy + 106 + (frame * 4)), 210, 350, fill=(210, 220, 214, 255), width=5)
            else:
                d.ellipse((ox + 40, oy + 18, ox + 88, oy + 64), fill=(160, 126, 104, 255), outline=(30, 20, 16, 255), width=2)
                d.rectangle((ox + 32, oy + 62, ox + 96, oy + 114), fill=base + (255,), outline=(34, 20, 18, 255), width=2)
                sword_x = 90 + (frame * 8)
                d.rectangle((ox + sword_x, oy + 46, ox + sword_x + 7, oy + 104), fill=(170, 176, 186, 255), outline=(32, 30, 30, 255))
                d.rectangle((ox + sword_x - 6, oy + 70, ox + sword_x + 13, oy + 76), fill=hi + (255,))
            d.ellipse((ox + 24, oy + 104, ox + 106, oy + 118), fill=(0, 0, 0, 70))
    save_rgba_png_and_raw("notima_grim_creatures", img)


make_wall()
make_floor()
make_ceiling()
make_portraits()
make_creatures()
