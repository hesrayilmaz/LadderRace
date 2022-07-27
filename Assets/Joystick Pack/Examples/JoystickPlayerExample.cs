using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class JoystickPlayerExample : MonoBehaviour
{
    public float speed;
    public FixedJoystick fixedJoystick;
    public Rigidbody rb;

    private void Update()
    {
        Debug.Log("vertical: " + fixedJoystick.Vertical);
        Debug.Log("horizontal: " + fixedJoystick.Horizontal);
        transform.DOMove((Vector3.forward * fixedJoystick.Vertical+Vector3.right * fixedJoystick.Horizontal), 0.03f).SetRelative();
    }
    public void FixedUpdate()
    {
        //Vector3 direction = Vector3.forward * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal;
        //rb.AddForce(direction * speed * Time.fixedDeltaTime, ForceMode.VelocityChange);
        
    }
}