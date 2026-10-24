using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace G10.Prototype.Tests
{
    /// <summary>
    /// Test-only tap on the real AudioListener mix. The audio callback copies into
    /// a bounded preallocated buffer and never modifies Unity's supplied samples.
    /// No synthesis, replacement audio, source playback, or preference changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneThreeAudioEvidence : MonoBehaviour
    {
        [Serializable]
        public sealed class Metrics
        {
            public string path;
            public int sampleRate, observedChannels, channelChanges;
            public int capturedFrames, capturedSamples, clippedSamples;
            public float durationSeconds, rms, peak, rmsDbfs, peakDbfs;
            public bool nonzero, reachedCapacity;
            public string channelMapping;
        }

        private const float MaximumSeconds = 20;
        private float[] samples;
        private int sampleRate, frameCapacity, writtenFrames;
        private int recording, writerActive, observedChannels, channelChanges;
        private bool finished;

        /// <summary>Call on the main thread with the actual enabled scene listener.</summary>
        public static ZoneThreeAudioEvidence Begin(AudioListener listener, float seconds)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (!listener.isActiveAndEnabled)
                throw new InvalidOperationException("Audio evidence requires the enabled scene AudioListener.");
            if (!float.IsFinite(seconds) || seconds <= 0 || seconds > MaximumSeconds)
                throw new ArgumentOutOfRangeException(nameof(seconds), "Audio evidence duration must be within (0, 20] seconds.");
            if (listener.GetComponent<ZoneThreeAudioEvidence>() != null)
                throw new InvalidOperationException("Finish and destroy the previous listener recorder before starting another.");

            int rate = AudioSettings.outputSampleRate;
            if (rate < 8000 || rate > 192000)
                throw new InvalidOperationException("Unexpected AudioSettings output sample rate.");
            var recorder = listener.gameObject.AddComponent<ZoneThreeAudioEvidence>();
            recorder.sampleRate = rate;
            recorder.frameCapacity = checked((int)Math.Ceiling(seconds * rate));
            recorder.samples = new float[checked(recorder.frameCapacity * 2)];
            // Publishing recording last ensures the audio thread sees the buffer and limits.
            Volatile.Write(ref recorder.recording, 1);
            return recorder;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (Volatile.Read(ref recording) == 0 || channels < 1 || data == null) return;
            if (Interlocked.CompareExchange(ref writerActive, 1, 0) != 0) return;
            try
            {
                if (Volatile.Read(ref recording) == 0) return;
                int previousChannels = Volatile.Read(ref observedChannels);
                if (previousChannels == 0) Volatile.Write(ref observedChannels, channels);
                else if (previousChannels != channels)
                {
                    Interlocked.Increment(ref channelChanges);
                    Volatile.Write(ref observedChannels, channels);
                }
                int start = Volatile.Read(ref writtenFrames);
                int count = Math.Min(data.Length / channels, frameCapacity - start);
                for (int frame = 0; frame < count; frame++)
                {
                    int input = frame * channels;
                    int output = (start + frame) * 2;
                    // Stereo output is the usual game configuration. Mono is duplicated;
                    // for a surround listener, retain the actual FL/FR channels and report
                    // the observed input count explicitly rather than inventing a downmix.
                    samples[output] = data[input];
                    samples[output + 1] = data[input + (channels > 1 ? 1 : 0)];
                }
                // Publish only after the entire appended chunk is copied. Finish can safely
                // read this immutable prefix even if another callback is already in flight.
                Volatile.Write(ref writtenFrames, start + count);
                if (start + count >= frameCapacity) Volatile.Write(ref recording, 0);
            }
            finally { Volatile.Write(ref writerActive, 0); }
        }

        /// <summary>
        /// Stop and save the captured actual mix on the main thread. Silence is
        /// saved and reported as nonzero=false; it is never replaced with a source.
        /// The component is destroyed afterward; the AudioListener remains untouched.
        /// </summary>
        public Metrics Finish(string path)
        {
            if (finished) throw new InvalidOperationException("Audio evidence was already finished.");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("An output path is required.", nameof(path));
            finished = true;
            Interlocked.Exchange(ref recording, 0);
            int frames = Volatile.Read(ref writtenFrames);
            int count = frames * 2;
            int channels = Volatile.Read(ref observedChannels);
            double squareSum = 0;
            float peak = 0;
            int clipped = 0;
            for (int i = 0; i < count; i++)
            {
                float value = samples[i];
                if (!float.IsFinite(value)) value = 0;
                float absolute = Math.Abs(value);
                if (absolute > peak) peak = absolute;
                squareSum += (double)value * value;
                if (absolute >= 1) clipped++;
            }
            float rms = count > 0 ? (float)Math.Sqrt(squareSum / count) : 0;
            string absolutePath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            using (var stream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, false))
            {
                int dataBytes = checked(count * sizeof(short));
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + dataBytes);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)2); writer.Write(sampleRate);
                writer.Write(sampleRate * 4); writer.Write((short)4); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(dataBytes);
                for (int i = 0; i < count; i++)
                {
                    float value = samples[i];
                    if (!float.IsFinite(value)) value = 0;
                    value = Math.Max(-1, Math.Min(1, value));
                    writer.Write((short)Math.Round(value * 32767));
                }
            }
            var result = new Metrics
            {
                path = absolutePath, sampleRate = sampleRate, observedChannels = channels,
                channelChanges = Volatile.Read(ref channelChanges), capturedFrames = frames,
                capturedSamples = count, clippedSamples = clipped,
                durationSeconds = sampleRate > 0 ? (float)frames / sampleRate : 0,
                rms = rms, peak = peak,
                rmsDbfs = rms > 0 ? (float)(20 * Math.Log10(rms)) : -160,
                peakDbfs = peak > 0 ? (float)(20 * Math.Log10(peak)) : -160,
                nonzero = frames > 0 && peak > .000001f,
                reachedCapacity = frames >= frameCapacity,
                channelMapping = channels == 1 ? "Actual mono listener mix duplicated to stereo" :
                    channels == 2 ? "Actual stereo listener mix unchanged" :
                    channels > 2 ? "Actual listener front-left/front-right channels; other surround channels excluded" :
                    "No audio callback observed; empty capture"
            };
            Destroy(this);
            return result;
        }

        private void OnDisable() => Interlocked.Exchange(ref recording, 0);
        private void OnDestroy() => Interlocked.Exchange(ref recording, 0);
    }
}
