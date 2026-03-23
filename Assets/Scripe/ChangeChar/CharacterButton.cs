using UnityEngine;

public class CharacterButton : MonoBehaviour
{
    public CharacterPreview preview;

    public void OnClick_ChangeCharacter()
    {
        CharacterSelection.Instance.NextCharacter();
        int index = CharacterSelection.Instance.characterIndex;
        preview.ShowCharacter(index);

        // Lưu lựa chọn nhân vật
        PlayerPrefs.SetInt("SelectedCharacter", index);
        PlayerPrefs.Save();
    }
}