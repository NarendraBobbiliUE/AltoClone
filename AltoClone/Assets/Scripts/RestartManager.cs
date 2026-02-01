using UnityEngine;

public class RestartManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController m_player;
    [SerializeField] private InfiniteTerrain m_terrain;

    [Tooltip("Player start position after restart")]
    [SerializeField] private Vector3 m_playerStartPosition = Vector3.zero;

    private bool m_canRestart;

    void OnEnable()
    {
        GameMessageHandler.Subscribe(GameMessageType.PlayerCrashed, OnPlayerCrashed);
        GameMessageHandler.Subscribe(GameMessageType.RestartRequested, OnRestartRequested);
    }

    void OnDisable()
    {
        GameMessageHandler.Unsubscribe(GameMessageType.PlayerCrashed, OnPlayerCrashed);
        GameMessageHandler.Unsubscribe(GameMessageType.RestartRequested, OnRestartRequested);
    }

    void OnPlayerCrashed()
    {
        m_canRestart = true;
    }

    void OnRestartRequested()
    {
        if (!m_canRestart)
            return;

        RestartGame();
    }

    void RestartGame()
    {
        m_canRestart = false;

        m_terrain.ResetTerrain();
        m_player.Restart(m_playerStartPosition);
    }
}
