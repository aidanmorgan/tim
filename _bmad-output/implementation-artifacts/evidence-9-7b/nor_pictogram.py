import math

S = 0.65 / 24
T = 0.035


def face(x, y):
    return ((x - 12) * S, (12 - y) * S)


def quad(p0, c, p1, t):
    a = (1 - t) ** 2
    b = 2 * t * (1 - t)
    d = t * t
    return (a * p0[0] + b * c[0] + d * p1[0], a * p0[1] + b * c[1] + d * p1[1])


def box(name, a, b, mesh):
    (ax, ay), (bx, by) = face(*a), face(*b)
    dx, dy = bx - ax, by - ay
    length = math.hypot(dx, dy)
    theta = math.atan2(dy, dx)
    c, s = math.cos(theta), math.sin(theta)
    print(f"{name} len={length + T:.4f} mesh={mesh}")
    print(f'Transform3D({c:.6f},{s:.6f},0,{-s:.6f},{c:.6f},0,0,0,1,{(ax + bx) / 2:.5f},{(ay + by) / 2:.5f},0.4)')


curves = {
    "Upper": ((5, 4), (14, 4), (17, 12)),
    "Lower": ((17, 12), (14, 20), (5, 20)),
    "Back": ((5, 20), (11, 12), (5, 4)),
}
for name, (p0, c, p1) in curves.items():
    for k in range(4):
        box(f"{name}{k + 1}", quad(p0, c, p1, k / 4), quad(p0, c, p1, (k + 1) / 4), name)
box("OutputStub", (21, 12), (23, 12), "Out")
print("bubble x", face(19, 12))
