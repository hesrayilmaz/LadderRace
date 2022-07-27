// Animancer // Copyright 2020 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// Base class for <see cref="AnimancerState"/>s which blend other states together.
    /// </summary>
    public abstract partial class MixerState : AnimancerState
    {
        /************************************************************************************************************************/
        #region Properties
        /************************************************************************************************************************/

        /// <summary>The number of input ports in the <see cref="Playable"/>.</summary>
        public abstract int PortCount { get; }

        /************************************************************************************************************************/

        /// <summary>Mixers should keep child playables connected to the graph at all times.</summary>
        public override bool KeepChildrenConnected { get { return true; } }

        /// <summary>An <see cref="MixerState"/> has no <see cref="AnimationClip"/>.</summary>
        public override AnimationClip Clip { get { return null; } }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the collection of states connected to this mixer. Note that some elements may be null.
        /// <para></para>
        /// Getting an enumerator that automatically skips over null states is slower and creates garbage, so
        /// internally we use this property and perform null checks manually even though it increases the code
        /// complexity a bit.
        /// </summary>
        public abstract IList<AnimancerState> ChildStates { get; }

        /// <summary>Returns an enumerator which will iterate through each state connected to this mixer.</summary>
        public override IEnumerator<AnimancerState> GetEnumerator()
        {
            var childStates = ChildStates;
            if (childStates == null)
                yield break;

            var count = childStates.Count;
            for (int i = 0; i < count; i++)
            {
                var state = childStates[i];
                if (state == null)
                    continue;

                yield return state;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Indicates whether the weights of all child states should be recalculated.</summary>
        public bool WeightsAreDirty { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// Determines whether the states in this mixer are playing.
        /// </summary>
        public override bool IsPlaying
        {
            get { return base.IsPlaying; }
            set
            {
                base.IsPlaying = value;

                var childStates = ChildStates;
                if (childStates == null)
                    return;

                var count = childStates.Count;
                for (int i = 0; i < count; i++)
                {
                    var state = childStates[i];
                    if (state == null)
                        continue;

                    state.IsPlaying = value;
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if any child state is looping.</summary>
        public override bool IsLooping
        {
            get
            {
                var childStates = ChildStates;
                if (childStates == null)
                    return false;

                var count = childStates.Count;
                for (int i = 0; i < count; i++)
                {
                    var state = childStates[i];
                    if (state == null)
                        continue;

                    if (state.IsLooping)
                        return true;
                }

                return false;
            }
        }

        /************************************************************************************************************************/

        private bool[] _SynchroniseChildren;

        /// <summary>
        /// Indicates which children should have their <see cref="AnimancerNode.Speed"/> modified in order to keep
        /// their <see cref="AnimancerState.NormalizedTime"/> at approximately the same value.
        /// <para></para>
        /// The array can be null or empty. Any elements not in the array will be treated as true.
        /// </summary>
        /// <remarks>
        /// The <see cref="AnimancerNode.Speed"/> is modified to allow each state to trigger its events properly
        /// where setting the <see cref="AnimancerState.NormalizedTime"/> directly would prevent any events.
        /// </remarks>
        public bool[] SynchroniseChildren
        {
            get { return _SynchroniseChildren; }
            set
            {
                if (value == null || value.Length == 0)
                {
                    _SynchroniseChildren = null;
                }
                else
                {
                    // Reset the speed of any children that are no longer synced to 1.
                    var childStates = ChildStates;
                    if (childStates != null)
                    {
                        var count = Mathf.Min(childStates.Count, value.Length);
                        for (int i = 0; i < count; i++)
                        {
                            if (value[i])
                                continue;

                            var state = childStates[i];
                            if (state == null)
                                continue;

                            state.Speed = 1;
                        }
                    }

                    _SynchroniseChildren = value;
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Initialisation
        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="MixerState"/> without connecting it to the <see cref="PlayableGraph"/>.
        /// </summary>
        public MixerState(AnimancerPlayable root)
            : base(root)
        {
            _Playable = AnimationMixerPlayable.Create(root._Graph, 0, true);
        }

        /// <summary>
        /// Constructs a new <see cref="MixerState"/> and connects it to the `layer`.
        /// </summary>
        public MixerState(AnimancerLayer layer)
            : this(layer.Root)
        {
            layer.AddChild(this);
        }

        /// <summary>
        /// Constructs a new <see cref="MixerState"/> and connects it to the `parent` at the specified
        /// `index`.
        /// </summary>
        public MixerState(AnimancerNode parent, int index)
            : this(parent.Root)
        {
            SetParent(parent, index);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates and returns a new <see cref="ClipState"/> to play the `clip` with this
        /// <see cref="MixerState"/> as its parent.
        /// </summary>
        public virtual ClipState CreateState(int index, AnimationClip clip)
        {
            return new ClipState(this, index, clip)
            {
                IsPlaying = IsPlaying,
                Time = Time
            };
        }

        /************************************************************************************************************************/

        /// <summary>The number of states using this mixer as their <see cref="AnimancerState.Parent"/>.</summary>
        public override int ChildCount { get { return ChildStates.Count; } }

        /// <summary>
        /// Returns the state connected to the specified `index` as a child of this mixer.
        /// </summary>
        public override AnimancerState GetChild(int index)
        {
            return ChildStates[index];
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Updates
        /************************************************************************************************************************/

        /// <summary>
        /// Updates the time of this mixer and all of its child states.
        /// </summary>
        protected internal override void Update(out bool needsMoreUpdates)
        {
            base.Update(out needsMoreUpdates);

            var childStates = ChildStates;
            if (childStates == null)
                return;

            if (WeightsAreDirty && Weight != 0)
            {
                RecalculateWeights();

                Debug.Assert(!WeightsAreDirty,
                    "MixerState.AreWeightsDirty has not been set to false by RecalculateWeights().");

                // We need to apply the child weights immediately to ensure they are all in sync. Otherwise some of the
                // child states might have already been updated before the mixer and would not apply it until next frame.
                var count = childStates.Count;
                for (int i = 0; i < count; i++)
                {
                    var state = childStates[i];
                    if (state == null)
                        continue;

                    state.ApplyWeight();
                }
            }

            ApplySynchroniseChildren(ref needsMoreUpdates);
        }

        /************************************************************************************************************************/

        /// <summary>Applies the effects of <see cref="SynchroniseChildren"/>.</summary>
        protected void ApplySynchroniseChildren(ref bool needsMoreUpdates)
        {
            if (AnimancerPlayable.DeltaTime == 0)
                return;

            var childStates = ChildStates;
            var childCount = childStates.Count;
            if (childStates == null)
                return;

            // Get the number of elements in the array.
            int flagCount;
            if (_SynchroniseChildren == null)
            {
                flagCount = 0;
            }
            else
            {
                flagCount = _SynchroniseChildren.Length;
                if (flagCount >= childCount)
                {
                    for (int i = 0; i < flagCount; i++)
                    {
                        if (_SynchroniseChildren[i])
                            goto Continue;
                    }

                    // If none of the flags are true, do nothing.
                    return;
                }
            }

            Continue:
            needsMoreUpdates = true;

            // Calculate the weighted average normalized time of all children.

            var totalWeight = 0f;
            var weightedNormalizedTime = 0f;
            var weightedLength = 0f;

            for (int i = 0; i < childCount; i++)
            {
                if (i < flagCount && !_SynchroniseChildren[i])
                    continue;

                var state = childStates[i];
                if (state == null)
                    continue;

                var weight = state.Weight;
                if (weight == 0)
                    continue;

                totalWeight += weight;
                weightedNormalizedTime += state.NormalizedTime * weight;
                weightedLength += state.Length * weight;
            }

            if (totalWeight <= 0 || weightedLength <= 0)
                return;

            // Increment that time value according to delta time.

            weightedNormalizedTime /= totalWeight;
            weightedNormalizedTime += AnimancerPlayable.DeltaTime * totalWeight / weightedLength;

            // Modify the speed of all children to go from their current normalized time to the average in one frame.

            var inverseDeltaTime = 1f / AnimancerPlayable.DeltaTime;

            for (int i = 0; i < childCount; i++)
            {
                if (i < flagCount && !_SynchroniseChildren[i])
                    continue;

                var state = childStates[i];
                if (state == null)
                    continue;

                state.Speed = (weightedNormalizedTime - state.NormalizedTime) * state.Length * inverseDeltaTime;
            }

            // After this, all the playables will update and advance according to their new speeds this frame.
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Recalculates the weights of all <see cref="ManualMixerState.States"/> based on the current value of the
        /// <see cref="MixerState{TParameter}.Parameter"/> and the thresholds.
        /// <para></para>
        /// Overrides of this method must set <see cref="WeightsAreDirty"/> = false.
        /// </summary>
        public abstract void RecalculateWeights();

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inverse Kinematics
        /************************************************************************************************************************/

        /// <summary>
        /// Determines whether <c>OnAnimatorIK(int layerIndex)</c> will be called on the animated object for any
        /// <see cref="ChildStates"/>.
        /// The initial value is determined by <see cref="AnimancerLayer.DefaultApplyAnimatorIK"/>.
        /// <para></para>
        /// This is equivalent to the "IK Pass" toggle in Animator Controller layers.
        /// <para></para>
        /// It requires Unity 2018.1 or newer, however 2018.3 or newer is recommended because a bug in earlier versions
        /// of the Playables API caused this value to only take effect while a state was at
        /// <see cref="AnimancerNode.Weight"/> == 1 which meant that IK would not work while fading between animations.
        /// </summary>
        public override bool ApplyAnimatorIK
        {
            get
            {
                var childStates = ChildStates;
                if (childStates == null)
                    return false;

                var count = childStates.Count;
                for (int i = 0; i < count; i++)
                {
                    var state = childStates[i];
                    if (state == null)
                        continue;

                    if (state.ApplyAnimatorIK)
                        return true;
                }

                return false;
            }
            set
            {
                var childStates = ChildStates;
                if (childStates == null)
                    return;

                var count = childStates.Count;
                for (int i = 0; i < count; i++)
                {
                    var state = childStates[i];
                    if (state == null)
                        continue;

                    state.ApplyAnimatorIK = value;
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether any of the <see cref="ChildStates"/> in this mixer are applying IK to the character's feet.
        /// The initial value is determined by <see cref="AnimancerLayer.DefaultApplyFootIK"/>.
        /// <para></para>
        /// This is equivalent to the "Foot IK" toggle in Animator Controller states.
        /// </summary>
        public override bool ApplyFootIK
        {
            get
            {
                var childStates = ChildStates;
                if (childStates == null)
                    return false;

                var count = childStates.Count;
                for (int i = 0; i < count; i++)
                {
                    var state = childStates[i];
                    if (state == null)
                        continue;

                    if (state.ApplyFootIK)
                        return true;
                }

                return false;
            }
            set
            {
                var childStates = ChildStates;
                if (childStates == null)
                    return;

                var count = childStates.Count;
                for (int i = 0; i < count; i++)
                {
                    var state = childStates[i];
                    if (state == null)
                        continue;

                    state.ApplyFootIK = value;
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Other Methods
        /************************************************************************************************************************/

        /// <summary>
        /// Sets <see cref="AnimancerState.Time"/> for all <see cref="ChildStates"/>.
        /// </summary>
        public void SetChildrenTime(float value, bool normalized = false)
        {
            var childStates = ChildStates;
            if (childStates == null)
                return;

            var count = childStates.Count;
            for (int i = 0; i < count; i++)
            {
                var state = childStates[i];
                if (state == null)
                    continue;

                if (normalized)
                    state.NormalizedTime = value;
                else
                    state.Time = value;
            }
        }

        /************************************************************************************************************************/

        /// <summary>The average velocity of the root motion caused by this state.</summary>
        public override Vector3 AverageVelocity
        {
            get
            {
                var velocity = default(Vector3);

                var childStates = ChildStates;
                if (childStates != null)
                {
                    var count = childStates.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var state = childStates[i];
                        if (state == null)
                            continue;

                        velocity += state.AverageVelocity * state.Weight;
                    }
                }

                return velocity;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Recalculates the <see cref="AnimancerState.Duration"/> of all child states so that they add up to 1.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown if there are any states with no <see cref="Clip"/>.</exception>
        public void NormalizeDurations()
        {
            var childStates = ChildStates;
            if (childStates == null)
                return;

            int divideBy = 0;
            float totalDuration = 0f;

            // Count the number of states that exist and their total duration.
            var count = childStates.Count;
            for (int i = 0; i < count; i++)
            {
                var state = childStates[i];
                if (state == null)
                    continue;

                divideBy++;
                totalDuration += state.Duration;
            }

            // Calculate the average duration.
            totalDuration /= divideBy;

            // Set all states to that duration.
            for (int i = 0; i < count; i++)
            {
                var state = childStates[i];
                if (state == null)
                    continue;

                state.Duration = totalDuration;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Gets a user-friendly key to identify the `state` in the Inspector.</summary>
        public abstract string GetDisplayKey(AnimancerState state);

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a string describing the type of this mixer and the name of <see cref="Clip"/>s connected to it.
        /// </summary>
        public override string ToString()
        {
            var name = new StringBuilder();
            name.Append(GetType().Name);
            name.Append(" (");
            bool first = true;

            var childStates = ChildStates;
            if (childStates == null)
            {
                name.Append("null");
            }
            else
            {
                var count = childStates.Count;
                for (int i = 0; i < count; i++)
                {
                    var state = childStates[i];
                    if (state == null)
                        continue;

                    if (first)
                        first = false;
                    else
                        name.Append(", ");

                    if (state.Clip != null)
                        name.Append(state.Clip.name);
                    else
                        name.Append(state);
                }
            }
            name.Append(")");
            return name.ToString();
        }

        /************************************************************************************************************************/

        /// <summary>[<see cref="IAnimationClipCollection"/>]
        /// Gathers all the animations in this state.
        /// </summary>
        public override void GatherAnimationClips(ICollection<AnimationClip> clips)
        {
            clips.GatherFromSources((IList)ChildStates);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inspector
        /************************************************************************************************************************/

        /// <summary>The number of parameters being managed by this state.</summary>
        protected virtual int ParameterCount { get { return 0; } }

        /// <summary>Returns the name of a parameter being managed by this state.</summary>
        /// <exception cref="NotSupportedException">Thrown if this state doesn't manage any parameters.</exception>
        protected virtual string GetParameterName(int index) { throw new NotSupportedException(); }

        /// <summary>Returns the type of a parameter being managed by this state.</summary>
        /// <exception cref="NotSupportedException">Thrown if this state doesn't manage any parameters.</exception>
        protected virtual UnityEngine.AnimatorControllerParameterType GetParameterType(int index) { throw new NotSupportedException(); }

        /// <summary>Returns the value of a parameter being managed by this state.</summary>
        /// <exception cref="NotSupportedException">Thrown if this state doesn't manage any parameters.</exception>
        protected virtual object GetParameterValue(int index) { throw new NotSupportedException(); }

        /// <summary>Sets the value of a parameter being managed by this state.</summary>
        /// <exception cref="NotSupportedException">Thrown if this state doesn't manage any parameters.</exception>
        protected virtual void SetParameterValue(int index, object value) { throw new NotSupportedException(); }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns a <see cref="Drawer"/> for this state.</summary>
        protected internal override Editor.IAnimancerNodeDrawer GetDrawer()
        {
            return new Drawer(this);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws the Inspector GUI for an <see cref="ControllerState"/>.</summary>
        public sealed class Drawer : Editor.ParametizedAnimancerStateDrawer<MixerState>
        {
            /************************************************************************************************************************/

            /// <summary>
            /// Constructs a new <see cref="Drawer"/> to manage the Inspector GUI for the `state`.
            /// </summary>
            public Drawer(MixerState state) : base(state) { }

            /************************************************************************************************************************/

            /// <summary>The number of parameters being managed by the target state.</summary>
            public override int ParameterCount { get { return Target.ParameterCount; } }

            /// <summary>Returns the name of a parameter being managed by the target state.</summary>
            /// <exception cref="NotSupportedException">Thrown if the target state doesn't manage any parameters.</exception>
            public override string GetParameterName(int index) { return Target.GetParameterName(index); }

            /// <summary>Returns the type of a parameter being managed by the target state.</summary>
            /// <exception cref="NotSupportedException">Thrown if the target state doesn't manage any parameters.</exception>
            public override UnityEngine.AnimatorControllerParameterType GetParameterType(int index) { return Target.GetParameterType(index); }

            /// <summary>Returns the value of a parameter being managed by the target state.</summary>
            /// <exception cref="NotSupportedException">Thrown if the target state doesn't manage any parameters.</exception>
            public override object GetParameterValue(int index) { return Target.GetParameterValue(index); }

            /// <summary>Sets the value of a parameter being managed by the target state.</summary>
            /// <exception cref="NotSupportedException">Thrown if the target state doesn't manage any parameters.</exception>
            public override void SetParameterValue(int index, object value) { Target.SetParameterValue(index, value); }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Transition
        /************************************************************************************************************************/

        /// <summary>
        /// Base class for serializable <see cref="ITransition"/>s which can create a particular type of
        /// <see cref="MixerState{TParameter}"/> when passed into
        /// <see cref="AnimancerPlayable.Play(ITransition)"/>.
        /// </summary>
        /// <remarks>
        /// Unfortunately the tool used to generate this documentation does not currently support nested types with
        /// identical names, so only one <c>Transition</c> class will actually have a documentation page.
        /// <para></para>
        /// Even though it has the <see cref="SerializableAttribute"/>, this class won't actually get serialized
        /// by Unity because it's generic and abstract. Each child class still needs to include the attribute.
        /// </remarks>
        [Serializable]
        public abstract class Transition<TMixer, TParameter> : ManualMixerState.Transition<TMixer>
            where TMixer : MixerState<TParameter>
        {
            /************************************************************************************************************************/

            [SerializeField, HideInInspector]
            private TParameter[] _Thresholds;

            /// <summary>[<see cref="SerializeField"/>]
            /// The parameter values at which each of the states are used and blended.
            /// </summary>
            public TParameter[] Thresholds
            {
                get { return _Thresholds; }
                set { _Thresholds = value; }
            }

            /************************************************************************************************************************/

            [SerializeField]
            private TParameter _DefaultParameter;

            /// <summary>[<see cref="SerializeField"/>]
            /// The initial parameter value to give the mixer when it is first created.
            /// </summary>
            public TParameter DefaultParameter
            {
                get { return _DefaultParameter; }
                set { _DefaultParameter = value; }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Initialises the <see cref="AnimancerState.Transition{TState}.State"/> immediately after it is created.
            /// </summary>
            public override void InitialiseState()
            {
                base.InitialiseState();

                State.SetThresholds(_Thresholds);
                State.Parameter = _DefaultParameter;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Transition 2D
        /************************************************************************************************************************/

        /// <summary>
        /// A serializable <see cref="ITransition"/> which can create a <see cref="CartesianMixerState"/> or
        /// <see cref="DirectionalMixerState"/> when passed into
        /// <see cref="AnimancerPlayable.Play(ITransition)"/>.
        /// </summary>
        [Serializable]
        public class Transition2D : Transition<MixerState<Vector2>, Vector2>
        {
            /************************************************************************************************************************/

            /// <summary>
            /// A type of <see cref="MixerState"/> that can be created by a <see cref="Transition2D"/>.
            /// </summary>
            public enum MixerType
            {
                /// <summary><see cref="CartesianMixerState"/></summary>
                Cartesian,

                /// <summary><see cref="DirectionalMixerState"/></summary>
                Directional,
            }

            [SerializeField]
            private MixerType _Type;

            /// <summary>[<see cref="SerializeField"/>]
            /// The type of <see cref="MixerState"/> that this transition will create.
            /// </summary>
            public MixerType Type
            {
                get { return _Type; }
                set { _Type = value; }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates and returns a new <see cref="CartesianMixerState"/> or <see cref="DirectionalMixerState"/>
            /// depending on the <see cref="Type"/> connected to the `layer`.
            /// <para></para>
            /// This method also assigns it as the <see cref="AnimancerState.Transition{TState}.State"/>.
            /// </summary>
            public override MixerState<Vector2> CreateState(AnimancerLayer layer)
            {
                switch (_Type)
                {
                    case MixerType.Cartesian: State = new CartesianMixerState(layer); break;
                    case MixerType.Directional: State = new DirectionalMixerState(layer); break;
                    default: throw new ArgumentOutOfRangeException("_Type");
                }
                InitialiseState();
                return State;
            }

            /************************************************************************************************************************/
            #region Drawer
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only] Adds context menu functions for this transition.</summary>
            protected override void AddItemsToContextMenu(GenericMenu menu, SerializedProperty property,
                Editor.Serialization.PropertyAccessor accessor)
            {
                base.AddItemsToContextMenu(menu, property, accessor);
                Drawer.AddItemsToContextMenu(menu);
            }

            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws the Inspector GUI for a <see cref="Vector2"/> <see cref="Transition{TMixer, TParameter}"/>.
            /// </summary>
            [CustomPropertyDrawer(typeof(Transition2D), true)]
            public class Drawer : TransitionDrawer
            {
                /************************************************************************************************************************/

                /// <summary>
                /// Constructs a new <see cref="Drawer"/> using the a wider `thresholdWidth` than usual to accomodate
                /// both the X and Y values.
                /// </summary>
                public Drawer() : base(ThresholdLabelWidth * 2 + 20) { }

                /************************************************************************************************************************/

                /// <summary>
                /// Fills the `menu` with functions relevant to the `rootProperty`.
                /// </summary>
                public static void AddItemsToContextMenu(GenericMenu menu)
                {
                    AddPropertyModifierFunction(menu, "Initialise Standard 4 Directions", InitialiseStandard4Directions);
                    AddThresholdItemsToContextMenu(menu);
                }

                /// <summary>
                /// Fills the `menu` with functions relevant to the `rootProperty`.
                /// </summary>
                public static void AddThresholdItemsToContextMenu(GenericMenu menu, string prefix = "Calculate Thresholds")
                {
                    var fromVelocity = "From Velocity";
                    if (prefix != null)
                        fromVelocity = prefix + "/" + fromVelocity;

                    AddCalculateThresholdsFunction(menu, fromVelocity + "/XY", (clip, threshold) =>
                    {
                        var velocity = clip.averageSpeed;
                        return new Vector2(velocity.x, velocity.y);
                    });

                    AddCalculateThresholdsFunction(menu, fromVelocity + "/XZ", (clip, threshold) =>
                    {
                        var velocity = clip.averageSpeed;
                        return new Vector2(velocity.x, velocity.z);
                    });

                    AddCalculateThresholdsFunctionPerAxis(menu, prefix, "From Speed",
                        (clip, threshold) => clip.apparentSpeed);
                    AddCalculateThresholdsFunctionPerAxis(menu, prefix, "From Velocity X",
                        (clip, threshold) => clip.averageSpeed.x);
                    AddCalculateThresholdsFunctionPerAxis(menu, prefix, "From Velocity Y",
                        (clip, threshold) => clip.averageSpeed.z);
                    AddCalculateThresholdsFunctionPerAxis(menu, prefix, "From Velocity Z",
                        (clip, threshold) => clip.averageSpeed.z);
                    AddCalculateThresholdsFunctionPerAxis(menu, prefix, "From Angular Speed (Rad)",
                        (clip, threshold) => clip.averageAngularSpeed * Mathf.Deg2Rad);
                    AddCalculateThresholdsFunctionPerAxis(menu, prefix, "From Angular Speed (Deg)",
                        (clip, threshold) => clip.averageAngularSpeed);
                }

                /************************************************************************************************************************/

                private static void InitialiseStandard4Directions(SerializedProperty property)
                {
                    var oldSpeedCount = CurrentSpeeds.arraySize;

                    CurrentClips.arraySize = CurrentThresholds.arraySize = CurrentSpeeds.arraySize = 5;
                    CurrentThresholds.GetArrayElementAtIndex(0).vector2Value = Vector2.zero;
                    CurrentThresholds.GetArrayElementAtIndex(1).vector2Value = Vector2.up;
                    CurrentThresholds.GetArrayElementAtIndex(2).vector2Value = Vector2.right;
                    CurrentThresholds.GetArrayElementAtIndex(3).vector2Value = Vector2.down;
                    CurrentThresholds.GetArrayElementAtIndex(4).vector2Value = Vector2.left;

                    InitialiseSpeeds(oldSpeedCount);

                    var type = property.FindPropertyRelative("_Type");
                    type.enumValueIndex = (int)MixerType.Directional;
                }

                /************************************************************************************************************************/

                private static void AddCalculateThresholdsFunction(GenericMenu menu, string label,
                    Func<AnimationClip, Vector2, Vector2> calculateThreshold)
                {
                    AddPropertyModifierFunction(menu, label, (property) =>
                    {
                        var count = CurrentClips.arraySize;
                        for (int i = 0; i < count; i++)
                        {
                            var clip = CurrentClips.GetArrayElementAtIndex(i).objectReferenceValue as AnimationClip;
                            if (clip == null)
                                continue;

                            var threshold = CurrentThresholds.GetArrayElementAtIndex(i);
                            var value = calculateThreshold(clip, threshold.vector2Value);
                            if (!float.IsNaN(value.x) && !float.IsNaN(value.y))
                                threshold.vector2Value = value;
                        }
                    });
                }

                /************************************************************************************************************************/

                private static void AddCalculateThresholdsFunctionPerAxis(GenericMenu menu, string prefix, string label,
                    Func<AnimationClip, float, float> calculateThreshold)
                {
                    if (prefix != null)
                        prefix += " ";

                    AddCalculateThresholdsFunction(menu, prefix + "X/" + label, 0, calculateThreshold);
                    AddCalculateThresholdsFunction(menu, prefix + "Y/" + label, 1, calculateThreshold);
                }

                private static void AddCalculateThresholdsFunction(GenericMenu menu, string label, int axis,
                    Func<AnimationClip, float, float> calculateThreshold)
                {
                    AddPropertyModifierFunction(menu, label, (property) =>
                    {
                        var count = CurrentClips.arraySize;
                        for (int i = 0; i < count; i++)
                        {
                            var clip = CurrentClips.GetArrayElementAtIndex(i).objectReferenceValue as AnimationClip;
                            if (clip == null)
                                continue;

                            var threshold = CurrentThresholds.GetArrayElementAtIndex(i);

                            var value = threshold.vector2Value;
                            var newValue = calculateThreshold(clip, value[axis]);
                            if (!float.IsNaN(newValue))
                                value[axis] = newValue;
                            threshold.vector2Value = value;
                        }
                    });
                }

                /************************************************************************************************************************/

                /// <summary><see cref="AddThresholdItemsToMenu"/> will add some functions to the menu.</summary>
                protected override bool HasThresholdContextMenu { get { return true; } }

                /// <summary>Adds functions to the `menu` relating to the thresholds.</summary>
                protected override void AddThresholdItemsToMenu(GenericMenu menu)
                {
                    AddThresholdItemsToContextMenu(menu, null);
                }

                /************************************************************************************************************************/
            }

            /************************************************************************************************************************/
#endif
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Transition Drawer
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws the Inspector GUI for a <see cref="Transition{TMixer, TParameter}"/>.</summary>
        /// <remarks>
        /// This class would be nested inside <see cref="Transition{TMixer, TParameter}"/>, but the generic parameters
        /// cause problems in Unity 2019.3.
        /// </remarks>
        [CustomPropertyDrawer(typeof(Transition<,>), true)]
        public class TransitionDrawer : ManualMixerState.Transition.Drawer
        {
            /************************************************************************************************************************/

            /// <summary>
            /// The number of horizontal pixels the "Threshold" label occupies.
            /// </summary>
            private readonly float ThresholdWidth;

            private static float _ThresholdLabelWidth;

            /// <summary>
            /// The number of horizontal pixels the word "Threshold" label occupies when drawn with the standard
            /// <see cref="GUISkin.label"/> style.
            /// </summary>
            protected static float ThresholdLabelWidth
            {
                get
                {
                    if (_ThresholdLabelWidth == 0)
                        _ThresholdLabelWidth = Editor.AnimancerGUI.CalculateWidth(EditorStyles.popup, "Threshold");
                    return _ThresholdLabelWidth;
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Constructs a new <see cref="TransitionDrawer"/> using the default <see cref="ThresholdLabelWidth"/>.
            /// </summary>
            public TransitionDrawer() : this(ThresholdLabelWidth) { }

            /// <summary>
            /// Constructs a new <see cref="TransitionDrawer"/> using a custom width for its threshold labels.
            /// </summary>
            protected TransitionDrawer(float thresholdWidth)
            {
                ThresholdWidth = thresholdWidth;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// The serialized <see cref="MixerState.Transition{TMixer, TParameter}.Thresholds"/> of the
            /// <see cref="ManualMixerState.Transition.Drawer.CurrentProperty"/>.
            /// </summary>
            protected static SerializedProperty CurrentThresholds { get; private set; }

            /************************************************************************************************************************/

            /// <summary>
            /// Called every time a `property` is drawn to find the relevant child properties and store them to be
            /// used in <see cref="ManualMixerState.Transition.Drawer.GetPropertyHeight"/> and
            /// <see cref="ManualMixerState.Transition.Drawer.OnGUI"/>.
            /// </summary>
            protected override void GatherSubProperties(SerializedProperty property)
            {
                base.GatherSubProperties(property);

                CurrentThresholds = property.FindPropertyRelative("_Thresholds");

                var count = Math.Max(CurrentClips.arraySize, CurrentThresholds.arraySize);
                CurrentClips.arraySize = count;
                CurrentThresholds.arraySize = count;
                if (CurrentSpeeds.arraySize != 0)
                    CurrentSpeeds.arraySize = count;
            }

            /************************************************************************************************************************/

            /// <summary>Splits the specified `area` into separate sections.</summary>
            protected void SplitListRect(Rect area, out Rect animation, out Rect threshold, out Rect speed, out Rect sync)
            {
                SplitListRect(area, out animation, out speed, out sync);

                threshold = animation;

                var xMin = threshold.xMin = Math.Max(
                    EditorGUIUtility.labelWidth + Editor.AnimancerGUI.IndentSize,
                    threshold.xMax - ThresholdWidth);

                animation.xMax = xMin - Editor.AnimancerGUI.StandardSpacing;
            }

            /************************************************************************************************************************/

            /// <summary>Draws the headdings of the state list.</summary>
            protected override void DoStateListHeaderGUI(Rect area)
            {
                Rect animationArea, thresholdArea, speedArea, syncArea;
                SplitListRect(area, out animationArea, out thresholdArea, out speedArea, out syncArea);

                DoAnimationLabelGUI(animationArea);

                EditorGUI.BeginProperty(thresholdArea, GUIContent.none, CurrentThresholds);
                var content = Editor.AnimancerGUI.TempContent("Threshold",
                    "The parameter values at which each child state will be fully active");
                if (HasThresholdContextMenu)
                {
                    if (EditorGUI.DropdownButton(thresholdArea, content, FocusType.Passive))
                    {
                        var menu = new GenericMenu();
                        AddThresholdItemsToMenu(menu);
                        menu.ShowAsContext();
                    }
                }
                else
                {
                    GUI.Label(thresholdArea, content);
                }
                EditorGUI.EndProperty();

                DoSpeedLabelGUI(speedArea);

                DoSyncLabelGUI(syncArea);
            }

            /************************************************************************************************************************/

            /// <summary>Draws the GUI of the state at the specified `index`.</summary>
            protected override void DoElementGUI(Rect area, int index,
                SerializedProperty clip, SerializedProperty speed)
            {
                var threshold = CurrentThresholds.GetArrayElementAtIndex(index);

                Rect animationArea, thresholdArea, speedArea, syncArea;
                SplitListRect(area, out animationArea, out thresholdArea, out speedArea, out syncArea);

                DoElementGUI(animationArea, speedArea, syncArea, index, clip, speed);

                EditorGUI.PropertyField(thresholdArea, threshold, GUIContent.none);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Called when adding a new state to the list to ensure that any other relevant arrays have new
            /// elements added as well.
            /// </summary>
            protected override void OnAddElement(ReorderableList list)
            {
                var index = CurrentClips.arraySize;
                base.OnAddElement(list);
                CurrentThresholds.InsertArrayElementAtIndex(index);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Called when removing a state from the list to ensure that any other relevant arrays have elements
            /// removed as well.
            /// </summary>
            protected override void OnRemoveElement(ReorderableList list)
            {
                base.OnRemoveElement(list);
                RemoveArrayElement(CurrentThresholds, list.index);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Called when reordering states in the list to ensure that any other relevant arrays have their
            /// corresponding elements reordered as well.
            /// </summary>
            protected override void OnReorderList(ReorderableList list, int oldIndex, int newIndex)
            {
                base.OnReorderList(list, oldIndex, newIndex);
                CurrentThresholds.MoveArrayElement(oldIndex, newIndex);
            }

            /************************************************************************************************************************/

            /// <summary>Indicates whether <see cref="AddThresholdItemsToMenu"/> will add anything to the menu.</summary>
            protected virtual bool HasThresholdContextMenu { get { return false; } }

            /// <summary>Adds functions to the `menu` relating to the thresholds.</summary>
            protected virtual void AddThresholdItemsToMenu(GenericMenu menu) { }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        #endregion
        /************************************************************************************************************************/
    }
}

