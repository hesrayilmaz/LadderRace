using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private GameObject _floor, _finishFloor;
    [SerializeField] private SpawnItems _items;
    [SerializeField] private AIManager _RedManager, _GreenManager, _OrangeManager;
    [SerializeField] private CharacterManager _CharacterManager;
    [SerializeField] private int _numOfFloor;
    private int _numOfBrick;
    private GameObject _currentLevel;
    private int _currentNum = 0, _minNum=35, _maxNum=45;
    private float _yDiff = 480f;
    private float _zDiff = 793f;

    private bool _isStarted = true;

    // Start is called before the first frame update
    void Start()
    {
        _floor.transform.position = new Vector3(-16f, 411f, 1263f);
        _currentLevel = _floor;
    }

    private void Update()
    {
        if (_isStarted)
        {
            _numOfBrick = Random.Range(_minNum, _maxNum);
            for (int i = 0; i < _numOfBrick; i++)
            {
                _items.Init(_currentLevel);
            }
            _RedManager.SetFirstFloor();
            _GreenManager.SetFirstFloor();
            _OrangeManager.SetFirstFloor();
            _CharacterManager.SetFirstFloor();
            _isStarted = false;
        }
    }
    // Update is called once per frame
    public void GenerateLevel()
    {
        _currentNum += 1;
        if (_currentNum < _numOfFloor)
        {
            _floor.transform.position = _floor.transform.position + new Vector3(0f, _yDiff, _zDiff);
            _currentLevel = Instantiate(_floor);
            _numOfBrick = Random.Range(_minNum, _maxNum);
            for(int i = 0; i < _numOfBrick; i++)
            {
                _items.Init(_currentLevel);
            }
        }
        else
        {
            _finishFloor.transform.position = _floor.transform.position + new Vector3(16f, 70, -620);
            Instantiate(_finishFloor);
            _CharacterManager._isFinished = true;
            AIManager._isFinished = true;
        }
        
    }

    public GameObject GetCurrentLevel()
    {
        return _currentLevel;
    }
   
}
