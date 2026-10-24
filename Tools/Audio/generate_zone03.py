"""Bounded, resumable Zone 3 ElevenLabs source production and authored audio design.

Exactly twenty sources, 45 requested seconds, at most 1800 included credits using
the published 40 credits/s bound. No top-up, retry spending, voice or music calls.
Keys/account responses/error bodies never enter logs. Offline --process only
rebuilds derived WAVs. --execute may resume an interrupted set but not regenerate.
"""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import shutil
import subprocess
import sys
import urllib.error
import wave

import numpy as np

from generate_sound import ROOT, request
from generate_terrain_impact import credentials

BASE = ROOT / 'Assets/_Project/Audio/Horror/Zone03'
QA = ROOT / 'Docs/QA/Zone03Cinematic'
CANDIDATES = QA / 'audio-candidates'
SR = 44100
MAX_CALLS = 20
MAX_CREDITS = 1800
RATE_BOUND = 40
PRICE_SOURCE = 'https://elevenlabs.io/docs/overview/capabilities/sound-effects'
NO_VOICE = ' No voices, speech, human breathing, music, melodic chime, cinematic braam, scream or jump-scare stinger.'

# id, seconds, loop, prompt. Different stone and blast candidates are retained.
SOURCES = [
 ('stone_bed',3.5,True,'Very quiet deep submerged stone ruin atmosphere: still water pressure with diffuse granular stone vibration, a sparse porous hollow space, no rhythmic pulse, no engine, no metallic creak, no wind.'),
 ('stone_near',2.5,False,'One massive porous submerged limestone slab resonates internally, a soft blunt onset then a low uneven granular hollow stone resonance, dense and natural, no sharp impact, no metal or bell.'),
 ('stone_far',2.5,False,'One very distant enormous submerged stone arch shifts microscopically under water pressure. Muted broad hollow mineral resonance, low fluttering decay, no visible crash, no steel hull, no metal ring or bell.'),
 ('stone_side',2.5,False,'A heavy flooded basalt column gives one restrained low hollow mineral resonance, small gritty granular details within a long dull body, physically deep underwater, no metallic ringing, no bell, no wood.'),
 ('stone_alternative',2.5,False,'One muted deep underwater sandstone pressure resonance, a very soft non-percussive swell of porous low stone vibration and dusty granular decay. Earthy and heavy, no bell, metal, steel, or hull creak.'),
 ('engine_reflection_texture',2,False,'A distant submarine propeller reflection arriving through an enormous submerged stone cavity, irregular fluttering low mechanical bands smear into granular mineral resonance, hollow watery diffusion, no direct engine foreground, no musical tone.'),
 ('creature05_pressure',1.5,False,'Close small translucent underwater organism reacts to pressure: three irregular tiny wet membrane compression releases and damp organic micro-bursts, moist but restrained, not a mouth, no mechanical click, no impact.'),
 ('creature06_motion',1.5,False,'Close underwater biological organism moves hard crystalline cartilage plates: fine dry brittle rubbing and tiny solid mineral ticks, delicate physical animal motion, non-musical and muted by water, no glass breaking, bell, wind chime or metal.'),
 ('creature07_discharge',1.5,False,'Close living underwater organism emits two unequal bioelectric discharges: soft wet low zrrp pulses with short fine electrical fizz, irregular organic timing, small local energy, no sci-fi laser, alarm or huge electric arc.'),
 ('radar_texture',.5,False,'One very short weak distant analog sonar reflection flutter, thin muffled watery electrical static with a tiny unstable tonal fragment, old scientific equipment, no alarm or repeating beeps.'),
 ('soft_movement',1.5,False,'One barely audible intimate soft fabric-like membrane shifting slowly against smooth glass, irregular fine dry-soft friction, unidentifiable near-field physical motion, no metal scrape, footsteps, impacts or creature voice.'),
 ('close_click',.5,False,'One tiny close dry instrument housing click, single restrained plastic relay tick with immediate decay into silence, no repeated pattern or alarm.'),
 ('assembly',3,False,'Functional heavy submarine rock-breaker assembly: two solid locking latches, a short compact low electric actuator, then a firm trustworthy mechanical engagement clunk, no confirmation melody, no horror sound, no explosion.'),
 ('detonation_pressure',3,False,'One underwater controlled demolition detonation heard from inside a submarine: instant massive muffled low pressure thump, extremely short dirty body, dense bass pressure expansion with a long watery low decay. No fire, sharp gunshot, Hollywood boom, music or falling debris.'),
 ('detonation_cavity',3,False,'One extremely heavy contained underwater explosive pressure pulse in a deep stone cavern. Immediate dense low-frequency whomp with little high treble, broad water-compressed pressure bloom and dark long decay, no cinematic riser, debris, gunshot or fire.'),
 ('collapse_large',2,False,'Two massive submerged stone blocks break free and settle, slow staggered dull rocky clunks with granular crushing between, heavy water damping, no explosion, metal, bell or glass.'),
 ('collapse_medium',2,False,'Several medium submerged rocks tumble unevenly over each other, dense dull stone knocks, coarse granular rumbling and crushed grit, close environmental foley, no explosion or metal.'),
 ('collapse_grit',2,False,'Small underwater stone fragments and sediment cascade softly, many irregular restrained mineral ticks and fine gritty movement then settling, no explosion, glass breaking, metal or bright sparkle.'),
 ('post_resonance',4,False,'After a distant underwater stone demolition, one broad hollow mineral space resonates softly with sparse settling stone dust and low receding pressure. Fades to emptiness, no new impact, engine, music or warning sound.'),
 ('sparse_water',3,True,'Very quiet still deep water inside a vast empty submerged passage, sparse soft pressure movement with enormous negative space, almost nothing happening, no bubbles, waves, wind, creatures, machinery or tonal drone.')
]
assert len(SOURCES) == MAX_CALLS
assert sum(s[1] for s in SOURCES) * RATE_BOUND <= MAX_CREDITS


