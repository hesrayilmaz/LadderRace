// Animancer // Copyright 2020 Kybernetik //

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>Various extension methods and utilities.</summary>
    public static partial class AnimancerUtilities
    {
        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] Loops the `value` so that <c>0 &lt;= value &lt; 1</c>.</summary>
        /// <remarks>This is more efficient than using <see cref="Mathf.Repeat"/> with a length of 1.</remarks>
        public static float Wrap01(this float value)
        {
            var valueAsDouble = (double)value;
            return (float)(valueAsDouble - Math.Floor(valueAsDouble));
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Adds the specified type of <see cref="IAnimancerComponent"/>, links it to the `animator`, and returns it.
        /// </summary>
        public static T AddAnimancerComponent<T>(this Animator animator) where T : Component, IAnimancerComponent
        {
            var animancer = animator.gameObject.AddComponent<T>();
            animancer.Animator = animator;
            return animancer;
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Returns the <see cref="IAnimancerComponent"/> on the same <see cref="GameObject"/> as the `animator` if
        /// there is one. Otherwise this method adds a new one and returns it.
        /// </summary>
        public static T GetOrAddAnimancerComponent<T>(this Animator animator) where T : Component, IAnimancerComponent
        {
            var animancer = animator.GetComponent<T>();
            if (animancer != null)
                return animancer;
            else
                return animator.AddAnimancerComponent<T>();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Checks if any <see cref="AnimationClip"/> in the `source` has an animation event with the specified
        /// `functionName`.
        /// </summary>
        public static bool HasEvent(IAnimationClipCollection source, string functionName)
        {
            var clips = ObjectPool.AcquireSet<AnimationClip>();
            source.GatherAnimationClips(clips);

            foreach (var clip in clips)
            {
                if (HasEvent(clip, functionName))
                {
                    ObjectPool.Release(clips);
                    return true;
                }
            }

            ObjectPool.Release(clips);
            return false;
        }

        /// <summary>Checks if the `clip` has an animation event with the specified `functionName`.</summary>
        public static bool HasEvent(AnimationClip clip, string functionName)
        {
            var events = clip.events;
            var count = events.Length;
            for (int i = 0; i < count; i++)
            {
                if (events[i].functionName == functionName)
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        /// <summary>[Pro-Only]
        /// Calculates all thresholds in the `mixer` using the <see cref="AnimancerState.AverageVelocity"/> of each
        /// state on the X and Z axes.
        /// <para></para>
        /// Note that this method requires the <c>Root Transform Position (XZ) -> Bake Into Pose</c> toggle to be
        /// disabled in the Import Settings of each <see cref="AnimationClip"/> in the mixer.
        /// </summary>
        public static void CalculateThresholdsFromAverageVelocityXZ(this MixerState<Vector2> mixer)
        {
            mixer.ValidateThresholdCount();

            var count = mixer.States.Length;
            for (int i = 0; i < count; i++)
            {
                var state = mixer.States[i];
                if (state == null)
                    continue;

                var averageVelocity = state.AverageVelocity;
                mixer.SetThreshold(i, new Vector2(averageVelocity.x, averageVelocity.z));
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Conditional] Marks the `target` as dirty.</summary>
        [System.Diagnostics.Conditional(Strings.EditorOnly)]
        public static void SetDirty(Object target)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(target);
#endif
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Conditional]
        /// If there are multiple components which inherit from <typeparamref name="T"/>, the first one is changed to
        /// the type of the second and any after the first are destroyed. This allows you to change the type without
        /// losing the values of any serialized fields they share.
        /// <para></para>
        /// The `currentComponent` is used to determine which <see cref="GameObject"/> to examine and the base
        /// component type <typeparamref name="T"/>.
        /// </summary>
        /// <example><code>
        /// protected void Reset()
        /// {
        ///     AnimancerUtilities.IfMultiComponentThenChangeType(this);
        /// }
        /// </code></example>
        [System.Diagnostics.Conditional(Strings.EditorOnly)]
        public static void IfMultiComponentThenChangeType<T>(T currentComponent) where T : MonoBehaviour
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // If there is already another instance of this component on the same object, delete this new instance and
            // change the original's type to match this one.
            var components = currentComponent.GetComponents<T>();
            if (components.Length > 1)
            {
                var oldComponent = components[0];
                var newComponent = components[1];

                if (oldComponent.GetType() != newComponent.GetType())
                {
                    // All we have to do is change the Script field to the new type and Unity will immediately deserialize
                    // the existing data as that type, so any fields shared between both types will keep their data.

                    using (var serializedObject = new UnityEditor.SerializedObject(oldComponent))
                    {
                        var scriptProperty = serializedObject.FindProperty("m_Script");
                        scriptProperty.objectReferenceValue = UnityEditor.MonoScript.FromMonoBehaviour(newComponent);
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                // Destroy all components other than the first (the oldest).
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    var i = 1;
                    for (; i < components.Length; i++)
                    {
                        Object.DestroyImmediate(components[i], true);
                    }
                };
            }
#endif
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Conditional]
        /// Plays the specified `clip` if called in Edit Mode and optionally pauses it immediately.
        /// </summary>
        /// <remarks>
        /// Before Unity 2018.3, playing animations in Edit Mode didn't work properly.
        /// </remarks>
        [System.Diagnostics.Conditional(Strings.EditorOnly)]
        public static void EditModePlay(IAnimancerComponent animancer, AnimationClip clip, bool pauseImmediately = true)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ||
                animancer == null || clip == null)
                return;

            // Delay for a frame in case this was called at a bad time (such as during OnValidate).
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ||
                    animancer == null || clip == null)
                    return;

                animancer.Playable.Play(clip);

                if (pauseImmediately)
                {
                    animancer.Playable.Evaluate();
                    animancer.Playable.PauseGraph();
                }
            };
#endif
        }

        /************************************************************************************************************************/
    }
}

