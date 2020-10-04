using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;

public class CameraRail : MonoBehaviour
{
    /*  This script controls the camera and scene for times when it is not being manipulated by Cinemachine (starting "cutscene", playerselect, etc).
     *  It has also been expanded into a general scene governor, and it oversees most things related to changes in gameplay state, music, effects, tutorials, and scene transitions
     *  Rail Flow:
     *      -Black screen [start]
     *      -Black screen >>> SHIPWRECKERS LETTERS [player input]
     *      -SHIPWRECKERS LETTERS >(+)> SUPER [player input]
     *      -SUPER, SHIPWRECKERS >(+)> Banners [instantaneous]
     *      -Banners, SUPER, SHIPWRECKERS >(/)> skull laugh
     *      -skull laugh >(/)> press start
     *      -press start >(/)> start crunch
     */
    
        //TITLESTATE: Overarching game governance system that controls which inputs are used when, what is currently happening in scene, and how camera is moving at any given time
        public enum TitleState { BlackScreen, SuperFadein, LaughPause, SkullLaugh, SkullOpen, PressStart, PanPause, ThroneOut, SkinSelect, Play, Win, ThroneIn, Cutscene };
        public TitleState titleState; //Initialize a TitleState variable

    [Header("Objects and Components:")] //Objects and Components:
    //CINEMACHINE:
    public Cinemachine.CinemachineBrain cinemachineBrain; //Secondary camera controller to take over when needed by CameraRail
    public GameObject CMvcam0; //Cinemachine virtual camera 0 (throneIn)
    public GameObject CMvcam1; //Cinemachine virtual camera 1 (play)
    public GameObject CMvcam2; //Cinemachine virtual camera 2 (start)
    public GameObject CMvcam3; //Cinemachine virtual camera 3 (win)
    public GameObject CMvcam4; //Cinemachine virtual camera 4 (throneOut)
    public GameObject targetGroup1; //Cinemachine targetGroup part
    //TITLECARD ELEMENTS:
    [Space()]
    public GameObject super; //"SUPER" gameObject
        private SpriteRenderer superRen; //"SUPER" spriteRenderer
    public GameObject shipwreckers; //"SHIPWRECKERS" gameObject
        private GameObject[] letters; //"SHIPWRECKERS" individual letter gameObjects (temp get storage)
        private List<GameObject> letterList = new List<GameObject>();  //Initialize individual letter gameObject list
    public GameObject pressStart; //"PressStart" gameObject
    public GameObject banners; //Banner gameObject
        private SpriteRenderer bannerRen; //Banner spriteRenderer
    public GameObject stageBackground; //Stage gameObject
        private SpriteRenderer stageRen; //Stage spriteRenderer
    public GameObject throneHighlight; //ThroneHighlight gameObject
    public GameObject skinSelect; //Skinselect tutorial gameObject
    //OTHER:
    [Space()]
    public string SkinSelectScene; //The name of the scene this scene loads into when reloading
    private SkinGiver skinGiver; //SkinGiver component
    public GameObject readyFlagL; //ReadyFlag prefab
    public GameObject readyFlagR; //ReadyFlag prefab (reversed)
    public GameObject healthTracker1; //Player1 health tracker
        private SpriteRenderer healthRen1; //Player1 health spriteRenderer
    public GameObject healthTracker2; //Player2 health tracker
        private SpriteRenderer healthRen2; //Player2 health spriteRenderer
    public GameObject player1; //Player1 gameObject
    public GameObject player2; //Player2 gameObject
    private PlayerController player1Cont; //Player1 controller
    private PlayerController player2Cont; //Player2 controller
    //SOUNDS:
    [Header("Sound Effects:")]
    public AudioClip[] letterAppearSounds; //Array of sounds (probably yargs) to play when letter appears
    public AudioClip superAppearSound; //Sound to play when "SUPER" has appeared
    public AudioClip laughSound; //Sound to play when skull is laughing
    public AudioClip ambientTitleSounds; //Play in background during title sequence
    public AudioClip boneBreakSound; //Sound to play when pressStart bone is breaking
    public AudioClip throneSound; //Sound to play when player gets on throne
    public AudioClip music; //Music loop that plays throughout game

