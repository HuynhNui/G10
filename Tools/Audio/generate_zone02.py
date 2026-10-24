"""Generate the authorized Zone 2 SFX package, resumably, within included quota only."""
import argparse
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

BASE = ROOT / 'Assets/_Project/Audio/Zone02'
# id, seconds, loop, peak dBFS, description. No speech and no music generation.
SOUNDS = [
 ('ambient_roots',20,True,-13,'Seamless quiet deep underwater environment inside a vast submerged alien root forest. Very low distant organic woody tension and still water pressure, sparse hollow resonances, stable subdued bed under a submarine engine. No wind, birds, engine, voices, human breathing, music, rhythmic pulse, impacts or sudden loud sounds.'),
 ('root_flex',3,False,-11,'One distant giant submerged wooden root slowly flexes under deep water pressure, low soft creaking organic fibers and hollow woody resonance, subdued natural environmental foley, gentle decay. No voice, music, engine, jump scare or loud impact.'),
 ('root_friction',2.5,False,-12,'Two huge wet fibrous roots gently rub past each other underwater, soft dry-wet organic friction with a faint metallic overtone, short irregular restrained environmental texture, then silence. No scream, voices, music, impact or engine.'),
 ('specimen_adhesive',3,True,-12,'Seamless extremely quiet close biological foley of a soft translucent underwater creature secreting thick adhesive threads, tiny moist stretching and sticky membrane movements. Sparse delicate wet texture, not disgusting, no mouth, voice, breathing, music, engine or rhythmic pulse.'),
 ('capture_adhesive',2,False,-9,'A small mechanical collection claw gently separates stretchy adhesive fibers from a soft underwater organism. Restrained moist elastic pull, tiny soft release and short damped tool click, one collection action, no scream, music, voice, explosion or engine.'),
 ('specimen_armor',3,True,-12,'Seamless quiet close foley of a small armored underwater creature subtly shifting overlapping hardened woody shell plates. Sparse soft dry plate rubs and tiny dull clicks, natural biological texture, no footsteps, voice, music, horror sting, loud hit or engine.'),
 ('capture_armor',1.4,False,-8,'One small submarine collection claw contacts a hard woody armored shell, a single dry solid clack followed by a short coarse plate friction and restrained mechanical settling. Grounded close foley, no explosion, voice, music or cinematic hit.'),
 ('radar_static',.65,False,-10,'One brief restrained analog radar electrical interference burst, thin static tick and fuzzy signal tear, immediate attack and quick decay into silence. Small old scientific instrument, no alarm, voice, music, loud shriek or repeating beeps.'),
 ('outside_movement',2,False,-11,'One very low muffled mass slowly moves past outside a steel submarine hull in deep water. Brief deep underwater displacement and soft low pressure resonance then silence. Unidentified physical environmental sound, no impact, scream, voice, music, jump scare or engine.'),
 ('hull_scrape',6,False,-7,'One continuous long slow coarse scrape traveling along the OUTSIDE steel hull of a small submerged submarine, heard from inside, beginning softly then sustained gritty metallic friction then receding into silence. Physical intimate krrrrrk texture, no collision thud, voices, music, alarm, engine or explosion.'),
 ('cabin_tick',.55,False,-11,'One very small dry loose metal relay tick inside a quiet submarine cabin, intimate restrained single clack, short tail then silence. No other sound, voice, music or alarm.'),
 ('electric_fizz',.65,False,-11,'One short quiet electrical contact fizz as an old submarine display briefly dims and recovers. Fine dry static crackle, restrained level, no loud snap, explosion, alarm, voices or music.'),
 ('hull_install',4,False,-7,'Small submarine pressure hull reinforcement installation sequence: solid mechanical latches locking, short low electric motor whirr, one deep firm metallic settling clunk, finally one small clean confirmation beep. Practical trustworthy machinery, no horror, music, voice, explosion or engine bed.'),
 ('far_root_groan',4,False,-12,'One very distant deep wooden root groans slowly under water pressure then fades completely to silence. Hollow organic restrained environmental resonance, final quiet farewell to a submerged root forest. No voice, creature scream, music, impact, jump scare or engine.')
]

def write_wav(path, samples):
    pcm=np.rint(np.clip(samples,-.9999,.9999)*32767).astype('<i2')
    with wave.open(str(path),'wb') as f:
        f.setnchannels(2);f.setsampwidth(2);f.setframerate(44100);f.writeframes(pcm.tobytes())
    return dict(duration_seconds=len(samples)/44100,peak_dbfs=round(20*math.log10(float(np.abs(samples).max())),3),
                rms_dbfs=round(20*math.log10(float(np.sqrt(np.mean(samples**2)))),3),clipped_samples=int(np.sum(np.abs(pcm.astype('int32'))>=32767)),
                seam_step=float(np.abs(samples[0]-samples[-1]).max()))

