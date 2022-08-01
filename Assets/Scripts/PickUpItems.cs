using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpItems : MonoBehaviour
{
    [SerializeField] private GameObject _character;
    [SerializeField] private GameObject _actualBrick;
    [SerializeField] private GameObject _myBrick;
    private List<GameObject> _brickList;
    private bool _pickedUp;


    // Start is called before the first frame update
    void Start()
    {
        _brickList = new List<GameObject>();
        _pickedUp = false;
    }

    public void Init()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (_pickedUp)
        {
            _myBrick.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            /*if(transform.position.z - (transform.position.z-1) > 0)
            {
                Debug.Log("111111");
                Debug.Log(Math.Abs(transform.position.z) - Math.Abs(transform.position.z) - 1);
                _myBrick.transform.position = transform.position + new Vector3(0f, 0f, -20f);
            }
            else
            {
                Debug.Log("22222");
                Debug.Log(Math.Abs(transform.position.z) - Math.Abs(transform.position.z) - 1);
                _myBrick.transform.position = transform.position + new Vector3(0f, 0f, 20f);
            }*/

            //_myBrick.transform.rotation = Quaternion.LookRotation(new Vector3(0f,0f,0f));
            _myBrick.transform.parent = _character.transform;

            if (_brickList.Count == 0)
                _myBrick.transform.position = _character.transform.position;
            else
                _myBrick.transform.position = _brickList[_brickList.Count - 1].transform.position+new Vector3(0f,1f,0f);

            _brickList.Add(_myBrick);
            Debug.Log(_brickList[_brickList.Count - 1]);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Brick")
        {
            Debug.Log("brickkkkkkkkkkkkkkkk");
            _pickedUp = true;
            _myBrick = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _pickedUp = false;
    }


}
