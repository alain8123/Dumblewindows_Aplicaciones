"""Genera el sprite pixel art del botón de salir: escudo carmesí con ribete dorado y una X dorada.

Uso:  python Tools/boton_salir_pixelart.py
Escribe Assets/Sprites/boton_salir.png (32x32).
"""
import math
import os

from PIL import Image, ImageDraw

N = 32
SS = 8

OUTLINE = (34, 16, 10)
GOLD_HI = (255, 226, 132)
GOLD = (222, 170, 62)
GOLD_LO = (150, 98, 30)
RED_HI = (176, 40, 48)
RED = (130, 22, 34)
RED_LO = (84, 12, 22)
X_SHADOW = (52, 6, 14)


def half_width(y):
    """Media anchura del escudo en la fila y (en píxeles finales)."""
    top, bend, tip, w = 1.5, 17.0, 30.5, 13.5
    if y < top or y > tip:
        return -1
    if y <= bend:
        return w
    k = (y - bend) / (tip - bend)
    return w * math.sqrt(max(0.0, 1 - k * k)) * (1 - 0.25 * k)


def shield_mask(inset):
    m = [[False] * N for _ in range(N)]
    cx = (N - 1) / 2
    for y in range(N):
        for x in range(N):
            hits = 0
            for sy in range(4):
                for sx in range(4):
                    fx, fy = x + (sx + 0.5) / 4, y + (sy + 0.5) / 4
                    hw = half_width(fy - inset * 0.6) - inset
                    if hw > 0 and abs(fx - 0.5 - cx) <= hw and fy >= 1.5 + inset:
                        hits += 1
            m[y][x] = hits >= 8
    return m


def x_mask(offset=(0, 0)):
    im = Image.new("L", (N * SS, N * SS), 0)
    d = ImageDraw.Draw(im)
    ox, oy = offset
    a, b = 10.0, 21.5
    for p, q in (((a, a - 1), (b, b - 1)), ((b, a - 1), (a, b - 1))):
        d.line([((p[0] + ox) * SS, (p[1] + oy) * SS), ((q[0] + ox) * SS, (q[1] + oy) * SS)],
               fill=255, width=int(3.2 * SS))
        for (ex, ey) in (p, q):
            r = 1.6 * SS
            d.ellipse([(ex + ox) * SS - r, (ey + oy) * SS - r, (ex + ox) * SS + r, (ey + oy) * SS + r], fill=255)
    small = im.resize((N, N), Image.BOX)
    return [[small.getpixel((x, y)) >= 128 for x in range(N)] for y in range(N)]


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.join(here, "..", "Assets", "Sprites")
    os.makedirs(out, exist_ok=True)

    outer = shield_mask(0)
    rim = shield_mask(1)      # todo lo que no es contorno
    inner = shield_mask(3)    # campo rojo
    xm = x_mask()
    xs = x_mask((1, 1))

    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    px = img.load()

    def ins(m, x, y):
        return 0 <= x < N and 0 <= y < N and m[y][x]

    for y in range(N):
        for x in range(N):
            if not outer[y][x]:
                continue
            if not rim[y][x]:
                px[x, y] = OUTLINE + (255,)
            elif not inner[y][x]:
                # ribete dorado con luz arriba-izquierda
                if not ins(rim, x - 1, y) or not ins(rim, x, y - 1):
                    col = GOLD_HI
                elif not ins(rim, x + 1, y) or not ins(rim, x, y + 1):
                    col = GOLD_LO
                elif ins(inner, x + 1, y) or ins(inner, x, y + 1):
                    col = GOLD_LO  # borde interior del ribete
                else:
                    col = GOLD
                px[x, y] = col + (255,)
            else:
                # campo carmesí con degradado vertical y brillo en la esquina
                if not ins(inner, x - 1, y - 1):
                    col = RED_HI
                elif y > 22 or not ins(inner, x + 1, y + 1):
                    col = RED_LO
                elif y < 9 and (x + y) % 2 == 0:
                    col = RED_HI
                elif y > 17 and (x + y) % 2 == 0:
                    col = RED_LO
                else:
                    col = RED
                px[x, y] = col + (255,)

    # X dorada con sombra
    for y in range(N):
        for x in range(N):
            if xs[y][x] and not xm[y][x] and inner[y][x]:
                px[x, y] = X_SHADOW + (255,)
    for y in range(N):
        for x in range(N):
            if xm[y][x]:
                if not ins(xm, x, y - 1) or not ins(xm, x - 1, y):
                    col = GOLD_HI
                elif not ins(xm, x, y + 1) or not ins(xm, x + 1, y):
                    col = GOLD_LO
                else:
                    col = GOLD
                px[x, y] = col + (255,)

    # destello en el ribete
    for (x, y, c) in ((6, 3, (255, 250, 220)), (5, 3, GOLD_HI), (7, 3, GOLD_HI), (6, 4, GOLD_HI)):
        if outer[y][x] and rim[y][x]:
            px[x, y] = c + (255,)

    path = os.path.join(out, "boton_salir.png")
    img.save(path)
    print(path)


if __name__ == "__main__":
    main()
