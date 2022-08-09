using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BrickPercent : MonoBehaviour
{
    //public Image _barImage;
    [SerializeField] private TextMeshPro _percentText;
    [SerializeField] private BuildLadder _items, _AIitems;
    private int _maxBrickNum = 30;
    public static int _percent;

    // Start is called before the first frame update
    void Start()
    {
        _items = GameObject.Find("stickman").GetComponent<BuildLadder>();
        _AIitems = GameObject.Find("AI").GetComponent<BuildLadder>();
        _percentText.text = "%" + 0;
    }

    // Update is called once per frame
    void Update()
    {
        if(this.gameObject.tag=="Stickman")
            _percent = (int)((100.0 / _maxBrickNum) * _items.GetBrickCount());
        else if(this.gameObject.tag=="AI")
            _percent = (int)((100.0 / _maxBrickNum) * _AIitems.GetBrickCount());
        _percentText.text = "%" + _percent;
        
        
    }
}
