using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    /* =============================
     * MOVEMENT
     * ============================= */

    [Header("Movement")]

    [Tooltip("Constant movement speed along the X axis")]
    [SerializeField] private float m_MovementSpeed = 6f;

    [Tooltip("Max movement speed along the slope")]
    [SerializeField] private float m_MaxSpeedAlongSlope = 10.0f;

    [Tooltip("Min movement speed along the slope")]
    [SerializeField] private float m_MinSpeedAlongSlope = 2.0f;

    [Tooltip("Upward velocity applied when the player jumps")]
    [SerializeField] private float m_jumpVelocity = 12f;

    /* =============================
     * AIR TRICKS
     * ============================= */

    [Header("Air Tricks")]

    [Tooltip("Degrees per second the player rotates while backflipping airborne")]
    [SerializeField] private float m_BackFlipRotSpeed = 360f;

    [Tooltip("Degrees per second the player rotates while not backflipping airborne")]
    [SerializeField] private float m_FwFlipRotSpeed = 120f;

    [Tooltip("Minimum dot product required between player up and ground normal for a clean landing")]
    [Range(0f, 1f)]
    [SerializeField] private float m_minLandingDot = 0.85f;

    /* =============================
     * RESTART
     * ============================= */

    [Header("Restart")]

    [Tooltip("Delay before restart input is accepted after crashing")]
    [SerializeField] private float m_restartDelay = 0.5f;

    /* =============================
     * GROUND ALIGNMENT
     * ============================= */

    [Header("Ground Alignment")]

    [Tooltip("Speed at which the player visually aligns to the ground slope")]
    [SerializeField] private float m_groundAlignSpeed = 12f;

    [Tooltip("Maximum angle (degrees) the player is allowed to align to the slope")]
    [SerializeField] private float m_maxAlignAngle = 60f;

    /* =============================
     * GROUND ADHESION
     * ============================= */

    [Header("Ground Adhesion")]

    [Tooltip("Force pushing the player into the slope to prevent micro bounces")]
    [SerializeField] private float m_stickToGroundForce = 30f;

    [Tooltip("Maximum allowed upward velocity along the ground normal while grounded")]
    [SerializeField] private float m_maxSnapUpwardNormalVelocity = 0.2f;

    /* =============================
     * GROUNDED STATE STABILITY
     * ============================= */

    [Header("Grounded State Stability")]

    [Tooltip("Minimum time the player must be off the ground before becoming ungrounded (prevents jitter)")]
    [SerializeField] private float m_minTimeOffGround = 0.08f; // NEW

    /* =============================
     * INTERNAL STATE
     * ============================= */

    [Tooltip("Physics body used for movement")]
    private Rigidbody2D m_rb;

    [Tooltip("True when the player is considered grounded")]
    private bool m_isGrounded;

    [Tooltip("True once the player has crashed")]
    private bool m_hasCrashed;

    [Tooltip("Grounded state from the previous frame")]
    private bool m_wasGroundedLastFrame;

    [Tooltip("True while a jump has authority over grounding (prevents re-grounding during takeoff)")]
    private bool m_isJumping;

    [Tooltip("Surface normal of the ground currently under the player")]
    private Vector2 m_groundNormal = Vector2.up;

    [Tooltip("Accumulated rotation in the air for trick tracking")]
    private float m_airRotationAccumulated;

    [Tooltip("Time spent airborne since last jump")]
    private float m_airTime;

    [Tooltip("Time elapsed since crashing")]
    private float m_timeSinceCrash;

    [Tooltip("Time elapsed since last valid ground contact")]
    private float m_timeSinceLastGroundContact; // NEW

    /* =============================
     * UNITY LIFECYCLE
     * ============================= */

    void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();

        // Physics should never control rotation (visual-only rotation model)
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

        if (!m_isGrounded)
            m_airTime += Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (m_hasCrashed)
            return;

        // Release jump authority only after upward motion stops
        if (m_isJumping && m_rb.velocity.y <= 0f)
        {
            m_isJumping = false;
        }

        // NEW: Passive ungrounding with hysteresis
        if (!m_isJumping && m_isGrounded)
        {
            m_timeSinceLastGroundContact += Time.fixedDeltaTime;

            if (m_timeSinceLastGroundContact >= m_minTimeOffGround)
            {
                m_isGrounded = false;
            }
        }

        if (m_isGrounded)
        {
            ApplyGroundAdhesion();
            AlignToGround();

            MoveOnSlope();
        }


        if (m_isGrounded == false)
        {
            HandleAirRotation();
        }
        DetectLandingTransition();
    }

    /* =============================
     * MOVEMENT
     * ============================= */

    void MoveOnSlope()
    {
        Vector2 slopeDir = new Vector2(m_groundNormal.y, -m_groundNormal.x);

        if (slopeDir.y > 0f)
        {
            slopeDir = -slopeDir;  //Since in our game we move towards +ve X OR right side of the screen.
        }
            
        Vector2 moveForce = m_MovementSpeed * slopeDir;

        m_rb.AddRelativeForce(moveForce);
    }

    void HandleJumpInput()
    {
        if (Input.GetMouseButtonDown(0) && m_isGrounded)
        {
            m_rb.velocity = new Vector2(m_rb.velocity.x, m_jumpVelocity);

            m_isGrounded = false;
            m_isJumping = true; // Jump temporarily owns grounding
            m_airTime = 0f;
        }
    }

    /* =============================
     * AIR ROTATION
     * ============================= */

    void HandleAirRotation()
    {
        if (m_isGrounded)
            return;

        if (Input.GetMouseButton(0))
        {
            float rotationThisFrame =
                m_BackFlipRotSpeed * Time.deltaTime;

            // Visual-only rotation
            transform.Rotate(0f, 0f, rotationThisFrame);
            m_airRotationAccumulated += rotationThisFrame;
        }
        else
        {
            float rotationThisFrame =
                m_FwFlipRotSpeed * Time.deltaTime;

            // Visual-only rotation
            transform.Rotate(0f, 0f, - rotationThisFrame);
        }
    }

    /* =============================
     * LANDING
     * ============================= */

    void DetectLandingTransition()
    {
        if (m_isGrounded && !m_wasGroundedLastFrame)
        {
            ValidateLanding();
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
        m_isJumping = false;

        m_airTime = 0f;
        m_airRotationAccumulated = 0f;
        m_timeSinceCrash = 0f;
        m_timeSinceLastGroundContact = 0f; // NEW

        transform.position = startPosition;
        transform.rotation = Quaternion.identity;

        m_rb.simulated = true;
        m_rb.velocity = Vector2.zero;
    }

    /* =============================
     * COLLISIONS
     * ============================= */

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contactCount == 0)
            return;

        ContactPoint2D contact = collision.GetContact(0);

        if (contact.normal.y > 0.3f)
        {
            m_groundNormal = contact.normal;
            m_isGrounded = true;
            m_timeSinceLastGroundContact = 0f; // NEW
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // Ignore collision-based grounding during jump takeoff
        if (m_isJumping)
            return;

        if (collision.contactCount == 0)
            return;

        ContactPoint2D contact = collision.GetContact(0);

        if (contact.normal.y > 0.3f)
        {
            m_groundNormal = contact.normal;
            m_isGrounded = true;
            m_timeSinceLastGroundContact = 0f; // NEW
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // Intentionally empty:
        // Grounded state is released via time-based hysteresis
    }

    /* =============================
     * GROUND ALIGNMENT
     * ============================= */

    void AlignToGround()
    {
        float angleDelta =
            Vector2.SignedAngle(transform.up, m_groundNormal);

        angleDelta = Mathf.Clamp(
            angleDelta,
            -m_maxAlignAngle,
            m_maxAlignAngle
        );

        float targetAngle =
            transform.eulerAngles.z + angleDelta;

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0f, 0f, targetAngle),
            Time.deltaTime * m_groundAlignSpeed
        );
    }

    /* =============================
     * GROUND ADHESION
     * ============================= */

    void ApplyGroundAdhesion()
    {
        float normalVelocity =
            Vector2.Dot(m_rb.velocity, m_groundNormal);

        if (normalVelocity > -m_maxSnapUpwardNormalVelocity)
        {
            Vector2 correction =
                m_groundNormal * (normalVelocity + m_maxSnapUpwardNormalVelocity);

            m_rb.velocity -= correction;
        }

        m_rb.AddForce(
            -m_groundNormal * m_stickToGroundForce,
            ForceMode2D.Force
        );
    }
}
