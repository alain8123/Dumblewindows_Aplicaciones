"""Definición de cada patronus. Coordenadas en píxeles de un frame de 120x120, suelo en y=100,
animal mirando a la derecha. Ver patronus_core.py para las utilidades de dibujo."""
import math

from patronus_core import (GROUND, TAU, H, HE, HL, HP, LegSpec, catmull, lerp, lerp2, quad, tr)


# ================================================================= équidos

def horse_head(r, c):
    H(r, c, [(0, 0), (4, 0.5), (8, 3), (12, 7), (15, 11), (17, 14.5), (17.6, 17), (16.2, 19.2), (13, 19.6),
             (11, 17.8), (8, 15.8), (4.5, 14.6), (1, 12.5), (-2.2, 9), (-2.4, 4)])
    # orejas
    H(r, c, [(-1.6, 1), (-0.6, -4), (0.6, -8), (2.4, -3.5), (2.6, 0.5)])
    HL(r, c, [(0.4, -6), (0.8, -1.5)], 2)
    HE(r, c, (4.6, 3.6), 2)
    r_ = HP(c, (15.6, 15.2))
    r.dot(*r_, 1)
    r.dot(r_[0] + 1, r_[1], 1)
    HL(r, c, [(16.8, 18.2), (14, 18.4)], 1)               # boca
    HL(r, c, [(10.8, 16.6), (8.8, 13.4), (6.4, 10.6), (3.4, 10)], 2, smooth=True)  # carrillo


def horse_tail(length=1.0, flow=1.0):
    def f(r, c):
        u, dy = c["u"], c["dy"]
        w = math.sin(TAU * u)
        base = (27, 51 + dy)
        pts = [base, (21 - w * 0.6 * flow, 55 + dy), (17 - w * 1.5 * flow, 64 + dy),
               (16 - w * 2.6 * flow, 64 + 14 * length + dy)]
        r.stroke(pts, 3.0, 2.2 * flow)
        tip = pts[-1]
        r.stroke([pts[2], (tip[0] - 1.8, tip[1] + 2.0)], 2.0, 1.2)
        r.line(catmull([(22, 56 + dy), (18.6 - w, 64 + dy), (17.8 - w * 2.2, 70 + 10 * length + dy)], False, 3), 2)
    return f


def horse_mane(wild=1.0, length=1.0):
    def f(r, c):
        u, dy = c["u"], c["dy"]
        hx, hy = c["head"]
        r.layer("mane", "mane", tone=-1)
        crest = [(hx - 2.0, hy - 0.5), (hx - 8, hy + 5), (hx - 15, hy + 12), (hx - 21, hy + 18.5), (68, 45 + dy)]
        crest = catmull(crest, False, 3)
        n = len(crest)
        # cresta: banda por encima del borde superior del cuello
        r.stroke(crest, 3.0, 3.0, smooth=False)
        k = 0
        for i in range(0, n - 1, 2):
            bx, by = crest[i]
            t = i / (n - 1)
            sway = math.sin(TAU * u * 2 + t * 4) * 1.0 * wild
            L = (6 + 4 * math.sin(t * math.pi)) * length
            tip = (bx - 3.0 - sway - L * 0.25, by + L)
            midp = (bx - 0.6 - sway * 0.5, by + L * 0.5)
            r.stroke([(bx, by), midp, tip], 2.2, 0.6)
            r.line(catmull([(bx + 0.6, by + 0.5), midp, tip], False, 3), 2)
            k += 1
        # tupé
        r.stroke([(hx + 0.5, hy - 0.5), (hx + 3.6, hy + 1.6), (hx + 4.8, hy + 4.0)], 1.5, 0.6)
    return f


HORSE_FRONT = LegSpec(upper=12, mid=15, low=13, r_up=4.6, r_mid=2.3, r_low=1.7, foot="hoof", foot_r=2.1,
                      mass=5.6, stride=7.5, lift=7, duty=0.52, flex=0.85)
HORSE_HIND = LegSpec(upper=14, mid=16, low=16, r_up=5.2, r_mid=2.5, r_low=1.7, foot="hoof", foot_r=2.1,
                     mass=8.0, stride=7.5, lift=6, duty=0.52, flex=0.7, angle0=0.12)

HORSE = dict(
    gait="trot", bob=1.0, hip=(36, 55), shoulder=(80, 60), far=(3.0, -1.5),
    back=[(24, 58), (27, 50), (33, 46), (41, 46.5), (51, 49), (61, 49), (69, 46), (75, 43)],
    belly=[(24, 60), (28, 68), (39, 72.5), (53, 74), (66, 72), (78, 70), (86, 64), (88, 57)],
    neck=([(66, 47), (76, 40), (84, 31), (90, 24)], [(82, 64), (88, 54), (94, 42), (97, 33)]),
    head_pos=(90, 23), head_ang=0.0, nod=0.8,
    front=HORSE_FRONT, hind=HORSE_HIND,
    head=horse_head, tail=horse_tail(), body_extra=horse_mane(),
)


def horse(r, u):
    quad(r, u, HORSE)


# ================================================================= cérvidos

DEER_EAR = [(0.8, 0.6), (0.4, -2.6), (-1.8, -6.0), (-5.0, -8.6), (-7.6, -8.4), (-7.2, -5.0), (-5.0, -1.4),
            (-1.8, 2.2)]


def deer_ear_far(size=1.0):
    def f(r, c):
        r.layer("ear_far", "ear_far", tone=-2)
        H(r, c, tr(DEER_EAR, 3.4, -1.2, -0.25), scale=size)
    return f


def deer_head(ears="big", size=1.0):
    def f(r, c):
        k = size
        H(r, c, [(0, 0), (4, -0.6), (8, 0.6), (12.5, 2.6), (16.5, 5.2), (18.2, 7.2), (17.4, 9.2), (14, 9.9),
                 (9, 9.3), (4, 8.7), (0, 7.6), (-2.4, 5), (-2.4, 1.5)], scale=k)
        ear = DEER_EAR if ears == "big" else tr(DEER_EAR, 0, 0, 0, 0.8, 0.8)
        H(r, c, ear, scale=k)
        HL(r, c, [(-1.4 * k, -0.2 * k), (-4.6 * k, -4.4 * k), (-6.4 * k, -5.6 * k)], 2)
        HE(r, c, (5.4 * k, 2.2 * k), 2)
        HL(r, c, [(7.8 * k, 3.4 * k), (9.4 * k, 4.6 * k)], 2)        # lagrimal
        x, y = HP(c, (16.8 * k, 6.6 * k))
        r.dot(x, y, 1)
        HL(r, c, [(17.0 * k, 8.6 * k), (14.0 * k, 8.9 * k)], 1)       # boca
        HL(r, c, [(10.4 * k, 9.0 * k), (7.0 * k, 6.4 * k), (3.0 * k, 5.8 * k)], 2, smooth=True)  # mandíbula
    return f


def antler(r, c, dx, dy, s, big):
    """Cuerna de ciervo en coordenadas de la cabeza (origen en la nuca)."""
    def P(pts):
        return tr([(x * s + dx, y * s + dy) for x, y in pts], c["head"][0], c["head"][1], c["ang"])
    if big:
        # vara principal: sube y se abre hacia atrás; las puntas miran adelante
        r.stroke(P([(1, -1), (-2.4, -6), (-6.4, -11), (-9.0, -16), (-9.6, -21), (-8.0, -25)]), 1.5 * s, 0.9 * s)
        r.stroke(P([(-0.6, -3.0), (4.0, -4.6), (7.4, -8.4)]), 1.05 * s, 0.6 * s)    # luchadera
        r.stroke(P([(-3.6, -8.0), (0.6, -10.4), (2.6, -13.8)]), 0.95 * s, 0.6 * s)  # puntal
        r.stroke(P([(-7.6, -13.4), (-3.8, -16.0), (-2.6, -19.2)]), 0.9 * s, 0.55 * s)
        # corona
        r.stroke(P([(-9.6, -21.0), (-13.4, -24.0), (-15.0, -27.0)]), 0.85 * s, 0.5 * s)
        r.stroke(P([(-8.6, -23.4), (-9.6, -27.4), (-9.2, -30.0)]), 0.85 * s, 0.5 * s)
        r.stroke(P([(-8.0, -25.0), (-5.0, -27.4), (-3.8, -29.4)]), 0.8 * s, 0.5 * s)
    else:
        r.stroke(P([(0.8, -1), (-1.2, -5), (-2.6, -9), (-2.2, -13)]), 1.2 * s, 0.7 * s)
        r.stroke(P([(-1.2, -5.2), (2.0, -7.0), (3.2, -9.6)]), 0.8 * s, 0.5 * s)
        r.stroke(P([(-2.6, -9.0), (-5.2, -10.6), (-6.2, -12.8)]), 0.75 * s, 0.5 * s)


def stag_antlers(big=True, s=1.0):
    def far(r, c):
        deer_ear_far(1.0)(r, c)
        r.layer("antler_far", "antler_far", tone=-2)
        antler(r, c, 6.5, 0.4, s * 0.95, big)

    def near(r, c):
        r.layer("antler", "antler")
        antler(r, c, 0.0, 0.0, s, big)
    return far, near


def deer_tail(r, c):
    dy = c["dy"]
    w = math.sin(TAU * c["u"] * 2) * 0.6
    r.stroke([(29.5, 50.5 + dy), (28.0, 52.5 + dy), (27.6 - w, 55.5 + dy)], 1.5, 1.0)


def rump_patch(x0, y0):
    def f(r, c):
        dy = c["dy"]
        r.tint([(x0 - 3, y0 - 6 + dy), (x0 + 6, y0 - 7 + dy), (x0 + 8, y0 + 1 + dy), (x0 + 2, y0 + 10 + dy),
                (x0 - 3, y0 + 6 + dy)], +1)
    return f


def throat_ruff(pts):
    def f(r, c):
        dy = c["dy"]
        for i, (x, y) in enumerate(pts):
            sway = math.sin(TAU * c["u"] * 2 + i) * 0.5
            r.poly([(x - 2.4, y + dy - 1), (x + 2.4, y + dy - 1.4), (x - 1.0 + sway, y + dy + 4.6)])
            r.line([(x - 0.4, y + dy), (x - 0.8 + sway, y + dy + 3.4)], 2)
    return f


def combine(*fs):
    def f(r, c):
        for g in fs:
            if g:
                g(r, c)
    return f


STAG_FRONT = LegSpec(upper=11, mid=15, low=15, r_up=3.8, r_mid=1.8, r_low=1.3, foot="cloven", foot_r=1.6,
                     mass=5.0, stride=7.5, lift=8, duty=0.5, flex=0.95)
