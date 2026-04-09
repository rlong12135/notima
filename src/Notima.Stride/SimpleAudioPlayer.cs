using System.Diagnostics;
using System.Text;

namespace Notima.Stride;

internal sealed class SimpleAudioPlayer : IDisposable
{
    private readonly string generatedAudioDirectory;
    private readonly string[] stepPaths;
    private readonly string[] clashPaths;
    private readonly string bellPath;
    private readonly string swishPath;
    private readonly string trumpetPath;
    private readonly string magicPath;
    private readonly string portalPath;
    private readonly string chestPath;
    private readonly string wolfAttackPath;
    private readonly string wolfGrowlPath;
    private readonly string wolfYelpPath;
    private readonly string leechSuckPath;
    private readonly string leechHitPath;
    private readonly Random random = new();
    private readonly string? ffplayPath;
    private readonly string? oggPlayerPath;
    private readonly string? wavPlayerPath;

    public SimpleAudioPlayer()
    {
        var contentAudioDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "Audio");
        generatedAudioDirectory = Path.Combine(Path.GetTempPath(), "notima-audio");
        Directory.CreateDirectory(generatedAudioDirectory);

        stepPaths = ResolveStepPaths(contentAudioDirectory);
        clashPaths = ResolveExistingPaths(
            Path.Combine(contentAudioDirectory, "metal-hit.wav"),
            Path.Combine(contentAudioDirectory, "metal-hit-2.wav"),
            Path.Combine(contentAudioDirectory, "metal-hit-3.wav"));
        bellPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "pleasing-bell.wav"),
            Path.Combine(generatedAudioDirectory, "bell.wav"),
            BuildBellPcm());
        swishPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "swish-miss.ogg"),
            Path.Combine(generatedAudioDirectory, "swish.wav"),
            BuildSwishPcm());
        trumpetPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "castlefanfare.ogg"),
            Path.Combine(generatedAudioDirectory, "trumpet.wav"),
            BuildTrumpetPcm());
        magicPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "magical_1.ogg"),
            Path.Combine(generatedAudioDirectory, "magic.wav"),
            BuildMagicPcm());
        portalPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "teleport.wav"),
            Path.Combine(generatedAudioDirectory, "portal.wav"),
            BuildPortalPcm());
        chestPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "door_open_01.ogg"),
            Path.Combine(generatedAudioDirectory, "chest.wav"),
            BuildChestPcm());
        wolfAttackPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "wolf_monster_6.mp3"),
            Path.Combine(generatedAudioDirectory, "wolf-attack.wav"),
            BuildClashPcm());
        wolfGrowlPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "wolf_monster_6.mp3"),
            Path.Combine(generatedAudioDirectory, "wolf-growl.wav"),
            BuildSwishPcm());
        wolfYelpPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "wolf_monster_6.mp3"),
            Path.Combine(generatedAudioDirectory, "wolf-yelp.wav"),
            BuildSwishPcm());
        leechSuckPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "leech-suck.ogg"),
            Path.Combine(generatedAudioDirectory, "leech.wav"),
            BuildMagicPcm());
        leechHitPath = ResolveOrCreate(
            Path.Combine(contentAudioDirectory, "impactsplat02.mp3.flac"),
            Path.Combine(generatedAudioDirectory, "leech-hit.wav"),
            BuildMeatCutPcm());
        ffplayPath = ResolveExecutable("/usr/bin/ffplay");
        oggPlayerPath = ResolveExecutable("/usr/bin/ogg123");
        wavPlayerPath = ResolveFirstAvailable("/usr/bin/paplay", "/usr/bin/pw-play", "/usr/bin/aplay");
    }

    public void PlayStep()
    {
        if (stepPaths.Length == 0)
        {
            return;
        }

        Play(stepPaths[random.Next(stepPaths.Length)]);
    }

    public void PlayBell() => Play(bellPath);

    public void PlayClash()
    {
        if (clashPaths.Length > 0)
        {
            Play(clashPaths[random.Next(clashPaths.Length)]);
            return;
        }

        var fallback = ResolveOrCreate(
            Path.Combine(generatedAudioDirectory, "missing-clash.wav"),
            Path.Combine(generatedAudioDirectory, "clash.wav"),
            BuildClashPcm());
        Play(fallback);
    }

    public void PlaySwish() => Play(swishPath);

    public void PlayTrumpet() => Play(trumpetPath);

    public void PlayMagic() => Play(magicPath);

    public void PlayPortal() => Play(portalPath);

    public void PlayChest() => Play(chestPath);

    public void PlayWolfAttack() => Play(wolfAttackPath);

    public void PlayWolfGrowl() => Play(wolfGrowlPath);

    public void PlayWolfYelp() => Play(wolfYelpPath);

    public void PlayLeechSuck() => Play(leechSuckPath);

    public void PlayLeechHit() => Play(leechHitPath);

    public void Dispose()
    {
    }

    private void Play(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if ((extension == ".ogg" || extension == ".mp3") && !string.IsNullOrWhiteSpace(ffplayPath))
        {
            _ = StartProcess(ffplayPath, $"-nodisp -autoexit -loglevel quiet \"{path}\"");
            return;
        }

        if (extension == ".ogg" && !string.IsNullOrWhiteSpace(oggPlayerPath))
        {
            _ = StartProcess(oggPlayerPath, $"-q \"{path}\"");
            return;
        }

        if (extension == ".wav" && !string.IsNullOrWhiteSpace(wavPlayerPath))
        {
            _ = StartProcess(wavPlayerPath, $"\"{path}\"");
            return;
        }

        if (!string.IsNullOrWhiteSpace(ffplayPath))
        {
            _ = StartProcess(ffplayPath, $"-nodisp -autoexit -loglevel quiet \"{path}\"");
        }
    }

    private static string[] ResolveStepPaths(string contentAudioDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(contentAudioDirectory, "01-footstep.ogg"),
            Path.Combine(contentAudioDirectory, "02-footstep.ogg"),
            Path.Combine(contentAudioDirectory, "03-footstep.ogg"),
            Path.Combine(contentAudioDirectory, "04-footstep.ogg"),
        };

        return candidates.Where(File.Exists).ToArray();
    }

    private static string[] ResolveExistingPaths(params string[] candidates)
    {
        return candidates.Where(File.Exists).ToArray();
    }

    private static string ResolveOrCreate(string preferredPath, string fallbackGeneratedPath, float[] fallbackPcm)
    {
        if (File.Exists(preferredPath))
        {
            return preferredPath;
        }

        WriteWav(fallbackGeneratedPath, fallbackPcm, 22050);
        return fallbackGeneratedPath;
    }

    private static string? ResolveExecutable(string path)
    {
        return File.Exists(path) ? path : null;
    }

    private static string? ResolveFirstAvailable(params string[] candidates)
    {
        foreach (var candidate in candidates)
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
                Arguments = arguments,
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

    private static float[] BuildMagicPcm()
    {
        const int sampleRate = 22050;
        const double duration = 0.55;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var env = t < 0.08f ? t / 0.08f : MathF.Exp(-4.0f * (t - 0.08f));
            var shimmer = MathF.Sin(2.0f * MathF.PI * (520.0f + (t * 420.0f)) * t) * 0.16f;
            var upper = MathF.Sin(2.0f * MathF.PI * (890.0f + (t * 260.0f)) * t) * 0.09f;
            samples[i] = (shimmer + upper) * env;
        }

        return samples;
    }

    private static float[] BuildPortalPcm()
    {
        const int sampleRate = 22050;
        const double duration = 1.4;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var env = MathF.Exp(-1.8f * t);
            var hum = MathF.Sin(2.0f * MathF.PI * 140.0f * t) * 0.08f;
            var sweep = MathF.Sin(2.0f * MathF.PI * (260.0f + (t * 540.0f)) * t) * 0.12f;
            var crackle = ((((i * 29) % 131) / 65.5f) - 1.0f) * 0.05f;
            samples[i] = (hum + sweep + crackle) * env;
        }

        return samples;
    }

    private static float[] BuildChestPcm()
    {
        const int sampleRate = 22050;
        const double duration = 0.4;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var env = MathF.Exp(-6.0f * t);
            var wood = MathF.Sin(2.0f * MathF.PI * 170.0f * t) * 0.08f;
            var scrape = MathF.Sin(2.0f * MathF.PI * (220.0f + (t * 90.0f)) * t) * 0.05f;
            var noise = ((((i * 19) % 101) / 50.5f) - 1.0f) * 0.03f;
            samples[i] = (wood + scrape + noise) * env;
        }

        return samples;
    }

    private static float[] BuildMeatCutPcm()
    {
        const int sampleRate = 22050;
        const double duration = 0.18;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var env = MathF.Exp(-14.0f * t);
            var thud = MathF.Sin(2.0f * MathF.PI * 120.0f * t) * 0.12f;
            var tear = MathF.Sin(2.0f * MathF.PI * (320.0f + (t * 180.0f)) * t) * 0.08f;
            var wetNoise = ((((i * 37) % 127) / 63.5f) - 1.0f) * 0.16f;
            samples[i] = (thud + tear + wetNoise) * env;
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
