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
            instructionsText.text = "Enter your name, then press Play to start the match against bots.";
        }

        RefreshBotCountText();
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

        string playerName = "Player";
        if (nameInputField != null)
        {
            playerName = string.IsNullOrWhiteSpace(nameInputField.text) ? "Player" : nameInputField.text.Trim();
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
            botCountText.text = "Bots in match: " + PlayerData.GetBotCount();
        }
    }

    private void WireButtons()
    {
        BindButton("PlayButton", OnPlayClicked);
        BindButton("BackButton", OnBackClicked);
    }

    private void BindButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
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
