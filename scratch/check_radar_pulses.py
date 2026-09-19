import os
import glob
import miniaudio
import numpy as np

f2 = r"C:\Users\Duy\.gemini\antigravity\brain\37912308-2749-42bc-973d-154d2b4ce7de\.user_uploaded\uploaded_media_2_1789829665933.mp3"
decoded = miniaudio.decode_file(f2)
samples = np.frombuffer(decoded.samples, dtype=np.int16)
if decoded.nchannels == 2:
    mono = samples.reshape(-1, 2).mean(axis=1)
else:
    mono = samples

# Find peaks in envelope
sr = decoded.sample_rate
window = int(0.05 * sr)
env = np.convolve(np.abs(mono), np.ones(window)/window, mode='same')

# Print envelope peaks
threshold = np.max(env) * 0.4
peaks = []
for i in range(1, len(env)-1):
    if env[i] > threshold and env[i] > env[i-1] and env[i] > env[i+1]:
        t = i / sr
        if not peaks or (t - peaks[-1]) > 0.3:
            peaks.append(t)

print("File 2 Radar Pulse Times (s):")
for p in peaks[:15]:
    print(f"  Pulse at {p:.3f}s")
if len(peaks) > 1:
    diffs = np.diff(peaks)
    print(f"Average pulse interval: {np.mean(diffs):.3f}s")
