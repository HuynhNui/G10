import math
import struct
import wave
import os
import random

SAMPLE_RATE = 44100

def write_wav(filepath, samples, sample_rate=SAMPLE_RATE):
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
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

def gen_radar_ping():
    # Crisp, bright dual-ping "ting... ting..." submarine sonar (duration ~0.65s)
    # Perfect for repeating at 1.0s interval with clean silent breath between pings.
    duration = 0.65
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples

    # Ping 1 at t=0.0s (First "ting"): fundamental 1760Hz (A6)
    # Ping 2 at t=0.14s (Second "ting"): higher fundamental 2217Hz (C#7)
    pings = [
        (0.00, 1760.0, 0.35, 1.0),
        (0.14, 2217.4, 0.45, 0.88),
    ]

    for start_t, f_base, decay_time, amp in pings:
        start_idx = int(start_t * SAMPLE_RATE)
        for i in range(start_idx, n_samples):
            t = (i - start_idx) / SAMPLE_RATE
            # Sharp 2ms crystalline strike attack, smooth exponential decay
            env = min(1.0, t / 0.002) * math.exp(-t / decay_time)
            # Slight pitch drop at instant of transducer pulse
            p = f_base - 20.0 * math.exp(-t / 0.015)
            # Pure sine tone
            tone = math.sin(2 * math.pi * p * t) * env
            # 2nd harmonic for crisp "ping" sparkle
            h2 = 0.35 * math.sin(2 * math.pi * (p * 2.0) * t) * (min(1.0, t / 0.002) * math.exp(-t / (decay_time * 0.6)))
            # 3rd harmonic
            h3 = 0.15 * math.sin(2 * math.pi * (p * 3.0) * t) * (min(1.0, t / 0.002) * math.exp(-t / (decay_time * 0.4)))
            
            # Submarine hull acoustic thud (90Hz)
            hull = 0.25 * math.sin(2 * math.pi * 90 * t) * math.exp(-t / 0.08)

            samples[i] += (tone + h2 + h3 + hull) * amp

    # Subtle underwater reverberation tail
    reverb_delay = int(0.08 * SAMPLE_RATE)
    for i in range(reverb_delay, n_samples):
        samples[i] += samples[i - reverb_delay] * 0.18

    return samples

def gen_camera_shutter():
    # Realistic mechanical SLR camera shutter sound (duration ~0.28s)
    # 1. Aperture lever tick at t=0.0s
    # 2. Mirror flip and focal plane shutter slap at t=0.035s
    # 3. Shutter curtain close and spring reset at t=0.10s
    duration = 0.28
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    rng = random.Random(101)

    for i in range(n_samples):
        t = i / SAMPLE_RATE
        sig = 0.0

        # Part 1: Aperture latch click (t=0 to 0.02)
        if t < 0.025:
            noise1 = rng.uniform(-1, 1) * math.exp(-t / 0.004)
            click1 = math.sin(2 * math.pi * 2800 * t) * math.exp(-t / 0.006)
            sig += 0.4 * noise1 + 0.5 * click1

        # Part 2: Main shutter curtain travel and snap (t=0.035)
        if t >= 0.035:
            dt = t - 0.035
            noise2 = rng.uniform(-1, 1) * math.exp(-dt / 0.008)
            snap1 = math.sin(2 * math.pi * 1450 * dt) * math.exp(-dt / 0.018)
            thud1 = math.sin(2 * math.pi * 520 * dt) * math.exp(-dt / 0.025)
            sig += 0.8 * snap1 + 0.6 * thud1 + 0.5 * noise2

        # Part 3: Shutter blade close & spring return (t=0.095)
        if t >= 0.095:
            dt2 = t - 0.095
            noise3 = rng.uniform(-1, 1) * math.exp(-dt2 / 0.005)
            snap2 = math.sin(2 * math.pi * 1950 * dt2) * math.exp(-dt2 / 0.015)
            metallic = 0.4 * math.sin(2 * math.pi * 3400 * dt2) * math.exp(-dt2 / 0.012)
            sig += 0.7 * snap2 + metallic + 0.4 * noise3

        # Part 4: Light gear motor rewind click (t=0.17 to 0.24)
        if 0.16 <= t <= 0.25:
            dt3 = t - 0.16
            whir = 0.25 * math.sin(2 * math.pi * 880 * dt3) * math.sin(2 * math.pi * 35 * dt3) * math.exp(-dt3 / 0.06)
            sig += whir

        samples[i] = sig

    return samples

