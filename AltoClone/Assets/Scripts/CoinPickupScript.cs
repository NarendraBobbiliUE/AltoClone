using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinPickupScript : MonoBehaviour
{

    [SerializeField] AudioClip m_coinPickupSFX;


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
        if (collidingObj.tag == "Player")
        {
            AudioSource.PlayClipAtPoint(m_coinPickupSFX, Camera.main.transform.position);
            GameSessionSingleton.Instance.CoinPickedUpOperation();
            Destroy(gameObject);
        }
    }
}
