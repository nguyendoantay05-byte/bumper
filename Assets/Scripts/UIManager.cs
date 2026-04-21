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
        WireButtons();
        HideResult();
        RefreshHUD();
    }

    private void Update()
    {
        RefreshHUD();
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMenu();
        }
    }

    private void WireButtons()
    {
        BindButton("ReplayButton", OnReplayClicked);
        BindButton("Back To MenuButton", OnBackToMenuClicked);
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

        Transform existing = textRect.parent.Find("HudCard");
        Image cardImage;
        if (existing != null)
        {
            cardImage = existing.GetComponent<Image>();
        }
        else
        {
            GameObject card = new GameObject("HudCard");
            card.transform.SetParent(textRect.parent, false);
            card.transform.SetSiblingIndex(0);
            RectTransform rect = card.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, -18f);
            rect.sizeDelta = new Vector2(250f, 98f);
            cardImage = card.AddComponent<Image>();
        }

        if (cardImage != null)
        {
            cardImage.color = new Color(0.05f, 0.09f, 0.16f, 0.82f);
        }

        CreateHudAccent(textRect.parent);
        ApplyHudTextStyle(playerNameText, new Vector2(20f, -18f), new Vector2(190f, 28f), TextAlignmentOptions.Left, 18, FontStyles.Bold);
        ApplyHudTextStyle(botsRemainingText, new Vector2(20f, -47f), new Vector2(150f, 24f), TextAlignmentOptions.Left, 15, FontStyles.Normal);
        ApplyHudTextStyle(eliminatedText, new Vector2(20f, -72f), new Vector2(120f, 24f), TextAlignmentOptions.Left, 15, FontStyles.Normal);

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
            RectTransform rect = accent.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, -18f);
            rect.sizeDelta = new Vector2(6f, 98f);
            accentImage = accent.AddComponent<Image>();
        }

        if (accentImage != null)
        {
            accentImage.color = new Color(0.1f, 0.68f, 1f, 1f);
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
