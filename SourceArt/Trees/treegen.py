# Cartoony autumn tree generator: skin-modifier trunks + lumpy leaf clumps, 4 material slots
# (wood, leaf dark, leaf mid, leaf light) to match the game's AutumnTree prefab.
import bpy, bmesh, math, random
from mathutils import Matrix, Quaternion, Vector, noise

SLOTS = ("Env_Wood", "Env_LeafRustDark", "Env_LeafRust", "Env_LeafOchre")
COLORS = {"Env_Wood": "#6B4A30", "Env_LeafRustDark": "#74492F", "Env_LeafRust": "#9A6440", "Env_LeafOchre": "#A88650"}


def srgb(hexstr):
    h = hexstr.lstrip("#")
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c] + [1.0]


def material(name, colors=COLORS):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = srgb(colors[name])
    bsdf.inputs["Roughness"].default_value = 0.9
    m.diffuse_color = srgb(colors[name])
    return m


def link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


def apply_modifiers(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    old = obj.data
    obj.modifiers.clear()
    obj.data = me
    bpy.data.meshes.remove(old)


def trunk(name, nodes, edges, smooth_levels=1):
    """nodes: list of (pos, radius); edges: index pairs. Node 0 is the root."""
    me = bpy.data.meshes.new(name)
    me.from_pydata([Vector(p) for p, _ in nodes], edges, [])
    obj = link(bpy.data.objects.new(name, me))
    mod = obj.modifiers.new("Skin", "SKIN")
    mod.use_smooth_shade = True
    for i, (_, r) in enumerate(nodes):
        sv = me.skin_vertices[0].data[i]
        sv.radius = (r, r)
        sv.use_root = i == 0
    sub = obj.modifiers.new("Sub", "SUBSURF")
    sub.levels = smooth_levels
    sub.render_levels = smooth_levels
    apply_modifiers(obj)
    for p in obj.data.polygons:
        p.use_smooth = True
        p.material_index = 0
    return obj


def branch_chain(nodes, edges, start, points):
    """Appends a chain of (pos, radius) from node index `start`; returns the last index."""
    prev = start
    for p, r in points:
        nodes.append((p, r))
        edges.append((prev, len(nodes) - 1))
        prev = len(nodes) - 1
    return prev


CLUMPS = []


def clump(name, center, radius, scale=(1, 1, 1), seed=0, lump=0.2, subdiv=3, tone=None, bands=(0.45, -0.25), leafy=True):
    """A lumpy leaf clump. Faces facing up take the light tone, facing down the dark one (toon banding)."""
    if leafy:
        CLUMPS.append((Vector(center), radius, Vector(scale), tone if tone is not None else 2))
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=radius)
    off = Vector((seed * 13.1, seed * 7.7, seed * 3.3))
    for v in bm.verts:
        n = v.co.normalized()
        d = noise.noise(n * 1.6 + off) * 0.75 + noise.noise(n * 3.4 + off * 2) * 0.35 + abs(noise.noise(n * 7.0 + off * 3)) * 0.55
        v.co = n * radius * (1 + lump * d)
        v.co.x *= scale[0]; v.co.y *= scale[1]; v.co.z *= scale[2]
    bm.normal_update()
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = link(bpy.data.objects.new(name, me))
    obj.location = Vector(center)
    hi, lo = bands
    for p in me.polygons:
        p.use_smooth = True
        if tone is not None:
            p.material_index = tone
        else:
            nz = p.normal.z
            p.material_index = 3 if nz > hi else (1 if nz < lo else 2)
    return obj


def assemble(name, parts, location=(0, 0, 0)):
    """Merges the parts (built around the trunk base at the world origin) into one mesh with the 4 slots."""
    mats = [material(s) for s in SLOTS]
    bm = bmesh.new()
    for o in parts:
        # matrix_world isn't refreshed for objects created this run; build it from the location.
        o.data.transform(Matrix.Translation(o.location))
        bm.from_mesh(o.data)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    for o in parts:
        mesh = o.data
        bpy.data.objects.remove(o)
        bpy.data.meshes.remove(mesh)
    obj = link(bpy.data.objects.new(name, me))
    obj.location = location
    return obj


def tris(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def dome_points(rng, count, center, rx, rz, min_z=-0.2):
    """Roughly even points over the upper part of an ellipsoid (Fibonacci sphere, jittered)."""
    pts = []
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(count):
        y = 1 - (i + 0.5) / count * (1 - min_z)  # from top (1) down to min_z
        r = math.sqrt(max(0.0, 1 - y * y))
        a = golden * i + rng.uniform(-0.3, 0.3)
        pts.append(Vector((center[0] + math.cos(a) * r * rx, center[1] + math.sin(a) * r * rx, center[2] + y * rz)))
    return pts


def decimate(obj, ratio):
    mod = obj.modifiers.new("Dec", "DECIMATE")
    mod.ratio = ratio
    mod.use_collapse_triangulate = True
    apply_modifiers(obj)
    for p in obj.data.polygons:
        p.use_smooth = True


def add_leaves(obj, clumps, count, rng, size=(0.24, 0.32), shape="oval"):
    """Small folded leaf shapes shingled over the clumps' outer surfaces, tips hanging down the slope. Each takes
    its clump's tone, a few one step lighter so the crown sparkles."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    weights = [r * r * sc.x * sc.y for _, r, sc, _ in clumps]
    for _ in range(count):
        c, r, sc, t = rng.choices(clumps, weights)[0]
        d = Vector((rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1) * 0.8 + 0.35)).normalized()
        n = Vector((d.x / sc.x, d.y / sc.y, d.z / sc.z)).normalized()  # ellipsoid normal
        base = c + Vector((d.x * r * sc.x, d.y * r * sc.y, d.z * r * sc.z)) * 0.99
        # Shingles: the leaf hangs down the slope of the clump (tip toward the ground), give or take 30 degrees.
        down = Vector((0, 0, -1))
        tangent = down - n * down.dot(n)
        if tangent.length < 0.15:
            tangent = n.cross(Vector((1, 0, 0)))
        tangent.normalize()
        tangent.rotate(Quaternion(n, rng.uniform(-0.5, 0.5)))
        L = rng.uniform(*size)
        axis = (tangent + n * 0.12).normalized()
        side = axis.cross(n).normalized()
        w = L * (0.42 if shape == "oval" else 0.55)
        tip = base + axis * L
        mid = base + axis * (L * 0.45)
        fold = n * (L * 0.12)
        verts = [bm.verts.new(p) for p in (base, mid + side * w, tip, mid - side * w, mid + fold)]
        tone_i = min(3, t + 1) if rng.random() < 0.15 else t
        for a, b in ((0, 1), (1, 2), (2, 3), (3, 0)):
            f = bm.faces.new((verts[a], verts[b], verts[4]))
            f.material_index = tone_i
            f.smooth = False
        # Face the folded side outward.
        f_normal = (verts[1].co - verts[0].co).cross(verts[4].co - verts[0].co)
        if f_normal.dot(n) < 0:
            for f in list(bm.faces)[-4:]:
                f.normal_flip()
    bm.to_mesh(obj.data)
    bm.free()
