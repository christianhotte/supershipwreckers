using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //CREDIT DISCLAIMER: Parts of this script are borrowed from a Unity forum user named "s-m-k"

    //Objects and Components:
    public GameObject player1; //Player1 gameObject
    public GameObject player2; //Player2 gameObject
    private Camera sceneCamera; //This camera's camera component

    [Header("Zone Control")]
    [Range(0, 1)] public float innerZoneWidth; //Sets the width of inner zone.   Range = 0 - 1
    [Range(0, 1)] public float innerZoneHeight; //Sets the height of inner zone. Range = 0 - 1
    [Range(0, 1)] public float outerZoneWidth; //Sets the width of outer zone.   Range = 0 - 1
    [Range(0, 1)] public float outerZoneHeight; //Sets the height of outer zone. Range = 0 - 1
    private Rect innerZone; //RECTANGLE SIZE IS RELATIVE TO CAMERA. If players are inside this zone, camera will shrink
    private Rect outerZone; //RECTANGLE SIZE IS RELATIVE TO CAMERA. If players are outside this zone, camera will expand
    public Vector2 perimeterBuffer; //How much space to add around players so that they aren't hanging partially offscreen
    public Vector2 defaultDimensions; //The default dimensions of this camera (which it reverts to when locked)

    [Header("Camera Control Variables:")] //Camera Control Variables:
    public float minCamSize; //Sets the smallest size the camera screen can be
    public float vertOffset; //How much to vertically offset the camera (gives players more on-screen room to jump and such)

    public bool lockSize; //Locks camera to default dimensions (meant to be used for locking screen during skin selection)

    private void Start()
    {
        //Get Objects and Components:
        sceneCamera = GetComponent<Camera>(); //Get camera component
            //Get Players:
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); //Get array of both players in scene
            if (players[0].GetComponent<PlayerController>().player == PlayerController.Player.Player1) //Assign players to their appropriate slots
            { player1 = players[0]; player2 = players[1]; }
            else { player1 = players[1]; player2 = players[0]; }
    }

    void Update()
    {
        //Set Zone Sizes:
        innerZone.position = transform.position; //Set zone position to camera center
        outerZone.position = transform.position; //Set zone position to camera center
        innerZone.width = innerZoneWidth * (sceneCamera.scaledPixelWidth * 2);
        innerZone.height = innerZoneHeight * (sceneCamera.scaledPixelHeight * 2);
        outerZone.width = outerZoneWidth * (sceneCamera.scaledPixelWidth * 2);
        outerZone.height = outerZoneHeight * (sceneCamera.scaledPixelHeight * 2);

        //Check Player Position in Zones:
        if (innerZone.Contains(player1.GetComponent<PlayerController>().rb.position) ||  //If player1 is inside inner zone...
            innerZone.Contains(player2.GetComponent<PlayerController>().rb.position))    //If player2 is inside inner zone...
        {
            Debug.Log("Players outside innerZone");
        }
        SetCameraPos();
        SetCameraSize();
    }

    void SetCameraPos()
    {
        Vector3 playerCenter = (player1.transform.position + player2.transform.position) * 0.5f; //Find center point between two players
        transform.position = new Vector3(playerCenter.x, (playerCenter.y + vertOffset), transform.position.z); //Set suitable positon
    }

    void SetCameraSize()
    {
        //Prepare variables:
        float width = 0; //Initialize width variable
        float height = 0; //Initialize height variable
        float minSizeX = minCamSize * Screen.width / Screen.height; //Keep camera dimensions true to screen aspect ratio
        if (player1.transform.position.x < player2.transform.position.x) //Use player transforms based on their x positions
        {
            width = Mathf.Abs(player1.transform.position.x - player2.transform.position.x - perimeterBuffer.x) * 0.5f; //Adjust for orthographicSize and buffer
        } else {
            width = Mathf.Abs(player2.transform.position.x - player1.transform.position.x - perimeterBuffer.x) * 0.5f; //Adjust for orthographicSize and buffer
        }
        if (player1.transform.position.y < player2.transform.position.y) //Use player transforms based on their y positions
        {
            height = Mathf.Abs(player1.transform.position.y - player2.transform.position.y - perimeterBuffer.y) * 0.5f; //Adjust for orthographicSize and buffer
        } else {
            height = Mathf.Abs(player2.transform.position.y - player1.transform.position.y - perimeterBuffer.y) * 0.5f; //Adjust for orthographicSize and buffer
        }
        
        float camSizeX = Mathf.Max(width, minSizeX); //Calculate camera size

        //Commit Size Changes:
        sceneCamera.orthographicSize = Mathf.Max(height, camSizeX * Screen.height / Screen.width, minCamSize); //Lock min cam size if needed
    }
}
