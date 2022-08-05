using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private GameObject _floor;
    [SerializeField] private SpawnItems _items;
    [SerializeField] private int _numOfFloor;
    private int _numOfBrick;
    private GameObject _currentLevel;
    private int _currentNum = 0;
    private float _yDiff = 413f;
    private float _zDiff = 793f;

    // Start is called before the first frame update
    void Start()
    {
        _floor.transform.position = new Vector3(-16f, 411f, 1263f);
        _currentLevel = _floor;
        //Instantiate(_floor);
        //GenerateLevel();
        _numOfBrick = Random.Range(25, 35);
        for (int i = 0; i < _numOfBrick; i++)
        {
            _items.Init(_currentLevel);
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
            _numOfBrick = Random.Range(15, 20);
            for(int i = 0; i < _numOfBrick; i++)
            {
                _items.Init(_currentLevel);
            }
            
        }
        else
            _currentLevel = null;
        
    }

    public GameObject GetCurrentLevel()
    {
        return _currentLevel;
    }

}
