using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Currency Settings")]
    public int gold = 0;
    public int gems = 0;

    [Header("Stage and Map Settings")]
    public string[] randomStageScenesMap1;
    public string[] randomStageScenesMap2;
    public string shopScene1 = "ShopMap1";
    public string shopScene2 = "ShopMap2";
    public string bossScene1 = "BossMap1";
    public string bossScene2 = "BossMap2";
    public string lobbyScene = "Lobby";

    private int currentStage = 1;
    private int currentMap = 1;
    private List<string> stagesQueue;
    private bool gameInProgress = false;

    [Header("Respawn Settings")]
    public GameObject playerPrefab;
    private GameObject currentPlayer;

    public static GameManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Currency Methods
    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"Added {amount} gold. Current gold: {gold}");
    }

    public void AddGems(int amount)
    {
        gems += amount;
        Debug.Log($"Added {amount} gems. Current gems: {gems}");
    }

    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            Debug.Log($"Spent {amount} gold. Current gold: {gold}");
            return true;
        }
        else
        {
            Debug.LogWarning("Not enough gold!");
            return false;
        }
    }

    public bool SpendGems(int amount)
    {
        if (gems >= amount)
        {
            gems -= amount;
            Debug.Log($"Spent {amount} gems. Current gems: {gems}");
            return true;
        }
        else
        {
            Debug.LogWarning("Not enough gems!");
            return false;
        }
    }

    public void ResetGoldOnDeath()
    {
        gold = 0;
        Debug.Log("Player died. Gold reset to 0.");
    }
    #endregion

    #region Game Flow Methods
    public void StartGame(int mapNumber)
    {
        currentMap = mapNumber;
        currentStage = 1;
        stagesQueue = GenerateStageQueueForMap(currentMap);
        gameInProgress = true;

        LoadNextStage();
    }

    public void LoadNextStage()
    {
        if (currentStage > 6)
        {
            Debug.Log("All stages completed. Returning to Lobby.");
            LoadLobby();
            return;
        }

        string sceneToLoad;

        if (currentStage <= 4)
        {
            sceneToLoad = stagesQueue[currentStage - 1];
        }
        else if (currentStage == 5)
        {
            sceneToLoad = (currentMap == 1) ? shopScene1 : shopScene2;
        }
        else
        {
            sceneToLoad = (currentMap == 1) ? bossScene1 : bossScene2;
        }

        Debug.Log($"Loading Stage {currentStage}: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);

        currentStage++;
        Invoke(nameof(RespawnPlayer), 1.0f);
    }

    private List<string> GenerateStageQueueForMap(int mapNumber)
    {
        string[] stagePool = (mapNumber == 1) ? randomStageScenesMap1 : randomStageScenesMap2;
        List<string> randomizedStages = new List<string>(stagePool);

        for (int i = 0; i < randomizedStages.Count; i++)
        {
            int randomIndex = Random.Range(0, randomizedStages.Count);
            (randomizedStages[i], randomizedStages[randomIndex]) = (randomizedStages[randomIndex], randomizedStages[i]);
        }

        return randomizedStages.GetRange(0, Mathf.Min(4, randomizedStages.Count));
    }

    public void LoadLobby()
    {
        gameInProgress = false;
        SceneManager.LoadScene(lobbyScene, LoadSceneMode.Single);
        Debug.Log("Loaded Lobby Scene.");
        Invoke(nameof(RespawnPlayer), 1.0f);
    }

    public void PlayerDied()
    {
        Debug.Log("Player died. Returning to Lobby.");
        ResetGoldOnDeath();
        LoadLobby();
    }

    public void RespawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not set!");
            return;
        }

        GameObject spawnPointObject = GameObject.Find("SpawnPoints");
        if (spawnPointObject == null)
        {
            Debug.LogError("SpawnPoints not found in the scene!");
            return;
        }

        Transform spawnPoint = spawnPointObject.transform;

        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
        }

        currentPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log("Player respawned at SpawnPoints.");
    }

    public void RestartMap()
    {
        StartGame(currentMap);
    }
    #endregion

    #region Debugging
    private void OnDrawGizmosSelected()
    {
        GameObject spawnPointObject = GameObject.Find("SpawnPoints");
        if (spawnPointObject != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(spawnPointObject.transform.position, 0.5f);
        }
    }
    #endregion
}