def rel(path):
    return str(path.relative_to(ROOT)).replace('\\', '/')


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


def decoder():
    configured = os.environ.get('G10_FFMPEG')
    found = configured or shutil.which('ffmpeg')
    fallback = Path.home() / 'AppData/Local/Microsoft/WinGet/Links/ffmpeg.exe'
    if not found and fallback.exists():
        found = str(fallback)
    if not found:
        raise RuntimeError('ffmpeg unavailable')
    return found


def read_audio(path):
    if path.suffix.lower() == '.wav':
        with wave.open(str(path), 'rb') as stream:
            assert stream.getframerate() == SR and stream.getsampwidth() == 2
            x = np.frombuffer(stream.readframes(stream.getnframes()), '<i2')
            x = x.reshape(-1, stream.getnchannels()).astype(np.float64) / 32768
        return np.repeat(x, 2, axis=1) if x.shape[1] == 1 else x[:, :2]
    target = CANDIDATES / (path.stem + '.decode.wav')
    subprocess.run([decoder(), '-v', 'error', '-y', '-i', str(path), '-ar', str(SR), '-ac', '2', '-c:a', 'pcm_s16le', str(target)], check=True)
    try:
        return read_audio(target)
    finally:
        target.unlink(missing_ok=True)


def filter_band(x, low=0, high=20000):
    # Smooth zero-phase frequency response; pads avoid cyclic wrap into the attack.
    n = len(x)
    fft_size = 1 << int(math.ceil(math.log2(max(2, n * 2))))
    f = np.fft.rfftfreq(fft_size, 1 / SR)
    gain = np.ones_like(f)
    if high < SR / 2:
        gain *= 1 / np.sqrt(1 + (f / high) ** 8)
    if low > 0:
        gain *= 1 / np.sqrt(1 + (low / np.maximum(f, .001)) ** 8)
    return np.fft.irfft(np.fft.rfft(x, n=fft_size, axis=0) * gain[:, None], n=fft_size, axis=0)[:n]


def level(x, db):
    peak = float(np.max(np.abs(x)))
    if peak < .00001:
        raise RuntimeError('Silent source or design')
    return x * (10 ** (db / 20) / peak)


def ramps(x, attack=.006, release=.08):
    x = x.copy()
    a = min(len(x), round(attack * SR)); b = min(len(x), round(release * SR))
    if a:
        x[:a] *= np.linspace(0, 1, a)[:, None]
    if b:
        x[-b:] *= np.linspace(1, 0, b)[:, None]
    return x


