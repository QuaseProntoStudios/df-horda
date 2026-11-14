"""Gera a pixel art provisória do Dead Frequency: Horda (pasta Art/).

Uso:  python Tools/make_art.py      (precisa do Pillow: pip install pillow)
Tudo aqui é desenhado por código, então é só editar e rodar de novo.
"""
import os
import random

from PIL import Image, ImageDraw

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

# ------------------------------------------------------------------ Novos inimigos (gerados por código)
OUTLINE = (14, 18, 16, 255)


def outline(image, color=OUTLINE):
    """Contorno de 1 pixel em volta de tudo que não é transparente."""
    w, h = image.size
    src = image.copy()
    for y in range(h):
        for x in range(w):
            if src.getpixel((x, y))[3] != 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src.getpixel((nx, ny))[3] != 0:
                    image.putpixel((x, y), color)
                    break
    return image


def box(d, x0, y0, x1, y1, color):
    d.rectangle([x0, y0, x1, y1], fill=color)


def humanoid(size, frame, p):
    """Desenha um zumbi de frente. frame 0/1 muda pernas, braços e a altura do corpo."""
    img = Image.new("RGBA", (size, size), T)
    d = ImageDraw.Draw(img)
    cx = size // 2
    bob = frame
    leg_h, leg_w = p["leg"]
    torso_w, torso_h = p["torso"]
    head_w, head_h = p["head"]
    arm_w, arm_l = p["arm"]

    legs_bottom = size - 2
    legs_top = legs_bottom - leg_h + 1
    torso_top = legs_top - torso_h + 1 + bob
    head_top = torso_top - head_h + 1 - (1 if p.get("neckless") else 0)

    # pernas: uma sobe e a outra desce a cada quadro
    lx = cx - torso_w // 2 + 1
    rx = cx + torso_w // 2 - leg_w
    box(d, lx, legs_top, lx + leg_w - 1, legs_bottom - (1 if frame else 0), p["pants"])
    box(d, rx, legs_top, rx + leg_w - 1, legs_bottom - (0 if frame else 1), p["pants"])

    # braços ao lado do corpo, balançando
    ay = torso_top + 1
    box(d, cx - torso_w // 2 - arm_w, ay + frame, cx - torso_w // 2 - 1, ay + frame + arm_l - 1, p["skin"])
    box(d, cx + torso_w // 2, ay + (1 - frame), cx + torso_w // 2 + arm_w - 1, ay + (1 - frame) + arm_l - 1, p["skin"])

    # tronco e cabeça
    box(d, cx - torso_w // 2, torso_top, cx + torso_w // 2 - 1, torso_top + torso_h - 1, p["shirt"])
    if "shade" in p:
        box(d, cx - torso_w // 2, torso_top + torso_h - 2, cx + torso_w // 2 - 1, torso_top + torso_h - 1, p["shade"])
    hx0 = cx - head_w // 2
    box(d, hx0, head_top, hx0 + head_w - 1, head_top + head_h - 1, p["skin"])

    # olhos e boca
    ey = head_top + max(1, head_h // 3)
    eye = p["eye"]
    d.point((hx0 + 1, ey), fill=eye)
    d.point((hx0 + head_w - 2, ey), fill=eye)
    if p.get("big_eyes"):
        d.point((hx0 + 1, ey + 1), fill=eye)
        d.point((hx0 + head_w - 2, ey + 1), fill=eye)
    my = head_top + head_h - 2
    box(d, hx0 + 1, my, hx0 + head_w - 2, my, p.get("mouth", (60, 24, 24, 255)))

    p["extra"](d, cx, head_top, head_w, head_h, torso_top, torso_w, torso_h, legs_top, frame)
    return outline(img)


def two_frames(builder, size):
    sheet = Image.new("RGBA", (size * 2, size), T)
    sheet.paste(builder(0), (0, 0))
    sheet.paste(builder(1), (size, 0))
    return sheet


def no_extra(*args):
    pass


# Corredor: magro, pálido, camiseta laranja.
RUNNER = dict(
    skin=(176, 190, 120, 255), shirt=(214, 112, 52, 255), pants=(48, 54, 92, 255), eye=(255, 230, 60, 255),
    head=(6, 5), torso=(6, 5), arm=(1, 5), leg=(4, 2), extra=no_extra, shade=(160, 84, 40, 255),
)
two_frames(lambda f: humanoid(16, f, RUNNER), 16).save(os.path.join(OUT, "runner.png"))


# Cuspidor: papo verde inchado embaixo da cabeça, boca aberta.
def spitter_extra(d, cx, head_top, head_w, head_h, torso_top, torso_w, torso_h, legs_top, frame):
    sac = (140, 230, 70, 255)
    box(d, cx - 2, head_top + head_h, cx + 1, head_top + head_h + 2, sac)
    d.point((cx - 1, head_top + head_h + 1), fill=(220, 255, 150, 255))


SPITTER = dict(
    skin=(104, 150, 82, 255), shirt=(84, 74, 112, 255), pants=(52, 56, 70, 255), eye=(230, 255, 90, 255),
    head=(6, 5), torso=(6, 5), arm=(1, 4), leg=(4, 3), extra=spitter_extra, mouth=(40, 70, 20, 255), shade=(64, 56, 90, 255),
)
two_frames(lambda f: humanoid(16, f, SPITTER), 16).save(os.path.join(OUT, "spitter.png"))


# Brutamontes: enorme, cinza-azulado, colete rasgado e punhos grandes.
def brute_extra(d, cx, head_top, head_w, head_h, torso_top, torso_w, torso_h, legs_top, frame):
    vest = (96, 62, 40, 255)
    box(d, cx - torso_w // 2 + 1, torso_top + 1, cx - torso_w // 2 + 3, torso_top + torso_h - 2, vest)
    box(d, cx + torso_w // 2 - 4, torso_top + 1, cx + torso_w // 2 - 2, torso_top + torso_h - 2, vest)
    fist = (110, 128, 140, 255)
    box(d, cx - torso_w // 2 - 3, torso_top + 6 + frame, cx - torso_w // 2 - 1, torso_top + 8 + frame, fist)
    box(d, cx + torso_w // 2 + 0, torso_top + 6 + (1 - frame), cx + torso_w // 2 + 2, torso_top + 8 + (1 - frame), fist)


BRUTE = dict(
    skin=(122, 140, 154, 255), shirt=(150, 164, 176, 255), pants=(58, 52, 68, 255), eye=(255, 80, 60, 255),
    head=(8, 6), torso=(14, 9), arm=(3, 7), leg=(5, 4), extra=brute_extra, shade=(104, 118, 130, 255), neckless=True, big_eyes=True,
)
two_frames(lambda f: humanoid(24, f, BRUTE), 24).save(os.path.join(OUT, "brute.png"))


# Bomba ambulante: corpo coberto de dinamite e uma luz que pisca.
def bomber_extra(d, cx, head_top, head_w, head_h, torso_top, torso_w, torso_h, legs_top, frame):
    stick = (206, 52, 44, 255)
    cap = (255, 214, 90, 255)
    for i, x in enumerate((cx - 3, cx - 1, cx + 1)):
        box(d, x, torso_top, x + 1, torso_top + torso_h - 1, stick)
        d.point((x, torso_top), fill=cap)
    d.point((cx, torso_top + 2), fill=(255, 250, 200, 255) if frame else (120, 30, 20, 255))


BOMBER = dict(
    skin=(150, 170, 104, 255), shirt=(120, 120, 128, 255), pants=(60, 62, 76, 255), eye=(255, 90, 60, 255),
    head=(6, 5), torso=(8, 5), arm=(1, 4), leg=(5, 2), extra=bomber_extra, big_eyes=True,
)
two_frames(lambda f: humanoid(16, f, BOMBER), 16).save(os.path.join(OUT, "bomber.png"))


# Zumbão da 96.6: gigante, de fone de ouvido, com uma antena e rádio brilhando no peito.
def boss_extra(d, cx, head_top, head_w, head_h, torso_top, torso_w, torso_h, legs_top, frame):
    band = (60, 200, 230, 255)
    box(d, cx - head_w // 2 - 1, head_top, cx - head_w // 2 - 1, head_top + head_h - 2, band)
    box(d, cx + head_w // 2, head_top, cx + head_w // 2, head_top + head_h - 2, band)
    box(d, cx - head_w // 2 - 2, head_top + 2, cx - head_w // 2 - 1, head_top + 5, (40, 150, 180, 255))
    box(d, cx + head_w // 2, head_top + 2, cx + head_w // 2 + 1, head_top + 5, (40, 150, 180, 255))
    box(d, cx - head_w // 2, head_top - 1, cx + head_w // 2 - 1, head_top - 1, band)
    # antena
    box(d, cx + 3, head_top - 4, cx + 3, head_top - 1, (200, 200, 210, 255))
    d.point((cx + 3, head_top - 5), fill=(90, 255, 140, 255) if frame else (40, 150, 80, 255))
    # rádio no peito
    box(d, cx - 4, torso_top + 3, cx + 3, torso_top + 8, (30, 36, 40, 255))
    box(d, cx - 3, torso_top + 4, cx + 2, torso_top + 7, (60, 255, 130, 255) if frame else (40, 200, 100, 255))
    d.point((cx - 2, torso_top + 5), fill=(20, 80, 40, 255))
    d.point((cx + 1, torso_top + 6), fill=(20, 80, 40, 255))


BOSS = dict(
    skin=(104, 140, 106, 255), shirt=(98, 72, 124, 255), pants=(44, 46, 70, 255), eye=(255, 60, 50, 255),
    head=(12, 9), torso=(20, 14), arm=(4, 11), leg=(7, 6), extra=boss_extra, shade=(76, 54, 98, 255), neckless=True, big_eyes=True,
    mouth=(70, 20, 24, 255),
)
two_frames(lambda f: humanoid(32, f, BOSS), 32).save(os.path.join(OUT, "boss.png"))

# ------------------------------------------------------------------ Projéteis e itens
SANDAL_PALETTE = {
    "o": (38, 24, 14, 255), "b": (150, 96, 52, 255), "l": (200, 140, 84, 255),
    "s": (60, 120, 220, 255), "t": (120, 180, 255, 255),
}
SANDAL = [
    "..........",
    "..oooooo..",
    ".obllllbo.",
    "obllsstlbo",
    "oblsstllbo",
    "oblsstllbo",
    "obllsstlbo",
    ".obllllbo.",
    "..oooooo..",
    "..........",
]
from_rows(SANDAL, SANDAL_PALETTE).save(os.path.join(OUT, "chinelo.png"))

EGG_PALETTE = {"o": (80, 60, 30, 255), "w": (250, 244, 224, 255), "y": (255, 196, 70, 255)}
EGG = [
    "..oooo..",
    ".owwwwo.",
    "owwwwwwo",
    "owwywwwo",
    "owwwwwwo",
    "owwwwwwo",
    ".owwwwo.",
    "..oooo..",
]
from_rows(EGG, EGG_PALETTE).save(os.path.join(OUT, "egg.png"))

SPIT_PALETTE = {"o": (30, 70, 10, 255), "g": (150, 235, 60, 255), "l": (230, 255, 170, 255)}
SPIT = [
    ".oooo.",
    "oggggo",
    "oglggo",
    "oggggo",
    "oggggo",
    ".oooo.",
]
from_rows(SPIT, SPIT_PALETTE).save(os.path.join(OUT, "spit.png"))

# Nuvem de gás: disco macio, sem contorno.
gas = Image.new("RGBA", (16, 16), T)
for y in range(16):
    for x in range(16):
        dist = ((x - 7.5) ** 2 + (y - 7.5) ** 2) ** 0.5
        if dist < 7.8:
            alpha = 120 if dist < 4.5 else 80 if dist < 6.3 else 45
            if (x * 7 + y * 13) % 5 == 0:
                alpha = max(20, alpha - 40)
            gas.putpixel((x, y), (130, 220, 70, alpha))
gas.save(os.path.join(OUT, "gas.png"))

REPAIR_PALETTE = {"o": (24, 24, 28, 255), "w": (240, 240, 232, 255), "r": (214, 48, 44, 255), "s": (190, 190, 180, 255)}
REPAIR = [
    "..oooooo..",
    ".owwwwwwo.",
    "owwwrrwwwo",
    "owwwrrwwwo",
    "owrrrrrrwo",
    "owrrrrrrwo",
    "owwwrrwwwo",
    "owwwrrwwwo",
    ".ossssssoo",
    "..oooooo..",
]
from_rows(REPAIR, REPAIR_PALETTE).save(os.path.join(OUT, "repair.png"))

# ------------------------------------------------------------------ Ícone do projeto
icon = Image.new("RGBA", (128, 128), (10, 20, 14, 255))
van_big = from_rows(VAN, VAN_PALETTE).resize((16 * 4, 28 * 4), Image.NEAREST)
icon.paste(van_big, (32, 8), van_big)
icon.save(os.path.join(OUT, "..", "icon.png"))

print("Arte gerada em", os.path.normpath(OUT))