def gen_zip_open():
    # Zipper opening sound (duration ~0.32s)
    # Rapid sequence of teeth friction clicks with rising pitch + slider metal clink
    duration = 0.32
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    rng = random.Random(202)

    # 32 zipper teeth clicks spaced across 0.01s to 0.26s
    n_teeth = 34
    for tooth in range(n_teeth):
        frac = tooth / n_teeth
        t_click = 0.01 + frac * 0.23
        idx = int(t_click * SAMPLE_RATE)
        # Pitch rises as zip opens from bottom to top: 900Hz -> 2300Hz
        freq = 900 + 1400 * frac
        
        click_len = int(0.012 * SAMPLE_RATE)
        for j in range(click_len):
            cur = idx + j
            if cur >= n_samples: break
            tj = j / SAMPLE_RATE
            noise = rng.uniform(-1, 1) * math.exp(-tj / 0.002)
            tone = math.sin(2 * math.pi * freq * tj) * math.exp(-tj / 0.004)
            samples[cur] += (0.45 * noise + 0.55 * tone) * 0.8

    # Metal pull-tab clink at end (t=0.25)
    t_end = int(0.25 * SAMPLE_RATE)
    for j in range(int(0.06 * SAMPLE_RATE)):
        cur = t_end + j
        if cur >= n_samples: break
        tj = j / SAMPLE_RATE
        clink = math.sin(2 * math.pi * 3200 * tj) * math.exp(-tj / 0.015) + \
                0.5 * math.sin(2 * math.pi * 4800 * tj) * math.exp(-tj / 0.008)
        samples[cur] += clink * 0.4

    return samples

def gen_zip_close():
    # Zipper closing sound (duration ~0.32s)
    # Rapid sequence of teeth friction clicks with falling pitch + solid snap closure
    duration = 0.32
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    rng = random.Random(303)

    # 34 zipper teeth clicks spaced across 0.01s to 0.25s
    n_teeth = 34
    for tooth in range(n_teeth):
        frac = tooth / n_teeth
        t_click = 0.01 + frac * 0.23
        idx = int(t_click * SAMPLE_RATE)
        # Pitch falls as zip closes from top to bottom: 2300Hz -> 850Hz
        freq = 2300 - 1450 * frac
        
        click_len = int(0.012 * SAMPLE_RATE)
        for j in range(click_len):
            cur = idx + j
            if cur >= n_samples: break
            tj = j / SAMPLE_RATE
            noise = rng.uniform(-1, 1) * math.exp(-tj / 0.002)
            tone = math.sin(2 * math.pi * freq * tj) * math.exp(-tj / 0.004)
            samples[cur] += (0.45 * noise + 0.55 * tone) * 0.8

    # Solid stop latch snap at end (t=0.25)
    t_end = int(0.25 * SAMPLE_RATE)
    for j in range(int(0.06 * SAMPLE_RATE)):
        cur = t_end + j
        if cur >= n_samples: break
        tj = j / SAMPLE_RATE
        thud = math.sin(2 * math.pi * 620 * tj) * math.exp(-tj / 0.02) + \
               0.6 * math.sin(2 * math.pi * 1400 * tj) * math.exp(-tj / 0.01)
        samples[cur] += thud * 0.65

    return samples

