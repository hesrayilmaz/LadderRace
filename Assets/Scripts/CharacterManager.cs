using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] private SimpleAnimancer _animancer;
    [SerializeField] private FixedJoystick fixedJoystick;
    [SerializeField] private AudioSource _climbAudio;
    [SerializeField] private LevelController _newLevel;
    [SerializeField] private BuildLadder _ladder;

    [SerializeField] private string _idleAnimName = "Idle";
    [SerializeField] private float _idleAnimSpeed = 1f;
    [SerializeField] private string _runAnimName = "Running";
    [SerializeField] private float _runAnimSpeed = 2f;
    [SerializeField] private string _climbAnimName = "Climb";
    [SerializeField] private float _climbAnimSpeed = 3f;
    [SerializeField] private float _rotateSpeed = 10f;

    public static bool _isClimbingUpward = false;
    public static bool _isClimbed = false; 
    public static bool _isNewLevel = false; 
    private bool _isCurrentLevel = true; 
    private Vector3 _characterPos;


    // Update is called once per frame
    void Update()
    {
        if (fixedJoystick.Vertical != 0 || fixedJoystick.Horizontal != 0)
        {
            //Debug.Log("1111");
            _isClimbed = false;
            RunAnimation();
        }
        else if (_isClimbingUpward && !_isClimbed)
        {
            //Debug.Log("2222");
            ClimbAnimation();
            transform.DOMoveY(7f, 0.05f).SetRelative();
        }
        else if (_isClimbed)
        {
            //Debug.Log("3333");
            _climbAudio.Stop();
            IdleAnimation();
            _isCurrentLevel = true;
        }
        else
            IdleAnimation();

        if (_isClimbed && _newLevel.GetCurrentLevel() != null)
        {
            _characterPos = _newLevel.GetCurrentLevel().transform.position + new Vector3(80f, -404f, -1600f);
            transform.position = _characterPos;
            transform.DOMove(_characterPos, 0.015f);
        }

        transform.DOMove((Vector3.forward * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal), 0.015f).SetRelative();
        //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation((Vector3.forward * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal)), Time.deltaTime * _rotateSpeed);

    }
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "LadderStart")
        {
            //_isClimbingUpward = true;
           
            _isClimbed = false;
           // _climbAudio.Play();
            if (_isCurrentLevel)
            {
                _newLevel.GenerateLevel();
                _isCurrentLevel = false;
            }
        
            _ladder.Drop();
            _isNewLevel = false;
        }
        else if (other.gameObject.tag == "LadderEnd")
        {
            _isClimbingUpward = false;
            _isClimbed = true;
            _isNewLevel = true;
            
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
