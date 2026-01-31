using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(SpriteShapeController))]
public class InfiniteTerrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform m_player;

    [Header("Terrain Shape")]
    [SerializeField] private float m_segmentLength = 5f;
    [SerializeField] private int m_initialSegments = 20;
    [SerializeField] private int m_keepSegmentsBehind = 5;

    [Header("Slope Settings")]
    [SerializeField] private float m_minSlope = -15f;
    [SerializeField] private float m_maxSlope = 10f;
    [SerializeField] private float m_maxSlopeDelta = 4f;

    private SpriteShapeController m_shape;
    private Spline m_spline;

    private float m_currentX;
    private float m_currentY;
    private float m_currentSlope;

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

    // ------------------------

    void InitializeSpline()
    {
        m_spline.Clear();

        m_currentX = 0f;
        m_currentY = 0f;
        m_currentSlope = 0f;

        // Start point
        AddPoint(new Vector3(m_currentX, m_currentY));

        for (int i = 0; i < m_initialSegments; i++)
            AddSegment();
    }

    void ExtendIfNeeded()
    {
        if (m_player.position.x + m_segmentLength * 5f > m_currentX)
        {
            AddSegment();
        }
    }

    void AddSegment()
    {
        float slopeDelta = Random.Range(-m_maxSlopeDelta, m_maxSlopeDelta);
        m_currentSlope = Mathf.Clamp(
            m_currentSlope + slopeDelta,
            m_minSlope,
            m_maxSlope
        );

        float heightDelta =
            Mathf.Tan(m_currentSlope * Mathf.Deg2Rad) * m_segmentLength;

        m_currentX += m_segmentLength;
        m_currentY += heightDelta;

        AddPoint(new Vector3(m_currentX, m_currentY));
    }

    void AddPoint(Vector3 pos)
    {
        int index = m_spline.GetPointCount();
        m_spline.InsertPointAt(index, pos);

        // Smooth tangents
        m_spline.SetTangentMode(index, ShapeTangentMode.Continuous);
    }

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
