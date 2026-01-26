using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExitScript : MonoBehaviour
{

    Collider2D m_collider2D;


    // Start is called before the first frame update
    private void Start()
    {
        m_collider2D = GetComponent<Collider2D>();
    }
    // Update is called once per frame
    void Update()
    {


    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject collidingObj = collision.gameObject;
        if(collidingObj.tag == "Player")
        {
            SceneManagerSingleton.Instance.LoadNextLevel();
            m_collider2D.enabled = false;
        }
    }
}
