using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý cài đặt âm lượng, bật/tắt âm thanh và số bot.
/// Có thể dùng ở Main Menu hoặc Lobby nếu bạn muốn tái sử dụng.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("Volume UI")]
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField] private Slider volumeSlider;

    [Header("Bot UI")]
    [SerializeField] private TMP_Text botCountValueText;

    private void OnEnable()
    {
        EnsureVolumeSlider();
        EnsureSettingsCard();
        WireButtons();
        RefreshUI();
    }

    public void IncreaseVolume()
    {
        SetVolume(PlayerData.GetVolume() + 0.1f, true);
    }

    public void DecreaseVolume()
    {
        SetVolume(PlayerData.GetVolume() - 0.1f, true);
    }

    public void IncreaseBots()
    {
        PlayClick();
        PlayerData.SetBotCount(PlayerData.GetBotCount() + 1);
        RefreshUI();
    }

    public void DecreaseBots()
    {
        PlayClick();
        PlayerData.SetBotCount(PlayerData.GetBotCount() - 1);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (volumeValueText != null)
        {
            volumeValueText.text = "Âm lượng: " + Mathf.RoundToInt(PlayerData.GetVolume() * 100f) + "%";
            volumeValueText.alignment = TextAlignmentOptions.Center;
            volumeValueText.fontSize = 30;
            volumeValueText.fontStyle = FontStyles.Bold;
        }

        if (botCountValueText != null)
        {
            botCountValueText.text = "Số bot: " + PlayerData.GetBotCount();
            botCountValueText.alignment = TextAlignmentOptions.Center;
            botCountValueText.fontSize = 28;
            botCountValueText.fontStyle = FontStyles.Bold;
        }

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(PlayerData.GetVolume());
        }

        ApplyVietnameseButtonLabels();
        HideLegacyVolumeButtons();
        StyleSettingsButtons();
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
        BindButton("Volume +Button", IncreaseVolume);
        BindButton("Volume -Button", DecreaseVolume);
        BindButton("Bots +Button", IncreaseBots);
        BindButton("Bots -Button", DecreaseBots);
    }

    private void BindButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
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

    private void ApplyVietnameseButtonLabels()
    {
        SetButtonLabel("Bots +Button", "+");
        SetButtonLabel("Bots -Button", "-");
        SetButtonLabel("BackButton", "Quay Lại");
    }

    private void SetButtonLabel(string buttonName, string labelText)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
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

    public void OnVolumeSliderChanged(float value)
    {
        SetVolume(value, false);
    }

    private void SetVolume(float value, bool playClick)
    {
        if (playClick)
        {
            PlayClick();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(value);
        }
        else
        {
            PlayerData.SetVolume(value);
        }

        RefreshUI();
    }

    private void EnsureVolumeSlider()
    {
        if (volumeSlider != null || volumeValueText == null || volumeValueText.transform.parent == null)
        {
            return;
        }

        Transform parent = volumeValueText.transform.parent;
        RepositionSettingsLayout(parent);
        Transform existing = parent.Find("VolumeSlider");
        if (existing != null)
        {
            volumeSlider = existing.GetComponent<Slider>();
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveAllListeners();
                volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
                StyleExistingSlider();
            }
            return;
        }

        GameObject sliderObject = new GameObject("VolumeSlider");
        sliderObject.transform.SetParent(parent, false);
        RectTransform rect = sliderObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 124f);
        rect.sizeDelta = new Vector2(320f, 34f);

        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(0.1f, 0.16f, 0.26f, 0.96f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(12f, 8f);
        fillAreaRect.offsetMax = new Vector2(-12f, -8f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.18f, 0.76f, 1f, 1f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0f, 0f);
        handleAreaRect.anchorMax = new Vector2(1f, 1f);
        handleAreaRect.offsetMin = new Vector2(12f, 0f);
        handleAreaRect.offsetMax = new Vector2(-12f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.98f, 1f, 1f, 1f);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(28f, 28f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.onValueChanged.AddListener(OnVolumeSliderChanged);

        volumeSlider = slider;
    }

    private void HideLegacyVolumeButtons()
    {
        HideButton("Volume +Button");
        HideButton("Volume -Button");
        HideButton("Sound ToggleButton");
        HideText("SoundLabel");
    }

    private void HideButton(string buttonName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == buttonName)
            {
                buttons[i].gameObject.SetActive(false);
            }
        }
    }

    private void HideText(string objectName)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == objectName)
            {
                texts[i].gameObject.SetActive(false);
            }
        }
    }

    private void RepositionSettingsLayout(Transform parent)
    {
        SetTextPosition(parent, "VolumeLabel", new Vector2(0f, 190f));
        SetTextPosition(parent, "BotLabel", new Vector2(0f, 34f));
        SetButtonPosition(parent, "Bots +Button", new Vector2(112f, 34f));
        SetButtonPosition(parent, "Bots -Button", new Vector2(-112f, 34f));
        SetButtonPosition(parent, "BackButton", new Vector2(0f, -82f));
    }

    private void SetTextPosition(Transform parent, string objectName, Vector2 anchoredPosition)
    {
        if (parent == null)
        {
            return;
        }

        Transform child = parent.Find(objectName);
        if (child != null && child.TryGetComponent(out RectTransform rect))
        {
            rect.anchoredPosition = anchoredPosition;
        }
    }

    private void SetButtonPosition(Transform parent, string objectName, Vector2 anchoredPosition)
    {
        if (parent == null)
        {
            return;
        }

        Transform child = parent.Find(objectName);
        if (child != null && child.TryGetComponent(out RectTransform rect))
        {
            rect.anchoredPosition = anchoredPosition;
        }
    }

    private void EnsureSettingsCard()
    {
        if (volumeValueText == null || volumeValueText.transform.parent == null)
        {
            return;
        }

        Transform parent = volumeValueText.transform.parent;
        CreateOrUpdatePanelBlock(parent, "SettingsCard", new Vector2(0f, 58f), new Vector2(360f, 340f), new Color(0.04f, 0.08f, 0.14f, 0.9f), 0);
        CreateOrUpdatePanelBlock(parent, "SettingsGlow", new Vector2(0f, 94f), new Vector2(300f, 120f), new Color(0.18f, 0.62f, 1f, 0.08f), 1);
        HidePanelBlock(parent, "SettingsAccent");
    }

    private void CreateOrUpdatePanelBlock(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, int siblingIndex)
    {
        Transform existing = parent.Find(name);
        Image image;
        if (existing != null)
        {
            image = existing.GetComponent<Image>();
        }
        else
        {
            GameObject block = new GameObject(name);
            block.transform.SetParent(parent, false);
            block.transform.SetSiblingIndex(siblingIndex);
            RectTransform rect = block.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            image = block.AddComponent<Image>();
            image.raycastTarget = false;
        }

        if (image == null)
        {
            return;
        }

        RectTransform imageRect = image.rectTransform;
        imageRect.anchoredPosition = anchoredPosition;
        imageRect.sizeDelta = size;
        image.color = color;
    }

    private void HidePanelBlock(Transform parent, string name)
    {
        if (parent == null)
        {
            return;
        }

        Transform existing = parent.Find(name);
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
        }
    }

    private void StyleExistingSlider()
    {
        if (volumeSlider == null)
        {
            return;
        }

        RectTransform sliderRect = volumeSlider.GetComponent<RectTransform>();
        if (sliderRect != null)
        {
            sliderRect.anchoredPosition = new Vector2(0f, 124f);
            sliderRect.sizeDelta = new Vector2(320f, 34f);
        }

        Image sliderBackground = volumeSlider.GetComponent<Image>();
        if (sliderBackground != null)
        {
            sliderBackground.color = new Color(0.1f, 0.16f, 0.26f, 0.96f);
        }

        if (volumeSlider.fillRect != null && volumeSlider.fillRect.TryGetComponent(out Image fillImage))
        {
            fillImage.color = new Color(0.18f, 0.76f, 1f, 1f);
        }

        if (volumeSlider.handleRect != null)
        {
            volumeSlider.handleRect.sizeDelta = new Vector2(28f, 28f);
            if (volumeSlider.handleRect.TryGetComponent(out Image handleImage))
            {
                handleImage.color = new Color(0.98f, 1f, 1f, 1f);
            }
        }
    }

    private void StyleSettingsButtons()
    {
        StyleButton("Bots +Button", new Vector2(58f, 58f), new Color(0.16f, 0.58f, 1f, 1f), 34);
        StyleButton("Bots -Button", new Vector2(58f, 58f), new Color(0.16f, 0.58f, 1f, 1f), 34);
        StyleButton("BackButton", new Vector2(250f, 52f), new Color(0.16f, 0.34f, 0.62f, 1f), 24);
    }

    private void StyleButton(string buttonName, Vector2 size, Color color, float fontSize)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name != buttonName)
            {
                continue;
            }

            RectTransform rect = buttons[i].GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = size;
            }

            Image image = buttons[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }

            TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = fontSize;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
            }
        }
    }
}
