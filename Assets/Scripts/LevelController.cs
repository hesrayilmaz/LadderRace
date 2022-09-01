using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private GameObject _floorPrefab, _firstFloor, _finishFloor;
    [SerializeField] private GameObject[] _levels;
    [SerializeField] private SpawnItems _items;
    [SerializeField] private AIManager _RedManager, _GreenManager, _OrangeManager;
    [SerializeField] private CharacterManager _CharacterManager;
    [SerializeField] private int _numOfFloor;
    private int _numOfBrick;
    private GameObject _currentLevel;
    private int _currentNum, _minNum=35, _maxNum=45;
    private float _yDiff = 480f;
    private float _zDiff = 793f;

    private bool _isStarted = true;

    // Start is called before the first frame update
    void Start()
    {
        _currentNum = 0;
        _floorPrefab.transform.position = new Vector3(-16f, 411f, 1263f);
        _currentLevel = _firstFloor;
        _levels = new GameObject[_numOfFloor+1];
        _levels[_currentNum] = _currentLevel;
    }

    private void Update()
    {
        if (_isStarted)
        {
            _numOfBrick = Random.Range(_minNum, _maxNum);
            for (int i = 0; i < _numOfBrick; i++)
                _items.Init(_currentLevel);
            
            GenerateLevel();
            _isStarted = false;
        }
    }
    // Update is called once per frame
    public void GenerateLevel()
    {
        
        while (_currentNum < _numOfFloor)
        {
            _currentNum += 1;
            _floorPrefab.transform.position = _floorPrefab.transform.position + new Vector3(0f, _yDiff, _zDiff);
            _currentLevel = Instantiate(_floorPrefab);
            _levels[_currentNum] = _currentLevel;
            _numOfBrick = Random.Range(_minNum, _maxNum);
            for(int i = 0; i < _numOfBrick; i++)
                _items.Init(_currentLevel);
            
        }
        if(_currentNum == _numOfFloor)
        {
            _finishFloor.transform.position = _floorPrefab.transform.position + new Vector3(16f, 70, -620);
            Instantiate(_finishFloor);
        }
    }

    public GameObject GetLevel(int index)
    {
        if (index < _levels.Length)
            return _levels[index];
        else
            return null;
    }

    public GameObject GetFinishLevel()
    {
        return _finishFloor;
    }
}
