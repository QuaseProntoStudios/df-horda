"""Gera a pixel art provisória do Dead Frequency: Horda (pasta Art/).

Uso:  python Tools/make_art.py      (precisa do Pillow: pip install pillow)
Tudo aqui é desenhado por código, então é só editar e rodar de novo.
"""
import os
import random

from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Art")
os.makedirs(OUT, exist_ok=True)

T = (0, 0, 0, 0)


def from_rows(rows, palette):
    """Monta uma imagem a partir de linhas de texto; cada letra é uma cor da paleta."""
    height, width = len(rows), max(len(r) for r in rows)
    image = Image.new("RGBA", (width, height), T)
    for y, row in enumerate(rows):
        for x, char in enumerate(row):
            if char in palette:
                image.putpixel((x, y), palette[char])
    return image


# ------------------------------------------------------------------ Van (frente para cima)
VAN_PALETTE = {
    "o": (16, 20, 24, 255),      # contorno
    "w": (226, 224, 208, 255),   # lataria
    "s": (176, 176, 164, 255),   # lataria na sombra
    "g": (40, 150, 80, 255),     # faixa verde da 96.6
    "b": (44, 70, 98, 255),      # vidro
    "c": (96, 140, 170, 255),    # reflexo do vidro
    "y": (255, 226, 110, 255),   # farol
    "r": (200, 44, 40, 255),     # lanterna
    "k": (36, 38, 42, 255),      # pneu / para-choque
    "a": (90, 255, 140, 255),    # antena
}
VAN = [
    "...oooooooooo...",
    "..okkkkkkkkkko..",
    ".oywwwwwwwwwwyo.",
    ".owwwwwwwwwwwwo.",
    "kowwwwwwwwwwwwok",
    "kowbbbbbbbbbbwok",
    "kobbccbbbbbbbbok",
    "kobcbbbbbbbbbbok",
    ".obbbbbbbbbbbbo.",
    ".owwwwwwwwwwwwo.",
    ".owwwwwwwwwwwwo.",
    ".oggggggggggggo.",
    ".owwwwwwawwwwwo.",
    ".owwwwwaaawwwwo.",
    ".owwwwwwawwwwwo.",
    ".owwwwwwwwwwwwo.",
    ".oggggggggggggo.",
    ".owwwwwwwwwwwwo.",
    ".owwwwwwwwwwwwo.",
    ".osssssssssssso.",
    "kosssssssssssso" + "k",
    "kowwwwwwwwwwwwok",
    "kowwwwwwwwwwwwok",
    "kowbbbbbbbbbbwok",
    ".owbbbbbbbbbbwo.",
    ".orwwwwwwwwwwro.",
    "..okkkkkkkkkko..",
    "...oooooooooo...",
]
from_rows(VAN, VAN_PALETTE).save(os.path.join(OUT, "van.png"))

# ------------------------------------------------------------------ Zumbi (2 quadros de 16x16)
Z_PALETTE = {
    "o": (14, 20, 16, 255),
    "g": (118, 168, 96, 255),    # pele
    "d": (80, 122, 70, 255),     # pele na sombra
    "e": (255, 70, 60, 255),     # olho
    "c": (92, 70, 110, 255),     # camisa
    "p": (54, 58, 84, 255),      # calça
    "m": (60, 24, 24, 255),      # boca
}
ZOMBIE_A = [
    "................",
    "....oooooo......",
    "...oggggggo.....",
    "...ogeggego.....",
    "...oggggggo.....",
    "...odgmmgdo.....",
    "....oooooo......",
    "...occccccooo...",
    "..occccccgggo...",
    "..occccccooo....",
    "..occccccgggo...",
    "...occcccooo....",
    "...oppoppo......",
    "...oppoppo......",
    "..oddo.oddo.....",
    "..ooo..ooo......",
]
ZOMBIE_B = [
    "................",
    "................",
    "....oooooo......",
    "...oggggggo.....",
    "...ogeggego.....",
    "...oggggggo.....",
    "...odgmmgdo.....",
    "....oooooo......",
    "...occccccooo...",
    "..occccccgggo...",
    "..occccccgggo...",
    "...occcccooo....",
    "...oppoppo......",
    "..oppo.oppo.....",
    ".oddo...oddo....",
    ".ooo....ooo.....",
]
zombie = Image.new("RGBA", (32, 16), T)
zombie.paste(from_rows(ZOMBIE_A, Z_PALETTE), (0, 0))
zombie.paste(from_rows(ZOMBIE_B, Z_PALETTE), (16, 0))
zombie.save(os.path.join(OUT, "zombie.png"))

