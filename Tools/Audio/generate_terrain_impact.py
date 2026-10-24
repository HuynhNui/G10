"""One bounded ElevenLabs generation for the explicitly requested terrain collision SFX.

Credentials stay in the process; neither account responses nor HTTP bodies are logged.
The source and provenance are retained. Run without --execute for a quota-only preflight.
"""
import argparse
import array
import hashlib
import json
import math
import os
from pathlib import Path
import subprocess
import sys
import urllib.error
import wave

from generate_sound import ROOT, request

PROMPT = (
    "Single submarine steel pressure hull colliding with a rocky underwater cliff, heard "
    "from INSIDE the small submarine cabin. Immediate heavy low metallic THUD at the "
    "very start, short gritty rock scrape and loose metal rattle, deep structural steel "
    "resonance decaying naturally into silence. Physical grounded foley, close and dry, "
    "muffled underwater weight. One impact only, no explosion, no voices, no music, no "
    "alarm, no engine bed, no long lead-in."
)
SOURCE = ROOT / "Assets/_Project/Audio/Zone01/Sources/sfx_terrain_impact_source.mp3"
OUTPUT = ROOT / "Assets/_Project/Audio/Zone01/SFX/sfx_terrain_impact.wav"
QUOTE = 80  # 2 s * 40 credits/s, official Sound Effects documentation reviewed 2026-10-21.


def credentials():
    key = os.environ.get("ELEVENLABS_API_KEY")
    if not key:
        private = Path.home() / ".codex/credentials/g10-elevenlabs.env"
        for line in private.read_text(encoding="utf-8-sig").splitlines():
            name, sep, value = line.strip().partition("=")
            if sep and name.strip().removeprefix("export ") == "ELEVENLABS_API_KEY":
                key = value.strip().strip("\"'")
                break
    if not key:
        raise RuntimeError("ElevenLabs credentials unavailable.")
    return key


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--execute", action="store_true")
    args = parser.parse_args()
    key = credentials()
    with request("/v1/user/subscription", key) as response:
        sub = json.load(response)
    remaining = sub.get("character_limit", 0) - sub.get("character_count", 0)
    plan = sub.get("tier", "unknown")
    print(json.dumps(dict(plan=plan, included_credits_remaining=remaining, generation_credits=QUOTE)))
    if not args.execute:
        return
    if remaining < QUOTE:
        raise RuntimeError("Insufficient included credits; no overage or top-up allowed.")
    if SOURCE.exists() or OUTPUT.exists():
        raise RuntimeError("Refusing to overwrite an existing audio asset.")
    payload = dict(text=PROMPT, duration_seconds=2, loop=False,
                   model_id="eleven_text_to_sound_v2", prompt_influence=.65)
    with request("/v1/sound-generation?output_format=mp3_44100_128", key, payload) as response:
        data = response.read()
        billed = response.headers.get("character-cost", "unknown")
    if not data:
        raise RuntimeError("Empty audio response.")
    SOURCE.parent.mkdir(parents=True, exist_ok=True)
    SOURCE.write_bytes(data)
    decoded = SOURCE.with_suffix(".decoded.wav")
    try:
        subprocess.run(["ffmpeg", "-v", "error", "-i", str(SOURCE), "-ar", "44100", "-ac", "2", "-c:a", "pcm_s16le", str(decoded)], check=True)
        with wave.open(str(decoded), "rb") as stream:
            samples = array.array("h", stream.readframes(stream.getnframes()))
        peak = max(abs(v) for v in samples)
        if peak < 128:
            raise RuntimeError("Generated audio is effectively silent; source retained for review.")
        # Trim only silence before the attack, leaving a 3 ms lead and a 2 ms anti-click ramp.
        threshold = max(64, peak * .015)
        start = next(i // 2 for i, value in enumerate(samples) if abs(value) >= threshold)
        start = max(0, start - 132)
        samples = samples[start * 2:]
        gain = (32767 * 10 ** (-3 / 20)) / peak
        frames = len(samples) // 2
        for i in range(len(samples)):
            frame = i // 2
            envelope = min(1, frame / 88, (frames - 1 - frame) / 882)
            samples[i] = round(samples[i] * gain * max(0, envelope))
        OUTPUT.parent.mkdir(parents=True, exist_ok=True)
        with wave.open(str(OUTPUT), "wb") as stream:
            stream.setnchannels(2); stream.setsampwidth(2); stream.setframerate(44100)
            stream.writeframes(samples.tobytes())
        peak_final = max(abs(v) for v in samples) / 32768
        rms = math.sqrt(sum((v / 32768) ** 2 for v in samples) / len(samples))
        provenance = dict(id="sfx_terrain_impact", provider="ElevenLabs", model=payload["model_id"],
                          prompt=PROMPT, duration_requested_seconds=2, billed_credits=billed,
                          reviewed_quote_credits=QUOTE,
                          pricing_source="https://elevenlabs.io/docs/overview/capabilities/sound-effects",
                          license=dict(plan_at_generation=plan, commercial_use=plan not in ("free", "unknown"),
                                       source="https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform"),
                          source_asset=str(SOURCE.relative_to(ROOT)).replace("\\", "/"),
                          source_sha256=hashlib.sha256(data).hexdigest(),
                          processing=dict(trim_start_seconds=start / 44100, gain_db=20 * math.log10(gain),
                                          output="stereo 44100 Hz PCM16 WAV; 2 ms attack ramp, 20 ms tail ramp"),
                          verification=dict(duration_seconds=frames / 44100, peak_dbfs=20 * math.log10(peak_final),
                                            rms_dbfs=20 * math.log10(rms), clipped_samples=sum(abs(v) >= 32767 for v in samples)),
                          asset_sha256=hashlib.sha256(OUTPUT.read_bytes()).hexdigest(),
                          listening_review="Human listening review pending; decode and level checks completed.")
        OUTPUT.with_suffix(".provenance.json").write_text(json.dumps(provenance, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(dict(output=str(OUTPUT), billed_credits=billed, **provenance["verification"])))
    finally:
        decoded.unlink(missing_ok=True)


if __name__ == "__main__":
    try:
        main()
    except urllib.error.HTTPError as error:
        print("ElevenLabs HTTP status", error.code, file=sys.stderr); sys.exit(1)
    except urllib.error.URLError:
        print("ElevenLabs network request failed.", file=sys.stderr); sys.exit(1)
    except Exception as error:
        # Do not print exception representations from credential/network parsing.
        print("Audio generation stopped:", type(error).__name__, file=sys.stderr); sys.exit(1)
