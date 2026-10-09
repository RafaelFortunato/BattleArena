# Rebuilds the 4 tree types and renders comparison sheets into Blender's temp folder.
# Run in Blender: exec(open(r"<this file>").read()). AutumnOak.fbx is the Round Oak, exported at the origin
# (FBX: -Z forward, Y up, apply transform, mesh only).
import bpy, math, os
from mathutils import Vector

DIR = r"C:\Users\GatO\Documentos\Unity\BattleArena\SourceArt\Trees"
exec(open(os.path.join(DIR, "treegen.py"), encoding="utf-8").read())
exec(open(os.path.join(DIR, "designs.py"), encoding="utf-8").read())

bpy.ops.wm.read_homefile(use_empty=True)
scene = bpy.context.scene

for s in SLOTS:
    material(s)

SPACING = 6.5
trees = []
raw = {}
BUDGET = 2200
LEAVES = 420
import random
for i, build in enumerate((round_oak, spreading_maple, tall_poplar, gnarled_oak)):
    CLUMPS.clear()
    t = build((i * SPACING, 0, 0))
    raw[t.name] = tris(t)
    if tris(t) > BUDGET:
        decimate(t, BUDGET / tris(t))
    add_leaves(t, list(CLUMPS), LEAVES, random.Random(i + 7))
    trees.append(t)

# Ground, light, camera.
gme = bpy.data.meshes.new("Ground")
gme.from_pydata([(-40, -40, 0), (40, -40, 0), (40, 40, 0), (-40, 40, 0)], [], [(0, 1, 2, 3)])
ground = link(bpy.data.objects.new("Ground", gme))
ground.location = (8, 0, 0)
gm = bpy.data.materials.new("Ground"); gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = srgb("#4E6B3A")
ground.data.materials.append(gm)

sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
scene.collection.objects.link(sun)
sun.data.energy = 4.5
sun.data.color = (1.0, 0.86, 0.66)
sun.data.angle = math.radians(8)
sun.rotation_euler = (math.radians(50), 0, math.radians(35))

world = bpy.data.worlds.new("World"); scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.62, 0.75, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.9

cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.type = "ORTHO"
cam.data.ortho_scale = 33
target = Vector((7.0, 0, 2.3))
cam.location = target + Vector((0, -30, 15))
cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()

for engine in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
    try:
        scene.render.engine = engine
        break
    except TypeError:
        pass
scene.view_settings.view_transform = "Standard"
scene.render.resolution_x = 2200
scene.render.resolution_y = 640
scene.view_settings.look = "None"
scene.render.filepath = os.path.join(bpy.app.tempdir, "lineup.png")
bpy.ops.render.render(write_still=True)

# Closer views of the four types.
scene.render.resolution_x = 1500
scene.render.resolution_y = 750
cam.data.ortho_scale = 13.5
for label, x in (("close_a", -2.75), ("close_b", 9.75), ("close_c", 19.5)):
    target = Vector((x, 0, 2.4))
    cam.location = target + Vector((0, -30, 15))
    scene.render.filepath = os.path.join(bpy.app.tempdir, label + ".png")
    bpy.ops.render.render(write_still=True)

result = {t.name: tris(t) for t in trees}
result["raw"] = raw
result["dims"] = {t.name: [round(d, 2) for d in t.dimensions] for t in trees}
