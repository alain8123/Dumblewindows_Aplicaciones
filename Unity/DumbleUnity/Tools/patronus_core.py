"""Motor de dibujo de los patronus: lienzo por capas, esqueletos de patas, sombreado y efectos.

- Cada animal se dibuja por capas (pata lejana, cola, cuerpo, pata cercana, cabeza...) a 4x.
- Las patas usan un esqueleto de 3 huesos con IK (cadera-rodilla-corvejón-pezuña).
- Cada grupo de capas se reduce a 120x120 y se sombrea según su volumen (luz arriba-izquierda).
- Entre capas de distinto grupo se traza una línea de separación (define patas sobre el cuerpo).
- Encima: líneas de detalle (boca, ollares, plumas...), ojos, contorno, halo y destellos.
"""
import math
import random

import numpy as np
from PIL import Image, ImageDraw

FRAMES = 8
W = 120
SS = 4
GROUND = 100.0
TAU = 2 * math.pi

# Rampa plateado-azulada de oscuro a claro
RAMP = [
    (30, 58, 150),    # 0 líneas profundas
    (52, 96, 200),    # 1
    (86, 140, 232),   # 2
    (132, 188, 252),  # 3
    (186, 226, 255),  # 4
    (232, 248, 255),  # 5
    (255, 255, 255),  # 6
]
OUTLINE_DARK = (34, 66, 168)
OUTLINE_LIT = (150, 204, 255)
EYE = (16, 30, 96)
GLOW = (96, 168, 255)
SPARK = (230, 248, 255)
LIGHT = np.array([-0.55, -0.75, 0.75])
LIGHT = LIGHT / np.linalg.norm(LIGHT)


# ----------------------------------------------------------------- geometría

def catmull(pts, closed=True, n=5):
    """Suaviza una lista de puntos con Catmull-Rom."""
    if len(pts) < 3:
        return list(pts)
    out = []
    m = len(pts)
    rng = range(m) if closed else range(m - 1)
    for i in rng:
        p0 = pts[(i - 1) % m] if closed else pts[max(i - 1, 0)]
        p1 = pts[i]
        p2 = pts[(i + 1) % m] if closed else pts[i + 1]
        p3 = pts[(i + 2) % m] if closed else pts[min(i + 2, m - 1)]
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2
                                    + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in range(2)))
    if not closed:
        out.append(tuple(pts[-1]))
    return out


def rot(p, ang, piv=(0.0, 0.0)):
    ca, sa = math.cos(ang), math.sin(ang)
    x, y = p[0] - piv[0], p[1] - piv[1]
    return (piv[0] + x * ca - y * sa, piv[1] + x * sa + y * ca)


def tr(pts, dx=0.0, dy=0.0, ang=0.0, sx=1.0, sy=1.0):
    """Escala, gira (alrededor del origen local) y traslada una lista de puntos."""
    ca, sa = math.cos(ang), math.sin(ang)
    return [(dx + x * sx * ca - y * sy * sa, dy + x * sx * sa + y * sy * ca) for x, y in pts]


def lerp(a, b, t):
    return a + (b - a) * t


def lerp2(p, q, t):
    return (lerp(p[0], q[0], t), lerp(p[1], q[1], t))


def smooth01(v):
    return v * v * (3 - 2 * v)


def ik(a, b, l1, l2, bend):
    """Articulación intermedia de 2 huesos. bend=+1 hacia delante (+x) si la pierna baja."""
    dx, dy = b[0] - a[0], b[1] - a[1]
    d0 = math.hypot(dx, dy) or 1e-6
    d = min(max(d0, abs(l1 - l2) + 1e-3), l1 + l2 - 1e-3)
    ux, uy = dx / d0, dy / d0
    x = (l1 * l1 - l2 * l2 + d * d) / (2 * d)
    h = math.sqrt(max(l1 * l1 - x * x, 0.0))
    return (a[0] + ux * x + uy * h * bend, a[1] + uy * x - ux * h * bend)


def clamp_reach(a, b, reach):
    dx, dy = b[0] - a[0], b[1] - a[1]
    d = math.hypot(dx, dy)
    if d <= reach:
        return b
    return (a[0] + dx * reach / d, a[1] + dy * reach / d)


# ----------------------------------------------------------------- lienzo por capas

class Layer:
    def __init__(self, name, group, tone, sep):
        self.name, self.group, self.tone, self.sep = name, group, tone, sep
        self.img = Image.new("L", (W * SS, W * SS), 0)
        self.draw = ImageDraw.Draw(self.img)


