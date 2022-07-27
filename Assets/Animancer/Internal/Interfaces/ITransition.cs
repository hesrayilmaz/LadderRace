// Animancer // Copyright 2020 Kybernetik //

namespace Animancer
{
    /// <summary>
    /// An object that can create an <see cref="AnimancerState"/> and manage the details of how it should be played.
    /// <para></para>
    /// Transitions are generally used as arguments for <see cref="AnimancerPlayable.Play(ITransition)"/>.
    /// </summary>
    public interface ITransition : IHasKey
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Creates and returns a new <see cref="AnimancerState"/> connected to the `layer`.
        /// </summary>
        /// <remarks>
        /// The first time a transition is used on an object, this method is called to create the state and register it
        /// in the internal dictionary using the <see cref="IHasKey.Key"/> so that it can be reused later on.
        /// </remarks>
        AnimancerState CreateState(AnimancerLayer layer);

        /// <summary>
        /// When a transition is passed into <see cref="AnimancerPlayable.Play(ITransition)"/>, this property
        /// determines which <see cref="Animancer.FadeMode"/> will be used.
        /// </summary>
        FadeMode FadeMode { get; }

        /// <summary>The amount of time the transition should take (in seconds).</summary>
        float FadeDuration { get; }

        /// <summary>
        /// Called by <see cref="AnimancerPlayable.Play(ITransition)"/> to apply any modifications to the `state`.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="CreateState"/>, this method is called every time the transition is used so it can do
        /// things like set the <see cref="AnimancerState.Events"/> or <see cref="AnimancerState.Time"/>.
        /// </remarks>
        void Apply(AnimancerState state);

        /************************************************************************************************************************/
    }

    /// <summary>
    /// An <see cref="ITransition"/> with some additional details for the Unity Editor GUI.
    /// </summary>
    public interface ITransitionDetailed : ITransition
    {
        /************************************************************************************************************************/

        /// <summary>Indicates what the value of <see cref="AnimancerState.IsLooping"/> will be for the created state.</summary>
        bool IsLooping { get; }

        /// <summary>Determines what <see cref="AnimancerState.NormalizedTime"/> to start the animation at.</summary>
        float NormalizedStartTime { get; set; }

        /// <summary>Determines how fast the animation plays (1x = normal speed).</summary>
        float Speed { get; set; }

        /// <summary>The maximum amount of time the animation is expected to take (in seconds).</summary>
        float MaximumDuration { get; }

#if UNITY_EDITOR
        /// <summary>[Editor-Only] Adds context menu functions for this transition.</summary>
        void AddItemsToContextMenu(UnityEditor.GenericMenu menu, UnityEditor.SerializedProperty property,
            Editor.Serialization.PropertyAccessor accessor);
#endif

        /************************************************************************************************************************/
    }
}

