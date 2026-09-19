import os
import miniaudio
import numpy as np
import wave

radar_src = r"C:\Users\Duy\Documents\MyProject\G10\Audio\Radar_Sound_Effect.mp3"
decoded = miniaudio.decode_file(radar_src)
samples = np.frombuffer(decoded.samples, dtype=np.int16).astype(np.float64)
if decoded.nchannels == 2:
    samples = samples.reshape(-1, 2)
else:
    samples = np.column_stack((samples, samples))

sr = decoded.sample_rate
# We want exactly 2.0s duration (or 2.2s with tail fade)
duration_s = 2.0
num_samples = int(duration_s * sr)
slice_samples = samples[:num_samples].copy()

# Apply 50ms fade out at the very end
fade_len = int(0.05 * sr)
fade = np.linspace(1.0, 0.0, fade_len)[:, None]
slice_samples[-fade_len:] *= fade

peak = np.max(np.abs(slice_samples))
if peak > 0:
    slice_samples = slice_samples * (32767.0 * 0.90 / peak)
slice_samples = np.clip(slice_samples, -32768, 32767).astype(np.int16)

dest_paths = [
    r"C:\Users\Duy\Documents\MyProject\G10\Assets\_Project\Audio\SFX\radar_ping.wav",
    r"C:\Users\Duy\Documents\MyProject\G10\Assets\_Project\Resources\Audio\SFX\radar_ping.wav"
]

for p in dest_paths:
    with wave.open(p, 'wb') as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(sr)
        wf.writeframes(slice_samples.tobytes())
    print(f"Wrote {p} (samples={len(slice_samples)}, duration={len(slice_samples)/sr:.2f}s)")
