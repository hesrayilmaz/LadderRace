// Animancer // Copyright 2020 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] [Pro-Only]
    /// An <see cref="EditorWindow"/> which allows the user to preview animation transitions separately from the rest
    /// of the scene in Edit Mode or Play Mode.
    /// </summary>
    public sealed class TransitionPreviewWindow : EditorWindow, IHasCustomMenu
    {
        /************************************************************************************************************************/
        #region Public API
        /************************************************************************************************************************/

        private static Texture _Icon;

        /// <summary>The icon image used by this window.</summary>
        public static Texture Icon
        {
            get
            {
#if UNITY_2019_3_OR_NEWER
                const string IconName = "ViewToolOrbit";
#else
                const string IconName = "UnityEditor.LookDevView";
#endif
                // Possible icons: "UnityEditor.LookDevView", "SoftlockInline", "ViewToolOrbit".

                if (_Icon == null)
                    _Icon = EditorGUIUtility.IconContent(IconName).image;
                return _Icon;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Focusses the <see cref="TransitionPreviewWindow"/> or creates one if none exists.</summary>
        public static void Open(SerializedProperty transitionProperty, bool open)
        {
            if (open)
            {
                GetWindow<TransitionPreviewWindow>(typeof(SceneView))
                    .SetTargetProperty(transitionProperty);
            }
            else if (IsPreviewingCurrentProperty())
            {
                EditorApplication.delayCall += _Instance.Close;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sets the <see cref="AnimancerState.NormalizedTime"/> of the current transition if the property being
        /// previewed matches the <see cref="TransitionDrawer.Context"/>.
        /// </summary>
        public static void SetPreviewNormalizedTime(float normalizedTime)
        {
            if (float.IsNaN(normalizedTime) ||
                !IsPreviewingCurrentProperty() ||
                _Instance.InstanceAnimancer == null)
                return;

            var transition = _Instance.ShowTransitionPaused();
            if (transition == null)
                return;

            var animancer = _Instance.InstanceAnimancer;
            var state = animancer.States.Current;

            var length = state.Length;
            var time = normalizedTime * length;
            var fadeDuration = transition.FadeDuration;

            var startTime = transition.NormalizedStartTime * length;
            if (float.IsNaN(startTime))
                startTime = 0;

            if (time < startTime)// Previous animation.
            {
                if (_Instance._PreviousAnimation != null)
                {
                    var fromState = animancer.States.GetOrCreate(PreviousAnimationKey, _Instance._PreviousAnimation, true);
                    animancer.Play(fromState);
                    _Instance.OnPlayAnimation();
                    fromState.NormalizedTime = normalizedTime;
                    normalizedTime = 0;
                }
            }
            else if (time < startTime + fadeDuration)// Fade from previous animation to the target.
            {
                if (_Instance._PreviousAnimation != null)
                {
                    var fromState = animancer.States.GetOrCreate(PreviousAnimationKey, _Instance._PreviousAnimation, true);
                    animancer.Play(fromState);
                    _Instance.OnPlayAnimation();
                    fromState.NormalizedTime = normalizedTime;

                    state.IsPlaying = true;
                    state.Weight = (time - startTime) / fadeDuration;
                    fromState.Weight = 1 - state.Weight;
                }
            }
            else if (_Instance._NextAnimation != null)// Fade from the target transition to the next animation.
            {
                var normalizedEndTime = state.Events.NormalizedEndTime;
                if (float.IsNaN(normalizedEndTime))
                    normalizedEndTime = AnimancerEvent.Sequence.GetDefaultNormalizedEndTime(state.Speed);

                if (normalizedTime < normalizedEndTime)
                {
                    // Just the main state.
                }
                else
                {
                    var toState = animancer.States.GetOrCreate(NextAnimationKey, _Instance._NextAnimation, true);
                    animancer.Play(toState);
                    _Instance.OnPlayAnimation();
                    toState.NormalizedTime = normalizedTime - normalizedEndTime;

                    var endTime = normalizedEndTime * length;
                    var fadeOutEnd = TimeRuler.GetFadeOutEnd(toState.Speed, endTime, transition.MaximumDuration);
                    if (time < fadeOutEnd)
                    {
                        state.IsPlaying = true;
                        toState.Weight = (time - endTime) / (fadeOutEnd - endTime);
                        state.Weight = 1 - toState.Weight;
                    }
                }
            }

            state.NormalizedTime = state.Weight > 0 ? normalizedTime : 0;
            animancer.Evaluate();

            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <see cref="AnimancerState"/> of the current transition if the property being previewed matches
        /// the <see cref="TransitionDrawer.Context"/>. Otherwise returns null.
        /// </summary>
        public static AnimancerState GetCurrentState()
        {
            if (!IsPreviewingCurrentProperty() ||
                _Instance.InstanceAnimancer == null)
                return null;

            var transition = _Instance.GetTransition();
            return _Instance.InstanceAnimancer.States[transition];
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether the current <see cref="TransitionDrawer.TransitionContext.Property"/> is being previewed
        /// at the moment.
        /// </summary>
        public static bool IsPreviewingCurrentProperty()
        {
            return
                _Instance != null &&
                TransitionDrawer.Context != null &&
                _Instance._TransitionProperty.IsValid() &&
                Serialization.AreSameProperty(TransitionDrawer.Context.Property, _Instance._TransitionProperty);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Messages
        /************************************************************************************************************************/

        private static TransitionPreviewWindow _Instance;

        /************************************************************************************************************************/

        private void OnEnable()
        {
            _Instance = this;
            titleContent = new GUIContent("Animancer", Icon);
            autoRepaintOnSceneChange = true;
            _InspectorWidth = EditorPrefs.GetFloat(InspectorWidthKey, 300);

            if (_TransitionProperty.IsValid() &&
                !CanBePreviewed(_TransitionProperty))
            {
                DestroyTransitionProperty();
            }

            InitialisePreview();

            UnityEditor.SceneManagement.EditorSceneManager.sceneOpening += OnSceneOpening;
#if UNITY_2017_3_OR_NEWER
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

        /************************************************************************************************************************/

        [NonSerialized] private bool _IsChangingPlayMode;

#if UNITY_2017_3_OR_NEWER
        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    DestroyModelInstance();
                    _IsChangingPlayMode = true;
                    break;

                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    _IsChangingPlayMode = false;
                    break;
            }
        }
#endif

        /************************************************************************************************************************/

        private void OnSceneOpening(string path, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            if (mode == UnityEditor.SceneManagement.OpenSceneMode.Single)
                DestroyModelInstance();
        }

        /************************************************************************************************************************/

        private void OnDisable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpening -= OnSceneOpening;
#if UNITY_2017_3_OR_NEWER
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif

            EditorPrefs.SetFloat(InspectorWidthKey, _InspectorWidth);

            DestroyAnimancerInstance();

            if (_PreviewRenderUtility != null)
            {
                _PreviewRenderUtility.Cleanup();
                _PreviewRenderUtility = null;
            }

            _Instance = null;
        }

        /************************************************************************************************************************/

        private void OnDestroy()
        {
            if (_PreviewSceneRoot != null)
            {
                DestroyImmediate(_PreviewSceneRoot.gameObject);
                _PreviewSceneRoot = null;
            }

            DestroyTransitionProperty();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /************************************************************************************************************************/

        private void OnGUI()
        {
            // Maximising then un-maximising the window causes it to lose the _Instance for some reason.
            _Instance = this;

            GUILayout.BeginHorizontal();
            {
                DoPreviewGUI();
                DoInspectorGUI();
            }
            GUILayout.EndHorizontal();
        }

        /************************************************************************************************************************/

        private void Update()
        {
            _Instance = this;

            if (!_IsChangingPlayMode && _InstanceRoot == null)
                InstantiateModel();

            if (AutoClose && !_TransitionProperty.IsValid())
                Close();
            else if (_InstanceAnimancer != null && _InstanceAnimancer.IsGraphPlaying)
                Repaint();
        }

        /************************************************************************************************************************/

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            ShowTransition.AddToggleFunction(menu);
            AutoClose.AddToggleFunction(menu);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Transition Property
        /************************************************************************************************************************/

        [SerializeField] private Serialization.PropertyReference _TransitionProperty;

        /************************************************************************************************************************/

        /// <summary>Indicates whether the `property` is able to be previewed by this system.</summary>
        public static bool CanBePreviewed(SerializedProperty property)
        {
            var type = property.GetAccessor().FieldType;
            return typeof(ITransitionDetailed).IsAssignableFrom(type);
        }

        /************************************************************************************************************************/

        private void SetTargetProperty(SerializedProperty property)
        {
            if (property.serializedObject.targetObjects.Length != 1)
            {
                Close();
                throw new ArgumentException("The TransitionPreviewWindow does not support multi-object selection.");
            }

            if (!CanBePreviewed(property))
            {
                Close();
                throw new ArgumentException("The specified property does not implement IAnimancerTransitionDetailed.");
            }

            DestroyTransitionProperty();

            _TransitionProperty = property;
            _SelectedInstanceAnimator = 0;
            _OriginalRoot = AnimancerEditorUtilities.FindRoot(_TransitionProperty.TargetObject);
        }

        /************************************************************************************************************************/

        private ITransitionDetailed GetTransition()
        {
            if (!_TransitionProperty.IsValid())
                return null;

            return _TransitionProperty.Property.GetValue<ITransitionDetailed>();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Preview
        /************************************************************************************************************************/

        [NonSerialized] private PreviewRenderUtility _PreviewRenderUtility;

        [SerializeField] private Transform _PreviewSceneRoot;
        [SerializeField] private Transform _OriginalRoot;
        [SerializeField] private Transform _InstanceRoot;

        /************************************************************************************************************************/

        [SerializeField] private Animator[] _InstanceAnimators;
        [SerializeField] private int _SelectedInstanceAnimator;
        [NonSerialized] private AnimationType _SelectedInstanceType;

        private Animator SelectedInstanceAnimator
        {
            get
            {
                if (_InstanceAnimators == null ||
                    _InstanceAnimators.Length == 0)
                    return null;

                if (_SelectedInstanceAnimator > _InstanceAnimators.Length)
                    _SelectedInstanceAnimator = _InstanceAnimators.Length;

                return _InstanceAnimators[_SelectedInstanceAnimator];
            }
        }

        /************************************************************************************************************************/

        [NonSerialized]
        private AnimancerPlayable _InstanceAnimancer;
        private AnimancerPlayable InstanceAnimancer
        {
            get
            {
                if ((_InstanceAnimancer == null || !_InstanceAnimancer.IsValid) &&
                    _InstanceRoot != null)
                {
                    var animator = SelectedInstanceAnimator;
                    if (animator != null)
                    {
                        AnimancerPlayable.SetNextGraphName(_InstanceRoot.name);
                        _InstanceAnimancer = AnimancerPlayable.Create();
                        _InstanceAnimancer.SetOutput(
                            new AnimancerEditorUtilities.DummyAnimancerComponent(animator, _InstanceAnimancer));
                    }
                }

                return _InstanceAnimancer;
            }
        }

        /************************************************************************************************************************/

        private void InitialisePreview()
        {
            if (_PreviewRenderUtility == null)
                _PreviewRenderUtility = new PreviewRenderUtility();

            if (_PreviewSceneRoot == null)
            {
                _PreviewSceneRoot = EditorUtility.CreateGameObjectWithHideFlags(
                    "Animancer Transition Preview Window", HideFlags.HideAndDontSave).transform;
                _PreviewRenderUtility.AddSingleGO(_PreviewSceneRoot.gameObject);
                _PreviewRenderUtility.ambientColor = new Color(0.1f, 0.1f, 0.1f, 0f);
            }
        }

        /************************************************************************************************************************/

        private void InstantiateModel()
        {
            DestroyModelInstance();

            if (_OriginalRoot == null)
                return;

            _PreviewSceneRoot.gameObject.SetActive(false);
            _InstanceRoot = Instantiate(_OriginalRoot, _PreviewSceneRoot);
            _InstanceRoot.localPosition = Vector3.zero;
            _InstanceRoot.name = _OriginalRoot.name;

            DestroyUnnecessaryComponents(_InstanceRoot.gameObject);

            _InstanceAnimators = _InstanceRoot.GetComponentsInChildren<Animator>();
            for (int i = 0; i < _InstanceAnimators.Length; i++)
            {
                var animator = _InstanceAnimators[i];
                animator.enabled = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.fireEvents = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.gameObject.AddComponent<RedirectRootMotion>()
                    .animator = animator;
            }

            _PreviewSceneRoot.gameObject.SetActive(true);

            InitialiseCamera();
            SetSelectedAnimator(_SelectedInstanceAnimator);

            AnimationGatherer.GatherFromGameObject(_OriginalRoot.gameObject, ref _OtherAnimations, true);

            if (_OtherAnimations.Length > 0 &&
                (_PreviousAnimation == null || _NextAnimation == null))
            {
                var defaultClip = _OtherAnimations[0];
                var defaultClipIsIdle = false;

                for (int i = 0; i < _OtherAnimations.Length; i++)
                {
                    var clip = _OtherAnimations[i];

                    if (defaultClipIsIdle && clip.name.Length > defaultClip.name.Length)
                        continue;

                    if (clip.name.IndexOf("idle", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        defaultClip = clip;
                        //defaultClipIsIdle = true;
                        break;
                    }
                }

                if (_PreviousAnimation == null)
                    _PreviousAnimation = defaultClip;
                if (_NextAnimation == null)
                    _NextAnimation = defaultClip;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Destroys all unnecessary components on the preview instance while accounting for any
        /// <see cref="RequireComponent"/> attributes.
        /// </summary>
        private static void DestroyUnnecessaryComponents(GameObject root)
        {
            var components = root.GetComponentsInChildren<Component>();

            var typeToDependencies = new Dictionary<Type, List<Type>>();
            var componentToDependencies = new Dictionary<Component, List<Component>>(components.Length);
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];

                List<Component> dependencies;
                if (!componentToDependencies.TryGetValue(component, out dependencies))
                {
                    var type = component.GetType();
                    List<Type> typeDependencies;
                    if (!typeToDependencies.TryGetValue(type, out typeDependencies))
                    {
                        if (type.IsDefined(typeof(RequireComponent), false))
                        {
                            var requirements = (RequireComponent[])type.GetCustomAttributes(typeof(RequireComponent), false);
                            for (int j = 0; j < requirements.Length; j++)
                            {
                                var requirement = requirements[j];
                                GatherRequirement(ref typeDependencies, requirement.m_Type0);
                                GatherRequirement(ref typeDependencies, requirement.m_Type1);
                                GatherRequirement(ref typeDependencies, requirement.m_Type2);
                            }
                        }

                        typeToDependencies.Add(type, typeDependencies);
                    }

                    if (typeDependencies != null)
                    {
                        dependencies = new List<Component>();

                        for (int j = 0; j < typeDependencies.Count; j++)
                        {
                            var requiredComponent = component.GetComponent(typeDependencies[j]);
                            if (requiredComponent != null)
                                dependencies.Add(requiredComponent);
                        }
                    }

                    componentToDependencies.Add(component, dependencies);
                }
            }

            var sortedComponents = AnimancerEditorUtilities.TopologicalSort(components,
                (component) => componentToDependencies[component]);

            for (int i = components.Length - 1; i >= 0; i--)
            {
                var component = sortedComponents[i];
                if (component is Transform ||
                    component is MeshFilter ||
                    component is Renderer ||
                    component is Animator)
                    continue;

                DestroyImmediate(component);
            }
        }

        /************************************************************************************************************************/

        private static void GatherRequirement(ref List<Type> requirements, Type type)
        {
            if (type == null)
                return;

            if (requirements == null)
                requirements = new List<Type>();

            requirements.Add(type);
        }

        /************************************************************************************************************************/

        private void SetSelectedAnimator(int index)
        {
            DestroyAnimancerInstance();

            var animator = SelectedInstanceAnimator;
            if (animator != null && animator.enabled)
            {
                animator.Rebind();
                animator.enabled = false;
                return;
            }

            _SelectedInstanceAnimator = index;

            animator = SelectedInstanceAnimator;
            if (animator != null)
            {
                animator.enabled = true;
                _SelectedInstanceType = AnimancerEditorUtilities.GetAnimationType(animator);

                if (_SelectedInstanceType == AnimationType.Sprite)
                {
                    Camera.transform.parent.localRotation = Quaternion.identity;
                }
                else
                {
                    CameraEulerAngles = CameraEulerAngles;
                }
            }
        }

        /************************************************************************************************************************/

        [NonSerialized] private bool _IsDraggingCamera;
        [NonSerialized] private Texture _PreviewTexture;

        private void DoPreviewGUI()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();

            var area = GUILayoutUtility.GetLastRect();

            var inspectorBorder = new Rect(area.xMax, area.y, 0, area.height);
            DoResizeInspectorGUI(inspectorBorder);

            var currentEvent = Event.current;
            switch (currentEvent.type)
            {
                case EventType.Repaint:
                    if (area.width <= 0 || area.height <= 0)
                        break;

                    DrawFloor();

                    // Start the model paused at the beginning of the animation.
                    // For some reason Unity doesn't like having this in OnEnable.
                    if (InstanceAnimancer != null && InstanceAnimancer.Layers.Count == 0)
                        ShowTransitionPaused();

                    var fog = RenderSettings.fog;
                    Unsupported.SetRenderSettingsUseFogNoDirty(false);

                    if (InstanceAnimancer != null ||
                        _PreviewTexture == null ||
                        _OriginalRoot == null)
                    {
                        _PreviewRenderUtility.BeginPreview(area, GUIStyle.none);
                        _PreviewRenderUtility.Render();
                        _PreviewTexture = _PreviewRenderUtility.EndPreview();
                    }

                    GUI.DrawTexture(area, _PreviewTexture, ScaleMode.StretchToFill, false);

                    Unsupported.SetRenderSettingsUseFogNoDirty(fog);
                    break;

                // Camera Control.

                case EventType.MouseDown:
                    _IsDraggingCamera = area.Contains(currentEvent.mousePosition);
                    if (_IsDraggingCamera)
                        currentEvent.Use();
                    break;

                case EventType.MouseUp:
                    if (_IsDraggingCamera)
                    {
                        _IsDraggingCamera = false;
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_IsDraggingCamera)
                    {
                        var sensitivity = Screen.dpi * 0.01f;

                        var euler = CameraEulerAngles;
                        euler.x += currentEvent.delta.y * sensitivity;
                        euler.y += currentEvent.delta.x * sensitivity;
                        CameraEulerAngles = euler;
                        currentEvent.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    if (area.Contains(currentEvent.mousePosition))
                    {
                        CameraZoom *= 1 + 0.03f * currentEvent.delta.y;
                        currentEvent.Use();
                    }
                    break;

                // Drag and Drop.

                case EventType.DragUpdated:
                    if (area.Contains(currentEvent.mousePosition) &&
                        GetDragAndDropRoot() != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        GUIUtility.ExitGUI();
                    }
                    break;

                case EventType.DragPerform:
                    if (area.Contains(currentEvent.mousePosition))
                    {
                        var root = GetDragAndDropRoot();
                        if (root != null)
                        {
                            _OriginalRoot = root;
                            InstantiateModel();
                            GUIUtility.ExitGUI();
                        }
                    }
                    break;
            }
        }

        /************************************************************************************************************************/

        private ITransitionDetailed ShowTransitionPaused()
        {
            var transition = GetTransition();
            if (transition != null)
            {
                var animancer = InstanceAnimancer;

#if UNITY_2018_3_OR_NEWER
                animancer.States.Destroy(transition);
#endif

                animancer.Play(transition, 0);
                OnPlayAnimation();
                animancer.Evaluate();
                animancer.PauseGraph();
            }
            return transition;
        }

        /************************************************************************************************************************/

        private static Transform GetDragAndDropRoot()
        {
            var objects = DragAndDrop.objectReferences;
            if (objects.Length != 1)
                return null;

            return AnimancerEditorUtilities.FindRoot(objects[0]);
        }

        /************************************************************************************************************************/

        private void OnPlayAnimation()
        {
            var animancer = InstanceAnimancer;
            if (animancer == null ||
                animancer.States.Current == null)
                return;

            var state = animancer.States.Current;
            var normalizedEndTime = state.Events.NormalizedEndTime;
            state.Events = null;
            state.Events.NormalizedEndTime = normalizedEndTime;
        }

        /************************************************************************************************************************/
        #region Camera
        /************************************************************************************************************************/

        private Camera Camera { get { return _PreviewRenderUtility.camera; } }

        /************************************************************************************************************************/

        [SerializeField] private float _CameraZoom;

        private float CameraZoom
        {
            get { return _CameraZoom; }
            set
            {
                _CameraZoom = value;
                if (Camera != null)
                    Camera.transform.localPosition = new Vector3(0, 0, -_CameraZoom);
            }
        }

        /************************************************************************************************************************/

        [SerializeField] private Vector3 _CameraEulerAngles = new Vector3(float.NaN, float.NaN, float.NaN);

        private Vector3 CameraEulerAngles
        {
            get { return _CameraEulerAngles; }
            set
            {
                if (_SelectedInstanceType == AnimationType.Sprite)
                    return;

                _CameraEulerAngles = value;
                if (Camera != null && Camera.transform.parent != null)
                    Camera.transform.parent.localEulerAngles = value;
            }
        }

        /************************************************************************************************************************/

        private void InitialiseCamera()
        {
            var renderers = _InstanceRoot.GetComponentsInChildren<Renderer>();
            var bounds = renderers.Length > 0 ? renderers[0].bounds : default(Bounds);
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            const string CameraParentName = "Animancer Preview Camera Root";
            var cameraParent = _PreviewSceneRoot.Find(CameraParentName);
            if (cameraParent == null)
            {
                cameraParent = EditorUtility.CreateGameObjectWithHideFlags(
                    CameraParentName, HideFlags.HideAndDontSave).transform;
                cameraParent.parent = _PreviewSceneRoot;

                var lights = _PreviewRenderUtility.lights;
                for (int i = 0; i < lights.Length; i++)
                    lights[i].transform.parent = cameraParent;

                Camera.transform.parent = cameraParent;
                Camera.transform.localRotation = Quaternion.identity;
            }

            cameraParent.localPosition = bounds.center;

            Camera.farClipPlane = 100;

            if (_CameraZoom == 0)
                CameraZoom = bounds.extents.magnitude * 2 / Mathf.Tan(Camera.fieldOfView * Mathf.Deg2Rad);
            else
                CameraZoom = CameraZoom;

            if (float.IsNaN(_CameraEulerAngles.x))
                CameraEulerAngles = new Vector3(45, 135, 0);
            else
                CameraEulerAngles = CameraEulerAngles;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Floor
        /************************************************************************************************************************/

        private const float FloorScale = 5;

        [NonSerialized] private Vector3 _FloorPosition;

        private void DrawFloor()
        {
            var rotation = _SelectedInstanceType == AnimationType.Sprite ?
                Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
            var scale = Vector3.one * FloorScale;// * _AvatarScale;
            var matrix = Matrix4x4.TRS(_FloorPosition, rotation, scale);
            var layer = 0;
            var camera = _PreviewRenderUtility.camera;
            Graphics.DrawMesh(Floor.Plane, matrix, Floor.Material, layer, camera, 0);
        }

        /************************************************************************************************************************/

        // Initialisation based on UnityEditor.AvatarPreview.
        private static class Floor
        {
            /************************************************************************************************************************/

            public static readonly Material Material;
            public static readonly Mesh Plane;

            /************************************************************************************************************************/

            static Floor()
            {
                var texture = (Texture2D)EditorGUIUtility.Load("Avatar/Textures/AvatarFloor.png");
                if (texture == null)
                    return;

                var shader = EditorGUIUtility.Load("Previews/PreviewPlaneWithShadow.shader") as Shader;
                if (shader == null)
                    return;

                Material = new Material(shader)
                {
                    mainTexture = texture,
                    mainTextureScale = Vector2.one * 5f * 4f,
                    hideFlags = HideFlags.HideAndDontSave
                };
                Material.SetVector("_Alphas", new Vector4(0.5f, 0.3f, 0f, 0f));
                //this.m_FloorMaterialSmall = new Material(Material);
                //this.m_FloorMaterialSmall.mainTextureScale = Vector2.one * 0.2f * 4f;
                //this.m_FloorMaterialSmall.hideFlags = HideFlags.HideAndDontSave;

                Plane = Resources.GetBuiltinResource(typeof(Mesh), "New-Plane.fbx") as Mesh;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        [AddComponentMenu("")]
        private sealed class RedirectRootMotion : MonoBehaviour
        {
            public Animator animator;

            private void OnAnimatorMove()
            {
                if (animator == null ||
                    _Instance == null)
                    return;

                if (animator == _Instance.SelectedInstanceAnimator)
                {
                    _Instance._FloorPosition -= animator.deltaPosition;

                    _Instance._FloorPosition.x %= FloorScale;
                    _Instance._FloorPosition.y = 0;
                    _Instance._FloorPosition.z %= FloorScale;
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Cleanup
        /************************************************************************************************************************/

        private void DestroyModelInstance()
        {
            DestroyAnimancerInstance();

            if (_InstanceRoot == null)
                return;

            DestroyImmediate(_InstanceRoot.gameObject);
            _InstanceRoot = null;
            _InstanceAnimators = null;
        }

        /************************************************************************************************************************/

        private void DestroyAnimancerInstance()
        {
            if (_InstanceAnimancer == null)
                return;

            _InstanceAnimancer.Destroy();
            _InstanceAnimancer = null;
        }

        /************************************************************************************************************************/

        private void DestroyTransitionProperty()
        {
            if (_TransitionProperty == null)
                return;

            DestroyModelInstance();

            _TransitionProperty.Dispose();
            _TransitionProperty = null;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inspector
        /************************************************************************************************************************/

        private const string
            KeyPrefix = "TransitionPreviewWindow.",
            InspectorWidthKey = BoolPref.KeyPrefix + KeyPrefix + "InspectorWidth";

        [SerializeField] private float _InspectorWidth;

        [NonSerialized] private AnimationClip[] _OtherAnimations;
        [SerializeField] private AnimationClip _PreviousAnimation;
        [SerializeField] private AnimationClip _NextAnimation;

        private readonly AnimancerPlayableDrawer
            PlayableDrawer = new AnimancerPlayableDrawer();

        private const string
            PreviousAnimationKey = "Previous Animation",
            NextAnimationKey = "Next Animation";

        private static readonly BoolPref
            ShowTransition = new BoolPref(KeyPrefix, "Show Transition", true),
            AutoClose = new BoolPref(KeyPrefix, "Auto Close", true);

        /************************************************************************************************************************/

        [NonSerialized] private bool _IsResizingInspector;

        private void DoResizeInspectorGUI(Rect borderArea)
        {
            borderArea.width = 10;
            borderArea.x -= borderArea.width * 0.5f;

            EditorGUIUtility.AddCursorRect(borderArea, MouseCursor.ResizeHorizontal);

            var currentEvent = Event.current;
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    _IsResizingInspector = borderArea.Contains(currentEvent.mousePosition);
                    if (_IsResizingInspector)
                        currentEvent.Use();
                    break;

                case EventType.MouseUp:
                    if (_IsResizingInspector)
                    {
                        _IsResizingInspector = false;
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_IsResizingInspector)
                    {
                        _InspectorWidth -= currentEvent.delta.x;
                        _InspectorWidth = Mathf.Clamp(_InspectorWidth, 250, position.width - 250);
                        currentEvent.Use();
                    }
                    break;
            }
        }

        /************************************************************************************************************************/

        [SerializeField] private Vector2 _InspectorScroll;

        private void DoInspectorGUI()
        {
            EditorGUIUtility.hierarchyMode = true;

            EditorGUIUtility.labelWidth = Math.Max(_InspectorWidth * 0.55f - 60, 100);
            EditorGUIUtility.wideMode = _InspectorWidth > 300;

            GUILayout.BeginVertical(GUILayout.Width(_InspectorWidth));
            {
                _InspectorScroll = GUILayout.BeginScrollView(_InspectorScroll, GUILayout.Width(_InspectorWidth));

                if (!_TransitionProperty.IsValid())
                {
                    GUILayout.Label("No target property");
                    DestroyTransitionProperty();
                }
                else
                {
                    DoTransitionPropertyGUI();
                    DoPreviewSettingsGUI();

                    var animancer = InstanceAnimancer;
                    if (animancer != null)
                    {
                        PlayableDrawer.DoGUI(animancer.Component);
                        if (animancer.IsGraphPlaying)
                            GUI.changed = true;
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            EditorGUIUtility.hierarchyMode = false;
        }

        /************************************************************************************************************************/

        private void DoTransitionPropertyGUI()
        {
            _TransitionProperty.Update();

            var enabled = GUI.enabled;
            GUI.enabled = false;
            {
                EditorGUI.showMixedValue = _TransitionProperty.TargetObjects.Length > 1;
                EditorGUILayout.ObjectField(_TransitionProperty.TargetObject, typeof(Object), true);
                EditorGUI.showMixedValue = false;

                GUILayout.Label(_TransitionProperty.Property.GetFriendlyPath());
            }
            GUI.enabled = enabled;

            if (ShowTransition)
            {
                var isExpanded = _TransitionProperty.Property.isExpanded;
                _TransitionProperty.Property.isExpanded = true;
                var height = EditorGUI.GetPropertyHeight(_TransitionProperty, true);

                const float Indent = 12;

                var padding = GUI.skin.box.padding;

                var area = GUILayoutUtility.GetRect(0, height + padding.horizontal - padding.bottom);
                area.x += Indent + padding.left;
                area.width -= Indent + padding.horizontal;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(area, _TransitionProperty, true);
                _TransitionProperty.Property.isExpanded = isExpanded;
                if (EditorGUI.EndChangeCheck())
                    _TransitionProperty.ApplyModifiedProperties();
            }
        }

        /************************************************************************************************************************/

        private void DoPreviewSettingsGUI()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Preview Settings", "(Not Serialized)");

            DoAnimatorSelectorGUI();

            DoAnimationFieldGUI(AnimancerGUI.TempContent("Previous Animation",
                "The animation for the preview to play before the target transition"),
                _PreviousAnimation, (clip) => _PreviousAnimation = clip);

            var animancer = InstanceAnimancer;
            DoCurrentAnimationGUI(animancer);

            DoAnimationFieldGUI(AnimancerGUI.TempContent("Next Animation",
            "The animation for the preview to play after the target transition"),
            _NextAnimation, (clip) => _NextAnimation = clip);

            if (animancer != null)
            {
                animancer.Speed = EditorGUILayout.FloatField("Overall Speed", animancer.Speed);

                if (animancer.IsGraphPlaying)
                {
                    if (GUILayout.Button("Pause", EditorStyles.miniButton))
                        animancer.PauseGraph();
                }
                else
                {
                    if (GUILayout.Button("Play", EditorStyles.miniButton))
                    {
                        if (_PreviousAnimation != null)
                        {
                            InstanceAnimancer.Stop();
                            var fromState = animancer.States.GetOrCreate(PreviousAnimationKey, _Instance._PreviousAnimation, true);
                            animancer.Play(fromState);
                            OnPlayAnimation();
                            fromState.Time = 0;
                            fromState.Events.endEvent = new AnimancerEvent(1 / fromState.Length, PlayTransition);
                        }
                        else
                        {
                            PlayTransition();
                        }

                        InstanceAnimancer.UnpauseGraph();
                    }
                }
            }

#if !UNITY_2018_3_OR_NEWER
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Playing multiple animations in Edit Mode does not work properly in this version of Unity." +
                    " It can play one animation, but transitioning to or from others will look wrong" +
                    " so if you want to use this feature you should upgrade to Unity 2018.3 or newer.",
                    MessageType.Warning);
            }
#endif

            GUILayout.EndVertical();
        }

        /************************************************************************************************************************/

        private void DoAnimatorSelectorGUI()
        {
            if (_InstanceAnimators == null ||
                _InstanceAnimators.Length <= 1)
                return;

            var area = AnimancerGUI.LayoutSingleLineRect(AnimancerGUI.SpacingMode.After);
            var labelArea = AnimancerGUI.StealFromLeft(ref area, EditorGUIUtility.labelWidth, AnimancerGUI.StandardSpacing);
            GUI.Label(labelArea, "Animator");

            var selectedAnimator = SelectedInstanceAnimator;
            var label = AnimancerGUI.TempContent(selectedAnimator != null ? selectedAnimator.name : "None");
            var clicked = EditorGUI.DropdownButton(area, label, FocusType.Passive);

            if (!clicked)
                return;

            var menu = new GenericMenu();

            for (int i = 0; i < _InstanceAnimators.Length; i++)
            {
                var animator = _InstanceAnimators[i];
                label = new GUIContent(animator.name);
                var index = i;
                menu.AddItem(label, animator == selectedAnimator, () =>
                {
                    SetSelectedAnimator(index);
                });
            }

            menu.ShowAsContext();
        }

        /************************************************************************************************************************/

        private void DoAnimationFieldGUI(GUIContent label, AnimationClip clip, Action<AnimationClip> setClip)
        {
            var area = AnimancerGUI.LayoutSingleLineRect();

            var labelWidth = EditorGUIUtility.labelWidth;

#if UNITY_2019_3_OR_NEWER
            labelWidth += 2;
            area.xMin -= 1;
#else
            area.xMin += 1;
            area.xMax -= 1;
#endif

            var spacing = AnimancerGUI.StandardSpacing;
            var labelArea = AnimancerGUI.StealFromLeft(ref area, labelWidth - spacing, spacing);

            if (_OtherAnimations != null && _OtherAnimations.Length > 0)
            {
                if (EditorGUI.DropdownButton(labelArea, label, FocusType.Passive))
                {
                    var menu = new GenericMenu();

                    menu.AddItem(new GUIContent("None"), clip == null, () => setClip(null));

                    for (int i = 0; i < _OtherAnimations.Length; i++)
                    {
                        var animation = _OtherAnimations[i];
                        menu.AddItem(new GUIContent(animation.name), animation == clip, () => setClip(animation));
                    }

                    menu.ShowAsContext();
                }
            }
            else
            {
                GUI.Label(labelArea, label);
            }

            EditorGUI.BeginChangeCheck();
            clip = (AnimationClip)EditorGUI.ObjectField(area, clip, typeof(AnimationClip), true);
            if (EditorGUI.EndChangeCheck())
                setClip(clip);
        }

        /************************************************************************************************************************/

        private void DoCurrentAnimationGUI(AnimancerPlayable animancer)
        {
            const string Label = "Current Animation";

            var enabled = GUI.enabled;
            GUI.enabled = false;

            string text = null;

            if (animancer != null)
            {

                var transition = GetTransition();
                var state = animancer.States[transition];

                if (state != null)
                {
                    var mainObject = state.MainObject;
                    if (mainObject != null)
                    {
                        EditorGUILayout.ObjectField(Label, mainObject, typeof(Object), true);
                    }
                    else
                    {
                        text = state.ToString();
                    }
                }
                else
                {
                    text = _TransitionProperty.Property.GetFriendlyPath();
                }
            }
            else
            {
                text = _TransitionProperty.Property.GetFriendlyPath();
            }

            if (text != null)
                EditorGUILayout.LabelField(Label, text);

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        private void PlayTransition()
        {
            var transition = GetTransition();

#if UNITY_2018_3_OR_NEWER
            InstanceAnimancer.States.Destroy(transition);
#endif

            var targetState = InstanceAnimancer.Play(transition);
            OnPlayAnimation();

            targetState.Events.OnEnd = () =>
            {
                if (_NextAnimation != null)
                {
                    var toState = InstanceAnimancer.States.GetOrCreate(NextAnimationKey, _Instance._NextAnimation, true);
                    InstanceAnimancer.Play(toState, AnimancerPlayable.DefaultFadeDuration);
                    OnPlayAnimation();
                }
            };
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

