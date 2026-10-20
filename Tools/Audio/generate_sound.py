"""Offline by default. Never reads a key unless an explicitly approved generation is requested."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import sys
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = Path(__file__).with_name('sound-manifest.json')


def request(path, key, payload=None):
    data = None if payload is None else json.dumps(payload).encode('utf-8')
    req = urllib.request.Request('https://api.elevenlabs.io' + path, data=data,
                                 headers={'xi-api-key': key, 'Content-Type': 'application/json'})
    return urllib.request.urlopen(req, timeout=120)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--id', help='One stable sound ID; omitted lists all concepts offline.')
    parser.add_argument('--execute', action='store_true', help='Make ONE paid generation after preflight.')
    parser.add_argument('--approved-credit-budget', type=int, default=0)
    parser.add_argument('--reviewed-quote-credits', type=int, default=0,
                        help='Quote verified against current API/account pricing before human approval.')
    parser.add_argument('--license-note', default='', help='Reviewed account plan and commercial rights.')
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
    if not args.id:
        if args.execute:
            parser.error('Generation requires exactly one --id; batch spending is disabled.')
        for sound in sorted(manifest['sounds'], key=lambda s: s['priority']):
            print(sound['id'], sound['duration_seconds'], 'loop' if sound['loop'] else 'one-shot', sound['status'])
        return
    sound = next((s for s in manifest['sounds'] if s['id'] == args.id), None)
    if sound is None:
        parser.error('Unknown sound ID.')
    output = (ROOT / manifest['asset_root'] / sound['filename']).resolve()
    if not output.is_relative_to((ROOT / manifest['asset_root']).resolve()):
        parser.error('Output must stay inside the horror asset directory.')
    payload = dict(text=sound['prompt'], duration_seconds=sound['duration_seconds'],
                   loop=sound['loop'], model_id=manifest['model_id'], prompt_influence=manifest['prompt_influence'])
    if not args.execute:
        print(json.dumps({'dry_run': True, 'output': str(output), 'request': payload}, indent=2))
        return
    if not (0 < args.reviewed_quote_credits <= args.approved_credit_budget and args.license_note.strip()):
        parser.error('Requires explicit approved budget, reviewed current quote within budget, and license note.')
    if output.exists() or output.with_suffix(output.suffix + '.meta').exists():
        parser.error('Refusing to overwrite an existing asset or its Unity meta.')
    key = os.environ.get('ELEVENLABS_API_KEY')
    if not key:
        parser.error('ELEVENLABS_API_KEY is unavailable. Use dry-run; no request was made.')
    # Read-only quota check. Do not log the account response, credentials or HTTP error body.
    with request('/v1/user/subscription', key) as response:
        subscription = json.load(response)
    remaining = subscription.get('character_limit', 0) - subscription.get('character_count', 0)
    if remaining < args.reviewed_quote_credits:
        parser.error('Insufficient included credits; overage/top-up generation is disabled.')
    with request('/v1/sound-generation?output_format=' + manifest['output_format'], key, payload) as response:
        audio = response.read()
        billed = response.headers.get('character-cost', 'unknown')
    if not audio:
        raise RuntimeError('Generation returned no audio.')
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open('xb') as stream:
        stream.write(audio)
    provenance = dict(id=sound['id'], provider='ElevenLabs', model=manifest['model_id'],
                      prompt=sound['prompt'], sha256=hashlib.sha256(audio).hexdigest(),
                      billed_credits=billed, reviewed_quote_credits=args.reviewed_quote_credits,
                      license_note=args.license_note, status='generated_pending_listening_review')
    output.with_suffix('.provenance.json').write_text(json.dumps(provenance, indent=2) + '\n', encoding='utf-8')
    print('Generated one asset; listening, license and loop review are required before assigning it:', output)


if __name__ == '__main__':
    try:
        main()
    except urllib.error.HTTPError as error:
        print('ElevenLabs request failed; HTTP status', error.code, file=sys.stderr)
        sys.exit(1)
    except urllib.error.URLError:
        print('ElevenLabs network request failed.', file=sys.stderr)
        sys.exit(1)