STAG_HIND = LegSpec(upper=13, mid=16, low=18, r_up=4.4, r_mid=2.0, r_low=1.3, foot="cloven", foot_r=1.6,
                    mass=6.6, stride=7.5, lift=6, duty=0.5, flex=0.75, angle0=0.14)
_far, _near = stag_antlers(True, 0.92)
STAG = dict(
    gait="trot", bob=1.1, hip=(39, 56), shoulder=(78, 59), far=(3.0, -1.5),
    back=[(28, 57), (30, 50), (36, 46.5), (46, 48.5), (58, 49.5), (68, 46.5), (75, 43)],
    belly=[(28, 59), (31, 65), (42, 69.5), (55, 70.5), (67, 69), (78, 68), (84, 62), (86, 55)],
    neck=([(66, 47), (77, 42), (85, 37), (89, 34)], [(80, 64), (87, 56), (93, 48), (96, 41)]),
    head_pos=(89, 35), head_ang=0.12, nod=0.7,
    front=STAG_FRONT, hind=STAG_HIND,
    head=deer_head("big", 1.0), tail=deer_tail,
    body_extra=combine(rump_patch(32, 53), throat_ruff([(89, 50), (92.5, 45), (85.5, 55)])),
    behind_head=_far, front_extra=_near,
)

_dfar, _dnear = stag_antlers(False, 1.0)
DEER = dict(STAG, back=[(30, 57), (32, 50.5), (38, 47.5), (48, 49.5), (59, 50), (68, 47.5), (74, 44)],
            belly=[(30, 59), (33, 65), (43, 69), (55, 70), (66, 68.5), (77, 67), (82, 61), (84, 55)],
            neck=([(66, 48), (76, 41), (83, 34), (87, 30)], [(78, 63), (85, 54), (90, 45), (93, 37)]),
            head_pos=(87, 31), head_ang=0.22, hip=(40, 56), shoulder=(76, 59),
            head=deer_head("big", 0.95), behind_head=_dfar, front_extra=_dnear,
            body_extra=rump_patch(33, 53))

DOE = dict(DEER, back=[(30, 58), (32, 52), (38, 49), (48, 51), (59, 51.5), (68, 49), (73, 46)],
           belly=[(30, 60), (33, 66), (43, 69.5), (55, 70.5), (66, 69), (76, 67.5), (81, 62), (83, 56)],
           neck=([(66, 50), (75, 43), (81, 36), (85, 31)], [(77, 64), (83, 55), (88, 45), (91, 37)]),
           head_pos=(85, 31), head_ang=0.3, behind_head=deer_ear_far(0.95), front_extra=None,
           head=deer_head("big", 0.92), body_extra=rump_patch(33, 54),
           front=LegSpec(upper=11, mid=15, low=15, r_up=3.4, r_mid=1.6, r_low=1.15, foot="cloven", foot_r=1.4,
                         mass=4.4, stride=7, lift=8, duty=0.5, flex=0.95),
           hind=LegSpec(upper=13, mid=16, low=18, r_up=4.0, r_mid=1.8, r_low=1.15, foot="cloven", foot_r=1.4,
                        mass=6.0, stride=7, lift=6, duty=0.5, flex=0.75, angle0=0.14))


def unicorn_head(r, c):
    horse_head(r, c)
    # barba de chivo
    H(r, c, [(11.5, 18.5), (14, 19.4), (12.6, 23.6), (11.6, 25.2), (11, 22)])
    HL(r, c, [(12.6, 20), (12.2, 23.4)], 2)


def unicorn_horn(r, c):
    r.layer("horn", "horn")
    r.poly(tr([(1.2, -0.6), (4.2, 0.6), (11.5, -16.5)], c["head"][0], c["head"][1], c["ang"]))
    for k in range(5):
        t = 0.15 + k * 0.17
        a = lerp2((2.0, -1.2), (11.0, -15.6), t)
        HL(r, c, [(a[0] - 0.9 + t, a[1] + 0.4), (a[0] + 1.3 - t, a[1] - 0.8)], 2)


def fetlock_tufts(r, c):
    for leg in (c["hl"], c["fl"]):
        fx, fy = leg["foot"]
        r.layer("tuft", "tuft_%d" % int(fx))
        r.poly([(fx - 2.6, fy - 6.5), (fx + 1.6, fy - 7.5), (fx + 1.2, fy - 3.4), (fx - 1.2, fy - 1.4),
                (fx - 3.8, fy - 0.6)], smooth=True)


UNICORN = dict(HORSE, back=[(25, 57), (28, 50), (34, 46.5), (42, 47.5), (52, 50), (61, 50), (69, 46.5), (75, 43)],
               belly=[(25, 59), (29, 67), (40, 71.5), (53, 72.5), (66, 70.5), (78, 69), (85, 63), (87, 57)],
               front=LegSpec(upper=12, mid=15, low=13, r_up=4.2, r_mid=2.0, r_low=1.5, foot="hoof", foot_r=1.9,
                             mass=5.2, stride=8, lift=8, duty=0.5, flex=0.95),
               hind=LegSpec(upper=14, mid=16, low=16, r_up=4.8, r_mid=2.2, r_low=1.5, foot="hoof", foot_r=1.9,
                            mass=7.2, stride=8, lift=7, duty=0.5, flex=0.75, angle0=0.12),
               head=unicorn_head, front_extra=unicorn_horn, leg_extra=fetlock_tufts,
               tail=horse_tail(1.35, 1.5), body_extra=horse_mane(1.4, 1.35))


# ================================================================= felinos

def whiskers(r, c, root, n=3, length=7.0, spread=0.18):
    x, y = HP(c, root)
    for k in range(n):
        a = c["ang"] + (k - (n - 1) / 2) * spread + 0.08
        r.wisp([(x + 1, y), (x + length * math.cos(a), y + length * math.sin(a) + k * 0.4)], 150)


def cat_head(r, c):
    H(r, c, [(0, 0), (4, -2.2), (8, -2.0), (11.5, 0.4), (13, 3), (13.2, 5), (12, 6.6), (9, 8), (5, 8.4), (1, 7),
             (-1.5, 4), (-1.2, 1)])
    H(r, c, [(1.6, -1.0), (2.8, -8.2), (7.0, -1.8)], smooth=False)       # oreja
    HL(r, c, [(3.0, -2.0), (3.2, -5.6), (5.2, -2.2)], 2)
    HE(r, c, (8.0, 0.8), 2)
    HL(r, c, [(9.6, 1.0), (10.6, 1.6)], 1)
    x, y = HP(c, (12.6, 3.4))
    r.dot(x, y, 0)
    HL(r, c, [(12.6, 4.4), (11.8, 5.8), (10.4, 5.6)], 1)
    whiskers(r, c, (11.6, 4.8), 3, 7.0)


def cat_ear_far(r, c):
    r.layer("ear_far", "ear_far", tone=-2)
    H(r, c, [(-1.6, 0.2), (-1.0, -7.2), (2.6, -1.6)], smooth=False)


def cat_tail(r, c):
    u, dy = c["u"], c["dy"]
    w = math.sin(TAU * u)
    pts = [(34, 64 + dy), (28.5, 58 + dy), (26 + w * 0.6, 49 + dy), (27 + w * 1.4, 41 + dy), (31 + w * 2.2, 37.5 + dy)]
    r.stroke(pts, 2.5, 1.8)


CAT_FRONT = LegSpec(upper=9, mid=11, low=7, r_up=3.2, r_mid=1.8, r_low=1.5, foot="paw", foot_r=1.9,
                    mass=4.2, stride=6, lift=5, duty=0.62, flex=0.9, angle0=0.0)
CAT_HIND = LegSpec(upper=11, mid=12, low=10, r_up=4.0, r_mid=2.0, r_low=1.5, foot="paw", foot_r=1.9,
                   mass=6.0, stride=6, lift=4, duty=0.62, flex=0.7, angle0=0.35)
CAT = dict(
    gait="walk", bob=0.5, hip=(41, 68), shoulder=(72, 70), far=(2.5, -1.2),
    back=[(32, 70), (34, 64), (40, 61), (50, 62.5), (60, 63), (68, 62), (75, 61)],
    belly=[(32, 72), (36, 77), (46, 79.5), (58, 79), (68, 77), (77, 75), (81, 70), (80, 65)],
    neck=([(70, 62), (76, 59), (81, 56)], [(78, 75), (83, 69), (86, 63)]),
    head_pos=(81, 55), head_ang=0.05, nod=0.4,
    front=CAT_FRONT, hind=CAT_HIND, head=cat_head, behind_head=cat_ear_far, tail=cat_tail,
)


def persian_head(r, c):
    # cabeza redonda, cara chata y orejas pequeñas y bajas
    H(r, c, [(-1, 0), (3, -3), (8, -3.2), (11.5, -1), (12.6, 2.4), (12.4, 5.4), (10.6, 8), (7, 9.4), (2.6, 9.2),
             (-1.4, 7), (-2.8, 3)])
    H(r, c, [(1.6, -2.2), (2.6, -6.2), (6.0, -3.0)], smooth=False)
    HL(r, c, [(2.8, -3.0), (3.0, -4.8)], 2)
    HE(r, c, (7.6, 1.2), 2)
    x, y = HP(c, (11.8, 3.6))
    r.dot(x, y, 0)
    HL(r, c, [(11.8, 4.6), (11.0, 5.8), (9.8, 5.4)], 1)
    whiskers(r, c, (11.0, 5.0), 3, 6.0)


def persian_ruff(r, c):
    hx, hy = c["head"]
    r.layer("ruff", "head")
    pts = []
    for i in range(9):
        a = -0.4 + i * 0.36
        rr = 9.5 + (1.4 if i % 2 else 0)
        pts.append((hx + 3 + math.cos(a) * rr * 0.75, hy + 5 + math.sin(a) * rr))
    r.poly([(hx + 3, hy - 2)] + pts, smooth=True)
    for i in range(1, 8, 2):
        a = -0.4 + i * 0.36
        r.line([(hx + 3 + math.cos(a) * 6.5 * 0.75, hy + 5 + math.sin(a) * 6.5),
                (hx + 3 + math.cos(a) * 9.6 * 0.75, hy + 5 + math.sin(a) * 9.6)], 2)


def fur_fringe(x0, x1, y, n, depth=3.0):
    """Borde de pelo largo: dientes de sierra inclinados hacia atrás."""
    def f(r, c):
        dy = c["dy"]
        pts = [(x0, y + dy - 3)]
        for i in range(n):
            x = lerp(x0, x1, (i + 0.5) / n)
            sway = math.sin(TAU * c["u"] * 2 + i) * 0.4
            pts += [(x + 1.6, y + dy - 1), (x - 1.0 + sway, y + dy + depth * (0.7 + 0.3 * (i % 2)))]
        pts.append((x1, y + dy - 3))
        r.poly(pts)
    return f


