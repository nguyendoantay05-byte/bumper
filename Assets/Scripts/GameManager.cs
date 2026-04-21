using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều phối trận đấu: spawn người chơi, spawn bot, theo dõi ai còn sống và kết thúc trận.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private enum MapStyle
    {
        ClassicIsland,
        LagoonRing,
        LongStrip,
        PebbleIsland,
        StarfishBay,
        TwinLagoon,
        CrescentAtoll,
        CoralMaze,
        SplitShoals,
        SunkenCrown,
        TurtleBack,
        DiamondCay
    }

    private static Sprite cachedArenaFillSprite;
    private static Sprite cachedArenaRingSprite;
    private static Sprite cachedArenaPatternSprite;
    private static Sprite cachedSquareSprite;
    private static Sprite cachedIslandSprite;
    private static Sprite cachedWaterPatternSprite;
    private static Sprite cachedBodySprite;
    private static Sprite cachedSmallCircleSprite;
    private static Sprite cachedStarSprite;
    private static Sprite cachedArrowSprite;
    private static Sprite cachedPalmSprite;
    private static Sprite cachedFighterSprite;
    private static Sprite cachedShadowSprite;

    public enum MatchState
    {
        WaitingToStart,
        Playing,
        Finished
    }

    [Header("Prefabs")]
    [SerializeField] private PlayerController playerPrefab;
    [SerializeField] private BotController botPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform[] botSpawnPoints;

    [Header("Match Settings")]
    [SerializeField] private int defaultBotCount = 1;
    [SerializeField] private float spawnRadiusFallback = 8.2f;
    [SerializeField] private float spawnProtectionDuration = 3f;
    [SerializeField] private float runtimeArenaRadius = 12.8f;

    private readonly List<FighterController> activeFighters = new List<FighterController>();
    private PlayerController playerInstance;
    private CameraFollow2D cameraFollow;
    private int eliminatedBots;
    private int playerKnockouts;
    private MatchState state = MatchState.WaitingToStart;
    private float matchStartTime;
    private MapStyle currentMapStyle;

    public MatchState State => state;
    public bool IsMatchRunning => state == MatchState.Playing;
    public PlayerController PlayerInstance => playerInstance;
    public int EliminatedBots => eliminatedBots;
    public int PlayerKnockouts => playerKnockouts;
    public int RemainingBots => Mathf.Max(0, GetBotCount() - eliminatedBots);

    public string PlayerName => PlayerData.GetPlayerName();
    public bool IsSpawnProtectionActive => state == MatchState.Playing && Time.time - matchStartTime < spawnProtectionDuration;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ConfigureRuntimeScene();
    }

    private void Start()
    {
        BeginMatch();
    }

    public void BeginMatch()
    {
        ClearMatch();
        SelectRandomMapStyle();
        ConfigureRuntimeScene();
        matchStartTime = Time.time;
        SpawnMatch();
        state = MatchState.Playing;
    }

    public void RestartMatch()
    {
        SceneLoader.ReloadCurrentScene();
    }

    public void ReturnToMenu()
    {
        SceneLoader.LoadMainMenu();
    }

    public void EliminateFighter(FighterController fighter)
    {
        if (fighter == null || fighter.IsEliminated)
        {
            return;
        }

        fighter.Eliminate();
        fighter.PlayFallIntoWater();

        if (fighter == playerInstance)
        {
            state = MatchState.Finished;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowResult(false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLose();
            }

            return;
        }

        if (fighter is BotController)
        {
            eliminatedBots++;

            FighterController impactSource = fighter.LastImpactSource;
            bool playerEarnedKnockout = impactSource != null
                && impactSource == playerInstance
                && Time.time - fighter.LastImpactTime <= 4f;

            if (playerEarnedKnockout)
            {
                playerKnockouts++;
            }

            if (playerInstance != null && !playerInstance.IsEliminated)
            {
                playerInstance.GrowAfterElimination(0.08f);
            }
        }

        RemoveFighterFromList(fighter);
        CheckWinCondition();
    }

    public List<FighterController> GetAllActiveFighters()
    {
        return activeFighters;
    }

    public FighterController GetNearestOpponent(FighterController seeker)
    {
        FighterController nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < activeFighters.Count; i++)
        {
            FighterController fighter = activeFighters[i];
            if (fighter == null || fighter == seeker || fighter.IsEliminated)
            {
                continue;
            }

            float distance = Vector2.Distance(seeker.Position, fighter.Position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = fighter;
            }
        }

        return nearest;
    }

    public FighterController GetHumanFighter()
    {
        return playerInstance;
    }

    private void SpawnMatch()
    {
        int botCount = GetBotCount();
        SpawnPlayer();
        SpawnBots(botCount);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshHUD();
        }
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPosition = GetPlayerSpawnPosition();
        playerInstance = playerPrefab != null
            ? Instantiate(playerPrefab, spawnPosition, Quaternion.identity)
            : CreateFallbackPlayer(spawnPosition);

        if (playerInstance == null)
        {
            playerInstance = CreateFallbackPlayer(spawnPosition);
        }

        playerInstance.SetDisplayName(PlayerName);
        PrepareSpawnedFighter(playerInstance, true, spawnPosition, 0);
        RegisterFighter(playerInstance);
        BindCameraTarget(playerInstance.transform);
    }

    private void SpawnBots(int botCount)
    {
        for (int i = 0; i < botCount; i++)
        {
            Vector3 spawnPosition = GetBotSpawnPosition(i, botCount);
            BotController bot = botPrefab != null
                ? Instantiate(botPrefab, spawnPosition, Quaternion.identity)
                : CreateFallbackBot(spawnPosition, i);

            if (bot == null)
            {
                bot = CreateFallbackBot(spawnPosition, i);
            }

            bot.SetDisplayName("Bot " + (i + 1));
            PrepareSpawnedFighter(bot, false, spawnPosition, i + 1);
            RegisterFighter(bot);
        }
    }

    private Vector3 GetPlayerSpawnPosition()
    {
        if (playerSpawnPoint != null)
        {
            return playerSpawnPoint.position;
        }

        if (ArenaBoundary.Instance != null)
        {
            return ArenaBoundary.Instance.Center;
        }

        return Vector3.zero;
    }

    private Vector3 GetBotSpawnPosition(int index, int botCount)
    {
        if (botSpawnPoints != null && index < botSpawnPoints.Length && botSpawnPoints[index] != null)
        {
            return botSpawnPoints[index].position;
        }

        Vector3 center = ArenaBoundary.Instance != null ? (Vector3)ArenaBoundary.Instance.Center : Vector3.zero;
        float angle = (Mathf.PI * 2f / Mathf.Max(1, botCount)) * index;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadiusFallback;
        return center + offset;
    }

    private void RegisterFighter(FighterController fighter)
    {
        if (fighter == null)
        {
            return;
        }

        activeFighters.Add(fighter);
    }

    private void RemoveFighterFromList(FighterController fighter)
    {
        if (fighter == null)
        {
            return;
        }

        activeFighters.Remove(fighter);
    }

    private void ClearMatch()
    {
        activeFighters.Clear();
        playerInstance = null;
        eliminatedBots = 0;
        playerKnockouts = 0;
        state = MatchState.WaitingToStart;
    }

    private void CheckWinCondition()
    {
        int aliveBots = 0;
        for (int i = 0; i < activeFighters.Count; i++)
        {
            if (activeFighters[i] is BotController && !activeFighters[i].IsEliminated)
            {
                aliveBots++;
            }
        }

        if (aliveBots <= 0 && playerInstance != null && !playerInstance.IsEliminated)
        {
            state = MatchState.Finished;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowResult(true);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWin();
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshHUD();
        }
    }

    private int GetBotCount()
    {
        int botCount = PlayerData.GetBotCount();
        if (botCount <= 0)
        {
            botCount = defaultBotCount;
        }

        return botCount;
    }

    private void SelectRandomMapStyle()
    {
        MapStyle[] mapStyles =
        {
            MapStyle.ClassicIsland,
            MapStyle.LagoonRing,
            MapStyle.LongStrip,
            MapStyle.PebbleIsland,
            MapStyle.StarfishBay,
            MapStyle.TwinLagoon,
            MapStyle.CrescentAtoll,
            MapStyle.CoralMaze,
            MapStyle.SplitShoals,
            MapStyle.SunkenCrown,
            MapStyle.TurtleBack,
            MapStyle.DiamondCay
        };

        currentMapStyle = mapStyles[Random.Range(0, mapStyles.Length)];
    }

    private void ConfigureRuntimeScene()
    {
        ConfigureMainCamera();
        ConfigureSceneLighting();
        ConfigureArenaBoundary();
        ConfigureSpawnPoints();
        ConfigureArenaVisual();
    }

    private void ConfigureMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }

        if (mainCamera == null)
        {
            return;
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 11.8f;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.2f, 0.62f, 0.94f, 1f);
        mainCamera.transform.position = new Vector3(0f, 2f, -10f);
        mainCamera.transform.rotation = Quaternion.identity;

        cameraFollow = mainCamera.GetComponent<CameraFollow2D>();
        if (cameraFollow == null)
        {
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow2D>();
        }
    }

    private void ConfigureSceneLighting()
    {
        Light[] sceneLights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i] != null)
            {
                sceneLights[i].enabled = false;
            }
        }
    }

    private void ConfigureArenaBoundary()
    {
        ArenaBoundary boundary = ArenaBoundary.Instance != null ? ArenaBoundary.Instance : FindAnyObjectByType<ArenaBoundary>();
        if (boundary == null)
        {
            return;
        }

        float islandScale = runtimeArenaRadius * 1.48f;
        float baseRadius = islandScale * 1.04f;
        float waveA = islandScale * 0.10f;
        float waveB = islandScale * 0.06f;
        float waveC = islandScale * 0.04f;
        float holeRadius = 0f;
        Vector2 holeOffset = Vector2.zero;
        ArenaBoundary.HoleZone[] extraHoles = null;

        switch (currentMapStyle)
        {
            case MapStyle.ClassicIsland:
                baseRadius = islandScale * 1.02f;
                waveA = islandScale * 0.08f;
                waveB = islandScale * 0.05f;
                waveC = islandScale * 0.035f;
                break;
            case MapStyle.LagoonRing:
                baseRadius = islandScale * 1.06f;
                waveA = islandScale * 0.09f;
                waveB = islandScale * 0.05f;
                waveC = islandScale * 0.03f;
                holeRadius = runtimeArenaRadius * 0.24f;
                break;
            case MapStyle.LongStrip:
                baseRadius = islandScale * 0.98f;
                waveA = islandScale * 0.16f;
                waveB = islandScale * 0.03f;
                waveC = islandScale * 0.02f;
                break;
            case MapStyle.PebbleIsland:
                baseRadius = islandScale * 0.94f;
                waveA = islandScale * 0.18f;
                waveB = islandScale * 0.1f;
                waveC = islandScale * 0.06f;
                break;
            case MapStyle.StarfishBay:
                baseRadius = islandScale * 1f;
                waveA = islandScale * 0.2f;
                waveB = islandScale * 0.11f;
                waveC = islandScale * 0.09f;
                break;
            case MapStyle.TwinLagoon:
                baseRadius = islandScale * 1.08f;
                waveA = islandScale * 0.12f;
                waveB = islandScale * 0.06f;
                waveC = islandScale * 0.05f;
                extraHoles = new[]
                {
                    new ArenaBoundary.HoleZone(new Vector2(-runtimeArenaRadius * 0.33f, runtimeArenaRadius * 0.12f), runtimeArenaRadius * 0.18f),
                    new ArenaBoundary.HoleZone(new Vector2(runtimeArenaRadius * 0.29f, -runtimeArenaRadius * 0.08f), runtimeArenaRadius * 0.16f)
                };
                break;
            case MapStyle.CrescentAtoll:
                baseRadius = islandScale * 1.1f;
                waveA = islandScale * 0.11f;
                waveB = islandScale * 0.07f;
                waveC = islandScale * 0.04f;
                holeRadius = runtimeArenaRadius * 0.33f;
                holeOffset = new Vector2(runtimeArenaRadius * 0.24f, runtimeArenaRadius * 0.24f);
                break;
            case MapStyle.CoralMaze:
                baseRadius = islandScale * 1.06f;
                waveA = islandScale * 0.16f;
                waveB = islandScale * 0.11f;
                waveC = islandScale * 0.08f;
                extraHoles = new[]
                {
                    new ArenaBoundary.HoleZone(new Vector2(-runtimeArenaRadius * 0.18f, runtimeArenaRadius * 0.1f), runtimeArenaRadius * 0.12f),
                    new ArenaBoundary.HoleZone(new Vector2(runtimeArenaRadius * 0.21f, runtimeArenaRadius * 0.22f), runtimeArenaRadius * 0.1f),
                    new ArenaBoundary.HoleZone(new Vector2(runtimeArenaRadius * 0.06f, -runtimeArenaRadius * 0.18f), runtimeArenaRadius * 0.13f)
                };
                break;
            case MapStyle.SplitShoals:
                baseRadius = islandScale * 1.14f;
                waveA = islandScale * 0.15f;
                waveB = islandScale * 0.06f;
                waveC = islandScale * 0.05f;
                extraHoles = new[]
                {
                    new ArenaBoundary.HoleZone(new Vector2(0f, runtimeArenaRadius * 0.02f), runtimeArenaRadius * 0.18f),
                    new ArenaBoundary.HoleZone(new Vector2(0f, -runtimeArenaRadius * 0.3f), runtimeArenaRadius * 0.11f)
                };
                break;
            case MapStyle.SunkenCrown:
                baseRadius = islandScale * 1.12f;
                waveA = islandScale * 0.2f;
                waveB = islandScale * 0.12f;
                waveC = islandScale * 0.09f;
                holeRadius = runtimeArenaRadius * 0.2f;
                extraHoles = new[]
                {
                    new ArenaBoundary.HoleZone(new Vector2(-runtimeArenaRadius * 0.34f, 0f), runtimeArenaRadius * 0.1f),
                    new ArenaBoundary.HoleZone(new Vector2(runtimeArenaRadius * 0.34f, 0f), runtimeArenaRadius * 0.1f),
                    new ArenaBoundary.HoleZone(new Vector2(0f, runtimeArenaRadius * 0.34f), runtimeArenaRadius * 0.1f)
                };
                break;
            case MapStyle.TurtleBack:
                baseRadius = islandScale * 1.18f;
                waveA = islandScale * 0.09f;
                waveB = islandScale * 0.04f;
                waveC = islandScale * 0.03f;
                break;
            case MapStyle.DiamondCay:
                baseRadius = islandScale * 0.98f;
                waveA = islandScale * 0.06f;
                waveB = islandScale * 0.02f;
                waveC = islandScale * 0.02f;
                extraHoles = new[]
                {
                    new ArenaBoundary.HoleZone(new Vector2(-runtimeArenaRadius * 0.2f, runtimeArenaRadius * 0.22f), runtimeArenaRadius * 0.09f),
                    new ArenaBoundary.HoleZone(new Vector2(runtimeArenaRadius * 0.22f, -runtimeArenaRadius * 0.2f), runtimeArenaRadius * 0.09f)
                };
                break;
        }

        boundary.ConfigureIslandShape(baseRadius, waveA, waveB, waveC, 0.72f, holeRadius, holeOffset, extraHoles);
    }

    private void ConfigureSpawnPoints()
    {
        Vector3 center = ArenaBoundary.Instance != null ? (Vector3)ArenaBoundary.Instance.Center : Vector3.zero;

        if (playerSpawnPoint != null)
        {
            playerSpawnPoint.position = GetSpawnPointForIndex(center, -1, 1);
        }

        if (botSpawnPoints == null)
        {
            return;
        }

        float spawnRadius = runtimeArenaRadius * 0.56f;
        for (int i = 0; i < botSpawnPoints.Length; i++)
        {
            if (botSpawnPoints[i] == null)
            {
                continue;
            }

            botSpawnPoints[i].position = GetSpawnPointForIndex(center, i, Mathf.Max(1, botSpawnPoints.Length));
        }
    }

    private Vector3 GetSpawnPointForIndex(Vector3 center, int index, int totalCount)
    {
        switch (currentMapStyle)
        {
            case MapStyle.LagoonRing:
            {
                if (index < 0)
                {
                    return center + new Vector3(0f, -runtimeArenaRadius * 0.62f, 0f);
                }

                float angle = ((Mathf.PI * 2f) / Mathf.Max(1, totalCount)) * index;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (runtimeArenaRadius * 0.72f);
                return center + offset;
            }
            case MapStyle.LongStrip:
            {
                if (index < 0)
                {
                    return center + new Vector3(-runtimeArenaRadius * 0.55f, -0.8f, 0f);
                }

                float x = Mathf.Lerp(-runtimeArenaRadius * 0.25f, runtimeArenaRadius * 0.55f, index / (float)Mathf.Max(1, totalCount - 1));
                float y = (index % 2 == 0 ? 1.2f : -1.2f);
                return center + new Vector3(x, y, 0f);
            }
            case MapStyle.PebbleIsland:
            {
                if (index < 0)
                {
                    return center + new Vector3(0f, -runtimeArenaRadius * 0.42f, 0f);
                }

                float angle = ((Mathf.PI * 2f) / Mathf.Max(1, totalCount)) * index - 0.45f;
                float radius = runtimeArenaRadius * (0.42f + (index % 2 == 0 ? 0.18f : 0.3f));
                return center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            }
            case MapStyle.StarfishBay:
            {
                if (index < 0)
                {
                    return center + new Vector3(0f, -runtimeArenaRadius * 0.48f, 0f);
                }

                float angle = ((Mathf.PI * 2f) / Mathf.Max(1, totalCount)) * index - 1.1f;
                float radius = runtimeArenaRadius * 0.62f;
                return center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            }
            case MapStyle.TwinLagoon:
            {
                Vector3[] spawnSlots =
                {
                    center + new Vector3(-runtimeArenaRadius * 0.54f, -runtimeArenaRadius * 0.34f, 0f),
                    center + new Vector3(runtimeArenaRadius * 0.58f, runtimeArenaRadius * 0.4f, 0f),
                    center + new Vector3(-runtimeArenaRadius * 0.05f, runtimeArenaRadius * 0.58f, 0f),
                    center + new Vector3(runtimeArenaRadius * 0.02f, -runtimeArenaRadius * 0.54f, 0f),
                    center + new Vector3(-runtimeArenaRadius * 0.56f, runtimeArenaRadius * 0.48f, 0f)
                };

                if (index < 0)
                {
                    return spawnSlots[0];
                }

                return spawnSlots[index % spawnSlots.Length];
            }
            case MapStyle.CrescentAtoll:
            {
                if (index < 0)
                {
                    return center + new Vector3(-runtimeArenaRadius * 0.52f, -runtimeArenaRadius * 0.38f, 0f);
                }

                float angle = ((Mathf.PI * 2f) / Mathf.Max(1, totalCount)) * index + 1.9f;
                float radius = runtimeArenaRadius * 0.7f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                offset += new Vector3(-runtimeArenaRadius * 0.08f, -runtimeArenaRadius * 0.08f, 0f);
                return center + offset;
            }
            case MapStyle.CoralMaze:
            {
                Vector3[] slots =
                {
                    center + new Vector3(-runtimeArenaRadius * 0.46f, -runtimeArenaRadius * 0.42f, 0f),
                    center + new Vector3(runtimeArenaRadius * 0.48f, -runtimeArenaRadius * 0.34f, 0f),
                    center + new Vector3(-runtimeArenaRadius * 0.48f, runtimeArenaRadius * 0.42f, 0f),
                    center + new Vector3(runtimeArenaRadius * 0.18f, runtimeArenaRadius * 0.5f, 0f),
                    center + new Vector3(runtimeArenaRadius * 0.48f, runtimeArenaRadius * 0.1f, 0f)
                };

                return slots[index < 0 ? 0 : index % slots.Length];
            }
            case MapStyle.SplitShoals:
            {
                Vector3[] slots =
                {
                    center + new Vector3(-runtimeArenaRadius * 0.56f, runtimeArenaRadius * 0.24f, 0f),
                    center + new Vector3(runtimeArenaRadius * 0.56f, runtimeArenaRadius * 0.18f, 0f),
                    center + new Vector3(-runtimeArenaRadius * 0.18f, -runtimeArenaRadius * 0.5f, 0f),
                    center + new Vector3(runtimeArenaRadius * 0.18f, -runtimeArenaRadius * 0.54f, 0f),
                    center + new Vector3(0f, runtimeArenaRadius * 0.56f, 0f)
                };

                return slots[index < 0 ? 0 : index % slots.Length];
            }
            case MapStyle.SunkenCrown:
            {
                if (index < 0)
                {
                    return center + new Vector3(0f, -runtimeArenaRadius * 0.5f, 0f);
                }

                float angle = ((Mathf.PI * 2f) / Mathf.Max(1, totalCount)) * index - 1.57f;
                float radius = runtimeArenaRadius * 0.68f;
                return center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            }
            case MapStyle.TurtleBack:
            {
                if (index < 0)
                {
                    return center + new Vector3(0f, -runtimeArenaRadius * 0.6f, 0f);
                }

                float angle = ((Mathf.PI * 2f) / Mathf.Max(1, totalCount)) * index - 0.9f;
                float radius = runtimeArenaRadius * (0.46f + (index % 2 == 0 ? 0.16f : 0.28f));
                return center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            }
            case MapStyle.DiamondCay:
            {
                Vector3[] slots =
                {
                    center + new Vector3(0f, -runtimeArenaRadius * 0.58f, 0f),
                    center + new Vector3(runtimeArenaRadius * 0.58f, 0f, 0f),
                    center + new Vector3(0f, runtimeArenaRadius * 0.58f, 0f),
                    center + new Vector3(-runtimeArenaRadius * 0.58f, 0f, 0f),
                    center + new Vector3(runtimeArenaRadius * 0.2f, runtimeArenaRadius * 0.18f, 0f)
                };

                return slots[index < 0 ? 0 : index % slots.Length];
            }
            default:
            {
                if (index < 0)
                {
                    return center + new Vector3(0f, -runtimeArenaRadius * 0.38f, 0f);
                }

                float angle = ((Mathf.PI * 2f) / Mathf.Max(1, totalCount)) * index - 0.65f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (runtimeArenaRadius * 0.56f);
                return center + offset;
            }
        }
    }

    private void ConfigureArenaVisual()
    {
        GameObject arena = GameObject.Find("Arena");
        if (arena == null)
        {
            return;
        }

        arena.transform.position = Vector3.zero;
        arena.transform.rotation = Quaternion.identity;
        arena.transform.localScale = Vector3.one;

        MeshRenderer meshRenderer = arena.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        SpriteRenderer oldArenaRenderer = arena.GetComponent<SpriteRenderer>();
        if (oldArenaRenderer != null)
        {
            oldArenaRenderer.enabled = false;
        }

        Sprite fillSprite = GetArenaFillSprite();
        Sprite ringSprite = GetArenaRingSprite();
        Sprite squareSprite = GetSquareSprite();
        Sprite patternSprite = GetArenaPatternSprite();
        Sprite islandSprite = GetIslandSprite();
        Sprite waterPatternSprite = GetWaterPatternSprite();
        Color waterColor = new Color(0.2f, 0.62f, 0.94f, 1f);
        Color islandTopColor = new Color(0.92f, 0.87f, 0.64f, 1f);
        Color islandSideColor = new Color(0.76f, 0.71f, 0.53f, 1f);
        Color deepSideColor = new Color(0.63f, 0.58f, 0.42f, 1f);
        float islandSize = runtimeArenaRadius * 1.48f;
        float patternSize = runtimeArenaRadius * 1.36f;
        float waterPatternAlpha = 0.55f;
        bool showInnerWater = false;
        float innerWaterScale = 0f;
        Vector3 innerWaterPosition = Vector3.zero;

        ApplyMapVisualProfile(
            ref waterColor,
            ref islandTopColor,
            ref islandSideColor,
            ref deepSideColor,
            ref islandSize,
            ref patternSize,
            ref waterPatternAlpha,
            ref showInnerWater,
            ref innerWaterScale,
            ref innerWaterPosition);

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = waterColor;
        }

        CreateOrUpdateArenaLayer(
            arena.transform,
            "WaterDeep",
            squareSprite,
            new Color(waterColor.r * 0.72f, waterColor.g * 0.78f, waterColor.b * 0.92f, 1f),
            new Vector3(0f, -1.2f, 0f),
            new Vector3(46f, 36f, 1f),
            -16);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "WaterBase",
            squareSprite,
            waterColor,
            Vector3.zero,
            new Vector3(42f, 32f, 1f),
            -15);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "WaterPattern",
            waterPatternSprite,
            new Color(1f, 1f, 1f, waterPatternAlpha),
            Vector3.zero,
            new Vector3(42f, 32f, 1f),
            -14);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "IslandShadow",
            islandSprite,
            new Color(0f, 0f, 0f, 0.24f),
            new Vector3(0.3f, -0.54f, 0f),
            new Vector3(islandSize * 1.1f, islandSize * 1.1f, 1f),
            -13);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "IslandDeepSide",
            islandSprite,
            deepSideColor,
            new Vector3(0f, -1.42f, 0f),
            new Vector3(islandSize * 1.01f, islandSize * 1.01f, 1f),
            -12);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "IslandSide",
            islandSprite,
            islandSideColor,
            new Vector3(0f, -0.88f, 0f),
            new Vector3(islandSize, islandSize, 1f),
            -11);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "IslandMidSide",
            islandSprite,
            Color.Lerp(islandSideColor, islandTopColor, 0.35f),
            new Vector3(0f, -0.45f, 0f),
            new Vector3(islandSize * 0.995f, islandSize * 0.995f, 1f),
            -10);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "IslandTop",
            islandSprite,
            islandTopColor,
            Vector3.zero,
            new Vector3(islandSize, islandSize, 1f),
            -9);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "IslandPattern",
            islandSprite,
            new Color(0.75f, 0.69f, 0.5f, 0.22f),
            new Vector3(0.18f, -0.12f, 0f),
            new Vector3(patternSize, patternSize, 1f),
            -8);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "IslandEdge",
            islandSprite,
            new Color(0.84f, 0.78f, 0.57f, 0.55f),
            new Vector3(0.04f, -0.08f, 0f),
            new Vector3(islandSize * 1.015f, islandSize * 1.015f, 1f),
            -7);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "IslandTopGlow",
            islandSprite,
            new Color(1f, 0.96f, 0.82f, 0.12f),
            new Vector3(-0.18f, 0.34f, 0f),
            new Vector3(islandSize * 0.76f, islandSize * 0.76f, 1f),
            -6);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "IslandEdgeHighlight",
            islandSprite,
            new Color(1f, 0.97f, 0.82f, 0.16f),
            new Vector3(-0.02f, 0.14f, 0f),
            new Vector3(islandSize * 0.96f, islandSize * 0.96f, 1f),
            -5);

        CreateOrUpdateArenaLayer(
            arena.transform,
            "WaterFoam",
            islandSprite,
            new Color(1f, 1f, 1f, 0.14f),
            new Vector3(0.08f, -0.18f, 0f),
            new Vector3(islandSize * 1.06f, islandSize * 1.06f, 1f),
            -4);

        if (showInnerWater)
        {
            CreateOrUpdateArenaLayer(
                arena.transform,
                "InnerWater",
                GetArenaFillSprite(),
                waterColor,
                innerWaterPosition,
                new Vector3(innerWaterScale, innerWaterScale, 1f),
                -5);

            CreateOrUpdateArenaLayer(
                arena.transform,
                "InnerFoam",
                GetArenaRingSprite(),
                new Color(1f, 1f, 1f, 0.2f),
                innerWaterPosition,
                new Vector3(innerWaterScale * 1.08f, innerWaterScale * 1.08f, 1f),
                -4);
        }
        else
        {
            DisableArenaLayer(arena.transform, "InnerWater");
            DisableArenaLayer(arena.transform, "InnerFoam");
        }

        CreateLowPolyIslandFacets(arena.transform);
        ConfigureSpecialWaterFeatures(arena.transform, waterColor);

        if (arena.GetComponent<ArenaFoamAnimator>() == null)
        {
            arena.AddComponent<ArenaFoamAnimator>();
        }

        ConfigureMapDecorations(arena.transform);
    }

    private void CreateOrUpdateArenaLayer(
        Transform parent,
        string layerName,
        Sprite sprite,
        Color color,
        Vector3 localPosition,
        Vector3 localScale,
        int sortingOrder)
    {
        if (parent == null || sprite == null)
        {
            return;
        }

        Transform layerTransform = parent.Find(layerName);
        GameObject layerObject = layerTransform != null ? layerTransform.gameObject : new GameObject(layerName);
        if (layerTransform == null)
        {
            layerObject.transform.SetParent(parent, false);
        }

        SpriteRenderer layerRenderer = layerObject.GetComponent<SpriteRenderer>();
        if (layerRenderer == null)
        {
            layerRenderer = layerObject.AddComponent<SpriteRenderer>();
        }

        layerRenderer.sprite = sprite;
        layerRenderer.color = color;
        layerRenderer.sortingOrder = sortingOrder;

        layerObject.transform.localPosition = localPosition;
        layerObject.transform.localRotation = Quaternion.identity;
        layerObject.transform.localScale = localScale;
        layerObject.SetActive(true);
    }

    private void DisableArenaLayer(Transform parent, string layerName)
    {
        if (parent == null)
        {
            return;
        }

        Transform layer = parent.Find(layerName);
        if (layer != null)
        {
            layer.gameObject.SetActive(false);
        }
    }

    private Sprite CreateRuntimeSquareSprite()
    {
        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRuntimeCircleSprite()
    {
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        Vector2 center = new Vector2(63.5f, 63.5f);
        float radius = 56f;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? fill : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRuntimeArenaPatternSprite()
    {
        Texture2D texture = new Texture2D(192, 192, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2(95.5f, 95.5f);
        float maxRadius = 84f;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Vector2 point = new Vector2(x, y) - center;
                float distance = point.magnitude;
                if (distance > maxRadius)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                float noiseA = Mathf.Abs(Mathf.Sin((x * 0.11f) + (y * 0.04f)));
                float noiseB = Mathf.Abs(Mathf.Cos((x * 0.06f) - (y * 0.09f)));
                float streak = Mathf.Abs(Mathf.Sin((x * 0.025f) + (y * 0.035f))) > 0.975f ? 0.18f : 0f;
                float patch = noiseA > 0.93f || noiseB > 0.955f ? 0.22f : 0f;
                float alpha = Mathf.Max(streak, patch);
                texture.SetPixel(x, y, alpha > 0f ? new Color(1f, 1f, 1f, alpha) : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRuntimeIslandSprite()
    {
        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2(127.5f, 127.5f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Vector2 point = new Vector2(x, y) - center;
                float angle = Mathf.Atan2(point.y, point.x);
                float baseRadius = 104f;
                float wobble = Mathf.Sin(angle * 3f) * 10f + Mathf.Cos(angle * 5f) * 6f + Mathf.Sin(angle * 7f) * 4f;
                float radius = baseRadius + wobble;
                texture.SetPixel(x, y, point.magnitude <= radius ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRuntimeWaterPatternSprite()
    {
        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color line = Color.white;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float waveA = Mathf.Abs(Mathf.Sin((x * 0.055f) + (y * 0.02f))) * 10f;
                float waveB = Mathf.Abs(Mathf.Cos((x * 0.03f) - (y * 0.06f))) * 7f;
                bool isLine = Mathf.Abs((y % 64) - 32f - waveA) < 1.6f || Mathf.Abs((x % 82) - 41f - waveB) < 1.6f;
                texture.SetPixel(x, y, isLine ? line : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRuntimeBodySprite()
    {
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2(64f, 64f);
        Vector2 bodyScale = new Vector2(0.95f, 1.15f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Vector2 point = new Vector2((x - center.x) / (46f * bodyScale.x), (y - center.y) / (52f * bodyScale.y));
                texture.SetPixel(x, y, point.sqrMagnitude <= 1f ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.42f), 100f);
    }

    private Sprite CreateRuntimeStarSprite()
    {
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2(63.5f, 63.5f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Vector2 point = new Vector2(x, y) - center;
                float angle = Mathf.Atan2(point.y, point.x);
                float radius = 24f + Mathf.Max(0f, Mathf.Cos(angle * 5f)) * 18f;
                texture.SetPixel(x, y, point.magnitude <= radius ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRuntimeArrowSprite()
    {
        Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 tip = new Vector2(20f, 48f);
        Vector2 tailA = new Vector2(70f, 26f);
        Vector2 tailB = new Vector2(70f, 70f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Vector2 point = new Vector2(x, y);
                bool insideTriangle = IsPointInTriangle(point, tip, tailA, tailB);
                texture.SetPixel(x, y, insideTriangle ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.2f, 0.5f), 100f);
    }

    private Sprite CreateRuntimePalmSprite()
    {
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int y = 14; y < 76; y++)
        {
            for (int x = 58; x < 70; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        Vector2 center = new Vector2(64f, 78f);
        for (int leaf = 0; leaf < 5; leaf++)
        {
            float angle = Mathf.Lerp(-70f, 70f, leaf / 4f) * Mathf.Deg2Rad;
            Vector2 leafDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            for (int i = 0; i < 34; i++)
            {
                Vector2 point = center + leafDir * i;
                for (int oy = -4; oy <= 4; oy++)
                {
                    for (int ox = -4; ox <= 4; ox++)
                    {
                        Vector2 offset = new Vector2(ox, oy);
                        if (offset.sqrMagnitude <= 10f)
                        {
                            int px = Mathf.RoundToInt(point.x + ox);
                            int py = Mathf.RoundToInt(point.y + oy);
                            if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                            {
                                texture.SetPixel(px, py, Color.white);
                            }
                        }
                    }
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.08f), 100f);
    }

    private Sprite CreateRuntimeRingSprite()
    {
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        Vector2 center = new Vector2(63.5f, 63.5f);
        float outerRadius = 58f;
        float innerRadius = 50f;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                bool insideRing = distance <= outerRadius && distance >= innerRadius;
                texture.SetPixel(x, y, insideRing ? fill : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRuntimeOutlinedFighterSprite()
    {
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        Color outline = new Color(0.15f, 0.2f, 0.28f, 1f);
        Vector2 center = new Vector2(63.5f, 63.5f);
        float radius = 48f;
        float outlineRadius = 56f;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                Color pixel = clear;
                if (distance <= outlineRadius)
                {
                    pixel = outline;
                }

                if (distance <= radius)
                {
                    pixel = fill;
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRuntimeShadowSprite()
    {
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2(63.5f, 63.5f);
        float radius = 52f;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalized = Mathf.Clamp01(1f - (distance / radius));
                float alpha = normalized * normalized * 0.7f;
                texture.SetPixel(x, y, distance <= radius ? new Color(0f, 0f, 0f, alpha) : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite GetArenaFillSprite()
    {
        if (cachedArenaFillSprite == null)
        {
            cachedArenaFillSprite = CreateRuntimeCircleSprite();
        }

        return cachedArenaFillSprite;
    }

    private Sprite GetSquareSprite()
    {
        if (cachedSquareSprite == null)
        {
            cachedSquareSprite = CreateRuntimeSquareSprite();
        }

        return cachedSquareSprite;
    }

    private Sprite GetArenaPatternSprite()
    {
        if (cachedArenaPatternSprite == null)
        {
            cachedArenaPatternSprite = CreateRuntimeArenaPatternSprite();
        }

        return cachedArenaPatternSprite;
    }

    private Sprite GetIslandSprite()
    {
        if (cachedIslandSprite == null)
        {
            cachedIslandSprite = CreateRuntimeIslandSprite();
        }

        return cachedIslandSprite;
    }

    private Sprite GetWaterPatternSprite()
    {
        if (cachedWaterPatternSprite == null)
        {
            cachedWaterPatternSprite = CreateRuntimeWaterPatternSprite();
        }

        return cachedWaterPatternSprite;
    }

    private Sprite GetArenaRingSprite()
    {
        if (cachedArenaRingSprite == null)
        {
            cachedArenaRingSprite = CreateRuntimeRingSprite();
        }

        return cachedArenaRingSprite;
    }

    private Sprite GetBodySprite()
    {
        if (cachedBodySprite == null)
        {
            cachedBodySprite = CreateRuntimeBodySprite();
        }

        return cachedBodySprite;
    }

    private Sprite GetSmallCircleSprite()
    {
        if (cachedSmallCircleSprite == null)
        {
            cachedSmallCircleSprite = CreateRuntimeCircleSprite();
        }

        return cachedSmallCircleSprite;
    }

    private Sprite GetStarSprite()
    {
        if (cachedStarSprite == null)
        {
            cachedStarSprite = CreateRuntimeStarSprite();
        }

        return cachedStarSprite;
    }

    private Sprite GetArrowSprite()
    {
        if (cachedArrowSprite == null)
        {
            cachedArrowSprite = CreateRuntimeArrowSprite();
        }

        return cachedArrowSprite;
    }

    private Sprite GetPalmSprite()
    {
        if (cachedPalmSprite == null)
        {
            cachedPalmSprite = CreateRuntimePalmSprite();
        }

        return cachedPalmSprite;
    }

    private Sprite GetFighterSprite()
    {
        if (cachedFighterSprite == null)
        {
            cachedFighterSprite = CreateRuntimeOutlinedFighterSprite();
        }

        return cachedFighterSprite;
    }

    private Sprite GetShadowSprite()
    {
        if (cachedShadowSprite == null)
        {
            cachedShadowSprite = CreateRuntimeShadowSprite();
        }

        return cachedShadowSprite;
    }

    private bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float area = 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
        float sign = area < 0f ? -1f : 1f;
        float s = (a.y * c.x - a.x * c.y + (c.y - a.y) * point.x + (a.x - c.x) * point.y) * sign;
        float t = (a.x * b.y - a.y * b.x + (a.y - b.y) * point.x + (b.x - a.x) * point.y) * sign;
        return s >= 0f && t >= 0f && (s + t) <= 2f * area * sign;
    }

    private PlayerController CreateFallbackPlayer(Vector3 spawnPosition)
    {
        GameObject playerObject = new GameObject("Player");
        playerObject.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        return playerObject.AddComponent<PlayerController>();
    }

    private BotController CreateFallbackBot(Vector3 spawnPosition, int botIndex)
    {
        GameObject botObject = new GameObject("Bot_" + (botIndex + 1));
        botObject.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        return botObject.AddComponent<BotController>();
    }

    private void PrepareSpawnedFighter(FighterController fighter, bool isPlayer, Vector3 spawnPosition, int paletteIndex)
    {
        if (fighter == null)
        {
            return;
        }

        fighter.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        fighter.transform.rotation = Quaternion.identity;
        fighter.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

        SpriteRenderer spriteRenderer = fighter.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = fighter.gameObject.AddComponent<SpriteRenderer>();
        }

        Color bodyColor = isPlayer ? new Color(1f, 0.95f, 0.55f, 1f) : GetBotBodyColor(paletteIndex);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = GetBodySprite();
            spriteRenderer.color = bodyColor;
            spriteRenderer.sortingOrder = 22;
        }

        EnsureVisibleVisual(fighter.gameObject, isPlayer, bodyColor);
        CreateOrUpdateNameTag(fighter.transform, fighter.FighterDisplayName);

        Rigidbody2D body = fighter.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = fighter.gameObject.AddComponent<Rigidbody2D>();
        }

        if (body != null)
        {
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.linearDamping = 2f;
            body.angularDamping = 5f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        CircleCollider2D collider = fighter.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = fighter.gameObject.AddComponent<CircleCollider2D>();
        }

        if (collider != null)
        {
            collider.radius = 0.5f;
        }
    }

    private void EnsureVisibleVisual(GameObject fighterObject, bool isPlayer, Color bodyColor)
    {
        if (fighterObject == null)
        {
            return;
        }

        Transform oldVisualQuad = fighterObject.transform.Find("VisualQuad");
        if (oldVisualQuad != null)
        {
            Destroy(oldVisualQuad.gameObject);
        }

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "SoftShadow",
            GetShadowSprite(),
            new Color(0f, 0f, 0f, 0.14f),
            new Vector3(0.28f, -0.28f, 0f),
            new Vector3(1.42f, 1.18f, 1f),
            17);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "Shadow",
            GetShadowSprite(),
            new Color(0f, 0f, 0f, 0.22f),
            new Vector3(0.2f, -0.24f, 0f),
            new Vector3(1.18f, 1.06f, 1f),
            18);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "BodySide",
            GetBodySprite(),
            new Color(0.36f, 0.34f, 0.96f, 0.95f),
            new Vector3(0.03f, -0.12f, 0f),
            new Vector3(1f, 0.88f, 1f),
            19);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "TopCap",
            GetBodySprite(),
            new Color(
                Mathf.Clamp01(bodyColor.r + 0.12f),
                Mathf.Clamp01(bodyColor.g + 0.12f),
                Mathf.Clamp01(bodyColor.b + 0.12f),
                1f),
            new Vector3(0.02f, 0.12f, 0f),
            new Vector3(0.82f, 0.7f, 1f),
            21);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "BodyRim",
            GetBodySprite(),
            new Color(1f, 1f, 1f, 0.18f),
            new Vector3(-0.05f, 0.18f, 0f),
            new Vector3(0.88f, 0.5f, 1f),
            22);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "RimGlow",
            GetBodySprite(),
            new Color(1f, 1f, 1f, 0.08f),
            new Vector3(0.02f, 0.06f, 0f),
            new Vector3(1.1f, 0.92f, 1f),
            22);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "LeftBumperShadow",
            GetSmallCircleSprite(),
            new Color(0f, 0f, 0f, 0.15f),
            new Vector3(-0.26f, -0.04f, 0f),
            new Vector3(0.48f, 0.54f, 1f),
            22);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "LeftBumper",
            GetSmallCircleSprite(),
            new Color(1f, 0.43f, 0.42f, 1f),
            new Vector3(-0.4f, 0.06f, 0f),
            new Vector3(0.56f, 0.66f, 1f),
            23);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "RightBumperShadow",
            GetSmallCircleSprite(),
            new Color(0f, 0f, 0f, 0.15f),
            new Vector3(0.33f, 0.08f, 0f),
            new Vector3(0.42f, 0.46f, 1f),
            23);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "RightBumper",
            GetSmallCircleSprite(),
            isPlayer ? new Color(1f, 0.9f, 0.55f, 1f) : bodyColor,
            new Vector3(0.26f, 0.19f, 0f),
            new Vector3(0.48f, 0.54f, 1f),
            24);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "Gloss",
            GetSmallCircleSprite(),
            new Color(1f, 1f, 1f, 0.32f),
            new Vector3(0.08f, 0.34f, 0f),
            new Vector3(0.24f, 0.14f, 1f),
            25);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "EyeDot",
            GetSmallCircleSprite(),
            new Color(1f, 0.94f, 0.35f, 1f),
            new Vector3(0.2f, 0.13f, 0f),
            new Vector3(0.07f, 0.07f, 1f),
            26);

        CreateOrUpdateFighterLayer(
            fighterObject.transform,
            "PlayerArrow",
            GetArrowSprite(),
            new Color(1f, 1f, 1f, isPlayer ? 0.95f : 0f),
            new Vector3(-0.74f, -0.22f, 0f),
            new Vector3(0.42f, 0.42f, 1f),
            27);

        FighterVisual3D visual3D = fighterObject.GetComponent<FighterVisual3D>();
        if (visual3D == null)
        {
            visual3D = fighterObject.AddComponent<FighterVisual3D>();
        }

        visual3D.Configure(isPlayer);
    }

    private void CreateOrUpdateFighterLayer(
        Transform parent,
        string layerName,
        Sprite sprite,
        Color color,
        Vector3 localPosition,
        Vector3 localScale,
        int sortingOrder)
    {
        if (parent == null || sprite == null)
        {
            return;
        }

        Transform layerTransform = parent.Find(layerName);
        GameObject layerObject = layerTransform != null ? layerTransform.gameObject : new GameObject(layerName);
        if (layerTransform == null)
        {
            layerObject.transform.SetParent(parent, false);
        }

        SpriteRenderer renderer = layerObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = layerObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        layerObject.transform.localPosition = localPosition;
        layerObject.transform.localRotation = Quaternion.identity;
        layerObject.transform.localScale = localScale;
    }

    private void CreateArenaDecoration(Transform parent, string name, Sprite sprite, Color color, Vector3 localPosition, Vector3 localScale, int sortingOrder)
    {
        CreateOrUpdateArenaLayer(parent, name, sprite, color, localPosition, localScale, sortingOrder);
    }

    private void ConfigureSpecialWaterFeatures(Transform parent, Color waterColor)
    {
        DisableArenaLayer(parent, "ExtraHoleWaterA");
        DisableArenaLayer(parent, "ExtraHoleFoamA");
        DisableArenaLayer(parent, "ExtraHoleWaterB");
        DisableArenaLayer(parent, "ExtraHoleFoamB");

        switch (currentMapStyle)
        {
            case MapStyle.TwinLagoon:
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterA", GetArenaFillSprite(), waterColor, new Vector3(-runtimeArenaRadius * 0.33f, runtimeArenaRadius * 0.12f, 0f), new Vector3(runtimeArenaRadius * 0.54f, runtimeArenaRadius * 0.54f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamA", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.24f), new Vector3(-runtimeArenaRadius * 0.33f, runtimeArenaRadius * 0.12f, 0f), new Vector3(runtimeArenaRadius * 0.6f, runtimeArenaRadius * 0.6f, 1f), -4);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterB", GetArenaFillSprite(), waterColor, new Vector3(runtimeArenaRadius * 0.29f, -runtimeArenaRadius * 0.08f, 0f), new Vector3(runtimeArenaRadius * 0.48f, runtimeArenaRadius * 0.48f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamB", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.24f), new Vector3(runtimeArenaRadius * 0.29f, -runtimeArenaRadius * 0.08f, 0f), new Vector3(runtimeArenaRadius * 0.54f, runtimeArenaRadius * 0.54f, 1f), -4);
                break;
            case MapStyle.CrescentAtoll:
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterA", GetArenaFillSprite(), waterColor, new Vector3(runtimeArenaRadius * 0.24f, runtimeArenaRadius * 0.24f, 0f), new Vector3(runtimeArenaRadius * 1.02f, runtimeArenaRadius * 1.02f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamA", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.22f), new Vector3(runtimeArenaRadius * 0.24f, runtimeArenaRadius * 0.24f, 0f), new Vector3(runtimeArenaRadius * 1.1f, runtimeArenaRadius * 1.1f, 1f), -4);
                break;
            case MapStyle.CoralMaze:
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterA", GetArenaFillSprite(), waterColor, new Vector3(-runtimeArenaRadius * 0.18f, runtimeArenaRadius * 0.1f, 0f), new Vector3(runtimeArenaRadius * 0.34f, runtimeArenaRadius * 0.34f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamA", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.22f), new Vector3(-runtimeArenaRadius * 0.18f, runtimeArenaRadius * 0.1f, 0f), new Vector3(runtimeArenaRadius * 0.4f, runtimeArenaRadius * 0.4f, 1f), -4);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterB", GetArenaFillSprite(), waterColor, new Vector3(runtimeArenaRadius * 0.21f, runtimeArenaRadius * 0.22f, 0f), new Vector3(runtimeArenaRadius * 0.28f, runtimeArenaRadius * 0.28f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamB", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.22f), new Vector3(runtimeArenaRadius * 0.21f, runtimeArenaRadius * 0.22f, 0f), new Vector3(runtimeArenaRadius * 0.34f, runtimeArenaRadius * 0.34f, 1f), -4);
                break;
            case MapStyle.SplitShoals:
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterA", GetArenaFillSprite(), waterColor, new Vector3(0f, runtimeArenaRadius * 0.02f, 0f), new Vector3(runtimeArenaRadius * 0.52f, runtimeArenaRadius * 0.52f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamA", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.2f), new Vector3(0f, runtimeArenaRadius * 0.02f, 0f), new Vector3(runtimeArenaRadius * 0.6f, runtimeArenaRadius * 0.6f, 1f), -4);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterB", GetArenaFillSprite(), waterColor, new Vector3(0f, -runtimeArenaRadius * 0.3f, 0f), new Vector3(runtimeArenaRadius * 0.3f, runtimeArenaRadius * 0.3f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamB", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.18f), new Vector3(0f, -runtimeArenaRadius * 0.3f, 0f), new Vector3(runtimeArenaRadius * 0.38f, runtimeArenaRadius * 0.38f, 1f), -4);
                break;
            case MapStyle.SunkenCrown:
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterA", GetArenaFillSprite(), waterColor, new Vector3(-runtimeArenaRadius * 0.34f, 0f, 0f), new Vector3(runtimeArenaRadius * 0.3f, runtimeArenaRadius * 0.3f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamA", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.18f), new Vector3(-runtimeArenaRadius * 0.34f, 0f, 0f), new Vector3(runtimeArenaRadius * 0.36f, runtimeArenaRadius * 0.36f, 1f), -4);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterB", GetArenaFillSprite(), waterColor, new Vector3(runtimeArenaRadius * 0.34f, 0f, 0f), new Vector3(runtimeArenaRadius * 0.3f, runtimeArenaRadius * 0.3f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamB", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.18f), new Vector3(runtimeArenaRadius * 0.34f, 0f, 0f), new Vector3(runtimeArenaRadius * 0.36f, runtimeArenaRadius * 0.36f, 1f), -4);
                break;
            case MapStyle.DiamondCay:
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterA", GetArenaFillSprite(), waterColor, new Vector3(-runtimeArenaRadius * 0.2f, runtimeArenaRadius * 0.22f, 0f), new Vector3(runtimeArenaRadius * 0.26f, runtimeArenaRadius * 0.26f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamA", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.2f), new Vector3(-runtimeArenaRadius * 0.2f, runtimeArenaRadius * 0.22f, 0f), new Vector3(runtimeArenaRadius * 0.32f, runtimeArenaRadius * 0.32f, 1f), -4);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleWaterB", GetArenaFillSprite(), waterColor, new Vector3(runtimeArenaRadius * 0.22f, -runtimeArenaRadius * 0.2f, 0f), new Vector3(runtimeArenaRadius * 0.26f, runtimeArenaRadius * 0.26f, 1f), -5);
                CreateOrUpdateArenaLayer(parent, "ExtraHoleFoamB", GetArenaRingSprite(), new Color(1f, 1f, 1f, 0.2f), new Vector3(runtimeArenaRadius * 0.22f, -runtimeArenaRadius * 0.2f, 0f), new Vector3(runtimeArenaRadius * 0.32f, runtimeArenaRadius * 0.32f, 1f), -4);
                break;
        }
    }

    private void ConfigureMapDecorations(Transform parent)
    {
        string[] decorationNames =
        {
            "StarDecorationShadow", "StarDecoration", "RockAShadow", "RockA", "RockBShadow", "RockB", "PalmShadow", "PalmTree",
            "ExtraDeco1Shadow", "ExtraDeco1", "ExtraDeco2Shadow", "ExtraDeco2", "ExtraDeco3Shadow", "ExtraDeco3", "ExtraDeco4Shadow", "ExtraDeco4",
            "ExtraDeco5Shadow", "ExtraDeco5", "ExtraDeco6Shadow", "ExtraDeco6"
        };

        for (int i = 0; i < decorationNames.Length; i++)
        {
            DisableArenaLayer(parent, decorationNames[i]);
        }

        switch (currentMapStyle)
        {
            case MapStyle.ClassicIsland:
                CreateArenaDecoration(parent, "PalmShadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(-3.35f, 2.68f, 0f), new Vector3(1.32f, 1.22f, 1f), -6);
                CreateArenaDecoration(parent, "PalmTree", GetPalmSprite(), new Color(0.24f, 0.66f, 0.28f, 1f), new Vector3(-3.5f, 2.9f, 0f), new Vector3(1.2f, 1.14f, 1f), -5);
                CreateArenaDecoration(parent, "RockAShadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.14f), new Vector3(3.15f, -2.52f, 0f), new Vector3(0.66f, 0.46f, 1f), -6);
                CreateArenaDecoration(parent, "RockA", GetSmallCircleSprite(), new Color(0.92f, 0.96f, 1f, 1f), new Vector3(3f, -2.34f, 0f), new Vector3(0.54f, 0.4f, 1f), -5);
                break;
            case MapStyle.LagoonRing:
                CreateArenaDecoration(parent, "StarDecorationShadow", GetStarSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(0.1f, -3.08f, 0f), new Vector3(1.22f, 1.22f, 1f), -6);
                CreateArenaDecoration(parent, "StarDecoration", GetStarSprite(), new Color(1f, 0.67f, 0.8f, 1f), new Vector3(0f, -2.84f, 0f), new Vector3(1.08f, 1.08f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(-5.18f, 1.88f, 0f), new Vector3(1.18f, 1.08f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetPalmSprite(), new Color(0.22f, 0.62f, 0.28f, 1f), new Vector3(-5.32f, 2.08f, 0f), new Vector3(1.04f, 0.98f, 1f), -5);
                break;
            case MapStyle.LongStrip:
                CreateArenaDecoration(parent, "RockAShadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(-6.2f, -0.92f, 0f), new Vector3(0.78f, 0.5f, 1f), -6);
                CreateArenaDecoration(parent, "RockA", GetSmallCircleSprite(), new Color(0.98f, 0.97f, 0.92f, 1f), new Vector3(-6f, -0.72f, 0f), new Vector3(0.64f, 0.42f, 1f), -5);
                CreateArenaDecoration(parent, "RockBShadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(5.88f, 1.28f, 0f), new Vector3(0.64f, 0.46f, 1f), -6);
                CreateArenaDecoration(parent, "RockB", GetSmallCircleSprite(), new Color(0.88f, 0.92f, 0.98f, 1f), new Vector3(5.7f, 1.46f, 0f), new Vector3(0.54f, 0.38f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.17f), new Vector3(0.48f, 3.52f, 0f), new Vector3(1.22f, 1.12f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetPalmSprite(), new Color(0.28f, 0.64f, 0.26f, 1f), new Vector3(0.34f, 3.7f, 0f), new Vector3(1.06f, 1f, 1f), -5);
                break;
            case MapStyle.PebbleIsland:
                CreateArenaDecoration(parent, "RockAShadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(-4.5f, -3.2f, 0f), new Vector3(0.72f, 0.52f, 1f), -6);
                CreateArenaDecoration(parent, "RockA", GetSmallCircleSprite(), new Color(0.9f, 0.93f, 0.97f, 1f), new Vector3(-4.3f, -2.98f, 0f), new Vector3(0.58f, 0.42f, 1f), -5);
                CreateArenaDecoration(parent, "RockBShadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(4.18f, -0.54f, 0f), new Vector3(0.58f, 0.42f, 1f), -6);
                CreateArenaDecoration(parent, "RockB", GetSmallCircleSprite(), new Color(0.86f, 0.89f, 0.95f, 1f), new Vector3(4f, -0.34f, 0f), new Vector3(0.48f, 0.36f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetStarSprite(), new Color(0f, 0f, 0f, 0.14f), new Vector3(1.98f, 2.48f, 0f), new Vector3(1f, 1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetStarSprite(), new Color(0.99f, 0.74f, 0.82f, 1f), new Vector3(1.86f, 2.66f, 0f), new Vector3(0.9f, 0.9f, 1f), -5);
                break;
            case MapStyle.StarfishBay:
                CreateArenaDecoration(parent, "StarDecorationShadow", GetStarSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(-1.08f, -2.72f, 0f), new Vector3(1.34f, 1.34f, 1f), -6);
                CreateArenaDecoration(parent, "StarDecoration", GetStarSprite(), new Color(1f, 0.66f, 0.86f, 1f), new Vector3(-1.22f, -2.5f, 0f), new Vector3(1.18f, 1.18f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(4.76f, 1.88f, 0f), new Vector3(1.12f, 1.04f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetPalmSprite(), new Color(0.24f, 0.66f, 0.28f, 1f), new Vector3(4.58f, 2.06f, 0f), new Vector3(0.98f, 0.94f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco2Shadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.15f), new Vector3(-4.56f, 1.08f, 0f), new Vector3(0.54f, 0.38f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco2", GetSmallCircleSprite(), new Color(0.94f, 0.96f, 1f, 1f), new Vector3(-4.72f, 1.24f, 0f), new Vector3(0.42f, 0.3f, 1f), -5);
                break;
            case MapStyle.TwinLagoon:
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(-5.9f, -0.84f, 0f), new Vector3(1.14f, 1.04f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetPalmSprite(), new Color(0.24f, 0.67f, 0.3f, 1f), new Vector3(-6.05f, -0.62f, 0f), new Vector3(1f, 0.94f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco2Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(5.44f, 2.18f, 0f), new Vector3(1.06f, 0.98f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco2", GetPalmSprite(), new Color(0.22f, 0.62f, 0.26f, 1f), new Vector3(5.26f, 2.38f, 0f), new Vector3(0.94f, 0.88f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco3Shadow", GetStarSprite(), new Color(0f, 0f, 0f, 0.14f), new Vector3(0.2f, 4.42f, 0f), new Vector3(1.08f, 1.08f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco3", GetStarSprite(), new Color(0.98f, 0.72f, 0.82f, 1f), new Vector3(0.04f, 4.62f, 0f), new Vector3(0.96f, 0.96f, 1f), -5);
                break;
            case MapStyle.CrescentAtoll:
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(-5.38f, -3.06f, 0f), new Vector3(1.22f, 1.14f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetPalmSprite(), new Color(0.23f, 0.64f, 0.28f, 1f), new Vector3(-5.58f, -2.84f, 0f), new Vector3(1.08f, 1.02f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco2Shadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(4.7f, -2.12f, 0f), new Vector3(0.74f, 0.52f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco2", GetSmallCircleSprite(), new Color(0.92f, 0.95f, 0.99f, 1f), new Vector3(4.52f, -1.9f, 0f), new Vector3(0.58f, 0.4f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco3Shadow", GetStarSprite(), new Color(0f, 0f, 0f, 0.14f), new Vector3(-1.6f, 4.38f, 0f), new Vector3(1f, 1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco3", GetStarSprite(), new Color(0.97f, 0.73f, 0.86f, 1f), new Vector3(-1.76f, 4.58f, 0f), new Vector3(0.9f, 0.9f, 1f), -5);
                break;
            case MapStyle.CoralMaze:
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(-5.6f, 2.8f, 0f), new Vector3(1.08f, 1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetPalmSprite(), new Color(0.22f, 0.68f, 0.34f, 1f), new Vector3(-5.8f, 3f, 0f), new Vector3(0.96f, 0.9f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco2Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(5.1f, -2.4f, 0f), new Vector3(1.18f, 1.1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco2", GetPalmSprite(), new Color(0.2f, 0.62f, 0.28f, 1f), new Vector3(4.9f, -2.18f, 0f), new Vector3(1.04f, 0.98f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco3Shadow", GetStarSprite(), new Color(0f, 0f, 0f, 0.14f), new Vector3(0.56f, 4.7f, 0f), new Vector3(0.96f, 0.96f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco3", GetStarSprite(), new Color(1f, 0.68f, 0.8f, 1f), new Vector3(0.4f, 4.88f, 0f), new Vector3(0.84f, 0.84f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco4Shadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(-4.1f, -3.9f, 0f), new Vector3(0.68f, 0.46f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco4", GetSmallCircleSprite(), new Color(0.95f, 0.98f, 1f, 1f), new Vector3(-4.26f, -3.7f, 0f), new Vector3(0.54f, 0.38f, 1f), -5);
                break;
            case MapStyle.SplitShoals:
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(-6.32f, 0.9f, 0f), new Vector3(1.08f, 1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetPalmSprite(), new Color(0.24f, 0.66f, 0.31f, 1f), new Vector3(-6.5f, 1.1f, 0f), new Vector3(0.96f, 0.9f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco2Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(6.2f, 0.72f, 0f), new Vector3(1.08f, 1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco2", GetPalmSprite(), new Color(0.24f, 0.66f, 0.31f, 1f), new Vector3(6f, 0.92f, 0f), new Vector3(0.96f, 0.9f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco3Shadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(0f, -5.1f, 0f), new Vector3(0.92f, 0.62f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco3", GetSmallCircleSprite(), new Color(0.92f, 0.95f, 1f, 1f), new Vector3(-0.12f, -4.88f, 0f), new Vector3(0.74f, 0.48f, 1f), -5);
                break;
            case MapStyle.SunkenCrown:
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetStarSprite(), new Color(0f, 0f, 0f, 0.14f), new Vector3(-5.08f, 2.4f, 0f), new Vector3(1f, 1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetStarSprite(), new Color(0.98f, 0.75f, 0.84f, 1f), new Vector3(-5.22f, 2.58f, 0f), new Vector3(0.88f, 0.88f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco2Shadow", GetStarSprite(), new Color(0f, 0f, 0f, 0.14f), new Vector3(5.08f, 2.4f, 0f), new Vector3(1f, 1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco2", GetStarSprite(), new Color(0.98f, 0.75f, 0.84f, 1f), new Vector3(4.94f, 2.58f, 0f), new Vector3(0.88f, 0.88f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco3Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(0f, -5.54f, 0f), new Vector3(1.24f, 1.12f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco3", GetPalmSprite(), new Color(0.24f, 0.62f, 0.28f, 1f), new Vector3(-0.16f, -5.34f, 0f), new Vector3(1.08f, 0.98f, 1f), -5);
                break;
            case MapStyle.TurtleBack:
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(-5.8f, -2.8f, 0f), new Vector3(0.9f, 0.66f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetSmallCircleSprite(), new Color(0.84f, 0.93f, 0.78f, 1f), new Vector3(-6f, -2.58f, 0f), new Vector3(0.72f, 0.52f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco2Shadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(5.7f, -2.5f, 0f), new Vector3(0.9f, 0.66f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco2", GetSmallCircleSprite(), new Color(0.84f, 0.93f, 0.78f, 1f), new Vector3(5.5f, -2.28f, 0f), new Vector3(0.72f, 0.52f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco3Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(0.1f, 5.02f, 0f), new Vector3(1.1f, 1.02f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco3", GetPalmSprite(), new Color(0.24f, 0.66f, 0.3f, 1f), new Vector3(-0.08f, 5.2f, 0f), new Vector3(0.98f, 0.94f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco4Shadow", GetStarSprite(), new Color(0f, 0f, 0f, 0.14f), new Vector3(2.3f, 3.64f, 0f), new Vector3(1f, 1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco4", GetStarSprite(), new Color(1f, 0.72f, 0.86f, 1f), new Vector3(2.14f, 3.82f, 0f), new Vector3(0.88f, 0.88f, 1f), -5);
                break;
            case MapStyle.DiamondCay:
                CreateArenaDecoration(parent, "ExtraDeco1Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(0f, 6.08f, 0f), new Vector3(1.1f, 1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco1", GetPalmSprite(), new Color(0.22f, 0.64f, 0.28f, 1f), new Vector3(-0.16f, 6.28f, 0f), new Vector3(0.98f, 0.92f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco2Shadow", GetPalmSprite(), new Color(0f, 0f, 0f, 0.18f), new Vector3(0f, -6.12f, 0f), new Vector3(1.1f, 1f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco2", GetPalmSprite(), new Color(0.22f, 0.64f, 0.28f, 1f), new Vector3(0.16f, -5.92f, 0f), new Vector3(0.98f, 0.92f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco3Shadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(-6.2f, 0f, 0f), new Vector3(0.8f, 0.58f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco3", GetSmallCircleSprite(), new Color(0.95f, 0.98f, 1f, 1f), new Vector3(-6.4f, 0.18f, 0f), new Vector3(0.62f, 0.44f, 1f), -5);
                CreateArenaDecoration(parent, "ExtraDeco4Shadow", GetSmallCircleSprite(), new Color(0f, 0f, 0f, 0.16f), new Vector3(6.2f, 0f, 0f), new Vector3(0.8f, 0.58f, 1f), -6);
                CreateArenaDecoration(parent, "ExtraDeco4", GetSmallCircleSprite(), new Color(0.95f, 0.98f, 1f, 1f), new Vector3(6f, 0.18f, 0f), new Vector3(0.62f, 0.44f, 1f), -5);
                break;
        }
    }

    private void ApplyMapVisualProfile(
        ref Color waterColor,
        ref Color islandTopColor,
        ref Color islandSideColor,
        ref Color deepSideColor,
        ref float islandSize,
        ref float patternSize,
        ref float waterPatternAlpha,
        ref bool showInnerWater,
        ref float innerWaterScale,
        ref Vector3 innerWaterPosition)
    {
        switch (currentMapStyle)
        {
            case MapStyle.ClassicIsland:
                waterColor = new Color(0.27f, 0.72f, 0.98f, 1f);
                islandTopColor = new Color(0.97f, 0.9f, 0.7f, 1f);
                islandSideColor = new Color(0.82f, 0.75f, 0.58f, 1f);
                deepSideColor = new Color(0.68f, 0.61f, 0.46f, 1f);
                islandSize = runtimeArenaRadius * 1.48f;
                patternSize = runtimeArenaRadius * 1.36f;
                waterPatternAlpha = 0.62f;
                break;
            case MapStyle.LagoonRing:
                waterColor = new Color(0.22f, 0.76f, 0.96f, 1f);
                islandTopColor = new Color(0.96f, 0.88f, 0.64f, 1f);
                islandSideColor = new Color(0.79f, 0.71f, 0.54f, 1f);
                deepSideColor = new Color(0.63f, 0.57f, 0.43f, 1f);
                islandSize = runtimeArenaRadius * 1.56f;
                patternSize = runtimeArenaRadius * 1.42f;
                waterPatternAlpha = 0.64f;
                showInnerWater = true;
                innerWaterScale = runtimeArenaRadius * 0.72f;
                innerWaterPosition = new Vector3(0f, 0.18f, 0f);
                break;
            case MapStyle.LongStrip:
                waterColor = new Color(0.29f, 0.73f, 1f, 1f);
                islandTopColor = new Color(0.97f, 0.91f, 0.72f, 1f);
                islandSideColor = new Color(0.79f, 0.72f, 0.58f, 1f);
                deepSideColor = new Color(0.65f, 0.58f, 0.47f, 1f);
                islandSize = runtimeArenaRadius * 1.66f;
                patternSize = runtimeArenaRadius * 1.54f;
                waterPatternAlpha = 0.58f;
                break;
            case MapStyle.PebbleIsland:
                waterColor = new Color(0.24f, 0.68f, 0.95f, 1f);
                islandTopColor = new Color(0.93f, 0.87f, 0.72f, 1f);
                islandSideColor = new Color(0.76f, 0.7f, 0.58f, 1f);
                deepSideColor = new Color(0.62f, 0.56f, 0.45f, 1f);
                islandSize = runtimeArenaRadius * 1.4f;
                patternSize = runtimeArenaRadius * 1.2f;
                waterPatternAlpha = 0.66f;
                break;
            case MapStyle.StarfishBay:
                waterColor = new Color(0.26f, 0.78f, 0.99f, 1f);
                islandTopColor = new Color(0.98f, 0.9f, 0.66f, 1f);
                islandSideColor = new Color(0.79f, 0.72f, 0.54f, 1f);
                deepSideColor = new Color(0.65f, 0.58f, 0.42f, 1f);
                islandSize = runtimeArenaRadius * 1.52f;
                patternSize = runtimeArenaRadius * 1.28f;
                waterPatternAlpha = 0.68f;
                break;
            case MapStyle.TwinLagoon:
                waterColor = new Color(0.23f, 0.79f, 0.99f, 1f);
                islandTopColor = new Color(0.95f, 0.92f, 0.74f, 1f);
                islandSideColor = new Color(0.78f, 0.73f, 0.59f, 1f);
                deepSideColor = new Color(0.62f, 0.58f, 0.45f, 1f);
                islandSize = runtimeArenaRadius * 1.62f;
                patternSize = runtimeArenaRadius * 1.38f;
                waterPatternAlpha = 0.64f;
                break;
            case MapStyle.CrescentAtoll:
                waterColor = new Color(0.18f, 0.74f, 0.99f, 1f);
                islandTopColor = new Color(0.99f, 0.92f, 0.74f, 1f);
                islandSideColor = new Color(0.82f, 0.75f, 0.6f, 1f);
                deepSideColor = new Color(0.66f, 0.6f, 0.46f, 1f);
                islandSize = runtimeArenaRadius * 1.64f;
                patternSize = runtimeArenaRadius * 1.4f;
                waterPatternAlpha = 0.62f;
                break;
            case MapStyle.CoralMaze:
                waterColor = new Color(0.14f, 0.78f, 0.92f, 1f);
                islandTopColor = new Color(0.97f, 0.9f, 0.7f, 1f);
                islandSideColor = new Color(0.78f, 0.67f, 0.56f, 1f);
                deepSideColor = new Color(0.58f, 0.49f, 0.4f, 1f);
                islandSize = runtimeArenaRadius * 1.6f;
                patternSize = runtimeArenaRadius * 1.44f;
                waterPatternAlpha = 0.7f;
                break;
            case MapStyle.SplitShoals:
                waterColor = new Color(0.2f, 0.7f, 0.96f, 1f);
                islandTopColor = new Color(0.98f, 0.9f, 0.76f, 1f);
                islandSideColor = new Color(0.79f, 0.72f, 0.6f, 1f);
                deepSideColor = new Color(0.63f, 0.56f, 0.46f, 1f);
                islandSize = runtimeArenaRadius * 1.72f;
                patternSize = runtimeArenaRadius * 1.5f;
                waterPatternAlpha = 0.56f;
                break;
            case MapStyle.SunkenCrown:
                waterColor = new Color(0.12f, 0.64f, 0.94f, 1f);
                islandTopColor = new Color(0.96f, 0.88f, 0.66f, 1f);
                islandSideColor = new Color(0.77f, 0.69f, 0.54f, 1f);
                deepSideColor = new Color(0.58f, 0.52f, 0.4f, 1f);
                islandSize = runtimeArenaRadius * 1.68f;
                patternSize = runtimeArenaRadius * 1.42f;
                waterPatternAlpha = 0.64f;
                showInnerWater = true;
                innerWaterScale = runtimeArenaRadius * 0.56f;
                innerWaterPosition = new Vector3(0f, 0.06f, 0f);
                break;
            case MapStyle.TurtleBack:
                waterColor = new Color(0.18f, 0.72f, 0.9f, 1f);
                islandTopColor = new Color(0.84f, 0.9f, 0.68f, 1f);
                islandSideColor = new Color(0.65f, 0.74f, 0.52f, 1f);
                deepSideColor = new Color(0.48f, 0.57f, 0.41f, 1f);
                islandSize = runtimeArenaRadius * 1.76f;
                patternSize = runtimeArenaRadius * 1.48f;
                waterPatternAlpha = 0.54f;
                break;
            case MapStyle.DiamondCay:
                waterColor = new Color(0.24f, 0.8f, 0.99f, 1f);
                islandTopColor = new Color(0.99f, 0.92f, 0.78f, 1f);
                islandSideColor = new Color(0.82f, 0.75f, 0.62f, 1f);
                deepSideColor = new Color(0.66f, 0.58f, 0.48f, 1f);
                islandSize = runtimeArenaRadius * 1.42f;
                patternSize = runtimeArenaRadius * 1.16f;
                waterPatternAlpha = 0.72f;
                break;
        }
    }

    private void CreateLowPolyIslandFacets(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        int facetCount = currentMapStyle == MapStyle.TurtleBack ? 22 : currentMapStyle == MapStyle.LongStrip ? 18 : 20;
        for (int i = 0; i < facetCount; i++)
        {
            float t = i / (float)(facetCount - 1);
            float angle = Mathf.Lerp(200f, 340f, t) * Mathf.Deg2Rad;
            float radius = runtimeArenaRadius * 0.94f;
            Vector3 localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius - 1.05f, 0f);
            float shade = 0.62f + (i % 2 == 0 ? 0.1f : 0.02f);
            Color color = new Color(shade, shade * 0.95f, shade * 0.75f, 0.96f);
            Vector3 scale = new Vector3(1.16f, 3.1f + Mathf.Sin(t * Mathf.PI) * 0.68f, 1f);

            CreateOrUpdateArenaLayer(
                parent,
                "Facet_" + i,
                GetSquareSprite(),
                color,
                localPosition,
                scale,
                -11);

            Transform facet = parent.Find("Facet_" + i);
            if (facet != null)
            {
                facet.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-22f, 22f, t));
            }
        }

        int topFacetCount = 12;
        for (int i = 0; i < topFacetCount; i++)
        {
            float t = i / (float)(topFacetCount - 1);
            float angle = Mathf.Lerp(190f, 350f, t) * Mathf.Deg2Rad;
            float radius = runtimeArenaRadius * 0.76f;
            Vector3 localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius + 0.16f, 0f);
            float shade = 0.9f + Mathf.Sin(t * Mathf.PI) * 0.05f;
            CreateOrUpdateArenaLayer(
                parent,
                "TopFacet_" + i,
                GetSquareSprite(),
                new Color(shade, shade * 0.96f, shade * 0.84f, 0.14f),
                localPosition,
                new Vector3(0.9f, 1.6f, 1f),
                -6);

            Transform facet = parent.Find("TopFacet_" + i);
            if (facet != null)
            {
                facet.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-35f, 35f, t));
            }
        }
    }

    private void CreateOrUpdateNameTag(Transform fighterTransform, string fighterName)
    {
        if (fighterTransform == null)
        {
            return;
        }

        Transform tagTransform = fighterTransform.Find("NameTag");
        GameObject tagObject = tagTransform != null ? tagTransform.gameObject : new GameObject("NameTag");
        if (tagTransform == null)
        {
            tagObject.transform.SetParent(fighterTransform, false);
        }

        TextMesh textMesh = tagObject.GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = tagObject.AddComponent<TextMesh>();
        }

        MeshRenderer renderer = tagObject.GetComponent<MeshRenderer>();
        textMesh.text = fighterName;
        textMesh.fontSize = 42;
        textMesh.characterSize = 0.06f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        if (renderer != null)
        {
            renderer.sortingOrder = 35;
        }

        tagObject.transform.localPosition = new Vector3(0f, 0.92f, 0f);
        tagObject.transform.localRotation = Quaternion.identity;
        tagObject.transform.localScale = Vector3.one;
    }

    private Color GetBotBodyColor(int paletteIndex)
    {
        Color[] palette =
        {
            new Color(0.76f, 0.88f, 1f, 1f),
            new Color(0.72f, 0.68f, 0.98f, 1f),
            new Color(0.82f, 0.9f, 0.82f, 1f),
            new Color(0.96f, 0.76f, 0.54f, 1f),
            new Color(0.72f, 0.74f, 0.8f, 1f)
        };

        return palette[Mathf.Abs(paletteIndex) % palette.Length];
    }

    private void BindCameraTarget(Transform target)
    {
        if (cameraFollow == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraFollow = mainCamera.GetComponent<CameraFollow2D>();
            }
        }

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(target);
        }
    }
}
