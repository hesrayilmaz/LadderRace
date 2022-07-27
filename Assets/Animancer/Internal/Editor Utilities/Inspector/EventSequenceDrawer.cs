// Animancer // Copyright 2020 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using EventSequence = Animancer.AnimancerEvent.Sequence.Serializable;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws the Inspector GUI for <see cref="EventSequence"/>.</summary>
    [CustomPropertyDrawer(typeof(EventSequence), true)]
    public sealed class EventSequenceDrawer : PropertyDrawer
    {
        /************************************************************************************************************************/

        /// <summary>Details of an <see cref="EventSequence"/>.</summary>
        public sealed class Context : IDisposable
        {
            /************************************************************************************************************************/

            /// <summary>The main property representing the <see cref="EventSequence"/> field.</summary>
            public SerializedProperty Property { get; private set; }

            /************************************************************************************************************************/

            /// <summary>The property representing the <see cref="EventSequence._NormalizedTimes"/> field.</summary>
            public SerializedProperty Times { get; private set; }

            private int _TimeCount;

            /// <summary>The cached <see cref="SerializedProperty.arraySize"/> of <see cref="Times"/>.</summary>
            public int TimeCount
            {
                get { return _TimeCount; }
                set { Times.arraySize = _TimeCount = value; }
            }

            /// <summary>Shorthand for <see cref="SerializedProperty.GetArrayElementAtIndex"/> on <see cref="Times"/>.</summary>
            public SerializedProperty GetTime(int index)
            {
                return Times.GetArrayElementAtIndex(index);
            }

            /************************************************************************************************************************/

            /// <summary>The property representing the <see cref="EventSequence._Callbacks"/> field.</summary>
            public SerializedProperty Callbacks { get; private set; }

            private int _CallbackCount;

            /// <summary>The cached <see cref="SerializedProperty.arraySize"/> of <see cref="Callbacks"/>.</summary>
            public int CallbackCount
            {
                get { return _CallbackCount; }
                set { Callbacks.arraySize = _CallbackCount = value; }
            }

            /// <summary>Shorthand for <see cref="SerializedProperty.GetArrayElementAtIndex"/> on <see cref="Callbacks"/>.</summary>
            public SerializedProperty GetCallback(int index)
            {
                return Callbacks.GetArrayElementAtIndex(index);
            }

            /************************************************************************************************************************/

            private int _SelectedEvent;

            /// <summary>The index of the currently selected event.</summary>
            public int SelectedEvent
            {
                get { return _SelectedEvent; }
                set
                {
                    if (Times != null && value >= 0 && (value < TimeCount || TimeCount == 0))
                    {
                        float normalizedTime;
                        if (TimeCount > 0)
                        {
                            normalizedTime = GetTime(value).floatValue;
                        }
                        else
                        {
                            var transition = TransitionContext.Transition;
                            var speed = transition != null ? transition.Speed : 1;
                            normalizedTime = AnimancerEvent.Sequence.GetDefaultNormalizedEndTime(speed);
                        }

                        TransitionPreviewWindow.SetPreviewNormalizedTime(normalizedTime);
                    }

                    if (_SelectedEvent == value &&
                        Callbacks != null)
                        return;

                    _SelectedEvent = value;
                    TemporarySettings.Instance.SetSelectedEvent(Callbacks, value);
                }
            }

            /************************************************************************************************************************/

            /// <summary>The singleton instance.</summary>
            public static readonly Context Instance = new Context();

            private Context() { }

            /// <summary>
            /// Returns a <see cref="Context"/> representing the `property`.
            /// <para></para>
            /// Note that the same instance is returned every time.
            /// </summary>
            public static Context Get(SerializedProperty property)
            {
                Instance.Initialise(property);
                return Instance;
            }

            private void Initialise(SerializedProperty property)
            {
                if (Property != property)
                {
                    Property = property;

                    Times = property.FindPropertyRelative(EventSequence.NormalizedTimesField);
                    Callbacks = property.FindPropertyRelative(EventSequence.Callbacks);
                    _TimeCount = Times.arraySize;
                    _CallbackCount = Callbacks.arraySize;

                    if (_CallbackCount > _TimeCount)
                        _CallbackCount = Callbacks.arraySize = _TimeCount;

                    _SelectedEvent = TemporarySettings.Instance.GetSelectedEvent(Callbacks);
                    if (_SelectedEvent > _TimeCount - 1)
                        _SelectedEvent = Mathf.Max(0, _TimeCount - 1);
                }

                EditorGUI.BeginChangeCheck();
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="IDisposable"/>]
            /// Reduces the <see cref="Callbacks"/> array size to remove any empty elements.
            /// </summary>
            public void Dispose()
            {
                if (_TimeCount == 1 && _CallbackCount == 0 && float.IsNaN(GetTime(0).floatValue))
                {
                    Times.arraySize = 0;
                }
                else
                {
                    var callbackCount = _CallbackCount;
                    if (callbackCount > _TimeCount)
                        callbackCount = _TimeCount;

                    while (callbackCount > 0)
                    {
                        var callbackProperty = GetCallback(callbackCount - 1);
                        var callback = Serialization.GetValue(callbackProperty);
                        if (callback != null && EventSequence.HasPersistentCalls(callback))
                            break;
                        else
                            callbackCount--;
                    }

                    if (callbackCount != _CallbackCount)
                        Callbacks.arraySize = callbackCount;
                }

                if (EditorGUI.EndChangeCheck())
                    Property.serializedObject.ApplyModifiedProperties();

                Property = null;
            }

            /************************************************************************************************************************/

            /// <summary>Shorthand for <see cref="TransitionDrawer.Context"/>.</summary>
            public TransitionDrawer.TransitionContext TransitionContext
            {
                get { return TransitionDrawer.Context; }
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calculates the number of vertical pixels the `property` will occupy when it is drawn.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            using (var context = Context.Get(property))
            {
                var height = AnimancerGUI.LineHeight;

                var fullLineHeight = AnimancerGUI.StandardSpacing + AnimancerGUI.LineHeight;

                if (property.isExpanded)// If expanded, draw all events.
                {
                    var fullDummyHeight = DummySerializableCallback.Height + AnimancerGUI.StandardSpacing;

                    if (context.TimeCount > 0)
                    {
                        height += context.TimeCount * fullLineHeight;

                        for (int i = 0; i < context.CallbackCount; i++)
                        {
                            var callback = context.GetCallback(i);
                            height += EditorGUI.GetPropertyHeight(callback, null, false) + AnimancerGUI.StandardSpacing;
                        }

                        height += (context.TimeCount - context.CallbackCount) * fullDummyHeight;
                    }
                    else
                    {
                        height += fullLineHeight + fullDummyHeight;
                    }
                }
                else// If not expanded, only draw the selected event.
                {
                    if (context.SelectedEvent >= 0)
                    {
                        // Time.
                        height += fullLineHeight;

                        // Callback.
                        if (context.SelectedEvent < context.CallbackCount)
                        {
                            var callback = context.GetCallback(context.SelectedEvent);
                            height += EditorGUI.GetPropertyHeight(callback, null, false) + AnimancerGUI.StandardSpacing;
                        }
                        else
                        {
                            height += DummySerializableCallback.Height + AnimancerGUI.StandardSpacing;
                        }
                    }
                }

                return height;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI for the `property`.</summary>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            using (var context = Context.Get(property))
            {
                DoHeaderGUI(ref area, label, context);

                EditorGUI.indentLevel++;
                if (property.isExpanded)
                {
                    DoAllEventsGUI(ref area, context);
                }
                else if (context.SelectedEvent >= 0)
                {
                    DoEventGUI(ref area, context, context.SelectedEvent, true, true);
                }
                EditorGUI.indentLevel--;
            }
        }

        /************************************************************************************************************************/

        private static readonly TimeRuler
            TimeRuler = new TimeRuler();

        private void DoHeaderGUI(ref Rect area, GUIContent label, Context context)
        {
            area.height = AnimancerGUI.LineHeight;
            var headerArea = area;
            AnimancerGUI.NextVerticalArea(ref area);

            label = EditorGUI.BeginProperty(headerArea, label, context.Property);

            var addEventArea = AnimancerGUI.StealFromRight(ref headerArea, headerArea.height, AnimancerGUI.StandardSpacing);
            DoAddEventButtonGUI(addEventArea, context);

            if (context.TransitionContext != null && context.TransitionContext.Transition != null)
            {
                EditorGUI.EndProperty();

                float addEventNormalizedTime;
                TimeRuler.DoGUI(headerArea, context, out addEventNormalizedTime);

                if (!float.IsNaN(addEventNormalizedTime))
                {
                    AddEvent(context, addEventNormalizedTime);
                }
            }
            else
            {
                label.text = AnimancerGUI.GetNarrowText(label.text);

                var summary = AnimancerGUI.TempContent();
                if (context.TimeCount == 0)
                {
                    summary.text = "[0] End Time 1";
                }
                else
                {
                    var index = context.TimeCount - 1; ;
                    var endTime = context.GetTime(index).floatValue;
                    summary.text = string.Concat("[", index.ToString(), "] End Time ", endTime.ToString("G3"));
                }

                EditorGUI.LabelField(headerArea, label, summary);

                EditorGUI.EndProperty();
            }

            EditorGUI.BeginChangeCheck();
            context.Property.isExpanded =
                EditorGUI.Foldout(headerArea, context.Property.isExpanded, GUIContent.none, true);
            if (EditorGUI.EndChangeCheck())
                context.SelectedEvent = -1;
        }

        /************************************************************************************************************************/

        private static readonly GUIContent
            AddEventContent = EditorGUIUtility.IconContent("Animation.AddEvent", Strings.ProOnlyTag + "Add event");

        /// <summary>Draws a button to add a new event.</summary>
        public void DoAddEventButtonGUI(Rect area, Context context)
        {
            if (!GUI.Button(area, AddEventContent, Styles.AddEventStyle))
                return;

            // If the target is currently being previewed, add the event at the currently selected time.
            var state = TransitionPreviewWindow.GetCurrentState();
            var normalizedTime = state != null ? state.NormalizedTime : float.NaN;
            AddEvent(context, normalizedTime);
        }

        /************************************************************************************************************************/

        private void AddEvent(Context context, float normalizedTime)
        {
            // Otherwise add it halfway between the last event and the end.
            if (context.TimeCount == 0)
            {
                // Having any events means we need the end time too.
                context.TimeCount = 2;
                context.GetTime(1).floatValue = float.NaN;
                if (float.IsNaN(normalizedTime))
                    normalizedTime = 0.5f;
            }
            else
            {
                context.Times.InsertArrayElementAtIndex(context.TimeCount - 1);
                context.TimeCount++;

                if (float.IsNaN(normalizedTime))
                {
                    var transition = context.TransitionContext.Transition;

                    var previousTime = context.TimeCount >= 3 ?
                        context.GetTime(context.TimeCount - 3).floatValue :
                        AnimancerEvent.Sequence.GetDefaultNormalizedStartTime(transition.Speed);

                    var endTime = context.GetTime(context.TimeCount - 1).floatValue;
                    if (float.IsNaN(endTime))
                        endTime = AnimancerEvent.Sequence.GetDefaultNormalizedEndTime(transition.Speed);

                    normalizedTime = previousTime < endTime ?
                        (previousTime + endTime) * 0.5f :
                        previousTime;
                }
            }

            WrapEventTime(context, ref normalizedTime);

            var newEvent = context.TimeCount - 2;
            context.GetTime(newEvent).floatValue = normalizedTime;
            context.SelectedEvent = context.TimeCount - 2;

            if (context.CallbackCount > newEvent)
            {
                context.Callbacks.InsertArrayElementAtIndex(newEvent);

                // Make sure the callback starts empty rather than copying an existing value.
                var callback = context.GetCallback(newEvent);
                callback.SetValue(null);
                context.Callbacks.OnPropertyChanged();
            }

            GUI.changed = true;
            GUIUtility.ExitGUI();
        }

        /************************************************************************************************************************/

        private static void WrapEventTime(Context context, ref float normalizedTime)
        {
            var transition = context.TransitionContext.Transition;
            if (transition != null && transition.IsLooping)
            {
                if (normalizedTime == 0)
                    return;
                else if (normalizedTime % 1 == 0)
                    normalizedTime = AnimancerEvent.AlmostOne;
                else
                    normalizedTime = normalizedTime.Wrap01();
            }
        }

        /************************************************************************************************************************/

        private static readonly int EventTimeHash = "EventTime".GetHashCode();

        private static int _HotControlAdjustRoot;
        private static int _SelectedEventToHotControl;

        private static void DoAllEventsGUI(ref Rect area, Context context)
        {
            var currentGUIEvent = Event.current;
            if (currentGUIEvent.type == EventType.Used)
                return;

            var rootControlID = GUIUtility.GetControlID(EventTimeHash - 1, FocusType.Passive);

            var eventCount = Mathf.Max(1, context.TimeCount);
            for (int i = 0; i < eventCount; i++)
            {
                var controlID = GUIUtility.GetControlID(EventTimeHash + i, FocusType.Passive);

                if (rootControlID == _HotControlAdjustRoot &&
                    _SelectedEventToHotControl > 0 &&
                    i == context.SelectedEvent)
                {
                    GUIUtility.hotControl = GUIUtility.keyboardControl = controlID + _SelectedEventToHotControl;
                    _SelectedEventToHotControl = 0;
                    _HotControlAdjustRoot = -1;
                }

                DoEventGUI(ref area, context, i, false, true);

                if (currentGUIEvent.type == EventType.Used)
                {
                    context.SelectedEvent = i;

                    if (SortEvents(context))
                    {
                        _SelectedEventToHotControl = GUIUtility.keyboardControl - controlID;
                        _HotControlAdjustRoot = rootControlID;
                        AnimancerGUI.Deselect();
                    }

                    GUIUtility.ExitGUI();
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI fields for the event at the specified `index`.</summary>
        public static void DoEventGUI(ref Rect area, Context context, int index, bool autoSort, bool showCallback)
        {
            string callbackLabel;
            DoEventTimeGUI(ref area, context, index, autoSort, out callbackLabel);

            if (showCallback)
            {
                var label = AnimancerGUI.TempContent(callbackLabel);

                if (index < context.CallbackCount)
                {
                    var callback = context.GetCallback(index);
                    area.height = EditorGUI.GetPropertyHeight(callback, false);
                    EditorGUI.BeginProperty(area, GUIContent.none, callback);

                    // UnityEvents ignore the proper indentation which makes them look terrible in a list.
                    // So we force the area to be indented.
                    var indentedArea = EditorGUI.IndentedRect(area);
                    var indentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    EditorGUI.PropertyField(indentedArea, callback, label, false);

                    EditorGUI.indentLevel = indentLevel;
                    EditorGUI.EndProperty();
                }
                else
                {
                    var length = context.TransitionContext.MaximumDuration;
                    object callback;
                    if (DummySerializableCallback.DoCallbackGUI(ref area, label, context.Callbacks, out callback))
                    {
                        context.Callbacks.RecordUndo();

                        context.Callbacks.ForEachTarget((callbacksProperty) =>
                        {
                            var accessor = callbacksProperty.GetAccessor();
                            var oldCallbacks = (Array)accessor.GetValue(callbacksProperty.serializedObject.targetObject);

                            Array newCallbacks;
                            if (oldCallbacks == null)
                            {
                                var elementType = accessor.FieldType.GetElementType();
                                newCallbacks = Array.CreateInstance(elementType, 1);
                            }
                            else
                            {
                                var elementType = oldCallbacks.GetType().GetElementType();
                                newCallbacks = Array.CreateInstance(elementType, index + 1);
                                Array.Copy(oldCallbacks, newCallbacks, oldCallbacks.Length);
                            }

                            newCallbacks.SetValue(callback, index);
                            accessor.SetValue(callbacksProperty, newCallbacks);
                        });

                        context.Callbacks.OnPropertyChanged();
                        context.CallbackCount = index + 1;

                        if (index >= context.TimeCount)
                        {
                            context.Times.InsertArrayElementAtIndex(index);
                            context.TimeCount++;
                            context.GetTime(index).floatValue = float.NaN;
                        }
                    }
                }

                AnimancerGUI.NextVerticalArea(ref area);
            }
        }

        /************************************************************************************************************************/

        private static float _PreviousTime = float.NaN;

        /// <summary>Draws the time field for the event at the specified `index`.</summary>
        public static void DoEventTimeGUI(ref Rect area, Context context, int index, bool autoSort, out string callbackLabel)
        {
            EditorGUI.BeginChangeCheck();

            area.height = AnimancerGUI.LineHeight;
            var timeArea = area;
            AnimancerGUI.NextVerticalArea(ref area);

            GUIContent timeLabel;
            float defaultTime;
            bool isEndEvent;
            GetEventLabels(index, context, out timeLabel, out callbackLabel, out defaultTime, out isEndEvent);
            var length = context.TransitionContext.MaximumDuration;

            float normalizedTime;

            if (index < context.TimeCount)
            {
                var timeProperty = context.GetTime(index);

                var wasEditingTextField = EditorGUIUtility.editingTextField;
                if (!wasEditingTextField)
                    _PreviousTime = float.NaN;

                EditorGUI.BeginChangeCheck();

                timeLabel = EditorGUI.BeginProperty(area, timeLabel, timeProperty);
                normalizedTime = AnimancerGUI.DoOptionalTimeField(
                    ref timeArea, timeLabel, timeProperty.floatValue, true, length, defaultTime);
                EditorGUI.EndProperty();

                var isEditingTextField = EditorGUIUtility.editingTextField;
                if (EditorGUI.EndChangeCheck() || (wasEditingTextField && !isEditingTextField))
                {
                    if (isEndEvent)
                    {
                        timeProperty.floatValue = normalizedTime;
                    }
                    else if (float.IsNaN(normalizedTime))
                    {
                        RemoveEvent(context, index);
                        AnimancerGUI.Deselect();
                    }
                    else if (!autoSort && isEditingTextField)
                    {
                        _PreviousTime = normalizedTime;
                    }
                    else
                    {
                        if (!float.IsNaN(_PreviousTime))
                        {
                            if (Event.current.keyCode != KeyCode.Escape)
                            {
                                normalizedTime = _PreviousTime;
                                AnimancerGUI.Deselect();
                            }

                            _PreviousTime = float.NaN;
                        }

                        WrapEventTime(context, ref normalizedTime);

                        timeProperty.floatValue = normalizedTime;

                        if (autoSort)
                            SortEvents(context);
                    }

                    GUI.changed = true;
                }
            }
            else// Dummy End Event.
            {
                Debug.Assert(index == 0, "This is assumed to be a dummy end event, which should only be at index 0");
                EditorGUI.BeginChangeCheck();

                EditorGUI.BeginProperty(timeArea, GUIContent.none, context.Times);
                normalizedTime = AnimancerGUI.DoOptionalTimeField(
                    ref timeArea, timeLabel, float.NaN, true, length, defaultTime, true);
                EditorGUI.EndProperty();

                if (EditorGUI.EndChangeCheck() && !float.IsNaN(normalizedTime))
                {
                    context.TimeCount = 1;
                    var timeProperty = context.GetTime(0);
                    timeProperty.floatValue = normalizedTime;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                TransitionPreviewWindow.SetPreviewNormalizedTime(normalizedTime);

                if (Event.current.type != EventType.Layout)
                    GUIUtility.ExitGUI();
            }
        }

        /************************************************************************************************************************/

        private static ConversionCache<int, string> _TimeLabelCache, _CallbackLabelCache;

        private static void GetEventLabels(int index, Context context, out GUIContent timeLabel, out string callbackLabel,
            out float defaultTime, out bool isEndEvent)
        {
            if (index >= context.TimeCount - 1)
            {
                timeLabel = AnimancerGUI.TempContent("End Time",
                    Strings.ProOnlyTag + "The time when the end callback will be triggered");

                callbackLabel = "End Callback";

                defaultTime = AnimancerEvent.Sequence.GetDefaultNormalizedEndTime(
                    context.TransitionContext.Transition.Speed);
                isEndEvent = true;
            }
            else
            {
                if (_CallbackLabelCache == null)
                {
                    _CallbackLabelCache = new ConversionCache<int, string>((i) => "Event " + i + " Callback");
                    _TimeLabelCache = new ConversionCache<int, string>((i) => "Event " + i + " Time");
                }

                timeLabel = AnimancerGUI.TempContent(_TimeLabelCache.Convert(index),
                    Strings.ProOnlyTag + "The time when the callback will be triggered");

                callbackLabel = _CallbackLabelCache.Convert(index);

                defaultTime = 0;
                isEndEvent = false;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Removes the event at the specified `index`.</summary>
        public static void RemoveEvent(Context context, int index)
        {
            // Only remove the time if it is not an End Event.
            if (index < context.TimeCount - 1)
            {
                context.Times.DeleteArrayElementAtIndex(index);
                context.TimeCount--;
            }
            else// If it was an End Event, prevent the selection from moving on to later GUI elements.
            {
                AnimancerGUI.Deselect();
            }

            if (index < context.CallbackCount)
            {
                context.Callbacks.DeleteArrayElementAtIndex(index);
                context.CallbackCount--;
            }
        }

        /************************************************************************************************************************/

        private static bool SortEvents(Context context)
        {
            if (context.TimeCount <= 2)
                return false;

            // The serializable sequence sorts itself in ISerializationCallbackReceiver.OnBeforeSerialize.
            var selectedEvent = context.SelectedEvent;
            var sorted = context.Property.serializedObject.ApplyModifiedProperties();
            if (!sorted)
                return false;

            context.Property.serializedObject.Update();
            context.CallbackCount = context.Callbacks.arraySize;
            return context.SelectedEvent != selectedEvent;
        }

        /************************************************************************************************************************/

        private static class Styles
        {
            public static readonly GUIStyle AddEventStyle = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(-1, 1, 0, 0),
                fixedHeight = 0,
            };
        }

        /************************************************************************************************************************/
    }
}

#endif

