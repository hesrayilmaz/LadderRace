// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>
    /// An <see cref="AnimancerState"/> which plays an <see cref="AnimationClip"/>.
    /// </summary>
    public sealed class ClipState : AnimancerState
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimationClip"/> which this state plays.</summary>
        private AnimationClip _Clip;

        /// <summary>The <see cref="AnimationClip"/> which this state plays.</summary>
        public override AnimationClip Clip
        {
            get { return _Clip; }
            set
            {
                if (ReferenceEquals(_Clip, value))
                    return;

                if (ReferenceEquals(_Key, _Clip))
                    Key = value;

                if (_Playable.IsValid())
                    Root._Graph.DestroyPlayable(_Playable);

                CreatePlayable(value);
                SetWeightDirty();
            }
        }

        /// <summary>The <see cref="AnimationClip"/> which this state plays.</summary>
        public override Object MainObject
        {
            get { return _Clip; }
            set { Clip = (AnimationClip)value; }
        }

        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimationClip.length"/>.</summary>
        public override float Length { get { return _Clip.length; } }

        /************************************************************************************************************************/

        /// <summary>The <see cref="Motion.isLooping"/>.</summary>
        public override bool IsLooping { get { return _Clip.isLooping; } }

        /************************************************************************************************************************/

        /// <summary>The average velocity of the root motion caused by this state.</summary>
        public override Vector3 AverageVelocity
        {
            get { return _Clip.averageSpeed; }
        }

        /************************************************************************************************************************/
        #region Inverse Kinematics
        /************************************************************************************************************************/

#if !UNITY_2018_1_OR_NEWER
        private const string IKNotSupported = "ApplyAnimatorIK is not supported by this version of Unity." +
            " Please upgrade to Unity 2018.1 or newer."
            ;
#endif

        /// <summary>
        /// Determines whether <c>OnAnimatorIK(int layerIndex)</c> will be called on the animated object.
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
#if UNITY_2018_1_OR_NEWER
            get { return ((AnimationClipPlayable)_Playable).GetApplyPlayableIK(); }
            set { ((AnimationClipPlayable)_Playable).SetApplyPlayableIK(value); }
#else
            get { throw new NotSupportedException(IKNotSupported); }
            set { throw new NotSupportedException(IKNotSupported); }
