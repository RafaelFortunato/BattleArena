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

        /// <summary>Adds a handler for events of type <typeparamref name="T"/>.</summary>
        public static void Subscribe(Action<T> handler) => _handlers += handler;

        /// <summary>Removes a handler added with <see cref="Subscribe"/>.</summary>
        public static void Unsubscribe(Action<T> handler) => _handlers -= handler;

        /// <summary>Delivers <paramref name="evt"/> to every current handler.</summary>
        public static void Raise(T evt) => _handlers?.Invoke(evt);
    }

    /// <summary>Clears every bus when entering play mode, so it works with domain reload disabled.</summary>
    internal static class EventBusRegistry
    {
        private static readonly List<Action> ClearActions = new();

        /// <summary>Records how to clear one event channel; called once per channel type.</summary>
        public static void Register(Action clear) => ClearActions.Add(clear);

        /// <summary>Drops every handler before play starts, so none survive from a previous session.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearAll()
        {
            foreach (var clear in ClearActions)
                clear();
        }
    }
}
