// Animancer // Copyright 2020 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws the Inspector GUI for an <see cref="AnimancerNode"/>.</summary>
    public interface IAnimancerNodeDrawer
    {
        /// <summary>Draws the details and controls for the target node in the Inspector.</summary>
        void DoGUI(IAnimancerComponent owner);
    }

    /************************************************************************************************************************/

    /// <summary>[Editor-Only] Draws the Inspector GUI for an <see cref="AnimancerNode"/>.</summary>
    public abstract class AnimancerNodeDrawer<T> : IAnimancerNodeDrawer where T : AnimancerNode
    {
        /************************************************************************************************************************/

        /// <summary>The node being managed.</summary>
        public T Target { get; protected set; }

        /// <summary>If true, the details of the <see cref="Target"/> will be expanded in the Inspector.</summary>
        public bool IsExpanded
        {
            get { return Target._IsInspectorExpanded; }
            protected set { Target._IsInspectorExpanded = value; }
        }

        /************************************************************************************************************************/

        /// <summary>The <see cref="GUIStyle"/> used for the area encompassing this drawer.</summary>
        protected abstract GUIStyle RegionStyle { get; }

        /************************************************************************************************************************/

        /// <summary>Draws the details and controls for the target <see cref="Target"/> in the Inspector.</summary>
        public virtual void DoGUI(IAnimancerComponent owner)
        {
            if (!Target.IsValid)
                return;

            AnimancerGUI.BeginVerticalBox(RegionStyle);
            {
                DoHeaderGUI();
                DoDetailsGUI(owner);
            }
            AnimancerGUI.EndVerticalBox(RegionStyle);

            CheckContextMenu(GUILayoutUtility.GetLastRect());
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the name and other details of the <see cref="Target"/> in the GUI.
        /// </summary>
        protected virtual void DoHeaderGUI()
        {
            var area = AnimancerGUI.LayoutSingleLineRect(AnimancerGUI.SpacingMode.Before);
            DoLabelGUI(area);
            DoFoldoutGUI(area);
        }

        /// <summary>
        /// Draws a field for the <see cref="AnimancerState.MainObject"/> if it has one, otherwise just a simple text
        /// label.
        /// </summary>
        protected abstract void DoLabelGUI(Rect area);

        /// <summary>Draws a foldout arrow to expand/collapse the node details.</summary>
        protected abstract void DoFoldoutGUI(Rect area);

        /// <summary>Draws the details of the <see cref="Target"/> in the GUI.</summary>
        protected abstract void DoDetailsGUI(IAnimancerComponent owner);

        /************************************************************************************************************************/

        /// <summary>
        /// Draws controls for <see cref="AnimancerState.IsPlaying"/>, <see cref="AnimancerNode.Speed"/>, and
        /// <see cref="AnimancerNode.Weight"/>.
        /// </summary>
        protected void DoNodeDetailsGUI()
        {
            var labelWidth = EditorGUIUtility.labelWidth;

            var area = AnimancerGUI.LayoutSingleLineRect(AnimancerGUI.SpacingMode.Before);
            area.xMin += EditorGUI.indentLevel * AnimancerGUI.IndentSize;

            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var right = area.xMax;

            // Is Playing.
            var state = Target as AnimancerState;
            if (state != null)
            {
                var label = AnimancerGUI.BeginTightLabel("Is Playing");
                area.width = EditorGUIUtility.labelWidth + 16;
                state.IsPlaying = EditorGUI.Toggle(area, label, state.IsPlaying);
                AnimancerGUI.EndTightLabel();

                area.x += area.width;
                area.xMax = right;
            }

            float speedWidth, weightWidth;
            Rect speedRect, weightRect;
            AnimancerGUI.SplitHorizontally(area, "Speed", "Weight", out speedWidth, out weightWidth, out speedRect, out weightRect);

            // Speed.
            EditorGUIUtility.labelWidth = speedWidth;
            EditorGUI.BeginChangeCheck();
            var speed = EditorGUI.FloatField(speedRect, "Speed", Target.Speed);
            if (EditorGUI.EndChangeCheck())
                Target.Speed = speed;
            if (AnimancerGUI.TryUseClickEvent(speedRect, 2))
                Target.Speed = Target.Speed != 1 ? 1 : 0;

            // Weight.
            EditorGUIUtility.labelWidth = weightWidth;
            EditorGUI.BeginChangeCheck();
            var weight = EditorGUI.FloatField(weightRect, "Weight", Target.Weight);
            if (EditorGUI.EndChangeCheck())
                Target.Weight = weight;
            if (AnimancerGUI.TryUseClickEvent(weightRect, 2))
                Target.Weight = Target.Weight != 1 ? 1 : 0;

            EditorGUI.indentLevel = indentLevel;
            EditorGUIUtility.labelWidth = labelWidth;

            DoFadeDetailsGUI();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws controls for <see cref="AnimancerNode.FadeSpeed"/> and <see cref="AnimancerNode.TargetWeight"/>.
        /// </summary>
        private void DoFadeDetailsGUI()
        {
            var area = AnimancerGUI.LayoutSingleLineRect(AnimancerGUI.SpacingMode.Before);
            area = EditorGUI.IndentedRect(area);

            var speedLabel = AnimancerGUI.GetNarrowText("Fade Speed");
            var targetLabel = AnimancerGUI.GetNarrowText("Target Weight");

            float speedWidth, weightWidth;
            Rect speedRect, weightRect;
            AnimancerGUI.SplitHorizontally(area, speedLabel, targetLabel,
                out speedWidth, out weightWidth, out speedRect, out weightRect);

            var labelWidth = EditorGUIUtility.labelWidth;
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.BeginChangeCheck();

            // Fade Speed.
            EditorGUIUtility.labelWidth = speedWidth;
            Target.FadeSpeed = EditorGUI.DelayedFloatField(speedRect, speedLabel, Target.FadeSpeed);
            if (AnimancerGUI.TryUseClickEvent(speedRect, 2))
            {
                Target.FadeSpeed = Target.FadeSpeed != 0 ?
                    0 :
                    Math.Abs(Target.Weight - Target.TargetWeight) / AnimancerPlayable.DefaultFadeDuration;
            }

            // Target Weight.
            EditorGUIUtility.labelWidth = weightWidth;
            Target.TargetWeight = EditorGUI.FloatField(weightRect, targetLabel, Target.TargetWeight);
            if (AnimancerGUI.TryUseClickEvent(weightRect, 2))
            {
                if (Target.TargetWeight != Target.Weight)
                    Target.TargetWeight = Target.Weight;
                else if (Target.TargetWeight != 1)
                    Target.TargetWeight = 1;
                else
                    Target.TargetWeight = 0;
            }

            if (EditorGUI.EndChangeCheck() && Target.FadeSpeed != 0)
                Target.StartFade(Target.TargetWeight, 1 / Target.FadeSpeed);

            EditorGUI.indentLevel = indentLevel;
            EditorGUIUtility.labelWidth = labelWidth;
        }

        /************************************************************************************************************************/
        #region Context Menu
        /************************************************************************************************************************/

        /// <summary>
        /// The menu label prefix used for details about the <see cref="Target"/>.
        /// </summary>
        protected const string DetailsPrefix = "Details/";

        /// <summary>
        /// Checks if the current event is a context menu click within the `clickArea` and opens a context menu with various
        /// functions for the <see cref="Target"/>.
        /// </summary>
        protected void CheckContextMenu(Rect clickArea)
        {
            if (!AnimancerGUI.TryUseClickEvent(clickArea, 1))
                return;

            var menu = new GenericMenu();

            menu.AddDisabledItem(new GUIContent(Target.ToString()));

            PopulateContextMenu(menu);

            menu.AddItem(new GUIContent(DetailsPrefix + "Log Details"), false,
                () => Debug.Log(Target.GetDescription()));

            menu.AddItem(new GUIContent(DetailsPrefix + "Log Details Of Everything"), false,
                () => Debug.Log(Target.Root.GetDescription()));
            AddPlayableGraphVisualizerFunction(menu);

            menu.ShowAsContext();
        }

        /// <summary>Adds functions relevant to the <see cref="Target"/>.</summary>
        protected abstract void PopulateContextMenu(GenericMenu menu);

        /************************************************************************************************************************/

        private void AddPlayableGraphVisualizerFunction(GenericMenu menu)
        {
            var type = Type.GetType("GraphVisualizer.PlayableGraphVisualizerWindow," +
                " Unity.PlayableGraphVisualizer.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

            AnimancerEditorUtilities.AddMenuItem(menu, DetailsPrefix + "Playable Graph Visualizer", type != null, () =>
            {
                var window = EditorWindow.GetWindow(type);

                var field = type.GetField("m_CurrentGraph",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                if (field != null)
                    field.SetValue(window, Target.Root._Graph);
            });
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

