using UnityEngine;
using UnityEngine.UI;
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

    private void BindButton(string buttonName, UnityEngine.Events.UnityAction action, Transform root = null)
    {
        if (root != null)
        {
            Button[] localButtons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < localButtons.Length; i++)
            {
                if (localButtons[i].name != buttonName)
                {
                    continue;
                }

                localButtons[i].onClick.RemoveAllListeners();
                localButtons[i].onClick.AddListener(action);
                return;
            }
        }

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
