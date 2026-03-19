using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UITransitionPopUpWithBack : MonoBehaviour
{
    public RectTransform uiCurrent;  // UI hiện tại
    public RectTransform uiNext;     // UI sẽ phóng to
    public Button backButton;        // Nút back trong UI2
    public float duration = 0.5f;    // thời gian trượt UI1
    public float scaleDuration = 0.3f; // thời gian scale UI2

    private Vector3 currentStartPos;
    private Vector3 currentEndPos;
    private Vector3 nextOriginalScale;

    void Start()
    {
        // Lưu vị trí ban đầu của UI1
        currentStartPos = uiCurrent.anchoredPosition;
        currentEndPos = currentStartPos + new Vector3(-Screen.width, 0, 0); // trượt sang trái

        // Lưu scale ban đầu của UI2
        nextOriginalScale = uiNext.localScale;
        uiNext.localScale = Vector3.zero;
        uiNext.gameObject.SetActive(false);

        // Gắn nút Back
        if (backButton != null)
            backButton.onClick.AddListener(ClosePanel);
    }

    // Nhấn nút mở panel
    public void OpenPanel()
    {
        StartCoroutine(OpenTransition());
    }

    // Nhấn nút Back
    public void ClosePanel()
    {
        StartCoroutine(CloseTransition());
    }

    // Coroutine mở UI2
    IEnumerator OpenTransition()
    {
        // Trượt UI1 sang trái
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            uiCurrent.anchoredPosition = Vector3.Lerp(currentStartPos, currentEndPos, lerp);

            yield return null;
        }
        uiCurrent.gameObject.SetActive(false);

        // Hiện UI2 và scale lên
        uiNext.gameObject.SetActive(true);
        yield return StartCoroutine(ScaleUI(uiNext, Vector3.zero, nextOriginalScale, scaleDuration));
    }

    // Coroutine đóng UI2
    IEnumerator CloseTransition()
    {
        // Thu nhỏ UI2
        yield return StartCoroutine(ScaleUI(uiNext, nextOriginalScale, Vector3.zero, scaleDuration));
        uiNext.gameObject.SetActive(false);

        // Hiện lại UI1 và trượt về vị trí ban đầu
        uiCurrent.gameObject.SetActive(true);
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            uiCurrent.anchoredPosition = Vector3.Lerp(currentEndPos, currentStartPos, lerp);

            yield return null;
        }
        uiCurrent.anchoredPosition = currentStartPos;
    }

    // Coroutine scale UI
    IEnumerator ScaleUI(RectTransform target, Vector3 from, Vector3 to, float time)
    {
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            float lerp = t / time;

            target.localScale = Vector3.Lerp(from, to, lerp);
            yield return null;
        }
        target.localScale = to;
    }
}