def trim_onset(x, lead=.006):
    envelope = np.max(np.abs(x), axis=1)
    indices = np.flatnonzero(envelope > max(.0005, envelope.max() * .025))
    return x[max(0, int(indices[0]) - round(lead * SR)):] if len(indices) else x


def put(destination, x, at, gain=1, pan=0):
    start = round(at * SR)
    length = min(len(x), len(destination) - start)
    if length > 0:
        stereo = np.array([1 - max(0, pan) * .65, 1 + min(0, pan) * .65])
        destination[start:start + length] += x[:length] * gain * stereo


def design_tail(x, seconds, high=1800, decay=1.8):
    out = np.zeros((round(seconds * SR), 2))
    put(out, filter_band(x, 32, high), 0)
    for delay, gain, pan in ((.113, .25, -.5), (.271, .19, .5), (.487, .14, -.7), (.813, .10, .65), (1.237, .075, -.2), (1.703, .055, .4)):
        put(out, filter_band(x, 30, high / (1 + delay)), delay, gain, pan)
    t = np.arange(len(out)) / SR
    out *= np.exp(-t / decay)[:, None]
    return ramps(out, .004, .2)


def loop_design(x, seconds, high, seed):
    # Long decorrelated bed from original organic source, with no zero-padding gaps.
    x = filter_band(x, 28, high)
    cross = min(round(.24 * SR), len(x) // 5)
    w = np.linspace(0, 1, cross)[:, None]
    cycle = np.concatenate((x[cross:-cross], x[-cross:] * (1-w) + x[:cross] * w))
    out = np.zeros((round(seconds * SR), 2))
    rng = np.random.default_rng(seed)
    length = len(cycle)
    for lane, gain in ((0, .7), (1, .3)):
        offset = int(rng.integers(0, length))
        indices = (np.arange(len(out)) + offset) % length
        layer = cycle[indices].copy()
        if lane:
            layer = layer[:, ::-1]
        out += layer * gain
    # Exact endpoint continuity for a loop played by Unity without an audible click.
    difference = out[-1] - out[0]
    out[-cross:] -= np.linspace(0, 1, cross)[:, None] * difference
    return out


def metrics(x):
    mono = x.mean(axis=1)
    spectrum = np.abs(np.fft.rfft(mono)) ** 2
    f = np.fft.rfftfreq(len(mono), 1/SR)
    total = max(float(spectrum.sum()), 1e-20)
    peak = float(np.max(np.abs(x)))
    rms = float(np.sqrt(np.mean(x*x)))
    block = 441
    envelopes = np.array([np.sqrt(np.mean(mono[i:i+block] ** 2)) for i in range(0,len(mono),block)])
    active = np.flatnonzero(envelopes > max(.0001, envelopes.max() * .08))
    correlation = float(np.corrcoef(x[:, 0], x[:, 1])[0, 1]) if rms else 1
    return dict(duration_seconds=round(len(x)/SR,5), peak_dbfs=round(20*math.log10(max(peak,1e-20)),3),
                rms_dbfs=round(20*math.log10(max(rms,1e-20)),3), clipped_samples=int(np.count_nonzero(np.abs(x) >= 1)),
                seam_step=float(np.abs(x[0]-x[-1]).max()), spectral_centroid_hz=round(float((spectrum*f).sum()/total),2),
                energy_below_250hz=round(float(spectrum[f<250].sum()/total),5),
                energy_above_2000hz=round(float(spectrum[f>2000].sum()/total),5),
                active_onset_seconds=round(float(active[0]*.01),3) if len(active) else None,
                stereo_correlation=round(correlation,5))


def write_wav(path, x):
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.rint(np.clip(x, -.9999, .9999) * 32767).astype('<i2')
    with wave.open(str(path), 'wb') as stream:
        stream.setnchannels(2); stream.setsampwidth(2); stream.setframerate(SR); stream.writeframes(pcm.tobytes())
    return metrics(x)


def produce():
    BASE.mkdir(parents=True, exist_ok=True)
    CANDIDATES.mkdir(parents=True, exist_ok=True)
    key = credentials()
    with request('/v1/user/subscription', key) as response:
        before = json.load(response)
    remaining = before.get('character_limit', 0) - before.get('character_count', 0)
    plan = before.get('tier', 'unknown')
    existing = list(CANDIDATES.glob('*.request.json'))
    spent = sum(int(json.loads(p.read_text()).get('billed_credits_bound', MAX_CREDITS)) for p in existing)
    missing = [(name, seconds, loop, prompt) for name, seconds, loop, prompt in SOURCES if not (CANDIDATES/(name+'.request.json')).exists()]
    bound = math.ceil(sum(s[1] for s in missing)*RATE_BOUND)
    print(json.dumps(dict(preflight=True, plan=plan, remaining_included=remaining, prior_budget_charged=spent,
                          missing_requests=len(missing), remaining_cost_bound=bound, budget=MAX_CREDITS)), flush=True)
    if len(existing) + len(missing) > MAX_CALLS or spent + bound > MAX_CREDITS or remaining < bound:
        raise RuntimeError('Included-credit budget or call bound prevents generation')
    for name, seconds, loop, prompt in SOURCES:
        proof = CANDIDATES/(name+'.request.json')
        if proof.exists():
            continue
        source = CANDIDATES/(name+'.mp3')
        # A source without its receipt is deliberately not retried automatically.
        if source.exists():
            raise RuntimeError('Unreceipted source needs reconciliation; refusing retry spending')
        payload = dict(text=prompt+NO_VOICE, duration_seconds=seconds, loop=loop,
                       model_id='eleven_text_to_sound_v2', prompt_influence=.7)
        with request('/v1/sound-generation?output_format=mp3_44100_128', key, payload) as response:
            data = response.read()
            billed_header = response.headers.get('character-cost')
        if not data:
            raise RuntimeError('Empty audio response')
        source.write_bytes(data)
        billed = int(float(billed_header)) if billed_header else math.ceil(seconds*RATE_BOUND)
        entry = dict(id=name, provider='ElevenLabs', model=payload['model_id'], prompt=payload['text'],
                     prompt_influence=.7, duration_requested_seconds=seconds, loop=loop,
                     billed_credits_header=billed_header, billed_credits_bound=billed,
                     published_price_bound=math.ceil(seconds*RATE_BOUND), pricing_source=PRICE_SOURCE,
                     source=rel(source), source_sha256=hashlib.sha256(data).hexdigest(),
                     license=dict(plan_at_generation=plan, commercial_use=plan not in ('free','unknown'),
                                  source='https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform'))
        save_json(proof, entry)
        spent += billed
        print(json.dumps(dict(generated=name, seconds=seconds, billed_credits=billed, cumulative=spent)), flush=True)
        if billed > math.ceil(seconds*RATE_BOUND) or spent > MAX_CREDITS:
            raise RuntimeError('Unexpected provider charge; generation halted')
    with request('/v1/user/subscription', key) as response:
        after = json.load(response)
    save_json(QA/'audio-budget.json', dict(provider='ElevenLabs', model='eleven_text_to_sound_v2',
        requests=len(SOURCES), requested_seconds=sum(s[1] for s in SOURCES), conservative_bound=1800,
        actual_header_credits=spent, included_before=remaining,
        reported_account_delta=after.get('character_count',0)-before.get('character_count',0),
        plan=plan, no_topup_or_overage=True, pricing_source=PRICE_SOURCE,
        note='Account quota endpoint may lag; per-response character-cost headers are the spend ledger.'))


def process_all():
    CANDIDATES.mkdir(parents=True, exist_ok=True); BASE.mkdir(parents=True, exist_ok=True)
    sources = {name:read_audio(CANDIDATES/(name+'.mp3')) for name, *_ in SOURCES}
    candidate_metrics = {}
    for name in ('stone_near','stone_far','stone_side','stone_alternative','detonation_pressure','detonation_cavity'):
        candidate_metrics[name] = metrics(sources[name])
        write_wav(CANDIDATES/(name+'.wav'), level(ramps(sources[name]), -6))
    # Objective first-pass choices, not a substitute for listening review.
    stones = ['stone_near','stone_alternative']
    chosen_stone = min(stones, key=lambda n:candidate_metrics[n]['energy_above_2000hz'])
    blasts = ['detonation_pressure','detonation_cavity']
    chosen_blast = max(blasts, key=lambda n:candidate_metrics[n]['energy_below_250hz'] - .1*candidate_metrics[n]['active_onset_seconds'])
    selection = dict(stone_near_selected=chosen_stone, detonation_selected=chosen_blast,
        method='Bounded two-candidate first-pass choice: lower high-frequency energy for stone; strong sub250Hz energy and prompt onset for blast. Neither criterion certifies perceived material or dramatic quality.',
        candidate_metrics=candidate_metrics, human_audition='Required. No listening-analysis tool is available in this session; source, alternatives and review mixes are retained for in-app playback.')
    save_json(QA/'audio-selection.json',selection)
    assets = []

    def asset(name,x,db,source_names,processing,loop=False):
        x=level(x,db); path=BASE/(name+'.wav'); verification=write_wav(path,x)
        requests=[json.loads((CANDIDATES/(n+'.request.json')).read_text()) for n in source_names if (CANDIDATES/(n+'.request.json')).exists()]
        entry=dict(id=name, provider='ElevenLabs source + local authored audio design',asset=rel(path),
            asset_sha256=digest(path),loop=loop,output='PCM16 stereo 44100Hz',
            sources=[dict(id=n,path=rel(CANDIDATES/(n+'.mp3')),sha256=digest(CANDIDATES/(n+'.mp3'))) for n in source_names],
            processing=processing,verification=verification,
            license=requests[0]['license'] if requests else {},listening_review='Human audition pending; non-silence, decoding, headroom, envelope and spectral checks passed.')
        save_json(path.with_suffix('.provenance.json'),entry);assets.append(entry)
        return x

    asset('ambient_stone',loop_design(sources['stone_bed'],18,1200,301),-18,['stone_bed'],
          '18s stereo decorrelated bed; 28Hz high-pass, 1200Hz low-pass,240ms cycle overlap, endpoint continuity. Source is intentionally quiet. No engine baked in.',True)
    for i,n in enumerate((chosen_stone,'stone_far','stone_side'),1):
        x=design_tail(trim_onset(sources[n],.08),4.2+i*.2,1250-i*160,2.1+i*.15)
        asset('stone_resonance_%02d'%i,x,-11-i,[n],'Dense original mineral source; smooth band filtering + irregular stereo cavity taps with progressively darker decays. No bell synthesis or reused hull source.')

    engine_path=ROOT/'Assets/_Project/Audio/Zone01/Machinery/ambient_propulsion_thrust.wav'
    engine=read_audio(engine_path)
    engine=engine[:round(2.7*SR)]
    # Audible source identity remains, while time-varying bands/decorrelation create
    # a reflection rather than a plain delayed or gain/pitch-shifted engine copy.
    # Independently level the bands: the original propulsion is bass-heavy, so a
    # mere filtered sum would hide all cavity flutter below the main engine bass.
    low=level(filter_band(engine,45,210),-17)
    mid=level(filter_band(engine,310,940),-14)
    upper=level(filter_band(engine,1100,2400),-21)
    echo=np.zeros((round(5.4*SR),2))
    for band,at,gain,pan,rate in ((low,.13,.66,-.6,7.1),(low,.63,.31,.65,5.3),
                                  (mid,.31,.34,.7,11.7),(mid,.88,.18,-.8,8.3),(upper,.54,.10,-.4,17.1)):
        t=np.arange(len(band))/SR
        flutter=.72+.28*np.sin(2*np.pi*rate*t+.4*np.sin(2*np.pi*.43*t))
        put(echo,band*flutter[:,None],at,gain,pan)
    texture=design_tail(filter_band(sources['engine_reflection_texture'],180,1300),4.7,1300,2.7)
    put(echo,level(texture,-18),.17,.68,.15)
    echo_peak=float(np.max(np.abs(echo)))
    echo=np.tanh(echo/max(echo_peak,1e-10)*2.0)/np.tanh(2.0)
    echo=ramps(echo,.09,.7)
    echo=asset('engine_reflection',echo,-12,['engine_reflection_texture'],
          '5.4s designed reflection. Real propulsion source independently levelled in45-210,310-940,1100-2400Hz bands to reveal cavity midrange; .13-.88s asymmetric stereo taps, nonuniform7.1-17.1Hz flutter, new generated180-1300Hz mineral cavity texture, mild tanh soft saturation to bring low reflections above idle machinery, dark late diffusion,700ms release. Direct engine is excluded. Runtime must trigger on actual propulsion stop.',False)
    engine_proof=BASE/'engine_reflection.provenance.json'
    entry=json.loads(engine_proof.read_text());entry['additional_source']=dict(path=rel(engine_path),sha256=digest(engine_path));save_json(engine_proof,entry);assets[-1]=entry

    asset('creature05_pressure',ramps(filter_band(trim_onset(sources['creature05_pressure']),50,4200)), -13,['creature05_pressure'], 'Isolated irregular wet pressure micro-bursts,50Hz high-pass/4.2kHz low-pass,6ms attack/80ms release; no creature gameplay baked in.')
    asset('creature06_crystal_motion',ramps(filter_band(trim_onset(sources['creature06_motion']),140,4800)), -14,['creature06_motion'], 'Biological hard-plate source;140Hz high-pass/4.8kHz low-pass, short edge ramps. No glass/chime layering.')
    asset('creature07_bioelectric',ramps(filter_band(trim_onset(sources['creature07_discharge']),55,6500)), -12,['creature07_discharge'], 'Irregular original bioelectric pulses,55Hz high-pass/6.5kHz low-pass; no regular pulse or laser synthesis.')

    radar_path=ROOT/'Assets/_Project/Audio/Zone01/Interface/sfx_radar_scan_ping.wav'
    radar=trim_onset(read_audio(radar_path))[:round(.8*SR)]
    false=np.zeros((round(2.7*SR),2))
    for at,gain,pan,hi in ((.04,.5,-.65,2100),(.27,.27,.6,1450),(.63,.16,-.2,900),(.98,.07,.75,650)):
        filtered=filter_band(radar,450,hi)
        t=np.arange(len(filtered))/SR
        filtered*= (.65+.35*np.sin(2*np.pi*19*t))[:,None]
        put(false,filtered,at,gain,pan)
    put(false,level(filter_band(sources['radar_texture'],500,3400),-12),.13,.65,.35)
    false=asset('false_radar_echo',ramps(false,.008,.4),-15,['radar_texture'],
        'Familiar radar source unchanged on disk; derived ghost response only. 450Hz high-pass,2100→650Hz low-pass,19Hz amplitude flutter, asymmetric .04/.27/.63/.98s stereo taps plus new electrical flutter texture. No scan event or radar resource mutation.')
    entry=json.loads((BASE/'false_radar_echo.provenance.json').read_text());entry['additional_source']=dict(path=rel(radar_path),sha256=digest(radar_path));save_json(BASE/'false_radar_echo.provenance.json',entry);assets[-1]=entry
    asset('soft_movement',ramps(filter_band(sources['soft_movement'],120,3800),.06,.18),-16,['soft_movement'],'New near-field membrane/fabric-like source;120Hz high-pass/3.8kHz low-pass,60ms onset; no hull-scrape reuse.')
    asset('close_click',ramps(filter_band(trim_onset(sources['close_click']),130,6500),.002,.04),-14,['close_click'],'Isolated close relay click, trimmed leading silence with6ms lead; short2ms attack/40ms release.')
    assembly=asset('rock_breaker_assembly',design_tail(trim_onset(sources['assembly']),3.6,6500,8),-8,['assembly'],'Original functional latches/actuator/engagement source, restrained short room taps, no horror stinger or confirmation melody.')
    blast_source=trim_onset(sources[chosen_blast],.003)
    bass=filter_band(blast_source,0,180)
    centered_bass=np.repeat(bass.mean(axis=1)[:,None],2,axis=1)
    blast_source=blast_source-bass+centered_bass
    blast=asset('underwater_detonation',design_tail(blast_source,6.3,980,3.1),-4,[chosen_blast],
                'Selected original blast by sub250Hz ratio/onset check; trimmed to3ms preattack, bass below180Hz centered for mono compatibility,32Hz high-pass/980Hz low-pass, multi-tap submerged cavity decay6.3s. No debris baked in so existing demolition timing can drive separate layers.')
    collapse=[]
    for i,n in enumerate(('collapse_large','collapse_medium','collapse_grit'),1):
        collapse.append(asset('rock_collapse_%02d'%i,design_tail(trim_onset(sources[n],.025),3.3-i*.2,1700+i*700,2),-8-i*2,[n],
                              'Distinct original large-block / medium-tumble / small-grit sources, damped uneven granular decay; separate layer for authored demolition offsets.'))
    aftermath=asset('post_explosion_resonance',design_tail(sources['post_resonance'],7.2,1100,4.5),-15,['post_resonance'],
                    'Original mineral aftermath source with7.2s widening, progressively filtered cavity tail; no new exit scare.')
    water=asset('sparse_water',loop_design(sources['sparse_water'],15,1800,312),-22,['sparse_water'],
                '15s decorrelated original sparse deep-water bed,28Hz high-pass/1.8kHz low-pass and240ms loop overlap; endpoint continuity. Not a silent audio file.',True)

    save_json(BASE/'generation-requests.json',[json.loads((CANDIDATES/(n+'.request.json')).read_text()) for n,*_ in SOURCES])
    save_json(QA/'audio-manifest.json',assets)
    # Clearly labelled offline demonstrations of the exact production assets.
    demo=np.zeros((round(12*SR),2));put(demo,level(engine,-16),.7);put(demo,echo,3.7)
    a=write_wav(QA/'audio-demo-A-engine-then-reflection.wav',ramps(demo,.01,.1))
    demo=np.zeros((round(14*SR),2));put(demo,assembly,0,.7);put(demo,blast,4,.8)
    for x,t in zip(collapse,(4.38,5.12,6.05)):put(demo,x,t,.8)
    put(demo,aftermath,6.2,.7)
    d=write_wav(QA/'audio-demo-D-demolition.wav',level(ramps(demo,.01,.2),-3))
    # Equal-gap review reel makes source order verifiable without another player.
    reel=np.zeros((round(sum(x['verification']['duration_seconds']+1 for x in assets)*SR)+SR,2));offset=0
    for entry in assets:
        x=read_audio(ROOT/entry['asset']);put(reel,x,offset);offset+=len(x)/SR+1
    write_wav(QA/'audio-review-reel.wav',reel)
    save_json(QA/'audio-demo-timings.json',dict(A=dict(asset=rel(QA/'audio-demo-A-engine-then-reflection.wav'),
        direct_propulsion_start=.7,direct_propulsion_end=3.4,reflection_start=3.7,reflection_end=9.1,
        note='Offline audio demonstration, not captured Unity output. Direct source stops before designed reflection starts.',verification=a),
        D=dict(asset=rel(QA/'audio-demo-D-demolition.wav'),assembly=0,detonation=4,large_blocks=4.38,medium_rocks=5.12,grit=6.05,aftermath=6.2,
        note='Offline layer audition, not proof of runtime transaction timing.',verification=d),
        review_order=[x['id'] for x in assets]))
    print(json.dumps(dict(processed=len(assets),asset_directory=rel(BASE),candidate_selection=selection['detonation_selected'],demo_A='audio-demo-A-engine-then-reflection.wav',demo_D='audio-demo-D-demolition.wav')),flush=True)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--execute',action='store_true');parser.add_argument('--process',action='store_true')
    args=parser.parse_args()
    if args.execute:
        produce()
    if args.execute or args.process:
        process_all()
    else:
        print(json.dumps(dict(dry_run=True,requests=len(SOURCES),source_seconds=sum(s[1] for s in SOURCES),
                              price_bound=RATE_BOUND,credit_bound=1800,assets=rel(BASE))))


if __name__=='__main__':
    try:
        main()
    except urllib.error.HTTPError as error:
        print('ElevenLabs HTTP status',error.code,file=sys.stderr);sys.exit(1)
    except urllib.error.URLError:
        print('ElevenLabs network request failed',file=sys.stderr);sys.exit(1)
    except Exception as error:
        print('Audio production stopped:',type(error).__name__,file=sys.stderr);sys.exit(1)
