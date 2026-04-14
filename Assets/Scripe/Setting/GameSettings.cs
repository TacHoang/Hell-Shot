using UnityEngine;
using System.Collections.Generic;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    public int resolutionIndex = 0;
    public bool isFullscreen = true;

    public Resolution[] resolutions;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitResolutions();
            LoadSettings();
            ApplySettings();
        }
        else Destroy(gameObject);
    }

    void InitResolutions()
    {
        Resolution[] all = Screen.resolutions;

        List<Resolution> unique = new List<Resolution>();
        HashSet<string> seen = new HashSet<string>();

        foreach (var r in all)
        {
            string key = r.width + "x" + r.height;

            if (!seen.Contains(key))
            {
                seen.Add(key);
                unique.Add(r);
            }
        }

        resolutions = unique.ToArray();
    }

    public void ApplySettings()
    {
        if (resolutionIndex < 0 || resolutionIndex >= resolutions.Length)
            resolutionIndex = resolutions.Length - 1;

        Resolution res = resolutions[resolutionIndex];

        Screen.SetResolution(
            res.width,
            res.height,
            isFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed
        );

        Screen.fullScreen = isFullscreen; // 👈 đảm bảo sync
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", resolutions.Length - 1);
        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);

        isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
    }
}