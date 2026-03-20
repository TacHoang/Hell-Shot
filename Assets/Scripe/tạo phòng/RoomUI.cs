using UnityEngine;
using TMPro;
using Fusion;

public class RoomUI : MonoBehaviour
{
    public static RoomUI Instance;

    public TextMeshProUGUI roomIDText;
    public TextMeshProUGUI roomNameText;
    public GameObject startGameButton;

    public TextMeshProUGUI playerSlot1;
    public TextMeshProUGUI playerSlot2;

    public SceneRef gameplayScene;

    private NetworkRunner runner;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null)
        {
            roomIDText.text = "ID: " + runner.SessionInfo.Name;
            startGameButton.SetActive(runner.IsServer);

            if (runner.IsServer)
            {
                roomNameText.text = "Phòng của: " + PlayerInfo.Instance.PlayerName;
            }
        }

        // update UI liên tục tránh miss sync
        InvokeRepeating(nameof(RefreshPlayers), 0.5f, 1f);
    }

    public void RefreshPlayers()
    {
        var players = FindObjectsOfType<RoomPlayer>();

        playerSlot1.text = "";
        playerSlot2.text = "";

        if (players.Length > 0)
            playerSlot1.text = players[0].PlayerName.ToString();

        if (players.Length > 1)
            playerSlot2.text = players[1].PlayerName.ToString();
    }

    public void OnClickStartGame()
    {
        if (runner != null && runner.IsServer)
        {
            runner.LoadScene(gameplayScene);
        }
    }

    public void OnClickLeave()
    {
        if (runner != null)
        {
            runner.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}