using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public void ShowSuccessPanel()
    {
        gameObject.SetActive(true);
        GameObject.Find("fail").SetActive(false);
        GameObject.Find("success").SetActive(true);
    } 
    public void ShowFailPanel()
    {
        gameObject.SetActive(true);
        GameObject.Find("success").SetActive(false);
        GameObject.Find("fail").SetActive(true);
    }

    public void Restart()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
