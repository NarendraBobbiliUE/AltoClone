using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float m_downhillForce = 30f;
    [SerializeField] protected float m_maxSpeed = 12f;
    [SerializeField] protected float m_slopeAlignSpeed = 10f;

    [Header("Ground Check")]
    [SerializeField] protected float m_groundCheckDistance = 1.2f;
    [SerializeField] protected LayerMask m_groundLayer;

    [Header("Jump")]
    [SerializeField] protected float m_jumpForce = 8f;

    [Header("Air Rotation")]
    [SerializeField] protected float m_airRotationSpeed = 360f;

    [Header("Landing Validation")]
    [Range(0f, 1f)]
    protected float m_minLandingDot = 0.85f;

    [Header("Ground Adhesion")]
    [SerializeField] protected float m_groundSnapDistance = 0.3f;

    [SerializeField] protected float m_groundedGraceTime = 0.1f;

    [SerializeField] protected float m_jumpVelocityThreshold = 1.0f;

    protected float m_lastGroundedTime;
    protected bool m_hasCrashed;
    protected float m_airRotationAccumulated;
    protected bool m_wasGroundedLastFrame;
    protected int m_completedFlips;

    protected Rigidbody m_rb;
    protected CapsuleCollider m_capsule;

    protected bool m_isGrounded;
    protected Vector3 m_groundNormal = Vector3.up;
    protected bool m_isJumpRequested;

    //  Capsule geometry
    protected float m_capsuleBottomOffset;

    void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        m_capsule = GetComponent<CapsuleCollider>();

        // Distance from center to bottom of capsule
        m_capsuleBottomOffset =
            (m_capsule.height * 0.5f) - m_capsule.radius;
    }

    void FixedUpdate()
    {
        if (m_hasCrashed)
            return;

        GroundCheck();

        if (m_isJumpRequested)
        {
            Jump();
            m_isJumpRequested = false;
        }

        if (!m_isGrounded)
        {
            HandleAirRotation();
        }
        else if (m_isGrounded && !m_wasGroundedLastFrame)
        {
            if (m_groundNormal != Vector3.zero)
            {
                OnLanded();
            }
        }

        MoveDownhill();
        AlignToSlope();
        ApplyGroundSnap();
        LockZPosition();

        m_wasGroundedLastFrame = m_isGrounded;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && m_isGrounded)
        {
            m_isJumpRequested = true;
        }
    }

    void HandleAirRotation()
    {
        if (Input.GetMouseButton(0))
        {
            float rotationThisFrame =
                m_airRotationSpeed * Time.fixedDeltaTime;

            transform.Rotate(Vector3.forward, rotationThisFrame);
            m_airRotationAccumulated += rotationThisFrame;

            if (m_airRotationAccumulated >= 360f)
            {
                m_completedFlips++;
                m_airRotationAccumulated -= 360f;
                Debug.Log("Backflip! Total: " + m_completedFlips);
            }
        }
    }

    void OnLanded()
    {
        ValidateLanding();
        m_airRotationAccumulated = 0f;
    }

    void ValidateLanding()
    {
        if (m_hasCrashed)
            return;

        if (m_groundNormal == Vector3.zero)
            return;

        float alignment =
            Vector3.Dot(transform.up.normalized, m_groundNormal.normalized);

        if (alignment < m_minLandingDot)
            Crash();
    }

    void Crash()
    {
        m_hasCrashed = true;

        m_rb.velocity = Vector3.zero;
        m_rb.angularVelocity = Vector3.zero;

        m_rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    void GroundCheck()
    {
        RaycastHit hit;
        float rayLength = m_groundCheckDistance + m_capsuleBottomOffset;

        if (Physics.Raycast(
            transform.position,
            Vector3.down,
            out hit,
            rayLength,
            m_groundLayer))
        {
            m_isGrounded = true;
            m_groundNormal = hit.normal;
            m_lastGroundedTime = Time.time;
        }
        else
        {
            m_isGrounded = Time.time - m_lastGroundedTime < m_groundedGraceTime;
        }
    }

    void MoveDownhill()
    {
        if (!m_isGrounded)
            return;

        Vector3 downhillDirection =
            Vector3.ProjectOnPlane(Vector3.right, m_groundNormal).normalized;

        m_rb.AddForce(downhillDirection * m_downhillForce, ForceMode.Acceleration);

        Vector3 velocity = m_rb.velocity;
        velocity.z = 0f;

        if (velocity.magnitude > m_maxSpeed)
            velocity = velocity.normalized * m_maxSpeed;

        m_rb.velocity = velocity;
    }

    void AlignToSlope()
    {
        if (!m_isGrounded)
            return;

        Quaternion targetRotation =
            Quaternion.FromToRotation(transform.up, m_groundNormal) * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.fixedDeltaTime * m_slopeAlignSpeed
        );
    }

    void LockZPosition()
    {
        Vector3 position = transform.position;
        position.z = 0f;
        transform.position = position;
    }

    void Jump()
    {
        Vector3 velocity = m_rb.velocity;
        velocity.y = 0f;
        m_rb.velocity = velocity;

        m_rb.AddForce(Vector3.up * m_jumpForce, ForceMode.VelocityChange);
        m_isGrounded = false;
    }

    //  FIXED GROUND SNAP
    void ApplyGroundSnap()
    {
        if (!m_isGrounded)
            return;

        RaycastHit hit;
        float rayLength = m_groundSnapDistance + m_capsuleBottomOffset;

        if (!Physics.Raycast(
            transform.position,
            Vector3.down,
            out hit,
            rayLength,
            m_groundLayer))
            return;

        float distanceToGround =
            hit.distance - m_capsuleBottomOffset;

        // Only snap if very close to ground
        if (distanceToGround > 0.05f)
            return;

        // Ignore real jumps, allow solver noise
        if (m_rb.velocity.y > m_jumpVelocityThreshold)
            return;

        Vector3 pos = transform.position;
        pos.y = hit.point.y + m_capsuleBottomOffset;
        transform.position = pos;
    }
}
