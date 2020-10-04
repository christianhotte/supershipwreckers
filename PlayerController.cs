using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    /* Universal script for controlling player character, accesses individual child parts to carry out animations and moves
     */

    //Enums:
    public enum Player { Player1, Player2 }; //Creates an enum for establishing which player an object is on

    [Header("Object References:")] //Objects and Components:
    public GameObject mainCamera; //Main camera in scene
    public GameObject skin; //Currently equipped skin, set in menus
    public GameObject hookArm; //Currently equipped hook arm/weapon, changed by pickups
    public GameObject throwArm; //Permanently equipped throwing arm, not changed by code
    public GameObject legs; //Currently equipped legs, changed by pickups
    public GameObject fallBox; //Trigger hitbox that allows dropping through platforms
    public GameObject readyFlag; //Ready flag sprite to enable when readying up
    internal Rigidbody2D rb; //Player rigidbody
    internal Yarger yarger; //Player yarger
    internal BoxCollider2D hurtBox; //Player collider
    internal CameraRail cameraRail; //Scene flow governor script
    private GameObject[] surfaces; //Objects in world player can stand on
    private GameObject[] dropthroughs; //Objects in world player can stand on an drop through
    public GameObject otherPlayer; //Other player in world
    public Vector3 groundOffset; //Amount central transform is offset from feet (used to get exact ground calculations)
    public Vector3 squatPos; //Final destination of where to move player parts when squatting

    [Header("Player Selector")] //Input Variables:
    public Player player;    //Allows player number to be set in inspector
    private float  ready;         //Readyup control axis (when navigating skin selection)
    private float  prevReady;     //Readyup control axis from last frame (when navigating skin selection)
    private float  up;            //Up control axis
    private float  prevUp;        //Up control axis from last frame (used to emulate "getButtonUp")
    private float  down;          //Down control axis (redundant to up)
    private float  left;          //Left control axis
    internal float skinLeft;      //Left control axis from last frame (when navigating skin selection)
    private float  right;         //Right control axis (redundant to left)
    internal float skinRight;     //Right control axis (when navigating skin selection)
    private float  jump;          //Jump control axis
    private float  prevJump;      //Jump control axis from last frame
    private float  attack;        //Control axis for attacking with primary weapon [LABEL ARCADE BUTTON PLACEMENT HERE]
    private float  prevAttack;    //Attack control axis from last frame (used to emulate "getButtonDown")
    internal float grab;          //Control axis for grabbing and throwing items
    internal float prevGrab;      //Grab control axis from last frame (used to emulate "getButtonDown")
    private float[] yargPack = { 0, 0, 0, 0 }; //Array of yarg inputs to send to Yarger
    internal float yargInput1; //Yarg control axis 1 [LABEL ARCADE BUTTON PLACEMENT HERE]
    internal float yargInput2; //Yarg control axis 2 [LABEL ARCADE BUTTON PLACEMENT HERE]
    internal float yargInput3; //Yarg control axis 3 [LABEL ARCADE BUTTON PLACEMENT HERE]
    internal float yargInput4; //Yarg control axis 4 [LABEL ARCADE BUTTON PLACEMENT HERE]

    [Header("Player Stats")] //Player Stat Variables:
    public float walkAccel; //How fast player will reach max speed when walking normally
    public float walkSpeed; //Max speed player can walk normally
    public float jumpCooldown; //How long it takes a jump to recharge after player touches ground (after a jump)
    public Vector2 jumpForce; //How hard a player jumps and in what direction
    public float jumpAccel; //How fast player will reach max speed when jumping
    public float jumpStrafeSpeed; //How fast player can adjust their direction when in the air
    public float squatSpeed; //How fast player goes from standing to squatting (and vice versa)
    //public float squatCancelTime; //How long can player squat before their squat is automatically cancelled
    public float defaultLegSpeed; //How fast player legs move normally

    [Header("Combat Stats")]
    public float combatDistance; //How close player must be to another player to enter combat mode
    public float combatHeightVariance; //Prevents combat from occuring based purely on vertical distance
    [Space()]
    public float combatAccel; //How fast player will reach max speed when in combat
    public float combatSpeed; //Max speed player can go when in combat
    public float combatLegSpeed; //How fast player legs move when in combat
    [Space()]
    public Vector2 vaultForce; //How hard a player jumps (and in what direction) when in combat
    public float vaultCooldown; //How long it takes a vault to recharge after player touches ground (after a vault)
    public float vaultIFrames; //How long after initially vaulting player is invincible
    public float vaultGrav; //How much to scale gravity when vaulting (can make vaults floatier or faster)
    public float vaultSpinSpeed; //How fast player speens during a vault (should be fixed to complete 360 right as player touches ground)
    public float vaultSpinDelay; //How long to wait after vault begins to begin spin
    public float backVaultPenalty; //What percentage of vault force to have when vaulting backwards
    public float vaultSpinJank; //If a vault hits a wall and angle is this far or more from 0, do not count collision

    [Header("Gameplay Switches:")]
    public bool instaJump; //If checked, jumping happens immediately upon pushing jump button.  If not, player squats first, then jumps OnButtonUp
    public bool expandedVault; //If checked, player can vault and normal jump at any time.  If not, vaulting only works in combat and normal jumping only works out of combat
    public Vector3 thronePos; //What position player is in when seated at throne

    internal bool onGround; //Keeps track of whether or not player is on the ground
    internal bool onDropthrough; //Keeps track of whether or not player is on dropthroughable ground
    private GameObject currentDropthrough; //Stores dropthrough player is passing through
    private GameObject standingOn; //The ground player is currently standing on
    private bool ignoreGroundThisUpdate; //Used to except certain actions which would otherwise be immediately interruped by ground (like vaulting)
    internal bool facingEnemy; //Tracks whether or not player is facing enemy
    internal bool facingPlayer; //Tracks whether or not enemy is facing player
    private float enemyDistance; //Tracks how far away enemy is
    private float enemyVertDistance; //Tracks how high enemy is relative to player height

    //State Data:
    internal bool skinReady; //Pulled true during SkinSelect phase when this player is locked in with their skin
    private bool collisionsSetup; //Used by SetupCollisions method to make it only run when needed
    private bool walking; //Tells animator whether or not to move legs
    internal bool squatting; //Tells program whether or not player is in a squat
    private bool jumping; //Tells program whether or not player is mid-jump (intentional, not knockback)
    internal bool vaulting; //Tells program whether or not player is mid-vault (combat jump)
    private bool droppingThrough; //Tells program player is dropping through ground
    internal bool inCombat; //Determines whether player is close enough to another player to engage in combat
    internal float velocityUnlocked; //Unclamps velocity, handy for weapon knockback and jumping
    internal bool weaponContinuous; //Set upon each weapon equip, tells input processor whether attack is GetButtonDown or GetButton
    internal bool onThrone; //Locks all player movement and sets throwArm to appropriate position

    //Internal Math Variables:
    internal Vector2 inputForce; //How much force is being applied to the player body through player input
    private Vector2 moveForce; //Vector for where player is attempting to move to
    private float currentVelocity; //Keeps track of player horizontal speed during sideways movement
    private float jumpLock; //Timer which prevents player from jumping again immediately after a jump
    private float vaultRotDirection; //What direction player rotates when vaulting (and by how many degrees (nominally 360))
    private float currentVaultVel; //Keeps track of player rotation during vault spin
    private float vaultRot; //Keeps track of actual transform rotation when vaulting
    private float postVaultRot; //Keeps track of rotation after vault has ended (vault correction)
    private float vaultTime; //Keeps track of how long player has been vaulting for

    private void Awake()
    {
        //Find Other Player:
        GameObject[] findOther = GameObject.FindGameObjectsWithTag("Player");
        if (findOther[0].GetComponent<PlayerController>().player == player) { otherPlayer = findOther[1]; } //Get not this player
        else { otherPlayer = findOther[0]; }                                                                //Get not this player
    }

    void Start()
    {
        //Get Body Parts:
        if (GetComponentInChildren<WeaponController>() != null) //If there is a weapon childed to player
        {
            hookArm = GetComponentInChildren<WeaponController>().gameObject; //Assign default (pre-equipped) weapon to weapon slot
        }

        //Get Objects and Components:
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera"); //Get main camera in scene
        cameraRail = mainCamera.GetComponent<CameraRail>(); //Get CameraRail from camera
        rb = GetComponent<Rigidbody2D>(); //Get this gameobject's rigidbody
        yarger = skin.GetComponent<Yarger>(); //Get this gameobject's yarger
        hurtBox = GetComponent<BoxCollider2D>(); //Get this gameobject's collider
        surfaces = GameObject.FindGameObjectsWithTag("Ground"); //Get all walkable surfaces in scene
        dropthroughs = GameObject.FindGameObjectsWithTag("Dropthrough"); //Get all dropthroughable surfaces in scene
    }

    private void Update()
    {
        //Detect Inputs:
        if (cameraRail.titleState == CameraRail.TitleState.Play) //Standard input during play:
        {
            up =    Input.GetAxis(PlayerInputs(player, "Up"));    //Get variable for current up axis input (CHANGE TO JUMP)
            down =  Input.GetAxis(PlayerInputs(player, "Down"));  //Get variable for current down axis input
            left =  Input.GetAxis(PlayerInputs(player, "Left"));  //Get variable for current left axis input
            right = Input.GetAxis(PlayerInputs(player, "Right")); //Get variable for current right axis input
                                                                  //Debug.Log("Up = " + up);
                                                                  //Debug.Log("Down = " + down);
                                                                  //Debug.Log("Left = " + left);
                                                                  //Debug.Log("Right = " + right);
            yargInput1 = Input.GetAxis(PlayerInputs(player, "Yarg1"));  //Get variable for current yarg1 axis input
            yargInput2 = Input.GetAxis(PlayerInputs(player, "Yarg2"));  //Get variable for current yarg2 axis input
            yargInput3 = Input.GetAxis(PlayerInputs(player, "Yarg3"));  //Get variable for current yarg3 axis input
            //yargInput4 = Input.GetAxis(PlayerInputs(player, "Yarg4"));  //Get variable for current yarg4 axis input (deprecated)
            jump =       Input.GetAxis(PlayerInputs(player, "Jump"));   //Get variable for current jump axis input
            attack =     Input.GetAxis(PlayerInputs(player, "Attack")); //Get variable for current attack axis input
            grab =       Input.GetAxis(PlayerInputs(player, "Grab"));   //Get variable for current attack axis input
        }
        else if (cameraRail.titleState == CameraRail.TitleState.SkinSelect && skinReady != true) //Input set when selecting skin:
        {
            ready =     Input.GetAxis(PlayerInputs(player, "Select"));  //Get variable for current escape button input

            skinLeft =  Input.GetAxis(PlayerInputs(player,  "Left"));   //Get variable for current left axis input
            skinRight = Input.GetAxis(PlayerInputs(player, "Right"));   //Get variable for current right axis input

            yargInput1 = Input.GetAxis(PlayerInputs(player, "Yarg1"));  //Get variable for current yarg1 axis input
            yargInput2 = Input.GetAxis(PlayerInputs(player, "Yarg2"));  //Get variable for current yarg2 axis input
            yargInput3 = Input.GetAxis(PlayerInputs(player, "Yarg3"));  //Get variable for current yarg3 axis input
        }
        else if (cameraRail.titleState == CameraRail.TitleState.SkinSelect && skinReady == true) //Input set when readied up:
        {
            ready =      Input.GetAxis(PlayerInputs(player, "Select")); //Get variable for current escape button input

            yargInput1 = Input.GetAxis(PlayerInputs(player, "Yarg1"));  //Get variable for current yarg1 axis input
            yargInput2 = Input.GetAxis(PlayerInputs(player, "Yarg2"));  //Get variable for current yarg2 axis input
            yargInput3 = Input.GetAxis(PlayerInputs(player, "Yarg3"));  //Get variable for current yarg3 axis input
        }
        else if (cameraRail.titleState == CameraRail.TitleState.Win) //Input when a player is dead:
        {
            if (GetComponent<Damager>().dead == false) //If player is alive...
            {
                up =         Input.GetAxis(PlayerInputs(player, "Up"));     //Get variable for current up axis input
                down =       Input.GetAxis(PlayerInputs(player, "Down"));   //Get variable for current down axis input
                left =       Input.GetAxis(PlayerInputs(player, "Left"));   //Get variable for current left axis input
                right =      Input.GetAxis(PlayerInputs(player, "Right"));  //Get variable for current right axis input
                yargInput1 = Input.GetAxis(PlayerInputs(player, "Yarg1"));  //Get variable for current yarg1 axis input
                yargInput2 = Input.GetAxis(PlayerInputs(player, "Yarg2"));  //Get variable for current yarg2 axis input
                yargInput3 = Input.GetAxis(PlayerInputs(player, "Yarg3"));  //Get variable for current yarg3 axis input
                //yargInput4 = Input.GetAxis(PlayerInputs(player, "Yarg4"));  //Get variable for current yarg4 axis input (deprecated)
                jump =       Input.GetAxis(PlayerInputs(player, "Jump"));   //Get variable for current jump axis input
                attack =     Input.GetAxis(PlayerInputs(player, "Attack")); //Get variable for current attack axis input
                grab =       Input.GetAxis(PlayerInputs(player, "Grab"));   //Get variable for current attack axis input
            }
            else //If player is dead
            {
                left =  Input.GetAxis(PlayerInputs(player, "Left"));  //Get variable for current left axis input
                right = Input.GetAxis(PlayerInputs(player, "Right")); //Get variable for current right axis input
                jump =  Input.GetAxis(PlayerInputs(player, "Jump"));  //Get variable for current jump axis input
                down = Input.GetAxis(PlayerInputs(player, "Down"));   //Get variable for current down axis input
                attack = 0; //Scrub attack input
            }
        }
        else if (onThrone == true) //If player has taken seat on throne
        {
            //Preliminary Aesthetic Stuff:
            rb.isKinematic = true; rb.position = thronePos;     //Set to throne position and freeze there
            throwArm.GetComponent<Animator>().SetBool("Throne", true); //Set throwArm to throne position

            yargInput1 = Input.GetAxis(PlayerInputs(player, "Yarg1"));  //Get variable for current yarg1 axis input
            yargInput2 = Input.GetAxis(PlayerInputs(player, "Yarg2"));  //Get variable for current yarg2 axis input
            yargInput3 = Input.GetAxis(PlayerInputs(player, "Yarg3"));  //Get variable for current yarg3 axis input
        }

        //Check Readiness (only in Skin Selection):
        if (cameraRail.titleState == CameraRail.TitleState.SkinSelect)
        {
            if (ready != 0 && prevReady == 0) //OnButtonDown for ready up
            {
                if (skinReady == false) { //If not ready before, ready up
                    skinReady = true; //Toggle skinReady
                    throwArm.GetComponent<ThrowArmController>().skinReady = true; //Toggle skinReady on throwArm
                    readyFlag.GetComponent<SpriteRenderer>().enabled = true; //Display ready flag
                } else {                  //If already readied, back out
                    skinReady = false; //Toggle skinReady
                    throwArm.GetComponent<ThrowArmController>().skinReady = false; //Toggle skinReady on throwArm
                    throwArm.GetComponent<Animator>().SetTrigger("Drop"); //Have animator go back to position 1
                    readyFlag.GetComponent<SpriteRenderer>().enabled = false; //Hide ready flag
                }
            }
            prevReady = ready; //Store ready as memory variable
        }
        else if (readyFlag.GetComponent<SpriteRenderer>().enabled == true) { readyFlag.GetComponent<SpriteRenderer>().enabled = false; } //Otherwise hide ready flag
    }

    private void FixedUpdate()
    {
        //Check/Setup Collisions:
        //INITIAL COLLISION ESTABLISHMENT CHAIN:
        if (collisionsSetup != true) { SetupCollisions(); } //Set up collisions if they aren't set up for current state
        //PASSIVE DROPTHROUGH CHECK:
        for (int i = dropthroughs.Length; i > 0; i--) //Parse through all dropthroughs in scene
        {
            Vector3 groundPos = dropthroughs[i - 1].transform.position; //Get box collider off of each dropthroughable surface
            BoxCollider2D groundBox = dropthroughs[i - 1].GetComponent<BoxCollider2D>(); //Get box collider off of each dropthroughable surface
            if (droppingThrough == true && groundBox.gameObject == currentDropthrough) //If player is currently dropping through this surface, ignore
            { Debug.Log("Dropthrough ground ignored."); }
            else if (groundPos.y > (transform.position.y + groundOffset.y)) //If dropthroughable is ABOVE player...
            {
                Physics2D.IgnoreCollision(hurtBox, groundBox, true); //Ignore collisions with player (player can move through)
            }
            else //If dropthroughable is BELOW player...
            {
                Physics2D.IgnoreCollision(hurtBox, groundBox, false); //Honor collisions with player (player can stand on)
            }
        }
        //ACTIVE DROPTHROUGH CHECK:
        if (droppingThrough == true && fallBox.GetComponent<BoxCollider2D>().IsTouching(currentDropthrough.GetComponent<BoxCollider2D>()) == false)
        {
            //For Cancelling Dropthrough Physics Ignore:
            droppingThrough = false; //Tell program dropthrough has ended
            Physics2D.IgnoreCollision(hurtBox, currentDropthrough.GetComponent<BoxCollider2D>(), false); //Stop ignoring collisions with that surface
            currentDropthrough = null; //Reset current dropthrough tracker
        }

        //Detect Environment:
            //On Ground:
            onGround = false; //Reset ground variable before loop
            onDropthrough = false; //Reset dropthrough variable before loop
            standingOn = null; //Reset current ground variable
            for (int i = surfaces.Length; i > 0; i--)
            {
                BoxCollider2D ground = surfaces[i - 1].GetComponent<BoxCollider2D>(); //Get box collider off of each surface
                if (hurtBox.IsTouching(ground) &&
                    surfaces[i - 1].transform.position.y < transform.position.y)
                { onGround = true; //Player is on ground if they are touching ground and are above the ground they are touching
                  standingOn = ground.gameObject; } //Set this as ground being stood on
                //IMPORTANT NOTE: y = 0 on ground surfaces should be at the exact top of their hitboxes
            }
            for (int i = dropthroughs.Length; i > 0; i--)
            {
                BoxCollider2D ground = dropthroughs[i - 1].GetComponent<BoxCollider2D>(); //Get box collider off of each dropthroughable surface
                if (hurtBox.IsTouching(ground) && dropthroughs[i - 1].transform.position.y < transform.position.y && vaulting == false ||
                    hurtBox.IsTouching(ground) && dropthroughs[i - 1].transform.position.y < transform.position.y && vaulting == true && 
                                                                                                                     rb.velocity.y < 0 &&
                                                                                                                     TrueRotation() < vaultSpinJank)
                { onGround = true;  //Player is on ground if they are touching ground and are above the ground they are touching
                  onDropthrough = true;
                  standingOn = ground.gameObject; } //Set this as ground being stood on
                //IMPORTANT NOTE: y = 0 on ground surfaces should be at the exact top of their hitboxes
            }
            if (ignoreGroundThisUpdate == true) {        ignoreGroundThisUpdate = false; //Used by certain moves to bypass ground jankiness
                                                         if (onGround == true) { Debug.Log("Ground Ignored"); } //Tell debugger if this was used
                                                         onGround = false; } //Ignore ground just this one time
            if  (onGround == true && vaulting == true) { jumping =  false; //END VAULT: Set jump complete once player touches ground
                                                         vaulting = false;            //Set vault complete once player touches ground
                                                     vaultTime = 0;                   //Reset vaultTime
                                                     rb.rotation = 0;                 //Make sure rotation is locked to 0
                                                     rb.freezeRotation = true;        //Re-freeze rotation
                                                     rb.gravityScale = 1;             //Re-normalize gravity
                                                         jumpLock = vaultCooldown;}   //Apply vault cooldown
            else if (onGround == true && jumping == true) {jumping = false;           //END JUMP:  Set jump complete once player touches ground
                                                           jumpLock = jumpCooldown; } //Apply jump cooldown
            //Debug.Log(player.ToString() + " On Ground = " + onGround);

            //Find Enemy Direction/Distance:
            enemyDistance = transform.position.x - otherPlayer.transform.position.x;     //Get distance from player
            enemyVertDistance = transform.position.y - otherPlayer.transform.position.y; //Get distance (vertically) from player
            if (otherPlayer.transform.localScale.x * enemyDistance > 0) { facingPlayer = true; } else { facingPlayer = false; } //Get enemy scale relative to player
            if (transform.localScale.x * enemyDistance < 0) { facingEnemy = true; } else { facingEnemy = false; } //Get player scale relative to enemy
            //if (player == Player.Player2) { Debug.Log("Facing enemy = " + facingEnemy); } //Log direction only for one player

            //Combat State:
            if (GetComponent<Damager>().dead == true) { inCombat = false; } //Player can't be in combat if dead
            else if (Mathf.Abs(enemyDistance) <= combatDistance &&  //Determine whether or not players are close enough to wrassle
                facingEnemy == true)                                //Distance should be close enough and players should be facing each other
            {
                if (Mathf.Abs(enemyVertDistance) <= combatHeightVariance || //If players are roughly on the same height level...
                    vaulting == true || otherPlayer.GetComponent<PlayerController>().vaulting == true) //...or are vaulting (exception)...
                { inCombat = true; } //Players are in combat (or in the case of vaulting, stay in combat despite height differences)
                else { inCombat = false; }
            }
            else if (Mathf.Abs(enemyDistance) >= combatDistance) //Exit combat/remain out of combat
            {
                inCombat = false;
            }
            //Debug.Log("In Combat = " + inCombat);

            //Calculate Movement:
                //Squatting:
                if (onGround == true && down != 0 && GetComponent<Damager>().dead == false) //Squatting currently works the same in and out of combat:
                {
                    squatting = true; //Tell program to calculate movement for squatting
                    //NOTE: Squatting locks player out of moving or jumping, unless squat is part of the jump (established in other code segment)
                }
                else if (squatting != false) //Most state conditions hold priority over squat, and deactivate it automatically
                {
                    squatting = false; //Tell program to stop calculating squat movement
                }

                //Horizontal Movement:
                if (onGround == true && squatting == false) //Walking movement when player is touching ground:
                {
                    if (inCombat != true) { inputForce.x = walkAccel; } //Normal acceleration
                    else { inputForce.x = combatAccel; }  //Combat acceleration
                }
                else if (squatting == false) //Horizontal control when player is in the air:
                {
                    if (inCombat != true) { inputForce.x = jumpAccel; } //Acceleration when mid-jump
                    else { inputForce.x = 0; } //Acceleration when mid-vault
                }
                if (right == left) { inputForce.x = 0; } else if (left > right) { inputForce.x = -inputForce.x; } //Set force direction

                //Vertical Movement (jump/vault/dropthrough):
                //PLATFORM DROPTHROUGH
                if (droppingThrough == false && onDropthrough == true && down != 0 && jump != 0 && prevJump == 0) //If conditions are met...
                {
                    if (standingOn.CompareTag("Dropthrough")) //If the ground player is standing on is indeed a dropthrough...
                    {
                        currentDropthrough = standingOn; //Store identity of this dropThrough
                        onGround = false; //Tell program player is no longer on ground
                        droppingThrough = true; //Tell program player is dropping through dropthroughable
                        Physics2D.IgnoreCollision(hurtBox, standingOn.GetComponent<BoxCollider2D>(), true); //Temp ignore collisions with that surface
                    }
                }

                else if (onGround == true && jumping == false && jumpLock == 0) //If conditions for jump are met and jump input is detected...
                {
                    //NORMAL JUMP (non-combat):
                    if (expandedVault != true && inCombat != true && GetComponent<Damager>().dead == false) //NonExpandedVault: Jumping mechanics when not in combat (and not dead):
                    {
                        if (instaJump != true) //Player squats when holding jump button and jumps on release (deprecated)
                        {
                            if (prevJump == 0 && jump != 0 || prevJump != 0 && jump != 0) //OnButtonDown and OnButtonHold for prepping jump:
                            {
                                squatting = true; //Get into squat position, preparing for jump
                            }
                            else if (prevJump != 0 && jump == 0) //Commit actual jump OnButtonUp for jump button:
                            {
                                squatting = false; //Cancel squat
                                jumping = true; //Begin jump
                                rb.velocity = Vector2.zero; //Scrub velocity
                                rb.AddForce(jumpForce); //Apply jump force
                                ignoreGroundThisUpdate = true;
                            }
                        }
                        else
                        {
                            if (prevJump == 0 && jump != 0) //OnButtonDown for jump
                            {
                                squatting = false; //Cancel squat
                                jumping = true; //Begin jump
                                rb.velocity = Vector2.zero; //Scrub velocity
                                rb.AddForce(jumpForce); //Apply jump force
                                ignoreGroundThisUpdate = true;
                            }
                        }
                    }
                    //NORMAL VAULT (combat jump):
                    else if (expandedVault != true) //NonExpandedVault: Jumping mechanics when in combat:
                    {
                        if (prevJump == 0 && jump != 0) //OnButtonDown...
                        {
                            //INITIALIZE STATE VARIABLES:
                            squatting =              false; //Cancel squat (should be redundant)
                            jumping =                true;  //Begin vault
                            vaulting =               true;  //Add additional vault property (eventually condense into enum)
                            ignoreGroundThisUpdate = true;  //Prevent ground from instantly cancelling vault
                            
                            //APPLY VAULT FORCE:
                            Vector2 adjustedVaultForce = vaultForce; //Vaultforce (to be adjusted for direction)
                            adjustedVaultForce.x *= transform.localScale.x; //Point vault force toward direction facing
                            vaultRotDirection = (-transform.localScale.x * 360f); //Always vault in direction facing
                            rb.velocity = Vector2.zero; //Scrub velocity
                            rb.AddForce(adjustedVaultForce); //Apply adjusted vault force to player body
                        }
                    }
                    else //ExpandedVault: Jumping mechanics do not change based on combat
                    {
                        if (jump != 0 && prevJump == 0) //OnButtonDown for jump, direction depends on input and combat factors
                        {
                            //INITIALIZE STATE VARIABLES:
                            squatting = false; //Cancel squat (should be redundant)
                            jumping = true;  //Begin vault
                            ignoreGroundThisUpdate = true;  //Prevent ground from instantly cancelling vault
                            Vector2 adjustedVaultForce = vaultForce; //Vaultforce (to be adjusted later for direction)
                            vaultRotDirection = (transform.localScale.x * -360f); //Always vault in direction facing...
                            if ((rb.velocity.x * transform.localScale.x) < 0) //...except if player is vaulting backwards
                                { vaultRotDirection = (transform.localScale.x * 360f); //Backflip instead of frontflip
                                  adjustedVaultForce *= backVaultPenalty; } //Backwards vaults are less powerful

                            //APPLY JUMP/VAULT FORCE (depending on input):
                            if (left != 0 && right == 0) //If player is pushing left
                            {
                                vaulting = true;  //Add additional vault property (eventually condense into enum)
                                //APPLY VAULT FORCE:
                                adjustedVaultForce.x *= -1; //Point vault force toward direction of input
                                rb.velocity = Vector2.zero; //Scrub velocity
                                rb.AddForce(adjustedVaultForce); //Apply adjusted vault force to player body
                            }
                            else if (left == 0 && right != 0) //If player is pushing right
                            {
                                vaulting = true;  //Add additional vault property (eventually condense into enum)
                                //APPLY VAULT FORCE:
                                adjustedVaultForce.x *= 1; //Point vault force toward direction of input

                                rb.velocity = Vector2.zero; //Scrub velocity
                                rb.AddForce(adjustedVaultForce); //Apply adjusted vault force to player body
                            }
                            else if (GetComponent<Damager>().dead == false) //If no player input (and alive)
                            {
                                squatting = false; //Cancel squat
                                rb.velocity = Vector2.zero; //Scrub velocity
                                rb.AddForce(jumpForce); //Apply jump force
                                ignoreGroundThisUpdate = true;
                            }
                        }
                    }

                }

        //Commit Movement:
        if (velocityUnlocked == 0 && squatting == false && vaulting == false) //Factor inputforce into velocity (unless during knockback event):
        {
            rb.AddForce(inputForce); //Add force in desired direction
        }


        //Post-Movement Gate:
        if (jumping == true) //If velocity is specially unclamped (for a jump)
        {
            if (inCombat != true && expandedVault != true) //Normal jump velocity clamp:
            {
                rb.velocity = new Vector2(Mathf.Clamp(rb.velocity.x, -jumpStrafeSpeed, jumpStrafeSpeed), rb.velocity.y);
            }
        }
        else if (velocityUnlocked == 0) //If velocity is being clamped (which it is normally)
        {
            if (inCombat != true) //Clamp normal velocity:
            {
                rb.velocity = new Vector2(Mathf.Clamp(rb.velocity.x, -walkSpeed, walkSpeed), rb.velocity.y);
            }
            else //Clamp combat velocity (make exception for vaulting):
            {
                rb.velocity = new Vector2(Mathf.Clamp(rb.velocity.x, -combatSpeed, combatSpeed), rb.velocity.y);
            }
        }

            //Update Velocity Lock:
            if (velocityUnlocked > 0) //If velocity is unlocked
            {
                velocityUnlocked -= Time.deltaTime; //Decrement velocity lock by deltaTime
                if (velocityUnlocked < 0) { velocityUnlocked = 0; } //Clamp velocitylock to zero
            }

            //Update Jump Lock:
            if (jumpLock > 0) //If jumping is locked (on cooldown)
            {
                jumpLock -= Time.deltaTime; //Decrement jumpLock by deltaTime
                if (jumpLock < 0) { jumpLock = 0; } //Clamp jumpLock to zero
            }

        //Check Positional/Movement Data Collection/Postprocessing (mostly visual):
        float otherPlayerDirection = (otherPlayer.transform.position.x - transform.position.x);
        if (inCombat == true && otherPlayerDirection != 0) //If enemy gets behind player in combat:
        {
            if (squatting != true) //When not squatting, immediately turn to face enemy
            {
                if (otherPlayerDirection > 0)      { transform.localScale = new Vector2(1, 1); }  //Look at enemy when in combat
                else if (otherPlayerDirection < 0) { transform.localScale = new Vector2(-1, 1); } //Look at enemy when in combat
            }
            else { inCombat = false; } //If you are squatting, you have been outmaneuvered and you leave combat
        }
        else if (squatting != true) //If not in combat or direction is 0, set direction based on inputForce
        {
            if (inputForce.x > 0) { transform.localScale = new Vector2(1, 1); } //Set player character facing right (and walking)
            if (inputForce.x < 0) { transform.localScale = new Vector2(-1, 1); } //Set player character facing left (and walking)
        }
        if (inputForce.x != 0 && onGround == true) { walking = true; } else { walking = false; } //Set walking true/false depending on input applied
        //Squatting Position Update:
        if (squatting == true) //Player body needs to move down in conjunction with leg animation
        {
            Vector3 relSquatPos = transform.position + squatPos; //Correct for central transform offset
            skin.transform.position = relSquatPos; throwArm.transform.position = relSquatPos; hookArm.transform.position = relSquatPos;
        }
        else if (skin.transform.position != transform.position && GetComponent<Damager>().dead == false) //Check if position needs updating otherwise
        {
            Vector3 relSquatPos = transform.position;
            skin.transform.position = relSquatPos; throwArm.transform.position = relSquatPos; hookArm.transform.position = relSquatPos;
        }
        //Vaulting I-Frame/Speen Update:
        if (vaulting == true) //Player body spins when vaulting
        {
            vaultTime += Time.deltaTime; //Update vaultTime whenever vaulting
            if (vaultTime <= vaultIFrames) //Invincible until I-frames end:
            {
                Physics2D.IgnoreCollision(hurtBox, otherPlayer.GetComponentInChildren<WeaponController>().hitBox, true); //Enemy can't hurt player during I-frames
            } else {
                Physics2D.IgnoreCollision(hurtBox, otherPlayer.GetComponentInChildren<WeaponController>().hitBox, false); //Re-enable hurtbox/hitbox collisions when I-Frames end
            }

            if (vaultTime >= vaultSpinDelay) //After spin delay, begin spinning:
            {
                if (rb.freezeRotation != false) { rb.freezeRotation = false; } //Unfreeze rotation
                if (rb.gravityScale != vaultGrav) { rb.gravityScale = vaultGrav; }; //Adjust gravity

                vaultRot = Mathf.SmoothDamp(rb.rotation, vaultRotDirection, ref currentVaultVel, vaultSpinSpeed); //Adjust rotator variable
                //if (player == Player.Player1) Debug.Log("VaultRot = " + vaultRot);
                rb.rotation = vaultRot; //Commit dampened rotation to transform z axis
            }
        }
        else if (rb.rotation != 0 || rb.freezeRotation != true || rb.gravityScale != 1) //Clean up rotation if necessary when not vaulting
        {
            //NOTE: This is a failsafe primitive version of code already implemented earlier in script (probably compress into method)
            vaultTime = 0; //Reset vaultTime
            rb.rotation = 0; //Make sure rotation is otherwise locked to 0
            rb.freezeRotation = true; //Re-freeze rotation
            rb.gravityScale = 1; //Re-normalize gravity
            Physics2D.IgnoreCollision(hurtBox, otherPlayer.GetComponentInChildren<WeaponController>().hitBox, false); //Re-enable hurtbox/hitbox collisions
        }

        //Send Data (Animation or Otherwise) to Other Scripts:
            //Yarger:
                yargPack[0] = yargInput1; yargPack[1] = yargInput2; yargPack[2] = yargInput3; yargPack[3] = yargInput4; //Package yargs
                yarger.yargPack = yargPack; //Send yargPack to yarger for processing

            //Attack Arm:
                if (vaulting == true) { attack = 0; } //Can't attack while vaulting
                if (hookArm != null) //If there is a weapon equipped (there should be)
                {
                    if (weaponContinuous == true) //If weapon can attack continuously...
                    {
                        if (attack != 0)                    { hookArm.GetComponent<WeaponController>().attack = true;  }
                        else                                { hookArm.GetComponent<WeaponController>().attack = false; }
                    }
                    else //If weapon processes attack based on GetButtonDown
                    {
                        if (attack != 0 && prevAttack == 0) { hookArm.GetComponent<WeaponController>().attack = true;  }
                        else                                { hookArm.GetComponent<WeaponController>().attack = false; }
                    }
                }

            //Throw Arm:
                if (grab != 0 && prevGrab == 0) //On GrabButtonDown...
                {
                    ThrowArmController tharmCont = throwArm.GetComponent<ThrowArmController>(); //Shorten throwArmController address
                    if (tharmCont.heldItem == null) { tharmCont.PickUp(null); } //If throwArm is empty... attempt to pick up an item
                    else if (tharmCont.heldItem != null) { tharmCont.Throw(); } //If throwArm is holding throwable... throw the item
                }

        //Send Animation Data:
            //LEGS:
            if (vaulting == false && jumping == true) { legs.GetComponent<Animator>().SetBool("Jumping", true); } //Only do jump legs...
                else { legs.GetComponent<Animator>().SetBool("Jumping", false); }                                 //...for normal jump
            if (jumping == true && rb.velocity.y < 0) { legs.GetComponent<Animator>().SetBool("Falling", true); } //Detect and animate falling
                else { legs.GetComponent<Animator>().SetBool("Falling", false); }
            if (inCombat == true) { legs.GetComponent<Animator>().SetFloat("SpeedMult", combatLegSpeed); } //Tell animator to walk mega fast during combat
                else { legs.GetComponent<Animator>().SetFloat("SpeedMult", defaultLegSpeed); } //Set walk speed to standard
            legs.GetComponent<Animator>().SetBool("Walking", walking); //Tell animator whether or not player is walking
                legs.GetComponent<Animator>().SetBool("Squatting", squatting); //Tell animator whether or not player is squatting


        //Update Memory Variables at End of FixedUpdate:
        prevUp = up; //(deprecated)
        prevJump = jump;
        prevAttack = attack;
        prevGrab = grab;

    }





    public string PlayerInputs(Player player, string input)
    {
        /* Method for storing and easily changing player inputs
         * There will be one of these methods local to each player, so be careful not to set two players to same input scheme
         */

        string playerSet = "P1"; //Addendum at the beginning of every control, depends on player (default is Player1)
        if (player == Player.Player1) { playerSet = "P1"; }      //Set all controls to P1
        else if (player == Player.Player2) { playerSet = "P2"; } //Set all controls to P2
        string useInput = playerSet + input; //Build input name based on request parameters
        return (useInput); //Return constructed input name
    }

    public void SetupCollisions()
    {
        /* Method for initializing collisions (both on playerx/playery and playerx/weaponx)
         * Should be called at start of program after both players are fully loaded in, and after each weapon equip
         * Code is somewhat bloated in order to accomodate more granular info being sent out through console
         */

        bool setupSuccessful = true; //Initialize variable to track success of setup

        //IGNORE OTHER PLAYER'S HITBOX:
        if (otherPlayer.GetComponent<PlayerController>().hurtBox != null && //If other player's hurtbox is available to get...
            hurtBox != null) //...and this player's hurtbox is set up...
        {
            Physics2D.IgnoreCollision(hurtBox, otherPlayer.GetComponent<PlayerController>().hurtBox, true); //Ignore collisions with other player
            Debug.Log("Player-to-player collisions successfully disabled on " + player.ToString());
        }
        else
        {
            setupSuccessful = false; //Have program run this setup again next cycle
            Debug.Log(player.ToString() + " could not find other player's hitbox");
        }

        //HAVE WEAPON IGNORE IT'S PLAYER'S HITBOX:
        if (GetComponentInChildren<WeaponController>() != null) //If player has a childed weaponController (it should)...
        {
            if (GetComponentInChildren<WeaponController>().noHitBox != true) //If weapon has a hitbox...
            {
                if (GetComponentInChildren<WeaponController>().hitBox != null && //...and weapon hitbox is set up...
                    hurtBox != null) //...and this player's hurtbox is set up...
                {
                    Physics2D.IgnoreCollision(hurtBox, GetComponentInChildren<WeaponController>().hitBox, true); //Ignore collisions with own weapon
                    Debug.Log("Weapon collisions successfully disabled on " + player.ToString());
                }
                else
                {
                    setupSuccessful = false; //Have program run this setup again next cycle
                    Debug.Log(player.ToString() + "'s colliders are not all set up yet");
                }

            }
            else
            {
                Debug.Log("Weapon on " + player.ToString() + " has no hitbox");
            }
        }
        else
        {
            setupSuccessful = false; //Have program run this setup again next cycle
            Debug.Log(player.ToString() + " has no weapon equipped");
        }

        collisionsSetup = setupSuccessful; //Tell program whether or not this setup needs to be run again
    }

    private float TrueRotation()
    {
        float output = rb.rotation; //Get raw rotation variable
        while (output < 0) { output = 360 + output; } //If less than zero, underflow (repeat as necessary)
        while (output > 360) { output = 0 + (output - 360); } //If greater than 360, overflow (repeat as necessary)
        return output; //Return actual rotation
    }

    public void ScrubInputs()
    {
        //Scrubs all inputs, immediately setting them to 0

    ready = 0;         //Readyup control axis (when navigating skin selection)
    prevReady = 0;     //Readyup control axis from last frame (when navigating skin selection)
    up = 0;            //Up control axis
    prevUp = 0;        //Up control axis from last frame (used to emulate "getButtonUp")
    down = 0;          //Down control axis (redundant to up)
    left = 0;          //Left control axis
    skinLeft = 0;      //Left control axis from last frame (when navigating skin selection)
    right = 0;         //Right control axis (redundant to left)
    skinRight = 0;     //Right control axis (when navigating skin selection)
    jump = 0;          //Jump control axis
    prevJump = 0;      //Jump control axis from last frame
    attack = 0;        //Control axis for attacking with primary weapon [LABEL ARCADE BUTTON PLACEMENT HERE]
    prevAttack = 0;    //Attack control axis from last frame (used to emulate "getButtonDown")
    grab = 0;          //Control axis for grabbing and throwing items
    prevGrab = 0;      //Grab control axis from last frame (used to emulate "getButtonDown")
    yargInput1 = 0; //Yarg control axis 1 [LABEL ARCADE BUTTON PLACEMENT HERE]
    yargInput2 = 0; //Yarg control axis 2 [LABEL ARCADE BUTTON PLACEMENT HERE]
    yargInput3 = 0; //Yarg control axis 3 [LABEL ARCADE BUTTON PLACEMENT HERE]
    yargInput4 = 0; //Yarg control axis 4 [LABEL ARCADE BUTTON PLACEMENT HERE]
}
}
