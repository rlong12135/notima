using System.Text.Json;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Stride.Shaders.Compiler;

namespace Notima.Stride;

public sealed class NotimaGame : Game
{
    private const float TileSize = 1.0f;
    private const float MoveRepeatDelay = 0.16f;

    private readonly Dictionary<char, TileDefinition> tileDefinitions = new()
    {
        ['.'] = new("Plains", new Color(105, 156, 92), 0.18f, true),
        ['*'] = new("Forest", new Color(66, 108, 62), 0.36f, true),
        ['='] = new("Road", new Color(166, 144, 107), 0.2f, true),
        ['~'] = new("Sea", new Color(59, 101, 166), 0.06f, false),
        ['^'] = new("Mountains", new Color(126, 120, 128), 0.64f, false),
        ['F'] = new("Fen", new Color(74, 118, 88), 0.12f, true),
        ['T'] = new("Town", new Color(214, 181, 101), 0.28f, true, "A trading town with shuttered inns."),
        ['K'] = new("Keep", new Color(209, 184, 136), 0.34f, true, "An old keep watches the northern approach."),
        ['R'] = new("Ruins", new Color(172, 130, 128), 0.26f, true, "Weathered ruins. Something once mattered here."),
        ['S'] = new("Shrine", new Color(194, 133, 210), 0.24f, true, "A small shrine stands quiet among the fen."),
        ['H'] = new("Harbor", new Color(191, 161, 124), 0.22f, true, "A harbor town leans into the inland sea."),
        ['C'] = new("Camp", new Color(196, 109, 85), 0.22f, true, "A lonely campfire marks a traveler stop."),
        ['D'] = new("Dungeon", new Color(145, 86, 86), 0.22f, true, "A dungeon mouth opens in the earth."),
        ['P'] = new("Path", new Color(149, 166, 110), 0.2f, true),
    };

    private Scene rootScene = null!;
    private CameraComponent camera = null!;
    private Entity playerEntity = null!;
    private Entity cursorEntity = null!;
    private Entity focusMarkerEntity = null!;
    private Entity? mapRoot;

    private OverworldMap map = null!;
    private Point playerCell;
    private string statusLine = "Find the road east and the old dungeon south.";
    private float moveCooldown;

    public NotimaGame()
    {
        GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
        GraphicsDeviceManager.PreferredBackBufferHeight = 720;
        GraphicsDeviceManager.SynchronizeWithVerticalRetrace = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        Window.Title = "notima";
        Window.AllowUserResizing = true;
    }

    protected override bool BeginDraw()
    {
        if (!base.BeginDraw())
        {
            return false;
        }

        GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, new Color4(0.70f, 0.84f, 0.95f, 1.0f));
        GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
        return true;
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        ConfigureLocalShaderCompiler();
        rootScene = new Scene();
        SceneSystem.GraphicsCompositor = GraphicsCompositorHelper.CreateDefault(false, graphicsProfile: GraphicsProfile.Level_10_0);
        SceneSystem.SceneInstance = new SceneInstance(Services, rootScene);

        CreateCamera();
        CreateLights();
        CreatePlayer();
        CreateCursor();
        LoadMapFromDisk();
        RebuildMapScene();
        UpdateWindowTitle();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var dt = (float)gameTime.Elapsed.TotalSeconds;
        moveCooldown = MathF.Max(0.0f, moveCooldown - dt);

