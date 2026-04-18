using UnityEngine;

/// <summary>
/// Lưu và đọc dữ liệu đơn giản bằng PlayerPrefs.
/// Dùng cho tên người chơi, âm lượng, bật/tắt âm thanh và số bot.
/// </summary>
public static class PlayerData
{
    private const string PlayerNameKey = "BUMPER_PLAYER_NAME";
    private const string VolumeKey = "BUMPER_VOLUME";
    private const string SoundEnabledKey = "BUMPER_SOUND_ENABLED";
    private const string BotCountKey = "BUMPER_BOT_COUNT";

    public const int MinBots = 1;
    public const int MaxBots = 8;

    public static string GetPlayerName()
    {
        string name = PlayerPrefs.GetString(PlayerNameKey, "Player");
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Player";
        }

        return name;
    }

    public static void SetPlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Player";
        }

        PlayerPrefs.SetString(PlayerNameKey, playerName.Trim());
        PlayerPrefs.Save();
    }

    public static float GetVolume()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
    }

    public static void SetVolume(float volume)
    {
        PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }

    public static bool IsSoundEnabled()
    {
        return PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
    }

    public static void SetSoundEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static int GetBotCount()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(BotCountKey, 3), MinBots, MaxBots);
    }

    public static void SetBotCount(int botCount)
    {
        PlayerPrefs.SetInt(BotCountKey, Mathf.Clamp(botCount, MinBots, MaxBots));
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(PlayerNameKey);
        PlayerPrefs.DeleteKey(VolumeKey);
        PlayerPrefs.DeleteKey(SoundEnabledKey);
        PlayerPrefs.DeleteKey(BotCountKey);
        PlayerPrefs.Save();
    }
}
