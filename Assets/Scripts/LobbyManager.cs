using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Màn hình chờ trước khi vào trận.
/// Người chơi nhập tên rồi bấm Play.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_Text instructionsText;
    [SerializeField] private TMP_Text botCountText;

    private void Start()
    {
        UIRuntimeFix.Apply();
        WireButtons();

        if (nameInputField != null)
        {
            nameInputField.text = PlayerData.GetPlayerName();
        }

        if (instructionsText != null)
        {
            instructionsText.text = "Nhập tên của bạn, sau đó bấm Chơi để bắt đầu trận đấu với bot.";
        }

        RefreshBotCountText();
        ApplyVietnameseStaticLabels();
    }

    private void OnEnable()
    {
        RefreshBotCountText();
    }

    public void OnPlayClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        string playerName = "Người chơi";
        if (nameInputField != null)
        {
            playerName = string.IsNullOrWhiteSpace(nameInputField.text) ? "Người chơi" : nameInputField.text.Trim();
        }

        PlayerData.SetPlayerName(playerName);
        SceneLoader.LoadGame();
    }

    public void OnBackClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        SceneLoader.LoadMainMenu();
    }

    private void RefreshBotCountText()
    {
        if (botCountText != null)
        {
            botCountText.text = "Số bot trong trận: " + PlayerData.GetBotCount();
        }
    }

    private void ApplyVietnameseStaticLabels()
    {
        if (nameInputField != null)
        {
            if (nameInputField.placeholder is TMP_Text placeholder)
            {
                placeholder.text = "Nhập tên...";
            }
        }

        SetTextByObjectName("Title", "Phòng Chờ");
        SetButtonLabel("PlayButton", "Chơi");
        SetButtonLabel("BackButton", "Quay Lại");
    }

    private void WireButtons()
    {
        BindButton("PlayButton", OnPlayClicked);
        BindButton("BackButton", OnBackClicked);
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
}
