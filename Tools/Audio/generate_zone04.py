"""Zone 4 staged audio production with one shared bounded authorization ledger.

Human-authorized aggregate ceiling: 14 requests, 60 requested seconds and 2400
existing included credits across all Zone 4 milestones, with no top-up/overage.
M3 is exposed only after explicit completed-M2-review acknowledgement. There are
no automatic paid retries, no top-up, and no runtime game generation.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import urllib.error
import wave

import numpy as np

from generate_sound import ROOT, request
from generate_terrain_impact import credentials
from generate_zone03 import decoder, filter_band, level, metrics, ramps, trim_onset, write_wav

BASE = ROOT / 'Assets/_Project/Audio/Horror/Zone04'
QA = ROOT / 'Docs/QA/Zone04FinalChapter'
CANDIDATES = QA / 'audio-candidates'
LEDGER = QA / 'audio-ledger.json'
MAX_CALLS, MAX_SECONDS, MAX_CREDITS, RATE_BOUND = 14, 60, 2400, 40
PRICE_SOURCE = 'https://elevenlabs.io/docs/overview/capabilities/sound-effects'
LICENSE_SOURCE = 'https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform'
SR = 44100
M2 = [
    ('fragment_recovery_packet', 1.5,
     'One quiet practical submarine data recovery: a soft miniature locking click, two '
     'short unequal dry digital packet ticks, then a tiny damped relay settle. Useful '
     'specimen coordinate data decoded by ordinary scientific equipment. Close dry '
     'instrument foley, immediate gentle onset and short decay. No melody, chime, alarm, '
     'static blast, magic, horror, impact, music, voice, breathing, ocean or engine bed.'),
    ('fragment_recovery_relay', 1.5,
     'One quiet scientific data extraction: a tiny dry contact rattle, a brief narrow '
     'filtered electronic read chirr with one deliberate interruption, then a soft '
     'mechanical relay tick. Trustworthy research equipment recovers a coordinate '
     'fragment. Close cabin foley, gentle onset, distinct stages then silence. No regular '
     'beeps, melody, chime, alarm, glitch storm, magic, music, voice, breathing, ocean or engine bed.')
]
assert all(.5 <= seconds <= 30 and len(prompt) <= 450 for _, seconds, prompt in M2)

M3 = [
    ('abyss_sparse_water_a', 8,
     'Almost empty deep ocean, one faint diffuse sheet of water moves softly then a long quiet '
     'gap, later a different small distant pressure motion. Huge airless space, tiny irregular '
     'physical detail, gentle starts and fades. No steady bass drone, pulse, melody, music, '
     'voice, breathing, monster, metal, stone ring, bubbles, waves, engine or impact.'),
    ('abyss_sparse_water_b', 8,
     'Extremely sparse abyss water texture: a distant broad soft water grain thins away, several '
     'seconds of near stillness, then a very low gentle lateral water fold. Unequal motions and '
     'large empty intervals, restrained natural pressure. No continuous drone, rhythm, wind, '
     'surf, bubbles, machinery, stone resonance, creature, music or voice.'),
    ('distant_structure_a', 3,
     'One extremely faint resonance from an immense unidentified flooded hollow structure far '
     'away: a soft nonpercussive fluid pressure swell slowly bends and recedes, diffuse and '
     'airless with a thin irregular upper edge. Not a struck object. No stone or bell ring, '
     'steel hull creak, wood scrape, animal growl, monster, musical drone, voice or impact.'),
    ('distant_structure_b', 3,
     'Very distant enormous submerged enclosure, one soft hollow compression gently unfolds '
     'through water, a fine diffuse upper texture drifts out of alignment and vanishes. '
     'Unidentified physical scale, no sharp onset. No mineral ring, metal strike, root creak, '
     'propeller echo, radar ping, breath, growl, monster, musical note, music or voice.'),
    ('hidden_signal_a_fold', 5,
     'An unfamiliar quiet underwater resonance: one soft narrow fluid cavity fold drifts and '
     'ends, a long empty gap, then a weaker irregular fold with a fine wavering edge. '
     'Airless intimate material with no clear source. No note sequence, singing, metallic '
     'or stone ring, radar ping, beat, steady drone, voice, growl, music or impact.'),
    ('hidden_signal_a_aperture', 5,
     'Two separated rounded refractive rustles with slow inharmonic resonance and a long '
     'unequal quiet pause. Thin soft layers seem to drift through a narrow acoustic aperture, '
     'then stop completely. Faint airless underwater material. No singing, musical notes, '
     'metal, stone, radio, sonar, breath, monster, constant drone, pulse or music.'),
    ('hidden_signal_b_film', 5,
     'Two isolated microscopic water-film ticks, fine short high-frequency transients with '
     'long empty gaps; a separate very low soft lateral water movement emerges much later. '
     'Different high and low physical events, not a pattern. No regular pings, glass chime, '
     'electrical arc, static blast, hull knock, scrape, stone ring, creature, music or voice.'),
    ('hidden_signal_b_drift', 5,
     'One delicate short brittle water-skin tick near one side, long quiet space, then a muted '
     'low water roll far on the other side and a much weaker tiny tick after an unequal pause. '
     'Faint separate transients and slow movement. No note sequence, sonar, electric fizz, '
     'metal, stone, knock, breathing, growl, drone, music or speech.'),
    ('final_signal_thread', 4,
     'One minimal unfamiliar subaquatic signature: a thin soft airy thread emerges from quiet, '
     'twists around one muted low fluid fold, both disappear, a tiny uneven residual texture '
     'ends alone. Quiet and emotionally ambiguous, delicate unidentifiable physical sound. '
     'No alarm, sonar ping, melody, triumphant chord, monster, scream, impact, music or voice.'),
    ('final_signal_fold', 4,
     'One sparse asymmetric pair of soft inward pressure folds; a very thin frayed upper edge '
     'briefly slips out of alignment and then complete quiet. Strange delicate underwater '
     'physical evidence, memorable but restrained, no clear creature identity. No jump-scare '
     'stinger, bell, sonar, drum, chord, bass drone, impact, breathing, music or voice.')
]
assert len(M3) == 10 and sum(s for _, s, _ in M3) == 50
assert all(.5 <= seconds <= 30 and len(prompt) <= 450 for _, seconds, prompt in M3)


def now():
    return datetime.now(timezone.utc).isoformat()


def rel(path):
    return str(path.relative_to(ROOT)).replace('\\', '/')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + '.pending')
    temporary.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')
    temporary.replace(path)


def ledger():
    if LEDGER.exists():
        value = json.loads(LEDGER.read_text())
        auth = value['authorization']
        if (auth['max_requests'], auth['max_requested_seconds'], auth['max_included_credits']) != (MAX_CALLS, MAX_SECONDS, MAX_CREDITS):
            raise RuntimeError('Existing authorization differs; refusing to alter its scope')
        return value
    return dict(schema_version=1, authorization=dict(
        approved=True, recorded_at_utc=now(),
        source='Human reply in the current conversation, confirmed by root delegation on 2026-10-23.',
        scope='All Zone 4 audio milestones combined; existing included credits only. M2 currently authorizes only the two fragment candidates.',
        max_requests=MAX_CALLS, max_requested_seconds=MAX_SECONDS,
        max_included_credits=MAX_CREDITS, topup_allowed=False, overage_allowed=False,
        wider_generation_gate='Do not generate wider Zone 4 library until root explicitly confirms M2 vertical-slice review is complete.'),
        requests=[])


def charged(value):
    # An in-flight or failed request remains conservatively reserved. It is never
    # automatically retried or treated as free after an ambiguous provider result.
    return sum(row.get('billed_credits_bound', row['reserved_credits']) for row in value['requests'])


def subscription(key):
    with request('/v1/user/subscription', key) as response:
        raw = json.load(response)
    return raw.get('tier', 'unknown'), raw.get('character_limit', 0) - raw.get('character_count', 0)


def execute_m2(corrected_manual_submission=False):
    value = ledger(); key = credentials(); plan, remaining = subscription(key)
    unresolved = [row for row in value['requests'] if row['status'] in ('submitted', 'unresolved')]
    if corrected_manual_submission:
        # One explicit root authorization reconciles the already observed initial
        # HTTP400; it does not enable future automatic retries after a new failure.
        if value.get('m2_manual_correction_authorization') or len(unresolved) != 1 or unresolved[0]['id'] != M2[0][0]:
            raise RuntimeError('Manual correction does not match the single acknowledged initial rejection')
        rejected = unresolved[0]
        rejected.update(status='rejected_http_400', http_status=400,
            reconciled_at_utc=now(), failed_prompt_characters=589,
            diagnosis='Outgoing prompt exceeded the documented450-character limit; inferred from local validation and official documentation.',
            billing_note='No source or billing header returned;60-credit reservation retained despite unchanged quota.')
        value['m2_manual_correction_authorization'] = dict(recorded_at_utc=now(),
            source='Root explicitly authorized two corrected manual submissions within the human-approved aggregate ceiling.',
            max_m2_attempts=3, max_m2_requested_seconds=4.5, max_m2_reserved_or_billed_credits=180,
            scope='Initial rejected attempt remains counted. Stop on any new failure; no further automatic retry.')
        save(LEDGER, value); unresolved = []
    if unresolved:
        raise RuntimeError('Previous request outcome is unresolved; paid automatic retry is forbidden')
    prior = {row['id']: row for row in value['requests'] if row['status'] == 'received'}
    missing = [(name, seconds, prompt) for name, seconds, prompt in M2 if name not in prior]
    requested = sum(row['requested_seconds'] for row in value['requests'])
    new_seconds = sum(row[1] for row in missing)
    bound = math.ceil(new_seconds * RATE_BOUND)
    if len(value['requests']) + len(missing) > MAX_CALLS or requested + new_seconds > MAX_SECONDS or charged(value) + bound > MAX_CREDITS:
        raise RuntimeError('Aggregate Zone 4 request, duration or credit ceiling exceeded')
    m2_rows = [row for row in value['requests'] if row['milestone'] == 'M2']
    m2_limits = value.get('m2_manual_correction_authorization', dict(
        max_m2_attempts=2, max_m2_requested_seconds=3, max_m2_reserved_or_billed_credits=120))
    if len(m2_rows) + len(missing) > m2_limits['max_m2_attempts'] or \
            sum(row['requested_seconds'] for row in m2_rows) + new_seconds > m2_limits['max_m2_requested_seconds'] or \
            sum(row.get('billed_credits_bound', row['reserved_credits']) for row in m2_rows) + bound > m2_limits['max_m2_reserved_or_billed_credits']:
        raise RuntimeError('M2-specific manual authorization exceeded')
    if 'initial_included_balance' not in value:
        value['initial_included_balance'] = remaining
        value['plan_at_first_generation'] = plan
    effective = min(remaining, value['initial_included_balance'] - charged(value))
    print(json.dumps(dict(preflight=True, milestone='M2', plan=plan, remaining_included=remaining,
        effective_included_floor=effective, missing_requests=len(missing), request_bound_credits=bound,
        aggregate_spent_or_reserved=charged(value), aggregate_ceiling=MAX_CREDITS)), flush=True)
    if effective < bound:
        raise RuntimeError('Insufficient existing included credits; no top-up or overage allowed')
    save(LEDGER, value)

    for name, seconds, prompt in missing:
        source = CANDIDATES/(name+'.mp3')
        if source.exists():
            raise RuntimeError('Unreceipted source exists; refusing paid regeneration')
        row = dict(id=name, milestone='M2', status='submitted', submitted_at_utc=now(),
                   requested_seconds=seconds, reserved_credits=math.ceil(seconds*RATE_BOUND))
        value['requests'].append(row); save(LEDGER, value)
        payload = dict(text=prompt, duration_seconds=seconds, loop=False,
                       model_id='eleven_text_to_sound_v2', prompt_influence=.7)
        try:
            with request('/v1/sound-generation?output_format=mp3_44100_128', key, payload) as response:
                data = response.read(); billed_header = response.headers.get('character-cost')
            if not data:
                raise RuntimeError('Empty provider response')
            source.parent.mkdir(parents=True, exist_ok=True); source.write_bytes(data)
            billed = int(math.ceil(float(billed_header))) if billed_header else row['reserved_credits']
            row.update(status='received', received_at_utc=now(), billed_credits_header=billed_header,
                       billed_credits_bound=billed, source=rel(source), source_sha256=sha(source))
            receipt = dict(**row, provider='ElevenLabs', model=payload['model_id'], prompt=prompt,
                prompt_influence=.7, loop=False, output_format='mp3_44100_128',
                published_price_bound_per_second=RATE_BOUND, pricing_source=PRICE_SOURCE,
                license=dict(plan_at_generation=plan, commercial_use=plan not in ('free','unknown'),
                             source=LICENSE_SOURCE), listening_review='Human audition pending.')
            save(CANDIDATES/(name+'.request.json'), receipt); save(LEDGER, value)
            print(json.dumps(dict(generated=name, billed_credits=billed,
                                   aggregate_spent_or_reserved=charged(value))), flush=True)
            if billed > row['reserved_credits']:
                raise RuntimeError('Unexpected provider charge; generation halted')
        except Exception as error:
            if row['status'] != 'received':
                row['status'] = 'unresolved'
                if isinstance(error, urllib.error.HTTPError): row['http_status'] = error.code
                save(LEDGER, value)
            raise
    _, after_remaining = subscription(key)
    value['last_reported_included_balance'] = after_remaining
    value['last_checked_at_utc'] = now()
    value['aggregate_header_or_reserved_credits'] = charged(value)
    value['aggregate_requested_seconds'] = sum(row['requested_seconds'] for row in value['requests'])
    value['aggregate_requests'] = len(value['requests'])
    value['quota_note'] = 'Quota endpoint may lag; per-response character-cost headers are the spend ledger.'
    save(LEDGER, value)


def execute_m3(review_complete):
    value = ledger()
    if not review_complete:
        raise RuntimeError('M3 requires the explicit completed-M2-review gate')
    if any(row['status'] in ('submitted', 'unresolved') for row in value['requests']):
        raise RuntimeError('Unresolved request remains; automatic paid retry is forbidden')
    received = {row['id']: row for row in value['requests'] if row['status'] == 'received'}
    if any(name not in received for name, *_ in M2):
        raise RuntimeError('M2 candidates must exist before M3')
    value.setdefault('m2_vertical_slice_review', dict(completed=True, recorded_at_utc=now(),
        source='Root explicitly confirmed completed M2 vertical-slice review on2026-10-24.',
        native_job='99de684654ae', tests_passed=4, tests_total=4,
        evidence='Actual archived wreck composition, manual SEND/capture, hidden coordinates not automatically discovered, readable Mission Log and nonzero listener capture without clipping.',
        human_headphone_aesthetic_review='Still required; not represented as certified by technical checks.'))
    key = credentials(); plan, remaining = subscription(key)
    missing = [(name, seconds, prompt) for name, seconds, prompt in M3 if name not in received]
    seconds = sum(row[1] for row in missing); bound = math.ceil(seconds * RATE_BOUND)
    current_seconds = sum(row['requested_seconds'] for row in value['requests'])
    if len(value['requests']) + len(missing) > MAX_CALLS or current_seconds + seconds > MAX_SECONDS or charged(value) + bound > MAX_CREDITS:
        raise RuntimeError('Aggregate Zone 4 authorization would be exceeded')
    effective = min(remaining, value['initial_included_balance'] - charged(value))
    print(json.dumps(dict(preflight=True, milestone='M3', plan=plan, remaining_included=remaining,
        effective_included_floor=effective, new_requests=len(missing), new_requested_seconds=seconds,
        new_conservative_bound=bound, aggregate_spent_or_reserved=charged(value), ceiling=MAX_CREDITS)), flush=True)
    if effective < bound:
        raise RuntimeError('Insufficient existing included quota; no top-up or overage')
    save(LEDGER, value)
    for name, duration, prompt in missing:
        if len(prompt) > 450:
            raise RuntimeError('Prompt too long before submission')
        source = CANDIDATES/(name+'.mp3')
        if source.exists():
            raise RuntimeError('Unreceipted source retained; refusing regeneration')
        row = dict(id=name, milestone='M3', status='submitted', submitted_at_utc=now(),
                   requested_seconds=duration, reserved_credits=math.ceil(duration*RATE_BOUND))
        value['requests'].append(row); save(LEDGER, value)
        payload = dict(text=prompt, duration_seconds=duration, loop=False,
                       model_id='eleven_text_to_sound_v2', prompt_influence=.7)
        try:
            with request('/v1/sound-generation?output_format=mp3_44100_128', key, payload) as response:
                data = response.read(); header = response.headers.get('character-cost')
            if not data:
                raise RuntimeError('Empty generated audio')
            source.parent.mkdir(parents=True, exist_ok=True); source.write_bytes(data)
            billed = int(math.ceil(float(header))) if header else row['reserved_credits']
            row.update(status='received', received_at_utc=now(), billed_credits_header=header,
                       billed_credits_bound=billed, source=rel(source), source_sha256=sha(source))
            receipt = dict(**row, provider='ElevenLabs', model=payload['model_id'], prompt=prompt,
                prompt_characters=len(prompt), prompt_influence=.7, loop=False,
                output_format='mp3_44100_128', published_price_bound_per_second=RATE_BOUND,
                pricing_source=PRICE_SOURCE, license=dict(plan_at_generation=plan,
                    commercial_use=plan not in ('free','unknown'), source=LICENSE_SOURCE),
                listening_review='Human audition pending; no perceptual approval claimed.')
            save(CANDIDATES/(name+'.request.json'), receipt); save(LEDGER, value)
            print(json.dumps(dict(generated=name, billed_credits=billed,
                                  aggregate_spent_or_reserved=charged(value))), flush=True)
            if billed > row['reserved_credits']:
                raise RuntimeError('Unexpected provider charge; generation stopped')
        except Exception as error:
            if row['status'] != 'received':
                row['status'] = 'unresolved'
                if isinstance(error, urllib.error.HTTPError): row['http_status'] = error.code
                save(LEDGER, value)
            raise
    _, remaining_after = subscription(key)
    value.update(last_reported_included_balance=remaining_after, last_checked_at_utc=now(),
        aggregate_header_or_reserved_credits=charged(value),
        aggregate_requested_seconds=sum(row['requested_seconds'] for row in value['requests']),
        aggregate_requests=len(value['requests']))
    save(LEDGER, value)


def decode(path):
    process = subprocess.run([decoder(), '-v', 'error', '-i', str(path), '-ar', str(SR),
                              '-ac', '2', '-f', 'f32le', 'pipe:1'],
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
    return np.frombuffer(process.stdout, '<f4').reshape(-1, 2).astype(np.float64)


def process_m2(selected):
    rows = []
    for name, seconds, _ in M2:
        source = CANDIDATES/(name+'.mp3'); receipt_path = CANDIDATES/(name+'.request.json')
        receipt = json.loads(receipt_path.read_text())
        if receipt['source_sha256'] != sha(source):
            raise RuntimeError('Source hash mismatch')
        raw = decode(source)
        if not np.isfinite(raw).all() or float(np.max(np.abs(raw))) < .001:
            raise RuntimeError('Invalid or effectively silent provider audio')
        # A short practical interface cue, with headroom for the validated capture
        # success and cabin machinery. No music, reverberant stinger or fake data.
        designed = filter_band(trim_onset(raw, .006), 90, 6500)
        designed = level(ramps(designed, .003, .065), -14)
        output = CANDIDATES/(name+'.wav')
        verification = write_wav(output, designed)
        if verification['clipped_samples'] or verification['rms_dbfs'] < -48:
            raise RuntimeError('Processed fragment failed level verification')
        entry = dict(id=name, provider='ElevenLabs source with local production processing',
            milestone='M2', asset=rel(output), asset_sha256=sha(output),
            source=rel(source), source_sha256=sha(source), request_receipt=rel(receipt_path),
            billed_credits=receipt['billed_credits_header'], license=receipt['license'],
            output='Stereo 44100Hz PCM16 WAV', loop=False,
            processing='Leading silence trimmed to6ms lead;90Hz high-pass/6.5kHz low-pass,3ms attack/65ms release,−14dBFS peak headroom. No normalization of gameplay recordings.',
            processing_source=rel(Path(__file__)), processing_source_sha256=sha(Path(__file__)),
            raw_metrics=metrics(raw), verification=verification,
            listening_review='Technical first-pass only. Human comparison of both candidates in the actual Computer/fragment slice remains required.')
        save(output.with_suffix('.provenance.json'), entry); rows.append(entry)
    chosen = next(row for row in rows if row['id'] == selected)
    BASE.mkdir(parents=True, exist_ok=True)
    output = BASE/'fragment_recovery.wav'
    output.write_bytes((ROOT/chosen['asset']).read_bytes())
    proof = dict(chosen)
    proof.update(id='fragment_recovery', asset=rel(output), asset_sha256=sha(output),
        selected_candidate=selected,
        selection_reason='Provisional M2 choice emphasizes practical locking/data-packet ticks and short completion, rather than an unfamiliar sustained carrier. Both candidates passed finite-data, audibility and headroom checks. This is not a claim of human audition.',
        acceptance_status='Ready for native Unity import, real playback binding and M2 human review; not yet gameplay-mix accepted.')
    if selected.endswith('relay'):
        proof['selection_reason'] = 'Explicit offline selection of the gated read/relay alternative; human gameplay audition still required.'
    save(output.with_suffix('.provenance.json'), proof)
    save(QA/'audio-m2-manifest.json', dict(selected=proof, candidates=rows,
        wider_production='Stopped. Awaiting explicit root confirmation of completed M2 vertical-slice review.',
        authorization_ledger=rel(LEDGER)))
    print(json.dumps(dict(processed_candidates=len(rows), production=rel(output),
                          selected=selected, verification=proof['verification'])), flush=True)


def read_pcm(path):
    with wave.open(str(path), 'rb') as stream:
        if stream.getsampwidth() != 2 or stream.getframerate() != SR:
            raise RuntimeError('Expected project PCM16 44.1kHz source')
        channels = stream.getnchannels()
        x = np.frombuffer(stream.readframes(stream.getnframes()), '<i2').reshape(-1, channels).astype(float)/32768
    return np.repeat(x, 2, axis=1) if channels == 1 else x[:, :2]


def place(out, x, seconds, gain=1, pan=0):
    at = round(seconds*SR); count = min(len(x), len(out)-at)
    if count <= 0: return
    gains = np.array([1-max(0,pan)*.55, 1+min(0,pan)*.55])
    out[at:at+count] += x[:count]*gain*gains


def energy_window(x, seconds, from_fraction=0, to_fraction=1):
    count = min(round(seconds*SR), len(x))
    first = max(0, round(len(x)*from_fraction))
    last = min(len(x)-count, round(len(x)*to_fraction)-count)
    last = max(first, last); first = min(first, len(x)-count)
    energy = np.mean(x*x, axis=1); accumulated = np.r_[0,np.cumsum(energy)]
    sums = accumulated[count:]-accumulated[:-count]
    at = first+int(np.argmax(sums[first:last+1]))
    return x[at:at+count].copy(), dict(start_seconds=round(at/SR,5), duration_seconds=round(count/SR,5))


def features(x):
    row = metrics(x)
    envelope = np.array([np.sqrt(np.mean(x[i:i+2205]**2)) for i in range(0,len(x),2205)])
    threshold = max(.00005, float(envelope.max())*.08)
    row['quiet_50ms_fraction'] = round(float(np.mean(envelope<threshold)),5)
    row['envelope_variation'] = round(float(envelope.std()/max(envelope.mean(),1e-12)),5)
    return row


def process_m3():
    if not ledger().get('m2_vertical_slice_review',{}).get('completed'):
        raise RuntimeError('M2 review gate is not recorded')
    raw, receipts, candidates = {}, {}, []
    for name, *_ in M3:
        source = CANDIDATES/(name+'.mp3')
        receipt = json.loads((CANDIDATES/(name+'.request.json')).read_text())
        if sha(source) != receipt['source_sha256']:
            raise RuntimeError('Provider source hash mismatch')
        x = decode(source)
        if not np.isfinite(x).all() or float(np.max(np.abs(x))) < .001:
            raise RuntimeError('Invalid or effectively silent provider source; no automatic regeneration')
        raw[name] = x; receipts[name] = receipt
        target = -20 if name.startswith('abyss') else -18 if name.startswith('distant') else -13 if name.startswith('final') else -15
        candidate = level(ramps(filter_band(x,35,8500),.03,.15),target)
        path = CANDIDATES/(name+'.wav'); verification = write_wav(path,candidate)
        entry = dict(id=name, milestone='M3', provider='ElevenLabs', asset=rel(path),
            asset_sha256=sha(path), source=rel(source), source_sha256=sha(source),
            request_receipt=rel(CANDIDATES/(name+'.request.json')), license=receipt['license'],
            raw_features=features(x), verification=verification,
            processing='Equal-peak audition within each pair,35Hz high-pass/8.5kHz low-pass and short edge ramps. Original provider MP3 retained.',
            listening_review='Human comparative listening remains required; no perceptual certification.')
        save(path.with_suffix('.provenance.json'),entry); candidates.append(entry)
    by_id = {row['id']:row for row in candidates}
    choice = {}
    choice['abyss'] = max(('abyss_sparse_water_a','abyss_sparse_water_b'),
        key=lambda n:by_id[n]['raw_features']['quiet_50ms_fraction']+by_id[n]['raw_features']['envelope_variation']*.1)
    choice['structure'] = max(('distant_structure_a','distant_structure_b'),
        key=lambda n:by_id[n]['raw_features']['envelope_variation'])
    choice['hidden_a'] = max(('hidden_signal_a_fold','hidden_signal_a_aperture'),
        key=lambda n:by_id[n]['raw_features']['energy_above_2000hz']*.2+by_id[n]['raw_features']['quiet_50ms_fraction'])
    choice['hidden_b'] = max(('hidden_signal_b_film','hidden_signal_b_drift'),
        key=lambda n:by_id[n]['raw_features']['energy_below_250hz'])
    choice['hidden_b_transients'] = max(('hidden_signal_b_film','hidden_signal_b_drift'),
        key=lambda n:by_id[n]['raw_features']['energy_above_2000hz'])
    choice['final'] = max(('final_signal_thread','final_signal_fold'),
        key=lambda n:1-by_id[n]['raw_features']['energy_below_250hz'])
    selected = {key:raw[name] for key,name in choice.items()}
    assets = []

    def asset(name,x,peak,source_ids,processing,loop=False,additional=None):
        x = level(x,peak); path = BASE/(name+'.wav')
        temporary = QA/'audio-build'/(name+'.wav'); verification = write_wav(temporary,x)
        if not path.exists() or temporary.read_bytes() != path.read_bytes(): temporary.replace(path)
        else: temporary.unlink()
        if verification['clipped_samples'] or verification['rms_dbfs'] < -58:
            raise RuntimeError('Production cue failed finite level/headroom verification')
        entry = dict(id=name,milestone='M3',asset=rel(path),asset_sha256=sha(path),
            provider='Original ElevenLabs sources with local authored audio design',
            output='Stereo44100Hz PCM16 WAV',loop=loop,processing=processing,
            sources=[dict(id=n,path=rel(CANDIDATES/(n+'.mp3')),sha256=sha(CANDIDATES/(n+'.mp3')),
                          request_receipt=rel(CANDIDATES/(n+'.request.json'))) for n in source_ids],
            license=receipts[source_ids[0]]['license'],verification=verification,
            processing_source=rel(Path(__file__)),processing_source_sha256=sha(Path(__file__)),
            listening_review='Technical first-pass selection and design; actual gameplay/headphone aesthetic review remains required.')
        if additional: entry['additional_sources']=additional
        save(path.with_suffix('.provenance.json'),entry); assets.append(entry)
        return x

    # A sparse loop contains natural water changes separated by genuine empty
    # intervals. It is an authored source bed, never the Acoustic Withdrawal state.
    bed=np.zeros((24*SR,2)); water=filter_band(selected['abyss'],35,1800)
    place(bed,ramps(water,.8,1.2),.5,.7,-.12)
    place(bed,ramps(water[round(1.4*SR):,::-1],1.1,1.4),14.0,.48,.18)
    bed=asset('abyss_sparse_water',bed,-22,[choice['abyss']],
        '24s sparse water bed: original8s motions and a different6.6s section at14s, long intervening quiet gaps,35Hz high-pass/1.8kHz low-pass, gentle ramps and exact quiet endpoints. No constant drone or engine baked in. Acoustic Withdrawal remains runtime mix automation.',True)

    structure=np.zeros((round(6.8*SR),2)); core=ramps(filter_band(selected['structure'],55,1900),.22,.65)
    place(structure,core,0,1,-.15); place(structure,filter_band(core,65,1050),.41,.11,.4)
    structure=asset('distant_structure',structure,-18,[choice['structure']],
        'One distant nonpercussive structure event,55Hz high-pass/1.9kHz low-pass,220ms soft attack,one restrained darker cross-channel diffusion component; bounded6.8s source with no hull/stone reuse.')

    hidden_a=np.zeros((9*SR,2)); a_band=filter_band(selected['hidden_a'],180,2400)
    a1,a1_range=energy_window(a_band,1.45,0,.55); a2,a2_range=energy_window(a_band,1.3,.45,1)
    place(hidden_a,ramps(level(a1,-18),.18,.28),.2,1,-.16)
    place(hidden_a,ramps(level(a2,-21),.28,.35),5.4,1,.22)
    hidden_a=asset('hidden_signal_a',hidden_a,-15,[choice['hidden_a']],dict(
        design='Two unequal soft180-2400Hz refractive-resonance pockets separated by3.75s of empty space; no pitch-shifted Zone3 stone source.',
        source_sections=[a1_range,a2_range],onsets_seconds=[.2,5.4],duration_seconds=9))

    hidden_b=np.zeros((9*SR,2)); b_high=filter_band(selected['hidden_b_transients'],2300,8500); b_low=filter_band(selected['hidden_b'],45,300)
    b1,b1_range=energy_window(b_high,.23,0,.65); b2,b2_range=energy_window(b_high,.16,.45,1)
    b3,b3_range=energy_window(b_low,1.05,0,1)
    place(hidden_b,ramps(level(b1,-18),.004,.035),.25,1,-.45)
    place(hidden_b,ramps(level(b3,-24),.18,.35),3.55,1,.35)
    place(hidden_b,ramps(level(b2,-22),.003,.035),7.2,1,.25)
    hidden_b=asset('hidden_signal_b',hidden_b,-17,[choice['hidden_b_transients'],choice['hidden_b']],dict(
        design='Layered complementary originalB candidates: actual2.3-8.5kHz fine transients from the film source,and45-300Hz lateral motion from the drift source,with multi-second quiet gaps. Different generated material and temporal/spectral structure from HiddenA; no pitch substitution.',
        source_sections=[b1_range,b3_range,b2_range],onsets_seconds=[.25,3.55,7.2],duration_seconds=9))

    final=np.zeros((round(5.8*SR),2)); f_original=trim_onset(selected['final'],.04)
    f_core=level(filter_band(f_original,60,220),-23)*.65+level(filter_band(f_original,260,2400),-17)
    f_core=np.tanh(f_core/max(float(np.max(np.abs(f_core))),1e-10)*1.4)/np.tanh(1.4)
    f_core=ramps(f_core,.08,.7)
    place(final,f_core,.06,1,-.08); place(final,filter_band(f_core,100,1600),.43,.065,.35)
    final=asset('final_signal',final,-13,[choice['final']],
        'Original final-thread signature selected for its richer non-bass content; independently levelled60-220Hz fluid body and260-2400Hz thread,restrained1.4x soft saturation,80ms gentle attack/700ms release and one weak darker residual.5.8s bounded event,not a warning or impact. Makes the upper signature measurable against idle machinery without raising output peak. Keep Anomalies readable during environmental withdrawal without bypassing user category preferences.')

    engine_path=ROOT/'Assets/_Project/Audio/Zone01/Machinery/ambient_submarine_engine.wav'
    engine=read_pcm(engine_path)[:8*SR]
    normal=np.zeros((9*SR,2)); place(normal,ramps(engine,.6,4.0),0,.15)
    place(normal,bed[round(.5*SR):round(4.0*SR)],2.3,.16)
    normal=asset('normal_ending_settle',ramps(normal,.1,1.0),-23,[choice['abyss']],
        'Offline-derived neutral ending layer: unchanged familiar auxiliary machinery source and a sparse selected abyss-water detail; steady reassurance followed by a long natural release. No new generation,music,fragment-data cue or final signature. This is presentation audio only and never changes working ship machinery.',
        additional=[dict(path=rel(engine_path),sha256=sha(engine_path))])

    hidden_tail=np.zeros((round(7.2*SR),2)); ending_water=filter_band(selected['abyss'],40,1100)
    place(hidden_tail,ramps(ending_water[:round(4.7*SR)],.3,2.3),.1,.23)
    late=filter_band(selected['final'][-round(.8*SR):],100,1600)
    place(hidden_tail,ramps(late,.15,.5),.4,.045,-.2)
    hidden_tail=asset('hidden_ending_tail',ramps(hidden_tail,.1,1.2),-27,[choice['abyss'],choice['final']],
        'Offline-derived receding7.2s ending tail using sparse water and only the selected final-source residual ending,not a replay of its complete signature. Final near-silence and environmental withdrawal are still owned runtime mix/envelope states,not a blank WAV. No new generation or music.')

    manifest=dict(milestone='M3',selected_candidates=choice,production=assets,candidates=candidates,
        selection_method='Measurable first-pass choices: quieter/nonstationary water,changing structure envelope,separated HiddenA spectral pockets,complementary real HiddenB high-transient and low-motion candidates,and final-thread source with richer non-bass content. Criteria are not aesthetic listening approval.',
        aggregate_ledger=rel(LEDGER),m2_fragment_unchanged=True,
        native_acceptance='Root owns Unity import,binding,actual listener-mix checks and human comparative review.')
    save(QA/'audio-m3-manifest.json',manifest)
    for group,ids in [('abyss',('abyss_sparse_water_a','abyss_sparse_water_b')),
                      ('structure',('distant_structure_a','distant_structure_b')),
                      ('hidden-a',('hidden_signal_a_fold','hidden_signal_a_aperture')),
                      ('hidden-b',('hidden_signal_b_film','hidden_signal_b_drift')),
                      ('final',('final_signal_thread','final_signal_fold'))]:
        length=max(len(raw[n]) for n in ids)/SR; reel=np.zeros((round((length*2+3)*SR),2))
        for at,name in zip((.5,length+1.5),ids):place(reel,read_pcm(CANDIDATES/(name+'.wav')),at)
        write_wav(QA/('audio-m3-comparison-'+group+'.wav'),reel)
    review=np.zeros((round(sum(row['verification']['duration_seconds']+2 for row in assets)*SR),2));at=0;timings=[]
    for row in assets:
        x=read_pcm(ROOT/row['asset']);place(review,x,at);timings.append(dict(id=row['id'],start_seconds=at));at+=len(x)/SR+2
    write_wav(QA/'audio-m3-production-review.wav',review)
    save(QA/'audio-m3-review-timings.json',timings)
    print(json.dumps(dict(processed_original_candidates=len(candidates),production_wavs=len(assets),
                          selected_candidates=choice,fragment_unchanged=True)),flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--execute-m2', action='store_true')
    parser.add_argument('--corrected-manual-m2', action='store_true', help='One acknowledged correction of the initial HTTP400 only; not an automatic retry mode.')
    parser.add_argument('--process-m2', action='store_true', help='Offline only; no provider request.')
    parser.add_argument('--execute-m3', action='store_true')
    parser.add_argument('--m2-review-complete', action='store_true', help='Explicit gate confirmed by root before M3.')
    parser.add_argument('--process-m3', action='store_true', help='Offline only; no provider request.')
    parser.add_argument('--select', choices=[r[0] for r in M2], default=M2[0][0])
    args = parser.parse_args()
    if args.execute_m2:
        execute_m2(args.corrected_manual_m2)
    if args.execute_m2 or args.process_m2:
        process_m2(args.select)
    if args.execute_m3:
        execute_m3(args.m2_review_complete)
    if args.execute_m3 or args.process_m3:
        process_m3()
    if not any((args.execute_m2,args.process_m2,args.execute_m3,args.process_m3)):
        print(json.dumps(dict(dry_run=True, milestone='M2', requests=2, requested_seconds=3,
                              conservative_bound=120, aggregate_credit_ceiling=2400)))


if __name__ == '__main__':
    try:
        main()
    except urllib.error.HTTPError as error:
        print('ElevenLabs HTTP status', error.code, file=sys.stderr); sys.exit(1)
    except urllib.error.URLError:
        print('ElevenLabs network request failed', file=sys.stderr); sys.exit(1)
    except Exception as error:
        print('Zone 4 audio production stopped:', type(error).__name__, file=sys.stderr); sys.exit(1)
