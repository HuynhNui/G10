import os
import glob
import miniaudio
import numpy as np

uploaded_dir = r"C:\Users\Duy\.gemini\antigravity\brain\37912308-2749-42bc-973d-154d2b4ce7de\.user_uploaded"
files = sorted(glob.glob(os.path.join(uploaded_dir, "uploaded_media_*")))

names = ["camera_shutter", "ambient_submarine", "radar_ping", "zip_open", "zip_close"]

for i, (f, name) in enumerate(zip(files, names)):
    decoded = miniaudio.decode_file(f)
    print(f"[{i}] {name}:")
    print(f"    Orig: {os.path.basename(f)}")
    print(f"    Sample rate: {decoded.sample_rate}")
    print(f"    Channels: {decoded.nchannels}")
    print(f"    Duration: {len(decoded.samples) / (decoded.sample_rate * decoded.nchannels * 2):.3f}s")
