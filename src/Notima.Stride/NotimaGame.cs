using System.Text.Json;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;

namespace Notima.Stride;

public sealed class NotimaGame : Game
{
    private const bool ShowProjectionDebug = true;
    private const float BaseWidth = 1280.0f;
    private const float BaseHeight = 720.0f;
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
    private const int EncounterPanelHeight = 316;
    private static readonly Dictionary<string, EquipmentDefinition> EquipmentCatalog = new()
    {
        ["club"] = new("club", "CLUB", EquipmentSlot.Weapon, 1, 0, 8, 0, 1),
        ["dagger"] = new("dagger", "DAGGER", EquipmentSlot.Weapon, 2, 0, 12, 0, 1),
        ["mace"] = new("mace", "MACE", EquipmentSlot.Weapon, 3, 0, 18, 1, 2),
        ["spear"] = new("spear", "SPEAR", EquipmentSlot.Weapon, 4, 0, 26, 1, 3),
        ["shortsword"] = new("shortsword", "SHORTSWORD", EquipmentSlot.Weapon, 5, 0, 38, 2, 4),
        ["longsword"] = new("longsword", "LONGSWORD", EquipmentSlot.Weapon, 6, 0, 54, 3, 5),
        ["battleaxe"] = new("battleaxe", "BATTLEAXE", EquipmentSlot.Weapon, 7, 0, 76, 4, 6),
        ["padded"] = new("padded", "PADDED ARMOR", EquipmentSlot.Armor, 0, 1, 10, 0, 1),
        ["leather"] = new("leather", "LEATHER ARMOR", EquipmentSlot.Armor, 0, 2, 16, 0, 1),
        ["hide"] = new("hide", "HIDE ARMOR", EquipmentSlot.Armor, 0, 3, 24, 1, 2),
        ["chain-shirt"] = new("chain-shirt", "CHAIN SHIRT", EquipmentSlot.Armor, 0, 4, 34, 2, 3),
        ["scale-mail"] = new("scale-mail", "SCALE MAIL", EquipmentSlot.Armor, 0, 5, 48, 3, 4),
        ["chain-mail"] = new("chain-mail", "CHAIN MAIL", EquipmentSlot.Armor, 0, 6, 68, 4, 5),
    };
    private static readonly string[] WeaponProgression = ["club", "dagger", "mace", "spear", "shortsword", "longsword", "battleaxe"];
    private static readonly string[] ArmorProgression = ["padded", "leather", "hide", "chain-shirt", "scale-mail", "chain-mail"];

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
        ['#'] = new("Wall", new Color(80, 80, 80), false, new Rectangle(64, 32, 16, 16), "Wet stone and old mortar block the way."),
        ['<'] = new("Stairs Up", new Color(160, 160, 160), true, new Rectangle(0, 64, 16, 16), "Stairs leading back to the surface."),
        ['>'] = new("Stairs Down", new Color(160, 160, 160), true, new Rectangle(0, 64, 16, 16), "Stairs descending deeper underground."),
        ['G'] = new("Chest", new Color(180, 150, 90), true, new Rectangle(32, 48, 16, 16), "A heavy chest sits unopened."),
        ['M'] = new("Threat", new Color(160, 90, 90), true, new Rectangle(32, 0, 16, 16), "Something living moves in the dark."),
        ['L'] = new("Fountain", new Color(140, 170, 190), true, new Rectangle(48, 48, 16, 16), "A black fountain glimmers with cold water."),
        ['k'] = new("Key", new Color(190, 170, 110), true, new Rectangle(32, 48, 16, 16), "A brass key lies in the dust."),
        ['x'] = new("Locked Gate", new Color(110, 98, 82), false, new Rectangle(16, 48, 16, 16), "A locked iron gate blocks the way."),
        ['B'] = new("Boss", new Color(170, 90, 90), true, new Rectangle(16, 48, 16, 16), "A powerful enemy waits here."),
    };

    private readonly Dictionary<char, string[]> glyphs = BitmapFont.Create();
    private readonly HashSet<GridPoint> visitedLandmarks = [];
    private readonly Random random = new();

    private SpriteBatch spriteBatch = null!;
    private Texture whiteTexture = null!;
    private Texture playerTexture = null!;
    private Texture tileTexture = null!;
    private Texture enemyTexture = null!;
    private Texture grimWallTexture = null!;
    private Texture grimFloorTexture = null!;
    private Texture grimCeilingTexture = null!;
    private Texture grimPortraitTexture = null!;
    private Texture grimEnemyPortraitTexture = null!;
    private Texture grimCreatureTexture = null!;
    private SimpleAudioPlayer? audioPlayer;
    private OverworldMap map = null!;
    private GridPoint playerCell;
    private DungeonState? dungeon;
    private GridPoint dungeonCell;
    private Direction facing = Direction.Down;
    private UiMode uiMode;
    private PartyState party = new();
    private EncounterState? encounter;
    private TownMenuState? townMenu;
    private string panelTitle = string.Empty;
    private List<string> panelLines = [];
    private string statusLine = "Find the road east and the old dungeon south.";
    private float moveCooldown;
    private float totalTime;
    private float playerAttackAnimationTime;
    private float enemyAttackAnimationTime;
    private float defeatPortalTimer;
    private int selectedEnemyIndex;
    private int attackingPartyMemberIndex;
    private int attackingEnemyIndex = -1;
    private int attackedPartyMemberIndex = -1;
    private int walkFrame;
    private bool encounterFromDungeon;
    private bool encounterIsBoss;
    private CombatAction selectedCombatAction = CombatAction.Attack;
    private SpellKind selectedSpell = SpellKind.Ember;
    private List<CombatTurnEntry> encounterTurnOrder = [];
    private int encounterTurnCursor;
    private float uiScale = 1.0f;
    private float layoutOffsetX;
    private float layoutOffsetY;

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
        tileTexture = LoadRgbaTexture("Content/Art/notima_isometric_tiles.rgba", 32, 495);
        playerTexture = LoadRgbaTexture("Content/Art/notima_isometric_hero.rgba", 99, 528);
        enemyTexture = LoadRgbaTexture("Content/Art/notima_enemy_sheet.rgba", 50, 75);
        grimWallTexture = LoadRgbaTexture("Content/Art/notima_grim_wall.rgba", 256, 256);
        grimFloorTexture = LoadRgbaTexture("Content/Art/notima_grim_floor.rgba", 256, 256);
        grimCeilingTexture = LoadRgbaTexture("Content/Art/notima_grim_ceiling.rgba", 256, 256);
        grimPortraitTexture = LoadRgbaTexture("Content/Art/notima_grim_portraits.rgba", 512, 128);
        grimEnemyPortraitTexture = LoadRgbaTexture("Content/Art/notima_grim_enemy_portraits.rgba", 384, 128);
        grimCreatureTexture = LoadRgbaTexture("Content/Art/notima_grim_creatures.rgba", 256, 384);
        audioPlayer = new SimpleAudioPlayer();
        LoadMapFromDisk();
        UpdateWindowTitle();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var dt = (float)gameTime.Elapsed.TotalSeconds;
        UpdateLayout();
        totalTime += dt;
        moveCooldown = MathF.Max(0.0f, moveCooldown - dt);
        playerAttackAnimationTime = MathF.Max(0.0f, playerAttackAnimationTime - dt);
        enemyAttackAnimationTime = MathF.Max(0.0f, enemyAttackAnimationTime - dt);
        if (defeatPortalTimer > 0.0f)
        {
            defeatPortalTimer = MathF.Max(0.0f, defeatPortalTimer - dt);
            if (defeatPortalTimer <= 0.0f)
            {
                uiMode = UiMode.Overworld;
                panelTitle = string.Empty;
                panelLines.Clear();
            }
            UpdateWindowTitle();
            return;
        }

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
        DrawMinimapOverlay();
        DrawPanels();
        if (defeatPortalTimer > 0.0f)
        {
            DrawDefeatPortalOverlay();
        }
        spriteBatch.End();
    }

    protected override void Destroy()
    {
        tileTexture?.Dispose();
        playerTexture?.Dispose();
        enemyTexture?.Dispose();
        grimWallTexture?.Dispose();
        grimFloorTexture?.Dispose();
        grimCeilingTexture?.Dispose();
        grimPortraitTexture?.Dispose();
        grimEnemyPortraitTexture?.Dispose();
        grimCreatureTexture?.Dispose();
        audioPlayer?.Dispose();
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

        if (Input.IsKeyPressed(Keys.F5))
        {
            ClearPendingMove();
            SaveGame();
            return;
        }

        if (Input.IsKeyPressed(Keys.F9))
        {
            ClearPendingMove();
            LoadGame();
            return;
        }

        if (Input.IsKeyPressed(Keys.F10))
        {
            ClearPendingMove();
            EnterDungeon();
            return;
        }

        switch (uiMode)
        {
            case UiMode.Town:
                HandleTownInput();
                return;
            case UiMode.Dungeon:
                HandleDungeonInput();
                return;
            case UiMode.Encounter:
                HandleEncounterInput();
                return;
            case UiMode.Dialog:
                HandleDialogInput();
                return;
        }

        if (Input.IsKeyPressed(Keys.R))
        {
            ClearPendingMove();
            ResetOverworld();
            return;
        }

        if (Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
            ClearPendingMove();
            InteractWithCurrentTile();
            return;
        }

        if (moveCooldown > 0.0f)
        {
            return;
        }

        if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A))
        {
            ClearPendingMove();
            TurnLeft();
            moveCooldown = MoveRepeatDelay;
            statusLine = $"Facing {facing.ToString().ToLowerInvariant()}.";
            return;
        }

        if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D))
        {
            ClearPendingMove();
            TurnRight();
            moveCooldown = MoveRepeatDelay;
            statusLine = $"Facing {facing.ToString().ToLowerInvariant()}.";
            return;
        }

        if (Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W))
        {
            ClearPendingMove();
            moveCooldown = MoveRepeatDelay;
            TryMove(GetForwardDelta());
            return;
        }

        if (Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
        {
            ClearPendingMove();
            moveCooldown = MoveRepeatDelay;
            TryMove(GetBackwardDelta());
        }
    }

    private void HandleEncounterInput()
    {
        var partyTurn = encounterTurnOrder.Count == 0
            || encounterTurnOrder[Math.Clamp(encounterTurnCursor, 0, Math.Max(0, encounterTurnOrder.Count - 1))].IsParty;

        if (Input.IsKeyPressed(Keys.Q))
        {
            if (!partyTurn)
            {
                statusLine = "The enemy is acting.";
                return;
            }
            selectedCombatAction = selectedCombatAction == CombatAction.Attack ? CombatAction.Spell : CombatAction.Attack;
            statusLine = selectedCombatAction == CombatAction.Attack ? "Action set to attack." : $"Action set to spell: {GetSpellName(selectedSpell)}.";
            RefreshEncounterPanel();
            return;
        }

        if (Input.IsKeyPressed(Keys.E) && selectedCombatAction == CombatAction.Spell)
        {
            if (!partyTurn)
            {
                statusLine = "The enemy is acting.";
                return;
            }
            selectedSpell = selectedSpell switch
            {
                SpellKind.Ember => SpellKind.Mend,
                SpellKind.Mend => SpellKind.Aegis,
                _ => SpellKind.Ember,
            };
            statusLine = $"Spell set to {GetSpellName(selectedSpell)}.";
            RefreshEncounterPanel();
            return;
        }

        if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A))
        {
            if (!partyTurn)
            {
                statusLine = "Wait for your next turn.";
                return;
            }
            CycleEnemyTarget(-1);
            return;
        }

        if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D) || Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W) || Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
        {
            if (!partyTurn)
            {
                statusLine = "Wait for your next turn.";
                return;
            }
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

    private void HandleTownInput()
    {
        if (townMenu is null)
        {
            uiMode = dungeon is null ? UiMode.Overworld : UiMode.Dungeon;
            return;
        }

        if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A) || Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W))
        {
            townMenu.SelectedIndex = (townMenu.SelectedIndex - 1 + townMenu.Options.Count) % townMenu.Options.Count;
            RefreshTownPanel();
            return;
        }

        if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D) || Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
        {
            townMenu.SelectedIndex = (townMenu.SelectedIndex + 1) % townMenu.Options.Count;
            RefreshTownPanel();
            return;
        }

        if (Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
            ExecuteTownOption();
            return;
        }

        if (Input.IsKeyPressed(Keys.R) || Input.IsKeyPressed(Keys.Escape))
        {
            CloseTownMenu("You step back onto the road.");
        }
    }

    private void HandleDungeonInput()
    {
        if (dungeon is null)
        {
            uiMode = UiMode.Overworld;
            return;
        }

        if (Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
            ClearPendingMove();
            InteractWithDungeonTile();
            return;
        }

        if (Input.IsKeyPressed(Keys.R))
        {
            ClearPendingMove();
            LeaveDungeon("You withdraw from the dungeon.");
            return;
        }

        if (moveCooldown > 0.0f)
        {
            return;
        }

        if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A))
        {
            ClearPendingMove();
            TurnLeft();
            moveCooldown = MoveRepeatDelay;
            statusLine = $"Facing {facing.ToString().ToLowerInvariant()}.";
            return;
        }

        if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D))
        {
            ClearPendingMove();
            TurnRight();
            moveCooldown = MoveRepeatDelay;
            statusLine = $"Facing {facing.ToString().ToLowerInvariant()}.";
            return;
        }

        if (Input.IsKeyPressed(Keys.Up) || Input.IsKeyPressed(Keys.W))
        {
            ClearPendingMove();
            moveCooldown = MoveRepeatDelay;
            TryMoveDungeon(GetForwardDelta());
            return;
        }

        if (Input.IsKeyPressed(Keys.Down) || Input.IsKeyPressed(Keys.S))
        {
            ClearPendingMove();
            moveCooldown = MoveRepeatDelay;
            TryMoveDungeon(GetBackwardDelta());
        }
    }

    private void HandleDialogInput()
    {
        if (Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
            ClearPendingMove();
            uiMode = dungeon is null ? UiMode.Overworld : UiMode.Dungeon;
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
            Mana = 12,
            MaxMana = 12,
            Keys = 0,
            Steps = 0
        };
        party.ResetMembers(6);
        EnsurePartyEquipmentState();
        encounter = null;
        townMenu = null;
        dungeon = null;
        panelTitle = string.Empty;
        panelLines.Clear();
        uiMode = UiMode.Overworld;
        visitedLandmarks.Clear();
        ClearPendingMove();
        statusLine = DescribeCurrentTile();
    }

    private void ResetOverworld()
    {
        LoadMapFromDisk();
        statusLine = "The overworld settles back into place.";
    }

    private void ClearPendingMove()
    {
    }

    private void TryMoveDungeon(GridPoint delta)
    {
        if (dungeon is null)
        {
            return;
        }

        var target = new GridPoint(dungeonCell.X + delta.X, dungeonCell.Y + delta.Y);
        if (target.X < 0 || target.Y < 0 || target.X >= dungeon.Width || target.Y >= dungeon.Height)
        {
            statusLine = "Cold stone blocks the way.";
            return;
        }

        var symbol = dungeon.Rows[target.Y][target.X];
        if (symbol == 'x')
        {
            if (party.Keys <= 0)
            {
                statusLine = "A locked gate bars the way.";
                return;
            }

            party.Keys--;
            SetDungeonTile(target, '.');
            symbol = '.';
            statusLine = "You unlock the gate with a brass key.";
        }

        var tile = GetTileDefinition(symbol);
        if (!tile.Walkable)
        {
            statusLine = "A dungeon wall blocks the way.";
            return;
        }

        dungeonCell = target;
        walkFrame = (walkFrame + 1) % 3;
        audioPlayer?.PlayStep();
        HandleTravelStep();
        if (AdvanceDungeonThreats())
        {
            return;
        }
        statusLine = DescribeCurrentTile();
        ResolveDungeonStep(symbol);
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
        audioPlayer?.PlayStep();
        HandleTravelStep();
        statusLine = DescribeCurrentTile();
        MaybeTriggerEncounter(tile);
    }

    private void HandleTravelStep()
    {
        party.Steps++;
        if (party.Steps % 5 != 0)
        {
            return;
        }

        party.Food = Math.Max(0, party.Food - 1);
        if (party.Food == 0)
        {
            DamageRandomAlivePartyMember(1, allowDefeat: false);
        }

        RegeneratePartyOneHitPoint();
    }

    private void MaybeTriggerEncounter(TileDefinition tile)
    {
        var adjustedEncounterChance = tile.EncounterChance * 0.25;
        if (!tile.CanEncounter || random.NextDouble() > adjustedEncounterChance)
        {
            return;
        }

        encounter = tile.Name switch
        {
            "Forest" => EncounterState.CreateWolfPack(),
            "Fen" => EncounterState.CreateFenLeeches(),
            _ => EncounterState.CreateRoadBandits(),
        };
        encounterFromDungeon = false;
        encounterIsBoss = false;
        selectedCombatAction = CombatAction.Attack;
        selectedSpell = SpellKind.Ember;
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
            GetTurnBanner(),
            GetCombatPrompt(),
        ];
        statusLine = $"Encounter! {encounter.Name} blocks the road.";
        audioPlayer?.PlayClash();
        ResetEncounterTurnState();
        uiMode = UiMode.Encounter;
    }

    private void ResolveEncounterRound()
    {
        if (encounter is null)
        {
            uiMode = UiMode.Overworld;
            return;
        }

        if (encounter.AliveCount == 0)
        {
            HandleEncounterVictory();
            return;
        }

        NormalizeEncounterTurnCursor();
        if (encounterTurnOrder.Count == 0)
        {
            HandleEncounterVictory();
            return;
        }

        var currentTurn = encounterTurnOrder[encounterTurnCursor];
        var roundEvents = new List<string>();

        if (currentTurn.IsParty)
        {
            ResolvePartyTurn(currentTurn.Index, roundEvents);
        }
        else
        {
            ResolveEnemyTurn(currentTurn.Index, roundEvents);
        }

        if (party.TotalHealth <= 0)
        {
            HandleDefeat();
            return;
        }

        if (encounter.AliveCount == 0)
        {
            HandleEncounterVictory();
            return;
        }

        statusLine = string.Join(". ", roundEvents.Take(3)) + (roundEvents.Count > 3 ? "..." : string.Empty);
        AdvanceEncounterTurn(currentTurn);
        RefreshEncounterPanel();
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
            ClearPendingMove();
            statusLine = $"You slip away from the {encounter.Name}.";
            encounter = null;
            uiMode = UiMode.Overworld;
            panelLines.Clear();
            panelTitle = string.Empty;
            return;
        }

        var targetPartyIndex = GetRandomAlivePartyIndex(preferFront: true) ?? 0;
        var enemy = encounter.Enemies.FirstOrDefault(enemyUnit => enemyUnit.IsAlive);
        var targetMember = party.Members[targetPartyIndex];
        var defense = GetArmorDefense(targetMember);
        var damage = Math.Max(1, (enemy is null ? 2 : random.Next(enemy.AttackMin, enemy.AttackMax + 1)) - Math.Max(0, party.Level / 2) - defense);
        DamagePartyMember(targetPartyIndex, damage);
        attackingEnemyIndex = enemy is null ? -1 : encounter.Enemies.IndexOf(enemy);
        attackedPartyMemberIndex = targetPartyIndex;
        enemyAttackAnimationTime = 0.24f;
        audioPlayer?.PlayClash();
        statusLine = $"Retreat failed. The {encounter.Name} hits for {damage}.";
        panelLines =
        [
            $"A {encounter.Name} HOLDS YOU FAST.",
            $"PARTY HP {party.TotalHealth}/{party.MaxTotalHealth}",
            GetTurnBanner(),
            GetCombatPrompt(),
        ];

        if (party.TotalHealth <= 0)
        {
            HandleDefeat();
        }
    }

    private void ResolvePartyTurn(int partyIndex, List<string> roundEvents)
    {
        if (partyIndex < 0 || partyIndex >= party.Members.Count || !party.Members[partyIndex].IsAlive)
        {
            roundEvents.Add("Turn lost.");
            return;
        }

        attackingPartyMemberIndex = partyIndex;
        playerAttackAnimationTime = 0.24f;

        if (partyIndex == 0 && selectedCombatAction == CombatAction.Spell && TryCastSpell(roundEvents))
        {
            return;
        }

        var actingTarget = GetSelectedEnemy() ?? encounter?.Enemies.FirstOrDefault(enemyUnit => enemyUnit.IsAlive);
        if (actingTarget is null)
        {
            return;
        }

        if (random.NextDouble() < 0.14)
        {
            roundEvents.Add($"{party.Members[partyIndex].Name} misses {actingTarget.Name}");
            audioPlayer?.PlaySwish();
            return;
        }

        var weapon = GetEquippedWeapon(party.Members[partyIndex]);
        var playerDamage = random.Next(weapon.Attack, weapon.Attack + 4) + party.Level;
        if (partyIndex >= 2)
        {
            playerDamage = Math.Max(2, playerDamage - 2);
        }

        actingTarget.Health = Math.Max(0, actingTarget.Health - playerDamage);
        roundEvents.Add($"{party.Members[partyIndex].Name} hits {actingTarget.Name} for {playerDamage}");
        audioPlayer?.PlayClash();
        if (!actingTarget.IsAlive)
        {
            selectedEnemyIndex = GetDefaultSelectedEnemy();
        }
    }

    private void ResolveEnemyTurn(int enemyIndex, List<string> roundEvents)
    {
        if (encounter is null || enemyIndex < 0 || enemyIndex >= encounter.Enemies.Count)
        {
            roundEvents.Add("Enemy loses its turn.");
            return;
        }

        var enemy = encounter.Enemies[enemyIndex];
        if (!enemy.IsAlive)
        {
            roundEvents.Add($"{enemy.Name} can no longer act.");
            return;
        }

        attackingEnemyIndex = enemyIndex;
        enemyAttackAnimationTime = 0.24f;

        var targetPartyIndex = GetRandomAlivePartyIndex(preferFront: true) ?? 0;
        attackedPartyMemberIndex = targetPartyIndex;

        var targetMember = party.Members[targetPartyIndex];
        var wardReduction = party.WardCharges > 0 ? 2 : 0;
        if (party.WardCharges > 0)
        {
            party.WardCharges--;
        }
        if (random.NextDouble() < 0.16)
        {
            roundEvents.Add($"{enemy.Name} misses {party.Members[targetPartyIndex].Name}");
            PlayEnemyMissSound(enemy.Name);
            return;
        }
        var enemyDamageBase = random.Next(enemy.AttackMin, enemy.AttackMax + 1);
        var enemyDamage = Math.Max(1, enemyDamageBase - (party.Level / 2) - GetArmorDefense(targetMember) - wardReduction);
        if (targetPartyIndex >= 2 && GetFrontAlivePartyIndex() is not null)
        {
            enemyDamage = Math.Max(1, enemyDamage - 2);
        }

        DamagePartyMember(targetPartyIndex, enemyDamage);
        roundEvents.Add($"{enemy.Name} hits {party.Members[targetPartyIndex].Name} for {enemyDamage}");
        PlayEnemyHitSound(enemy.Name);
    }

    private void HandleDefeat()
    {
        ClearPendingMove();
        party.ResetMembers(6 + ((party.Level - 1) * 2));
        party.Gold = Math.Max(0, party.Gold - 12);
        playerCell = new GridPoint(map.Start.X, map.Start.Y);
        dungeon = null;
        townMenu = null;
        encounter = null;
        panelTitle = string.Empty;
        panelLines.Clear();
        uiMode = UiMode.Overworld;
        defeatPortalTimer = 5.0f;
        audioPlayer?.PlayPortal();
        statusLine = "The party was defeated and dragged back to safety.";
    }

    private void InteractWithCurrentTile()
    {
        if (dungeon is not null)
        {
            InteractWithDungeonTile();
            return;
        }

        var symbol = map.Rows[playerCell.Y][playerCell.X];
        switch (symbol)
        {
            case 'T':
            case 'H':
            case 'C':
                OpenTownMenu(symbol);
                break;
            case 'K':
                OpenTownMenu(symbol);
                break;
            case 'S':
                OpenTownMenu(symbol);
                break;
            case 'R':
                OpenTownMenu(symbol);
                break;
            case 'D':
                EnterDungeon();
                break;
            default:
                statusLine = DescribeCurrentTile();
                break;
        }
    }

    private void OpenTownMenu(char symbol)
    {
        ClearPendingMove();
        townMenu = TownMenuState.Create(symbol);
        uiMode = UiMode.Town;
        audioPlayer?.PlayBell();
        RefreshTownPanel();
    }

    private void RefreshTownPanel()
    {
        if (townMenu is null)
        {
            return;
        }

        panelTitle = townMenu.Title;
        panelLines =
        [
            townMenu.Description,
            "",
            .. townMenu.Options.Select((option, index) => $"{(index == townMenu.SelectedIndex ? ">" : " ")} {option}"),
            "",
            "LEFT RIGHT CHOOSE  ENTER CONFIRMS",
        ];
    }

    private void ExecuteTownOption()
    {
        if (townMenu is null)
        {
            return;
        }

        var choice = townMenu.Options[townMenu.SelectedIndex];
        switch (townMenu.Symbol)
        {
            case 'T':
                ExecuteTownOption(choice);
                break;
            case 'H':
                ExecuteHarborOption(choice);
                break;
            case 'C':
                ExecuteCampOption(choice);
                break;
            case 'K':
                ExecuteKeepOption(choice);
                break;
            case 'S':
                ExecuteShrineOption(choice);
                break;
            case 'R':
                ExecuteRuinsOption(choice);
                break;
        }
    }

    private void ExecuteTownOption(string choice)
    {
        switch (choice)
        {
            case "REST - 8 GOLD":
                if (party.Gold < 8)
                {
                    statusLine = "You cannot afford a safe room.";
                    return;
                }

                party.Gold -= 8;
                HealAllPartyMembers();
                statusLine = "The party rests behind stout doors.";
                break;
            case "BUY RATIONS +25 FOOD - 6 GOLD":
                if (party.Gold < 6)
                {
                    statusLine = "You cannot afford provisions.";
                    return;
                }

                party.Gold -= 6;
                party.Food += 25;
                statusLine = "You buy dried meat and hard bread.";
                break;
            case "FORGE WEAPON +1":
                var nextWeapon = GetNextShopItem(EquipmentSlot.Weapon);
                if (nextWeapon is null)
                {
                    statusLine = "No finer weapons are for sale here.";
                    return;
                }

                if (party.Gold < nextWeapon.Cost)
                {
                    statusLine = $"{nextWeapon.Name} costs {nextWeapon.Cost} gold.";
                    return;
                }

                party.Gold -= nextWeapon.Cost;
                statusLine = GrantEquipment(nextWeapon.Id, "The smith equips");
                break;
            case "BUY ARMOR +1":
                var nextArmor = GetNextShopItem(EquipmentSlot.Armor);
                if (nextArmor is null)
                {
                    statusLine = "No sturdier armor is available.";
                    return;
                }

                if (party.Gold < nextArmor.Cost)
                {
                    statusLine = $"{nextArmor.Name} costs {nextArmor.Cost} gold.";
                    return;
                }

                party.Gold -= nextArmor.Cost;
                statusLine = GrantEquipment(nextArmor.Id, "The outfitter fits");
                break;
            default:
                CloseTownMenu("You step back onto the road.");
                return;
        }

        CloseTownMenu(statusLine);
    }

    private void ExecuteHarborOption(string choice)
    {
        switch (choice)
        {
            case "REST - 8 GOLD":
            case "BUY RATIONS +25 FOOD - 6 GOLD":
                ExecuteTownOption(choice);
                return;
            case "BUY ARMOR +1":
                var nextArmor = GetNextShopItem(EquipmentSlot.Armor);
                if (nextArmor is null)
                {
                    statusLine = "The harbor has no better armor on hand.";
                    return;
                }

                if (party.Gold < nextArmor.Cost)
                {
                    statusLine = $"{nextArmor.Name} costs {nextArmor.Cost} gold.";
                    return;
                }

                party.Gold -= nextArmor.Cost;
                CloseTownMenu(GrantEquipment(nextArmor.Id, "A sea captain sells you"));
                return;
            default:
                CloseTownMenu("You leave the harbor behind.");
                return;
        }
    }

    private void ExecuteCampOption(string choice)
    {
        if (choice == "LEAVE")
        {
            CloseTownMenu("The campfire burns behind you.");
            return;
        }

        ExecuteTownOption(choice);
    }

    private void ExecuteKeepOption(string choice)
    {
        if (choice == "LEAVE")
        {
            CloseTownMenu("You leave the keep yard.");
            return;
        }

        if (party.Gold < 30)
        {
            statusLine = "The captain demands 30 gold for training.";
            return;
        }

        party.Gold -= 30;
        party.Level++;
        RaisePartyMaxHealth(2);
        HealAllPartyMembers();
        CloseTownMenu("Training at the keep hardens the party.");
    }

    private void ExecuteShrineOption(string choice)
    {
        if (choice == "LEAVE")
        {
            CloseTownMenu("The shrine falls silent behind you.");
            return;
        }

        if (visitedLandmarks.Add(playerCell))
        {
            RaisePartyMaxHealth(1);
            HealAllPartyMembers();
            CloseTownMenu("A quiet blessing settles over the party.");
            return;
        }

        HealAllPartyMembers();
        CloseTownMenu("The shrine offers only a moment of peace.");
    }

    private void ExecuteRuinsOption(string choice)
    {
        if (choice == "LEAVE")
        {
            CloseTownMenu("You leave the broken stones undisturbed.");
            return;
        }

        if (visitedLandmarks.Add(playerCell))
        {
            var loot = random.Next(12, 29);
            party.Gold += loot;
            CloseTownMenu($"You find {loot} gold in the rubble.");
            return;
        }

        var scrap = random.Next(0, 2) == 0 ? 0 : random.Next(2, 7);
        party.Gold += scrap;
        CloseTownMenu(scrap == 0 ? "The ruins are mostly picked clean." : $"You salvage {scrap} gold worth of scrap.");
    }

    private void CloseTownMenu(string message)
    {
        ClearPendingMove();
        townMenu = null;
        panelTitle = string.Empty;
        panelLines.Clear();
        uiMode = dungeon is null ? UiMode.Overworld : UiMode.Dungeon;
        statusLine = message;
    }

    private void EnterDungeon()
    {
        ClearPendingMove();
        dungeon = GenerateDungeonLevel(1);
        dungeonCell = dungeon.Start;
        uiMode = UiMode.Dungeon;
        statusLine = "You descend into cold stone and torch smoke. RETURN TO < AND PRESS ENTER TO LEAVE.";
    }

    private void InteractWithDungeonTile()
    {
        if (dungeon is null)
        {
            return;
        }

        var symbol = dungeon.Rows[dungeonCell.Y][dungeonCell.X];
        switch (symbol)
        {
            case '<':
                LeaveDungeon("You climb back into daylight.");
                break;
            case '>':
                dungeon = GenerateDungeonLevel(dungeon.Level + 1);
                dungeonCell = dungeon.Start;
                statusLine = $"You descend to dungeon level {dungeon.Level}.";
                break;
            case 'G':
                audioPlayer?.PlayChest();
                CollectDungeonTreasure();
                break;
            case 'L':
                HealAllPartyMembers();
                SetDungeonTile(dungeonCell, '.');
                statusLine = "A cold fountain restores the party.";
                break;
            case 'k':
                party.Keys++;
                SetDungeonTile(dungeonCell, '.');
                statusLine = $"You take a brass key. Keys: {party.Keys}.";
                break;
            default:
                statusLine = DescribeCurrentTile();
                break;
        }
    }

    private void ResolveDungeonStep(char symbol)
    {
        switch (symbol)
        {
            case 'M':
            case 'B':
                SetDungeonTile(dungeonCell, '.');
                StartDungeonEncounter(symbol == 'B');
                break;
            case 'G':
                statusLine = "A chest lies here. Press Enter to open it.";
                break;
            case '>':
                statusLine = "A stair descends into a deeper dark.";
                break;
            case '<':
                statusLine = "Stone stairs rise back to the surface.";
                break;
            case 'L':
                statusLine = "A black fountain reflects dim light.";
                break;
            case 'k':
                statusLine = "A brass key glints here. Press Enter to take it.";
                break;
        }
    }

    private void LeaveDungeon(string message)
    {
        ClearPendingMove();
        dungeon = null;
        dungeonCell = GridPoint.Zero;
        uiMode = UiMode.Overworld;
        statusLine = message;
    }

    private void CollectDungeonTreasure()
    {
        if (dungeon is null)
        {
            return;
        }

        var gold = random.Next(8, 20) + (dungeon.Level * 3);
        var food = random.Next(0, 2) == 0 ? 0 : random.Next(4, 10);
        party.Gold += gold;
        party.Food += food;
        SetDungeonTile(dungeonCell, '.');

        var drops = new List<string>();
        if (food > 0)
        {
            drops.Add($"{food} food");
        }

        if (TryAwardEquipmentDrop(dungeon.Level + 1, out var itemDrop))
        {
            drops.Add(itemDrop);
        }

        statusLine = drops.Count > 0
            ? $"You recover {gold} gold and {string.Join(", ", drops)}."
            : $"You recover {gold} gold.";
    }

    private void SetDungeonTile(GridPoint point, char symbol)
    {
        if (dungeon is null)
        {
            return;
        }

        var row = dungeon.Rows[point.Y].ToCharArray();
        row[point.X] = symbol;
        dungeon.Rows[point.Y] = new string(row);
    }

    private DungeonState GenerateDungeonLevel(int level)
    {
        var rows = new List<string>();
        const int size = 12;
        for (var y = 0; y < size; y++)
        {
            var chars = new char[size];
            for (var x = 0; x < size; x++)
            {
                chars[x] = x == 0 || y == 0 || x == size - 1 || y == size - 1 ? '#' : '.';
            }

            rows.Add(new string(chars));
        }

        var state = new DungeonState
        {
            Level = level,
            Rows = rows,
            Start = new GridPoint(1, size - 2)
        };

        // Carve a few broad chambers and connector passages so the dungeon has shape.
        for (var y = 2; y <= 4; y++)
        {
            for (var x = 2; x <= 4; x++)
            {
                state.SetTile(new GridPoint(x, y), '.');
            }
        }

        for (var y = 7; y <= 9; y++)
        {
            for (var x = 1; x <= 4; x++)
            {
                state.SetTile(new GridPoint(x, y), '.');
            }
        }

        for (var y = 2; y <= 4; y++)
        {
            for (var x = 7; x <= 9; x++)
            {
                state.SetTile(new GridPoint(x, y), '.');
            }
        }

        for (var y = 1; y < size - 1; y++)
        {
            state.SetTile(new GridPoint(5, y), '.');
        }

        for (var x = 1; x < size - 1; x++)
        {
            state.SetTile(new GridPoint(x, 6), '.');
        }

        // Reintroduce some masonry so the space reads like a dungeon rather than an open box.
        foreach (var point in new[]
        {
            new GridPoint(3, 5),
            new GridPoint(7, 5),
            new GridPoint(8, 6),
            new GridPoint(2, 8),
            new GridPoint(8, 8),
        })
        {
            state.SetTile(point, '#');
        }

        state.SetTile(state.Start, '<');
        var stairsPoint = new GridPoint(size - 2, 1);
        state.ExitPoint = stairsPoint;
        state.SetTile(new GridPoint(size / 2, size / 2), 'L');

        if (level >= 2)
        {
            var gateY = size / 2;
            for (var x = 1; x < size - 1; x++)
            {
                state.SetTile(new GridPoint(x, gateY), '#');
            }

            var gatePoint = new GridPoint(size / 2, gateY);
            state.SetTile(gatePoint, 'x');
            state.GatePoint = gatePoint;
            PlaceDungeonFeature(state, 'k', maxYExclusive: gateY);
        }

        var monsters = 3 + level;
        for (var i = 0; i < monsters; i++)
        {
            PlaceDungeonFeature(state, 'M');
        }

        var treasures = 2 + (level / 2);
        for (var i = 0; i < treasures; i++)
        {
            PlaceDungeonFeature(state, 'G');
        }

        if (level % 3 == 0)
        {
            state.BossFloor = true;
            state.SetTile(stairsPoint, '.');
            state.BossExitPoint = stairsPoint;
            PlaceDungeonFeature(state, 'B', minYInclusive: size / 2);
        }
        else
        {
            state.SetTile(stairsPoint, '>');
        }

        return state;
    }

    private bool AdvanceDungeonThreats()
    {
        if (dungeon is null)
        {
            return false;
        }

        var threats = new List<(GridPoint point, char symbol)>();
        for (var y = 0; y < dungeon.Rows.Count; y++)
        {
            for (var x = 0; x < dungeon.Rows[y].Length; x++)
            {
                var symbol = dungeon.Rows[y][x];
                if (symbol is 'M' or 'B')
                {
                    threats.Add((new GridPoint(x, y), symbol));
                }
            }
        }

        foreach (var threat in threats)
        {
            if (random.NextDouble() > 0.5)
            {
                continue;
            }

            var next = GetThreatStep(threat.point, dungeonCell);
            if (next == threat.point)
            {
                continue;
            }

            if (next == dungeonCell)
            {
                SetDungeonTile(threat.point, '.');
                StartDungeonEncounter(threat.symbol == 'B');
                statusLine = threat.symbol == 'B' ? "The dungeon lord closes in." : "A lurking threat rushes you.";
                return true;
            }

            if (dungeon.GetTile(next) != '.')
            {
                continue;
            }

            SetDungeonTile(threat.point, '.');
            SetDungeonTile(next, threat.symbol);
        }

        return false;
    }

    private GridPoint GetThreatStep(GridPoint from, GridPoint toward)
    {
        if (dungeon is null)
        {
            return from;
        }

        var dx = toward.X - from.X;
        var dy = toward.Y - from.Y;
        var primary = Math.Abs(dx) >= Math.Abs(dy)
            ? new GridPoint(Math.Sign(dx), 0)
            : new GridPoint(0, Math.Sign(dy));
        var secondary = primary.X == 0
            ? new GridPoint(Math.Sign(dx), 0)
            : new GridPoint(0, Math.Sign(dy));

        foreach (var delta in new[] { primary, secondary })
        {
            if (delta == GridPoint.Zero)
            {
                continue;
            }

            var candidate = new GridPoint(from.X + delta.X, from.Y + delta.Y);
            if (candidate == dungeonCell)
            {
                return candidate;
            }

            if (candidate.X < 0 || candidate.Y < 0 || candidate.X >= dungeon.Width || candidate.Y >= dungeon.Height)
            {
                continue;
            }

            if (dungeon.GetTile(candidate) == '.')
            {
                return candidate;
            }
        }

        return from;
    }

    private void StartDungeonEncounter(bool boss)
    {
        ClearPendingMove();
        encounter = boss ? EncounterState.CreateDungeonBoss(dungeon?.Level ?? 1) : EncounterState.CreateDungeonPack(dungeon?.Level ?? 1);
        encounterFromDungeon = true;
        encounterIsBoss = boss;
        selectedEnemyIndex = GetDefaultSelectedEnemy();
        attackingEnemyIndex = -1;
        attackedPartyMemberIndex = -1;
        playerAttackAnimationTime = 0.0f;
        enemyAttackAnimationTime = 0.0f;
        panelTitle = boss ? "BOSS" : "ENCOUNTER";
        statusLine = boss ? "A dungeon lord blocks the way." : "Something stirs in the dark.";
        audioPlayer?.PlayClash();
        ResetEncounterTurnState();
        RefreshEncounterPanel();
        uiMode = UiMode.Encounter;
    }

    private void PlaceDungeonFeature(DungeonState state, char symbol, int minYInclusive = 1, int? maxYExclusive = null)
    {
        var maxY = maxYExclusive ?? state.Height - 1;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var point = new GridPoint(random.Next(1, state.Width - 1), random.Next(minYInclusive, maxY));
            if (state.GetTile(point) == '.')
            {
                state.SetTile(point, symbol);
                return;
            }
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
        var symbol = dungeon is null ? map.Rows[playerCell.Y][playerCell.X] : dungeon.Rows[dungeonCell.Y][dungeonCell.X];
        if (dungeon is not null && symbol == '.')
        {
            return $"[D{dungeon.Level}:{dungeonCell.X},{dungeonCell.Y}] DUNGEON FLOOR: Worn stone underfoot, cold with old damp.";
        }

        var tile = GetTileDefinition(symbol);
        var location = dungeon is null
            ? $"[{playerCell.X},{playerCell.Y}]"
            : $"[D{dungeon.Level}:{dungeonCell.X},{dungeonCell.Y}]";
        var baseText = $"{location} {tile.Name}";
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
        if (dungeon is not null)
        {
            DrawDungeonCrawlerView();
            return;
        }

        DrawOverworldCrawlerView();
    }

    private void DrawOverworldCrawlerView()
    {
        var frame = new RectangleF(28, 28, 692, 664);
        var view = new RectangleF(52, 52, 644, 548);
        DrawPanel(frame.X, frame.Y, frame.Width, frame.Height, new Color(18, 22, 28, 244));
        DrawFrame(frame, new Color(112, 120, 138), 2);

        var currentTile = GetTileDefinition(map.Rows[playerCell.Y][playerCell.X]);
        DrawText("OVERWORLD", new Vector2(view.X + 18, view.Y + 16), new Color(210, 194, 154), 2);
        DrawText(currentTile.Name.ToUpperInvariant(), new Vector2(view.X + 198, view.Y + 16), new Color(140, 160, 180), 2);
        DrawText($"FACING {facing.ToString().ToUpperInvariant()}", new Vector2(view.X + 438, view.Y + 16), new Color(138, 152, 174), 2);

        var viewport = new RectangleF(view.X + 18, view.Y + 46, view.Width - 36, 420);
        DrawPanel(viewport.X, viewport.Y, viewport.Width, viewport.Height, new Color(10, 12, 18, 255));
        DrawFrame(viewport, new Color(80, 88, 106), 2);
        DrawOverworldEnvironment(viewport);
        DrawOverworldParticles(viewport);
        DrawDungeonPortraitStrip(view);
    }

    private void DrawDungeonCrawlerView()
    {
        var frame = new RectangleF(28, 28, 692, 664);
        var view = new RectangleF(52, 52, 644, 548);
        DrawPanel(frame.X, frame.Y, frame.Width, frame.Height, new Color(20, 22, 28, 244));
        DrawFrame(frame, new Color(112, 120, 138), 2);

        DrawText($"DUNGEON LV {dungeon?.Level ?? 1}", new Vector2(view.X + 18, view.Y + 16), new Color(210, 194, 154), 2);
        DrawText($"FACING {facing.ToString().ToUpperInvariant()}", new Vector2(view.X + 438, view.Y + 16), new Color(138, 152, 174), 2);

        var viewport = new RectangleF(view.X + 18, view.Y + 46, view.Width - 36, 420);
        DrawPanel(viewport.X, viewport.Y, viewport.Width, viewport.Height, new Color(10, 12, 18, 255));
        DrawFrame(viewport, new Color(80, 88, 106), 2);
        DrawCrawlerEnvironment(viewport, isDungeonView: true);
        DrawDungeonParticles(viewport);
        DrawDungeonPortraitStrip(view);
    }

    private void DrawOverworldParticles(RectangleF viewport)
    {
        var cycle = ((playerCell.X * 31) + (playerCell.Y * 17) + (int)facing) % 3;
        var showSnow = cycle != 1;
        var showLeaves = cycle != 0;
        var count = showSnow && showLeaves ? 36 : 24;

        for (var i = 0; i < count; i++)
        {
            var seed = (i * 97) + (playerCell.X * 13) + (playerCell.Y * 19);
            var drift = (totalTime * (12.0f + (seed % 7))) + (seed * 0.37f);
            var x = viewport.X + PositiveModulo((seed * 23.0f) + (showSnow ? drift * 0.85f : drift * 1.35f), viewport.Width);
            var y = viewport.Y + PositiveModulo((seed * 41.0f) + (showSnow ? drift * 1.9f : drift * 1.35f), viewport.Height);

            if (showSnow && (!showLeaves || i % 2 == 0))
            {
                var size = 2.0f + ((seed % 3) * 0.8f);
                DrawPanel(x, y, size, size, new Color(240, 246, 255, 170));
                continue;
            }

            var sway = MathF.Sin((totalTime * 2.4f) + seed) * 4.0f;
            var leaf = new RectangleF(x + sway, y, 5.0f, 3.0f);
            var tint = (seed % 3) switch
            {
                0 => new Color(168, 122, 54, 176),
                1 => new Color(122, 98, 36, 176),
                _ => new Color(154, 82, 42, 176),
            };
            DrawPanel(leaf.X, leaf.Y, leaf.Width, leaf.Height, tint);
        }
    }

    private void DrawDungeonParticles(RectangleF viewport)
    {
        for (var i = 0; i < 15; i++)
        {
            var seed = (i * 73) + (dungeonCell.X * 11) + (dungeonCell.Y * 17);
            var drift = (totalTime * (8.0f + (seed % 5))) + (seed * 0.21f);
            var x = viewport.X + PositiveModulo((seed * 19.0f) + drift, viewport.Width);
            var y = viewport.Y + PositiveModulo((seed * 29.0f) + (drift * 0.7f), viewport.Height);
            var size = 1.5f + ((seed % 4) * 0.55f);
            var alpha = (byte)(20 + (seed % 36));
            DrawPanel(x, y, size, size, new Color(112, 98, 84, alpha));
        }
    }

    private static float PositiveModulo(float value, float modulus)
    {
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private void DrawOverworldEnvironment(RectangleF viewport)
    {
        var skyTop = new RectangleF(viewport.X, viewport.Y, viewport.Width, viewport.Height * 0.34f);
        var skyMid = new RectangleF(viewport.X, skyTop.Bottom, viewport.Width, viewport.Height * 0.22f);
        var ground = new RectangleF(viewport.X, skyMid.Bottom, viewport.Width, viewport.Bottom - skyMid.Bottom);
        DrawPanel(skyTop.X, skyTop.Y, skyTop.Width, skyTop.Height, new Color(104, 154, 220));
        DrawPanel(skyMid.X, skyMid.Y, skyMid.Width, skyMid.Height, new Color(142, 188, 234));
        DrawPanel(ground.X, ground.Y, ground.Width, ground.Height, new Color(96, 104, 88));

        DrawCloud(new RectangleF(viewport.X + 52.0f, viewport.Y + 26.0f, 104.0f, 28.0f));
        DrawCloud(new RectangleF(viewport.X + 224.0f, viewport.Y + 44.0f, 132.0f, 34.0f));
        DrawCloud(new RectangleF(viewport.Right - 186.0f, viewport.Y + 24.0f, 116.0f, 30.0f));

        var horizonY = skyMid.Bottom - 6.0f;
        DrawPanel(viewport.X, horizonY, viewport.Width, 6.0f, new Color(166, 158, 126, 120));
        DrawOverworldCellField(viewport, ground, horizonY);
    }

    private void DrawOverworldCellField(RectangleF viewport, RectangleF ground, float horizonY)
    {
        var rows = 84;
        var columns = 96;
        var rowHeight = ground.Height / rows;
        var columnWidth = viewport.Width / columns;
        var cameraX = playerCell.X + 0.5f;
        var cameraY = playerCell.Y + 0.5f;
        var forward = GetForwardDelta();
        var right = GetRightDelta();
        const float nearDistance = 0.4f;
        const float farDistance = 8.25f;
        const float sideSpread = 0.82f;

        for (var row = 0; row < rows; row++)
        {
            var y0 = ground.Y + (row * rowHeight);
            var y1 = y0 + rowHeight + 1.0f;
            var t0 = row / (float)rows;
            var t1 = (row + 1) / (float)rows;
            var distNear = nearDistance + ((1.0f - t1) * (1.0f - t1) * farDistance);
            var distFar = nearDistance + ((1.0f - t0) * (1.0f - t0) * farDistance);
            var halfWidthNear = distNear * sideSpread;
            var halfWidthFar = distFar * sideSpread;

            var startColumn = 0;
            while (startColumn < columns)
            {
                var uStart = ((startColumn + 0.5f) / columns * 2.0f) - 1.0f;
                var sample = SampleOverworldProjectedSymbol(cameraX, cameraY, forward, right, distNear, halfWidthNear, uStart);
                var endColumn = startColumn + 1;
                while (endColumn < columns)
                {
                    var u = ((endColumn + 0.5f) / columns * 2.0f) - 1.0f;
                    if (SampleOverworldProjectedSymbol(cameraX, cameraY, forward, right, distNear, halfWidthNear, u) != sample)
                    {
                        break;
                    }

                    endColumn++;
                }

                var x0 = viewport.X + (startColumn * columnWidth);
                var x1 = viewport.X + (endColumn * columnWidth);
                var topLeft = x0;
                var topRight = x1;
                var bottomLeft = x0;
                var bottomRight = x1;
                DrawTrapezoid(topLeft, topRight, bottomLeft, bottomRight, y0, y1, GetOverworldSurfaceColor(sample));

                if (sample is '=' or 'P')
                {
                    var center = (x0 + x1) * 0.5f;
                    var topHalf = (x1 - x0) * 0.22f;
                    var bottomHalf = (x1 - x0) * 0.22f;
                    DrawTrapezoid(center - topHalf, center + topHalf, center - bottomHalf, center + bottomHalf, y0, y1, new Color(128, 100, 72, 236));
                }
                startColumn = endColumn;
            }
        }

        DrawCurrentCellForeground(viewport, ground);

        DrawOverworldFeatureField(viewport, ground, cameraX, cameraY, forward, right);
    }

    private Color GetOverworldSurfaceColor(char symbol) => symbol switch
    {
        '~' => new Color(52, 96, 158, 236),
        '=' or 'P' => new Color(144, 120, 88, 222),
        'F' => new Color(92, 106, 82, 216),
        '^' => new Color(98, 100, 108, 224),
        '*' => new Color(86, 106, 78, 220),
        _ => new Color(112, 108, 84, 208),
    };

    private void DrawCurrentCellForeground(RectangleF viewport, RectangleF ground)
    {
        var symbol = map.Rows[playerCell.Y][playerCell.X];
        var rect = new RectangleF(viewport.Center.X - 112.0f, ground.Bottom - 84.0f, 224.0f, 84.0f);
        DrawPanel(rect.X, rect.Y, rect.Width, rect.Height, GetOverworldSurfaceColor(symbol));

        if (symbol is '=' or 'P')
        {
            DrawPanel(rect.Center.X - 26.0f, rect.Y, 52.0f, rect.Height, new Color(128, 100, 72, 236));
        }
        else if (symbol == '~')
        {
            DrawPanel(rect.X + 8.0f, rect.Y + 12.0f, rect.Width - 16.0f, 8.0f, new Color(120, 170, 220, 44));
        }
        else if (symbol is not '.' and not '*' and not '^' and not 'F')
        {
            DrawOverworldBillboardFromFootprint(viewport, symbol, rect.Center.X, rect.Bottom, rect.Width * 0.42f, rect.Height, 0.0f);
        }
    }

    private char SampleOverworldProjectedSymbol(float cameraX, float cameraY, GridPoint forward, GridPoint right, float distance, float halfWidth, float u)
    {
        var worldX = cameraX + (forward.X * distance) + (right.X * halfWidth * u);
        var worldY = cameraY + (forward.Y * distance) + (right.Y * halfWidth * u);
        return GetOverworldProjectedSymbol(worldX, worldY);
    }

    private char GetOverworldProjectedSymbol(float worldX, float worldY)
    {
        var gridX = (int)MathF.Floor(worldX);
        var gridY = (int)MathF.Floor(worldY);
        if (gridX < 0 || gridY < 0 || gridY >= map.Rows.Count || gridX >= map.Rows[gridY].Length)
        {
            return '~';
        }

        return map.Rows[gridY][gridX];
    }

    private void DrawOverworldCellFeature(RectangleF viewport, char symbol, float centerX, float baseY, int depth, bool centerLane, int variantSeed)
    {
        if (symbol is '.' or '~' or '=' or 'P')
        {
            return;
        }

        var depthScales = new[] { 0.88f, 0.66f, 0.48f, 0.34f };
        var scale = depthScales[Math.Clamp(depth - 1, 0, depthScales.Length - 1)] * (centerLane ? 1.0f : 0.94f);
        var footprintWidth = 62.0f * scale;
        var footprintHeight = 42.0f * scale;

        if (symbol == '^')
        {
            footprintWidth *= centerLane ? 2.6f : 2.1f;
            footprintHeight *= centerLane ? 2.3f : 1.9f;
        }
        else if (symbol == '*')
        {
            footprintWidth *= 1.35f;
            footprintHeight *= 1.45f;
        }

        DrawOverworldBillboardFromFootprint(viewport, symbol, centerX, baseY, footprintWidth, footprintHeight, 0.0f, variantSeed);
    }

    private void DrawOverworldFeatureField(RectangleF viewport, RectangleF ground, float cameraX, float cameraY, GridPoint forward, GridPoint right)
    {
        var drawn = new System.Collections.Generic.HashSet<GridPoint>();
        for (var depth = 6; depth >= 1; depth--)
        {
            for (var side = -6; side <= 6; side++)
            {
                var cell = GetRelativeCell(side, depth, isDungeonView: false);
                if (cell.X < 0 || cell.Y < 0 || cell.Y >= map.Rows.Count || cell.X >= map.Rows[cell.Y].Length || !drawn.Add(cell))
                {
                    continue;
                }

                var symbol = map.Rows[cell.Y][cell.X];
                if (symbol is '.' or '~' or '=' or 'P')
                {
                    continue;
                }

                var cellCenterX = cell.X + 0.5f;
                var cellCenterY = cell.Y + 0.5f;
                var dx = cellCenterX - cameraX;
                var dy = cellCenterY - cameraY;
                var localForward = (dx * forward.X) + (dy * forward.Y);
                var localSide = (dx * right.X) + (dy * right.Y);

                if (localForward < 0.35f || !TryProjectOverworldFeature(viewport, ground, localForward, localSide, out var screenX, out var baseY, out var projectedDepth))
                {
                    continue;
                }

                var variantSeed = (cell.X * 73856093) ^ (cell.Y * 19349663) ^ symbol;
                DrawOverworldCellFeature(viewport, symbol, screenX, baseY, projectedDepth, Math.Abs(localSide) < 0.45f, variantSeed);
            }
        }
    }

    private bool TryProjectOverworldFeature(RectangleF viewport, RectangleF ground, float localForward, float localSide, out float screenX, out float baseY, out int depth)
    {
        const float nearDistance = 0.4f;
        const float farDistance = 8.25f;
        const float sideSpread = 0.82f;

        if (localForward < nearDistance || localForward > nearDistance + farDistance)
        {
            screenX = 0.0f;
            baseY = 0.0f;
            depth = 1;
            return false;
        }

        var normalizedDistance = Math.Clamp((localForward - nearDistance) / farDistance, 0.0f, 1.0f);
        var t = 1.0f - MathF.Sqrt(normalizedDistance);
        var halfWidth = Math.Max(0.12f, localForward * sideSpread);
        var u = localSide / halfWidth;
        if (MathF.Abs(u) > 1.25f)
        {
            screenX = 0.0f;
            baseY = 0.0f;
            depth = 1;
            return false;
        }

        screenX = viewport.Center.X + (u * viewport.Width * 0.5f);
        baseY = ground.Y + (t * ground.Height);
        depth = Math.Clamp((int)MathF.Round(localForward), 1, 4);
        return true;
    }

    private void DrawCrawlerEnvironment(RectangleF viewport, bool isDungeonView)
    {
        if (isDungeonView)
        {
            DrawDungeonRaycastEnvironment(viewport);
            return;
        }

        var ceilingRect = new RectangleF(viewport.X, viewport.Y, viewport.Width, viewport.Height * 0.47f);
        var floorRect = new RectangleF(viewport.X, viewport.Y + (viewport.Height * 0.47f), viewport.Width, viewport.Height * 0.53f);
        var ceilingTint = new Color(110, 126, 148);
        var floorTint = new Color(128, 118, 96);
        spriteBatch.Draw(grimCeilingTexture, UiRect(ceilingRect), ceilingTint);
        spriteBatch.Draw(grimFloorTexture, UiRect(floorRect), floorTint);

        var portals = new[]
        {
            new RectangleF(viewport.X + 42, viewport.Y + 38, viewport.Width - 84, viewport.Height - 108),
            new RectangleF(viewport.X + 106, viewport.Y + 78, viewport.Width - 212, viewport.Height - 176),
            new RectangleF(viewport.X + 158, viewport.Y + 112, viewport.Width - 316, viewport.Height - 230),
            new RectangleF(viewport.X + 202, viewport.Y + 140, viewport.Width - 404, viewport.Height - 274),
            new RectangleF(viewport.X + 236, viewport.Y + 164, viewport.Width - 472, viewport.Height - 312),
        };

        for (var depth = portals.Length - 1; depth >= 0; depth--)
        {
            var portal = portals[depth];
            var next = depth == portals.Length - 1 ? portal : portals[depth + 1];
            var forward = GetRelativeCell(0, depth + 1, isDungeonView);
            var forwardSymbol = GetCrawlerCellSymbol(forward, isDungeonView);
            var frontBlocked = IsBlockedCrawlerSymbol(forwardSymbol, isDungeonView);

            var leftCell = GetRelativeCell(-1, depth, isDungeonView);
            var rightCell = GetRelativeCell(1, depth, isDungeonView);
            var leftSymbol = GetCrawlerCellSymbol(leftCell, isDungeonView);
            var rightSymbol = GetCrawlerCellSymbol(rightCell, isDungeonView);
            if (IsBlockedCrawlerSymbol(leftSymbol, isDungeonView))
            {
                var leftWall = new RectangleF(portal.X, next.Y, Math.Max(28.0f, next.X - portal.X + 14.0f), next.Height);
                spriteBatch.Draw(grimWallTexture, UiRect(leftWall), GetCrawlerWallTint(leftSymbol, isDungeonView));
                DrawFrame(leftWall, new Color(42, 40, 46), 2);
            }

            if (IsBlockedCrawlerSymbol(rightSymbol, isDungeonView))
            {
                var rightWall = new RectangleF(next.Right - 14.0f, next.Y, Math.Max(28.0f, portal.Right - next.Right + 14.0f), next.Height);
                spriteBatch.Draw(grimWallTexture, UiRect(rightWall), GetCrawlerWallTint(rightSymbol, isDungeonView));
                DrawFrame(rightWall, new Color(42, 40, 46), 2);
            }

            if (frontBlocked)
            {
                spriteBatch.Draw(grimWallTexture, UiRect(portal), GetCrawlerWallTint(forwardSymbol, isDungeonView));
                DrawFrame(portal, new Color(30, 28, 32), 3);
                DrawCrawlerFeatureBillboard(forwardSymbol, portal, depth, isDungeonView);
                break;
            }

            DrawCrawlerFeatureBillboard(forwardSymbol, portal, depth, isDungeonView);
        }

    }

    private void DrawDungeonRaycastEnvironment(RectangleF viewport)
    {
        var ceilingRect = new RectangleF(viewport.X, viewport.Y, viewport.Width, viewport.Height * 0.42f);
        var floorRect = new RectangleF(viewport.X, ceilingRect.Bottom, viewport.Width, viewport.Bottom - ceilingRect.Bottom);
        DrawPanel(ceilingRect.X, ceilingRect.Y, ceilingRect.Width, ceilingRect.Height, new Color(16, 18, 24));
        DrawPanel(floorRect.X, floorRect.Y, floorRect.Width, floorRect.Height, new Color(40, 38, 36));
        DrawPanel(viewport.X, ceilingRect.Bottom - 4.0f, viewport.Width, 4.0f, new Color(74, 70, 64));
        DrawPanel(viewport.X, floorRect.Y, viewport.Width, 2.0f, new Color(110, 100, 88));

        var columns = 180;
        var columnWidth = viewport.Width / columns;
        var player = new Vector2(dungeonCell.X + 0.5f, dungeonCell.Y + 0.5f);
        var forward = new Vector2(GetForwardDelta().X, GetForwardDelta().Y);
        var right = new Vector2(GetRightDelta().X, GetRightDelta().Y);
        var wallDistances = new float[columns];
        Array.Fill(wallDistances, 99.0f);

        for (var column = 0; column < columns; column++)
        {
            var u = (((column + 0.5f) / columns) * 2.0f) - 1.0f;
            var ray = forward + (right * (u * 0.8f));
            ray.Normalize();
            var rayStep = new Vector2(ray.X * 0.035f, ray.Y * 0.035f);
            var sample = player;
            var hitSymbol = '.';
            var hitDistance = 8.0f;

            for (var distance = 0.08f; distance <= 8.0f; distance += 0.035f)
            {
                sample += rayStep;
                var point = new GridPoint((int)MathF.Floor(sample.X), (int)MathF.Floor(sample.Y));
                var symbol = GetCrawlerCellSymbol(point, isDungeonView: true);
                if (IsBlockedCrawlerSymbol(symbol, true))
                {
                    hitSymbol = symbol;
                    hitDistance = distance;
                    break;
                }
            }

            wallDistances[column] = hitDistance;
            var correctedDistance = hitDistance * Math.Max(0.2f, Vector2.Dot(ray, forward));
            var wallHeight = Math.Min(viewport.Height * 0.92f, viewport.Height / Math.Max(0.28f, correctedDistance * 0.72f));
            var wallTop = ceilingRect.Bottom - (wallHeight * 0.42f);
            var wallBottom = wallTop + wallHeight;
            var x = viewport.X + (column * columnWidth);
            var wallColor = hitSymbol == 'x'
                ? new Color(96, 80, 68)
                : new Color(82, 80, 86);
            var shade = Math.Clamp(1.0f - (correctedDistance / 9.0f), 0.34f, 1.0f);
            wallColor = new Color(
                (byte)(wallColor.R * shade),
                (byte)(wallColor.G * shade),
                (byte)(wallColor.B * shade));

            DrawPanel(x, wallTop, columnWidth + 1.0f, wallHeight, wallColor);
            if (column % 6 == 0)
            {
                DrawPanel(x, wallTop, 1.0f, wallHeight, new Color(32, 32, 38, 86));
            }
            if (((int)(wallTop + wallHeight)) % 26 < 2)
            {
                DrawPanel(x, wallBottom - 2.0f, columnWidth + 1.0f, 2.0f, new Color(26, 26, 32, 86));
            }
        }

        DrawDungeonFeatureSprites(viewport, ceilingRect.Bottom, floorRect.Bottom, player, forward, right, wallDistances, columns);
    }

    private void DrawDungeonFeatureSprites(RectangleF viewport, float horizonY, float floorBottom, Vector2 player, Vector2 forward, Vector2 right, float[] wallDistances, int columns)
    {
        if (dungeon is null)
        {
            return;
        }

        var drawn = new System.Collections.Generic.HashSet<GridPoint>();
        for (var depth = 6; depth >= 1; depth--)
        {
            for (var side = -4; side <= 4; side++)
            {
                var cell = GetRelativeCell(side, depth, isDungeonView: true);
                if (!drawn.Add(cell))
                {
                    continue;
                }

                var symbol = GetCrawlerCellSymbol(cell, true);
                if (symbol is '.' or '#' or 'M' or 'B')
                {
                    continue;
                }

                var cellCenter = new Vector2(cell.X + 0.5f, cell.Y + 0.5f);
                var delta = cellCenter - player;
                var localForward = Vector2.Dot(delta, forward);
                var localSide = Vector2.Dot(delta, right);
                if (localForward <= 0.2f)
                {
                    continue;
                }

                var normalizedSide = localSide / Math.Max(0.16f, localForward * 0.8f);
                if (MathF.Abs(normalizedSide) > 1.1f)
                {
                    continue;
                }

                var screenX = viewport.Center.X + (normalizedSide * viewport.Width * 0.5f);
                var columnIndex = Math.Clamp((int)(((screenX - viewport.X) / viewport.Width) * columns), 0, columns - 1);
                if (localForward >= wallDistances[columnIndex] - 0.18f)
                {
                    continue;
                }

                var scale = Math.Clamp(1.0f / localForward, 0.18f, 1.0f);
                var height = 42.0f + (viewport.Height * 0.26f * scale);
                var width = 32.0f + (viewport.Width * 0.08f * scale);
                var depthT = Math.Clamp((localForward - 0.45f) / 5.55f, 0.0f, 1.0f);
                var floorPerspective = 1.0f - MathF.Sqrt(depthT);
                var baseY = horizonY + ((floorBottom - horizonY) * floorPerspective) - 2.0f;
                var dest = new RectangleF(screenX - (width * 0.5f), baseY - height, width, height);
                DrawDungeonProjectedFeature(symbol, dest, depth);
            }
        }
    }

    private void DrawOverworldTerrainSpans(RectangleF viewport, float horizonY, RectangleF ground, float[] laneOffsets, float[] baseYs)
    {
        var cellTopWidths = new[] { 16.0f, 26.0f, 42.0f, 66.0f };
        var cellBottomWidths = new[] { 28.0f, 44.0f, 68.0f, 102.0f };
        var bandTops = new[] { horizonY + 8.0f, ground.Y + 30.0f, ground.Y + 78.0f, ground.Y + 146.0f };
        var bandBottoms = new[] { ground.Y + 56.0f, ground.Y + 118.0f, ground.Y + 206.0f, ground.Bottom - 6.0f };

        for (var depth = 1; depth <= 4; depth++)
        {
            var sideSymbols = new char[5];
            for (var i = 0; i < 5; i++)
            {
                sideSymbols[i] = GetCrawlerCellSymbol(GetRelativeCell(i - 2, depth, isDungeonView: false), isDungeonView: false);
            }

            var waterCount = sideSymbols.Count(c => c == '~');
            var centerSymbol = sideSymbols[2];

            if (waterCount >= 3)
            {
                DrawTrapezoid(viewport.X, viewport.Right, viewport.X, viewport.Right, bandTops[depth - 1], bandBottoms[depth - 1], new Color(74, 106, 146, 220));
                DrawTrapezoid(viewport.X + 12.0f, viewport.Right - 12.0f, viewport.X + 24.0f, viewport.Right - 24.0f, bandTops[depth - 1] + ((bandBottoms[depth - 1] - bandTops[depth - 1]) * 0.18f), bandTops[depth - 1] + ((bandBottoms[depth - 1] - bandTops[depth - 1]) * 0.32f), new Color(184, 210, 228, 76));
                continue;
            }

            if (centerSymbol is '=' or 'P')
            {
                DrawOverworldSpanGroups(viewport, depth, laneOffsets[depth - 1], bandTops[depth - 1], bandBottoms[depth - 1], cellTopWidths[depth - 1] * 1.8f, cellBottomWidths[depth - 1] * 2.15f, new[] {'.','.',centerSymbol,'.','.'}, '=','P');
            }

            DrawOverworldSpanGroups(viewport, depth, laneOffsets[depth - 1], bandTops[depth - 1], bandBottoms[depth - 1], cellTopWidths[depth - 1], cellBottomWidths[depth - 1], sideSymbols, '~');
        }

        DrawPanel(viewport.Center.X - 2.0f, horizonY + 18.0f, 4.0f, ground.Bottom - horizonY - 24.0f, new Color(166, 148, 116, 56));
    }

    private void DrawOverworldSpanGroups(RectangleF viewport, int depth, float laneOffset, float bandTop, float bandBottom, float cellTopWidth, float cellBottomWidth, char[] sideSymbols, params char[] matchSymbols)
    {
        var side = -2;
        while (side <= 2)
        {
            if (Array.IndexOf(matchSymbols, sideSymbols[side + 2]) < 0)
            {
                side++;
                continue;
            }

            var start = side;
            while (side <= 2 && Array.IndexOf(matchSymbols, sideSymbols[side + 2]) >= 0)
            {
                side++;
            }

            var end = side - 1;
            var topOffset = laneOffset * 0.56f;
            var topLeft = viewport.Center.X + (start * topOffset) - (cellTopWidth * 0.5f);
            var topRight = viewport.Center.X + (end * topOffset) + (cellTopWidth * 0.5f);
            var bottomLeft = viewport.Center.X + (start * laneOffset) - (cellBottomWidth * 0.5f);
            var bottomRight = viewport.Center.X + (end * laneOffset) + (cellBottomWidth * 0.5f);
            if (matchSymbols[0] == '~')
            {
                DrawTrapezoid(topLeft, topRight, bottomLeft, bottomRight, bandTop, bandBottom, new Color(74, 106, 146, 220));
                DrawTrapezoid(topLeft + 4.0f, topRight - 4.0f, bottomLeft + 8.0f, bottomRight - 8.0f, bandTop + ((bandBottom - bandTop) * 0.18f), bandTop + ((bandBottom - bandTop) * 0.32f), new Color(184, 210, 228, 76));
            }
            else
            {
                DrawTrapezoid(topLeft, topRight, bottomLeft, bottomRight, bandTop, bandBottom, new Color(124, 98, 72, 214));
                DrawTrapezoid(topLeft, topRight, bottomLeft, bottomRight, bandTop, bandTop + 1.0f, new Color(88, 70, 48));
            }
        }
    }

    private void DrawOverworldSideWaterFields(RectangleF viewport, float horizonY, float groundBottom)
    {
        var leftWater = 0;
        var rightWater = 0;
        for (var depth = 1; depth <= 4; depth++)
        {
            for (var side = -2; side <= -1; side++)
            {
                if (GetCrawlerCellSymbol(GetRelativeCell(side, depth, isDungeonView: false), isDungeonView: false) == '~')
                {
                    leftWater++;
                }
            }

            for (var side = 1; side <= 2; side++)
            {
                if (GetCrawlerCellSymbol(GetRelativeCell(side, depth, isDungeonView: false), isDungeonView: false) == '~')
                {
                    rightWater++;
                }
            }
        }

        if (leftWater >= 4)
        {
            DrawTrapezoid(viewport.X, viewport.Center.X - 38.0f, viewport.X - 18.0f, viewport.Center.X - 126.0f, horizonY + 8.0f, groundBottom - 12.0f, new Color(74, 106, 146, 196));
            DrawTrapezoid(viewport.X + 12.0f, viewport.Center.X - 62.0f, viewport.X + 28.0f, viewport.Center.X - 150.0f, horizonY + 46.0f, horizonY + 88.0f, new Color(184, 210, 228, 58));
        }

        if (rightWater >= 4)
        {
            DrawTrapezoid(viewport.Center.X + 38.0f, viewport.Right, viewport.Center.X + 126.0f, viewport.Right + 18.0f, horizonY + 8.0f, groundBottom - 12.0f, new Color(74, 106, 146, 196));
            DrawTrapezoid(viewport.Center.X + 62.0f, viewport.Right - 12.0f, viewport.Center.X + 150.0f, viewport.Right - 28.0f, horizonY + 46.0f, horizonY + 88.0f, new Color(184, 210, 228, 58));
        }
    }

    private void DrawTrapezoid(float topLeft, float topRight, float bottomLeft, float bottomRight, float topY, float bottomY, Color color)
    {
        var height = Math.Max(1.0f, bottomY - topY);
        var steps = Math.Max(1, (int)MathF.Ceiling(height));
        for (var i = 0; i < steps; i++)
        {
            var t = i / Math.Max(1.0f, steps - 1.0f);
            var y = topY + (t * height);
            var left = topLeft + ((bottomLeft - topLeft) * t);
            var right = topRight + ((bottomRight - topRight) * t);
            DrawPanel(left, y, Math.Max(1.0f, right - left), 1.4f, color);
        }
    }

    private void DrawOverworldBillboardFromFootprint(RectangleF viewport, char symbol, float centerX, float groundBottom, float footprintWidth, float footprintHeight, float verticalLift, int variantSeed = 0)
    {
        if (symbol is '.' or '=' or 'P' or '~')
        {
            return;
        }

        var width = Math.Max(18.0f, footprintWidth * 1.24f);
        var height = Math.Max(26.0f, footprintHeight * 2.65f);
        var rect = new RectangleF(centerX - (width * 0.5f), (groundBottom + verticalLift) - height, width, height);

        if (rect.Right < viewport.X || rect.X > viewport.Right || rect.Bottom < viewport.Y || rect.Y > viewport.Bottom)
        {
            return;
        }

        switch (symbol)
        {
            case '*':
                DrawTreeSilhouette(rect, variantSeed);
                break;
            case '^':
                DrawMountainSilhouette(rect);
                break;
            case 'F':
                DrawFenSilhouette(rect);
                break;
            case 'T':
                DrawTownGateSilhouette(rect);
                break;
            case 'H':
                DrawHarborSilhouette(rect);
                break;
            case 'K':
                DrawKeepSilhouette(rect);
                break;
            case 'R':
                DrawRuinSilhouette(rect);
                break;
            case 'S':
                DrawShrineSilhouette(rect);
                break;
            case 'C':
                DrawCampSilhouette(rect);
                break;
            case 'D':
                DrawDungeonMouthSilhouette(rect);
                break;
        }
    }

    private void DrawDungeonPortraitStrip(RectangleF view)
    {
        var top = view.Y + 472.0f;
        for (var i = 0; i < party.Members.Count; i++)
        {
            var member = party.Members[i];
            var card = new RectangleF(view.X + 206 + (i * 112), top, 96, 96);
            var portraitIndex = i switch
            {
                0 => 1,
                1 => 0,
                _ => i,
            };
            var source = new RectangleF(portraitIndex * 128, 0, 90, 90);
            var portraitRect = new RectangleF(card.X + 3, card.Y - 3, 90, 90);
            DrawPanel(card.X, card.Y, card.Width, card.Height, new Color(16, 18, 22, 220));
            DrawFrame(card, new Color(92, 86, 74), 2);
            spriteBatch.Draw(grimPortraitTexture, UiRect(portraitRect), source, Color.White, 0, Vector2.Zero);
            var missingRatio = 1.0f - (member.Health / (float)Math.Max(1, member.MaxHealth));
            if (missingRatio > 0.0f)
            {
                var overlayHeight = portraitRect.Height * missingRatio;
                var overlayRect = new RectangleF(portraitRect.X, portraitRect.Bottom - overlayHeight, portraitRect.Width, overlayHeight);
                DrawPanel(overlayRect.X, overlayRect.Y, overlayRect.Width, overlayRect.Height, new Color(168, 28, 28, 108));
            }
            DrawPanel(card.X + 3, card.Bottom - 17, 90, 12, new Color(8, 10, 14, 200));
            DrawText($"{member.Name} {member.Health}/{member.MaxHealth}", new Vector2(card.X + 6, card.Bottom - 16), member.IsAlive ? new Color(228, 228, 236) : new Color(122, 122, 132), 1);
        }
    }

    private void DrawCrawlerFeatureBillboard(char symbol, RectangleF portal, int depth, bool isDungeonView)
    {
        if (symbol is '.' or '#' or '~' or '^')
        {
            return;
        }

        var scale = 1.0f - (depth * 0.16f);
        var width = portal.Width * MathF.Max(0.56f, 0.88f * scale);
        var height = portal.Height * MathF.Max(0.64f, 0.98f * scale);
        var floorBias = isDungeonView && symbol is '<' or '>' or 'G' ? portal.Height * 0.18f : 0.0f;
        var dest = new RectangleF(portal.Center.X - (width * 0.5f), portal.Bottom + floorBias - height - 2.0f, width, height);

        if (symbol is 'M' or 'B' || (!isDungeonView && symbol is '*'))
        {
            var row = symbol switch
            {
                'B' => 2,
                '*' => 1,
                _ => 0,
            };
            var frame = ((int)(totalTime * 4.0f)) % 2;
            var source = new RectangleF(frame * 128, row * 128, 128, 128);
            spriteBatch.Draw(grimCreatureTexture, UiRect(dest), source, Color.White, 0, Vector2.Zero);
            return;
        }

        if (isDungeonView)
        {
            DrawDungeonProjectedFeature(symbol, dest, depth);
            return;
        }

        if (!isDungeonView && symbol is '=' or 'P')
        {
            var roadRect = new RectangleF(portal.Center.X - (portal.Width * 0.12f), portal.Bottom - (portal.Height * 0.32f), portal.Width * 0.24f, portal.Height * 0.28f);
            DrawPanel(roadRect.X, roadRect.Y, roadRect.Width, roadRect.Height, new Color(118, 92, 66, 180));
            DrawFrame(roadRect, new Color(86, 68, 44), 1);
            return;
        }

        var color = GetCrawlerFeatureColor(symbol, isDungeonView);
        var label = GetCrawlerFeatureLabel(symbol, isDungeonView);
        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        DrawPanel(dest.X, dest.Y, dest.Width, dest.Height, new Color(0, 0, 0, 60));
        DrawFrame(dest, new Color(42, 38, 34), 1);
        DrawText(label, new Vector2(dest.X + 10, dest.Center.Y - 6), color, 2);
    }

    private void DrawDungeonWallSurface(RectangleF rect, char symbol, int depth, bool frontFace)
    {
        var baseColor = symbol == 'x' ? new Color(88, 72, 64) : new Color(78, 76, 82);
        var midColor = symbol == 'x' ? new Color(104, 86, 74) : new Color(92, 90, 96);
        var mortar = new Color(42, 40, 46);
        DrawPanel(rect.X, rect.Y, rect.Width, rect.Height, baseColor);

        var rows = Math.Max(3, (int)(rect.Height / 26.0f));
        var rowHeight = rect.Height / rows;
        for (var row = 0; row < rows; row++)
        {
            var y = rect.Y + (row * rowHeight);
            DrawPanel(rect.X, y, rect.Width, 2.0f, mortar);
            var brickOffset = row % 2 == 0 ? 0.0f : rect.Width * 0.12f;
            var columns = Math.Max(2, (int)(rect.Width / 36.0f));
            var brickWidth = rect.Width / columns;
            for (var col = 0; col < columns; col++)
            {
                var x = rect.X + (col * brickWidth) + brickOffset;
                if (x >= rect.Right - 3.0f)
                {
                    break;
                }

                DrawPanel(x, y + 2.0f, Math.Min(brickWidth - 2.0f, rect.Right - x), Math.Max(4.0f, rowHeight - 3.0f), (col + row + depth) % 2 == 0 ? midColor : baseColor);
            }
        }

        DrawPanel(rect.X, rect.Y, rect.Width, Math.Max(4.0f, rect.Height * 0.08f), new Color(110, 106, 102, 90));
        DrawPanel(rect.X, rect.Bottom - Math.Max(8.0f, rect.Height * 0.14f), rect.Width, Math.Max(8.0f, rect.Height * 0.14f), new Color(22, 22, 28, 84));
        DrawPanel(rect.X, rect.Y, Math.Max(6.0f, rect.Width * 0.06f), rect.Height, new Color(26, 26, 32, frontFace ? 72 : 48));
        DrawPanel(rect.Right - Math.Max(6.0f, rect.Width * 0.06f), rect.Y, Math.Max(6.0f, rect.Width * 0.06f), rect.Height, new Color(18, 18, 24, frontFace ? 88 : 60));

        if (symbol == 'x')
        {
            var gateWidth = rect.Width * 0.34f;
            var gateHeight = rect.Height * 0.56f;
            var gate = new RectangleF(rect.Center.X - (gateWidth * 0.5f), rect.Bottom - gateHeight, gateWidth, gateHeight);
            DrawPanel(gate.X, gate.Y, gate.Width, gate.Height, new Color(58, 46, 42));
            for (var i = 1; i <= 3; i++)
            {
                var x = gate.X + (gate.Width * i / 4.0f);
                DrawPanel(x - 1.0f, gate.Y + 2.0f, 2.0f, gate.Height - 4.0f, new Color(118, 108, 94));
            }
            DrawPanel(gate.X + 2.0f, gate.Y + (gate.Height * 0.28f), gate.Width - 4.0f, 2.0f, new Color(118, 108, 94));
            DrawPanel(gate.X + 2.0f, gate.Y + (gate.Height * 0.62f), gate.Width - 4.0f, 2.0f, new Color(118, 108, 94));
        }
    }

    private void DrawDungeonSideWall(RectangleF outerPortal, RectangleF innerPortal, char symbol, int depth, bool leftSide)
    {
        var baseColor = symbol == 'x' ? new Color(82, 68, 60) : new Color(74, 72, 78);
        var midColor = symbol == 'x' ? new Color(98, 82, 70) : new Color(90, 88, 94);
        var shadowColor = new Color(24, 24, 30, 110);
        var lightColor = new Color(118, 112, 106, 68);

        var outerX = leftSide ? outerPortal.X : outerPortal.Right;
        var innerX = leftSide ? innerPortal.X + 12.0f : innerPortal.Right - 12.0f;
        var topOuterX = outerX;
        var topInnerX = innerX;
        var bottomOuterX = outerX;
        var bottomInnerX = innerX;

        if (leftSide)
        {
            DrawTrapezoid(topOuterX, topInnerX, bottomOuterX, bottomInnerX, outerPortal.Y, outerPortal.Bottom, baseColor);
            DrawTrapezoid(topOuterX + 2.0f, topInnerX - 2.0f, bottomOuterX + 2.0f, bottomInnerX - 2.0f, outerPortal.Y + 8.0f, outerPortal.Bottom - 8.0f, midColor);
            DrawTrapezoid(topInnerX - 8.0f, topInnerX - 2.0f, bottomInnerX - 8.0f, bottomInnerX - 2.0f, innerPortal.Y, innerPortal.Bottom, shadowColor);
            DrawTrapezoid(topOuterX, topOuterX + 6.0f, bottomOuterX, bottomOuterX + 6.0f, outerPortal.Y, outerPortal.Bottom, lightColor);
        }
        else
        {
            DrawTrapezoid(topInnerX, topOuterX, bottomInnerX, bottomOuterX, outerPortal.Y, outerPortal.Bottom, baseColor);
            DrawTrapezoid(topInnerX + 2.0f, topOuterX - 2.0f, bottomInnerX + 2.0f, bottomOuterX - 2.0f, outerPortal.Y + 8.0f, outerPortal.Bottom - 8.0f, midColor);
            DrawTrapezoid(topInnerX + 2.0f, topInnerX + 8.0f, bottomInnerX + 2.0f, bottomInnerX + 8.0f, innerPortal.Y, innerPortal.Bottom, shadowColor);
            DrawTrapezoid(topOuterX - 6.0f, topOuterX, bottomOuterX - 6.0f, bottomOuterX, outerPortal.Y, outerPortal.Bottom, lightColor);
        }

        var mortarLines = 6;
        for (var i = 1; i < mortarLines; i++)
        {
            var y = outerPortal.Y + ((outerPortal.Height * i) / mortarLines);
            var t = (y - outerPortal.Y) / Math.Max(1.0f, outerPortal.Height);
            var left = leftSide ? outerX : innerX + ((outerX - innerX) * t);
            var right = leftSide ? innerX + ((outerX - innerX) * t) : outerX;
            DrawPanel(Math.Min(left, right), y, Math.Abs(right - left), 1.5f, new Color(32, 32, 38, 86));
        }
    }

    private void DrawDungeonBoundaryWalls(RectangleF viewport, RectangleF[] portals)
    {
        var leftDepth = 0;
        var rightDepth = 0;
        for (var depth = 1; depth <= portals.Length; depth++)
        {
            if (IsBlockedCrawlerSymbol(GetCrawlerCellSymbol(GetRelativeCell(-1, depth, isDungeonView: true), isDungeonView: true), isDungeonView: true))
            {
                leftDepth = depth;
            }
            else
            {
                break;
            }
        }

        for (var depth = 1; depth <= portals.Length; depth++)
        {
            if (IsBlockedCrawlerSymbol(GetCrawlerCellSymbol(GetRelativeCell(1, depth, isDungeonView: true), isDungeonView: true), isDungeonView: true))
            {
                rightDepth = depth;
            }
            else
            {
                break;
            }
        }

        if (leftDepth > 0)
        {
            var near = portals[0];
            var far = portals[Math.Clamp(leftDepth - 1, 0, portals.Length - 1)];
            DrawDungeonContinuousBoundaryWall(viewport, near, far, true);
        }

        if (rightDepth > 0)
        {
            var near = portals[0];
            var far = portals[Math.Clamp(rightDepth - 1, 0, portals.Length - 1)];
            DrawDungeonContinuousBoundaryWall(viewport, near, far, false);
        }
    }

    private void DrawDungeonContinuousBoundaryWall(RectangleF viewport, RectangleF nearPortal, RectangleF farPortal, bool leftSide)
    {
        var baseColor = new Color(74, 72, 78);
        var midColor = new Color(90, 88, 94);
        var darkColor = new Color(24, 24, 30, 110);
        var lightColor = new Color(118, 112, 106, 62);

        var bottomInner = leftSide ? nearPortal.X : nearPortal.Right;
        var topInner = leftSide ? farPortal.X : farPortal.Right;
        var outer = leftSide ? viewport.X : viewport.Right;

        if (leftSide)
        {
            DrawTrapezoid(outer, topInner, outer, bottomInner, viewport.Y + 38.0f, viewport.Bottom - 70.0f, baseColor);
            DrawTrapezoid(outer + 2.0f, topInner - 10.0f, outer + 2.0f, bottomInner - 10.0f, viewport.Y + 46.0f, viewport.Bottom - 78.0f, midColor);
            DrawTrapezoid(topInner - 12.0f, topInner - 2.0f, bottomInner - 12.0f, bottomInner - 2.0f, viewport.Y + 42.0f, viewport.Bottom - 72.0f, darkColor);
            DrawTrapezoid(outer, outer + 6.0f, outer, outer + 6.0f, viewport.Y + 38.0f, viewport.Bottom - 70.0f, lightColor);
        }
        else
        {
            DrawTrapezoid(topInner, outer, bottomInner, outer, viewport.Y + 38.0f, viewport.Bottom - 70.0f, baseColor);
            DrawTrapezoid(topInner + 10.0f, outer - 2.0f, bottomInner + 10.0f, outer - 2.0f, viewport.Y + 46.0f, viewport.Bottom - 78.0f, midColor);
            DrawTrapezoid(topInner + 2.0f, topInner + 12.0f, bottomInner + 2.0f, bottomInner + 12.0f, viewport.Y + 42.0f, viewport.Bottom - 72.0f, darkColor);
            DrawTrapezoid(outer - 6.0f, outer, outer - 6.0f, outer, viewport.Y + 38.0f, viewport.Bottom - 70.0f, lightColor);
        }

        for (var i = 1; i <= 6; i++)
        {
            var y = viewport.Y + 38.0f + (((viewport.Height - 108.0f) * i) / 7.0f);
            var t = (y - (viewport.Y + 38.0f)) / Math.Max(1.0f, viewport.Height - 108.0f);
            var inner = topInner + ((bottomInner - topInner) * t);
            var left = leftSide ? outer : inner;
            var right = leftSide ? inner : outer;
            DrawPanel(Math.Min(left, right), y, Math.Abs(right - left), 1.5f, new Color(34, 34, 40, 88));
        }
    }

    private void DrawDungeonProjectedFeature(char symbol, RectangleF dest, int depth)
    {
        var floorY = dest.Bottom - 2.0f;
        var shadow = new RectangleF(dest.X + (dest.Width * 0.14f), floorY - 4.0f, dest.Width * 0.72f, 4.0f);
        DrawPanel(shadow.X, shadow.Y, shadow.Width, shadow.Height, new Color(0, 0, 0, 56));

        switch (symbol)
        {
            case '<':
                for (var i = 0; i < 4; i++)
                {
                    var stepHeight = dest.Height * 0.08f;
                    var stepY = floorY - ((i + 1) * stepHeight) - (i * (dest.Height * 0.015f));
                    var step = new RectangleF(dest.X + (dest.Width * (0.18f + (i * 0.08f))), stepY, dest.Width * (0.64f - (i * 0.1f)), stepHeight);
                    DrawPanel(step.X, step.Y, step.Width, step.Height, new Color(156, 146, 122));
                }
                DrawPanel(dest.Center.X - (dest.Width * 0.03f), floorY - (dest.Height * 0.44f), dest.Width * 0.06f, dest.Height * 0.22f, new Color(190, 180, 154));
                break;
            case '>':
                for (var i = 0; i < 4; i++)
                {
                    var stepHeight = dest.Height * 0.08f;
                    var stepY = floorY - (dest.Height * 0.36f) + (i * (dest.Height * 0.055f));
                    var step = new RectangleF(dest.X + (dest.Width * (0.18f + (i * 0.08f))), stepY, dest.Width * (0.64f - (i * 0.1f)), stepHeight);
                    DrawPanel(step.X, step.Y, step.Width, step.Height, new Color(134, 126, 108));
                }
                DrawPanel(dest.Center.X - (dest.Width * 0.03f), floorY - (dest.Height * 0.28f), dest.Width * 0.06f, dest.Height * 0.18f, new Color(172, 162, 136));
                break;
            case 'G':
                var chestBody = new RectangleF(dest.X + (dest.Width * 0.18f), floorY - (dest.Height * 0.14f), dest.Width * 0.64f, dest.Height * 0.12f);
                var chestLid = new RectangleF(dest.X + (dest.Width * 0.22f), chestBody.Y - (dest.Height * 0.09f), dest.Width * 0.56f, dest.Height * 0.09f);
                DrawPanel(chestBody.X, chestBody.Y, chestBody.Width, chestBody.Height, new Color(138, 96, 42));
                DrawPanel(chestLid.X, chestLid.Y, chestLid.Width, chestLid.Height, new Color(172, 126, 58));
                DrawFrame(chestBody, new Color(76, 52, 24), 1);
                DrawPanel(chestBody.Center.X - 2.0f, chestBody.Y + (chestBody.Height * 0.08f), 4.0f, chestBody.Height * 0.72f, new Color(224, 192, 92));
                break;
            case 'L':
                DrawPanel(dest.X + (dest.Width * 0.24f), dest.Bottom - (dest.Height * 0.28f), dest.Width * 0.52f, dest.Height * 0.16f, new Color(84, 108, 118));
                DrawPanel(dest.X + (dest.Width * 0.3f), dest.Bottom - (dest.Height * 0.5f), dest.Width * 0.4f, dest.Height * 0.22f, new Color(112, 148, 162));
                DrawPanel(dest.X + (dest.Width * 0.36f), dest.Bottom - (dest.Height * 0.44f), dest.Width * 0.28f, dest.Height * 0.1f, new Color(196, 226, 236));
                DrawPanel(dest.Center.X - 2.0f, dest.Y + (dest.Height * 0.16f), 4.0f, dest.Height * 0.18f, new Color(180, 206, 214, 170));
                break;
            case 'k':
                DrawPanel(dest.X + (dest.Width * 0.36f), dest.Center.Y - 2.0f, dest.Width * 0.28f, 4.0f, new Color(196, 170, 92));
                DrawPanel(dest.X + (dest.Width * 0.58f), dest.Center.Y - (dest.Height * 0.12f), 4.0f, dest.Height * 0.24f, new Color(196, 170, 92));
                DrawPanel(dest.X + (dest.Width * 0.22f), dest.Center.Y - (dest.Height * 0.1f), dest.Width * 0.16f, dest.Height * 0.14f, new Color(196, 170, 92));
                DrawPanel(dest.X + (dest.Width * 0.26f), dest.Center.Y - (dest.Height * 0.05f), dest.Width * 0.04f, dest.Height * 0.04f, new Color(24, 24, 28));
                break;
            case 'x':
                DrawPanel(dest.X + (dest.Width * 0.22f), dest.Bottom - (dest.Height * 0.46f), dest.Width * 0.56f, dest.Height * 0.34f, new Color(64, 52, 46));
                for (var i = 1; i <= 3; i++)
                {
                    var x = dest.X + (dest.Width * (0.22f + (i * 0.14f)));
                    DrawPanel(x, dest.Bottom - (dest.Height * 0.44f), 3.0f, dest.Height * 0.3f, new Color(126, 114, 98));
                }
                DrawPanel(dest.X + (dest.Width * 0.24f), dest.Bottom - (dest.Height * 0.36f), dest.Width * 0.52f, 3.0f, new Color(126, 114, 98));
                break;
        }
    }

    private GridPoint GetRelativeCell(int side, int forward, bool isDungeonView)
    {
        var origin = isDungeonView ? dungeonCell : playerCell;
        var forwardVector = facing switch
        {
            Direction.Up => new GridPoint(0, -1),
            Direction.Down => new GridPoint(0, 1),
            Direction.Left => new GridPoint(-1, 0),
            _ => new GridPoint(1, 0),
        };
        var rightVector = facing switch
        {
            Direction.Up => new GridPoint(1, 0),
            Direction.Down => new GridPoint(-1, 0),
            Direction.Left => new GridPoint(0, -1),
            _ => new GridPoint(0, 1),
        };

        return new GridPoint(
            origin.X + (forwardVector.X * forward) + (rightVector.X * side),
            origin.Y + (forwardVector.Y * forward) + (rightVector.Y * side));
    }

    private char GetCrawlerCellSymbol(GridPoint point, bool isDungeonView)
    {
        if (isDungeonView)
        {
            if (dungeon is null || point.X < 0 || point.Y < 0 || point.X >= dungeon.Width || point.Y >= dungeon.Height)
            {
                return '#';
            }

            return dungeon.GetTile(point);
        }

        if (point.X < 0 || point.Y < 0 || point.Y >= map.Rows.Count || point.X >= map.Rows[point.Y].Length)
        {
            return '^';
        }

        return map.Rows[point.Y][point.X];
    }

    private bool IsBlockedCrawlerSymbol(char symbol, bool isDungeonView) => !GetTileDefinition(symbol).Walkable;

    private string GetCrawlerFeatureLabel(char symbol, bool isDungeonView) => symbol switch
    {
        '<' => "UP",
        '>' => "DOWN",
        'G' => "CHEST",
        'L' => "FOUNTAIN",
        'k' => "KEY",
        'x' => "GATE",
        '*' when !isDungeonView => "TREES",
        '=' or 'P' when !isDungeonView => string.Empty,
        'F' when !isDungeonView => "FEN",
        'T' when !isDungeonView => "TOWN",
        'K' when !isDungeonView => "KEEP",
        'R' when !isDungeonView => "RUINS",
        'S' when !isDungeonView => "SHRINE",
        'H' when !isDungeonView => "HARBOR",
        'C' when !isDungeonView => "CAMP",
        'D' when !isDungeonView => "DUNGEON",
        _ => "ALTAR",
    };

    private Color GetCrawlerFeatureColor(char symbol, bool isDungeonView) => symbol switch
    {
        '<' => new Color(214, 198, 154),
        '>' => new Color(184, 168, 136),
        'G' => new Color(194, 152, 88),
        'L' => new Color(112, 150, 170),
        'k' => new Color(186, 160, 92),
        'x' => new Color(110, 94, 82),
        '*' when !isDungeonView => new Color(118, 166, 110),
        'F' when !isDungeonView => new Color(134, 160, 118),
        'T' or 'H' when !isDungeonView => new Color(210, 190, 144),
        'K' when !isDungeonView => new Color(188, 194, 208),
        'R' when !isDungeonView => new Color(174, 142, 138),
        'S' when !isDungeonView => new Color(170, 150, 194),
        'C' when !isDungeonView => new Color(188, 142, 112),
        'D' when !isDungeonView => new Color(176, 124, 118),
        _ => new Color(160, 122, 96),
    };

    private Color GetCrawlerWallTint(char symbol, bool isDungeonView) => symbol switch
    {
        '~' when !isDungeonView => new Color(78, 100, 126),
        '^' when !isDungeonView => new Color(86, 88, 94),
        _ when !isDungeonView => new Color(122, 114, 98),
        'x' => new Color(92, 80, 72),
        _ => new Color(86, 84, 90),
    };

    private Color GetOverworldInsetColor(char symbol) => symbol switch
    {
        '~' => new Color(54, 80, 110),
        '^' => new Color(74, 78, 84),
        '*' => new Color(62, 92, 62),
        'F' => new Color(78, 96, 74),
        '=' or 'P' => new Color(116, 92, 68),
        'T' or 'H' => new Color(140, 128, 92),
        'K' => new Color(122, 126, 140),
        'R' => new Color(120, 90, 84),
        'S' => new Color(118, 102, 138),
        'C' => new Color(126, 88, 70),
        'D' => new Color(132, 76, 72),
        _ => new Color(72, 88, 60),
    };

    private Color GetDungeonMinimapColor(char symbol) => symbol switch
    {
        '#' => new Color(58, 60, 66),
        '<' or '>' => new Color(120, 114, 96),
        'G' => new Color(126, 102, 62),
        'L' => new Color(76, 102, 118),
        'k' => new Color(144, 124, 70),
        'x' => new Color(82, 70, 64),
        'M' => new Color(120, 66, 66),
        'B' => new Color(156, 72, 72),
        _ => new Color(74, 80, 88),
    };

    private Color GetMinimapCellColor(char symbol, bool isDungeonView) => isDungeonView
        ? GetDungeonMinimapColor(symbol)
        : GetOverworldInsetColor(symbol);

    private static bool ShouldDrawMinimapIsoIcon(char symbol, bool isDungeonView) => isDungeonView
        ? symbol is not '.' and not '#'
        : symbol is not '.' and not '~' and not '^';

    private void TurnLeft()
    {
        facing = facing switch
        {
            Direction.Up => Direction.Left,
            Direction.Left => Direction.Down,
            Direction.Down => Direction.Right,
            _ => Direction.Up,
        };
    }

    private void TurnRight()
    {
        facing = facing switch
        {
            Direction.Up => Direction.Right,
            Direction.Right => Direction.Down,
            Direction.Down => Direction.Left,
            _ => Direction.Up,
        };
    }

    private GridPoint GetForwardDelta() => facing switch
    {
        Direction.Up => new GridPoint(0, -1),
        Direction.Down => new GridPoint(0, 1),
        Direction.Left => new GridPoint(-1, 0),
        _ => new GridPoint(1, 0),
    };

    private GridPoint GetRightDelta() => facing switch
    {
        Direction.Up => new GridPoint(1, 0),
        Direction.Down => new GridPoint(-1, 0),
        Direction.Left => new GridPoint(0, -1),
        _ => new GridPoint(0, 1),
    };

    private GridPoint GetBackwardDelta()
    {
        var forward = GetForwardDelta();
        return new GridPoint(-forward.X, -forward.Y);
    }

    private static string GetFacingLabelForDelta(GridPoint delta) => delta switch
    {
        { X: -1, Y: 0 } => "west",
        { X: 1, Y: 0 } => "east",
        { X: 0, Y: -1 } => "north",
        _ => "south",
    };

    private void DrawCloud(RectangleF rect)
    {
        var white = new Color(244, 248, 255, 210);
        DrawPanel(rect.X + (rect.Width * 0.12f), rect.Y + (rect.Height * 0.34f), rect.Width * 0.72f, rect.Height * 0.34f, white);
        DrawPanel(rect.X, rect.Y + (rect.Height * 0.4f), rect.Width * 0.28f, rect.Height * 0.22f, white);
        DrawPanel(rect.X + (rect.Width * 0.2f), rect.Y + (rect.Height * 0.12f), rect.Width * 0.28f, rect.Height * 0.32f, white);
        DrawPanel(rect.X + (rect.Width * 0.46f), rect.Y, rect.Width * 0.26f, rect.Height * 0.34f, white);
        DrawPanel(rect.X + (rect.Width * 0.68f), rect.Y + (rect.Height * 0.18f), rect.Width * 0.22f, rect.Height * 0.26f, white);
    }

    private void DrawDungeonStoneFill(char symbol, RectangleF tileDestination)
    {
        if (GetTileDefinition(symbol).Walkable)
        {
            return;
        }

        var stones = new (float x, float y, float w, float h, Color color)[]
        {
            (14, 13, 11, 7, new Color(76, 72, 68, 244)),
            (26, 18, 14, 9, new Color(58, 54, 50, 246)),
            (38, 14, 10, 8, new Color(88, 82, 78, 244)),
            (21, 28, 12, 8, new Color(48, 46, 44, 248)),
            (35, 30, 15, 9, new Color(68, 64, 60, 246)),
        };

        foreach (var stone in stones)
        {
            DrawPanel(tileDestination.X + stone.x, tileDestination.Y + stone.y, stone.w, stone.h, stone.color);
        }
    }

    private void DrawDungeonFeatureMarker(char symbol, RectangleF tileDestination)
    {
        if (symbol is not ('<' or '>' or 'G' or 'L' or 'k' or 'x' or 'M' or 'B'))
        {
            return;
        }

        var color = symbol switch
        {
            '<' => new Color(210, 198, 138),
            '>' => new Color(186, 172, 132),
            'G' => new Color(203, 171, 92),
            'L' => new Color(128, 170, 188),
            'k' => new Color(194, 168, 104),
            'x' => new Color(151, 116, 92),
            'B' => new Color(176, 86, 86),
            _ => new Color(188, 122, 102),
        };

        var phase = totalTime * 3.2f + (tileDestination.X * 0.01f) + (tileDestination.Y * 0.02f);
        var glow = 0.72f + (0.28f * (0.5f + (0.5f * MathF.Sin(phase))));
        var bob = MathF.Sin(phase) * 1.6f;
        var centerX = tileDestination.X + 32.0f;
        var centerY = tileDestination.Y + 24.0f + bob;
        var halo = new Color(
            (byte)Math.Clamp(color.R + 36, 0, 255),
            (byte)Math.Clamp(color.G + 36, 0, 255),
            (byte)Math.Clamp(color.B + 36, 0, 255),
            (byte)(52 + (56 * glow)));

        DrawPanel(centerX - 8.0f, centerY - 8.0f, 16.0f, 16.0f, halo);

        switch (symbol)
        {
            case '<':
                DrawPanel(centerX - 1.0f, centerY - 8.0f, 2.0f, 12.0f, color);
                DrawPanel(centerX - 6.0f, centerY - 4.0f, 12.0f, 2.0f, color);
                DrawPanel(centerX - 5.0f, centerY - 8.0f, 2.0f, 3.0f, color);
                DrawPanel(centerX + 3.0f, centerY - 8.0f, 2.0f, 3.0f, color);
                DrawPanel(centerX - 7.0f, centerY + 5.0f, 14.0f, 2.0f, new Color(104, 92, 58));
                break;
            case '>':
                DrawPanel(centerX - 1.0f, centerY - 6.0f, 2.0f, 12.0f, color);
                DrawPanel(centerX - 6.0f, centerY + 2.0f, 12.0f, 2.0f, color);
                DrawPanel(centerX - 5.0f, centerY + 6.0f, 2.0f, 3.0f, color);
                DrawPanel(centerX + 3.0f, centerY + 6.0f, 2.0f, 3.0f, color);
                DrawPanel(centerX - 7.0f, centerY - 9.0f, 14.0f, 2.0f, new Color(96, 84, 54));
                break;
            case 'G':
                DrawPanel(centerX - 7.0f, centerY - 1.0f, 14.0f, 7.0f, color);
                DrawFrame(new RectangleF(centerX - 7.0f, centerY - 1.0f, 14.0f, 7.0f), new Color(82, 58, 24), 1);
                DrawPanel(centerX - 6.0f, centerY - 6.0f, 12.0f, 5.0f, new Color(153, 116, 52));
                DrawPanel(centerX - 1.0f, centerY - 1.0f, 2.0f, 7.0f, new Color(234, 212, 124));
                break;
            case 'L':
                DrawPanel(centerX - 6.0f, centerY - 3.0f, 12.0f, 8.0f, new Color(86, 116, 132));
                DrawPanel(centerX - 4.0f, centerY - 5.0f, 8.0f, 12.0f, color);
                DrawPanel(centerX - 3.0f, centerY - 2.0f, 6.0f, 6.0f, new Color(220, 238, 246));
                DrawPanel(centerX - 1.0f, centerY - 7.0f, 2.0f, 2.0f, new Color(234, 248, 255, 180));
                break;
            case 'k':
                DrawPanel(centerX - 2.0f, centerY - 1.0f, 8.0f, 2.0f, color);
                DrawPanel(centerX + 5.0f, centerY - 4.0f, 2.0f, 8.0f, color);
                DrawPanel(centerX - 7.0f, centerY - 4.0f, 6.0f, 6.0f, color);
                DrawPanel(centerX - 5.0f, centerY - 2.0f, 2.0f, 2.0f, new Color(22, 26, 34));
                DrawPanel(centerX - 1.0f, centerY + 2.0f, 2.0f, 3.0f, color);
                DrawPanel(centerX + 2.0f, centerY + 2.0f, 2.0f, 2.0f, color);
                break;
            case 'x':
                DrawPanel(centerX - 7.0f, centerY - 6.0f, 14.0f, 2.0f, color);
                DrawPanel(centerX - 7.0f, centerY + 5.0f, 14.0f, 2.0f, color);
                DrawPanel(centerX - 7.0f, centerY - 6.0f, 2.0f, 13.0f, color);
                DrawPanel(centerX + 5.0f, centerY - 6.0f, 2.0f, 13.0f, color);
                DrawPanel(centerX - 4.0f, centerY - 3.0f, 2.0f, 7.0f, new Color(74, 54, 40));
                DrawPanel(centerX, centerY - 3.0f, 2.0f, 7.0f, new Color(74, 54, 40));
                DrawPanel(centerX - 2.0f, centerY - 9.0f, 4.0f, 4.0f, new Color(182, 152, 86));
                break;
            case 'M':
                DrawPanel(centerX - 5.0f, centerY - 5.0f, 4.0f, 4.0f, color);
                DrawPanel(centerX + 1.0f, centerY - 5.0f, 4.0f, 4.0f, color);
                DrawPanel(centerX - 3.0f, centerY + 1.0f, 6.0f, 2.0f, color);
                DrawPanel(centerX - 1.0f, centerY + 3.0f, 2.0f, 3.0f, color);
                break;
            case 'B':
                DrawPanel(centerX - 6.0f, centerY - 4.0f, 12.0f, 8.0f, color);
                DrawPanel(centerX - 3.0f, centerY - 8.0f, 2.0f, 4.0f, new Color(220, 188, 122));
                DrawPanel(centerX - 1.0f, centerY - 10.0f, 2.0f, 6.0f, new Color(240, 206, 132));
                DrawPanel(centerX + 1.0f, centerY - 8.0f, 2.0f, 4.0f, new Color(220, 188, 122));
                DrawPanel(centerX - 2.0f, centerY - 1.0f, 4.0f, 4.0f, new Color(232, 206, 206));
                break;
        }
    }

    private void DrawPartyTrail(RectangleF leaderTile)
    {
        var source = GetPlayerSourceFrameFor(Direction.Down, (int)(totalTime * 8.0f) % 3, 1);
        var sourceRect = new RectangleF(source.X, source.Y, source.Width, source.Height);
        var tints = new[]
        {
            new Color(105, 126, 152),
            new Color(140, 109, 119),
            new Color(143, 129, 92),
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

            spriteBatch.Draw(whiteTexture, UiRect(new RectangleF(destination.X + 10.0f, destination.Y + destination.Height - 5.0f, destination.Width - 20.0f, 3.0f)), new Color(0, 0, 0, 56));
            spriteBatch.Draw(playerTexture, UiRect(destination), sourceRect, tints[i], 0, Vector2.Zero);
        }
    }

    private void DrawTreeSilhouette(RectangleF rect, int variantSeed)
    {
        var seed = variantSeed != 0
            ? variantSeed
            : ((int)(rect.X * 0.19f) ^ ((int)(rect.Y * 0.13f) << 1) ^ ((int)(rect.Width * 0.09f) << 2));
        var clusterCount = 3 + (TreeNoise(seed, 1) > 0.52f ? 1 : 0);
        for (var i = 0; i < clusterCount; i++)
        {
            var position = clusterCount == 1 ? 0.5f : 0.14f + (i * (0.72f / Math.Max(1, clusterCount - 1)));
            position += (TreeNoise(seed, 2 + i) - 0.5f) * 0.12f;
            var width = rect.Width * (0.24f + (TreeNoise(seed, 6 + i) * 0.18f));
            var height = rect.Height * (0.56f + (TreeNoise(seed, 10 + i) * 0.3f));
            var crownBottom = rect.Bottom - (rect.Height * (0.08f + (TreeNoise(seed, 14 + i) * 0.12f)));
            var crownRect = new RectangleF(
                rect.X + (rect.Width * position) - (width * 0.5f),
                crownBottom - height,
                width,
                height);

            var trunkWidth = Math.Max(3.0f, crownRect.Width * 0.14f);
            var trunkTop = crownRect.Y + (crownRect.Height * (0.42f + (TreeNoise(seed, 18 + i) * 0.12f)));
            var trunk = new RectangleF(crownRect.Center.X - (trunkWidth * 0.5f), trunkTop, trunkWidth, Math.Max(4.0f, rect.Bottom - trunkTop));

            DrawPanel(trunk.X, trunk.Y, trunk.Width, trunk.Height, new Color(72, 56, 36));

            var topCrown = new RectangleF(
                crownRect.X + (crownRect.Width * 0.14f),
                crownRect.Y + (crownRect.Height * 0.02f),
                crownRect.Width * 0.72f,
                crownRect.Height * 0.24f);
            var midCrown = new RectangleF(
                crownRect.X + (crownRect.Width * 0.02f),
                crownRect.Y + (crownRect.Height * 0.24f),
                crownRect.Width * 0.96f,
                crownRect.Height * 0.28f);
            var lowCrown = new RectangleF(
                crownRect.X + (crownRect.Width * 0.12f),
                crownRect.Y + (crownRect.Height * 0.5f),
                crownRect.Width * 0.76f,
                crownRect.Height * 0.2f);

            DrawPanel(topCrown.X, topCrown.Y, topCrown.Width, topCrown.Height, new Color(44, 102, 48));
            DrawPanel(midCrown.X, midCrown.Y, midCrown.Width, midCrown.Height, new Color(34, 84, 38));
            DrawPanel(lowCrown.X, lowCrown.Y, lowCrown.Width, lowCrown.Height, new Color(24, 62, 28));
            DrawPanel(crownRect.X + (crownRect.Width * 0.26f), crownRect.Bottom - (crownRect.Height * 0.06f), crownRect.Width * 0.48f, crownRect.Height * 0.03f, new Color(18, 28, 18, 84));
        }
    }

    private void DrawMountainSilhouette(RectangleF rect)
    {
        var seed = (int)(rect.X * 0.17f) ^ ((int)(rect.Y * 0.11f) << 1) ^ ((int)(rect.Width * 0.07f) << 2);
        var leftWidth = 0.22f + (MountainNoise(seed, 1) * 0.12f);
        var centerWidth = 0.24f + (MountainNoise(seed, 2) * 0.18f);
        var rightWidth = 0.18f + (MountainNoise(seed, 3) * 0.12f);
        var leftHeight = 0.42f + (MountainNoise(seed, 4) * 0.24f);
        var centerHeight = 0.58f + (MountainNoise(seed, 5) * 0.22f);
        var rightHeight = 0.36f + (MountainNoise(seed, 6) * 0.28f);
        var leftX = 0.04f + (MountainNoise(seed, 7) * 0.12f);
        var centerX = 0.26f + (MountainNoise(seed, 8) * 0.12f);
        var rightX = 0.54f + (MountainNoise(seed, 9) * 0.16f);
        var leftTop = 0.28f + (MountainNoise(seed, 10) * 0.18f);
        var centerTop = 0.1f + (MountainNoise(seed, 11) * 0.14f);
        var rightTop = 0.22f + (MountainNoise(seed, 12) * 0.2f);

        DrawPanel(rect.X + (rect.Width * leftX), rect.Y + (rect.Height * leftTop), rect.Width * leftWidth, rect.Height * leftHeight, new Color(78, 80, 86));
        DrawPanel(rect.X + (rect.Width * centerX), rect.Y + (rect.Height * centerTop), rect.Width * centerWidth, rect.Height * centerHeight, new Color(92, 94, 100));
        DrawPanel(rect.X + (rect.Width * rightX), rect.Y + (rect.Height * rightTop), rect.Width * rightWidth, rect.Height * rightHeight, new Color(70, 72, 78));

        if (MountainNoise(seed, 13) > 0.38f)
        {
            var capWidth = rect.Width * (0.1f + (MountainNoise(seed, 14) * 0.08f));
            var capX = rect.X + (rect.Width * (centerX + 0.04f));
            var capY = rect.Y + (rect.Height * (centerTop + 0.02f));
            DrawPanel(capX, capY, capWidth, rect.Height * 0.08f, new Color(170, 172, 178));
        }

        if (MountainNoise(seed, 15) > 0.54f)
        {
            DrawPanel(rect.X + (rect.Width * (leftX + 0.04f)), rect.Y + (rect.Height * (leftTop + 0.12f)), rect.Width * 0.08f, rect.Height * 0.06f, new Color(132, 134, 140));
        }
    }

    private static float MountainNoise(int seed, int salt)
    {
        unchecked
        {
            var value = seed ^ (salt * 374761393);
            value = (value ^ (value >> 13)) * 1274126177;
            value ^= value >> 16;
            return (value & 1023) / 1023.0f;
        }
    }

    private static float TreeNoise(int seed, int salt)
    {
        unchecked
        {
            var value = seed ^ (salt * 668265263);
            value = (value ^ (value >> 15)) * unchecked((int)2246822519u);
            value ^= value >> 13;
            return (value & 1023) / 1023.0f;
        }
    }

    private void DrawFenSilhouette(RectangleF rect)
    {
        DrawPanel(rect.X + (rect.Width * 0.12f), rect.Bottom - (rect.Height * 0.28f), rect.Width * 0.76f, rect.Height * 0.18f, new Color(64, 94, 98));
        DrawPanel(rect.X + (rect.Width * 0.2f), rect.Bottom - (rect.Height * 0.42f), rect.Width * 0.12f, rect.Height * 0.3f, new Color(88, 110, 72));
        DrawPanel(rect.X + (rect.Width * 0.44f), rect.Bottom - (rect.Height * 0.48f), rect.Width * 0.12f, rect.Height * 0.36f, new Color(82, 102, 68));
        DrawPanel(rect.X + (rect.Width * 0.66f), rect.Bottom - (rect.Height * 0.4f), rect.Width * 0.1f, rect.Height * 0.28f, new Color(90, 112, 74));
    }

    private void DrawTownGateSilhouette(RectangleF rect)
    {
        var wallColor = new Color(132, 118, 92);
        var roofColor = new Color(146, 82, 64);
        var towerColor = new Color(122, 110, 88);
        var shadowColor = new Color(72, 52, 38);
        var trimColor = new Color(174, 156, 120);

        DrawPanel(rect.X + (rect.Width * 0.08f), rect.Bottom - (rect.Height * 0.16f), rect.Width * 0.84f, rect.Height * 0.08f, shadowColor);

        DrawPanel(rect.X + (rect.Width * 0.12f), rect.Y + (rect.Height * 0.26f), rect.Width * 0.16f, rect.Height * 0.46f, towerColor);
        DrawPanel(rect.X + (rect.Width * 0.72f), rect.Y + (rect.Height * 0.24f), rect.Width * 0.16f, rect.Height * 0.48f, towerColor);
        DrawPanel(rect.X + (rect.Width * 0.18f), rect.Y + (rect.Height * 0.18f), rect.Width * 0.1f, rect.Height * 0.08f, trimColor);
        DrawPanel(rect.X + (rect.Width * 0.72f), rect.Y + (rect.Height * 0.16f), rect.Width * 0.1f, rect.Height * 0.08f, trimColor);

        DrawPanel(rect.X + (rect.Width * 0.28f), rect.Y + (rect.Height * 0.34f), rect.Width * 0.18f, rect.Height * 0.28f, wallColor);
        DrawPanel(rect.X + (rect.Width * 0.48f), rect.Y + (rect.Height * 0.3f), rect.Width * 0.2f, rect.Height * 0.34f, wallColor);
        DrawPanel(rect.X + (rect.Width * 0.32f), rect.Y + (rect.Height * 0.26f), rect.Width * 0.12f, rect.Height * 0.08f, roofColor);
        DrawPanel(rect.X + (rect.Width * 0.5f), rect.Y + (rect.Height * 0.2f), rect.Width * 0.14f, rect.Height * 0.1f, roofColor);

        DrawPanel(rect.X + (rect.Width * 0.24f), rect.Y + (rect.Height * 0.46f), rect.Width * 0.52f, rect.Height * 0.22f, wallColor);
        DrawPanel(rect.X + (rect.Width * 0.28f), rect.Y + (rect.Height * 0.4f), rect.Width * 0.44f, rect.Height * 0.06f, trimColor);
        DrawPanel(rect.X + (rect.Width * 0.42f), rect.Bottom - (rect.Height * 0.3f), rect.Width * 0.16f, rect.Height * 0.22f, shadowColor);

        DrawPanel(rect.X + (rect.Width * 0.18f), rect.Y + (rect.Height * 0.52f), rect.Width * 0.04f, rect.Height * 0.06f, shadowColor);
        DrawPanel(rect.X + (rect.Width * 0.24f), rect.Y + (rect.Height * 0.5f), rect.Width * 0.04f, rect.Height * 0.08f, shadowColor);
        DrawPanel(rect.X + (rect.Width * 0.68f), rect.Y + (rect.Height * 0.5f), rect.Width * 0.04f, rect.Height * 0.08f, shadowColor);
        DrawPanel(rect.X + (rect.Width * 0.74f), rect.Y + (rect.Height * 0.52f), rect.Width * 0.04f, rect.Height * 0.06f, shadowColor);
    }

    private void DrawHarborSilhouette(RectangleF rect)
    {
        DrawPanel(rect.X + (rect.Width * 0.16f), rect.Bottom - (rect.Height * 0.18f), rect.Width * 0.68f, rect.Height * 0.08f, new Color(94, 76, 58));
        DrawPanel(rect.X + (rect.Width * 0.24f), rect.Y + (rect.Height * 0.2f), rect.Width * 0.16f, rect.Height * 0.52f, new Color(116, 98, 76));
        DrawPanel(rect.X + (rect.Width * 0.44f), rect.Y + (rect.Height * 0.28f), rect.Width * 0.24f, rect.Height * 0.44f, new Color(132, 118, 90));
        DrawPanel(rect.X + (rect.Width * 0.7f), rect.Y + (rect.Height * 0.16f), rect.Width * 0.04f, rect.Height * 0.46f, new Color(126, 114, 88));
        DrawPanel(rect.X + (rect.Width * 0.74f), rect.Y + (rect.Height * 0.16f), rect.Width * 0.18f, rect.Height * 0.04f, new Color(186, 188, 178));
    }

    private void DrawKeepSilhouette(RectangleF rect)
    {
        DrawPanel(rect.X + (rect.Width * 0.18f), rect.Y + (rect.Height * 0.18f), rect.Width * 0.18f, rect.Height * 0.58f, new Color(118, 122, 132));
        DrawPanel(rect.X + (rect.Width * 0.64f), rect.Y + (rect.Height * 0.18f), rect.Width * 0.18f, rect.Height * 0.58f, new Color(118, 122, 132));
        DrawPanel(rect.X + (rect.Width * 0.34f), rect.Y + (rect.Height * 0.28f), rect.Width * 0.32f, rect.Height * 0.48f, new Color(132, 136, 146));
        DrawPanel(rect.X + (rect.Width * 0.3f), rect.Y + (rect.Height * 0.14f), rect.Width * 0.4f, rect.Height * 0.08f, new Color(152, 156, 164));
    }

    private void DrawRuinSilhouette(RectangleF rect)
    {
        DrawPanel(rect.X + (rect.Width * 0.2f), rect.Y + (rect.Height * 0.34f), rect.Width * 0.16f, rect.Height * 0.42f, new Color(108, 92, 82));
        DrawPanel(rect.X + (rect.Width * 0.46f), rect.Y + (rect.Height * 0.24f), rect.Width * 0.18f, rect.Height * 0.52f, new Color(122, 102, 90));
        DrawPanel(rect.X + (rect.Width * 0.68f), rect.Y + (rect.Height * 0.42f), rect.Width * 0.12f, rect.Height * 0.34f, new Color(98, 84, 74));
        DrawPanel(rect.X + (rect.Width * 0.18f), rect.Bottom - (rect.Height * 0.12f), rect.Width * 0.64f, rect.Height * 0.06f, new Color(72, 64, 58));
    }

    private void DrawShrineSilhouette(RectangleF rect)
    {
        DrawPanel(rect.X + (rect.Width * 0.26f), rect.Y + (rect.Height * 0.28f), rect.Width * 0.48f, rect.Height * 0.44f, new Color(126, 114, 136));
        DrawPanel(rect.X + (rect.Width * 0.22f), rect.Y + (rect.Height * 0.2f), rect.Width * 0.56f, rect.Height * 0.08f, new Color(162, 146, 178));
        DrawPanel(rect.Center.X - (rect.Width * 0.03f), rect.Y + (rect.Height * 0.04f), rect.Width * 0.06f, rect.Height * 0.22f, new Color(190, 174, 212));
        DrawPanel(rect.Center.X - (rect.Width * 0.12f), rect.Y + (rect.Height * 0.12f), rect.Width * 0.24f, rect.Height * 0.04f, new Color(196, 182, 220));
    }

    private void DrawCampSilhouette(RectangleF rect)
    {
        DrawPanel(rect.X + (rect.Width * 0.18f), rect.Bottom - (rect.Height * 0.16f), rect.Width * 0.64f, rect.Height * 0.04f, new Color(96, 82, 58));
        DrawPanel(rect.X + (rect.Width * 0.26f), rect.Y + (rect.Height * 0.38f), rect.Width * 0.28f, rect.Height * 0.28f, new Color(104, 92, 72));
        DrawPanel(rect.X + (rect.Width * 0.5f), rect.Y + (rect.Height * 0.36f), rect.Width * 0.24f, rect.Height * 0.24f, new Color(92, 82, 68));
        DrawPanel(rect.Center.X - (rect.Width * 0.04f), rect.Bottom - (rect.Height * 0.28f), rect.Width * 0.08f, rect.Height * 0.08f, new Color(212, 126, 62));
    }

    private void DrawDungeonMouthSilhouette(RectangleF rect)
    {
        DrawPanel(rect.X + (rect.Width * 0.14f), rect.Y + (rect.Height * 0.18f), rect.Width * 0.72f, rect.Height * 0.56f, new Color(96, 84, 78));
        DrawPanel(rect.X + (rect.Width * 0.3f), rect.Y + (rect.Height * 0.34f), rect.Width * 0.4f, rect.Height * 0.4f, new Color(18, 20, 24));
        DrawPanel(rect.X + (rect.Width * 0.2f), rect.Y + (rect.Height * 0.12f), rect.Width * 0.6f, rect.Height * 0.08f, new Color(114, 100, 92));
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

        DrawText("NOTIMA", new Vector2(HudX + 22, 46), new Color(178, 150, 96), 3);
        DrawText(dungeon is null ? map.Name : $"DUNGEON LV {dungeon.Level}", new Vector2(HudX + 22, 88), new Color(124, 142, 174), 2);

        DrawText($"HP {party.TotalHealth}/{party.MaxTotalHealth}", new Vector2(HudX + 22, 138), party.TotalHealth > (party.MaxTotalHealth / 2) ? new Color(108, 156, 113) : new Color(170, 111, 111), 2);
        DrawText($"MP {party.Mana}/{party.MaxMana}", new Vector2(HudX + 250, 138), new Color(112, 137, 179), 2);
        DrawText($"FOOD {party.Food}", new Vector2(HudX + 22, 168), new Color(171, 150, 99), 2);
        DrawText($"GOLD {party.Gold}", new Vector2(HudX + 22, 198), new Color(184, 151, 88), 2);
        DrawText($"LVL {party.Level}", new Vector2(HudX + 22, 228), new Color(123, 144, 173), 2);
        DrawText($"ATK {GetPartyAttackRating()}", new Vector2(HudX + 22, 258), new Color(170, 130, 104), 2);
        DrawText($"DEF {GetPartyDefenseRating()}", new Vector2(HudX + 170, 258), new Color(133, 140, 160), 2);
        DrawText($"KEYS {party.Keys}", new Vector2(HudX + 314, 258), new Color(171, 150, 99), 2);
        DrawText($"STEPS {party.Steps}", new Vector2(HudX + 22, 288), new Color(133, 140, 160), 2);
        DrawText($"INV {party.Inventory.Count}", new Vector2(HudX + 170, 288), new Color(133, 140, 160), 2);
        DrawText(GetBestWeaponName(), new Vector2(HudX + 22, 314), new Color(158, 128, 104), 1);
        DrawText(GetBestArmorName(), new Vector2(HudX + 220, 314), new Color(128, 136, 151), 1);

        DrawText("TILE", new Vector2(HudX + 22, 340), new Color(178, 150, 96), 2);
        DrawWrappedText(DescribeCurrentTile(), new Vector2(HudX + 22, 370), 2, HudWidth - 44, new Color(158, 169, 189));

        DrawText("STATUS", new Vector2(HudX + 22, 450), new Color(178, 150, 96), 2);
        DrawWrappedText(statusLine, new Vector2(HudX + 22, 480), 2, HudWidth - 44, new Color(166, 170, 180));

        DrawText("MOVE WASD OR ARROWS", new Vector2(HudX + 22, 622), new Color(126, 145, 172), 2);
        DrawText("ENTER INTERACTS", new Vector2(HudX + 22, 648), new Color(126, 145, 172), 2);
        DrawText("F5 SAVE  F9 LOAD  F10 DUNGEON", new Vector2(HudX + 22, 674), new Color(126, 145, 172), 2);
    }

    private void DrawMinimapOverlay()
    {
        var panel = new RectangleF(70, 524, 182, 96);
        DrawPanel(panel.X, panel.Y, panel.Width, panel.Height, new Color(16, 20, 28, 222));
        DrawFrame(panel, new Color(114, 126, 152), 2);
        DrawText(dungeon is null ? "MINIMAP" : "DUNGEON MAP", new Vector2(panel.X + 10, panel.Y + 8), new Color(198, 182, 140), 1);

        var mapArea = new RectangleF(panel.X + 8, panel.Y + 22, panel.Width - 16, panel.Height - 30);
        DrawPanel(mapArea.X, mapArea.Y, mapArea.Width, mapArea.Height, new Color(10, 14, 18, 216));
        DrawFrame(mapArea, new Color(72, 82, 102), 1);

        var rows = dungeon is null ? map.Rows : dungeon.Rows;
        var currentCell = dungeon is null ? playerCell : dungeonCell;
        var cell = dungeon is null ? 5.0f : 6.0f;
        var originX = mapArea.Center.X - ((currentCell.X + 0.5f) * cell);
        var originY = mapArea.Center.Y - ((currentCell.Y + 0.5f) * cell);

        for (var y = 0; y < rows.Count; y++)
        {
            var row = rows[y];
            for (var x = 0; x < row.Length; x++)
            {
                var tileRect = new RectangleF(originX + (x * cell), originY + (y * cell), cell, cell);
                if (tileRect.Right < mapArea.X || tileRect.Bottom < mapArea.Y || tileRect.X > mapArea.Right || tileRect.Y > mapArea.Bottom)
                {
                    continue;
                }

                var symbol = row[x];
                DrawPanel(tileRect.X, tileRect.Y, tileRect.Width, tileRect.Height, GetMinimapCellColor(symbol, dungeon is not null));

                if (ShouldDrawMinimapIsoIcon(symbol, dungeon is not null))
                {
                    var icon = new RectangleF(tileRect.X + 1.0f, tileRect.Y + 1.0f, tileRect.Width - 2.0f, tileRect.Height - 2.0f);
                    spriteBatch.Draw(tileTexture, UiRect(icon), GetIsoTileSource(symbol), Color.White, 0, Vector2.Zero);
                }
            }
        }

        var playerMarker = new RectangleF(mapArea.Center.X - 4.0f, mapArea.Center.Y - 4.0f, 8.0f, 8.0f);
        DrawPanel(playerMarker.X, playerMarker.Y, playerMarker.Width, playerMarker.Height, new Color(226, 92, 72));
        DrawFrame(playerMarker, new Color(255, 220, 180), 1);

        var facingTip = facing switch
        {
            Direction.Up => new RectangleF(playerMarker.Center.X - 1.0f, playerMarker.Y - 5.0f, 2.0f, 4.0f),
            Direction.Down => new RectangleF(playerMarker.Center.X - 1.0f, playerMarker.Bottom + 1.0f, 2.0f, 4.0f),
            Direction.Left => new RectangleF(playerMarker.X - 5.0f, playerMarker.Center.Y - 1.0f, 4.0f, 2.0f),
            _ => new RectangleF(playerMarker.Right + 1.0f, playerMarker.Center.Y - 1.0f, 4.0f, 2.0f),
        };
        DrawPanel(facingTip.X, facingTip.Y, facingTip.Width, facingTip.Height, new Color(255, 220, 180));

    }

    private string GetProjectionDebugLine(int depth)
    {
        var left = GetCrawlerCellSymbol(GetRelativeCell(-1, depth, isDungeonView: false), false);
        var center = GetCrawlerCellSymbol(GetRelativeCell(0, depth, isDungeonView: false), false);
        var right = GetCrawlerCellSymbol(GetRelativeCell(1, depth, isDungeonView: false), false);
        return $"D{depth} {left}{center}{right}";
    }

    private void DrawPanels()
    {
        if (uiMode == UiMode.Overworld)
        {
            return;
        }

        if (uiMode == UiMode.Town && townMenu is not null)
        {
            DrawTownScene();
            return;
        }

        if ((uiMode == UiMode.Dungeon || uiMode == UiMode.Dialog) && string.IsNullOrWhiteSpace(panelTitle) && panelLines.Count == 0)
        {
            return;
        }

        var panelRect = GetPanelRect();
        DrawPanel(panelRect.X, panelRect.Y, panelRect.Width, panelRect.Height, new Color(15, 18, 29, 236));
        DrawFrame(panelRect, new Color(173, 145, 89), 2);
        DrawText(panelTitle, new Vector2(panelRect.X + 22, panelRect.Y + 22), new Color(188, 164, 109), 3);

        if (uiMode == UiMode.Encounter && encounter is not null)
        {
            DrawEncounterAnimation(panelRect);
        }

        var lineY = uiMode == UiMode.Encounter ? panelRect.Y + 182 : panelRect.Y + 72;
        foreach (var line in panelLines)
        {
            DrawWrappedText(line, new Vector2(panelRect.X + 22, lineY), 2, (int)panelRect.Width - 44, new Color(232, 238, 252));
            lineY += uiMode == UiMode.Encounter ? 32 : 32;
        }
    }

    private RectangleF GetPanelRect()
    {
        return uiMode == UiMode.Encounter
            ? new RectangleF(84, 148, 580, EncounterPanelHeight)
            : new RectangleF(PanelX, PanelY, PanelWidth, PanelHeight);
    }

    private void DrawTownScene()
    {
        if (townMenu is null)
        {
            return;
        }

        var rect = new RectangleF(786, 246, 426, 290);
        DrawPanel(rect.X, rect.Y, rect.Width, rect.Height, new Color(23, 21, 28, 242));
        DrawFrame(rect, new Color(166, 140, 88), 2);
        DrawText(townMenu.Title, new Vector2(rect.X + 24, rect.Y + 22), new Color(191, 167, 109), 3);
        DrawWrappedText(townMenu.Description, new Vector2(rect.X + 24, rect.Y + 62), 2, 370, new Color(156, 165, 182));

        var vignette = townMenu.Symbol switch
        {
            'T' => "INN  FORGE  MARKET",
            'H' => "DOCK  ARMORER  WAREHOUSE",
            'C' => "FIRE  PACKS  BEDROLLS",
            'K' => "BARRACKS  YARD  CAPTAIN",
            'S' => "ALTAR  CANDLES  SILENCE",
            _ => "ARCHES  RUBBLE  SHADOWS",
        };
        DrawText(vignette, new Vector2(rect.X + 24, rect.Y + 110), new Color(126, 135, 150), 2);

        var optionWidth = 122.0f;
        var optionHeight = 56.0f;
        var optionsPerRow = 3;
        var startX = rect.X + 18.0f;
        var startY = rect.Y + 154.0f;
        for (var i = 0; i < townMenu.Options.Count; i++)
        {
            var column = i % optionsPerRow;
            var row = i / optionsPerRow;
            var box = new RectangleF(startX + (column * (optionWidth + 10.0f)), startY + (row * (optionHeight + 10.0f)), optionWidth, optionHeight);
            var selected = i == townMenu.SelectedIndex;
            DrawPanel(box.X, box.Y, box.Width, box.Height, selected ? new Color(78, 62, 45, 220) : new Color(36, 38, 46, 220));
            DrawFrame(box, selected ? new Color(194, 160, 97) : new Color(89, 97, 112), 1);
            DrawWrappedText(townMenu.Options[i], new Vector2(box.X + 8, box.Y + 10), 1, 106, selected ? new Color(235, 216, 178) : new Color(174, 179, 190));
        }

        DrawText("LEFT RIGHT SELECT", new Vector2(rect.X + 24, rect.Bottom - 40), new Color(126, 145, 172), 2);
        DrawText("ENTER CONFIRMS  R LEAVES", new Vector2(rect.X + 24, rect.Bottom - 18), new Color(126, 145, 172), 2);
    }

    private void DrawDefeatPortalOverlay()
    {
        var progress = 1.0f - (defeatPortalTimer / 5.0f);
        var pulse = 0.5f + (0.5f * MathF.Sin(totalTime * 5.6f));
        var overlay = new RectangleF(0, 0, BaseWidth, BaseHeight);
        DrawPanel(overlay.X, overlay.Y, overlay.Width, overlay.Height, new Color(12, 22, 46, 96));

        var center = new Vector2(BaseWidth * 0.5f, BaseHeight * 0.5f);
        for (var i = 0; i < 5; i++)
        {
            var ringScale = 1.0f - (i * 0.14f);
            var width = (220.0f + (progress * 1040.0f)) * ringScale;
            var height = (128.0f + (progress * 620.0f)) * ringScale;
            var alpha = (byte)Math.Clamp(170 - (i * 28) + (pulse * 28.0f), 40, 210);
            var ring = new RectangleF(center.X - (width * 0.5f), center.Y - (height * 0.5f), width, height);
            DrawFrame(ring, new Color(86, 146, 255, alpha), 3);
        }

        for (var i = 0; i < 7; i++)
        {
            var angle = totalTime * 1.8f + (i * 0.9f);
            var radius = 58.0f + (progress * 190.0f) + (i * 16.0f);
            var x = center.X + (MathF.Cos(angle) * radius);
            var y = center.Y + (MathF.Sin(angle) * radius * 0.58f);
            DrawPanel(x - 3.0f, y - 14.0f, 6.0f, 28.0f, new Color(188, 226, 255, 128));
        }

        DrawText("THE BLUE GATE TAKES YOU", new Vector2(392, 302), new Color(214, 230, 255), 3);
        DrawText("BACK TO THE STARTING ROAD", new Vector2(368, 336), new Color(184, 212, 255), 2);
    }

    private void DrawEncounterAnimation(RectangleF panelRect)
    {
        var boardRect = new RectangleF(panelRect.X + 18, panelRect.Y + 6, panelRect.Width - 36, 112);
        DrawPanel(boardRect.X, boardRect.Y, boardRect.Width, boardRect.Height, new Color(28, 36, 50, 220));
        DrawFrame(boardRect, new Color(106, 128, 177), 1);
        if (encounter is null)
        {
            return;
        }

        var cardWidth = 156.0f;
        var cardHeight = 92.0f;
        var gap = 12.0f;
        var totalWidth = (cardWidth * encounter.Enemies.Count) + (gap * Math.Max(0, encounter.Enemies.Count - 1));
        var startX = boardRect.Center.X - (totalWidth * 0.5f);
        var startY = boardRect.Y + 6.0f;

        for (var i = 0; i < encounter.Enemies.Count; i++)
        {
            var enemy = encounter.Enemies[i];
            var card = new RectangleF(startX + (i * (cardWidth + gap)), startY, cardWidth, cardHeight);
            var portraitRect = new RectangleF(card.X + 5.0f, card.Y + 5.0f, 82.0f, 82.0f);
            var captionRect = new RectangleF(card.Right - 64.0f, card.Y + 5.0f, 59.0f, cardHeight - 10.0f);
            var targetable = IsEnemyTargetable(i);
            var selected = i == selectedEnemyIndex && targetable;
            var cardColor = enemy.IsAlive ? new Color(18, 20, 26, 232) : new Color(14, 14, 18, 214);
            DrawPanel(card.X, card.Y, card.Width, card.Height, cardColor);
            DrawFrame(card, selected ? new Color(194, 160, 97) : new Color(92, 97, 112), selected ? 2 : 1);

            var source = GetEnemyPortraitSource(enemy.Name);
            var tint = enemy.IsAlive ? Color.White : new Color(118, 118, 126);
            spriteBatch.Draw(grimEnemyPortraitTexture, UiRect(portraitRect), source, tint, 0, Vector2.Zero);

            var missingRatio = 1.0f - (enemy.Health / (float)Math.Max(1, enemy.MaxHealth));
            if (missingRatio > 0.0f)
            {
                var overlayHeight = portraitRect.Height * missingRatio;
                var overlayRect = new RectangleF(portraitRect.X, portraitRect.Bottom - overlayHeight, portraitRect.Width, overlayHeight);
                DrawPanel(overlayRect.X, overlayRect.Y, overlayRect.Width, overlayRect.Height, new Color(168, 28, 28, 108));
            }

            if (selected)
            {
                var highlight = new RectangleF(portraitRect.X - 1.0f, portraitRect.Y - 1.0f, portraitRect.Width + 2.0f, portraitRect.Height + 2.0f);
                DrawFrame(highlight, new Color(214, 184, 108), 2);
            }

            DrawPanel(captionRect.X, captionRect.Y, captionRect.Width, captionRect.Height, new Color(8, 10, 14, 208));
            DrawText(enemy.Name, new Vector2(captionRect.X + 4.0f, captionRect.Y + 4.0f), enemy.IsAlive ? new Color(228, 228, 236) : new Color(122, 122, 132), 1);
            DrawText($"{enemy.Health}/{enemy.MaxHealth}", new Vector2(captionRect.X + 4.0f, captionRect.Bottom - 12.0f), enemy.IsAlive ? new Color(214, 220, 228) : new Color(122, 122, 132), 1);
        }
    }

    private static RectangleF GetEnemyPortraitSource(string enemyName)
    {
        var column = enemyName switch
        {
            "WOLF" => 0,
            "LEECH" => 1,
            _ => 2,
        };

        return new RectangleF(column * 128, 0, 128, 128);
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

    private void PlayEnemyHitSound(string enemyName)
    {
        switch (enemyName)
        {
            case "WOLF":
                audioPlayer?.PlayWolfAttack();
                break;
            case "LEECH":
                audioPlayer?.PlayLeechSuck();
                break;
            default:
                audioPlayer?.PlayClash();
                break;
        }
    }

    private void PlayEnemyMissSound(string enemyName)
    {
        switch (enemyName)
        {
            case "WOLF":
                audioPlayer?.PlayWolfGrowl();
                break;
            default:
                audioPlayer?.PlaySwish();
                break;
        }
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

        ReviveFallenAfterVictory();

        party.Gold += encounter.RewardGold;
        party.Food += encounter.RewardFood;
        string? itemDrop = null;
        if (TryAwardEquipmentDrop(Math.Max(1, (dungeon?.Level ?? party.Level) + 1), out var awardedItem))
        {
            itemDrop = awardedItem;
        }

        if (encounterFromDungeon && dungeon is not null && encounterIsBoss)
        {
            dungeon.BossDefeated = true;
            if (dungeon.BossExitPoint is not null)
            {
                dungeon.SetTile(dungeon.BossExitPoint.Value, '>');
            }

            party.Keys++;
            statusLine = $"Boss defeated. Gold +{encounter.RewardGold}, Food +{encounter.RewardFood}, Key +1.";
        }
        else
        {
            statusLine = $"{encounter.Name} defeated. Gold +{encounter.RewardGold}, Food +{encounter.RewardFood}.";
        }
        if (!string.IsNullOrWhiteSpace(itemDrop))
        {
            statusLine += $" {itemDrop}.";
        }
        OpenDialog(
            "VICTORY",
            $"{encounter.Name} FALLS.",
            $"GOLD +{encounter.RewardGold}",
            $"FOOD +{encounter.RewardFood}",
            itemDrop ?? string.Empty,
            "ENTER CONTINUES"
        );
        audioPlayer?.PlayTrumpet();
        encounter = null;
        encounterFromDungeon = false;
        encounterIsBoss = false;
        encounterTurnOrder.Clear();
        encounterTurnCursor = 0;
        selectedEnemyIndex = 0;
        playerAttackAnimationTime = 0.0f;
        enemyAttackAnimationTime = 0.0f;
    }

    private bool TryCastSpell(List<string> roundEvents)
    {
        var manaCost = GetSpellCost(selectedSpell);
        if (party.Mana < manaCost)
        {
            roundEvents.Add($"Not enough mana for {GetSpellName(selectedSpell)}");
            return false;
        }

        party.Mana -= manaCost;

        switch (selectedSpell)
        {
            case SpellKind.Ember:
                audioPlayer?.PlayMagic();
                var target = GetSelectedEnemy() ?? encounter?.Enemies.FirstOrDefault(enemy => enemy.IsAlive);
                if (target is null)
                {
                    return false;
                }

                var emberDamage = 6 + party.Level + Math.Max(1, GetEquippedWeapon(party.Members[0]).Attack / 2);
                target.Health = Math.Max(0, target.Health - emberDamage);
                roundEvents.Add($"AVA casts EMBER for {emberDamage}");
                if (!target.IsAlive)
                {
                    selectedEnemyIndex = GetDefaultSelectedEnemy();
                }

                return true;
            case SpellKind.Mend:
                audioPlayer?.PlayMagic();
                var allyIndex = GetLowestHealthPartyIndex();
                if (allyIndex is null)
                {
                    return false;
                }

                var ally = party.Members[allyIndex.Value];
                var heal = 6 + party.Level;
                ally.Health = Math.Min(ally.MaxHealth, ally.Health + heal);
                roundEvents.Add($"AVA casts MEND on {ally.Name} for {heal}");
                return true;
            default:
                audioPlayer?.PlayMagic();
                party.WardCharges = 2;
                roundEvents.Add("AVA casts AEGIS");
                return true;
        }
    }

    private int GetSpellCost(SpellKind spell) => spell switch
    {
        SpellKind.Ember => 3,
        SpellKind.Mend => 4,
        _ => 5,
    };

    private string GetSpellName(SpellKind spell) => spell switch
    {
        SpellKind.Ember => "EMBER",
        SpellKind.Mend => "MEND",
        _ => "AEGIS",
    };

    private string GetCombatSummary()
    {
        return selectedCombatAction == CombatAction.Attack
            ? "MODE ATTACK"
            : $"MODE SPELL {GetSpellName(selectedSpell)} MP {party.Mana}/{party.MaxMana}";
    }

    private void ResetEncounterTurnState()
    {
        BuildEncounterTurnOrder();
        encounterTurnCursor = 0;
        NormalizeEncounterTurnCursor();
    }

    private void BuildEncounterTurnOrder()
    {
        encounterTurnOrder.Clear();

        var partyTurns = party.Members
            .Select((member, index) => new CombatTurnEntry(true, index))
            .Where(turn => party.Members[turn.Index].IsAlive)
            .ToList();
        var enemyTurns = encounter?.Enemies
            .Select((enemy, index) => new CombatTurnEntry(false, index))
            .Where(turn => encounter!.Enemies[turn.Index].IsAlive)
            .ToList() ?? [];

        var count = Math.Max(partyTurns.Count, enemyTurns.Count);
        for (var i = 0; i < count; i++)
        {
            if (i < partyTurns.Count)
            {
                encounterTurnOrder.Add(partyTurns[i]);
            }

            if (i < enemyTurns.Count)
            {
                encounterTurnOrder.Add(enemyTurns[i]);
            }
        }
    }

    private void NormalizeEncounterTurnCursor()
    {
        BuildEncounterTurnOrder();
        if (encounterTurnOrder.Count == 0)
        {
            encounterTurnCursor = 0;
            return;
        }

        encounterTurnCursor = ((encounterTurnCursor % encounterTurnOrder.Count) + encounterTurnOrder.Count) % encounterTurnOrder.Count;
    }

    private void AdvanceEncounterTurn(CombatTurnEntry currentTurn)
    {
        var previousOrder = encounterTurnOrder.ToList();
        var currentIndex = encounterTurnCursor;
        BuildEncounterTurnOrder();
        if (encounterTurnOrder.Count == 0)
        {
            encounterTurnCursor = 0;
            return;
        }

        CombatTurnEntry? nextTurn = null;
        for (var offset = 1; offset <= previousOrder.Count; offset++)
        {
            var candidate = previousOrder[(currentIndex + offset) % previousOrder.Count];
            if (IsCombatTurnAlive(candidate))
            {
                nextTurn = candidate;
                break;
            }
        }

        if (nextTurn is null)
        {
            encounterTurnCursor = 0;
            return;
        }

        var matchIndex = encounterTurnOrder.FindIndex(turn => turn.IsParty == nextTurn.Value.IsParty && turn.Index == nextTurn.Value.Index);
        encounterTurnCursor = matchIndex >= 0 ? matchIndex : 0;
    }

    private bool IsCombatTurnAlive(CombatTurnEntry turn)
    {
        if (turn.IsParty)
        {
            return turn.Index >= 0 && turn.Index < party.Members.Count && party.Members[turn.Index].IsAlive;
        }

        return encounter is not null
            && turn.Index >= 0
            && turn.Index < encounter.Enemies.Count
            && encounter.Enemies[turn.Index].IsAlive;
    }

    private string GetTurnBanner()
    {
        if (encounterTurnOrder.Count == 0)
        {
            return "TURN ?";
        }

        var turn = encounterTurnOrder[Math.Clamp(encounterTurnCursor, 0, encounterTurnOrder.Count - 1)];
        return turn.IsParty
            ? $"TURN {party.Members[turn.Index].Name}"
            : $"TURN {encounter?.Enemies[turn.Index].Name ?? "FOE"}";
    }

    private string GetCombatPrompt()
    {
        if (encounterTurnOrder.Count == 0)
        {
            return "ENTER ACTS";
        }

        var turn = encounterTurnOrder[Math.Clamp(encounterTurnCursor, 0, encounterTurnOrder.Count - 1)];
        return turn.IsParty
            ? $"ARROWS PICK TARGET  ENTER ACTS  {GetCombatSummary()}"
            : "ENTER RESOLVES ENEMY TURN";
    }

    private void RefreshEncounterPanel()
    {
        if (encounter is null)
        {
            return;
        }

        var target = GetSelectedEnemy();
        panelLines =
        [
            target is null ? "NO TARGET" : $"{target.Name} HP {target.Health}/{target.MaxHealth}",
            $"PARTY HP {party.TotalHealth}/{party.MaxTotalHealth}",
            GetTurnBanner(),
            GetCombatPrompt(),
        ];
    }

    private int? GetLowestHealthPartyIndex()
    {
        var bestRatio = float.MaxValue;
        int? bestIndex = null;
        for (var i = 0; i < party.Members.Count; i++)
        {
            var member = party.Members[i];
            if (!member.IsAlive)
            {
                continue;
            }

            var ratio = (float)member.Health / member.MaxHealth;
            if (ratio < bestRatio)
            {
                bestRatio = ratio;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void EnsurePartyEquipmentState()
    {
        EnsureInventoryContains("club");
        EnsureInventoryContains("dagger");
        EnsureInventoryContains("mace");
        EnsureInventoryContains("padded");
        EnsureInventoryContains("leather");

        if (party.Members.Count > 0)
        {
            party.Members[0].EquippedWeaponId ??= "mace";
            party.Members[0].EquippedArmorId ??= "leather";
        }
        if (party.Members.Count > 1)
        {
            party.Members[1].EquippedWeaponId ??= "club";
            party.Members[1].EquippedArmorId ??= "padded";
        }
        if (party.Members.Count > 2)
        {
            party.Members[2].EquippedWeaponId ??= "dagger";
            party.Members[2].EquippedArmorId ??= "leather";
        }
        if (party.Members.Count > 3)
        {
            party.Members[3].EquippedWeaponId ??= "dagger";
            party.Members[3].EquippedArmorId ??= "padded";
        }

        ApplyLegacyTierUpgrades();
        foreach (var member in party.Members)
        {
            EnsureInventoryContains(member.EquippedWeaponId ?? "club");
            EnsureInventoryContains(member.EquippedArmorId ?? "padded");
        }
        SyncLegacyGearTiers();
    }

    private void ApplyLegacyTierUpgrades()
    {
        if (party.WeaponTier > 0)
        {
            var maxRank = Math.Min(WeaponProgression.Length - 1, party.WeaponTier + 1);
            for (var i = 0; i <= maxRank; i++)
            {
                EnsureInventoryContains(WeaponProgression[i]);
            }
        }

        if (party.ArmorTier > 0)
        {
            var maxRank = Math.Min(ArmorProgression.Length - 1, party.ArmorTier + 1);
            for (var i = 0; i <= maxRank; i++)
            {
                EnsureInventoryContains(ArmorProgression[i]);
            }
        }
    }

    private void SyncLegacyGearTiers()
    {
        party.WeaponTier = party.Inventory
            .Select(id => EquipmentCatalog.TryGetValue(id, out var item) && item.Slot == EquipmentSlot.Weapon ? item.Rank : 0)
            .DefaultIfEmpty(0)
            .Max();
        party.ArmorTier = party.Inventory
            .Select(id => EquipmentCatalog.TryGetValue(id, out var item) && item.Slot == EquipmentSlot.Armor ? item.Rank : 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    private void EnsureInventoryContains(string itemId)
    {
        if (!party.Inventory.Contains(itemId))
        {
            party.Inventory.Add(itemId);
        }
    }

    private EquipmentDefinition GetEquippedWeapon(PartyMember member)
    {
        var weaponId = member.EquippedWeaponId ?? "club";
        return EquipmentCatalog.TryGetValue(weaponId, out var item) && item.Slot == EquipmentSlot.Weapon
            ? item
            : EquipmentCatalog["club"];
    }

    private EquipmentDefinition GetEquippedArmor(PartyMember member)
    {
        var armorId = member.EquippedArmorId ?? "padded";
        return EquipmentCatalog.TryGetValue(armorId, out var item) && item.Slot == EquipmentSlot.Armor
            ? item
            : EquipmentCatalog["padded"];
    }

    private int GetArmorDefense(PartyMember member) => GetEquippedArmor(member).Defense;

    private EquipmentDefinition? GetNextShopItem(EquipmentSlot slot)
    {
        var progression = slot == EquipmentSlot.Weapon ? WeaponProgression : ArmorProgression;
        foreach (var itemId in progression)
        {
            if (!party.Inventory.Contains(itemId))
            {
                return EquipmentCatalog[itemId];
            }
        }

        return null;
    }

    private string GrantEquipment(string itemId, string prefix)
    {
        EnsureInventoryContains(itemId);
        var item = EquipmentCatalog[itemId];
        var equipLine = TryAutoEquip(item, out var memberName)
            ? $" {memberName} equips it."
            : " It goes into the packs.";
        SyncLegacyGearTiers();
        return $"{prefix} {item.Name}.{equipLine}";
    }

    private bool TryAutoEquip(EquipmentDefinition item, out string memberName)
    {
        memberName = string.Empty;
        PartyMember? bestMember = null;
        var bestGain = 0;
        foreach (var member in party.Members)
        {
            var current = item.Slot == EquipmentSlot.Weapon ? GetEquippedWeapon(member) : GetEquippedArmor(member);
            var gain = item.Slot == EquipmentSlot.Weapon ? item.Attack - current.Attack : item.Defense - current.Defense;
            if (gain > bestGain)
            {
                bestGain = gain;
                bestMember = member;
            }
        }

        if (bestMember is null)
        {
            return false;
        }

        if (item.Slot == EquipmentSlot.Weapon)
        {
            bestMember.EquippedWeaponId = item.Id;
        }
        else
        {
            bestMember.EquippedArmorId = item.Id;
        }

        memberName = bestMember.Name;
        return true;
    }

    private string GetBestWeaponName()
    {
        var best = party.Members.Select(GetEquippedWeapon).OrderByDescending(item => item.Attack).FirstOrDefault();
        return best?.Name ?? "CLUB";
    }

    private string GetBestArmorName()
    {
        var best = party.Members.Select(GetEquippedArmor).OrderByDescending(item => item.Defense).FirstOrDefault();
        return best?.Name ?? "PADDED ARMOR";
    }

    private int GetPartyAttackRating() => party.Members.Where(member => member.IsAlive).Sum(member => GetEquippedWeapon(member).Attack);

    private int GetPartyDefenseRating() => party.Members.Where(member => member.IsAlive).Sum(member => GetEquippedArmor(member).Defense);

    private bool TryAwardEquipmentDrop(int lootLevel, out string itemDrop)
    {
        itemDrop = string.Empty;
        if (random.NextDouble() >= 0.42)
        {
            return false;
        }

        var slot = random.NextDouble() < 0.5 ? EquipmentSlot.Weapon : EquipmentSlot.Armor;
        var candidates = EquipmentCatalog.Values
            .Where(item => item.Slot == slot && item.LootLevel <= lootLevel + 1)
            .OrderBy(item => item.Rank)
            .ToList();

        if (candidates.Count == 0)
        {
            return false;
        }

        var unseen = candidates.Where(item => !party.Inventory.Contains(item.Id)).ToList();
        var pool = unseen.Count > 0 ? unseen : candidates;
        var awarded = pool[random.Next(pool.Count)];
        EnsureInventoryContains(awarded.Id);
        var equipLine = TryAutoEquip(awarded, out var memberName)
            ? $"{awarded.Name} for {memberName}"
            : $"{awarded.Name} to inventory";
        SyncLegacyGearTiers();
        itemDrop = equipLine;
        return true;
    }

    private void RegeneratePartyOneHitPoint()
    {
        foreach (var member in party.Members)
        {
            if (!member.IsAlive || member.Health >= member.MaxHealth)
            {
                continue;
            }

            member.Health++;
        }
    }

    private void ReviveFallenAfterVictory()
    {
        if (party.Members.All(member => !member.IsAlive))
        {
            return;
        }

        foreach (var member in party.Members)
        {
            if (!member.IsAlive)
            {
                member.Health = 1;
            }
        }
    }

    private string GetSavePath()
    {
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "notima");
        Directory.CreateDirectory(baseDir);
        return Path.Combine(baseDir, "save.json");
    }

    private void SaveGame()
    {
        if (uiMode == UiMode.Encounter)
        {
            statusLine = "You cannot save in the middle of a fight.";
            return;
        }

        var save = new SaveGameData
        {
            PlayerX = playerCell.X,
            PlayerY = playerCell.Y,
            DungeonX = dungeonCell.X,
            DungeonY = dungeonCell.Y,
            Party = PartySaveData.FromParty(party),
            VisitedLandmarks = visitedLandmarks.Select(point => new PointSaveData { X = point.X, Y = point.Y }).ToList(),
            Dungeon = dungeon is null ? null : DungeonSaveData.FromDungeon(dungeon),
        };

        File.WriteAllText(GetSavePath(), JsonSerializer.Serialize(save, new JsonSerializerOptions { WriteIndented = true }));
        statusLine = "Game saved.";
    }

    private void LoadGame()
    {
        var path = GetSavePath();
        if (!File.Exists(path))
        {
            statusLine = "No saved game found.";
            return;
        }

        var save = JsonSerializer.Deserialize<SaveGameData>(File.ReadAllText(path));
        if (save is null)
        {
            statusLine = "Save data could not be read.";
            return;
        }

        LoadMapFromDisk();
        playerCell = new GridPoint(save.PlayerX, save.PlayerY);
        dungeonCell = new GridPoint(save.DungeonX, save.DungeonY);
        party = save.Party.ToParty();
        EnsurePartyEquipmentState();
        visitedLandmarks.Clear();
        foreach (var point in save.VisitedLandmarks)
        {
            visitedLandmarks.Add(new GridPoint(point.X, point.Y));
        }

        dungeon = save.Dungeon?.ToDungeon();
        townMenu = null;
        encounter = null;
        encounterFromDungeon = false;
        encounterIsBoss = false;
        panelTitle = string.Empty;
        panelLines.Clear();
        uiMode = dungeon is null ? UiMode.Overworld : UiMode.Dungeon;
        statusLine = "Game loaded.";
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

        party.Mana = party.MaxMana;
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
        var scaledPosition = UiPosition(position);
        var scaledPixel = MathF.Max(1.0f, scale * uiScale);
        var cursorX = scaledPosition.X;
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
                        new RectangleF(cursorX + (column * scaledPixel), scaledPosition.Y + (row * scaledPixel), scaledPixel, scaledPixel),
                        color);
                }
            }

            cursorX += (pattern[0].Length + 1) * scaledPixel;
        }
    }

    private void DrawPanel(float x, float y, float width, float height, Color color)
    {
        spriteBatch.Draw(whiteTexture, UiRect(new RectangleF(x, y, width, height)), color);
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
            UiMode.Town => panelTitle,
            UiMode.Dungeon => $"DUNGEON {dungeon?.Level ?? 1}",
            UiMode.Encounter => "ENCOUNTER",
            UiMode.Dialog => panelTitle,
            _ => "OVERWORLD"
        };
        Window.Title = $"notima | {modeText} | HP {party.TotalHealth}/{party.MaxTotalHealth} | GOLD {party.Gold} | FOOD {party.Food} | ATK {GetPartyAttackRating()} DEF {GetPartyDefenseRating()} K{party.Keys}";
    }

    private void UpdateLayout()
    {
        var viewportWidth = GraphicsDevice.Presenter.BackBuffer.Width;
        var viewportHeight = GraphicsDevice.Presenter.BackBuffer.Height;
        uiScale = MathF.Max(0.5f, MathF.Min(viewportWidth / BaseWidth, viewportHeight / BaseHeight));
        layoutOffsetX = (viewportWidth - (BaseWidth * uiScale)) * 0.5f;
        layoutOffsetY = (viewportHeight - (BaseHeight * uiScale)) * 0.5f;
    }

    private Vector2 UiPosition(Vector2 position) => new(layoutOffsetX + (position.X * uiScale), layoutOffsetY + (position.Y * uiScale));

    private RectangleF UiRect(RectangleF rect) => new(
        layoutOffsetX + (rect.X * uiScale),
        layoutOffsetY + (rect.Y * uiScale),
        rect.Width * uiScale,
        rect.Height * uiScale);

    private static RectangleF GetIsoTileSource(char symbol)
    {
        return symbol switch
        {
            '~' => new RectangleF(0, 7 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            '^' => new RectangleF(0, 2 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            '*' => new RectangleF(0, 0, IsoSheetCell, IsoSheetCell),
            'F' => new RectangleF(0, 4 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            '=' => new RectangleF(0, 6 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'P' => new RectangleF(0, 5 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'T' => new RectangleF(0, 3 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'K' => new RectangleF(0, 8 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'R' => new RectangleF(0, 9 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'S' => new RectangleF(0, 10 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'H' => new RectangleF(0, 11 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'C' => new RectangleF(0, 12 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'D' => new RectangleF(0, 13 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'k' => new RectangleF(0, 9 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'x' => new RectangleF(0, 8 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            'B' => new RectangleF(0, 13 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
            _ => new RectangleF(0, 14 * IsoSheetPitch, IsoSheetCell, IsoSheetCell),
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

    public int Mana { get; set; }

    public int MaxMana { get; set; }

    public int WardCharges { get; set; }

    public int WeaponTier { get; set; }

    public int ArmorTier { get; set; }

    public int Keys { get; set; }

    public int Steps { get; set; }

    public List<string> Inventory { get; } = [];

    public List<PartyMember> Members { get; } = [];

    public int TotalHealth => Members.Sum(member => member.Health);

    public int MaxTotalHealth => Members.Sum(member => member.MaxHealth);

    public void ResetMembers(int memberHealth)
    {
        if (Members.Count == 0)
        {
            Members.Add(new PartyMember("AVA", new Color(176, 166, 156), memberHealth));
            Members.Add(new PartyMember("BRI", new Color(97, 118, 142), memberHealth));
            Members.Add(new PartyMember("CYR", new Color(124, 92, 100), memberHealth));
            Members.Add(new PartyMember("DAS", new Color(129, 112, 76), memberHealth));
            Mana = MaxMana;
            WardCharges = 0;
            return;
        }

        foreach (var member in Members)
        {
            member.MaxHealth = memberHealth;
            member.Health = memberHealth;
        }

        Mana = MaxMana;
        WardCharges = 0;
    }
}

internal sealed class PartyMember(string name, Color tint, int maxHealth)
{
    public string Name { get; } = name;

    public Color Tint { get; } = tint;

    public int MaxHealth { get; set; } = maxHealth;

    public int Health { get; set; } = maxHealth;

    public string? EquippedWeaponId { get; set; }

    public string? EquippedArmorId { get; set; }

    public bool IsAlive => Health > 0;
}

internal sealed class TownMenuState
{
    public char Symbol { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public List<string> Options { get; init; } = [];

    public int SelectedIndex { get; set; }

    public static TownMenuState Create(char symbol)
    {
        return symbol switch
        {
            'T' => new TownMenuState
            {
                Symbol = symbol,
                Title = "TOWN",
                Description = "Lantern light, bread ovens, and closed shutters.",
                Options = ["REST - 8 GOLD", "BUY RATIONS +25 FOOD - 6 GOLD", "FORGE WEAPON +1", "BUY ARMOR +1", "LEAVE"],
            },
            'H' => new TownMenuState
            {
                Symbol = symbol,
                Title = "HARBOR",
                Description = "Salt wind and rope creak across dark piers.",
                Options = ["REST - 8 GOLD", "BUY RATIONS +25 FOOD - 6 GOLD", "BUY ARMOR +1", "LEAVE"],
            },
            'C' => new TownMenuState
            {
                Symbol = symbol,
                Title = "CAMP",
                Description = "A guarded fire and a few quiet bedrolls.",
                Options = ["REST - 8 GOLD", "BUY RATIONS +25 FOOD - 6 GOLD", "LEAVE"],
            },
            'K' => new TownMenuState
            {
                Symbol = symbol,
                Title = "KEEP",
                Description = "The captain will drill the party for 30 gold.",
                Options = ["TRAIN - 30 GOLD", "LEAVE"],
            },
            'S' => new TownMenuState
            {
                Symbol = symbol,
                Title = "SHRINE",
                Description = "A hush settles around old carved stone.",
                Options = ["PRAY", "LEAVE"],
            },
            _ => new TownMenuState
            {
                Symbol = symbol,
                Title = "RUINS",
                Description = "Fallen masonry and wind through broken arches.",
                Options = ["SEARCH", "LEAVE"],
            },
        };
    }
}

internal sealed class DungeonState
{
    public int Level { get; set; }

    public GridPoint Start { get; set; }

    public GridPoint ExitPoint { get; set; }

    public GridPoint? BossExitPoint { get; set; }

    public GridPoint? GatePoint { get; set; }

    public bool BossFloor { get; set; }

    public bool BossDefeated { get; set; }

    public List<string> Rows { get; set; } = [];

    public int Width => Rows.Count == 0 ? 0 : Rows[0].Length;

    public int Height => Rows.Count;

    public char GetTile(GridPoint point) => Rows[point.Y][point.X];

    public void SetTile(GridPoint point, char symbol)
    {
        var chars = Rows[point.Y].ToCharArray();
        chars[point.X] = symbol;
        Rows[point.Y] = new string(chars);
    }
}

internal sealed class SaveGameData
{
    public int PlayerX { get; set; }

    public int PlayerY { get; set; }

    public int DungeonX { get; set; }

    public int DungeonY { get; set; }

    public PartySaveData Party { get; set; } = new();

    public List<PointSaveData> VisitedLandmarks { get; set; } = [];

    public DungeonSaveData? Dungeon { get; set; }
}

internal sealed class PartySaveData
{
    public int Level { get; set; }

    public int Gold { get; set; }

    public int Food { get; set; }

    public int Mana { get; set; }

    public int MaxMana { get; set; }

    public int WardCharges { get; set; }

    public int WeaponTier { get; set; }

    public int ArmorTier { get; set; }

    public int Keys { get; set; }

    public int Steps { get; set; }

    public List<string> Inventory { get; set; } = [];

    public List<PartyMemberSaveData> Members { get; set; } = [];

    public static PartySaveData FromParty(PartyState party)
    {
        return new PartySaveData
        {
            Level = party.Level,
            Gold = party.Gold,
            Food = party.Food,
            Mana = party.Mana,
            MaxMana = party.MaxMana,
            WardCharges = party.WardCharges,
            WeaponTier = party.WeaponTier,
            ArmorTier = party.ArmorTier,
            Keys = party.Keys,
            Steps = party.Steps,
            Inventory = party.Inventory.ToList(),
            Members = party.Members.Select(member => new PartyMemberSaveData
            {
                Name = member.Name,
                TintR = member.Tint.R,
                TintG = member.Tint.G,
                TintB = member.Tint.B,
                MaxHealth = member.MaxHealth,
                Health = member.Health,
                EquippedWeaponId = member.EquippedWeaponId,
                EquippedArmorId = member.EquippedArmorId,
            }).ToList(),
        };
    }

    public PartyState ToParty()
    {
        var party = new PartyState
        {
            Level = Level,
            Gold = Gold,
            Food = Food,
            Mana = Mana,
            MaxMana = MaxMana,
            WardCharges = WardCharges,
            WeaponTier = WeaponTier,
            ArmorTier = ArmorTier,
            Keys = Keys,
            Steps = Steps,
        };
        party.Inventory.AddRange(Inventory);

        foreach (var member in Members)
        {
            party.Members.Add(new PartyMember(member.Name, new Color(member.TintR, member.TintG, member.TintB), member.MaxHealth)
            {
                Health = member.Health,
                EquippedWeaponId = member.EquippedWeaponId,
                EquippedArmorId = member.EquippedArmorId,
            });
        }

        return party;
    }
}

internal sealed class PartyMemberSaveData
{
    public string Name { get; set; } = string.Empty;

    public byte TintR { get; set; }

    public byte TintG { get; set; }

    public byte TintB { get; set; }

    public int MaxHealth { get; set; }

    public int Health { get; set; }

    public string? EquippedWeaponId { get; set; }

    public string? EquippedArmorId { get; set; }
}

internal sealed class DungeonSaveData
{
    public int Level { get; set; }

    public PointSaveData Start { get; set; } = new();

    public PointSaveData ExitPoint { get; set; } = new();

    public PointSaveData? BossExitPoint { get; set; }

    public PointSaveData? GatePoint { get; set; }

    public bool BossFloor { get; set; }

    public bool BossDefeated { get; set; }

    public List<string> Rows { get; set; } = [];

    public static DungeonSaveData FromDungeon(DungeonState dungeon)
    {
        return new DungeonSaveData
        {
            Level = dungeon.Level,
            Start = PointSaveData.FromPoint(dungeon.Start),
            ExitPoint = PointSaveData.FromPoint(dungeon.ExitPoint),
            BossExitPoint = dungeon.BossExitPoint is null ? null : PointSaveData.FromPoint(dungeon.BossExitPoint.Value),
            GatePoint = dungeon.GatePoint is null ? null : PointSaveData.FromPoint(dungeon.GatePoint.Value),
            BossFloor = dungeon.BossFloor,
            BossDefeated = dungeon.BossDefeated,
            Rows = dungeon.Rows.ToList(),
        };
    }

    public DungeonState ToDungeon()
    {
        return new DungeonState
        {
            Level = Level,
            Start = Start.ToPoint(),
            ExitPoint = ExitPoint.ToPoint(),
            BossExitPoint = BossExitPoint?.ToPoint(),
            GatePoint = GatePoint?.ToPoint(),
            BossFloor = BossFloor,
            BossDefeated = BossDefeated,
            Rows = Rows.ToList(),
        };
    }
}

internal sealed class PointSaveData
{
    public int X { get; set; }

    public int Y { get; set; }

    public static PointSaveData FromPoint(GridPoint point) => new() { X = point.X, Y = point.Y };

    public GridPoint ToPoint() => new(X, Y);
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

    public static EncounterState CreateDungeonPack(int level)
    {
        return new EncounterState
        {
            Name = "DUNGEON PACK",
            RewardGold = 10 + (level * 3),
            RewardFood = 2 + level,
            Enemies =
            [
                new EnemyUnit("WOLF", 8 + level, 3 + (level / 2), 6 + level, 3, 0),
                new EnemyUnit("LEECH", 7 + level, 3, 5 + level, 3, 1),
                new EnemyUnit("BANDIT", 9 + level, 3 + (level / 2), 6 + level, 2, 2),
            ],
        };
    }

    public static EncounterState CreateDungeonBoss(int level)
    {
        return new EncounterState
        {
            Name = "DREAD LORD",
            RewardGold = 35 + (level * 8),
            RewardFood = 10 + level,
            Enemies =
            [
                new EnemyUnit("BANDIT", 16 + (level * 2), 5 + level, 9 + level, 3, 1),
                new EnemyUnit("WOLF", 12 + level, 4 + (level / 2), 7 + level, 2, 0),
                new EnemyUnit("LEECH", 11 + level, 4, 7 + level, 2, 2),
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

internal sealed class EquipmentDefinition(string id, string name, EquipmentSlot slot, int attack, int defense, int cost, int rank, int lootLevel)
{
    public string Id { get; } = id;

    public string Name { get; } = name;

    public EquipmentSlot Slot { get; } = slot;

    public int Attack { get; } = attack;

    public int Defense { get; } = defense;

    public int Cost { get; } = cost;

    public int Rank { get; } = rank;

    public int LootLevel { get; } = lootLevel;
}

internal enum EquipmentSlot
{
    Weapon,
    Armor,
}

internal enum UiMode
{
    Overworld,
    Town,
    Dungeon,
    Encounter,
    Dialog,
}

internal enum CombatAction
{
    Attack,
    Spell,
}

internal enum SpellKind
{
    Ember,
    Mend,
    Aegis,
}

internal readonly record struct CombatTurnEntry(bool IsParty, int Index);

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
            ['+'] = ["00000", "00100", "00100", "11111", "00100", "00100", "00000"],
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
