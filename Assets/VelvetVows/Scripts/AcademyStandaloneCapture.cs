#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VelvetVows
{
    public sealed class AcademyStandaloneCapture : MonoBehaviour
    {
        public bool Recording { get; private set; }
        private const int Fps = 15;
        private readonly ConcurrentQueue<byte[]> videoQueue = new ConcurrentQueue<byte[]>();
        private readonly object audioLock = new object();
        private readonly Stopwatch clock = new Stopwatch();
        private Process encoder;
        private Task videoWriter, errorWriter;
        private FileStream audioStream;
        private StreamWriter timeline;
        private string folder;
        private volatile bool draining;
        private int frames, audioChannels = 2, sampleRate;
        private long audioSamples;
        private long audioGapSamples;
        private double firstAudioDelay = -1;
        private float audioPeak;
        private string lastEvent = "menu";
        private bool keyframe;
        private int errors;

        public void BeginRecording()
        {
            string root = Environment.GetEnvironmentVariable("VELVET_RECORDING_ROOT") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../Playthrough_Recording"));
            string run = Environment.GetEnvironmentVariable("VELVET_RECORDING_RUN") ?? "attempt_01";
            folder = Path.Combine(root, run);
            Directory.CreateDirectory(folder);
            string ffmpeg = File.ReadAllText(Path.Combine(root, "ffmpeg_path.txt")).Trim();
            var start = new ProcessStartInfo(ffmpeg,
                "-hide_banner -loglevel warning -y -f image2pipe -framerate " + Fps +
                " -vcodec mjpeg -i pipe:0 -an -vf scale=1280:-2 -c:v libx264 -preset veryfast -crf 20 -pix_fmt yuv420p -movflags +faststart \"" +
                Path.Combine(folder, "video_silent.mp4") + "\"")
            {
                UseShellExecute = false, RedirectStandardInput = true,
                RedirectStandardError = true, CreateNoWindow = true
            };
            encoder = Process.Start(start);
            errorWriter = Task.Run(() => File.WriteAllText(Path.Combine(folder, "encoder.log"), encoder.StandardError.ReadToEnd()));
            draining = true;
            videoWriter = Task.Run(async () =>
            {
                while (draining || !videoQueue.IsEmpty)
                {
                    if (videoQueue.TryDequeue(out var frame))
                        await encoder.StandardInput.BaseStream.WriteAsync(frame, 0, frame.Length);
                    else await Task.Delay(5);
                }
                encoder.StandardInput.Close();
            });
            sampleRate = AudioSettings.outputSampleRate;
            audioStream = new FileStream(Path.Combine(folder, "audio.f32"), FileMode.Create, FileAccess.Write,
                FileShare.Read, 65536);
            timeline = new StreamWriter(Path.Combine(folder, "timeline.tsv"), false, Encoding.UTF8) { AutoFlush = true };
            timeline.WriteLine("seconds\ttype\tevent");
            Application.logMessageReceived += OnLog;
            clock.Start(); Recording = true;
            AudioListener.pause = false;
            StartCoroutine(RecordFrames());
            Note("recording", "Normal speed; actual controller movement, E and UI clicks; BEGIN to ending");
        }

        private IEnumerator RecordFrames()
        {
            double next = 0, statusAt = 0;
            var end = new WaitForEndOfFrame();
            while (Recording)
            {
                yield return end;
                if (!Recording || clock.Elapsed.TotalSeconds < next) continue;
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null) continue;
                byte[] jpeg = texture.EncodeToJPG(76);
                Destroy(texture);
                double now = clock.Elapsed.TotalSeconds;
                int expectedFrames = (int)Math.Floor(now * Fps) + 1;
                while (frames < expectedFrames)
                {
                    videoQueue.Enqueue(jpeg); frames++;
                }
                next = frames / (double)Fps;
                if (now >= statusAt || keyframe)
                {
                    File.WriteAllBytes(Path.Combine(folder, "latest.jpg"), jpeg);
                    File.WriteAllText(Path.Combine(folder, "progress.txt"),
                        $"{now:F1}s / {frames} frames / {lastEvent} / errors={errors} / audioSamples={audioSamples}");
                    statusAt = now + 5;
                    if (keyframe)
                    {
                        File.WriteAllBytes(Path.Combine(folder, "frame_" + ((int)now).ToString("D4") + ".jpg"), jpeg);
                        keyframe = false;
                    }
                }
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!Recording) return;
            lock (audioLock)
            {
                if (!Recording || audioStream == null) return;
                if (firstAudioDelay < 0) firstAudioDelay = clock.Elapsed.TotalSeconds;
                audioChannels = channels;
                long due = (long)((clock.Elapsed.TotalSeconds - firstAudioDelay) * sampleRate);
                long missing = due - audioSamples / channels;
                if (missing > sampleRate * .06)
                {
                    // Preserve wall-clock alignment when an Editor stall drops an audio callback.
                    var silence = new byte[missing * channels * sizeof(float)];
                    audioStream.Write(silence, 0, silence.Length);
                    audioSamples += missing * channels;
                    audioGapSamples += missing * channels;
                }
                var bytes = new byte[data.Length * sizeof(float)];
                Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
                audioStream.Write(bytes, 0, bytes.Length);
                audioSamples += data.Length;
                for (int i = 0; i < data.Length; i++) audioPeak = Mathf.Max(audioPeak, Mathf.Abs(data[i]));
            }
        }

        public void Note(string type, string words)
        {
            if (!Recording || timeline == null) return;
            timeline.WriteLine(clock.Elapsed.TotalSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                "\t" + type + "\t" + words.Replace('\n', ' ').Replace('\t', ' '));
            if (type == "state" || type == "complete" || type == "blocked")
            { lastEvent = words; keyframe = true; }
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            { errors++; Note("unity-error", message + " " + stack); }
        }

        public void FinishRecording()
        {
            if (!Recording) return;
            Note("recording", "Stop");
            Recording = false;
            clock.Stop();
            Application.logMessageReceived -= OnLog;
            lock (audioLock) { audioStream?.Flush(); audioStream?.Dispose(); audioStream = null; }
            timeline?.Dispose(); timeline = null;
            draining = false;
            videoWriter?.Wait(60000);
            if (encoder != null) encoder.WaitForExit(60000);
            errorWriter?.Wait(10000);
            File.WriteAllText(Path.Combine(folder, "capture_stats.json"), JsonUtility.ToJson(new Stats
            {
                duration = clock.Elapsed.TotalSeconds, frames = frames, fps = Fps,
                audioSampleRate = sampleRate, audioChannels = audioChannels,
                audioSamples = audioSamples, audioDuration = audioSamples / (double)(sampleRate * audioChannels),
                audioGapSamples = audioGapSamples,
                firstAudioDelay = firstAudioDelay, audioPeak = audioPeak, unityErrors = errors,
                encoderExitCode = encoder != null && encoder.HasExited ? encoder.ExitCode : -1
            }, true));
        }

        [Serializable]
        private class Stats
        {
            public double duration, audioDuration, firstAudioDelay;
            public int frames, fps, audioSampleRate, audioChannels, unityErrors, encoderExitCode;
            public long audioSamples;
            public long audioGapSamples;
            public float audioPeak;
        }
        private void OnDestroy() { FinishRecording(); }
    }
}

#endif
