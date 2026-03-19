using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class FusionLauncher : MonoBehaviour
{
    public NetworkRunner runnerPrefab;

    private async void Start()
    {
        var runner = FindObjectOfType<NetworkRunner>();

        // Nếu chưa có thì tạo mới
        if (runner == null)
        {
            runner = Instantiate(runnerPrefab);
        }

        runner.ProvideInput = true;

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "TestRoom",
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
        });

        Debug.Log("✅ Fusion Started!");
    }
}