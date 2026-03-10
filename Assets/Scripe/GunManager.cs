using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunManager : MonoBehaviour
{
    public List<bool> bullets = new List<bool>();

    public bool doubleDamage = false;
    public bool enemySkipTurn = false;

    public ItemManager itemManager;

    public int maxPlayerHP = 5;
    public int maxEnemyHP = 5;

    public int playerHP;
    public int enemyHP;

    public bool playerTurn = true;

    public int currentRound = 1;

    public GameObject shotCanvas;

    Coroutine botRoutine;

    bool isChangingRound = false;

    void Start()
    {
        playerHP = maxPlayerHP;
        enemyHP = maxEnemyHP;

        GenerateBullets();

        itemManager.GiveItemsByRound(currentRound);

        if (playerTurn)
            shotCanvas.SetActive(true);
        else
            botRoutine = StartCoroutine(BotTurn());
    }

    void GenerateBullets()
    {
        bullets.Clear();

        int rand;

        if (currentRound == 1)
        {
            AddBullets(1, 1);
        }
        else if (currentRound == 2)
        {
            rand = Random.Range(0, 3);

            if (rand == 0) AddBullets(2, 2);
            if (rand == 1) AddBullets(1, 3);
            if (rand == 2) AddBullets(3, 1);
        }
        else if (currentRound == 3)
        {
            rand = Random.Range(0, 2);

            if (rand == 0) AddBullets(2, 3);
            else AddBullets(3, 2);
        }
        else if (currentRound == 4)
        {
            rand = Random.Range(0, 3);

            if (rand == 0) AddBullets(3, 3);
            if (rand == 1) AddBullets(2, 4);
            if (rand == 2) AddBullets(4, 2);
        }
        else if (currentRound == 5)
        {
            rand = Random.Range(0, 4);

            if (rand == 0) AddBullets(4, 3);
            if (rand == 1) AddBullets(3, 4);
            if (rand == 2) AddBullets(2, 5);
            if (rand == 3) AddBullets(5, 2);
        }
        else
        {
            rand = Random.Range(0, 5);

            if (rand == 0) AddBullets(4, 4);
            if (rand == 1) AddBullets(3, 5);
            if (rand == 2) AddBullets(5, 3);
            if (rand == 3) AddBullets(2, 6);
            if (rand == 4) AddBullets(6, 2);
        }

        ShuffleBullets();
    }

    void AddBullets(int real, int blank)
    {
        for (int i = 0; i < real; i++)
            bullets.Add(true);

        for (int i = 0; i < blank; i++)
            bullets.Add(false);
    }

    void ShuffleBullets()
    {
        for (int i = 0; i < bullets.Count; i++)
        {
            bool temp = bullets[i];
            int randomIndex = Random.Range(i, bullets.Count);

            bullets[i] = bullets[randomIndex];
            bullets[randomIndex] = temp;
        }
    }

    public void ShootSelf()
    {
        if (!playerTurn) return;

        shotCanvas.SetActive(false);
        Shoot(true);
    }

    public void ShootEnemy()
    {
        if (!playerTurn) return;

        shotCanvas.SetActive(false);
        Shoot(false);
    }

    void Shoot(bool shootSelf)
    {
        if (isChangingRound) return;

        if (bullets.Count == 0)
        {
            StartCoroutine(StartNextRound());
            return;
        }

        bool bullet = bullets[0];
        bullets.RemoveAt(0);

        int damage = doubleDamage ? 2 : 1;
        doubleDamage = false;

        if (bullet)
        {
            Debug.Log("Đạn thật");

            if (shootSelf)
            {
                if (playerTurn) playerHP -= damage;
                else enemyHP -= damage;
            }
            else
            {
                if (playerTurn) enemyHP -= damage;
                else playerHP -= damage;
            }

            CheckGameOver();
            ChangeTurn();
        }
        else
        {
            Debug.Log("Đạn rỗng");

            if (shootSelf)
            {
                if (playerTurn)
                {
                    shotCanvas.SetActive(true);
                }
                else
                {
                    botRoutine = StartCoroutine(BotTurn());
                }
            }
            else
            {
                ChangeTurn();
            }
        }

        if (bullets.Count == 0 && !isChangingRound)
        {
            StartCoroutine(StartNextRound());
        }
    }

    void ChangeTurn()
    {
        if (enemySkipTurn)
        {
            enemySkipTurn = false;
            Debug.Log("Enemy bị còng → mất lượt");
            shotCanvas.SetActive(true);
            return;
        }

        playerTurn = !playerTurn;

        if (botRoutine != null)
        {
            StopCoroutine(botRoutine);
            botRoutine = null;
        }

        if (playerTurn)
        {
            shotCanvas.SetActive(true);
        }
        else
        {
            botRoutine = StartCoroutine(BotTurn());
        }
    }

    IEnumerator BotTurn()
    {
        yield return new WaitForSeconds(2f);

        if (playerTurn || isChangingRound) yield break;

        // dùng item trước
        yield return StartCoroutine(itemManager.BotSmartUse());

        yield return new WaitForSeconds(1f);

        if (bullets.Count == 0) yield break;

        bool bullet = bullets[0];

        if (bullet)
        {
            Debug.Log("Bot biết đạn thật → bắn player");
            Shoot(false);
        }
        else
        {
            Debug.Log("Bot biết đạn rỗng → bắn mình");
            Shoot(true);
        }
    }

    IEnumerator StartNextRound()
    {
        if (isChangingRound) yield break;

        isChangingRound = true;

        shotCanvas.SetActive(false);

        yield return new WaitForSeconds(2f);

        NextRound();

        isChangingRound = false;
    }

    void NextRound()
    {
        currentRound++;

        if (currentRound > 100)
        {
            Debug.Log("Game Over - hết 100 round");
            return;
        }

        if (botRoutine != null)
        {
            StopCoroutine(botRoutine);
            botRoutine = null;
        }

        Debug.Log("Round " + currentRound);

        itemManager.GiveItemsByRound(currentRound);

        GenerateBullets();

        if (playerTurn)
            shotCanvas.SetActive(true);
        else
            botRoutine = StartCoroutine(BotTurn());
    }

    void CheckGameOver()
    {
        if (playerHP <= 0)
        {
            Debug.Log("BOT WIN");
            Time.timeScale = 0;
        }

        if (enemyHP <= 0)
        {
            Debug.Log("PLAYER WIN");
            Time.timeScale = 0;
        }
    }
}