class Rig:
    def __init__(self):
        self.layers = []
        self.cur = None
        self.lines = []   # (puntos, tono)
        self.eyes = []    # (x, y, tamaño)
        self.dots = []    # (x, y, tono)
        self.tints = []   # (capa, polígono, delta) zonas más claras/oscuras dentro de una capa
        self.under = []   # efectos detrás del animal: (x, y, alfa)
        self.wisps = []   # líneas luminosas por fuera de la silueta: (puntos, alfa)

    def layer(self, name, group=None, tone=0, sep=True):
        self.cur = Layer(name, group or name, tone, sep)
        self.layers.append(self.cur)
        return self

    def poly(self, pts, smooth=False, erase=False):
        if smooth:
            pts = catmull(pts, True)
        if len(pts) >= 3:
            self.cur.draw.polygon([(x * SS, y * SS) for x, y in pts], fill=0 if erase else 255)

    def ellipse(self, cx, cy, rx, ry, ang=0.0, erase=False):
        if ang == 0.0:
            self.cur.draw.ellipse([(cx - rx) * SS, (cy - ry) * SS, (cx + rx) * SS, (cy + ry) * SS],
                                  fill=0 if erase else 255)
        else:
            pts = [(rx * math.cos(a), ry * math.sin(a)) for a in np.linspace(0, TAU, 28, endpoint=False)]
            self.poly(tr(pts, cx, cy, ang), erase=erase)

    def circle(self, p, r):
        self.ellipse(p[0], p[1], r, r)

    def capsule(self, p, q, r1, r2=None):
        r2 = r1 if r2 is None else r2
        dx, dy = q[0] - p[0], q[1] - p[1]
        L = math.hypot(dx, dy) or 1e-6
        nx, ny = -dy / L, dx / L
        self.poly([(p[0] + nx * r1, p[1] + ny * r1), (q[0] + nx * r2, q[1] + ny * r2),
                   (q[0] - nx * r2, q[1] - ny * r2), (p[0] - nx * r1, p[1] - ny * r1)])
        self.circle(p, r1)
        self.circle(q, r2)

    def stroke(self, pts, r0, r1, smooth=True):
        if smooth and len(pts) > 2:
            pts = catmull(pts, False, 4)
        n = len(pts) - 1
        for i in range(n):
            self.capsule(pts[i], pts[i + 1], lerp(r0, r1, i / n), lerp(r0, r1, (i + 1) / n))

    def band(self, top, bottom, smooth=True):
        """Polígono entre una línea superior y otra inferior (ambas de izquierda a derecha)."""
        if smooth:
            top = catmull(top, False, 4)
            bottom = catmull(bottom, False, 4)
        self.poly(list(top) + list(reversed(bottom)))

    def line(self, pts, tone=1, smooth=False):
        if smooth and len(pts) > 2:
            pts = catmull(pts, False, 3)
        self.lines.append((list(pts), tone))

    def eye(self, x, y, size=1):
        self.eyes.append((x, y, size))

    def dot(self, x, y, tone=1):
        self.dots.append((x, y, tone))

    def tint(self, pts, delta, smooth=True, layer=None):
        """Aclara (+) u oscurece (-) la zona del polígono en una capa (manchas, calcetines...).
        layer: nombre de la capa; por defecto la actual."""
        lay = self.cur if layer is None else next(l for l in self.layers if l.name == layer)
        self.tints.append((lay, catmull(pts, True) if smooth else pts, delta))

    def wisp(self, pts, alpha=200, smooth=False):
        """Línea luminosa que puede salir de la silueta (bigotes, estelas)."""
        if smooth and len(pts) > 2:
            pts = catmull(pts, False, 3)
        self.wisps.append((list(pts), alpha))


# ----------------------------------------------------------------- marcha y patas

def foot_path(u, duty, stride, lift, base_x, ground=GROUND):
    """Trayectoria del pie: apoyo (retrocede por el suelo) y vuelo (sube y avanza)."""
    u %= 1.0
    if u < duty:
        s = u / duty
        return base_x + stride * (1 - 2 * s), ground, 0.0
    v = (u - duty) / (1 - duty)
    return base_x - stride + 2 * stride * smooth01(v), ground - lift * math.sin(math.pi * v), v


