# Four autumn tree types built with treegen. exec() after treegen.py.
import math, random
from mathutils import Vector


def tone(z, lo, hi, rng):
    """Clump colour by height in the crown, with a little randomness: dark low, mid around, golden on top."""
    t = (z - lo) / max(hi - lo, 1e-3) + rng.uniform(-0.18, 0.18)
    return 3 if t > 0.62 else (2 if t > 0.25 else 1)


def round_oak(location):
    rng = random.Random(11)
    nodes = [((0, 0, 0), 0.42), ((0, 0, 0.7), 0.34), ((0.05, 0, 1.35), 0.29)]
    edges = [(0, 1), (1, 2)]
    for pts in ([((0.45, 0.15, 1.9), 0.19), ((0.8, 0.25, 2.45), 0.12)],
                [((-0.4, 0.25, 2.0), 0.18), ((-0.75, 0.35, 2.5), 0.11)],
                [((0.0, -0.4, 2.05), 0.17), ((0.05, -0.7, 2.6), 0.1)],
                [((0.05, 0.05, 2.2), 0.19), ((0.0, 0.0, 2.9), 0.11)]):
        branch_chain(nodes, edges, 2, pts)
    for p in ((0.5, 0.1, 0.06), (-0.35, 0.42, 0.06), (-0.22, -0.47, 0.06)):
        branch_chain(nodes, edges, 0, [(p, 0.13)])
    parts = [trunk("oak_trunk", nodes, edges)]
    center = Vector((0, 0, 3.05))
    parts.append(clump("oak_core", center, 1.2, (1.1, 1.1, 0.88), seed=1, tone=1))
    for i, p in enumerate(dome_points(rng, 11, center, 1.2, 1.0, min_z=-0.4)):
        r = rng.uniform(0.55, 0.75)
        t = tone(p.z, 2.4, 4.0, rng)
        parts.append(clump(f"oak_c{i}", p, r, seed=i + 2, tone=t))
    return assemble("Tree_RoundOak", parts, location)


def spreading_maple(location):
    rng = random.Random(23)
    nodes = [((0, 0, 0), 0.4), ((0, 0, 0.8), 0.31), ((0, 0.05, 1.45), 0.27)]
    edges = [(0, 1), (1, 2)]
    for k in range(5):
        a = k / 5 * math.tau + 0.3
        d = Vector((math.cos(a), math.sin(a), 0))
        mid = Vector((0, 0.05, 1.45)) + d * 0.55 + Vector((0, 0, 0.55))
        end = Vector((0, 0.05, 1.45)) + d * 1.15 + Vector((0, 0, 1.05))
        branch_chain(nodes, edges, 2, [(tuple(mid), 0.16), (tuple(end), 0.09)])
    branch_chain(nodes, edges, 2, [((0, 0.05, 2.3), 0.15), ((0.05, 0.1, 3.0), 0.08)])
    for p in ((0.45, -0.2, 0.06), (-0.4, -0.25, 0.06), (0.05, 0.48, 0.06)):
        branch_chain(nodes, edges, 0, [(p, 0.12)])
    parts = [trunk("maple_trunk", nodes, edges)]
    # Broad, flat canopy: a low ring, a smaller upper ring and a cap; clumps squashed into cushions.
    parts.append(clump("maple_core", (0, 0, 2.95), 1.25, (1.25, 1.25, 0.5), seed=40, tone=1))
    for i in range(7):
        a = i / 7 * math.tau + rng.uniform(-0.2, 0.2)
        p = (math.cos(a) * 1.35, math.sin(a) * 1.35, 2.75 + rng.uniform(-0.1, 0.1))
        r = rng.uniform(0.7, 0.85); t = rng.choice((1, 2, 2))
        parts.append(clump(f"maple_lo{i}", p, r, (1, 1, 0.62), seed=i + 50, tone=t))
    for i in range(4):
        a = i / 4 * math.tau + 0.6
        p = (math.cos(a) * 0.7, math.sin(a) * 0.7, 3.3)
        r = rng.uniform(0.65, 0.78); t = rng.choice((2, 3, 3))
        parts.append(clump(f"maple_hi{i}", p, r, (1, 1, 0.62), seed=i + 60, tone=t))
    parts.append(clump("maple_cap", (0, 0, 3.65), 0.7, (1, 1, 0.6), seed=70, tone=3))
    return assemble("Tree_SpreadingMaple", parts, location)


