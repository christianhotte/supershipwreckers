using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Pauser : MonoBehaviour
{
    //Objects and Components
    public string SkinSelect;
    private CameraRail cameraRail;

    void Start()
    {
        cameraRail = GetComponent<CameraRail>(); //Get cameraRail
    }


    void Update()
    {

        if (cameraRail.titleState == CameraRail.TitleState.Play && Input.GetAxis("Cancel") != 0)
        {
            Application.Quit();
        }

        if (cameraRail.titleState == CameraRail.TitleState.Play && Input.GetAxis("enter") != 0)
        {
            SceneManager.LoadScene(SkinSelect);
        }
    }
}