class LegSpec:
    def __init__(self, upper, mid, low, r_up, r_mid, r_low, foot="hoof", foot_r=1.6, flex=0.9, mass=None,
                 angle0=0.06, stride=7.0, lift=6.0, duty=0.55, dx=0.0):
        self.upper, self.mid, self.low = upper, mid, low
        self.r_up, self.r_mid, self.r_low = r_up, r_mid, r_low
        self.foot, self.foot_r, self.flex, self.mass = foot, foot_r, flex, mass
        self.angle0, self.stride, self.lift, self.duty, self.dx = angle0, stride, lift, duty, dx


def draw_foot(r, ankle, foot, kind, size):
    fx, fy = foot
    if kind == "hoof":
        ang = math.atan2(fy - ankle[1], fx - ankle[0]) - math.pi / 2
        pts = [(-size * 0.7, -size * 1.6), (size * 0.8, -size * 1.5), (size * 1.3, 0.0), (-size * 0.9, 0.0)]
        r.poly(tr(pts, fx, fy, ang * 0.5))
    elif kind == "cloven":  # pezuña hendida (ciervo, cabra, jabalí)
        ang = math.atan2(fy - ankle[1], fx - ankle[0]) - math.pi / 2
        pts = [(-size * 0.6, -size * 1.5), (size * 0.6, -size * 1.5), (size * 1.5, 0.0), (-size * 0.7, 0.0)]
        r.poly(tr(pts, fx, fy, ang * 0.5))
    elif kind == "paw":
        r.ellipse(fx + size * 0.5, fy - size * 0.75, size * 1.3, size * 0.8)
    elif kind == "bigpaw":
        r.ellipse(fx + size * 0.6, fy - size * 0.85, size * 1.6, size * 0.95)
    elif kind == "stump":
        r.ellipse(fx + 0.3, fy - size * 0.75, size * 1.2, size * 0.8)


def leg_layers(r, name, tone):
    r.layer(name + "_mass", name + "_mass", tone=tone, sep=False)
    mass_layer = r.cur
    r.layer(name, name, tone=tone)
    return mass_layer


def hind_leg(r, hip, u, L, ground=GROUND, name="near_hind", tone=0):
    mass_layer = leg_layers(r, name, tone)
    fx, fy, v = foot_path(u, L.duty, L.stride, L.lift, hip[0] + L.dx, ground)
    flex = math.sin(math.pi * v) if v > 0 else 0.0
    th = L.angle0 - L.flex * flex  # ángulo del metatarso respecto a la vertical (+: corvejón detrás)
    hock = (fx - L.low * math.sin(th), fy - L.low * math.cos(th))
    hock = clamp_reach(hip, hock, L.upper + L.mid - 0.3)
    foot = (hock[0] + L.low * math.sin(th), hock[1] + L.low * math.cos(th))
    stifle = ik(hip, hock, L.upper, L.mid, +1)
    mass = L.mass or (L.r_up * 1.6)
    mid = lerp2(hip, stifle, 0.42)
    ang = math.atan2(stifle[1] - hip[1], stifle[0] - hip[0])
    leg_layer, r.cur = r.cur, mass_layer
    r.ellipse(mid[0] - 0.8, mid[1], L.upper * 0.6 + mass * 0.25, mass, ang)
    r.cur = leg_layer
    r.capsule(stifle, hock, L.r_up * 0.8, L.r_mid)
    r.capsule(hock, foot, L.r_mid * 0.8, L.r_low)
    draw_foot(r, hock, foot, L.foot, L.foot_r)
    return dict(hip=hip, stifle=stifle, hock=hock, foot=foot)


def front_leg(r, shoulder, u, L, ground=GROUND, name="near_front", tone=0):
    mass_layer = leg_layers(r, name, tone)
    fx, fy, v = foot_path(u, L.duty, L.stride, L.lift, shoulder[0] + L.dx, ground)
    flex = math.sin(math.pi * v) if v > 0 else 0.0
    th = -L.angle0 * 0.5 + L.flex * flex * 1.3  # >0: la rodilla adelanta al pie (mano recogida)
    knee = (fx + L.low * math.sin(th), fy - L.low * math.cos(th))
    knee = clamp_reach(shoulder, knee, L.upper + L.mid - 0.3)
    foot = (knee[0] - L.low * math.sin(th), knee[1] + L.low * math.cos(th))
    elbow = ik(shoulder, knee, L.upper, L.mid, -1)
    mass = L.mass or (L.r_up * 1.5)
    mid = lerp2(shoulder, elbow, 0.5)
    ang = math.atan2(elbow[1] - shoulder[1], elbow[0] - shoulder[0])
    leg_layer, r.cur = r.cur, mass_layer
    r.ellipse(mid[0], mid[1], L.upper * 0.55 + mass * 0.25, mass, ang)
    r.cur = leg_layer
    r.capsule(elbow, knee, L.r_up * 0.85, L.r_mid)
    r.capsule(knee, foot, L.r_mid * 0.85, L.r_low)
    draw_foot(r, knee, foot, L.foot, L.foot_r)
    return dict(shoulder=shoulder, elbow=elbow, knee=knee, foot=foot)


