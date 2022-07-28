using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CharacterController : MonoBehaviour
{
    [SerializeField] private SimpleAnimancer _animancer;
    [SerializeField] private FixedJoystick fixedJoystick;
    [SerializeField] private AudioSource _audio;


    [SerializeField] private string _idleAnimName = "Idle";
    [SerializeField] private float _idleAnimSpeed = 1f;
    [SerializeField] private string _runAnimName = "Running";
    [SerializeField] private float _runAnimSpeed = 2f;
    [SerializeField] private string _climbAnimName = "Climb";
    [SerializeField] private float _climbAnimSpeed = 2f;
    private bool _isClimbingUpward = false;
    private bool _isClimbingDownward = false;
    private bool _isClimbed = false;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (_isClimbingUpward && !_isClimbed)
        {
            ClimbAnimation();
            transform.DOMoveY(7f, 0.1f).SetRelative();
        }
        else if (_isClimbingUpward && _isClimbed)
        {
            _audio.Stop();
            IdleAnimation();
        }
        else if (_isClimbingDownward)
        {
            ClimbAnimation();
            transform.DOMoveY(-7f, 0.1f).SetRelative();
        }
        else if (fixedJoystick.Vertical != 0 || fixedJoystick.Horizontal != 0)
        {
            RunAnimation();
        }
        else
            IdleAnimation();
        
        transform.DOMove((Vector3.forward * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal), 0.03f).SetRelative();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "LadderStart")
        {
            _isClimbingUpward = true;
            _isClimbingDownward = false;
            _audio.Play();
        }
        else if (other.gameObject.tag == "LadderEnd")
        {
            _isClimbingUpward = false;
            _isClimbingDownward = true;
            _isClimbed = true;
        }
    }

    public void IdleAnimation()
    {
        PlayAnimation(_idleAnimName, _idleAnimSpeed);
    }

    public void RunAnimation()
    {
        PlayAnimation(_runAnimName, _runAnimSpeed);
    }

    public void ClimbAnimation()
    {
        PlayAnimation(_climbAnimName, _climbAnimSpeed);
    }
    public void PlayAnimation(string animName, float animSpeed)
    {
        _animancer.PlayAnimation(animName);
        _animancer.SetStateSpeed(animSpeed);
    }
}
