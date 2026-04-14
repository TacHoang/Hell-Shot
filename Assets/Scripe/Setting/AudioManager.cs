using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    List<AudioSource> musicSources = new();
    List<AudioSource> sfxSources = new();

    float musicVolume = 1f;
    float sfxVolume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolume();
        }
        else Destroy(gameObject);
    }

    public void RegisterMusic(AudioSource s)
    {
        if (!musicSources.Contains(s))
        {
            musicSources.Add(s);
            s.volume = musicVolume;
        }
    }

    public void RegisterSFX(AudioSource s)
    {
        if (!sfxSources.Contains(s))
        {
            sfxSources.Add(s);
            s.volume = sfxVolume;
        }
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = v;
        foreach (var s in musicSources)
            if (s) s.volume = v;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = v;
        foreach (var s in sfxSources)
            if (s) s.volume = v;
    }

    public void SaveVolume()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    void LoadVolume()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
}