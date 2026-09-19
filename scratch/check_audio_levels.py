import os
import miniaudio
import numpy as np
import wave

audio_dir = r"C:\Users\Duy\Documents\MyProject\G10\Audio"
files = [
    "Camera Shutter Sound Effects.mp3",
    "Diving_Sea_Ambience.mp3",
    "Radar_Sound_Effect.mp3",
    "Sound zip open bag.mp3",
    "Sound zip close bag.mp3"
]

for fname in files:
    path = os.path.join(audio_dir, fname)
    decoded = miniaudio.decode_file(path)
    samples = np.frombuffer(decoded.samples, dtype=np.int16)
    if decoded.nchannels == 2:
        mono = samples.reshape(-1, 2).mean(axis=1)
    else:
        mono = samples
    
    print(f"=== {fname} ===")
    print(f"  Channels: {decoded.nchannels}, Sample Rate: {decoded.sample_rate}")
    print(f"  Length: {len(mono)/decoded.sample_rate:.3f}s")
    print(f"  Peak Amplitude: {np.max(np.abs(mono))}/32768 ({np.max(np.abs(mono))/32768*100:.1f}%)")
    print(f"  RMS: {np.sqrt(np.mean(mono.astype(float)**2)):.1f}")