def persian_tail(r, c):
    u, dy = c["u"], c["dy"]
    w = math.sin(TAU * u)
    pts = [(30, 66 + dy), (24, 66 + dy), (18, 64 + dy + w), (13, 60 + dy + w * 1.6)]
    r.stroke(pts, 4.6, 4.2)
    for k in range(3):
        a = lerp2(pts[1], pts[3], k / 2.5)
        r.line([(a[0] + 1.5, a[1] - 2.0), (a[0] - 1.0, a[1] + 0.4)], 2)


PERSIAN = dict(
    gait="walk", bob=0.4, hip=(40, 68), shoulder=(70, 70), far=(2.5, -1.2),
    back=[(29, 70), (31, 63), (38, 59), (49, 60), (60, 60.5), (68, 59), (75, 58)],
    belly=[(29, 72), (33, 78), (44, 81), (56, 81), (67, 79.5), (76, 77), (81, 71), (80, 64)],
    neck=([(70, 60), (75, 57), (79, 55)], [(78, 76), (82, 70), (85, 64)]),
    head_pos=(80, 52), head_ang=0.0, nod=0.35,
    front=LegSpec(upper=8, mid=10, low=6, r_up=3.6, r_mid=2.4, r_low=2.0, foot="paw", foot_r=2.2,
                  mass=4.6, stride=5, lift=4, duty=0.62, flex=0.9, angle0=0.0),
    hind=LegSpec(upper=10, mid=11, low=8, r_up=4.4, r_mid=2.6, r_low=2.0, foot="paw", foot_r=2.2,
                 mass=6.4, stride=5, lift=3.5, duty=0.62, flex=0.7, angle0=0.35),
    head=persian_head, behind_head=persian_ruff, tail=persian_tail,
    body_extra=fur_fringe(34, 76, 80.0, 11, 2.2),
)


def lion_head(r, c):
    H(r, c, [(0, 0), (4, -1.8), (9, -1.2), (13, 0.6), (16, 2.6), (17.4, 4.8), (17.2, 7.2), (15.6, 9.4),
             (12, 10.8), (7, 11.2), (3, 10.2), (0, 8), (-1.6, 4)])
    H(r, c, [(0.2, -0.6), (0.6, -3.8), (2.8, -4.8), (4.6, -2.6), (4.0, -0.4)])   # oreja redonda
    HL(r, c, [(1.6, -1.2), (2.6, -3.0)], 2)
    HE(r, c, (8.6, 1.6), 2)
    HL(r, c, [(10.4, 2.2), (12.6, 2.4)], 2)
    x, y = HP(c, (16.6, 4.4))
    r.dot(x, y, 0)
    r.dot(x - 1, y, 0)
    HL(r, c, [(16.4, 5.6), (16.0, 7.6), (13.2, 8.8)], 1)
    HL(r, c, [(13.6, 4.2), (14.2, 7.0)], 2)
    HL(r, c, [(11.0, 10.6), (6.0, 8.4), (2.4, 7.6)], 2, smooth=True)
    whiskers(r, c, (15.0, 7.0), 3, 6.0)


def lion_tail(r, c):
    u, dy = c["u"], c["dy"]
    w = math.sin(TAU * u)
    pts = [(21, 59 + dy), (15.5, 63 + dy), (12.5 + w * 0.6, 72 + dy), (13 + w * 1.4, 81 + dy), (17 + w * 2, 85 + dy)]
    r.stroke(pts, 2.2, 1.4)
    r.layer("tuft", "tuft", tone=-2)
    tip = pts[-1]
    r.ellipse(tip[0] + 1.6, tip[1] + 0.4, 2.6, 2.0, 0.4)


LIONESS = dict(
    gait="walk", bob=0.7, hip=(34, 62), shoulder=(75, 64), far=(3.0, -1.5),
    back=[(20, 64), (23, 57), (30, 53.5), (42, 55), (55, 56), (66, 53), (74, 50), (81, 51)],
    belly=[(20, 66), (25, 72), (36, 76), (50, 78.5), (62, 77.5), (72, 75), (82, 71), (86, 63)],
    neck=([(74, 52), (82, 54), (88, 56)], [(84, 72), (90, 69), (94, 66)]),
    head_pos=(89, 55), head_ang=0.0, nod=0.5,
    front=LegSpec(upper=12, mid=14, low=9, r_up=5.0, r_mid=3.0, r_low=2.4, foot="bigpaw", foot_r=2.4,
                  mass=6.6, stride=7, lift=5, duty=0.62, flex=0.9, angle0=0.0),
    hind=LegSpec(upper=14, mid=14, low=12, r_up=5.6, r_mid=2.9, r_low=2.2, foot="bigpaw", foot_r=2.3,
                 mass=8.0, stride=7, lift=4.5, duty=0.62, flex=0.7, angle0=0.35),
    head=lion_head, tail=lion_tail,
    body_extra=lambda r, c: r.line(catmull([(40, 74.5 + c["dy"]), (52, 77 + c["dy"]), (64, 75.5 + c["dy"])],
                                           False, 3), 2),
)


# ================================================================= cánidos

def jrt_head(r, c):
    H(r, c, [(0, 0), (4, -2), (8, -1.6), (10.5, 0), (14, 0.8), (17.5, 1.8), (18.6, 3.6), (17.8, 5.6),
             (14.4, 7.4), (10, 8.2), (6, 8.4), (2, 7.2), (-1, 4.2), (-1, 1)])
    # oreja doblada en V
    H(r, c, [(1.2, -1.4), (5.6, -3.6), (7.6, -1.6), (6.4, 2.4), (3.2, 0.8)])
    HL(r, c, [(5.8, -2.6), (6.0, 1.4)], 2)
    r.tint(tr([(0.4, -2.4), (8.4, -3.4), (10.0, 1.6), (6.8, 4.4), (1.2, 4.0)], *c["head"], c["ang"]), -2)
    HE(r, c, (8.8, 1.0), 2)
    x, y = HP(c, (17.9, 2.4))
    r.eye(x, y, 2)
    HL(r, c, [(17.6, 5.4), (14.0, 6.4), (11.6, 5.6)], 1)
    HL(r, c, [(10.6, 0.4), (12.0, 2.6)], 2)


def jrt_tail(r, c):
    u, dy = c["u"], c["dy"]
    wag = math.sin(TAU * u * 4) * 1.6
    r.stroke([(35, 61 + dy), (32.6 + wag * 0.4, 55 + dy), (33 + wag, 49.5 + dy)], 2.3, 1.6)


def jrt_patches(r, c):
    dy = c["dy"]
    r.tint([(32, 60 + dy), (40, 57.5 + dy), (44, 62 + dy), (39, 67 + dy), (33, 66 + dy)], -2)


JRT = dict(
    gait="trot", bob=1.0, hip=(42, 66), shoulder=(73, 68), far=(2.5, -1.2),
    back=[(32, 66), (35, 60), (42, 58), (52, 59.5), (62, 60), (70, 58), (76, 56)],
    belly=[(32, 68), (36, 74), (46, 76), (58, 76.5), (68, 77), (77, 76), (82, 71), (82, 64)],
    neck=([(72, 58), (78, 52), (82, 47)], [(80, 72), (85, 64), (88, 57)]),
    head_pos=(82, 46), head_ang=0.05, nod=0.6,
    front=LegSpec(upper=9, mid=11, low=8, r_up=3.4, r_mid=2.0, r_low=1.6, foot="paw", foot_r=1.9,
                  mass=4.4, stride=6, lift=6, duty=0.5, flex=0.95, angle0=0.0),
    hind=LegSpec(upper=10, mid=11, low=9, r_up=4.0, r_mid=2.0, r_low=1.6, foot="paw", foot_r=1.9,
                 mass=6.0, stride=6, lift=5, duty=0.5, flex=0.75, angle0=0.3),
    head=jrt_head, tail=jrt_tail, body_extra=jrt_patches,
)


def fox_head(r, c):
    H(r, c, [(0, 0), (4, -2), (8, -1.4), (11, 0.4), (15, 2.0), (19, 3.4), (21, 4.4), (20.4, 5.8), (17, 6.6),
             (13, 7.6), (8, 8.6), (3, 8.4), (-0.6, 5), (-1, 1.4)])
    H(r, c, [(1.0, -0.8), (3.0, -9.6), (7.0, -1.6)], smooth=False)
    HL(r, c, [(2.8, -1.8), (3.4, -6.8), (5.6, -2.0)], 2)
    r.tint(tr([(3, 5.6), (12, 6.4), (14, 7.6), (8, 9.4), (1, 8.8)], *c["head"], c["ang"]), +1)
    HE(r, c, (9.0, 0.8), 2)
    HL(r, c, [(10.8, 1.4), (12.6, 2.2)], 1)
    x, y = HP(c, (20.4, 4.2))
    r.eye(x, y, 2)
    HL(r, c, [(19.6, 5.8), (14.4, 6.4)], 1)
    whiskers(r, c, (18.6, 5.2), 2, 5.0)


def fox_ear_far(r, c):
    r.layer("ear_far", "ear_far", tone=-2)
    H(r, c, [(-2.0, 0.2), (-1.2, -8.8), (2.4, -1.4)], smooth=False)


def fox_tail(r, c):
    u, dy = c["u"], c["dy"]
    w = math.sin(TAU * u)
    pts = [(32, 60 + dy), (25, 62 + dy + w * 0.5), (17, 63 + dy + w), (9, 61 + dy + w * 1.8)]
    sm = catmull(pts, False, 4)
    n = len(sm) - 1
    for i in range(n):
        t = i / n
        rad = 2.6 + 3.2 * math.sin(min(1.0, t * 1.25) * math.pi * 0.75)
        r.capsule(sm[i], sm[i + 1], rad, rad)
    tip = pts[-1]
    r.tint([(tip[0] - 4.5, tip[1] - 4), (tip[0] + 3.5, tip[1] - 4.5), (tip[0] + 3.5, tip[1] + 4.5),
            (tip[0] - 4.5, tip[1] + 4)], +2)
    for k in range(3):
        a = lerp2(pts[1], pts[3], 0.2 + k * 0.28)
        r.line([(a[0] + 2, a[1] - 2.5), (a[0] - 1, a[1] + 0.5)], 2)


def fox_socks(r, c):
    for key, leg in (("near_hind", c["hl"]), ("near_front", c["fl"])):
        j = leg.get("hock") or leg.get("knee")
        f = leg["foot"]
        r.tint([(j[0] - 3, j[1] - 1), (j[0] + 3, j[1] - 1), (f[0] + 4, f[1] + 1), (f[0] - 3, f[1] + 1)], -2,
               smooth=False, layer=key)


