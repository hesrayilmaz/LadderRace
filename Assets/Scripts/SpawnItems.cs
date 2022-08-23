using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItems : MonoBehaviour
{

    [SerializeField] private GameObject _brick, _redBrick, _greenBrick, _orangeBrick, 
                                        _StickmanBrickParent, _RedBrickParent, _GreenBrickParent, _OrangeBrickParent;
    private GameObject _brickObj, _redBrickObj, _greenBrickObj, _orangeBrickObj, _level;
    private Vector3 Min, Max, _randomPosition;
    private float _xAxis, _yAxis, _zAxis;
    private Dictionary<string,List<GameObject>> PickUps;
    private Dictionary<string,GameObject> Parents;
    private List<GameObject> StickmanToPickUp, RedToPickUp, GreenToPickUp, OrangeToPickUp;
    public static bool _isFirstLevel = true;

    private void Start()
    {
        StickmanToPickUp = new List<GameObject>();
        RedToPickUp = new List<GameObject>();
        GreenToPickUp = new List<GameObject>();
        OrangeToPickUp = new List<GameObject>();

        PickUps = new Dictionary<string, List<GameObject>>();
        PickUps.Add("Stickman", StickmanToPickUp);
        PickUps.Add("Red", RedToPickUp);
        PickUps.Add("Green", GreenToPickUp);
        PickUps.Add("Orange", OrangeToPickUp);

        Parents = new Dictionary<string, GameObject>();
        Parents.Add("Stickman", _StickmanBrickParent);
        Parents.Add("Red", _RedBrickParent);
        Parents.Add("Green", _GreenBrickParent);
        Parents.Add("Orange", _OrangeBrickParent);
    }
    private void SetRanges()
    {
        Min = new Vector3(_level.transform.position.x - 350, _yAxis, _level.transform.position.z - 1500); 
        Max = new Vector3(_level.transform.position.x + 350, _yAxis, _level.transform.position.z - 1000);
        _xAxis = Random.Range(Min.x, Max.x);
        _yAxis = _level.transform.position.y-400;
        _zAxis = Random.Range(Min.z, Max.z);
        _randomPosition = new Vector3(_xAxis, _yAxis, _zAxis);
    }
    public void Init(GameObject level)
    {
        _level = level;

            SetRanges();
            _brickObj = Instantiate(_brick, _randomPosition, Quaternion.identity);
            PickUps["Stickman"].Add(_brickObj);
        
            SetRanges();
            _redBrickObj = Instantiate(_redBrick, _randomPosition, Quaternion.identity);
            PickUps["Red"].Add(_redBrickObj);
 
            SetRanges();
            _greenBrickObj = Instantiate(_greenBrick, _randomPosition, Quaternion.identity);
            PickUps["Green"].Add(_greenBrickObj);
 
            SetRanges();
            _orangeBrickObj = Instantiate(_orangeBrick, _randomPosition, Quaternion.identity);
            PickUps["Orange"].Add(_orangeBrickObj);
    }

    public void SetParent(string playerTag)
    {
        for (int i = 0; i < PickUps[playerTag].Count; i++)
        {
           if(PickUps[playerTag][i] != null && !PickUps[playerTag][i].CompareTag("Untagged"))
                PickUps[playerTag][i].transform.parent = Parents[playerTag].transform;
        }
    }

    public void ClearParent(string playerTag)
    {
        foreach (Transform child in Parents[playerTag].transform)
        {
            PickUps[playerTag].Remove(child.gameObject);
            GameObject.Destroy(child.gameObject);
        }
    }

}
