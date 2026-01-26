using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovementScript : MonoBehaviour
{
    
    [SerializeField] float m_movementSpeed = 5f;
    // Start is called before the first frame update


    Rigidbody2D m_rigidBody;
    void Start()
    {
        m_rigidBody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        m_rigidBody.velocity = new Vector2(m_movementSpeed, 0);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        LayerMask layerMask = LayerMask.GetMask("Player", "Collectible");
        if ((layerMask.value & (1 << collision.gameObject.layer)) != 0)
        {
            return;
        }
            m_movementSpeed = -m_movementSpeed;
            FlipEnemyFacing();
    }

    void FlipEnemyFacing()
    {
        transform.localScale = new Vector2(-Mathf.Sign(m_rigidBody.velocity.x), 1f);
    }
}
