"""Offline-only derivative of already licensed Zone02 sources. No network or credits."""
import array
import hashlib
import json
import pathlib
import subprocess
import wave

ROOT = pathlib.Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets/_Project/Audio/Zone02"
FILTER = "[0:a]lowpass=f=1100,volume=0.55,afade=t=in:d=0.25,pan=stereo|c0=0.80*c0|c1=0.45*c1[a];[1:a]lowpass=f=1800,volume=0.18,adelay=800|950,afade=t=out:st=2.5:d=0.8[b];[a][b]amix=inputs=2:duration=longest:normalize=0,alimiter=limit=0.75:level=0,atrim=duration=3.4"

def main():
    output = ASSETS / "following_tail.wav"
    command = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(ASSETS / "outside_movement.wav"),
               "-i", str(ASSETS / "root_friction.wav"), "-filter_complex", FILTER, "-ar", "44100", "-ac", "2", "-c:a", "pcm_s16le", str(output)]
    subprocess.run(command, check=True)
    sources = []
    for name in ("outside_movement", "root_friction"):
        source = ASSETS / (name + ".wav")
        provenance = json.loads((ASSETS / (name + ".provenance.json")).read_text(encoding="utf-8-sig"))
        sources.append({"path": source.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(source.read_bytes()).hexdigest(), "license": provenance["license"]})
    with wave.open(str(output), "rb") as audio:
        values = array.array("h", audio.readframes(audio.getnframes()))
        verification = {"channels": audio.getnchannels(), "sample_rate": audio.getframerate(),
                        "duration_seconds": audio.getnframes() / audio.getframerate(), "peak": max(abs(x) for x in values) / 32768,
                        "rail_samples": sum(abs(x) >= 32767 for x in values)}
    data = {"id": "following_tail", "method": "offline derivative; no API request, purchase or new credit consumption",
            "sources": sources, "filter_complex": FILTER, "output_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
            "license": {"commercial_use": False, "terms": "Inherited recorded ElevenLabs Free noncommercial/attribution source terms; no new license granted."},
            "verification": verification, "human_listening": "pending; numerical checks do not establish perceptual quality"}
    (ASSETS / "following_tail.provenance.json").write_text(json.dumps(data, indent=2), encoding="utf-8")
    print(json.dumps(verification))

if __name__ == "__main__":
    main()
