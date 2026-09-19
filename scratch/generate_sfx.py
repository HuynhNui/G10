import math
import struct
import wave
import os

SAMPLE_RATE = 44100

def write_wav(filepath, samples, sample_rate=SAMPLE_RATE):
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    # Normalize peak to 0.85 (-1.4 dB)
    max_val = max(abs(s) for s in samples) if samples else 1.0
    if max_val < 1e-6:
        max_val = 1.0
    scale = 0.85 / max_val
    
    with wave.open(filepath, 'w') as wav:
        wav.setnchannels(1)  # Mono
        wav.setsampwidth(2)  # 16-bit
        wav.setframerate(sample_rate)
        raw = bytearray()
        for s in samples:
            val = int(max(-1.0, min(1.0, s * scale)) * 32767.0)
            raw.extend(struct.pack('<h', val))
        wav.writeframes(raw)
    print(f"Generated: {filepath} ({len(samples)} samples, {len(samples)/sample_rate:.2f}s)")

def gen_button_click():
    # Crisp mechanical switch click (duration ~0.05s)
    duration = 0.055
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    import random
    rng = random.Random(42)
    
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Fast transient noise burst in first 4ms
        noise = (rng.uniform(-1, 1)) * math.exp(-t / 0.003) if t < 0.015 else 0.0
        # Dual resonant mechanical switch tone: 1400Hz + 850Hz snap
        snap = math.sin(2 * math.pi * 1400 * t) * math.exp(-t / 0.012)
        thud = math.sin(2 * math.pi * 850 * t) * math.exp(-t / 0.025)
        # Upper metallic ring (2800Hz)
        metallic = 0.3 * math.sin(2 * math.pi * 2800 * t) * math.exp(-t / 0.008)
        samples[i] = 0.5 * noise + 0.6 * snap + 0.5 * thud + metallic
    return samples

def gen_button_back():
    # Softer, lower pitched mechanical release (duration ~0.07s)
    duration = 0.075
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Downward pitch slide 680Hz -> 420Hz
        freq = 680 - 260 * (t / duration)
        tone = math.sin(2 * math.pi * freq * t) * math.exp(-t / 0.028)
        sub = 0.4 * math.sin(2 * math.pi * (freq * 0.5) * t) * math.exp(-t / 0.035)
        samples[i] = tone + sub
    return samples

def gen_item_click():
    # Crystal / glass specimen container chime (duration ~0.25s)
    duration = 0.26
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Fast bubble transient (1200 -> 1600Hz in first 8ms)
        f_init = 1200 + 400 * min(1.0, t / 0.008)
        chirp = math.sin(2 * math.pi * f_init * t) * math.exp(-t / 0.015)
        # Harmonic chimes: 1568Hz (G6) + 2349Hz (D7) + 3136Hz (G7)
        bell1 = math.sin(2 * math.pi * 1567.98 * t) * math.exp(-t / 0.07)
        bell2 = 0.6 * math.sin(2 * math.pi * 2349.32 * t) * math.exp(-t / 0.05)
        sparkle = 0.3 * math.sin(2 * math.pi * 3135.96 * t) * math.exp(-t / 0.03)
        samples[i] = 0.35 * chirp + 0.7 * bell1 + bell2 + sparkle
    return samples

def gen_radar_ping():
    # Authentic submarine sonar ping with underwater acoustics and echoes (duration ~1.5s)
    duration = 1.5
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    
    # Submarine acoustic parameters
    f_ping = 915.0  # classic naval sonar ping frequency
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Direct active sonar ping: rapid 6ms attack, long ringing exponential decay
        env_direct = min(1.0, t / 0.006) * math.exp(-t / 0.28)
        # Slight pitch drift (transducer discharge)
        pitch = f_ping - 15.0 * math.exp(-t / 0.08)
        direct = math.sin(2 * math.pi * pitch * t) * env_direct
        # 2nd harmonic
        direct_h2 = 0.25 * math.sin(2 * math.pi * (pitch * 2.0) * t) * (min(1.0, t / 0.005) * math.exp(-t / 0.15))
        
        # Deep submarine hull resonance (95Hz and 190Hz sub-bass thump)
        hull = 0.4 * math.sin(2 * math.pi * 95 * t) * math.exp(-t / 0.18) + \
               0.2 * math.sin(2 * math.pi * 190 * t) * math.exp(-t / 0.12)
        
        # Echo 1 at 0.17s (underwater cave / seafloor bounce)
        t_echo1 = t - 0.17
        echo1 = 0.0
        if t_echo1 > 0:
            env_e1 = min(1.0, t_echo1 / 0.015) * math.exp(-t_echo1 / 0.32)
            echo1 = 0.4 * math.sin(2 * math.pi * (f_ping * 0.99) * t_echo1) * env_e1
            
        # Echo 2 at 0.38s (distant oceanic return)
        t_echo2 = t - 0.38
        echo2 = 0.0
        if t_echo2 > 0:
            env_e2 = min(1.0, t_echo2 / 0.02) * math.exp(-t_echo2 / 0.4)
            echo2 = 0.22 * math.sin(2 * math.pi * (f_ping * 0.985) * t_echo2) * env_e2
            
        # Distant low reverberation tail
        tail_env = max(0.0, math.exp(-t / 0.45) - math.exp(-t / 0.05))
        tail = 0.15 * math.sin(2 * math.pi * (f_ping * 0.5) * t) * tail_env
        
        samples[i] = direct + direct_h2 + hull + echo1 + echo2 + tail
    return samples

