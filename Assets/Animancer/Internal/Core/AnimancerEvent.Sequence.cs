// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Animancer
{
    partial struct AnimancerEvent
    {
        /// <summary>
        /// A variable-size list of <see cref="AnimancerEvent"/>s which keeps itself sorted by
        /// <see cref="normalizedTime"/>.
        /// <para></para>
        /// Animancer Lite does not allow the use of events in a runtime build, except for <see cref="OnEnd"/>.
        /// </summary>
        public sealed partial class Sequence : IEnumerable<AnimancerEvent>
        {
            /************************************************************************************************************************/
            #region Fields and Properties
            /************************************************************************************************************************/

            private const string
                NoCallbackError = "Event has no callback",
                IndexTooHighError = "index must be less than Count and not negative";

            /// <summary>
            /// A zero length array of <see cref="AnimancerEvent"/>s which is used by all lists before any elements are
            /// added to them (unless their <see cref="Capacity"/> is set manually).
            /// </summary>
            public static readonly AnimancerEvent[] EmptyArray = new AnimancerEvent[0];

            /// <summary>The initial <see cref="Capacity"/> that will be used if another value is not specified.</summary>
            public const int DefaultCapacity = 8;

            /************************************************************************************************************************/

            /// <summary>
            /// An <see cref="AnimancerEvent"/> which denotes the end of the animation. Its values can be accessed via
            /// <see cref="OnEnd"/> and <see cref="NormalizedEndTime"/>.
            /// <para></para>
            /// By default, the <see cref="normalizedTime"/> will be <see cref="float.NaN"/> so that it can choose the
            /// correct value based on the current play direction: forwards ends at 1 and backwards ends at 0.
            /// <para></para>
            /// Animancer Lite does not allow the <see cref="normalizedTime"/> to be changed in a runtime build.
            /// </summary>
            ///
            /// <example>
            /// <code>
            /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
            /// {
            ///     var state = animancer.Play(clip);
            ///     state.Events.OnEnd = OnAnimationEnd;
            ///     state.Events.NormalizedEndTime = 0.75f;
            ///
            ///     // Or set the time and callback at the same time:
            ///     state.Events.endEvent = new AnimancerEvent(0.75f, OnAnimationEnd);
            /// }
            ///
            /// void OnAnimationEnd()
            /// {
            ///     Debug.Log("Animation ended");
            /// }
            /// </code>
            /// </example>
            ///
            /// <remarks>
            /// See the documentation for more information about
            /// <see href="https://kybernetik.com.au/animancer/docs/manual/events/end">
            /// End Events</see>.
            /// </remarks>
            public AnimancerEvent endEvent = new AnimancerEvent(float.NaN, null);

            /// <summary>The internal array in which the events are stored (excluding the <see cref="endEvent"/>).</summary>
            private AnimancerEvent[] _Events;

            /// <summary>[Pro-Only] The number of events in this sequence (excluding the <see cref="endEvent"/>).</summary>
            public int Count { get; private set; }

            /// <summary>[Pro-Only]
            /// The number of times the contents of this sequence have been modified. This applies to general events,
            /// but not the <see cref="endEvent"/>.
            /// </summary>
            public int Version { get; private set; }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Constructors
            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="Sequence"/> which starts at 0 <see cref="Capacity"/>.
            /// <para></para>
            /// Adding anything to the list will set the <see cref="Capacity"/> = <see cref="DefaultCapacity"/>
            /// and then double it whenever the <see cref="Count"/> would exceed the <see cref="Capacity"/>.
            /// </summary>
            public Sequence()
            {
                _Events = EmptyArray;
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Creates a new <see cref="Sequence"/> which starts with the specified <see cref="Capacity"/>. It will be
            /// initially empty, but will have room for the given number of elements before any reallocations are
            /// required.
            /// </summary>
            public Sequence(int capacity)
            {
                _Events = capacity > 0 ? new AnimancerEvent[capacity] : EmptyArray;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="Sequence"/>, copying the contents of `copyFrom` into it.
            /// </summary>
            public Sequence(Sequence copyFrom)
            {
                CopyFrom(copyFrom);
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Creates a new <see cref="Sequence"/>, copying and sorting the contents of the `collection` into it.
            /// The <see cref="Count"/> and <see cref="Capacity"/> will be equal to the
            /// <see cref="ICollection{T}.Count"/>.
            /// </summary>
            public Sequence(ICollection<AnimancerEvent> collection)
            {
                if (collection == null)
                    throw new ArgumentNullException("collection");

                var count = collection.Count;
                if (count == 0)
                {
                    _Events = EmptyArray;
                }
                else
                {
                    _Events = new AnimancerEvent[count];
                    AddRange(collection);
                }
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Creates a new <see cref="Sequence"/>, copying and sorting the contents of the `enumerable` into it.
            /// </summary>
            public Sequence(IEnumerable<AnimancerEvent> enumerable)
            {
                if (enumerable == null)
                    throw new ArgumentNullException("enumerable");

                _Events = EmptyArray;
                AddRange(enumerable);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Iteration
            /************************************************************************************************************************/

            /// <summary>
            /// Indicates whether the list has any events in it or the <see cref="endEvent"/> event's
            /// <see cref="normalizedTime"/> is not at the default value (1).
            /// </summary>
            public bool IsEmpty
            {
                get
                {
                    return
                        endEvent.callback == null &&
                        float.IsNaN(endEvent.normalizedTime) &&
                        Count == 0;
                }
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// The size of the internal array used to hold events.
            /// <para></para>
            /// When set, the array is reallocated to the given size.
            /// <para></para>
            /// By default, the <see cref="Capacity"/> starts at 0 and increases to the <see cref="DefaultCapacity"/>
            /// when the first event is added.
            /// </summary>
            public int Capacity
            {
                get { return _Events.Length; }
                set
                {
                    if (value < Count)
                        throw new ArgumentOutOfRangeException("value", "Capacity cannot be set lower than Count");

                    if (value == _Events.Length)
                        return;

                    if (value > 0)
                    {
                        var newEvents = new AnimancerEvent[value];
                        if (Count > 0)
                            Array.Copy(_Events, 0, newEvents, 0, Count);
                        _Events = newEvents;
                    }
                    else
                    {
                        _Events = EmptyArray;
                    }
                }
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only] Gets the event at the specified `index`.</summary>
            public AnimancerEvent this[int index]
            {
                get
                {
                    Debug.Assert((uint)index < (uint)Count, IndexTooHighError);
                    return _Events[index];
                }
            }

            /************************************************************************************************************************/

            /// <summary>[Assert]
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if the <see cref="normalizedTime"/> of any events
            /// is less than 0 or greater than or equal to 1.
            /// <para></para>
            /// This does not include the <see cref="endEvent"/> since it works differently to other events.
            /// </summary>
            [System.Diagnostics.Conditional(Strings.Assert)]
            public void AssertNormalizedTimes()
            {
                if (Count == 0 ||
                    (_Events[0].normalizedTime >= 0 && _Events[Count - 1].normalizedTime < 1))
                    return;

                throw new ArgumentOutOfRangeException("The normalized time of an event in the Sequence is" +
                    " < 0 or >= 1, which is not allowed on looping animations. " + DeepToString());
            }

            /// <summary>[Assert]
            /// Calls <see cref="AssertNormalizedTimes()"/> if `isLooping` is true.
            /// </summary>
            [System.Diagnostics.Conditional(Strings.Assert)]
            public void AssertNormalizedTimes(bool isLooping)
            {
                if (isLooping)
                    AssertNormalizedTimes();
            }

            /************************************************************************************************************************/

            /// <summary>Returns a string containing the details of all events in this sequence.</summary>
            public string DeepToString(bool multiLine = true)
            {
                var text = new StringBuilder()
                    .Append(ToString())
                    .Append(" [")
                    .Append(Count)
                    .Append(multiLine ? "]\n{" : "] { ");

                for (int i = 0; i < Count; i++)
                {
                    if (multiLine)
                        text.Append("\n    ");
                    else if (i > 0)
                        text.Append(", ");

                    text.Append(this[i]);
                }

                text.Append(multiLine ? "\n}\nendEvent=" : " } (endEvent=")
                    .Append(endEvent);

                if (!multiLine)
                    text.Append(")");

                return text.ToString();
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only] Returns an <see cref="Enumerator"/> for this sequence.</summary>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            IEnumerator<AnimancerEvent> IEnumerable<AnimancerEvent>.GetEnumerator()
            {
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(this);
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// An iterator that can cycle through every event in a <see cref="Sequence"/> except for the
            /// <see cref="endEvent"/>.
            /// </summary>
            public struct Enumerator : IEnumerator<AnimancerEvent>
            {
                /************************************************************************************************************************/

                /// <summary>The target <see cref="AnimancerEvent.Sequence"/>.</summary>
                public readonly Sequence Sequence;

                private int _Index;
                private int _Version;
                private AnimancerEvent _Current;

                private const string InvalidVersion =
                    "AnimancerEvent.Sequence was modified. Enumeration operation may not execute.";

                /************************************************************************************************************************/

                /// <summary>The event this iterator is currently pointing to.</summary>
                public AnimancerEvent Current { get { return _Current; } }

                /// <summary>The event this iterator is currently pointing to.</summary>
                object IEnumerator.Current
                {
                    get
                    {
                        if (_Index == 0 || _Index == Sequence.Count + 1)
                            throw new InvalidOperationException(
                                "Operation is not valid due to the current state of the object.");

                        return Current;
                    }
                }

                /************************************************************************************************************************/

                /// <summary>Creates a new <see cref="Enumerator"/>.</summary>
                public Enumerator(Sequence sequence)
                {
                    Sequence = sequence;
                    _Index = 0;
                    _Version = sequence.Version;
                    _Current = default(AnimancerEvent);
                }

                /************************************************************************************************************************/

                void IDisposable.Dispose() { }

                /************************************************************************************************************************/

                /// <summary>
                /// Moves to the next event in the <see cref="Sequence"/> and returns true if there is one.
                /// </summary>
                /// <exception cref="InvalidOperationException">
                /// Thrown if the <see cref="Version"/> has changed since this iterator was created.
                /// </exception>
                public bool MoveNext()
                {
                    if (_Version != Sequence.Version)
                        throw new InvalidOperationException(InvalidVersion);

                    if ((uint)_Index < (uint)Sequence.Count)
                    {
                        _Current = Sequence._Events[_Index];
                        _Index++;
                        return true;
                    }
                    else
                    {
                        _Index = Sequence.Count + 1;
                        _Current = default(AnimancerEvent);
                        return false;
                    }
                }

                /************************************************************************************************************************/

                /// <summary>
                /// Returns this iterator to the start of the <see cref="Sequence"/>.
                /// </summary>
                /// <exception cref="InvalidOperationException">
                /// Thrown if the <see cref="Version"/> has changed since this iterator was created.
                /// </exception>
                void IEnumerator.Reset()
                {
                    if (_Version != Sequence.Version)
                        throw new InvalidOperationException(InvalidVersion);

                    _Index = 0;
                    _Current = default(AnimancerEvent);
                }

                /************************************************************************************************************************/
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Modification
            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Adds the given event to this list. The <see cref="Count"/> is increased by one and if required, the
            /// <see cref="Capacity"/> is doubled to fit the new event.
            /// <para></para>
            /// This methods returns the index at which the event is added, which is determined by its
            /// <see cref="normalizedTime"/> in order to keep the list sorted in ascending order. If there are already
            /// any events with the same <see cref="normalizedTime"/>, the new event is added immediately after them.
            /// </summary>
            public int Add(AnimancerEvent animancerEvent)
            {
                Debug.Assert(animancerEvent.callback != null, NoCallbackError);
                var index = Insert(animancerEvent.normalizedTime);
                _Events[index] = animancerEvent;
                return index;
            }

            /// <summary>[Pro-Only]
            /// Adds the given event to this list. The <see cref="Count"/> is increased by one and if required, the
            /// <see cref="Capacity"/> is doubled to fit the new event.
            /// <para></para>
            /// This methods returns the index at which the event is added, which is determined by its
            /// <see cref="normalizedTime"/> in order to keep the list sorted in ascending order. If there are already
            /// any events with the same <see cref="normalizedTime"/>, the new event is added immediately after them.
            /// </summary>
            public int Add(float normalizedTime, Action callback)
            {
                return Add(new AnimancerEvent(normalizedTime, callback));
            }

            /// <summary>[Pro-Only]
            /// Adds the given event to this list. The <see cref="Count"/> is increased by one and if required, the
            /// <see cref="Capacity"/> is doubled to fit the new event.
            /// <para></para>
            /// This methods returns the index at which the event is added, which is determined by its
            /// <see cref="normalizedTime"/> in order to keep the list sorted in ascending order. If there are already
            /// any events with the same <see cref="normalizedTime"/>, the new event is added immediately after them.
            /// </summary>
            public int Add(int indexHint, AnimancerEvent animancerEvent)
            {
                Debug.Assert(animancerEvent.callback != null, NoCallbackError);
                indexHint = Insert(indexHint, animancerEvent.normalizedTime);
                _Events[indexHint] = animancerEvent;
                return indexHint;
            }

            /// <summary>[Pro-Only]
            /// Adds the given event to this list. The <see cref="Count"/> is increased by one and if required, the
            /// <see cref="Capacity"/> is doubled to fit the new event.
            /// <para></para>
            /// This methods returns the index at which the event is added, which is determined by its
            /// <see cref="normalizedTime"/> in order to keep the list sorted in ascending order. If there are already
            /// any events with the same <see cref="normalizedTime"/>, the new event is added immediately after them.
            /// </summary>
            public int Add(int indexHint, float normalizedTime, Action callback)
            {
                return Add(indexHint, new AnimancerEvent(normalizedTime, callback));
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Adds every event in the `enumerable` to this list. The <see cref="Count"/> is increased by one and if
            /// required, the <see cref="Capacity"/> is doubled to fit the new event.
            /// <para></para>
            /// This methods returns the index at which the event is added, which is determined by its
            /// <see cref="normalizedTime"/> in order to keep the list sorted in ascending order. If there are already
            /// any events with the same <see cref="normalizedTime"/>, the new event is added immediately after them.
            /// </summary>
            public void AddRange(IEnumerable<AnimancerEvent> enumerable)
            {
                foreach (var item in enumerable)
                    Add(item);
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Replaces the <see cref="callback"/> of the event at the specified `index`.
            /// </summary>
            public void Set(int index, Action callback)
            {
                var animancerEvent = _Events[index];
                animancerEvent.callback = callback;
                _Events[index] = animancerEvent;
                Version++;
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Determines the index where a new event with the specified `normalizedTime` should be added in order to
            /// keep this list sorted, increases the <see cref="Count"/> by one, doubles the <see cref="Capacity"/> if
            /// required, moves any existing events to open up the chosen index, and returns that index.
            /// <para></para>
            /// This overload starts searching for the desired index from the end of the list, using the assumption
            /// that elements will usually be added in order.
            /// </summary>
            private int Insert(float normalizedTime)
            {
                var index = Count;
                while (index > 0 && _Events[index - 1].normalizedTime > normalizedTime)
                    index--;
                Insert(index);
                return index;
            }

            /// <summary>[Pro-Only]
            /// Determines the index where a new event with the specified `normalizedTime` should be added in order to
            /// keep this list sorted, increases the <see cref="Count"/> by one, doubles the <see cref="Capacity"/> if
            /// required, moves any existing events to open up the chosen index, and returns that index.
            /// <para></para>
            /// This overload starts searching for the desired index from the `hint`.
            /// </summary>
            private int Insert(int hint, float normalizedTime)
            {
                if (hint >= Count)
                    return Insert(normalizedTime);

                if (_Events[hint].normalizedTime > normalizedTime)
                {
                    while (hint > 0 && _Events[hint - 1].normalizedTime > normalizedTime)
                        hint--;
                }
                else
                {
                    while (hint < Count && _Events[hint].normalizedTime <= normalizedTime)
                        hint++;
                }

                Insert(hint);
                return hint;
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Increases the <see cref="Count"/> by one, doubles the <see cref="Capacity"/> if required, and moves any
            /// existing events to open up the `index`.
            /// </summary>
            private void Insert(int index)
            {
                Debug.Assert((uint)index <= (uint)Count, "index must be less than or equal to Count");

                var capacity = _Events.Length;
                if (Count == capacity)
                {
                    if (capacity == 0)
                    {
                        _Events = new AnimancerEvent[DefaultCapacity];
                    }
                    else
                    {
                        capacity *= 2;
                        if (capacity < DefaultCapacity)
                            capacity = DefaultCapacity;

                        var newEvents = new AnimancerEvent[capacity];

                        Array.Copy(_Events, 0, newEvents, 0, index);
                        if (Count > index)
                            Array.Copy(_Events, index, newEvents, index + 1, Count - index);

                        _Events = newEvents;
                    }
                }
                else if (Count > index)
                {
                    Array.Copy(_Events, index, _Events, index + 1, Count - index);
                }

                Count++;
                Version++;
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Removes the event at the specified `index` from this list by decrementing the <see cref="Count"/> and
            /// copying all events after the removed one down one place.
            /// </summary>
            public void Remove(int index)
            {
                Debug.Assert((uint)index < (uint)Count, IndexTooHighError);
                Count--;
                if (index < Count)
                    Array.Copy(_Events, index + 1, _Events, index, Count - index);
                _Events[Count] = default(AnimancerEvent);
                Version++;
            }

            /// <summary>[Pro-Only]
            /// Removes the `animancerEvent` from this list by decrementing the <see cref="Count"/> and copying all
            /// events after the removed one down one place. Returns true if the event was found and removed.
            /// </summary>
            public bool Remove(AnimancerEvent animancerEvent)
            {
                var index = Array.IndexOf(_Events, animancerEvent);
                if (index >= 0)
                {
                    Remove(index);
                    return true;
                }
                else return false;
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Removes all events except the <see cref="endEvent"/>.
            /// <seealso cref="Clear"/>
            /// </summary>
            public void RemoveAll()
            {
                Array.Clear(_Events, 0, Count);
                Count = 0;
                Version++;
            }

            /// <summary>
            /// Removes all events, including the <see cref="endEvent"/>.
            /// <seealso cref="RemoveAll"/>
            /// </summary>
            public void Clear()
            {
                RemoveAll();
                endEvent = new AnimancerEvent(float.NaN, null);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region On End
            /************************************************************************************************************************/

            /// <summary>
            /// Shorthand for the <c>endEvent.callback</c>. This callback is triggered when the animation passes the
            /// <see cref="NormalizedEndTime"/> (not when the state is interrupted or exited for whatever reason).
            /// <para></para>
            /// Unlike regular events, this callback will be triggered every frame while it is past the end so if you
            /// want to ensure that your callback only occurs once, you will need to clear it as part of that callback.
            /// <para></para>
            /// This callback is automatically cleared by <see cref="AnimancerState.Play"/>,
            /// <see cref="AnimancerState.OnStartFade"/>, and <see cref="AnimancerState.Stop"/>.
            /// </summary>
            ///
            /// <example>
            /// <code>
            /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
            /// {
            ///     var state = animancer.Play(clip);
            ///     state.Events.OnEnd = OnAnimationEnd;
            ///     state.Events.NormalizedEndTime = 0.75f;
            ///
            ///     // Or set the time and callback at the same time:
            ///     state.Events.endEvent = new AnimancerEvent(0.75f, OnAnimationEnd);
            /// }
            ///
            /// void OnAnimationEnd()
            /// {
            ///     Debug.Log("Animation ended");
            /// }
            /// </code>
            /// </example>
            ///
            /// <remarks>
            /// See the documentation for more information about
            /// <see href="https://kybernetik.com.au/animancer/docs/manual/events/end">
            /// End Events</see>.
            /// </remarks>
            public Action OnEnd
            {
                get { return endEvent.callback; }
                set { endEvent.callback = value; }
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Shorthand for <c>endEvent.normalizedTime</c>.
            /// <para></para>
            /// By default, this value will be <see cref="float.NaN"/> so that it can choose the correct value based on
            /// the current play direction: forwards ends at 1 and backwards ends at 0.
            /// <para></para>
            /// Animancer Lite does not allow this value to be changed in a runtime build.
            /// </summary>
            ///
            /// <example>
            /// <code>
            /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
            /// {
            ///     var state = animancer.Play(clip);
            ///     state.Events.OnEnd = OnAnimationEnd;
            ///     state.Events.NormalizedEndTime = 0.75f;
            ///
            ///     // Or set the time and callback at the same time:
            ///     state.Events.endEvent = new AnimancerEvent(0.75f, OnAnimationEnd);
            /// }
            ///
            /// void OnAnimationEnd()
            /// {
            ///     Debug.Log("Animation ended");
            /// }
            /// </code>
            /// </example>
            ///
            /// <remarks>
            /// See the documentation for more information about
            /// <see href="https://kybernetik.com.au/animancer/docs/manual/events/end">
            /// End Events</see>.
            /// </remarks>
            public float NormalizedEndTime
            {
                get { return endEvent.normalizedTime; }
                set { endEvent.normalizedTime = value; }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// The default <see cref="AnimancerState.NormalizedTime"/> for an animation to start at when playing
            /// forwards is 0 (the start of the animation) and when playing backwards is 1 (the end of the animation).
            /// <para></para>
            /// `speed` 0 or <see cref="float.NaN"/> will also return 0.
            /// </summary>
            /// <remarks>
            /// This method has nothing to do with events, so it is only here because of
            /// <see cref="GetDefaultNormalizedEndTime"/>.
            /// </remarks>
            public static float GetDefaultNormalizedStartTime(float speed)
            {
                return speed < 0 ? 1 : 0;
            }

            /// <summary>
            /// The default <see cref="normalizedTime"/> for an <see cref="endEvent"/> when playing forwards is 1 (the
            /// end of the animation) and when playing backwards is 0 (the start of the animation).
            /// <para></para>
            /// `speed` 0 or <see cref="float.NaN"/> will also return 1.
            /// </summary>
            public static float GetDefaultNormalizedEndTime(float speed)
            {
                return speed < 0 ? 0 : 1;
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Copying
            /************************************************************************************************************************/

            /// <summary>
            /// Copies all the events from the `source` to replace the previous contents of this list.
            /// </summary>
            public void CopyFrom(Sequence source)
            {
                var sourceCount = source.Count;

                if (Count > sourceCount)
                    Array.Clear(_Events, Count, sourceCount - Count);
                else if (_Events.Length < sourceCount)
                    Capacity = sourceCount;

                Count = sourceCount;

                Array.Copy(source._Events, 0, _Events, 0, sourceCount);

                endEvent = source.endEvent;
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="ICollection{T}"/>]
            /// Copies all the events from this list into the `array`, starting at the `index`.
            /// </summary>
            public void CopyTo(AnimancerEvent[] array, int index)
            {
                Array.Copy(_Events, 0, array, index, Count);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }
    }
}

