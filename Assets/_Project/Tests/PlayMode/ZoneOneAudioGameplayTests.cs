using System;
using System.Collections;
using System.IO;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Feedback;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.Tutorial;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class ZoneOneAudioGameplayTests
    {
        private string folder;
        private CabinStationView cabin;
        private ZoneOneStory story;
        private ZoneOneAudioController audio;
        private ExpeditionLoop loop;
        private EffectDirector effects;
        private ZoneNavigation Nav => cabin.Navigation;
        private CreatureCatcher Catcher => cabin.GetComponent<CreatureCatcher>();
        private PhotoCaptureService Photos => cabin.GetComponent<PhotoCaptureService>();
        [UnitySetUp] public IEnumerator Setup()
        {
            folder=Path.Combine(Application.temporaryCachePath,"ZoneOneAudio-"+Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride=Path.Combine(folder,"timeline.json");
            PhotoCaptureService.ArchivePathOverride=Path.Combine(folder,"photos");
            TutorialTestSave.SeedReturningPlayer();
            yield return Load();
        }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore",LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01",LoadSceneMode.Additive);
            yield return null;yield return null;
            cabin=Object.FindAnyObjectByType<CabinStationView>();loop=Object.FindAnyObjectByType<ExpeditionLoop>();
            story=cabin.GetComponent<ZoneOneStory>();audio=cabin.GetComponent<ZoneOneAudioController>();effects=cabin.GetComponent<EffectDirector>();
            Assert.That(loop.IsInitialized,Is.True,loop.LastError);Assert.That(audio,Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            yield return SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Single);
            if(SceneFlowController.Instance!=null)Object.Destroy(SceneFlowController.Instance.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride=null;PhotoCaptureService.ArchivePathOverride=null;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        private void Place(string poiId)
        {
            var poi=story.survey.FindPoi(poiId);Assert.That(poi,Is.Not.Null);
            Nav.Ship.Refill();Nav.RestoreVoyage(story.survey.ContactPosition(poi)+Vector2.down*14,0,story.survey.DepthFor(poi),Nav.DistanceTravelled);
        }
        private PhotoRecord Send(string poiId)
        {
            Place(poiId);var photo=Photos.Capture();Assert.That(photo,Is.Not.Null,Photos.LastError);
            Assert.That(photo.MissionObjectiveId,Is.Not.Null.And.Not.Empty,photo.Result);
            Assert.That(Photos.SubmitPhoto(photo),Is.True,Photos.LastError);return photo;
        }
        private void CaptureAlgae()
        {
            Place("zone01-left");
            Assert.That(Catcher.TryCapture(),Is.EqualTo(CreatureCatcher.Result.Started));
            audio.Tick(.01f);
            Assert.That(effects.IsPlaying("z1.audio.sfx_capture_mechanism"),Is.True);
            CaptureMinigamePlayModeTests.Win(Catcher.minigame);
            Assert.That(Catcher.LastResult,Is.EqualTo(CreatureCatcher.Result.Caught));
        }
        private void Sail(Vector2 point,float depth=230,float distance=600)
        {
            cabin.OpenNavigation();Nav.Ship.Refill();Nav.RestoreVoyage(point,90,depth,distance);
            Nav.Navigate(1,0,0,.02f);Assert.That(Nav.IsMoving,Is.True);
        }
        private bool Playing(string name)=>effects.IsPlaying("z1.audio."+name);

        private void PrepareHelm(float depth = 300)
        {
            cabin.OpenNavigation();
            cabin.SendMessage("OnApplicationFocus", true);
            Nav.Ship.Refill();
            Nav.RestoreVoyage(new Vector2(745, 295), 90, depth, 600);
        }
        private AudioSource[] Voices(string sound)
        {
            var clip = effects.Definition("z1.audio." + sound).clip;
            return cabin.GetComponentsInChildren<AudioSource>().Where(s => s.clip == clip && s.isPlaying).ToArray();
        }

        [UnityTest] public IEnumerator HelmPointerControlsDoNotPlayLegacyUiClicks()
        {
            PrepareHelm();
            var manager = G10.Prototype.Audio.AudioManager.Instance;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var source = (AudioSource)typeof(G10.Prototype.Audio.AudioManager).GetField("sfxSource", flags).GetValue(manager);
            var clickField = typeof(G10.Prototype.Audio.AudioManager).GetField("buttonClickClip", flags);
            var original = (AudioClip)clickField.GetValue(manager);
            var sentinel = AudioClip.Create("UI click regression sentinel", 22050, 1, 44100, false);
            var menu = new GameObject("Ordinary menu button", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            clickField.SetValue(manager, sentinel);
            try
            {
                manager.AutoBindAllButtonsInActiveScene(); manager.AutoBindAllButtonsInActiveScene();
                foreach (var name in new[] { "ForwardHotspot", "ReverseHotspot", "TurnLeftHotspot", "TurnRightHotspot", "AscendHotspot", "DiveHotspot" })
                {
                    Nav.Brake(); source.Stop();
                    var node = cabin.NavigationPanel.transform.Find(name).gameObject;
                    var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                        { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
                    UnityEngine.EventSystems.ExecuteEvents.Execute(node, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
                    yield return null; yield return null;
                    // Reverse accelerates slowly; very short Editor frames may not yet cross the movement epsilon.
                    float movementDeadline = Time.realtimeSinceStartup + .5f;
                    while (!Nav.IsMoving && Time.realtimeSinceStartup < movementDeadline) yield return null;
                    Assert.That(Nav.IsMoving, Is.True, name + " must still apply its held control.");
                    Assert.That(source.isPlaying, Is.False, name + " must not play a legacy click on press.");
                    UnityEngine.EventSystems.ExecuteEvents.Execute(node, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
                    UnityEngine.EventSystems.ExecuteEvents.Execute(node, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                    yield return null;
                    Assert.That(source.isPlaying, Is.False, name + " must not play a legacy click on release.");
                }
                menu.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.That(source.isPlaying, Is.True, "The ordinary menu click is still bound; this probe must detect real playback.");
            }
            finally
            {
                source.Stop(); clickField.SetValue(manager, original);
                Object.Destroy(sentinel); Object.Destroy(menu);
            }
        }

        [UnityTest] public IEnumerator HeldSteeringDiveAndAscentLoopPastClipEndAndStopOnRelease()
        {
            PrepareHelm();
            foreach (int command in new[] { 4, 6, 5 })
            {
                string sound = command == 4 ? "sfx_steering_servo" : "sfx_depth_pressure_adjustment";
                cabin.Hold(command);
                yield return null; yield return null;
                var voices = Voices(sound);
                Assert.That(voices, Has.Length.EqualTo(1));
                var source = voices[0];
                Assert.That(source.loop, Is.True);
                int previousSample = source.timeSamples;
                bool wrapped = false;
                float until = Time.realtimeSinceStartup + source.clip.length * 2 + .25f;
                while (Time.realtimeSinceStartup < until)
                {
                    yield return null;
                    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    Assert.That(Voices(sound), Has.Length.EqualTo(1), $"Held command {command}: held={typeof(CabinStationView).GetField("heldControl", flags).GetValue(cabin)}, focus={typeof(CabinStationView).GetField("hasFocus", flags).GetValue(cabin)}, dt={Time.deltaTime}, depth={Nav.Depth}, max={Nav.Ship.MaximumDepth}, energy={Nav.Ship.Energy}, hull={Nav.Ship.Hull}, turning={Nav.IsTurning}, depthMoving={Nav.IsChangingDepth}, blocked={Nav.ExpeditionBlocked}, panel={cabin.Panels.CurrentPanel?.name}, modal={cabin.Panels.IsModalOpen}, direct={cabin.IsDirectInteractionActive}.");
                    Assert.That(Voices(sound)[0], Is.SameAs(source));
                    wrapped |= source.timeSamples < previousSample;
                    previousSample = source.timeSamples;
                }
                Assert.That(wrapped, Is.True, "Playback must cross the clip boundary while the control stays held.");
                cabin.Release(command);
                yield return new WaitForSeconds(.18f);
                Assert.That(Playing(sound), Is.False);
                Assert.That(Voices(sound), Is.Empty, "Release stops the source, not just its director handle.");
                // Re-press before the old three-second cooldown would have allowed another sound.
                cabin.Hold(command);
                yield return null; yield return null;
                Assert.That(Voices(sound), Has.Length.EqualTo(1));
                cabin.Release(command);
                yield return new WaitForSeconds(.18f);
                Assert.That(Voices(sound), Is.Empty);
            }
        }

        [UnityTest] public IEnumerator ControlAudioStopsAtLimitsPanelPauseFocusAndBlockedMovement()
        {
            const string depthSound = "sfx_depth_pressure_adjustment", turnSound = "sfx_steering_servo";
            PrepareHelm(Nav.Ship.MaximumDepth - .1f);
            cabin.Hold(6);
            yield return new WaitForSeconds(.25f);
            Assert.That(Nav.Depth, Is.EqualTo(Nav.Ship.MaximumDepth));
            Assert.That(Voices(depthSound), Is.Empty, "Holding DIVE at the ceiling is not actual depth movement.");
            cabin.Release(6);
            Nav.RestoreVoyage(Nav.Position, Nav.Heading, Nav.MinimumDepth, Nav.DistanceTravelled);
            cabin.Hold(5);
            yield return new WaitForSeconds(.18f);
            Assert.That(Voices(depthSound), Is.Empty);
            cabin.Release(5);

            cabin.Hold(4);
            yield return null; yield return null;
            Assert.That(Voices(turnSound), Has.Length.EqualTo(1));
            cabin.OpenComputer();
            yield return new WaitForSeconds(.18f);
            Assert.That(Voices(turnSound), Is.Empty);
            cabin.OpenNavigation();
            yield return null; yield return null;
            Assert.That(Voices(turnSound), Is.Empty, "Opening the helm must not resume old controls.");

            cabin.Hold(3);
            yield return null; yield return null;
            Assert.That(Voices(turnSound), Has.Length.EqualTo(1));
            cabin.OpenPause();
            yield return null; yield return null;
            Assert.That(Voices(turnSound), Is.Empty);
            PauseMenuController.Instance.Resume();
            yield return null; yield return null;
            Assert.That(Voices(turnSound), Is.Empty);

            cabin.Hold(4);
            yield return null; yield return null;
            Assert.That(Voices(turnSound), Has.Length.EqualTo(1));
            cabin.SendMessage("OnApplicationFocus", false);
            yield return new WaitForSeconds(.18f);
            Assert.That(Voices(turnSound), Is.Empty);
            cabin.SendMessage("OnApplicationFocus", true);

            Nav.TransitionBlocked = true;
            cabin.Hold(4);
            yield return new WaitForSeconds(.18f);
            Assert.That(Voices(turnSound), Is.Empty);
            Nav.TransitionBlocked = false;
            Nav.Ship.ConsumeMovement(10000);
            yield return new WaitForSeconds(.18f);
            Assert.That(Nav.Ship.CanMove, Is.False);
            Assert.That(Voices(turnSound), Is.Empty);
            cabin.Release(4);
        }

        [UnityTest] public IEnumerator TwoSentPhotosAndActualCaptureInstallTheLampOnce()
        {
            Assert.That(story.config.locations,Has.Length.EqualTo(2));
            Assert.That(story.survey.locations,Has.Length.EqualTo(2));
            Place("zone01-left");
            Assert.That(Catcher.TryCapture(),Is.EqualTo(CreatureCatcher.Result.PhotoRequired));
            var first=Photos.Capture();Assert.That(first.MissionObjectiveId,Is.EqualTo(ZoneOneStory.PhotoOneObjective));
            Assert.That(story.HasObjective(ZoneOneStory.PhotoOneObjective),Is.False,"Taking a photo is not SEND.");
            Assert.That(Catcher.TryCapture(),Is.EqualTo(CreatureCatcher.Result.PhotoRequired));
            Assert.That(Photos.SubmitPhoto(first),Is.True);Assert.That(Playing("sfx_data_transmit_success"),Is.True);
            CaptureAlgae();
            Assert.That(Playing("sfx_capture_mechanism"),Is.False);
            Assert.That(Playing("sfx_algae_capture_resonance"),Is.True);
            Assert.That(story.inventory.GetCount(ZoneOneStory.LuminescentMaterial),Is.EqualTo(1));
            audio.Tick(.01f);Assert.That(Playing("ambient_singing_algae"),Is.True,"The forest survives collection.");
            Assert.That(story.CanInstallBioLamp,Is.False);
            yield return new WaitForSeconds(.7f);Send("zone01-north");
            Assert.That(story.CanInstallBioLamp,Is.True);
            float hull=Nav.Ship.Hull,energy=Nav.Ship.Energy,maximumDepth=Nav.Ship.MaximumDepth;
            Assert.That(story.ApplyProgressionAction(),Is.True);audio.Tick(0);
            Assert.That(story.BioLampInstalled,Is.True);Assert.That(Playing("sfx_biolamp_startup"),Is.True);
            Assert.That(story.inventory.GetCount(ZoneOneStory.LuminescentMaterial),Is.Zero);
            Assert.That(story.MainObjectivesComplete,Is.True);Assert.That(story.HasZone("Zone02"),Is.True);
            Assert.That(story.ApplyProgressionAction(),Is.False,"One module and one material payment.");
            Assert.That(Nav.Ship.Hull,Is.EqualTo(hull));Assert.That(Nav.Ship.Energy,Is.EqualTo(energy));
            Assert.That(Nav.Ship.MaximumDepth,Is.EqualTo(maximumDepth),"An external lamp does not upgrade the pressure hull.");
            Assert.That(ExpeditionSaveStore.TryRead(out var saved,out _),Is.True);
            Assert.That(saved.current.zones.Find(z=>z.zone=="Zone01").missionProgress.worldFlags,Does.Contain(ZoneOneStory.BioLampOnline));
        }
        [UnityTest] public IEnumerator FailedInstallSaveRollsBackMaterialAndNeverStartsAudio()
        {
            Send("zone01-left");CaptureAlgae();yield return new WaitForSeconds(.7f);Send("zone01-north");
            string valid=ExpeditionSaveStore.PathOverride;
            string blocked=Path.Combine(folder,"blocked");Directory.CreateDirectory(blocked);
            ExpeditionSaveStore.PathOverride=blocked;
            try
            {
                Assert.That(story.InstallBioLamp(),Is.False);
                Assert.That(story.BioLampInstalled,Is.False);Assert.That(story.inventory.GetCount(ZoneOneStory.LuminescentMaterial),Is.EqualTo(1));
                Assert.That(Playing("sfx_biolamp_startup"),Is.False);Assert.That(loop.ZoneOneAudio.Has("lamp"),Is.False);
            }
            finally {ExpeditionSaveStore.PathOverride=valid;}
            Assert.That(story.InstallBioLamp(),Is.True);
        }
        [UnityTest] public IEnumerator KnocksWaitForFieldworkDistanceHelmAndModalThenPersistAcrossReload()
        {
            Sail(new(745,295));audio.Tick(12);Assert.That(audio.KnockCount,Is.Zero);
            Send("zone01-left");CaptureAlgae();Sail(new(745,295));
            cabin.OpenComputer();audio.Tick(12);Assert.That(audio.KnockCount,Is.Zero);
            cabin.ClosePanel();cabin.OpenNavigation();
            var modal=new GameObject("Audio gate test modal",typeof(RectTransform));
            Assert.That(cabin.Panels.TryOpenModal(modal),Is.True);audio.Tick(12);Assert.That(audio.KnockCount,Is.Zero);
            cabin.Panels.EndModal(modal);Object.Destroy(modal);
            Sail(new(745,295));audio.Tick(8.1f);Assert.That(audio.KnockCount,Is.EqualTo(1));
            Assert.That(Playing("sfx_hull_knocks_three"),Is.True);
            var clip=effects.Definition("z1.audio.sfx_hull_knocks_three").clip;
            Assert.That(cabin.GetComponentsInChildren<AudioSource>().Any(s=>s.clip==clip&&s.isPlaying),Is.True);
            audio.Tick(15);Assert.That(audio.KnockCount,Is.EqualTo(1));
            Assert.That(loop.SaveCurrent(),Is.True);yield return Load();
            Sail(new(745,295));audio.Tick(20);Assert.That(audio.KnockCount,Is.Zero,"Saved cue is not replayed by a new component.");
            Assert.That(loop.ZoneOneAudio.Has("knocks"),Is.True);
        }
        private static string HullQaFolder()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/Zone01HullFeedback"));
            Directory.CreateDirectory(path); return path;
        }
        private static IEnumerator HullScreenshot(string name)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(Path.Combine(HullQaFolder(), name + ".png"), frame.EncodeToPNG()); }
            finally { Object.Destroy(frame); }
        }
        private static void RestoreShake(int old)
        {
            if (old < 0) PlayerPrefs.DeleteKey(ImpactShake.PreferenceKey); else PlayerPrefs.SetInt(ImpactShake.PreferenceKey, old);
            PlayerPrefs.Save();
        }
        [UnityTest] public IEnumerator ThreeKnockScreenImpulsesFollowAudioAttacksAndSettle()
        {
            int old = PlayerPrefs.GetInt(ImpactShake.PreferenceKey, -1); ImpactShake.SetScreenShakeEnabled(true);
            try
            {
                Send("zone01-left"); CaptureAlgae(); Sail(new(745, 295));
                var surface = effects.Surface(EffectTarget.Global); var shake = surface.shake.shake;
                yield return HullScreenshot("01-before-knocks");
                audio.Tick(8.1f);
                Assert.That(audio.hullKnocks, Is.Not.Null); Assert.That(effects.ActiveSequenceCount, Is.EqualTo(1));
                var voice = Voices("sfx_hull_knocks_three").Single();
                var times = new System.Collections.Generic.List<float>();
                long winner = 0; float peakOffset = 0, peakTint = 0;
                float timeout = Time.realtimeSinceStartup + 4;
                while (effects.ActiveSequenceCount > 0 && Time.realtimeSinceStartup < timeout)
                {
                    if (effects.IsPlaying("z1.audio.knock_pulse"))
                    {
                        peakOffset = Mathf.Max(peakOffset, shake.Offset.magnitude);
                        peakTint = Mathf.Max(peakTint, surface.overlay.tint.color.a);
                        if (surface.shake.Winner != 0 && surface.shake.Winner != winner)
                        {
                            winner = surface.shake.Winner; times.Add(voice.time);
                            if (times.Count == 2) yield return HullScreenshot("02-second-knock");
                        }
                    }
                    yield return null;
                }
                Assert.That(times, Has.Count.EqualTo(3));
                for (int i = 0; i < 3; i++) Assert.That(times[i], Is.EqualTo(.12f + i * .6f).Within(.12f));
                Assert.That(peakOffset, Is.GreaterThan(.4f), "The visible cabin frame must actually move.");
                Assert.That(peakTint, Is.GreaterThan(.025f));
                Assert.That(shake.IsShaking, Is.False); Assert.That(shake.Offset, Is.EqualTo(Vector3.zero));
                Assert.That(surface.overlay.tint.color.a, Is.LessThan(.01f));
                Assert.That(voice.isPlaying, Is.False); Assert.That(audio.KnockCount, Is.EqualTo(1));
                Assert.That(loop.ZoneOneAudio.Has("knocks"), Is.True);
                yield return HullScreenshot("03-after-knocks");
                File.WriteAllText(Path.Combine(HullQaFolder(), "knock-timing.json"),
                    "{\"audio_attack_seconds\":[" + string.Join(",", times.Select(t => t.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))) +
                    "],\"peak_offset_pixels\":" + peakOffset.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"peak_tint_alpha\":" + peakTint.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "}");
            }
            finally { RestoreShake(old); }
        }
        [UnityTest] public IEnumerator KnockAccessibilityPauseAndTimelineRestoreCancelAllChildrenWithoutReplay()
        {
            int old = PlayerPrefs.GetInt(ImpactShake.PreferenceKey, -1); ImpactShake.SetScreenShakeEnabled(false);
            try
            {
                Send("zone01-left"); CaptureAlgae(); Assert.That(loop.Rest(), Is.True);
                Sail(new(745, 295)); audio.Tick(8.1f);
                float until = Time.realtimeSinceStartup + 2;
                while (!effects.IsPlaying("z1.audio.knock_pulse") && Time.realtimeSinceStartup < until) yield return null;
                Assert.That(effects.IsPlaying("z1.audio.knock_pulse"), Is.True);
                var surface = effects.Surface(EffectTarget.Global); var shake = surface.shake.shake;
                Assert.That(Voices("sfx_hull_knocks_three"), Has.Length.EqualTo(1));
                Assert.That(shake.IsShaking, Is.False); Assert.That(shake.Offset, Is.EqualTo(Vector3.zero));
                Assert.That(surface.overlay.tint.color.a, Is.GreaterThan(0), "The restrained tint remains available without motion.");
                cabin.OpenPause(); yield return null; yield return null;
                Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(Voices("sfx_hull_knocks_three"), Is.Empty);
                Assert.That(surface.overlay.tint.color.a, Is.Zero);
                PauseMenuController.Instance.Resume(); Sail(new(745, 295)); audio.Tick(12);
                yield return new WaitForSeconds(.8f);
                Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(audio.KnockCount, Is.EqualTo(1));

                // The checkpoint predates the cue, so an explicit journal rollback permits a new occurrence.
                Assert.That(loop.RestoreDay(1), Is.True); Sail(new(745, 295)); audio.Tick(8.1f);
                Assert.That(audio.KnockCount, Is.EqualTo(2)); Assert.That(effects.ActiveSequenceCount, Is.EqualTo(1));
                Assert.That(loop.RestoreDay(1), Is.True); yield return null;
                Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(Voices("sfx_hull_knocks_three"), Is.Empty);
                Assert.That(shake.Offset, Is.EqualTo(Vector3.zero)); Assert.That(surface.overlay.tint.color.a, Is.Zero);
                // Starting another timeline presentation also owns and cancels every child.
                Sail(new(745, 295)); audio.Tick(8.1f); Assert.That(effects.ActiveSequenceCount, Is.EqualTo(1));
                audio.enabled = false; yield return new WaitForSeconds(.8f);
                Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(Voices("sfx_hull_knocks_three"), Is.Empty);
                Assert.That(surface.overlay.tint.color.a, Is.Zero);
            }
            finally { RestoreShake(old); }
        }
        [UnityTest] public IEnumerator JournalRollbackRestoresCueEligibilityWithoutRepeatingLaterCheckpoint()
        {
            Send("zone01-left");CaptureAlgae();Assert.That(loop.Rest(),Is.True);
            Sail(new(745,295));audio.Tick(9);Assert.That(audio.KnockCount,Is.EqualTo(1));
            Nav.Brake();Assert.That(loop.Rest(),Is.True);Assert.That(loop.RestoreDay(2),Is.True);
            Sail(new(745,295));audio.Tick(9);Assert.That(audio.KnockCount,Is.EqualTo(1));
            Assert.That(loop.RestoreDay(1),Is.True);Assert.That(loop.ZoneOneAudio.Has("knocks"),Is.False);
            Sail(new(745,295));audio.Tick(9);Assert.That(audio.KnockCount,Is.EqualTo(2));
            yield return null;
        }
        [UnityTest] public IEnumerator TutorialIsQuietAndMotionLayersAndDeepToneCancelCorrectly()
        {
            Assert.That(loop.ResetTutorialProgress(),Is.True);Place("zone01-left");audio.Tick(1);
            Assert.That(audio.AlgaeStrength,Is.Zero);Assert.That(Playing("ambient_singing_algae"),Is.False);
            Assert.That(Playing("ambient_submarine_engine"),Is.True);
            var complete=new TutorialProgressState{completed=true};Assert.That(loop.SaveTutorialProgress(complete),Is.True);
            Place("zone01-left");audio.Tick(1);Assert.That(audio.AlgaeStrength,Is.GreaterThan(.5f));
            Assert.That(Playing("ambient_singing_algae"),Is.True);
            Sail(new(1000,700),460);audio.Tick(.1f);Assert.That(Playing("sfx_deep_specimen_low_tone"),Is.True);
            Assert.That(Playing("ambient_propulsion_thrust"),Is.True);
            Nav.Brake();audio.Tick(.1f);Assert.That(Playing("sfx_deep_specimen_low_tone"),Is.False);
            Sail(new(1000,700),460);audio.Tick(1);Assert.That(Playing("sfx_deep_specimen_low_tone"),Is.False,"The interrupted cue is still once per timeline.");
            yield return null;
        }
        [UnityTest] public IEnumerator LampInterruptsOnlyNearbyBiologyAndFinalEchoIsOnce()
        {
            Send("zone01-left");CaptureAlgae();yield return new WaitForSeconds(.7f);Send("zone01-north");
            Place("zone01-left");audio.Tick(.1f);Assert.That(story.InstallBioLamp(),Is.True);
            audio.Tick(0);Assert.That(audio.ExternalLightLevel,Is.Zero);
            audio.Tick(2.2f);Assert.That(audio.ExternalLightLevel,Is.EqualTo(1));
            audio.Tick(2.05f);Assert.That(Playing("ambient_singing_algae"),Is.False);
            Assert.That(Playing("ambient_submarine_engine"),Is.True);Assert.That(Playing("ambient_water_current"),Is.True);
            audio.Tick(.5f);Assert.That(Playing("ambient_singing_algae"),Is.True);
            Sail(new(1650,680),460,1800);audio.Tick(9);Assert.That(audio.FinalSongCount,Is.EqualTo(1));
            Assert.That(Playing("sfx_hull_knocks_three"),Is.False,"Departure must not backfill a missed first-trip knock.");
            Assert.That(Playing("sfx_algae_final_echo"),Is.True);audio.Tick(15);Assert.That(audio.FinalSongCount,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator CancelledCaptureNeverAwardsMaterialOrResonanceAndDoesNotScrape()
        {
            Send("zone01-left");Place("zone01-left");Assert.That(Catcher.TryCapture(),Is.EqualTo(CreatureCatcher.Result.Started));
            audio.Tick(.1f);Catcher.minigame.Cancel();audio.Tick(.1f);
            Assert.That(Playing("sfx_capture_mechanism"),Is.False);Assert.That(Playing("sfx_algae_capture_resonance"),Is.False);
            Assert.That(story.HasObjective(ZoneOneStory.AlgaeCaptureObjective),Is.False);
            Assert.That(story.inventory.GetCount(ZoneOneStory.LuminescentMaterial),Is.Zero);
            Assert.That(cabin.GetComponent<CabinFeedbackController>().HasPendingScrape,Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator NewGameOpeningRunsBeforeTutorialAndContinueDoesNotReplay()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap",LoadSceneMode.Single);
            float until=Time.realtimeSinceStartup+10;
            while((SceneFlowController.Instance==null||SceneManager.GetActiveScene().name!="MainMenu"||SceneFlowController.Instance.IsTransitioning)&&Time.realtimeSinceStartup<until)yield return null;
            var flow=SceneFlowController.Instance;Assert.That(flow,Is.Not.Null);flow.StartNewGame();
            until=Time.realtimeSinceStartup+45;bool intro=false,silence=false,caption=false;
            float started=0;
            while(Time.realtimeSinceStartup<until)
            {
                var live=Object.FindAnyObjectByType<ZoneOneAudioController>();
                if(live!=null&&live.IsIntroPlaying)
                {
                    if(!intro){started=Time.realtimeSinceStartup;intro=true;}
                    float elapsed=Time.realtimeSinceStartup-started;
                    if(elapsed>20.15f&&elapsed<20.7f)
                    {
                        var sources=live.GetComponentsInChildren<AudioSource>().Where(s=>s.isPlaying&&s.clip!=null).ToArray();
                        Assert.That(sources.All(s=>s.clip.name=="intro_system_reboot"),Is.True,"Crash and machinery must stop for the one-second silence.");
                        silence=true;
                    }
                    if(elapsed>23.4f&&elapsed<25)caption=Object.FindObjectsByType<TMPro.TMP_Text>().Any(t=>t.text.Contains("EMERGENCY RESTART"));
                }
                if(intro&&!flow.IsTransitioning)break;
                yield return null;
            }
            Assert.That(intro&&silence&&caption,Is.True);Assert.That(flow.IsTransitioning,Is.False);
            loop=Object.FindAnyObjectByType<ExpeditionLoop>();Assert.That(loop.ZoneOneIntroPending,Is.False);Assert.That(loop.ZoneOneAudio.Has("intro"),Is.True);
            Assert.That(ExpeditionSaveStore.TryRead(out var completed,out _),Is.True);
            Assert.That(completed.dayStart.zoneOneIntroPending,Is.False,"Death on day one must not restart the descent.");
            yield return Load();Assert.That(audio.IsIntroPlaying,Is.False);Assert.That(loop.ZoneOneIntroPending,Is.False);
        }
    }
}