        HandleInput();
        UpdatePlayerTransform();
        UpdateFocusMarker();
        UpdateWindowTitle();
    }

    private void HandleInput()
    {
        if (Input.IsKeyPressed(Keys.Escape))
        {
            Exit();
            return;
        }

        if (Input.IsKeyPressed(Keys.R))
        {
            LoadMapFromDisk();
            RebuildMapScene();
            statusLine = "The overworld settles back into place.";
            return;
        }

        if (Input.IsKeyPressed(Keys.Enter))
        {
            statusLine = DescribeCurrentTile();
            return;
        }

        if (moveCooldown > 0.0f)
        {
            return;
        }

        var delta = Point.Zero;
        if (Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W))
        {
            delta = new Point(0, -1);
        }
        else if (Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
        {
            delta = new Point(0, 1);
        }
        else if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A))
        {
            delta = new Point(-1, 0);
        }
        else if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D))
        {
            delta = new Point(1, 0);
        }

        if (delta == Point.Zero)
        {
            return;
        }

        moveCooldown = MoveRepeatDelay;
        TryMove(delta);
    }

    private void TryMove(Point delta)
    {
        var target = new Point(playerCell.X + delta.X, playerCell.Y + delta.Y);
        if (target.X < 0 || target.Y < 0 || target.X >= map.Width || target.Y >= map.Height)
        {
            statusLine = "The sea edge blocks the way.";
            return;
        }

        var tile = GetTileDefinition(map.Rows[target.Y][target.X]);
        if (!tile.Walkable)
        {
            statusLine = $"The {tile.Name.ToLowerInvariant()} blocks your path.";
            return;
        }

        playerCell = target;
        statusLine = DescribeCurrentTile();
    }

    private string DescribeCurrentTile()
    {
        var tile = GetTileDefinition(map.Rows[playerCell.Y][playerCell.X]);
        var baseText = $"[{playerCell.X},{playerCell.Y}] {tile.Name}";
        return string.IsNullOrWhiteSpace(tile.InspectText) ? baseText : $"{baseText}: {tile.InspectText}";
    }

    private void LoadMapFromDisk()
    {
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Content", "Maps", "overworld.json");
        var json = File.ReadAllText(mapPath);
        map = JsonSerializer.Deserialize<OverworldMap>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to load overworld map.");

        if (map.Rows.Count == 0)
        {
            throw new InvalidOperationException("Overworld map is empty.");
        }

        playerCell = new Point(map.Start.X, map.Start.Y);
        statusLine = DescribeCurrentTile();
    }

    private void RebuildMapScene()
    {
        if (mapRoot is not null)
        {
            rootScene.Entities.Remove(mapRoot);
        }

        mapRoot = new Entity("MapRoot");
        rootScene.Entities.Add(mapRoot);

        for (var y = 0; y < map.Rows.Count; y++)
        {
            var row = map.Rows[y];
            for (var x = 0; x < row.Length; x++)
            {
                var tile = GetTileDefinition(row[x]);
                var worldPosition = CellToWorld(x, y);
                var tileEntity = CreateBoxEntity(
                    $"Tile.{x}.{y}",
                    new Vector3(TileSize, tile.Height, TileSize),
                    new Vector3(worldPosition.X, (tile.Height * 0.5f) - 0.02f, worldPosition.Z),
                    tile.Color);

                mapRoot.AddChild(tileEntity);

                if (!tile.Walkable)
                {
                    continue;
                }

                if (row[x] is 'T' or 'K' or 'R' or 'S' or 'H' or 'C' or 'D')
                {
                    var marker = CreateLandmarkEntity(row[x], worldPosition);
                    mapRoot.AddChild(marker);
                }
            }
        }

        UpdatePlayerTransform();
        UpdateFocusMarker();
    }

    private Entity CreateLandmarkEntity(char symbol, Vector3 worldPosition)
    {
        var colors = symbol switch
        {
            'T' => new Color(242, 202, 109),
            'K' => new Color(219, 214, 196),
            'R' => new Color(173, 129, 129),
            'S' => new Color(206, 145, 226),
            'H' => new Color(210, 168, 117),
            'C' => new Color(214, 116, 92),
            'D' => new Color(167, 90, 90),
            _ => new Color(255, 255, 255),
        };

        var model = new ProceduralModelDescriptor(new CubeProceduralModel
        {
            Size = new Vector3(0.42f, 0.52f, 0.42f),
            MaterialInstance = { Material = CreateMaterial(colors) }
        }).GenerateModel(Services);

        var entity = new Entity($"Landmark.{symbol}")
        {
            new ModelComponent(model)
        };
        entity.Transform.Position = new Vector3(worldPosition.X, 0.45f, worldPosition.Z);
        return entity;
    }

    private void CreateCamera()
    {
        camera = new CameraComponent
        {
            Slot = SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId()
        };

        var cameraEntity = new Entity("Camera")
        {
            camera
        };

        rootScene.Entities.Add(cameraEntity);
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        var center = new Vector3((map?.Width ?? 20) * 0.5f - 0.5f, 0.0f, (map?.Height ?? 20) * -0.5f + 0.5f);
        var cameraPosition = center + new Vector3(0.0f, 17.5f, 12.0f);
        var lookTarget = center + new Vector3(0.0f, 0.0f, -2.0f);

        var cameraEntity = camera.Entity!;
        cameraEntity.Transform.UseTRS = false;
        cameraEntity.Transform.LocalMatrix = Matrix.Invert(Matrix.LookAtRH(cameraPosition, lookTarget, Vector3.UnitY));
    }

    private void CreateLights()
    {
        var lightEntity = new Entity("Light")
        {
            new LightComponent()
        };
        lightEntity.Transform.Position = new Vector3(5.0f, 18.0f, 6.0f);
        lightEntity.Transform.Rotation = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(-24.0f),
            MathUtil.DegreesToRadians(-57.0f),
            0.0f);

        rootScene.Entities.Add(lightEntity);
    }

    private void CreatePlayer()
    {
        var model = new ProceduralModelDescriptor(new SphereProceduralModel
        {
            Radius = 0.28f,
            Tessellation = 18,
            MaterialInstance = { Material = CreateMaterial(new Color(255, 231, 184)) }
        }).GenerateModel(Services);

        playerEntity = new Entity("Player")
        {
            new ModelComponent(model)
        };
        rootScene.Entities.Add(playerEntity);
    }

    private void CreateCursor()
    {
        var ringModel = new ProceduralModelDescriptor(new CubeProceduralModel
        {
            Size = new Vector3(0.92f, 0.03f, 0.92f),
            MaterialInstance = { Material = CreateMaterial(new Color(255, 247, 160)) }
        }).GenerateModel(Services);

        cursorEntity = new Entity("Cursor")
        {
            new ModelComponent(ringModel)
        };
        rootScene.Entities.Add(cursorEntity);

        var focusModel = new ProceduralModelDescriptor(new CubeProceduralModel
        {
            Size = new Vector3(0.25f, 0.8f, 0.25f),
            MaterialInstance = { Material = CreateMaterial(new Color(255, 255, 255)) }
        }).GenerateModel(Services);

        focusMarkerEntity = new Entity("FocusMarker")
        {
            new ModelComponent(focusModel)
        };
        rootScene.Entities.Add(focusMarkerEntity);
    }

    private void UpdatePlayerTransform()
    {
        var world = CellToWorld(playerCell.X, playerCell.Y);
        playerEntity.Transform.Position = new Vector3(world.X, 0.48f, world.Z);
        cursorEntity.Transform.Position = new Vector3(world.X, 0.03f, world.Z);
    }

    private void UpdateFocusMarker()
    {
        var world = CellToWorld(playerCell.X, playerCell.Y);
        focusMarkerEntity.Transform.Position = new Vector3(world.X, 0.62f, world.Z);
    }

    private Vector3 CellToWorld(int x, int y)
    {
        var offsetX = (map.Width - 1) * 0.5f;
        var offsetZ = (map.Height - 1) * 0.5f;
        return new Vector3((x - offsetX) * TileSize, 0.0f, (offsetZ - y) * TileSize);
    }

    private TileDefinition GetTileDefinition(char symbol)
    {
        if (tileDefinitions.TryGetValue(symbol, out var definition))
        {
            return definition;
        }

        return tileDefinitions['.'];
    }

    private Entity CreateBoxEntity(string name, Vector3 size, Vector3 position, Color color)
    {
        var model = new ProceduralModelDescriptor(new CubeProceduralModel
        {
            Size = size,
            MaterialInstance = { Material = CreateMaterial(color) }
        }).GenerateModel(Services);

        var entity = new Entity(name)
        {
            new ModelComponent(model)
        };
        entity.Transform.Position = position;
        return entity;
    }

    private Material CreateMaterial(Color color)
    {
        return Material.New(GraphicsDevice, new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            }
        });
    }

    private void UpdateWindowTitle()
    {
        Window.Title = $"notima | {map.Name} | {statusLine}";
    }

    private void ConfigureLocalShaderCompiler()
    {
        var strideSourceRoot = ResolveStrideSourceRoot();
        if (strideSourceRoot is null)
        {
            return;
        }

        List<string> shaderDirectories;
        var useFileSystem = Platform.IsWindowsDesktop;

        if (useFileSystem)
        {
            shaderDirectories = Directory
                .EnumerateFiles(strideSourceRoot, "*.sdsl", SearchOption.AllDirectories)
                .Select(Path.GetDirectoryName)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()!;
        }
        else
        {
            var shaderCachePath = PrepareLinuxShaderCache(strideSourceRoot);
            if (shaderCachePath is null)
            {
                return;
            }

            VirtualFileSystem.RemountFileSystem("/shaders", shaderCachePath);
            shaderDirectories = ["/shaders"];
        }

        if (shaderDirectories.Count == 0)
        {
            return;
        }

        var databaseProvider = Services.GetService<IDatabaseFileProviderService>()?.FileProvider;
        if (databaseProvider is null)
        {
            return;
        }

        IVirtualFileProvider compilerFileProvider = useFileSystem ? Content.FileProvider : new GlobalVirtualFileProvider();

        var compiler = new EffectCompiler(compilerFileProvider)
        {
            UseFileSystem = useFileSystem
        };
        compiler.SourceDirectories.AddRange(shaderDirectories);
        EffectSystem.Compiler = new EffectCompilerCache(compiler, databaseProvider);
    }

    private static string? PrepareLinuxShaderCache(string strideSourceRoot)
    {
        var generatedShaderRoots = Directory
            .EnumerateDirectories(strideSourceRoot, "Assets", SearchOption.AllDirectories)
            .Where(path => path.Replace('\\', '/').Contains("/obj/Debug/stride/"))
            .ToList();

        if (generatedShaderRoots.Count == 0)
        {
            return null;
        }

        var cachePath = Path.Combine(Path.GetTempPath(), "notima-stride-shaders");
        Directory.CreateDirectory(cachePath);

        foreach (var shaderFile in generatedShaderRoots.SelectMany(root => Directory.EnumerateFiles(root, "*.sdsl", SearchOption.AllDirectories)))
        {
            var destinationPath = Path.Combine(cachePath, Path.GetFileName(shaderFile));
            var sourceInfo = new FileInfo(shaderFile);
            var destinationInfo = new FileInfo(destinationPath);
            if (!destinationInfo.Exists || sourceInfo.LastWriteTimeUtc > destinationInfo.LastWriteTimeUtc)
            {
                File.Copy(shaderFile, destinationPath, overwrite: true);
            }
        }

        return cachePath;
    }

    private static string? ResolveStrideSourceRoot()
    {
        var configuredRoot = Environment.GetEnvironmentVariable("STRIDE_SOURCE_DIR");
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var configuredEngineRoot = Path.Combine(configuredRoot, "sources", "engine");
            if (Directory.Exists(configuredEngineRoot))
            {
                return configuredEngineRoot;
            }

            if (Directory.Exists(configuredRoot))
            {
                return configuredRoot;
            }
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var defaultRoots = new[]
        {
            Path.Combine(userProfile, "Applications", "stride", "sources", "engine"),
            Path.Combine(userProfile, "source", "stride", "sources", "engine"),
            @"C:\Users\rlong\source\stride\sources\engine",
        };

        return defaultRoots.FirstOrDefault(Directory.Exists);
    }
}

internal sealed record TileDefinition(string Name, Color Color, float Height, bool Walkable, string? InspectText = null);

internal sealed class OverworldMap
{
    public string Name { get; set; } = "Untitled Overworld";

    public StartCell Start { get; set; } = new();

    public List<string> Rows { get; set; } = [];

    public int Width => Rows.Count == 0 ? 0 : Rows[0].Length;

    public int Height => Rows.Count;
}

internal sealed class StartCell
{
    public int X { get; set; }

    public int Y { get; set; }
}
