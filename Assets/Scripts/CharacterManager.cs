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
    [SerializeField] private SpawnItems _range;
    [SerializeField] private CameraController _camera;

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
    public static bool _isCurrentLevel = true; 
    private Vector3 _characterPos;
    private Vector3 _level;
    public bool _isFinished = false;
    private bool _isDancing = false;



    public List<GameObject> _brickList;
    private int _maxBricks = 10;
    private GameObject _myBrick;
    [SerializeField] private AudioSource _pickUpAudio;
    private bool _pickedUp = false;
    [SerializeField] private GameObject _characterBack;
    [SerializeField] private GameObject _characterHand;
    [SerializeField] private GameObject _cup;

    private bool _isStarted = true;

    private void Start()
    {
        _isClimbingUpward = false;
        _isClimbed = false;
        _isNewLevel = false;
        _brickList = new List<GameObject>();
        _level = new Vector3(-16, 411, 1263);
    }
    // Update is called once per frame
    void Update()
    {
        if (_isStarted)
        {
            _range.SetParent(transform.tag);
            _isStarted = false;
        }

        if (_pickedUp)
        {
            PickUp();
            _pickedUp = false;
        }

        else if (fixedJoystick.Vertical != 0 || fixedJoystick.Horizontal != 0)
        {
            //Debug.Log("1111");
            _isClimbed = false;
            RunAnimation();
        }
        else if (_isClimbingUpward && !_isClimbed)
        {
            //Debug.Log("2222");
            _range.ClearParent(transform.tag);
            ClimbAnimation();
            transform.DOMoveY(7f, 0.05f).SetRelative();
        }
        else if (_isClimbed)
        {
            //Debug.Log("3333");
            //_climbAudio.Stop();
            _range.SetParent(transform.tag);
            IdleAnimation();
            _characterPos = _level + new Vector3(100f, -404f, -1600f);
            transform.position = _characterPos;
            if (_isDancing && GameObject.FindGameObjectWithTag("BlueParent").transform.childCount == 0)
            {
                _camera.EnableFinishCamera();
                StartCoroutine(FixPosition());
                StartCoroutine(Dance());
            }
            else
            {
                transform.DOMove(_characterPos, 0.015f);
                _isDancing = false;
            }
           
            _isCurrentLevel = true;

        }
        else
            IdleAnimation();


        transform.DOMove((Vector3.forward * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal), 0.015f).SetRelative();
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation((Vector3.forward * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal)), Time.deltaTime * _rotateSpeed);

    }
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "LadderStart")
        {
            _isClimbed = false;
           // _climbAudio.Play();
           if (_isCurrentLevel)
            {
                _newLevel.GenerateLevel();
                AIManager[] AIs = FindObjectsOfType<AIManager>();
                foreach (AIManager AI in AIs)
                    AI._isCurrentLevel = false;
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
                _ladder.ClearBricks();
                _ladder.ChangeLadderPos();
                _level = _level + new Vector3(0, 480, 793);
            if (_isFinished)
            {
                AIManager._isGameOver = true;
                GameObject.Find("Canvas").transform.Find("Fixed Joystick").gameObject.SetActive(false);
                GameObject.Find("Canvas").transform.Find("GameOverPanel").GetComponent<GameOver>().ShowPanel();
                _isDancing = true;
            }

        }
        else if (other.gameObject.tag == "Brick")
        {
            _pickUpAudio.Play();
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
                _myBrick.transform.position = _brickList[_brickList.Count - 1].transform.position + new Vector3(0f, 10f, 0f);
            StartCoroutine(TrailRendererProcess(_myBrick));
            _myBrick.transform.localRotation = Quaternion.identity;
            _brickList.Add(_myBrick);
            _myBrick.gameObject.tag = "Untagged";
        }

    }

    IEnumerator TrailRendererProcess(GameObject go)
    {
        go.GetComponent<TrailRenderer>().emitting = true;
        yield return new WaitForSeconds(0.7f);
        go.GetComponent<TrailRenderer>().emitting = false;
    }

    IEnumerator FixPosition()
    {
        transform.DOMoveY(_characterPos.y, 0.01f);
        yield return new WaitForSeconds(0.01f);
        
    }
    
    IEnumerator Dance()
    {
        _cup = GameObject.FindGameObjectWithTag("Cup");
        Vector3 _cupPos = _cup.transform.position;
        _cupPos.y = _characterPos.y;
        _cupPos.z -= 20f;
        //transform.DOMoveY(_characterPos.y, 0.01f);
        //yield return new WaitForSeconds(0.01f);
        
        RunAnimation();
        transform.DOMove(_cupPos, 0.9f);
        yield return new WaitForSeconds(0.9f);
        //_cup.transform.position = _characterHand.transform.position;
        DanceAnimation();

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
}
