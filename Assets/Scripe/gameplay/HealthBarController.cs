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

    public bool isAnimating = false;

    void Awake()
    {
        myOriginalPos = myHealthGroup.anchoredPosition;
        enemyOriginalPos = enemyHealthGroup.anchoredPosition;

        myCG = myHealthGroup.GetComponent<CanvasGroup>() ?? myHealthGroup.gameObject.AddComponent<CanvasGroup>();
        enemyCG = enemyHealthGroup.GetComponent<CanvasGroup>() ?? enemyHealthGroup.gameObject.AddComponent<CanvasGroup>();

        foreach (var h in myHearts) h.gameObject.SetActive(false);
        foreach (var h in enemyHearts) h.gameObject.SetActive(false);
        
        myCG.alpha = 0;
        enemyCG.alpha = 0;
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

        for (int i = 0; i < myHearts.Count; i++)
        {
            myHearts[i].gameObject.SetActive(true);
            myHearts[i].color = Color.white;
            myHearts[i].transform.localScale = Vector3.zero;
            myHearts[i].transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

            enemyHearts[i].gameObject.SetActive(true);
            enemyHearts[i].color = Color.white;
            enemyHearts[i].transform.localScale = Vector3.zero;
            enemyHearts[i].transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(1.5f); 
        yield return StartCoroutine(HideHealthGroups());
    }

    public IEnumerator ShowHealthGroups()
    {
        isAnimating = true; 
        myHealthGroup.DOAnchorPosX(myOriginalPos.x, 0.8f).SetEase(Ease.OutBack);
        myCG.DOFade(1f, 0.8f);
        enemyHealthGroup.DOAnchorPosX(enemyOriginalPos.x, 0.8f).SetEase(Ease.OutBack);
        enemyCG.DOFade(1f, 0.8f);
        yield return new WaitForSeconds(0.8f);
    }

    public IEnumerator HideHealthGroups()
    {
        myHealthGroup.DOAnchorPosX(myOriginalPos.x - 1000f, 0.8f).SetEase(Ease.InCubic);
        myCG.DOFade(0f, 0.8f);
        enemyHealthGroup.DOAnchorPosX(enemyOriginalPos.x + 1000f, 0.8f).SetEase(Ease.InCubic);
        enemyCG.DOFade(0f, 0.8f);
        
        yield return new WaitForSeconds(0.8f);
        isAnimating = false; 
    }

    public void PlayHeartDropEffect(Image heart, bool isLeft)
    {
        if (heart == null || !heart.gameObject.activeSelf) return;

        float jumpDirection = isLeft ? 150f : -150f; 
        
        heart.rectTransform.DOJumpAnchorPos(heart.rectTransform.anchoredPosition + new Vector2(jumpDirection, -400f), 200f, 1, 1.0f);
        heart.DOFade(0, 1.0f);
        heart.transform.DOScale(0.2f, 1.0f).OnComplete(() => {
            heart.gameObject.SetActive(false);
            heart.color = Color.white;
        });
    }

    public void UpdateHealthUI(int leftHP, int rightHP)
    {
        // Duyệt qua danh sách tim và kiểm tra xem tim nào cần rơi
        for (int i = 0; i < myHearts.Count; i++)
        {
            if (i >= leftHP && myHearts[i].gameObject.activeSelf)
                PlayHeartDropEffect(myHearts[i], true);
            else if (i < leftHP && !myHearts[i].gameObject.activeSelf && !isAnimating) 
            {
                // Chỉ tự hồi tim khi không trong lúc đang animation trượt để tránh lỗi hiển thị
                myHearts[i].gameObject.SetActive(true);
                myHearts[i].transform.localScale = Vector3.zero;
                myHearts[i].color = Color.white;
                myHearts[i].transform.DOScale(1f, 0.3f);
            }
        }

        for (int i = 0; i < enemyHearts.Count; i++)
        {
            if (i >= rightHP && enemyHearts[i].gameObject.activeSelf)
                PlayHeartDropEffect(enemyHearts[i], false);
            else if (i < rightHP && !enemyHearts[i].gameObject.activeSelf && !isAnimating)
            {
                enemyHearts[i].gameObject.SetActive(true);
                enemyHearts[i].transform.localScale = Vector3.zero;
                enemyHearts[i].color = Color.white;
                enemyHearts[i].transform.DOScale(1f, 0.3f);
            }
        }
    }
}