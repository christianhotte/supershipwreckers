using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrelController : MonoBehaviour
{
    /*  This script handles everything that has to do with Weapon Upgrade Barrels, including their contents, their animations, and
     *  their explosion behavior.  This script borrows heavily from ThrowableController.
     *  IMPORTANT NOTE: When adding weapons, their glowing animations need to be added to the WeaponBarrel Animator Controller and a
     *  parameter with name equal to that weapon's name ToString() needs to be created for everything to work properly
     */

    public enum PInRange { None, Player1, Player2, Both }; //Creates an enum for establishing which player is within range

    //Objects and Components:
    private Animator animator; //This barrel's animator
    internal Collider2D hitBox; //This barrel's collider
    private GameObject player1; //Player gameObject
    private GameObject player2; //Player gameObject
    private PlayerController p1Cont; //Player Controller
    private PlayerController p2Cont; //Player Controller

    //Inspector Stats:
    public GameObject barrelExplosion; //Explosion prefab for when barrel is destroyed after use
    public GameObject contains; //Weapon this barrel contains.  If empty, barrel will pick a random weapon (from pool)
    public GameObject[] weaponPool; //Weapons this barrel could contain, chosen at random if "contains" is left empty
    public bool cantUseInCombat; //If checked, barrel cannot be accessed when players are in combat
    public bool canUseIfSame; //If checked and barrel is set to specific weapon player has, player can't use this barrel
    public bool canBeSame; //If checked, weapon gotten from barrel can be same as player's current weapon
    public bool cycleGlow; //If checked and barrel contains a random weapon, barrel will cycle through glows of all weapons in weaponPool
    public float cycleSpeed; //If cycleGlow is checked, this determines how fast glow animator cycles through glows

    //Internal Math Variables:
    internal PInRange playerInRange; //Determines which players are able to pick up throwable, and if throwable should be highlighted


    void Start()
    {
        //Get Objects and Components:
        animator = GetComponent<Animator>(); //Get animator
        hitBox = GetComponent<Collider2D>(); //Get collider
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); //Get array of both players in scene
        if (players[0].GetComponent<PlayerController>().player == PlayerController.Player.Player1) //Assign players to their appropriate slots
        { player1 = players[0]; player2 = players[1]; }
        else { player1 = players[1]; player2 = players[0]; }
        p1Cont = player1.GetComponent<PlayerController>(); //Get player1 controller
        p2Cont = player2.GetComponent<PlayerController>(); //Get player2 controller

        //Set Glow Animator:
        if (contains != null) //If barrel contains a specific weapon...
        {
            animator.SetBool(contains.ToString(), true); //Tell animator to glow for that weapon
        }

        //Initialize Physics Preferences:
        IgnorePlayer(player1, true); //Ignore collisions with p1
        IgnorePlayer(player2, true); //Ignore collisions with p2

    }

    void Update()
    {
        //Update Positional Data:
        float distanceFromP1 = Vector2.Distance(transform.position, player1.transform.position); //Get distance from player1
        float distanceFromP2 = Vector2.Distance(transform.position, player2.transform.position); //Get distance from player2
            float p1Range = player1.GetComponentInChildren<ThrowArmController>().pickupRange; //Shorten pickupRange address
            float p2Range = player2.GetComponentInChildren<ThrowArmController>().pickupRange; //Shorten pickupRange address
            //Check if Within Pickup Range (and identify player(s) within range):
            if (distanceFromP1 <= p1Range &&                                           //If barrel is within range...
                p1Cont.onGround == true)                                               //...and player is on ground...
                { playerInRange = PInRange.Player1; } //Player1 in range
            if (distanceFromP2 <= p2Range &&                                           //If barrel is within range...
                p2Cont.onGround == true)                                               //...and player is on ground...
            {
                if (playerInRange == PInRange.Player1) { playerInRange = PInRange.Both; } //Both players in range
                else { playerInRange = PInRange.Player2; }  //Just Player2 in range
            }
            if (distanceFromP1 > p1Range && distanceFromP2 > p2Range) //If barrel is not within range of either player...
                { playerInRange = PInRange.None; } //Neither player in range
            if (cantUseInCombat == true && p1Cont.inCombat == true || cantUseInCombat == true && p2Cont.inCombat == true) //If combat access is denied
                { playerInRange = PInRange.None; } //Barrel cannot be used

        //Get Player Input:
        if (playerInRange == PInRange.Player1 && p1Cont.grab != 0) //Player1 in range and pressing equip button:
        {
            EatPlayer(player1); //Begin weapon switching process
        }
        else if (playerInRange == PInRange.Player2 && p2Cont.grab != 0) //Player1 in range and pressing equip button:
        {
            EatPlayer(player2); //Begin weapon switching process
        }

        //Update Animator:
        //Cycle Glow:
        if (contains == null)
            {
                if (cycleGlow == true)
                {
                    //Add cycling system where animation states match up with each other
                }
                else
                {
                    animator.SetBool("Random", true); //Use question mark glow animation
                }
            }
        if (playerInRange != PInRange.None) { animator.SetBool("PlayerInRange", true); } //Tell animator a player is in range if so
            else { animator.SetBool("PlayerInRange", false); } //Else, keep barrel idle


    }

    public GameObject DetermineContents(GameObject playerUsingBarrel)
    {
        //Used to either select the selected weapon of this barrel, or return a random weapon from within given pool

        GameObject containedWeapon; //Create gameObject variable to return (as selected contents at given time)
        PlayerController pCont = playerUsingBarrel.GetComponent<PlayerController>(); //The PlayerController of the player trying to access this barrel

        //Determine Contents:
        if (contains != null) //If barrel contains ONE specific weapon...
        {
            containedWeapon = contains; //Set barrel to contain specific weapon
        }
        else //If no weapon is specified, pick random weapon from pool...
        {
            int poolChoice = Random.Range(0, weaponPool.Length); //Get random weapon index within range of weapon pool
            containedWeapon = weaponPool[poolChoice]; //Pick weapon from pool
            if (canBeSame != true) //If weapon needs to be different from player's currently-equipped weapon
            {
                while (pCont.hookArm.name == (containedWeapon.name + "(Clone)")) //If picked weapon is the same as player's currently-equipped weapon
                {
                    poolChoice = Random.Range(0, weaponPool.Length); //Get new random weapon index
                    containedWeapon = weaponPool[poolChoice]; //Assign new weapon
                }
            }

        }

        return containedWeapon; //Return decided weapon
    }

    public void EatPlayer(GameObject player)
    {

        if (contains != null && canUseIfSame != true &&
            player.GetComponent<PlayerController>().hookArm.name == (contains.name + "(Clone)")) //Check if objects come from the same prefab
        {
            //If player is trying to use barrel but this barrel is the same as their current weapon...
            Debug.Log("Weapon Barrel did nothing");
            //Do nothing
        }
        else
        {
            Destroy(player.GetComponent<PlayerController>().hookArm); //Destroy player's current weapon
            GameObject newWeapon = Instantiate(DetermineContents(player)); //Create new weapon based on contents calculation
            player.GetComponent<PlayerController>().hookArm = newWeapon; //Assign new weapon to player using barrel
            newWeapon.transform.localScale = new Vector3(player.transform.localScale.x, 1, 1); //Set weapon orientation to player's
            newWeapon.transform.parent = player.transform; //Child weapon to player
            newWeapon.transform.position = Vector3.zero; //Lock weapon position to player

            Explode(); //Detonate barrel
        }
    }

    public void Explode() //Explodes barrel and destroys this gameObject
    {
        GameObject explosion = Instantiate(barrelExplosion); //Instantiate explosion
        explosion.transform.position = transform.position; //Move explosion to position of this barrel
        gameObject.SetActive(false); //"Destroy" this barrel once exploded

    }

    public void IgnorePlayer(GameObject player, bool ignore)
    {
        Physics2D.IgnoreCollision(hitBox, player.GetComponent<BoxCollider2D>(), ignore);
    }

}
