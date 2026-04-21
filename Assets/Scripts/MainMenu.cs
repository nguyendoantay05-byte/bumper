using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// Điều khiển màn hình Main Menu.
/// Dùng các panel trong cùng một scene để mở Settings và How To Play.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    private void Start()
    {
        UIRuntimeFix.Apply();
        WireButtons();
        ApplyVietnameseLabels();
        EnsureNavigationButtons();
        ShowMainPanel();
    }

    public void OnStartClicked()
    {
        PlayClick();
        SceneLoader.LoadLobby();
    }

    public void OnSettingsClicked()
    {
        PlayClick();
        ShowSettingsPanel();
    }

    public void OnHowToPlayClicked()
    {
        PlayClick();
        ShowHowToPlayPanel();
    }

    public void OnExitClicked()
    {
        PlayClick();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnBackFromSettings()
    {
        PlayClick();
        ShowMainPanel();
    }

    public void OnBackFromHowToPlay()
    {
        PlayClick();
        ShowMainPanel();
    }

    private void ShowMainPanel()
    {
        SetActiveSafe(mainPanel, true);
        SetActiveSafe(settingsPanel, false);
        SetActiveSafe(howToPlayPanel, false);
    }

    private void ShowSettingsPanel()
    {
        SetActiveSafe(mainPanel, false);
        SetActiveSafe(settingsPanel, true);
        SetActiveSafe(howToPlayPanel, false);
    }

    private void ShowHowToPlayPanel()
    {
        SetActiveSafe(mainPanel, false);
        SetActiveSafe(settingsPanel, false);
        SetActiveSafe(howToPlayPanel, true);
    }

    private void SetActiveSafe(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }

    private void PlayClick()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }
    }

    private void WireButtons()
    {
        BindButton("StartButton", OnStartClicked);
        BindButton("SettingsButton", OnSettingsClicked);
        BindButton("How To PlayButton", OnHowToPlayClicked);
        BindButton("ExitButton", OnExitClicked);

        if (settingsPanel != null)
        {
            BindButton("BackButton", OnBackFromSettings, settingsPanel.transform);
        }

        if (howToPlayPanel != null)
        {
            BindButton("BackButton", OnBackFromHowToPlay, howToPlayPanel.transform);
        }
    }

    private void ApplyVietnameseLabels()
    {
        SetButtonLabel("StartButton", "Bắt Đầu");
        SetButtonLabel("SettingsButton", "Cài Đặt");
        SetButtonLabel("How To PlayButton", "Hướng Dẫn");
        SetButtonLabel("ExitButton", "Thoát");
        SetButtonLabel("BackButton", "Quay Lại");

        SetTextByObjectName("TitleText", "Bumper");
        SetTextByObjectName("Title", "Bumper");
        SetTextByObjectName("HowText", "Dùng WASD để di chuyển\nĐẩy đối thủ rơi khỏi đảo\nNgười trụ lại cuối cùng sẽ thắng");
        SetTextByObjectName("VolumeLabel", "Âm lượng: 100%");
        SetTextByObjectName("BotLabel", "Số bot: 3");
    }

    private void EnsureNavigationButtons()
    {
        EnsureBackButton(settingsPanel, OnBackFromSettings);
        EnsureBackButton(howToPlayPanel, OnBackFromHowToPlay);
    }

    private void EnsureBackButton(GameObject panel, UnityEngine.Events.UnityAction action)
    {
        if (panel == null)
        {
            return;
        }

        Button button = FindPanelButton(panel.transform, "BackButton");
        if (button == null)
        {
            button = CreateRuntimeButton(panel.transform, "BackButton", "Quay Lại");
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = panel == howToPlayPanel ? new Vector2(0f, -185f) : new Vector2(0f, -220f);
            rect.sizeDelta = new Vector2(280f, 56f);
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.16f, 0.34f, 0.62f, 1f);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "Quay Lại";
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
        }

        button.gameObject.SetActive(true);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
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

    private void SetTextByObjectName(string objectName, string content)
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == objectName)
            {
                texts[i].text = content;
            }
        }
    }

    private void BindButton(string buttonName, UnityEngine.Events.UnityAction action, Transform root = null)
    {
        if (root != null)
        {
            Button localButton = FindPanelButton(root, buttonName);
            if (localButton != null)
            {
                localButton.onClick.RemoveAllListeners();
                localButton.onClick.AddListener(action);
                return;
            }
        }

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

    private Button FindPanelButton(Transform root, string buttonName)
    {
        if (root == null)
        {
            return null;
        }

        Button[] localButtons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < localButtons.Length; i++)
        {
            if (localButtons[i].name == buttonName)
            {
                return localButtons[i];
            }
        }

        return null;
    }

    private Button CreateRuntimeButton(Transform parent, string buttonName, string labelText)
    {
        GameObject buttonObject = new GameObject(buttonName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.34f, 0.62f, 1f);

        Button button = buttonObject.AddComponent<Button>();

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.color = Color.white;
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;

        return button;
    }
}
