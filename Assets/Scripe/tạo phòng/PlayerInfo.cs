using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public static PlayerInfo Instance;

    [Header("Player Data")]
    public string PlayerName;
    
    // 🔥 Biến này để lưu nhân vật bạn chọn ở Menu (0, 1, 2, 3...)
    public int SelectedCharacterIndex = 0; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Giữ Object này không bị xóa khi chuyển từ Menu sang Lobby/Gameplay
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Hàm phụ để bạn gọi từ nút bấm chọn nhân vật ở Menu
    public void SaveSelection(int index)
    {
        SelectedCharacterIndex = index;
        Debug.Log($"Đã lưu nhân vật số: {index} vào bộ nhớ tạm.");
    }
}