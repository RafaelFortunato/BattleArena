"""Builds the critical hit from separately generated layers, the way impact sounds are designed by hand:
a sharp transient (slash), a heavy body (thump) and a bright sweetener (crystal ching) on top.

    python_embeded/python.exe mix_crit.py
Writes crit_hit_1..3.wav into the Unity project, each from a different combination of takes.
"""
import pathlib
import sys

import numpy as np

# The portable python ignores the script's folder, so put it on the path for the shared helpers.
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import process_sfx as p

# layer: (max seconds, gain, offset seconds)
LAYERS = {
    "crit_layer_attack": (0.15, 0.75, 0.0),
    "crit_layer_body": (0.35, 1.0, 0.006),
    "crit_layer_sweet": (0.9, 0.55, 0.018),
}


def onset_strength(clip, rate):
    """How much of the sound arrives in its first 25 ms: higher reads as a sharper hit."""
    head = np.abs(clip[: int(rate * 0.025)]).max()
    return head / (np.sqrt((clip ** 2).mean()) + 1e-6)


def takes(layer):
    max_seconds = LAYERS[layer][0]
    shaped = []
    for path in sorted((p.SRC / layer).glob("*.wav")):
        data, rate = p.read(path)
        clip = p.shape_one_shot(data, rate, max_seconds)
        if clip is not None:
            shaped.append((clip, rate))
    # The transient and body want the snappiest takes; the sweetener just wants its full ring.
    if layer != "crit_layer_sweet":
        shaped.sort(key=lambda t: onset_strength(*t), reverse=True)
    return shaped


def main():
    layers = {name: takes(name) for name in LAYERS}
    rate = layers["crit_layer_body"][0][1]
    for variant in range(3):
        length = max(int(rate * offset) + len(layers[name][variant % len(layers[name])][0])
                     for name, (_, _, offset) in LAYERS.items())
        mix = np.zeros((length, 2), dtype=np.float32)
        for name, (_, gain, offset) in LAYERS.items():
            clip, _ = layers[name][variant % len(layers[name])]
            if clip.shape[1] == 1:
                clip = np.repeat(clip, 2, axis=1)
            start = int(rate * offset)
            mix[start:start + len(clip)] += clip * gain
        # Soft-clip so the stacked peaks glue together instead of spiking, then level to -1 dBFS.
        mix = np.tanh(mix * 1.4)
        mix = mix / np.abs(mix).max() * 10 ** (-1 / 20)
        p.write(p.DST / f"crit_hit_{variant + 1}.wav", mix, rate)
        print(f"crit_hit_{variant + 1}.wav", round(length / rate, 2), "s")


if __name__ == "__main__":
    main()