    [Header("Phase Timing:")] //Segments (lengths) of on-screen elements in order
    public Vector4 defaultCameraPos; //Sets default position of camera (for when it gets fucked up by Cinemachine). Fourth variable is size
    public float musicVolume; //How loud music gets
    public float musicFadeTime; //How long it takes music to fade in after being activated
    public TitleState startMusic; //The phase where music begins
    public float startTimeBuffer; //Gives program time to settle stuff before starting to detect inputs
    public float autoLetterAppear; //If no buttons are pushed, automatically appear a new letter (from "SHIPWRECKERS") once this amount of time has passed
    [Range(0, 1)] public float minLetterVol; //How quiet quietest letter is when it appears
    [Range(0, 1)] public float maxLetterVol; //How loud loudest letter is when it appears
    public float superAppearSpeed; //How many seconds it takes "SUPER" to go from invisible to fully visible
    public Vector3 superShake; //How much "SUPER" shakes screen when it appears
    public float pauseBeforeLaugh; //How long to wait between "SUPER" fadein phase and laugh phase
    public int skullLaughs; //How many times the skull will laugh
    public float skullLaughInterval; //How long skull waits in between laughs
    public float pressStartWait; //How long to wait after skull laugh ends to make pressStart bone appear
    public float pauseBeforePan; //How long to wait before beginning pan to player select
    public float panTime; //How long it takes camera to pan to skin select screen
    public float skinSelectCamY; //Where the camera moves to when panning (y-axis only)
    public float skinSelectCamSize; //How zoomed camera is when it reaches skin select
    public float skinScroll; //Determines how fast players can scroll through skins
    public float skinReadyTime; //How long both players have to be ready for game to begin
    public float healthShowTime; //How long it takes health to show up once match has begun
    public Vector4 healthPos; //Default positions and scale of health trackers (program will put them in their appropriate spots)
    public float winSkullTalk; //How long after a player dies does the skull tell other player to go to the throne
    public float throneInZoomDelay; //How long after player sits on throne to zoom all the way in
    public float endTime; //How long after player sits on throne to end game and load next scene (should be tuned exactly with camera zoom)

    //Internal State Variables:
    private bool skippingLetters; //Pulled true if players cancel out of manual letter entry, automatically places down letters according to autoLetterAppear
    private bool musicOn; //Tells program that music is on
    private bool cameraPanning; //Tells program that camera is panning to skinSelect
    private bool skullLaughing; //Tells program to make skull laugh
    private bool showHealth; //Tells program to show health trackers

    //Internal Math Variables:
    private int   lettersAppeared; //How many letters have appeared so far (used to scale volume)
    private float letterVolStep;   //How much to increase volume of each letter after another
    private float timeSinceSkip;   //Keeps track of how much time has passed since last letter placement
    private float timeSinceLaugh;  //Keeps track of how much time has passed since last laugh
    private float superStartScale; //Notes scale "SUPER" starts at for smoothdamp purposes
    private float superVel1;  //Keeps track of "SUPER" opacity velocity when appearing
    private float superVel2;  //Keeps track of "SUPER" scale velocity when appearing
    private float volumeVel;  //Keeps track of music volume velocity when fading in
    private float cameraVel1; //Keeps track of camera y pos velocity when panning
    private float cameraVel2; //Keeps track of camera size velocity when panning
    private float healthVel;  //Keeps track of health tracker opacity when fading in
    private Vector3 healthVel1; //Keeps track of health tracker velocity when fading in
    private Vector3 healthVel2; //Keeps track of health tracker velocity when fading in
    private Vector3 healthVel3; //Keeps track of health tracker scale when fading in
    private float player1SkinTime; //Keeps track of how much time has passed since player1 last scrolled through skin
    private float player2SkinTime; //Keeps track of how much time has passed since player2 last scrolled through skin
    private float readyTime; //Keeps track of how long both players have been ready for
    private Vector3 healthPos1;  //Player1 health tracker default positional destination
    private Vector3 healthPos2;  //Player2 health tracker default positional destination
    private Vector3 healthScale; //Health tracker default scale destination
    private float healthClock; //Health tracker smoothdamp clock
    public float winTime; //Keeps track of how long it has been since the Win Phase began

    void Start()
    {
        //Cursor:
        Cursor.visible = false; //Hide cursor
        //Get Objects and Components:
        superRen =  super.GetComponent<SpriteRenderer>();           //Get "SUPER" spriteRenderer
        stageRen =  stageBackground.GetComponent<SpriteRenderer>(); //Get stage spriteRenderer
        bannerRen = banners.GetComponent<SpriteRenderer>();         //Get banner spriteRenderer
        healthRen1 = healthTracker1.GetComponent<SpriteRenderer>(); //Get healthTracker1 spriteRenderer
        healthRen2 = healthTracker2.GetComponent<SpriteRenderer>(); //Get healthTracker2 spriteRenderer
            //SHIPWRECKERS Letter Renderers:
            letters = GameObject.FindGameObjectsWithTag("TitleLetter"); //Find all letter objects
            for (int x = letters.Length; x > 0; x--) //Parse through contents of letters and assign to list
            { letterList.Add(letters[x - 1]); } //Add letter to list
            //Players:
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); //Get array of both players in scene
            if (players[0].GetComponent<PlayerController>().player == PlayerController.Player.Player1) //Assign players to their appropriate slots
            { player1 = players[0]; player2 = players[1]; } else { player1 = players[1]; player2 = players[0]; }
        player1Cont = player1.GetComponent<PlayerController>(); //Get Player1 controller
        player2Cont = player2.GetComponent<PlayerController>(); //Get Player2 controller
        skinGiver = GetComponent<SkinGiver>(); //Get SkinGiver component

