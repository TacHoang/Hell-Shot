using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class HealthBarController : MonoBehaviour
{
    public RectTransform myHealthGroup;    
    public RectTransform enemyHealthGroup; 
    
    public List<Image> myHearts;    
    public List<Image> enemyHearts; 

    private Vector2 myOriginalPos;
    private Vector2 enemyOriginalPos;
    private CanvasGroup myCG;
    private CanvasGroup enemyCG;

    // Chốt chặn để không bị loop chồng tim
    public bool isAnimating = false;

    void Awake()
    {
        myOriginalPos = myHealthGroup.anchoredPosition;
        enemyOriginalPos = enemyHealthGroup.anchoredPosition;

        myCG = myHealthGroup.GetComponent<CanvasGroup>() ?? myHealthGroup.gameObject.AddComponent<CanvasGroup>();
        enemyCG = enemyHealthGroup.GetComponent<CanvasGroup>() ?? enemyHealthGroup.gameObject.AddComponent<CanvasGroup>();

        foreach (var h in myHearts) h.gameObject.SetActive(false);
        foreach (var h in enemyHearts) h.gameObject.SetActive(false);
    }

    public void StartHealthIntro()
    {
        if (isAnimating) return;
        StartCoroutine(IntroRoutine());
    }

    public IEnumerator IntroRoutine()
    {
        isAnimating = true;
        myCG.alpha = 1;
        enemyCG.alpha = 1;
        myHealthGroup.anchoredPosition = myOriginalPos;
        enemyHealthGroup.anchoredPosition = enemyOriginalPos;

        for (int i = 0; i < 5; i++)
        {
            myHearts[i].gameObject.SetActive(true);
            myHearts[i].color = Color.white;
            myHearts[i].transform.localScale = Vector3.zero;
            myHearts[i].transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);

            enemyHearts[i].gameObject.SetActive(true);
            enemyHearts[i].color = Color.white;
            enemyHearts[i].transform.localScale = Vector3.zero;
            enemyHearts[i].transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(HideHealthGroups());
        isAnimating = false;
    }

    public IEnumerator ShowHealthGroups()
    {
        isAnimating = true; 
        myHealthGroup.DOAnchorPosX(myOriginalPos.x, 0.5f).SetEase(Ease.OutBack);
        myCG.DOFade(1f, 0.5f);
        enemyHealthGroup.DOAnchorPosX(enemyOriginalPos.x, 0.5f).SetEase(Ease.OutBack);
        enemyCG.DOFade(1f, 0.5f);
        yield return new WaitForSeconds(0.5f);
    }

    public IEnumerator HideHealthGroups()
    {
        myHealthGroup.DOAnchorPosX(myOriginalPos.x - 1000f, 0.6f).SetEase(Ease.InCubic);
        myCG.DOFade(0f, 0.6f);
        enemyHealthGroup.DOAnchorPosX(enemyOriginalPos.x + 1000f, 0.6f).SetEase(Ease.InCubic);
        enemyCG.DOFade(0f, 0.6f);
        yield return new WaitForSeconds(0.6f);
        isAnimating = false;
    }

    // Hàm tạo hiệu ứng rơi tim
    public void PlayHeartDropEffect(Image heart, bool isLeft)
    {
        if (heart == null || !heart.gameObject.activeSelf) return;

        // Tính hướng bay về giữa màn hình
        float jumpDirection = isLeft ? 200f : -200f; 
        
        heart.rectTransform.DOJumpAnchorPos(heart.rectTransform.anchoredPosition + new Vector2(jumpDirection, -300f), 150f, 1, 0.7f);
        heart.DOFade(0, 0.7f);
        heart.transform.DOScale(0.2f, 0.7f).OnComplete(() => {
            heart.gameObject.SetActive(false);
            heart.color = Color.white; // Reset để lần sau hiện lại
        });
    }

    public void UpdateHealthUI(int leftHP, int rightHP)
    {
        // Nếu đang chạy animation trượt hoặc mọc tim thì không ép state ở đây
        if (isAnimating) return;

        for (int i = 0; i < myHearts.Count; i++)
        {
            if (i >= leftHP && myHearts[i].gameObject.activeSelf)
            {
                // Thay vì tắt bụp, gọi hiệu ứng rơi
                PlayHeartDropEffect(myHearts[i], true);
            }
            else if (i < leftHP && !myHearts[i].gameObject.activeSelf)
            {
                myHearts[i].gameObject.SetActive(true);
                myHearts[i].transform.localScale = Vector3.zero;
                myHearts[i].color = Color.white;
                myHearts[i].transform.DOScale(1f, 0.3f);
            }
        }

        for (int i = 0; i < enemyHearts.Count; i++)
        {
            if (i >= rightHP && enemyHearts[i].gameObject.activeSelf)
            {
                PlayHeartDropEffect(enemyHearts[i], false);
            }
            else if (i < rightHP && !enemyHearts[i].gameObject.activeSelf)
            {
                enemyHearts[i].gameObject.SetActive(true);
                enemyHearts[i].transform.localScale = Vector3.zero;
                enemyHearts[i].color = Color.white;
                enemyHearts[i].transform.DOScale(1f, 0.3f);
            }
        }
    }
}