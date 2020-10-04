using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;

public class GauntletController : MonoBehaviour
{
    private PlayerController playerCont; //This weapon's player's controller
    private WeaponController wepCont; //This weapon's controller
    private Animator animator; //This weapon's animator

    [Header("TIME STONE (components and variables):")]
    public float timeSlow; //How fast time passes when slowed down
    public float timeSlowSpeed; //How fast time slows down and speeds up when manipulated

    [Header("SNAP (components and variables):")]
    public GameObject CMvcamSnap0; //Zooms in on Thanos for cutscene dialogue
    public GameObject CMvcamSnap1; //Zooms in on fingers for snap
    public GameObject whiteOut; //White out rectangle to flash during snap
    public AudioClip endGameSound; //Clip that plays when player first presses snap input
    public AudioClip snapSound; //Clip that plays when snap is triggered
    public float zoomWait; //Tunes how long after snap input to begin zooming on Thanos
    public float lineWait; //Tunes how long after snap input Thanos says "You should have gone for the head"
    public float handZoomWait; //Tunes how long after snap input camera zooms close up on hand
    public float snapWait; //Tunes how long after snap input snap animation actually happens
    public float whiteOutWait; //Tunes how long after snap input whiteOut begins
    public float whiteOutSpeed; //Tunes how long whiteout takes to go from 0 to fully opaque (once this has finished, scene transition occurs
    public float whiteOutTime; //How long screen stays white before loading new scene

    private bool slowingTime; //Pulled true if player has (validly) called for time to be slowed
    private bool endGame; //Pulled true if player has snapped and snap cutscene has begun
    internal float originaltime; //Normal timescale
    private float timevelocity;  //SmoothDamp variable
    private float timevelocity2; //SmoothDamp variable
    private AudioSource[] audioSources; //Audio to slow down
    private float endGameTime; //Counts how much time has passed since endGame started
    private SpriteRenderer whiteRen; //Container for instantiated whiteOut renderer
    private bool snapSoundPlayed; //Makes sure snap sound only gets played once
    private float opacityVel; //SmoothDamp variable

    void Start()
    {
        playerCont = GetComponentInParent<PlayerController>();
        wepCont = GetComponent<WeaponController>();
        animator = GetComponent<Animator>();

        originaltime = Time.timeScale;
    }

