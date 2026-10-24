# Quick photo flow restored — 2026-10-21

The user confirmed the intended flow: cabin quick shutter → Photo Lab → SEND. The added observation screen and shortcuts were a mistaken implementation and have been removed.

Removed from the saved Zone01 scene: Helm Observation, Upgrade CheckExternalLight, live image layers, observation caption, return-to-live and observation-close buttons, and the live camera component. Removed the associated runtime scripts, editor installer and obsolete observation tests through Unity AssetDatabase. Removed the installer call and observation entry method; re-running the Zone 1 audio installer does not create them again. The pre-existing legacy CameraPanel stays inactive and is not opened by the gameplay shutter.

Kept: real quick-photo capture/archive/SEND ownership, Bio Lamp illumination in new still photographs, existing archived-image immutability, and the improved pressure cue timing. No Zone 2–4 progression changes.

Validation: **19/19 PlayMode tests passed** (`playmode.json`): all 14 PhotoSubmission tests plus five quick-photo/lighting/pressure tests. The new end-to-end test presses the actual cabin shutter, verifies one charge and one stored photograph, checks that no Camera panel opens, opens Photo Lab and presses its actual SEND button. It checks the mission remains incomplete before SEND and completes only after the durable submission.

`01-photo-lab-before-send.png` and `02-photo-lab-after-send.png` are actual Game View captures of the corrected flow. Test archives and saves use temporary isolated paths. The earlier observation screenshots/results are historical and superseded. No commit or push.