def fox_chest(r, c):
    dy = c["dy"]
    r.tint([(78, 58 + dy), (86, 56 + dy), (88, 62 + dy), (82, 70 + dy), (77, 66 + dy)], +1)


FOX = dict(
    gait="trot", bob=0.9, hip=(41, 63), shoulder=(72, 65), far=(2.8, -1.4),
    back=[(30, 64), (33, 58), (40, 56), (50, 57.5), (60, 58), (68, 56), (75, 55)],
    belly=[(30, 66), (35, 72), (45, 74), (57, 74.5), (67, 73.5), (76, 72), (81, 67), (81, 61)],
    neck=([(70, 57), (77, 52), (81, 49)], [(79, 70), (85, 63), (89, 57)]),
    head_pos=(81, 48), head_ang=0.06, nod=0.6,
    front=LegSpec(upper=10, mid=12, low=9, r_up=3.2, r_mid=1.7, r_low=1.3, foot="paw", foot_r=1.8,
                  mass=4.2, stride=7, lift=6, duty=0.5, flex=0.95, angle0=0.0),
    hind=LegSpec(upper=12, mid=12, low=11, r_up=4.0, r_mid=1.9, r_low=1.3, foot="paw", foot_r=1.8,
                 mass=5.8, stride=7, lift=5, duty=0.5, flex=0.75, angle0=0.3),
    head=fox_head, behind_head=fox_ear_far, tail=fox_tail, leg_extra=fox_socks, body_extra=fox_chest,
)


# ================================================================= cabra, jabalí, rinoceronte, oso

GOAT_HORN = [(1.2, -0.6), (-0.8, -4.8), (-4.6, -7.8), (-8.6, -7.6), (-10.6, -5.0), (-10.4, -2.6)]


def goat_horn_far(r, c):
    r.layer("horn_far", "horn_far", tone=-2)
    r.stroke(tr(GOAT_HORN, c["head"][0] + 2.6, c["head"][1] - 0.6, c["ang"]), 1.6, 0.7)


def goat_head(r, c):
    H(r, c, [(0, 0), (4, -0.8), (8, 0.4), (12, 2.4), (15, 4.6), (16, 6.4), (15, 8), (12.4, 8.4), (8, 7.6),
             (4, 7.6), (0.4, 6.4), (-1.8, 3.4), (-1.6, 0.6)])
    H(r, c, [(7.6, 7.4), (12.2, 8.0), (11.0, 13.0), (9.4, 14.6), (8.4, 11.0)])           # barba
    HL(r, c, [(9.8, 8.6), (9.8, 12.6)], 2)
    H(r, c, [(-0.6, 1.6), (-6.4, 0.6), (-8.4, 2.4), (-5.4, 3.8), (-0.4, 3.4)])            # oreja caída
    HL(r, c, [(-1.4, 2.4), (-6.0, 1.8)], 2)
    x, y = HP(c, (5.4, 2.0))
    r.eye(x, y, 1)
    r.eye(x + 1, y, 1)
    HL(r, c, [(4.4, 1.0), (7.2, 1.0)], 2)
    x, y = HP(c, (15.0, 5.4))
    r.dot(x, y, 1)
    HL(r, c, [(15.4, 7.6), (12.8, 7.8)], 1)
    r.layer("horn", "horn")
    r.stroke(tr(GOAT_HORN, c["head"][0], c["head"][1], c["ang"]), 1.8, 0.8)
    for t in (0.2, 0.4, 0.6):
        p = catmull(GOAT_HORN, False, 4)[int(t * 20)]
        HL(r, c, [(p[0] - 0.8, p[1] - 0.8), (p[0] + 0.8, p[1] + 0.8)], 2)


def goat_tail(r, c):
    dy = c["dy"]
    w = math.sin(TAU * c["u"] * 2)
    r.stroke([(31.5, 56 + dy), (29.4, 52 + dy), (29.2 + w * 0.5, 48.6 + dy)], 1.9, 1.0)


GOAT = dict(
    gait="walk", bob=0.6, hip=(40, 61), shoulder=(73, 63), far=(2.8, -1.4),
    back=[(30, 62), (32, 55.5), (38, 53), (48, 55), (60, 55.5), (70, 53), (77, 51)],
    belly=[(30, 64), (34, 70), (44, 73.5), (56, 74.5), (68, 73), (77, 71), (81, 66), (82, 58)],
    neck=([(72, 54), (79, 47), (83, 42)], [(80, 68), (85, 58), (89, 50)]),
    head_pos=(83, 41), head_ang=0.35, nod=0.6,
    front=LegSpec(upper=10, mid=13, low=12, r_up=3.4, r_mid=1.8, r_low=1.4, foot="cloven", foot_r=1.6,
                  mass=4.6, stride=6, lift=6, duty=0.6, flex=0.95),
    hind=LegSpec(upper=12, mid=14, low=14, r_up=4.0, r_mid=1.9, r_low=1.4, foot="cloven", foot_r=1.6,
                 mass=5.8, stride=6, lift=5, duty=0.6, flex=0.75, angle0=0.14),
    head=goat_head, behind_head=goat_horn_far, tail=goat_tail,
    body_extra=lambda r, c: r.line([(38, 54 + c["dy"]), (40, 58 + c["dy"])], 2),
)


def boar_head(r, c):
    H(r, c, [(0, 0), (5, 1), (10, 3.4), (15, 6.4), (19, 8.6), (21.6, 9.6), (22.6, 12), (22.0, 14.6), (19, 14.4),
             (15, 14.8), (10, 16.2), (5, 17), (0, 16.4), (-2.6, 11), (-2.4, 4)])
    H(r, c, [(-0.4, 0.8), (-1.6, -5.2), (1.0, -7.6), (3.4, -2.2), (2.6, 1.2)])            # oreja
    HL(r, c, [(0.4, -1.0), (0.8, -5.0)], 2)
    HL(r, c, [(21.4, 10.2), (21.6, 14.0)], 2)                                            # disco del hocico
    x, y = HP(c, (22.0, 11.6))
    r.dot(x, y, 0)
    r.dot(x, y + 1.6, 0)
    HE(r, c, (8.4, 5.2), 2)
    HL(r, c, [(6.8, 4.2), (10.4, 4.4)], 1)
    HL(r, c, [(19.4, 14.0), (13, 15.6)], 1)
    HL(r, c, [(13, 7.6), (16.4, 10.4)], 2)
    HL(r, c, [(4, 13.6), (9, 12.0), (12, 14.6)], 2, smooth=True)
    r.layer("tusk", "tusk", tone=+1)
    r.stroke(tr([(16.8, 14.0), (19.2, 12.6), (19.6, 10.2)], *c["head"], c["ang"]), 1.0, 0.6)


def boar_bristles(r, c):
    dy = c["dy"]
    r.layer("bristle", "bristle", tone=-1)
    pts = catmull([(82, 54), (74, 51.5), (64, 52), (54, 55), (44, 58.5)], False, 3)
    for i, (x, y) in enumerate(pts):
        h = 4.8 * (1 - i / len(pts)) + 1.4
        sway = math.sin(TAU * c["u"] * 2 + i * 0.7) * 0.4
        r.poly([(x + 1.6, y + dy + 1.6), (x - 1.6, y + dy + 1.6), (x - 2.0 + sway, y + dy - h)])


def boar_tail(r, c):
    dy = c["dy"]
    w = math.sin(TAU * c["u"] * 2) * 1.2
    pts = [(27.5, 70 + dy), (24.4, 71.5 + dy), (22.4 + w * 0.4, 75.5 + dy), (22.8 + w, 80 + dy)]
    r.stroke(pts, 1.0, 0.7)
    r.ellipse(pts[-1][0], pts[-1][1] + 1.0, 1.3, 2.0)


BOAR = dict(
    gait="trot", bob=0.8, hip=(38, 73), shoulder=(70, 74), far=(2.8, -1.2),
    back=[(26, 77), (28, 70), (34, 65), (44, 61.5), (56, 57.5), (66, 55), (74, 55), (80, 58)],
    belly=[(26, 79), (30, 85), (40, 87), (52, 87.5), (64, 87), (74, 85), (80, 79), (83, 69)],
    head_pos=(77, 59), head_ang=0.0, nod=0.5,
    front=LegSpec(upper=8, mid=9, low=7, r_up=4.0, r_mid=2.2, r_low=1.5, foot="cloven", foot_r=1.7,
                  mass=5.6, stride=5, lift=4.5, duty=0.5, flex=0.95),
    hind=LegSpec(upper=9, mid=10, low=9, r_up=4.6, r_mid=2.2, r_low=1.5, foot="cloven", foot_r=1.7,
                 mass=7.0, stride=5, lift=4, duty=0.5, flex=0.75, angle0=0.14),
    head=boar_head, tail=boar_tail, body_extra=boar_bristles,
)


def rhino_head(r, c):
    H(r, c, [(0, 0), (5, 1.4), (10, 3.6), (15, 6.2), (20, 8.4), (23.6, 10), (25, 12.6), (24.4, 15), (22, 16.4),
             (18, 16.4), (13, 17), (7, 17.4), (2, 15.6), (-1.6, 11), (-2, 5)])
    H(r, c, [(0.4, 0.6), (-0.8, -5.0), (1.0, -7.6), (3.6, -5.8), (3.2, 0.8)])            # oreja tubular
    HL(r, c, [(1.2, -1.0), (1.4, -5.2)], 2)
    HE(r, c, (11.0, 8.4), 2)
    HL(r, c, [(9.4, 7.0), (12.8, 7.6)], 1)
    HL(r, c, [(24.6, 14.2), (20, 15.6)], 1)
    x, y = HP(c, (23.4, 12.2))
    r.dot(x, y, 1)
    HL(r, c, [(4, 6), (6, 11), (4.6, 15)], 2, smooth=True)
    r.layer("horn", "horn")
    H(r, c, [(18.4, 8.8), (23.6, 10.6), (24.8, 2.0), (24.0, -4.4), (21.4, 1.4)])             # cuerno frontal
    H(r, c, [(12.4, 5.4), (16.8, 7.4), (15.2, 0.6)])                                          # cuerno trasero
    HL(r, c, [(22.4, 6.8), (23.6, 3.0)], 3)


def rhino_folds(r, c):
    dy = c["dy"]
    for pts in ([(66, 55), (63.5, 68), (65, 82)], [(40, 56), (42.5, 70), (40, 83)], [(80, 57), (78, 66), (80, 78)],
                [(52, 57), (53, 60)], [(30, 60), (34, 63)]):
        r.line(catmull([(x, y + dy) for x, y in pts], False, 3) if len(pts) > 2 else [(x, y + dy) for x, y in pts], 2)


