using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BrickPercent : MonoBehaviour
{
    //public Image _barImage;
    [SerializeField] private TextMeshPro _percentText;
    [SerializeField] private BuildLadder _items, _AIItems;
    private int _maxBrickNum = 30;
    public static int _percent;

    // Start is called before the first frame update
    void Start()
    {
        _items = GameObject.Find("stickman").GetComponent<BuildLadder>();
        if (this.gameObject.tag == "Red")
            _AIItems = GameObject.Find("redAI").GetComponent<BuildLadder>();
        else if(this.gameObject.tag == "Green")
            _AIItems = GameObject.Find("greenAI").GetComponent<BuildLadder>();
        else if(this.gameObject.tag == "Orange")
            _AIItems = GameObject.Find("orangeAI").GetComponent<BuildLadder>();
         
        _percentText.text = "%" + 0;
    }

    // Update is called once per frame
    void Update()
    {
        if(this.gameObject.tag=="Stickman")
            _percent = (int)((100.0 / _maxBrickNum) * _items.GetBrickCount());
        else if(this.gameObject.tag=="Red")
            _percent = (int)((100.0 / _maxBrickNum) * _AIItems.GetBrickCount());
        else if(this.gameObject.tag=="Green")
            _percent = (int)((100.0 / _maxBrickNum) * _AIItems.GetBrickCount());
        else if(this.gameObject.tag=="Orange")
            _percent = (int)((100.0 / _maxBrickNum) * _AIItems.GetBrickCount());
      
        _percentText.text = "%" + _percent;
        
        
    }
}
