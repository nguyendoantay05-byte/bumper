using UnityEngine;

/// <summary>
/// Quản lý âm thanh toàn game.
/// Singleton này nên đặt trong scene MainMenu để giữ xuyên suốt các scene khác.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const string PreferredMusicClipName = "Graze the Roof - Laura Shigihara";

    public static AudioManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapAudioManager()
    {
        if (Instance != null || Object.FindAnyObjectByType<AudioManager>() != null)
        {
            return;
        }

        GameObject audioManagerObject = new GameObject("AudioManager");
        audioManagerObject.AddComponent<AudioManager>();
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip bumpClip;
    [SerializeField] private AudioClip buttonClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [SerializeField] private AudioClip musicClip;

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

        EnsureAudioSources();
        EnsureRuntimeClips();
        LoadSettings();
    }

    private void Start()
    {
        ApplySettings();
        StartMusicIfNeeded();
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

        StartMusicIfNeeded();
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

        if (!soundEnabled)
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }
        }
        else
        {
            StartMusicIfNeeded();
        }
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

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicObject = new GameObject("MusicSource");
            musicObject.transform.SetParent(transform, false);
            musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        musicSource.spatialBlend = 0f;
        musicSource.priority = 32;

        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SfxSource");
            sfxObject.transform.SetParent(transform, false);
            sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        sfxSource.spatialBlend = 0f;
        sfxSource.priority = 16;
    }

    private void EnsureRuntimeClips()
    {
        AudioClip importedMusicClip = TryLoadImportedMusicClip();
        if (importedMusicClip != null)
        {
            musicClip = importedMusicClip;
        }

        if (buttonClip == null)
        {
            buttonClip = CreateToneClip("ButtonClip", 860f, 0.08f, 0.22f);
        }

        if (bumpClip == null)
        {
            bumpClip = CreateSweepClip("BumpClip", 220f, 90f, 0.16f, 0.35f);
        }

        if (winClip == null)
        {
            winClip = CreateChordClip("WinClip", new[] { 523.25f, 659.25f, 783.99f }, 0.6f, 0.18f);
        }

        if (loseClip == null)
        {
            loseClip = CreateSweepClip("LoseClip", 260f, 110f, 0.55f, 0.2f);
        }

        if (musicClip == null)
        {
            musicClip = CreateAmbientLoopClip("MusicClip", 24f);
        }
    }

    private AudioClip TryLoadImportedMusicClip()
    {
        AudioClip[] audioClips = Resources.LoadAll<AudioClip>("Audio");
        if (audioClips == null || audioClips.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < audioClips.Length; i++)
        {
            AudioClip clip = audioClips[i];
            if (clip == null)
            {
                continue;
            }

            if (clip.name == PreferredMusicClipName)
            {
                return clip;
            }
        }

        AudioClip bestClip = null;
        float bestLength = -1f;

        for (int i = 0; i < audioClips.Length; i++)
        {
            AudioClip clip = audioClips[i];
            if (clip == null)
            {
                continue;
            }

            if (clip.length > bestLength)
            {
                bestClip = clip;
                bestLength = clip.length;
            }
        }

        return bestClip;
    }

    private void StartMusicIfNeeded()
    {
        if (musicSource == null || musicClip == null)
        {
            return;
        }

        if (musicSource.clip != musicClip)
        {
            musicSource.clip = musicClip;
        }

        if (!soundEnabled)
        {
            return;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private AudioClip CreateToneClip(string clipName, float frequency, float duration, float amplitude)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (i / (float)sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * envelope;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateSweepClip(string clipName, float startFrequency, float endFrequency, float duration, float amplitude)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            phase += 2f * Mathf.PI * frequency / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t);
            samples[i] = Mathf.Sin(phase) * amplitude * envelope;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateChordClip(string clipName, float[] notes, float duration, float amplitude)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(1f - (t / duration));
            float sample = 0f;
            for (int noteIndex = 0; noteIndex < notes.Length; noteIndex++)
            {
                sample += Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t);
            }

            samples[i] = (sample / Mathf.Max(1, notes.Length)) * amplitude * envelope;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateAmbientLoopClip(string clipName, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float padA = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.05f;
            float padB = Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.04f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * 261.63f * t) * (0.015f + Mathf.Sin(t * 0.65f) * 0.006f);
            float wave = Mathf.Sin(t * 0.8f) * 0.5f + 0.5f;
            samples[i] = (padA + padB + shimmer) * (0.75f + wave * 0.25f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
