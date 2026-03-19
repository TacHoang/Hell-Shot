using UnityEngine;

public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance;

    public int characterIndex = 0; // 0 → 3

    void Awake()
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

    public void NextCharacter()
    {
        characterIndex++;
        if (characterIndex > 3) characterIndex = 0;

        Debug.Log("Selected Character: " + characterIndex);
    }
}