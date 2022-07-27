// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// An <see cref="AnimancerState"/> which plays a <see cref="PlayableAsset"/>.
    /// </summary>
    public sealed class PlayableAssetState : AnimancerState
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>The <see cref="PlayableAsset"/> which this state plays.</summary>
        private PlayableAsset _Asset;

        /// <summary>The <see cref="PlayableAsset"/> which this state plays.</summary>
        public PlayableAsset Asset
        {
            get { return _Asset; }
            set
            {
                if (ReferenceEquals(_Asset, value))
                    return;

                if (ReferenceEquals(_Key, _Asset))
                    Key = value;

                if (_Playable.IsValid())
                    Root._Graph.DestroyPlayable(_Playable);

                CreatePlayable(value);
                SetWeightDirty();
            }
        }

        /// <summary>The <see cref="PlayableAsset"/> which this state plays.</summary>
        public override Object MainObject
        {
            get { return _Asset; }
            set { _Asset = (PlayableAsset)value; }
        }

        /************************************************************************************************************************/

        /// <summary>The <see cref="PlayableAsset.duration"/>.</summary>
        public override float Length { get { return (float)_Asset.duration; } }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Methods
        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="PlayableAssetState"/> to play the `asset` without connecting it to the
        /// <see cref="PlayableGraph"/>. You must call <see cref="AnimancerState.SetParent(AnimancerNode, int)"/> or it
        /// will not actually do anything.
        /// </summary>
        public PlayableAssetState(AnimancerPlayable root, PlayableAsset asset)
            : base(root)
        {

            CreatePlayable(asset);
        }

        /// <summary>
        /// Constructs a new <see cref="PlayableAssetState"/> to play the `asset` and connects it to a new port on the
        /// `layer`s <see cref="Playable"/>.
        /// </summary>
        public PlayableAssetState(AnimancerLayer layer, PlayableAsset asset)
            : this(layer.Root, asset)
        {
            layer.AddChild(this);
        }

        /// <summary>
        /// Constructs a new <see cref="PlayableAssetState"/> to play the `asset` and connects it to the `parent`s
        /// <see cref="Playable"/> at the specified `index`.
        /// </summary>
        public PlayableAssetState(AnimancerNode parent, int index, PlayableAsset asset)
            : this(parent.Root, asset)
        {
            SetParent(parent, index);
        }

        /************************************************************************************************************************/

        private void CreatePlayable(PlayableAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");

            _Asset = asset;
            _Playable = asset.CreatePlayable(Root._Graph, Root.Component.gameObject);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a string describing the type of this state and the name of the <see cref="Asset"/>.
        /// </summary>
        public override string ToString()
        {
            if (_Asset != null)
                return string.Concat(base.ToString(), " (", _Asset.name, ")");
            else
                return base.ToString() + " (null)";
        }

        /************************************************************************************************************************/

        /// <summary>Destroys the <see cref="Playable"/>.</summary>
        public override void Destroy()
        {
            _Asset = null;
            base.Destroy();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Transition
        /************************************************************************************************************************/

        /// <summary>
        /// A serializable <see cref="ITransition"/> which can create a <see cref="PlayableAssetState"/> when
        /// passed into <see cref="AnimancerPlayable.Play(ITransition)"/>.
        /// </summary>
        /// <remarks>
        /// Unfortunately the tool used to generate this documentation does not currently support nested types with
        /// identical names, so only one <c>Transition</c> class will actually have a documentation page.
        /// </remarks>
        [Serializable]
        public class Transition : Transition<PlayableAssetState>, IAnimationClipCollection
        {
            /************************************************************************************************************************/

            [SerializeField, Tooltip("The asset to play")]
            private PlayableAsset _Asset;

            /// <summary>[<see cref="SerializeField"/>] The asset to play.</summary>
            public PlayableAsset Asset
            {
                get { return _Asset; }
                set { _Asset = value; }
            }

            /// <summary>
            /// The <see cref="Asset"/> will be used as the <see cref="AnimancerState.Key"/> for the created state to
            /// be registered with.
            /// </summary>
            public override object Key { get { return _Asset; } }

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

            /// <summary>[<see cref="ITransitionDetailed"/>]
            /// The maximum amount of time the animation is expected to take (in seconds).
            /// </summary>
            public override float MaximumDuration
            {
                get
                {
                    return _Asset != null ? (float)_Asset.duration : 0;
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates and returns a new <see cref="PlayableAssetState"/> connected to the `layer`.
            /// <para></para>
            /// This method also assigns it as the <see cref="AnimancerState.Transition{TState}.State"/>.
            /// </summary>
            public override PlayableAssetState CreateState(AnimancerLayer layer)
            {
                return State = new PlayableAssetState(layer, _Asset);
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

            /// <summary>Gathers all the animations associated with this object.</summary>
            void IAnimationClipCollection.GatherAnimationClips(ICollection<AnimationClip> clips)
            {
                clips.GatherFromAsset(_Asset);
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
                public Drawer() : base("_Asset") { }

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

