using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cập nhật HUD trong GameScene và điều khiển panel thắng/thua.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text botsRemainingText;
    [SerializeField] private TMP_Text eliminatedText;
    [SerializeField] private TMP_Text statusText;

    [Header("End Panels")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultDescriptionText;

    private GameObject pausePanel;
    private Button pauseButton;
    private Button resumeButton;
    private Button quitButton;
    private bool isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UIRuntimeFix.Apply();
        BuildRuntimeHudDecor();
        BuildPauseMenu();
        WireButtons();
        HideResult();
        RefreshHUD();
    }

    private void Update()
    {
        RefreshHUD();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private void OnDisable()
    {
        SetPauseState(false);
    }

    public void RefreshHUD()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (playerNameText != null)
        {
            playerNameText.text = GameManager.Instance.PlayerName;
        }

        if (botsRemainingText != null)
        {
            botsRemainingText.text = "Bot còn lại: " + GameManager.Instance.RemainingBots;
        }

        if (eliminatedText != null)
        {
            eliminatedText.text = "Bạn hạ gục: " + GameManager.Instance.PlayerKnockouts;
        }

        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
    }

    public void ShowResult(bool victory)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = victory ? "CHIẾN THẮNG" : "THẤT BẠI";
        }

        if (resultDescriptionText != null)
        {
            resultDescriptionText.text = victory
                ? "Bạn là người cuối cùng còn trên đảo."
                : "Bạn đã bị đẩy rơi khỏi đấu trường.";
        }
    }

    public void HideResult()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    public void OnPauseClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        PauseGame();
    }

    public void OnResumeClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        ResumeGame();
    }

    public void OnReplayClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartMatch();
        }
    }

    public void OnBackToMenuClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        SetPauseState(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMenu();
        }
    }

    private void WireButtons()
    {
        BindButton("ReplayButton", OnReplayClicked);
        BindButton("Back To MenuButton", OnBackToMenuClicked);
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(OnPauseClicked);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnBackToMenuClicked);
        }
    }

    private void BuildRuntimeHudDecor()
    {
        CreateHudCard();
        StyleResultPanel();
    }

    private void CreateHudCard()
    {
        if (playerNameText == null)
        {
            return;
        }

        RectTransform textRect = playerNameText.rectTransform;
        if (textRect == null || textRect.parent == null)
        {
            return;
        }

        Transform canvasRoot = textRect.parent;
        Transform layoutRoot = canvasRoot.Find("HudLayoutRoot");
        RectTransform layoutRect;
        if (layoutRoot != null)
        {
            layoutRect = layoutRoot as RectTransform;
        }
        else
        {
            GameObject layoutObject = new GameObject("HudLayoutRoot");
            layoutObject.transform.SetParent(canvasRoot, false);
            layoutRect = layoutObject.AddComponent<RectTransform>();
        }

        if (layoutRect == null)
        {
            return;
        }

        layoutRect.anchorMin = new Vector2(0f, 1f);
        layoutRect.anchorMax = new Vector2(0f, 1f);
        layoutRect.pivot = new Vector2(0f, 1f);
        layoutRect.anchoredPosition = new Vector2(8f, -8f);
        layoutRect.sizeDelta = new Vector2(210f, 78f);

        playerNameText.rectTransform.SetParent(layoutRect, false);
        if (botsRemainingText != null)
        {
            botsRemainingText.rectTransform.SetParent(layoutRect, false);
        }

        if (eliminatedText != null)
        {
            eliminatedText.rectTransform.SetParent(layoutRect, false);
        }

        Transform existing = layoutRect.Find("HudCard");
        Image cardImage;
        if (existing != null)
        {
            cardImage = existing.GetComponent<Image>();
        }
        else
        {
            GameObject card = new GameObject("HudCard");
            card.transform.SetParent(layoutRect, false);
            card.transform.SetSiblingIndex(0);
            card.AddComponent<RectTransform>();
            cardImage = card.AddComponent<Image>();
        }

        if (cardImage != null)
        {
            cardImage.color = new Color(0.06f, 0.1f, 0.17f, 0.88f);
            RectTransform rect = cardImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        CreateHudAccent(layoutRect);
        ApplyHudTextStyle(playerNameText, new Vector2(14f, -10f), new Vector2(182f, 22f), TextAlignmentOptions.Left, 15, FontStyles.Bold);
        ApplyHudTextStyle(botsRemainingText, new Vector2(14f, -34f), new Vector2(182f, 18f), TextAlignmentOptions.Left, 13, FontStyles.Normal);
        ApplyHudTextStyle(eliminatedText, new Vector2(14f, -54f), new Vector2(182f, 18f), TextAlignmentOptions.Left, 13, FontStyles.Normal);

        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
    }

    private void CreateHudAccent(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        Transform existing = parent.Find("HudAccent");
        Image accentImage;
        if (existing != null)
        {
            accentImage = existing.GetComponent<Image>();
        }
        else
        {
            GameObject accent = new GameObject("HudAccent");
            accent.transform.SetParent(parent, false);
            accent.transform.SetSiblingIndex(1);
            accent.AddComponent<RectTransform>();
            accentImage = accent.AddComponent<Image>();
        }

        if (accentImage != null)
        {
            accentImage.color = new Color(0.1f, 0.68f, 1f, 1f);
            RectTransform rect = accentImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(5f, 0f);
        }
    }

    private void BuildPauseMenu()
    {
        if (playerNameText == null)
        {
            return;
        }

        Canvas canvas = playerNameText.GetComponentInParent<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.transform : null;
        if (canvasRoot == null)
        {
            return;
        }

        pauseButton = CreateTopRightButton(canvasRoot, "PauseButton", "II", new Vector2(-8f, -8f), new Vector2(42f, 34f));
        pausePanel = CreateOverlayPanel(canvasRoot, "PausePanel");
        pausePanel.SetActive(false);

        RectTransform card = CreatePanelCard(pausePanel.transform, "PauseCard", new Vector2(260f, 170f));
        CreatePanelTitle(card, "Tạm dừng", new Vector2(0f, -26f));
        resumeButton = CreateMenuButton(card, "ResumeButton", "Tiếp tục", new Vector2(0f, -80f));
        quitButton = CreateMenuButton(card, "QuitButton", "Thoát", new Vector2(0f, -126f));
    }

    private Button CreateTopRightButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
    {
        Transform existing = parent.Find(name);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();
            buttonObject.AddComponent<Image>();
            buttonObject.AddComponent<Button>();
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.08f, 0.12f, 0.2f, 0.92f);
        }

        TMP_Text text = EnsureButtonLabel(buttonObject.transform, label);
        text.fontSize = 20;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.characterSpacing = 8f;

        return buttonObject.GetComponent<Button>();
    }

    private GameObject CreateOverlayPanel(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject panelObject = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            panelObject.transform.SetParent(parent, false);
            panelObject.AddComponent<RectTransform>();
            panelObject.AddComponent<Image>();
        }

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panelObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0f, 0.02f, 0.05f, 0.48f);
        }

        return panelObject;
    }

    private RectTransform CreatePanelCard(Transform parent, string name, Vector2 size)
    {
        Transform existing = parent.Find(name);
        GameObject cardObject = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            cardObject.transform.SetParent(parent, false);
            cardObject.AddComponent<RectTransform>();
            cardObject.AddComponent<Image>();
        }

        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = cardObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.07f, 0.11f, 0.17f, 0.96f);
        }

        return rect;
    }

    private void CreatePanelTitle(RectTransform parent, string content, Vector2 anchoredPosition)
    {
        Transform existing = parent.Find("Title");
        GameObject titleObject = existing != null ? existing.gameObject : new GameObject("Title");
        if (existing == null)
        {
            titleObject.transform.SetParent(parent, false);
            titleObject.AddComponent<RectTransform>();
            titleObject.AddComponent<TextMeshProUGUI>();
        }

        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(180f, 36f);

        TMP_Text text = titleObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = 24;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }

    private Button CreateMenuButton(RectTransform parent, string name, string label, Vector2 anchoredPosition)
    {
        Transform existing = parent.Find(name);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();
            buttonObject.AddComponent<Image>();
            buttonObject.AddComponent<Button>();
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(180f, 34f);

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = name == "QuitButton"
                ? new Color(0.22f, 0.27f, 0.36f, 1f)
                : new Color(0.12f, 0.61f, 1f, 1f);
        }

        TMP_Text text = EnsureButtonLabel(buttonObject.transform, label);
        text.fontSize = 18;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;

        return buttonObject.GetComponent<Button>();
    }

    private TMP_Text EnsureButtonLabel(Transform parent, string content)
    {
        Transform existing = parent.Find("Label");
        GameObject labelObject = existing != null ? existing.gameObject : new GameObject("Label");
        if (existing == null)
        {
            labelObject.transform.SetParent(parent, false);
            labelObject.AddComponent<RectTransform>();
            labelObject.AddComponent<TextMeshProUGUI>();
        }

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = labelObject.GetComponent<TMP_Text>();
        text.text = content;
        text.color = Color.white;
        return text;
    }

    private void PauseGame()
    {
        if (resultPanel != null && resultPanel.activeSelf)
        {
            return;
        }

        SetPauseState(true);
    }

    private void ResumeGame()
    {
        SetPauseState(false);
    }

    private void SetPauseState(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }
    }

    private void ApplyHudTextStyle(
        TMP_Text text,
        Vector2 anchoredPosition,
        Vector2 size,
        TextAlignmentOptions alignment,
        float fontSize,
        FontStyles fontStyle)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void StyleResultPanel()
    {
        if (resultPanel == null)
        {
            return;
        }

        Image panelImage = resultPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.06f, 0.08f, 0.13f, 0.92f);
        }

        CreateResultDecor(resultPanel.transform);

        if (resultTitleText != null)
        {
            resultTitleText.color = Color.white;
            resultTitleText.fontSize = 54;
        }

        if (resultDescriptionText != null)
        {
            resultDescriptionText.color = new Color(0.88f, 0.93f, 0.98f, 1f);
            resultDescriptionText.fontSize = 24;
        }

        StyleResultButtons();
        ApplyButtonVietnameseLabels();
    }

    private void CreateResultDecor(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        CreatePanelBar(parent, "TopAccent", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(360f, 8f), new Color(0.14f, 0.72f, 1f, 1f));
        CreatePanelBar(parent, "BottomAccent", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(360f, 4f), new Color(0.14f, 0.72f, 1f, 0.55f));
        CreatePanelBar(parent, "CenterGlow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(420f, 220f), new Color(0.2f, 0.5f, 0.8f, 0.08f));
    }

    private void CreatePanelBar(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        Transform existing = parent.Find(name);
        Image image;
        if (existing != null)
        {
            image = existing.GetComponent<Image>();
        }
        else
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent, false);
            bar.transform.SetSiblingIndex(0);
            RectTransform rect = bar.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            image = bar.AddComponent<Image>();
        }

        if (image != null)
        {
            image.color = color;
            image.raycastTarget = false;
        }
    }

    private void StyleResultButtons()
    {
        StyleButton("ReplayButton", new Color(0.14f, 0.62f, 1f, 1f));
        StyleButton("Back To MenuButton", new Color(0.18f, 0.24f, 0.34f, 1f));
    }

    private void StyleButton(string buttonName, Color backgroundColor)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name != buttonName)
            {
                continue;
            }

            Image image = buttons[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = backgroundColor;
            }

            TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 24;
                label.fontStyle = FontStyles.Bold;
                label.color = Color.white;
            }
        }
    }

    private void ApplyButtonVietnameseLabels()
    {
        SetButtonLabel("ReplayButton", "Chơi Lại");
        SetButtonLabel("Back To MenuButton", "Về Menu");
    }

    private void SetButtonLabel(string buttonName, string labelText)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name != buttonName)
            {
                continue;
            }

            TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = labelText;
            }
        }
    }

    private void BindButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name != buttonName)
            {
                continue;
            }

            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(action);
            return;
        }
    }
}
