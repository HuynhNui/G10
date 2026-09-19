import os
import miniaudio
import numpy as np

f_radar = r"C:\Users\Duy\Documents\MyProject\G10\Audio\Radar_Sound_Effect.mp3"
decoded = miniaudio.decode_file(f_radar)
samples = np.frombuffer(decoded.samples, dtype=np.int16)
mono = samples.reshape(-1, 2).mean(axis=1) if decoded.nchannels == 2 else samples
sr = decoded.sample_rate

print(f"Total samples: {len(mono)}, duration: {len(mono)/sr:.3f}s")
# Inspect first 3 seconds in 0.2s slices
for t in np.arange(0, 3.0, 0.2):
    i1 = int(t * sr)
    i2 = int((t + 0.2) * sr)
    chunk = mono[i1:i2]
    print(f"t={t:.1f}s - {t+0.2:.1f}s: Peak={np.max(np.abs(chunk)):.0f}, RMS={np.sqrt(np.mean(chunk**2)):.0f}")
