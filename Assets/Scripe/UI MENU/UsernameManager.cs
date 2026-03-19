using UnityEngine;
using TMPro;

public class UsernameManager : MonoBehaviour
{
    public TMP_InputField usernameInputField;  // Ô nhập tên player
    public GameObject usernamePanel;           // Panel chứa ô nhập
    public GameObject mainMenuPanel;           // Panel menu chính

    private void Start()
    {
        // Nếu đã có PlayerInfo và tên không rỗng → bỏ qua nhập tên
        if (PlayerInfo.Instance != null && !string.IsNullOrEmpty(PlayerInfo.Instance.PlayerName))
        {
            usernamePanel.SetActive(false);
            mainMenuPanel.SetActive(true);
            usernameInputField.text = PlayerInfo.Instance.PlayerName; // điền sẵn
        }
        else
        {
            usernamePanel.SetActive(true);
            mainMenuPanel.SetActive(false);

            // Load tên cũ nếu đã lưu trên máy
            if (PlayerPrefs.HasKey("SavedUsername"))
            {
                usernameInputField.text = PlayerPrefs.GetString("SavedUsername");
            }
        }
    }

    // Xác nhận tên player
    public void ConfirmUsername()
    {
        string name = usernameInputField.text.Trim();

        if (!string.IsNullOrEmpty(name))
        {
            // Lưu vào Singleton
            PlayerInfo.Instance.PlayerName = name;

            // Lưu vào máy
            PlayerPrefs.SetString("SavedUsername", name);

            // Chuyển sang menu chính
            usernamePanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Vui lòng nhập tên player!");
        }
    }
}