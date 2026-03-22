using UnityEngine;

public class CharacterPreview : MonoBehaviour
{
    public GameObject[] characterPrefabs; // 4 nhân vật
    public Transform previewPoint;

    private GameObject currentCharacter;

    void Start()
    {
        int index = CharacterSelection.Instance.characterIndex;

        ShowCharacter(index);
    }

    public void ShowCharacter(int index)
    {
        // Xoá nhân vật cũ
        if (currentCharacter != null)
        {
            Destroy(currentCharacter);
        }

        // Tạo nhân vật mới
        currentCharacter = Instantiate(
            characterPrefabs[index],
            previewPoint.position,
            Quaternion.identity
        );
    }
}