using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip barkSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float bgmVolume = 1f;
    [SerializeField] private float uiVolume = 1f;
    [SerializeField] private float characterVolume = 1f;
    [SerializeField] private float effectVolume = 1f;
    [SerializeField] private bool isSoundEnabled = true;
    private AudioSource generalAudioSource;
    private AudioSource dogAudioSource;
    private AudioSource characterAudioSource;
    private AudioSource bgmSource;
    private SettingsManager settingsManager;

    public float MasterVolume { get => masterVolume; set => SetMasterVolume(value); }
    public float BGMVolume { get => bgmVolume; set => SetBGMVolume(value); }
    public float UIVolume { get => uiVolume; set => SetUIVolume(value); }
    public float CharacterVolume { get => characterVolume; set => SetCharacterVolume(value); }

    public float EffectVolume { get => effectVolume; set => SetEffectVolume(value); }
    public bool IsSoundEnabled { get => isSoundEnabled; set => SetSoundEnabled(value); }
    public bool IsBGMPlaying { get => bgmSource.isPlaying; }
    public bool HasBGMClip { get => bgmSource.clip != null; }

    void Awake()
    {
        Debug.Log("SoundManager Awake called");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            generalAudioSource = gameObject.AddComponent<AudioSource>();
            dogAudioSource = gameObject.AddComponent<AudioSource>();
            characterAudioSource = gameObject.AddComponent<AudioSource>();
            bgmSource = gameObject.AddComponent<AudioSource>();
            LoadSettings();
        }
        // else
        // {
        //     Destroy(gameObject);
        // }
    }

    public void PlayClickSound()
    {
        if (clickSound != null && isSoundEnabled)
        {
            generalAudioSource.volume = masterVolume * uiVolume;
            generalAudioSource.PlayOneShot(clickSound);
        }
    }

    public void PlayHoverSound()
    {
        if (hoverSound != null && isSoundEnabled)
        {
            generalAudioSource.volume = masterVolume * uiVolume;
            generalAudioSource.PlayOneShot(hoverSound);
        }
    }

    public void PlayPickupSound()
    {
        if (pickupSound != null && isSoundEnabled)
        {
            generalAudioSource.volume = masterVolume * effectVolume;
            generalAudioSource.PlayOneShot(pickupSound);
        }
    }

    public void PlayBarkSound()
    {
        if (barkSound != null && isSoundEnabled)
        {
            dogAudioSource.volume = masterVolume * characterVolume;
            dogAudioSource.PlayOneShot(barkSound);
        }
    }

    public void PlayFootstep()
    {
        if (footstepSound != null && isSoundEnabled)
        {
            characterAudioSource.volume = masterVolume * characterVolume;
            characterAudioSource.PlayOneShot(footstepSound);
        }
    }

    public void PlayBGM()
    {
        PlayBGM(bgmClip);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (!isSoundEnabled) return;
        if (clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.volume = masterVolume * bgmVolume;
            bgmSource.Play();
        }
    }

    public void ResumeBGM()
    {
        if (!isSoundEnabled) return;
        if (bgmSource.clip != null && !bgmSource.isPlaying)
        {
            bgmSource.UnPause();
        }
    }

    public void PauseBGM()
    {
        bgmSource.Pause();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    public void SetCharacterVolume(float volume)
    {
        characterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    public void SetEffectVolume(float volume)
    {
        effectVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    public void SetSoundEnabled(bool enabled)
    {
        isSoundEnabled = enabled;

        if (!enabled)
        {
            bgmSource.Stop();
            generalAudioSource.Stop();
            dogAudioSource.Stop();
            characterAudioSource.Stop();
        }
        else if (bgmSource.clip != null)
        {
            bgmSource.Play();
        }

        UpdateVolumes();
    }

    private void UpdateVolumes()
    {
        generalAudioSource.volume =
            isSoundEnabled ? masterVolume * uiVolume : 0;

        dogAudioSource.volume =
            isSoundEnabled ? masterVolume * characterVolume : 0;

        characterAudioSource.volume =
            isSoundEnabled ? masterVolume * characterVolume : 0;

        bgmSource.volume =
            isSoundEnabled ? masterVolume * bgmVolume : 0;

        SaveSettings();
    }

    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        uiVolume = PlayerPrefs.GetFloat("UIVolume", 1f);
        characterVolume = PlayerPrefs.GetFloat("CharacterVolume", 1f);
        effectVolume = PlayerPrefs.GetFloat("EffectVolume", 1f);
        isSoundEnabled = PlayerPrefs.GetInt("IsSoundEnabled", 1) == 1;
        UpdateVolumes();
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("UIVolume", uiVolume);
        PlayerPrefs.SetFloat("CharacterVolume", characterVolume);
        PlayerPrefs.SetFloat("EffectVolume", effectVolume);
        PlayerPrefs.SetInt("IsSoundEnabled", isSoundEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}