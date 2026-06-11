using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    private const string MusicEnabledKey = "settings.music.enabled";
    private const string MusicVolumeKey = "settings.music.volume";
    private const string SoundEnabledKey = "settings.sound.enabled";
    private const string SoundVolumeKey = "settings.sound.volume";

    private static readonly List<AudioSource> musicSources = new List<AudioSource>();
    private static readonly Dictionary<AudioSource, float> musicBaseVolumes = new Dictionary<AudioSource, float>();

    private static bool preferencesLoaded;
    private AudioSource sceneMusicSource;

    public static bool MusicEnabled { get; private set; } = true;
    public static bool SoundEnabled { get; private set; } = true;
    public static float MusicVolume { get; private set; } = 1f;
    public static float SoundVolume { get; private set; } = 1f;
    public static float EffectiveSoundVolume => SoundEnabled ? SoundVolume : 0f;

    private void Awake()
    {
        EnsurePreferencesLoaded();

        sceneMusicSource = GetComponent<AudioSource>();
        if (sceneMusicSource != null)
        {
            RegisterMusicSource(sceneMusicSource, sceneMusicSource.volume);
        }
    }

    private void OnDestroy()
    {
        if (sceneMusicSource != null)
        {
            UnregisterMusicSource(sceneMusicSource);
        }
    }

    public static void RegisterMusicSource(AudioSource source, float baseVolume)
    {
        if (source == null)
        {
            return;
        }

        EnsurePreferencesLoaded();

        if (!musicSources.Contains(source))
        {
            musicSources.Add(source);
        }

        musicBaseVolumes[source] = Mathf.Clamp01(baseVolume);
        ApplyMusicSettings();
    }

    public static void UnregisterMusicSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        musicSources.Remove(source);
        musicBaseVolumes.Remove(source);
    }

    public static void SetMusicEnabled(bool isEnabled)
    {
        EnsurePreferencesLoaded();
        MusicEnabled = isEnabled;
        PlayerPrefs.SetInt(MusicEnabledKey, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicSettings();
    }

    public static void SetMusicVolume(float volume)
    {
        EnsurePreferencesLoaded();
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
        ApplyMusicSettings();
    }

    public static void SetSoundEnabled(bool isEnabled)
    {
        EnsurePreferencesLoaded();
        SoundEnabled = isEnabled;
        PlayerPrefs.SetInt(SoundEnabledKey, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetSoundVolume(float volume)
    {
        EnsurePreferencesLoaded();
        SoundVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SoundVolumeKey, SoundVolume);
        PlayerPrefs.Save();
    }

    public static void PlaySfx(AudioSource source, AudioClip clip)
    {
        EnsurePreferencesLoaded();

        if (source == null || clip == null || !SoundEnabled)
        {
            return;
        }

        source.PlayOneShot(clip, EffectiveSoundVolume);
    }

    public static void LoadSettings()
    {
        EnsurePreferencesLoaded();
        ApplyMusicSettings();
    }

    private static void EnsurePreferencesLoaded()
    {
        if (preferencesLoaded)
        {
            return;
        }

        MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        SoundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SoundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
        preferencesLoaded = true;
    }

    private static void ApplyMusicSettings()
    {
        EnsurePreferencesLoaded();

        for (int i = musicSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = musicSources[i];
            if (source == null)
            {
                musicSources.RemoveAt(i);
                continue;
            }

            float baseVolume = musicBaseVolumes.TryGetValue(source, out float savedVolume) ? savedVolume : 1f;
            source.mute = !MusicEnabled;
            source.volume = MusicEnabled ? baseVolume * MusicVolume : 0f;
        }
    }
}
