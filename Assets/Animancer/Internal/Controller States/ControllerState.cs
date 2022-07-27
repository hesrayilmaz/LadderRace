// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// An <see cref="AnimancerState"/> which plays a <see cref="RuntimeAnimatorController"/>.
    /// You can control this state very similarly to an <see cref="Animator"/> via its <see cref="Playable"/> field.
    /// </summary>
    public class ControllerState : AnimancerState
    {
        /************************************************************************************************************************/
        #region Fields and Auto-Properties
        /************************************************************************************************************************/

        private RuntimeAnimatorController _Controller;

        /// <summary>The <see cref="RuntimeAnimatorController"/> which this state plays.</summary>
        public RuntimeAnimatorController Controller
        {
            get { return _Controller; }
            set
            {
                if (ReferenceEquals(_Controller, value))
                    return;

                if (ReferenceEquals(_Key, value))
                    Key = value;

                if (_Playable.IsValid())
                    Root._Graph.DestroyPlayable(_Playable);

                CreatePlayable(value);
                SetWeightDirty();
            }
        }

        /// <summary>The <see cref="RuntimeAnimatorController"/> which this state plays.</summary>
        public override Object MainObject
        {
            get { return Controller; }
            set { Controller = (RuntimeAnimatorController)value; }
        }

        /// <summary>The internal system which plays the <see cref="RuntimeAnimatorController"/>.</summary>
        public AnimatorControllerPlayable Playable { get; private set; }

        /************************************************************************************************************************/

        /// <summary>
        /// If false, <see cref="Stop"/> will reset all layers to their default state. Default False.
        /// <para></para>
        /// If you set this value to false after the constructor, you must assign the <see cref="DefaultStateHashes"/>
        /// or call <see cref="GatherDefaultStates"/> yourself.
        /// </summary>
        public bool KeepStateOnStop { get; set; }

        /// <summary>
        /// The <see cref="AnimatorStateInfo.shortNameHash"/> of the default state on each layer, used to reset to
        /// those states when <see cref="Stop"/> is called.
        /// </summary>
        public int[] DefaultStateHashes { get; set; }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Public API
        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="ControllerState"/> to play the `animatorController` without connecting
        /// it to the <see cref="PlayableGraph"/>.
        /// </summary>
        protected ControllerState(AnimancerPlayable root, RuntimeAnimatorController animatorController,
            bool keepStateOnStop = false)
            : base(root)
        {
            KeepStateOnStop = keepStateOnStop;
            CreatePlayable(animatorController);
        }

        /// <summary>
        /// Constructs a new <see cref="ControllerState"/> to play the `animatorController` and connects it to
        /// the `layer`.
        /// </summary>
        public ControllerState(AnimancerLayer layer, RuntimeAnimatorController animatorController,
            bool keepStateOnStop = false)
            : this(layer.Root, animatorController, keepStateOnStop)
        {
            layer.AddChild(this);
        }

        /// <summary>
        /// Constructs a new <see cref="ControllerState"/> to play the `animatorController` and
        /// connects it to the `parent` at the specified `index`.
        /// </summary>
        public ControllerState(AnimancerNode parent, int index, RuntimeAnimatorController animatorController,
            bool keepStateOnStop = false)
            : this(parent.Root, animatorController, keepStateOnStop)
        {
            SetParent(parent, index);
        }

        /************************************************************************************************************************/

        private void CreatePlayable(RuntimeAnimatorController animatorController)
        {
            if (animatorController == null)
                throw new ArgumentNullException("animatorController");

            _Controller = animatorController;
            Playable = AnimatorControllerPlayable.Create(Root._Graph, animatorController);
            _Playable = Playable;

            if (!KeepStateOnStop)
                GatherDefaultStates();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The current state on layer 0, or the next state if it is currently in a transition.
        /// </summary>
        public AnimatorStateInfo StateInfo
        {
            get
            {
                return Playable.IsInTransition(0) ?
                    Playable.GetNextAnimatorStateInfo(0) :
                    Playable.GetCurrentAnimatorStateInfo(0);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="AnimatorStateInfo.normalizedTime"/> * <see cref="AnimatorStateInfo.length"/> of the
        /// <see cref="StateInfo"/>
        /// </summary>
        protected override float NewTime
        {
            get
            {
                var info = StateInfo;
                return info.normalizedTime * info.length;
            }
            set
            {
                Playable.PlayInFixedTime(0, 0, value);
            }
        }

        /************************************************************************************************************************/

        /// <summary>The current <see cref="AnimatorStateInfo.length"/> (on layer 0).</summary>
        public override float Length { get { return StateInfo.length; } }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether the current state on layer 0 will loop back to the start when it reaches the end.
        /// </summary>
        public override bool IsLooping { get { return StateInfo.loop; } }

        /************************************************************************************************************************/

        /// <summary>
        /// Gathers the <see cref="DefaultStateHashes"/> from the current states.
        /// </summary>
        public void GatherDefaultStates()
        {
            var layerCount = Playable.GetLayerCount();
            if (DefaultStateHashes == null || DefaultStateHashes.Length != layerCount)
                DefaultStateHashes = new int[layerCount];

            while (--layerCount >= 0)
                DefaultStateHashes[layerCount] = Playable.GetCurrentAnimatorStateInfo(layerCount).shortNameHash;
        }

        /// <summary>
        /// Calls the base <see cref="AnimancerState.Stop"/> and if <see cref="KeepStateOnStop"/> is false it also
        /// calls <see cref="ResetToDefaultStates"/>.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            if (!KeepStateOnStop)
                ResetToDefaultStates();
        }

        /// <summary>
        /// Resets all layers to their default state.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown if <see cref="DefaultStateHashes"/> is null.</exception>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown if the size of <see cref="DefaultStateHashes"/> is larger than the number of layers in the
        /// <see cref="Controller"/>.
        /// </exception>
        public void ResetToDefaultStates()
        {
            for (int i = 0; i < DefaultStateHashes.Length; i++)
                Playable.Play(DefaultStateHashes[i], i, 0);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a string describing the type of this state and the name of the <see cref="Controller"/>.
        /// </summary>
        public override string ToString()
        {
            if (_Controller != null)
                return string.Concat(base.ToString(), " (", _Controller.name, ")");
            else
                return base.ToString() + " (null)";
        }

        /************************************************************************************************************************/

        /// <summary>[<see cref="IAnimationClipCollection"/>]
        /// Gathers all the animations in this state.
        /// </summary>
        public override void GatherAnimationClips(ICollection<AnimationClip> clips)
        {
            if (_Controller != null)
                clips.Gather(_Controller.animationClips);
        }

        /************************************************************************************************************************/

        /// <summary>Destroys the <see cref="Playable"/>.</summary>
        public override void Destroy()
        {
            _Controller = null;
            base.Destroy();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Parameters
        /************************************************************************************************************************/

        /// <summary>
        /// A wrapper for the name and hash of an <see cref="AnimatorControllerParameter"/> to allow easy access.
        /// </summary>
        public struct Parameter
        {
            /************************************************************************************************************************/

            private string _Name;
            private int _Hash;

            /************************************************************************************************************************/

            /// <summary>
            /// The name of the wrapped parameter. This will be null if the <see cref="Hash"/> was assigned directly.
            /// </summary>
            public string Name
            {
                get { return _Name; }
                set
                {
                    _Name = value;
                    _Hash = Animator.StringToHash(value);
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// The name hash of the wrapped parameter.
            /// </summary>
            public int Hash
            {
                get { return _Hash; }
                set
                {
                    _Name = null;
                    _Hash = value;
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Constructs a new <see cref="Parameter"/> with the specified <see cref="Name"/> and uses
            /// <see cref="Animator.StringToHash"/> to calculate the <see cref="Hash"/>.
            /// </summary>
            public Parameter(string name)
            {
                _Name = name;
                _Hash = Animator.StringToHash(name);
            }

            /// <summary>
            /// Constructs a new <see cref="Parameter"/> with the specified <see cref="Hash"/> and leaves the
            /// <see cref="Name"/> null.
            /// </summary>
            public Parameter(int hash)
            {
                _Name = null;
                _Hash = hash;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Constructs a new <see cref="Parameter"/> with the specified <see cref="Name"/> and uses
            /// <see cref="Animator.StringToHash"/> to calculate the <see cref="Hash"/>.
            /// </summary>
            public static implicit operator Parameter(string name)
            {
                return new Parameter(name);
            }

            /// <summary>
            /// Constructs a new <see cref="Parameter"/> with the specified <see cref="Hash"/> and leaves the
            /// <see cref="Name"/> null.
            /// </summary>
            public static implicit operator Parameter(int hash)
            {
                return new Parameter(hash);
            }

            /************************************************************************************************************************/

            /// <summary>Returns the <see cref="Hash"/>.</summary>
            public static implicit operator int(Parameter parameter)
            {
                return parameter._Hash;
            }

            /************************************************************************************************************************/

#if UNITY_EDITOR
            private static Dictionary<RuntimeAnimatorController, Dictionary<int, AnimatorControllerParameterType>>
                _ControllerToParameterHashAndType;
#endif

            /// <summary>[Editor-Conditional]
            /// Throws if the `controller` doesn't have a parameter with the specified <see cref="Hash"/>
            /// and `type`.
            /// </summary>
            /// <exception cref="ArgumentException"/>
            [System.Diagnostics.Conditional(Strings.EditorOnly)]
            public void ValidateHasParameter(RuntimeAnimatorController controller, AnimatorControllerParameterType type)
            {
#if UNITY_EDITOR
                if (_ControllerToParameterHashAndType == null)
                    _ControllerToParameterHashAndType = new Dictionary<RuntimeAnimatorController, Dictionary<int, AnimatorControllerParameterType>>();

                // Get the parameter details.
                Dictionary<int, AnimatorControllerParameterType> parameterDetails;
                if (!_ControllerToParameterHashAndType.TryGetValue(controller, out parameterDetails))
                {
                    parameterDetails = new Dictionary<int, AnimatorControllerParameterType>();

                    var animatorController = controller as AnimatorController;
                    var parameters = animatorController.parameters;
                    var count = parameters.Length;
                    for (int i = 0; i < count; i++)
                    {
                        var parameter = parameters[i];
                        parameterDetails.Add(parameter.nameHash, parameter.type);
                    }

                    _ControllerToParameterHashAndType.Add(controller, parameterDetails);
                }

                // Check that there is a parameter with the correct hash and type.

                AnimatorControllerParameterType parameterType;
                if (!parameterDetails.TryGetValue(_Hash, out parameterType))
                {
                    throw new ArgumentException(controller + " has no " + type + " parameter matching " + this);
                }

                if (type != parameterType)
                {
                    throw new ArgumentException(controller + " has a parameter matching " + this + ", but it is not a " + type);
                }
#endif
            }

            /************************************************************************************************************************/

            /// <summary>Returns a string containing the <see cref="Name"/> and <see cref="Hash"/>.</summary>
            public override string ToString()
            {
                return string.Concat(
                    "ControllerState.Parameter(Name:'",
                    _Name,
                    "', Hash:",
                    _Hash.ToString(),
                    ")");
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inspector
        /************************************************************************************************************************/

        /// <summary>The number of parameters being wrapped by this state.</summary>
        public virtual int ParameterCount { get { return 0; } }

        /// <summary>Returns the hash of a parameter being wrapped by this state.</summary>
        /// <exception cref="NotSupportedException">Thrown if this state doesn't wrap any parameters.</exception>
        public virtual int GetParameterHash(int index) { throw new NotSupportedException(); }

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
        public sealed class Drawer : Editor.ParametizedAnimancerStateDrawer<ControllerState>
        {
            /************************************************************************************************************************/

            /// <summary>
            /// Constructs a new <see cref="Drawer"/> to manage the Inspector GUI for the `state`.
            /// </summary>
            public Drawer(ControllerState state) : base(state) { }

            /************************************************************************************************************************/

            /// <summary> Draws the details of the target state in the GUI.</summary>
            protected override void DoDetailsGUI(IAnimancerComponent owner)
            {
                GatherParameters();
                base.DoDetailsGUI(owner);
            }

            /************************************************************************************************************************/

            private readonly List<AnimatorControllerParameter>
                Parameters = new List<AnimatorControllerParameter>();

            /// <summary>
            /// Fills the <see cref="Parameters"/> list with the current parameter details.
            /// </summary>
            private void GatherParameters()
            {
                Parameters.Clear();

                var count = Target.ParameterCount;
                if (count == 0)
                    return;

                for (int i = 0; i < count; i++)
                {
                    var hash = Target.GetParameterHash(i);
                    Parameters.Add(GetParameter(hash));
                }
            }

            /************************************************************************************************************************/

            private AnimatorControllerParameter GetParameter(int hash)
            {
                var parameterCount = Target.Playable.GetParameterCount();
                for (int i = 0; i < parameterCount; i++)
                {
                    var parameter = Target.Playable.GetParameter(i);
                    if (parameter.nameHash == hash)
                        return parameter;
                }

                return null;
            }

            /************************************************************************************************************************/

            /// <summary>The number of parameters being managed by the target state.</summary>
            public override int ParameterCount { get { return Parameters.Count; } }

            /// <summary>Returns the name of a parameter being managed by the target state.</summary>
            /// <exception cref="NotSupportedException">Thrown if the target state doesn't manage any parameters.</exception>
            public override string GetParameterName(int index) { return Parameters[index].name; }

            /// <summary>Returns the type of a parameter being managed by the target state.</summary>
            /// <exception cref="NotSupportedException">Thrown if the target state doesn't manage any parameters.</exception>
            public override AnimatorControllerParameterType GetParameterType(int index) { return Parameters[index].type; }

            /// <summary>Returns the value of a parameter being managed by the target state.</summary>
            /// <exception cref="NotSupportedException">Thrown if the target state doesn't manage any parameters.</exception>
            public override object GetParameterValue(int index)
            {
                return Editor.AnimancerEditorUtilities.GetParameterValue(Target.Playable, Parameters[index]);
            }

            /// <summary>Sets the value of a parameter being managed by the target state.</summary>
            /// <exception cref="NotSupportedException">Thrown if the target state doesn't manage any parameters.</exception>
            public override void SetParameterValue(int index, object value)
            {
                Editor.AnimancerEditorUtilities.SetParameterValue(Target.Playable, Parameters[index], value);
            }

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
        /// <see cref="ControllerState"/> when passed into <see cref="AnimancerPlayable.Play(ITransition)"/>.
        /// </summary>
        /// <remarks>
        /// Unfortunately the tool used to generate this documentation does not currently support nested types with
        /// identical names, so only one <c>Transition</c> class will actually have a documentation page.
        /// <para></para>
        /// Even though it has the <see cref="SerializableAttribute"/>, this class won't actually get serialized
        /// by Unity because it's generic and abstract. Each child class still needs to include the attribute.
        /// </remarks>
        [Serializable]
        public abstract new class Transition<TState> : AnimancerState.Transition<TState>, IAnimationClipCollection
            where TState : ControllerState
        {
            /************************************************************************************************************************/

            [SerializeField]
            private RuntimeAnimatorController _Controller;

            /// <summary>[<see cref="SerializeField"/>]
            /// The <see cref="ControllerState.Controller"/> that will be used for the created state.
            /// </summary>
            public RuntimeAnimatorController Controller
            {
                get { return _Controller; }
                set { _Controller = value; }
            }

            /************************************************************************************************************************/

            [SerializeField, Tooltip("If false, stopping this state will reset all its layers to their default state. Default False.")]
            private bool _KeepStateOnStop;

            /// <summary>[<see cref="SerializeField"/>]
            /// If false, <see cref="Stop"/> will reset all layers to their default state.
            /// <para></para>
            /// If you set this value to false after the constructor, you must assign the <see cref="DefaultStateHashes"/>
            /// or call <see cref="GatherDefaultStates"/> yourself.
            /// </summary>
            public bool KeepStateOnStop
            {
                get { return _KeepStateOnStop; }
                set { _KeepStateOnStop = value; }
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="ITransitionDetailed"/>]
            /// The maximum amount of time the animation is expected to take (in seconds).
            /// </summary>
            public override float MaximumDuration
            {
                get
                {
                    if (_Controller == null)
                        return 0;

                    var duration = 0f;

                    var clips = _Controller.animationClips;
                    for (int i = 0; i < clips.Length; i++)
                    {
                        var length = clips[i].length;
                        if (duration < length)
                            duration = length;
                    }

                    return duration;
                }
            }

            /************************************************************************************************************************/

            /// <summary>Returns the <see cref="Controller"/>.</summary>
            public static implicit operator RuntimeAnimatorController(Transition<TState> transition)
            {
                return transition != null ? transition.Controller : null;
            }

            /************************************************************************************************************************/

            /// <summary>Adds all clips in the <see cref="Controller"/> to the collection.</summary>
            void IAnimationClipCollection.GatherAnimationClips(ICollection<AnimationClip> clips)
            {
                if (_Controller != null)
                    clips.Gather(_Controller.animationClips);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>
        /// A serializable <see cref="ITransition"/> which can create a <see cref="ControllerState"/> when
        /// passed into <see cref="AnimancerPlayable.Play(ITransition)"/>.
        /// <para></para>
        /// This class can be implicitly cast to and from <see cref="RuntimeAnimatorController"/>.
        /// </summary>
        [Serializable]
        public class Transition : Transition<ControllerState>
        {
            /************************************************************************************************************************/

            /// <summary>
            /// Creates and returns a new <see cref="ControllerState"/> connected to the `layer`.
            /// <para></para>
            /// This method also assigns it as the <see cref="AnimancerState.Transition{TState}.State"/>.
            /// </summary>
            public override ControllerState CreateState(AnimancerLayer layer)
            {
                return new ControllerState(layer, Controller, KeepStateOnStop);
            }

            /************************************************************************************************************************/

            /// <summary>Constructs a new <see cref="Transition"/>.</summary>
            public Transition() { }

            /// <summary>Constructs a new <see cref="Transition"/> with the specified Animator Controller.</summary>
            public Transition(RuntimeAnimatorController controller)
            {
                Controller = controller;
            }

            /************************************************************************************************************************/

            /// <summary>Constructs a new <see cref="Transition"/> with the specified Animator Controller.</summary>
            public static implicit operator Transition(RuntimeAnimatorController controller)
            {
                return new Transition(controller);
            }

            /************************************************************************************************************************/
            #region Drawer
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>
            /// [Editor-Only] Draws the Inspector GUI for a <see cref="Transition{TState}"/> or
            /// <see cref="Transition"/>.
            /// </summary>
            [CustomPropertyDrawer(typeof(Transition<>), true)]
            [CustomPropertyDrawer(typeof(Transition), true)]
            public class Drawer : Editor.TransitionDrawer
            {
                /************************************************************************************************************************/

                private readonly string[] Parameters;
                private readonly string[] ParameterPrefixes;

                /************************************************************************************************************************/

                /// <summary>Constructs a new <see cref="Drawer"/> without any parameters.</summary>
                public Drawer() : this(null) { }

                /// <summary>Constructs a new <see cref="Drawer"/> and sets the <see cref="Parameters"/>.</summary>
                public Drawer(params string[] parameters) : base("_Controller")
                {
                    Parameters = parameters;
                    if (parameters == null)
                        return;

                    ParameterPrefixes = new string[parameters.Length];

                    for (int i = 0; i < ParameterPrefixes.Length; i++)
                    {
                        ParameterPrefixes[i] = "." + parameters[i];
                    }
                }

                /************************************************************************************************************************/

                /// <summary>
                /// Draws the `property` GUI in relation to the `rootProperty` which was passed into
                /// <see cref="Editor.TransitionDrawer.OnGUI"/>.
                /// </summary>
                protected override void DoPropertyGUI(ref Rect area, SerializedProperty rootProperty, SerializedProperty property, GUIContent label)
                {
                    if (ParameterPrefixes != null)
                    {
                        var controllerProperty = rootProperty.FindPropertyRelative(MainPropertyName);
                        var controller = controllerProperty.objectReferenceValue as AnimatorController;
                        if (controller != null)
                        {
                            var path = property.propertyPath;

                            for (int i = 0; i < ParameterPrefixes.Length; i++)
                            {
                                if (path.EndsWith(ParameterPrefixes[i]))
                                {
                                    area.height = Editor.AnimancerGUI.LineHeight;
                                    DoParameterGUI(area, controller, property);
                                    return;
                                }
                            }
                        }
                    }

                    EditorGUI.BeginChangeCheck();

                    base.DoPropertyGUI(ref area, rootProperty, property, label);

                    // When the controller changes, validate all parameters.
                    if (EditorGUI.EndChangeCheck() &&
                        Parameters != null &&
                        property.propertyPath.EndsWith(MainPropertyPathSuffix))
                    {
                        var controller = property.objectReferenceValue as AnimatorController;
                        if (controller != null)
                        {
                            for (int i = 0; i < Parameters.Length; i++)
                            {
                                property = rootProperty.FindPropertyRelative(Parameters[i]);
                                var parameterName = property.stringValue;
                                if (!HasFloatParameter(controller, parameterName))
                                {
                                    parameterName = GetFirstFloatParameterName(controller);
                                    if (!string.IsNullOrEmpty(parameterName))
                                        property.stringValue = parameterName;
                                }
                            }
                        }
                    }
                }

                /************************************************************************************************************************/

                /// <summary>
                /// Draws a dropdown menu to select the name of a parameter in the `controller`.
                /// </summary>
                protected void DoParameterGUI(Rect area, AnimatorController controller, SerializedProperty property)
                {
                    var parameterName = property.stringValue;
                    var parameters = controller.parameters;

                    var label = Editor.AnimancerGUI.TempContent(property);
                    label = EditorGUI.BeginProperty(area, label, property);

                    var xMax = area.xMax;
                    area.width = EditorGUIUtility.labelWidth;
                    EditorGUI.PrefixLabel(area, label);

                    area.x += area.width;
                    area.xMax = xMax;

                    var color = GUI.color;
                    if (!HasFloatParameter(controller, parameterName))
                        GUI.color = Editor.AnimancerGUI.ErrorFieldColor;

                    var content = Editor.AnimancerGUI.TempContent(parameterName);
                    if (EditorGUI.DropdownButton(area, content, FocusType.Passive))
                    {
                        property = property.Copy();

                        var menu = new GenericMenu();

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var parameter = parameters[i];
                            Editor.AnimancerEditorUtilities.AddMenuItem(menu, parameter.name,
                                parameter.type == AnimatorControllerParameterType.Float, () =>
                                {
                                    Editor.Serialization.ForEachTarget(property, (targetProperty) =>
                                    {
                                        targetProperty.stringValue = parameter.name;
                                    });
                                });
                        }

                        if (menu.GetItemCount() == 0)
                            menu.AddDisabledItem(new GUIContent("No Parameters"));

                        menu.ShowAsContext();
                    }

                    GUI.color = color;

                    EditorGUI.EndProperty();
                }

                /************************************************************************************************************************/

                private static bool HasFloatParameter(AnimatorController controller, string name)
                {
                    if (string.IsNullOrEmpty(name))
                        return false;

                    var parameters = controller.parameters;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        if (parameter.type == AnimatorControllerParameterType.Float && name == parameters[i].name)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                /************************************************************************************************************************/

                private static string GetFirstFloatParameterName(AnimatorController controller)
                {
                    var parameters = controller.parameters;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        if (parameter.type == AnimatorControllerParameterType.Float)
                        {
                            return parameter.name;
                        }
                    }

                    return "";
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
    }
}

