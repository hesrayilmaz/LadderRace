// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Animancer
{
    /// <summary>
    /// Base class for all states in an <see cref="AnimancerPlayable"/> graph.
    /// Each state is a wrapper for a <see cref="Playable"/> in the <see cref="PlayableGraph"/>.
    /// <para></para>
    /// This class can be used as a custom yield instruction to wait until the animation either stops playing or reaches its end.
    /// </summary>
    /// <remarks>
    /// There are various different ways of getting a state:
    /// <list type="bullet">
    ///   <item>
    ///   Use one of the state's constructors. Generally the first parameter is a layer or mixer which will be used as
    ///   the state's parent. If not specified, you will need to call SetParent manually. Also note than an
    ///   AnimancerComponent can be implicitly cast to its first layer.
    ///   </item>
    ///   <item>
    ///   AnimancerController.CreateState creates a new ClipState. You can optionally specify a custom `key` to
    ///   register it in the dictionary instead of the default (the `clip` itself).
    ///   </item>
    ///   <item>
    ///   AnimancerController.GetOrCreateState looks for an existing state registered with the specified `key` and only
    ///   creates a new one if it doesn’t already exist.
    ///   </item>
    ///   <item>
    ///   AnimancerController.GetState returns an existing state registered with the specified `key` if there is one.
    ///   </item>
    ///   <item>
    ///   AnimancerController.TryGetState is similar but returns a bool to indicate success and returns the `state`
    ///   as an out parameter.
    ///   </item>
    ///   <item>
    ///   AnimancerController.Play and CrossFade also return the state they play.
    ///   </item>
    /// </list>
    /// <para></para>
    /// Note that when inheriting from this class, the <see cref="AnimancerNode._Playable"/> field must be assigned in the
    /// constructor to avoid throwing <see cref="ArgumentException"/>s throughout the system.
    /// </remarks>
    public abstract partial class AnimancerState : AnimancerNode, IAnimationClipCollection
    {
        /************************************************************************************************************************/
        #region Hierarchy
        /************************************************************************************************************************/

        /// <summary>The object which receives the output of the <see cref="Playable"/>.</summary>
        public override IPlayableWrapper Parent { get { return _Parent; } }
        private AnimancerNode _Parent;

        /// <summary>
        /// Connects this state to the `parent` mixer at the specified `index`.
        /// <para></para>
        /// See also <see cref="AnimancerLayer.AddChild(AnimancerState)"/> to connect a state to an available port on a
        /// layer.
        /// </summary>
        public void SetParent(AnimancerNode parent, int index)
        {
            if (_Parent != null)
                _Parent.OnRemoveChild(this);

            Index = index;
            _Parent = parent;

            if (parent != null)
            {
                SetWeightDirty();
                parent.OnAddChild(this);
            }
        }

        /// <summary>[Internal]
        /// Called by <see cref="AnimancerNode.OnAddChild(IList{AnimancerState}, AnimancerState)"/> if the specified
        /// port is already occupied so it can be cleared without triggering any other calls.
        /// </summary>
        internal void ClearParent()
        {
            Index = -1;
            _Parent = null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="AnimancerNode.Weight"/> of this state multiplied by the <see cref="AnimancerNode.Weight"/> of each of
        /// its parents down the hierarchy to determine how much this state affects the final output.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown if this state has no <see cref="AnimancerNode.Parent"/>.</exception>
        public float EffectiveWeight
        {
            get
            {
                var weight = Weight;

                var parent = _Parent;
                while (parent != null)
                {
                    weight *= parent.Weight;
                    parent = parent.Parent as AnimancerNode;
                }

                return weight;
            }
        }

        /************************************************************************************************************************/
        // Layer.
        /************************************************************************************************************************/

        /// <summary>The root <see cref="AnimancerLayer"/> which this state is connected to.</summary>
        public override AnimancerLayer Layer { get { return _Parent.Layer; } }

        /// <summary>
        /// The index of the <see cref="AnimancerLayer"/> this state is connected to (determined by the
        /// <see cref="Parent"/>).
        /// </summary>
        public int LayerIndex
        {
            get { return _Parent.Layer.Index; }
            set
            {
                if (_Parent != null && LayerIndex == value)
                    return;

                Root.Layers[value].AddChild(this);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Key and Clip
        /************************************************************************************************************************/

        internal object _Key;

        /// <summary>
        /// The object used to identify this state in the root <see cref="AnimancerPlayable.States"/> dictionary.
        /// Can be null.
        /// </summary>
        public object Key
        {
            get { return _Key; }
            set
            {
                Root.States.Unregister(this);
                Root.States.Register(value, this);
            }
        }

        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimationClip"/> which this state plays (if any).</summary>
        /// <exception cref="NotSupportedException">
        /// Thrown if this state type doesn't have a clip and you try to set it.
        /// </exception>
        public virtual AnimationClip Clip
        {
            get { return null; }
            set { throw new NotSupportedException(GetType() + " does not support setting the Clip."); }
        }

        /// <summary>The main object to show in the Inspector for this state (if any).</summary>
        /// <exception cref="NotSupportedException">
        /// Thrown if this state type doesn't have a main object and you try to set it.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// Thrown if you try to assign something this state can't use.
        /// </exception>
        public virtual Object MainObject
        {
            get { return null; }
            set { throw new NotSupportedException(GetType() + " does not support setting the MainObject."); }
        }

        /************************************************************************************************************************/

        /// <summary>The average velocity of the root motion caused by this state.</summary>
        public virtual Vector3 AverageVelocity
        {
            get { return default(Vector3); }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Playing
        /************************************************************************************************************************/

        /// <summary>Is the <see cref="Time"/> automatically advancing?</summary>
        private bool _IsPlaying = true;

        /// <summary>
        /// Has <see cref="_IsPlaying"/> changed since it was last applied to the <see cref="Playable"/>.
        /// </summary>
        /// <remarks>
        /// Playables start playing by default so we start dirty to pause it during the first update (unless
        /// <see cref="IsPlaying"/> is set to true before that).
        /// </remarks>
        private bool _IsPlayingDirty;

        /************************************************************************************************************************/

        /// <summary>Is the <see cref="Time"/> automatically advancing?</summary>
        ///
        /// <example>
        /// <code>
        /// void IsPlayingExample(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.States.GetOrCreate(clip);
        ///
        ///     if (state.IsPlaying)
        ///         Debug.Log(clip + " is playing");
        ///     else
        ///         Debug.Log(clip + " is paused");
        ///
        ///     state.IsPlaying = false;// Pause the animation.
        ///
        ///     state.IsPlaying = true;// Unpause the animation.
        /// }
        /// </code>
        /// </example>
        public virtual bool IsPlaying
        {
            get { return _IsPlaying; }
            set
            {
                if (_IsPlaying == value)
                    return;

                _IsPlaying = value;

                // If it was already dirty then we just returned to the previous state so it is no longer dirty.
                if (_IsPlayingDirty)
                {
                    _IsPlayingDirty = false;
                }
                else
                {
                    _IsPlayingDirty = true;
                    Root.RequireUpdate(this);
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if this state is playing and is at or fading towards a non-zero
        /// <see cref="AnimancerNode.Weight"/>.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return
                    _IsPlaying &&
                    TargetWeight > 0;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if this state is not playing and is at 0 <see cref="AnimancerNode.Weight"/>.
        /// </summary>
        public bool IsStopped
        {
            get
            {
                return
                    !_IsPlaying &&
                    Weight == 0;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Updates the <see cref="AnimancerNode.Weight"/> for fading, applies it to this state's port on the parent
        /// mixer, and plays or pauses the <see cref="Playable"/> if its state is dirty.
        /// <para></para>
        /// If the <see cref="Parent"/>'s <see cref="AnimancerNode.KeepChildrenConnected"/> is set to false, this
        /// method will also connect/disconnect this node from the <see cref="Parent"/> in the playable graph.
        /// </summary>
        protected internal override void Update(out bool needsMoreUpdates)
        {
            base.Update(out needsMoreUpdates);

            if (_IsPlayingDirty)
            {
                _IsPlayingDirty = false;

                if (_IsPlaying)
                {
#if UNITY_2017_3_OR_NEWER
                    _Playable.Play();
#else
                    _Playable.SetPlayState(PlayState.Playing);
#endif
                }
                else
                {
#if UNITY_2017_3_OR_NEWER
                    _Playable.Pause();
#else
                    _Playable.SetPlayState(PlayState.Paused);
#endif
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Timing
        /************************************************************************************************************************/
        // Time.
        /************************************************************************************************************************/

        /// <summary>
        /// The current time of the <see cref="Playable"/>, retrieved by <see cref="Time"/> whenever the
        /// <see cref="_TimeFrameID"/> is different from the <see cref="AnimancerPlayable.FrameID"/>.</summary>
        private float _Time;

        /// <summary>
        /// The <see cref="AnimancerPlayable.FrameID"/> from when the <see cref="Time"/> was last retrieved from the
        /// <see cref="Playable"/>.
        /// </summary>
        private uint _TimeFrameID;

        /************************************************************************************************************************/

        /// <summary>
        /// The number of seconds that have passed since the start of this animation.
        /// <para></para>
        /// This value will continue increasing after the animation passes the end of its <see cref="Length"/> while
        /// the animated object either freezes in place or starts again from the beginning according to whether it is
        /// looping or not.
        /// <para></para>
        /// Animancer Lite does not allow this value to be changed in a runtime build (except resetting it to 0).
        /// </summary>
        ///
        /// <example>
        /// <code>
        /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.Play(clip);
        ///
        ///     // Skip 0.5 seconds into the animation:
        ///     state.Time = 0.5f;
        ///
        ///     // Skip 50% of the way through the animation (0.5 in a range of 0 to 1):
        ///     state.NormalizedTime = 0.5f;
        ///
        ///     // Skip to the end of the animation and play backwards.
        ///     state.NormalizedTime = 1;
        ///     state.Speed = -1;
        /// }
        /// </code>
        /// </example>
        ///
        /// <remarks>
        /// This property internally uses <see cref="NewTime"/> whenever the value is out of date or gets changed.
        /// </remarks>
        public float Time
        {
            get
            {
                var frameID = Root.FrameID;
                if (_TimeFrameID != frameID)
                {
                    _TimeFrameID = frameID;
                    _Time = NewTime;
                }

                return _Time;
            }
            set
            {

                if (_TimeFrameID == Root.FrameID)
                {
                    if (_Time == value)
                        return;
                }
                else
                {
                    _TimeFrameID = Root.FrameID;
                }

                Debug.Assert(!float.IsNaN(value), "Time must not be NaN");

                _Time = value;
                NewTime = value;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The internal implementation of <see cref="Time"/> which actually gets and sets the underlying value.
        /// </summary>
        /// <remarks>
        /// Setting this value actually calls <see cref="PlayableExtensions.SetTime"/> twice to ensure that animation
        /// events aren't triggered incorrectly. Calling it only once would trigger any animation events between the
        /// previous time and the new time. So if an animation plays to the end and you set the time back to 0 (such as
        /// by calling <see cref="Stop"/> or playing a different animation), the next time that animation played it
        /// would immediately trigger all of its events, then play through and trigger them normally as well.
        /// </remarks>
        protected virtual float NewTime
        {
            get { return (float)_Playable.GetTime(); }
            set
            {
                var time = (double)value;
                _Playable.SetTime(time);
                _Playable.SetTime(time);

                if (_EventUpdatable != null)
                    _EventUpdatable.OnTimeChanged();
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="Time"/> of this state as a portion of the animation's <see cref="Length"/>, meaning the
        /// value goes from 0 to 1 as it plays from start to end, regardless of how long that actually takes.
        /// <para></para>
        /// This value will continue increasing after the animation passes the end of its <see cref="Length"/> while
        /// the animated object either freezes in place or starts again from the beginning according to whether it is
        /// looping or not.
        /// <para></para>
        /// The fractional part of the value (<c>NormalizedTime % 1</c>) is the percentage (0-1) of progress in the
        /// current loop while the integer part (<c>(int)NormalizedTime</c>) is the number of times the animation has
        /// been looped.
        /// <para></para>
        /// Animancer Lite does not allow this value to be changed to a value other than 0 in a runtime build.
        /// </summary>
        ///
        /// <example>
        /// <code>
        /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.Play(clip);
        ///
        ///     // Skip 0.5 seconds into the animation:
        ///     state.Time = 0.5f;
        ///
        ///     // Skip 50% of the way through the animation (0.5 in a range of 0 to 1):
        ///     state.NormalizedTime = 0.5f;
        ///
        ///     // Skip to the end of the animation and play backwards.
        ///     state.NormalizedTime = 1;
        ///     state.Speed = -1;
        /// }
        /// </code>
        /// </example>
        public float NormalizedTime
        {
            get
            {
                var length = Length;
                if (length != 0)
                    return Time / Length;
                else
                    return 0;
            }
            set { Time = value * Length; }
        }

        /************************************************************************************************************************/
        // Duration.
        /************************************************************************************************************************/

        /// <summary>
        /// The number of seconds the animation will take to play fully at its current
        /// <see cref="AnimancerNode.Speed"/>.
        /// <para></para>
        /// Setting this value modifies the <see cref="AnimancerNode.Speed"/>, not the <see cref="Length"/>.
        /// Animancer Lite does not allow this value to be changed in a runtime build.
        /// <para></para>
        /// For the time remaining from now until it reaches the end, use <see cref="RemainingDuration"/> instead.
        /// </summary>
        ///
        /// <example>
        /// <code>
        /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.Play(clip);
        ///
        ///     state.Duration = 1;// Play fully in 1 second.
        ///     state.Duration = 2;// Play fully in 2 seconds.
        ///     state.Duration = 0.5f;// Play fully in half a second.
        ///     state.Duration = -1;// Play backwards fully in 1 second.
        ///     state.NormalizedTime = 1; state.Duration = -1;// Play backwards from the end in 1 second.
        /// }
        /// </code>
        /// </example>
        public float Duration
        {
            get
            {
                var speed = Speed;
                if (speed == 0)
                    return float.PositiveInfinity;
                else
                    return Length / Math.Abs(speed);
            }
            set
            {
                if (value == 0)
                    Speed = float.PositiveInfinity;
                else
                    Speed = Length / value;
            }
        }

        /// <summary>
        /// The number of seconds the animation will take to reach the end at its current <see cref="AnimancerNode.Speed"/>.
        /// <para></para>
        /// Setting this value modifies the <see cref="AnimancerNode.Speed"/>, not the <see cref="Length"/>.
        /// Animancer Lite does not allow this value to be changed in a runtime build.
        /// <para></para>
        /// For the time it would take to play fully from the start, use <see cref="Duration"/> instead.
        /// </summary>
        ///
        /// <example>
        /// <code>
        /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.Play(clip);
        ///
        ///     state.RemainingDuration = 1;// Play from the current time to the end in 1 second.
        ///     state.RemainingDuration = 2;// Play from the current time to the end in 2 seconds.
        ///     state.RemainingDuration = 0.5f;// Play from the current time to the end in half a second.
        ///     state.RemainingDuration = -1;// Play backwards from the current time to the end in 1 second.
        /// }
        /// </code>
        /// </example>
        public float RemainingDuration
        {
            get
            {
                var speed = Speed;
                if (speed == 0)
                    return float.PositiveInfinity;

                var length = Length;
                if (_EventUpdatable != null)
                {
                    if (speed > 0)
                        length *= _EventUpdatable.Events.NormalizedEndTime;
                    else
                        length *= 1 - _EventUpdatable.Events.NormalizedEndTime;
                }

                var time = Time;
                if (IsLooping)
                    time = Mathf.Repeat(time, length);

                return (length - time) / Math.Abs(speed);
            }
            set
            {
                if (value == 0)
                    throw new ArgumentException("Duration cannot be set to 0 because that would require infinite speed.");

                var length = Length;
                if (_EventUpdatable != null)
                {
                    if (value > 0)
                        length *= _EventUpdatable.Events.NormalizedEndTime;
                    else
                        length *= 1 - _EventUpdatable.Events.NormalizedEndTime;
                }

                var time = Time;
                if (IsLooping)
                    time = Mathf.Repeat(time, length);

                Speed = (length - time) / value;
            }
        }

        /************************************************************************************************************************/
        // Length.
        /************************************************************************************************************************/

        /// <summary>The total time this state takes to play in seconds (when <c>Speed = 1</c>).</summary>
        public abstract float Length { get; }

        /// <summary>
        /// Indicates whether this state will loop back to the start when it reaches the end.
        /// </summary>
        public virtual bool IsLooping { get { return false; } }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inverse Kinematics
        /************************************************************************************************************************/

        /// <summary>
        /// Determines whether <c>OnAnimatorIK(int layerIndex)</c> will be called on the animated object.
        /// The initial value is determined by <see cref="AnimancerLayer.DefaultApplyAnimatorIK"/>.
        /// <para></para>
        /// This is equivalent to the "IK Pass" toggle in Animator Controller layers, except that due to limitations in
        /// the Playables API the <c>layerIndex</c> will always be zero.
        /// <para></para>
        /// It requires Unity 2018.1 or newer, however 2018.3 or newer is recommended because a bug in earlier versions
        /// of the Playables API caused this value to only take effect while a state was at
        /// <see cref="AnimancerNode.Weight"/> == 1 which meant that IK would not work while fading between animations.
        /// <para></para>
        /// Returns false and does nothing if this state does not support IK.
        /// </summary>
        public virtual bool ApplyAnimatorIK
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Indicates whether this state is applying IK to the character's feet.
        /// The initial value is determined by <see cref="AnimancerLayer.DefaultApplyFootIK"/>.
        /// <para></para>
        /// This is equivalent to the "Foot IK" toggle in Animator Controller states.
        /// <para></para>
        /// Returns false and does nothing if this state does not support IK.
        /// </summary>
        public virtual bool ApplyFootIK
        {
            get { return false; }
            set { }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Methods
        /************************************************************************************************************************/

        /// <summary>Constructs a new <see cref="AnimancerState"/>.</summary>
        public AnimancerState(AnimancerPlayable root) : base(root)
        {
            IsPlaying = false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Plays this animation immediately, without any blending.
        /// Sets <see cref="IsPlaying"/> = true, <see cref="AnimancerNode.Weight"/> = 1, and clears the
        /// <see cref="Events"/>.
        /// <para></para>
        /// This method does not change the <see cref="Time"/> so it will continue from its current value.
        /// </summary>
        public void Play()
        {
            IsPlaying = true;
            Weight = 1;
            EventUpdatable.TryClear(_EventUpdatable);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Stops the animation and makes it inactive immediately so it no longer affects the output.
        /// Sets <see cref="AnimancerNode.Weight"/> = 0, <see cref="IsPlaying"/> = false, <see cref="Time"/> = 0, and
        /// clears the <see cref="Events"/>.
        /// <para></para>
        /// If you only want to freeze the animation in place, you can set <see cref="IsPlaying"/> = false instead. Or
        /// to freeze all animations, you can call <see cref="AnimancerPlayable.PauseGraph"/>.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            IsPlaying = false;
            Time = 0;
            EventUpdatable.TryClear(_EventUpdatable);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Called by <see cref="AnimancerNode.StartFade"/>. Clears the <see cref="Events"/>.
        /// </summary>
        protected internal override void OnStartFade()
        {
            EventUpdatable.TryClear(_EventUpdatable);
        }

        /************************************************************************************************************************/

        /// <summary>Destroys the <see cref="Playable"/>.</summary>
        public virtual void Destroy()
        {
            GC.SuppressFinalize(this);

            if (_Parent != null)
                _Parent.OnRemoveChild(this);

            Index = -1;
            EventUpdatable.TryClear(_EventUpdatable);

            Root.States.Unregister(this);

            // For some reason this is slightly faster than _Playable.Destroy().
            if (_Playable.IsValid())
                Root._Graph.DestroyPlayable(_Playable);
        }

        /************************************************************************************************************************/

        /// <summary>[<see cref="IAnimationClipCollection"/>]
        /// Gathers all the animations in this state.
        /// </summary>
        public virtual void GatherAnimationClips(ICollection<AnimationClip> clips)
        {
            clips.Gather(Clip);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the animation is playing and has not yet passed the
        /// <see cref="AnimancerEvent.Sequence.endEvent"/>.
        /// <para></para>
        /// This method is called by <see cref="IEnumerator.MoveNext"/> so this object can be used as a custom yield
        /// instruction to wait until it finishes.
        /// </summary>
        protected internal override bool IsPlayingAndNotEnding()
        {
            if (!IsPlaying)
                return false;

            var speed = EffectiveSpeed;
            if (speed > 0)
            {
                float endTime;
                if (_EventUpdatable != null)
                {
                    endTime = _EventUpdatable.Events.endEvent.normalizedTime;
                    if (float.IsNaN(endTime))
                        endTime = Length;
                    else
                        endTime *= Length;
                }
                else endTime = Length;

                return Time <= endTime;
            }
            else if (speed < 0)
            {
                float endTime;
                if (_EventUpdatable != null)
                {
                    endTime = _EventUpdatable.Events.endEvent.normalizedTime;
                    if (float.IsNaN(endTime))
                        endTime = 0;
                    else
                        endTime *= Length;
                }
                else endTime = 0;

                return Time >= endTime;
            }
            else return true;
        }

        /************************************************************************************************************************/
        #region Descriptions
        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only] Returns a custom drawer for this state.</summary>
        protected internal virtual Editor.IAnimancerNodeDrawer GetDrawer()
        {
            return new Editor.AnimancerStateDrawer<AnimancerState>(this);
        }
#endif

        /************************************************************************************************************************/

        /// <summary>
        /// Called by <see cref="AnimancerNode.AppendDescription"/> to append the details of this node.
        /// </summary>
        protected override void AppendDetails(StringBuilder text, string delimiter)
        {
            base.AppendDetails(text, delimiter);

            text.Append(delimiter).Append("IsPlaying: ").Append(IsPlaying);
            text.Append(delimiter).Append("Time (Normalized): ").Append(Time);
            text.Append(" (").Append(NormalizedTime).Append(')');
            text.Append(delimiter).Append("Length: ").Append(Length);
            text.Append(delimiter).Append("IsLooping: ").Append(IsLooping);

            if (_Key != null)
                text.Append(delimiter).Append("Key: ").Append(_Key);

            if (_EventUpdatable != null && _EventUpdatable.Events != null)
                _EventUpdatable.Events.endEvent.AppendDetails(text, "EndEvent", delimiter);

            var clip = Clip;
            if (clip != null)
            {
#if UNITY_EDITOR
                text.Append(delimiter).Append("AssetPath: ").Append(AssetDatabase.GetAssetPath(clip));
#endif
            }
        }

        /************************************************************************************************************************/

        /// <summary>Returns the hierarchy path of this state through its <see cref="Parent"/>s.</summary>
        public string GetPath()
        {
            if (_Parent == null)
                return null;

            var path = new StringBuilder();

            AppendPath(path, _Parent);
            AppendPortAndType(path);

            return path.ToString();
        }

        /// <summary>Appends the hierarchy path of this state through its <see cref="Parent"/>s.</summary>
        private static void AppendPath(StringBuilder path, AnimancerNode parent)
        {
            var parentState = parent as AnimancerState;
            if (parentState != null && parentState._Parent != null)
            {
                AppendPath(path, parentState._Parent);
            }
            else
            {
                path.Append("Layers[")
                    .Append(parent.Layer.Index)
                    .Append("].States");
                return;
            }

            var state = parent as AnimancerState;
            if (state != null)
            {
                state.AppendPortAndType(path);
            }
            else
            {
                path.Append(" -> ")
                    .Append(parent.GetType());
            }
        }

        /// <summary>Appends "[Index] -> GetType().Name".</summary>
        private void AppendPortAndType(StringBuilder path)
        {
            path.Append('[')
                .Append(Index)
                .Append("] -> ")
                .Append(GetType().Name);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Transition
        /************************************************************************************************************************/

        /// <summary>
        /// Base class for serializable <see cref="ITransition"/>s which can create a particular type of
        /// <see cref="AnimancerState"/> when passed into <see cref="AnimancerPlayable.Play(ITransition)"/>.
        /// </summary>
        /// <remarks>
        /// Unfortunately the tool used to generate this documentation does not currently support nested types with
        /// identical names, so only one <c>Transition</c> class will actually have a documentation page.
        /// <para></para>
        /// Even though it has the <see cref="SerializableAttribute"/>, this class won't actually get serialized
        /// by Unity because it's generic and abstract. Each child class still needs to include the attribute.
        /// </remarks>
        [Serializable]
        public abstract class Transition<TState> : ITransitionDetailed where TState : AnimancerState
        {
            /************************************************************************************************************************/

            [SerializeField, Tooltip(Strings.ProOnlyTag + "The amount of time the transition will take (in seconds)")]
            private float _FadeDuration = AnimancerPlayable.DefaultFadeDuration;

            /// <summary>[<see cref="SerializeField"/>] The amount of time the transition will take (in seconds).</summary>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when setting the value to a negative number.</exception>
            public float FadeDuration
            {
                get { return _FadeDuration; }
                set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException("value", "must not be negative");

                    _FadeDuration = value;
                }
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="ITransitionDetailed"/>]
            /// Indicates what the value of <see cref="AnimancerState.IsLooping"/> will be for the created state.
            /// Returns false unless overridden.
            /// </summary>
            public virtual bool IsLooping { get { return false; } }

            /// <summary>[<see cref="ITransitionDetailed"/>]
            /// Determines what <see cref="NormalizedTime"/> to start the animation at.
            /// Returns <see cref="float.NaN"/> unless overridden.
            /// </summary>
            public virtual float NormalizedStartTime { get { return float.NaN; } set { } }

            /// <summary>[<see cref="ITransitionDetailed"/>]
            /// Determines how fast the animation plays (1x = normal speed).
            /// Returns 1 unless overridden.
            /// </summary>
            public virtual float Speed { get { return 1; } set { } }

            /// <summary>[<see cref="ITransitionDetailed"/>]
            /// The maximum amount of time the animation is expected to take (in seconds).
            /// </summary>
            public abstract float MaximumDuration { get; }

            /************************************************************************************************************************/

            [SerializeField, Tooltip(Strings.ProOnlyTag + "Events which will be triggered as the animation plays")]
            private AnimancerEvent.Sequence.Serializable _Events;

            /// <summary>[<see cref="SerializeField"/>] Events which will be triggered as the animation plays.</summary>
            public AnimancerEvent.Sequence.Serializable Events
            {
                get { return _Events; }
                set { _Events = value; }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// The state that was created by this object. Specifically, this is the state that was most recently
            /// passed into <see cref="Apply"/> (usually by <see cref="AnimancerPlayable.Play(ITransition)"/>).
            /// <para></para>
            /// You can use <see cref="AnimancerPlayable.StateDictionary.GetOrCreate(ITransition)"/> or
            /// <see cref="AnimancerLayer.GetOrCreateState(ITransition)"/> to get or create the state for a
            /// specific object.
            /// <para></para>
            /// <see cref="State"/> is simply a shorthand for casting this to <typeparamref name="TState"/>.
            /// </summary>
            public AnimancerState BaseState { get; private set; }

            /************************************************************************************************************************/

            private TState _State;

            /// <summary>
            /// The state that was created by this object. Specifically, this is the state that was most recently
            /// passed into <see cref="Apply"/> (usually by <see cref="AnimancerPlayable.Play(ITransition)"/>).
            /// <para></para>
            /// You can use <see cref="AnimancerPlayable.StateDictionary.GetOrCreate(ITransition)"/> or
            /// <see cref="AnimancerLayer.GetOrCreateState(ITransition)"/> to get or create the state for a
            /// specific object.
            /// <para></para>
            /// This property is shorthand for casting the <see cref="BaseState"/> to <typeparamref name="TState"/>.
            /// </summary>
            /// <exception cref="InvalidCastException">
            /// Thrown if the <see cref="BaseState"/> is not actually a <typeparamref name="TState"/>. This should only
            /// happen if a different type of state was created by something else and registered using the
            /// <see cref="Key"/>, causing this <see cref="AnimancerPlayable.Play(ITransition)"/> to pass that
            /// state into <see cref="Apply"/> instead of calling <see cref="CreateState"/> to make the correct type of
            /// state.
            /// </exception>
            public TState State
            {
                get
                {
                    if (_State == null)
                        _State = (TState)BaseState;

                    return _State;
                }
                protected set
                {
                    BaseState = _State = value;
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// The <see cref="AnimancerState.Key"/> which the created state will be registered with.
            /// <para></para>
            /// By default, a transition is used as its own <see cref="Key"/>, but this property can be overridden.
            /// </summary>
            public virtual object Key { get { return this; } }

            /// <summary>
            /// When a transition is passed into <see cref="AnimancerPlayable.Play(ITransition)"/>, this property
            /// determines which <see cref="Animancer.FadeMode"/> will be used.
            /// </summary>
            public virtual FadeMode FadeMode { get { return FadeMode.FixedSpeed; } }

            /// <summary>
            /// Creates and returns a new <typeparamref name="TState"/> connected to the `layer`.
            /// </summary>
            public abstract TState CreateState(AnimancerLayer layer);

            /// <summary>
            /// Creates and returns a new <typeparamref name="TState"/> connected to the `layer`.
            /// </summary>
            AnimancerState ITransition.CreateState(AnimancerLayer layer)
            {
                return CreateState(layer);
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="ITransition"/>]
            /// Called by <see cref="AnimancerPlayable.Play(ITransition)"/> to set the <see cref="BaseState"/>
            /// and apply any other modifications to the `state`.
            /// </summary>
            /// <remarks>
            /// This method also clears the <see cref="State"/> if necessary, so it will re-cast the
            /// <see cref="BaseState"/> when it gets accessed again.
            /// </remarks>
            public virtual void Apply(AnimancerState state)
            {
                state.Events = _Events;

                BaseState = state;

                if (_State != state)
                    _State = null;
            }

            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only] Don't use Inspector Gadgets Nested Object Drawers.</summary>
            private const bool NestedObjectDrawers = false;

            /************************************************************************************************************************/

            /// <summary>[Editor-Only] Adds context menu functions for this transition.</summary>
            void ITransitionDetailed.AddItemsToContextMenu(GenericMenu menu, SerializedProperty property,
                Editor.Serialization.PropertyAccessor accessor)
            {
                AddItemsToContextMenu(menu, property, accessor);
            }

            /// <summary>[Editor-Only] Adds context menu functions for this transition.</summary>
            protected virtual void AddItemsToContextMenu(GenericMenu menu, SerializedProperty property,
                Editor.Serialization.PropertyAccessor accessor)
            {
                var transition = (Transition<TState>)accessor.GetValue(property);

                const string EventsPrefix = "Transition Event Details/";
                int timeCount, callbackCount;
                AnimancerEvent.Sequence.Serializable.GetDetails(transition._Events, out timeCount, out callbackCount);
                menu.AddDisabledItem(new GUIContent(EventsPrefix + "Serialized Time Count: " + timeCount));
                menu.AddDisabledItem(new GUIContent(EventsPrefix + "Serialized Callback Count: " + callbackCount));

                Editor.Serialization.AddPropertyModifierFunction(menu, property, "Reset Transition", Reset);
            }

            /************************************************************************************************************************/

            private static void Reset(SerializedProperty property)
            {
                var transition = Editor.Serialization.GetValue(property);
                if (transition == null)
                    return;

                const System.Reflection.BindingFlags Bindings =
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance;

                var type = transition.GetType();
                var constructor = type.GetConstructor(Bindings, null, Type.EmptyTypes, null);
                if (constructor == null)
                {
                    Debug.LogError("Parameterless constructor not found in " + type);
                    return;
                }

                Editor.Serialization.RecordUndo(property);

                constructor.Invoke(transition, null);

                Editor.Serialization.OnPropertyChanged(property);
            }

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