GAITS = {
    # fase de cada pata: (trasera cercana, delantera cercana, trasera lejana, delantera lejana)
    "walk": (0.0, 0.25, 0.5, 0.75),
    "trot": (0.0, 0.5, 0.5, 0.0),
}


def quad(r, u, S):
    """Cuadrúpedo mirando a la derecha. S: especificación de la especie (coordenadas absolutas)."""
    gait = GAITS[S.get("gait", "trot")]
    dy = S.get("bob", 1.0) * math.cos(TAU * u * 2)
    hipP, shP = S["hip"], S["shoulder"]
    far_dx, far_dy = S.get("far", (3.0, -1.5))
    hind, front = S["hind"], S["front"]
    nod = S.get("nod", 0.6) * math.sin(TAU * u * 2 + S.get("nod_phase", 0.6))
    hx, hy = S["head_pos"]
    head = (hx, hy + dy + nod)
    ang = S.get("head_ang", 0.0) + nod * 0.03
    ctx = dict(u=u, dy=dy, head=head, ang=ang)

    if S.get("far_extra"):
        S["far_extra"](r, ctx)

    hind_leg(r, (hipP[0] + far_dx, hipP[1] + dy + far_dy), u + gait[2], hind, GROUND + far_dy, "far_hind", -2)
    front_leg(r, (shP[0] + far_dx, shP[1] + dy + far_dy), u + gait[3], front, GROUND + far_dy, "far_front", -2)

    if S.get("tail"):
        r.layer("tail", "tail", tone=S.get("tail_tone", 0))
        S["tail"](r, ctx)

    r.layer("body", "body")
    r.band([(x, y + dy) for x, y in S["back"]], [(x, y + dy) for x, y in S["belly"]])
    if S.get("neck"):
        ntop, nbot = S["neck"]

        def nk(pts):
            k = len(pts) - 1
            return [(x, y + dy + nod * (i / k) ** 1.5) for i, (x, y) in enumerate(pts)]
        r.band(nk(ntop), nk(nbot))
    if S.get("body_extra"):
        S["body_extra"](r, ctx)

    ctx["hl"] = hind_leg(r, (hipP[0], hipP[1] + dy), u + gait[0], hind)
    ctx["fl"] = front_leg(r, (shP[0], shP[1] + dy), u + gait[1], front)
    if S.get("leg_extra"):
        S["leg_extra"](r, ctx)

    if S.get("behind_head"):
        S["behind_head"](r, ctx)
    r.layer("head", "head", sep=S.get("head_sep", True))
    S["head"](r, ctx)
    if S.get("front_extra"):
        S["front_extra"](r, ctx)


def H(r, ctx, pts, smooth=True, scale=1.0):
    """Polígono relativo al pivote de la cabeza (girado con el cabeceo)."""
    r.poly(tr(pts, ctx["head"][0], ctx["head"][1], ctx["ang"], scale, scale), smooth=smooth)


def HP(ctx, p):
    return tr([p], ctx["head"][0], ctx["head"][1], ctx["ang"])[0]


def HL(r, ctx, pts, tone=1, smooth=False):
    r.line(tr(pts, ctx["head"][0], ctx["head"][1], ctx["ang"]), tone, smooth)


def HE(r, ctx, p, size=2):
    x, y = HP(ctx, p)
    r.eye(x, y, size)


# ----------------------------------------------------------------- render

def gauss(a, sigma):
    rad = max(1, int(sigma * 2.5))
    xs = np.arange(-rad, rad + 1)
    k = np.exp(-xs * xs / (2 * sigma * sigma))
    k /= k.sum()
    h, w = a.shape
    p = np.pad(a, ((rad, rad), (0, 0)))
    t = sum(k[i] * p[i:i + h, :] for i in range(2 * rad + 1))
    p = np.pad(t, ((0, 0), (rad, rad)))
    return sum(k[i] * p[:, i:i + w] for i in range(2 * rad + 1))


