using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BrickPercent : MonoBehaviour
{

    [SerializeField] private TextMeshPro _percentText;
    //private LevelController _levelController;
    [SerializeField] private BuildLadder _items, _AIItems;
    private int _maxBrickNum = 30;
    private int _percent;

    // Start is called before the first frame update
    void Start()
    {
        //_levelController = GameObject.Find("LevelController").GetComponent<LevelController>();
        _items = GameObject.Find("stickman").GetComponent<BuildLadder>();

        if (gameObject.tag == "Red")
            _AIItems = GameObject.Find("redAI").GetComponent<BuildLadder>();
        else if(gameObject.tag == "Green")
            _AIItems = GameObject.Find("greenAI").GetComponent<BuildLadder>();
        else if(gameObject.tag == "Orange")
            _AIItems = GameObject.Find("orangeAI").GetComponent<BuildLadder>();

       //_percentText = _levelController.GetLevel(0).transform.Find(gameObject.tag+"Ladder").transform.Find("Percent").transform.Find("Text").GetComponent<TextMeshPro>();
        _percentText.text = "%" + 0;
    }

    // Update is called once per frame
    void Update()
    {
        if(gameObject.tag=="Stickman")
            _percent = (int)((100.0 / _maxBrickNum) * _items.GetBrickCount());
        else
            _percent = (int)((100.0 / _maxBrickNum) * _AIItems.GetBrickCount());

        _percentText.text = "%" + _percent;
    }
}
