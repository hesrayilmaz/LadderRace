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
        _bricksOnLadder = new List<GameObject>();
        _ladderPos = _ladder.transform.position + new Vector3(-5, -170, 265);
        _firstStepPos = _ladderPos;
    }

    public void Drop()
    {
        _climbAudio.Play();
        StartCoroutine(DropProcess());
    }

    public void ClearBricks()
    {
        _bricksOnLadder.Clear();
    }

    public void ChangeLadderPos()
    {
        _ladderPos = _ladderPos + new Vector3(0, 0, 793);
        _firstStepPos = _ladderPos;
    }

    public Vector3 GetLadderPosAI()
    {
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
        if(!gameObject.CompareTag("Stickman"))
        {
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

        }
        else
        {
            while (_characterManager._brickList.Count != 0 && _bricksOnLadder.Count < _necessaryBricks)
            {
                _ladderStep = Instantiate(_actualLadderStep);
                _ladderStep.transform.localScale = new Vector3(170, 150, 200);
                _bricksOnLadder.Add(_ladderStep);
                _ladderStep.transform.parent = _ladder.transform;
                _ladderStep.transform.localRotation = Quaternion.identity;
                _ladderPos = _ladderPos + new Vector3(0, 16, 0);
                _ladderStep.transform.position = _ladderPos;
                _myBrick = _characterManager._brickList[_characterManager._brickList.Count - 1];
                Destroy(_myBrick);
                _characterManager._brickList.Remove(_myBrick);
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        if (this.gameObject.tag == "Stickman" && _bricksOnLadder.Count == _necessaryBricks)
            _characterManager._isClimbingUpward = true;
        else if(this.gameObject.tag != "Stickman" && _bricksOnLadder.Count == _necessaryBricks)
            _AIManager._isClimbingUpward = true;

        _climbAudio.Stop();
    }
}
