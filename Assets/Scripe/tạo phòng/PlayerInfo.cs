using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public static PlayerInfo Instance;
    public string PlayerName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}