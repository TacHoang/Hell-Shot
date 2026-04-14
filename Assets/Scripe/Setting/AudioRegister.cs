using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioRegister : MonoBehaviour
{
    public enum Type { Music, SFX }
    public Type type;

    void Start()
    {
        var src = GetComponent<AudioSource>();

        if (type == Type.SFX)
        {
            src.playOnAwake = false; // 🔥 chặn auto play
            src.Stop();              // 🔥 đảm bảo không phát
            AudioManager.Instance.RegisterSFX(src);
        }
        else
        {
            AudioManager.Instance.RegisterMusic(src);
        }
    }
}