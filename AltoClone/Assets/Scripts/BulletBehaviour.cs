using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    [SerializeField]float m_bulletSpeed = 5.0f;
    PlayerMovement m_playerMovement;

    // Start is called before the first frame update
    float xSpeed;
    Rigidbody2D m_rigidbody;
    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody2D>();
        m_playerMovement = FindAnyObjectByType<PlayerMovement>();
        xSpeed =  m_playerMovement.transform.localScale.x * m_bulletSpeed;
        transform.localScale = new Vector2(Mathf.Sign(xSpeed), 1f);
    }   

    // Update is called once per frame
    void Update()
    {
        m_rigidbody.velocity = new Vector2(xSpeed, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.tag =="Enemy")
        {
            Destroy(other.gameObject);
        }
            Destroy(gameObject);
       
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag != "Player")
        {
            Destroy(gameObject);
        }
    }
}
