using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowArmController : MonoBehaviour
{
    /* Script for controlling player throwArms. Contains methods to accept input from PlayerController, and governs stats related to
     * throwing items (i.e. throw power, throw angle, throw speed). Also is the script called to by events which cause player to drop
     * their held item (or which otherwise need to use throwArm. Also controls throwArm animator.
     */

    //Objects and Components:
    private PlayerController playerCont; //This arm's player's controller
    private Animator animator; //This arm's animator
    public GameObject heldItem; //The item (if any) currently being held by player
    private ThrowableController itemCont; //Held item's controller (also if applicable)
    private GameObject[] throwables = { }; //Create an array for parsing through throwables in scene

    [Header("Pickup Stats:")] //Pickup-related Stat Variables:
    public float pickupRange; //How close player needs to be to an item to pick it up (off the ground)
    public float catchRange; //How much distance player has to react to catch a throwable in the air

    [Header("Throwing Stats:")] //Throw-related Stat Variables:
    public Vector2 throwAngle; //Controls what direction items get thrown in (becomes a normalized vector)
    public float strength; //Base variable for controlling how hard any given object is thrown
    public float recoverSpeed; //How long after throwing it takes throwArm to return to idle position (when it can pick up objects again)

    //Math Variables:
    private float throwCooldown; //Counter for how long it has been since player has thrown item

    //State Variables:
    internal bool skinReady;

    void Start()
    {
        //Get Objects and Components:
        playerCont = GetComponentInParent<PlayerController>(); //Get player controller
        animator = GetComponent<Animator>(); //Get animator

    }

    void Update()
    {
        //Update Throw Cooldown:
        if (throwCooldown > 0) { throwCooldown -= Time.deltaTime; if (throwCooldown < 0) { throwCooldown = 0; } }

        //Update Animator:
        if (heldItem != null || skinReady == true) { animator.SetBool("Holding", true); } else { animator.SetBool("Holding", false); } //Set visible arm position
    }

    public void PickUp(GameObject throwable) //Equips throwable to player
    {
        //Find Pickup Candidates:
        if (throwable == null && throwCooldown == 0) //If no data is given for throwable identity...
        {
            float nearestDist = pickupRange + catchRange; //Initialize variable for finding closest throwable (size is arbitrary)
            throwables = GameObject.FindGameObjectsWithTag("Throwable"); //Populate array with throwables in scene
            for (int x =  throwables.Length; x > 0; x--) //Parse through all throwables in scene, checking if any are within grabbing range:
            {
                ThrowableController throwCont = throwables[x - 1].GetComponent<ThrowableController>(); //Shorten ThrowableController address
                if (throwCont.inHand == true) { } //Disregard if throwable is already held by someone else
                else if (throwCont.playerInRange == ThrowableController.PInRange.Both || //Find if throwable can be grabbed by player
                    playerCont.player == PlayerController.Player.Player1 && throwCont.playerInRange == ThrowableController.PInRange.Player1 ||
                    playerCont.player == PlayerController.Player.Player2 && throwCont.playerInRange == ThrowableController.PInRange.Player2)
                {   //This code is designed to have player pick up CLOSEST available throwable (maybe also factor in player facing direction)
                    float distance = Vector2.Distance(throwCont.transform.position, playerCont.transform.position); //Get distance from player
                    if (distance < nearestDist) { nearestDist = distance; throwable = throwables[x - 1]; } //Update position in grab priority
                }
            }
        }

        if (throwable != null) //If a pickupable candidate was found, pick it up:
        {
            //Local Upkeep:
            heldItem = throwable; //Set held item to selected throwable
            itemCont = throwable.GetComponent<ThrowableController>(); //Get throwable controller
            throwable.transform.SetParent(gameObject.transform); //oog
            //Attach Item:
            itemCont.rb.velocity = Vector2.zero; //Cancel item velocity
            itemCont.rb.angularVelocity = 0; //Cancel angular velocity
            itemCont.throwController = this; //Set this arm as throwController
            itemCont.rb.bodyType = RigidbodyType2D.Kinematic; //Lock item to position
            itemCont.hitBox.enabled = false; //Disable item hitbox
            //heldItem.transform.SetParent(transform); //Child throwable to this arm
                Vector3 clipPos = itemCont.clipPoint;                 //Initialize clip position
                clipPos.x *= transform.parent.transform.localScale.x; //Adjust for player direction
            heldItem.transform.position = clipPos + transform.parent.transform.position; //Move throwable to adjusted clip point (on hand)
            heldItem.transform.rotation = new Quaternion(0, 0, itemCont.clipRot, 0);
        }
        else { Debug.Log("No throwables in grab range"); }

    }

    public void Throw() //Throws held throwable
    {
        //Unattach Item:
        heldItem.transform.SetParent(null); //Disown throwable
        itemCont.hitBox.enabled = true; //Enable item hitbox
        itemCont.rb.bodyType = RigidbodyType2D.Dynamic; //Unlock item position
        itemCont.throwController = null; //Scrub throwController
        itemCont.thrownBy = playerCont.gameObject; //Set thrown by to player gameObject
        itemCont.DetectTarget(); //Automatically enable collisions between object and enemy
        //Execute Throw:
        itemCont.rb.AddForce((throwAngle.normalized * strength) * transform.parent.transform.localScale.x);

        //Local Upkeep:
        animator.SetTrigger("Throw"); //Tell animator to run throw animation
        heldItem = null; //Scrub held item
        itemCont = null; //Scrup item controller
        throwCooldown = recoverSpeed; //Set cooldown timer
    }

    public void Drop() //Drops held throwable
    {
        if (heldItem != null) //Player must be holding an item in order to drop it
        {
            //Unattach Item:
            heldItem.transform.SetParent(null); //Disown throwable
            itemCont.hitBox.enabled = true; //Enable item hitbox
            itemCont.rb.bodyType = RigidbodyType2D.Dynamic; //Unlock item position
            itemCont.throwController = null; //Scrub throwController
                                             //Local Upkeep:
            heldItem = null; //Scrub held item
            itemCont = null; //Scrup item controller
        }
    }

    public void ToggleReadyFlag() //Specifically used to affect ready up flag
    {

    }
}
