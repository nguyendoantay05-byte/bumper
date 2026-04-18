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

    [Header("Sound UI")]
    [SerializeField] private TMP_Text soundStateText;

    [Header("Bot UI")]
    [SerializeField] private TMP_Text botCountValueText;

    private void OnEnable()
    {
        WireButtons();
        RefreshUI();
    }

    public void IncreaseVolume()
    {
        PlayClick();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.IncreaseVolume(0.1f);
        }

        RefreshUI();
    }

    public void DecreaseVolume()
    {
        PlayClick();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.DecreaseVolume(0.1f);
        }

        RefreshUI();
    }

    public void ToggleSound()
    {
        PlayClick();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleSound();
        }
        else
        {
            PlayerData.SetSoundEnabled(!PlayerData.IsSoundEnabled());
        }

        RefreshUI();
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
            volumeValueText.text = "Volume: " + Mathf.RoundToInt(PlayerData.GetVolume() * 100f) + "%";
        }

        if (soundStateText != null)
        {
            soundStateText.text = "Sound: " + (PlayerData.IsSoundEnabled() ? "On" : "Off");
        }

        if (botCountValueText != null)
        {
            botCountValueText.text = "Bots: " + PlayerData.GetBotCount();
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
        BindButton("Volume +Button", IncreaseVolume);
        BindButton("Volume -Button", DecreaseVolume);
        BindButton("Sound ToggleButton", ToggleSound);
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
}
