using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;

public class AIManager : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Transform _player;
    private Vector3 walkPoint;
    private bool _isWalkPointSet;
    private float _walkPointRange;
    private GameObject _level;
    public float _distToPlayer = 1f;

    [SerializeField] private SimpleAnimancer _animancer;
    [SerializeField] private AudioSource _climbAudio;
    [SerializeField] private LevelController _levelController;
    [SerializeField] private BuildLadder _ladder;
    [SerializeField] private SpawnItems _range;

    [SerializeField] private FixedJoystick fixedJoystick;

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
    public static bool _goToLadder = false;
    private bool _isCurrentLevel = true;
    private bool _isMoving = true;
    private bool _isLadder = false;
    private Vector3 _characterPos, target;

    private void Start()
    {
        _level = _levelController.GetCurrentLevel();
        //RunAnimation();
    }
    // Update is called once per frame
    void Update()
    {
        //if(Vector3.Distance(transform.position,_player.position) < _distToPlayer)
          //  Move();

        //if(_isMoving)
          Move();
       /* if (_goToLadder)
        {
            transform.DOMove(target, 70f).SetSpeedBased().SetEase(Ease.Linear);
        }
        /*else if (fixedJoystick.Vertical != 0 || fixedJoystick.Horizontal != 0)
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

        if (_isClimbed && _levelController.GetCurrentLevel() != null)
        {
            _characterPos = _levelController.GetCurrentLevel().transform.position + new Vector3(80f, -404f, -1600f);
            transform.position = _characterPos;
            transform.DOMove(_characterPos, 0.015f);
        }
        */
        //transform.DOMove((Vector3.forward * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal), 0.015f).SetRelative();
        //Move();
        
        //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation((Vector3.forward * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal)), Time.deltaTime * _rotateSpeed);

    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "LadderStart")
        {
            //_isClimbingUpward = true;
            
            _goToLadder = false;
            _isClimbed = false;
            _isLadder = true;
            _ladder.Drop();
            _isNewLevel = false;
            //_isMoving = true;
        }
        else if (other.gameObject.tag == "LadderEnd")
        {
            _isClimbingUpward = false;
            _isClimbed = true;
            _isNewLevel = true;

        }
    }

    public void Move()
    {
        if (!_isWalkPointSet) SetWalkPoint();
        else _agent.SetDestination(walkPoint);
       
        Vector3 _distToWalkPoint = transform.position - walkPoint;
        Debug.Log(_distToWalkPoint.magnitude);
        if (_distToWalkPoint.magnitude < 10f)
            _isWalkPointSet = false;
    }

    public void SetWalkPoint()
    {
        _level = _levelController.GetCurrentLevel();
        float _yAxis = _level.transform.position.y - 400;
        Vector3 Min = new Vector3(_level.transform.position.x - 700, _yAxis, _level.transform.position.z - 1500);
        Vector3 Max = new Vector3(_level.transform.position.x + 400, _yAxis, _level.transform.position.z - 900);
        float _xAxis = Random.Range(Min.x, Max.x);
        float _zAxis = Random.Range(Min.z, Max.z);
        walkPoint = new Vector3(_xAxis, _yAxis, _zAxis);
        _isWalkPointSet = true;
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

    public void GoToLadder(Vector3 endPoint)
    {
        target = endPoint;
    }

   
}