def shift(a, dx, dy, fill):
    """Desplaza el array: el resultado en (y, x) es a[y - dy, x - dx]."""
    out = np.full_like(a, fill)
    h, w = a.shape
    xs0, xs1 = max(0, dx), min(w, w + dx)
    ys0, ys1 = max(0, dy), min(h, h + dy)
    out[ys0:ys1, xs0:xs1] = a[ys0 - dy:ys1 - dy, xs0 - dx:xs1 - dx]
    return out


N4 = ((1, 0), (-1, 0), (0, 1), (0, -1))


def bresenham(p, q):
    x0, y0 = int(math.floor(p[0])), int(math.floor(p[1]))
    x1, y1 = int(math.floor(q[0])), int(math.floor(q[1]))
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    err = dx + dy
    while True:
        yield x0, y0
        if x0 == x1 and y0 == y1:
            return
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy


def poly_mask(pts):
    img = Image.new("L", (W * SS, W * SS), 0)
    ImageDraw.Draw(img).polygon([(x * SS, y * SS) for x, y in pts], fill=255)
    return np.asarray(img.resize((W, W), Image.BOX)) >= 128


def render_frame(r, frame, sparkles, pulse):
    cov = [np.asarray(l.img.resize((W, W), Image.BOX), dtype=np.float32) / 255.0 for l in r.layers]
    label = np.full((W, W), -1, dtype=np.int32)
    for i, c in enumerate(cov):
        label[c >= 0.5] = i
    union = label >= 0
    # quitar píxeles sueltos
    nb = sum(shift(union, dx, dy, False).astype(np.int32) for dx, dy in N4)
    union &= ~(nb == 0)
    label[~union] = -1

    # sombreado por grupo según su volumen
    groups = {}
    for i, l in enumerate(r.layers):
        groups.setdefault(l.group, []).append(i)
    tone = np.zeros((W, W), dtype=np.float32)
    for g, idxs in groups.items():
        m = np.isin(label, idxs)
        if not m.any():
            continue
        gc = np.maximum.reduce([cov[i] for i in idxs])
        hgt = 0.5 * gauss(gc, 1.3) + 0.5 * gauss(gc, 3.2)
        gy, gx = np.gradient(hgt)
        nx, ny = -gx * 3.0, -gy * 3.0
        nrm = np.sqrt(nx * nx + ny * ny + 1.0)
        lam = (nx * LIGHT[0] + ny * LIGHT[1] + LIGHT[2]) / nrm
        val = 0.05 + 0.95 * lam + 0.35 * (hgt - 0.5)
        tone[m] = val[m]
    idx = np.clip(np.round(tone * 6.0), 2, 6).astype(np.int32)
    for i, l in enumerate(r.layers):
        if l.tone:
            m = label == i
            idx[m] = np.clip(idx[m] + l.tone, 1, 5)
    layer_index = {id(l): i for i, l in enumerate(r.layers)}
    for lay, pts, delta in r.tints:
        m = poly_mask(pts) & (label == layer_index[id(lay)])
        idx[m] = np.clip(idx[m] + delta, 1, 6)

    # líneas de separación: píxel de la capa de atrás junto a una capa de delante de otro grupo
    gid = {}
    grp = np.array([gid.setdefault(l.group, len(gid)) for l in r.layers] + [-1])
    sepf = np.array([l.sep for l in r.layers] + [False])
    sepline = np.zeros((W, W), dtype=bool)
    for dx, dy in N4:
        nl = shift(label, dx, dy, -1)
        f = (nl > label) & (label >= 0)
        f &= grp[nl] != grp[label]
        f &= sepf[nl]
        sepline |= f
    idx[sepline] = np.minimum(idx[sepline] - 2, 2).clip(1, 2)

    # detalles
    for pts, t in r.lines:
        for a, b in zip(pts, pts[1:]):
            for x, y in bresenham(a, b):
                if 0 <= x < W and 0 <= y < W and union[y, x]:
                    idx[y, x] = t
    for x, y, t in r.dots:
        ix, iy = int(math.floor(x)), int(math.floor(y))
        if 0 <= ix < W and 0 <= iy < W and union[iy, ix]:
            idx[iy, ix] = t

    rgba = np.zeros((W, W, 4), dtype=np.uint8)
    ramp = np.array(RAMP, dtype=np.uint8)
    rgba[union, :3] = ramp[np.clip(idx[union], 0, 6)]
    rgba[union, 3] = 255

    # contorno: oscuro en la sombra, claro en el lado iluminado (arriba / izquierda)
    empty_up = ~shift(union, 0, 1, False)
    empty_left = ~shift(union, 1, 0, False)
    empty_down = ~shift(union, 0, -1, False)
    empty_right = ~shift(union, -1, 0, False)
    edge = union & (empty_up | empty_left | empty_down | empty_right)
    lit = edge & (empty_up | empty_left) & ~empty_down
    rgba[edge & ~lit, :3] = OUTLINE_DARK
    rgba[lit, :3] = OUTLINE_LIT

    for x, y, s in r.eyes:
        ix, iy = int(math.floor(x)), int(math.floor(y))
        for ex in range(s):
            for ey in range(s):
                if 0 <= ix + ex < W and 0 <= iy + ey < W:
                    rgba[iy + ey, ix + ex] = EYE + (255,)
        if s >= 2 and 0 <= ix < W and 0 <= iy < W:
            rgba[iy, ix] = (190, 236, 255, 255)

    # halo exterior escalonado
    cur = union.copy()
    alphas = [150 + 40 * pulse, 70 + 25 * pulse, 32 + 12 * pulse, 14 + 6 * pulse]
    yy, xx = np.mgrid[0:W, 0:W]
    for k, a in enumerate(alphas):
        grown = cur | shift(cur, 1, 0, False) | shift(cur, -1, 0, False) | shift(cur, 0, 1, False) | shift(cur, 0, -1, False)
        ring = grown & ~cur
        if k >= 2:
            ring &= ((xx + yy + frame) % 2 == 0)
        rgba[ring, :3] = GLOW
        rgba[ring, 3] = int(a)
        cur = grown

    for pts, a in r.wisps:
        for p, q in zip(pts, pts[1:]):
            for x, y in bresenham(p, q):
                if 0 <= x < W and 0 <= y < W and not union[y, x]:
                    rgba[y, x] = OUTLINE_LIT + (a,)

    # efectos bajo el animal (agua, niebla)
    for x, y, a in r.under:
        ix, iy = int(math.floor(x)), int(math.floor(y))
        if 0 <= ix < W and 0 <= iy < W and not union[iy, ix]:
            rgba[iy, ix] = GLOW + (int(a),)

    # destellos
    for s in sparkles:
        age = (frame + s["off"]) % FRAMES
        x = int(round(s["x"] - s["vx"] * age))
        y = int(round(s["y"] - s["vy"] * age))
        life = 1 - age / FRAMES
        if not (1 <= x < W - 1 and 1 <= y < W - 1) or union[y, x]:
            continue
        a = int(255 * min(1.0, life * 1.5))
        rgba[y, x] = SPARK + (a,)
        if s["big"] and 1 <= age <= 3:
            for dx, dy in N4:
                if not union[y + dy, x + dx]:
                    rgba[y + dy, x + dx] = GLOW + (int(a * 0.7),)
    return Image.fromarray(rgba, "RGBA")