def gen_underwater_diving_ambient():
    # Deep underwater diving / submersible ambiance (duration 4.0s seamless loop)
    # Muffled ocean pressure resonance, hydrostatic hum, gentle submerged bubbles.
    # Completely free of surface wave slapping or ocean surf sounds.
    duration = 4.0
    n_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * n_samples
    rng = random.Random(404)

    # 1. Deep hydrostatic sub-bass pressure tones (52Hz, 104Hz, 156Hz)
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Deep submarine pressure hum
        h1 = 0.45 * math.sin(2 * math.pi * 52 * t)
        h2 = 0.25 * math.sin(2 * math.pi * 104 * t)
        h3 = 0.12 * math.sin(2 * math.pi * 156 * t)
        # Slow ocean pressure breathing swell (0.25Hz = 4s period, perfectly matches loop length!)
        swell = 0.85 + 0.15 * math.sin(2 * math.pi * 0.25 * t)
        # Deep ocean muffled background rumble (filtered white noise)
        samples[i] = (h1 + h2 + h3) * swell

    # 2. Submerged air bubbles (Minnaert resonant bubble chirps: bloop... glug)
    # Each bubble starts at frequency f0 and sweeps up slightly, decaying rapidly.
    bubble_events = [
        (0.42, 380.0, 0.065, 0.25),
        (0.85, 490.0, 0.055, 0.20),
        (1.55, 340.0, 0.080, 0.28),
        (2.10, 560.0, 0.050, 0.18),
        (2.80, 420.0, 0.070, 0.24),
        (3.45, 510.0, 0.055, 0.22),
    ]

    for b_time, f0, b_dur, b_amp in bubble_events:
        start_idx = int(b_time * SAMPLE_RATE)
        b_len = int(b_dur * SAMPLE_RATE)
        for j in range(b_len):
            idx = start_idx + j
            if idx >= n_samples: break
            tj = j / SAMPLE_RATE
            # Minnaert bubble frequency rises slightly with volume reduction
            f_bub = f0 * (1.0 + 0.15 * (tj / b_dur))
            env_bub = math.sin(math.pi * min(1.0, tj / (b_dur * 0.3))) * math.exp(-tj / (b_dur * 0.45))
            bubble_tone = math.sin(2 * math.pi * f_bub * tj) * env_bub
            samples[idx] += bubble_tone * b_amp

    # 3. Seamless loop windowing (200ms crossfade at edges to prevent any click)
    fade_samples = int(SAMPLE_RATE * 0.2)
    for i in range(fade_samples):
        w = i / fade_samples
        # blend end into start
        samples[i] = samples[i] * w + samples[n_samples - fade_samples + i] * (1.0 - w)
        samples[n_samples - fade_samples + i] = samples[i]

    return samples

def main():
    print("Synthesizing new specialized sound effects...")
    
    # 1. Radar Ping ("Ting Ting" sonar)
    radar = gen_radar_ping()
    write_wav("Assets/_Project/Audio/SFX/radar_ping.wav", radar)
    write_wav("Assets/_Project/Resources/Audio/SFX/radar_ping.wav", radar)

    # 2. Camera Shutter (Mechanical photo snapshot)
    shutter = gen_camera_shutter()
    write_wav("Assets/_Project/Audio/SFX/camera_shutter.wav", shutter)
    write_wav("Assets/_Project/Resources/Audio/SFX/camera_shutter.wav", shutter)

    # 3. Zip Open (Backpack unzipping)
    zip_open = gen_zip_open()
    write_wav("Assets/_Project/Audio/SFX/zip_open.wav", zip_open)
    write_wav("Assets/_Project/Resources/Audio/SFX/zip_open.wav", zip_open)

    # 4. Zip Close (Backpack zipping shut)
    zip_close = gen_zip_close()
    write_wav("Assets/_Project/Audio/SFX/zip_close.wav", zip_close)
    write_wav("Assets/_Project/Resources/Audio/SFX/zip_close.wav", zip_close)

    # 5. Underwater Diving Ambient (Deep sea pressure + submerged bubbles, NO surface waves)
    underwater = gen_underwater_diving_ambient()
    write_wav("Assets/_Project/Audio/Ambient/ambient_submarine_loop.wav", underwater)
    write_wav("Assets/_Project/Resources/Audio/Ambient/ambient_submarine_loop.wav", underwater)

    print("All audio files synthesized successfully!")

if __name__ == "__main__":
    main()
