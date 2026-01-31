using UnityEngine;

/// <summary>
/// Handles restarting the game state (player + terrain).
/// </summary>
public class RestartManager : MonoBehaviour
{
    [Header("References")]

    [Tooltip("Reference to the player controller")]
    [SerializeField] private PlayerController m_player;

    [Tooltip("Reference to the infinite terrain controller")]
    [SerializeField] private InfiniteTerrain m_terrain;

    [Tooltip("Player start position after restart")]
    [SerializeField] private Vector3 m_playerStartPosition = Vector3.zero;

    public void RestartGame()
    {
        // Reset terrain first
        m_terrain.ResetTerrain();

        // Reset player
        m_player.Restart(m_playerStartPosition);
    }
}
