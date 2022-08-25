using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class JoystickPlayerExample : MonoBehaviour
{
    public FixedJoystick fixedJoystick;

    private void Update()
    {
        if(fixedJoystick.Vertical!=0 || fixedJoystick.Horizontal != 0)
        {

        }
        transform.DOMove((Vector3.forward * fixedJoystick.Vertical+Vector3.right * fixedJoystick.Horizontal), 0.03f).SetRelative();
    }
 
}