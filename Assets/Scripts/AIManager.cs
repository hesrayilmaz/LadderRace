using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;

public class AIManager : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;

    private Vector3 walkPoint;
    public static bool _isWalkPointSet;
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
    public int radius = 3;





    private List<GameObject> _brickList;
    private int _maxBricks = 10;
    private GameObject _myBrick;
    [SerializeField] private AudioSource _pickUpAudio;
    private bool _pickedUp=false;
    [SerializeField] private GameObject _characterBack;


    private void Start()
    {
        _brickList = new List<GameObject>();
        _level = new Vector3(-16, 411, 1263);
    }
    // Update is called once per frame
    void Update()
    {

        if (_pickedUp)
        {
            PickUp();
            _pickedUp = false;
        }
            
       // if(_isMoving)
        else if(!_goToLadder && !_isClimbingUpward && !_isClimbed)
            Move();
       
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
        if (_isClimbed)
        {
            //StartCoroutine(FixPosition());
            transform.position += new Vector3(0f, 15f, 70f);
            gameObject.GetComponent<NavMeshAgent>().enabled = true;
            _isCurrentLevel = true;
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
                //_surface.BuildNavMesh();
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
        else if(!(other.gameObject.tag == transform.tag) &&
                  other.gameObject.tag.StartsWith(transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material.name.Substring(0, 1)))
        {
            Debug.Log("çarpýþtý");
            Debug.Log(other.gameObject.tag);
            _pickUpAudio.Play();
            _myBrick = other.gameObject;
            _pickedUp = true;
            //PickUp();
        }

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
                //_startPos = _myBrick.transform.position;
                //_endPos = _brickList[_brickList.Count - 1].transform.position + new Vector3(0f, 1.1f, 0f);
                _myBrick.transform.position = _brickList[_brickList.Count - 1].transform.position + new Vector3(0f, 10f, 0f);
                StartCoroutine(TrailRendererProcess(_myBrick));
            }
            _myBrick.transform.localRotation = Quaternion.identity;
            //Vector3.Lerp(_brickList[_brickList.Count - 1].transform.position, _brickList[_brickList.Count - 1].transform.position+new Vector3(0f,1.1f,0f),Time.deltaTime*100f);
            _brickList.Add(_myBrick);
            _isWalkPointSet = false;
        }

        /*if (_brickList.Count == _maxBricks)
        {
            GoToLadder(_firstStepPos + new Vector3(-61, 180, 127));
            _goToLadder = true;
        }*/


    }
    /* public void Move()
     {
         if (!_isWalkPointSet) SetWalkPoint();
         else
         {
            // Debug.Log("hedef: " + walkPoint);
             _agent.SetDestination(walkPoint);
         }


         _isClimbed = false;
         RunAnimation();
         Vector3 _distToWalkPoint = transform.position - walkPoint;
        // Debug.Log(_distToWalkPoint.magnitude);
         if (_distToWalkPoint.magnitude < 10f)
             _isWalkPointSet = false;
     }*/

    public void Move()
    {
        if (!_isWalkPointSet)
        {
            Debug.Log(transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material.name.Substring(0, 1));
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
            List<Vector3> targetColor = new List<Vector3>();
            for(int i = 0; i < hitColliders.Length; i++)
            {
                
                if (!(hitColliders[i].tag==transform.tag) &&
                    hitColliders[i].tag.StartsWith(transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material.name.Substring(0, 1)))
                {
                    Debug.Log(hitColliders[i].tag);
                    targetColor.Add(hitColliders[i].transform.position);
                }
                    
            }
            
            if (targetColor.Count > 0)
            {
                Debug.Log(targetColor.Count);
                walkPoint = targetColor[targetColor.Count-1];
                Debug.Log("walk point: " + walkPoint);
                Debug.Log("transform: " + transform.position);
            }
                
            else
            {
                //Debug.Log(GameObject.FindGameObjectWithTag(transform.tag + "Parent").ToString());
                int bricksOnGround = GameObject.FindGameObjectWithTag(transform.tag+"Parent").transform.childCount;
                    //GameObject.Find("AIBricks").transform.childCount;
                Debug.Log("child count:"+bricksOnGround);
                int random = Random.Range(0, bricksOnGround);
                walkPoint = GameObject.FindGameObjectWithTag(transform.tag + "Parent").transform.GetChild(random).position;
            }

            _agent.SetDestination(walkPoint);
            RunAnimation();
            _isWalkPointSet = true;
        }

        
        
        _isClimbed = false;
        //Vector3 _distToWalkPoint = transform.position - walkPoint;
        //Debug.Log("dist: "+_distToWalkPoint.magnitude);
        //if (_distToWalkPoint.magnitude < 10f)
          //  _isWalkPointSet = false;
    }

    public void SetWalkPoint()
    {
        
        if(_isNewLevel)
        {
            Debug.Log("?????????????????????????");
            //gameObject.GetComponent<NavMeshAgent>().enabled = true;
            _level = _level + new Vector3(0, 480, 793);
            _isNewLevel = false;
        }
        
        Debug.Log("level: "+_level);
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
        transform.LookAt(endPoint);
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
