// Animancer // Copyright 2020 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Various utilities used throughout Animancer.</summary>
    public static partial class AnimancerEditorUtilities
    {
        /************************************************************************************************************************/
        #region Misc
        /************************************************************************************************************************/

        /// <summary>
        /// Tries to find a <typeparamref name="T"/> component on the `gameObject` or its parents or children (in that
        /// order).
        /// </summary>
        public static T GetComponentInHierarchy<T>(GameObject gameObject) where T : class
        {
            var component = gameObject.GetComponentInParent<T>();
            if (component != null)
                return component;

            return gameObject.GetComponentInChildren<T>();
        }

        /************************************************************************************************************************/

        /// <summary>Assets cannot reference scene objects.</summary>
        public static bool ShouldAllowReference(Object obj, Object reference)
        {
            return obj == null || reference == null ||
                !EditorUtility.IsPersistent(obj) ||
                EditorUtility.IsPersistent(reference);
        }

        /************************************************************************************************************************/

        /// <summary>Wraps <see cref="UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded"/>.</summary>
        public static bool GetIsInspectorExpanded(Object obj)
        {
            return UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(obj);
        }

        /// <summary>Wraps <see cref="UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded"/>.</summary>
        public static void SetIsInspectorExpanded(Object obj, bool isExpanded)
        {
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(obj, isExpanded);
        }

        /// <summary>Calls <see cref="SetIsInspectorExpanded(Object, bool)"/> on all `objects`.</summary>
        public static void SetIsInspectorExpanded(Object[] objects, bool isExpanded)
        {
            for (int i = 0; i < objects.Length; i++)
                SetIsInspectorExpanded(objects[i], isExpanded);
        }

        /************************************************************************************************************************/

        private static Dictionary<Type, Dictionary<string, MethodInfo>> _TypeToMethodNameToMethod;

        /// <summary>
        /// Tries to find a method with the specified name on the `target` object and invoke it.
        /// </summary>
        public static object Invoke(object target, string methodName)
        {
            return Invoke(target.GetType(), target, methodName);
        }

        /// <summary>
        /// Tries to find a method with the specified name on the `target` object and invoke it.
        /// </summary>
        public static object Invoke(Type type, object target, string methodName)
        {
            if (_TypeToMethodNameToMethod == null)
                _TypeToMethodNameToMethod = new Dictionary<Type, Dictionary<string, MethodInfo>>();

            Dictionary<string, MethodInfo> nameToMethod;
            if (!_TypeToMethodNameToMethod.TryGetValue(type, out nameToMethod))
            {
                nameToMethod = new Dictionary<string, MethodInfo>();
                _TypeToMethodNameToMethod.Add(type, nameToMethod);
            }

            MethodInfo method;
            if (!nameToMethod.TryGetValue(methodName, out method))
            {
                method = type.GetMethod(methodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                nameToMethod.Add(methodName, method);

                if (method == null)
                    RegisterNonCriticalMissingMember(type.FullName, methodName);
            }

            if (method != null)
                return method.Invoke(target, null);

            return null;
        }

        /************************************************************************************************************************/

        private static List<Action<StringBuilder>> _NonCriticalIssues;

        /// <summary>
        /// Registers a delegate that can construct a description of an issue at a later time so that it doesn't waste
        /// the user's time on unimportant issues.
        /// </summary>
        public static void RegisterNonCriticalIssue(Action<StringBuilder> describeIssue)
        {
            if (_NonCriticalIssues == null)
                _NonCriticalIssues = new List<Action<StringBuilder>>();

            _NonCriticalIssues.Add(describeIssue);

        }

        /// <summary>
        /// Calls <see cref="RegisterNonCriticalIssue"/> with an issue indicating that a particular type was not
        /// found by reflection.
        /// </summary>
        public static void RegisterNonCriticalMissingType(string type)
        {
            RegisterNonCriticalIssue((text) => text
                .Append("[Reflection] Unable to find type '")
                .Append(type)
                .Append("'"));
        }

        /// <summary>
        /// Calls <see cref="RegisterNonCriticalIssue"/> with an issue indicating that a particular member was not
        /// found by reflection.
        /// </summary>
        public static void RegisterNonCriticalMissingMember(string type, string name)
        {
            RegisterNonCriticalIssue((text) => text
                .Append("[Reflection] Unable to find member '")
                .Append(name)
                .Append("' in type '")
                .Append(type)
                .Append("'"));
        }

        /// <summary>
        /// Appends all issues given to <see cref="RegisterNonCriticalIssue"/> to the `text`.
        /// </summary>
        public static void AppendNonCriticalIssues(StringBuilder text)
        {
            if (_NonCriticalIssues == null)
                return;

            text.Append("\n\nThe following non-critical issues have also been found" +
                " (in Animancer generally, not specifically this object):\n\n");

            for (int i = 0; i < _NonCriticalIssues.Count; i++)
            {
                text.Append(" - ");
                _NonCriticalIssues[i](text);
                text.Append("\n\n");
            }
        }

        /************************************************************************************************************************/

        /// <summary>Gets the value of the `parameter` in the `animator`.</summary>
        public static object GetParameterValue(Animator animator, AnimatorControllerParameter parameter)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    return animator.GetFloat(parameter.nameHash);

                case AnimatorControllerParameterType.Int:
                    return animator.GetInteger(parameter.nameHash);

                case AnimatorControllerParameterType.Bool:
                case AnimatorControllerParameterType.Trigger:
                    return animator.GetBool(parameter.nameHash);

                default:
                    throw new ArgumentException("Unhandled AnimatorControllerParameterType: " + parameter.type);
            }
        }

        /// <summary>Gets the value of the `parameter` in the `playable`.</summary>
        public static object GetParameterValue(AnimatorControllerPlayable playable, AnimatorControllerParameter parameter)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    return playable.GetFloat(parameter.nameHash);

                case AnimatorControllerParameterType.Int:
                    return playable.GetInteger(parameter.nameHash);

                case AnimatorControllerParameterType.Bool:
                case AnimatorControllerParameterType.Trigger:
                    return playable.GetBool(parameter.nameHash);

                default:
                    throw new ArgumentException("Unhandled AnimatorControllerParameterType: " + parameter.type);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Sets the value of the `parameter` in the `animator`.</summary>
        public static void SetParameterValue(Animator animator, AnimatorControllerParameter parameter, object value)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(parameter.nameHash, (float)value);
                    break;

                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(parameter.nameHash, (int)value);
                    break;

                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(parameter.nameHash, (bool)value);
                    break;

                case AnimatorControllerParameterType.Trigger:
                    if ((bool)value)
                        animator.SetTrigger(parameter.nameHash);
                    else
                        animator.ResetTrigger(parameter.nameHash);
                    break;

                default:
                    throw new ArgumentException("Unhandled AnimatorControllerParameterType: " + parameter.type);
            }
        }

        /// <summary>Sets the value of the `parameter` in the `playable`.</summary>
        public static void SetParameterValue(AnimatorControllerPlayable playable, AnimatorControllerParameter parameter, object value)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    playable.SetFloat(parameter.nameHash, (float)value);
                    break;

                case AnimatorControllerParameterType.Int:
                    playable.SetInteger(parameter.nameHash, (int)value);
                    break;

                case AnimatorControllerParameterType.Bool:
                    playable.SetBool(parameter.nameHash, (bool)value);
                    break;

                case AnimatorControllerParameterType.Trigger:
                    if ((bool)value)
                        playable.SetTrigger(parameter.nameHash);
                    else
                        playable.ResetTrigger(parameter.nameHash);
                    break;

                default:
                    throw new ArgumentException("Unhandled AnimatorControllerParameterType: " + parameter.type);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the `node` is not null and <see cref="AnimancerNode.IsValid"/>.
        /// </summary>
        /// <remarks>
        /// Normally a method can't have the same name as a property, but an extension method can.
        /// </remarks>
        public static bool IsValid(this AnimancerNode node)
        {
            return node != null && node.IsValid;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Waits one frame to call the `method` as long as Unity is currently in Edit Mode.
        /// </summary>
        public static void EditModeDelayCall(Action method)
        {
            // Would be better to check this before the delayCall, but it only works on the main thread.

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    method();
            };
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Context Menus
        /************************************************************************************************************************/

        /// <summary>
        /// Adds a menu function which is disabled if `isEnabled` is false.
        /// </summary>
        public static void AddMenuItem(GenericMenu menu, string label, bool isEnabled, GenericMenu.MenuFunction func)
        {
            if (!isEnabled)
            {
                menu.AddDisabledItem(new GUIContent(label));
                return;
            }

            menu.AddItem(new GUIContent(label), false, func);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds a menu function which passes the result of <see cref="CalculateEditorFadeDuration"/> into `startFade`.
        /// </summary>
        public static void AddFadeFunction(GenericMenu menu, string label, bool isEnabled, AnimancerNode node, Action<float> startFade)
        {
            // Fade functions need to be delayed twice since the context menu itself causes the next frame delta
            // time to be unreasonably high (which would skip the start of the fade).
            AddMenuItem(menu, label, isEnabled,
                () => EditorApplication.delayCall +=
                () => EditorApplication.delayCall +=
                () =>
                {
                    startFade(node.CalculateEditorFadeDuration());
                });
        }

        /// <summary>
        /// Returns the duration of the `node`s current fade (if any), otherwise returns the `defaultDuration`.
        /// </summary>
        public static float CalculateEditorFadeDuration(this AnimancerNode node, float defaultDuration = 1)
        {
            return node.FadeSpeed > 0 ? 1 / node.FadeSpeed : defaultDuration;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds a menu function to open a web page. If the `linkSuffix` starts with a '/' then it will be relative to
        /// the <see cref="Strings.DocumentationURL"/>.
        /// </summary>
        public static void AddDocumentationLink(GenericMenu menu, string label, string linkSuffix)
        {
            menu.AddItem(new GUIContent(label), false, () =>
            {
                if (linkSuffix[0] == '/')
                    linkSuffix = Strings.DocumentationURL + linkSuffix;

                EditorUtility.OpenWithDefaultApp(linkSuffix);
            });
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Toggles the <see cref="Motion.isLooping"/> flag between true and false.
        /// </summary>
        [MenuItem("CONTEXT/AnimationClip/Toggle Looping")]
        private static void ToggleLooping(MenuCommand command)
        {
            var clip = (AnimationClip)command.context;
            SetLooping(clip, !clip.isLooping);
        }

        /// <summary>
        /// Sets the <see cref="Motion.isLooping"/> flag.
        /// </summary>
        public static void SetLooping(AnimationClip clip, bool looping)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = looping;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            Debug.Log("Set " + clip.name + " to be " + (looping ? "Looping" : "Not Looping") +
                ". Note that you need to restart Unity for this change to take effect.", clip);

            // None of these let us avoid the need to restart Unity.
            //EditorUtility.SetDirty(clip);
            //AssetDatabase.SaveAssets();

            //var path = AssetDatabase.GetAssetPath(clip);
            //AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /************************************************************************************************************************/

        /// <summary>Swaps the <see cref="AnimationClip.legacy"/> flag between true and false.</summary>
        [MenuItem("CONTEXT/AnimationClip/Toggle Legacy")]
        private static void ToggleLegacy(MenuCommand command)
        {
            var clip = (AnimationClip)command.context;
            clip.legacy = !clip.legacy;
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Animator.Rebind"/>.</summary>
        [MenuItem("CONTEXT/Animator/Restore Bind Pose", priority = 110)]
        private static void RestoreBindPose(MenuCommand command)
        {
            var animator = (Animator)command.context;

            Undo.RegisterFullObjectHierarchyUndo(animator.gameObject, "Restore bind pose");

            var type = Type.GetType("UnityEditor.AvatarSetupTool, UnityEditor");
            if (type != null)
            {
                var method = type.GetMethod("SampleBindPose", BindingFlags.Static | BindingFlags.Public);
                if (method != null)
                    method.Invoke(null, new object[] { animator.gameObject });
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Dummy Animancer Component
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="IAnimancerComponent"/> that is not actually a <see cref="Component"/>.
        /// </summary>
        public sealed class DummyAnimancerComponent : IAnimancerComponent
        {
            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="DummyAnimancerComponent"/>.</summary>
            public DummyAnimancerComponent(Animator animator, AnimancerPlayable playable)
            {
                Animator = animator;
                Playable = playable;
                InitialUpdateMode = animator.updateMode;
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns true.</summary>
            public bool enabled { get { return true; } }

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns the <see cref="Animator"/>'s <see cref="GameObject"/>.</summary>
            public GameObject gameObject { get { return Animator.gameObject; } }

            /// <summary>[<see cref="IAnimancerComponent"/>] The target <see cref="UnityEngine.Animator"/>.</summary>
            public Animator Animator { get; set; }

            /// <summary>[<see cref="IAnimancerComponent"/>] The target <see cref="AnimancerPlayable"/>.</summary>
            public AnimancerPlayable Playable { get; private set; }

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns true.</summary>
            public bool IsPlayableInitialised { get { return true; } }

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns false.</summary>
            public bool ResetOnDisable { get { return false; } }

            /// <summary>[<see cref="IAnimancerComponent"/>] Does nothing.</summary>
            public AnimatorUpdateMode UpdateMode { get; set; }

            /************************************************************************************************************************/

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns the `clip`.</summary>
            public object GetKey(AnimationClip clip)
            {
                return clip;
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns null.</summary>
            public string AnimatorFieldName { get { return null; } }

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns null.</summary>
            public AnimatorUpdateMode? InitialUpdateMode { get; private set; }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

