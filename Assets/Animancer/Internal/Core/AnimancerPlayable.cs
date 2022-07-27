// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>
    /// A <see cref="PlayableBehaviour"/> which can be used as a substitute for the
    /// <see cref="RuntimeAnimatorController"/> normally used to control an <see cref="Animator"/>.
    /// <para></para>
    /// This class can be used as a custom yield instruction to wait until all animations finish playing.
    /// </summary>
    public sealed partial class AnimancerPlayable : PlayableBehaviour,
        IEnumerator, IPlayableWrapper, IAnimationClipCollection
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>
        /// The fade duration for any of the CrossFade methods to use if the caller doesn't specify.
        /// </summary>
        public const float DefaultFadeDuration = 0.25f;

        /************************************************************************************************************************/

        /// <summary>[Internal] The <see cref="PlayableGraph"/> containing this <see cref="AnimancerPlayable"/>.</summary>
        internal PlayableGraph _Graph;

        /// <summary>[Internal] The <see cref="Playable"/> connected to the <see cref="PlayableGraph"/> output.</summary>
        internal Playable _RootPlayable;

        /// <summary>[Internal] The <see cref="Playable"/> which layers connect to.</summary>
        internal Playable _LayerMixer;

        /// <summary>[Internal] The <see cref="Playable"/> which layers connect to.</summary>
        Playable IPlayableWrapper.Playable { get { return _LayerMixer; } }

        /// <summary>An <see cref="AnimancerPlayable"/> is the root of the graph so it has no parent.</summary>
        IPlayableWrapper IPlayableWrapper.Parent { get { return null; } }

        /************************************************************************************************************************/
        // These collections can not be readonly because when Unity clones the Template it copies the memory without running the
        // field initialisers on the new clone so everything would be referencing the same collections.
        /************************************************************************************************************************/

        /// <summary>[Pro-Only] The layers which each manage their own set of animations.</summary>
        public LayerList Layers { get; private set; }

        /// <summary>The states managed by this playable.</summary>
        public StateDictionary States { get; private set; }

        /// <summary>All of the nodes that need to be updated.</summary>
        private Key.KeyedList<AnimancerNode> _DirtyNodes;

        /// <summary>All of the objects that need to be updated early.</summary>
        private Key.KeyedList<IUpdatable> _Updatables;

        /// <summary>The <see cref="PlayableBehaviour"/> that calls <see cref="IUpdatable.LateUpdate"/>.</summary>
        private LateUpdate _LateUpdate;

        /************************************************************************************************************************/

        /// <summary>The component that is playing this <see cref="AnimancerPlayable"/>.</summary>
        public IAnimancerComponent Component { get; private set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The number of times the <see cref="StateDictionary.Current"/> has changed. By storing this
        /// value and later comparing the stored value to the current value, you can determine whether the state has
        /// been changed since then, even it has changed back to the same state.
        /// </summary>
        public int CommandCount { get { return Layers[0].CommandCount; } }

        /************************************************************************************************************************/

        /// <summary>Determines what time source is used to update the <see cref="PlayableGraph"/>.</summary>
        public DirectorUpdateMode UpdateMode
        {
            get { return _Graph.GetTimeUpdateMode(); }
            set { _Graph.SetTimeUpdateMode(value); }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// How fast the <see cref="AnimancerState.Time"/> of all animations is advancing every frame.
        /// <para></para>
        /// 1 is the normal speed.
        /// <para></para>
        /// A negative value will play the animations backwards.
        /// <para></para>
        /// Setting this value to 0 would pause all animations, but calling <see cref="PauseGraph"/> is more efficient.
        /// <para></para>
        /// Animancer Lite does not allow this value to be changed in a runtime build.
        /// </summary>
        ///
        /// <example>
        /// <code>
        /// void SetSpeed(AnimancerComponent animancer)
        /// {
        ///     animancer.Playable.Speed = 1;// Normal speed.
        ///     animancer.Playable.Speed = 2;// Double speed.
        ///     animancer.Playable.Speed = 0.5f;// Half speed.
        ///     animancer.Playable.Speed = -1;// Normal speed playing backwards.
        /// }
        /// </code>
        /// </example>
        public float Speed
        {
            get { return (float)_LayerMixer.GetSpeed(); }
            set { _LayerMixer.SetSpeed(value); }
        }

        /************************************************************************************************************************/
        #region KeepChildrenConnected
        /************************************************************************************************************************/

        private bool _KeepChildrenConnected;

        /// <summary>
        /// Indicates whether child playables should stay connected to the graph at all times.
        /// <para></para>
        /// By default, this value is false so that playables will be disconnected from the graph while they are at 0
        /// weight which stops it from evaluating them every frame and is generally more efficient.
        /// </summary>
        public bool KeepChildrenConnected
        {
            get { return _KeepChildrenConnected; }
            set
            {
                if (_KeepChildrenConnected == value)
                    return;

                _KeepChildrenConnected = value;
                Layers.SetWeightlessChildrenConnected(value);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Graph Management
        /************************************************************************************************************************/

        /// <summary>
        /// Since <see cref="ScriptPlayable{T}.Create(PlayableGraph, int)"/> needs to clone an existing instance, we
        /// keep a static template to avoid allocating an extra garbage one every time.
        /// This is why the fields are assigned in OnPlayableCreate rather than being readonly with field initialisers.
        /// </summary>
        private static readonly AnimancerPlayable Template = new AnimancerPlayable();

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="PlayableGraph"/> containing an <see cref="AnimancerPlayable"/>.
        /// <para></para>
        /// The caller is responsible for calling <see cref="Destroy()"/> on the returned object, except in Edit Mode
        /// where it will be called automatically.
        /// <para></para>
        /// Consider calling <see cref="SetNextGraphName"/> before this method to give it a name.
        /// </summary>
        public static AnimancerPlayable Create()
        {
#if UNITY_EDITOR && UNITY_2018_1_OR_NEWER
            var graph = _NextGraphName != null ?
                PlayableGraph.Create(_NextGraphName) :
                PlayableGraph.Create();
            _NextGraphName = null;
#else
            var graph = PlayableGraph.Create();
#endif

            return ScriptPlayable<AnimancerPlayable>.Create(graph, Template, 2)
                .GetBehaviour();
        }

        /************************************************************************************************************************/

        /// <summary>[Internal] Called by Unity as it creates this <see cref="AnimancerPlayable"/>.</summary>
        public override void OnPlayableCreate(Playable playable)
        {
            _RootPlayable = playable;
            _Graph = playable.GetGraph();

            Layers = new LayerList(this, out _LayerMixer);
            States = new StateDictionary(this);
            _Updatables = new Key.KeyedList<IUpdatable>();
            _DirtyNodes = new Key.KeyedList<AnimancerNode>();
            _LateUpdate = LateUpdate.Create(this);

#if UNITY_EDITOR
            RegisterInstance();
#endif
        }

        /************************************************************************************************************************/

#if UNITY_EDITOR && UNITY_2018_1_OR_NEWER
        private static string _NextGraphName;
#endif

        /// <summary>[Editor-Conditional]
        /// Sets the display name for the next <see cref="Create"/> call to give its <see cref="PlayableGraph"/>.
        /// </summary>
        /// <remarks>
        /// Having this method separate from <see cref="Create"/> allows the
        /// <see cref="System.Diagnostics.ConditionalAttribute"/> to compile it out of runtime builds which would
        /// otherwise require #ifs on the caller side.
        /// </remarks>
        [System.Diagnostics.Conditional(Strings.EditorOnly)]
        public static void SetNextGraphName(string name)
        {
#if UNITY_EDITOR && UNITY_2018_1_OR_NEWER
            _NextGraphName = name;
#endif
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Plays this playable on the <see cref="IAnimancerComponent.Animator"/>.
        /// </summary>
        public void SetOutput(IAnimancerComponent animancer)
        {
            SetOutput(animancer.Animator, animancer);
        }

        /// <summary>
        /// Plays this playable on the specified `animator`.
        /// </summary>
        public void SetOutput(Animator animator, IAnimancerComponent animancer)
        {
#if UNITY_EDITOR
            // Do nothing if the target is a prefab.
            if (UnityEditor.EditorUtility.IsPersistent(animator))
                return;
#endif

#if UNITY_ASSERTIONS
            if (animancer != null)
            {
                Debug.Assert(animancer.IsPlayableInitialised && animancer.Playable == this,
                    "SetOutput was called on an AnimancerPlayable which does not match the IAnimancerComponent.Playable.");
                Debug.Assert(animator == animancer.Animator,
                    "SetOutput was called with an Animator which does not match the IAnimancerComponent.Animator.");
            }
#endif

            Component = animancer;

            var output = _Graph.GetOutput(0);
            if (output.IsOutputValid())
                _Graph.DestroyOutput(output);

            if (animator != null)
            {
                AnimationPlayableUtilities.Play(animator, _RootPlayable, _Graph);
                _IsGraphPlaying = true;
            }
        }

        /************************************************************************************************************************/

        private bool _IsGraphPlaying = true;

        /// <summary>Indicates whether the <see cref="PlayableGraph"/> is currently playing.</summary>
        public bool IsGraphPlaying
        {
            get { return _IsGraphPlaying; }
            set
            {
                if (value)
                    UnpauseGraph();
                else
                    PauseGraph();
            }
        }

        /// <summary>
        /// Resumes playing the <see cref="PlayableGraph"/> if <see cref="PauseGraph"/> was called previously.
        /// </summary>
        public void UnpauseGraph()
        {
            if (!_IsGraphPlaying)
            {
                _Graph.Play();
                _IsGraphPlaying = true;

#if UNITY_EDITOR
                // In Edit Mode, unpausing the graph does not work properly unless we force it to change.
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                    Evaluate(Time.maximumDeltaTime);
#endif
            }
        }

        /// <summary>
        /// Freezes the <see cref="PlayableGraph"/> at its current state.
        /// <para></para>
        /// If you call this method, you are responsible for calling <see cref="UnpauseGraph"/> to resume playing.
        /// </summary>
        public void PauseGraph()
        {
            if (_IsGraphPlaying)
            {
                _Graph.Stop();
                _IsGraphPlaying = false;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Evaluates all of the currently playing animations to apply their states to the animated objects.
        /// </summary>
        public void Evaluate()
        {
            _Graph.Evaluate();
        }

        /// <summary>
        /// Advances all currently playing animations by the specified amount of time (in seconds) and evaluates the
        /// graph to apply their states to the animated objects.
        /// </summary>
        public void Evaluate(float deltaTime)
        {
            _Graph.Evaluate(deltaTime);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true as long as the <see cref="PlayableGraph"/> hasn't been destroyed (such as by <see cref="Destroy()"/>).
        /// </summary>
        public bool IsValid { get { return _Graph.IsValid(); } }

        /// <summary>
        /// Destroys the <see cref="PlayableGraph"/> and all its layers and states. This operation cannot be undone.
        /// </summary>
        public void Destroy()
        {
            GC.SuppressFinalize(this);

            // Destroy all active updatables.
            Debug.Assert(_CurrentUpdatable == -1, UpdatableLoopStartError);
            _CurrentUpdatable = _Updatables.Count;
            ContinueLoop:
            try
            {
                while (--_CurrentUpdatable >= 0)
                {
                    _Updatables[_CurrentUpdatable].OnDestroy();
                }

                _Updatables.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                goto ContinueLoop;
            }

            // No need to destroy every layer and state individually because destroying the graph will do so anyway.

            Layers = null;
            States = null;

            if (_Graph.IsValid())
                _Graph.Destroy();
        }

        /************************************************************************************************************************/

        /// <summary>Appends a detailed descrption of all currently playing states and other registered states.</summary>
        public string GetDescription(int maxChildDepth = 7)
        {
            var text = new StringBuilder();
            AppendDescription(text, maxChildDepth);
            return text.ToString();
        }

        /// <summary>
        /// Appends a detailed descrption of all currently playing states and other registered states.
        /// </summary>
        public void AppendDescription(StringBuilder text, int maxChildDepth = 7)
        {
            text.Append("AnimancerPlayable (").Append(Component)
                .Append(") Layer Count: ").Append(Layers.Count);

            var count = Layers.Count;
            for (int i = 0; i < count; i++)
                Layers[i].AppendDescription(text, maxChildDepth, "\n    ");

            text.AppendLine();

            count = _Updatables.Count;
            text.Append("    Updatables: ").Append(count);
            for (int j = 0; j < count; j++)
            {
                text.AppendLine();
                text.Append("        ");
                text.Append(_Updatables[j].ToString());
            }

            text.AppendLine();

            count = _DirtyNodes.Count;
            text.Append("    Dirty Nodes: ").Append(count);
            for (int j = 0; j < count; j++)
            {
                text.AppendLine();
                text.Append("        ");
                text.Append(_DirtyNodes[j].ToString());
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Play Management
        /************************************************************************************************************************/

        /// <summary>Calls <see cref="IAnimancerComponent.GetKey"/> on the <see cref="Component"/>.</summary>
        public object GetKey(AnimationClip clip)
        {
            return Component.GetKey(clip);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Stops all other animations, plays the `clip`, and returns its state.
        /// <para></para>
        /// The animation will continue playing from its current <see cref="AnimancerState.Time"/>.
        /// To restart it from the beginning you can use <c>...Play(clip, layerIndex).Time = 0;</c>.
        /// </summary>
        public AnimancerState Play(AnimationClip clip)
        {
            return Play(States.GetOrCreate(clip));
        }

        /// <summary>
        /// Stops all other animations, plays the `state`, and returns it.
        /// <para></para>
        /// The animation will continue playing from its current <see cref="AnimancerState.Time"/>.
        /// If you wish to force it back to the start, you can simply set the `state`s time to 0.
        /// </summary>
        public AnimancerState Play(AnimancerState state)
        {
            return state.Layer.Play(state);
        }

        /// <summary>
        /// Creates a state for the `transition` if it didn't already exist, then calls
        /// <see cref="Play(AnimancerState)"/> or <see cref="Play(AnimancerState, float, FadeMode)"/>
        /// depending on the <see cref="ITransition.FadeDuration"/>.
        /// </summary>
        public AnimancerState Play(ITransition transition)
        {
            return Play(transition, transition.FadeDuration, transition.FadeMode);
        }

        /// <summary>
        /// Stops all other animations, plays the animation registered with the `key`, and returns that
        /// state. If no state is registered with the `key`, this method does nothing and returns null.
        /// <para></para>
        /// The animation will continue playing from its current <see cref="AnimancerState.Time"/>.
        /// If you wish to force it back to the start, you can simply set the returned state's time to 0.
        /// on the returned state.
        /// </summary>
        public AnimancerState Play(object key)
        {
            AnimancerState state;
            if (States.TryGet(key, out state))
                return Play(state);
            else
                return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Starts fading in the `clip` over the course of the `fadeDuration` while fading out all others in the same
        /// layer. Returns its state.
        /// <para></para>
        /// If the `state` was already playing and fading in with less time remaining than the `fadeDuration`, this
        /// method will allow it to complete the existing fade rather than starting a slower one.
        /// <para></para>
        /// If the layer currently has 0 <see cref="AnimancerNode.Weight"/>, this method will fade in the layer itself
        /// and simply <see cref="AnimancerState.Play"/> the `state`.
        /// <para></para>
        /// Animancer Lite only allows the default `fadeDuration` (0.25 seconds) in a runtime build.
        /// </summary>
        public AnimancerState Play(AnimationClip clip, float fadeDuration, FadeMode mode = FadeMode.FixedSpeed)
        {
            return Play(States.GetOrCreate(clip), fadeDuration, mode);
        }

        /// <summary>
        /// Starts fading in the `state` over the course of the `fadeDuration` while fading out all others in the same
        /// layer. Returns the `state`.
        /// <para></para>
        /// If the `state` was already playing and fading in with less time remaining than the `fadeDuration`, this
        /// method will allow it to complete the existing fade rather than starting a slower one.
        /// <para></para>
        /// If the layer currently has 0 <see cref="AnimancerNode.Weight"/>, this method will fade in the layer itself
        /// and simply <see cref="AnimancerState.Play"/> the `state`.
        /// <para></para>
        /// Animancer Lite only allows the default `fadeDuration` (0.25 seconds) in a runtime build.
        /// </summary>
        public AnimancerState Play(AnimancerState state, float fadeDuration, FadeMode mode = FadeMode.FixedSpeed)
        {
            return state.Layer.Play(state, fadeDuration, mode);
        }

        /// <summary>
        /// Creates a state for the `transition` if it didn't already exist, then calls
        /// <see cref="Play(AnimancerState)"/> or <see cref="Play(AnimancerState, float, FadeMode)"/>
        /// depending on the <see cref="ITransition.FadeDuration"/>.
        /// </summary>
        public AnimancerState Play(ITransition transition, float fadeDuration, FadeMode mode = FadeMode.FixedSpeed)
        {
            var state = States.GetOrCreate(transition);
            state = Play(state, fadeDuration, mode);
            transition.Apply(state);
            return state;
        }

        /// <summary>
        /// Starts fading in the animation registered with the `key` over the course of the `fadeDuration` while fading
        /// out all others in the same layer. Returns the animation's state (or null if none was registered).
        /// <para></para>
        /// If the `state` was already playing and fading in with less time remaining than the `fadeDuration`, this
        /// method will allow it to complete the existing fade rather than starting a slower one.
        /// <para></para>
        /// If the layer currently has 0 <see cref="AnimancerNode.Weight"/>, this method will fade in the layer itself
        /// and simply <see cref="AnimancerState.Play"/> the `state`.
        /// <para></para>
        /// Animancer Lite only allows the default `fadeDuration` (0.25 seconds) in a runtime build.
        /// </summary>
        public AnimancerState Play(object key, float fadeDuration, FadeMode mode = FadeMode.FixedSpeed)
        {
            AnimancerState state;
            if (States.TryGet(key, out state))
                return Play(state, fadeDuration, mode);
            else
                return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets the state registered with the <see cref="IHasKey.Key"/>, stops and rewinds it to the start, then
        /// returns it.
        /// </summary>
        public AnimancerState Stop(IHasKey hasKey)
        {
            return Stop(hasKey.Key);
        }

        /// <summary>
        /// Calls <see cref="AnimancerState.Stop"/> on the state registered with the `key` to stop it from playing and
        /// rewind it to the start.
        /// </summary>
        public AnimancerState Stop(object key)
        {
            AnimancerState state;
            if (States.TryGet(key, out state))
                state.Stop();

            return state;
        }

        /// <summary>
        /// Calls <see cref="AnimancerState.Stop"/> on all animations to stop them from playing and rewind them to the
        /// start.
        /// </summary>
        public void Stop()
        {
            if (Layers._Layers == null)
                return;

            var count = Layers.Count;
            for (int i = 0; i < count; i++)
                Layers._Layers[i].Stop();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if a state is registered with the <see cref="IHasKey.Key"/> and it is currently playing.
        /// </summary>
        public bool IsPlaying(IHasKey hasKey)
        {
            return IsPlaying(hasKey.Key);
        }

        /// <summary>
        /// Returns true if a state is registered with the `key` and it is currently playing.
        /// </summary>
        public bool IsPlaying(object key)
        {
            AnimancerState state;

            return
                States.TryGet(key, out state) &&
                state.IsPlaying;
        }

        /// <summary>
        /// Returns true if at least one animation is being played.
        /// </summary>
        public bool IsPlaying()
        {
            if (!_IsGraphPlaying)
                return false;

            var count = Layers.Count;
            for (int i = 0; i < count; i++)
            {
                if (Layers._Layers[i].IsAnyStatePlaying())
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the `clip` is currently being played by at least one state in the specified layer.
        /// <para></para>
        /// This method is inefficient because it searches through every state to find any that are playing the `clip`,
        /// unlike <see cref="IsPlaying(object)"/> which only checks the state registered using the specified key.
        /// </summary>
        public bool IsPlayingClip(AnimationClip clip)
        {
            if (!_IsGraphPlaying)
                return false;

            var count = Layers.Count;
            while (--count >= 0)
            {
                if (Layers._Layers[count].IsPlayingClip(clip))
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calculates the total <see cref="AnimancerNode.Weight"/> of all states in this playable.
        /// </summary>
        public float GetTotalWeight()
        {
            float weight = 0;

            var count = Layers.Count;
            for (int i = 0; i < count; i++)
            {
                weight += Layers._Layers[i].GetTotalWeight();
            }

            return weight;
        }

        /************************************************************************************************************************/

        /// <summary>[<see cref="IAnimationClipCollection"/>]
        /// Gathers all the animations in all layers.
        /// </summary>
        public void GatherAnimationClips(ICollection<AnimationClip> clips)
        {
            Layers.GatherAnimationClips(clips);
        }

        /************************************************************************************************************************/
        // IEnumerator for yielding in a coroutine to wait until animations have stopped.
        /************************************************************************************************************************/

        /// <summary>
        /// Determines if any animations are still playing so this object can be used as a custom yield instruction.
        /// </summary>
        bool IEnumerator.MoveNext()
        {
            var count = Layers.Count;
            for (int i = 0; i < count; i++)
            {
                if (Layers._Layers[i].IsPlayingAndNotEnding())
                    return true;
            }

            return false;
        }

        /// <summary>Returns null.</summary>
        object IEnumerator.Current { get { return null; } }

        /// <summary>Does nothing.</summary>
        void IEnumerator.Reset() { }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region End Events
        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="AnimationEvent"/> called 'End' which is currently being triggered.
        /// </summary>
        public static AnimationEvent CurrentEndEvent { get; private set; }

        /************************************************************************************************************************/

        /// <summary>
        /// Invokes the <see cref="AnimancerEvent.Sequence.OnEnd"/> callback of the state that is playing the animation
        /// which triggered the event. Returns true if such a state exists (even if it doesn't have a callback).
        /// </summary>
        public bool OnEndEventReceived(AnimationEvent animationEvent)
        {
            // This method could be changed to invoke all events with the correct clip and weight by collecting all the
            // events into a list and invoking them at the end.

            var count = Layers.Count;
            for (int i = 0; i < count; i++)
            {
                if (TryInvokeOnEndEvent(animationEvent, Layers._Layers[i].CurrentState))
                    return true;
            }

            for (int i = 0; i < count; i++)
            {
                if (Layers._Layers[i].TryInvokeOnEndEvent(animationEvent))
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the <see cref="AnimancerState.Clip"/> and <see cref="AnimancerNode.Weight"/> match the
        /// <see cref="AnimationEvent"/>, this method invokes the <see cref="AnimancerEvent.Sequence.OnEnd"/> callback
        /// and returns true.
        /// </summary>
        internal static bool TryInvokeOnEndEvent(AnimationEvent animationEvent, AnimancerState state)
        {
            if (state.Weight != animationEvent.animatorClipInfo.weight ||
                state.Clip != animationEvent.animatorClipInfo.clip ||
                !state.HasEvents)
                return false;

            var endEvent = state.Events.endEvent;
            if (endEvent.callback != null)
            {
                Debug.Assert(CurrentEndEvent == null, "Recursive call to TryInvokeOnEndEvent detected");

                try
                {
                    CurrentEndEvent = animationEvent;
                    endEvent.Invoke(state);
                }
                finally
                {
                    CurrentEndEvent = null;
                }
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the <see cref="CurrentEndEvent"/> has a float parameter above 0, this method returns that value.
        /// Otherwise this method calls <see cref="AnimancerEvent.GetFadeOutDuration"/> so if you aren't using an
        /// Animation Event with the function name "End" you can just call that method directly.
        /// </summary>
        public static float GetFadeOutDuration(float minDuration = DefaultFadeDuration)
        {
            if (CurrentEndEvent != null && CurrentEndEvent.floatParameter > 0)
                return CurrentEndEvent.floatParameter;

            return AnimancerEvent.GetFadeOutDuration(minDuration);
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
        /// Just call <see cref="AnimancerState.Stop"/>.
        /// </summary>
        [System.Obsolete("You should not use an AnimancerState as a key. Just call AnimancerState.Stop().", true)]
        public AnimancerState Stop(AnimancerState key)
        {
            key.Stop();
            return key;
        }

        /// <summary>[Warning]
        /// You should not use an <see cref="AnimancerState"/> as a key.
        /// Just check <see cref="AnimancerState.IsPlaying"/>.
        /// </summary>
        [System.Obsolete("You should not use an AnimancerState as a key. Just check AnimancerState.IsPlaying.", true)]
        public bool IsPlaying(AnimancerState key)
        {
            return key.IsPlaying;
        }

        /************************************************************************************************************************/
#endif
        #endregion
        /************************************************************************************************************************/
        #region Update
        /************************************************************************************************************************/

        /// <summary>
        /// Adds the `updatable` to the list of objects that need to be updated if it wasn't there already.
        /// <para></para>
        /// This method is safe to call at any time, even during an update.
        /// <para></para>
        /// The execution order of updatables is non-deterministic. Specifically, the most recently added will be
        /// updated first and <see cref="CancelUpdate"/> will change the order by swapping the last one into the place
        /// of the removed element.
        /// </summary>
        public void RequireUpdate(IUpdatable updatable)
        {
            _Updatables.AddNew(updatable);
        }

        /// <summary>
        /// Removes the `updatable` from the list of objects that need to be updated.
        /// <para></para>
        /// This method is safe to call at any time, even during an update.
        /// <para></para>
        /// The last element is swapped into the place of the one being removed so that the rest of them do not need to
        /// be moved down one place to fill the gap. This is more efficient, by means that the update order can change.
        /// </summary>
        public void CancelUpdate(IUpdatable updatable)
        {
            var index = Key.IndexOf(updatable.Key);
            if (index < 0)
                return;

            _Updatables.RemoveAtSwap(index);

            if (_CurrentUpdatable < index && this == Current)
                _CurrentUpdatable--;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds the `node` to the list that need to be updated if it wasn't there already.
        /// This method is safe to call at any time, even during an update.
        /// </summary>
        public void RequireUpdate(AnimancerNode node)
        {
            Validate.Root(node, this);
            _DirtyNodes.AddNew(node);
        }

        /************************************************************************************************************************/

        /// <summary>The object currently executing <see cref="PrepareFrame"/>.</summary>
        public static AnimancerPlayable Current { get; private set; }

        /// <summary>
        /// The current (most recent) <see cref="FrameData.deltaTime"/>.
        /// <para></para>
        /// After <see cref="PrepareFrame"/>, this property will be left at its most recent value.
        /// </summary>
        public static float DeltaTime { get; private set; }

        /// <summary>
        /// The current (most recent) <see cref="FrameData.frameId"/>.
        /// <para></para>
        /// <see cref="AnimancerState.Time"/> uses this value to determine whether it has accessed the playable's time
        /// since it was last updated in order to cache its value.
        /// </summary>
        public uint FrameID { get; private set; }

        /// <summary>The index of the <see cref="IUpdatable"/> currently being updated.</summary>
        private static int _CurrentUpdatable = -1;

        /// <summary>An error message for potential multithreading issues.</summary>
        private const string UpdatableLoopStartError = "AnimancerPlayable._CurrentUpdatable != -1." +
            " This may mean that multiple loops are iterating through the updatables simultaneously" +
            " (likely on different threads).";

        /************************************************************************************************************************/

        /// <summary>[Internal]
        /// Called by the <see cref="PlayableGraph"/> before the rest of the <see cref="Playable"/>s are evaluated.
        /// Calls <see cref="IUpdatable.EarlyUpdate"/> and <see cref="AnimancerNode.Update"/> on everything
        /// that needs it.
        /// </summary>
        public override void PrepareFrame(Playable playable, FrameData info)
        {
            Current = this;
            DeltaTime = info.deltaTime;

            // These loops could potentially be swapped. The only thing EarlyUpdate currently does is cache the time of
            // states for events to compare with the time after updating.

            Debug.Assert(_CurrentUpdatable == -1, UpdatableLoopStartError);
            _CurrentUpdatable = _Updatables.Count;
            ContinueLoop:
            try
            {
                while (--_CurrentUpdatable >= 0)
                {
                    _Updatables[_CurrentUpdatable].EarlyUpdate();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                goto ContinueLoop;
            }

            var count = _DirtyNodes.Count;
            while (--count >= 0)
            {
                bool needsMoreUpdates;
                _DirtyNodes[count].Update(out needsMoreUpdates);
                if (!needsMoreUpdates)
                    _DirtyNodes.RemoveAtSwap(count);
            }

            _LateUpdate.IsConnected = _Updatables.Count != 0;

            // Any time before or during this method will still have all Playables at their time from last frame, so we
            // don't want them to think their time is dirty until we are done.
            FrameID = (uint)info.frameId;
            Current = null;
        }

        /************************************************************************************************************************/
        #region Late Update
        /************************************************************************************************************************/

        /// <summary>
        /// A <see cref="PlayableBehaviour"/> which connects to a later port than the main layer mixer so that its
        /// <see cref="PrepareFrame"/> method gets called after all other playables are updated in order to call
        /// <see cref="IUpdatable.LateUpdate"/> on the <see cref="_Updatables"/>.
        /// </summary>
        private sealed class LateUpdate : PlayableBehaviour
        {
            /************************************************************************************************************************/

            /// <summary>See <see cref="AnimancerPlayable.Template"/>.</summary>
            private static readonly LateUpdate Template = new LateUpdate();

            /// <summary>The <see cref="AnimancerPlayable"/> this behaviour is connected to.</summary>
            private AnimancerPlayable _Root;

            /// <summary>The underlying <see cref="Playable"/> of this behaviour.</summary>
            private Playable _Playable;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="LateUpdate"/> for the `root`.</summary>
            public static LateUpdate Create(AnimancerPlayable root)
            {
                var instance = ScriptPlayable<LateUpdate>.Create(root._Graph, Template, 0)
                    .GetBehaviour();
                instance._Root = root;
                return instance;
            }

            /************************************************************************************************************************/

            /// <summary>Called by Unity as it creates this <see cref="AnimancerPlayable"/>.</summary>
            public override void OnPlayableCreate(Playable playable)
            {
                _Playable = playable;
            }

            /************************************************************************************************************************/

            private bool _IsConnected;

            /// <summary>
            /// Indicates whether this behaviour is connected to the <see cref="PlayableGraph"/> and thus, whether it
            /// will receive <see cref="PrepareFrame"/> calls.
            /// </summary>
            public bool IsConnected
            {
                get { return _IsConnected; }
                set
                {
                    if (value)
                    {
                        if (!_IsConnected)
                        {
                            _IsConnected = true;
                            _Root._Graph.Connect(_Playable, 0, _Root._RootPlayable, 1);
                        }
                    }
                    else
                    {
                        if (!_IsConnected)
                        {
                            _IsConnected = false;
                            _Root._Graph.Disconnect(_Root._RootPlayable, 1);
                        }
                    }
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Called by the <see cref="PlayableGraph"/> after the rest of the <see cref="Playable"/>s are evaluated.
            /// Calls <see cref="IUpdatable.LateUpdate"/> on everything that needs it.
            /// </summary>
            public override void PrepareFrame(Playable playable, FrameData info)
            {
                Debug.Assert(_CurrentUpdatable == -1, UpdatableLoopStartError);
                var updatables = _Root._Updatables;
                _CurrentUpdatable = updatables.Count;
                ContinueLoop:
                try
                {
                    while (--_CurrentUpdatable >= 0)
                    {
                        updatables[_CurrentUpdatable].LateUpdate();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    goto ContinueLoop;
                }

                // Ideally we would be able to update the dirty nodes here instead of in the early update so that they
                // can respond immediately to the effects of the late update.

                // However, doing that with KeepChildrenConnected == false (the default for efficiency) causes problems
                // where states that aren't connected early (before they update) don't affect the output even though
                // weight changes do apply. So in the first frame when cross fading to a new animation it will lower
                // the weight of the previous state a bit without the corresponding increase to the new animation's
                // weight having any effect, giving a total weight less than 1 and thus an incorrect output.
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Editor
#if UNITY_EDITOR
        /************************************************************************************************************************/

        private static List<AnimancerPlayable> _AllInstances;

        /// <summary>[Editor-Only]
        /// Registers this object in the list of things that need to be updated in edit-mode.
        /// </summary>
        private void RegisterInstance()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (_AllInstances == null)
            {
                _AllInstances = new List<AnimancerPlayable>();

                var previousFrameTime = UnityEditor.EditorApplication.timeSinceStartup;

                UnityEditor.EditorApplication.update += () =>
                {
                    var time = UnityEditor.EditorApplication.timeSinceStartup;
#if !UNITY_2018_3_OR_NEWER
                    var deltaTime = (float)(time - previousFrameTime);
#endif
                    previousFrameTime = time;

                    for (int i = _AllInstances.Count - 1; i >= 0; i--)
                    {
                        var playable = _AllInstances[i];
                        if (playable.ShouldStayAlive())
                        {
#if !UNITY_2018_3_OR_NEWER
                            // Unity 2018.3+ automatically updates playables in Edit Mode.
                            if (playable._IsGraphPlaying)
                                playable.Evaluate(deltaTime);
#endif
                        }
                        else
                        {
                            if (playable != null &&
                                playable.IsValid)
                                playable.Destroy();

                            _AllInstances.RemoveAt(i);
                        }
                    }
                };

                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
                {
                    for (int i = _AllInstances.Count - 1; i >= 0; i--)
                    {
                        var playable = _AllInstances[i];
                        if (playable.IsValid)
                            playable.Destroy();
                    }

                    _AllInstances.Clear();
                };
            }

            _AllInstances.Add(this);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Determines whether this playable should stay alive or be destroyed.
        /// </summary>
        private bool ShouldStayAlive()
        {
            if (!IsValid)
                return false;

            if (Component == null)
                return true;

            var obj = Component as Object;
            if (!ReferenceEquals(obj, null) && obj == null)
                return false;

            if (Component.Animator == null)
                return false;

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns true if the `initial` mode was <see cref="AnimatorUpdateMode.AnimatePhysics"/> and the `current`
        /// has changed to another mode or if the `initial` mode was something else and the `current` has changed to
        /// <see cref="AnimatorUpdateMode.AnimatePhysics"/>.
        /// </summary>
        public static bool HasChangedToOrFromAnimatePhysics(AnimatorUpdateMode? initial, AnimatorUpdateMode current)
        {
            if (initial == null)
                return false;

            var wasAnimatePhysics = initial.Value == AnimatorUpdateMode.AnimatePhysics;
            var isAnimatePhysics = current == AnimatorUpdateMode.AnimatePhysics;
            return wasAnimatePhysics != isAnimatePhysics;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draws the <see cref="_Updatables"/> and <see cref="_DirtyNodes"/> lists.
        /// </summary>
        internal void DoUpdateListGUI()
        {
            Editor.AnimancerGUI.BeginVerticalBox(GUI.skin.box);

            GUILayout.Label("Updatables " + _Updatables.Count);
            for (int i = 0; i < _Updatables.Count; i++)
            {
                GUILayout.Label(_Updatables[i].ToString());
            }

            GUILayout.Label("Dirty Nodes " + _DirtyNodes.Count);
            for (int i = 0; i < _DirtyNodes.Count; i++)
            {
                GUILayout.Label(_DirtyNodes[i].ToString());
            }

            Editor.AnimancerGUI.EndVerticalBox(GUI.skin.box);

            if (Editor.AnimancerGUI.TryUseClickEventInLastRect(1))
            {
                var menu = new UnityEditor.GenericMenu();
                Editor.AnimancerLayerDrawer.ShowUpdatingNodes.AddToggleFunction(menu);
                menu.ShowAsContext();
            }
        }

        /************************************************************************************************************************/
#endif
        #endregion
        /************************************************************************************************************************/
    }
}

