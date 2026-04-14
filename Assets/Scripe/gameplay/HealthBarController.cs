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

    // 🔥 Lưu vị trí gốc của từng trái tim để hồi máu không bị lệch
    private Dictionary<Image, Vector2> heartAnchoredPositions = new Dictionary<Image, Vector2>();

    [HideInInspector] public bool isAnimating = false;

    void Awake()
    {
        myOriginalPos = myHealthGroup.anchoredPosition;
        enemyOriginalPos = enemyHealthGroup.anchoredPosition;

        myCG = myHealthGroup.GetComponent<CanvasGroup>() ?? myHealthGroup.gameObject.AddComponent<CanvasGroup>();
        enemyCG = enemyHealthGroup.GetComponent<CanvasGroup>() ?? enemyHealthGroup.gameObject.AddComponent<CanvasGroup>();

        // Lưu vị trí ban đầu của tất cả trái tim
        CacheHeartPositions(myHearts);
        CacheHeartPositions(enemyHearts);

        foreach (var h in myHearts) h.gameObject.SetActive(false);
        foreach (var h in enemyHearts) h.gameObject.SetActive(false);
        
        myCG.alpha = 0;
        enemyCG.alpha = 0;
    }

    void CacheHeartPositions(List<Image> hearts)
    {
        foreach (var h in hearts)
        {
            if (!heartAnchoredPositions.ContainsKey(h))
                heartAnchoredPositions.Add(h, h.rectTransform.anchoredPosition);
        }
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
            ResetHeartVisual(myHearts[i]);
            ResetHeartVisual(enemyHearts[i]);
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(1.0f); 
        yield return StartCoroutine(HideHealthGroups());
        
        // Đảm bảo sau Intro là phải nhả lock
        isAnimating = false; 
    }

    // 🔥 Hàm quan trọng: Đưa tim về vị trí cũ và reset trạng thái
    private void ResetHeartVisual(Image heart)
    {
        heart.gameObject.SetActive(true);
        heart.transform.DOKill();
        heart.rectTransform.DOKill();
        
        // Đưa về đúng tọa độ trên thanh máu đã lưu trong Dictionary
        heart.rectTransform.anchoredPosition = heartAnchoredPositions[heart];
        
        heart.color = Color.white;
        heart.transform.localScale = Vector3.zero;
        heart.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
    }

    public IEnumerator ShowHealthGroups()
    {
        isAnimating = true; 
        myHealthGroup.DOKill();
        enemyHealthGroup.DOKill();
        
        myHealthGroup.DOAnchorPosX(myOriginalPos.x, 0.6f).SetEase(Ease.OutBack);
        myCG.DOFade(1f, 0.6f);
        enemyHealthGroup.DOAnchorPosX(enemyOriginalPos.x, 0.6f).SetEase(Ease.OutBack);
        enemyCG.DOFade(1f, 0.6f);
        
        yield return new WaitForSeconds(0.6f);
    }

    public IEnumerator HideHealthGroups()
    {
        myHealthGroup.DOKill();
        enemyHealthGroup.DOKill();

        myHealthGroup.DOAnchorPosX(myOriginalPos.x - 800f, 0.6f).SetEase(Ease.InSine);
        myCG.DOFade(0f, 0.5f);
        enemyHealthGroup.DOAnchorPosX(enemyOriginalPos.x + 800f, 0.6f).SetEase(Ease.InSine);
        
        enemyCG.DOFade(0f, 0.5f).OnComplete(() => {
            isAnimating = false;
            
            // --- THÊM DÒNG NÀY VÀO ---
            // Tìm ItemsManager hoặc GunManager để ép nó mở khóa UI
        });
        
        yield return new WaitForSeconds(0.6f);
    }

    public void UpdateHealthUI(int p1HP, int p2HP)
    {
        // 1. Xác định mình là ai (Host = 0, Client = 1)
        // p1HP: Máu của Host, p2HP: Máu của Client
        int localPlayerIndex = (FindFirstObjectByType<Fusion.NetworkRunner>().IsServer) ? 0 : 1;

        if (localPlayerIndex == 0)
        {
            // --- NẾU LÀ HOST (P1) ---
            // Thanh trái (myHearts) hiện máu mình (p1HP)
            // Thanh phải (enemyHearts) hiện máu đối thủ (p2HP)
            UpdateHeartList(myHearts, p1HP, true);
            UpdateHeartList(enemyHearts, p2HP, false);
        }
        else
        {
            // --- NẾU LÀ CLIENT (P2) ---
            // Thanh trái (myHearts) hiện máu mình (p2HP)
            // Thanh phải (enemyHearts) hiện máu đối thủ (p1HP)
            UpdateHeartList(myHearts, p2HP, true); 
            UpdateHeartList(enemyHearts, p1HP, false);
        }
    }

    private void UpdateHeartList(List<Image> hearts, int currentHP, bool isLeft)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            if (i >= currentHP && hearts[i].gameObject.activeSelf)
            {
                PlayHeartDropEffect(hearts[i], isLeft);
            }
            else if (i < currentHP && !hearts[i].gameObject.activeSelf)
            {
                ResetHeartVisual(hearts[i]);
            }
        }
    }

    public void PlayHeartDropEffect(Image heart, bool isLeft)
    {
        if (heart == null) return;
        heart.transform.DOKill();
        heart.rectTransform.DOKill();

        float jumpDirection = isLeft ? 120f : -120f; 
        
        heart.rectTransform.DOJumpAnchorPos(heart.rectTransform.anchoredPosition + new Vector2(jumpDirection, -500f), 150f, 1, 0.8f);
        heart.DOFade(0, 0.8f);
        heart.transform.DOScale(0.3f, 0.8f).OnComplete(() => {
            heart.gameObject.SetActive(false);
        });
    }
}