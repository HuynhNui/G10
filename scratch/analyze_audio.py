import os
import glob
import miniaudio
import numpy as np
import wave

uploaded_dir = r"C:\Users\Duy\.gemini\antigravity\brain\37912308-2749-42bc-973d-154d2b4ce7de\.user_uploaded"
files = sorted(glob.glob(os.path.join(uploaded_dir, "uploaded_media_*")))

for i, f in enumerate(files):
    decoded = miniaudio.decode_file(f)
    samples = np.frombuffer(decoded.samples, dtype=np.int16)
    duration = len(samples) / (decoded.sample_rate * decoded.nchannels)
    print(f"File {i}: {os.path.basename(f)}")
    print(f"  Sample Rate: {decoded.sample_rate}, Channels: {decoded.nchannels}, Duration: {duration:.3f}s")
    
    # Calculate spectral features or energy profile
    if decoded.nchannels == 2:
        mono = samples.reshape(-1, 2).mean(axis=1)
    else:
        mono = samples
        
    peak = np.max(np.abs(mono))
    rms = np.sqrt(np.mean(mono.astype(np.float64)**2))
    print(f"  Peak: {peak}, RMS: {rms:.1f}")
    
    # Check frequency spectrum in first half vs second half to see if ascending/descending
    half = len(mono) // 2
    f_first = np.fft.rfft(mono[:half])
    f_second = np.fft.rfft(mono[half:half*2])
    freqs = np.fft.rfftfreq(half, 1.0 / decoded.sample_rate)
    
    centroid_1 = np.sum(freqs * np.abs(f_first)) / (np.sum(np.abs(f_first)) + 1e-9)
    centroid_2 = np.sum(freqs * np.abs(f_second)) / (np.sum(np.abs(f_second)) + 1e-9)
    print(f"  Centroid 1st half: {centroid_1:.0f} Hz -> 2nd half: {centroid_2:.0f} Hz")