        //Initialize Variables:
        superStartScale = super.transform.localScale.x; //Get starting scale for "SUPER"
        letterVolStep = (maxLetterVol - minLetterVol) / letters.Length; //Get volume increment for each letter
        //Health Trackers:
        healthPos1 = new Vector3(healthPos.x, healthPos.y, healthPos.z);  //Store default health position for later use
        healthPos2 = new Vector3(-healthPos.x, healthPos.y, healthPos.z); //Store default health position for later use
        healthScale = Vector3.one * healthPos.w;                          //Store default health scale for later use
        /*healthTracker1.transform.localPosition = healthPos1;  //Set position
        healthTracker2.transform.localPosition = healthPos2;  //Set position
        healthTracker1.transform.localScale =    healthScale; //Set scale
        healthTracker2.transform.localScale =    healthScale; *///Set scale
        //Intro Phase:
        if (titleState == TitleState.BlackScreen) //If starting from intro of Title Screen, initialize camera pos and scale
        {
            //EXPERIMENTAL: Adapted for cinemachine
            CMvcam2.transform.position = defaultCameraPos; //Set position to first three variables of defaultCameraPos
            CMvcam2.GetComponent<CinemachineVirtualCamera>().m_Lens.OrthographicSize = defaultCameraPos.w; //Set size to fourth variable
        }

    }

    void Update()
    {
            //PASSIVE MUSIC/AMBIENTSOUND UPDATE:
            if (musicOn == false && titleState == startMusic) //Start music at designated phase
            {
                GetComponent<AudioSource>().clip = music; //Assign music as audio source
                GetComponent<AudioSource>().Play(); //Play music
                musicOn = true; //Tell script to fade music in
            }
            if (musicOn == true && GetComponent<AudioSource>().volume != musicVolume) //If music is fading in
            {
                float volume = Mathf.SmoothDamp(GetComponent<AudioSource>().volume, musicVolume, ref volumeVel, musicFadeTime); //Get smoothdamped volume
                GetComponent<AudioSource>().volume = volume; //Commit volume
            }

            //PASSIVE CAMERAPAN/TEXTFADE UPDATE:
            if (cameraPanning == true)
            {
                //Calculate Targets:
                float camY    = Mathf.SmoothDamp(transform.position.y, skinSelectCamY, ref cameraVel1, panTime);
                float camSize = Mathf.SmoothDamp(GetComponent<Camera>().orthographicSize, skinSelectCamSize, ref cameraVel2, panTime);
                float opacity = Mathf.SmoothDamp(super.GetComponent<SpriteRenderer>().color.a, 0, ref superVel1, panTime/2);
                //Commit Movement:
                CMvcam2.transform.position = new Vector3(transform.position.x, camY, transform.position.z);        //Move camera upward
                CMvcam2.GetComponent<CinemachineVirtualCamera>().m_Lens.OrthographicSize = camSize;                //Scale virtual camera
                superRen.color   = new Color(superRen.color.r, superRen.color.g, superRen.color.b, opacity);       //Commit updated opacity
                    //Fade Each Letter:
                    letters = GameObject.FindGameObjectsWithTag("TitleLetter"); //Find all letter objects
                    for (int x = letters.Length; x > 0; x--) //Parse through contents of letters and assign to list
                    { letterList.Add(letters[x - 1]); } //Add letter to list
                    while (letterList.Count > 0) { letterList[0].GetComponent<SpriteRenderer>().color =
                        superRen.color = new Color(letterList[0].GetComponent<SpriteRenderer>().color.r,
                                                   letterList[0].GetComponent<SpriteRenderer>().color.g,
                                                   letterList[0].GetComponent<SpriteRenderer>().color.b,
                                                   opacity);
                                                   letterList.RemoveAt(0);}
                //if (bannerRen.enabled == true) { bannerRen.enabled = false; } //Get rid of unnecessary extra banner sprite

                //PASSIVEBREAKOUT:
                if (transform.position.y == skinSelectCamY) { cameraPanning = false; } //End once camera has officially reached its final destination
            }

            //PASSIVE SHOWHEALTH UPDATE:
            if (showHealth == true)
            {
                healthClock += Time.deltaTime; //Increment healthClock
                float dampTime = healthShowTime * Time.deltaTime; //Get shortened time float to use for each smoothdamp
                //UPDATE OPACITY:
                float opacity = Mathf.SmoothDamp(healthRen1.color.a, 1, ref healthVel, dampTime); //Get updated opacity
                healthRen1.color = new Color(healthRen1.color.r, healthRen1.color.g, healthRen1.color.b, opacity); //Commit updated opacity
                healthRen2.color = new Color(healthRen2.color.r, healthRen2.color.g, healthRen2.color.b, opacity); //Commit updated opacity
                //UPDATE POSITION/SCALE:
                Vector3 position1 = Vector3.SmoothDamp(healthTracker1.transform.position, healthPos1, ref healthVel1, dampTime); //Get updated position
                Vector3 position2 = Vector3.SmoothDamp(healthTracker2.transform.position, healthPos2, ref healthVel2, dampTime); //Get updated position
                Vector3 scale =     Vector3.SmoothDamp(healthTracker1.transform.localScale, healthScale, ref healthVel3, dampTime); //Get updated scale
                healthTracker1.transform.localPosition = position1; healthTracker1.transform.localScale = scale; //Commit updated position/scale
                healthTracker2.transform.localPosition = position2; healthTracker2.transform.localScale = scale; //Commit updated position/scale
         
                //PASSIVEBREAKOUT:
                if (healthClock >= healthShowTime)  //Stop updating once finished
                {
                    //Play Appear Sound/ScreenShake
                    banners.GetComponent<AudioSource>().clip = superAppearSound;
                    banners.GetComponent<AudioSource>().Play();
                    GetComponent<CinemachineImpulseSource>().GenerateImpulse(superShake * 0.75f); //Generate minor screen shake
                    //Top Off Variables:
                    healthRen1.color = new Color(healthRen1.color.r, healthRen1.color.g, healthRen1.color.b, 255);
                    healthRen2.color = new Color(healthRen2.color.r, healthRen2.color.g, healthRen2.color.b, 255);
                    healthTracker1.transform.localPosition = healthPos1;
                    healthTracker2.transform.localPosition = healthPos2;
                    healthTracker1.transform.localScale = healthScale;
                    healthTracker2.transform.localScale = healthScale;
                    //Advance Tutorials:
                    skinSelect.GetComponent<Animator>().SetBool("Play", true); //Tell tutorialMap to forward to next set of tutorials
                    //PASSIVEBREAKOUT:
                    showHealth = false;
                }
            }

            //PASSIVE SKULL LAUGH UPDATE:
            if (titleState == TitleState.SkullLaugh && skullLaughing == false ||                         //First activation
                titleState == TitleState.Win && skullLaughing == false && winTime > winSkullTalk)        //Second activation
            {
                skullLaughing = true; //Tell program to only do this once
                banners.GetComponent<AudioSource>().clip = laughSound; //Set laugh clip
                banners.GetComponent<AudioSource>().Play(); //Play laugh clip
            }

            //PASSIVE PAUSER UPDATE:
            //Check for Pause or Quit:
            if (titleState == TitleState.Play || titleState == TitleState.Win || titleState == TitleState.SkinSelect || titleState == TitleState.ThroneIn)
            {
                if (Input.GetButtonDown("Cancel") == true) { Application.Quit(); } //Quit game
                if (Input.GetButtonDown("enter") == true)  { SceneManager.LoadScene(SkinSelectScene); } //Load skin selection screen
            }

            //PASSIVE GAUNTLETTIME UPDATE:
            if (player1Cont.hookArm != null && player2Cont.hookArm != null)
            {
                if (player1Cont.hookArm.name != "Weapon InfinityGauntlet(Clone)" && player2Cont.hookArm.name != "Weapon InfinityGauntlet(Clone)" && Time.timeScale != 1) //If no player is playing with the infinity gauntlet
                {
                    Time.timeScale = 1; //Set timescale to default
                    AudioSource[] audioSources = Object.FindObjectsOfType<AudioSource>(); //Update all audio sources list
                    for (int x = audioSources.Length; x > 0; x--) //Immediately put all audio sources back on normal time
                    { audioSources[x - 1].pitch = Time.timeScale; }
                }
            }

        //SHIPWRECKER LETTER APPEAR PHASE:
        if (titleState == TitleState.BlackScreen && Time.realtimeSinceStartup > startTimeBuffer)
        {
            timeSinceSkip += Time.deltaTime; //Increment time tracker
            //Detect Input:
            if (Input.anyKeyDown == true) //If players skip manual letter appearance:
            {
                skippingLetters = true; //Make sure letters don't make sound when they appear all at once
                for (int x = letters.Length; x > 0; x--) { NextLetter(); } //Make all letters appear now
                //BREAKOUT (failsafe):
                titleState = TitleState.SuperFadein; //Trigger next title state if letters don't already
            }
            //Make Letters Appear (BREAKOUT in method):
            if (timeSinceSkip > autoLetterAppear)
            {
                    timeSinceSkip = 0; //Reset time tracker
                    NextLetter(); //Place new letter
            }
        }

        //SUPER APPEAR PHASE:
        if (titleState == TitleState.SuperFadein)
        {
            //UPDATE OPACITY:
            float opacity = Mathf.SmoothDamp(0, 255, ref superVel1, superAppearSpeed * Time.deltaTime);   //Get updated opacity
            superRen.color =  new Color(superRen.color.r, superRen.color.g, superRen.color.b, opacity);     //Commit updated opacity
            stageRen.color =  new Color(stageRen.color.r, stageRen.color.g, stageRen.color.b, opacity);     //Commit updated opacity
            bannerRen.color = new Color(bannerRen.color.r, bannerRen.color.g, bannerRen.color.b, opacity);  //Commit updated opacity
            //UPDATE SCALE:
            float scale = Mathf.SmoothDamp(super.transform.localScale.x, 1, ref superVel2, superAppearSpeed * Time.deltaTime); //Get updated scale
            super.transform.localScale = new Vector3(scale, scale, super.transform.localScale.z); //Commit updated scale
            //BREAKOUT:
            if (super.transform.localScale.x < 1.2) //If "SUPER" has fully appeared:
            {
                super.transform.localScale = Vector3.one; //Finish position update
                super.GetComponent<AudioSource>().clip = superAppearSound; //Get "SUPER" appear sound
                super.GetComponent<AudioSource>().Play(); //Play "SUPER" appear sound
                GetComponent<AudioSource>().clip = ambientTitleSounds; //Assign yarg conversation
                GetComponent<AudioSource>().Play(); //Play yarg conversation
                GetComponent<CinemachineImpulseSource>().GenerateImpulse(superShake); //Generate screen shake
                //BREAKOUT:
                titleState = TitleState.LaughPause; //Move on to next phase
            }
        }

        //PAUSE PHASE:
        if (titleState == TitleState.LaughPause)
        {
            timeSinceLaugh += Time.deltaTime; //Temporarily hijack next phase's time tracker
            if (timeSinceLaugh > pauseBeforeLaugh) //Once pause has concluded...
            {
                //BREAKOUT:
                timeSinceLaugh = 0; //Reset hijacked variable
                titleState = TitleState.SkullLaugh; //Move on to next phase
            }
        }

        //SKULL LAUGH PHASE:
        if (titleState == TitleState.SkullLaugh)
        {
            timeSinceLaugh += Time.deltaTime; //Update time tracker
            if (skullLaughs > 0 && timeSinceLaugh > skullLaughInterval) //If skull has any laughs left (and it has not laughed for long enough)...
            {
                skullLaughs--; //Decrement laugh tracker
                SkullLaugh(); //Skull laughs
            }
            if (skullLaughs == 0) //If skull has no more laughs...
            {
                //PREP NEXT PHASE'S ANIMATIONS
                banners.GetComponent<Animator>().SetBool("Open", true); //Tell animator to open skull mouth

                //BREAKOUT:
                timeSinceLaugh = 0; //Prep time counter for other uses
                titleState = TitleState.SkullOpen; //Move on to next phase
            }
        }

        //OPENMOUTH PHASE:
        if (titleState == TitleState.SkullOpen)
        {
            timeSinceLaugh += Time.deltaTime; //Temporarily hijack next phase's time tracker
            if (timeSinceLaugh > pressStartWait) //Once enough time has passed...
            {
                //BREAKOUT:
                pressStart.GetComponent<Animator>().SetTrigger("NextStep"); //Animate pressStart appearance
                titleState = TitleState.PressStart; //Move on to next phase
            }
        }

        //WAIT-FOR-START PHASE:
        if (titleState == TitleState.PressStart)
        {
            //Wait For Input:
            if (Input.anyKeyDown)
            {
                //Initiate Animations:
                banners.GetComponent<Animator>().SetBool("Open", false); //Close skull mouth
                pressStart.GetComponent<Animator>().SetTrigger("NextStep"); //Break bone
                //Make Sound:
                pressStart.GetComponent<AudioSource>().clip = boneBreakSound; //Set clip
                pressStart.GetComponent<AudioSource>().Play(); //Play clip
                //BREAKOUT:
                titleState = TitleState.PanPause; //Move on to next phase
            }
        }

        //PANNING TO SKINSELECT PHASE:
        if (titleState == TitleState.PanPause)
        {
            timeSinceLaugh += Time.deltaTime; //Temporarily hijack next phase's time tracker
            if (timeSinceLaugh > pauseBeforePan) //Once pause has concluded...
            {
                //BREAKOUT:
                timeSinceLaugh = 0; //Reset hijacked variable
                cameraPanning = true; //Tell program to pan camera and fade out title screen
                titleState = TitleState.SkinSelect; //Move on to next phase
            }
        }

        //THRONEOUT PHASE (ALT):
        if (titleState == TitleState.ThroneOut)
        {
            //For scenes where a player has won and sat on the throne, zoom out from winner and into skinselect
            if (CMvcam0 != null) //Only do this if scene has a vcam 0 and it is assigned to a script
            {
                if (Time.timeSinceLevelLoad > 0.02) //Wait one fixed update for things to get situated
                {
                    CMvcam2.SetActive(true); //Immediately activate normal scene cam
                    titleState = TitleState.SkinSelect; //Immediately BREAKOUT into skinselect
                }
            }
            else
            {
                titleState = TitleState.SkinSelect; //Bypass BREAKOUT for exception
            }
        }

        //SKINSELECT PHASE:
        if (titleState == TitleState.SkinSelect)
        {
            //PlayerController partially handles the skin selection process, producing inputs depending on TitleState
            //SkinGiver does the heavy lifting of storing skin prefabs and keeping track of skindexes
            //Individual skins are equipped to automatically attach themselves and establish contingencies upon instantiation

            //Prep Input:
            if (player1Cont.skinLeft ==  0 && 
                player1Cont.skinRight == 0)   { player1SkinTime = 0; } //Let player shift through skins as fast as they can push joystick
            if (player2Cont.skinLeft ==  0 &&
                player2Cont.skinRight == 0)   { player2SkinTime = 0; } //Let player shift through skins as fast as they can push joystick

            if (player1Cont.skinReady == false)
            {
                player1SkinTime -= Time.deltaTime; //Decrement skin time
                if (player1SkinTime < 0) { player1SkinTime = 0; } //Clamp at 0
                if (player1Cont.skinLeft != 0 && player1SkinTime == 0) //If detecting input and scroll interval has passed...
                {
                    skinGiver.AssignSkin(player1, skinGiver.skins[IncrementSkindex(PlayerController.Player.Player1, true)]); //Assign skin
                    player1SkinTime = skinScroll; //Start countdown
                }
                if (player1Cont.skinRight != 0 && player1SkinTime == 0) //If detecting input and scroll interval has passed...
                {
                    skinGiver.AssignSkin(player1, skinGiver.skins[IncrementSkindex(PlayerController.Player.Player1, false)]); //Assign skin
                    player1SkinTime = skinScroll; //Start countdown
                }
            }
            if (player2Cont.skinReady == false)
            {
                player2SkinTime -= Time.deltaTime; //Decrement skin time
                if (player2SkinTime < 0) { player2SkinTime = 0; } //Clamp at 0
                if (player2Cont.skinLeft != 0 && player2SkinTime == 0) //If detecting input and scroll interval has passed...
                {
                    skinGiver.AssignSkin(player2, skinGiver.skins[IncrementSkindex(PlayerController.Player.Player2, true)]); //Assign skin
                    player2SkinTime = skinScroll; //Start countdown
                }
                if (player2Cont.skinRight != 0 && player2SkinTime == 0) //If detecting input and scroll interval has passed...
                {
                    skinGiver.AssignSkin(player2, skinGiver.skins[IncrementSkindex(PlayerController.Player.Player2, false)]); //Assign skin
                    player2SkinTime = skinScroll; //Start countdown
                }
            }
            if (player1Cont.skinReady == true)
            {
                //Scrub Variables:
                player1SkinTime = 0; //Reset skinTime
                if (player1Cont.skinLeft != 0)  { player1Cont.skinLeft = 0; }  //Erase residual skin input
                if (player1Cont.skinRight != 0) { player1Cont.skinRight = 0; } //Erase residual skin input

            }
            if (player2Cont.skinReady == true)
            {
                //Scrub Variables:
                player2SkinTime = 0; //Reset skinTime
                if (player2Cont.skinLeft != 0)  { player2Cont.skinLeft = 0; }  //Erase residual skin input
                if (player2Cont.skinRight != 0) { player2Cont.skinRight = 0; } //Erase residual skin input

            }

            if (player1Cont.skinReady == true && player2Cont.skinReady == true) //If both players are readied up...
            {
                readyTime += Time.deltaTime; //Increment readyTime
                if (readyTime > skinReadyTime) //Once players are readied up:
                {
                    //Sort Flags Out:
                    player1Cont.skinReady = false; player1Cont.throwArm.GetComponent<ThrowArmController>().skinReady = false; //Unready skins
                    player2Cont.skinReady = false; player2Cont.throwArm.GetComponent<ThrowArmController>().skinReady = false; //Unready skins
                    player1Cont.throwArm.GetComponent<ThrowArmController>().PickUp(readyFlagL); //Instantiate throwable readyFlag on player1
                    player2Cont.throwArm.GetComponent<ThrowArmController>().PickUp(readyFlagR); //Instantiate throwable readyFlag on player2
                    //Remove Excess Skin:
                    GameObject[] skins = GameObject.FindGameObjectsWithTag("Skin"); //Get array of all skins in scene
                    for (int x = skins.Length; x > 0; x--) //Parse through array...
                    { GameObject skin = skins[x - 1]; //Get skin from array
                        if (skin.GetComponent<SpriteRenderer>().enabled == false) { Destroy(skin); } } //Destroy if inactive
                    //Activate Cinemachine:
                    CMvcam1.SetActive(true); //Activate player-following camera
                    //Show Health:
                    showHealth = true;
                    //BREAKOUT:
                    titleState = TitleState.Play; //Move on to next phase
                }

            } else { readyTime = 0; } //Reset readyTime when one or both players is unready

        }

        //PLAY PHASE:
        if (titleState == TitleState.Play)
        {
            //Check For Win Condition:
            if (player1.GetComponent<Damager>().dead == true || player2.GetComponent<Damager>().dead == true)
            {
                //Prep For Win Phase:
                GetComponent<AudioSource>().Stop(); //Halt music
                throneHighlight.SetActive(true); //Make throne highlight visible
                skullLaughing = false; //Refresh skull laughing
                skullLaughs = 4; //Refresh skull laughs
                //BREAKOUT:
                titleState = TitleState.Win; //Move on to next phase
            }
        }

        //WIN PHASE:
        if (titleState == TitleState.Win)
        {
            //If player1 gets the throne, go to scene where player1 skin select has crown
            //If player2 gets the throne, go to scene where player2 skin select has crown

            //SKULL LAUGH:
            winTime += Time.deltaTime; //Update time tracker
            timeSinceLaugh += Time.deltaTime; //Update time tracker
            if (skullLaughs > 0 && timeSinceLaugh > skullLaughInterval && winTime > winSkullTalk) //If skull has any laughs left (and it has not laughed for long enough)...
            {
                skullLaughs--; //Decrement laugh tracker
                SkullLaugh(); //Skull laughs
            }

            //SITTING ON THRONE:
            float distanceFromP1 = Vector2.Distance(player1Cont.thronePos, player1.transform.position); //Get distance from player1
            float distanceFromP2 = Vector2.Distance(player2Cont.thronePos, player2.transform.position); //Get distance from player2
            float p1Range = player1.GetComponentInChildren<ThrowArmController>().pickupRange; //Shorten pickupRange address
            float p2Range = player2.GetComponentInChildren<ThrowArmController>().pickupRange; //Shorten pickupRange address
            if (player1.GetComponent<Damager>().dead == false && distanceFromP1 < p1Range && player1Cont.grab != 0) //If player1 won and is within range of throne and is grabbing...
            {
                //Throne Player:
                player1Cont.onThrone = true; //Tell player script to handle sitting on throne
                banners.GetComponent<AudioSource>().clip = throneSound; //Set throneSound clip
                banners.GetComponent<AudioSource>().Play(); //Play clip
                AudioClip[] playerLaugh = { laughSound }; //Make array with skull laugh sound for player
                player1Cont.yarger.shortYargs = playerLaugh; player1Cont.yarger.mediumYargs = playerLaugh; player1Cont.yarger.longYargs = playerLaugh; //Replace all yargs with skull laugh
                throneHighlight.SetActive(false); //Make throne highlight invisible
                healthTracker1.SetActive(false); healthTracker2.SetActive(false); //Disable health trackers
                CMvcam3.GetComponent<CinemachineVirtualCamera>().Follow = player1.transform; //Have zoom camera follow player transform
                CMvcam3.SetActive(true); //Immediately activate zoom camera
                winTime = 0; //Reset winTime
                //BREAKOUT:
                titleState = TitleState.ThroneIn; //Move on to next phase
            }
            else if (player2.GetComponent<Damager>().dead == false && distanceFromP2 < p2Range && player2Cont.grab != 0) //If player2 won and is within range of throne and is grabbing...
            {
                //Throne Player:
                player2Cont.onThrone = true; //Tell player script to handle sitting on throne
                banners.GetComponent<AudioSource>().clip = throneSound; //Set throneSound clip
                banners.GetComponent<AudioSource>().Play(); //Play clip
                AudioClip[] playerLaugh = { laughSound }; //Make array with skull laugh sound for player
                player2Cont.yarger.shortYargs = playerLaugh; player2Cont.yarger.mediumYargs = playerLaugh; player2Cont.yarger.longYargs = playerLaugh; //Replace all yargs with skull laugh
                throneHighlight.SetActive(false); //Make throne highlight invisible
                healthTracker1.SetActive(false); healthTracker2.SetActive(false); //Disable health trackers
                CMvcam3.GetComponent<CinemachineVirtualCamera>().Follow = player2.transform; //Have zoom camera follow player transform
                CMvcam3.SetActive(true); //Immediately activate zoom camera
                winTime = 0; //Reset winTime
                //BREAKOUT:
                titleState = TitleState.ThroneIn; //Move on to next phase
            }
            
        }

        //THRONE RESOLUTION PHASE:
        if (titleState == TitleState.ThroneIn)
        {
            winTime += Time.deltaTime; //Increment time tracker
            if (winTime > throneInZoomDelay && CMvcam4.activeSelf != true) //If it has come time for final camera zoom...
            {
                CMvcam4.GetComponent<CinemachineVirtualCamera>().Follow = CMvcam3.GetComponent<CinemachineVirtualCamera>().Follow; //Inherit target from previous camera
                CMvcam4.SetActive(true); //Activate final camera zoom
            }
            if (winTime > endTime) //If it has come time for next scene to load...
            {
                //BREAKOUT:
                if (player1Cont.onThrone == true)  //Load player1 KingBeard scene
                {
                    SceneManager.LoadScene("P1WinSkinSelect");
                }
                else if (player2Cont.onThrone == true) //Load player2 KingBeard scene
                {
                    SceneManager.LoadScene("P2WinSkinSelect");
                }
                else //Extenuating circumstance, load normal scene
                {
                    SceneManager.LoadScene("SkinSelect");
                }
            }
        }

        //CUTSCENE WAIT PHASE:
        if (titleState == TitleState.Cutscene)
        {
            //Nominally, do nothing.  Detect no inputs and wait for initiator of cutscene to resolve itself
        }

    }

    public void NextLetter()
    {
        //Causes letters of "SHIPWRECKERS" to appear until there are none left, at which point it causes "SUPER" to appear

        if (letterList.Count > 0) //If there are letters left to appear
        {
            GameObject letter = letterList[Random.Range(0, letterList.Count)]; //Get un-appeared letter from list
            lettersAppeared++; //Increment lettersAppeared
            float currentvolume = lettersAppeared * letterVolStep; if (currentvolume > 1) { currentvolume = 0; } //Find and clamp volume for letter

            letter.GetComponent<SpriteRenderer>().sortingOrder = 4; //Make letter visible
            if (skippingLetters != true) //Make letter appear whisper yarg, unless all letters are appearing at once
            {
                letter.GetComponent<AudioSource>().volume = currentvolume; //Set letter volume
                letter.GetComponent<AudioSource>().clip = letterAppearSounds[Random.Range(0, letterAppearSounds.Length)]; //Set letter appear sound
                letter.GetComponent<AudioSource>().Play(); //Play appear sound
            }

            letterList.Remove(letter); //Remove each letter from list after it has been used

        }
        else  //If there are no more letters left to appear, make "SUPER" appear
        {
            //BREAKOUT:
            titleState = TitleState.SuperFadein; //Forward state to next phase
        }
    }

    public void SkullLaugh()
    {
        //Causes skull to laugh
        banners.GetComponent<Animator>().SetTrigger("Laugh"); //Start laugh animation
        //banners.GetComponent<AudioSource>().clip = laughSound; //Set laugh clip
        //banners.GetComponent<AudioSource>().Play(); //Play laugh clip
        timeSinceLaugh = 0; //Reset time counter
    }

    public int IncrementSkindex(PlayerController.Player player, bool plus)
    {
        //Increments and auto-clamps given Skindex.  Player designates which player's skindex to do this to, "plus" designates which direction to increment in

        int result = 0; //Initialize result int
        if (player == PlayerController.Player.Player1) //Player1 Skindex:
        {
            if (plus == true)
            {
                skinGiver.p1Skindex++; //Increment skindex
                if (skinGiver.p1Skindex > skinGiver.skins.Length - 1) { skinGiver.p1Skindex = 0; } //Overflow
            }
            else
            {
                skinGiver.p1Skindex--; //Decrement skindex
                if (skinGiver.p1Skindex < 0) { skinGiver.p1Skindex = skinGiver.skins.Length - 1; } //Underflow
            }
            result = skinGiver.p1Skindex; //Set return output
            //Debug.Log("P1Skindex = " + skinGiver.p1Skindex);
        }
        else if (player == PlayerController.Player.Player2) //Player2 Skindex:
        {
            if (plus == true)
            {
                skinGiver.p2Skindex++; //Increment skindex
                if (skinGiver.p2Skindex > skinGiver.skins.Length - 1) { skinGiver.p2Skindex = 0; } //Overflow
            }
            else
            {
                skinGiver.p2Skindex--; //Decrement skindex
                if (skinGiver.p2Skindex < 0) { skinGiver.p2Skindex = skinGiver.skins.Length - 1; } //Underflow
            }
            result = skinGiver.p2Skindex; //Set return output
            //Debug.Log("P2Skindex = " + skinGiver.p2Skindex);
        }

        return result; //Return clamped int
    }

}