def make_sparkles(name, bbox):
    rnd = random.Random(name)
    x0, y0, x1, y1 = bbox
    sp = []
    for i in range(22):
        sp.append(dict(
            x=rnd.uniform(x0 + 4, x1 - 6) if i % 3 else rnd.uniform(x0 - 2, x0 + 12),
            y=rnd.uniform(y0, y1),
            vx=rnd.uniform(1.0, 2.6), vy=rnd.uniform(0.3, 1.1),
            off=rnd.randrange(FRAMES), big=rnd.random() < 0.4))
    return sp


def render_animal(name, fn):
    rigs = []
    for i in range(FRAMES):
        r = Rig()
        fn(r, i / FRAMES)
        rigs.append(r)
    acc = np.zeros((W, W), dtype=bool)
    for l in rigs[0].layers:
        acc |= np.asarray(l.img.resize((W, W), Image.BOX)) >= 128
    ys, xs = np.nonzero(acc)
    sparkles = make_sparkles(name, (xs.min(), ys.min(), xs.max(), ys.max()))
    sheet = Image.new("RGBA", (W * FRAMES, W), (0, 0, 0, 0))
    for i, r in enumerate(rigs):
        pulse = (math.sin(TAU * i / FRAMES) + 1) / 2
        sheet.paste(render_frame(r, i, sparkles, pulse), (i * W, 0))
    return sheet