def gen_capture_grab():
    # Mechanical claw / harpoon launcher deploy and clamp (duration ~0.45s)
    duration = 0.45
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    import random
    rng = random.Random(123)
    
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Phase 1: Pneumatic burst / water surge (0 - 0.12s)
        surge = 0.0
        if t < 0.15:
            noise = rng.uniform(-1, 1)
            surge_env = min(1.0, t / 0.01) * math.exp(-t / 0.04)
            surge = 0.5 * noise * surge_env
            
        # Phase 2: Mechanical clamp strike (at t = 0.04s and t = 0.08s)
        clamp = 0.0
        if t >= 0.04:
            t_c1 = t - 0.04
            clamp += 0.8 * math.sin(2 * math.pi * 320 * t_c1) * math.exp(-t_c1 / 0.025)
            clamp += 0.5 * math.sin(2 * math.pi * 720 * t_c1) * math.exp(-t_c1 / 0.015)
        if t >= 0.08:
            t_c2 = t - 0.08
            clamp += 0.6 * math.sin(2 * math.pi * 240 * t_c2) * math.exp(-t_c2 / 0.035)
            clamp += 0.4 * math.sin(2 * math.pi * 540 * t_c2) * math.exp(-t_c2 / 0.02)
            
        # Phase 3: Cable winch / ratchet clicks (at 0.14s, 0.20s, 0.26s, 0.32s)
        ratchet = 0.0
        for click_t in [0.14, 0.20, 0.26, 0.32]:
            dt = t - click_t
            if 0 <= dt < 0.03:
                ratchet += 0.45 * math.sin(2 * math.pi * 1850 * dt) * math.exp(-dt / 0.005)
                
        samples[i] = surge + clamp + ratchet
    return samples

def gen_capture_success():
    # Rewarding discovery chime / fanfare (C5 -> E5 -> G5 -> C6) (duration ~1.2s)
    duration = 1.2
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    
    notes = [
        (0.00, 523.25, 0.4),   # C5
        (0.12, 659.25, 0.45),  # E5
        (0.24, 783.99, 0.5),   # G5
        (0.36, 1046.50, 0.8),  # C6 (held long)
    ]
    
    for note_start, freq, sustain in notes:
        for i in range(n_samples):
            t = i / SAMPLE_RATE
            dt = t - note_start
            if dt >= 0:
                env = min(1.0, dt / 0.008) * math.exp(-dt / sustain)
                tone = math.sin(2 * math.pi * freq * dt) * env
                # Harmonic overtone
                overtone = 0.35 * math.sin(2 * math.pi * (freq * 2.0) * dt) * (min(1.0, dt / 0.006) * math.exp(-dt / (sustain * 0.6)))
                # Shimmer on final C6
                shimmer = 0.0
                if freq > 1000:
                    shimmer = 0.2 * math.sin(2 * math.pi * (freq * 3.0) * dt) * (min(1.0, dt / 0.005) * math.exp(-dt / 0.4))
                samples[i] += tone + overtone + shimmer
    return samples

def gen_capture_fail():
    # Hollow claw slip / bubble flutter (duration ~0.45s)
    duration = 0.45
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Pitch slide down 340 -> 140Hz
        freq = 340 - 200 * (t / duration)
        # Bubble flutter (16Hz modulation)
        mod = 0.7 + 0.3 * math.sin(2 * math.pi * 16 * t)
        tone = math.sin(2 * math.pi * freq * t) * math.exp(-t / 0.22) * mod
        # Hollow clang
        clang = 0.4 * math.sin(2 * math.pi * 420 * t) * math.exp(-t / 0.06)
        samples[i] = tone + clang
    return samples

