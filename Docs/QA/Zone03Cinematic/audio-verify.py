"""Offline verification of the produced Zone 3 package; no provider calls."""
import hashlib
import json
from pathlib import Path
import sys
import wave

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'Tools/Audio'))
import generate_zone03 as audio

EXPECTED = {
    'ambient_stone', 'stone_resonance_01', 'stone_resonance_02', 'stone_resonance_03',
    'engine_reflection', 'creature05_pressure', 'creature06_crystal_motion',
    'creature07_bioelectric', 'false_radar_echo', 'soft_movement', 'close_click',
    'rock_breaker_assembly', 'underwater_detonation', 'rock_collapse_01',
    'rock_collapse_02', 'rock_collapse_03', 'post_explosion_resonance', 'sparse_water'
}
base = audio.BASE
files = {p.stem: p for p in base.glob('*.wav')}
assert set(files) == EXPECTED, 'Missing/unexpected production cue'
checks = []
for name, path in sorted(files.items()):
    with wave.open(str(path), 'rb') as stream:
        assert stream.getnchannels() == 2 and stream.getframerate() == 44100 and stream.getsampwidth() == 2
    m = audio.metrics(audio.read_audio(path))
    assert m['clipped_samples'] == 0 and -50 < m['rms_dbfs'] < -18
    assert -23 <= m['peak_dbfs'] <= -3.99 and m['duration_seconds'] > .2
    if name in ('ambient_stone', 'sparse_water'):
        assert m['seam_step'] <= 1 / 32768, 'Loop boundary click risk'
    proof = json.loads(path.with_suffix('.provenance.json').read_text())
    assert proof['asset_sha256'] == hashlib.sha256(path.read_bytes()).hexdigest()
    for source in proof['sources']:
        assert source['sha256'] == hashlib.sha256((ROOT / source['path']).read_bytes()).hexdigest()
    checks.append(dict(id=name, passed=True, **m))
requests = [json.loads(p.read_text()) for p in audio.CANDIDATES.glob('*.request.json')]
assert len(requests) == 20
credits = sum(x['billed_credits_bound'] for x in requests)
assert credits <= 1800
assert sum(x['duration_requested_seconds'] for x in requests) * 40 <= 1800
direct = audio.metrics(audio.read_audio(ROOT/'Assets/_Project/Audio/Zone01/Machinery/ambient_propulsion_thrust.wav'))
reflection = next(c for c in checks if c['id'] == 'engine_reflection')
assert reflection['spectral_centroid_hz'] > direct['spectral_centroid_hz'] * 1.5
assert reflection['energy_below_250hz'] < direct['energy_below_250hz'] - .15
blast = next(c for c in checks if c['id'] == 'underwater_detonation')
assert blast['energy_below_250hz'] > .8 and blast['stereo_correlation'] > .8
result = dict(passed=True, production_wavs=len(files), generated_sources=len(requests),
              header_credits=credits, zero_clipping=True, seamless_loop_endpoints=True,
              source_and_asset_hashes_match=True, output='Stereo 44100Hz PCM16',
              reflection_direct_spectral_comparison=dict(direct=direct, reflected=reflection),
              checks=checks,
              limitation='File, waveform and spectral verification only. Human listening and gameplay mix evaluation remain required; no automated result certifies material perception or dramatic quality.')
audio.save_json(Path(__file__).with_name('audio-verification.json'), result)
print(json.dumps({k:v for k,v in result.items() if k not in ('checks','reflection_direct_spectral_comparison')}))
