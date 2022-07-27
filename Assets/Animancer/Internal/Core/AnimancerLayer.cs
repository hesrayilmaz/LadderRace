// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>
    /// A layer on which animations can play with their states managed independantly of other layers while blending the
    /// output with those layers.
    /// <para></para>
    /// This class can be used as a custom yield instruction to wait until all animations finish playing.
    /// </summary>
    public sealed class AnimancerLayer : AnimancerNode, IAnimationClipCollection
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>[Internal] Constructs a new <see cref="AnimancerLayer"/>.</summary>
        internal AnimancerLayer(AnimancerPlayable root, int index)
            : base(root)
        {

            Index = index;
            _Playable = AnimationMixerPlayable.Create(Root._Graph);
        }

        /************************************************************************************************************************/

        /// <summary>A layer is its own root.</summary>
        public override AnimancerLayer Layer { get { return this; } }

        /// <summary>The <see cref="AnimancerNode.Root"/> receives the output of the <see cref="Playable"/>.</summary>
        public override IPlayableWrapper Parent { get { return Root; } }

        /// <summary>Indicates whether child playables should stay connected to this mixer at all times.</summary>
        public override bool KeepChildrenConnected { get { return Root.KeepChildrenConnected; } }

        /************************************************************************************************************************/

        /// <summary>All of the animation states connected to this layer.</summary>
        private readonly List<AnimancerState> States = new List<AnimancerState>();

        /************************************************************************************************************************/

        private AnimancerState _CurrentState;

        /// <summary>
        /// The state of the animation currently being played.
        /// <para></para>
        /// Specifically, this is the state that was most recently started using any of the Play or CrossFade methods
        /// on this layer. States controlled individually via methods in the <see cref="AnimancerState"/> itself will
        /// not register in this property.
        /// <para></para>
        /// Each time this property changes, the <see cref="CommandCount"/> is incremented.
        /// </summary>
        public AnimancerState CurrentState
        {
            get { return _CurrentState; }
            private set
            {
                _CurrentState = value;
                CommandCount++;
            }
        }

        /// <summary>
        /// The number of times the <see cref="CurrentState"/> has changed. By storing this value and later comparing
        /// the stored value to the current value, you can determine whether the state has been changed since then,
        /// even it has changed back to the same state.
        /// </summary>
        public int CommandCount { get; private set; }

        /************************************************************************************************************************/

        /// <summary>[Pro-Only]
        /// Determines whether this layer is set to additive blending. Otherwise it will override any earlier layers.
        /// </summary>
        public bool IsAdditive
        {
            get { return Root.Layers.IsAdditive(Index); }
            set { Root.Layers.SetAdditive(Index, value); }
        }

        /************************************************************************************************************************/

        /// <summary>[Pro-Only]
        /// Sets an <see cref="AvatarMask"/> to determine which bones this layer will affect.
        /// </summary>
        public void SetMask(AvatarMask mask)
        {
            Root.Layers.SetMask(Index, mask);
        }

#if UNITY_EDITOR
        /// <summary>[Editor-Only]
        /// The <see cref="AvatarMask"/> that determines which bones this layer will affect.
        /// </summary>
        internal AvatarMask _Mask;
