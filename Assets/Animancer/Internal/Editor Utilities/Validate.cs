// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animancer
{
    /// <summary>
    /// Enforces various rules throughout the system, most of which are compiled out if UNITY_ASSERTIONS is not defined
    /// (by default, it is defined in the Unity Editor and in Development Builds).
    /// </summary>
    public static class Validate
    {
        /************************************************************************************************************************/

        /// <summary>[Assert]
        /// Throws if the `clip` is marked as <see cref="AnimationClip.legacy"/>.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        [System.Diagnostics.Conditional(Strings.Assert)]
        public static void NotLegacy(AnimationClip clip)
        {
            if (clip.legacy)
                throw new ArgumentException("Legacy clip '" + clip + "' cannot be used by Animancer." +
                    " Set the legacy property to false before using this clip." +
                    " If it was imported as part of a model then the model's Rig type must be changed to Humanoid or Generic." +
                    " Otherwise you can use the 'Toggle Legacy' function in the clip's context menu" +
                    " (via the cog icon in the top right of its Inspector).");
        }

        /************************************************************************************************************************/

        /// <summary>[Assert]
        /// Throws if the <see cref="AnimancerNode.Root"/> is not the `root`.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        [System.Diagnostics.Conditional(Strings.Assert)]
        public static void Root(AnimancerNode node, AnimancerPlayable root)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (node.Root != root)
                throw new ArgumentException("AnimancerNode.Root mismatch:" +
                    " you are attempting to use a node in an AnimancerPlayable that is not it's root: " + node);
        }

        /************************************************************************************************************************/

        /// <summary>[Assert]
        /// Throws if the <see cref="AnimancerState.Parent"/> is not the `parent`.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        [System.Diagnostics.Conditional(Strings.Assert)]
        public static void Parent(AnimancerState state, AnimancerNode parent)
        {
            if (state.Parent != parent)
                throw new ArgumentException("AnimancerState.Parent mismatch:" +
                    " you are attempting to use a state in an AnimancerLayer that is not it's parent.");
        }

        /************************************************************************************************************************/

        /// <summary>[Assert]
        /// Throws if the `state` was not actually assigned to its specified <see cref="AnimancerNode.Index"/> in
        /// the `states`.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown if the <see cref="AnimancerNode.Index"/> is larger than the number of `states`.
        /// </exception>
        [System.Diagnostics.Conditional(Strings.Assert)]
        public static void RemoveChild(AnimancerState state, IList<AnimancerState> states)
        {
            var index = state.Index;

            if (index < 0)
                throw new InvalidOperationException(
                    "Tried to remove a child state that did not actually have a Index assigned");

            if (index > states.Count)
                throw new IndexOutOfRangeException(
                    "state.Index (" + state.Index + ") is outside the collection of states (count " + states.Count + ")");

            if (states[state.Index] != state)
                throw new InvalidOperationException(
                    "Tried to remove a child state that was not actually connected to its port on " + state.Parent + ":" +
                    "\n    Port: " + state.Index +
                    "\n    Connected Child: " + states[state.Index] +
                    "\n    Disconnecting Child: " + state);
        }

        /************************************************************************************************************************/
    }
}

