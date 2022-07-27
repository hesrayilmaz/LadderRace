// Animancer // Copyright 2020 Kybernetik //

using UnityEngine;

namespace Animancer
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> which holds a <see cref="ManualMixerState.Transition"/>.
    /// </summary>
    [CreateAssetMenu(menuName = Strings.MenuPrefix + "Mixer Transition/Manual", order = Strings.AssetMenuOrder + 1)]
    public class ManualMixerTransition : AnimancerTransition<ManualMixerState.Transition> { }
}