def rhino_tail(r, c):
    dy = c["dy"]
    w = math.sin(TAU * c["u"] * 2) * 0.8
    pts = [(21, 64 + dy), (17.6, 67 + dy), (16.4 + w, 74 + dy)]
    r.stroke(pts, 1.4, 0.9)
    r.ellipse(pts[-1][0], pts[-1][1] + 1.6, 1.3, 2.2)


RHINO = dict(
    gait="walk", bob=0.6, hip=(36, 67), shoulder=(72, 69), far=(3.0, -1.4),
    back=[(20, 70), (23, 62), (30, 56.5), (42, 55), (54, 56), (62, 54.5), (70, 50.5), (78, 52)],
    belly=[(20, 72), (25, 80), (36, 84), (50, 85.5), (62, 85), (72, 83), (80, 78), (84, 68)],
    neck=([(76, 52), (83, 55), (88, 58)], [(82, 80), (88, 77), (92, 73)]),
    head_pos=(87, 57), head_ang=0.0, nod=0.5,
    front=LegSpec(upper=9, mid=10, low=7, r_up=5.6, r_mid=4.2, r_low=3.6, foot="stump", foot_r=3.6,
                  mass=7.6, stride=5, lift=3.5, duty=0.62, flex=0.8),
    hind=LegSpec(upper=11, mid=10, low=8, r_up=6.4, r_mid=4.2, r_low=3.6, foot="stump", foot_r=3.6,
                 mass=9.0, stride=5, lift=3, duty=0.62, flex=0.6, angle0=0.1),
    head=rhino_head, tail=rhino_tail, body_extra=rhino_folds,
)


def bear_head(r, c):
    H(r, c, [(0, 0), (4, -1.4), (8, -1), (12, 0.4), (16, 2.2), (19.4, 3.4), (20.6, 5), (19.8, 6.8), (16.6, 7.8),
             (12, 8.8), (7, 9.6), (2.6, 9.4), (-0.8, 7), (-1.6, 3)])
    H(r, c, [(0.0, -0.4), (0.4, -3.2), (2.6, -4.0), (4.0, -1.8), (3.4, -0.2)])
    HL(r, c, [(1.6, -1.0), (2.2, -2.6)], 2)
    HE(r, c, (8.6, 1.4), 2)
    x, y = HP(c, (19.6, 4.0))
    r.eye(x, y, 2)
    HL(r, c, [(19.4, 6.4), (15.0, 7.2)], 1)
    HL(r, c, [(11.0, 2.0), (13.4, 3.4)], 2)


def bear_fur(r, c):
    dy = c["dy"]
    fur_fringe(32, 74, 80.5, 10, 2.4)(r, c)
    r.line(catmull([(68, 60 + dy), (72, 68 + dy), (70, 76 + dy)], False, 3), 2)


POLAR_BEAR = dict(
    gait="walk", bob=0.7, hip=(36, 63), shoulder=(71, 67), far=(3.0, -1.4),
    back=[(22, 67), (25, 59), (32, 54), (44, 55), (56, 57.5), (66, 58.5), (74, 58.5)],
    belly=[(22, 69), (27, 77), (38, 80.5), (52, 81.5), (64, 80.5), (73, 78), (80, 74), (82, 66)],
    neck=([(72, 59), (80, 60.5), (86, 62)], [(80, 76), (87, 74), (92, 71)]),
    head_pos=(86, 61), head_ang=0.08, nod=0.7,
    front=LegSpec(upper=11, mid=12, low=8, r_up=5.2, r_mid=3.6, r_low=3.0, foot="bigpaw", foot_r=3.0,
                  mass=6.6, stride=6, lift=4.5, duty=0.62, flex=0.85),
    hind=LegSpec(upper=12, mid=12, low=10, r_up=6.0, r_mid=3.6, r_low=3.0, foot="bigpaw", foot_r=3.0,
                 mass=8.4, stride=6, lift=4, duty=0.62, flex=0.6, angle0=0.2),
    head=bear_head, body_extra=bear_fur,
    tail=lambda r, c: r.ellipse(23, 60 + c["dy"], 2.6, 2.2),
)


def hedgehog(r, u):
    dy = 0.5 * math.cos(TAU * u * 2)
    cx, cy = 56, 82 + dy
    leg = LegSpec(upper=3.5, mid=4, low=3, r_up=2.2, r_mid=1.6, r_low=1.3, foot="paw", foot_r=1.5,
                  mass=2.6, stride=3.5, lift=2.6, duty=0.6, flex=0.8, angle0=0.2)
    from patronus_core import front_leg, hind_leg
    hind_leg(r, (44, 90 + dy), u + 0.5, leg, GROUND - 1, "far_hind", -2)
    front_leg(r, (69, 90 + dy), u + 0.75, leg, GROUND - 1, "far_front", -2)
    r.layer("body", "body")
    r.ellipse(cx, cy, 24, 13)
    # cabeza puntiaguda
    hx = 78
    r.poly([(hx - 4, cy - 6), (hx + 6, cy - 1), (hx + 14, cy + 4.2), (hx + 15.2, cy + 6.0), (hx + 13, cy + 7.6),
            (hx + 4, cy + 9.6), (hx - 6, cy + 10)], smooth=True)
    r.eye(hx + 4, cy + 1.2, 2)
    r.eye(hx + 14.0, cy + 4.6, 2)
    r.line([(hx + 13, cy + 7.0), (hx + 9, cy + 7.4)], 1)
    whisk = HP(dict(head=(hx + 12, cy + 6), ang=0.1), (0, 0))
    r.wisp([whisk, (whisk[0] + 6, whisk[1] + 0.6)], 140)
    r.wisp([whisk, (whisk[0] + 5.5, whisk[1] + 2.4)], 140)
    hind_leg(r, (42, 91 + dy), u, leg, GROUND, "near_hind")
    front_leg(r, (68, 91 + dy), u + 0.25, leg, GROUND, "near_front")
    # oreja
    r.layer("ear", "ear")
    r.ellipse(hx - 1.0, cy - 2.6, 2.2, 2.4)
    # púas: filas de espinas apuntando hacia atrás, sobre el lomo
    r.layer("spines", "spines")
    rows = [(26, 15.5, 9), (22, 12.5, 8), (17, 9, 7)]
    for k, (rx, ry, L) in enumerate(rows):
        n = 16 - k * 2
        for i in range(n):
            a = math.pi * (1.02 + 0.98 * i / (n - 1)) + (0.06 if k % 2 else 0)
            px, py = cx - 2 + rx * math.cos(a), cy + 2 + ry * math.sin(a)
            if px > hx + 2:
                continue
            ang = a - 0.75  # inclinadas hacia atrás
            tip = (px + L * math.cos(ang) * 0.75, py + L * math.sin(ang) * 0.75)
            base_w = 1.8
            nx, ny = -math.sin(a), math.cos(a)
            r.poly([(px + nx * base_w, py + ny * base_w), (px - nx * base_w, py - ny * base_w), tip])
            r.line([(px, py), lerp2((px, py), tip, 0.7)], 2)
    r.layer("face", "face", sep=False)
    r.poly([(hx - 3, cy - 4), (hx + 6, cy - 0.6), (hx + 6, cy + 9), (hx - 4, cy + 9.6)], smooth=True)


# ================================================================= cocodrilo

def crocodile(r, u):
    from patronus_core import front_leg, hind_leg
    t = TAU * u

    def wave(x):
        k = max(0.0, 1 - x / 42)
        return math.sin(t - x * 0.12) * 1.6 * k

    prof = [(5, 88.6, 89.6), (14, 86.6, 90.6), (24, 84.2, 91.8), (34, 81.6, 93.0), (42, 80.2, 94.0),
            (52, 79.6, 95.0), (62, 80.0, 94.6), (70, 81.0, 93.4), (78, 82.4, 92.2)]
    top = [(x, a + wave(x)) for x, a, b in prof]
    bot = [(x, b + wave(x)) for x, a, b in prof]
    leg = LegSpec(upper=6, mid=6.5, low=3, r_up=3.0, r_mid=2.3, r_low=1.9, foot="paw", foot_r=2.4,
                  mass=3.6, stride=5, lift=3.0, duty=0.6, flex=0.6, angle0=0.3)
    fleg = LegSpec(upper=5.5, mid=6, low=3, r_up=2.6, r_mid=2.0, r_low=1.7, foot="paw", foot_r=2.1,
                   mass=3.0, stride=5, lift=3.0, duty=0.6, flex=0.7, angle0=0.0)
    hind_leg(r, (44, 89), u + 0.5, leg, GROUND - 1.2, "far_hind", -2)
    front_leg(r, (69, 89), u, fleg, GROUND - 1.2, "far_front", -2)
    r.layer("body", "body")
    r.band(top, bot)
    # cresta: doble fila de escamas en la cola y placas en el lomo
    r.layer("scutes", "scutes", tone=-1)
    for i in range(16):
        x = 8 + i * 4.0
        y = lerp(88.6, 80.0, min(1, x / 52)) + wave(x)
        h = 2.6 if x < 36 else 1.6
        r.poly([(x - 1.8, y + 1.0), (x + 1.8, y + 1.0), (x - 0.4, y - h)])
    r.layer("body2", "body")
    for x in range(40, 74, 5):
        y = 80.0 + wave(x)
        r.dot(x, y + 3.0, 2)
        r.dot(x + 2, y + 6.0, 2)
        r.line([(x, y + 8.5), (x + 1, y + 11)], 2)
    hind_leg(r, (44, 90), u, leg, GROUND, "near_hind")
    front_leg(r, (69, 90), u + 0.5, fleg, GROUND, "near_front")
    # cabeza larga con la boca dentada
    r.layer("head", "head")
    c = dict(head=(77, 82.6), ang=0.02 * math.sin(t))
    H(r, c, [(0, -0.4), (4, -2.4), (7, -4.4), (9.6, -3.8), (12, -1.6), (20, -0.8), (28, 0.2), (31.4, 0.6),
             (32.6, 2.6), (31.6, 5.0), (24, 6.0), (14, 7.6), (6, 9.0), (0, 9.6)])
    HE(r, c, (7.4, -2.6), 2)
    HL(r, c, [(5.0, -3.6), (9.4, -3.2)], 1)
    x, y = HP(c, (30.6, 0.8))
    r.dot(x, y, 1)
    HL(r, c, [(31.4, 3.0), (24, 3.8), (16, 4.6), (8, 5.6), (3, 6.4)], 1)
    for k in range(9):
        px = 29 - k * 3.0
        py = 3.0 + k * 0.38
        x, y = HP(c, (px, py + (0.9 if k % 2 else -0.9)))
        r.dot(x, y, 6)
    HL(r, c, [(2.0, 1.0), (6, 2.4)], 2)


