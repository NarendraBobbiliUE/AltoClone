using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(SpriteShapeController))]
public class InfiniteTerrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform m_player;

    [Header("Terrain Shape")]
    [SerializeField] private float m_segmentLength = 4.0f;
    [SerializeField] private int m_initialSegments = 20;
    [SerializeField] private int m_keepSegmentsBehind = 5;

    /* =========================================================
     * SLOPE DESIGN (Alto-style)
     * ========================================================= */

    private enum SlopeMode
    {
        Downhill,
        Ramp
    }

    [Header("Downhill Slopes")]
    [SerializeField] private Vector2Int m_downhillSegmentRange = new Vector2Int(6, 10);
    [SerializeField] private Vector2 m_downhillSlopeRange = new Vector2(-16f, -10f);

    [Header("Ramps")]
    [SerializeField] private Vector2Int m_rampSegmentRange = new Vector2Int(3, 5);
    [SerializeField] private Vector2 m_rampSlopeRange = new Vector2(6f, 12f);

    [Header("Slope Smoothness")]
    [Tooltip("How fast slope recovers upward (degrees per segment)")]
    [SerializeField] private float m_uphillSlopeChangeSpeed = 4.0f;

    [Tooltip("How fast slope decays downward (degrees per segment)")]
    [SerializeField] private float m_downhillSlopeChangeSpeed = 1.2f;

    [Header("Curve Control")]
    [Tooltip("Base tangent length as fraction of segment length")]
    [SerializeField] private float m_tangentStrength = 0.5f;

    /* ========================================================= */

    private SpriteShapeController m_shape;
    private Spline m_spline;

    private float m_currentX;
    private float m_currentY;

    private SlopeMode m_currentMode;
    private int m_segmentsRemainingInMode;

    // Slope state (CRITICAL)
    private float m_currentSlopeAngle;
    private float m_targetSlopeAngle;

    void Awake()
    {
        m_shape = GetComponent<SpriteShapeController>();
        m_spline = m_shape.spline;
    }

    void Start()
    {
        InitializeSpline();
    }

    void Update()
    {
        ExtendIfNeeded();
        CleanupBehindPlayer();
    }

    /* ========================================================= */

    void InitializeSpline()
    {
        m_spline.Clear();

        m_currentX = 0f;
        m_currentY = 0f;

        m_currentSlopeAngle = 0;
        m_targetSlopeAngle = Random.Range(
                m_downhillSlopeRange.x,
                m_downhillSlopeRange.y
            );

        m_currentMode = SlopeMode.Downhill;
        m_segmentsRemainingInMode =
            Random.Range(m_downhillSegmentRange.x, m_downhillSegmentRange.y);

        AddPoint(new Vector3(m_currentX, m_currentY));

        for (int i = 0; i < m_initialSegments; i++)
            AddSegment();
    }

    void ExtendIfNeeded()
    {
        if (m_player.position.x + m_segmentLength * 15f > m_currentX)
        {
            AddSegment();
        }
    }

    void AddSegment()
    {
        if (m_segmentsRemainingInMode <= 0)
        {
            SwitchSlopeMode();
        }

        float slopeAngle = GetSmoothedSlopeAngle();

        float heightDelta =
            Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * m_segmentLength;

        m_currentX += m_segmentLength;
        m_currentY += heightDelta;

        AddPoint(new Vector3(m_currentX, m_currentY));

        m_segmentsRemainingInMode--;
    }

    void SwitchSlopeMode()
    {
        if (m_currentMode == SlopeMode.Downhill)
        {
            m_currentMode = SlopeMode.Ramp;
            m_segmentsRemainingInMode =
                Random.Range(m_rampSegmentRange.x, m_rampSegmentRange.y);

            // 🔑 Pick ramp slope ONCE
            m_targetSlopeAngle = Random.Range(
                m_rampSlopeRange.x,
                m_rampSlopeRange.y
            );
        }
        else
        {
            m_currentMode = SlopeMode.Downhill;
            m_segmentsRemainingInMode =
                Random.Range(m_downhillSegmentRange.x, m_downhillSegmentRange.y);

            // Pick downhill slope ONCE
            m_targetSlopeAngle = Random.Range(
                m_downhillSlopeRange.x,
                m_downhillSlopeRange.y
            );
        }
    }

    float GetSmoothedSlopeAngle()
    {
        float speed;

        // If target slope is higher than current, we are climbing (ramp)
        if (m_targetSlopeAngle > m_currentSlopeAngle)
        {
            speed = m_uphillSlopeChangeSpeed;
        }
        else
        {
            speed = m_downhillSlopeChangeSpeed;
        }

        m_currentSlopeAngle = Mathf.MoveTowards(
            m_currentSlopeAngle,
            m_targetSlopeAngle,
            speed
        );

        return m_currentSlopeAngle;
    }

    /* =========================================================
     * SPLINE / CURVATURE (THE IMPORTANT PART)
     * ========================================================= */

    void AddPoint(Vector3 pos)
    {
        int index = m_spline.GetPointCount();
        m_spline.InsertPointAt(index, pos);
        m_spline.SetTangentMode(index, ShapeTangentMode.Continuous);

        if (index == 0)
            return;

        // Tangent direction from slope (not chord)
        Vector3 slopeDir = new Vector3(
            Mathf.Cos(m_currentSlopeAngle * Mathf.Deg2Rad),
            Mathf.Sin(m_currentSlopeAngle * Mathf.Deg2Rad),
            0f
        ).normalized;

        float baseTangentLength = m_segmentLength * m_tangentStrength;

        // Flatten tangents near crests & valleys (low slope magnitude)
        float slopeFactor =
            Mathf.InverseLerp(0f, 12f, Mathf.Abs(m_currentSlopeAngle));

        float tangentLength =
            baseTangentLength * Mathf.Lerp(0.25f, 1f, slopeFactor);

        // Fix previous point exit tangent
        m_spline.SetRightTangent(index - 1, slopeDir * tangentLength);

        // Set current point entry tangent
        m_spline.SetLeftTangent(index, -slopeDir * tangentLength);

        // Temporary exit tangent (will be refined when next point is added)
        m_spline.SetRightTangent(index, slopeDir * tangentLength);
    }

    /* ========================================================= */

    void CleanupBehindPlayer()
    {
        while (m_spline.GetPointCount() > 2)
        {
            Vector3 p1 = m_spline.GetPosition(1);

            if (p1.x < m_player.position.x - m_keepSegmentsBehind * m_segmentLength)
            {
                m_spline.RemovePointAt(0);
            }
            else
                break;
        }
    }

    public void ResetTerrain()
    {
        InitializeSpline();
    }
}
