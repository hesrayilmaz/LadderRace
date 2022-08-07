using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BrickBar : MonoBehaviour
{
    public Image _barImage;
    [SerializeField] private TextMeshProUGUI _percent;
    [SerializeField] private BuildLadder _items;
    private int maxValue = 20, percent;
    public static float _currentVal = 0f;
    // Start is called before the first frame update
    void Start()
    {
        _barImage.fillAmount = _currentVal;
        _percent.text = "%" + 0;
    }

    // Update is called once per frame
    void Update()
    {
        percent = (100 / maxValue) * _items.GetBrickCount();
        _percent.text = "%" + percent;
        _barImage.fillAmount= _items.GetBrickCount()*0.05f;
      
    }
}
