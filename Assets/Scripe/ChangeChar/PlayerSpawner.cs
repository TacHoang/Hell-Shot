using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour, IPlayerJoined
{
    public NetworkPrefabRef[] characterPrefabs; // 4 prefab
    public Transform spawnPoint;

    private NetworkRunner runner;

    void Start()
    {
        runner = FindObjectOfType<NetworkRunner>();
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            SpawnSelectedCharacter(player);
        }
    }

    void SpawnSelectedCharacter(PlayerRef player)
    {
        int index = CharacterSelection.Instance.characterIndex;

        NetworkObject playerObj = runner.Spawn(
            characterPrefabs[index],
            spawnPoint.position,
            Quaternion.identity,
            player
        );

        runner.SetPlayerObject(player, playerObj);
    }
}