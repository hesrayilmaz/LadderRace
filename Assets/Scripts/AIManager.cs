using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;

public class AIManager : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private NavMeshSurface _surface;

    [SerializeField] private Transform _player;
    private Vector3 walkPoint;
    private bool _isWalkPointSet;
    private float _walkPointRange;
    private Vector3 _level;
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
    public static bool _isCurrentLevel = true;
    private bool _isMoving = true;
    private bool _isLadder = false;
    private Vector3 _characterPos, target;
    private int _levelIndex = 0;
    private Vector3 _ladderEndPos;

    private void Start()
    {
        _level = new Vector3(-16, 411, 1263);
    }
    // Update is called once per frame
    void Update()
    {
        //if(Vector3.Distance(transform.position,_player.position) < _distToPlayer)
          //  Move();

       // if(_isMoving)
       if(!_goToLadder && !_isClimbingUpward && !_isClimbed)
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
        }*/
        else if (_isClimbingUpward && !_isClimbed)
        {
            _isMoving = false;
            gameObject.GetComponent<NavMeshAgent>().enabled = false;
            ClimbAnimation();
            transform.DOMoveY(7f, 0.2f).SetRelative();
            //_ladderEndPos = transform.position + new Vector3(0, 460, 0);
            //_agent.SetDestination(_ladderEndPos);
        }
        /*
         else if (_isClimbed)
         {
             //Debug.Log("3333");
             _climbAudio.Stop();
             IdleAnimation();
             _isCurrentLevel = true;
         }
         else
             IdleAnimation();
        */
        if (_isClimbed && _levelController.GetCurrentLevel() != null)
        {
            StartCoroutine(FixPosition());
            _isClimbed = false;
            //IdleAnimation();
           // _characterPos = transform.position + new Vector3(0f, 50f, 70f);
            //transform.position = _characterPos;
            //transform.DOMove(_characterPos, 0.015f);


            //_isWalkPointSet = false;
            // gameObject.GetComponent<NavMeshAgent>().enabled = true;


            // _agent.SetDestination(_characterPos);
            // transform.DOMove(_characterPos, 0.015f);
        }

        //else if (!_isClimbed)
          //  _isMoving = true;       

    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "LadderStart")
        {
            //_isClimbingUpward = true;
            
            
            _isClimbed = false;
            _isLadder = true;
            if (_isCurrentLevel)
            {
                _levelController.GenerateLevel();
                _surface.BuildNavMesh();
                _isCurrentLevel = false;
                CharacterManager._isCurrentLevel = false;
            }
            StartCoroutine(DropProcess());
            _isNewLevel = false;

            
            //_isMoving = true;
        }
        else if (other.gameObject.tag == "LadderEnd")
        {
            _isClimbingUpward = false;
            _isClimbed = true;
            _isNewLevel = true;
            _isWalkPointSet = false;


        }
    }

    public void Move()
    {
        if (!_isWalkPointSet) SetWalkPoint();
        else _agent.SetDestination(walkPoint);

        _isClimbed = false;
        RunAnimation();
        Vector3 _distToWalkPoint = transform.position - walkPoint;
       // Debug.Log(_distToWalkPoint.magnitude);
        if (_distToWalkPoint.magnitude < 10f)
            _isWalkPointSet = false;
    }

    public void SetWalkPoint()
    {
        
        //if(_level == null)
        if(_isNewLevel)
        {
            Debug.Log("?????????????????????????");
            gameObject.GetComponent<NavMeshAgent>().enabled = true;
            _level = _level + new Vector3(0, 480, 793);
            //RunAnimation();
            _isNewLevel = false;
            //_levelIndex++;

        }
        
        Debug.Log(_level);
        float _yAxis = _level.y - 400;
        Vector3 Min = new Vector3(_level.x - 350, _yAxis, _level.z - 1500);
        Vector3 Max = new Vector3(_level.x + 350, _yAxis, _level.z - 1000);
        float _xAxis = Random.Range(Min.x, Max.x);
        float _zAxis = Random.Range(Min.z, Max.z);
        walkPoint = new Vector3(_xAxis, _yAxis, _zAxis);
        //Debug.Log(walkPoint);
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
        //target = endPoint;
        Debug.Log("ladder");
        _agent.SetDestination(endPoint);

        //Vector3 _distToWalkPoint = transform.position - walkPoint;
        // Debug.Log(_distToWalkPoint.magnitude);
        //if (_distToWalkPoint.magnitude < 10f)
          //  _isWalkPointSet = false;
    }

    IEnumerator DropProcess()
    {
        IdleAnimation();
        _ladder.Drop();
        yield return new WaitForSeconds(1f);
        _goToLadder = false;
        RunAnimation();
    }

    IEnumerator FixPosition()
    {
        _characterPos = transform.position + new Vector3(0f, 20f, 70f);
        //_characterPos = _levelController.GetCurrentLevel().transform.position + new Vector3(80f, -404f, -1600f);
        transform.position = _characterPos;
        transform.DOMove(_characterPos, 0.015f);
        yield return new WaitForSeconds(0.5f);
        //gameObject.GetComponent<NavMeshAgent>().enabled = true;
       // Move();
        //IdleAnimation();
    }
   
}
