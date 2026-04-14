using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public float musicVolume = 1f;
    public float sfxVolume = 1f;

    private List<AudioSource> musicSources = new List<AudioSource>();
    private List<AudioSource> sfxSources = new List<AudioSource>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolume();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 🔥 REGISTER
    public void RegisterMusic(AudioSource src)
    {
        if (!musicSources.Contains(src))
        {
            musicSources.Add(src);
            src.volume = musicVolume;
        }
    }

    public void RegisterSFX(AudioSource src)
    {
        if (!sfxSources.Contains(src))
        {
            sfxSources.Add(src);
            src.volume = sfxVolume;
        }
    }

    // 🔥 APPLY ALL
    public void SetMusicVolume(float v)
    {
        musicVolume = v;

        foreach (var s in musicSources)
        {
            if (s != null)
                s.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = v;

        foreach (var s in sfxSources)
        {
            if (s != null)
                s.volume = sfxVolume;
        }
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;

    public void SaveVolume()
    {
        PlayerPrefs.SetFloat("MusicVol", musicVolume);
        PlayerPrefs.SetFloat("SFXVol", sfxVolume);
        PlayerPrefs.Save();
    }

    void LoadVolume()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVol", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVol", 1f);
    }
}