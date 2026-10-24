#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using G10.Prototype.Atmosphere;
using G10.Prototype.Audio;
using G10.Prototype.Feedback;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests
{
    public sealed class HorrorContentAcceptanceTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Testing/HorrorPresentationPreview.unity";
        private const string Output = "Docs/QA/HorrorPolish";
        [Serializable] private class Row
        {
            public int preset;
            public string name;
            public float listenerPeakRms, listenerMaxSample, clipPeakRms, minLight = 1;
            public bool signal, silhouette, shaking, instrumentJitter, recovered, pauseCancelled;
            public string[] loadedClips;
        }
        [Serializable] private class Report { public Row[] presets; }
        [Serializable] private class AmbientRow { public string clip; public float sourcePeakRms; public bool loops; }
        [Serializable] private class AmbientReport { public AmbientRow[] clips; }
        private static float Rms(float[] values)
        { double sum=0; foreach(float v in values) sum+=v*v; return (float)Math.Sqrt(sum/values.Length); }
        [UnityTest] public IEnumerator NineRealPresetsProduceAudioVisualsAndRestore()
        {
            if (Application.isBatchMode) Assert.Ignore("Rendered audiovisual acceptance requires a visible Unity Game view.");
            Directory.CreateDirectory(Output);
            var gameView = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"); EditorWindow.GetWindow(gameView).Show();
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return new WaitForEndOfFrame();
            var preview = UnityEngine.Object.FindAnyObjectByType<HorrorPreviewAudioBinding>();
            var director = preview.director;
            var atmosphere = UnityEngine.Object.FindAnyObjectByType<CabinAtmosphere>();
            Assert.That(preview, Is.Not.Null);
            Assert.That(director.Definition("horror.knock").clip.name, Is.EqualTo("horror_knock_three"));
            var result = new List<Row>();
            var listener = new float[2048]; var voice = new float[2048];
            var shots = new[]{.7f,1f,.8f,.7f,.7f,3f,.3f,4.7f,.8f};
            var errors = new List<string>();
            for(int preset=1;preset<=9;preset++)
            {
                director.ClearTransients(); yield return null;
                ScreenCapture.CaptureScreenshot($"{Output}/preset-{preset}-before.png"); yield return new WaitForEndOfFrame();
                var row=new Row{preset=preset,name=HorrorPreviewAudioBinding.PresetNames[preset-1]};
                var names=new HashSet<string>(); bool shot=false; float started=Time.time;
                preview.PlayPreset(preset,1,1);
                do
                {
                    float elapsed=Time.time-started;
                    foreach(var source in preview.GetComponentsInChildren<AudioSource>())
                    {
                        if(!source.isPlaying || source.clip==null) continue;
                        names.Add(source.clip.name);
                        if(source.loop) continue;
                        source.GetOutputData(voice,0); row.clipPeakRms=Mathf.Max(row.clipPeakRms,Rms(voice));
                    }
                    AudioListener.GetOutputData(listener,0); row.listenerPeakRms=Mathf.Max(row.listenerPeakRms,Rms(listener));
                    foreach (float value in listener) row.listenerMaxSample=Mathf.Max(row.listenerMaxSample,Mathf.Abs(value));
                    row.minLight=Mathf.Min(row.minLight,atmosphere.Light);
                    row.signal|=director.Surface(EffectTarget.Radar).visual.SignalVisible || director.Surface(EffectTarget.Computer).visual.SignalVisible;
                    row.silhouette|=director.Surface(EffectTarget.Cabin).visual.AnomalyVisible;
                    row.shaking|=director.Surface(EffectTarget.Global).shake.shake.IsShaking;
                    row.instrumentJitter|=UnityEngine.Object.FindObjectsByType<GaugeJitter>(FindObjectsSortMode.None).Any(g=>Quaternion.Angle(g.transform.localRotation,Quaternion.identity)>.05f);
                    if(!shot && elapsed>=shots[preset-1]) { ScreenCapture.CaptureScreenshot($"{Output}/preset-{preset}-peak.png"); shot=true; }
                    yield return null;
                } while(Time.time-started<11 && (director.ActiveTransientCount>0 || director.ActiveSequenceCount>0 || Time.time-started<1));
                yield return new WaitForSeconds(.2f);
                row.loadedClips=names.ToArray();
                row.recovered=director.ActiveTransientCount==0 && director.ActiveSequenceCount==0 &&
                    !director.Surface(EffectTarget.Radar).visual.SignalVisible && !director.Surface(EffectTarget.Computer).visual.SignalVisible &&
                    !director.Surface(EffectTarget.Cabin).visual.AnomalyVisible && Mathf.Abs(atmosphere.Light-1)<.002f;
                ScreenCapture.CaptureScreenshot($"{Output}/preset-{preset}-after.png"); yield return new WaitForEndOfFrame();
                preview.PlayPreset(preset,1,1); yield return new WaitForSeconds(.12f);
                director.SetSuspended(true); yield return null;
                row.pauseCancelled=director.ActiveTransientCount==0 && director.ActiveSequenceCount==0;
                director.SetSuspended(false); yield return new WaitForSeconds(.1f);
                director.ClearTransients(); result.Add(row);
                if(preset<9 && row.clipPeakRms<.00001f) errors.Add("No source output: "+preset);
                if(!row.recovered || !row.pauseCancelled) errors.Add("Cleanup/pause: "+preset);
                if(row.listenerMaxSample>=.9999f) errors.Add("Output clipping: "+preset);
            }
            File.WriteAllText($"{Output}/preset-runtime.json",JsonUtility.ToJson(new Report{presets=result.ToArray()},true));
            Assert.That(result[2].minLight,Is.LessThan(.88f),"Cabin illustration must visibly dim");
            Assert.That(result[3].signal && result[4].signal,Is.True,"Both actual displays must render interference");
            Assert.That(result[5].shaking,Is.True,"Heavy pressure must vibrate");
            Assert.That(result[5].instrumentJitter,Is.True,"Heavy pressure must move the decorative instrument");
            Assert.That(result[6].silhouette && result[7].silhouette,Is.True,"Default sprite must render");
            Assert.That(result[7].loadedClips,Does.Contain("horror_distant_impact"));
            Assert.That(errors,Is.Empty,string.Join("; ",errors));
            // Independent controls: zero visual strength still plays audio, zero volume still renders.
            preview.PlayPreset(5,0,1); yield return new WaitForSeconds(.3f);
            Assert.That(director.Surface(EffectTarget.Computer).visual.SignalVisible,Is.False);
            Assert.That(preview.GetComponentsInChildren<AudioSource>().Any(s=>s.isPlaying && !s.loop && s.volume>0),Is.True);
            preview.PlayPreset(5,1,0); yield return new WaitForSeconds(.3f);
            Assert.That(director.Surface(EffectTarget.Computer).visual.SignalVisible,Is.True);
            Assert.That(preview.GetComponentsInChildren<AudioSource>().Where(s=>!s.loop).All(s=>s.volume==0),Is.True);
            preview.PlayPreset(8,1,1,director.Definition("horror.knock").clip,director.Definition("horror.silhouette").silhouette);
            yield return new WaitForSeconds(.3f);
            Assert.That(preview.GetComponentsInChildren<AudioSource>().Any(s=>s.isPlaying && !s.loop),Is.False,"Combined must keep authored cue timing even when optional overrides are assigned");
            Assert.That(director.Surface(EffectTarget.Global).visual.AnomalyVisible,Is.False);
            var loops=new List<AmbientRow>();
            for(int i=0;i<preview.ambience.Length;i++)
            {
                preview.director.ClearTransients(); preview.SetAmbience(i,1);
                float until=Time.time+2; var loop=new AmbientRow{clip=preview.ambience[i].id};
                while(Time.time<until)
                {
                    var source=preview.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>s.clip==preview.ambience[i].clip && s.isPlaying);
                    if(source!=null) { source.GetOutputData(voice,0); loop.sourcePeakRms=Mathf.Max(loop.sourcePeakRms,Rms(voice)); loop.loops=source.loop; }
                    yield return null;
                }
                Assert.That(loop.sourcePeakRms,Is.GreaterThan(.00001f),loop.clip); Assert.That(loop.loops,Is.True); loops.Add(loop);
            }
            File.WriteAllText($"{Output}/ambient-runtime.json",JsonUtility.ToJson(new AmbientReport{clips=loops.ToArray()},true));
            preview.StopPreview(); yield return null;
            Assert.That(preview.GetComponentsInChildren<AudioSource>().Any(s=>s.isPlaying),Is.False);
        }
    }
}
#endif
