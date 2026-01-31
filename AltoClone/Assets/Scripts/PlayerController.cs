using UnityEngine;

/// <summary>
/// Controls player movement, jumping, air tricks, and landing validation.
/// Designed for SpriteShape + Rigidbody2D terrain.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    /* =========================
     * MOVEMENT
     * ========================= */

    [Header("Movement")]

    [Tooltip("Constant forward speed along the X axis")]
    [SerializeField] private float m_forwardSpeed = 6f;

    [Tooltip("Upward velocity applied when jumping")]
    [SerializeField] private float m_jumpVelocity = 12f;

    /* =========================
     * AIR TRICKS
     * ========================= */

    [Header("Air Tricks")]

    [Tooltip("Degrees per second the player rotates while holding input in air")]
    [SerializeField] private float m_airRotationSpeed = 360f;

    [Tooltip("Minimum alignment dot product required for a safe landing")]
    [Range(0f, 1f)]
    [SerializeField] private float m_minLandingDot = 0.85f;

    /* =========================
     * STATE (READ ONLY)
     * ========================= */

    [Header("Runtime State (Read Only)")]

    [Tooltip("True when the player is currently touching the ground")]
    private bool m_isGrounded;

    [Tooltip("True once the player has crashed")]
     private bool m_hasCrashed;

    [Tooltip("Number of completed flips during the current jump")]
    private int m_completedFlips;

    [Header("Landing Detection")]

    [Tooltip("Minimum time the player must be airborne before a landing is validated")]
    [SerializeField] private float m_minAirTimeForLanding = 0.1f;

    [Header("Restart")]

    [Tooltip("Delay before restart input is accepted after crash")]
    [SerializeField] private float m_restartDelay = 0.5f;



    /* =========================
     * INTERNAL
     * ========================= */

    private Rigidbody2D m_rb;

    // Accumulated Z rotation in air (degrees)
    private float m_airRotationAccumulated;

    // Ground normal from last collision
    private Vector2 m_groundNormal = Vector2.up;

    // Track grounded state change
    private bool m_wasGroundedLastFrame;

    private float m_airTime;
    private float m_timeSinceCrash;


    /* =========================
     * UNITY LIFECYCLE
     * ========================= */

    void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();

        // We control rotation manually
        m_rb.freezeRotation = true;

    }

    public void Restart(Vector3 startPosition)
    {
        // Reset state
        m_hasCrashed = false;
        m_isGrounded = false;
        m_wasGroundedLastFrame = false;
        m_airRotationAccumulated = 0f;
        m_completedFlips = 0;
        m_airTime = 0f;
        m_timeSinceCrash = 0f;

        // Reset transform
        transform.position = startPosition;
       

        // Reset physics
        m_rb.simulated = true;
        m_rb.velocity = Vector2.zero;
        m_rb.angularVelocity = 0f;

        m_timeSinceCrash = 0f;
        m_rb.SetRotation(0f);
    }


    void Update()
    {
        if (m_hasCrashed)
        {
            m_timeSinceCrash += Time.deltaTime;

            if (m_timeSinceCrash >= m_restartDelay &&
                Input.GetMouseButtonDown(1))
            {
                FindObjectOfType<RestartManager>().RestartGame();
            }

            return;
        }

        HandleJumpInput();
        HandleAirRotation();
        if (!m_isGrounded)
        {
            m_airTime += Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (m_hasCrashed)
            return;

        MoveForward();
        DetectLandingTransition();

        
    }

    /* =========================
     * MOVEMENT
     * ========================= */

    void MoveForward()
    {
        // Preserve Y velocity (gravity), enforce constant X speed
        m_rb.velocity = new Vector2(
            m_forwardSpeed,
            m_rb.velocity.y
        );
    }

    void HandleJumpInput()
    {
        if (Input.GetMouseButtonDown(0) && m_isGrounded)
        {
            m_rb.velocity = new Vector2(
                m_rb.velocity.x,
                m_jumpVelocity
            );

            m_isGrounded = false;
            m_airTime = 0f;
        }
    }

    /* =========================
     * AIR TRICKS
     * ========================= */

    void HandleAirRotation()
    {
        if (m_isGrounded)
            return;

        if (Input.GetMouseButton(0))
        {
            float rotationThisFrame =
                m_airRotationSpeed * Time.deltaTime;

            transform.Rotate(0f, 0f, rotationThisFrame);

            m_airRotationAccumulated += rotationThisFrame;

            if (m_airRotationAccumulated >= 360f)
            {
                m_completedFlips++;
                m_airRotationAccumulated -= 360f;

                Debug.Log("Flip! Total: " + m_completedFlips);
            }
        }
    }

    /* =========================
     * LANDING / CRASH
     * ========================= */

    void DetectLandingTransition()
    {
        if (m_isGrounded && !m_wasGroundedLastFrame )
        {
            if (m_airTime >= m_minAirTimeForLanding)
            {
                ValidateLanding();
            }


            // Reset air state
            m_airTime = 0f;
            m_airRotationAccumulated = 0f;
            m_completedFlips = 0;
        }

        m_wasGroundedLastFrame = m_isGrounded;
    }

    void ValidateLanding()
    {
        Vector2 up = transform.up;
        float alignment = Vector2.Dot(up.normalized, m_groundNormal.normalized);

        if (alignment < m_minLandingDot)
        {
            Crash();
        }
        else
        {
            Debug.Log("Clean landing! Alignment: " + alignment.ToString("F2"));
        }
    }

    void Crash()
    {
        m_hasCrashed = true;

        Debug.Log("CRASH!");

        // Stop motion completely
        m_rb.velocity = Vector2.zero;
        m_rb.simulated = false;
    }

    /* =========================
     * COLLISIONS
     * ========================= */

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contactCount == 0)
            return;

        // Take the first contact as ground
        ContactPoint2D contact = collision.GetContact(0);

        m_groundNormal = contact.normal;
        m_isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        m_isGrounded = false;
    }
}
