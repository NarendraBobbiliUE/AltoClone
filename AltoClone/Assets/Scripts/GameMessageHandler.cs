using System;
using System.Collections.Generic;

/// <summary>
/// Central enum-based message broadcaster / subscriber.
/// Classic, readable, and decoupled.
/// </summary>
public static class GameMessageHandler
{
    private static readonly Dictionary<GameMessageType, Action> m_listeners =
        new Dictionary<GameMessageType, Action>();

    public static void Subscribe(GameMessageType message, Action callback)
    {
        if (m_listeners.ContainsKey(message))
        {
            m_listeners[message] += callback;
        }
        else
        {
            m_listeners[message] = callback;
        }
    }

    public static void Unsubscribe(GameMessageType message, Action callback)
    {
        if (!m_listeners.ContainsKey(message))
            return;

        m_listeners[message] -= callback;

        if (m_listeners[message] == null)
        {
            m_listeners.Remove(message);
        }
    }

    public static void Broadcast(GameMessageType message)
    {
        if (m_listeners.TryGetValue(message, out var callback))
        {
            callback?.Invoke();
        }
    }
}
