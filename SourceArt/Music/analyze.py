# Decodes the battle theme and reports silence, a loudness envelope and a tempo estimate.
import sys
import av
import numpy as np

path = sys.argv[1]
container = av.open(path)
stream = container.streams.audio[0]
sr = stream.rate
chunks = []
for frame in container.decode(stream):
    a = frame.to_ndarray()
    if a.ndim == 1 or a.shape[0] not in (1, 2):
        a = a.reshape(-1, frame.layout.nb_channels).T
    chunks.append(a.astype(np.float32))
x = np.concatenate(chunks, axis=1)
if np.abs(x).max() > 2:
    x /= 32768.0
mono = x.mean(axis=0)
n = mono.size
print(f"rate {sr}, channels {x.shape[0]}, samples {n}, duration {n / sr:.2f}s, codec {stream.codec_context.name}")

thr = 10 ** (-60 / 20)
nz = np.nonzero(np.abs(mono) > thr)[0]
print(f"leading silence {nz[0] / sr * 1000:.1f} ms, trailing silence {(n - 1 - nz[-1]) / sr * 1000:.1f} ms")

win = sr // 2
rms = [np.sqrt(np.mean(mono[i:i + win] ** 2)) for i in range(0, n - win, win)]
db = 20 * np.log10(np.maximum(rms, 1e-9))
print("loudness per 0.5 s (dBFS):")
for i in range(0, len(db), 8):
    print(f"  {i * 0.5:6.1f}s  " + " ".join(f"{v:6.1f}" for v in db[i:i + 8]))

# Tempo from the autocorrelation of an onset envelope.
hop = 512
frames = n // hop
env = np.array([np.sum(mono[i * hop:(i + 1) * hop] ** 2) for i in range(frames)])
onset = np.maximum(np.diff(np.log(env + 1e-9)), 0)
onset -= onset.mean()
ac = np.correlate(onset, onset, mode="full")[onset.size - 1:]
fps = sr / hop
lags = np.arange(ac.size) / fps
mask = (lags > 60 / 200) & (lags < 60 / 70)
best = lags[mask][np.argmax(ac[mask])]
print(f"tempo estimate {60 / best:.1f} bpm (beat {best:.3f}s)")
np.save(sys.argv[2], x)
print("saved", sys.argv[2], sr)
