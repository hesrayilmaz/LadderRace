// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Animancer
{
    partial class AnimancerPlayable
    {
        /// <summary>
        /// A dictionary of <see cref="AnimancerState"/>s mapped to their <see cref="AnimancerState.Key"/>.
        /// </summary>
        public sealed class StateDictionary : IEnumerable<KeyValuePair<object, AnimancerState>>, IAnimationClipCollection
        {
            /************************************************************************************************************************/

            /// <summary>The <see cref="AnimancerPlayable"/> at the root of the graph.</summary>
            private readonly AnimancerPlayable Root;

            /// <summary><see cref="AnimancerState.Key"/> mapped to <see cref="AnimancerState"/>.</summary>
            private readonly Dictionary<object, AnimancerState>
                States = new Dictionary<object, AnimancerState>(FastComparer.Instance);

            /************************************************************************************************************************/

            /// <summary>[Internal] Constructs a new <see cref="StateDictionary"/>.</summary>
            internal StateDictionary(AnimancerPlayable root)
            {
                Root = root;
            }

            /************************************************************************************************************************/

            /// <summary>The number of states that have been registered with a <see cref="AnimancerState.Key"/>.</summary>
            public int Count { get { return States.Count; } }

            /************************************************************************************************************************/

            internal void Clear()
            {
                States.Clear();
            }

            /************************************************************************************************************************/
            #region Create
            /************************************************************************************************************************/

            /// <summary>
            /// Creates and returns a new <see cref="ClipState"/> to play the `clip`.
            /// <para></para>
            /// This method uses <see cref="GetKey"/> to determine the <see cref="AnimancerState.Key"/>.
            /// <para></para>
            /// To create a state on a different layer, call <c>animancer.Layers[x].CreateState(clip)</c> instead.
            /// </summary>
            public ClipState Create(AnimationClip clip)
            {
                return Root.Layers[0].CreateState(clip);
            }

            /// <summary>
            /// Creates and returns a new <see cref="ClipState"/> to play the `clip` and registers it with the `key`.
            /// <para></para>
            /// To create a state on a different layer, call <c>animancer.Layers[x].CreateState(key, clip)</c> instead.
            /// </summary>
            public ClipState Create(object key, AnimationClip clip)
            {
                return Root.Layers[0].CreateState(key, clip);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls <see cref="GetOrCreate(AnimationClip, bool)"/> for each of the specified clips.
            /// <para></para>
            /// If you only want to create a single state, use <see cref="AnimancerLayer.CreateState(AnimationClip)"/>.
            /// </summary>
            public void CreateIfNew(AnimationClip clip0, AnimationClip clip1)
            {
                GetOrCreate(clip0);
                GetOrCreate(clip1);
            }

            /// <summary>
            /// Calls <see cref="GetOrCreate(AnimationClip, bool)"/> for each of the specified clips.
            /// <para></para>
            /// If you only want to create a single state, use <see cref="AnimancerLayer.CreateState(AnimationClip)"/>.
            /// </summary>
            public void CreateIfNew(AnimationClip clip0, AnimationClip clip1, AnimationClip clip2)
            {
                GetOrCreate(clip0);
                GetOrCreate(clip1);
                GetOrCreate(clip2);
            }

            /// <summary>
            /// Calls <see cref="GetOrCreate(AnimationClip, bool)"/> for each of the specified clips.
            /// <para></para>
            /// If you only want to create a single state, use <see cref="AnimancerLayer.CreateState(AnimationClip)"/>.
            /// </summary>
            public void CreateIfNew(AnimationClip clip0, AnimationClip clip1, AnimationClip clip2, AnimationClip clip3)
            {
                GetOrCreate(clip0);
                GetOrCreate(clip1);
                GetOrCreate(clip2);
                GetOrCreate(clip3);
            }

            /// <summary>
            /// Calls <see cref="GetOrCreate(AnimationClip, bool)"/> for each of the specified `clips`.
            /// <para></para>
            /// If you only want to create a single state, use <see cref="AnimancerLayer.CreateState(AnimationClip)"/>.
            /// </summary>
            public void CreateIfNew(params AnimationClip[] clips)
            {
                if (clips == null)
                    return;

                var count = clips.Length;
                for (int i = 0; i < count; i++)
                {
                    var clip = clips[i];
                    if (clip != null)
                        GetOrCreate(clip);
                }
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Access
            /************************************************************************************************************************/

            /// <summary>
            /// The <see cref="AnimancerLayer.CurrentState"/> on layer 0.
            /// <para></para>
            /// Specifically, this is the state that was most recently started using any of the Play methods on that layer.
            /// States controlled individually via methods in the <see cref="AnimancerState"/> itself will not register in
            /// this property.
            /// </summary>
            public AnimancerState Current { get { return Root.Layers[0].CurrentState; } }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls <see cref="GetKey"/> then returns the state registered with that key, or null if none exists.
            /// </summary>
            public AnimancerState this[AnimationClip clip]
            {
                get
                {
                    if (clip != null)
                        return this[Root.GetKey(clip)];
                    else
                        return null;
                }
            }

            /// <summary>
            /// Returns the state registered with the <see cref="IHasKey.Key"/>, or null if none exists.
            /// </summary>
            public AnimancerState this[IHasKey hasKey]
            {
                get { return this[hasKey.Key]; }
            }

            /// <summary>
            /// Returns the state registered with the `key`, or null if none exists.
            /// </summary>
            public AnimancerState this[object key]
            {
                get
                {
                    AnimancerState state;
                    TryGet(key, out state);
                    return state;
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls <see cref="GetKey"/> then passes the key to
            /// <see cref="TryGet(object, out AnimancerState)"/> and returns the result.
            /// </summary>
            public bool TryGet(AnimationClip clip, out AnimancerState state)
            {
                if (clip != null)
                {
                    return States.TryGetValue(Root.GetKey(clip), out state);
                }
                else
                {
                    state = null;
                    return false;
                }
            }

            /// <summary>
            /// Passes the <see cref="IHasKey.Key"/> into <see cref="TryGet(object, out AnimancerState)"/>
            /// and returns the result.
            /// </summary>
            public bool TryGet(IHasKey hasKey, out AnimancerState state)
            {
                if (hasKey != null)
                {
                    return States.TryGetValue(hasKey.Key, out state);
                }
                else
                {
                    state = null;
                    return false;
                }
            }

            /// <summary>
            /// If a state is registered with the `key`, this method outputs it as the `state` and returns true. Otherwise
            /// `state` is set to null and this method returns false.
            /// </summary>
            public bool TryGet(object key, out AnimancerState state)
            {
                return States.TryGetValue(key, out state);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls <see cref="AnimancerPlayable.GetKey"/> and returns the state which registered with that key or
            /// creates one if it doesn't exist.
            /// <para></para>
            /// If the state already exists but has the wrong <see cref="AnimancerState.Clip"/>, the `allowSetClip`
            /// parameter determines what will happen. False causes it to throw an <see cref="ArgumentException"/> while
            /// true allows it to change the <see cref="AnimancerState.Clip"/>. Note that the change is somewhat costly to
            /// performance to use with caution.
            /// </summary>
            /// <exception cref="ArgumentException"/>
            public AnimancerState GetOrCreate(AnimationClip clip, bool allowSetClip = false)
            {
                return GetOrCreate(Root.GetKey(clip), clip, allowSetClip);
            }

            /// <summary>
            /// Returns the state registered with the `transition`s <see cref="IHasKey.Key"/> if there is one. Otherwise
            /// this method uses <see cref="ITransition.CreateState"/> to create a new one and registers it with
            /// that key before returning it.
            /// </summary>
            public AnimancerState GetOrCreate(ITransition transition)
            {
                var key = transition.Key;

                AnimancerState state;
                if (!States.TryGetValue(key, out state))
                {
                    state = transition.CreateState(Root.Layers[0]);
                    Root.States.Register(key, state);
                }

                return state;
            }

            /// <summary>
            /// Returns the state which registered with the `key` or creates one if it doesn't exist.
            /// <para></para>
            /// If the state already exists but has the wrong <see cref="AnimancerState.Clip"/>, the `allowSetClip`
            /// parameter determines what will happen. False causes it to throw an <see cref="ArgumentException"/> while
            /// true allows it to change the <see cref="AnimancerState.Clip"/>. Note that the change is somewhat costly to
            /// performance to use with caution.
            /// </summary>
            /// <exception cref="ArgumentException"/>
            /// <exception cref="ArgumentNullException">Thrown if the `key` is null.</exception>
            /// <remarks>See also: <see cref="AnimancerLayer.GetOrCreateState(object, AnimationClip, bool)"/></remarks>
            public AnimancerState GetOrCreate(object key, AnimationClip clip, bool allowSetClip = false)
            {
                if (key == null)
                    throw new ArgumentNullException("key");

                AnimancerState state;
                if (States.TryGetValue(key, out state))
                {
                    // If a state exists with the 'key' but has the wrong clip, either change it or complain.
                    if (!ReferenceEquals(state.Clip, clip))
                    {
                        if (allowSetClip)
                        {
                            state.Clip = clip;
                        }
                        else
                        {
                            throw new ArgumentException(string.Concat(
                                "A state already exists using the specified 'key', but has a different AnimationClip:",
                                "\n - Key: ", key.ToString(),
                                "\n - Existing Clip: ", state.Clip.ToString(),
                                "\n - New Clip: ", clip.ToString()));
                        }
                    }
                }
                else
                {
                    state = Root.Layers[0].CreateState(key, clip);
                }

                return state;
            }

            /************************************************************************************************************************/

            /// <summary>[Internal]
            /// Registers the `state` in this dictionary so the `key` can be used to get it later on using
            /// <see cref="this[object]"/>.
            /// </summary>
            internal void Register(object key, AnimancerState state)
            {
                if (key != null)
                    States.Add(key, state);

                state._Key = key;
            }

            /// <summary>[Internal]
            /// Removes the `state` from this dictionary.
            /// </summary>
            internal void Unregister(AnimancerState state)
            {
                if (state._Key == null)
                    return;

                States.Remove(state._Key);
                state._Key = null;
            }

            /************************************************************************************************************************/
            #region Enumeration
            /************************************************************************************************************************/
            // IEnumerable for 'foreach' statements.
            /************************************************************************************************************************/

            /// <summary>
            /// Returns an enumerator that will iterate through all states in each layer (not states inside mixers).
            /// </summary>
            public IEnumerator<KeyValuePair<object, AnimancerState>> GetEnumerator()
            {
                return States.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

            /************************************************************************************************************************/

            /// <summary>[<see cref="IAnimationClipCollection"/>]
            /// Gathers all the animations in all layers.
            /// </summary>
            public void GatherAnimationClips(ICollection<AnimationClip> clips)
            {
                foreach (var state in States.Values)
                    clips.GatherFromSource(state);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Destroy
            /************************************************************************************************************************/

            /// <summary>
            /// Calls <see cref="AnimancerState.Destroy"/> on the state associated with the `clip` (if any).
            /// Returns true if the state existed.
            /// </summary>
            public bool Destroy(AnimationClip clip)
            {
                if (clip == null)
                    return false;

                return Destroy(Root.GetKey(clip));
            }

            /// <summary>
            /// Calls <see cref="AnimancerState.Destroy"/> on the state associated with the <see cref="IHasKey.Key"/>
            /// (if any). Returns true if the state existed.
            /// </summary>
            public bool Destroy(IHasKey hasKey)
            {
                if (hasKey == null)
                    return false;

                return Destroy(hasKey.Key);
            }

            /// <summary>
            /// Calls <see cref="AnimancerState.Destroy"/> on the state associated with the `key` (if any).
            /// Returns true if the state existed.
            /// </summary>
            public bool Destroy(object key)
            {
                if (key == null)
                    return false;

                AnimancerState state;
                if (States.TryGetValue(key, out state))
                {
                    state.Destroy();
                    return true;
                }

                return false;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls <see cref="Destroy(AnimationClip)"/> on each of the `clips`.
            /// </summary>
            public void DestroyAll(IList<AnimationClip> clips)
            {
                if (clips == null)
                    return;

                for (int i = 0; i < clips.Count; i++)
                    Destroy(clips[i]);
            }

            /// <summary>
            /// Calls <see cref="Destroy(AnimationClip)"/> on each of the `clips`.
            /// </summary>
            public void DestroyAll(IEnumerable<AnimationClip> clips)
            {
                if (clips == null)
                    return;

                foreach (var clip in clips)
                    Destroy(clip);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls <see cref="Destroy(AnimationClip)"/> on all states gathered by
            /// <see cref="IAnimationClipSource.GetAnimationClips"/>.
            /// </summary>
            public void DestroyAll(IAnimationClipSource source)
            {
                if (source == null)
                    return;

                var clips = ObjectPool.AcquireList<AnimationClip>();
                for (int i = 0; i < clips.Count; i++)
                    Destroy(clips[i]);
                ObjectPool.Release(clips);
            }

            /// <summary>
            /// Calls <see cref="Destroy(AnimationClip)"/> on all states gathered by
            /// <see cref="IAnimationClipCollection.GatherAnimationClips"/>.
            /// </summary>
            public void DestroyAll(IAnimationClipCollection source)
            {
                if (source == null)
                    return;

                var clips = ObjectPool.AcquireSet<AnimationClip>();
                foreach (var clip in clips)
                    Destroy(clip);
                ObjectPool.Release(clips);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Destroys all states connected to all layers (regardless of whether they are actually registered in this
            /// dictionary).
            /// </summary>
            public void DestroyAll()
            {
                var count = Root.Layers.Count;
                while (--count >= 0)
                    Root.Layers._Layers[count].DestroyStates();

                States.Clear();
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Key Error Methods
#if UNITY_EDITOR
            /************************************************************************************************************************/
            // These are overloads of other methods that take a System.Object key to ensure the user doesn't try to use an
            // AnimancerState as a key, since the whole point of a key is to identify a state in the first place.
            /************************************************************************************************************************/

            /// <summary>[Warning]
            /// You should not use an <see cref="AnimancerState"/> as a key.
            /// The whole point of a key is to identify a state in the first place.
            /// </summary>
            [System.Obsolete("You should not use an AnimancerState as a key. The whole point of a key is to identify a state in the first place.", true)]
            public AnimancerState this[AnimancerState key]
            {
                get { return key; }
            }

            /// <summary>[Warning]
            /// You should not use an <see cref="AnimancerState"/> as a key.
            /// The whole point of a key is to identify a state in the first place.
            /// </summary>
            [System.Obsolete("You should not use an AnimancerState as a key. The whole point of a key is to identify a state in the first place.", true)]
            public bool TryGet(AnimancerState key, out AnimancerState state)
            {
                state = key;
                return true;
            }

            /// <summary>[Warning]
            /// You should not use an <see cref="AnimancerState"/> as a key.
            /// The whole point of a key is to identify a state in the first place.
            /// </summary>
            [System.Obsolete("You should not use an AnimancerState as a key. The whole point of a key is to identify a state in the first place.", true)]
            public AnimancerState GetOrCreate(AnimancerState key, AnimationClip clip)
            {
                return key;
            }

            /// <summary>[Warning]
            /// You should not use an <see cref="AnimancerState"/> as a key.
            /// Just call <see cref="AnimancerState.Destroy"/>.
            /// </summary>
            [System.Obsolete("You should not use an AnimancerState as a key. Just call AnimancerState.Destroy.", true)]
            public bool Destroy(AnimancerState key)
            {
                key.Destroy();
                return true;
            }

            /************************************************************************************************************************/
#endif
            #endregion
            /************************************************************************************************************************/
        }
    }
}

