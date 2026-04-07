using System.Text.Json;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;

namespace Notima.Stride;

public sealed class NotimaGame : Game
{
    private const float MoveRepeatDelay = 0.16f;
    private const int TilePixels = 24;
    private const int MapOffsetX = 28;
    private const int MapOffsetY = 28;
    private const int HudX = 760;
    private const int HudWidth = 484;
    private const int PanelX = 812;
    private const int PanelY = 308;
    private const int PanelWidth = 388;
    private const int PanelHeight = 230;

    private readonly Dictionary<char, TileDefinition> tileDefinitions = new()
    {
        ['.'] = new("Plains", new Color(163, 212, 124), true, new Rectangle(0, 0, 16, 16), "Open grassland. Travel is easy here."),
        ['*'] = new("Forest", new Color(116, 192, 129), true, new Rectangle(32, 0, 16, 16), "Dense woods close around the road."),
        ['='] = new("Road", new Color(226, 212, 164), true, new Rectangle(64, 16, 16, 16), "A worked road links the settled places."),
        ['~'] = new("Sea", new Color(136, 187, 255), false, new Rectangle(16, 0, 16, 16), "The sea bars passage without a ship."),
        ['^'] = new("Mountains", new Color(208, 208, 220), false, new Rectangle(64, 32, 16, 16), "The mountains wall off the horizon."),
        ['F'] = new("Fen", new Color(159, 203, 146), true, new Rectangle(48, 16, 16, 16), "The fen is wet, dim, and full of insects."),
        ['T'] = new("Town", new Color(255, 226, 124), true, new Rectangle(0, 48, 16, 16), "A trading town with shuttered inns."),
        ['K'] = new("Keep", new Color(245, 238, 206), true, new Rectangle(16, 48, 16, 16), "An old keep watches the northern approach."),
        ['R'] = new("Ruins", new Color(232, 178, 170), true, new Rectangle(32, 48, 16, 16), "Weathered ruins. Something once mattered here."),
        ['S'] = new("Shrine", new Color(240, 183, 255), true, new Rectangle(48, 48, 16, 16), "A small shrine stands quiet among the fen."),
        ['H'] = new("Harbor", new Color(255, 215, 143), true, new Rectangle(64, 48, 16, 16), "A harbor town leans into the inland sea."),
        ['C'] = new("Camp", new Color(255, 169, 137), true, new Rectangle(80, 48, 16, 16), "A lonely campfire marks a traveler stop."),
        ['D'] = new("Dungeon", new Color(214, 136, 136), true, new Rectangle(0, 64, 16, 16), "A dungeon mouth opens in the earth."),
        ['P'] = new("Path", new Color(206, 224, 144), true, new Rectangle(80, 16, 16, 16), "A narrow track worn by many feet."),
    };

    private readonly Dictionary<char, string[]> glyphs = BitmapFont.Create();
    private readonly HashSet<GridPoint> visitedLandmarks = [];
    private readonly Random random = new();

    private SpriteBatch spriteBatch = null!;
    private Texture whiteTexture = null!;
    private Texture playerTexture = null!;
    private Texture tileTexture = null!;
    private OverworldMap map = null!;
    private GridPoint playerCell;
    private Direction facing = Direction.Down;
    private UiMode uiMode;
    private PartyState party = new();
    private EncounterState? encounter;
    private string panelTitle = string.Empty;
    private List<string> panelLines = [];
    private string statusLine = "Find the road east and the old dungeon south.";
    private float moveCooldown;
    private float totalTime;
    private int walkFrame;

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

        GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, new Color4(0.13f, 0.17f, 0.25f, 1.0f));
        GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
        return true;
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        spriteBatch = new SpriteBatch(GraphicsDevice);
        whiteTexture = GraphicsDevice.GetSharedWhiteTexture();
        tileTexture = LoadRgbaTexture("Content/Art/overworld-tiles-cc0.rgba", 96, 112);
        playerTexture = LoadRgbaTexture("Content/Art/julia-player-cc0.rgba", 128, 128);
        LoadMapFromDisk();
        UpdateWindowTitle();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var dt = (float)gameTime.Elapsed.TotalSeconds;
        totalTime += dt;
        moveCooldown = MathF.Max(0.0f, moveCooldown - dt);

        HandleInput();
        UpdateWindowTitle();
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
        spriteBatch.Begin(GraphicsContext);
        DrawMap();
        DrawHud();
        DrawPanels();
        spriteBatch.End();
    }

    protected override void Destroy()
    {
        tileTexture?.Dispose();
        playerTexture?.Dispose();
        spriteBatch?.Dispose();
        base.Destroy();
    }

    private void HandleInput()
    {
        if (Input.IsKeyPressed(Keys.Escape))
        {
            Exit();
            return;
        }

        switch (uiMode)
        {
            case UiMode.Encounter:
                HandleEncounterInput();
                return;
            case UiMode.Dialog:
                HandleDialogInput();
                return;
        }

        if (Input.IsKeyPressed(Keys.R))
        {
            ResetOverworld();
            return;
        }

        if (Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
            InteractWithCurrentTile();
            return;
        }

        if (moveCooldown > 0.0f)
        {
            return;
        }

        var delta = GridPoint.Zero;
        if (Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W))
        {
            delta = new GridPoint(0, -1);
            facing = Direction.Up;
        }
        else if (Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
        {
            delta = new GridPoint(0, 1);
            facing = Direction.Down;
        }
        else if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A))
        {
            delta = new GridPoint(-1, 0);
            facing = Direction.Left;
        }
        else if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D))
        {
            delta = new GridPoint(1, 0);
            facing = Direction.Right;
        }

        if (delta == GridPoint.Zero)
        {
            return;
        }

        moveCooldown = MoveRepeatDelay;
        TryMove(delta);
    }

    private void HandleEncounterInput()
    {
        if (Input.IsKeyPressed(Keys.R))
        {
            AttemptRetreat();
            return;
        }

        if (Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
            ResolveEncounterRound();
        }
    }

    private void HandleDialogInput()
    {
        if (Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
            uiMode = UiMode.Overworld;
            panelTitle = string.Empty;
            panelLines.Clear();
        }
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

        playerCell = new GridPoint(map.Start.X, map.Start.Y);
        party = new PartyState
        {
            Level = 1,
            MaxHealth = 24,
            Health = 24,
            Food = 120,
            Gold = 30,
            Steps = 0
        };
        encounter = null;
        panelTitle = string.Empty;
        panelLines.Clear();
        uiMode = UiMode.Overworld;
        visitedLandmarks.Clear();
        statusLine = DescribeCurrentTile();
    }

    private void ResetOverworld()
    {
        LoadMapFromDisk();
        statusLine = "The overworld settles back into place.";
    }

    private void TryMove(GridPoint delta)
    {
        var target = new GridPoint(playerCell.X + delta.X, playerCell.Y + delta.Y);
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
        walkFrame = (walkFrame + 1) % 3;
        party.Steps++;
        if (party.Steps % 5 == 0)
        {
            party.Food = Math.Max(0, party.Food - 1);
            if (party.Food == 0)
            {
                party.Health = Math.Max(1, party.Health - 1);
            }
        }

        statusLine = DescribeCurrentTile();
        MaybeTriggerEncounter(tile);
    }

    private void MaybeTriggerEncounter(TileDefinition tile)
    {
        if (!tile.CanEncounter || random.NextDouble() > tile.EncounterChance)
        {
            return;
        }

        encounter = tile.Name switch
        {
            "Forest" => new EncounterState("WOLF PACK", 14, 3, 6, 11, 4),
            "Fen" => new EncounterState("FEN LEECHES", 13, 2, 5, 9, 6),
            _ => new EncounterState("ROAD BANDITS", 12, 2, 5, 12, 3),
        };

        panelTitle = "ENCOUNTER";
        panelLines =
        [
            $"A {encounter.Name} RUSHES IN.",
            $"FOE HP {encounter.Health}/{encounter.MaxHealth}",
            "ENTER ATTACKS",
            "R ATTEMPTS RETREAT",
        ];
        statusLine = $"Encounter! {encounter.Name} blocks the road.";
        uiMode = UiMode.Encounter;
    }

    private void ResolveEncounterRound()
    {
        if (encounter is null)
        {
            uiMode = UiMode.Overworld;
            return;
        }

        var playerDamage = random.Next(4, 9) + party.Level;
        encounter.Health = Math.Max(0, encounter.Health - playerDamage);
        if (encounter.Health <= 0)
        {
            party.Gold += encounter.RewardGold;
            party.Food += encounter.RewardFood;
            statusLine = $"{encounter.Name} defeated. Gold +{encounter.RewardGold}, Food +{encounter.RewardFood}.";
            OpenDialog(
                "VICTORY",
                $"{encounter.Name} FALLS.",
                $"GOLD +{encounter.RewardGold}",
                $"FOOD +{encounter.RewardFood}",
                "ENTER CONTINUES"
            );
            encounter = null;
            return;
        }

        var enemyDamage = Math.Max(1, random.Next(encounter.AttackMin, encounter.AttackMax + 1) - (party.Level / 2));
        party.Health = Math.Max(0, party.Health - enemyDamage);
        statusLine = $"You hit for {playerDamage}. The {encounter.Name} answers for {enemyDamage}.";
        panelLines =
        [
            $"{encounter.Name} HP {encounter.Health}/{encounter.MaxHealth}",
            $"PARTY HP {party.Health}/{party.MaxHealth}",
            "ENTER ATTACKS",
            "R ATTEMPTS RETREAT",
        ];

        if (party.Health <= 0)
        {
            HandleDefeat();
        }
    }

    private void AttemptRetreat()
    {
        if (encounter is null)
        {
            uiMode = UiMode.Overworld;
            return;
        }

        if (random.NextDouble() < 0.45)
        {
            statusLine = $"You slip away from the {encounter.Name}.";
            encounter = null;
            uiMode = UiMode.Overworld;
            panelLines.Clear();
            panelTitle = string.Empty;
            return;
        }

        var damage = Math.Max(1, random.Next(encounter.AttackMin, encounter.AttackMax + 1) - party.Level);
        party.Health = Math.Max(0, party.Health - damage);
        statusLine = $"Retreat failed. The {encounter.Name} hits for {damage}.";
        panelLines =
        [
            $"A {encounter.Name} HOLDS YOU FAST.",
            $"PARTY HP {party.Health}/{party.MaxHealth}",
            "ENTER STRIKES BACK",
            "R TRIES TO FLEE AGAIN",
        ];

        if (party.Health <= 0)
        {
            HandleDefeat();
        }
    }

    private void HandleDefeat()
    {
        party.Health = party.MaxHealth;
        party.Gold = Math.Max(0, party.Gold - 12);
        playerCell = new GridPoint(map.Start.X, map.Start.Y);
        encounter = null;
        OpenDialog(
            "DEFEAT",
            "THE PARTY COLLAPSES.",
            "A STRANGER BRINGS YOU BACK",
            "TO THE STARTING ROAD.",
            "ENTER RISES AGAIN"
        );
        statusLine = "The party was defeated and dragged back to safety.";
    }

    private void InteractWithCurrentTile()
    {
        var symbol = map.Rows[playerCell.Y][playerCell.X];
        switch (symbol)
        {
            case 'T':
            case 'H':
            case 'C':
                party.Health = party.MaxHealth;
                party.Food += 18;
                OpenDialog(
                    symbol switch
                    {
                        'T' => "TOWN",
                        'H' => "HARBOR",
                        _ => "CAMP",
                    },
                    "YOU REST AND RECOVER.",
                    "HEALTH FULLY RESTORED.",
                    "FOOD +18",
                    "ENTER CONTINUES"
                );
                statusLine = "The party rests and resupplies.";
                break;
            case 'K':
                if (party.Gold >= 30)
                {
                    party.Gold -= 30;
                    party.Level++;
                    party.MaxHealth += 6;
                    party.Health = party.MaxHealth;
                    OpenDialog(
                        "KEEP",
                        "THE LORD'S CAPTAIN TRAINS YOU.",
                        $"LEVEL IS NOW {party.Level}.",
                        "MAX HP +6",
                        "ENTER CONTINUES"
                    );
                    statusLine = "Training at the keep hardens the party.";
                }
                else
                {
                    OpenDialog(
                        "KEEP",
                        "THE CAPTAIN OFFERS TRAINING",
                        "FOR 30 GOLD.",
                        "YOU CANNOT PAY YET.",
                        "ENTER CONTINUES"
                    );
                    statusLine = "The keep demands gold for training.";
                }

                break;
            case 'S':
                if (visitedLandmarks.Add(playerCell))
                {
                    party.MaxHealth += 4;
                    party.Health = party.MaxHealth;
                    OpenDialog(
                        "SHRINE",
                        "A QUIET BLESSING FALLS.",
                        "MAX HP +4",
                        "THE PARTY IS HEALED.",
                        "ENTER CONTINUES"
                    );
                    statusLine = "The shrine grants a lasting blessing.";
                }
                else
                {
                    OpenDialog(
                        "SHRINE",
                        "THE SHRINE IS STILL.",
                        "YOU TAKE A MOMENT TO BREATHE.",
                        "NOT EVERY GIFT REPEATS.",
                        "ENTER CONTINUES"
                    );
                    statusLine = "The shrine has no new blessing today.";
                }

                break;
            case 'R':
                if (visitedLandmarks.Add(playerCell))
                {
                    var loot = random.Next(12, 29);
                    party.Gold += loot;
                    OpenDialog(
                        "RUINS",
                        "BROKEN STONE HIDES A CACHE.",
                        $"GOLD +{loot}",
                        "NOT ALL EMPIRES VANISH CLEANLY.",
                        "ENTER CONTINUES"
                    );
                    statusLine = "You found coin in the ruins.";
                }
                else
                {
                    OpenDialog(
                        "RUINS",
                        "YOU SEARCH THE BROKEN WALLS.",
                        "THE PLACE IS EMPTY NOW.",
                        "",
                        "ENTER CONTINUES"
                    );
                    statusLine = "The ruins have already yielded their secrets.";
                }

                break;
            case 'D':
                OpenDialog(
                    "DUNGEON",
                    "STONE STAIRS DESCEND INTO DARKNESS.",
                    "THE DUNGEON CRAWL IS NOT YET",
                    "IMPLEMENTED IN THIS BUILD.",
                    "ENTER CONTINUES"
                );
                statusLine = "The dungeon waits for a deeper game system.";
                break;
            default:
                statusLine = DescribeCurrentTile();
                break;
        }
    }

    private void OpenDialog(string title, params string[] lines)
    {
        panelTitle = title;
        panelLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        uiMode = UiMode.Dialog;
    }

    private string DescribeCurrentTile()
    {
        var tile = GetTileDefinition(map.Rows[playerCell.Y][playerCell.X]);
        var baseText = $"[{playerCell.X},{playerCell.Y}] {tile.Name}";
        return string.IsNullOrWhiteSpace(tile.InspectText) ? baseText : $"{baseText}: {tile.InspectText}";
    }

    private Texture LoadRgbaTexture(string relativePath, int width, int height)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var rawBytes = File.ReadAllBytes(fullPath);
        var colors = new Color[width * height];

        for (var pixelIndex = 0; pixelIndex < colors.Length; pixelIndex++)
        {
            var byteOffset = pixelIndex * 4;
            colors[pixelIndex] = new Color(
                rawBytes[byteOffset],
                rawBytes[byteOffset + 1],
                rawBytes[byteOffset + 2],
                rawBytes[byteOffset + 3]);
        }

        return Texture.New2D(GraphicsDevice, width, height, PixelFormat.R8G8B8A8_UNorm, colors);
    }

    private TileDefinition GetTileDefinition(char symbol)
    {
        if (tileDefinitions.TryGetValue(symbol, out var definition))
        {
            return definition;
        }

        return tileDefinitions['.'];
    }

    private void DrawMap()
    {
        var mapWidthPixels = map.Width * TilePixels;
        var mapHeightPixels = map.Height * TilePixels;

        DrawPanel(MapOffsetX - 8, MapOffsetY - 8, mapWidthPixels + 16, mapHeightPixels + 16, new Color(38, 50, 74, 255));
        DrawPanel(MapOffsetX - 3, MapOffsetY - 3, mapWidthPixels + 6, mapHeightPixels + 6, new Color(96, 118, 164, 255));

        for (var y = 0; y < map.Rows.Count; y++)
        {
            var row = map.Rows[y];
            for (var x = 0; x < row.Length; x++)
            {
                var symbol = row[x];
                var tile = GetTileDefinition(symbol);
                var destination = new RectangleF(
                    MapOffsetX + (x * TilePixels),
                    MapOffsetY + (y * TilePixels),
                    TilePixels,
                    TilePixels);

                var tileSource = new RectangleF(tile.SourceRegion.X, tile.SourceRegion.Y, tile.SourceRegion.Width, tile.SourceRegion.Height);
                spriteBatch.Draw(tileTexture, destination, tileSource, tile.Tint, 0, Vector2.Zero);

                if (symbol is 'T' or 'K' or 'R' or 'S' or 'H' or 'C' or 'D')
                {
                    DrawLandmarkMarker(symbol, x, y);
                }
            }
        }

        var cursorDestination = new RectangleF(
            MapOffsetX + (playerCell.X * TilePixels),
            MapOffsetY + (playerCell.Y * TilePixels),
            TilePixels,
            TilePixels);
        spriteBatch.Draw(whiteTexture, cursorDestination, new Color(255, 244, 166, 48));
        DrawFrame(cursorDestination, new Color(255, 235, 119), 2);

        var playerFrame = GetPlayerSourceFrame();
        var playerDestination = new RectangleF(
            cursorDestination.X + 2.0f,
            cursorDestination.Y + 1.0f,
            TilePixels - 4.0f,
            TilePixels - 2.0f);

        DrawPartyTrail(cursorDestination);
        spriteBatch.Draw(whiteTexture, new RectangleF(playerDestination.X + 3.0f, playerDestination.Y + playerDestination.Height - 4.0f, playerDestination.Width - 6.0f, 3.0f), new Color(0, 0, 0, 82));
        var playerSource = new RectangleF(playerFrame.X, playerFrame.Y, playerFrame.Width, playerFrame.Height);
        spriteBatch.Draw(playerTexture, playerDestination, playerSource, Color.White, 0, Vector2.Zero);
    }

    private void DrawPartyTrail(RectangleF leaderTile)
    {
        var source = GetPlayerSourceFrameFor(Direction.Down, (int)(totalTime * 8.0f) % 3);
        var sourceRect = new RectangleF(source.X, source.Y, source.Width, source.Height);
        var tints = new[]
        {
            new Color(181, 214, 255),
            new Color(255, 204, 214),
            new Color(255, 232, 174),
        };

        var spacing = facing switch
        {
            Direction.Up => new Vector2(0.0f, 7.0f),
            Direction.Down => new Vector2(0.0f, -7.0f),
            Direction.Left => new Vector2(7.0f, 0.0f),
            _ => new Vector2(-7.0f, 0.0f),
        };

        for (var i = 0; i < tints.Length; i++)
        {
            var bob = MathF.Sin((totalTime * 4.0f) + (i * 0.85f)) * 1.2f;
            var offset = spacing * (i + 1);
            var destination = new RectangleF(
                leaderTile.X + 2.0f + offset.X,
                leaderTile.Y + 1.0f + offset.Y + bob,
                TilePixels - 6.0f,
                TilePixels - 4.0f);

            spriteBatch.Draw(whiteTexture, new RectangleF(destination.X + 2.0f, destination.Y + destination.Height - 3.0f, destination.Width - 4.0f, 2.0f), new Color(0, 0, 0, 56));
            spriteBatch.Draw(playerTexture, destination, sourceRect, tints[i], 0, Vector2.Zero);
        }
    }

    private void DrawLandmarkMarker(char symbol, int x, int y)
    {
        var color = symbol switch
        {
            'T' => new Color(255, 234, 139),
            'K' => new Color(247, 242, 228),
            'R' => new Color(241, 180, 171),
            'S' => new Color(245, 189, 255),
            'H' => new Color(255, 214, 146),
            'C' => new Color(255, 163, 124),
            'D' => new Color(230, 144, 144),
            _ => Color.White,
        };

        var px = MapOffsetX + (x * TilePixels);
        var py = MapOffsetY + (y * TilePixels);
        DrawPanel(px + 15, py + 2, 7, 7, color);
        DrawFrame(new RectangleF(px + 14, py + 1, 9, 9), new Color(35, 20, 20), 1);
    }

    private Rectangle GetPlayerSourceFrame()
    {
        return GetPlayerSourceFrameFor(facing, walkFrame);
    }

    private static Rectangle GetPlayerSourceFrameFor(Direction direction, int frame)
    {
        var row = direction switch
        {
            Direction.Down => 0,
            Direction.Left => 1,
            Direction.Right => 2,
            _ => 3,
        };

        var column = frame switch
        {
            0 => 1,
            1 => 0,
            _ => 2,
        };

        return new Rectangle(column * 16, row * 16, 16, 16);
    }

    private void DrawHud()
    {
        DrawPanel(HudX, 28, HudWidth, 664, new Color(22, 28, 42, 232));
        DrawFrame(new RectangleF(HudX, 28, HudWidth, 664), new Color(108, 126, 173), 2);

        DrawText("NOTIMA", new Vector2(HudX + 22, 46), new Color(255, 234, 160), 3);
        DrawText(map.Name, new Vector2(HudX + 22, 88), new Color(188, 207, 255), 2);

        DrawText($"HP {party.Health}/{party.MaxHealth}", new Vector2(HudX + 22, 138), party.Health > (party.MaxHealth / 2) ? new Color(155, 241, 163) : new Color(255, 179, 179), 2);
        DrawText($"FOOD {party.Food}", new Vector2(HudX + 22, 168), new Color(255, 230, 155), 2);
        DrawText($"GOLD {party.Gold}", new Vector2(HudX + 22, 198), new Color(255, 219, 122), 2);
        DrawText($"LVL {party.Level}", new Vector2(HudX + 22, 228), new Color(191, 220, 255), 2);
        DrawText($"STEPS {party.Steps}", new Vector2(HudX + 22, 258), new Color(201, 212, 236), 2);

        DrawText("TILE", new Vector2(HudX + 22, 320), new Color(255, 234, 160), 2);
        DrawWrappedText(DescribeCurrentTile(), new Vector2(HudX + 22, 350), 2, HudWidth - 44, new Color(219, 230, 255));

        DrawText("STATUS", new Vector2(HudX + 22, 450), new Color(255, 234, 160), 2);
        DrawWrappedText(statusLine, new Vector2(HudX + 22, 480), 2, HudWidth - 44, new Color(230, 234, 243));

        DrawText("PARTY", new Vector2(HudX + 22, 550), new Color(255, 234, 160), 2);
        DrawPartyBanner(new Vector2(HudX + 22, 574));

        DrawText("MOVE WASD OR ARROWS", new Vector2(HudX + 22, 622), new Color(198, 220, 255), 2);
        DrawText("ENTER INTERACTS", new Vector2(HudX + 22, 648), new Color(198, 220, 255), 2);
        DrawText("R RESETS  ESC QUITS", new Vector2(HudX + 22, 674), new Color(198, 220, 255), 2);
    }

    private void DrawPanels()
    {
        if (uiMode == UiMode.Overworld)
        {
            return;
        }

        DrawPanel(PanelX, PanelY, PanelWidth, PanelHeight, new Color(15, 18, 29, 236));
        DrawFrame(new RectangleF(PanelX, PanelY, PanelWidth, PanelHeight), new Color(255, 227, 147), 2);
        DrawText(panelTitle, new Vector2(PanelX + 22, PanelY + 22), new Color(255, 236, 168), 3);

        if (uiMode == UiMode.Encounter && encounter is not null)
        {
            DrawEncounterAnimation();
        }

        var lineY = uiMode == UiMode.Encounter ? PanelY + 152 : PanelY + 72;
        foreach (var line in panelLines)
        {
            DrawWrappedText(line, new Vector2(PanelX + 22, lineY), 2, PanelWidth - 44, new Color(232, 238, 252));
            lineY += 32;
        }
    }

    private void DrawPartyBanner(Vector2 origin)
    {
        var colors = new[]
        {
            Color.White,
            new Color(181, 214, 255),
            new Color(255, 204, 214),
            new Color(255, 232, 174),
        };

        for (var i = 0; i < colors.Length; i++)
        {
            var frame = GetPlayerSourceFrameFor(i == 0 ? facing : Direction.Down, ((int)(totalTime * 6.0f) + i) % 3);
            var sourceRect = new RectangleF(frame.X, frame.Y, frame.Width, frame.Height);
            var bounce = MathF.Sin((totalTime * 5.0f) + i) * 2.0f;
            var destination = new RectangleF(origin.X + (i * 42.0f), origin.Y + bounce, 30.0f, 30.0f);
            spriteBatch.Draw(whiteTexture, new RectangleF(destination.X + 4.0f, destination.Y + destination.Height - 3.0f, destination.Width - 8.0f, 2.0f), new Color(0, 0, 0, 56));
            spriteBatch.Draw(playerTexture, destination, sourceRect, colors[i], 0, Vector2.Zero);
        }
    }

    private void DrawEncounterAnimation()
    {
        DrawPanel(PanelX + 18, PanelY + 56, PanelWidth - 36, 84, new Color(34, 39, 57, 210));
        DrawFrame(new RectangleF(PanelX + 18, PanelY + 56, PanelWidth - 36, 84), new Color(106, 128, 177), 1);

        var partyBase = new Vector2(PanelX + 34, PanelY + 80);
        var partyColors = new[]
        {
            Color.White,
            new Color(181, 214, 255),
            new Color(255, 204, 214),
            new Color(255, 232, 174),
        };

        for (var i = 0; i < partyColors.Length; i++)
        {
            var frame = GetPlayerSourceFrameFor(i == 0 ? facing : Direction.Right, ((int)(totalTime * 7.0f) + i) % 3);
            var sourceRect = new RectangleF(frame.X, frame.Y, frame.Width, frame.Height);
            var bounce = MathF.Sin((totalTime * 7.0f) + (i * 0.7f)) * 1.8f;
            var destination = new RectangleF(partyBase.X + (i * 30.0f), partyBase.Y + bounce, 26.0f, 26.0f);
            spriteBatch.Draw(playerTexture, destination, sourceRect, partyColors[i], 0, Vector2.Zero);
        }

        DrawEnemySprite(new Vector2(PanelX + 260, PanelY + 74), 4);
    }

    private void DrawEnemySprite(Vector2 origin, int scale)
    {
        if (encounter is null)
        {
            return;
        }

        var frameIndex = ((int)(totalTime * 6.0f)) % 2;
        var bob = MathF.Sin(totalTime * 5.0f) * 2.0f;
        var palette = GetEnemyPalette(encounter.Name);
        var frame = GetEnemyFrame(encounter.Name, frameIndex);

        for (var row = 0; row < frame.Length; row++)
        {
            for (var column = 0; column < frame[row].Length; column++)
            {
                var code = frame[row][column];
                if (code == '.')
                {
                    continue;
                }

                if (!palette.TryGetValue(code, out var color))
                {
                    continue;
                }

                spriteBatch.Draw(
                    whiteTexture,
                    new RectangleF(origin.X + (column * scale), origin.Y + (row * scale) + bob, scale, scale),
                    color);
            }
        }
    }

    private static Dictionary<char, Color> GetEnemyPalette(string encounterName)
    {
        return encounterName switch
        {
            "WOLF PACK" => new Dictionary<char, Color>
            {
                ['A'] = new(92, 101, 122),
                ['B'] = new(165, 179, 207),
                ['C'] = new(232, 236, 255),
                ['D'] = new(255, 118, 118),
            },
            "FEN LEECHES" => new Dictionary<char, Color>
            {
                ['A'] = new(88, 131, 84),
                ['B'] = new(140, 203, 130),
                ['C'] = new(220, 250, 186),
                ['D'] = new(255, 148, 133),
            },
            _ => new Dictionary<char, Color>
            {
                ['A'] = new(118, 69, 54),
                ['B'] = new(183, 111, 84),
                ['C'] = new(247, 205, 164),
                ['D'] = new(232, 232, 240),
            },
        };
    }

    private static string[] GetEnemyFrame(string encounterName, int frameIndex)
    {
        return encounterName switch
        {
            "WOLF PACK" => frameIndex == 0
                ? ["....AA......", "...ABBA.....", "..ABBBAA....", ".ABBCCBA....", ".ABCCCCBA...", ".ABBDDDBA...", ".AABBBBAA...", ".A..AA..A..."]
                : [".....AA.....", "....ABBA....", "...ABBBAA...", "..ABBCCBA...", ".ABCCCCBA...", ".ABBDDDBA...", ".AABBBBAA...", ".AA..AA.A..."],
            "FEN LEECHES" => frameIndex == 0
                ? ["....AA......", "..AABBAA....", ".ABBCCBBA...", ".ABCCCCBA...", ".ABCDDCBA...", ".ABBCCBBA...", "..AABBAA....", "....AA......"]
                : ["...AA.......", "..AABBA.....", ".ABBCCBBA...", ".ABCCCCCBA..", ".ABCDDDCBA..", ".ABBCCBBA...", "..AABBA.....", "...AA......."],
            _ => frameIndex == 0
                ? ["....DD......", "...DAAD.....", "..DABBAD....", ".DAABBACD...", ".DAABBBAD...", ".DABCCBAD...", ".DDAA.ADD...", "...D..D....."]
                : [".....DD.....", "....DAAD....", "...DABBAD...", "..DAABBACD..", ".DAABBBAD...", ".DABCCBAD...", ".DDAA.ADD...", "..D....D...."],
        };
    }

    private void DrawWrappedText(string text, Vector2 position, int scale, int maxWidth, Color color)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var words = text.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = string.Empty;
        var y = position.Y;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            if (MeasureTextWidth(candidate, scale) <= maxWidth)
            {
                currentLine = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                DrawText(currentLine, new Vector2(position.X, y), color, scale);
                y += (8 * scale) + 4;
            }

            currentLine = word;
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            DrawText(currentLine, new Vector2(position.X, y), color, scale);
        }
    }

    private int MeasureTextWidth(string text, int scale)
    {
        var width = 0;
        foreach (var character in text.ToUpperInvariant())
        {
            if (!glyphs.TryGetValue(character, out var pattern))
            {
                pattern = glyphs['?'];
            }

            width += (pattern[0].Length + 1) * scale;
        }

        return width;
    }

    private void DrawText(string text, Vector2 position, Color color, int scale)
    {
        var cursorX = position.X;
        foreach (var character in text.ToUpperInvariant())
        {
            if (!glyphs.TryGetValue(character, out var pattern))
            {
                pattern = glyphs['?'];
            }

            for (var row = 0; row < pattern.Length; row++)
            {
                for (var column = 0; column < pattern[row].Length; column++)
                {
                    if (pattern[row][column] != '1')
                    {
                        continue;
                    }

                    spriteBatch.Draw(
                        whiteTexture,
                        new RectangleF(cursorX + (column * scale), position.Y + (row * scale), scale, scale),
                        color);
                }
            }

            cursorX += (pattern[0].Length + 1) * scale;
        }
    }

    private void DrawPanel(float x, float y, float width, float height, Color color)
    {
        spriteBatch.Draw(whiteTexture, new RectangleF(x, y, width, height), color);
    }

    private void DrawFrame(RectangleF rect, Color color, float thickness)
    {
        DrawPanel(rect.X, rect.Y, rect.Width, thickness, color);
        DrawPanel(rect.X, rect.Bottom - thickness, rect.Width, thickness, color);
        DrawPanel(rect.X, rect.Y, thickness, rect.Height, color);
        DrawPanel(rect.Right - thickness, rect.Y, thickness, rect.Height, color);
    }

    private void UpdateWindowTitle()
    {
        var modeText = uiMode switch
        {
            UiMode.Encounter => "ENCOUNTER",
            UiMode.Dialog => panelTitle,
            _ => "OVERWORLD"
        };
        Window.Title = $"notima | {modeText} | HP {party.Health}/{party.MaxHealth} | GOLD {party.Gold} | FOOD {party.Food}";
    }
}

