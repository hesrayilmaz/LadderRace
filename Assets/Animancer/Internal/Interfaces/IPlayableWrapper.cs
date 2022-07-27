// Animancer // Copyright 2020 Kybernetik //

using UnityEngine;
using UnityEngine.Playables;

namespace Animancer
{
    /// <summary>
    /// Interface for objects that manage a <see cref="UnityEngine.Playables.Playable"/>.
    /// </summary>
    public interface IPlayableWrapper
    {
        /************************************************************************************************************************/

        /// <summary>The object which receives the output of the <see cref="Playable"/>.</summary>
        IPlayableWrapper Parent { get; }

        /// <summary>The <see cref="UnityEngine.Playables.Playable"/> managed by this object.</summary>
        Playable Playable { get; }

        /// <summary>
        /// Indicates whether child playables should stay connected to the graph at all times.
        /// <para></para>
        /// If false, playables will be disconnected from the graph while they are at 0 weight to stop it from
        /// evaluating them every frame which is generally more efficient.
        /// </summary>
        bool KeepChildrenConnected { get; }

        /// <summary>
        /// How fast the <see cref="Time"/> is advancing every frame.
        /// <para></para>
        /// 1 is the normal speed.
        /// <para></para>
        /// A negative value will play the animation backwards.
        /// <para></para>
        /// Animancer Lite does not allow this value to be changed in a runtime build.
        /// </summary>
        ///
        /// <example>
        /// <code>
        /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.Play(clip);
        ///
        ///     state.Speed = 1;// Normal speed.
        ///     state.Speed = 2;// Double speed.
        ///     state.Speed = 0.5f;// Half speed.
        ///     state.Speed = -1;// Normal speed playing backwards.
        /// }
        /// </code>
        /// </example>
        float Speed { get; set; }

        /************************************************************************************************************************/
    }
}