def tall_poplar(location):
    rng = random.Random(37)
    nodes = [((0, 0, 0), 0.34), ((0, 0, 1.0), 0.25), ((0.02, 0, 2.2), 0.18), ((0, 0.02, 3.6), 0.09)]
    edges = [(0, 1), (1, 2), (2, 3)]
    for p in ((0.38, 0.1, 0.06), (-0.3, 0.3, 0.06), (-0.1, -0.38, 0.06)):
        branch_chain(nodes, edges, 0, [(p, 0.1)])
    parts = [trunk("poplar_trunk", nodes, edges)]
    parts.append(clump("poplar_core", (0, 0, 3.3), 0.85, (1, 1, 2.0), seed=80, tone=1))
    # Tufts spiral up a teardrop profile: widest a third of the way up, pointed at the top.
    count = 16
    for i in range(count):
        t = i / (count - 1)
        z = 1.75 + t * 3.2
        width = 0.95 * math.sin(math.pi * (0.15 + 0.85 * t) ** 0.75) + 0.1
        a = i * 2.4 + rng.uniform(-0.25, 0.25)
        p = (math.cos(a) * width, math.sin(a) * width, z)
        parts.append(clump(f"poplar_t{i}", p, rng.uniform(0.42, 0.55) * (1.15 - 0.45 * t), (1, 1, 1.25), seed=i + 90, tone=tone(z, 1.75, 4.95, rng)))
    return assemble("Tree_TallPoplar", parts, location)


def gnarled_oak(location):
    rng = random.Random(53)
    nodes = [((0, 0, 0), 0.46), ((0.18, 0.05, 0.75), 0.34), ((0.05, 0.12, 1.45), 0.28)]
    edges = [(0, 1), (1, 2)]
    left = branch_chain(nodes, edges, 2, [((-0.55, 0.2, 1.95), 0.2), ((-1.05, 0.15, 2.45), 0.14), ((-1.35, 0.05, 3.0), 0.09)])
    right = branch_chain(nodes, edges, 2, [((0.6, -0.05, 1.95), 0.19), ((1.05, -0.25, 2.4), 0.13), ((1.3, -0.35, 2.95), 0.08)])
    top = branch_chain(nodes, edges, 2, [((0.2, 0.3, 2.3), 0.17), ((0.05, 0.45, 3.0), 0.11), ((0.2, 0.4, 3.5), 0.07)])
    # Bare twigs poking out of the foliage: a graveyard touch.
    branch_chain(nodes, edges, left - 1, [((-1.45, 0.55, 2.35), 0.05), ((-1.75, 0.7, 2.55), 0.03)])
    branch_chain(nodes, edges, right, [((1.6, -0.3, 3.45), 0.04)])
    branch_chain(nodes, edges, top, [((0.45, 0.55, 4.15), 0.03)])
    for p in ((0.55, -0.05, 0.06), (-0.3, 0.48, 0.06), (-0.35, -0.42, 0.06), (0.25, 0.5, 0.06)):
        branch_chain(nodes, edges, 0, [(p, 0.14)])
    parts = [trunk("gnarled_trunk", nodes, edges)]
    # Three lopsided clouds at the branch ends instead of one round crown.
    clouds = (((-1.25, 0.1, 3.15), 0.95), ((1.2, -0.3, 3.05), 0.85), ((0.15, 0.4, 3.65), 0.9))
    k = 0
    for c, size in clouds:
        c = Vector(c)
        parts.append(clump(f"g_core{k}", c, size * 0.75, (1.15, 1.05, 0.75), seed=100 + k, tone=1)); k += 1
        for j in range(4):
            a = rng.uniform(0, math.tau)
            p = c + Vector((math.cos(a) * size * 0.65, math.sin(a) * size * 0.6, rng.uniform(-0.15, 0.35)))
            r = size * rng.uniform(0.42, 0.55); t = tone(p.z, c.z - 0.3, c.z + 0.5, rng)
            parts.append(clump(f"g_c{k}", p, r, seed=100 + k, tone=t)); k += 1
    return assemble("Tree_GnarledOak", parts, location)
