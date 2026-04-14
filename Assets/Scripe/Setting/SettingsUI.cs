using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Root")]
    public GameObject settingsCanvas;

    [Header("UI Components")]
    public TMP_Dropdown resolutionDropdown;
    public Slider musicSlider;
    public Slider sfxSlider;

    int tempRes;

    void Start()
    {
        settingsCanvas.SetActive(false);

        SetupDropdown();
        LoadUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsCanvas.activeSelf)
                CloseSettings();
            else
                OpenSettings();
        }
    }

    void SetupDropdown()
    {
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        foreach (var r in GameSettings.Instance.resolutions)
        {
            options.Add(r.width + " x " + r.height);
        }

        resolutionDropdown.AddOptions(options);
    }

    public void OpenSettings()
    {
        settingsCanvas.SetActive(true);
        LoadUI();
    }

    public void CloseSettings()
    {
        settingsCanvas.SetActive(false);
    }

    void LoadUI()
    {
        var gs = GameSettings.Instance;

        resolutionDropdown.value = gs.resolutionIndex;
        resolutionDropdown.RefreshShownValue();

        tempRes = gs.resolutionIndex;

        musicSlider.value = AudioManager.Instance.GetMusicVolume();
        sfxSlider.value = AudioManager.Instance.GetSFXVolume();
    }

    public void OnMusicChanged(float v)
    {
        AudioManager.Instance.SetMusicVolume(v);
    }

    public void OnSFXChanged(float v)
    {
        AudioManager.Instance.SetSFXVolume(v);
    }

    public void OnResolutionChanged(int i)
    {
        tempRes = i;
        UISound.Instance.PlayClick();
    }

    public void Apply()
    {
        var gs = GameSettings.Instance;

        gs.resolutionIndex = tempRes;

        gs.ApplySettings();
        gs.SaveSettings();

        AudioManager.Instance.SaveVolume();

        CloseSettings();
    }

    public void Cancel()
    {
        LoadUI();
        CloseSettings();
    }
}