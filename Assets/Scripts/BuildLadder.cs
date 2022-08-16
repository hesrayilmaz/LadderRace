using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildLadder : MonoBehaviour
{
    [SerializeField] private LevelController _levelController;
    [SerializeField] private GameObject _character;
    [SerializeField] private GameObject _actualBrick;
    [SerializeField] private GameObject _ladder;
    [SerializeField] private GameObject _actualLadderStep;
    [SerializeField] private AIManager _AIManager;
    private GameObject _myBrick;
    private GameObject _ladderStep;
    [SerializeField] private AudioSource _pickUpAudio;
    [SerializeField] private AudioSource _climbAudio;
    private List<GameObject> _brickList;
    private List<GameObject> _bricksOnLadder;
    private bool _pickedUp;
    private Vector3 _startPos, _endPos;
    private Vector3 _firstStepPos, _ladderPos;
    private int _maxBricks = 10, _necessaryBricks=30;


    // Start is called before the first frame update
    void Start()
    {
        _brickList = new List<GameObject>(); 
        _bricksOnLadder = new List<GameObject>();
        _pickedUp = false;
        _firstStepPos = new Vector3(-7, -175, 265);
        _ladderPos = _firstStepPos;
    }


    // Update is called once per frame
    void Update()
    {
        if (_pickedUp)
        {
            //PickUp();
            _pickedUp = false;
        }

        if ((this.gameObject.tag == "Stickman" && CharacterManager._isNewLevel) ||
            (this.gameObject.tag == "AI" && AIManager._isNewLevel))
            _bricksOnLadder.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((this.gameObject.tag=="Stickman" && other.gameObject.tag == "Brick")||
            (this.gameObject.tag == "AI" && other.gameObject.tag == "RedBrick"))
        {
            _pickUpAudio.Play();
            _myBrick = other.gameObject;
            _pickedUp = true;
        }

        if ((this.gameObject.tag == "AI" && other.gameObject.tag == "LadderEnd"))
        {
            _firstStepPos = _ladderPos + new Vector3(0, 0, 793);
            _ladderPos = _firstStepPos;
        }
            
    }


   /* public void PickUp()
    {
        if (_brickList.Count <= _maxBricks)
        {
            _myBrick.gameObject.GetComponent<Rigidbody>().isKinematic = true;

            _myBrick.transform.parent = _character.transform;

            if (_brickList.Count == 0)
                _myBrick.transform.position = _character.transform.position;
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
        }

        if(this.gameObject.tag=="AI" && _brickList.Count == _maxBricks)
        {
            _AIManager.GoToLadder(_firstStepPos+new Vector3(-61,180,127));
            AIManager._goToLadder = true;
        }

        
    }*/

    public void Drop()
    {
        if (CharacterManager._isNewLevel)
            _ladderPos = _ladderPos + new Vector3(0, 8, 793);
        _climbAudio.Play();
        StartCoroutine(DropProcess());
    }

    public int GetBrickCount()
    {
        return _bricksOnLadder.Count;
    }
    public GameObject GetLastBrick()
    {
        return _bricksOnLadder[_bricksOnLadder.Count-1];
    }

    IEnumerator TrailRendererProcess(GameObject go)
    {
        go.GetComponent<TrailRenderer>().emitting = true;
        //go.transform.position = Vector3.Lerp(_startPos, _endPos, 1f);
        yield return new WaitForSeconds(0.7f);
        go.GetComponent<TrailRenderer>().emitting = false;
    }

    IEnumerator DropProcess()
    {
        while (_brickList.Count != 0 && _bricksOnLadder.Count < _necessaryBricks)
        {
            _ladderStep = Instantiate(_actualLadderStep);
            _ladderStep.transform.localScale = new Vector3(170, 150, 200);
            _bricksOnLadder.Add(_ladderStep);
            _ladderStep.transform.parent = _ladder.transform;
            _ladderStep.transform.localRotation = Quaternion.identity;
            _ladderPos = _ladderPos + new Vector3(0, 16, 0);
            _ladderStep.transform.localPosition = _ladderPos;
            _myBrick = _brickList[_brickList.Count - 1];
            Destroy(_myBrick);
            _brickList.Remove(_myBrick);
            yield return new WaitForSeconds(0.1f);
        }
        
        if (this.gameObject.tag == "Stickman" && _bricksOnLadder.Count == _necessaryBricks)
            CharacterManager._isClimbingUpward = true;
        else if(this.gameObject.tag == "AI" && _bricksOnLadder.Count == _necessaryBricks)
            AIManager._isClimbingUpward = true;
        _climbAudio.Stop();
    }
}
