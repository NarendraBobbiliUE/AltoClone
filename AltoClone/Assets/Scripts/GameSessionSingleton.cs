using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSessionSingleton : MonoBehaviour
{
    [SerializeField] int m_playerLives=3;

    int m_InitialLivesCount;
    int m_CoinCount;

    private static GameSessionSingleton _instance;
    // Ensure only one instance of the SceneManagerSingleton exists
    public static GameSessionSingleton Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject singleton = new GameObject("GameSession");
                _instance = singleton.AddComponent<GameSessionSingleton>();
                DontDestroyOnLoad(singleton);
            }

            return _instance;
        }
    }



    private void Awake()
    {
        // Ensure only one instance exists
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            m_InitialLivesCount = m_playerLives;
            m_CoinCount = 0;
        }
    }


    public void CoinPickedUpOperation()
    {
        m_CoinCount++;
    }

    public void PlayerDeathOperation()
    {
        m_playerLives--;

        if (m_playerLives >= 1)
        {
            // Get the index of the current scene
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            SceneManagerSingleton.Instance.LoadLevel(currentSceneIndex);
        }
        else
        {
            int firstLvlIndex = 0;
            m_playerLives = m_InitialLivesCount;
            m_CoinCount = 0;
            SceneManagerSingleton.Instance.LoadLevel(firstLvlIndex);

        }

    }
}