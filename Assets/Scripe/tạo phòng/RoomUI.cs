using UnityEngine;
using TMPro;
using Fusion;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;

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
    private bool hasLeft = false; // 👈 chống load nhiều lần

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null && runner.SessionInfo.IsValid)
        {
            roomIDText.text = "ID: " + runner.SessionInfo.Name;
            startGameButton.SetActive(runner.IsServer);

            if (runner.SessionInfo.Properties.TryGetValue("HostName", out var hostName))
            {
                roomNameText.text = "Phòng của: " + hostName;
            }
            else
            {
                roomNameText.text = "Phòng đang tải...";
            }
        }

        InvokeRepeating(nameof(RefreshPlayers), 0.5f, 1f);
    }

    void Update()
    {
        if (runner == null || hasLeft) return;

        // 👉 CLIENT tự detect host thoát
        if (!runner.IsServer)
        {
            if (runner.ActivePlayers.Count() <= 1)
            {
                hasLeft = true;
                Debug.Log("Host đã thoát → quay về menu");
                SceneManager.LoadScene(0);
            }
        }
    }

    public void RefreshPlayers()
    {
        var players = FindObjectsOfType<RoomPlayer>();

        // 🔥 FIX NULL CRASH
        players = System.Array.FindAll(players, p =>
            p != null &&
            p.Object != null &&
            p.Object.InputAuthority != null
        );

        if (players.Length > 1)
        {
            System.Array.Sort(players, (a, b) =>
                a.Object.InputAuthority.RawEncoded.CompareTo(b.Object.InputAuthority.RawEncoded)
            );
        }

        playerSlot1.text = "";
        playerSlot2.text = "";

        if (players.Length > 0)
        {
            string name = players[0].PlayerName.ToString();
            playerSlot1.text = string.IsNullOrEmpty(name) ? "..." : name;
        }

        if (players.Length > 1)
        {
            string name = players[1].PlayerName.ToString();
            playerSlot2.text = string.IsNullOrEmpty(name) ? "..." : name;
        }
    }

    public void OnClickStartGame()
    {
        if (runner != null && runner.IsServer)
        {
            runner.LoadScene(gameplayScene);
        }
    }

    // ================== LEAVE ==================

    public void OnClickLeave()
    {
        if (!hasLeft)
        {
            hasLeft = true;
            StartCoroutine(FullResetGame());
        }
    }

    IEnumerator FullResetGame()
    {
        CancelInvoke();

        var runners = FindObjectsOfType<NetworkRunner>();

        foreach (var r in runners)
        {
            if (r != null && r.IsRunning)
            {
                Debug.Log("Shutdown runner: " + r.name);
                r.Shutdown(true); // 👈 force leave
            }
        }

        yield return new WaitForSeconds(1.5f);

        foreach (var r in runners)
        {
            if (r != null)
                Destroy(r.gameObject);
        }

        if (PlayerInfo.Instance != null)
            PlayerInfo.Instance.PlayerName = "";

        RoomUI.Instance = null;

        Debug.Log("RESET COMPLETE → Load lại game");

        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}