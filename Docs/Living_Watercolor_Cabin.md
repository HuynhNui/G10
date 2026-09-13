# Living watercolor cabin

The existing watercolor PNG remains intact. New effects use ordinary uGUI alpha
blending; there is no bloom, additive neon material, new camera, render texture,
post-processing stack or 3D conversion. Generated textures are replaceable soft
placeholders, not painted replacements for the supplied artwork.

## Installation and layers

Open GameplayCore and Zone01, outside Play Mode. Choose
**G10 → Atmosphere → Install Living Watercolor Cabin**, then save Zone01.
The installer is additive, Undo-aware and leaves an existing installation alone.
Scene assets are authored through Unity Editor APIs, never direct YAML editing.
After changing effect opacity, use **G10 → Atmosphere → Sync Authored FX Opacity**
to synchronize the stopped Editor preview, then save the scene.

Under `Cabin Station/CabinCanvas/CabinFrame`:

```
CabinViewport                         clip + visibility + mood
  CameraBreathingRig                  gentle presentation drift/zoom, 2% overscan
    CabinMotion                       floating artwork and matching hotspots
      Background/CabinArt             original PNG
      Midground_Instruments           soft screen washes, lamps, ambient sweep/needle
      FX_WaterAndPigment               caustics, pigment wash, paper
      Foreground_Particles            two native ParticleSystems, drawn into uGUI
      Interaction                     existing cabin hotspots
NavigationPanel / MapPanel / RadarPanel / ComputerScreen / other panels
```

Sibling order defines compositing. All effects have `raycastTarget=false`.
Art and hotspots share both motion rigs. Functional panels stay outside those rigs
and remain stable; the cabin presentation hides while a panel is open. The existing
UIManager still owns blocking and Escape. Movement, collision and gameplay radar
calculations are not changed by this feature.

This source is flattened: true separate wall/floor/furniture parallax would require
layered transparent art. The current groups support replacing the background and
adding foreground cutouts later. Particles and independently moving wash layers
provide limited depth without inventing or cutting out painted objects.

## Inspector tuning

| Behaviour / object | Defaults / purpose |
|---|---|
| CabinBob | 2 px horizontal, 3 px vertical, 0.12° roll; independent slow frequencies |
| CameraBreathing | 0.8/0.6 px drift, 0.15% zoom; separate rig so it does not fight Canvas scaling |
| CausticScroller | UV drift 0.006/0.003 per second, 4.5% opacity, 12% relative alpha pulse |
| PaperTexture RawImage | 4.5% tint alpha, additionally attenuated by grain texture alpha |
| WatercolorBreathing | 3.8% pigment alpha, slow 0.027 Hz cycle; screen instances use 3.5–6.5% |
| RandomBlink | Seeded 3–7 second intervals and 0.8 second fades; no gameplay RNG changes |
| GaugeJitter | At most 0.65° of smooth noise on a decorative needle only |
| RadarSweep | 14°/second, low alpha; ambient mark, never a fake contact or scan command |
| CabinDust | 0.55 particles/second; at most 30 |
| WindowMotes | 0.25 particles/second; at most 14 |

Units on UI rigs are pixels at the 1920×1080 authoring size. Keep amplitudes low and
overscan above maximum motion so edges never reveal gaps. For a sprite scene,
CabinBob and CameraBreathing can use world-unit amplitudes; reduce the default pixel
values accordingly. CameraBreathing optionally accepts an orthographic Camera and
then pulses its orthographicSize instead of the rig scale. Do not assign both a
camera and the UI surrogate in this existing overlay setup.

Caustic textures must use Repeat wrap. SoftDot uses Clamp. Swap textures on RawImage
or UIParticleAtmosphere in Inspector. Parameters are serialized public Inspector
fields. Animation restores base transforms/colors when disabled and does not
accumulate drift. Most effects run only in Play Mode to keep authored poses stable.

UIParticleAtmosphere bridges a local-space ParticleSystem into the overlay canvas;
its ParticleSystemRenderer stays disabled to avoid duplicate world rendering.
Keep simulationSpace Local and capacity at least maxParticles; re-enable after
changing capacity. The mesh refreshes at 30 Hz using a preallocated particle array.

## Mood states

Select CabinViewport → CabinAtmosphere. Normal, LowPower, Danger and ScanActive each
expose light, motion, caustics, screen glow, particle alpha and blink-rate multipliers.
Transitions ease over two seconds. Danger increases activity slightly without camera
shake, red flashing or palette changes. LowPower and Danger are manual presentation
states until real resource/damage systems exist. No fake energy system is introduced.

With Follow Real Radar Scan enabled, actual RadarDisplay.IsScanning temporarily
selects ScanActive from Normal. The gameplay radar stays the source of truth.
The full-screen radar panel itself retains its existing real sweep.

## Validation

Validated in Unity 6000.4.2f1 on 2026-09-11: all 8 Play Mode tests passed.
Coverage includes bounded motion, shared art/hotspot transforms, foreground raycasts,
stable modal panels, mood response, actual particle simulation, particle caps and
pose restoration, alongside computer Phase A/B, navigation and scene-flow regressions.
Native Editor visual checks confirmed the soft cabin presentation, opening the
computer by clicking its monitor, stable readable desktop and returning to the cabin.
Zone01 contains the saved installation. Standalone player build and GPU profiling
were not performed.

No camera/capture gameplay, coordinate foundation or spawn-region work is included.
