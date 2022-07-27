// Animancer // Copyright 2020 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only]
    /// Allows animations to be procedurally gathered throughout the hierarchy without needing explicit references.
    /// </summary>
    /// <remarks>
    /// This class is [Editor-Only] because it uses reflection and is not particularly efficient, but it does not
    /// actually use any Editor Only functionality so it could be made usable at runtime by simply removing the
    /// <c>#if UNITY_EDITOR</c> at the top of the file and <c>#endif</c> at the bottom.
    /// </remarks>
    public static class AnimationGatherer
    {
        /************************************************************************************************************************/

        private const int MaxFieldDepth = 7;

        /************************************************************************************************************************/

        private static readonly HashSet<object>
            RecursionGuard = new HashSet<object>();

        private static int _CallCount;

        private static bool BeginRecursionGuard(object obj)
        {
            if (RecursionGuard.Contains(obj))
                return false;

            RecursionGuard.Add(obj);
            return true;
        }

        private static void EndCall()
        {
            if (_CallCount == 0)
                RecursionGuard.Clear();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Fills the `clips` with any <see cref="AnimationClip"/>s referenced by components in the same hierarchy as
        /// the `gameObject`. See <see cref="ICharacterRoot"/> for details.
        /// </summary>
        public static void GatherFromGameObject(GameObject gameObject, ICollection<AnimationClip> clips)
        {
            if (!BeginRecursionGuard(gameObject))
                return;

            try
            {
                _CallCount++;

                var clipSet = clips as HashSet<AnimationClip>;
                if (clipSet == null)
                    clipSet = ObjectPool.AcquireSet<AnimationClip>();

                GatherFromComponents(gameObject, clipSet);

                if (clipSet != clips)
                {
                    clips.Gather(clipSet);
                    ObjectPool.Release(clipSet);
                }
            }
            finally
            {
                _CallCount--;
                EndCall();
            }
        }

        /// <summary>
        /// Fills the `clips` with any <see cref="AnimationClip"/>s referenced by components in the same hierarchy as
        /// the `gameObject`. See <see cref="ICharacterRoot"/> for details.
        /// </summary>
        public static void GatherFromGameObject(GameObject gameObject, ref AnimationClip[] clips, bool sort)
        {
            if (!BeginRecursionGuard(gameObject))
                return;

            try
            {
                _CallCount++;

                var clipSet = ObjectPool.AcquireSet<AnimationClip>();

                GatherFromComponents(gameObject, clipSet);

                if (clips == null || clips.Length != clipSet.Count)
                    clips = new AnimationClip[clipSet.Count];

                clipSet.CopyTo(clips);
                ObjectPool.Release(clipSet);

                if (sort)
                    Array.Sort(clips, (a, b) => a.name.CompareTo(b.name));
            }
            finally
            {
                _CallCount--;
                EndCall();
            }
        }

        /************************************************************************************************************************/

        private static void GatherFromComponents(GameObject gameObject, HashSet<AnimationClip> clips)
        {
            var root = AnimancerEditorUtilities.FindRoot(gameObject);

            var components = ObjectPool.AcquireList<MonoBehaviour>();
            root.GetComponentsInChildren(true, components);
            GatherFromComponents(components, clips);
            ObjectPool.Release(components);
        }

        /************************************************************************************************************************/

        private static void GatherFromComponents(List<MonoBehaviour> components, HashSet<AnimationClip> clips)
        {
            var i = components.Count;
            GatherClips:
            try
            {
                while (--i >= 0)
                {
                    GatherFromObject(components[i], clips, 0);
                }
            }
            catch (Exception ex)
            {
                // If something throws an exception, log it and go to the next object.
                Debug.LogException(ex);
                goto GatherClips;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gathers all animations from the `source`s fields.
        /// </summary>
        private static void GatherFromObject(object source, ICollection<AnimationClip> clips, int depth)
        {
            if (!BeginRecursionGuard(source))
                return;

            try
            {
                if (clips.GatherFromSource(source))
                    return;
            }
            finally
            {
                RecursionGuard.Remove(source);
            }

            GatherFromFields(source, clips, depth);
        }

        /************************************************************************************************************************/

        /// <summary>Types mapped to a delegate that can quickly gather their clips.</summary>
        private static readonly Dictionary<Type, Action<object, ICollection<AnimationClip>>>
            TypeToGatherer = new Dictionary<Type, Action<object, ICollection<AnimationClip>>>();

        /// <summary>
        /// Uses reflection to gather <see cref="AnimationClip"/>s from fields on the `source` object.
        /// </summary>
        private static void GatherFromFields(object source, ICollection<AnimationClip> clips, int depth)
        {
            if (depth >= MaxFieldDepth ||
                source == null ||
                !BeginRecursionGuard(source))
                return;

            var type = source.GetType();
            Action<object, ICollection<AnimationClip>> gatherClips;

            if (!TypeToGatherer.TryGetValue(type, out gatherClips))
            {
                gatherClips = BuildClipGatherer(type, depth);
                TypeToGatherer.Add(type, gatherClips);
            }

            if (gatherClips != null)
                gatherClips(source, clips);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a delegate to gather <see cref="AnimationClip"/>s from all relevant fields in a given `type`.
        /// </summary>
        private static Action<object, ICollection<AnimationClip>> BuildClipGatherer(Type type, int depth)
        {
            if (type.IsPrimitive ||
                type.IsEnum ||
                type.IsAutoClass ||
                type.IsPointer)
                return null;

            Action<object, ICollection<AnimationClip>> gatherer = null;

            while (type != null)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    var fieldType = field.FieldType;
                    if (fieldType.IsPrimitive ||
                        fieldType.IsEnum ||
                        fieldType.IsAutoClass ||
                        fieldType.IsPointer)
                        continue;

                    if (fieldType == typeof(AnimationClip))
                    {
                        gatherer += (obj, clips) =>
                        {
                            var clip = (AnimationClip)field.GetValue(obj);
                            clips.Gather(clip);
                        };
                    }
                    else if (typeof(IAnimationClipSource).IsAssignableFrom(fieldType) ||
                        typeof(IAnimationClipCollection).IsAssignableFrom(fieldType))
                    {
                        gatherer += (obj, clips) =>
                        {
                            var source = field.GetValue(obj);
                            clips.GatherFromSource(source);
                        };
                    }
                    else if (typeof(ICollection).IsAssignableFrom(fieldType))
                    {
                        gatherer += (obj, clips) =>
                        {
                            var collection = (ICollection)field.GetValue(obj);
                            if (collection != null)
                            {
                                foreach (var item in collection)
                                {
                                    GatherFromObject(item, clips, depth + 1);
                                }
                            }
                        };
                    }
                    else
                    {
                        gatherer += (obj, clips) =>
                        {
                            var source = field.GetValue(obj);
                            if (source == null)
                                return;

                            var sourceObject = source as Object;
                            if (!ReferenceEquals(sourceObject, null) && sourceObject == null)
                                return;

                            GatherFromObject(source, clips, depth + 1);
                        };
                    }
                }

                type = type.BaseType;
            }

            return gatherer;
        }

        /************************************************************************************************************************/
    }
}

#endif

