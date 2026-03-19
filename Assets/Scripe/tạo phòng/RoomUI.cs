using UnityEngine;
using TMPro;
using Fusion;

public class RoomUI : MonoBehaviour
{
    public static RoomUI Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI roomIDText;
    public TextMeshProUGUI roomNameText;
    public GameObject startGameButton;

    [Header("Player Slots (max 2)")]
    public TextMeshProUGUI playerSlot1; // Host
    public TextMeshProUGUI playerSlot2; // Client

    [Header("Gameplay Scene")]
    public SceneRef gameplayScene; // SceneRef gameplay

    private NetworkRunner _runner;

    void Awake() => Instance = this;

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();

        if (_runner != null && _runner.SessionInfo.IsValid)
        {
            // Hiển thị RoomID + Host Name
            roomIDText.text = "ID: " + _runner.SessionInfo.Name;

            if (_runner.SessionInfo.Properties.TryGetValue("HostName", out var host))
            {
                roomNameText.text = "Phòng của: " + host;
                playerSlot1.text = host.ToString(); // Host = playerSlot1
            }
            else
            {
                roomNameText.text = "Phòng: Đang tải...";
                playerSlot1.text = "";
            }

            if (startGameButton != null)
                startGameButton.SetActive(_runner.IsServer);
        }
    }

    public void AddPlayer(string playerName)
    {
        if (playerName == playerSlot1.text) return; // tránh trùng với host
        if (playerSlot2.text == "") playerSlot2.text = playerName;
        else Debug.LogWarning("Hết slot người chơi!");
    }

    public void RemovePlayer(string playerName)
    {
        if (playerSlot2.text == playerName) playerSlot2.text = "";
    }

    public void OnClickStartGame()
    {
        if (_runner != null && _runner.IsServer)
        {
            _runner.LoadScene(gameplayScene); // load gameplay
        }
    }

    public void OnClickLeave()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene(0); // về menu
        }
    }
}