# ================================================================= mustélidos y liebre

def otter(r, u):
    t = TAU * u
    n = 12
    spine = []
    for i in range(n + 1):
        x = 16 + i * 6.2
        spine.append((x, 64 + math.sin(t - i * 0.6) * 4.6 * (1.15 - i / n)))
    radii = [1.2, 2.0, 3.0, 4.2, 5.6, 6.8, 7.4, 7.6, 7.6, 7.2, 6.6, 5.8, 5.2]
    tang = math.atan2(spine[-1][1] - spine[-3][1], spine[-1][0] - spine[-3][0])
    hx, hy = spine[-1]
    c = dict(head=(hx - 1, hy), ang=tang * 0.6)
    # patas lejanas
    r.layer("far_front", "far_front", tone=-2)
    sh = spine[9]
    a = 0.4 + math.sin(t + 1.2) * 0.6
    r.capsule((sh[0], sh[1] + 4), (sh[0] - 3 - math.cos(a) * 3, sh[1] + 10 + math.sin(a) * 2), 2.2, 1.6)
    r.layer("far_hind", "far_hind", tone=-2)
    hp = spine[4]
    a = math.sin(t + math.pi) * 0.5
    foot = (hp[0] - 9, hp[1] + 6 + a * 4)
    r.capsule((hp[0], hp[1] + 3), foot, 2.8, 2.0)
    r.ellipse(foot[0] - 2.4, foot[1], 3.4, 1.6, 0.3 + a * 0.4)
    r.layer("body", "body")
    for i in range(n):
        r.capsule(spine[i], spine[i + 1], radii[i], radii[i + 1])
    # vientre más claro
    r.tint([(spine[5][0], spine[5][1] + 5), (spine[11][0] + 2, spine[11][1] + 3),
            (spine[11][0] + 2, spine[11][1] + 8), (spine[5][0], spine[5][1] + 9)], +1, smooth=False)
    r.line(catmull([(spine[i][0], spine[i][1] + radii[i] * 0.55) for i in range(5, 12)], False, 3), 2)
    # patas cercanas
    r.layer("near_hind", "near_hind")
    a = math.sin(t) * 0.5
    foot = (hp[0] - 10, hp[1] + 7 + a * 4)
    r.capsule((hp[0] + 1, hp[1] + 3), foot, 3.2, 2.2)
    r.ellipse(foot[0] - 2.8, foot[1], 4.0, 1.8, 0.3 + a * 0.4)
    r.line([(foot[0] - 5, foot[1] - 0.6), (foot[0] - 1.5, foot[1])], 2)
    r.layer("near_front", "near_front")
    a = 0.4 + math.sin(t + 1.2 + math.pi) * 0.6
    r.capsule((sh[0] + 1, sh[1] + 4), (sh[0] - 2 - math.cos(a) * 3, sh[1] + 11 + math.sin(a) * 2), 2.4, 1.8)
    # cabeza ancha y plana
    r.layer("head", "head")
    H(r, c, [(-1, -6.6), (5, -7.2), (10, -5.6), (14, -3.2), (15.8, -0.8), (15.2, 1.8), (12, 3.6), (6, 5.0),
             (0, 6.0), (-3, 1)])
    r.circle(HP(c, (3.0, -6.8)), 2.0)
    HL(r, c, [(2.4, -7.0), (3.6, -6.0)], 2)
    HE(r, c, (8.4, -3.4), 2)
    x, y = HP(c, (14.6, -1.2))
    r.eye(x, y, 2)
    HL(r, c, [(14.4, 1.6), (10.0, 2.6)], 1)
    r.tint(tr([(4, 2.8), (12, 2.4), (12, 5.0), (3, 6.6)], *c["head"], c["ang"]), +1)
    whiskers(r, c, (13.0, 0.6), 3, 7.0, 0.2)


def mustelid(r, u, tail_len=8.0, tail_r=(1.8, 1.0), bushy=False, tone=0, size=1.0):
    """Comadreja / visón: carrera a saltos con el lomo arqueado."""
    from patronus_core import front_leg, hind_leg
    t = TAU * u
    L = (34 + 6 * math.cos(t)) * size
    arch = (3 + 5 * (1 - math.cos(t)) / 2) * size
    by = 80 - 3 * math.sin(t)
    cx = 56
    hip = (cx - L / 2, by)
    sh = (cx + L / 2, by + 0.5)
    mid = (cx, by - arch)
    leg = LegSpec(upper=7, mid=8, low=6, r_up=3.0, r_mid=1.8, r_low=1.4, foot="paw", foot_r=1.8, mass=3.8,
                  stride=6, lift=5, duty=0.4, flex=0.8, angle0=0.3)
    fleg = LegSpec(upper=6, mid=7, low=5, r_up=2.6, r_mid=1.6, r_low=1.3, foot="paw", foot_r=1.7, mass=3.0,
                   stride=6, lift=5, duty=0.4, flex=0.9, angle0=0.0)
    hind_leg(r, (hip[0] + 1.5, hip[1] + 2), u + 0.06, leg, GROUND - 1, "far_hind", -2 + tone)
    front_leg(r, (sh[0] + 1.5, sh[1] + 2), u + 0.56, fleg, GROUND - 1, "far_front", -2 + tone)
    r.layer("tail", "tail", tone=tone)
    sway = math.sin(t - 1.0) * 2.0
    rump = (hip[0] - 4, by + 0.5)
    tail = [rump, (rump[0] - tail_len * 0.5, rump[1] - 1.5 + sway * 0.4), (rump[0] - tail_len, rump[1] - 3 + sway)]
    r.stroke(tail, tail_r[0], tail_r[1])
    if bushy:
        for k in range(2):
            a = lerp2(tail[0], tail[2], 0.35 + k * 0.3)
            r.line([(a[0] + 1.6, a[1] - 1.8), (a[0] - 1.0, a[1] + 0.4)], 2)
    r.layer("body", "body", tone=tone)
    spine = [rump, hip, mid, sh, (sh[0] + 6, sh[1] - 3)]
    r.stroke(spine, 5.0 * size, 4.4 * size)
    r.line(catmull([(hip[0], hip[1] + 3.2), (mid[0], mid[1] + 4.4), (sh[0], sh[1] + 3.6)], False, 3), 2)
    hind_leg(r, (hip[0], hip[1] + 2.5), u, leg, GROUND, "near_hind", tone)
    front_leg(r, (sh[0], sh[1] + 2.5), u + 0.5, fleg, GROUND, "near_front", tone)
    r.layer("head", "head", tone=tone)
    c = dict(head=(sh[0] + 7, sh[1] - 4), ang=-0.1 + 0.1 * math.sin(t))
    H(r, c, [(-1, -3.8), (4, -4.6), (8.6, -3.0), (11.4, -0.4), (10.8, 1.6), (6.6, 3.0), (0.6, 3.6), (-2.4, 0)])
    r.circle(HP(c, (1.2, -4.2)), 1.8)
    HE(r, c, (6.2, -1.6), 2)
    x, y = HP(c, (10.8, -0.6))
    r.eye(x, y, 1)
    r.tint(tr([(2, 1.0), (10, 0.8), (8, 3.4), (1, 4.0)], *c["head"], c["ang"]), +2)
    whiskers(r, c, (9.6, 0.4), 2, 5.0)


def keyframes(u, keys):
    """Interpolación suave entre fotogramas clave [(u, valor), ...] cíclicos."""
    u %= 1.0
    for (u0, v0), (u1, v1) in zip(keys, keys[1:] + [(keys[0][0] + 1.0, keys[0][1])]):
        if u0 <= u < u1 or (u1 > 1.0 and u0 <= u):
            s = smooth_t = (u - u0) / (u1 - u0)
            s = smooth_t * smooth_t * (3 - 2 * smooth_t)
            if isinstance(v0, tuple):
                return tuple(lerp(a, b, s) for a, b in zip(v0, v1))
            return lerp(v0, v1, s)
    return keys[0][1]


def chain(start, segs):
    """Cadena de huesos: segs = [(longitud, ángulo en grados desde +x, y hacia abajo)]."""
    pts = [start]
    for L, a in segs:
        x, y = pts[-1]
        pts.append((x + L * math.cos(math.radians(a)), y + L * math.sin(math.radians(a))))
    return pts


