using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public GameObject globalManager; //The persistent gameObject which carries information such as skin selection between scenes
    private SceneLoader sceneLoader; //GlobalManager's primary script for building new scenes and carrying information such as selected skins
    public GameObject player1Skin; //Player1 skin gameObject
    public GameObject player2Skin; //Player2 skin gameObject
    public GameObject readyPlayerOne; //Player1 ready flag
    public GameObject readyPlayerTwo; //Player2 ready flag
    public GameObject playerOneArm; //Player1 throwArm
    public GameObject playerTwoArm; //Player2 throwArm
    private SpriteRenderer p1SkinRen; //Player1 skin renderer
    private SpriteRenderer p2SkinRen; //Player2 skin renderer
    private SpriteRenderer rp1renderer; //Player1 flag spriteRenderer
    private SpriteRenderer rp2renderer; //Player2 flag spriteRenderer
    private Animator p1ArmAnimator; //Player1 throwArm Animator
    private Animator p2ArmAnimator; //Player2 throwArm Animator
    public bool nerd;
    public bool nerd2;
    public float goTime;
    internal float timeCounter;

    private void Start()
    {
        sceneLoader = globalManager.GetComponent<SceneLoader>();
        p1SkinRen = player1Skin.GetComponent<SpriteRenderer>();
        p2SkinRen = player2Skin.GetComponent<SpriteRenderer>();
        rp1renderer = readyPlayerOne.GetComponent<SpriteRenderer>();
        rp2renderer = readyPlayerTwo.GetComponent<SpriteRenderer>();
        p1ArmAnimator = playerOneArm.GetComponent<Animator>();
        p2ArmAnimator = playerTwoArm.GetComponent<Animator>();
    }

    private void Update()
    {
        //Check Input:
        if (Input.GetButtonDown("Cancel") == true) //For Player1 readyUp button press:
        {
            if (nerd == false && nerd2 == true && p1SkinRen.sprite == p2SkinRen.sprite) //If Player2 has already readied up with same skin
            {
                Debug.Log("Player1 is trying to pick claimed skin");
            }
            else //No skin conflict:
            {
                nerd = !nerd;
            }
        }
        if (Input.GetButtonDown("enter") == true) //For Player2 readyUp button press:
        {
            if (nerd2 == false && nerd == true && p2SkinRen.sprite == p1SkinRen.sprite) //If Player2 has already readied up with same skin
            {
                Debug.Log("Player2 is trying to pick claimed skin");
            }
            else //No skin conflict:
            {
                nerd2 = !nerd2;
            }
        }

        //Update Animators and Flags:
        if (rp1renderer.enabled != nerd) rp1renderer.enabled = nerd;
        if (rp2renderer.enabled != nerd2)  rp2renderer.enabled = nerd2;
        p1ArmAnimator.SetBool("Ready", nerd);
        p2ArmAnimator.SetBool("Ready", nerd2);


        //Check Ready State:
        if (nerd == true && nerd2 == true)
        {
            timeCounter += Time.deltaTime;
            if (timeCounter >= goTime && sceneLoader.sceneBuilt != true)
            {
                sceneLoader.BuildScene(sceneLoader.targetScene); //Call sceneLoader to build new scene
            }
        }
        else if (timeCounter != 0)
        {
            timeCounter = 0; //Reset timeCounter if either player stops being readied up (when needed)
        }

    }
}
