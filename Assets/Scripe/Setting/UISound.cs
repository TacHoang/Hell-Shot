using UnityEngine;

public class UISound : MonoBehaviour
{
    public static UISound Instance;

    public AudioSource sfxSource;
    public AudioClip clickSound;

    float lastPlayTime = 0f;
    public float cooldown = 0.15f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PlayClick()
    {
        if (Input.GetMouseButton(1)) return;
        if (Time.unscaledTime - lastPlayTime < cooldown)
            return;

        sfxSource.PlayOneShot(clickSound);
        lastPlayTime = Time.unscaledTime;
    }
}
