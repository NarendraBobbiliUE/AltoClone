using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerSingleton : MonoBehaviour
{
    [SerializeField] float m_waitTime = 2.0f;
    private static SceneManagerSingleton _instance;

    // Ensure only one instance of the SceneManagerSingleton exists
    public static SceneManagerSingleton Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject singleton = new GameObject("SceneManagerSingleton");
                _instance = singleton.AddComponent<SceneManagerSingleton>();
                DontDestroyOnLoad(singleton);
            }

            return _instance;
        }
    }

    // Load a level by its name
    public void LoadLevel(string levelName)
    {
        if (SceneManager.GetSceneByName(levelName) != null)
        {
            int levelIndex = SceneManager.GetSceneByName(levelName).buildIndex;
            StartCoroutine(LoadLevelAfterDelay(levelIndex));
        }
    }

    public void LoadLevel(int levelIndex)
    {
        StartCoroutine(LoadLevelAfterDelay(levelIndex));
    }


    // Example method: Load the next level
    public void LoadNextLevel()
    {
        // Get the index of the current scene
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Load the next scene (wrap around to the first scene if it's the last one)
        int nextSceneIndex = (currentSceneIndex + 1) % SceneManager.sceneCountInBuildSettings;
        StartCoroutine(LoadLevelAfterDelay(nextSceneIndex));
    }


    IEnumerator LoadLevelAfterDelay(int nextSceneIdx)
    {
        yield return new WaitForSeconds(m_waitTime);
        SceneManager.LoadScene(nextSceneIdx);
    }
    // Additional methods can be added based on your game's needs

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
        }
    }
}