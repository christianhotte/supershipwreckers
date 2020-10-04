using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LatchOnController : MonoBehaviour
{
    public GameObject replacementSkin; //Skin to apply to player when hit
    public float latchVelocity; //How fast (in x direction) object
    private float prevVelocity;
    private Rigidbody2D rb; //This throwable's rigidBody
    private BoxCollider2D hitBox; //This throwable's hitbox
    public AudioClip latchSound; //Sound this throwable makes when attaching to player head

    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); //Get rigidBody
        hitBox = GetComponent<BoxCollider2D>(); //Get box collider
    }

    private void FixedUpdate()
    {
        prevVelocity = Mathf.Abs(rb.velocity.x); //Update previous velocity
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && prevVelocity >= latchVelocity)
        {
            if (collision.gameObject.GetComponent<PlayerController>().skin.GetComponent<SpriteRenderer>().enabled == true) //Only follow through if player has visible skin to be latched on to
            {
                Instantiate(replacementSkin, collision.gameObject.transform); //Instantiate skin on player
                collision.gameObject.GetComponent<AudioSource>().clip = latchSound;
                collision.gameObject.GetComponent<AudioSource>().Play(); //Play latchSound
                gameObject.SetActive(false); //Deactivate self
            }
        }
    }
}
