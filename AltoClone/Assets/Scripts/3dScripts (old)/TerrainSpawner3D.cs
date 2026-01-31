using UnityEngine;
using System.Collections.Generic;

public class TerrainSpawner3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    protected GameObject m_segmentPrefab;

    [SerializeField]
    protected Transform m_player;

    [Header("Macro Chunk Settings")]
    [SerializeField]
    protected float m_macroChunkLength = 20f;

    [SerializeField]
    protected int m_initialMacroChunks = 5;

    [Header("Slope Limits")]
    [SerializeField]
    protected float m_minSlopeAngle = -15f;

    [SerializeField]
    protected float m_maxSlopeAngle = 5f;

    [Header("Slope Trend")]
    [SerializeField]
    protected float m_maxSlopeDelta = 4f;

    [Header("Curved Terrain")]
    [SerializeField]
    protected int m_segmentsPerMacroChunk = 4;

    [SerializeField]
    protected float m_maxSegmentAngleDelta = 1.2f;

    [Header("Collider Fix")]
    [SerializeField]
    protected float m_segmentOverlap = 0.05f;

    // Runtime state
    protected Vector3 m_nextSpawnPosition = Vector3.zero;
    protected float m_currentSlopeAngle = 0f;
    protected float m_targetSlopeAngle = 0f;

    protected readonly List<GameObject> m_spawnedSegments =
        new List<GameObject>();

    protected virtual void Start()
    {
        for (int i = 0; i < m_initialMacroChunks; i++)
        {
            SpawnMacroChunk();
        }
    }

    protected virtual void Update()
    {
        if (m_player == null)
            return;

        if (m_player.position.x >
            m_nextSpawnPosition.x - (m_macroChunkLength * 3f))
        {
            SpawnMacroChunk();
            CleanupSegments();
        }
    }

    protected virtual void SpawnMacroChunk()
    {
        // Decide long-term slope trend
        float trendDelta = Random.Range(
            -m_maxSlopeDelta,
             m_maxSlopeDelta
        );

        m_targetSlopeAngle = Mathf.Clamp(
            m_targetSlopeAngle + trendDelta,
            m_minSlopeAngle,
            m_maxSlopeAngle
        );

        float segmentLength =
            m_macroChunkLength / m_segmentsPerMacroChunk;

        for (int i = 0; i < m_segmentsPerMacroChunk; i++)
        {
            float delta = Mathf.Clamp(
                m_targetSlopeAngle - m_currentSlopeAngle,
                -m_maxSegmentAngleDelta,
                 m_maxSegmentAngleDelta
            );

            m_currentSlopeAngle += delta;

            Quaternion rotation =
                Quaternion.Euler(0f, 0f, m_currentSlopeAngle);

            Vector3 direction =
                rotation * Vector3.right;

            Vector3 spawnPosition =
                m_nextSpawnPosition +
                direction * (segmentLength * 0.5f);

            GameObject segment = Instantiate(
                m_segmentPrefab,
                spawnPosition,
                rotation
            );

            // Scale segment to correct length
            Vector3 scale = segment.transform.localScale;
            scale.x = segmentLength + m_segmentOverlap;
            segment.transform.localScale = scale;

            m_spawnedSegments.Add(segment);

            m_nextSpawnPosition += direction * segmentLength;
        }
    }

    protected virtual void CleanupSegments()
    {
        if (m_spawnedSegments.Count <=
            m_initialMacroChunks * m_segmentsPerMacroChunk)
            return;

        GameObject oldest = m_spawnedSegments[0];

        if (m_player.position.x - oldest.transform.position.x >
            m_macroChunkLength * 2f)
        {
            m_spawnedSegments.RemoveAt(0);
            Destroy(oldest);
        }
    }
}
