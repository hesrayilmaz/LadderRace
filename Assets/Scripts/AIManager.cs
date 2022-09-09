using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;

public class AIManager : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private SimpleAnimancer _animancer;
    [SerializeField] private AudioSource _climbAudio;
    [SerializeField] private LevelController _levelController;
    [SerializeField] private BuildLadder _ladder;
    [SerializeField] private SpawnItems _range;
    [SerializeField] private AudioSource _pickUpAudio, _failAudio;
    [SerializeField] private GameObject _characterBack;
    [SerializeField] private GameObject _cup;
    [SerializeField] private CharacterManager _characterManager;

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
    public bool _isGameOver;
    public int radius = 5, _levelIndex;
    private bool _isDancing, _isWalkPointSet, _pickedUp;

    private Vector3 walkPoint, _ladderPos;
    public List<GameObject> _brickList { get; set; }
    private int _maxBricks = 10;
    private GameObject _myBrick;
    private CameraController _camera;
    

    private void Start()
    {
       
        _isDancing = false;
        _isWalkPointSet = false;
        _pickedUp = false;
        _isGameOver = false;
        _isClimbingUpward = false;
        _isClimbed = false;
        _isNewLevel = false;
        _goToLadder = false;
        _brickList = new List<GameObject>();
        _camera = GameObject.Find("Camera").GetComponent<CameraController>();
        _levelIndex = 0;
        gameObject.GetComponent<NavMeshAgent>().enabled = true;
    }

   
    // Update is called once per frame
    void Update()
    {
       
        if (_pickedUp)
        {
            PickUp();
            _pickedUp = false;
        }
        if (_isGameOver)
        {
            IdleAnimation();
            if (gameObject.GetComponent<NavMeshAgent>().enabled)
                _agent.SetDestination(transform.position);
            else
                transform.DOKill();
                //transform.DOMove(transform.position, 0.1f); 
        }
        else if(!_goToLadder && !_isClimbingUpward && !_isClimbed)
        {
            Move();
        }
        else if (_isClimbingUpward && !_isClimbed)
        {
            gameObject.GetComponent<NavMeshAgent>().enabled = false;
            transform.position = _ladderPos - new Vector3(0, 0, 15);
            ClimbAnimation();
            transform.DOMoveY(7f, 0.2f).SetRelative();
        }

        if (_isClimbed)
        {
            //StartCoroutine(FixPosition());
            if (gameObject.tag == "Red")
                Debug.Log("444444");
            transform.position += new Vector3(0f, 15f, 70f);
            gameObject.GetComponent<NavMeshAgent>().enabled = true;
            _isClimbed = false;
            if (_isDancing)
            {
                //_failAudio.Play();
                _camera.EnableFinishCamera();
                StartCoroutine(Dance());
            }
            else
                _isDancing = false;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "LadderStart" && other.transform.parent.name == gameObject.tag + "Ladder")
        {
            transform.DOMove(_ladderPos, 0.3f);
            _isClimbed = false;
            gameObject.GetComponent<NavMeshAgent>().enabled = false;
            StartCoroutine(DropProcess());
            _isNewLevel = false;
        }
        else if (other.gameObject.tag == "LadderEnd" && _ladder.GetBrickCount() == 30)
        {
            _levelIndex++;
            _isClimbingUpward = false;
            _isClimbed = true;
            _isNewLevel = true;
            _ladder.ClearBricks();
            _ladder.ChangeLadderPos();
            
            if (_levelController.GetLevel(_levelIndex) == null)
            {
                _isWalkPointSet = true;
                AIManager[] AIs = FindObjectsOfType<AIManager>();
                foreach (AIManager AI in AIs)
                {
                    if (AI.gameObject != gameObject)
                        AI._isGameOver = true;
                }
                _characterManager._isGameOver = true;
                GameObject.Find("Canvas").transform.Find("Fixed Joystick").gameObject.SetActive(false);
                GameObject.Find("Canvas").transform.Find("GameOverPanel").GetComponent<GameOver>().ShowFailPanel();
                _isDancing = true;
            }
            else
                _isWalkPointSet = false;
        }
        else if(other.gameObject.tag.StartsWith(transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material.name.Substring(0, 1)))
        {
            _myBrick = other.gameObject;
            _pickedUp = true;
        }

    }


    public void PickUp()
    {
        _myBrick.transform.parent = _characterBack.transform;

        if (_brickList.Count == 0)
            _myBrick.transform.position = _characterBack.transform.position;
        else
            _myBrick.transform.position = _brickList[_brickList.Count - 1].transform.position + new Vector3(0f, 10f, 0f);
        
        StartCoroutine(TrailRendererProcess(_myBrick));
        _myBrick.transform.localRotation = Quaternion.identity;
        _brickList.Add(_myBrick);
        _myBrick.gameObject.tag = "Untagged";
        _isWalkPointSet = false;
       
        if (_brickList.Count == _maxBricks)
        {
            GoToLadder(_ladder.GetLadderPosAI());
            _goToLadder = true;
        }
    }
 
    public void Move()
    {

        if (Mathf.Approximately(_agent.velocity.sqrMagnitude, 0))
            _isWalkPointSet = false;

        if (!_isWalkPointSet)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
            List<Vector3> targetColor = new List<Vector3>();
            for(int i = 0; i < hitColliders.Length; i++)
            { 
                if (hitColliders[i].tag.StartsWith(transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material.name.Substring(0, 1)))
                        targetColor.Add(hitColliders[i].transform.position);
            }

            int bricksOnGround = _levelController.GetLevel(_levelIndex).transform.Find(transform.tag + "Bricks").transform.childCount;
            int random = Random.Range(0, bricksOnGround);

            if (targetColor.Count > 0)
            {
                walkPoint = targetColor[targetColor.Count - 1];
                if (Mathf.Approximately(_agent.velocity.sqrMagnitude, 0))
                {
                    random = Random.Range(0, bricksOnGround);
                    walkPoint = _levelController.GetLevel(_levelIndex).transform.Find(transform.tag + "Bricks").transform.GetChild(random).position;
                }
            }
            else
            {
                if (_levelController.GetLevel(_levelIndex).transform.Find(transform.tag + "Bricks").transform.childCount != 0)
                {
                    random = Random.Range(0, bricksOnGround);
                    walkPoint = _levelController.GetLevel(_levelIndex).transform.Find(transform.tag + "Bricks").transform.GetChild(random).position;
                }
                else 
                    return;
                
                if (Mathf.Approximately(_agent.velocity.sqrMagnitude, 0))
                {
                    random = Random.Range(0, bricksOnGround);
                    walkPoint = _levelController.GetLevel(_levelIndex).transform.Find(transform.tag + "Bricks").transform.GetChild(random).position;
                }

            }
            _agent.SetDestination(walkPoint);
            RunAnimation();
            _isWalkPointSet = true;
        }
        _isClimbed = false;
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
        //Debug.Log("ladder poiint: "+endPoint);
        _ladderPos = endPoint;
        _isWalkPointSet = true;
        _agent.SetDestination(endPoint);
        }

    IEnumerator DropProcess()
    {
        //transform.DOMove(_ladderPos, 0.3f);
        var rotationVector = transform.rotation.eulerAngles;
        rotationVector.y = -20;
        transform.rotation = Quaternion.Euler(rotationVector);
        IdleAnimation();
        _ladder.Drop();
        yield return new WaitForSeconds(1.5f);
        
        _goToLadder = false;
        gameObject.GetComponent<NavMeshAgent>().enabled = true;
        RunAnimation();
        _isWalkPointSet = false;
    }

    IEnumerator TrailRendererProcess(GameObject go)
    {
        go.GetComponent<TrailRenderer>().emitting = true;
        yield return new WaitForSeconds(0.7f);
        go.GetComponent<TrailRenderer>().emitting = false;
    }

    IEnumerator Dance()
    {
        _cup = GameObject.FindGameObjectWithTag("Cup");
        Vector3 _cupPos = _cup.transform.position-new Vector3(0,52f,50f);
        //Debug.Log("cup pos: " + _cupPos);
        RunAnimation();
        _agent.SetDestination(_cupPos);
        DanceAnimation();
        yield return new WaitForSeconds(0.6f);
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
