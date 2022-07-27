// Animancer // Copyright 2020 Kybernetik //

using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// A <see cref="ControllerState"/> which manages three float parameters.
    /// </summary>
    /// <remarks>
    /// See also: <see cref="Float1ControllerState"/> and <see cref="Float2ControllerState"/>.
    /// </remarks>
    public sealed class Float3ControllerState : ControllerState
    {
        /************************************************************************************************************************/

        private Parameter _ParameterX;

        /// <summary>
        /// The name of the parameter which <see cref="ParameterX"/> will get and set.
        /// This will be null if the <see cref="ParameterHashX"/> was assigned directly.
        /// </summary>
        public string ParameterNameX
        {
            get { return _ParameterX.Name; }
            set
            {
                _ParameterX.Name = value;
                _ParameterX.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);
            }
        }

        /// <summary>
        /// The name hash of the parameter which <see cref="ParameterX"/> will get and set.
        /// </summary>
        public int ParameterHashX
        {
            get { return _ParameterX.Hash; }
            set
            {
                _ParameterX.Hash = value;
                _ParameterX.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);
            }
        }

        /// <summary>
        /// Gets and sets a float parameter in the <see cref="ControllerState.Controller"/> using the
        /// <see cref="ParameterHashX"/> as the id.
        /// </summary>
        public float ParameterX
        {
            get { return Playable.GetFloat(_ParameterX); }
            set { Playable.SetFloat(_ParameterX, value); }
        }

        /************************************************************************************************************************/

        private Parameter _ParameterY;

        /// <summary>
        /// The name of the parameter which <see cref="ParameterY"/> will get and set.
        /// This will be null if the <see cref="ParameterHashY"/> was assigned directly.
        /// </summary>
        public string ParameterNameY
        {
            get { return _ParameterY.Name; }
            set
            {
                _ParameterY.Name = value;
                _ParameterY.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);
            }
        }

        /// <summary>
        /// The name hash of the parameter which <see cref="ParameterY"/> will get and set.
        /// </summary>
        public int ParameterHashY
        {
            get { return _ParameterY.Hash; }
            set
            {
                _ParameterY.Hash = value;
                _ParameterY.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);
            }
        }

        /// <summary>
        /// Gets and sets a float parameter in the <see cref="ControllerState.Controller"/> using the
        /// <see cref="ParameterHashY"/> as the id.
        /// </summary>
        public float ParameterY
        {
            get { return Playable.GetFloat(_ParameterY); }
            set { Playable.SetFloat(_ParameterY, value); }
        }

        /************************************************************************************************************************/

        private Parameter _ParameterZ;

        /// <summary>
        /// The name of the parameter which <see cref="ParameterZ"/> will get and set.
        /// This will be null if the <see cref="ParameterHashZ"/> was assigned directly.
        /// </summary>
        public string ParameterNameZ
        {
            get { return _ParameterZ.Name; }
            set
            {
                _ParameterZ.Name = value;
                _ParameterZ.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);
            }
        }

        /// <summary>
        /// The name hash of the parameter which <see cref="ParameterZ"/> will get and set.
        /// </summary>
        public int ParameterHashZ
        {
            get { return _ParameterZ.Hash; }
            set
            {
                _ParameterZ.Hash = value;
                _ParameterZ.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);
            }
        }

        /// <summary>
        /// Gets and sets a float parameter in the <see cref="ControllerState.Controller"/> using the
        /// <see cref="ParameterHashZ"/> as the id.
        /// </summary>
        public float ParameterZ
        {
            get { return Playable.GetFloat(_ParameterZ); }
            set { Playable.SetFloat(_ParameterZ, value); }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets and sets <see cref="ParameterX"/>, <see cref="ParameterY"/>, and <see cref="ParameterZ"/>.
        /// </summary>
        public new Vector3 Parameter
        {
            get
            {
                return new Vector3(ParameterX, ParameterY, ParameterZ);
            }
            set
            {
                ParameterX = value.x;
                ParameterY = value.y;
                ParameterZ = value.z;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="Float3ControllerState"/> to play the `controller` without connecting
        /// it to the <see cref="PlayableGraph"/>.
        /// </summary>
        private Float3ControllerState(AnimancerPlayable root, RuntimeAnimatorController controller,
            Parameter parameterX, Parameter parameterY, Parameter parameterZ, bool resetStatesOnStop = true)
            : base(root, controller, resetStatesOnStop)
        {
            _ParameterX = parameterX;
            _ParameterX.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);

            _ParameterY = parameterY;
            _ParameterY.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);

            _ParameterZ = parameterZ;
            _ParameterZ.ValidateHasParameter(Controller, AnimatorControllerParameterType.Float);
        }

        /// <summary>
        /// Constructs a new <see cref="Float3ControllerState"/> to play the `controller` and connects it to the
        /// the `layer`.
        /// </summary>
        public Float3ControllerState(AnimancerLayer layer, RuntimeAnimatorController controller,
            Parameter parameterX, Parameter parameterY, Parameter parameterZ, bool resetStatesOnStop = true)
            : this(layer.Root, controller, parameterX, parameterY, parameterZ, resetStatesOnStop)
        {
            layer.AddChild(this);
        }

        /// <summary>
        /// Constructs a new <see cref="Float3ControllerState"/> to play the `controller` and
        /// connects it to the `parent` at the specified `index`.
        /// </summary>
        public Float3ControllerState(AnimancerNode parent, int index, RuntimeAnimatorController controller,
            Parameter parameterX, Parameter parameterY, Parameter parameterZ, bool resetStatesOnStop = true)
            : this(parent.Root, controller, parameterX, parameterY, parameterZ, resetStatesOnStop)
        {
            SetParent(parent, index);
        }

        /************************************************************************************************************************/

        /// <summary>The number of parameters being wrapped by this state.</summary>
        public override int ParameterCount { get { return 3; } }

        /// <summary>Returns the hash of a parameter being wrapped by this state.</summary>
        public override int GetParameterHash(int index)
        {
            switch (index)
            {
                case 0: return ParameterHashX;
                case 1: return ParameterHashY;
                case 2: return ParameterHashZ;
                default: throw new ArgumentOutOfRangeException("index");
            };
        }

        /************************************************************************************************************************/
        #region Transition
        /************************************************************************************************************************/

        /// <summary>
        /// A serializable <see cref="ITransition"/> which can create a <see cref="Float3ControllerState"/>
        /// when passed into <see cref="AnimancerPlayable.Play(ITransition)"/>.
        /// </summary>
        /// <remarks>
        /// Unfortunately the tool used to generate this documentation does not currently support nested types with
        /// identical names, so only one <c>Transition</c> class will actually have a documentation page.
        /// </remarks>
        [Serializable]
        public new class Transition : Transition<Float3ControllerState>
        {
            /************************************************************************************************************************/

            [SerializeField]
            private string _ParameterNameX;

            /// <summary>[<see cref="SerializeField"/>]
            /// The <see cref="Float3ControllerState.ParameterNameX"/> that will be used for the created state.
            /// </summary>
            public string ParameterNameX
            {
                get { return _ParameterNameX; }
                set { _ParameterNameX = value; }
            }

            /************************************************************************************************************************/

            [SerializeField]
            private string _ParameterNameY;

            /// <summary>[<see cref="SerializeField"/>]
            /// The <see cref="Float3ControllerState.ParameterNameY"/> that will be used for the created state.
            /// </summary>
            public string ParameterNameY
            {
                get { return _ParameterNameY; }
                set { _ParameterNameY = value; }
            }

            /************************************************************************************************************************/

            [SerializeField]
            private string _ParameterNameZ;

            /// <summary>[<see cref="SerializeField"/>]
            /// The <see cref="Float3ControllerState.ParameterNameZ"/> that will be used for the created state.
            /// </summary>
            public string ParameterNameZ
            {
                get { return _ParameterNameZ; }
                set { _ParameterNameZ = value; }
            }

            /************************************************************************************************************************/

            /// <summary>Constructs a new <see cref="Transition"/>.</summary>
            public Transition() { }

            /// <summary>Constructs a new <see cref="Transition"/> with the specified Animator Controller and parameters.</summary>
            public Transition(RuntimeAnimatorController controller,
                string parameterNameX, string parameterNameY, string parameterNameZ)
            {
                Controller = controller;
                _ParameterNameX = parameterNameX;
                _ParameterNameY = parameterNameY;
                _ParameterNameZ = parameterNameZ;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates and returns a new <see cref="Float3ControllerState"/> connected to the `layer`.
            /// <para></para>
            /// This method also assigns it as the <see cref="AnimancerState.Transition{TState}.State"/>.
            /// </summary>
            public override Float3ControllerState CreateState(AnimancerLayer layer)
            {
                return new Float3ControllerState(layer, Controller,
                    _ParameterNameX, _ParameterNameY, _ParameterNameZ, KeepStateOnStop);
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
                public Drawer() : base("_ParameterNameX", "_ParameterNameY", "_ParameterNameZ") { }

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

