using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] private SimpleAnimancer _animancer;
    [SerializeField] private FixedJoystick fixedJoystick;
    [SerializeField] private AudioSource _climbAudio;
    [SerializeField] private GameObject _platform;
    [SerializeField] private LevelController _newLevel;
    [SerializeField] private PickUpItems _ladder;

    [SerializeField] private string _idleAnimName = "Idle";
    [SerializeField] private float _idleAnimSpeed = 1f;
    [SerializeField] private string _runAnimName = "Running";
    [SerializeField] private float _runAnimSpeed = 2f;
    [SerializeField] private string _climbAnimName = "Climb";
    [SerializeField] private float _climbAnimSpeed = 2f;
    [SerializeField] private float _rotateSpeed = 10f;

    public static bool _isClimbingUpward = false;
    public static bool _isClimbingDownward = false;
    public static bool _isClimbedOnce = true;
    public static bool _isClimbed = false; 
    public static bool _isNewLevel = false; 
    private bool _isCurrentLevel = true; 
    private bool _isLastBrick = true;
    private Vector3 _characterPos;

    // Start is called before the first frame update
    void Start()
    {

    }

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
            transform.DOMoveY(7f, 0.06f).SetRelative();
        }
        else if (_isClimbed)
        {
            //Debug.Log("3333");
            _climbAudio.Stop();
            IdleAnimation();
            _isCurrentLevel = true;
            _isClimbedOnce = true;
            PickUpItems._bricksOnLadder.Clear();
        }
        else if (_isClimbingDownward)
        {
            ClimbAnimation();
            transform.DOMoveY(-7f, 0.07f).SetRelative();
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
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation((Vector3.forward * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal)), Time.deltaTime * _rotateSpeed);

    }
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "LadderStart" && _isClimbedOnce)
        {
           
            _isClimbingUpward = true;
            //_isClimbingDownward = false;
            _isClimbed = false;
            _climbAudio.Play();
            if (_isCurrentLevel)
            {
                _newLevel.GenerateLevel();
                _isCurrentLevel = false;
            }
            
           //if(_isLastBrick)
            _ladder.Drop();
            _isClimbedOnce = false;
            _isLastBrick = false;
            _isNewLevel = false;
        }
        else if(other.gameObject.tag == "LadderStart")
        {
            _isClimbingUpward = false;
            _isClimbingDownward = false;
            _climbAudio.Stop();
            _isClimbedOnce = true;
        }
        else if (other.gameObject.tag == "LadderEnd")
        {
            _isClimbingUpward = false;
            //_isClimbingDownward = true;
            _isClimbed = true;
            _isNewLevel = true;
        }

        if(other.gameObject.tag == "LastBrick")
        {
            _isLastBrick = true;
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
