using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tạo nhanh khung project Bumper trong Unity Editor.
/// Sau khi import script, mở menu Tools/Bumper/Bootstrap Project để tự sinh scene, UI và prefab cơ bản.
/// </summary>
public static class BumperBootstrapper
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string SpriteFolder = "Assets/Generated";
    private const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";
    private const string LobbyScenePath = ScenesFolder + "/Lobby.unity";
    private const string GameScenePath = ScenesFolder + "/GameScene.unity";
    private const string PlayerPrefabPath = PrefabsFolder + "/Player.prefab";
    private const string BotPrefabPath = PrefabsFolder + "/Bot.prefab";
    private const string SquareSpriteAssetPath = SpriteFolder + "/BumperSquare.asset";

    [MenuItem("Tools/Bumper/Bootstrap Project")]
    public static void BootstrapProject()
    {
        EnsureFolder("Assets", "Editor");
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets", "Generated");

        var circleSprite = CreateCircleSpriteAsset();
        var squareSprite = CreateSquareSpriteAsset();
        CreatePrefabs(circleSprite);
        CreateMainMenuScene();
        CreateLobbyScene();
        CreateGameScene(circleSprite, squareSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Bumper Bootstrap",
            "Project scaffolding đã được tạo xong. Hãy mở từng scene và kiểm tra reference trong Inspector.",
            "OK");
    }

    [MenuItem("Tools/Bumper/Fix Open GameScene")]
    public static void FixOpenGameScene()
    {
        if (EditorSceneManager.GetActiveScene().name != "GameScene")
        {
            EditorUtility.DisplayDialog("Bumper", "Hãy mở scene GameScene trước rồi chạy lệnh này.", "OK");
            return;
        }

        var squareSprite = CreateSquareSpriteAsset();
        FixCameraFor2D();
        FixArenaFor2D(squareSprite);
        FixFighterRenderOrders();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Bumper", "Đã sửa GameScene sang 2D mode. Bấm Play để test lại.", "OK");
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = Path.Combine(parent, folderName).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static Sprite CreateCircleSpriteAsset()
    {
        string assetPath = SpriteFolder + "/BumperCircle.asset";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null)
        {
            return existing;
        }

        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
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

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        AssetDatabase.CreateAsset(sprite, assetPath);
        return sprite;
    }

    private static Sprite CreateSquareSpriteAsset()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpriteAssetPath);
        if (existing != null)
        {
            return existing;
        }

        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        AssetDatabase.CreateAsset(sprite, SquareSpriteAssetPath);
        return sprite;
    }

    private static void CreatePrefabs(Sprite circleSprite)
    {
        CreateFighterPrefab("Player", PlayerPrefabPath, circleSprite, true);
        CreateFighterPrefab("Bot", BotPrefabPath, circleSprite, false);
        FixFighterRenderOrders();
    }

    private static void CreateFighterPrefab(string name, string prefabPath, Sprite circleSprite, bool isPlayer)
    {
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            var existingRenderer = existingPrefab.GetComponent<SpriteRenderer>();
            if (existingRenderer != null)
            {
                existingRenderer.sprite = circleSprite;
                existingRenderer.color = isPlayer ? new Color(0.35f, 0.85f, 1f) : new Color(1f, 0.45f, 0.35f);
                existingRenderer.sortingOrder = 20;
                EditorUtility.SetDirty(existingPrefab);
            }
            return;
        }

        GameObject root = new GameObject(name);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = isPlayer ? new Color(0.35f, 0.85f, 1f) : new Color(1f, 0.45f, 0.35f);

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 2f;
        rb.angularDamping = 5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = root.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        if (isPlayer)
        {
            root.AddComponent<PlayerController>();
        }
        else
        {
            root.AddComponent<BotController>();
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        Object.DestroyImmediate(root);
    }

    private static void CreateMainMenuScene()
    {
        if (File.Exists(MainMenuScenePath))
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "MainMenu";

        EnsureEventSystem();
        GameObject root = new GameObject("MainMenuController");
        MainMenu mainMenu = root.AddComponent<MainMenu>();

        Canvas canvas = CreateCanvas("Canvas");
        GameObject mainPanel = CreatePanel(canvas.transform, "MainPanel");
        GameObject settingsPanel = CreatePanel(canvas.transform, "SettingsPanel");
        GameObject howPanel = CreatePanel(canvas.transform, "HowToPlayPanel");

        settingsPanel.SetActive(false);
        howPanel.SetActive(false);

        CreateTitle(mainPanel.transform, "Bumper");
        CreateButton(mainPanel.transform, "Bắt Đầu", new Vector2(0, 80), "OnStartClicked", mainMenu, "Start");
        CreateButton(mainPanel.transform, "Cài Đặt", new Vector2(0, 20), "OnSettingsClicked", mainMenu, "Settings");
        CreateButton(mainPanel.transform, "Hướng Dẫn", new Vector2(0, -40), "OnHowToPlayClicked", mainMenu, "How To Play");
        CreateButton(mainPanel.transform, "Thoát", new Vector2(0, -100), "OnExitClicked", mainMenu, "Exit");

        SettingsManager settingsManager = settingsPanel.AddComponent<SettingsManager>();
        CreateSettingsContent(settingsPanel.transform, settingsManager);
        CreateBackButton(settingsPanel.transform, "Quay Lại", "OnBackFromSettings", mainMenu, "Back");

        CreateHowToPlayContent(howPanel.transform);
        CreateBackButton(howPanel.transform, "Quay Lại", "OnBackFromHowToPlay", mainMenu, "Back");

        SetField(mainMenu, "mainPanel", mainPanel);
        SetField(mainMenu, "settingsPanel", settingsPanel);
        SetField(mainMenu, "howToPlayPanel", howPanel);

        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        Object.DestroyImmediate(root);
    }

    private static void CreateLobbyScene()
    {
        if (File.Exists(LobbyScenePath))
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "Lobby";

        EnsureEventSystem();
        GameObject root = new GameObject("LobbyManager");
        LobbyManager lobbyManager = root.AddComponent<LobbyManager>();

        Canvas canvas = CreateCanvas("Canvas");
        GameObject panel = CreatePanel(canvas.transform, "LobbyPanel");
        CreateTitle(panel.transform, "Phòng Chờ");
        TMP_InputField inputField = CreateInputField(panel.transform, "NameInput", new Vector2(0, 40));
        CreateLabel(panel.transform, "Instructions", "Nhập tên và bấm Chơi.", new Vector2(0, -10));
        TMP_Text botInfo = CreateLabel(panel.transform, "BotInfo", "Số bot: 3", new Vector2(0, 100));
        CreateButton(panel.transform, "Chơi", new Vector2(0, -80), "OnPlayClicked", lobbyManager, "Play");
        CreateButton(panel.transform, "Quay Lại", new Vector2(0, -140), "OnBackClicked", lobbyManager, "Back");

        SetField(lobbyManager, "nameInputField", inputField);
        SetField(lobbyManager, "instructionsText", panel.transform.Find("Instructions")?.GetComponent<TMP_Text>());
        SetField(lobbyManager, "botCountText", botInfo);

        EditorSceneManager.SaveScene(scene, LobbyScenePath);
        Object.DestroyImmediate(root);
    }

    private static void CreateGameScene(Sprite circleSprite, Sprite squareSprite)
    {
        if (File.Exists(GameScenePath))
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "GameScene";

        EnsureEventSystem();
        GameObject root = new GameObject("GameManager");
        GameManager gameManager = root.AddComponent<GameManager>();

        FixCameraFor2D();

        GameObject arena = new GameObject("Arena");
        var arenaRenderer = arena.AddComponent<SpriteRenderer>();
        arenaRenderer.sprite = squareSprite;
        arenaRenderer.color = new Color(1f, 1f, 1f, 0.12f);
        arenaRenderer.sortingOrder = -20;
        arena.transform.localScale = new Vector3(16f, 16f, 1f);
        arena.transform.position = new Vector3(0f, 0f, 5f);

        GameObject arenaBoundaryObj = new GameObject("ArenaBoundary");
        ArenaBoundary boundary = arenaBoundaryObj.AddComponent<ArenaBoundary>();

        GameObject centerPoint = new GameObject("CenterPoint");
        centerPoint.transform.position = Vector3.zero;
        boundary.GetType().GetField("centerPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(boundary, centerPoint.transform);
        boundary.GetType().GetField("radius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(boundary, 7.2f);

        GameObject playerSpawn = new GameObject("PlayerSpawnPoint");
        playerSpawn.transform.position = new Vector3(-2f, 0f, 0f);
        GameObject botSpawn1 = new GameObject("BotSpawnPoint1");
        botSpawn1.transform.position = new Vector3(2f, 1.5f, 0f);
        GameObject botSpawn2 = new GameObject("BotSpawnPoint2");
        botSpawn2.transform.position = new Vector3(2.5f, -1.5f, 0f);

        GameObject uiRoot = new GameObject("UIManager");
        UIManager uiManager = uiRoot.AddComponent<UIManager>();

        Canvas canvas = CreateCanvas("Canvas");
        CreateHud(canvas.transform, uiManager);
        CreateResultPanel(canvas.transform, uiManager);

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject botPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BotPrefabPath);

        SetField(gameManager, "playerPrefab", playerPrefab != null ? playerPrefab.GetComponent<PlayerController>() : null);
        SetField(gameManager, "botPrefab", botPrefab != null ? botPrefab.GetComponent<BotController>() : null);
        SetField(gameManager, "playerSpawnPoint", playerSpawn.transform);
        SetField(gameManager, "botSpawnPoints", new Transform[] { botSpawn1.transform, botSpawn2.transform });

        EditorSceneManager.SaveScene(scene, GameScenePath);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(centerPoint);
        Object.DestroyImmediate(playerSpawn);
        Object.DestroyImmediate(botSpawn1);
        Object.DestroyImmediate(botSpawn2);
        Object.DestroyImmediate(uiRoot);
        Object.DestroyImmediate(arenaBoundaryObj);
    }

    private static void FixCameraFor2D()
    {
        Camera camera = Object.FindAnyObjectByType<Camera>();
        if (camera == null)
        {
            return;
        }

        camera.orthographic = true;
        camera.orthographicSize = 7.5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;
    }

    private static void FixArenaFor2D(Sprite squareSprite)
    {
        GameObject arena = GameObject.Find("Arena");
        if (arena == null)
        {
            return;
        }

        var meshRenderer = arena.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        var meshFilter = arena.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Object.DestroyImmediate(meshFilter);
        }

        Transform visualTransform = arena.transform.Find("ArenaVisual");
        GameObject visualObject;
        if (visualTransform == null)
        {
            visualObject = new GameObject("ArenaVisual");
            visualObject.transform.SetParent(arena.transform, false);
        }
        else
        {
            visualObject = visualTransform.gameObject;
        }

        var spriteRenderer = visualObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.12f);
        spriteRenderer.sortingOrder = -20;
        visualObject.transform.localPosition = new Vector3(0f, 0f, 5f);
        visualObject.transform.localScale = new Vector3(16f, 16f, 1f);
    }

    private static void FixFighterRenderOrders()
    {
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var botPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BotPrefabPath);

        FixPrefabSorting(playerPrefab);
        FixPrefabSorting(botPrefab);
    }

    private static void FixPrefabSorting(GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }

        var renderer = prefab.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 20;
        }
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    private static void CreateTitle(Transform parent, string text)
    {
        CreateLabel(parent, "Title", text, new Vector2(0, 180), 56);
    }

    private static TMP_Text CreateLabel(Transform parent, string name, string text, Vector2 anchoredPosition, int fontSize = 28)
    {
        GameObject label = new GameObject(name);
        label.transform.SetParent(parent, false);
        RectTransform rect = label.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(720, 80);
        rect.anchoredPosition = anchoredPosition;
        TMP_Text tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string text, Vector2 anchoredPosition, string methodName, Object target, string objectName = null)
    {
        GameObject buttonGo = new GameObject((string.IsNullOrEmpty(objectName) ? text : objectName) + "Button");
        buttonGo.transform.SetParent(parent, false);

        Image image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.2f, 0.45f, 0.8f, 1f);

        Button button = buttonGo.AddComponent<Button>();
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280, 50);
        rect.anchoredPosition = anchoredPosition;

        TMP_Text label = CreateLabel(buttonGo.transform, "Label", text, Vector2.zero, 24);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        if (target != null)
        {
            button.onClick.AddListener(() =>
            {
                if (target is GameObject targetGameObject)
                {
                    targetGameObject.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
                }
                else if (target is Component targetComponent)
                {
                    targetComponent.gameObject.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
                }
            });
        }

        return button;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, Vector2 anchoredPosition)
    {
        GameObject field = new GameObject(name);
        field.transform.SetParent(parent, false);

        Image image = field.AddComponent<Image>();
        image.color = Color.white;

        TMP_InputField input = field.AddComponent<TMP_InputField>();
        RectTransform rect = field.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360, 50);
        rect.anchoredPosition = anchoredPosition;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(field.transform, false);
        TMP_Text text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = "Người chơi";
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Left;
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 6);
        textRect.offsetMax = new Vector2(-10, -6);

        GameObject placeholderGo = new GameObject("Placeholder");
        placeholderGo.transform.SetParent(field.transform, false);
        TMP_Text placeholder = placeholderGo.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Nhập tên";
        placeholder.color = new Color(0f, 0f, 0f, 0.4f);
        placeholder.alignment = TextAlignmentOptions.Left;
        RectTransform placeholderRect = placeholderGo.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 6);
        placeholderRect.offsetMax = new Vector2(-10, -6);

        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }

    private static void CreateSettingsContent(Transform parent, SettingsManager settingsManager)
    {
        TMP_Text volumeLabel = CreateLabel(parent, "VolumeLabel", "Âm lượng: 100%", new Vector2(0, 190));
        TMP_Text botLabel = CreateLabel(parent, "BotLabel", "Số bot: 3", new Vector2(0, 34), 24);
        CreateButton(parent, "Tăng", new Vector2(0, 20), nameof(SettingsManager.IncreaseVolume), settingsManager, "Volume +");
        CreateButton(parent, "Giảm", new Vector2(0, -40), nameof(SettingsManager.DecreaseVolume), settingsManager, "Volume -");
        CreateButton(parent, "+", new Vector2(112, 34), nameof(SettingsManager.IncreaseBots), settingsManager, "Bots +");
        CreateButton(parent, "-", new Vector2(-112, 34), nameof(SettingsManager.DecreaseBots), settingsManager, "Bots -");

        SetField(settingsManager, "volumeValueText", volumeLabel);
        SetField(settingsManager, "botCountValueText", botLabel);
    }

    private static void CreateHowToPlayContent(Transform parent)
    {
        CreateLabel(parent, "HowText", "Dùng WASD để di chuyển\nĐẩy đối thủ rơi khỏi đảo\nNgười sống sót cuối cùng sẽ thắng", new Vector2(0, 40), 30);
    }

    private static void CreateBackButton(Transform parent, string text, string methodName, Object target, string objectName = null)
    {
        CreateButton(parent, text, new Vector2(0, -280), methodName, target, objectName);
    }

    private static void CreateHud(Transform parent, UIManager uiManager)
    {
        TMP_Text playerText = CreateLabel(parent, "PlayerNameText", "Người chơi", new Vector2(-500, 260), 24);
        TMP_Text botsText = CreateLabel(parent, "BotsRemainingText", "Bot còn lại: 3", new Vector2(-500, 220), 24);
        TMP_Text eliminatedText = CreateLabel(parent, "EliminatedText", "Hạ gục: 0", new Vector2(-500, 180), 24);
        TMP_Text statusText = CreateLabel(parent, "StatusText", "Chiến!", new Vector2(0, 260), 28);

        SetField(uiManager, "playerNameText", playerText);
        SetField(uiManager, "botsRemainingText", botsText);
        SetField(uiManager, "eliminatedText", eliminatedText);
        SetField(uiManager, "statusText", statusText);
    }

    private static void CreateResultPanel(Transform parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel(parent, "ResultPanel");
        panel.SetActive(false);
        TMP_Text title = CreateLabel(panel.transform, "ResultTitle", "CHIẾN THẮNG", new Vector2(0, 80), 48);
        TMP_Text description = CreateLabel(panel.transform, "ResultDescription", "Bạn là người cuối cùng còn trên đảo.", new Vector2(0, 20), 24);
        CreateButton(panel.transform, "Chơi Lại", new Vector2(0, -60), nameof(UIManager.OnReplayClicked), uiManager, "Replay");
        CreateButton(panel.transform, "Về Menu", new Vector2(0, -120), nameof(UIManager.OnBackToMenuClicked), uiManager, "Back To Menu");

        SetField(uiManager, "resultPanel", panel);
        SetField(uiManager, "resultTitleText", title);
        SetField(uiManager, "resultDescriptionText", description);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void SetField(Object target, string fieldName, object value)
    {
        if (target == null)
        {
            return;
        }

        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }
    }
}
