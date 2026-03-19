using Fusion;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject joinRoomPanel;
    public GameObject lobbyCanvas; // Canvas lobby
    public TMP_InputField joinRoomInput;

    [Header("Fusion Setup")]
    public NetworkRunner runnerPrefab; // Prefab NetworkRunner
    private NetworkRunner runner;

    [Header("Lobby Scene")]
    public SceneRef lobbyScene; // Lobby SceneRef

    // --- Tạo/Join phòng ---
    public async void StartGame(GameMode mode, string sessionID)
    {
        if (runner == null)
        {
            runner = Instantiate(runnerPrefab);
            DontDestroyOnLoad(runner.gameObject);
        }

        runner.ProvideInput = (mode == GameMode.Client); // client gửi input

        var props = new Dictionary<string, SessionProperty>();
        if (mode == GameMode.Host) props["HostName"] = PlayerInfo.Instance.PlayerName;

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionID,
            SessionProperties = props,
            Scene = lobbyScene,
            PlayerCount = 2,
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        // Hiển thị canvas lobby sau khi join xong
        lobbyCanvas.SetActive(true);
        mainMenuPanel.SetActive(false);
        joinRoomPanel.SetActive(false);

        Debug.Log($"{mode} started. RoomID: {sessionID}");
    }

    // --- Nút tạo phòng ---
    public void OnClickCreate()
    {
        string randomID = Random.Range(100000, 999999).ToString();
        StartGame(GameMode.Host, randomID);
    }

    // --- Nút mở bảng Join Room ---
    public void OpenJoinRoom()
    {
        joinRoomPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    // --- Nút quay lại menu ---
    public void BackFromJoin()
    {
        joinRoomPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // --- Nút xác nhận join phòng ---
    public void ConfirmJoin()
    {
        string id = joinRoomInput.text.Trim();
        if (!string.IsNullOrEmpty(id))
        {
            StartGame(GameMode.Client, id);
        }
        else
        {
            Debug.LogWarning("Nhập ID phòng trước khi join!");
        }
    }
}