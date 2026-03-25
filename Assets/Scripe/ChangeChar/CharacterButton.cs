using UnityEngine;

public class CharacterButton : MonoBehaviour
{
    public CharacterPreview preview;

    public void OnClick_ChangeCharacter()
    {
        // Gọi hàm tăng index trong CharacterSelection
        CharacterSelection.Instance.NextCharacter();
        int index = CharacterSelection.Instance.characterIndex;
        
        preview.ShowCharacter(index);

        // 🔥 QUAN TRỌNG: Cập nhật luôn vào PlayerInfo để RoomPlayer thấy được
        if (PlayerInfo.Instance != null) {
            PlayerInfo.Instance.SelectedCharacterIndex = index;
        }
    }
}