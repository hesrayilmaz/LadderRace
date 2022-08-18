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
    [SerializeField] private CharacterManager _characterManager;
    [SerializeField] private AudioSource _pickUpAudio;
    [SerializeField] private AudioSource _climbAudio;

    private GameObject _myBrick;
    private GameObject _ladderStep;
    private List<GameObject> _brickList;
    private List<GameObject> _bricksOnLadder;
    private Vector3 _startPos, _endPos;
    private Vector3 _firstStepPos, _ladderPos;
    private int _maxBricks = 10, _necessaryBricks=30;


    // Start is called before the first frame update
    void Start()
    {
        //_brickList = new List<GameObject>(); 
        /*if (GetComponent<AIManager>() != null)
            _brickList = GetComponent<AIManager>()._brickList;
        else
            _brickList = GetComponent<CharacterManager>()._brickList;
        */
        _bricksOnLadder = new List<GameObject>();
        /*_firstStepPos = new Vector3(-7, -175, 265);
        _ladderPos = _firstStepPos;
    */
        //_firstStepPos = _ladder.transform.position;
        //_ladderPos = _ladder.transform.position + new Vector3(-110,-350,100);
        _ladderPos = _ladder.transform.position + new Vector3(-5, -170, 265);
        _firstStepPos = _ladderPos;
    }


    // Update is called once per frame
    void Update()
    {
        /*if (this.gameObject.tag != "Stickman" && _brickList.Count == _maxBricks)
        {
            _AIManager.GoToLadder(_firstStepPos + new Vector3(-61, 180, 127));
        }*/

        /*if ((this.gameObject.tag == "Stickman" && _characterManager._isNewLevel) ||
            (this.gameObject.tag == "AI" && _AIManager._isNewLevel))
            _bricksOnLadder.Clear();*/
    }

    private void OnTriggerEnter(Collider other)
    {
       /* if ((this.gameObject.tag == "AI" && other.gameObject.tag == "LadderEnd"))
        {
            _firstStepPos = _ladderPos + new Vector3(0, 0, 793);
            _ladderPos = _firstStepPos;
        }*/
    }


    public void Drop()
    {
        //if (_characterManager._isNewLevel)
          //  _ladderPos = _ladderPos + new Vector3(0, 8, 793);
        _climbAudio.Play();
        StartCoroutine(DropProcess());
    }

    public void ClearBricks()
    {
        _bricksOnLadder.Clear();
    }

    public void ChangeLadderPos()
    {
        _ladderPos = _ladderPos + new Vector3(0, 8, 793);
        _firstStepPos = _ladderPos;
    }
    public void ChangeLadderPosAI()
    {
        _ladderPos = _ladderPos + new Vector3(0, 0, 793);
        _firstStepPos = _ladderPos;
       // _ladderPos = _firstStepPos;
    }

    public Vector3 GetLadderPosAI()
    {
        //return _firstStepPos;
        return _firstStepPos;
    }
    public int GetBrickCount()
    {
        return _bricksOnLadder.Count;
    }
    public GameObject GetLastBrick()
    {
        return _bricksOnLadder[_bricksOnLadder.Count-1];
    }

    IEnumerator DropProcess()
    {
        Debug.Log("bricklist count: "+ _AIManager._brickList.Count);
        while (_AIManager._brickList.Count != 0 && _bricksOnLadder.Count < _necessaryBricks)
        {
            _ladderStep = Instantiate(_actualLadderStep);
            _ladderStep.transform.localScale = new Vector3(170, 150, 200);
            _bricksOnLadder.Add(_ladderStep);
            _ladderStep.transform.parent = _ladder.transform;
            _ladderStep.transform.localRotation = Quaternion.identity;
            _ladderPos = _ladderPos + new Vector3(0, 16, 0);
            _ladderStep.transform.position = _ladderPos;
            _myBrick = _AIManager._brickList[_AIManager._brickList.Count - 1];
            Destroy(_myBrick);
            _AIManager._brickList.Remove(_myBrick);
            yield return new WaitForSeconds(0.1f);
        }
        
        if (this.gameObject.tag == "Stickman" && _bricksOnLadder.Count == _necessaryBricks)
            _characterManager._isClimbingUpward = true;
        else if((this.gameObject.tag == "Red" || this.gameObject.tag == "Green" || this.gameObject.tag == "Orange")
                 && _bricksOnLadder.Count == _necessaryBricks)
            _AIManager._isClimbingUpward = true;
        _climbAudio.Stop();
    }
}
