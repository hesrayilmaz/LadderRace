using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItems : MonoBehaviour
{

    [SerializeField] private GameObject _brick, _redBrick, _greenBrick, _orangeBrick, 
                                        _brickParent, _redBrickParent, _greenBrickParent, _orangeBrickParent;
    private GameObject _brickObj, _redBrickObj, _greenBrickObj, _orangeBrickObj, _level;
    private Vector3 Min;
    private Vector3 Max;
    private float _xAxis;
    private float _yAxis;
    private float _zAxis;
    private Vector3 _randomPosition;

    private void SetRanges()
    {
        Min = new Vector3(_level.transform.position.x - 400, _yAxis, _level.transform.position.z - 1500); 
        Max = new Vector3(_level.transform.position.x + 400, _yAxis, _level.transform.position.z - 900);
        _xAxis = Random.Range(Min.x, Max.x);
        _yAxis = _level.transform.position.y-400;
        _zAxis = Random.Range(Min.z, Max.z);
        _randomPosition = new Vector3(_xAxis, _yAxis, _zAxis);
    }
    public void Init(GameObject level)
    {
        this._level = level;
        SetRanges();
        _brickObj=Instantiate(_brick, _randomPosition, Quaternion.identity);
        _brickObj.transform.parent = _brickParent.transform;
        SetRanges();
        _redBrickObj=Instantiate(_redBrick, _randomPosition, Quaternion.identity);
        _redBrickObj.transform.parent = _redBrickParent.transform;
        SetRanges();
        _greenBrickObj = Instantiate(_greenBrick, _randomPosition, Quaternion.identity);
        _greenBrickObj.transform.parent = _greenBrickParent.transform;
        SetRanges();
        _orangeBrickObj = Instantiate(_orangeBrick, _randomPosition, Quaternion.identity);
        _orangeBrickObj.transform.parent = _orangeBrickParent.transform;
    }

    
}
