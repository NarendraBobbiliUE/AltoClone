using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    /* =============================
     * MOVEMENT
     * ============================= */



    [Tooltip("Max movement speed along the slope")]
    [SerializeField] private float m_MaxSpeedAlongSlope = 40f;

    [Tooltip("Min movement speed along the slope")]
    [SerializeField] private float m_MinSpeedAlongSlope = 8f;

    [SerializeField]
    private float m_SlopeSpeedAcceleration = 8f;


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
    [SerializeField] private float m_minTimeOffGround = 0.08f;

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
    private float m_timeSinceLastGroundContact;


    [Tooltip("The currentSlopeSpeedOfPlayer")]
    private float m_currentSlopeSpeed;

    /* =============================
     * UNITY LIFECYCLE
     * ============================= */

    void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();

        // Physics should never control rotation (visual-only rotation model)
        m_rb.freezeRotation = true;
        m_currentSlopeSpeed = 0;
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

        // Passive ungrounding with hysteresis
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

            MoveOnSlope();
            ApplyGroundAdhesion();
          
            AlignToGround();
        }
        else
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
        // Direction along the slope (perpendicular to ground normal)
        Vector2 slopeDir = new Vector2(m_groundNormal.y, -m_groundNormal.x);

        // Ensure movement is always towards +X (right side of screen)
        Vector2 moveDirection = slopeDir;
        if (slopeDir.x < 0f)
            moveDirection = -slopeDir;

        slopeDir.Normalize();
        moveDirection.Normalize();

        Vector2 velocity = m_rb.velocity;


      
        // Extract speed along the slope
        float currSpeedAlongSlope = Vector2.Dot(velocity, moveDirection);


        // Project gravity onto slope direction
        // This gives signed downhill / uphill factor
        float signedSlopeFactor =
        Vector2.Dot(Vector2.down, slopeDir);

        // Apply baseline movement
        float DesiredSpeedAlongSlope = currSpeedAlongSlope + m_SlopeSpeedAcceleration * signedSlopeFactor * Time.fixedDeltaTime;

        // Enforce min & max speed
        DesiredSpeedAlongSlope = Mathf.Clamp(
            DesiredSpeedAlongSlope,
            m_MinSpeedAlongSlope,
            m_MaxSpeedAlongSlope
        );

        //// Preserve perpendicular velocity (jump / bumps)
        // Vector2 perpendicularVelocity =
        //     velocity - slopeDir * Vector2.Dot(velocity, slopeDir);

        // Rebuild final velocity
        m_rb.velocity = moveDirection * DesiredSpeedAlongSlope;
    }


    void HandleJumpInput()
    {
        if (Input.GetMouseButtonDown(0) && m_isGrounded)
        {
            m_rb.velocity = new Vector2(m_rb.velocity.x, m_jumpVelocity);

            m_isGrounded = false;
            m_isJumping = true;
            m_airTime = 0f;
        }
    }

    /* =============================
     * AIR ROTATION
     * ============================= */

    void HandleAirRotation()
    {
        if (Input.GetMouseButton(0))
        {
            float rotationThisFrame =
                m_BackFlipRotSpeed * Time.deltaTime;

            transform.Rotate(0f, 0f, rotationThisFrame);
            m_airRotationAccumulated += rotationThisFrame;
        }
        else
        {
            float rotationThisFrame =
                m_FwFlipRotSpeed * Time.deltaTime;

            transform.Rotate(0f, 0f, -rotationThisFrame);
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
        m_timeSinceLastGroundContact = 0f;

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
            m_timeSinceLastGroundContact = 0f;
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (m_isJumping)
            return;

        if (collision.contactCount == 0)
            return;

        ContactPoint2D contact = collision.GetContact(0);

        if (contact.normal.y > 0.3f)
        {
            m_groundNormal = contact.normal;
            m_isGrounded = true;
            m_timeSinceLastGroundContact = 0f;
        }
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
