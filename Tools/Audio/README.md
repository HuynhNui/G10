# Offline horror sound production

The official ElevenLabs `sound-effects` skill is installed in the local Codex skill directory. Source: https://github.com/elevenlabs/skills/tree/main/sound-effects . It is available in subsequent Codex turns. No project credentials are stored. `ELEVENLABS_API_KEY` was absent during implementation; zero remote generation requests were made.

`existing-audio-inventory.json` records 25 existing audio files with checksums. The shipped resource copies of the ambient and SFX assets match their authoring counterparts. Reuse existing impact, hull creak, exterior scrape, critical warning, power-down and low-power hum without moving or overwriting artist assets. Existing provenance must be reviewed separately; filenames do not establish a license.

`sound-manifest.json` contains all 20 requested concepts, stable IDs, English prompts, duration, looping, context, mix gain, processing, status and licensing metadata. Priorities 1–5 are the requested first batch. No manifest item is represented as generated audio.

## Offline commands

```powershell
python Tools/Audio/generate_sound.py
python Tools/Audio/generate_sound.py --id horror.knocks.three
```

Both commands are offline and require no key. The second prints the exact planned request and output. Generation is deliberately one asset per invocation, refuses overwrites, and requires `--execute`, a human-approved credit budget, a current reviewed quote and a reviewed license note. Without those inputs the command exits before reading credentials or making requests. Do not pass secrets as CLI arguments.

Before authorizing generation, review current API/account pricing and commercial rights on the actual account. The script checks included quota via `/v1/user/subscription`, rejects insufficient quota and records the returned `character-cost` header when available. A reviewed quote is an estimate, not a server-enforced billing ceiling; do not generate if the account quote is uncertain. No automatic retries, batches, top-ups or overage requests are implemented.

Example after separate explicit human approval (replace budget/quote with the approved numbers):

```powershell
python Tools/Audio/generate_sound.py --id horror.knocks.three --execute --approved-credit-budget APPROVED_NUMBER --reviewed-quote-credits REVIEWED_NUMBER --license-note "Reviewed account plan, date and commercial-use terms"
```

Use a securely supplied process environment variable. Do not run onboarding that writes keys into this Unity repository. The upstream skill recommends onboarding when credentials are missing; this task explicitly requires an offline fallback and prohibits storing project keys, so onboarding was skipped.

The pinned request uses `eleven_text_to_sound_v2`, `mp3_44100_128`, explicit duration and loop parameters. This avoids requesting higher-tier PCM/WAV output. Official endpoint documentation: https://elevenlabs.io/docs/api-reference/text-to-sound-effects/convert . Pricing: https://elevenlabs.io/pricing . Terms: https://elevenlabs.io/terms-of-use . Recheck these before paid work.

## Review and admit an asset

1. Check the generated provenance sidecar, rights and checksum. Listen for unexpected voices, music, loud transients and artifacts. Retain headroom; avoid normalizing every quiet ambience to full scale.
2. Audition loop boundaries and adjust offline in the team's audio editor if necessary. Do not overwrite originals; preserve raw output and document derivatives.
3. Let Unity import the asset and commit its `.meta` alongside it when the developer later commits. Use compressed-in-memory for short effects, streaming only for genuinely long ambience after profiling. Avoid loading every concept at startup.
4. Assign the reviewed clip to an EffectDefinition, select its AudioCategory and tune gain/pan/low-pass/fade. For long environmental loops select UnderwaterAmbient; essential machinery and warning sounds stay in protected categories. The existing AudioManager output preference remains the final volume control.
5. Preview in `G10 > Horror > Presentation Preview`. Three knocks currently demonstrates rhythm using an explicitly labeled existing creak sample; `horror.knock` has no production clip until reviewed audio is assigned. Artist silhouette sprites are also optional and unassigned.
6. Update the manifest status and provenance after review. Unity contains no ElevenLabs SDK, API key, networking integration or runtime generation.
