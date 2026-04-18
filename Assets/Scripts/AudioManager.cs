using UnityEngine;

/// <summary>
/// Quản lý âm thanh toàn game.
/// Singleton này nên đặt trong scene MainMenu để giữ xuyên suốt các scene khác.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip bumpClip;
    [SerializeField] private AudioClip buttonClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;

    private float currentVolume = 1f;
    private bool soundEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    private void Start()
    {
        ApplySettings();
    }

    public void LoadSettings()
    {
        currentVolume = PlayerData.GetVolume();
        soundEnabled = PlayerData.IsSoundEnabled();
    }

    public void ApplySettings()
    {
        float volume = soundEnabled ? currentVolume : 0f;

        if (musicSource != null)
        {
            musicSource.volume = volume;
            musicSource.mute = !soundEnabled;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = volume;
            sfxSource.mute = !soundEnabled;
        }
    }

    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        PlayerData.SetVolume(currentVolume);
        ApplySettings();
    }

    public void IncreaseVolume(float step = 0.1f)
    {
        SetVolume(currentVolume + step);
    }

    public void DecreaseVolume(float step = 0.1f)
    {
        SetVolume(currentVolume - step);
    }

    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
        PlayerData.SetSoundEnabled(enabled);
        ApplySettings();
    }

    public void ToggleSound()
    {
        SetSoundEnabled(!soundEnabled);
    }

    public void PlayBump()
    {
        PlaySfx(bumpClip);
    }

    public void PlayButton()
    {
        PlaySfx(buttonClip);
    }

    public void PlayWin()
    {
        PlaySfx(winClip);
    }

    public void PlayLose()
    {
        PlaySfx(loseClip);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (!soundEnabled || clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }
}