def hare(r, u):
    # (cuerpo_y, cabeceo, [muslo, tibia, pie], [brazo, antebrazo])
    K = [(0.00, (78.0, 0.06, 70, 150, 5, 92, 96)),
         (0.22, (75.0, -0.22, 118, 128, 115, 50, 62)),
         (0.45, (65.0, -0.04, 135, 145, 140, 28, 38)),
         (0.70, (71.0, 0.24, 58, 118, 25, 95, 110))]
    keys = [(k, v) for k, v in K]
    by, pitch, a1, a2, a3, b1, b2 = keyframes(u, keys)
    cx = 54
    c0 = (cx, by)

    def P(p):
        return tr([p], c0[0], c0[1], pitch)[0]
    hip, sh = P((-8, 4)), P((9, 5))
    # patas lejanas
    r.layer("far_hind", "far_hind", tone=-2)
    pts = chain((hip[0] + 2.5, hip[1] - 1), [(11, a1 + 8), (12, a2 + 8), (9, a3 + 6)])
    r.ellipse(*lerp2(pts[0], pts[1], 0.4), 7, 5.0, math.radians(a1))
    r.stroke(pts[1:3], 2.4, 1.6, smooth=False)
    r.capsule(pts[2], pts[3], 1.8, 1.4)
    r.layer("far_front", "far_front", tone=-2)
    fp = chain((sh[0] + 2, sh[1] - 1), [(8, b1 + 10), (9, b2 + 10)])
    r.stroke(fp, 1.7, 1.2, smooth=False)
    r.ellipse(fp[-1][0] + 1, fp[-1][1], 1.8, 1.1)
    # cuerpo
    r.layer("body", "body")
    r.ellipse(*c0, 15, 9.5, pitch)
    r.ellipse(*P((-7, 0.5)), 10.5, 9.4, pitch)
    r.ellipse(*P((8, -1.5)), 8.0, 7.6, pitch)
    r.capsule(P((10, -3)), P((14, -8)), 5.4, 4.6)
    r.line(catmull([P((-2, 8.6)), P((6, 8.0)), P((12, 5.6))], False, 3), 2)
    # cola de algodón
    r.layer("tail", "tail", tone=+1)
    r.ellipse(*P((-16.5, -3.5)), 3.4, 3.2)
    # patas cercanas
    r.layer("near_hind", "near_hind")
    pts = chain(hip, [(11, a1), (12, a2), (9, a3)])
    r.ellipse(*lerp2(pts[0], pts[1], 0.4), 7.5, 5.4, math.radians(a1))
    r.stroke(pts[1:3], 2.6, 1.8, smooth=False)
    r.capsule(pts[2], pts[3], 2.0, 1.5)
    r.layer("near_front", "near_front")
    fp = chain(sh, [(8, b1), (9, b2)])
    r.stroke(fp, 2.0, 1.4, smooth=False)
    r.ellipse(fp[-1][0] + 1, fp[-1][1], 2.0, 1.2)
    # orejas largas con puntas oscuras
    head = P((17, -10))
    c = dict(head=head, ang=pitch * 0.7)
    sway = math.sin(TAU * u - 1.0) * 0.12
    for far, dx, base_ang in ((True, 2.6, -2.35), (False, 0.0, -1.9)):
        r.layer("ear_far" if far else "ear", "ear_far" if far else "ear", tone=-2 if far else 0)
        b = HP(c, (1.0 + dx, -3.0))
        ang = base_ang + pitch * 0.6 + sway
        tip = (b[0] + math.cos(ang) * 15, b[1] + math.sin(ang) * 15)
        mid = (b[0] + math.cos(ang + 0.08) * 8, b[1] + math.sin(ang + 0.08) * 8)
        r.stroke([b, mid, tip], 2.6, 1.7)
        r.tint([(tip[0] - 3, tip[1] - 3), (tip[0] + 3, tip[1] - 3), (tip[0] + 3, tip[1] + 3.5),
                (tip[0] - 3, tip[1] + 3.5)], -2, smooth=False)
        if not far:
            r.line([lerp2(b, tip, 0.2), lerp2(b, tip, 0.75)], 2)
    r.layer("head", "head")
    H(r, c, [(-1.6, -3.4), (3, -4.8), (7.6, -3.8), (10.8, -1.4), (12.2, 1.2), (11, 3.4), (7, 4.6), (2, 4.4),
             (-2.0, 1.6)])
    HE(r, c, (5.4, -1.8), 2)
    x, y = HP(c, (11.6, 0.4))
    r.dot(x, y, 0)
    HL(r, c, [(11.0, 2.0), (9.4, 3.2)], 1)
    whiskers(r, c, (10.4, 1.4), 3, 6.0)


# ================================================================= aves en vuelo

def wing_outline(n, slot, finger=0.12):
    """Contorno del ala en coordenadas (s: envergadura 0-1, c: cuerda 0-1 hacia atrás)."""
    pts = [(0, 0), (0.18, -0.07), (0.42, -0.10), (0.62, -0.06), (0.82, 0.03), (0.97, 0.14)]
    notches = []
    for i in range(n):
        f = i / max(1, n - 1)
        tip = (lerp(1.03, 0.56, f), lerp(0.30, 0.84, f) + finger * (1 - f))
        pts.append(tip)
        if i < n - 1:
            g = (i + 0.5) / max(1, n - 1)
            notch = (lerp(1.03, 0.56, g) - 0.03, lerp(0.30, 0.84, g) - slot + finger * (1 - g) * 0.5)
            pts.append(notch)
            notches.append(notch)
    for k in range(5):
        s = lerp(0.50, 0.04, k / 4)
        pts.append((s, 0.96 if k % 2 == 0 else 0.89))
    pts.append((0.0, 0.84))
    return pts, notches


def bird_wing(r, B, flap_t, far, body):
    t = flap_t
    flap = math.sin(t)
    up = max(0.0, math.cos(t))  # recuperación: el ala se pliega un poco
    beta = B.get("beta0", 0.3) + B.get("amp", 1.05) * flap
    L = B["span"] * (0.82 if far else 1.0) * (1 - 0.18 * up * B.get("fold", 1.0))
    C = B["chord"] * (0.92 if far else 1.0)
    cx, cy = body
    rx, ry = B["wing_root"]
    root = (cx + rx + (2.2 if far else 0.0), cy + ry - (1.0 if far else 0.0))
    sweep = B.get("sweep", 0.3) + 0.25 * up
    sv = (-sweep * L, -math.sin(beta) * L)
    cv = (-C, 0.18 * C)
    pts, notches = wing_outline(B.get("fingers", 5), B.get("slot", 0.12), B.get("finger", 0.12))

    def P(s, c):
        return (root[0] + s * sv[0] + c * cv[0], root[1] + s * sv[1] + c * cv[1])
    r.layer("far_wing" if far else "wing", "far_wing" if far else "wing", tone=-2 if far else 0)
    r.poly([P(s, c) for s, c in pts])
    if not far:
        # separación de primarias y fila de coberteras
        for s, c in notches:
            r.line([P(s, c), P(s - 0.08, c - 0.22)], 2)
        r.line(catmull([P(0.06, 0.46), P(0.3, 0.5), P(0.55, 0.42), P(0.75, 0.3)], False, 3), 2)
        for k in range(4):
            s = lerp(0.46, 0.1, k / 3)
            r.line([P(s, 0.62), P(s - 0.02, 0.86)], 2)
    return P


def flying_bird(r, u, B):
    t = TAU * u
    bob = math.cos(t) * B.get("bob", 1.6)
    body = (B["body"][0], B["body"][1] + bob)
    cx, cy = body
    ctx = dict(u=u, t=t, body=body, bob=bob)
    bird_wing(r, B, t, True, body)
    if B.get("tail"):
        r.layer("tail", "tail", tone=B.get("tail_tone", 0))
        B["tail"](r, ctx)
    r.layer("body", "body")
    B["body_fn"](r, ctx)
    r.layer("head", "head")
    B["head"](r, ctx)
    P = bird_wing(r, B, t, False, body)
    ctx["wingP"] = P
    if B.get("after"):
        B["after"](r, ctx)


def owl_body(r, c):
    cx, cy = c["body"]
    r.ellipse(cx, cy, 15, 10, -0.12)
    r.ellipse(cx + 6, cy + 1, 9, 9.5)
    # patas emplumadas colgando
    r.capsule((cx + 2, cy + 8), (cx - 1, cy + 14), 2.4, 2.0)
    for k in range(3):
        r.line([(cx - 2.6 + k * 1.4, cy + 15.2), (cx - 2.8 + k * 1.4, cy + 17)], 1)
    r.line(catmull([(cx - 8, cy + 2), (cx, cy + 6), (cx + 8, cy + 5)], False, 3), 2)


def owl_head(r, c):
    cx, cy = c["body"]
    hx, hy = cx + 15, cy - 8
    r.ellipse(hx, hy, 9.6, 9.0)
    # disco facial mirando al espectador
    r.tint([(hx - 7.6 + 7.6 * math.cos(a) * 0.98 + 7.6, hy + 0.8 + 7.0 * math.sin(a)) for a in
            [k * TAU / 16 for k in range(16)]], +1, smooth=False)
    r.line(catmull([(hx + 7.6 * math.cos(a), hy + 0.8 + 7.0 * math.sin(a)) for a in
                    [k * TAU / 14 for k in range(15)]], False, 2), 2)
    r.line([(hx, hy - 6), (hx, hy - 1)], 2)
    for ex in (-3.6, 2.4):
        r.eye(hx + ex, hy - 0.6, 2)
        r.line(catmull([(hx + ex - 1.6, hy - 1.8), (hx + ex + 0.8, hy - 2.8), (hx + ex + 2.8, hy - 1.4)], False, 2), 1)
    r.poly([(hx - 1.2, hy + 1.4), (hx + 1.2, hy + 1.4), (hx, hy + 4.6)])
    r.dot(hx, hy + 3.0, 1)


def owl_tail(r, c):
    cx, cy = c["body"]
    w = math.sin(c["t"] - 1) * 0.8
    r.poly([(cx - 10, cy - 4), (cx - 21, cy - 2 + w), (cx - 22.5, cy + 2 + w), (cx - 20, cy + 5.5 + w),
            (cx - 9, cy + 5)], smooth=True)
    r.line([(cx - 12, cy + 1), (cx - 21, cy + 2 + w)], 2)


OWL = dict(body=(52, 58), span=36, chord=17, fingers=5, slot=0.1, finger=0.1, wing_root=(4, -6), sweep=0.32,
           amp=1.05, beta0=0.35, bob=1.8, body_fn=owl_body, head=owl_head, tail=owl_tail)


def raven_body(r, c):
    cx, cy = c["body"]
    r.ellipse(cx, cy, 16, 7, -0.06)
    r.ellipse(cx + 8, cy, 8, 7.2)
    r.capsule((cx - 2, cy + 5), (cx - 7, cy + 8), 1.4, 1.0)


def raven_head(r, c):
    cx, cy = c["body"]
    hx, hy = cx + 17, cy - 4
    r.ellipse(hx, hy, 6.0, 5.2)
    # pico grueso y curvado
    r.poly([(hx + 3, hy - 3.4), (hx + 8, hy - 3.6), (hx + 12.4, hy - 1.6), (hx + 13.0, hy + 0.2),
            (hx + 8, hy + 1.2), (hx + 3, hy + 2.6)], smooth=True)
    r.line([(hx + 4, hy - 0.2), (hx + 12, hy - 0.4)], 1)
    r.eye(hx + 1.2, hy - 1.8, 2)
    # plumas erizadas de la garganta
    for k in range(4):
        x = hx - 1 + k * 1.8
        r.poly([(x - 1.4, hy + 3), (x + 1.4, hy + 3), (x - 0.6, hy + 7.2 - k * 0.5)])


def raven_tail(r, c):
    cx, cy = c["body"]
    w = math.sin(c["t"] - 1) * 0.8
    r.poly([(cx - 12, cy - 3), (cx - 24, cy - 3.6 + w), (cx - 30, cy + 1 + w), (cx - 24, cy + 5.6 + w),
            (cx - 12, cy + 4)])
    r.line([(cx - 14, cy + 0.6), (cx - 28, cy + 1 + w)], 2)


RAVEN = dict(body=(52, 56), span=40, chord=15, fingers=6, slot=0.2, finger=0.16, wing_root=(5, -4), sweep=0.36,
             amp=1.1, beta0=0.3, bob=1.6, body_fn=raven_body, head=raven_head, tail=raven_tail)


