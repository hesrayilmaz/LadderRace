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
    private Vector3 _startPos, _endPos;


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

            _myBrick.transform.localRotation = Quaternion.identity;
            //_myBrick.transform.localPosition = Vector3.zero;
            _myBrick.transform.parent = _character.transform;
            
            if (_brickList.Count == 0)
                _myBrick.transform.position = _character.transform.position;
            else
            {
                _startPos = _myBrick.transform.position;
                _endPos = _brickList[_brickList.Count - 1].transform.position + new Vector3(0f, 1.1f, 0f);
                StartCoroutine(TrailRendererProcess(_myBrick));
            }
                
                //Vector3.Lerp(_brickList[_brickList.Count - 1].transform.position, _brickList[_brickList.Count - 1].transform.position+new Vector3(0f,1.1f,0f),Time.deltaTime*100f);
            _brickList.Add(_myBrick);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Brick")
        {
            _pickUpAudio.Play();
            _myBrick = other.gameObject;
            
            _pickedUp = true;
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _pickedUp = false;
        
            //_myBrick.GetComponent<TrailRenderer>().emitting = false;
    }


    IEnumerator TrailRendererProcess(GameObject go)
    {
        go.GetComponent<TrailRenderer>().emitting = true;
        go.transform.position = Vector3.Lerp(_startPos, _endPos, 1f);
        yield return new WaitForSeconds(0.7f);
        go.GetComponent<TrailRenderer>().emitting = false;
    }

}
