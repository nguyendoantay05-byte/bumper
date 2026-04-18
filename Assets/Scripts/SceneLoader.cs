using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Hỗ trợ chuyển scene an toàn trong cả Unity Editor và bản build.
/// </summary>
public static class SceneLoader
{
    public const string MainMenuName = "MainMenu";
    public const string LobbyName = "Lobby";
    public const string GameSceneName = "GameScene";

    public const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    public const string LobbyPath = "Assets/Scenes/Lobby.unity";
    public const string GameScenePath = "Assets/Scenes/GameScene.unity";

    public static void LoadMainMenu()
    {
        LoadSceneSafe(MainMenuName, MainMenuPath);
    }

    public static void LoadLobby()
    {
        LoadSceneSafe(LobbyName, LobbyPath);
    }

    public static void LoadGame()
    {
        LoadSceneSafe(GameSceneName, GameScenePath);
    }

    public static void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(currentScene.buildIndex);
            return;
        }

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(currentScene.path))
        {
            EditorSceneManager.LoadSceneInPlayMode(currentScene.path, new LoadSceneParameters(LoadSceneMode.Single));
        }
#endif
    }

    private static void LoadSceneSafe(string sceneName, string scenePath)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
        if (buildIndex >= 0)
        {
            SceneManager.LoadScene(buildIndex);
            return;
        }

#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError("Scene is not available: " + sceneName);
#endif
    }
}
