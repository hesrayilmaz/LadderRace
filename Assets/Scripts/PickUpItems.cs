using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpItems : MonoBehaviour
{
    [SerializeField] private GameObject _character;
    [SerializeField] private GameObject _actualBrick;
    private GameObject _myBrick;
    [SerializeField] private AudioSource _pickUpAudio;
    private List<GameObject> _brickList;
    private bool _pickedUp;


    // Start is called before the first frame update
    void Start()
    {
        _brickList = new List<GameObject>();
        _pickedUp = false;
    }


    // Update is called once per frame
    void Update()
    {
        if (_pickedUp)
        {
            _myBrick.gameObject.GetComponent<Rigidbody>().isKinematic = true;

            //_myBrick.transform.rotation = Quaternion.LookRotation(new Vector3(0f,0f,0f));
            _myBrick.transform.parent = _character.transform;

            if (_brickList.Count == 0)
                _myBrick.transform.position = _character.transform.position;
            else
                _myBrick.transform.position = _brickList[_brickList.Count - 1].transform.position+new Vector3(0f,1f,0f);

            _brickList.Add(_myBrick);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Brick")
        {
            _pickUpAudio.Play();
            _pickedUp = true;
            _myBrick = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _pickedUp = false;
    }


}
