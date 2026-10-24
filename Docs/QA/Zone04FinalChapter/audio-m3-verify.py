"""Independent offline checks of the Zone 4 produced library and shared ledger."""
import hashlib
import json
from pathlib import Path
import sys
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'Tools/Audio'))
import generate_zone04 as audio

EXPECTED = {'fragment_recovery','abyss_sparse_water','distant_structure','hidden_signal_a',
            'hidden_signal_b','final_signal','normal_ending_settle','hidden_ending_tail'}
paths = {p.stem:p for p in audio.BASE.glob('*.wav')}
assert set(paths) == EXPECTED
rows = []
decoded = {}
for name,path in sorted(paths.items()):
    with wave.open(str(path)) as stream:
        assert (stream.getnchannels(),stream.getsampwidth(),stream.getframerate()) == (2,2,44100)
        x=np.frombuffer(stream.readframes(stream.getnframes()),'<i2').reshape(-1,2).astype(float)/32768
    assert np.isfinite(x).all() and float(np.max(np.abs(x))) > .001
    assert np.count_nonzero(np.abs(x)>=1) == 0
    proof=json.loads(path.with_suffix('.provenance.json').read_text())
    assert audio.sha(path) == proof['asset_sha256']
    sources=proof.get('sources') or [dict(path=proof['source'],sha256=proof['source_sha256'])]
    for source in sources+proof.get('additional_sources',[]):
        assert audio.sha(ROOT/source['path']) == source['sha256']
    features=audio.features(x)
    assert features['peak_dbfs'] <= -12.99 and features['rms_dbfs'] > -58
    rows.append(dict(id=name,passed=True,bytes=path.stat().st_size,**features))
    decoded[name]=x

assert float(np.max(np.abs(decoded['abyss_sparse_water'][0]-decoded['abyss_sparse_water'][-1]))) == 0
assert np.max(np.abs(decoded['hidden_signal_a'][round(1.8*44100):round(5.3*44100)])) == 0
assert np.max(np.abs(decoded['hidden_signal_b'][round(.6*44100):round(3.5*44100)])) == 0
assert np.max(np.abs(decoded['hidden_signal_b'][round(4.8*44100):round(7.1*44100)])) == 0
assert audio.metrics(decoded['hidden_signal_b'][round(.25*44100):round(.48*44100)])['energy_above_2000hz'] > .5
assert audio.metrics(decoded['hidden_signal_b'][round(3.55*44100):round(4.6*44100)])['energy_below_250hz'] > .8
assert audio.metrics(decoded['final_signal'])['energy_below_250hz'] < .8

value=json.loads(audio.LEDGER.read_text())
assert len(value['requests']) == 13 <= 14
assert sum(r['requested_seconds'] for r in value['requests']) == 54.5 <= 60
assert audio.charged(value) == 590 <= 2400
assert sum(int(r.get('billed_credits_header',0)) for r in value['requests']) == 530
assert sum(r['milestone']=='M3' for r in value['requests']) == 10
assert all(r['status']=='received' for r in value['requests'] if r['milestone']=='M3')
assert value['m2_vertical_slice_review']['completed']
for name,seconds,prompt in audio.M3:
    assert len(prompt) <= 450
    receipt=json.loads((audio.CANDIDATES/(name+'.request.json')).read_text())
    assert receipt['prompt']==prompt and receipt['source_sha256']==audio.sha(audio.CANDIDATES/(name+'.mp3'))

result=dict(passed=True,production_wavs=len(paths),production_wav_bytes=sum(p.stat().st_size for p in paths.values()),
    original_m3_requests=10,original_m3_seconds=50,m3_actual_header_credits=500,
    cumulative_actual_header_credits=530,failed_attempt_reservation=60,cumulative_spent_or_reserved=590,
    cumulative_submitted_attempts=13,cumulative_requested_seconds=54.5,
    fragment_matches_m2_hash=audio.sha(paths['fragment_recovery'])=='a2f5dc85e5ae605b568cc6f76cbabf46fe3f1463a80a78220e77da9198c65f39',
    source_and_output_hashes_match=True,all_pcm16_stereo_44100=True,all_nonzero=True,
    zero_saturated_pcm_samples=True,abyss_loop_endpoints_equal=True,hidden_long_quiet_gaps_verified=True,
    hidden_b_separate_high_transients_low_motion_verified=True,final_upper_signature_energy_verified=True,
    assets=rows,limitation='Offline source/file/waveform checks only; native Unity import/playback/mix and human aesthetic evaluation are separate.')
assert result['fragment_matches_m2_hash']
audio.save(audio.QA/'audio-m3-verification.json',result)
print(json.dumps({k:v for k,v in result.items() if k!='assets'}))
