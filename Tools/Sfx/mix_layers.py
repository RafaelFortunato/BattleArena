"""Builds layered sounds from separately generated layers, the way impact sounds are designed by hand:
a sharp transient, a body and a sweetener (or texture) mixed with a few milliseconds between them.

    python_embeded/python.exe mix_layers.py [sound ...]   # default: every recipe
Writes <sound>_1..3.wav into the Unity project, each from a different combination of takes.
"""
import pathlib
import sys

import numpy as np

# The portable python ignores the script's folder, so put it on the path for the shared helpers.
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import process_sfx as p

# sound -> layer -> (max seconds, gain, offset seconds, sort by onset)
RECIPES = {
    # Slash transient, heavy thump, crystal ching ringing over the top.
    "crit_hit": {
        "crit_layer_attack": (0.15, 0.75, 0.0, True),
        "crit_layer_body": (0.35, 1.0, 0.006, True),
        "crit_layer_sweet": (0.9, 0.55, 0.018, False),
    },
    # A lightning discharge: tearing strike crack up front, the high-voltage arc under it, one short low thump.
    "chain_lightning": {
        "chain_layer_strike": (0.3, 1.0, 0.0, True),
        "chain_layer_arc": (0.7, 0.75, 0.005, True),
        "chain_layer_boom": (0.6, 0.55, 0.015, True),
    },
}

VARIANTS = 3


def onset_strength(clip, rate):
    """How much of the sound arrives in its first 25 ms: higher reads as a sharper hit."""
    head = np.abs(clip[: int(rate * 0.025)]).max()
    return head / (np.sqrt((clip ** 2).mean()) + 1e-6)


def takes(layer, max_seconds, by_onset):
    shaped = []
    for path in sorted((p.SRC / layer).glob("*.wav")):
        data, rate = p.read(path)
        clip = p.shape_one_shot(data, rate, max_seconds)
        if clip is not None:
            shaped.append((clip, rate))
    if by_onset:
        shaped.sort(key=lambda t: onset_strength(*t), reverse=True)
    return shaped


def mix(sound, recipe):
    layers = {name: (takes(name, seconds, by_onset), gain, offset)
              for name, (seconds, gain, offset, by_onset) in recipe.items()}
    rate = next(iter(layers.values()))[0][0][1]
    for old in p.DST.glob(f"{sound}_*.wav"):
        old.unlink()

    for variant in range(VARIANTS):
        picks = [(shaped[variant % len(shaped)][0], gain, offset) for shaped, gain, offset in layers.values()]
        length = max(int(rate * offset) + len(clip) for clip, _, offset in picks)
        out = np.zeros((length, 2), dtype=np.float32)
        for clip, gain, offset in picks:
            if clip.shape[1] == 1:
                clip = np.repeat(clip, 2, axis=1)
            start = int(rate * offset)
            out[start:start + len(clip)] += clip * gain
        # Soft-clip so the stacked peaks glue together instead of spiking, then level to -1 dBFS.
        out = np.tanh(out * 1.4)
        out = out / np.abs(out).max() * 10 ** (-1 / 20)
        p.write(p.DST / f"{sound}_{variant + 1}.wav", out, rate)
        print(f"{sound}_{variant + 1}.wav", round(length / rate, 2), "s")


def main():
    for sound in sys.argv[1:] or RECIPES:
        mix(sound, RECIPES[sound])


if __name__ == "__main__":
    main()
