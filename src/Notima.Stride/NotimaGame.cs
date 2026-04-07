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
    private const float IsoHalfWidth = 32.0f;
    private const float IsoHalfHeight = 16.0f;
    private const float IsoTileWidth = 64.0f;
    private const float IsoTileHeight = 64.0f;
    private const float IsoOriginX = 348.0f;
    private const float IsoOriginY = 74.0f;
    private const int IsoSheetPitch = 33;
    private const int IsoSheetCell = 32;
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
    private float playerAttackAnimationTime;
    private float enemyAttackAnimationTime;
    private int selectedEnemyIndex;
    private int attackingPartyMemberIndex;
    private int attackingEnemyIndex = -1;
    private int attackedPartyMemberIndex = -1;
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
        tileTexture = LoadRgbaTexture("Content/Art/notima_isometric_tiles.rgba", 32, 264);
        playerTexture = LoadRgbaTexture("Content/Art/notima_isometric_hero.rgba", 99, 528);
        LoadMapFromDisk();
        UpdateWindowTitle();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var dt = (float)gameTime.Elapsed.TotalSeconds;
        totalTime += dt;
        moveCooldown = MathF.Max(0.0f, moveCooldown - dt);
        playerAttackAnimationTime = MathF.Max(0.0f, playerAttackAnimationTime - dt);
        enemyAttackAnimationTime = MathF.Max(0.0f, enemyAttackAnimationTime - dt);

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
        if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A))
        {
            CycleEnemyTarget(-1);
            return;
        }

        if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D) || Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W) || Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
        {
            CycleEnemyTarget(1);
            return;
        }

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
            Food = 120,
            Gold = 30,
            Steps = 0
        };
        party.ResetMembers(6);
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
                DamageRandomAlivePartyMember(1, allowDefeat: false);
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
            "Forest" => EncounterState.CreateWolfPack(),
            "Fen" => EncounterState.CreateFenLeeches(),
            _ => EncounterState.CreateRoadBandits(),
        };
        selectedEnemyIndex = GetDefaultSelectedEnemy();
        attackingEnemyIndex = -1;
        attackedPartyMemberIndex = -1;
        playerAttackAnimationTime = 0.0f;
        enemyAttackAnimationTime = 0.0f;

        panelTitle = "ENCOUNTER";
        panelLines =
        [
            $"A {encounter.Name} RUSHES IN.",
            $"FOES {encounter.AliveCount}/{encounter.Enemies.Count}",
            "ARROWS PICK A TARGET",
            "ENTER ATTACKS  R RETREATS",
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

        var target = GetSelectedEnemy();
        if (target is null)
        {
            HandleEncounterVictory();
            return;
        }

        var attackerIndex = GetFrontAlivePartyIndex() ?? GetBackAlivePartyIndex() ?? 0;
        attackingPartyMemberIndex = attackerIndex;
        playerAttackAnimationTime = 0.24f;

        var playerDamage = random.Next(4, 9) + party.Level;
        if (attackerIndex >= 2)
        {
            playerDamage = Math.Max(2, playerDamage - 2);
        }

        target.Health = Math.Max(0, target.Health - playerDamage);
        if (!target.IsAlive)
        {
            selectedEnemyIndex = GetDefaultSelectedEnemy();
        }

        if (encounter.AliveCount == 0)
        {
            HandleEncounterVictory();
            return;
        }

        var enemy = encounter.Enemies.FirstOrDefault(enemyUnit => enemyUnit.IsAlive);
        attackingEnemyIndex = enemy is null ? -1 : encounter.Enemies.IndexOf(enemy);
        enemyAttackAnimationTime = enemy is null ? 0.0f : 0.24f;

        var targetPartyIndex = GetRandomAlivePartyIndex(preferFront: true) ?? 0;
        attackedPartyMemberIndex = targetPartyIndex;

        var enemyDamageBase = enemy is null ? 1 : random.Next(enemy.AttackMin, enemy.AttackMax + 1);
        var enemyDamage = Math.Max(1, enemyDamageBase - (party.Level / 2));
        if (targetPartyIndex >= 2 && GetFrontAlivePartyIndex() is not null)
        {
            enemyDamage = Math.Max(1, enemyDamage - 2);
        }

        DamagePartyMember(targetPartyIndex, enemyDamage);
        statusLine = $"You hit {target.Name} for {playerDamage}. {enemy?.Name ?? encounter.Name} answers for {enemyDamage}.";
        panelLines =
        [
            $"{target.Name} HP {target.Health}/{target.MaxHealth}",
            $"PARTY HP {party.TotalHealth}/{party.MaxTotalHealth}",
            "ARROWS PICK A TARGET",
            "ENTER ATTACKS  R RETREATS",
        ];

        if (party.TotalHealth <= 0)
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

        var enemy = encounter.Enemies.FirstOrDefault(enemyUnit => enemyUnit.IsAlive);
        var damage = Math.Max(1, (enemy is null ? 2 : random.Next(enemy.AttackMin, enemy.AttackMax + 1)) - party.Level);
        var targetPartyIndex = GetRandomAlivePartyIndex(preferFront: true) ?? 0;
        DamagePartyMember(targetPartyIndex, damage);
        attackingEnemyIndex = enemy is null ? -1 : encounter.Enemies.IndexOf(enemy);
        attackedPartyMemberIndex = targetPartyIndex;
        enemyAttackAnimationTime = 0.24f;
        statusLine = $"Retreat failed. The {encounter.Name} hits for {damage}.";
        panelLines =
        [
            $"A {encounter.Name} HOLDS YOU FAST.",
            $"PARTY HP {party.TotalHealth}/{party.MaxTotalHealth}",
            "ARROWS PICK A TARGET",
            "ENTER STRIKES BACK",
        ];

        if (party.TotalHealth <= 0)
        {
            HandleDefeat();
        }
    }

    private void HandleDefeat()
    {
        party.ResetMembers(6 + ((party.Level - 1) * 2));
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
                HealAllPartyMembers();
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
                    RaisePartyMaxHealth(2);
                    HealAllPartyMembers();
                    OpenDialog(
                        "KEEP",
                        "THE LORD'S CAPTAIN TRAINS YOU.",
                        $"LEVEL IS NOW {party.Level}.",
                        "EACH MEMBER HP +2",
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
                    RaisePartyMaxHealth(1);
                    HealAllPartyMembers();
                    OpenDialog(
                        "SHRINE",
                        "A QUIET BLESSING FALLS.",
                        "EACH MEMBER HP +1",
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
        DrawPanel(28, 28, 692, 664, new Color(26, 35, 49, 240));
        DrawFrame(new RectangleF(28, 28, 692, 664), new Color(102, 124, 171), 2);

        for (var y = 0; y < map.Rows.Count; y++)
        {
            var row = map.Rows[y];
            for (var x = 0; x < row.Length; x++)
            {
                var symbol = row[x];
                var destination = GetIsoTileDestination(x, y);
                var tileSource = GetIsoTileSource(symbol);
                spriteBatch.Draw(tileTexture, destination, tileSource, Color.White, 0, Vector2.Zero);

                if (symbol is 'T' or 'K' or 'R' or 'S' or 'H' or 'C' or 'D')
                {
                    DrawLandmarkMarker(symbol, destination);
                }
            }
        }

        var cursorDestination = GetIsoHighlightDestination(playerCell.X, playerCell.Y);
        spriteBatch.Draw(whiteTexture, cursorDestination, new Color(255, 244, 166, 34));
        DrawFrame(cursorDestination, new Color(255, 235, 119), 2);

        var playerFrame = GetPlayerSourceFrame(0);
        var playerDestination = GetIsoCharacterDestination(playerCell.X, playerCell.Y, 0.0f);

        DrawPartyTrail(cursorDestination);
        spriteBatch.Draw(whiteTexture, new RectangleF(playerDestination.X + 10.0f, playerDestination.Y + playerDestination.Height - 6.0f, playerDestination.Width - 20.0f, 4.0f), new Color(0, 0, 0, 82));
        var playerSource = new RectangleF(playerFrame.X, playerFrame.Y, playerFrame.Width, playerFrame.Height);
        spriteBatch.Draw(playerTexture, playerDestination, playerSource, Color.White, 0, Vector2.Zero);
    }

    private void DrawPartyTrail(RectangleF leaderTile)
    {
        var source = GetPlayerSourceFrameFor(Direction.Down, (int)(totalTime * 8.0f) % 3, 1);
        var sourceRect = new RectangleF(source.X, source.Y, source.Width, source.Height);
        var tints = new[]
        {
            new Color(181, 214, 255),
            new Color(255, 204, 214),
            new Color(255, 232, 174),
        };

        var spacing = facing switch
        {
            Direction.Up => new Vector2(IsoHalfWidth * 0.33f, IsoHalfHeight * 0.33f),
            Direction.Down => new Vector2(-IsoHalfWidth * 0.33f, -IsoHalfHeight * 0.33f),
            Direction.Left => new Vector2(IsoHalfWidth * 0.33f, -IsoHalfHeight * 0.33f),
            _ => new Vector2(-IsoHalfWidth * 0.33f, IsoHalfHeight * 0.33f),
        };

        for (var i = 0; i < tints.Length; i++)
        {
            var bob = MathF.Sin((totalTime * 4.0f) + (i * 0.85f)) * 2.0f;
            var offset = spacing * (i + 1);
            var destination = new RectangleF(
                leaderTile.Center.X - 22.0f + offset.X,
                leaderTile.Y - 28.0f + offset.Y + bob,
                44.0f,
                44.0f);

            spriteBatch.Draw(whiteTexture, new RectangleF(destination.X + 10.0f, destination.Y + destination.Height - 5.0f, destination.Width - 20.0f, 3.0f), new Color(0, 0, 0, 56));
            spriteBatch.Draw(playerTexture, destination, sourceRect, tints[i], 0, Vector2.Zero);
        }
    }

    private void DrawLandmarkMarker(char symbol, RectangleF tileDestination)
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

        DrawPanel(tileDestination.Center.X - 5.0f, tileDestination.Y + 20.0f, 10.0f, 10.0f, color);
        DrawFrame(new RectangleF(tileDestination.Center.X - 6.0f, tileDestination.Y + 19.0f, 12.0f, 12.0f), new Color(35, 20, 20), 1);
    }

    private static Rectangle GetPlayerSourceFrameFor(Direction direction, int frame, int roleIndex)
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

        var x = column * IsoSheetPitch;
        var roleBlock = Math.Clamp(roleIndex, 0, 3) * 4;
        var y = (roleBlock + row) * IsoSheetPitch;
        return new Rectangle(x, y, IsoSheetCell, IsoSheetCell);
    }

    private Rectangle GetPlayerSourceFrame(int roleIndex)
    {
        return GetPlayerSourceFrameFor(facing, walkFrame, roleIndex);
    }

    private void DrawHud()
    {
        DrawPanel(HudX, 28, HudWidth, 664, new Color(22, 28, 42, 232));
        DrawFrame(new RectangleF(HudX, 28, HudWidth, 664), new Color(108, 126, 173), 2);

        DrawText("NOTIMA", new Vector2(HudX + 22, 46), new Color(255, 234, 160), 3);
        DrawText(map.Name, new Vector2(HudX + 22, 88), new Color(188, 207, 255), 2);

        DrawText($"HP {party.TotalHealth}/{party.MaxTotalHealth}", new Vector2(HudX + 22, 138), party.TotalHealth > (party.MaxTotalHealth / 2) ? new Color(155, 241, 163) : new Color(255, 179, 179), 2);
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

        var lineY = uiMode == UiMode.Encounter ? PanelY + 214 : PanelY + 72;
        foreach (var line in panelLines)
        {
            DrawWrappedText(line, new Vector2(PanelX + 22, lineY), 2, PanelWidth - 44, new Color(232, 238, 252));
            lineY += 32;
        }
    }

    private void DrawPartyBanner(Vector2 origin)
    {
        for (var i = 0; i < party.Members.Count; i++)
        {
            var member = party.Members[i];
            var frame = GetPlayerSourceFrameFor(i == 0 ? facing : Direction.Down, ((int)(totalTime * 6.0f) + i) % 3, i);
            var sourceRect = new RectangleF(frame.X, frame.Y, frame.Width, frame.Height);
            var bounce = MathF.Sin((totalTime * 5.0f) + i) * 2.0f;
            var destination = new RectangleF(origin.X + (i * 42.0f), origin.Y + bounce, 30.0f, 30.0f);
            spriteBatch.Draw(whiteTexture, new RectangleF(destination.X + 4.0f, destination.Y + destination.Height - 3.0f, destination.Width - 8.0f, 2.0f), new Color(0, 0, 0, 56));
            var tint = member.IsAlive ? member.Tint : new Color(84, 92, 112);
            spriteBatch.Draw(playerTexture, destination, sourceRect, tint, 0, Vector2.Zero);
            DrawText($"{member.Health}/{member.MaxHealth}", new Vector2(destination.X - 2.0f, destination.Bottom + 6.0f), member.IsAlive ? new Color(220, 230, 255) : new Color(130, 136, 148), 1);
        }
    }

    private void DrawEncounterAnimation()
    {
        var boardRect = new RectangleF(PanelX + 18, PanelY + 56, PanelWidth - 36, 144);
        DrawPanel(boardRect.X, boardRect.Y, boardRect.Width, boardRect.Height, new Color(28, 36, 50, 220));
        DrawFrame(boardRect, new Color(106, 128, 177), 1);

        var boardOrigin = new Vector2(boardRect.X + (boardRect.Width * 0.5f) - 34.0f, boardRect.Y + 22.0f);
        for (var boardY = 0; boardY < 3; boardY++)
        {
            for (var boardX = 0; boardX < 4; boardX++)
            {
                var tile = GetEncounterTileDestination(boardOrigin, boardX, boardY);
                spriteBatch.Draw(tileTexture, tile, GetIsoTileSource('.'), Color.White, 0, Vector2.Zero);
            }
        }

        for (var i = 0; i < party.Members.Count; i++)
        {
            var member = party.Members[i];
            var frame = GetPlayerSourceFrameFor(i == 0 ? facing : Direction.Right, ((int)(totalTime * 7.0f) + i) % 3, i);
            var sourceRect = new RectangleF(frame.X, frame.Y, frame.Width, frame.Height);
            var bounce = MathF.Sin((totalTime * 7.0f) + (i * 0.7f)) * 2.4f;
            var tile = GetEncounterTileDestination(boardOrigin, i % 2, i / 2);
            var offset = GetPartyAttackOffset(i);
            var destination = new RectangleF(tile.X + 12.0f + offset.X, tile.Y - 4.0f + bounce + offset.Y, 34.0f, 34.0f);
            var shadowColor = member.IsAlive ? new Color(0, 0, 0, 64) : new Color(0, 0, 0, 24);
            spriteBatch.Draw(whiteTexture, new RectangleF(destination.X + 8.0f, destination.Y + destination.Height - 5.0f, destination.Width - 16.0f, 3.0f), shadowColor);
            var tint = member.IsAlive ? member.Tint : new Color(78, 84, 98);
            spriteBatch.Draw(playerTexture, destination, sourceRect, tint, 0, Vector2.Zero);
            if (!member.IsAlive)
            {
                spriteBatch.Draw(whiteTexture, new RectangleF(destination.X + 6.0f, destination.Y + 16.0f, destination.Width - 12.0f, 2.0f), new Color(176, 82, 82));
            }
        }

        DrawEnemyBoardPresence(boardOrigin);
    }

    private void DrawEnemyBoardPresence(Vector2 boardOrigin)
    {
        if (encounter is null)
        {
            return;
        }

        for (var i = 0; i < encounter.Enemies.Count; i++)
        {
            var enemy = encounter.Enemies[i];
            if (!enemy.IsAlive)
            {
                continue;
            }

            var enemyTile = GetEncounterTileDestination(boardOrigin, enemy.BoardX, enemy.BoardY);
            spriteBatch.Draw(tileTexture, enemyTile, GetIsoTileSource(encounter.Name == "FEN LEECHES" ? 'F' : '*'), Color.White, 0, Vector2.Zero);

            if (i == selectedEnemyIndex && IsEnemyTargetable(i))
            {
                var highlight = new RectangleF(enemyTile.X + 8.0f, enemyTile.Y + 14.0f, enemyTile.Width - 16.0f, enemyTile.Height - 28.0f);
                spriteBatch.Draw(whiteTexture, highlight, new Color(255, 212, 114, 42));
                DrawFrame(highlight, new Color(255, 221, 132), 1);
            }

            var offset = GetEnemyAttackOffset(i);
            DrawEnemySprite(
                new Vector2(enemyTile.X + 6.0f + offset.X, enemyTile.Y - 10.0f + offset.Y),
                i == selectedEnemyIndex ? 4 : 3,
                enemy.Name);
        }
    }

    private static RectangleF GetEncounterTileDestination(Vector2 origin, int x, int y)
    {
        var screenX = origin.X + ((x - y) * (IsoHalfWidth * 0.68f));
        var screenY = origin.Y + ((x + y) * (IsoHalfHeight * 0.68f));
        return new RectangleF(screenX, screenY, IsoTileWidth * 0.68f, IsoTileHeight * 0.68f);
    }

    private void DrawEnemySprite(Vector2 origin, int scale, string enemyName)
    {
        var frameIndex = ((int)(totalTime * 6.0f)) % 2;
        var bob = MathF.Sin(totalTime * 5.0f) * 2.0f;
        var palette = GetEnemyPalette(enemyName);
        var frame = GetEnemyFrame(enemyName, frameIndex);

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

    private static Dictionary<char, Color> GetEnemyPalette(string enemyName)
    {
        return enemyName switch
        {
            "WOLF" => new Dictionary<char, Color>
            {
                ['A'] = new(92, 101, 122),
                ['B'] = new(165, 179, 207),
                ['C'] = new(232, 236, 255),
                ['D'] = new(255, 118, 118),
            },
            "LEECH" => new Dictionary<char, Color>
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

    private static string[] GetEnemyFrame(string enemyName, int frameIndex)
    {
        return enemyName switch
        {
            "WOLF" => frameIndex == 0
                ? ["....AA......", "...ABBA.....", "..ABBBAA....", ".ABBCCBA....", ".ABCCCCBA...", ".ABBDDDBA...", ".AABBBBAA...", ".A..AA..A..."]
                : [".....AA.....", "....ABBA....", "...ABBBAA...", "..ABBCCBA...", ".ABCCCCBA...", ".ABBDDDBA...", ".AABBBBAA...", ".AA..AA.A..."],
            "LEECH" => frameIndex == 0
                ? ["....AA......", "..AABBAA....", ".ABBCCBBA...", ".ABCCCCBA...", ".ABCDDCBA...", ".ABBCCBBA...", "..AABBAA....", "....AA......"]
                : ["...AA.......", "..AABBA.....", ".ABBCCBBA...", ".ABCCCCCBA..", ".ABCDDDCBA..", ".ABBCCBBA...", "..AABBA.....", "...AA......."],
            _ => frameIndex == 0
                ? ["....DD......", "...DAAD.....", "..DABBAD....", ".DAABBACD...", ".DAABBBAD...", ".DABCCBAD...", ".DDAA.ADD...", "...D..D....."]
                : [".....DD.....", "....DAAD....", "...DABBAD...", "..DAABBACD..", ".DAABBBAD...", ".DABCCBAD...", ".DDAA.ADD...", "..D....D...."],
        };
    }

    private void CycleEnemyTarget(int direction)
    {
        if (encounter is null)
        {
            return;
        }

        var legal = encounter.Enemies
            .Select((enemy, index) => (enemy, index))
            .Where(pair => pair.enemy.IsAlive && IsEnemyTargetable(pair.index))
            .Select(pair => pair.index)
            .ToList();

        if (legal.Count == 0)
        {
            return;
        }

        var current = legal.IndexOf(selectedEnemyIndex);
        if (current < 0)
        {
            selectedEnemyIndex = legal[0];
        }
        else
        {
            current = (current + direction + legal.Count) % legal.Count;
            selectedEnemyIndex = legal[current];
        }

        statusLine = $"Targeting {encounter.Enemies[selectedEnemyIndex].Name}.";
    }

    private int GetDefaultSelectedEnemy()
    {
        if (encounter is null)
        {
            return 0;
        }

        for (var i = 0; i < encounter.Enemies.Count; i++)
        {
            if (encounter.Enemies[i].IsAlive && IsEnemyTargetable(i))
            {
                return i;
            }
        }

        return encounter.Enemies.FindIndex(enemy => enemy.IsAlive);
    }

    private EnemyUnit? GetSelectedEnemy()
    {
        if (encounter is null || selectedEnemyIndex < 0 || selectedEnemyIndex >= encounter.Enemies.Count)
        {
            return null;
        }

        var enemy = encounter.Enemies[selectedEnemyIndex];
        if (!enemy.IsAlive)
        {
            return null;
        }

        return enemy;
    }

    private bool IsEnemyTargetable(int enemyIndex)
    {
        if (encounter is null || enemyIndex < 0 || enemyIndex >= encounter.Enemies.Count)
        {
            return false;
        }

        var enemy = encounter.Enemies[enemyIndex];
        if (!enemy.IsAlive)
        {
            return false;
        }

        var frontAlive = encounter.Enemies.Any(unit => unit.IsAlive && unit.BoardX >= 3);
        return !frontAlive || enemy.BoardX >= 3;
    }

    private int? GetFrontAlivePartyIndex()
    {
        for (var i = 0; i < Math.Min(2, party.Members.Count); i++)
        {
            if (party.Members[i].IsAlive)
            {
                return i;
            }
        }

        return null;
    }

    private int? GetBackAlivePartyIndex()
    {
        for (var i = 2; i < party.Members.Count; i++)
        {
            if (party.Members[i].IsAlive)
            {
                return i;
            }
        }

        return null;
    }

    private int? GetRandomAlivePartyIndex(bool preferFront)
    {
        var front = party.Members
            .Select((member, index) => (member, index))
            .Where(pair => pair.index < 2 && pair.member.IsAlive)
            .Select(pair => pair.index)
            .ToList();
        var back = party.Members
            .Select((member, index) => (member, index))
            .Where(pair => pair.index >= 2 && pair.member.IsAlive)
            .Select(pair => pair.index)
            .ToList();

        var preferred = preferFront && front.Count > 0 ? front : back.Count > 0 ? back : front;
        if (preferred.Count == 0)
        {
            return null;
        }

        return preferred[random.Next(preferred.Count)];
    }

    private Vector2 GetPartyAttackOffset(int partyIndex)
    {
        if (playerAttackAnimationTime <= 0.0f || partyIndex != attackingPartyMemberIndex)
        {
            if (enemyAttackAnimationTime > 0.0f && partyIndex == attackedPartyMemberIndex)
            {
                var t = enemyAttackAnimationTime / 0.24f;
                return new Vector2(-10.0f * t, -4.0f * t);
            }

            return Vector2.Zero;
        }

        var progress = 1.0f - (playerAttackAnimationTime / 0.24f);
        var amount = progress < 0.5f ? progress * 2.0f : (1.0f - progress) * 2.0f;
        return new Vector2(14.0f * amount, -6.0f * amount);
    }

    private Vector2 GetEnemyAttackOffset(int enemyIndex)
    {
        if (enemyAttackAnimationTime <= 0.0f || enemyIndex != attackingEnemyIndex)
        {
            return Vector2.Zero;
        }

        var progress = 1.0f - (enemyAttackAnimationTime / 0.24f);
        var amount = progress < 0.5f ? progress * 2.0f : (1.0f - progress) * 2.0f;
        return new Vector2(-14.0f * amount, 6.0f * amount);
    }

    private void HandleEncounterVictory()
    {
        if (encounter is null)
        {
            return;
        }

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
        selectedEnemyIndex = 0;
        playerAttackAnimationTime = 0.0f;
        enemyAttackAnimationTime = 0.0f;
    }

    private void DamageRandomAlivePartyMember(int damage, bool allowDefeat)
    {
        var targetIndex = GetRandomAlivePartyIndex(preferFront: false);
        if (targetIndex is null)
        {
            return;
        }

        var member = party.Members[targetIndex.Value];
        var floor = allowDefeat ? 0 : 1;
        member.Health = Math.Max(floor, member.Health - damage);
    }

    private void DamagePartyMember(int memberIndex, int damage)
    {
        if (memberIndex < 0 || memberIndex >= party.Members.Count)
        {
            return;
        }

        var member = party.Members[memberIndex];
        if (!member.IsAlive)
        {
            return;
        }

        member.Health = Math.Max(0, member.Health - damage);
    }

    private void HealAllPartyMembers()
    {
        foreach (var member in party.Members)
        {
            member.Health = member.MaxHealth;
        }
    }

    private void RaisePartyMaxHealth(int amount)
    {
        foreach (var member in party.Members)
        {
            member.MaxHealth += amount;
        }
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
        Window.Title = $"notima | {modeText} | HP {party.TotalHealth}/{party.MaxTotalHealth} | GOLD {party.Gold} | FOOD {party.Food}";
    }

    private static RectangleF GetIsoTileSource(char symbol)
    {
        return symbol switch
        {
            '~' => new RectangleF(0, 7 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            '^' => new RectangleF(0, 2 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            '*' => new RectangleF(0, 0, IsoSheetCell, IsoSheetCell),
            'F' => new RectangleF(0, 4 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            '=' => new RectangleF(0, 6 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'T' or 'K' or 'R' or 'S' or 'H' or 'C' or 'D' => new RectangleF(0, 3 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            _ => new RectangleF(0, 3 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
        };
    }

    private static RectangleF GetIsoTileDestination(int x, int y)
    {
        var screenX = IsoOriginX + ((x - y) * IsoHalfWidth);
        var screenY = IsoOriginY + ((x + y) * IsoHalfHeight);
        return new RectangleF(screenX, screenY, IsoTileWidth, IsoTileHeight);
    }

    private static RectangleF GetIsoHighlightDestination(int x, int y)
    {
        var tile = GetIsoTileDestination(x, y);
        return new RectangleF(tile.X + 8.0f, tile.Y + 16.0f, IsoTileWidth - 16.0f, IsoHalfHeight + 2.0f);
    }

    private static RectangleF GetIsoCharacterDestination(int x, int y, float bob)
    {
        var tile = GetIsoTileDestination(x, y);
        return new RectangleF(tile.X + 14.0f, tile.Y - 2.0f + bob, 40.0f, 40.0f);
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

    public int Gold { get; set; }

    public int Food { get; set; }

    public int Steps { get; set; }

    public List<PartyMember> Members { get; } = [];

    public int TotalHealth => Members.Sum(member => member.Health);

    public int MaxTotalHealth => Members.Sum(member => member.MaxHealth);

    public void ResetMembers(int memberHealth)
    {
        if (Members.Count == 0)
        {
            Members.Add(new PartyMember("AVA", new Color(255, 255, 255), memberHealth));
            Members.Add(new PartyMember("BRI", new Color(181, 214, 255), memberHealth));
            Members.Add(new PartyMember("CYR", new Color(255, 204, 214), memberHealth));
            Members.Add(new PartyMember("DAS", new Color(255, 232, 174), memberHealth));
            return;
        }

        foreach (var member in Members)
        {
            member.MaxHealth = memberHealth;
            member.Health = memberHealth;
        }
    }
}

internal sealed class PartyMember(string name, Color tint, int maxHealth)
{
    public string Name { get; } = name;

    public Color Tint { get; } = tint;

    public int MaxHealth { get; set; } = maxHealth;

    public int Health { get; set; } = maxHealth;

    public bool IsAlive => Health > 0;
}

internal sealed class EncounterState
{
    public string Name { get; init; } = string.Empty;

    public List<EnemyUnit> Enemies { get; init; } = [];

    public int RewardGold { get; init; }

    public int RewardFood { get; init; }

    public int AliveCount => Enemies.Count(enemy => enemy.IsAlive);

    public static EncounterState CreateWolfPack()
    {
        return new EncounterState
        {
            Name = "WOLF PACK",
            RewardGold = 11,
            RewardFood = 4,
            Enemies =
            [
                new EnemyUnit("WOLF", 8, 3, 6, 3, 0),
                new EnemyUnit("WOLF", 8, 3, 6, 3, 1),
                new EnemyUnit("WOLF", 7, 2, 5, 2, 0),
            ],
        };
    }

    public static EncounterState CreateFenLeeches()
    {
        return new EncounterState
        {
            Name = "FEN LEECHES",
            RewardGold = 9,
            RewardFood = 6,
            Enemies =
            [
                new EnemyUnit("LEECH", 7, 2, 5, 3, 0),
                new EnemyUnit("LEECH", 7, 2, 5, 3, 1),
                new EnemyUnit("LEECH", 6, 2, 4, 2, 1),
            ],
        };
    }

    public static EncounterState CreateRoadBandits()
    {
        return new EncounterState
        {
            Name = "ROAD BANDITS",
            RewardGold = 12,
            RewardFood = 3,
            Enemies =
            [
                new EnemyUnit("BANDIT", 9, 2, 5, 3, 0),
                new EnemyUnit("BANDIT", 9, 2, 5, 3, 1),
                new EnemyUnit("BANDIT", 7, 2, 4, 2, 2),
            ],
        };
    }
}

internal sealed class EnemyUnit(string name, int maxHealth, int attackMin, int attackMax, int boardX, int boardY)
{
    public string Name { get; } = name;

    public int MaxHealth { get; } = maxHealth;

    public int Health { get; set; } = maxHealth;

    public int AttackMin { get; } = attackMin;

    public int AttackMax { get; } = attackMax;

    public int BoardX { get; } = boardX;

    public int BoardY { get; } = boardY;

    public bool IsAlive => Health > 0;
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
