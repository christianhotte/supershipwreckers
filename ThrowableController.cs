using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowableController : MonoBehaviour
{
    /* Universal script for controlling throwable map elements. Detects player distance and controls throwable animator. Controls
     * how throwable behaves when thrown.
     */

    public enum PInRange { None, Player1, Player2, Both }; //Creates an enum for establishing which player is within range

    //Objects and Components:
    private Animator animator; //This throwable's animator
    public Collider2D hitBox; //This throwable's collider
    public Rigidbody2D rb; //This throwable's rigidBody
    private GameObject player1; //Player gameObject
    private GameObject player2; //Player gameObject
    internal ThrowArmController throwController; //ThrowArmController of the throwArm holding this item (if item is being held)
    private GameObject[] surfaces; //Objects in world item can stand on

    [Header("Throwable Stats:")] //Throwable Stat Variables:
    public Vector3 clipPoint; //Determines the orientation at which throwable sits in player hand when equipped
    public Vector2 velCancel; //How slow item needs to be going to be "stopped"
    public float clipRot; //Determines the rotation of object when equipped in player hand
    public bool knockoverable; //Sets whether this item can be collided with by players
    public bool destructible; //Sets whether this item can take damage and be destroyed by players
    public bool catchable; //Sets whether players can catch this item when it is thrown at them
    public bool shatterable; //Sets whether this item breaks when it hits player/ground, or if it persists in the world
    public bool knockbackable; //Sets whether this item is affected by weapon impact
    public float spin; //How fast this item spins when thrown (positive means clockwise spin, negative means counterclockwise)

    [Header("Damage Stats:")] //Throwable Damage Stat Variables:
    public Vector2 impactForce; //How much this item knocks a player back by upon impact
    public int damage; //How much damage this item deals when it hits a player

    //State Variables:
    internal bool onGround; //Tells programs whether or not this item is sitting on a surface
    internal PInRange playerInRange; //Determines which players are able to pick up throwable, and if throwable should be highlighted
    internal bool inHand; //Pulled true if this throwable is currently in the hand of a player
    internal GameObject thrownBy; //The player who most recently threw this throwable (prevents it from being immediately caught again)

    //Math Variables:


    void Start()
    {
        //Get Objects and Components:
        animator = GetComponent<Animator>(); //Get animator
        hitBox = GetComponent<Collider2D>(); //Get collider
        rb = GetComponent<Rigidbody2D>(); //Get rigidBody
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); //Get array of both players in scene
        if (players[0].GetComponent<PlayerController>().player == PlayerController.Player.Player1) //Assign players to their appropriate slots
        { player1 = players[0]; player2 = players[1]; } else { player1 = players[1]; player2 = players[0]; }

        //Initialize Throwable Physics:
        if (knockoverable != true) //If throwable is not knockoverable, set to not collide with players
        {
            IgnorePlayer(player1, true);
            IgnorePlayer(player2, true);
        }

        surfaces = GameObject.FindGameObjectsWithTag("Ground"); //Get all walkable surfaces in scene

        //Optionally Set as Starting Throwable:
        if (transform.IsChildOf(player1.transform)) { player1.GetComponentInChildren<ThrowArmController>().PickUp(gameObject); }
        else if (transform.IsChildOf(player2.transform)) { player1.GetComponentInChildren<ThrowArmController>().PickUp(gameObject); }

    }

    void Update()
    {
        //Detect Ground Hit:
        onGround = false; //Reset ground variable before loop
        for (int i = surfaces.Length; i > 0; i--)
        {
            BoxCollider2D ground = surfaces[i - 1].GetComponent<BoxCollider2D>(); //Get box collider off of each surface
            if (hitBox.IsTouching(ground) &&
                surfaces[i - 1].transform.position.y < transform.position.y)
            { onGround = true; } //Item is on ground if they are touching ground and are above the ground they are touching
        }
        //Detect Throw End:
        if (thrownBy != null && //If item has been thrown...
            Mathf.Abs(rb.velocity.x) <= velCancel.x && Mathf.Abs(rb.velocity.y) <= velCancel.y) //...and if item has "stopped moving"...
        {
            thrownBy = null; //Scrub thrownBy
            if (knockoverable != true) { IgnorePlayer(player1, true);  IgnorePlayer(player2, true);  }
            else                       { IgnorePlayer(player1, false); IgnorePlayer(player2, false); }
        }

        //Update Ownership:
        if (throwController != null) { inHand = true; } else { inHand = false; } //Update inHand

        //Update Positional Data:
        float distanceFromP1 = Vector2.Distance(transform.position, player1.transform.position); //Get distance from player1
        float distanceFromP2 = Vector2.Distance(transform.position, player2.transform.position); //Get distance from player2
        if (inHand == false && thrownBy == null) //If item is on ground...
        {
            float p1Range = player1.GetComponentInChildren<ThrowArmController>().pickupRange; //Shorten pickupRange address
            float p2Range = player2.GetComponentInChildren<ThrowArmController>().pickupRange; //Shorten pickupRange address
            //Check if Within Pickup Range (and identify player(s) within range):
            if (distanceFromP1 <= p1Range)                                          { playerInRange = PInRange.Player1;  } //Player1 in range
            if (distanceFromP2 <= p2Range) { if (playerInRange == PInRange.Player1) { playerInRange = PInRange.Both;     } //Both players in range
                else                                                                { playerInRange = PInRange.Player2; }} //Just Player2 in range
            if (distanceFromP1 > p1Range && distanceFromP2 > p2Range)               { playerInRange = PInRange.None;     } //Neither player in range
        }
        else if (thrownBy != null && catchable == true) //If item is in air and can be caught...
        {
            float distanceFromP = 0; //Initialize distanceFromP (only applies to player who didn't throw item)
            float catchRange = 0; //Initialize catchRange (only applies to player who didn't throw item)
            if (thrownBy == player1) { distanceFromP = distanceFromP2; } else { distanceFromP = distanceFromP1; } //Get correct distance value
            if (thrownBy == player1) { catchRange = player2.GetComponentInChildren<ThrowArmController>().catchRange; } //Get and shorten pickupRange address
            else                     { catchRange = player1.GetComponentInChildren<ThrowArmController>().catchRange; } //Get and shorten pickupRange address
            //Check if Within Catch Range (of player which didn't throw item):
            if (distanceFromP <= catchRange) { if (thrownBy == player1) { playerInRange = PInRange.Player2;   } //Open catch window
                                               else                     { playerInRange = PInRange.Player1; } } //Open catch window
        }

        //Update Animator:
        if (playerInRange != PInRange.None &&
            throwController == null) { animator.SetBool("PlayerInRange", true);  } //Tell animator to glow when a player is in range
        else                         { animator.SetBool("PlayerInRange", false); } //Otherwise, turn highlight off

    }

    public void IgnorePlayer(GameObject player, bool ignore)
    {
        Physics2D.IgnoreCollision(hitBox, player.GetComponent<BoxCollider2D>(), ignore);
    }

    public void DetectTarget() //Automatically enables collisions with player who is having this item thrown at them
    {
        if (thrownBy == player1) { Physics2D.IgnoreCollision(hitBox, player2.GetComponent<BoxCollider2D>(), false); }
        else if (thrownBy == player2) { Physics2D.IgnoreCollision(hitBox, player1.GetComponent<BoxCollider2D>(), false); }
    }
}
