import os
import miniaudio
import numpy as np
import wave

audio_src_dir = r"C:\Users\Duy\Documents\MyProject\G10\Audio"

file_map = {
    "Camera Shutter Sound Effects.mp3": ("SFX", "camera_shutter.wav", 0.90),
    "Diving_Sea_Ambience.mp3": ("Ambient", "ambient_submarine_loop.wav", 0.85),
    "Radar_Sound_Effect.mp3": ("SFX", "radar_ping.wav", 0.90),
    "Sound zip open bag.mp3": ("SFX", "zip_open.wav", 0.88),
    "Sound zip close bag.mp3": ("SFX", "zip_close.wav", 0.88),
}

dest_audio_dir = r"C:\Users\Duy\Documents\MyProject\G10\Assets\_Project\Audio"
dest_res_dir = r"C:\Users\Duy\Documents\MyProject\G10\Assets\_Project\Resources\Audio"

def process_file(src_name, folder, dst_name, target_peak_ratio):
    src_path = os.path.join(audio_src_dir, src_name)
    decoded = miniaudio.decode_file(src_path)
    
    samples = np.frombuffer(decoded.samples, dtype=np.int16).astype(np.float64)
    if decoded.nchannels == 2:
        samples = samples.reshape(-1, 2)
    else:
        # Convert mono to stereo for rich spatial feel
        samples = np.column_stack((samples, samples))
    
    # Normalize gain
    peak = np.max(np.abs(samples))
    if peak > 0:
        gain = (32767.0 * target_peak_ratio) / peak
        samples = samples * gain
    samples = np.clip(samples, -32768, 32767).astype(np.int16)
    
    # Write to target paths
    for base_dir in [dest_audio_dir, dest_res_dir]:
        out_folder = os.path.join(base_dir, folder)
        os.makedirs(out_folder, exist_ok=True)
        out_path = os.path.join(out_folder, dst_name)
        
        with wave.open(out_path, 'wb') as wf:
            wf.setnchannels(2)
            wf.setsampwidth(2)
            wf.setframerate(decoded.sample_rate)
            wf.writeframes(samples.tobytes())
            
        print(f"Wrote {out_path} ({os.path.getsize(out_path)} bytes, sr={decoded.sample_rate}, len={len(samples)/decoded.sample_rate:.2f}s)")

for src_name, (folder, dst_name, peak_ratio) in file_map.items():
    process_file(src_name, folder, dst_name, peak_ratio)

# Also create radar_ping_single.wav (single pulse 1.0s)
radar_src = os.path.join(audio_src_dir, "Radar_Sound_Effect.mp3")
d_radar = miniaudio.decode_file(radar_src)
r_samples = np.frombuffer(d_radar.samples, dtype=np.int16).astype(np.float64)
if d_radar.nchannels == 2:
    r_samples = r_samples.reshape(-1, 2)
else:
    r_samples = np.column_stack((r_samples, r_samples))

# Take exactly 1.0s (first pulse)
pulse_len = int(1.000 * d_radar.sample_rate)
single_pulse = r_samples[:pulse_len].copy()
# Apply gentle 20ms fade out at end of single pulse
fade_len = int(0.03 * d_radar.sample_rate)
fade = np.linspace(1.0, 0.0, fade_len)[:, None]
single_pulse[-fade_len:] *= fade

peak = np.max(np.abs(single_pulse))
if peak > 0:
    single_pulse = single_pulse * (32767.0 * 0.90 / peak)
single_pulse = np.clip(single_pulse, -32768, 32767).astype(np.int16)

for base_dir in [dest_audio_dir, dest_res_dir]:
    out_path = os.path.join(base_dir, "SFX", "radar_ping_single.wav")
    with wave.open(out_path, 'wb') as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(d_radar.sample_rate)
        wf.writeframes(single_pulse.tobytes())
    print(f"Wrote {out_path} ({os.path.getsize(out_path)} bytes)")
