using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterFlasher : MonoBehaviour
{
    //Damage Components:
    public ParticleSystem bloodSpray;
    public AudioClip hurtSound;
    public Vector3 bloodOrigin; //Where on the player body blood spray originates

    //Objects and Components:
    private AudioSource audioSource;
    internal PlayerController playerCont;
    private SpriteRenderer skinRen;
    private SpriteRenderer weaponRen;
    private SpriteRenderer throwArmRen;
    private SpriteRenderer legRen;

    //State Variables:
    private bool gotComponents;

    void Start()
    {
        //Get Object and Components:
        gotComponents = GetComponents();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (gotComponents == false) { GetComponents(); }
    }

    public void FlashDamage() //Proto script, NOTE: move to Damager
    {
        //Activates all physical feedback related to being damaged
        //AUDIO:
        gameObject.GetComponent<AudioSource>().Play(); //Play hurt sound
        //PARTICLE EFFECTS:
        Transform sprayPos = playerCont.transform;
        sprayPos.localScale = new Vector3(transform.localScale.x, sprayPos.localScale.y, sprayPos.localScale.z); //Flip spray direction
        ParticleSystem spray = Instantiate(bloodSpray, sprayPos); //Create bloodspray and set to proper position
    }

    private bool GetComponents()
    {
        playerCont = GetComponentInParent<PlayerController>();
        bool success = true;
        if (playerCont != null)
        {
            if (playerCont.skin.GetComponent<SpriteRenderer>() != null) skinRen = playerCont.skin.GetComponent<SpriteRenderer>();
                else { success = false; }
            if (playerCont.hookArm.GetComponent<SpriteRenderer>() != null) weaponRen = playerCont.hookArm.GetComponent<SpriteRenderer>();
                else { success = false; }
            if (playerCont.throwArm.GetComponent<SpriteRenderer>() != null) throwArmRen = playerCont.throwArm.GetComponent<SpriteRenderer>();
                else { success = false; }
            if (playerCont.legs.GetComponent<SpriteRenderer>() != null) legRen = playerCont.legs.GetComponent<SpriteRenderer>();
                else { success = false; }
        }
        else
        {
            success = false;
        }

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
