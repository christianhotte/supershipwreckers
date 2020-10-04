using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class WeaponController : MonoBehaviour
{
    /* Universal script for controlling hook arm/weapon.  May recieve modification from custom weapon scripts. Attached (as component) to
     * every arm/weapon in the game, with baked-in basic combat stats.
     */

    //Objects and Components:
    private PlayerController playerController; //This weapon's player's controller
    private PlayerController enemyController; //This weapon's player's enemy's controller
    internal BoxCollider2D hitBox; //This weapon's hitbox
    private Animator animator; //This weapon's animator
    private Damager enemyDamager; //This weapon's player's enemy's damager
    private CinemachineImpulseSource screenShaker; //If not null, this weapon's screenShaker

    //Player Input:
    internal bool attack; //Set to true when player is pressing the attack button
    internal bool attacking; //Set to true when weapon is attacking (regardless of player input)

    [Header("Weapon Stats")]   //Weapon Stat Variables:
    public int damage;         //How much damage this weapon deals
    public Vector2 knockback;  //How much force this weapon applies to enemy on a successful hit
    public Vector2 recoil;     //How much force this weapon applies to player when fired
    public float freeVelocityTime; //Time (in seconds) after launching enemy that enemy velocity becomes re-locked (should be right as they touch ground)
    public bool continuousAttack; //Whether or not the player can hold down attack button and attack continuously, or must press it once per attack
    public bool instantHit; //If checked, weapon will deal damage and knockback immediately upon hit (better for weapons with longer hit times)
    public Vector3 screenShake; //If not zero, this will cause the screen to shake when used/makes impact
    public bool shakeOnFire; //If true (and weapon is screenShaker), screen will shake on attack.  Otherwise, screen will shake on hit
    public float windupTime;   //Time taken between attack input and attack hit
    public float hitTime;      //Time when hitBox can damage enemy
    public float recoveryTime; //Time taken after hitTime has ended but before player can attack again
    private float attackTime;  //Timer begins when attack starts and ends when attack ends, timing each separate phase

    [Header("Audio:")]
    public AudioClip windupSound; //The sound this weapon makes when it is winding up
    public AudioClip attackSound; //The sound this weapon makes when it attacks
    public AudioClip damageSound; //The sound this weapon makes when it hits a player
    public bool attackSoundInterrupts; //If pulled true, attacking interrupts windup sound with attack sound

    //Internal State Variables:
    internal bool attachedToPlayer; //Indicates whether weapon is attached to a player or not
    internal bool noHitBox; //Indicates that weapon has no hitbox.  This may be true for a projectile-exclusive weapon
    internal bool attackSoundInterrupted; //Indicates that attacksound has interrupted windupSound

    //Weapon Hit Confirmation Variables:
    internal float hitStarted; //Exact time attack began (used to check which weapon should parry which)
    private bool hitPlayer; //Pulled true if weapon hitBox touches enemy hurtBox during hitTime
    private bool alreadyHit; //Pulled true if player hits enemy early to avoid multi-hits
    private bool soundTriggered; //Causes attack sound to only be triggered once per attack cycle
    private bool screenShook; //Pulled true if screen has already been shook this hit

    void Start()
    {
        //Get Objects/Components:
        if (GetComponent<BoxCollider2D>() != null) //If weapon has a box collider...
        {
            hitBox = GetComponent<BoxCollider2D>(); //Get box collider
        }
        else { noHitBox = true; } //If no hitbox, inform program that this is the case
        animator = GetComponent<Animator>(); //Get animator
        if (screenShake != Vector3.zero && GetComponent<CinemachineImpulseSource>() != null) { screenShaker = GetComponent<CinemachineImpulseSource>(); } //Assign screenshaker if present

        //Check Starting Attachment Status:
        UpdatePlayerAttachment();
    }


    void Update()
    {
        //Attack Upkeep:
        if (attacking) //While player is attacking...
        {
            attackTime += Time.deltaTime; //Increment attack time tracker
        }

        //Detect Player Input:
        if (attack && attacking == false && playerController.vaulting == false) //Initiate new attack:
        {
            attacking = true; //Tell script to begin attack
            Debug.Log("Attack!");
        }

        //Execute Ongoing Attack:
        if (attacking) //If attack input is detected and weapon is not disabled
        {
            //WINDUP STAGE:
            if (attackTime <= windupTime)
            {
                animator.SetBool("Windup", true); //Tell animator to do windup animation

                if (soundTriggered == false && windupSound != null)
                {
                    GetComponent<AudioSource>().Stop(); //Stop playing previous sound
                    GetComponent<AudioSource>().clip = windupSound; //Set attackSound clip
                    GetComponent<AudioSource>().Play(); //Play attack sound
                    soundTriggered = true; //Tell program not to do this again until reset
                }
            }

            //HIT STAGE:
            else if (attackTime > windupTime && attackTime <= hitTime + windupTime && soundTriggered == true && attackSoundInterrupts == true && attackSoundInterrupted != true) //Optionally reset soundtrigger once
                {
                    soundTriggered = false; //Allow attackSound to play
                    attackSoundInterrupted = true; //Logs that attack sound has been interrupted
                }
            else if (attackTime > windupTime && attackTime <= hitTime + windupTime)
            {
                //Do Attack Sound:
                if (soundTriggered == false) //Trigger sound just once per attack cycle
                {
                    GetComponent<AudioSource>().Stop(); //Stop playing previous sound
                    GetComponent<AudioSource>().clip = attackSound; //Set attackSound clip
                    GetComponent<AudioSource>().Play(); //Play attack sound
                    soundTriggered = true; //Tell program not to do this again until reset
                }
                //Do Screen Shake:
                if (screenShaker != null && shakeOnFire == true && screenShook == false) //Shake screen if screenShake is on
                {
                    GetComponent<CinemachineImpulseSource>().GenerateImpulse(); //Trigger screenshake
                    screenShook = true; //Tell program not to do this again until reset
                }
                //Do Recoil:
                if (recoil != Vector2.zero) //If weapon has recoil...
                {
                    Vector2 directionalRecoil = new Vector2(recoil.x * -playerController.transform.localScale.x, recoil.y); //Factor in player direction
                    playerController.rb.AddForce(directionalRecoil); //Add directional recoil force
                }

                if (noHitBox != true) //Activate hit detection if this weapon has a hitbox:
                {
                    hitStarted = Time.realtimeSinceStartup; //Set hitStarted to time (in seconds) since game began
                    hitBox.enabled = true; //Begin hit detection
                }

                //Check Hit Results:
                if (instantHit == true && hitPlayer == true && alreadyHit == false)
                {
                    DealHit();
                    alreadyHit = true; //Ensure this only happens once per attack
                }

                animator.SetBool("Hit", true); //Tell animator to do hit animation
            }

            //RECOVERY STAGE:
            else if (attackTime > hitTime + windupTime && attackTime <= recoveryTime + hitTime + windupTime)
            {
                //Check Hit Results:
                if (instantHit != true && hitPlayer == true && alreadyHit == false) //On a successful hit...
                {
                    DealHit();
                    alreadyHit = true;
                } 

                //Reset hit variables after hit stage has ended:
                if (noHitBox != true)
                {
                    hitBox.enabled = false; //End hitbox
                    hitStarted = 0; //Reset hitStarted
                    hitPlayer = false; //Reset hitPlayer
                } 

                animator.SetBool("Recovery", true); //Tell animator to recover to normal position
            }

            //RESET:
            else if (attackTime > recoveryTime + hitTime + windupTime)
            {
                ResetAttack(); //Reset all variables used in attack
            }
        }

        //Set Animation Speeds:
        animator.SetFloat("WindupTime", windupTime);     //Set windup animation speed
        animator.SetFloat("HitTime", hitTime);           //Set hit animation speed
        animator.SetFloat("RecoveryTime", recoveryTime); //Set recovery animation speed

    }

    private void OnTriggerEnter2D(Collider2D collision) //Detects hits during HitTime
    {
        //ENEMY HIT:
        if (collision.gameObject == playerController.otherPlayer) //If attack hits other player's hurtBox...
        {
            Debug.Log("Hit Detected");
            if (enemyController.squatting == false || playerController.squatting == true)
            {
                hitPlayer = true; //Tell program a player was hit during hitTime
                Debug.Log(playerController.gameObject.ToString() + " has hit " + collision.gameObject.ToString() + "!");
            }
        }
    }

    public void DealHit()
    {
        enemyDamager.playerHealth -= damage; //Subtract damage from enemy health
        enemyDamager.FlashDamage(); //Execute damage animation on enemy
        enemyController.velocityUnlocked = freeVelocityTime; //Unlock velocity
        enemyController.rb.velocity = Vector2.zero; //Scrub velocity
        enemyController.rb.AddForce(knockback * playerController.transform.localScale); //Apply knockback and adjust for direction
        if (screenShaker != null && shakeOnFire == false && screenShook == false) //Shake screen if screenShake is on:
            { GetComponent<CinemachineImpulseSource>().GenerateImpulse(); screenShook = true; }
    }

    public void UpdatePlayerAttachment()
    {
        if (GetComponentInParent<PlayerController>() != null) //If weapon is attached to a player...
        {
            attachedToPlayer = true; //Tell rest of script weapon is attached to player
            if (playerController == null) { GetComponentInParent<PlayerController>().SetupCollisions(); } //Set up collisions with player if needed
            playerController = GetComponentInParent<PlayerController>(); //Set that player as this weapon's playerController
            transform.position = playerController.transform.position; //Clip on to player position
            enemyController = playerController.otherPlayer.GetComponent<PlayerController>(); //Get enemy controller
            enemyDamager = playerController.otherPlayer.GetComponent<Damager>(); //Set enemy damager
            enemyDamager.hurtSound = damageSound; //Set enemy hurt sound

            playerController.weaponContinuous = continuousAttack; //Tell playerController how to decide on what an attack input is
            playerController.GetComponent<Damager>().gotComponents = false; //Force Damager to update spriterenderer references
            if (playerController.player == PlayerController.Player.Player1)      GetComponent<SpriteRenderer>().sortingLayerName = "Player1 Sprites"; //Set sorting layer
            else if (playerController.player == PlayerController.Player.Player2) GetComponent<SpriteRenderer>().sortingLayerName = "Player2 Sprites"; //Set sorting layer

            Debug.Log(ToString() + " is attached to " + playerController.player.ToString());
        }
        else
        {
            attachedToPlayer = false; //Tell rest of script weapon is not attached to player
            playerController = null; //Weapon has no player controller
            Debug.Log(ToString() + " is not attached to a player");
        }
    }

    private void ResetAttack()
    {
        //Moved from reset phase in Update to method because this is used by parry system to bypass recovery

        animator.SetTrigger("Reset");        //Tell animator to reset weapon to idle position...
        animator.SetBool("Windup", false);   //"
        animator.SetBool("Hit", false);      //"
        animator.SetBool("Recovery", false); //"
        attacking = false; //Reset attack capability
        attackTime = 0; //Reset attack time counter
        alreadyHit = false; //Reset attack stop tracker
        soundTriggered = false; //Reset single sound trigger
        screenShook = false; //Reset screen shake tracker
        attackSoundInterrupted = false; //Reset attack sound interrupt
    }
}