def process(source, output, loop, peak):
    decoded=source.with_suffix('.decoded.wav')
    try:
        subprocess.run(['ffmpeg','-v','error','-i',str(source),'-ar','44100','-ac','2','-c:a','pcm_s16le',str(decoded)],check=True)
        with wave.open(str(decoded),'rb') as f: x=np.frombuffer(f.readframes(f.getnframes()),'<i2').reshape(-1,2).astype('float64')/32768
        if np.abs(x).max()<.001: raise RuntimeError('Silent generated source')
        if loop:
            n=6615;w=np.linspace(0,1,n)[:,None]
            x=np.concatenate([x[n:-n],x[-n:]*(1-w)+x[:n]*w])
        else:
            x[:88]*=np.linspace(0,1,88)[:,None];x[-1764:]*=np.linspace(1,0,1764)[:,None]
        x*=10**(peak/20)/np.abs(x).max()
        return write_wav(output,x)
    finally: decoded.unlink(missing_ok=True)

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--execute',action='store_true');args=parser.parse_args()
    key=credentials()
    with request('/v1/user/subscription',key) as r: sub=json.load(r)
    remaining=sub.get('character_limit',0)-sub.get('character_count',0);plan=sub.get('tier','unknown')
    required=math.ceil(sum(s[1]*40 for s in SOUNDS if not (BASE/'Sources'/(s[0]+'.mp3')).exists()))
    print(json.dumps(dict(preflight=True,plan=plan,remaining_included=remaining,conservative_quote_credits=required)),flush=True)
    if not args.execute:return
    if remaining<required:raise RuntimeError('Included quota insufficient; no top-up/overage allowed')
    (BASE/'Sources').mkdir(parents=True,exist_ok=True)
    manifest=[]
    for name,seconds,loop,peak,prompt in SOUNDS:
        source=BASE/'Sources'/(name+'.mp3');output=BASE/(name+'.wav');proof=output.with_suffix('.provenance.json')
        if output.exists() and proof.exists(): manifest.append(json.loads(proof.read_text()));continue
        billed='previous source retained'
        if not source.exists():
            payload=dict(text=prompt,duration_seconds=seconds,loop=loop,model_id='eleven_text_to_sound_v2',prompt_influence=.65)
            with request('/v1/sound-generation?output_format=mp3_44100_128',key,payload) as r: data=r.read();billed=r.headers.get('character-cost','unknown')
            if not data:raise RuntimeError('Empty generated audio')
            source.write_bytes(data)
        verification=process(source,output,loop,peak)
        entry=dict(id=name,provider='ElevenLabs',model='eleven_text_to_sound_v2',prompt=prompt,prompt_influence=.65,
                   duration_requested_seconds=seconds,loop=loop,billed_credits=billed,source_asset=str(source.relative_to(ROOT)).replace('\\','/'),
                   source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),asset_sha256=hashlib.sha256(output.read_bytes()).hexdigest(),
                   license=dict(plan_at_generation=plan,commercial_use=plan not in ('free','unknown'),source='https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform'),
                   processing='PCM16 stereo 44100 Hz, normalized headroom; loop overlap 150 ms or 2 ms attack / 40 ms release',verification=verification,
                   listening_review='Human listening review pending; decode and level checks completed.')
        proof.write_text(json.dumps(entry,indent=2)+'\n');manifest.append(entry)
        print(json.dumps(dict(id=name,billed_credits=billed,**verification)),flush=True)
    # Familiar Zone 1 source; central, filtered rear impression and a longer final echo. No new API request.
    knock=BASE/'knocks_rear.wav'
    if not knock.exists():
        source=ROOT/'Assets/_Project/Audio/Zone01/Sources/sfx_hull_knock_single_source.wav'
        with wave.open(str(source),'rb') as f: x=np.frombuffer(f.readframes(f.getnframes()),'<i2').reshape(-1,f.getnchannels()).astype('float64').mean(axis=1)/32768
        start=max(0,int(np.argmax(np.abs(x)>max(.002,np.abs(x).max()*.02)))-44);x=x[start:][:int(.9*44100)]
        x[-882:]*=np.linspace(1,0,882)
        mono=np.zeros(int(3.6*44100))
        for i,t in enumerate((.12,.72,1.32)):
            offset=round(t*44100);mono[offset:offset+len(x)]+=x
            if i==2:
                for delay,g in ((.11,.30),(.23,.18),(.39,.10)):
                    at=offset+round(delay*44100);mono[at:at+len(x)]+=x*g
        stereo=np.stack((mono,mono),axis=1);stereo*=10**(-9/20)/np.abs(stereo).max()
        verification=write_wav(knock,stereo)
        knock.with_suffix('.provenance.json').write_text(json.dumps(dict(id='knocks_rear',provider='Derived ElevenLabs source; no new request',
            source_asset=str(source.relative_to(ROOT)).replace('\\','/'),source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
            timing=[.12,.72,1.32],processing='Central stereo; final knock echoes at +.11/.23/.39 s. Playback low-pass/reverb provide a rear impression, not true positional audio.',
            billed_credits=0,license=manifest[0]['license'],verification=verification),indent=2)+'\n')
    with request('/v1/user/subscription',key) as r: after=json.load(r)
    manifest_file=BASE/'generation-requests.json';manifest_file.write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps(dict(done=True,new_sources=len(manifest),quota_delta=after.get('character_count',0)-sub.get('character_count',0))),flush=True)

if __name__=='__main__':
    try:main()
    except urllib.error.HTTPError as e:print('ElevenLabs HTTP status',e.code,file=sys.stderr);sys.exit(1)
    except Exception as e:print('Generation stopped:',type(e).__name__,file=sys.stderr);sys.exit(1)
