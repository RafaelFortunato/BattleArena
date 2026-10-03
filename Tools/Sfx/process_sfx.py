"""Trim, fade, normalise and pick takes from the ComfyUI renders, writing game-ready WAVs into the Unity project.

Run with the ComfyUI portable python (it has numpy):
    python_embeded/python.exe process_sfx.py [name ...]
"""
import pathlib
import sys
import wave

import numpy as np

SRC = pathlib.Path(r"C:\Users\GatO\Documentos\ComfyUI\ComfyUI\output\audio\battlearena")
DST = pathlib.Path(r"C:\Users\GatO\Documentos\Unity\BattleArena\Assets\Game\Audio\Sfx")

# Longest a sound may run after trimming; everything else keeps its natural tail.
MAX_SECONDS = {
    "ui_click": 0.25, "ui_hover": 0.15, "gem_pickup": 0.6, "bolt_shoot": 0.6, "bolt_hit": 0.6, "lightning_hit": 0.3, "enemy_hit": 0.45,
    "crit_hit": 0.9, "enemy_death": 1.0, "player_hurt": 0.7, "arrow_hit": 0.5, "arrow_shoot": 1.0, "shard_pickup": 1.2,
    "phase_step": 1.0, "enemy_windup": 1.2, "enemy_magic_shoot": 1.0, "enemy_magic_hit": 0.8, "bomb_throw": 1.2,
    "upgrade_pick": 1.5, "chest_appear": 1.5,
}
TAKES = 3


def read(path):
    with wave.open(str(path)) as f:
        rate, channels = f.getframerate(), f.getnchannels()
        data = np.frombuffer(f.readframes(f.getnframes()), dtype="<i2").astype(np.float32) / 32768.0
    return data.reshape(-1, channels), rate


def write(path, data, rate):
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = (np.clip(data, -1.0, 1.0) * 32767.0).astype("<i2")
    with wave.open(str(path), "wb") as f:
        f.setnchannels(data.shape[1])
        f.setsampwidth(2)
        f.setframerate(rate)
        f.writeframes(pcm.tobytes())


def envelope(data, rate, window=0.01):
    mono = np.abs(data).max(axis=1)
    hop = max(1, int(rate * window))
    frames = len(mono) // hop
    return mono[: frames * hop].reshape(frames, hop).max(axis=1), hop


def shape_one_shot(data, rate, max_seconds):
    env, hop = envelope(data, rate)
    peak = env.max()
    if peak < 1e-3:
        return None
    loud = np.nonzero(env > peak * 10 ** (-40 / 20))[0]
    start = max(0, loud[0] - 1) * hop
    tail = np.nonzero(env > peak * 10 ** (-45 / 20))[0]
    end = min(len(data), (tail[-1] + 2) * hop)
    clip = data[start:end].copy()
    if max_seconds:
        clip = clip[: int(rate * max_seconds)]

    fade_in = min(len(clip), int(rate * 0.004))
    clip[:fade_in] *= np.linspace(0.0, 1.0, fade_in)[:, None]
    fade_out = min(len(clip), max(int(rate * 0.03), int(len(clip) * 0.2)))
    clip[-fade_out:] *= (np.linspace(1.0, 0.0, fade_out) ** 2)[:, None]
    return clip / np.abs(clip).max() * 10 ** (-1 / 20)


def score(clip, rate):
    """Prefer takes whose energy arrives early and that don't smear: a punchy one-shot."""
    env, _ = envelope(clip, rate)
    weights = np.arange(len(env)) + 1
    centroid = (env * weights).sum() / env.sum() / len(env)
    return -centroid


def loop(data, rate, crossfade=3.0):
    """Crossfade the tail over the head so the bed loops without a seam, then level it."""
    n = int(rate * crossfade)
    head, body, tail = data[:n], data[n:-n], data[-n:]
    ramp = np.linspace(0.0, 1.0, n)[:, None]
    blended = np.concatenate([body, tail * (1 - ramp) + head * ramp])
    rms = np.sqrt((blended ** 2).mean())
    return blended * (10 ** (-20 / 20) / rms)


def main():
    only = set(sys.argv[1:])
    for folder in sorted(p for p in SRC.iterdir() if p.is_dir()):
        name = folder.name
        # Layers are mixed by mix_layers.py, and rejected takes are only kept for reference.
        if "_layer_" in name or "_rejected" in name or (only and name not in only):
            continue
        for old in DST.glob(f"{name}_*.wav"):
            old.unlink()

        takes = [read(p) for p in sorted(folder.glob("*.wav"))]
        if name.startswith("ambience"):
            # The steadier take (least level variation) makes the less noticeable bed.
            best = min(takes, key=lambda t: envelope(t[0], t[1], 0.5)[0].std())
            write(DST / f"{name}_1.wav", loop(*best), best[1])
            print(name, "loop")
            continue

        shaped = [(shape_one_shot(d, r, MAX_SECONDS.get(name)), r) for d, r in takes]
        shaped = [(c, r) for c, r in shaped if c is not None and len(c) > r * 0.05]
        shaped.sort(key=lambda t: score(*t), reverse=True)
        for i, (clip, rate) in enumerate(shaped[:TAKES], 1):
            write(DST / f"{name}_{i}.wav", clip, rate)
        print(name, [round(len(c) / r, 2) for c, r in shaped[:TAKES]])


if __name__ == "__main__":
    main()
