using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
   
    [SerializeField]float m_runSpeed = 5f;
    [SerializeField]float m_climbSpeed = 5f;
    [SerializeField] float m_jumpSpeed = 5f;
    [SerializeField] Vector2 m_deathForce = new Vector2(20,20);
    [SerializeField] GameObject m_bulletPrefabRef;

    Vector2 m_moveInput;
    Rigidbody2D m_rigidbody2D;
    CapsuleCollider2D m_bodyCollider;
    BoxCollider2D m_feetCollider;
    float m_gravityScaleAtStart;
    Animator m_animator;
    Transform m_gunTx;
    bool m_isAlive = true;

    Vector2 m_playerInitialPos;
    Quaternion m_playerInitialRotation;


    // Start is called before the first frame update
    void Start()
    {
        m_rigidbody2D = GetComponent<Rigidbody2D>();
        m_animator = GetComponent<Animator>();
        m_bodyCollider = GetComponent<CapsuleCollider2D>();
        m_feetCollider = GetComponent<BoxCollider2D>();
        m_gravityScaleAtStart = m_rigidbody2D.gravityScale;
        m_gunTx = transform.Find("Gun");

        //Shortcut to Reset initial transform. It's a testing feature.
        m_playerInitialPos = transform.position;
        m_playerInitialRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_isAlive)
        {
            RunUpdate();
            FlipSpriteUpdate();
            ClimbUpdate();
            PlayerDiesUpdate();
        }
    }

    void OnMove(InputValue value)
    {
        m_moveInput = value.Get<Vector2>();
        Debug.Log(m_moveInput);
    }

   

    void RunUpdate() 
    {
        Vector2 playerVelocity = new Vector2(m_runSpeed * m_moveInput.x, m_rigidbody2D.velocity.y);
        m_rigidbody2D.velocity = playerVelocity;

        m_animator.SetBool("isRunning", IsPlayerMovingHorizontally());
        
    }

    void ClimbUpdate()
    {

       
        if (CanClimb())
        {
            Vector2 climbVelocity = new Vector2(m_rigidbody2D.velocity.x, m_moveInput.y * m_climbSpeed);
            m_rigidbody2D.velocity = climbVelocity;
            m_rigidbody2D.gravityScale = 0f;
            m_animator.SetBool("isClimbing", IsPlayerMovingVertically());
            return;
        }


        m_animator.SetBool("isClimbing", false);
        m_rigidbody2D.gravityScale = m_gravityScaleAtStart;


    }

    void FlipSpriteUpdate()
    {
        if (IsPlayerMovingHorizontally())
        {
            transform.localScale = new Vector2(Mathf.Sign(m_rigidbody2D.velocity.x), 1f);
        }
    }

    void PlayerDiesUpdate()
    {
        if (m_isAlive)
        {
            int layerMask = LayerMask.GetMask("Enemies", "Hazards");
            if (m_bodyCollider.IsTouchingLayers(layerMask))
            {
                m_isAlive = false;
                m_animator.SetTrigger("Dying");
                m_rigidbody2D.velocity = m_deathForce;
                GameSessionSingleton.Instance.PlayerDeathOperation();
            }
        }
    }

    void OnJump(InputValue value)
    {
        if (CanJump() && m_isAlive)
        {
            if (value.isPressed)
            {    
                m_rigidbody2D.velocity += new Vector2(0f, m_jumpSpeed);
            }
        }
    }

    void OnFire(InputValue value)
    {
        if(m_bulletPrefabRef)
        {
             GameObject instantiatedBullet = Instantiate(m_bulletPrefabRef, m_gunTx.position, m_gunTx.rotation);
        }
    }

    bool IsPlayerMovingHorizontally()
    {
        bool playerHasHorizontalSpeed = Mathf.Abs(m_rigidbody2D.velocity.x) > Mathf.Epsilon;
        return playerHasHorizontalSpeed;
    }

    bool IsPlayerMovingVertically()
    {
        bool playerHasVerticalSpeed = Mathf.Abs(m_rigidbody2D.velocity.y) > Mathf.Epsilon;
        return playerHasVerticalSpeed;
    }


    bool CanJump()
    {
        int layerMask = LayerMask.GetMask("Ground", "Climbing","Bouncing");
        return m_feetCollider.IsTouchingLayers(layerMask);
    }

    bool CanClimb()
    {
        int layerMask = LayerMask.GetMask("Climbing");
        return m_feetCollider.IsTouchingLayers(layerMask);
    }

    void OnRespawn(InputValue value)
    {
      ResetPlayer();
    }

    void ResetPlayer()
    {
        m_isAlive = true;
        transform.SetPositionAndRotation(m_playerInitialPos,m_playerInitialRotation);
        m_animator.Rebind();
        m_animator.Update(0f);
    }
}
