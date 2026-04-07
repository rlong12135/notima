using System.Diagnostics;
using System.Text;

namespace Notima.Stride;

internal sealed class SimpleAudioPlayer : IDisposable
{
    private readonly string audioDirectory;
    private readonly string stepPath;
    private readonly string bellPath;
    private readonly string clashPath;
    private readonly string swishPath;
    private readonly string trumpetPath;
    private readonly string? playerPath;

    public SimpleAudioPlayer()
    {
        audioDirectory = Path.Combine(Path.GetTempPath(), "notima-audio");
        Directory.CreateDirectory(audioDirectory);

        stepPath = Path.Combine(audioDirectory, "step.wav");
        bellPath = Path.Combine(audioDirectory, "bell.wav");
        clashPath = Path.Combine(audioDirectory, "clash.wav");
        swishPath = Path.Combine(audioDirectory, "swish.wav");
        trumpetPath = Path.Combine(audioDirectory, "trumpet.wav");

        WriteWav(stepPath, BuildStepPcm(), 22050);
        WriteWav(bellPath, BuildBellPcm(), 22050);
        WriteWav(clashPath, BuildClashPcm(), 22050);
        WriteWav(swishPath, BuildSwishPcm(), 22050);
        WriteWav(trumpetPath, BuildTrumpetPcm(), 22050);

        playerPath = ResolvePlayer();
    }

    public void PlayStep() => Play(stepPath);

    public void PlayBell() => Play(bellPath);

    public void PlayClash() => Play(clashPath);

    public void PlaySwish() => Play(swishPath);

    public void PlayTrumpet() => Play(trumpetPath);

    public void Dispose()
    {
    }

    private void Play(string path)
    {
        if (string.IsNullOrWhiteSpace(playerPath))
        {
            return;
        }

        _ = StartProcess(playerPath, $"\"{path}\"");
    }

    private static string? ResolvePlayer()
    {
        foreach (var candidate in new[] { "/usr/bin/paplay", "/usr/bin/pw-play", "/usr/bin/aplay", "/usr/bin/ffplay" })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static Process? StartProcess(string fileName, string arguments)
    {
        try
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = fileName.EndsWith("ffplay", StringComparison.Ordinal) ? $"-nodisp -loglevel quiet \"{arguments.Trim('"')}\"" : arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });
        }
        catch
        {
            return null;
        }
    }

    private static float[] BuildStepPcm()
    {
        const int sampleRate = 22050;
        const double duration = 0.07;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];
        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = MathF.Exp(-32.0f * t);
            var tone = MathF.Sin(2.0f * MathF.PI * 160.0f * t) * 0.4f;
            var click = ((i % 37) / 37.0f - 0.5f) * 0.12f;
            samples[i] = (tone + click) * envelope;
        }

        return samples;
    }

    private static float[] BuildBellPcm()
    {
        const int sampleRate = 22050;
        const double duration = 0.9;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];
        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = MathF.Exp(-3.6f * t);
            var fundamental = MathF.Sin(2.0f * MathF.PI * 880.0f * t);
            var overtone = MathF.Sin(2.0f * MathF.PI * 1320.0f * t) * 0.55f;
            var upper = MathF.Sin(2.0f * MathF.PI * 1760.0f * t) * 0.22f;
            samples[i] = (fundamental + overtone + upper) * 0.20f * envelope;
        }

        return samples;
    }

    private static float[] BuildClashPcm()
    {
        const int sampleRate = 22050;
        const double duration = 0.16;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];
        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = MathF.Exp(-18.0f * t);
            var metallicA = MathF.Sin(2.0f * MathF.PI * 620.0f * t);
            var metallicB = MathF.Sin(2.0f * MathF.PI * 910.0f * t) * 0.7f;
            var noise = (((i * 17) % 91) / 45.5f - 1.0f) * 0.22f;
            samples[i] = (metallicA + metallicB + noise) * 0.28f * envelope;
        }

        return samples;
    }

    private static float[] BuildTrumpetPcm()
    {
        const int sampleRate = 22050;
        const double duration = 0.95;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var note = t switch
            {
                < 0.22f => 523.25f,
                < 0.44f => 659.25f,
                < 0.68f => 783.99f,
                _ => 1046.5f,
            };

            var env = t < 0.08f ? t / 0.08f : MathF.Exp(-1.7f * (t - 0.08f));
            var bright = MathF.Sin(2.0f * MathF.PI * note * t);
            var brass = MathF.Sin(2.0f * MathF.PI * note * 2.0f * t) * 0.46f;
            var bite = MathF.Sin(2.0f * MathF.PI * note * 3.0f * t) * 0.18f;
            samples[i] = (bright + brass + bite) * 0.18f * env;
        }

        return samples;
    }

    private static float[] BuildSwishPcm()
    {
        const int sampleRate = 22050;
        const double duration = 0.14;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var sweep = 380.0f + (t * 900.0f);
            var env = MathF.Exp(-16.0f * t);
            var airy = MathF.Sin(2.0f * MathF.PI * sweep * t) * 0.14f;
            var noise = ((((i * 23) % 101) / 50.5f) - 1.0f) * 0.12f;
            samples[i] = (airy + noise) * env;
        }

        return samples;
    }

    private static void WriteWav(string path, float[] samples, int sampleRate)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false);

        var bytesPerSample = sizeof(short);
        var dataSize = samples.Length * bytesPerSample;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * bytesPerSample);
        writer.Write((short)bytesPerSample);
        writer.Write((short)(bytesPerSample * 8));
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            var clamped = Math.Clamp(sample, -1.0f, 1.0f);
            writer.Write((short)(clamped * short.MaxValue));
        }
    }
}