def gen_ui_pause():
    # Submarine HUD pause ambient chord cue (duration ~0.5s)
    duration = 0.5
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        env = min(1.0, t / 0.035) * math.exp(-t / 0.22)
        tone1 = math.sin(2 * math.pi * 440 * t) * env
        tone2 = 0.7 * math.sin(2 * math.pi * 659.25 * t) * env
        samples[i] = 0.6 * tone1 + 0.5 * tone2
    return samples

def gen_ambient_submarine():
    # Seamless looping submarine hull drone and ocean deeps (~4.0s)
    duration = 4.0
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    import random
    rng = random.Random(789)
    noise_buffer = [rng.uniform(-1, 1) for _ in range(n_samples)]
    
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Steady engine hum 58Hz and 116Hz
        hum1 = 0.45 * math.sin(2 * math.pi * 58.0 * t)
        hum2 = 0.25 * math.sin(2 * math.pi * 116.0 * t)
        hum3 = 0.10 * math.sin(2 * math.pi * 174.0 * t)
        
        # Oceanic gentle ebb (slow 0.25Hz sine modulation of noise)
        wave_mod = 0.5 + 0.5 * math.sin(2 * math.pi * 0.25 * t)
        # Simple low-pass filtered noise
        idx = i
        filtered_noise = (noise_buffer[idx] + noise_buffer[(idx - 1) % n_samples] + noise_buffer[(idx - 2) % n_samples]) / 3.0
        ocean = 0.20 * filtered_noise * wave_mod
        
        samples[i] = hum1 + hum2 + hum3 + ocean
        
    # Ensure seamless looping by crossfading first 0.2s with last 0.2s
    fade_len = int(SAMPLE_RATE * 0.2)
    for i in range(fade_len):
        w = i / fade_len
        # Crossfade
        samples[i] = samples[i] * w + samples[n_samples - fade_len + i] * (1.0 - w)
        samples[n_samples - fade_len + i] = samples[i]
        
    return samples

def create_meta_file(wav_path, guid, is_ambient=False):
    meta_path = wav_path + '.meta'
    load_type = 2 if is_ambient else 0  # Streaming for ambient, DecompressOnLoad for SFX
    meta_content = f'''fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 6
  defaultSettings:
    loadType: {load_type}
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 0
    quality: 1
    conversionMode: 0
  customSampleRateSetting: 0
  customSampleRateOverride: 44100
  customCompressionFormat: 0
  customQuality: 1
  customConversionMode: 0
  forceToMono: 0
  normalize: 1
  preloadAudioData: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
'''
    with open(meta_path, 'w', encoding='utf-8') as f:
        f.write(meta_content)
    print(f"Created meta: {meta_path} [GUID: {guid}]")

if __name__ == '__main__':
    sfx_dir = r'Assets\_Project\Audio\SFX'
    ambient_dir = r'Assets\_Project\Audio\Ambient'
    
    # GUID map for determinism
    audio_assets = [
        (os.path.join(sfx_dir, 'ui_button_click.wav'), gen_button_click, 'a1000000000000000000000000000001', False),
        (os.path.join(sfx_dir, 'ui_button_back.wav'), gen_button_back, 'a1000000000000000000000000000002', False),
        (os.path.join(sfx_dir, 'ui_item_click.wav'), gen_item_click, 'a1000000000000000000000000000003', False),
        (os.path.join(sfx_dir, 'radar_ping.wav'), gen_radar_ping, 'a1000000000000000000000000000004', False),
        (os.path.join(sfx_dir, 'capture_grab.wav'), gen_capture_grab, 'a1000000000000000000000000000005', False),
        (os.path.join(sfx_dir, 'capture_success.wav'), gen_capture_success, 'a1000000000000000000000000000006', False),
        (os.path.join(sfx_dir, 'capture_fail.wav'), gen_capture_fail, 'a1000000000000000000000000000007', False),
        (os.path.join(sfx_dir, 'ui_pause.wav'), gen_ui_pause, 'a1000000000000000000000000000008', False),
        (os.path.join(ambient_dir, 'ambient_submarine_loop.wav'), gen_ambient_submarine, 'a1000000000000000000000000000009', True),
    ]
    
    for filepath, generator, guid, is_ambient in audio_assets:
        samples = generator()
        write_wav(filepath, samples)
        create_meta_file(filepath, guid, is_ambient)
        
    print("\nAll SFX and Ambient WAV files and .meta files generated successfully!")
