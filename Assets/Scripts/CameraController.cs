using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject finishCamera;
    [SerializeField] private Transform target;
    [SerializeField] private LevelController levelController;
    private Vector3 offset;
    private GameObject finishFloor;

    // Start is called before the first frame update
    void Start()
    {
        finishFloor = levelController.GetFinishLevel();
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
        finishCamera.transform.position= new Vector3(finishFloor.transform.position.x+100f,
          300f+ finishFloor.transform.position.y, finishFloor.transform.position.z-175f);
        mainCamera.SetActive(false);
        finishCamera.SetActive(true);
    }
}
