using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject finishCamera;
    public Transform target;
    private Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera.SetActive(true);
        finishCamera.SetActive(false);
        offset = mainCamera.transform.position - target.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (mainCamera)
        {
            Vector3 newPosition = new Vector3(offset.x + target.position.x, offset.y + target.position.y, offset.z + target.position.z);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, newPosition, 50 * Time.deltaTime);
        }
    }

    public void EnableFinishCamera()
    {
        mainCamera.SetActive(false);
        finishCamera.SetActive(true);
    }
}
