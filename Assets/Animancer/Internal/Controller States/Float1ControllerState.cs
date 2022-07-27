// Animancer // Copyright 2020 Kybernetik //

using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// A <see cref="ControllerState"/> which manages one float parameter.
    /// </summary>
    /// <remarks>
    /// See also: <see cref="Float2ControllerState"/> and <see cref="Float3ControllerState"/>.
    /// </remarks>
    public sealed class Float1ControllerState : ControllerState
    {
        /************************************************************************************************************************/

        private Parameter _Parameter;

        /// <summary>
        /// The name of the parameter which <see cref="Parameter"/> will get and set.
        /// This will be null if the <see cref="ParameterHash"/> was assigned directly.
        /// </summary>
        public string ParameterName
        {
            get { return _Parameter.Name; }
            set
            {
                _Parameter.Name = value;
                _Parameter.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);
            }
        }

        /// <summary>
        /// The name hash of the parameter which <see cref="Parameter"/> will get and set.
        /// </summary>
        public int ParameterHash
        {
            get { return _Parameter.Hash; }
            set
            {
                _Parameter.Hash = value;
                _Parameter.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);
            }
        }

        /// <summary>
        /// Gets and sets a float parameter in the <see cref="ControllerState.Controller"/> using the
        /// <see cref="ParameterHash"/> as the id.
        /// </summary>
        public new float Parameter
        {
            get { return Playable.GetFloat(_Parameter); }
            set { Playable.SetFloat(_Parameter, value); }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="Float1ControllerState"/> to play the `controller` without connecting
        /// it to the <see cref="PlayableGraph"/>.
        /// </summary>
        private Float1ControllerState(AnimancerPlayable root, RuntimeAnimatorController controller, Parameter parameter,
            bool resetStatesOnStop = true)
            : base(root, controller, resetStatesOnStop)
        {
            _Parameter = parameter;
            _Parameter.ValidateHasParameter(controller, AnimatorControllerParameterType.Float);
        }

        /// <summary>
        /// Constructs a new <see cref="Float1ControllerState"/> to play the `controller` and connects it to the
        /// the `layer`.
        /// </summary>
        public Float1ControllerState(AnimancerLayer layer, RuntimeAnimatorController controller, Parameter parameter,
            bool resetStatesOnStop = true)
            : this(layer.Root, controller, parameter, resetStatesOnStop)
        {
            layer.AddChild(this);
        }

        /// <summary>
        /// Constructs a new <see cref="Float1ControllerState"/> to play the `controller` and connects
        /// connects it to the `parent` at the specified `index`.
        /// </summary>
        public Float1ControllerState(AnimancerNode parent, int index, RuntimeAnimatorController controller, Parameter parameter,
            bool resetStatesOnStop = true)
            : this(parent.Root, controller, parameter, resetStatesOnStop)
        {
            SetParent(parent, index);
        }

        /************************************************************************************************************************/

        /// <summary>The number of parameters being wrapped by this state.</summary>
        public override int ParameterCount { get { return 1; } }

        /// <summary>Returns the hash of a parameter being wrapped by this state.</summary>
        public override int GetParameterHash(int index) { return ParameterHash; }

        /************************************************************************************************************************/
        #region Transition
        /************************************************************************************************************************/

        /// <summary>
        /// A serializable <see cref="ITransition"/> which can create a <see cref="Float1ControllerState"/>
        /// when passed into <see cref="AnimancerPlayable.Play(ITransition)"/>.
        /// </summary>
        /// <remarks>
        /// Unfortunately the tool used to generate this documentation does not currently support nested types with
        /// identical names, so only one <c>Transition</c> class will actually have a documentation page.
        /// </remarks>
        [Serializable]
        public new class Transition : Transition<Float1ControllerState>
        {
            /************************************************************************************************************************/

            [SerializeField]
            private string _ParameterName;

            /// <summary>[<see cref="SerializeField"/>]
            /// The <see cref="Float1ControllerState.ParameterName"/> that will be used for the created state.
            /// </summary>
            public string ParameterName
            {
                get { return _ParameterName; }
                set { _ParameterName = value; }
            }

            /************************************************************************************************************************/

            /// <summary>Constructs a new <see cref="Transition"/>.</summary>
            public Transition() { }

            /// <summary>Constructs a new <see cref="Transition"/> with the specified Animator Controller and parameter.</summary>
            public Transition(RuntimeAnimatorController controller, string parameterName)
            {
                Controller = controller;
                _ParameterName = parameterName;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates and returns a new <see cref="Float1ControllerState"/> connected to the `layer`.
            /// <para></para>
            /// This method also assigns it as the <see cref="AnimancerState.Transition{TState}.State"/>.
            /// </summary>
            public override Float1ControllerState CreateState(AnimancerLayer layer)
            {
                return new Float1ControllerState(layer, Controller, _ParameterName, KeepStateOnStop);
            }

            /************************************************************************************************************************/
            #region Drawer
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only] Draws the Inspector GUI for a <see cref="Transition"/>.</summary>
            [UnityEditor.CustomPropertyDrawer(typeof(Transition), true)]
            public class Drawer : ControllerState.Transition.Drawer
            {
                /************************************************************************************************************************/

                /// <summary>
                /// Constructs a new <see cref="Drawer"/> and sets the
                /// <see cref="ControllerState.Transition.Drawer.Parameters"/>.
                /// </summary>
                public Drawer() : base("_ParameterName") { }

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

