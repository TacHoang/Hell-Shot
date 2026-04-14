using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Collections;

public class ExitGame : MonoBehaviour
{
    public void QuitGame()
    {
        StartCoroutine(ExitRoutine());
    }

    IEnumerator ExitRoutine()
    {
        // 🔥 Tìm runner hiện tại
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();

        if (runner != null)
        {
            // 🔥 Shutdown đúng cách (QUAN TRỌNG NHẤT)
            yield return runner.Shutdown();

            // 🔥 XÓA LUÔN runner khỏi scene (reset sạch)
            Destroy(runner.gameObject);
        }

        // 🔥 Đợi 1 frame cho chắc
        yield return null;

        // 🔥 Load lại scene menu (trạng thái như mới mở game)
        SceneManager.LoadScene("menu");
    }
}