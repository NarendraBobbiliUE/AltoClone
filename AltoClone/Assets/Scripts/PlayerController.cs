using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Constant forward speed along X axis")]
    [SerializeField] private float m_forwardSpeed = 6f;

    [Tooltip("Upward velocity applied on jump")]
    [SerializeField] private float m_jumpVelocity = 12f;

    [Header("Air Tricks")]
    [Tooltip("Degrees per second rotation while airborne")]
    [SerializeField] private float m_airRotationSpeed = 360f;

    [Tooltip("Minimum alignment required for clean landing")]
    [Range(0f, 1f)]
    [SerializeField] private float m_minLandingDot = 0.85f;

    [Header("Landing Detection")]
    [Tooltip("Minimum air time before landing is validated")]
    [SerializeField] private float m_minAirTimeForLanding = 0.1f;

    [Header("Restart")]
    [Tooltip("Delay before restart input is accepted")]
    [SerializeField] private float m_restartDelay = 0.5f;

    private Rigidbody2D m_rb;

    private bool m_isGrounded;
    private bool m_hasCrashed;
    private bool m_wasGroundedLastFrame;

    private Vector2 m_groundNormal = Vector2.up;

    private float m_airRotationAccumulated;
    private float m_airTime;
    private float m_timeSinceCrash;

    void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();
        m_rb.freezeRotation = true;
    }

    void Update()
    {
        if (m_hasCrashed)
        {
            m_timeSinceCrash += Time.deltaTime;

            if (m_timeSinceCrash >= m_restartDelay &&
                Input.GetMouseButtonDown(1))
            {
                GameMessageHandler.Broadcast(GameMessageType.RestartRequested);
            }

            return;
        }

        HandleJumpInput();
        HandleAirRotation();

        if (!m_isGrounded)
            m_airTime += Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (m_hasCrashed)
            return;

        MoveForward();
        DetectLandingTransition();
    }

    void MoveForward()
    {
        m_rb.velocity = new Vector2(m_forwardSpeed, m_rb.velocity.y);
    }

    void HandleJumpInput()
    {
        if (Input.GetMouseButtonDown(0) && m_isGrounded)
        {
            m_rb.velocity = new Vector2(m_rb.velocity.x, m_jumpVelocity);
            m_isGrounded = false;
            m_airTime = 0f;
        }
    }

    void HandleAirRotation()
    {
        if (m_isGrounded)
            return;

        if (Input.GetMouseButton(0))
        {
            float rotationThisFrame = m_airRotationSpeed * Time.deltaTime;
            transform.Rotate(0f, 0f, rotationThisFrame);
            m_airRotationAccumulated += rotationThisFrame;
        }
    }

    void DetectLandingTransition()
    {
        if (m_isGrounded && !m_wasGroundedLastFrame)
        {
            if (m_airTime >= m_minAirTimeForLanding)
            {
                ValidateLanding();
            }

            m_airTime = 0f;
            m_airRotationAccumulated = 0f;
        }

        m_wasGroundedLastFrame = m_isGrounded;
    }

    void ValidateLanding()
    {
        float alignment =
            Vector2.Dot(transform.up.normalized, m_groundNormal.normalized);

        if (alignment < m_minLandingDot)
        {
            Crash();
            Debug.Log("CRASH! Alignment: " + alignment.ToString("F2"));
        }
        else 
        { 
            Debug.Log("Clean landing! Alignment: " + alignment.ToString("F2")); 
        }
    }

    void Crash()
    {
        if (m_hasCrashed)
            return;

        m_hasCrashed = true;

        m_rb.velocity = Vector2.zero;
        m_rb.simulated = false;

        GameMessageHandler.Broadcast(GameMessageType.PlayerCrashed);
    }

    public void Restart(Vector3 startPosition)
    {
        m_hasCrashed = false;
        m_isGrounded = false;
        m_wasGroundedLastFrame = false;

        m_airTime = 0f;
        m_airRotationAccumulated = 0f;
        m_timeSinceCrash = 0f;

        transform.position = startPosition;

        m_rb.simulated = true;
        m_rb.velocity = Vector2.zero;
        m_rb.angularVelocity = 0f;
        m_rb.SetRotation(0f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contactCount == 0)
            return;

        ContactPoint2D contact = collision.GetContact(0);

        if (contact.normal.y > 0.3f)
        {
            m_groundNormal = contact.normal;
            m_isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        m_isGrounded = false;
    }
}
