// Animancer // Copyright 2020 Kybernetik //

using UnityEngine;
using UnityEngine.Playables;

namespace Animancer
{
    /// <summary>
    /// An object that can be updated during <see cref="PlayableBehaviour.PrepareFrame"/>.
    /// <para></para>
    /// Register to receive updates using <see cref="AnimancerPlayable.RequireUpdate(IUpdatable)"/> and stop
    /// receiving updates using <see cref="AnimancerPlayable.CancelUpdate(IUpdatable)"/>.
    /// </summary>
    ///
    /// <example><code>
    /// public sealed class UpdatableBehaviour : MonoBehaviour, IUpdatable
    /// {
    ///     [SerializeField] private AnimancerComponent _Animancer;
    ///
    ///     private void OnEnable()
    ///     {
    ///         _Animancer.Playable.RequireUpdate(this);
    ///     }
    ///
    ///     private void OnEnable()
    ///     {
    ///         _Animancer.Playable.CancelUpdate(this);
    ///     }
    ///
    ///     public void EarlyUpdate()
    ///     {
    ///         // Called at the start of every Animator update before the playables get updated.
    ///     }
    ///
    ///     public void LateUpdate()
    ///     {
    ///         // Called at the end of every Animator update after the playables get updated.
    ///     }
    ///
    ///     public void OnDestroy()
    ///     {
    ///         // Called by AnimancerPlayable.Destroy if this object is currently being updated.
    ///     }
    /// }
    /// </code></example>
    public interface IUpdatable : IKeyHolder
    {
        /************************************************************************************************************************/

        /// <summary>Called at the start of every <see cref="Animator"/> update before the playables get updated.</summary>
        /// <remarks>The <see cref="Animator.updateMode"/> determines when it updates.</remarks>
        void EarlyUpdate();

        /// <summary>Called at the end of every <see cref="Animator"/> update after the playables get updated.</summary>
        /// <remarks>
        /// The <see cref="Animator.updateMode"/> determines when it updates.
        /// This method has nothing to do with <see cref="MonoBehaviour"/>.LateUpdate().
        /// </remarks>
        void LateUpdate();

        /// <summary>Called by <see cref="AnimancerPlayable.Destroy"/> if this object is currently being updated.</summary>
        void OnDestroy();

        /************************************************************************************************************************/
    }
}

