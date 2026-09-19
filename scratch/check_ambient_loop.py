import os
import miniaudio
import numpy as np

f_amb = r"C:\Users\Duy\Documents\MyProject\G10\Audio\Diving_Sea_Ambience.mp3"
decoded = miniaudio.decode_file(f_amb)
samples = np.frombuffer(decoded.samples, dtype=np.int16)
mono = samples.reshape(-1, 2).mean(axis=1) if decoded.nchannels == 2 else samples
sr = decoded.sample_rate

print(f"Diving_Sea_Ambience: duration={len(mono)/sr:.3f}s")
# Check first 0.1s and last 0.1s
start = mono[:int(0.1*sr)]
end = mono[-int(0.1*sr):]
print(f"Start RMS={np.sqrt(np.mean(start**2)):.1f}, End RMS={np.sqrt(np.mean(end**2)):.1f}")
print(f"Start sample: {mono[0]}, End sample: {mono[-1]}")
