// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Text;
using UnityEngine;

namespace Animancer
{
    /// <summary>
    /// A <see cref="callback"/> delegate paired with a <see cref="normalizedTime"/> to determine when to invoke it.
    /// </summary>
    public partial struct AnimancerEvent
    {
        /************************************************************************************************************************/
        #region Event
        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimancerState.NormalizedTime"/> at which to invoke the <see cref="callback"/>.</summary>
        public float normalizedTime;

        /// <summary>The delegate to invoke when the <see cref="normalizedTime"/> passes.</summary>
        public Action callback;

        /// <summary>The largest possible float value less than 1.</summary>
        public const float AlmostOne = 0.99999994f;

        /************************************************************************************************************************/

        /// <summary>Constructs a new <see cref="AnimancerEvent"/>.</summary>
        public AnimancerEvent(float normalizedTime, Action callback)
        {
            this.normalizedTime = normalizedTime;
            this.callback = callback;
        }

        /************************************************************************************************************************/

        /// <summary>Returns "AnimancerEvent(normalizedTime, callbackTarget.CallbackMethod)".</summary>
        public override string ToString()
        {
            var text = new StringBuilder()
                .Append("AnimancerEvent(")
                .Append(normalizedTime)
                .Append(", ");

            if (callback == null)
            {
                text.Append("null)");
            }
            else if (callback.Target == null)
            {
                text.Append(callback.Method.Name)
                    .Append(")");
            }
            else
            {
                text.Append(callback.Target)
                    .Append('.')
                    .Append(callback.Method.Name)
                    .Append(")");
            }

            return text.ToString();
        }

        /************************************************************************************************************************/

        /// <summary>Appends the details of this event to the `text`.</summary>
        public void AppendDetails(StringBuilder text, string name, string delimiter = "\n")
        {
            text.Append(delimiter).Append(name).Append(".NormalizedTime=").Append(normalizedTime);

            if (callback != null)
            {
                text.Append(delimiter).Append(name).Append(".Target=").Append(callback.Target);
                text.Append(delimiter).Append(name).Append(".Method=").Append(callback.Method);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Invocation
        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimancerState"/> currently triggering an event using <see cref="Invoke"/>.</summary>
        public static AnimancerState CurrentState { get { return _CurrentState; } }
        private static AnimancerState _CurrentState;

        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimancerEvent"/> currently being triggered by <see cref="Invoke"/>.</summary>
        public static AnimancerEvent CurrentEvent { get { return _CurrentEvent; } }
        private static AnimancerEvent _CurrentEvent;

        /************************************************************************************************************************/

        /// <summary>
        /// Sets the static <see cref="CurrentState"/> and <see cref="CurrentEvent"/> then invokes the <see cref="callback"/>.
        /// <para></para>
        /// This method catches and logs any exception thrown by the <see cref="callback"/>.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown if the <see cref="callback"/> is null.</exception>
        public void Invoke(AnimancerState state)
        {
            var previousState = _CurrentState;
            var previousEvent = _CurrentEvent;

            _CurrentState = state;
            _CurrentEvent = this;

            try
            {
                callback();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            _CurrentState = previousState;
            _CurrentEvent = previousEvent;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns either the `minDuration` or the <see cref="AnimancerState.RemainingDuration"/> of the
        /// <see cref="CurrentState"/> state (whichever is higher).
        /// </summary>
        public static float GetFadeOutDuration(float minDuration = AnimancerPlayable.DefaultFadeDuration)
        {
            var state = CurrentState;
            if (state == null)
                return minDuration;

            var time = state.Time;
            var speed = state.EffectiveSpeed;

            float remainingDuration;
            if (state.IsLooping)
            {
                var previousTime = time - speed * Time.deltaTime;
                var inverseLength = 1f / state.Length;

                // If we just passed the end of the animation, the remaining duration would technically be the full
                // duration of the animation, so we most likely want to use the minimum duration instead.
                if (Math.Floor(time * inverseLength) != Math.Floor(previousTime * inverseLength))
                    return minDuration;
            }

            if (speed > 0)
            {
                remainingDuration = (state.Length - time) * speed;
            }
            else
            {
                remainingDuration = time * -speed;
            }

            return Math.Max(minDuration, remainingDuration);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