# ------------------------------------------------------------------ Sinal (XP)
GEM_PALETTE = {
    "o": (10, 40, 20, 255),
    "g": (60, 230, 120, 255),
    "l": (190, 255, 210, 255),
    "d": (30, 150, 80, 255),
}
GEM = [
    "...oo...",
    "..olgo..",
    ".olggdo.",
    "olggggdo",
    "oggggddo",
    ".ogddo..",
    "..odo...",
    "...o....",
]
from_rows(GEM, GEM_PALETTE).save(os.path.join(OUT, "gem.png"))

# ------------------------------------------------------------------ Onda de rádio (aponta para a direita)
WAVE_PALETTE = {"g": (90, 255, 140, 255), "l": (220, 255, 230, 255), "d": (30, 140, 70, 255)}
WAVE = [
    "..d.....",
    "...gd...",
    ".d..gl..",
    "..g..gl.",
    "..g..gl.",
    ".d..gl..",
    "...gd...",
    "..d.....",
]
from_rows(WAVE, WAVE_PALETTE).save(os.path.join(OUT, "wave.png"))

# ------------------------------------------------------------------ Asfalto (ladrilho 64x64 que repete)
rng = random.Random(966)
SIZE = 64
BASE = (38, 40, 46)
ground = Image.new("RGBA", (SIZE, SIZE), BASE + (255,))
for y in range(SIZE):
    for x in range(SIZE):
        roll = rng.random()
        if roll < 0.10:
            shade = -5
        elif roll < 0.18:
            shade = 5
        elif roll < 0.195:
            shade = 14
        else:
            continue
        ground.putpixel((x, y), tuple(max(0, c + shade) for c in BASE) + (255,))

# Rachaduras: caminhos que dão a volta na borda, para o ladrilho emendar sem costura.
CRACK = (24, 25, 30, 255)
for _ in range(3):
    x, y = rng.randrange(SIZE), rng.randrange(SIZE)
    dx, dy = rng.choice([(1, 0), (0, 1), (1, 1), (1, -1)])
    for _ in range(rng.randrange(10, 22)):
        ground.putpixel((x % SIZE, y % SIZE), CRACK)
        if rng.random() < 0.35:
            x += rng.choice([-1, 0, 1])
            y += rng.choice([-1, 0, 1])
        else:
            x += dx
            y += dy

# Manchas de óleo e um tufo de mato.
for _ in range(2):
    cx, cy = rng.randrange(SIZE), rng.randrange(SIZE)
    for _ in range(14):
        ground.putpixel(((cx + rng.randrange(-2, 3)) % SIZE, (cy + rng.randrange(-2, 3)) % SIZE), (30, 31, 37, 255))
cx, cy = rng.randrange(SIZE), rng.randrange(SIZE)
for ox, oy in [(0, 0), (1, -1), (-1, -1), (2, 0), (0, -2)]:
    ground.putpixel(((cx + ox) % SIZE, (cy + oy) % SIZE), (52, 86, 50, 255))
ground.save(os.path.join(OUT, "ground.png"))

# ------------------------------------------------------------------ Ícone do projeto
icon = Image.new("RGBA", (128, 128), (10, 20, 14, 255))
van_big = from_rows(VAN, VAN_PALETTE).resize((16 * 4, 28 * 4), Image.NEAREST)
icon.paste(van_big, (32, 8), van_big)
icon.save(os.path.join(OUT, "..", "icon.png"))

print("Arte gerada em", os.path.normpath(OUT))
