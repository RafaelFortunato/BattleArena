using UnityEngine;

namespace BattleArena.Core
{
    /// <summary>
    /// Base for the handful of objects there is exactly one of. It neither searches the scene nor creates
    /// itself: a manager's value is the references wired into it, so a missing one should cost its feature
    /// (callers null-check <see cref="Instance"/>), never conjure a stand-in with every slot empty.
    ///
    /// Read <see cref="Instance"/> from OnEnable or later, and give each concrete singleton
    /// [DefaultExecutionOrder(-100)] (the attribute is not inherited) so it binds before anyone reads it.
    /// Binding happens in Awake AND OnEnable: a domain reload during play re-runs OnEnable but not Awake.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        /// <summary>The bound instance, or null when there is none in the scene.</summary>
        public static T Instance { get; private set; }

        /// <summary>Binds this instance.</summary>
        protected virtual void Awake() => Bind();

        /// <summary>Binds again, for the domain reload case where Awake does not run.</summary>
        protected virtual void OnEnable() => Bind();

        /// <summary>Clears <see cref="Instance"/> when the bound instance goes away.</summary>
        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Becomes <see cref="Instance"/>, or logs an error and stays inactive if another instance is already bound.
        /// </summary>
        private void Bind()
        {
            if (Instance != null && Instance != this)
            {
                // A duplicate is an authoring mistake worth seeing; destroying it would take its GameObject down too.
                Debug.LogError($"{typeof(T).Name}: a second one exists on '{name}'. Keeping the first.", this);
                return;
            }

            Instance = (T)this;
            OnBind();
        }

        /// <summary>Cache anything derived from the wired references here. Runs on every bind.</summary>
        protected virtual void OnBind() { }
    }
}