internal sealed record TileDefinition(string Name, Color Tint, bool Walkable, Rectangle SourceRegion, string? InspectText = null)
{
    public bool CanEncounter => Name is "Plains" or "Forest" or "Fen";

    public double EncounterChance => Name switch
    {
        "Forest" => 0.18,
        "Fen" => 0.15,
        "Plains" => 0.09,
        _ => 0.0,
    };
}

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

internal sealed class PartyState
{
    public int Level { get; set; }

    public int Health { get; set; }

    public int MaxHealth { get; set; }

    public int Gold { get; set; }

    public int Food { get; set; }

    public int Steps { get; set; }
}

internal sealed class EncounterState(string name, int maxHealth, int attackMin, int attackMax, int rewardGold, int rewardFood)
{
    public string Name { get; } = name;

    public int MaxHealth { get; } = maxHealth;

    public int Health { get; set; } = maxHealth;

    public int AttackMin { get; } = attackMin;

    public int AttackMax { get; } = attackMax;

    public int RewardGold { get; } = rewardGold;

    public int RewardFood { get; } = rewardFood;
}

internal enum UiMode
{
    Overworld,
    Encounter,
    Dialog,
}

internal enum Direction
{
    Down,
    Left,
    Right,
    Up,
}

internal readonly record struct GridPoint(int X, int Y)
{
    public static GridPoint Zero => new(0, 0);
}

