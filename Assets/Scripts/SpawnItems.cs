using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItems : MonoBehaviour
{

    [SerializeField] private GameObject _brick, _AIBrick, level;
    [SerializeField] private LevelController _level;
    private Vector3 Min;
    private Vector3 Max;
    private float _xAxis;
    private float _yAxis;
    private float _zAxis;
    private Vector3 _randomPosition;

    private void SetRanges()
    {
        Min = new Vector3(level.transform.position.x - 400, _yAxis, level.transform.position.z - 1500); 
        Max = new Vector3(level.transform.position.x + 400, _yAxis, level.transform.position.z - 900);
        _xAxis = Random.Range(Min.x, Max.x);
        _yAxis = level.transform.position.y-400;
        _zAxis = Random.Range(Min.z, Max.z);
        _randomPosition = new Vector3(_xAxis, _yAxis, _zAxis);
    }
    public void Init(GameObject level)
    {
        this.level = level;
        SetRanges();
        Instantiate(_brick, _randomPosition, Quaternion.identity);
        SetRanges();
        Instantiate(_AIBrick, _randomPosition, Quaternion.identity);
    }


}
