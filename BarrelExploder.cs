using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrelExploder : MonoBehaviour
{
    //Barrel Bits:
    public AudioClip breakSound;
    public GameObject Ring1;
    public GameObject Ring2;
    public GameObject Ring3;
    public GameObject Ring4;
    public GameObject Ring5;
    public GameObject Ring6;
    public GameObject Wood1;
    public GameObject Wood2;
    public GameObject Wood3;
    public GameObject Wood4;

    public float woodPower;
    public float woodPowerVariance;
    public float woodSpin;
    public float woodSpinVariance;
    public float ringPower;
    public float ringPowerVariance;
    public float ringTiltVariance;

    void Start()
    {
        GetComponent<AudioSource>().clip = breakSound;
        GetComponent<AudioSource>().Play();

        Ring1.GetComponent<Rigidbody2D>().AddRelativeForce(Vector3.up * (ringPower + Random.Range(-ringPowerVariance, ringPowerVariance)));
        Ring2.GetComponent<Rigidbody2D>().AddRelativeForce(Vector3.up * (ringPower + Random.Range(-ringPowerVariance, ringPowerVariance)));
        Ring3.GetComponent<Rigidbody2D>().AddRelativeForce(Vector3.up * (ringPower + Random.Range(-ringPowerVariance, ringPowerVariance)));
        Ring4.GetComponent<Rigidbody2D>().AddRelativeForce(Vector3.up * (ringPower + Random.Range(-ringPowerVariance, ringPowerVariance)));
        Ring5.GetComponent<Rigidbody2D>().AddRelativeForce(Vector3.up * (ringPower + Random.Range(-ringPowerVariance, ringPowerVariance)));
        Ring6.GetComponent<Rigidbody2D>().AddRelativeForce(Vector3.up * (ringPower + Random.Range(-ringPowerVariance, ringPowerVariance)));

        Ring1.GetComponent<Rigidbody2D>().AddTorque(Random.Range(-ringTiltVariance, ringTiltVariance));
        Ring2.GetComponent<Rigidbody2D>().AddTorque(Random.Range(-ringTiltVariance, ringTiltVariance));
        Ring3.GetComponent<Rigidbody2D>().AddTorque(Random.Range(-ringTiltVariance, ringTiltVariance));
        Ring4.GetComponent<Rigidbody2D>().AddTorque(Random.Range(-ringTiltVariance, ringTiltVariance));
        Ring5.GetComponent<Rigidbody2D>().AddTorque(Random.Range(-ringTiltVariance, ringTiltVariance));
        Ring6.GetComponent<Rigidbody2D>().AddTorque(Random.Range(-ringTiltVariance, ringTiltVariance));

        Wood1.GetComponent<Rigidbody2D>().AddRelativeForce(new Vector2(-1, 0.2f) * (woodPower + Random.Range(-woodPowerVariance, woodPowerVariance)));
        Wood2.GetComponent<Rigidbody2D>().AddRelativeForce(new Vector2(1, 0.2f) * (woodPower + Random.Range(-woodPowerVariance, woodPowerVariance)));
        Wood3.GetComponent<Rigidbody2D>().AddRelativeForce(new Vector2(0, 0.2f) * (woodPower + Random.Range(-woodPowerVariance, woodPowerVariance)));
        Wood4.GetComponent<Rigidbody2D>().AddRelativeForce(new Vector2(0, 1) * (woodPower + Random.Range(-woodPowerVariance, woodPowerVariance)));

    }
}
