using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BrickPercent : MonoBehaviour
{
    //public Image _barImage;
    [SerializeField] private TextMeshPro _percentText;
    [SerializeField] private BuildLadder _items;
    private int _maxBrickNum = 30, _percent;

    // Start is called before the first frame update
    void Start()
    {
        _items = GameObject.Find("stickman").GetComponent<BuildLadder>();
        _percentText.text = "%" + 0;
    }

    // Update is called once per frame
    void Update()
    {
        _percent = (int)((100.0 / _maxBrickNum) * _items.GetBrickCount());
        _percentText.text = "%" + _percent;
    }
}
