using Fusion;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject joinRoomPanel;
    public TMP_InputField joinRoomInput;

    [Header("Fusion Setup")]
    public NetworkRunner runnerPrefab;
    private NetworkRunner runner;

    [Header("Scenes")]
    public SceneRef lobbyScene;

    public async void StartGame(GameMode mode, string sessionID)
    {
        if (runner == null)
        {
            runner = Instantiate(runnerPrefab);
            DontDestroyOnLoad(runner.gameObject);
        }

        runner.ProvideInput = (mode == GameMode.Client);

        var props = new Dictionary<string, SessionProperty>();
        if (mode == GameMode.Host)
            props["HostName"] = PlayerInfo.Instance.PlayerName;

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionID,
            SessionProperties = props,
            Scene = lobbyScene, // 👉 QUAN TRỌNG: load lobby scene
            PlayerCount = 2,
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        // Ẩn menu cũ
        mainMenuPanel.SetActive(false);
        joinRoomPanel.SetActive(false);

        Debug.Log($"{mode} started. RoomID: {sessionID}");
    }

    public void OnClickCreate()
    {
        string randomID = Random.Range(100000, 999999).ToString();
        StartGame(GameMode.Host, randomID);
    }

    public void OpenJoinRoom()
    {
        joinRoomPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    public void BackFromJoin()
    {
        joinRoomPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ConfirmJoin()
    {
        string id = joinRoomInput.text.Trim();

        if (!string.IsNullOrEmpty(id))
            StartGame(GameMode.Client, id);
        else
            Debug.LogWarning("Nhập ID phòng!");
    }
}