    void Update()
    {
        //TIMESTONE:
            //Check Input:
            if (Input.GetButtonDown(playerCont.PlayerInputs(playerCont.player, "Yarg1")) == true)
            {
                slowingTime = !slowingTime;
                audioSources = Object.FindObjectsOfType<AudioSource>(); //Update all audio sources list
            }

            //Slow Down Time:
            if (slowingTime == true && GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraRail>().titleState == CameraRail.TitleState.Play)
            {
                animator.SetBool("Timestone", true);
                Time.timeScale = Mathf.SmoothDamp(Time.timeScale, timeSlow, ref timevelocity, timeSlowSpeed);
                for (int x = audioSources.Length; x > 0; x--) //Slow down audio with time
                {
                    audioSources[x - 1].pitch = Time.timeScale;
                }
            }

            //Speed Up Time:
            else if (Time.timeScale != originaltime)
            {
                slowingTime = false;
                animator.SetBool("Timestone", false);
                Time.timeScale = Mathf.SmoothDamp(Time.timeScale, originaltime, ref timevelocity2, timeSlowSpeed);
                if (Time.timeScale >= 0.95 * originaltime) { Time.timeScale = originaltime; } //Make sure timeScale actually reaches its destination
                for (int x = audioSources.Length; x > 0; x--) //Speed up audio with time
                {
                    audioSources[x - 1].pitch = Time.timeScale;
                }
            }

        //SNAP:
        if (playerCont.skin.ToString() == "Skin InevitableBeard(Clone) (UnityEngine.GameObject)" && //If player is wearing the Thanos skin...
            Input.GetButtonDown(playerCont.PlayerInputs(playerCont.player, "Yarg3")) == true &&     //...and player pushes yarg button...
            Time.timeScale == originaltime &&                                                       //...and time is not currently slowed down...
            endGame != true &&                                                                      //...and endGame has not already begun...
            GetComponent<WeaponController>().attacking == false &&
            GetComponent<WeaponController>().attack == false)
        {
            endGame = true; //Tell program to begin endGame sequence

            //All Inputs Off (cutscene mode):
            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraRail>().titleState = CameraRail.TitleState.Cutscene; //Put game scene type into special mode
            playerCont.ScrubInputs(); //Scrub all inputs on player
            playerCont.otherPlayer.GetComponent<PlayerController>().ScrubInputs(); //Scrub all inputs on enemy player
            //Audio Prep:
            playerCont.skin.GetComponent<AudioSource>().Stop();      //Shut player up
            playerCont.skin.GetComponent<AudioSource>().volume = 0;  //Shut player up
            GetComponent<AudioSource>().clip = snapSound; //Prep clip

            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<AudioSource>().Stop(); //Cut music short
            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<AudioSource>().clip = endGameSound; //Assign endGameSound clip to be played
            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<AudioSource>().volume = 1; //Make sure volume is maxxed out
            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<AudioSource>().loop = false; //Make sure clip doesn't loop
            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<AudioSource>().Play(); //Play clip
            //Visuals Prep:
            GameObject whiteOutClone = Instantiate(whiteOut); //Create transparent whiteOut object
            whiteRen = whiteOutClone.GetComponent<SpriteRenderer>(); //Get whiteOut's spriteRenderer

        }
        if (endGame == true) //Called every frame once snap has begun
        {
            endGameTime += Time.deltaTime; //Increment time counter

            //Zoom In On Thanos:
            if (endGameTime >= zoomWait && GameObject.Find("CM vcamSnap0(Clone)") == null)
            {
                GameObject snapCam0 = Instantiate(CMvcamSnap0); //Instantiate special zoom camera (primary)
                snapCam0.GetComponent<CinemachineVirtualCamera>().Follow = playerCont.gameObject.transform; //Set camera to focus on player
                snapCam0.SetActive(true); //Activate snapCam0
            }
            //Begin Audio:
            if (endGameTime >= lineWait && snapSoundPlayed == false)
            {
                snapSoundPlayed = true;
                GetComponent<AudioSource>().Play(); //Begin audio line
                playerCont.yargInput2 = 1; //Open mouth
            }
            if (endGameTime >= handZoomWait && GameObject.Find("CM vcamSnap1(Clone)") == null)
            {
                GameObject snapCam1 = Instantiate(CMvcamSnap1); //Instantiate special zoom camera (secondary)
                snapCam1.GetComponent<CinemachineVirtualCamera>().Follow = playerCont.gameObject.transform; //Set camera to focus on player
                snapCam1.SetActive(true); //Activate snapCam1
                playerCont.yargInput2 = 0; //Close mouth
            }
            //Snap Fingers:
            if (endGameTime >= snapWait)
            {
                animator.SetBool("Snap", true); //Animate snap
            }
            //Whiteout:
            if (endGameTime >= whiteOutWait)
            {
                float newOpacity = Mathf.SmoothDamp(whiteRen.color.a, 1, ref opacityVel, whiteOutSpeed); //Get updated opacity
                whiteRen.color = new Color(whiteRen.color.r, whiteRen.color.g, whiteRen.color.b, newOpacity); //Commit updated opacity
            }
            //Next Scene:
            if (whiteRen.color.a >= 0.99)
            {
                Debug.Log("oog");

            }
            if (endGameTime >= whiteOutTime)
            {
                SceneManager.LoadScene("SkinSelect");
            }
        }

    }

    public void InstantNormalTime()
    {
        Time.timeScale = originaltime;
        for (int x = audioSources.Length; x > 0; x--) //Immediately put all audio sources back on normal time
        { audioSources[x - 1].pitch = originaltime; }
    }
}
