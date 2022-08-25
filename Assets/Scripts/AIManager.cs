using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;

public class AIManager : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;

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


    [SerializeField] private string _idleAnimName = "Idle";
    [SerializeField] private float _idleAnimSpeed = 1f;
    [SerializeField] private string _runAnimName = "Running";
    [SerializeField] private float _runAnimSpeed = 2f;
    [SerializeField] private string _climbAnimName = "Climb";
    [SerializeField] private float _climbAnimSpeed = 3f;
    [SerializeField] private string _danceAnimName = "Samba Dancing";
    [SerializeField] private float _danceAnimSpeed = 3f;
    [SerializeField] private float _rotateSpeed = 10f;

    public bool _isClimbingUpward { get; set; }
    public bool _isClimbed { get; set; }
    public bool _isNewLevel { get; set; }
    public bool _goToLadder { get; set; }
    public bool _isCurrentLevel = true;
    private Vector3 _characterPos;
    private Vector3 _ladderEndPos;
    public int radius = 3;
    private bool _isStarted = true;
    public static bool _isFinished = false;
    private bool _isDancing = false;



    public List<GameObject> _brickList { get; set; }
    private int _maxBricks = 10;
    private GameObject _myBrick;
    [SerializeField] private AudioSource _pickUpAudio;
    private bool _pickedUp=false;
    [SerializeField] private GameObject _characterBack;
    [SerializeField] private GameObject _cup;


    private void Start()
    {
        _isClimbingUpward = false;
        _isClimbed = false;
        _isNewLevel = false;
        _goToLadder = false;
        _brickList = new List<GameObject>();
        _level = new Vector3(-16, 411, 1263);
        
    }

   
    // Update is called once per frame
    void Update()
    {
       
        if (_pickedUp)
        {
            //Debug.Log("11111111111"); 
            PickUp();
            _pickedUp = false;
        }
        else if(!_goToLadder && !_isClimbingUpward && !_isClimbed)
        {
            //Debug.Log("22222222222");
            Move();
        }
        else if (_isClimbingUpward && !_isClimbed)
        {
            //Debug.Log("3333");
            _range.ClearParent(transform.tag);
            gameObject.GetComponent<NavMeshAgent>().enabled = false;
            ClimbAnimation();
            transform.DOMoveY(7f, 0.2f).SetRelative();
        }
        /*
         else if (_isClimbed)
         {
             //Debug.Log("3333");
             _climbAudio.Stop();
             IdleAnimation();
             _isCurrentLevel = true;
         }
        */
        if (_isClimbed)
        {
            //StartCoroutine(FixPosition());
            //Debug.Log("444444");
            _range.SetParent(transform.tag);
            transform.position += new Vector3(0f, 15f, 70f);
            gameObject.GetComponent<NavMeshAgent>().enabled = true;
            _isCurrentLevel = true;
            if(!_isDancing)
                _isClimbed = false;
            else
            {
                StartCoroutine(Dance());
                
            }
                
        }

    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "LadderStart")
        {
            Debug.Log("LADDER START");
            _isClimbed = false;
            if (_isCurrentLevel)
            {
                _levelController.GenerateLevel();
                _isCurrentLevel = false;
                CharacterManager._isCurrentLevel = false;
                AIManager[] AIs = FindObjectsOfType<AIManager>();
                foreach(AIManager AI in AIs)
                    AI._isCurrentLevel = false;
            }
            StartCoroutine(DropProcess());
           
            _isNewLevel = false;
            _isWalkPointSet = false;

        }
        else if (other.gameObject.tag == "LadderEnd")
        {
           
                _isClimbingUpward = false;
                _isClimbed = true;
                _isNewLevel = true;
                _ladder.ClearBricks();
                _ladder.ChangeLadderPos();
                _isWalkPointSet = false;
            if (_isFinished)
            {
                _isDancing = true;
            }

        }
        else if(other.gameObject.tag != transform.tag &&
                  other.gameObject.tag.StartsWith(transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material.name.Substring(0, 1)))
        {
           // _pickUpAudio.Play();
            _myBrick = other.gameObject;
            _pickedUp = true;
        }

    }

    public void SetFirstFloor()
    {
        _range.SetParent(transform.tag);
    }

    public void PickUp()
    {
        if (_brickList.Count <= _maxBricks)
        {
            _myBrick.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            _myBrick.transform.parent = _characterBack.transform;

            if (_brickList.Count == 0)
                 _myBrick.transform.position = _characterBack.transform.position;
            else
            {
                _myBrick.transform.position = _brickList[_brickList.Count - 1].transform.position + new Vector3(0f, 10f, 0f);

            }
            StartCoroutine(TrailRendererProcess(_myBrick));
            _myBrick.transform.localRotation = Quaternion.identity;
            _brickList.Add(_myBrick);
            _myBrick.gameObject.tag = "Untagged";
        }

        if (_brickList.Count == _maxBricks)
        {
            GoToLadder(_ladder.GetLadderPosAI());
            _goToLadder = true;
        }
    }
 
    public void Move()
    {
        if (!_isWalkPointSet)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
            List<Vector3> targetColor = new List<Vector3>();
            for(int i = 0; i < hitColliders.Length; i++)
            { 
                if ((hitColliders[i].tag!=transform.tag) &&
                    hitColliders[i].tag.StartsWith(transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material.name.Substring(0, 1)))
                        targetColor.Add(hitColliders[i].transform.position);
            }
            
            if (targetColor.Count > 0)
            {
                //Debug.Log(targetColor.Count);
                walkPoint = targetColor[targetColor.Count-1];
            }
            else
            {
                int bricksOnGround = GameObject.FindGameObjectWithTag(transform.tag+"Parent").transform.childCount;
                int random = Random.Range(0, bricksOnGround);
                walkPoint = GameObject.FindGameObjectWithTag(transform.tag + "Parent").transform.GetChild(random).position;
            }
            
            Debug.Log("walk point: " + walkPoint);
            Debug.Log("transform: " + transform.position);
            Debug.Log("distttttttttttttt: "+ (walkPoint.y - transform.position.y));
            
            if (walkPoint.y - transform.position.y > 10 || walkPoint.y - transform.position.y<0 ||
                (walkPoint.z == transform.position.z && walkPoint.z == transform.position.z))
                return;
            
            _agent.SetDestination(walkPoint);
            RunAnimation();
            _isWalkPointSet = true;
        }
       
        _isClimbed = false;
        Vector3 _distToWalkPoint = transform.position - walkPoint;
        //Debug.Log("dist: "+_distToWalkPoint.magnitude);
        if (_distToWalkPoint.magnitude < 10f)
            _isWalkPointSet = false;
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
    public void DanceAnimation()
    {
        PlayAnimation(_danceAnimName, _danceAnimSpeed);
    }
    public void PlayAnimation(string animName, float animSpeed)
    {
        _animancer.PlayAnimation(animName);
        _animancer.SetStateSpeed(animSpeed);
    }

    public void GoToLadder(Vector3 endPoint)
    {
        Debug.Log("ladder poiint: "+endPoint);
        //transform.LookAt(endPoint);
        _agent.SetDestination(endPoint);
    }

    IEnumerator DropProcess()
    {
        IdleAnimation();
        _ladder.Drop();
        yield return new WaitForSeconds(1.5f);
        _goToLadder = false;
        RunAnimation();
    }

    IEnumerator TrailRendererProcess(GameObject go)
    {
        go.GetComponent<TrailRenderer>().emitting = true;
        //go.transform.position = Vector3.Lerp(_startPos, _endPos, 1f);
        yield return new WaitForSeconds(0.7f);
        go.GetComponent<TrailRenderer>().emitting = false;
    }

    IEnumerator Dance()
    {
        _cup = GameObject.FindGameObjectWithTag("Cup");
        Vector3 _cupPos = _cup.transform.position-new Vector3(0,52f,50f);
        
        Debug.Log("cup pos: " + _cupPos);
        RunAnimation();
        //_agent.SetDestination(_cupPos);
        yield return new WaitForSeconds(0.5f);
        DanceAnimation();
        
    }

    /* IEnumerator FixPosition()
     {
         _characterPos = transform.position + new Vector3(0f, 15f, 70f);
         //_characterPos = _levelController.GetCurrentLevel().transform.position + new Vector3(80f, -404f, -1600f);
         transform.position = _characterPos;
         transform.DOMove(_characterPos, 0.015f);
         yield return new WaitForSeconds(0.5f);

         //gameObject.GetComponent<NavMeshAgent>().enabled = true;
         // Move();
         //IdleAnimation();
     }*/

}
