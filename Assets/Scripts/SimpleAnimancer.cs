using Animancer;
using UnityEngine;


    public class SimpleAnimancer : MonoBehaviour
    {
        [SerializeField] private AnimancerComponent _animancer;
        [SerializeField] private float _fadeDuration = 0.1f;
        [Space]
        [SerializeField] private AnimationClip[] _clips;
        [SerializeField] private bool _playDefault = false;

        private AnimancerState _currentState;

        private void Start()
        {
            if (_playDefault)
            {
                PlayAnimation(_clips[0].name);
            }
        }

        public void Kill()
        {
            _animancer.Stop();
            _animancer.enabled = false;
        }

        public void MakeRandomKeyframe()
        {
            _currentState.NormalizedTime = Random.Range(0f, 1f);
        }

        public void PlayAnimation(string clipName)
        {
            AnimationClip clip = GetAnimationClipByName(clipName);

            //Debug.Log("KLÝP VAR MI "+ (clip.name));
            if (_animancer != null && clip != null)
            {
                _currentState = _animancer.Play(clip, _fadeDuration);
            }
        }

        public void PlayAnimation(AnimationClip clip)
        {
            if (_animancer != null && clip != null)
            {
                _currentState = _animancer.Play(clip, _fadeDuration);
            }
        }

        public void PlayMixer(LinearMixerTransition transition, float speed)
        {
            _currentState = _animancer.Play(transition, _fadeDuration);
        }

        public void SetStateSpeed(float speed)
        {
            if (_currentState == null)
            {
                return;
            }
            _currentState.Speed = speed;
        }

        AnimationClip GetAnimationClipByName(string clipName)
        {
            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i].name.Equals(clipName))
                {
                    return _clips[i];
                }
            }
            return null;
        }

        public Transform GetAnimatorTransform()
        {
            return _animancer.transform;
        }
    }
