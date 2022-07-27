// Animancer // Copyright 2020 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws the Inspector GUI for an <see cref="IAnimancerComponent.Playable"/>.</summary>
    public sealed class AnimancerPlayableDrawer
    {
        /************************************************************************************************************************/

        /// <summary>Only get <see cref="AnimancerPlayable.IsGraphPlaying"/> during <see cref="EventType.Layout"/>.</summary>
        private bool _IsGraphPlaying;

        /// <summary>A lazy list of information about the layers currently being displayed.</summary>
        private readonly List<AnimancerLayerDrawer>
            LayerInfos = new List<AnimancerLayerDrawer>();

        /// <summary>The number of elements in <see cref="LayerInfos"/> that are currently being used.</summary>
        private int _LayerCount;

        /************************************************************************************************************************/

        /// <summary>Draws the GUI of the <see cref="IAnimancerComponent.Playable"/> if there is only one target.</summary>
        public void DoGUI(IAnimancerComponent[] targets)
        {
            if (targets.Length != 1)
                return;

            DoGUI(targets[0]);
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI of the <see cref="IAnimancerComponent.Playable"/>.</summary>
        public void DoGUI(IAnimancerComponent target)
        {
            if (!target.IsPlayableInitialised)
            {
                DoPlayableNotInitialisedGUI(target);
                return;
            }

            EditorGUI.BeginChangeCheck();

            // Gather the during the layout event and use the same ones during subsequent events to avoid GUI errors
            // in case they change (they shouldn't, but this is also more efficient).
            if (Event.current.type == EventType.Layout)
            {
                AnimancerLayerDrawer.GatherLayerEditors(target.Playable, LayerInfos, out _LayerCount);
                _IsGraphPlaying = target.Playable.IsGraphPlaying;
            }

            if (!_IsGraphPlaying)
            {
                AnimancerGUI.BeginVerticalBox(GUI.skin.box);
                _IsGraphPlaying = EditorGUILayout.Toggle("Is Graph Playing", _IsGraphPlaying);
                AnimancerGUI.EndVerticalBox(GUI.skin.box);

                if (_IsGraphPlaying)
                    target.Playable.UnpauseGraph();
            }

            for (int i = 0; i < _LayerCount; i++)
            {
                LayerInfos[i].DoGUI(target);
            }

            DoLayerWeightWarningGUI();

            if (AnimancerLayerDrawer.ShowUpdatingNodes)
                target.Playable.DoUpdateListGUI();

            if (EditorGUI.EndChangeCheck() && !_IsGraphPlaying)
                target.Playable.Evaluate();
        }

        /************************************************************************************************************************/

        private void DoPlayableNotInitialisedGUI(IAnimancerComponent target)
        {
            if (!EditorApplication.isPlaying ||
                target.Animator == null ||
                EditorUtility.IsPersistent(target.Animator))
                return;

            EditorGUILayout.HelpBox("Playable is not initialised." +
                " It will be initialised automatically when something needs it, such as playing an animation.",
                 MessageType.Info);

            if (AnimancerGUI.TryUseClickEventInLastRect(1))
            {
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("Initialise"), false, () => target.Playable.Evaluate());

                AnimancerEditorUtilities.AddDocumentationLink(menu, "Layer Documentation", "/docs/manual/blending/layers");

                menu.ShowAsContext();
            }
        }

        /************************************************************************************************************************/

        private void DoLayerWeightWarningGUI()
        {
            for (int i = 0; i < _LayerCount; i++)
            {
                var layer = LayerInfos[i].Target;
                if (layer.Weight == 1 &&
                    !layer.IsAdditive &&
                    layer._Mask == null &&
                    Mathf.Approximately(layer.GetTotalWeight(), 1))
                    return;
            }

            EditorGUILayout.HelpBox(
                "There are no Override layers at weight 1, which will likely give undesirable results." +
                " Click here for more information.",
                MessageType.Warning);

            if (AnimancerGUI.TryUseClickEventInLastRect())
                EditorUtility.OpenWithDefaultApp(Strings.DocsURLs.Fading);
        }

        /************************************************************************************************************************/
    }
}

#endif

