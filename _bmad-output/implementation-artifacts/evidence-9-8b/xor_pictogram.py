import math

# CAT-027 Exactly one gate pictogram, WorkshopIcons.cs@a6c914e:L77:
# M2 4q6 8 0 16 M6 4q11 0 15 8-4 8-15 8 6-8 0-16z M21 12h2
# The Or body and output stub are identical to electrical_or.tscn; only the extra back curve is new.
S = 0.65 / 24
T = 0.035


def face(x, y):
    return ((x - 12) * S, (12 - y) * S)


def quad(p0, c, p1, t):
    a, b, d = (1 - t) ** 2, 2 * t * (1 - t), t * t
    return (a * p0[0] + b * c[0] + d * p1[0], a * p0[1] + b * c[1] + d * p1[1])


def box(name, a, b):
    (ax, ay), (bx, by) = face(*a), face(*b)
    dx, dy = bx - ax, by - ay
    theta = math.atan2(dy, dx)
    c, s = math.cos(theta), math.sin(theta)
    print(f"{name} len={math.hypot(dx, dy) + T:.4f} "
          f"Transform3D({c:.6f},{s:.6f},0,{-s:.6f},{c:.6f},0,0,0,1,{(ax + bx) / 2:.5f},{(ay + by) / 2:.5f},0.4)")


p0, c, p1 = (2, 4), (8, 12), (2, 20)
for k in range(4):
    box(f"Extra{k + 1}", quad(p0, c, p1, k / 4), quad(p0, c, p1, (k + 1) / 4))
