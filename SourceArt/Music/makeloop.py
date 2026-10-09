# Cuts Battle Theme 4 to its 50-bar body (first downbeat to the final chord) so it loops seamlessly:
# the last bar runs straight into bar 1. Drops the MP3 padding and the 3.5 s ring-out.
# Needs numpy + PyAV (ComfyUI's python_embeded has both). From this folder:
#   python analyze.py battle_theme_4.mp3 theme.npy
#   python makeloop.py ../../Assets/Game/Audio/Music/battle_theme_4_loop.wav seam_preview.wav
import sys, wave
import numpy as np

x = np.load("theme.npy")
sr = 44100
mono = x.mean(axis=0)
BARS, BAR = 50, 2.0  # 120 bpm, 4/4

# Exact first attack: first sample above -40 dBFS, backed off 1 ms so the transient stays whole.
first = int(np.argmax(np.abs(mono) > 10 ** (-40 / 20)))
s = max(0, first - sr // 1000)
length = int(round(BARS * BAR * sr))

# Refine the loop length by up to +-15 ms: line the final chord's attack up with bar 1's attack
# (onset envelopes cross-correlated), so the downbeat lands exactly where the chord did.
def env(a, n):
    seg = mono[a:a + n]
    h = 64
    e = np.sqrt(np.convolve(seg ** 2, np.ones(h) / h, mode="same"))
    return np.maximum(np.diff(np.log(e + 1e-6)), 0)

n = int(0.12 * sr)
ref = env(s, n)
best, shift = -1, 0
for d in range(-int(0.015 * sr), int(0.015 * sr) + 1, 8):
    c = float(np.dot(ref, env(s + length + d, n)))
    if c > best:
        best, shift = c, d
length += shift
e = s + length
print(f"start {s / sr:.4f}s, end {e / sr:.4f}s, length {length / sr:.4f}s (shift {shift / sr * 1000:+.1f} ms)")

out = x[:, s:e].copy()
# The cut sits on a beat, so the step from the last sample back to the first is a click at most:
# a 12 ms equal-power blend of the tail into the silence just before bar 1 removes it.
C = int(0.012 * sr)
t = np.linspace(0, 1, C, dtype=np.float32)
pre = x[:, s - C:s] if s >= C else np.zeros((2, C), np.float32)
out[:, -C:] = out[:, -C:] * np.cos(t * np.pi / 2) + pre * np.sin(t * np.pi / 2)

peak = np.abs(out).max()
print(f"peak {20 * np.log10(peak):.2f} dBFS, wrap step {np.abs(out[:, -1] - out[:, 0]).max():.4f}")
pcm = (np.clip(out, -1, 1) * 32767).round().astype("<i2").T.copy()
with wave.open(sys.argv[1], "wb") as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(sr)
    w.writeframes(pcm.tobytes())

# A preview of the seam: the last 6 s then the first 6 s, as the loop will play it.
seam = np.concatenate([out[:, -6 * sr:], out[:, :6 * sr]], axis=1)
pcm = (np.clip(seam, -1, 1) * 32767).round().astype("<i2").T.copy()
with wave.open(sys.argv[2], "wb") as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(sr)
    w.writeframes(pcm.tobytes())