internal static class BitmapFont
{
    public static Dictionary<char, string[]> Create()
    {
        return new Dictionary<char, string[]>
        {
            ['A'] = ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
            ['B'] = ["11110", "10001", "11110", "10001", "10001", "10001", "11110"],
            ['C'] = ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
            ['D'] = ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
            ['E'] = ["11111", "10000", "11110", "10000", "10000", "10000", "11111"],
            ['F'] = ["11111", "10000", "11110", "10000", "10000", "10000", "10000"],
            ['G'] = ["01111", "10000", "10000", "10111", "10001", "10001", "01111"],
            ['H'] = ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
            ['I'] = ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
            ['J'] = ["00111", "00010", "00010", "00010", "00010", "10010", "01100"],
            ['K'] = ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
            ['L'] = ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
            ['M'] = ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
            ['N'] = ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
            ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
            ['P'] = ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
            ['Q'] = ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
            ['R'] = ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
            ['S'] = ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
            ['T'] = ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
            ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
            ['V'] = ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
            ['W'] = ["10001", "10001", "10001", "10101", "10101", "11011", "10001"],
            ['X'] = ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
            ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
            ['Z'] = ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
            ['0'] = ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
            ['1'] = ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
            ['2'] = ["01110", "10001", "00001", "00010", "00100", "01000", "11111"],
            ['3'] = ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
            ['4'] = ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
            ['5'] = ["11111", "10000", "10000", "11110", "00001", "00001", "11110"],
            ['6'] = ["01110", "10000", "10000", "11110", "10001", "10001", "01110"],
            ['7'] = ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
            ['8'] = ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
            ['9'] = ["01110", "10001", "10001", "01111", "00001", "00001", "01110"],
            ['!'] = ["00100", "00100", "00100", "00100", "00100", "00000", "00100"],
            ['?'] = ["01110", "10001", "00001", "00010", "00100", "00000", "00100"],
            ['.'] = ["00000", "00000", "00000", "00000", "00000", "00110", "00110"],
            [','] = ["00000", "00000", "00000", "00000", "00000", "00110", "00100"],
            [':'] = ["00000", "00110", "00110", "00000", "00110", "00110", "00000"],
            ['-'] = ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
            ['/'] = ["00001", "00010", "00100", "01000", "10000", "00000", "00000"],
            ['['] = ["01110", "01000", "01000", "01000", "01000", "01000", "01110"],
            [']'] = ["01110", "00010", "00010", "00010", "00010", "00010", "01110"],
            ['\''] = ["00100", "00100", "00010", "00000", "00000", "00000", "00000"],
            [' '] = ["000", "000", "000", "000", "000", "000", "000"],
        };
    }
}
