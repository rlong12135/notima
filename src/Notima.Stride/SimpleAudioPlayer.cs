using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly string overworldMusicPath;
    private readonly string dungeonMusicPath;
    private readonly string townMusicPath;
    private readonly string harborMusicPath;
    private readonly string shrineMusicPath;
    private readonly string wolfAttackPath;
    private readonly string wolfGrowlPath;
    private readonly string wolfYelpPath;
    private readonly string leechSuckPath;
    private readonly string leechHitPath;
    private readonly Random random = new();
    private readonly string? ffplayPath;
    private readonly string? oggPlayerPath;
    private readonly string? wavPlayerPath;
    private string? currentMusicPath;
    private int currentMusicVolume = -1;
    private CancellationTokenSource? musicLoopCts;
    private Task? musicLoopTask;
    private Process? currentMusicPlaybackProcess;

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
        overworldMusicPath = ResolveOrCreate(
            Path.Combine(generatedAudioDirectory, "overworld-music.wav"),
            Path.Combine(generatedAudioDirectory, "overworld-music.wav"),
            BuildOverworldMusicPcm());
        dungeonMusicPath = ResolveOrCreate(
            Path.Combine(generatedAudioDirectory, "dungeon-music.wav"),
            Path.Combine(generatedAudioDirectory, "dungeon-music.wav"),
            BuildDungeonMusicPcm());
        townMusicPath = ResolveOrCreate(
            Path.Combine(generatedAudioDirectory, "town-music.wav"),
            Path.Combine(generatedAudioDirectory, "town-music.wav"),
            BuildTownMusicPcm());
        harborMusicPath = ResolveOrCreate(
            Path.Combine(generatedAudioDirectory, "harbor-music.wav"),
            Path.Combine(generatedAudioDirectory, "harbor-music.wav"),
            BuildHarborMusicPcm());
        shrineMusicPath = ResolveOrCreate(
            Path.Combine(generatedAudioDirectory, "shrine-music.wav"),
            Path.Combine(generatedAudioDirectory, "shrine-music.wav"),
            BuildShrineMusicPcm());
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

    public void SyncMusic(MusicMode mode)
    {
        var (desiredPath, desiredVolume) = mode switch
        {
            MusicMode.Dungeon => (dungeonMusicPath, 65),
            MusicMode.Town => (townMusicPath, 48),
            MusicMode.Harbor => (harborMusicPath, 50),
            MusicMode.Shrine => (shrineMusicPath, 42),
            _ => (overworldMusicPath, 52),
        };
        if (string.Equals(currentMusicPath, desiredPath, StringComparison.Ordinal) && currentMusicVolume == desiredVolume && musicLoopTask is not null)
        {
            if (!musicLoopTask.IsCompleted)
            {
                return;
            }
        }

        StartLoopingMusic(desiredPath, desiredVolume);
    }

    public void Dispose()
    {
        StopMusic();
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

    private void StartLoopingMusic(string path, int volume)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || string.IsNullOrWhiteSpace(wavPlayerPath))
        {
            StopMusic();
            return;
        }

        StopMusic();
        currentMusicPath = path;
        currentMusicVolume = volume;
        musicLoopCts = new CancellationTokenSource();
        musicLoopTask = Task.Run(() => RunMusicLoop(path, volume, musicLoopCts.Token));
    }

    private void StopMusic()
    {
        if (musicLoopCts is not null)
        {
            try
            {
                musicLoopCts.Cancel();
            }
            catch
            {
            }
        }

        try
        {
            if (currentMusicPlaybackProcess is not null && !currentMusicPlaybackProcess.HasExited)
            {
                currentMusicPlaybackProcess.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }

        try
        {
            currentMusicPlaybackProcess?.Dispose();
        }
        catch
        {
        }

        currentMusicPlaybackProcess = null;
        musicLoopTask = null;
        musicLoopCts?.Dispose();
        musicLoopCts = null;
        currentMusicPath = null;
        currentMusicVolume = -1;
    }

    private void RunMusicLoop(string path, int volume, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var playback = StartWavPlayback(path, volume);
            currentMusicPlaybackProcess = playback;
            if (playback is null)
            {
                return;
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested && !playback.WaitForExit(250))
                {
                }
            }
            catch
            {
                return;
            }
            finally
            {
                if (ReferenceEquals(currentMusicPlaybackProcess, playback))
                {
                    currentMusicPlaybackProcess = null;
                }

                try
                {
                    playback.Dispose();
                }
                catch
                {
                }
            }
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

    private Process? StartWavPlayback(string path, int volume)
    {
        if (string.IsNullOrWhiteSpace(wavPlayerPath))
        {
            return null;
        }

        var playerName = Path.GetFileName(wavPlayerPath);
        var arguments = playerName switch
        {
            "paplay" or "pw-play" => $"--volume={Math.Clamp((int)(volume * 655.35f), 0, 65536)} \"{path}\"",
            _ => $"\"{path}\"",
        };

        return StartProcess(wavPlayerPath, arguments);
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

    private static float[] BuildOverworldMusicPcm()
    {
        const int sampleRate = 22050;
        const double duration = 14.0;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];
        var melody = new[] { 392.0f, 440.0f, 523.25f, 659.25f, 587.33f, 523.25f, 440.0f, 349.23f };
        var chordRoots = new[] { 196.0f, 220.0f, 261.63f, 174.61f };

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var noteIndex = (int)(t / 1.75f) % melody.Length;
            var chordIndex = (int)(t / 3.5f) % chordRoots.Length;
            var note = melody[noteIndex];
            var root = chordRoots[chordIndex];
            var beat = t % 1.75f;
            var env = beat < 0.18f ? beat / 0.18f : MathF.Exp(-0.95f * (beat - 0.18f));
            var pad = MathF.Sin(2.0f * MathF.PI * root * t) * 0.08f;
            var fifth = MathF.Sin(2.0f * MathF.PI * root * 1.5f * t) * 0.05f;
            var lead = MathF.Sin(2.0f * MathF.PI * note * t) * 0.1f * env;
            var shimmer = MathF.Sin(2.0f * MathF.PI * note * 2.0f * t) * 0.03f * env;
            samples[i] = pad + fifth + lead + shimmer;
        }

        return samples;
    }

    private static float[] BuildDungeonMusicPcm()
    {
        const int sampleRate = 22050;
        const double duration = 14.0;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];
        var tones = new[] { 87.31f, 98.0f, 116.54f, 130.81f };

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var section = (int)(t / 3.5f) % tones.Length;
            var root = tones[section];
            var drift = MathF.Sin(2.0f * MathF.PI * 0.17f * t) * 7.0f;
            var hum = MathF.Sin(2.0f * MathF.PI * root * t) * 0.09f;
            var shadow = MathF.Sin(2.0f * MathF.PI * (root * 0.5f) * t) * 0.07f;
            var dissonance = MathF.Sin(2.0f * MathF.PI * (root * 1.52f + drift) * t) * 0.035f;
            var pulse = MathF.Sin(2.0f * MathF.PI * 1.4f * t);
            var pulseEnv = MathF.Max(0.0f, pulse) * 0.02f;
            var hiss = ((((i * 31) % 173) / 86.5f) - 1.0f) * pulseEnv;
            samples[i] = hum + shadow + dissonance + hiss;
        }

        return samples;
    }

    private static float[] BuildTownMusicPcm()
    {
        const int sampleRate = 22050;
        const double duration = 12.0;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];
        var melody = new[] { 523.25f, 587.33f, 659.25f, 698.46f, 659.25f, 587.33f, 523.25f, 440.0f };
        var roots = new[] { 261.63f, 293.66f, 329.63f, 220.0f };

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var note = melody[(int)(t / 1.5f) % melody.Length];
            var root = roots[(int)(t / 3.0f) % roots.Length];
            var pulse = t % 1.5f;
            var env = pulse < 0.08f ? pulse / 0.08f : MathF.Exp(-1.4f * (pulse - 0.08f));
            var drone = MathF.Sin(2.0f * MathF.PI * root * t) * 0.05f;
            var third = MathF.Sin(2.0f * MathF.PI * root * 1.25f * t) * 0.03f;
            var lead = MathF.Sin(2.0f * MathF.PI * note * t) * 0.08f * env;
            var bell = MathF.Sin(2.0f * MathF.PI * note * 2.0f * t) * 0.018f * env;
            samples[i] = drone + third + lead + bell;
        }

        return samples;
    }

    private static float[] BuildHarborMusicPcm()
    {
        const int sampleRate = 22050;
        const double duration = 12.0;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];
        var melody = new[] { 293.66f, 329.63f, 392.0f, 440.0f, 392.0f, 329.63f, 293.66f, 246.94f };

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var note = melody[(int)(t / 1.5f) % melody.Length];
            var swell = 0.5f + (0.5f * MathF.Sin(2.0f * MathF.PI * 0.22f * t));
            var low = MathF.Sin(2.0f * MathF.PI * 146.83f * t) * 0.045f;
            var wave = MathF.Sin(2.0f * MathF.PI * (note + (6.0f * MathF.Sin(2.0f * MathF.PI * 0.12f * t))) * t) * 0.07f * swell;
            var airy = ((((i * 17) % 97) / 48.5f) - 1.0f) * 0.012f * swell;
            samples[i] = low + wave + airy;
        }

        return samples;
    }

    private static float[] BuildShrineMusicPcm()
    {
        const int sampleRate = 22050;
        const double duration = 14.0;
        var length = (int)(sampleRate * duration);
        var samples = new float[length];
        var tones = new[] { 261.63f, 349.23f, 392.0f, 523.25f };

        for (var i = 0; i < length; i++)
        {
            var t = i / (float)sampleRate;
            var tone = tones[(int)(t / 3.5f) % tones.Length];
            var slowEnv = 0.55f + (0.45f * MathF.Sin(2.0f * MathF.PI * 0.08f * t));
            var baseTone = MathF.Sin(2.0f * MathF.PI * tone * t) * 0.035f * slowEnv;
            var upper = MathF.Sin(2.0f * MathF.PI * tone * 1.5f * t) * 0.024f * slowEnv;
            var halo = MathF.Sin(2.0f * MathF.PI * (tone * 2.0f + 2.0f * MathF.Sin(2.0f * MathF.PI * 0.11f * t)) * t) * 0.016f;
            samples[i] = baseTone + upper + halo;
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

internal enum MusicMode
{
    Overworld,
    Dungeon,
    Town,
    Harbor,
    Shrine,
}
