using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleKiller : MonoBehaviour
{
    public float lifeTime;
    public ParticleSystem particomponent;


    void Start()
    {
        //particomponent = GetComponent<ParticleSystem>(); //Get particle system component
        //lifeTime = particomponent.main.duration; //Set lifetime of effect to duration of particles
    }

    // Update is called once per frame
    void Update()
    {
        lifeTime -= Time.deltaTime; //Decrement lifetime counter
        if (lifeTime < 0) //If effect's time has come
        {
            Destroy(gameObject); //Destroy self
        }
    }
}