#endif
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether this state is applying IK to the character's feet.
        /// The initial value is determined by <see cref="AnimancerLayer.DefaultApplyFootIK"/>.
        /// <para></para>
        /// This is equivalent to the "Foot IK" toggle in Animator Controller states.
        /// </summary>
        public override bool ApplyFootIK
        {
            get { return ((AnimationClipPlayable)_Playable).GetApplyFootIK(); }
            set { ((AnimationClipPlayable)_Playable).SetApplyFootIK(value); }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Applies the default IK flags from the specified `layer`.
        /// </summary>
        private void InitialiseIKDefaults(AnimancerLayer layer)
        {
            // Foot IK is actually enabled by default so we disable it if necessary.
            if (!layer.DefaultApplyFootIK)
                ApplyFootIK = false;

#if UNITY_2018_1_OR_NEWER
            if (layer.DefaultApplyAnimatorIK)
                ApplyAnimatorIK = true;
#endif
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Methods
        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="ClipState"/> to play the `clip` without connecting it to the
        /// <see cref="PlayableGraph"/>. You must call <see cref="AnimancerState.SetParent(AnimancerNode, int)"/> or it
        /// will not actually do anything.
        /// </summary>
        public ClipState(AnimancerPlayable root, AnimationClip clip)
            : base(root)
        {
            CreatePlayable(clip);
        }

        /// <summary>
        /// Constructs a new <see cref="ClipState"/> to play the `clip` and connects it to a new port on the `layer`s
        /// <see cref="Playable"/>.
        /// </summary>
        public ClipState(AnimancerLayer layer, AnimationClip clip)
            : this(layer.Root, clip)
        {
            layer.AddChild(this);
            InitialiseIKDefaults(layer);
        }

        /// <summary>
        /// Constructs a new <see cref="ClipState"/> to play the `clip` and connects it to the `parent`s
        /// <see cref="Playable"/> at the specified `index`.
        /// </summary>
        public ClipState(AnimancerNode parent, int index, AnimationClip clip)
            : this(parent.Root, clip)
        {
            SetParent(parent, index);
            InitialiseIKDefaults(parent.Layer);
        }

        /************************************************************************************************************************/

        private void CreatePlayable(AnimationClip clip)
        {
            if (clip == null)
                throw new ArgumentNullException("clip");

            Validate.NotLegacy(clip);

            _Clip = clip;
            _Playable = AnimationClipPlayable.Create(Root._Graph, clip);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a string describing the type of this state and the name of the <see cref="Clip"/>.
        /// </summary>
        public override string ToString()
        {
            if (_Clip != null)
                return string.Concat(base.ToString(), " (", _Clip.name, ")");
            else
                return base.ToString() + " (null)";
        }

        /************************************************************************************************************************/

        /// <summary>Destroys the <see cref="Playable"/>.</summary>
        public override void Destroy()
        {
            _Clip = null;
            base.Destroy();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inspector
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns a <see cref="Drawer"/> for this state.</summary>
        protected internal override Editor.IAnimancerNodeDrawer GetDrawer()
        {
            return new Drawer(this);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws the Inspector GUI for a <see cref="ClipState"/>.</summary>
        public sealed class Drawer : Editor.AnimancerStateDrawer<ClipState>
        {
            /************************************************************************************************************************/

            /// <summary>Indicates whether the animation has an event called "End".</summary>
            private bool _HasEndEvent;

            /************************************************************************************************************************/

            /// <summary>
            /// Constructs a new <see cref="Drawer"/> to manage the Inspector GUI for the `state`.
            /// </summary>
            public Drawer(ClipState state) : base(state)
            {
                var events = state._Clip.events;
                for (int i = events.Length - 1; i >= 0; i--)
                {
                    if (events[i].functionName == "End")
                    {
                        _HasEndEvent = true;
                        break;
                    }
                }
            }

            /************************************************************************************************************************/

            /// <summary> Draws the details of the target state in the GUI.</summary>
            protected override void DoDetailsGUI(IAnimancerComponent owner)
            {
                base.DoDetailsGUI(owner);
                DoAnimationTypeWarningGUI(owner);
                DoEndEventWarningGUI();
            }

            /************************************************************************************************************************/

            private string _AnimationTypeWarning;
            private Animator _AnimationTypeWarningOwner;

            /// <summary>
            /// Validates the <see cref="Clip"/> type compared to the owner's <see cref="Animator"/> type.
            /// </summary>
            private void DoAnimationTypeWarningGUI(IAnimancerComponent owner)
            {
                // Validate the clip type compared to the owner.
                if (owner.Animator == null)
                {
                    _AnimationTypeWarning = null;
                    return;
                }

                if (_AnimationTypeWarningOwner != owner.Animator)
                {
                    _AnimationTypeWarning = null;
                    _AnimationTypeWarningOwner = owner.Animator;
                }

                if (_AnimationTypeWarning == null)
                {
                    var ownerAnimationType = Editor.AnimancerEditorUtilities.GetAnimationType(_AnimationTypeWarningOwner);
                    var clipAnimationType = Editor.AnimancerEditorUtilities.GetAnimationType(Target._Clip);

                    if (ownerAnimationType == clipAnimationType)
                    {
                        _AnimationTypeWarning = "";
                    }
                    else
                    {
                        var text = new StringBuilder()
                            .Append("Possible animation type mismatch:\n - Animator type is ")
                            .Append(ownerAnimationType)
                            .Append("\n - AnimationClip type is ")
                            .Append(clipAnimationType)
                            .Append("\nThis means that the clip may not work correctly," +
                                " however this check is not totally accurate. Click here for more info.");

                        _AnimationTypeWarning = text.ToString();
                    }
                }

                if (_AnimationTypeWarning != "")
                {
                    UnityEditor.EditorGUILayout.HelpBox(_AnimationTypeWarning, UnityEditor.MessageType.Warning);

                    if (Editor.AnimancerGUI.TryUseClickEventInLastRect())
                        UnityEditor.EditorUtility.OpenWithDefaultApp(
                            Strings.DocsURLs.AnimationTypes);
                }
            }

            /************************************************************************************************************************/

            private void DoEndEventWarningGUI()
            {
                if (_HasEndEvent && Target.Events.OnEnd == null && Target.TargetWeight != 0)
                {
                    UnityEditor.EditorGUILayout.HelpBox("This animation has an event called 'End'" +
                        " but no 'OnEnd' callback is currently registered for this state. Click here for more info.",
                        UnityEditor.MessageType.Warning);

                    if (Editor.AnimancerGUI.TryUseClickEventInLastRect())
                        UnityEditor.EditorUtility.OpenWithDefaultApp(
                            Strings.DocsURLs.EndEvents);
                }
            }

            /************************************************************************************************************************/

            /// <summary>Adds the details of this state to the menu.</summary>
            protected override void AddContextMenuFunctions(UnityEditor.GenericMenu menu)
            {
                menu.AddDisabledItem(new GUIContent(DetailsPrefix + "Animation Type: " +
                    Editor.AnimancerEditorUtilities.GetAnimationType(Target._Clip)));

                base.AddContextMenuFunctions(menu);

                menu.AddItem(new GUIContent("Inverse Kinematics/Apply Animator IK"),
                    Target.ApplyAnimatorIK,
                    () => Target.ApplyAnimatorIK = !Target.ApplyAnimatorIK);
                menu.AddItem(new GUIContent("Inverse Kinematics/Apply Foot IK"),
                    Target.ApplyFootIK,
                    () => Target.ApplyFootIK = !Target.ApplyFootIK);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        #endregion
        /************************************************************************************************************************/
        #region Transition
        /************************************************************************************************************************/

        /// <summary>
        /// A serializable <see cref="ITransition"/> which can create a <see cref="ClipState"/> when passed
        /// into <see cref="AnimancerPlayable.Play(ITransition)"/>.
        /// </summary>
        /// <remarks>
        /// Unfortunately the tool used to generate this documentation does not currently support nested types with
        /// identical names, so only one <c>Transition</c> class will actually have a documentation page.
        /// </remarks>
        [Serializable]
        public class Transition : Transition<ClipState>, IAnimationClipCollection
        {
            /************************************************************************************************************************/

            [SerializeField, Tooltip("The animation to play")]
            private AnimationClip _Clip;

            /// <summary>[<see cref="SerializeField"/>] The animation to play.</summary>
            public AnimationClip Clip
            {
                get { return _Clip; }
                set
                {
                    if (value != null)
                        Validate.NotLegacy(value);

                    _Clip = value;
                }
            }

            /// <summary>
            /// The <see cref="Clip"/> will be used as the <see cref="AnimancerState.Key"/> for the created state to be
            /// registered with.
            /// </summary>
            public override object Key { get { return _Clip; } }

            /************************************************************************************************************************/

            [SerializeField, Tooltip(Strings.ProOnlyTag +
                "How fast the animation plays (1x = normal speed, 2x = double speed)")]
            private float _Speed = 1;

            /// <summary>[<see cref="SerializeField"/>]
            /// Determines how fast the animation plays (1x = normal speed, 2x = double speed).
            /// </summary>
            public override float Speed
            {
                get { return _Speed; }
                set { _Speed = value; }
            }

            /************************************************************************************************************************/

            [SerializeField, Tooltip(Strings.ProOnlyTag + "If enabled, the animation's time will start at this value when played")]
            [UnityEngine.Serialization.FormerlySerializedAs("_StartTime")]
            private float _NormalizedStartTime = float.NaN;

            /// <summary>[<see cref="SerializeField"/>]
            /// Determines what <see cref="AnimancerState.NormalizedTime"/> to start the animation at.
            /// <para></para>
            /// The default value is <see cref="float.NaN"/> which indicates that this value is not used so the
            /// animation will continue from its current time.
            /// </summary>
            public override float NormalizedStartTime
            {
                get { return _NormalizedStartTime; }
                set { _NormalizedStartTime = value; }
            }

            /// <summary>
            /// If this transition will set the <see cref="AnimancerState.NormalizedTime"/>, then it needs to use
            /// <see cref="FadeMode.FromStart"/>.
            /// </summary>
            public override FadeMode FadeMode
            {
                get
                {
                    return float.IsNaN(_NormalizedStartTime) ? FadeMode.FixedSpeed : FadeMode.FromStart;
                }
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="ITransitionDetailed"/>] Returns <see cref="Motion.isLooping"/>.</summary>
            public override bool IsLooping
            {
                get
                {
                    return _Clip != null ? _Clip.isLooping : false;
                }
            }

            /// <summary>[<see cref="ITransitionDetailed"/>]
            /// The maximum amount of time the animation is expected to take (in seconds).
            /// </summary>
            public override float MaximumDuration
            {
                get
                {
                    return _Clip != null ? _Clip.length : 0;
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates and returns a new <see cref="ClipState"/> connected to the `layer`.
            /// <para></para>
            /// This method also assigns it as the <see cref="AnimancerState.Transition{TState}.State"/>.
            /// </summary>
            public override ClipState CreateState(AnimancerLayer layer)
            {
                return State = new ClipState(layer, _Clip);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Called by <see cref="AnimancerPlayable.Play(ITransition)"/> to apply the <see cref="Speed"/>
            /// and <see cref="NormalizedStartTime"/>.
            /// </summary>
            public override void Apply(AnimancerState state)
            {
                base.Apply(state);

                if (!float.IsNaN(_Speed))
                    state.Speed = _Speed;

                if (!float.IsNaN(_NormalizedStartTime))
                    state.NormalizedTime = _NormalizedStartTime;
                else if (state.Weight == 0)
                    state.NormalizedTime = AnimancerEvent.Sequence.GetDefaultNormalizedStartTime(_Speed);
            }

            /************************************************************************************************************************/

            /// <summary>Adds the <see cref="Clip"/> to the collection.</summary>
            void IAnimationClipCollection.GatherAnimationClips(ICollection<AnimationClip> clips)
            {
                clips.Gather(_Clip);
            }

            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only] Draws the Inspector GUI for a <see cref="Transition"/>.</summary>
            [UnityEditor.CustomPropertyDrawer(typeof(Transition), true)]
            public class Drawer : Editor.TransitionDrawer
            {
                /************************************************************************************************************************/

                /// <summary>Constructs a new <see cref="Drawer"/>.</summary>
                public Drawer() : base("_Clip") { }

                /************************************************************************************************************************/
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

