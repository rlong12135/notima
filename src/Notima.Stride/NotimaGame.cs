using System.Text.Json;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;

namespace Notima.Stride;

public sealed class NotimaGame : Game
{
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
    private const int EncounterPanelHeight = 372;

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
        enemyTexture?.Dispose();
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
            SaveGame();
            return;
        }

        if (Input.IsKeyPressed(Keys.F9))
        {
            LoadGame();
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
            InteractWithDungeonTile();
            return;
        }

        if (Input.IsKeyPressed(Keys.R))
        {
            LeaveDungeon("You withdraw from the dungeon.");
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
        TryMoveDungeon(delta);
    }

    private void HandleDialogInput()
    {
        if (Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
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
            WeaponTier = 0,
            ArmorTier = 0,
            Keys = 0,
            Steps = 0
        };
        party.ResetMembers(6);
        encounter = null;
        townMenu = null;
        dungeon = null;
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
            statusLine = $"You slip away from the {encounter.Name}.";
            encounter = null;
            uiMode = UiMode.Overworld;
            panelLines.Clear();
            panelTitle = string.Empty;
            return;
        }

        var enemy = encounter.Enemies.FirstOrDefault(enemyUnit => enemyUnit.IsAlive);
        var damage = Math.Max(1, (enemy is null ? 2 : random.Next(enemy.AttackMin, enemy.AttackMax + 1)) - party.Level - party.ArmorTier);
        var targetPartyIndex = GetRandomAlivePartyIndex(preferFront: true) ?? 0;
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

        var playerDamage = random.Next(4, 9) + party.Level + (party.WeaponTier * 2);
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

        var wardReduction = party.WardCharges > 0 ? 2 : 0;
        if (party.WardCharges > 0)
        {
            party.WardCharges--;
        }
        if (random.NextDouble() < 0.16)
        {
            roundEvents.Add($"{enemy.Name} misses {party.Members[targetPartyIndex].Name}");
            audioPlayer?.PlaySwish();
            return;
        }
        var enemyDamageBase = random.Next(enemy.AttackMin, enemy.AttackMax + 1);
        var enemyDamage = Math.Max(1, enemyDamageBase - (party.Level / 2) - party.ArmorTier - wardReduction);
        if (targetPartyIndex >= 2 && GetFrontAlivePartyIndex() is not null)
        {
            enemyDamage = Math.Max(1, enemyDamage - 2);
        }

        DamagePartyMember(targetPartyIndex, enemyDamage);
        roundEvents.Add($"{enemy.Name} hits {party.Members[targetPartyIndex].Name} for {enemyDamage}");
        audioPlayer?.PlayClash();
    }

    private void HandleDefeat()
    {
        party.ResetMembers(6 + ((party.Level - 1) * 2));
        party.Gold = Math.Max(0, party.Gold - 12);
        playerCell = new GridPoint(map.Start.X, map.Start.Y);
        dungeon = null;
        townMenu = null;
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
                var weaponCost = GetWeaponUpgradeCost();
                if (party.Gold < weaponCost)
                {
                    statusLine = $"{GetNextWeaponName()} costs {weaponCost} gold.";
                    return;
                }

                party.Gold -= weaponCost;
                party.WeaponTier++;
                statusLine = $"The smith equips {GetWeaponName()}.";
                break;
            case "BUY ARMOR +1":
                var armorCost = GetArmorUpgradeCost();
                if (party.Gold < armorCost)
                {
                    statusLine = $"{GetNextArmorName()} costs {armorCost} gold.";
                    return;
                }

                party.Gold -= armorCost;
                party.ArmorTier++;
                statusLine = $"The outfitter fits {GetArmorName()}.";
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
                var armorCost = GetArmorUpgradeCost();
                if (party.Gold < armorCost)
                {
                    statusLine = $"{GetNextArmorName()} costs {armorCost} gold.";
                    return;
                }

                party.Gold -= armorCost;
                party.ArmorTier++;
                CloseTownMenu($"A sea captain sells you {GetArmorName()}.");
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
        townMenu = null;
        panelTitle = string.Empty;
        panelLines.Clear();
        uiMode = dungeon is null ? UiMode.Overworld : UiMode.Dungeon;
        statusLine = message;
    }

    private void EnterDungeon()
    {
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

        if (TryAwardEquipmentDrop(out var itemDrop))
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
        DrawPanel(28, 28, 692, 664, new Color(26, 35, 49, 240));
        DrawFrame(new RectangleF(28, 28, 692, 664), new Color(102, 124, 171), 2);

        var rows = dungeon is null ? map.Rows : dungeon.Rows;
        var currentCell = dungeon is null ? playerCell : dungeonCell;

        for (var y = 0; y < rows.Count; y++)
        {
            var row = rows[y];
            for (var x = 0; x < row.Length; x++)
            {
                var symbol = row[x];
                var destination = GetIsoTileDestination(x, y);
                var tileSource = GetIsoTileSource(symbol);
                spriteBatch.Draw(tileTexture, UiRect(destination), tileSource, Color.White, 0, Vector2.Zero);
                if (dungeon is not null)
                {
                    DrawDungeonStoneFill(symbol, destination);
                    DrawDungeonFeatureMarker(symbol, destination);
                }

            }
        }

        var cursorDestination = GetIsoHighlightDestination(currentCell.X, currentCell.Y);
        spriteBatch.Draw(whiteTexture, UiRect(cursorDestination), new Color(156, 138, 78, 26));
        DrawFrame(cursorDestination, new Color(170, 148, 84), 2);

        var playerFrame = GetPlayerSourceFrame(0);
        var playerDestination = GetIsoCharacterDestination(currentCell.X, currentCell.Y, 0.0f);

        DrawPartyTrail(cursorDestination);
        spriteBatch.Draw(whiteTexture, UiRect(new RectangleF(playerDestination.X + 10.0f, playerDestination.Y + playerDestination.Height - 6.0f, playerDestination.Width - 20.0f, 4.0f)), new Color(0, 0, 0, 82));
        var playerSource = new RectangleF(playerFrame.X, playerFrame.Y, playerFrame.Width, playerFrame.Height);
        spriteBatch.Draw(playerTexture, UiRect(playerDestination), playerSource, Color.White, 0, Vector2.Zero);
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
        DrawText($"WPN {party.WeaponTier}", new Vector2(HudX + 22, 258), new Color(170, 130, 104), 2);
        DrawText($"ARM {party.ArmorTier}", new Vector2(HudX + 170, 258), new Color(133, 140, 160), 2);
        DrawText($"KEYS {party.Keys}", new Vector2(HudX + 314, 258), new Color(171, 150, 99), 2);
        DrawText($"STEPS {party.Steps}", new Vector2(HudX + 22, 288), new Color(133, 140, 160), 2);
        DrawText(GetWeaponName(), new Vector2(HudX + 22, 314), new Color(158, 128, 104), 1);
        DrawText(GetArmorName(), new Vector2(HudX + 220, 314), new Color(128, 136, 151), 1);

        DrawText("TILE", new Vector2(HudX + 22, 340), new Color(178, 150, 96), 2);
        DrawWrappedText(DescribeCurrentTile(), new Vector2(HudX + 22, 370), 2, HudWidth - 44, new Color(158, 169, 189));

        DrawText("STATUS", new Vector2(HudX + 22, 450), new Color(178, 150, 96), 2);
        DrawWrappedText(statusLine, new Vector2(HudX + 22, 480), 2, HudWidth - 44, new Color(166, 170, 180));

        DrawText("PARTY", new Vector2(HudX + 22, 550), new Color(178, 150, 96), 2);
        DrawPartyBanner(new Vector2(HudX + 22, 574));

        DrawText("MOVE WASD OR ARROWS", new Vector2(HudX + 22, 622), new Color(126, 145, 172), 2);
        DrawText("ENTER INTERACTS", new Vector2(HudX + 22, 648), new Color(126, 145, 172), 2);
        DrawText("F5 SAVE  F9 LOAD", new Vector2(HudX + 22, 674), new Color(126, 145, 172), 2);
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

        var lineY = uiMode == UiMode.Encounter ? panelRect.Y + 226 : panelRect.Y + 72;
        foreach (var line in panelLines)
        {
            DrawWrappedText(line, new Vector2(panelRect.X + 22, lineY), 2, (int)panelRect.Width - 44, new Color(232, 238, 252));
            lineY += uiMode == UiMode.Encounter ? 38 : 32;
        }
    }

    private RectangleF GetPanelRect()
    {
        return uiMode == UiMode.Encounter
            ? new RectangleF(84, 266, 580, EncounterPanelHeight)
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

    private void DrawPartyBanner(Vector2 origin)
    {
        for (var i = 0; i < party.Members.Count; i++)
        {
            var member = party.Members[i];
            var frame = GetPlayerSourceFrameFor(i == 0 ? facing : Direction.Down, ((int)(totalTime * 6.0f) + i) % 3, i);
            var sourceRect = new RectangleF(frame.X, frame.Y, frame.Width, frame.Height);
            var bounce = MathF.Sin((totalTime * 5.0f) + i) * 2.0f;
            var destination = new RectangleF(origin.X + (i * 42.0f), origin.Y + bounce, 30.0f, 30.0f);
            spriteBatch.Draw(whiteTexture, UiRect(new RectangleF(destination.X + 4.0f, destination.Y + destination.Height - 3.0f, destination.Width - 8.0f, 2.0f)), new Color(0, 0, 0, 56));
            var tint = member.IsAlive ? member.Tint : new Color(84, 92, 112);
            spriteBatch.Draw(playerTexture, UiRect(destination), sourceRect, tint, 0, Vector2.Zero);
            DrawText($"{member.Health}/{member.MaxHealth}", new Vector2(destination.X - 2.0f, destination.Bottom + 6.0f), member.IsAlive ? new Color(220, 230, 255) : new Color(130, 136, 148), 1);
        }
    }

    private void DrawEncounterAnimation(RectangleF panelRect)
    {
        var boardRect = new RectangleF(panelRect.X + 18, panelRect.Y + 56, panelRect.Width - 36, 144);
        DrawPanel(boardRect.X, boardRect.Y, boardRect.Width, boardRect.Height, new Color(28, 36, 50, 220));
        DrawFrame(boardRect, new Color(106, 128, 177), 1);

        var boardOrigin = new Vector2(boardRect.X + (boardRect.Width * 0.5f) - 34.0f, boardRect.Y + 22.0f);
        for (var boardY = 0; boardY < 3; boardY++)
        {
            for (var boardX = 0; boardX < 4; boardX++)
            {
                var tile = GetEncounterTileDestination(boardOrigin, boardX, boardY);
                spriteBatch.Draw(tileTexture, UiRect(tile), GetIsoTileSource('.'), Color.White, 0, Vector2.Zero);
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
            spriteBatch.Draw(whiteTexture, UiRect(new RectangleF(destination.X + 8.0f, destination.Y + destination.Height - 5.0f, destination.Width - 16.0f, 3.0f)), shadowColor);
            var tint = member.IsAlive ? member.Tint : new Color(78, 84, 98);
            spriteBatch.Draw(playerTexture, UiRect(destination), sourceRect, tint, 0, Vector2.Zero);
            if (!member.IsAlive)
            {
                spriteBatch.Draw(whiteTexture, UiRect(new RectangleF(destination.X + 6.0f, destination.Y + 16.0f, destination.Width - 12.0f, 2.0f)), new Color(176, 82, 82));
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
            spriteBatch.Draw(tileTexture, UiRect(enemyTile), GetIsoTileSource(encounter.Name == "FEN LEECHES" ? 'F' : '*'), Color.White, 0, Vector2.Zero);

            if (i == selectedEnemyIndex && IsEnemyTargetable(i))
            {
                var highlight = new RectangleF(enemyTile.X + 8.0f, enemyTile.Y + 14.0f, enemyTile.Width - 16.0f, enemyTile.Height - 28.0f);
                spriteBatch.Draw(whiteTexture, UiRect(highlight), new Color(142, 112, 64, 36));
                DrawFrame(highlight, new Color(164, 132, 76), 1);
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
        var source = GetEnemySourceFrame(enemyName, frameIndex);
        var destination = new RectangleF(origin.X, origin.Y + bob, source.Width * scale, source.Height * scale);
        spriteBatch.Draw(whiteTexture, UiRect(new RectangleF(destination.X + 4.0f, destination.Bottom - 4.0f, destination.Width - 8.0f, 3.0f)), new Color(0, 0, 0, 56));
        spriteBatch.Draw(enemyTexture, UiRect(destination), new RectangleF(source.X, source.Y, source.Width, source.Height), Color.White, 0, Vector2.Zero);
    }

    private static Rectangle GetEnemySourceFrame(string enemyName, int frameIndex)
    {
        var row = enemyName switch
        {
            "WOLF" => 0,
            "LEECH" => 1,
            _ => 2,
        };

        return new Rectangle(frameIndex * 25, row * 25, 24, 24);
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

        ReviveFallenAfterVictory();

        party.Gold += encounter.RewardGold;
        party.Food += encounter.RewardFood;

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
        OpenDialog(
            "VICTORY",
            $"{encounter.Name} FALLS.",
            $"GOLD +{encounter.RewardGold}",
            $"FOOD +{encounter.RewardFood}",
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
                var target = GetSelectedEnemy() ?? encounter?.Enemies.FirstOrDefault(enemy => enemy.IsAlive);
                if (target is null)
                {
                    return false;
                }

                var emberDamage = 8 + party.Level + party.WeaponTier;
                target.Health = Math.Max(0, target.Health - emberDamage);
                roundEvents.Add($"AVA casts EMBER for {emberDamage}");
                if (!target.IsAlive)
                {
                    selectedEnemyIndex = GetDefaultSelectedEnemy();
                }

                return true;
            case SpellKind.Mend:
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

    private int GetWeaponUpgradeCost() => 18 + (party.WeaponTier * 12);

    private int GetArmorUpgradeCost() => 16 + (party.ArmorTier * 10);

    private string GetWeaponName() => party.WeaponTier switch
    {
        0 => "RUSTED KNIFE",
        1 => "IRON BLADE",
        2 => "MERCENARY SWORD",
        3 => "TEMPERED SABRE",
        4 => "STARFORGED EDGE",
        _ => "MYTHIC EDGE",
    };

    private string GetArmorName() => party.ArmorTier switch
    {
        0 => "PATCHED CLOTH",
        1 => "LEATHER JACK",
        2 => "RING MAIL",
        3 => "LAMELLAR COAT",
        4 => "BLACK PLATE",
        _ => "WARDEN MAIL",
    };

    private string GetNextWeaponName() => party.WeaponTier switch
    {
        0 => "IRON BLADE",
        1 => "MERCENARY SWORD",
        2 => "TEMPERED SABRE",
        3 => "STARFORGED EDGE",
        _ => "MYTHIC EDGE",
    };

    private string GetNextArmorName() => party.ArmorTier switch
    {
        0 => "LEATHER JACK",
        1 => "RING MAIL",
        2 => "LAMELLAR COAT",
        3 => "BLACK PLATE",
        _ => "WARDEN MAIL",
    };

    private bool TryAwardEquipmentDrop(out string itemDrop)
    {
        itemDrop = string.Empty;
        if (random.NextDouble() < 0.18 && party.WeaponTier < 4)
        {
            party.WeaponTier++;
            itemDrop = GetWeaponName();
            return true;
        }

        if (random.NextDouble() < 0.18 && party.ArmorTier < 4)
        {
            party.ArmorTier++;
            itemDrop = GetArmorName();
            return true;
        }

        return false;
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
        Window.Title = $"notima | {modeText} | HP {party.TotalHealth}/{party.MaxTotalHealth} | GOLD {party.Gold} | FOOD {party.Food} | W{party.WeaponTier} A{party.ArmorTier} K{party.Keys}";
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
            Members = party.Members.Select(member => new PartyMemberSaveData
            {
                Name = member.Name,
                TintR = member.Tint.R,
                TintG = member.Tint.G,
                TintB = member.Tint.B,
                MaxHealth = member.MaxHealth,
                Health = member.Health,
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

        foreach (var member in Members)
        {
            party.Members.Add(new PartyMember(member.Name, new Color(member.TintR, member.TintG, member.TintB), member.MaxHealth)
            {
                Health = member.Health
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
