// Animancer // Copyright 2020 Kybernetik //

using UnityEngine;

namespace Animancer
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> which holds a <see cref="ControllerState.Transition"/>.
    /// </summary>
    [CreateAssetMenu(menuName = Strings.MenuPrefix + "Controller Transition/Base", order = Strings.AssetMenuOrder + 4)]
    public class ControllerTransition : AnimancerTransition<ControllerState.Transition> { }
}