#endif

        /************************************************************************************************************************/

        /// <summary>
        /// The average velocity of the root motion of all currently playing animations, taking their current
        /// <see cref="AnimancerNode.Weight"/> into account.
        /// </summary>
        public Vector3 AverageVelocity
        {
            get
            {
                var velocity = default(Vector3);

                var count = States.Count;
                for (int i = 0; i < count; i++)
                {
                    var state = States[i];
                    velocity += state.AverageVelocity * state.Weight;
                }

                return velocity;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Child States
        /************************************************************************************************************************/

        /// <summary>The number of states using this layer as their <see cref="AnimancerState.Parent"/>.</summary>
        public override int ChildCount { get { return States.Count; } }

        /// <summary>Returns the state connected to the specified `index` as a child of this layer.</summary>
        /// <remarks>This method is identical to <see cref="this[int]"/>.</remarks>
        public override AnimancerState GetChild(int index)
        {
            return States[index];
        }

        /// <summary>Returns the state connected to the specified `index` as a child of this layer.</summary>
        /// <remarks>This indexer is identical to <see cref="GetChild(int)"/>.</remarks>
        public AnimancerState this[int index]
        {
            get { return States[index]; }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds a new port and uses <see cref="AnimancerState.SetParent"/> to connect the `state` to it.
        /// </summary>
        public void AddChild(AnimancerState state)
        {
            if (state.Parent == this)
                return;

            var index = States.Count;
            States.Add(null);
            _Playable.SetInputCount(index + 1);
            state.SetParent(this, index);
        }

        /************************************************************************************************************************/

        /// <summary>Connects the `state` to this layer at its <see cref="AnimancerNode.Index"/>.</summary>
        protected internal override void OnAddChild(AnimancerState state)
        {
            OnAddChild(States, state);
        }

        /************************************************************************************************************************/

        /// <summary>Disconnects the `state` from this layer at its <see cref="AnimancerNode.Index"/>.</summary>
        protected internal override void OnRemoveChild(AnimancerState state)
        {
            var index = state.Index;
            Validate.RemoveChild(state, States);

            if (_Playable.GetInput(index).IsValid())
                Root._Graph.Disconnect(_Playable, index);

            // Swap the last state into the place of the one that was just removed.
            var lastPort = States.Count - 1;
            if (index < lastPort)
            {
                state = States[lastPort];
                state.DisconnectFromGraph();

                States[index] = state;
                state.Index = index;

                if (KeepChildrenConnected || state.Weight != 0)
                    state.ConnectToGraph();
            }

            States.RemoveAt(lastPort);
            _Playable.SetInputCount(lastPort);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns an enumerator that will iterate through all states connected directly to this layer (not inside
        /// <see cref="MixerState"/>s).
        /// </summary>
        public override IEnumerator<AnimancerState> GetEnumerator() { return States.GetEnumerator(); }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Create State
        /************************************************************************************************************************/

        /// <summary>
        /// Creates and returns a new <see cref="ClipState"/> to play the `clip`.
        /// <para></para>
        /// This method uses <see cref="AnimancerPlayable.GetKey"/> to determine the <see cref="AnimancerState.Key"/>.
        /// </summary>
        public ClipState CreateState(AnimationClip clip)
        {
            return CreateState(Root.GetKey(clip), clip);
        }

        /// <summary>
        /// Creates and returns a new <see cref="ClipState"/> to play the `clip` and registers it with the `key`.
        /// </summary>
        public ClipState CreateState(object key, AnimationClip clip)
        {
            var state = new ClipState(this, clip);
            Root.States.Register(key, state);
            return state;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="GetOrCreateState(AnimationClip, bool)"/> for each of the specified clips.
        /// <para></para>
        /// If you only want to create a single state, use <see cref="CreateState(AnimationClip)"/>.
        /// </summary>
        public void CreateIfNew(AnimationClip clip0, AnimationClip clip1)
        {
            GetOrCreateState(clip0);
            GetOrCreateState(clip1);
        }

        /// <summary>
        /// Calls <see cref="GetOrCreateState(AnimationClip, bool)"/> for each of the specified clips.
        /// <para></para>
        /// If you only want to create a single state, use <see cref="CreateState(AnimationClip)"/>.
        /// </summary>
        public void CreateIfNew(AnimationClip clip0, AnimationClip clip1, AnimationClip clip2)
        {
            GetOrCreateState(clip0);
            GetOrCreateState(clip1);
            GetOrCreateState(clip2);
        }

        /// <summary>
        /// Calls <see cref="GetOrCreateState(AnimationClip, bool)"/> for each of the specified clips.
        /// <para></para>
        /// If you only want to create a single state, use <see cref="CreateState(AnimationClip)"/>.
        /// </summary>
        public void CreateIfNew(AnimationClip clip0, AnimationClip clip1, AnimationClip clip2, AnimationClip clip3)
        {
            GetOrCreateState(clip0);
            GetOrCreateState(clip1);
            GetOrCreateState(clip2);
            GetOrCreateState(clip3);
        }

        /// <summary>
        /// Calls <see cref="GetOrCreateState(AnimationClip, bool)"/> for each of the specified clips.
        /// <para></para>
        /// If you only want to create a single state, use <see cref="CreateState(AnimationClip)"/>.
        /// </summary>
        public void CreateIfNew(params AnimationClip[] clips)
        {
            if (clips == null)
                return;

            var count = clips.Length;
            for (int i = 0; i < count; i++)
            {
                var clip = clips[i];
                if (clip != null)
                    GetOrCreateState(clip);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="AnimancerPlayable.GetKey"/> and returns the state which registered with that key or
        /// creates one if it doesn't exist.
        /// <para></para>
        /// If the state already exists but has the wrong <see cref="AnimancerState.Clip"/>, the `allowSetClip`
        /// parameter determines what will happen. False causes it to throw an <see cref="ArgumentException"/> while
        /// true allows it to change the <see cref="AnimancerState.Clip"/>. Note that the change is somewhat costly to
        /// performance to use with caution.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public AnimancerState GetOrCreateState(AnimationClip clip, bool allowSetClip = false)
        {
            return GetOrCreateState(Root.GetKey(clip), clip, allowSetClip);
        }

        /// <summary>
        /// Returns the state registered with the <see cref="IHasKey.Key"/> if there is one. Otherwise
        /// this method uses <see cref="ITransition.CreateState"/> to create a new one and registers it with
        /// that key before returning it.
        /// </summary>
        public AnimancerState GetOrCreateState(ITransition transition)
        {
            var key = transition.Key;

            AnimancerState state;
            if (!Root.States.TryGet(key, out state))
            {
                state = transition.CreateState(this);
                Root.States.Register(key, state);
            }

            return state;
        }

        /// <summary>
        /// Returns the state which registered with the `key` or creates one if it doesn't exist.
        /// <para></para>
        /// If the state already exists but has the wrong <see cref="AnimancerState.Clip"/>, the `allowSetClip`
        /// parameter determines what will happen. False causes it to throw an <see cref="ArgumentException"/> while
        /// true allows it to change the <see cref="AnimancerState.Clip"/>. Note that the change is somewhat costly to
        /// performance to use with caution.
        /// <seealso cref="AnimancerState"/>
        /// </summary>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException">Thrown if the `key` is null.</exception>
        /// <remarks>
        /// See also: <see cref="AnimancerPlayable.StateDictionary.GetOrCreate(object, AnimationClip, bool)"/>.
        /// </remarks>
        public AnimancerState GetOrCreateState(object key, AnimationClip clip, bool allowSetClip = false)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            AnimancerState state;
            if (Root.States.TryGet(key, out state))
            {
                // If a state exists with the 'key' but has the wrong clip, either change it or complain.
                if (!ReferenceEquals(state.Clip, clip))
                {
                    if (allowSetClip)
                    {
                        state.Clip = clip;
                    }
                    else
                    {
                        throw new ArgumentException(string.Concat(
                            "A state already exists using the specified 'key', but has a different AnimationClip:",
                            "\n - Key: ", key.ToString(),
                            "\n - Existing Clip: ", state.Clip.ToString(),
                            "\n - New Clip: ", clip.ToString()));
                    }
                }
                // Otherwise make sure it is on the correct layer.
                else
                {
                    AddChild(state);
                }
            }
            else
            {
                state = CreateState(key, clip);
            }

            return state;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Destroys all states connected to this layer. This operation cannot be undone.
        /// </summary>
        public void DestroyStates()
        {
            var count = States.Count;
            while (--count >= 0)
            {
                States[count].Destroy();
            }

            States.Clear();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Play Management
        /************************************************************************************************************************/
        // Starting
        /************************************************************************************************************************/

        /// <summary>
        /// Called by <see cref="AnimancerNode.StartFade"/> (when this layer starts fading, not when one of its states
        /// starts fading). Clears the <see cref="AnimancerState.Events"/> of all states.
        /// </summary>
        protected internal override void OnStartFade()
        {
            var count = States.Count;
            for (int i = 0; i < count; i++)
            {
                States[i].OnStartFade();
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Stops all other animations, plays the `clip`, and returns its state.
        /// <para></para>
        /// The animation will continue playing from its current <see cref="AnimancerState.Time"/>.
        /// To restart it from the beginning you can use <c>...Play(clip).Time = 0;</c>.
        /// </summary>
        public AnimancerState Play(AnimationClip clip)
        {
            return Play(GetOrCreateState(clip));
        }

        /// <summary>
        /// Stops all other animations, plays the `state`, and returns it.
        /// <para></para>
        /// The animation will continue playing from its current <see cref="AnimancerState.Time"/>.
        /// If you wish to force it back to the start, you can simply set the `state`s time to 0.
        /// </summary>
        public AnimancerState Play(AnimancerState state)
        {
            Validate.Root(state, Root);

            if (TargetWeight != 1)
                Weight = 1;

            AddChild(state);

            CurrentState = state;

            state.Play();

            var count = States.Count;
            for (int i = 0; i < count; i++)
            {
                var otherState = States[i];
                if (otherState != state)
                    otherState.Stop();
            }

            return state;
        }

        /// <summary>
        /// Creates a state for the `transition` if it didn't already exist, then calls
        /// <see cref="Play(AnimancerState)"/> or <see cref="Play(AnimancerState, float, FadeMode)"/>
        /// depending on the <see cref="ITransition.FadeDuration"/>.
        /// </summary>
        public AnimancerState Play(ITransition transition)
        {
            return Play(transition, transition.FadeDuration, transition.FadeMode);
        }

        /// <summary>
        /// Stops all other animations, plays the animation registered with the `key`, and returns that
        /// state. If no state is registered with the `key`, this method does nothing and returns null.
        /// <para></para>
        /// The animation will continue playing from its current <see cref="AnimancerState.Time"/>.
        /// If you wish to force it back to the start, you can simply set the returned state's time to 0.
        /// on the returned state.
        /// </summary>
        public AnimancerState Play(object key)
        {
            AnimancerState state;
            if (Root.States.TryGet(key, out state))
                return Play(state);
            else
                return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Starts fading in the `clip` over the course of the `fadeDuration` while fading out all others in the same
        /// layer. Returns its state.
        /// <para></para>
        /// If the `state` was already playing and fading in with less time remaining than the `fadeDuration`, this
        /// method will allow it to complete the existing fade rather than starting a slower one.
        /// <para></para>
        /// If the layer currently has 0 <see cref="AnimancerNode.Weight"/>, this method will fade in the layer itself
        /// and simply <see cref="AnimancerState.Play"/> the `state`.
        /// <para></para>
        /// Animancer Lite only allows the default `fadeDuration` (0.25 seconds) in a runtime build.
        /// </summary>
        public AnimancerState Play(AnimationClip clip, float fadeDuration, FadeMode mode = FadeMode.FixedSpeed)
        {
            return Play(Root.States.GetOrCreate(clip), fadeDuration, mode);
        }

        /// <summary>
        /// Starts fading in the `state` over the course of the `fadeDuration` while fading out all others in this
        /// layer. Returns the `state`.
        /// <para></para>
        /// If the `state` was already playing and fading in with less time remaining than the `fadeDuration`, this
        /// method will allow it to complete the existing fade rather than starting a slower one.
        /// <para></para>
        /// If the layer currently has 0 <see cref="AnimancerNode.Weight"/>, this method will fade in the layer itself
        /// and simply <see cref="AnimancerState.Play"/> the `state`.
        /// <para></para>
        /// Animancer Lite only allows the default `fadeDuration` (0.25 seconds) in a runtime build.
        /// </summary>
        public AnimancerState Play(AnimancerState state, float fadeDuration, FadeMode mode = FadeMode.FixedSpeed)
        {
            Validate.Root(state, Root);

            if (fadeDuration <= 0 ||// With no fade duration, Play immediately.
                (Index == 0 && Weight == 0))// First animation on Layer 0 snap Weight to 1.
                return Play(state);

            bool isFixedDuration;
            EvaluateFadeMode(mode, ref state, ref fadeDuration, out isFixedDuration);

            StartFade(1, fadeDuration);
            if (Weight == 0)
                return Play(state);

            AddChild(state);

            CurrentState = state;

            // If the state is already playing or will finish fading in faster than this new fade,
            // continue the existing fade but still pretend it was restarted.
            if (state.IsPlaying && state.TargetWeight == 1 &&
                (state.Weight == 1 || state.FadeSpeed * fadeDuration > Math.Abs(1 - state.Weight)))
            {
                OnStartFade();
            }
            // Otherwise fade in the target state and fade out all others.
            else
            {
                state.IsPlaying = true;
                state.StartFade(1, fadeDuration);

                var count = States.Count;
                for (int i = 0; i < count; i++)
                {
                    var otherState = States[i];
                    if (otherState != state)
                        otherState.StartFade(0, fadeDuration);
                }
            }

            return state;
        }

        /// <summary>
        /// Creates a state for the `transition` if it didn't already exist, then calls
        /// <see cref="Play(AnimancerState)"/> or <see cref="Play(AnimancerState, float, FadeMode)"/>
        /// depending on the <see cref="ITransition.FadeDuration"/>.
        /// </summary>
        public AnimancerState Play(ITransition transition, float fadeDuration, FadeMode mode = FadeMode.FixedSpeed)
        {
            var state = Root.States.GetOrCreate(transition);
            state = Play(state, fadeDuration, mode);
            transition.Apply(state);
            return state;
        }

        /// <summary>
        /// Starts fading in the animation registered with the `key` over the course of the `fadeDuration` while fading
        /// out all others in the same layer. Returns the animation's state (or null if none was registered).
        /// <para></para>
        /// If the `state` was already playing and fading in with less time remaining than the `fadeDuration`, this
        /// method will allow it to complete the existing fade rather than starting a slower one.
        /// <para></para>
        /// If the layer currently has 0 <see cref="AnimancerNode.Weight"/>, this method will fade in the layer itself
        /// and simply <see cref="AnimancerState.Play"/> the `state`.
        /// <para></para>
        /// Animancer Lite only allows the default `fadeDuration` (0.25 seconds) in a runtime build.
        /// </summary>
        public AnimancerState Play(object key, float fadeDuration, FadeMode mode = FadeMode.FixedSpeed)
        {
            AnimancerState state;
            if (Root.States.TryGet(key, out state))
                return Play(state, fadeDuration, mode);
            else
                return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Manipulates the other parameters according to the `mode`.
        /// </summary>
        private void EvaluateFadeMode(FadeMode mode, ref AnimancerState state, ref float fadeDuration, out bool isFixedDuration)
        {
            switch (mode)
            {
                case FadeMode.FixedSpeed:
                    fadeDuration *= Mathf.Abs(1 - state.Weight);
                    isFixedDuration = false;
                    break;

                case FadeMode.FixedDuration:
                    isFixedDuration = true;
                    break;

                case FadeMode.FromStart:
                    {
                        var previousState = state;
                        state = GetOrCreateWeightlessState(state);
                        if (previousState != state)
                        {
                            var previousLayer = previousState.Layer;
                            if (previousLayer != this && previousLayer.CurrentState == previousState)
                                previousLayer.StartFade(0, fadeDuration);
                        }
                        isFixedDuration = false;
                        break;
                    }

                case FadeMode.NormalizedSpeed:
                    fadeDuration *= Mathf.Abs(1 - state.Weight) * state.Length;
                    isFixedDuration = false;
                    break;

                case FadeMode.NormalizedDuration:
                    fadeDuration *= state.Length;
                    isFixedDuration = true;
                    break;

                case FadeMode.NormalizedFromStart:
                    {
                        var previousState = state;
                        state = GetOrCreateWeightlessState(state);
                        fadeDuration *= state.Length;
                        if (previousState != state)
                        {
                            var previousLayer = previousState.Layer;
                            if (previousLayer != this && previousLayer.CurrentState == previousState)
                                previousLayer.StartFade(0, fadeDuration);
                        }
                        isFixedDuration = false;
                        break;
                    }

                default:
                    throw new ArgumentException("Invalid FadeMode: " + mode, "mode");
            }
        }

        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only]
        /// The maximum number of duplicate states that can be created by <see cref="GetOrCreateWeightlessState"/> for
        /// a single clip before it will start giving usage warnings. Default = 5.
        /// </summary>
        public static int maxStateDepth = 5;
#endif

        /// <summary>
        /// If the `state` is not currently at 0 <see cref="AnimancerNode.Weight"/>, this method finds a copy of it
        /// which is at 0 or creates a new one.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="AnimancerState.Clip"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if more states have been created for this <see cref="AnimancerState.Clip"/> than the
        /// <see cref="maxStateDepth"/> allows.
        /// </exception>
        public AnimancerState GetOrCreateWeightlessState(AnimancerState state)
        {
            if (state.Weight != 0)
            {
                var clip = state.Clip;
                if (clip == null)
                {
                    throw new InvalidOperationException(
                        "GetOrCreateWeightlessState was called on a state which has no clip: " + state);

                    // We could probably support any state type by giving them a Clone method, but that would take a
                    // lot of work for something that might never get used.
                }
                else
                {
                    // Get the default state registered with the clip.
                    if (state.Key as Object != clip)
                        state = Root.States.GetOrCreate(clip, clip);

#if UNITY_EDITOR
                    int depth = 0;
#endif

                    // If that state is not at 0 weight, get or create another state registered using the previous state as a key.
                    // Keep going through states in this manner until you find one at 0 weight.
                    while (state.Weight != 0)
                    {
                        // Explicitly cast the state to an object to avoid the overload that warns about using a state as a key.
                        state = Root.States.GetOrCreate((object)state, clip);

#if UNITY_EDITOR
                        if (++depth == maxStateDepth)
                        {
                            throw new ArgumentOutOfRangeException("depth", "GetOrCreateWeightlessState has created " +
                                maxStateDepth + " or more states for a single clip." +
                                " This is most likely a result of calling the method repeatedly on consecutive frames." +
                                " You probably just want to use FadeMode.FixedSpeed instead, but you can increase" +
                                " AnimancerLayer.maxStateDepth if necessary.");
                        }
#endif
                    }
                }
            }

            // Make sure it is on this layer and at time 0.
            AddChild(state);
            state.Time = 0;

            return state;
        }

        /************************************************************************************************************************/
        // Stopping
        /************************************************************************************************************************/

        /// <summary>
        /// Sets <see cref="AnimancerNode.Weight"/> = 0 and calls <see cref="AnimancerState.Stop"/> on all animations
        /// to stop them from playing and rewind them to the start.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            CurrentState = null;

            var count = States.Count;
            while (--count >= 0)
            {
                States[count].Stop();
            }
        }

        /************************************************************************************************************************/
        // Checking
        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the `clip` is currently being played by at least one state.
        /// </summary>
        public bool IsPlayingClip(AnimationClip clip)
        {
            var count = States.Count;
            for (int i = 0; i < count; i++)
            {
                var state = States[i];
                if (state.Clip == clip && state.IsPlaying)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if at least one animation is being played.
        /// </summary>
        public bool IsAnyStatePlaying()
        {
            var count = States.Count;
            for (int i = 0; i < count; i++)
            {
                if (States[i].IsPlaying)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the <see cref="CurrentState"/> is playing and hasn't yet reached its end.
        /// <para></para>
        /// This method is called by <see cref="IEnumerator.MoveNext"/> so this object can be used as a custom yield
        /// instruction to wait until it finishes.
        /// </summary>
        protected internal override bool IsPlayingAndNotEnding()
        {
            return _CurrentState != null && _CurrentState.IsPlayingAndNotEnding();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calculates the total <see cref="AnimancerNode.Weight"/> of all states in this layer.
        /// </summary>
        public float GetTotalWeight()
        {
            float weight = 0;

            var count = States.Count;
            for (int i = 0; i < count; i++)
            {
                weight += States[i].Weight;
            }

            return weight;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Invokes the <see cref="AnimancerEvent.Sequence.OnEnd"/> callback of the state that is playing the animation
        /// which triggered the event. Returns true if such a state exists (even if it doesn't have a callback).
        /// </summary>
        internal bool TryInvokeOnEndEvent(AnimationEvent animationEvent)
        {
            var count = States.Count;
            for (int i = 0; i < count; i++)
            {
                if (AnimancerPlayable.TryInvokeOnEndEvent(animationEvent, States[i]))
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inverse Kinematics
        /************************************************************************************************************************/

        /// <summary>
        /// Determines the default value of <see cref="AnimancerState.ApplyAnimatorIK"/> for all new states created in
        /// this layer. Default false.
        /// <para></para>
        /// It requires Unity 2018.1 or newer, however 2018.3 or newer is recommended because a bug in earlier versions
        /// of the Playables API caused this value to only take effect while at least one state was at
        /// <see cref="AnimancerNode.Weight"/> = 1 which meant that IK would not work while fading between animations.
        /// </summary>
        public bool DefaultApplyAnimatorIK { get; set; }

        /// <summary>
        /// Determines whether <c>OnAnimatorIK(int layerIndex)</c> will be called on the animated object for any
        /// <see cref="States"/>. The initial value is determined by <see cref="DefaultApplyAnimatorIK"/> when a new
        /// state is created and setting this value will also set the default.
        /// <para></para>
        /// This is equivalent to the "IK Pass" toggle in Animator Controller layers, except that due to limitations in
        /// the Playables API the <c>layerIndex</c> will always be zero.
        /// <para></para>
        /// It requires Unity 2018.1 or newer, however 2018.3 or newer is recommended because a bug in earlier versions
        /// of the Playables API caused this value to only take effect while at least one state was at
        /// <see cref="AnimancerNode.Weight"/> = 1 which meant that IK would not work while fading between animations.
        /// </summary>
        public bool ApplyAnimatorIK
        {
            get
            {
                var count = States.Count;
                for (int i = 0; i < count; i++)
                {
                    if (States[i].ApplyAnimatorIK)
                        return true;
                }

                return false;
            }
            set
            {
                DefaultApplyAnimatorIK = value;

                var count = States.Count;
                for (int i = 0; i < count; i++)
                {
                    States[i].ApplyAnimatorIK = value;
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Determines the default value of <see cref="AnimancerState.ApplyFootIK"/> for all new states created in this
        /// layer. Default false.
        /// </summary>
        public bool DefaultApplyFootIK { get; set; }

        /// <summary>
        /// Determines whether any of the <see cref="States"/> in this layer are applying IK to the character's feet.
        /// The initial value is determined by <see cref="DefaultApplyFootIK"/> when a new state is created.
        /// <para></para>
        /// This is equivalent to the "Foot IK" toggle in Animator Controller states (applied to the whole layer).
        /// </summary>
        public bool ApplyFootIK
        {
            get
            {
                var count = States.Count;
                for (int i = 0; i < count; i++)
                {
                    if (States[i].ApplyFootIK)
                        return true;
                }

                return false;
            }
            set
            {
                DefaultApplyFootIK = value;

                var count = States.Count;
                for (int i = 0; i < count; i++)
                {
                    States[i].ApplyFootIK = value;
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inspector
        /************************************************************************************************************************/

        /// <summary>[<see cref="IAnimationClipCollection"/>]
        /// Gathers all the animations in this layer.
        /// </summary>
        public void GatherAnimationClips(ICollection<AnimationClip> clips)
        {
            clips.GatherFromSources(States);
        }

        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only] The Inspector display name of this layer.</summary>
        private string _Name;
#endif

        /// <summary>The Inspector display name of this layer.</summary>
        public override string ToString()
        {
#if UNITY_EDITOR
            if (_Name == null)
            {
                if (_Mask != null)
                    return _Mask.name;

                _Name = Index == 0 ? "Base Layer" : "Layer " + Index;
            }

            return _Name;
#else
            return "Layer " + Index;
#endif
        }

        /// <summary>[Editor-Conditional]
        /// Sets the Inspector display name of this layer. Note that layer names are Editor-Only so any calls to this
        /// method will automatically be compiled out of a runtime build.
        /// </summary>
        [System.Diagnostics.Conditional(Strings.EditorOnly)]
        public void SetName(string name)
        {
#if UNITY_EDITOR
            _Name = name;
#endif
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Called by <see cref="AnimancerNode.AppendDescription"/> to append the details of this node.
        /// </summary>
        protected override void AppendDetails(StringBuilder text, string delimiter)
        {
            base.AppendDetails(text, delimiter);

            text.Append(delimiter).Append("CurrentState: ").Append(CurrentState);
            text.Append(delimiter).Append("CommandCount: ").Append(CommandCount);
            text.Append(delimiter).Append("IsAdditive: ").Append(IsAdditive);

#if UNITY_EDITOR
            text.Append(delimiter).Append("AvatarMask: ").Append(_Mask);
#endif
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

