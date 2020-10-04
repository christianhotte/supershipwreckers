using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damager : MonoBehaviour
{
    /* This script goes on each player gameObject and controls how players take damage.  It reference variables in PlayerController,
     * and needs playerController to function, thus it basically works as an extension of PlayerController rather than a wholly independent
     * script.  For the sake of data flow, the intention with this script is to handle damage without having to add too many dependencies to
     * PlayerController itself.
     */

    //Damage Components:
    [Header("Objects and Components:")]
    public GameObject healthTracker; //The health display for this player
    public ParticleSystem bloodSpray; //The blood particle effect triggered on non-lethal damage
    public ParticleSystem deathBloodBurst; //The initial blood particle effect triggered upon death
    public ParticleSystem deathBloodSpray; //The looping blood particle effect triggered upon death
    public Vector3 bloodOrigin; //Where on the player body blood spray originates
    public AudioClip hurtSound; //The sound triggered on non-lethal damage (leave blank, should be assigned by weapon doing the hurting
    public AudioClip deathSound; //The sound this player makes when they die

    //Objects and Components:
    private PlayerController playerController; //This player's PlayerController
    private AudioSource audioSource;
    internal PlayerController playerCont;
    internal SpriteRenderer skinRen;
    private SpriteRenderer weaponRen;
    private SpriteRenderer throwArmRen;
    private SpriteRenderer legRen;
    private SpriteRenderer healthRen; //This player's healthTracker's spriteRenderer

    [Header("Health Stats")]   //Health Stat Variables:
    public bool instaDeath;    //If checked "true", any amount of damage from any source will kill player (like in Duck Game)
    public bool singleFlash;   //If checked, player will flash a solid color once when damaged and return to normal after flashTime
    public int playerHealth;   //How much health this player has (can be initially set in inspector but will be changed by scripts)

    [Header("Damage Stats")]        //Damage Stat Variables:
    public float flashTime; //How many frames the player will be invincible for after being hit
    public float flashSpeed; //How fast player flashes when being damaged
    public Color flashColor; //The color to paint sprite when damaged

    //State Variables:
    internal bool dead; //If pulled true, this player is dead
    internal bool gotComponents; //Pulled true once all components on this script are initialized
    private bool flashingDamage; //Kept true while damage is being flashed
    private float flashingTime; //Amount of time damage is being flashed (based on I frames)
    private float flashingInterval; //Internal time counter to time speed between flashes
    private bool flashOn; //Used by damage flasher to toggle damage flash

    void Start()
    {
        //Get Objects and Components:
        playerController = GetComponent<PlayerController>(); //Get PlayerController
        gotComponents = GetComponents();
        audioSource = GetComponent<AudioSource>();
        //Initiate HealthTracker Animator:
        healthTracker.GetComponent<Animator>().SetInteger("Health", playerHealth); //Set animator health to display
        healthTracker.GetComponent<Animator>().SetTrigger("UpdateHealth"); //Update health animator
    }

    private void Update()
    {
        if (gotComponents == false) { GetComponents(); } //Check for components if necessary

        //DEATH CHECK:
        if (playerHealth <= 0 && dead == false) //If player health has been reduced to zero
        {
            healthRen.enabled = false; //Disenable health renderer
            //Play Death Sound:
            GetComponent<AudioSource>().clip = deathSound; //Set deathSound as clip
            GetComponent<AudioSource>().volume = 1; //Max volume
            GetComponent<AudioSource>().Play(); //Play deathSound
            //Play Death Animation:
            Instantiate(deathBloodBurst, gameObject.transform); //Instantiate bloodBurst
            Instantiate(deathBloodSpray, gameObject.transform); //Instantiate bloodSpray
            playerCont.skin.GetComponent<SpriteRenderer>().enabled =     false; //Hide skin
            playerCont.throwArm.GetComponent<SpriteRenderer>().enabled = false; //Hide throwArm
            playerCont.hookArm.GetComponent<SpriteRenderer>().enabled =  false; //Hide weapon
            playerCont.legs.GetComponent<Animator>().SetBool("Dead", true); //Tell leg animator to display spine
            playerCont.throwArm.GetComponent<ThrowArmController>().Drop(); //Make sure player drops any held items
            //Decrease Player Movement:
            playerCont.walkSpeed = playerCont.walkSpeed / 5; //Decrease walk speed
            playerCont.vaultForce = new Vector2(50, 150);    //Decrease vault force
            playerCont.vaultGrav = 1;                        //Change vault gravity
            playerCont.vaultSpinSpeed = 0.15f;               //Change vault spin speed
            playerCont.vaultSpinDelay = 0.15f;               //Change vault spin delay
            //Die:
            dead = true; //Inform other scripts that this player has lost
        }


        if (flashingDamage == true)
        {
            flashingTime -= Time.deltaTime; //Decrease flashTime based on time
            if (singleFlash != true) //For multi-flash
            {
                flashingInterval -= Time.deltaTime; //Decrease flashSpeed based on time
                if (flashingInterval <= 0) //Once flashingInterval has been passed...
                {
                    flashingInterval = flashSpeed; //Reset flashingInterval
                    ToggleFlash(false); //Toggle flash
                    Debug.Log("Flashing Damage");
                }
                if (flashingTime <= 0) //Once flash has reached its duration
                {
                    flashingTime = 0; //Set invincibility time dormant
                    flashingDamage = false; //Tell program to no longer run this program
                    ToggleFlash(true); //Reset normal sprite color
                }
            }
            else //For single flash
            {
                if (flashingTime > 0 && flashOn == false) { ToggleFlash(false); } //Turn flash on immediately
                else if (flashingTime <= 0) { flashingTime = 0; flashingDamage = false; ToggleFlash(true); } //Turn flash off at end
            }
        }
    }

    public void FlashDamage()
    {
        //Activates all physical feedback related to being damaged
        //HEALTHTRACKER:
        healthTracker.GetComponent<Animator>().SetInteger("Health", playerHealth); //Set animator health to display
        healthTracker.GetComponent<Animator>().SetTrigger("UpdateHealth"); //Update health animator
        //AUDIO:
        gameObject.GetComponent<AudioSource>().clip = hurtSound; //Select hurt sound
        gameObject.GetComponent<AudioSource>().Play(); //Play hurt sound
        //PARTICLE EFFECTS:
        Transform sprayPos = playerCont.transform;
        sprayPos.localScale = new Vector3(transform.localScale.x, sprayPos.localScale.y, sprayPos.localScale.z); //Flip spray direction
        ParticleSystem spray = Instantiate(bloodSpray, sprayPos); //Create bloodspray and set to proper position
        //SPRITERENDERER FLASH:
        flashingDamage = true; //Tell program it is flashing damage
        flashingTime = flashTime; //Initialize invincibilityTime
        flashingInterval = flashSpeed; //Set flash speed
    }

    public void ToggleFlash(bool kill)
    {
        //Toggles colored damage flash on and off (or just off if kill is true)
        if (kill != true) { flashOn = !flashOn; } //Normal toggle, switches flash on/off based on what its previous state was
        else { flashOn = false; } //Set flash off always

        if (flashOn == true) //For flashing on:
        {
            healthRen.color =   flashColor; //Set health color
            skinRen.color =     flashColor; //Set skin color
            weaponRen.color =   flashColor; //Set weapon color
            throwArmRen.color = flashColor; //Set weapon color
            legRen.color =      flashColor; //Set leg color
        }
        else //For flashing off:
        {
            healthRen.color =   Color.white; //Reset health color
            skinRen.color =     Color.white; //Reset skin color
            weaponRen.color =   Color.white; //Reset weapon color
            throwArmRen.color = Color.white; //Reset weapon color
            legRen.color =      Color.white; //Reset leg color
        }

    }

    public bool GetComponents()
    {
        //Assemles all necessary components on this script, and attempts to do so until successful

        playerCont = GetComponentInParent<PlayerController>();
        bool success = true;
        if (playerCont != null)
        {
            if (healthTracker.GetComponent<SpriteRenderer>() != null) healthRen = healthTracker.GetComponent<SpriteRenderer>();
            else { success = false; }
            if (playerCont.skin.GetComponent<SpriteRenderer>() != null) skinRen = playerCont.skin.GetComponent<SpriteRenderer>();
            else { success = false; }
            if (playerCont.hookArm != null) weaponRen = playerCont.hookArm.GetComponent<SpriteRenderer>();
            else { success = false; }
            if (playerCont.throwArm.GetComponent<SpriteRenderer>() != null) throwArmRen = playerCont.throwArm.GetComponent<SpriteRenderer>();
            else { success = false; }
            if (playerCont.legs.GetComponent<SpriteRenderer>() != null) legRen = playerCont.legs.GetComponent<SpriteRenderer>();
            else { success = false; }
        }
        else
        { success = false; }

        if (success == true)
        {
            Debug.Log(gameObject.ToString() + ": component retrieval successful");
            gotComponents = true;
            transform.position = playerCont.gameObject.transform.position;
        }
        else { Debug.Log(gameObject.ToString() + ": component retrieval unsuccessful"); }

        return success;
    }
}
