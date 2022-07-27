// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Animancer
{
    partial class AnimancerPlayable
    {
        /// <summary>[Pro-Only]
        /// A list of <see cref="AnimancerLayer"/>s with methods to control their mixing and masking.
        /// </summary>
        public sealed class LayerList : IEnumerable<AnimancerLayer>, IAnimationClipCollection
        {
            /************************************************************************************************************************/
            #region Fields
            /************************************************************************************************************************/

            /// <summary>The <see cref="AnimancerPlayable"/> at the root of the graph.</summary>
            private readonly AnimancerPlayable Root;

            /// <summary>[Internal] The layers which each manage their own set of animations.</summary>
            internal AnimancerLayer[] _Layers;

            /// <summary>[Internal] The <see cref="AnimationLayerMixerPlayable"/> which blends the layers.</summary>
            internal readonly AnimationLayerMixerPlayable LayerMixer;

            /// <summary>The number of layers that have actually been created.</summary>
            private int _Count;

            /************************************************************************************************************************/

            /// <summary>[Internal] Constructs a new <see cref="LayerList"/>.</summary>
            internal LayerList(AnimancerPlayable root, out Playable layerMixer)
            {
                Root = root;
                layerMixer = LayerMixer = AnimationLayerMixerPlayable.Create(root._Graph, 1);
                Root._Graph.Connect(LayerMixer, 0, Root._RootPlayable, 0);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region List Operations
            /************************************************************************************************************************/

            /// <summary>[Pro-Only] The number of layers in this list.</summary>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Thrown if the value is set higher than the <see cref="defaultCapacity"/>. This is simply a safety measure,
            /// so if you do actually need more layers you can just increase the limit.
            /// </exception>
            /// <exception cref="IndexOutOfRangeException">Thrown if the value is set to a negative number.</exception>
            public int Count
            {
                get { return _Count; }
                set
                {
                    var count = _Count;

                    CheckAgain:

                    if (value == count)
                        return;

                    if (value > count)// Increasing.
                    {
                        Add();
                        count++;
                        goto CheckAgain;
                    }
                    else// Decreasing.
                    {
                        if (_Layers != null)
                        {
                            while (value < count--)
                            {
                                var layer = _Layers[count];
                                if (layer._Playable.IsValid())
                                    Root._Graph.DestroySubgraph(layer._Playable);
                                layer.DestroyStates();
                            }

                            Array.Clear(_Layers, value, _Count - value);
                        }

                        _Count = value;

                        Root._LayerMixer.SetInputCount(value);
                    }
                }
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// If the <see cref="Count"/> is below the specified `min`, this method increases it to that value.
            /// </summary>
            public void SetMinCount(int min)
            {
                if (Count < min)
                    Count = min;
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// The maximum number of layers that can be created before an <see cref="ArgumentOutOfRangeException"/> will
            /// be thrown (default 4).
            /// <para></para>
            /// Lowering this value will not affect layers that have already been created.
            /// </summary>
            /// <example>
            /// To set this value automatically when the application starts, place the following method in any class:
            /// <code>[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
            /// private static void SetMaxLayerCount()
            /// {
            ///     Animancer.AnimancerPlayable.LayerList.defaultCapacity = 8;
            /// }</code>
            /// Otherwise you can set the <see cref="Capacity"/> of each individual list:
            /// <code>AnimancerComponent animancer;
            /// animancer.Layers.Capacity = 8;</code>
            /// </example>
            public static int defaultCapacity = 4;

            /// <summary>[Pro-Only]
            /// If the <see cref="defaultCapacity"/> is below the specified `min`, this method increases it to that value.
            /// </summary>
            public static void SetMinDefaultCapacity(int min)
            {
                if (defaultCapacity < min)
                    defaultCapacity = min;
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// The maximum number of layers that can be created before an <see cref="ArgumentOutOfRangeException"/> will
            /// be thrown. The initial capacity is determined by <see cref="defaultCapacity"/>.
            /// <para></para>
            /// Lowering this value will destroy any layers beyond the specified value.
            /// <para></para>
            /// Any changes to this value after a layer has been created will cause the allocation of a new array and
            /// garbage collection of the old one, so you should generally set it during initialisation.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is not greater than 0.</exception>
            public int Capacity
            {
                get { return _Layers != null ? _Layers.Length : defaultCapacity; }
                set
                {
                    if (value <= 0)
                        throw new ArgumentOutOfRangeException("value", "must be greater than 0 (" + value + " <= 0)");

                    if (value < _Count)
                        Count = value;

                    Array.Resize(ref _Layers, value);
                }
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Creates and returns a new <see cref="AnimancerLayer"/>. New layers will override earlier layers by default.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Thrown if the value is set higher than the <see cref="Capacity"/>. This is simply a safety measure,
            /// so if you do actually need more layers you can just increase the limit.
            /// </exception>
            public AnimancerLayer Add()
            {
                if (_Layers == null)
                    _Layers = new AnimancerLayer[defaultCapacity];

                var index = _Count;

                if (index >= _Layers.Length)
                    throw new ArgumentOutOfRangeException(
                        "Attempted to increase the layer count above the current capacity (" +
                        (index + 1) + " > " + _Layers.Length + "). This is simply a safety measure," +
                        " so if you do actually need more layers you can just increase the Capacity or defaultCapacity.");

                _Count = index + 1;
                Root._LayerMixer.SetInputCount(_Count);

                var layer = new AnimancerLayer(Root, index);
                _Layers[index] = layer;
                if (Root.KeepChildrenConnected)
                    layer.ConnectToGraph();
                return layer;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Returns the layer at the specified index. If it didn't already exist, this method creates it.
            /// </summary>
            public AnimancerLayer this[int index]
            {
                get
                {
                    SetMinCount(index + 1);
                    return _Layers[index];
                }
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Enumeration
            /************************************************************************************************************************/

            /// <summary>Returns an enumerator that will iterate through all layers.</summary>
            public IEnumerator<AnimancerLayer> GetEnumerator()
            {
                if (_Layers == null)
                    _Layers = new AnimancerLayer[defaultCapacity];

                return ((IEnumerable<AnimancerLayer>)_Layers).GetEnumerator();
            }

            /// <summary>Returns an enumerator that will iterate through all layers.</summary>
            IEnumerator IEnumerable.GetEnumerator()
            {
                if (_Layers == null)
                    _Layers = new AnimancerLayer[defaultCapacity];

                return _Layers.GetEnumerator();
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Returns an enumerator that will iterate through all states in each layer (not states inside mixers).
            /// </summary>
            public IEnumerable<AnimancerState> GetAllStateEnumerable()
            {
                var count = Count;
                for (int i = 0; i < count; i++)
                {
                    foreach (var state in _Layers[i])
                    {
                        yield return state;
                    }
                }
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="IAnimationClipCollection"/>]
            /// Gathers all the animations in all layers.
            /// </summary>
            public void GatherAnimationClips(ICollection<AnimationClip> clips)
            {
                clips.GatherFromSources(_Layers);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Layer Details
            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Checks whether the layer at the specified index is set to additive blending. Otherwise it will override any
            /// earlier layers.
            /// </summary>
            public bool IsAdditive(int index)
            {
                return LayerMixer.IsLayerAdditive((uint)index);
            }

            /// <summary>[Pro-Only]
            /// Sets the layer at the specified index to blend additively with earlier layers (if true) or to override them
            /// (if false). Newly created layers will override by default.
            /// </summary>
            public void SetAdditive(int index, bool value)
            {
                SetMinCount(index + 1);
                LayerMixer.SetLayerAdditive((uint)index, value);
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Sets an <see cref="AvatarMask"/> to determine which bones the layer at the specified index will affect.
            /// </summary>
            public void SetMask(int index, AvatarMask mask)
            {
                SetMinCount(index + 1);

#if UNITY_EDITOR
                _Layers[index]._Mask = mask;
#endif

                if (mask == null)
                    mask = new AvatarMask();

                LayerMixer.SetLayerMaskFromAvatarMask((uint)index, mask);
            }

            /************************************************************************************************************************/

            /// <summary>[Editor-Conditional]
            /// Sets the Inspector display name of the layer at the specified index. Note that layer names are Editor-Only
            /// so any calls to this method will automatically be compiled out of a runtime build.
            /// </summary>
            [System.Diagnostics.Conditional(Strings.EditorOnly)]
            public void SetName(int index, string name)
            {
                this[index].SetName(name);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// The average velocity of the root motion of all currently playing animations, taking their current
            /// <see cref="AnimancerNode.Weight"/> into account.
            /// </summary>
            public Vector3 AverageVelocity
            {
                get
                {
                    var velocity = default(Vector3);

                    for (int i = 0; i < _Count; i++)
                    {
                        var layer = _Layers[i];
                        velocity += layer.AverageVelocity * layer.Weight;
                    }

                    return velocity;
                }
            }

            /************************************************************************************************************************/

            /// <summary>[Internal]
            /// Connects or disconnects all children from their parent <see cref="Playable"/>.
            /// </summary>
            internal void SetWeightlessChildrenConnected(bool connected)
            {
                if (_Layers == null)
                    return;

                if (connected)
                {
                    var count = _Count;
                    while (--count >= 0)
                        _Layers[count].ConnectAllChildrenToGraph();
                }
                else
                {
                    var count = _Count;
                    while (--count >= 0)
                        _Layers[count].DisconnectWeightlessChildrenFromGraph();
                }
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }
    }
}

