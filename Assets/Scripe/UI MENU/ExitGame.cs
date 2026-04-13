using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Collections;

public class ExitGame : MonoBehaviour
{
    public NetworkRunner runner;

    public void QuitGame()
    {
        StartCoroutine(ExitRoutine());
    }

    IEnumerator ExitRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (runner != null)
        {
            runner.Shutdown(); // 🔥 QUAN TRỌNG
        }

        SceneManager.LoadScene("menu");
    }
}