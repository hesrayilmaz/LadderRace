// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animancer
{
    partial class AnimancerState
    {
        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="IUpdatable"/> that manages the events of this state.
        /// <para></para>
        /// This field is null by default, acquires its reference from an <see cref="ObjectPool"/> when accessed, and
        /// if it contains no events at the end of an update it releases the reference back to the pool.
        /// </summary>
        private EventUpdatable _EventUpdatable;

        /************************************************************************************************************************/

        /// <summary>
        /// A list of <see cref="AnimancerEvent"/>s that will occur while this state plays as well as one that
        /// specifically defines when this state ends.
        /// <para></para>
        /// Animancer Lite does not allow the use of events in a runtime build, except for
        /// <see cref="AnimancerEvent.Sequence.OnEnd"/>.
        /// </summary>
        public AnimancerEvent.Sequence Events
        {
            get
            {
                EventUpdatable.Acquire(this);
                return _EventUpdatable.Events;
            }
            set
            {
                if (value != null)
                {
                    EventUpdatable.Acquire(this);
                    _EventUpdatable.Events = value;
                }
                else if (_EventUpdatable != null)
                {
                    _EventUpdatable.Events = null;
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether this state currently has an <see cref="AnimancerEvent.Sequence"/> (since accessing the
        /// <see cref="Events"/> would automatically get one from the <see cref="ObjectPool"/>).
        /// </summary>
        public bool HasEvents { get { return _EventUpdatable != null; } }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="ObjectPool{T}.Capacity"/> for <see cref="AnimancerEvent.Sequence"/> and
        /// <see cref="EventUpdatable"/>.
        /// </summary>
        public static int EventPoolCapacity
        {
            get { return ObjectPool<EventUpdatable>.Capacity; }
            set
            {
                ObjectPool<EventUpdatable>.Capacity = value;
                ObjectPool<AnimancerEvent.Sequence>.Capacity = value;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// An <see cref="IUpdatable"/> which manages the triggering of events.
        /// </summary>
        private sealed class EventUpdatable : Key, IUpdatable
        {
            /************************************************************************************************************************/
            #region Pooling
            /************************************************************************************************************************/

            /// <summary>
            /// If the `state` has no <see cref="EventUpdatable"/>, this method gets one from the
            /// <see cref="ObjectPool"/>.
            /// </summary>
            public static void Acquire(AnimancerState state)
            {
                var updatable = state._EventUpdatable;
                if (updatable != null)
                    return;

                ObjectPool.Acquire(out updatable);

#if UNITY_ASSERTIONS
                if (updatable._State != null)
                    Debug.LogError(updatable + " already has a state even though it was in the list of spares.");

                if (updatable._Events != null)
                    Debug.LogError(updatable + " has event sequence even though it was in the list of spares.");

                if (updatable._GotEventsFromPool)
                    Debug.LogError(updatable + " is marked as having pooled events even though it has no events.");

                if (updatable._NextEventIndex != RecalculateEventIndex)
                    Debug.LogError(updatable + " has a _NextEventIndex even though it was pooled.");

                if (IsInList(updatable))
                    Debug.LogError(updatable + " is currently in a Keyed List even though it was in the list of spares.");
#endif

                updatable._IsLooping = state.IsLooping;
                updatable._State = state;
                state._EventUpdatable = updatable;
                state.Root.RequireUpdate(updatable);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Returns this <see cref="EventUpdatable"/> to the <see cref="ObjectPool"/>.
            /// </summary>
            private void Release()
            {
                if (_State == null)
                    return;

                _State.Root.CancelUpdate(this);

                if (_GotEventsFromPool)
                {
                    _Events.Clear();
                    ObjectPool.Release(_Events);
                    _GotEventsFromPool = false;
                }

                _Events = null;
                _State._EventUpdatable = null;
                _State = null;
                _NextEventIndex = RecalculateEventIndex;

                ObjectPool.Release(this);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// If the <see cref="AnimancerEvent.Sequence"/> was acquired from the <see cref="ObjectPool"/>, this
            /// method clears it. Otherwise it simply discards the reference.
            /// </summary>
            public static void TryClear(EventUpdatable events)
            {
                if (events != null && events._Events != null)
                {
                    events._NextEventIndex = RecalculateEventIndex;
                    if (events._GotEventsFromPool)
                    {
                        events._Events.Clear();
                        events._GotEventsFromPool = false;
                    }

                    events._Events = null;
                }
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/

            private AnimancerState _State;
            private AnimancerEvent.Sequence _Events;
            private bool _GotEventsFromPool;
            private bool _IsLooping;
            private float _PreviousTime;
            private int _NextEventIndex = RecalculateEventIndex;
            private int _SequenceVersion;
            private bool _WasPlayingForwards;

            private const int RecalculateEventIndex = int.MinValue;

            /// <summary>
            /// This system accounts for external modifications to the sequence, but modifying it while checking which
            /// of its events to update is not allowed because it would be impossible to efficiently keep track of
            /// which events have been checked/invoked and which still need to be checked.
            /// </summary>
            private const string SequenceVersionException =
                "AnimancerState.Events sequence was modified while iterating through it." +
                " Events in a sequence must not modify that sequence.";

            /************************************************************************************************************************/

            public AnimancerEvent.Sequence Events
            {
                get
                {
                    if (_Events == null)
                    {
                        ObjectPool.Acquire(out _Events);
                        _GotEventsFromPool = true;

#if UNITY_ASSERTIONS
                        if (!_Events.IsEmpty)
                            Debug.LogError(_Events + " is not in its default state even though it was in the list of spares.");
#endif
                    }

                    return _Events;
                }
                set
                {
                    if (_GotEventsFromPool)
                    {
                        _GotEventsFromPool = false;
                        ObjectPool.Release(_Events);
                    }

                    _Events = value;
                    _NextEventIndex = RecalculateEventIndex;
                }
            }

            /************************************************************************************************************************/

            void IUpdatable.EarlyUpdate()
            {
                if (_Events == null || _Events.IsEmpty)
                {
                    Release();
                    return;
                }

                _PreviousTime = _State.NormalizedTime;
            }

            /************************************************************************************************************************/

            void IUpdatable.LateUpdate()
            {
                if (_Events == null || _Events.IsEmpty)
                {
                    Release();
                    return;
                }

                var currentTime = _State.NormalizedTime;
                if (_PreviousTime == currentTime)
                    return;

                // General events are triggered on the frame when their time passes.
                // This happens either once or repeatedly depending on whether the animation is looping or not.
                CheckGeneralEvents(currentTime);
                if (_Events == null)
                {
                    Release();
                    return;
                }

                // End events are triggered every frame after their time passes. This ensures that assigning the event
                // after the time has passed will still trigger it rather than leaving it playing indefinitely.
                var onEnd = _Events.endEvent;
                if (onEnd.callback != null)
                {

                    if (currentTime > _PreviousTime)// Playing Forwards.
                    {
                        var eventTime = float.IsNaN(onEnd.normalizedTime) ?
                            1 : onEnd.normalizedTime;

                        if (currentTime > eventTime)
                            onEnd.Invoke(_State);
                    }
                    else// Playing Backwards.
                    {
                        var eventTime = float.IsNaN(onEnd.normalizedTime) ?
                            0 : onEnd.normalizedTime;

                        if (currentTime < eventTime)
                            onEnd.Invoke(_State);
                    }
                }
            }

            /************************************************************************************************************************/

            public void OnTimeChanged()
            {
                _NextEventIndex = RecalculateEventIndex;
            }

            /************************************************************************************************************************/

            private void CheckGeneralEvents(float currentTime)
            {
                var count = _Events.Count;
                if (count == 0)
                    return;

                float playDirectionFloat;
                int playDirectionInt;
                ValidateNextEventIndex(ref currentTime, out playDirectionFloat, out playDirectionInt);

                if (_IsLooping)// Looping.
                {
                    var animancerEvent = _Events[_NextEventIndex];
                    var eventTime = animancerEvent.normalizedTime * playDirectionFloat;

                    var loopDelta = GetLoopDelta(_PreviousTime, currentTime, eventTime);
                    if (loopDelta == 0)
                        return;

                    // For each additional loop, invoke all events without needing to check their times.
                    if (!InvokeAllEvents(loopDelta - 1, playDirectionInt))
                        return;

                    var loopStartIndex = _NextEventIndex;

                    Invoke:
                    animancerEvent.Invoke(_State);

                    if (!NextEventLooped(playDirectionInt) ||
                        _NextEventIndex == loopStartIndex)
                        return;

                    animancerEvent = _Events[_NextEventIndex];
                    eventTime = animancerEvent.normalizedTime * playDirectionFloat;
                    if (loopDelta == GetLoopDelta(_PreviousTime, currentTime, eventTime))
                        goto Invoke;
                }
                else// Non-Looping.
                {
                    while ((uint)_NextEventIndex < (uint)count)
                    {
                        var animancerEvent = _Events[_NextEventIndex];
                        var eventTime = animancerEvent.normalizedTime * playDirectionFloat;

                        if (currentTime <= eventTime)
                            break;

                        animancerEvent.Invoke(_State);

                        if (!NextEvent(playDirectionInt))
                            return;
                    }
                }
            }

            /************************************************************************************************************************/

            private void ValidateNextEventIndex(ref float currentTime,
                out float playDirectionFloat, out int playDirectionInt)
            {
                if (currentTime > _PreviousTime)// Playing Forwards.
                {
                    playDirectionFloat = 1;
                    playDirectionInt = 1;

                    if (_NextEventIndex == RecalculateEventIndex ||
                        _SequenceVersion != _Events.Version ||
                        !_WasPlayingForwards)
                    {
                        _NextEventIndex = 0;
                        _SequenceVersion = _Events.Version;
                        _WasPlayingForwards = true;

                        var previousTime = _PreviousTime;
                        if (_IsLooping)
                            previousTime = previousTime.Wrap01();

                        var max = _Events.Count - 1;
                        while (_NextEventIndex < max &&
                            _Events[_NextEventIndex].normalizedTime < previousTime)
                            _NextEventIndex++;

                        _Events.AssertNormalizedTimes(_IsLooping);
                    }
                }
                else// Playing Backwards.
                {
                    var previousTime = _PreviousTime;
                    _PreviousTime = -previousTime;
                    currentTime = -currentTime;
                    playDirectionFloat = -1;
                    playDirectionInt = -1;

                    if (_NextEventIndex == RecalculateEventIndex ||
                        _SequenceVersion != _Events.Version ||
                        _WasPlayingForwards)
                    {
                        _NextEventIndex = _Events.Count - 1;
                        _SequenceVersion = _Events.Version;
                        _WasPlayingForwards = false;

                        if (_IsLooping)
                            previousTime = previousTime.Wrap01();

                        while (_NextEventIndex > 0 &&
                            _Events[_NextEventIndex].normalizedTime > previousTime)
                            _NextEventIndex--;

                        _Events.AssertNormalizedTimes(_IsLooping);
                    }
                }

                // This method could be slightly optimised for playback direction changes by using the current index
                // as the starting point instead of iterating from the edge of the sequence, but that would make it
                // significantly more complex for something that should not happen very often and would only matter if
                // there are lots of events (in which case the optimisation would be tiny compared to the cost of
                // actually invoking all those events and running the rest of the application).
            }

            /************************************************************************************************************************/

            private static int GetLoopDelta(float previousTime, float nextTime, float eventTime)
            {
                previousTime -= eventTime;
                var previousLoopCount = Mathf.FloorToInt(previousTime);
                var nextLoopCount = Mathf.FloorToInt(nextTime - eventTime);

                if (previousTime == previousLoopCount)
                    nextLoopCount++;

                return nextLoopCount - previousLoopCount;
            }

            /************************************************************************************************************************/

            private bool InvokeAllEvents(int count, int playDirectionInt)
            {
                var loopStartIndex = _NextEventIndex;
                while (count-- > 0)
                {
                    do
                    {
                        _Events[_NextEventIndex].Invoke(_State);

                        if (!NextEventLooped(playDirectionInt))
                            return false;
                    }
                    while (_NextEventIndex != loopStartIndex);
                }

                return true;
            }

            /************************************************************************************************************************/

            private bool NextEvent(int playDirectionInt)
            {
                if (_NextEventIndex == RecalculateEventIndex)
                    return false;

                if (_Events.Version != _SequenceVersion)
                    throw new InvalidOperationException(SequenceVersionException);

                _NextEventIndex += playDirectionInt;

                return true;
            }

            /************************************************************************************************************************/

            private bool NextEventLooped(int playDirectionInt)
            {
                if (!NextEvent(playDirectionInt))
                    return false;

                var count = _Events.Count;
                if (_NextEventIndex >= count)
                    _NextEventIndex = 0;
                else if (_NextEventIndex < 0)
                    _NextEventIndex = count - 1;

                return true;
            }

            /************************************************************************************************************************/

            void IUpdatable.OnDestroy()
            {
                Release();
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

