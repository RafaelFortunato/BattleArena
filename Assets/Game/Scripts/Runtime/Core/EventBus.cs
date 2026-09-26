using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleArena.Core
{
    /// <summary>Marker for payloads that travel through <see cref="EventBus{T}"/>.</summary>
    public interface IGameEvent { }

    /// <summary>
    /// Type-safe publish/subscribe channel. Events are structs, so raising them does not allocate.
    /// Subscribers are expected to unsubscribe in OnDisable.
    /// </summary>
    public static class EventBus<T> where T : struct, IGameEvent
    {
        private static Action<T> _handlers;

        static EventBus() => EventBusRegistry.Register(() => _handlers = null);

        public static void Subscribe(Action<T> handler) => _handlers += handler;

        public static void Unsubscribe(Action<T> handler) => _handlers -= handler;

        public static void Raise(T evt) => _handlers?.Invoke(evt);
    }

    /// <summary>Clears every bus when entering play mode, so it works with domain reload disabled.</summary>
    internal static class EventBusRegistry
    {
        private static readonly List<Action> ClearActions = new();

        public static void Register(Action clear) => ClearActions.Add(clear);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearAll()
        {
            foreach (var clear in ClearActions)
                clear();
        }
    }
}