def magpie_body(r, c):
    cx, cy = c["body"]
    r.ellipse(cx, cy, 12.5, 6.4, -0.06)
    r.ellipse(cx + 6, cy, 7, 6.4)
    r.tint([(cx - 2, cy + 1), (cx + 10, cy + 0.5), (cx + 9, cy + 6.5), (cx - 4, cy + 6.5)], +2)   # vientre blanco
    r.tint([(cx + 8, cy - 8), (cx + 16, cy - 8), (cx + 16, cy + 1), (cx + 9, cy + 1)], -2)          # pecho negro


def magpie_head(r, c):
    cx, cy = c["body"]
    hx, hy = cx + 13.5, cy - 3
    r.ellipse(hx, hy, 5.0, 4.6)
    r.poly([(hx + 3, hy - 2.2), (hx + 9.4, hy - 0.6), (hx + 3.2, hy + 1.6)])
    r.tint([(hx - 6, hy - 6), (hx + 6, hy - 6), (hx + 6, hy + 6), (hx - 6, hy + 6)], -2, smooth=False)
    r.eye(hx + 1.0, hy - 1.4, 2)


def magpie_tail(r, c):
    cx, cy = c["body"]
    w = math.sin(c["t"] - 1.2) * 1.4
    pts = [(cx - 9, cy - 0.5), (cx - 22, cy + 2 + w * 0.4), (cx - 35, cy + 5 + w), (cx - 44, cy + 7.5 + w * 1.4)]
    r.stroke(pts, 3.2, 2.6)
    r.line(catmull([(cx - 11, cy + 0.4), (cx - 24, cy + 2.6 + w * 0.4), (cx - 41, cy + 7.4 + w * 1.4)], False, 3), 2)
    r.tint([(cx - 50, cy - 4), (cx - 8, cy - 4), (cx - 8, cy + 14), (cx - 50, cy + 14)], -1, smooth=False)


def magpie_patch(r, c):
    P = c["wingP"]
    # primarias blancas
    r.tint([P(0.55, 0.25), P(1.05, 0.10), P(1.05, 0.45), P(0.56, 0.95), P(0.5, 0.6)], +2, layer="wing")
    r.tint([P(0.0, 0.0), P(0.5, -0.1), P(0.5, 0.5), P(0.0, 0.5)], -2, layer="wing")


MAGPIE = dict(body=(62, 54), span=30, chord=13, fingers=5, slot=0.08, finger=0.06, wing_root=(3, -3.5),
              sweep=0.28, amp=1.1, beta0=0.3, bob=1.5, body_fn=magpie_body, head=magpie_head, tail=magpie_tail,
              after=magpie_patch)


def phoenix_body(r, c):
    cx, cy = c["body"]
    r.ellipse(cx, cy, 15, 7.6, -0.08)
    r.ellipse(cx + 8, cy - 1, 8.4, 7.4)
    # cuello largo y elegante
    r.stroke([(cx + 10, cy - 2), (cx + 15, cy - 7), (cx + 18, cy - 11)], 5.0, 4.0)


def phoenix_head(r, c):
    cx, cy = c["body"]
    t = c["t"]
    hx, hy = cx + 20, cy - 13
    # cresta de plumas
    for k, (dx, h, a) in enumerate(((-2.6, 9, -2.2), (-1.0, 11, -1.95), (0.6, 8, -1.7))):
        sway = math.sin(t * 2 + k) * 0.12
        b = (hx + dx, hy - 2.8)
        tip = (b[0] + math.cos(a + sway) * h, b[1] + math.sin(a + sway) * h)
        r.stroke([b, lerp2(b, tip, 0.6), tip], 1.0, 0.6)
        r.ellipse(tip[0], tip[1], 1.6, 1.3)
    r.ellipse(hx, hy, 5.2, 4.6)
    r.poly([(hx + 3.0, hy - 2.2), (hx + 7.6, hy - 1.6), (hx + 8.6, hy + 0.8), (hx + 7.0, hy + 2.6),
            (hx + 3.0, hy + 1.8)], smooth=True)
    r.line([(hx + 3.4, hy + 0.2), (hx + 7.6, hy + 0.6)], 1)
    r.eye(hx + 1.0, hy - 1.2, 2)
    r.line([(hx + 2.6, hy - 1.0), (hx + 4.4, hy - 0.6)], 1)


def phoenix_tail(r, c):
    cx, cy = c["body"]
    t = c["t"]
    for k, (a, L) in enumerate(((0.26, 31), (0.42, 37), (0.60, 28))):
        pts = []
        for i in range(8):
            f = i / 7
            ang = math.pi - a - 0.1 * f
            x = cx - 12 + math.cos(ang) * L * f
            y = cy + 2 + math.sin(math.pi - ang) * L * f * 0.6 + math.sin(t - f * 3 + k) * 2.2 * f
            pts.append((x, y))
        r.stroke(pts, 2.2, 1.2)
        tip = pts[-1]
        r.poly([(tip[0] + 2, tip[1] - 2.6), (tip[0] - 4.6, tip[1] - 0.6), (tip[0] - 7.6, tip[1] + 2.4),
                (tip[0] - 3.4, tip[1] + 3.0), (tip[0] + 1.6, tip[1] + 2.0)], smooth=True)
        r.dot(tip[0] - 2.6, tip[1] + 0.8, 2)
        r.wisp([(tip[0] - 7, tip[1] + 2), (tip[0] - 11, tip[1] + 1 + math.sin(t * 2 + k))], 120)


PHOENIX = dict(body=(63, 50), span=44, chord=17, fingers=7, slot=0.16, finger=0.2, wing_root=(5, -5),
               sweep=0.34, amp=1.05, beta0=0.32, bob=1.8, body_fn=phoenix_body, head=phoenix_head,
               tail=phoenix_tail)


def swan(r, u):
    t = TAU * u
    bob = math.sin(t) * 0.8
    lift = math.sin(t) * 2.0
    cx, wl = 54, 86 + bob   # línea de flotación
    # agua: ondas animadas bajo el cisne
    for k in range(7):
        x0 = 14 + k * 14 + (u * 14) % 14
        y = 91 + (k % 2) * 3
        for x in range(int(x0), int(x0 + 7)):
            if 0 <= x < 120:
                r.under.append((x, y, 110 - abs(x - x0 - 3) * 12))
    def arched_wing(ox, oy, ry, lines):
        """Ala arqueada sobre el lomo con el borde festoneado de plumas."""
        ccx, ccy = cx - 6 + ox, wl - 10 + oy
        rx = 22
        pts = [(ccx + 20, ccy + 2)]
        tips = []
        n = 9
        for i in range(n * 2 + 1):
            a = math.pi * lerp(0.12, 0.93, i / (n * 2))
            k = 1.0 if i % 2 == 0 else 0.86
            p = (ccx + rx * k * math.cos(a), ccy - ry * k * math.sin(a))
            if i % 2 == 0:
                p = (p[0] - 1.6, p[1])  # puntas de pluma hacia atrás
                tips.append(p)
            pts.append(p)
        pts += [(ccx - 20, ccy + 4), (ccx + 4, ccy + 5)]
        r.poly(pts)
        if lines:
            for i, p in enumerate(tips[1:-1]):
                q = lerp2((ccx + 2, ccy + 2), p, 0.55)
                r.line([q, lerp2((ccx + 2, ccy + 2), p, 0.9)], 2)
            r.line(catmull([(ccx + 16, ccy - 6), (ccx + 2, ccy - 13 - lift * 0.4), (ccx - 14, ccy - 10)], False, 3), 2)

    # ala lejana (arqueada)
    r.layer("far_wing", "far_wing", tone=-2)
    arched_wing(5, -3, 22 + lift, False)
    r.layer("body", "body")
    r.poly([(cx - 30, wl - 10), (cx - 34, wl - 18), (cx - 26, wl - 12), (cx - 12, wl - 14), (cx + 6, wl - 15),
            (cx + 20, wl - 12), (cx + 25, wl - 6), (cx + 22, wl), (cx - 20, wl), (cx - 30, wl - 4)], smooth=True)
    # cuello en S y cabeza
    sway = math.sin(t - 0.8) * 0.8
    neck = [(cx + 19, wl - 10), (cx + 25, wl - 18), (cx + 25 + sway, wl - 28), (cx + 21 + sway, wl - 37),
            (cx + 23 + sway, wl - 45), (cx + 28 + sway, wl - 48)]
    r.stroke(neck, 4.2, 2.8)
    hx, hy = cx + 29 + sway, wl - 48
    r.layer("head", "head")
    r.ellipse(hx, hy, 4.0, 3.2, 0.2)
    r.poly([(hx + 2, hy - 1.6), (hx + 10.6, hy + 2.4), (hx + 10.0, hy + 3.8), (hx + 2.0, hy + 2.4)], smooth=True)
    r.tint([(hx + 1, hy - 3), (hx + 4.6, hy - 2), (hx + 4.4, hy + 3), (hx + 1, hy + 3)], -2, smooth=False)  # protuberancia
    r.eye(hx + 0.6, hy - 0.8, 2)
    # ala cercana arqueada sobre el lomo, con plumas
    r.layer("wing", "wing")
    arched_wing(0, 0, 20 + lift, True)
    r.line(catmull([(cx - 26, wl - 6), (cx - 6, wl - 4), (cx + 12, wl - 6)], False, 3), 2)


ANIMALS = {
    "owl": lambda r, u: flying_bird(r, u, OWL),
    "raven_or_crow": lambda r, u: flying_bird(r, u, RAVEN),
    "magpie": lambda r, u: flying_bird(r, u, MAGPIE),
    "phoenix": lambda r, u: flying_bird(r, u, PHOENIX),
    "swan": swan,
    "crocodile": crocodile,
    "otter": otter,
    "weasel": lambda r, u: mustelid(r, u, 9.0, (1.9, 1.2)),
    "mink": lambda r, u: mustelid(r, u, 14.0, (2.8, 2.6), bushy=True, tone=-1, size=1.06),
    "hare": hare,
    "goat": lambda r, u: quad(r, u, GOAT),
    "boar": lambda r, u: quad(r, u, BOAR),
    "rhinoceros": lambda r, u: quad(r, u, RHINO),
    "polar_bear": lambda r, u: quad(r, u, POLAR_BEAR),
    "hedgehog": hedgehog,
    "cat": lambda r, u: quad(r, u, CAT),
    "persian_cat": lambda r, u: quad(r, u, PERSIAN),
    "lioness": lambda r, u: quad(r, u, LIONESS),
    "dog": lambda r, u: quad(r, u, JRT),
    "fox": lambda r, u: quad(r, u, FOX),
    "horse": horse,
    "unicorn": lambda r, u: quad(r, u, UNICORN),
    "stag": lambda r, u: quad(r, u, STAG),
    "deer": lambda r, u: quad(r, u, DEER),
    "doe": lambda r, u: quad(r, u, DOE),
}
